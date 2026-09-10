using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    public sealed partial class VoxelWorld : MonoBehaviour,IGrassWorld,ITreeWorld,IFluidWorld
    {
        sealed class Resident
        {
            public int Revision, Token;
            public byte[] Cells;
            public GameObject View;
            public Mesh Mesh,FluidMesh;
            public GameObject FluidView;
            public bool Busy,Dirty=true;
        }
        readonly Dictionary<ChunkPos,Resident> chunks=new Dictionary<ChunkPos,Resident>();
        readonly Dictionary<(long x,long z),(int min,int max)> surfaceRanges=new Dictionary<(long,long),(int,int)>();
        readonly Dictionary<ChunkPos,Dictionary<int,byte>> edits=new Dictionary<ChunkPos,Dictionary<int,byte>>();
        readonly List<Task<ChunkBuild>> work=new List<Task<ChunkBuild>>();
        int nextToken;
        public TerrainGenerator Generator { get; private set; }
        public BlockPos Origin { get; private set; }
        public Transform Observer;
        public int ViewDistance=10;
        public float FogStart => Math.Max(48,(ViewDistance*32-24)*.80f);
        public float FogEnd => ViewDistance*32-16;
        public Material TerrainMaterial;
        Material fluidMaterial;
        public FluidSimulation FluidSimulation {get;private set;}
        float fluidAccumulator;
        public double LastFluidTickMs {get;private set;}
        public int ResidentCount => chunks.Count;
        public int ReadyCount => chunks.Values.Count(c=>c.Cells!=null);
        public int PendingCount => chunks.Values.Count(c=>c.Dirty);
        public int EditCount => edits.Values.Sum(e=>e.Count);
        public int MeshTriangles => chunks.Values.Sum(c=>c.Mesh==null||c.Mesh.subMeshCount==0?0:(int)c.Mesh.GetIndexCount(0)/3);
        public double LastBuildMs { get; private set; }
        public int RejectedJobs { get; private set; }
        public string Error { get; private set; }
        public event Action<Vector3> OriginShifted;
        public event Action<BlockPos> BlockChanged;
        public event Action ResidencyChanged;
        public Func<BlockPos,bool> IsOpenMachine;
        public event Action<BlockPos,byte> BlockMined;
        readonly HashSet<ChunkPos> treeMeshes=new HashSet<ChunkPos>();
        public TreeSimulation Trees {get;private set;}
        float treeAccumulator;
        public double LastTreeTickMs {get;private set;}
        public double MaxTreeTickMs {get;private set;}
        float nextDemand;
        ChunkPos lastCentre;
        bool stopped;
        readonly Dictionary<(long x,long z),SortedSet<int>> editedColumns=new Dictionary<(long,long),SortedSet<int>>();
        readonly Dictionary<(long x,long z),int> skyHeights=new Dictionary<(long,long),int>();
        readonly List<ChunkPos> grassChunks=new List<ChunkPos>();
        float grassAccumulator;
        public GrassSimulation Grass {get;private set;}
        public double LastGrassTickMs {get;private set;}

        public void Initialize(int seed)
        {
            Generator=new TerrainGenerator(seed);Origin=new BlockPos(0,0,0);
            Grass=new GrassSimulation(seed);Trees=new TreeSimulation();
            FluidSimulation=new FluidSimulation(Fluids.Registry);
            fluidMaterial=Resources.Load<Material>("Materials/Water");
            TerrainMaterial=Resources.Load<Material>("Materials/Terrain");
            TorchView=gameObject.AddComponent<TorchPresentation>();TorchView.Initialize(this);
        }
        public Vector3 Local(BlockPos p) => new Vector3((float)(p.X-Origin.X),p.Y-Origin.Y,(float)(p.Z-Origin.Z));
        public BlockPos Address(Vector3 local) => WorldPoint.FromLocal(local,Origin).Cell;
        public bool Ready(BlockPos p) => chunks.TryGetValue(p.Chunk,out var c)&&c.Cells!=null;
        public byte Get(BlockPos p)
        {
            if(edits.TryGetValue(p.Chunk,out var e)&&e.TryGetValue(p.Index,out byte b))return b;
            if(chunks.TryGetValue(p.Chunk,out var c)&&c.Cells!=null)
            {int i=p.Index;return c.Cells[ChunkMesher.Index(i%32,i/32%32,i/1024)];}
            return Generator.At(p);
        }
        public bool Solid(BlockPos p) => !Ready(p)||BlockId.Solid(Get(p))&&!(IsOpenMachine?.Invoke(p)??false);
        public Func<BlockPos,bool> CanRemoveMachine;
        public bool Remove(BlockPos p,byte expected) => (CanRemoveMachine?.Invoke(p)??true)&&expected!=0&&expected!=BlockId.Bedrock&&Change(p,expected,0);
        public bool Place(BlockPos p,byte id) => id==BlockId.Torch?PlaceTorch(p,p.Offset(0,-1,0)):BlockId.Placeable(id)&&(Get(p)==0||Fluids.IsFluid(Get(p)))&&Change(p,Get(p),id);
        public bool ChangeFluid(BlockPos p,byte expected,byte replacement)
            =>(expected==0||expected==BlockId.Torch||Fluids.IsFluid(expected))&&(replacement==0||Fluids.IsFluid(replacement))&&Change(p,expected,replacement,false);
        public bool Submerged(Vector3 point,out FluidDefinition fluid,out byte cell)
        {
            var p=Address(point);cell=Ready(p)?Get(p):(byte)0;fluid=Fluids.Registry.Get(cell);
            return fluid!=null&&point.y-Local(p).y<(Fluids.Registry.Get(Get(p.Offset(0,1,0)))==fluid?1:fluid.Height(cell));
        }
        public Vector3 Current(BlockPos p)
        {
            byte cell=Get(p);var f=Fluids.Registry.Get(cell);if(f==null)return Vector3.zero;
            Vector3 direction=f.IsFalling(cell)?Vector3.down:Vector3.zero;
            foreach(var d in FluidSimulation.Sides)
            {
                var n=p.Offset(d.x,0,d.z);if(!Ready(n))continue;byte other=Get(n);
                if(other==0||Fluids.Registry.Get(other)==f&&f.Level(other)>f.Level(cell))direction+=new Vector3(d.x,0,d.z);
            }
            return direction.normalized*f.CurrentSpeed;
        }
        public void AdvanceFluids(float dt)
        {
            if(stopped)return;fluidAccumulator+=dt;int steps=0;
            var clock=Stopwatch.StartNew();
            while(fluidAccumulator>=FluidSimulation.StepSeconds&&steps++<4)
            {FluidSimulation.Step(this);fluidAccumulator-=FluidSimulation.StepSeconds;}
            LastFluidTickMs=clock.Elapsed.TotalMilliseconds;
        }
        public bool Mine(BlockPos p,byte expected,ToolCapability tool,ToolTier tier=ToolTier.Diamond)
        {
            if(!BlockId.Mineable(expected,tool,tier))return false;
            bool fell=expected==BlockId.Log&&(tool&ToolCapability.Axe)!=0&&NaturalLog(p);
            if(!Remove(p,expected))return false;
            BlockMined?.Invoke(p,expected);
            if(fell)Trees.FellAbove(this,p);
            return true;
        }
        public bool Till(BlockPos p)=>Ready(p.Offset(0,1,0))&&Get(p.Offset(0,1,0))==0&&
            (Get(p)==BlockId.Grass?Change(p,BlockId.Grass,BlockId.Farmland):Change(p,BlockId.Dirt,BlockId.Farmland));
        public bool Plant(BlockPos p)=>Get(p.Offset(0,-1,0))==BlockId.Farmland&&Change(p,0,BlockId.PotatoPlant);
        public bool Grow(BlockPos p,byte expected)=>Get(p.Offset(0,-1,0))==BlockId.Farmland&&BlockId.Crop(expected)&&expected<BlockId.MaturePotatoPlant&&Change(p,expected,(byte)(expected+1));
        public bool Uproot(BlockPos p,byte expected)
        {
            if(!BlockId.Crop(expected)||!Change(p,expected,0,false,false))return false;
            BlockMined?.Invoke(p,expected);return true;
        }
        public bool NaturalLog(BlockPos p)=>Get(p)==BlockId.Log&&
            !(edits.TryGetValue(p.Chunk,out var e)&&e.ContainsKey(p.Index));
        public bool NaturalLeaf(BlockPos p)=>Get(p)==BlockId.Leaves&&
            !(edits.TryGetValue(p.Chunk,out var e)&&e.ContainsKey(p.Index));
        public bool RemoveTreeBlock(BlockPos p,byte expected,bool drop)
        {
            if(expected==BlockId.Log&&!NaturalLog(p))return false;
            if((expected!=BlockId.Log&&expected!=BlockId.Leaves)||p.Y<TerrainGenerator.MinY||p.Y>TerrainGenerator.MaxY||!Change(p,expected,0,false,false))return false;
            if(drop)BlockMined?.Invoke(p,expected);return true;
        }
        public void AdvanceTrees(float dt)
        {
            if(stopped)return;treeAccumulator+=dt;
            if(treeAccumulator<TreeSimulation.StepSeconds)return;
            treeAccumulator=Math.Min(treeAccumulator-TreeSimulation.StepSeconds,TreeSimulation.StepSeconds);
            if(Trees.PendingFells==0&&Trees.PendingLeaves==0&&treeMeshes.Count==0){LastTreeTickMs=0;return;}
            long began=Stopwatch.GetTimestamp();Trees.Step(this);
            var clock=Stopwatch.StartNew();
            foreach(var key in treeMeshes)
                if(chunks.TryGetValue(key,out var c)&&c.Cells!=null&&c.Dirty)Apply(ChunkMesher.Build(key,c.Revision,c.Cells),c);
            if(treeMeshes.Count>0)LastEditMeshMs=clock.Elapsed.TotalMilliseconds;
            treeMeshes.Clear();
            LastTreeTickMs=(Stopwatch.GetTimestamp()-began)*1000.0/Stopwatch.Frequency;MaxTreeTickMs=Math.Max(MaxTreeTickMs,LastTreeTickMs);
        }
        public bool TryRead(BlockPos p,out byte id)
        {id=0;if(!Ready(p))return false;id=Get(p);return true;}
        public byte SkyLight(BlockPos air)
        {
            var key=(air.X,air.Z);
            if(!skyHeights.TryGetValue(key,out int top))
            {
                top=Generator.OpaqueHeight(air.X,air.Z);
                if(editedColumns.TryGetValue(key,out var column)&&column.Count>0)top=Math.Max(top,column.Max);
                while(top>=TerrainGenerator.MinY&&!BlockId.Opaque(Get(new BlockPos(air.X,top,air.Z))))top--;
                skyHeights[key]=top;
            }
            // Leaves transmit daylight for grass; voxel light attenuation remains a later system.
            return air.Y>top?(byte)15:(byte)0;
        }
        public bool ChangeGrass(BlockPos p,byte expected,byte replacement)
            => (expected==1&&replacement==2||expected==2&&replacement==1)&&Change(p,expected,replacement,false);
        public void AdvanceGrass(float dt)
        {
            if(stopped||Observer==null)return;
            grassAccumulator+=dt;int steps=0;
            while(grassAccumulator>=GrassSimulation.StepSeconds&&steps++<4)
            {
                var clock=Stopwatch.StartNew();Grass.Step(this,grassChunks);LastGrassTickMs=clock.Elapsed.TotalMilliseconds;
                grassAccumulator-=GrassSimulation.StepSeconds;
            }
        }
        bool Change(BlockPos p,byte expected,byte replacement,bool immediate=true,bool requireReady=true)
        {
            if(p.Y<=TerrainGenerator.MinY||p.Y>TerrainGenerator.MaxY||Math.Abs(p.X)>TerrainGenerator.HorizontalLimit||Math.Abs(p.Z)>TerrainGenerator.HorizontalLimit||expected==BlockId.Bedrock)return false;
            if((requireReady&&!Ready(p))||Get(p)!=expected)return false;
            if(!edits.TryGetValue(p.Chunk,out var e)){e=new Dictionary<int,byte>();edits[p.Chunk]=e;}
            e[p.Index]=replacement;
            var columnKey=(p.X,p.Z);
            if(!editedColumns.TryGetValue(columnKey,out var column)){column=new SortedSet<int>();editedColumns[columnKey]=column;}
            if(BlockId.Opaque(replacement))column.Add(p.Y);else column.Remove(p.Y);
            if(BlockId.Opaque(expected)!=BlockId.Opaque(replacement))skyHeights.Remove(columnKey);
            // Update every resident halo touching the edit. Collision sees the change now.
            for(int cz=-1;cz<=1;cz++)for(int cy=-1;cy<=1;cy++)for(int cx=-1;cx<=1;cx++)
            {
                var key=p.Chunk.Offset(cx,cy,cz);if(!chunks.TryGetValue(key,out var resident))continue;
                var kv=new KeyValuePair<ChunkPos,Resident>(key,resident);
                var min=kv.Key.Min;long x=p.X-min.X,z=p.Z-min.Z;int y=p.Y-min.Y;
                if(x < -1 || x>32 || y < -1 || y>32 || z < -1 || z>32)continue;
                var c=kv.Value;c.Revision++;c.Dirty=true;
                if(!requireReady)treeMeshes.Add(kv.Key);
                if(c.Cells!=null)c.Cells[ChunkMesher.Index((int)x,y,(int)z)]=replacement;
            }
            // Masking the old block requires fresh meshes; their local surface work is prioritized.
            // Hide stale chunks immediately, publishing a synchronous local rebuild for this edit only.
            // 32^3 bounded meshing is measured independently from asynchronous streaming.
            // Automatically decaying leaves cannot be part of another leaf's valid
            // support path. The original support removal already scheduled affected leaves.
            if((expected==BlockId.Log||expected==BlockId.Leaves&&requireReady)&&replacement!=expected)Trees.SupportRemoved(this,p);
            TorchChanged(p,expected,replacement);
            FluidSimulation.Changed(this,p);
            if(!immediate){BlockChanged?.Invoke(p);return true;}
            var editClock=Stopwatch.StartNew();
            foreach(var kv in chunks.ToArray())
            {
                var c=kv.Value;if(!c.Dirty||c.Cells==null)continue;
                var min=kv.Key.Min;
                if(Math.Abs(p.X-min.X)>33||Math.Abs(p.Z-min.Z)>33||Math.Abs(p.Y-min.Y)>33)continue;
                var mesh=ChunkMesher.Build(kv.Key,c.Revision,c.Cells);Apply(mesh,c);
            }
            LastEditMeshMs=editClock.Elapsed.TotalMilliseconds;BlockChanged?.Invoke(p);return true;
        }
        public double LastEditMeshMs { get; private set; }

        void Update()
        {
            if(Generator==null||Observer==null||stopped)return;
            Shader.SetGlobalColor("_RRFogColour",RenderSettings.fogColor);
            Shader.SetGlobalVector("_RRFogRange",new Vector4(FogStart,FogEnd,0,0));
            if(Mathf.Abs(Observer.position.x)>512||Mathf.Abs(Observer.position.z)>512)
            {
                var shift=new Vector3(Mathf.Floor(Observer.position.x/32)*32,0,Mathf.Floor(Observer.position.z/32)*32);
                Origin=Origin.Offset((int)shift.x,0,(int)shift.z);
                foreach(var kv in chunks)if(kv.Value.View!=null)kv.Value.View.transform.position=Local(kv.Key.Min);
                OriginShifted?.Invoke(shift);
            }
            // The shader's macro palette repeats every 1024 m. Reduce the integer origin
            // before converting to float so shifts and distant coordinates preserve its phase.
            Shader.SetGlobalVector("_RRWorldOffset",new Vector4((Origin.X%1024+1024)%1024,0,(Origin.Z%1024+1024)%1024,0));
            var centre=Address(Observer.position).Chunk;
            if(Time.unscaledTime>=nextDemand||!centre.Equals(lastCentre))
            {nextDemand=Time.unscaledTime+0.4f;lastCentre=centre;Demand(centre);}
            var clock=Stopwatch.StartNew();
            for(int i=work.Count-1;i>=0;i--)
            {
                var t=work[i];if(!t.IsCompleted)continue;work.RemoveAt(i);
                if(t.IsFaulted){Error=t.Exception?.GetBaseException().Message;continue;}
                var result=t.Result;
                if(!chunks.TryGetValue(result.Position,out var c)||c.Token!=result.Token){RejectedJobs++;continue;}
                c.Busy=false;
                if(c.Revision!=result.Revision){RejectedJobs++;c.Dirty=true;continue;}
                if(result.HasSurfaceRange&&!surfaceRanges.ContainsKey((result.Position.X,result.Position.Z)))
                {surfaceRanges[(result.Position.X,result.Position.Z)]=(result.SurfaceMin,result.SurfaceMax);nextDemand=0;}
                Apply(result,c);LastBuildMs=result.Milliseconds;
                if(clock.Elapsed.TotalMilliseconds>4)break;
            }
            if(work.Count<2)
            {
                var candidates=chunks.Where(k=>k.Value.Dirty&&!k.Value.Busy).OrderBy(k=>Distance(k.Key,centre)).Take(2-work.Count).ToArray();
                foreach(var kv in candidates)Launch(kv.Key,kv.Value);
            }
        }
        static double Distance(ChunkPos a,ChunkPos b) {double x=a.X-b.X,z=a.Z-b.Z,y=a.Y-b.Y;return x*x+z*z+y*y*0.5;}
        void Demand(ChunkPos centre)
        {
            var wanted=new HashSet<ChunkPos>();
            var columns=new HashSet<(long,long)>();
            for(int z=-ViewDistance;z<=ViewDistance;z++)for(int x=-ViewDistance;x<=ViewDistance;x++)
            {
                if(x*x+z*z>ViewDistance*ViewDistance+1)continue;
                long cx=centre.X+x,cz=centre.Z+z;
                if(cx < -TerrainGenerator.HorizontalLimit/32||cx>=TerrainGenerator.HorizontalLimit/32||cz < -TerrainGenerator.HorizontalLimit/32||cz>=TerrainGenerator.HorizontalLimit/32)continue;
                columns.Add((cx,cz));
                int surface=Generator.Height(cx*32+16,cz*32+16)/32,lowest=surface-1,highest=surface+1;
                // Workers publish exact extrema with their first page. Mountain coverage expands
                // without a second synchronous column scan or an unbounded world-height cache.
                if(surfaceRanges.TryGetValue((cx,cz),out var range))
                {lowest=(int)BlockPos.FloorDiv(range.min,32)-1;highest=(int)BlockPos.FloorDiv(range.max,32);}
                // Large cavities can expose floors/ceilings far beyond the former three-layer
                // player band. Cover the view sphere vertically too, with one seam margin.
                int vertical=(int)Math.Ceiling(Math.Sqrt(Math.Max(0,ViewDistance*ViewDistance+1-x*x-z*z)))+1;
                int caveLow=centre.Y-vertical,caveHigh=Math.Max(centre.Y+1,Math.Min(highest,centre.Y+vertical));
                int low=Math.Max(TerrainGenerator.MinY/32,Math.Min(lowest,caveLow)),high=Math.Min(TerrainGenerator.MaxY/32,Math.Max(highest,caveHigh));
                // Keep surface coverage separate beyond the view sphere, and avoid loading
                // high empty sky columns merely to inspect underground caves.
                for(int y=low;y<=high;y++)
                {
                    if((y<caveLow||y>caveHigh)&&(y<lowest||y>highest))continue;
                    var p=new ChunkPos(cx,y,cz);wanted.Add(p);
                    if(!chunks.ContainsKey(p))chunks.Add(p,new Resident{Token=++nextToken});
                }
            }
            foreach(var key in chunks.Keys.Where(k=>!wanted.Contains(k)).ToArray())
            {Release(chunks[key]);chunks.Remove(key);ResidencyChanged?.Invoke();}
            foreach(var key in surfaceRanges.Keys.Where(k=>!columns.Contains(k)).ToArray())surfaceRanges.Remove(key);
            grassChunks.Clear();
            // Only a bounded neighbourhood ticks; unloaded/distant terrain receives no catch-up.
            grassChunks.AddRange(wanted.Where(p=>Math.Abs(p.X-centre.X)<=2&&Math.Abs(p.Z-centre.Z)<=2&&Math.Abs(p.Y-centre.Y)<=1)
                .OrderBy(p=>p.X).ThenBy(p=>p.Y).ThenBy(p=>p.Z));
            foreach(var key in skyHeights.Keys.Where(k=>Math.Abs(BlockPos.FloorDiv(k.x,32)-centre.X)>2||Math.Abs(BlockPos.FloorDiv(k.z,32)-centre.Z)>2).ToArray())skyHeights.Remove(key);
        }
        void Launch(ChunkPos p,Resident c)
        {
            c.Busy=true;int revision=c.Revision,token=c.Token;var generator=Generator;
            // Immutable edit snapshot prevents worker/main-thread dictionary races.
            var changes=new List<KeyValuePair<BlockPos,byte>>();
            for(int z=-1;z<=1;z++)for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
            {
                var key=p.Offset(x,y,z);
                if(!edits.TryGetValue(key,out var e))continue;
                foreach(var v in e)changes.Add(new KeyValuePair<BlockPos,byte>(key.Min.Offset(v.Key%32,v.Key/32%32,v.Key/1024),v.Value));
            }
            work.Add(Task.Run(()=>
            {
                var sw=Stopwatch.StartNew();byte[] cells=generator.Generate(p,out int surfaceMin,out int surfaceMax);var min=p.Min;
                foreach(var e in changes)
                {
                    long x=e.Key.X-min.X,z=e.Key.Z-min.Z;int y=e.Key.Y-min.Y;
                    if(x>=-1&&x<=32&&y>=-1&&y<=32&&z>=-1&&z<=32)cells[ChunkMesher.Index((int)x,y,(int)z)]=e.Value;
                }
                var result=ChunkMesher.Build(p,revision,cells);result.Token=token;result.HasSurfaceRange=true;result.SurfaceMin=surfaceMin;result.SurfaceMax=surfaceMax;result.Milliseconds=sw.Elapsed.TotalMilliseconds;return result;
            }));
        }
        void Apply(ChunkBuild result,Resident c)
        {
            if(c.View==null)
            {
                c.View=new GameObject("Chunk");c.View.transform.SetParent(transform,false);
                c.View.AddComponent<MeshFilter>();var r=c.View.AddComponent<MeshRenderer>();r.sharedMaterial=TerrainMaterial;
                r.shadowCastingMode=ShadowCastingMode.On;
            }
            c.View.transform.position=Local(result.Position.Min);
            var mesh=new Mesh{name="Voxel chunk",indexFormat=IndexFormat.UInt32};
            mesh.vertices=result.Vertices;mesh.normals=result.Normals;mesh.uv=result.UV;mesh.uv2=result.Tiles;mesh.triangles=result.Triangles;mesh.RecalculateBounds();
            c.View.GetComponent<MeshFilter>().sharedMesh=mesh;
            if(c.Mesh!=null)Destroy(c.Mesh);
            bool first=c.Cells==null;
            c.Mesh=mesh;c.Cells=result.Cells;c.Dirty=false;
            bool visibleFluid=result.FluidMesh.Indices.Length>0;
            if(c.FluidView==null&&visibleFluid)
            {
                c.FluidView=new GameObject("Fluid surface");c.FluidView.transform.SetParent(c.View.transform,false);
                c.FluidView.AddComponent<MeshFilter>();var renderer=c.FluidView.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=fluidMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            if(c.FluidMesh!=null)Destroy(c.FluidMesh);
            c.FluidMesh=visibleFluid?result.FluidMesh.ToMesh():null;
            if(c.FluidView!=null)
            {c.FluidView.SetActive(visibleFluid);c.FluidView.GetComponent<MeshFilter>().sharedMesh=c.FluidMesh;}
            if(first)
            {
                FluidSimulation.Ready(result.Position);ResidencyChanged?.Invoke();
                // The worker identifies exposed/unsettled cells. Stable source interiors and
                // shared source boundaries never enter the scheduled queue on mere residency.
                var min=result.Position.Min;
                foreach(int i in result.FluidMesh.ActiveCells)
                    FluidSimulation.Changed(this,min.Offset(i%32,i/32%32,i/1024));
            }
        }
        static void Release(Resident c)
        {if(c.View!=null)Destroy(c.View);if(c.Mesh!=null)Destroy(c.Mesh);if(c.FluidMesh!=null)Destroy(c.FluidMesh);}
        public bool Raycast(Vector3 start,Vector3 direction,float reach,out BlockPos hit,out byte id)
            => Raycast(start,direction,reach,out hit,out id,out _);
        public bool Raycast(Vector3 start,Vector3 direction,float reach,out BlockPos hit,out byte id,out Vector3Int face,bool fluidSources=false)
            => Trace(start,direction,reach,out hit,out id,out face,fluidSources,false);
        // Camera clearance shares movement's solid predicate, not interaction targeting.
        public bool RaycastSolid(Vector3 start,Vector3 direction,float reach,out BlockPos hit,out byte id)
            => Trace(start,direction,reach,out hit,out id,out _,false,true);
        bool Trace(Vector3 start,Vector3 direction,float reach,out BlockPos hit,out byte id,out Vector3Int face,bool fluidSources,bool solidsOnly)
        {
            hit=default;id=0;face=Vector3Int.zero;var cell=Address(start);
            Vector3 localCell=Local(cell),step=new Vector3(Math.Sign(direction.x),Math.Sign(direction.y),Math.Sign(direction.z));
            Vector3 delta=new Vector3(direction.x==0?float.PositiveInfinity:Mathf.Abs(1/direction.x),direction.y==0?float.PositiveInfinity:Mathf.Abs(1/direction.y),direction.z==0?float.PositiveInfinity:Mathf.Abs(1/direction.z));
            Vector3 t=new Vector3((direction.x>0?localCell.x+1-start.x:start.x-localCell.x)*delta.x,(direction.y>0?localCell.y+1-start.y:start.y-localCell.y)*delta.y,(direction.z>0?localCell.z+1-start.z:start.z-localCell.z)*delta.z);
            for(int axis=0;axis<3;axis++)if(direction[axis]==0)t[axis]=float.PositiveInfinity;
            float distance=0;
            for(int i=0;i<64&&distance<=reach;i++)
            {
                if(!Ready(cell)){if(solidsOnly){hit=cell;return true;}return false;}
                byte b=Get(cell);var fluid=Fluids.Registry.Get(b);
                if(solidsOnly?Solid(cell):b!=0&&(fluid==null||fluidSources&&fluid.IsSource(b))){hit=cell;id=b;return true;}
                int axis=t.x<t.y?(t.x<t.z?0:2):(t.y<t.z?1:2);
                distance=t[axis];t[axis]+=delta[axis];
                face=Vector3Int.zero;face[axis]=-(int)step[axis];
                cell=cell.Offset(axis==0?(int)step.x:0,axis==1?(int)step.y:0,axis==2?(int)step.z:0);
            }
            return false;
        }
        public bool Overlaps(Vector3 feet,float width,float height)
        {
            CollisionCells(feet,width,height,out var min,out var max);
            for(long z=min.Z;z<=max.Z;z++)for(int y=min.Y;y<=max.Y;y++)for(long x=min.X;x<=max.X;x++)if(Solid(new BlockPos(x,y,z)))return true;
            return false;
        }
        public bool OccupiesCell(Vector3 feet,float width,float height,BlockPos cell)
        {
            CollisionCells(feet,width,height,out var min,out var max);
            return cell.X>=min.X&&cell.X<=max.X&&cell.Y>=min.Y&&cell.Y<=max.Y&&cell.Z>=min.Z&&cell.Z<=max.Z;
        }
        void CollisionCells(Vector3 feet,float width,float height,out BlockPos min,out BlockPos max)
        {
            min=Address(feet+new Vector3(-width/2+0.001f,0.001f,-width/2+0.001f));
            max=Address(feet+new Vector3(width/2-0.001f,height-0.001f,width/2-0.001f));
        }
        public Vector3 Move(Vector3 feet,Vector3 delta,float width,float height,out bool grounded)
        {
            grounded=false;int steps=Math.Max(1,Mathf.CeilToInt(delta.magnitude/0.18f));var step=delta/steps;
            for(int i=0;i<steps;i++)for(int axis=0;axis<3;axis++)
            {
                if(Mathf.Abs(step[axis])<0.000001f)continue;
                Vector3 dest=feet;dest[axis]+=step[axis];
                if(!Overlaps(dest,width,height))feet=dest;
                else
                {
                    float low=0,high=1;
                    for(int b=0;b<9;b++){float t=(low+high)*0.5f;dest=feet;dest[axis]+=step[axis]*t;if(Overlaps(dest,width,height))high=t;else low=t;}
                    feet[axis]+=step[axis]*low;if(axis==1&&step.y<0)grounded=true;
                    step[axis]=0;
                }
            }
            return feet;
        }
        public void Stop() {stopped=true;if(TorchView!=null)TorchView.Clear();foreach(var c in chunks.Values)Release(c);chunks.Clear();surfaceRanges.Clear();}
        void OnDestroy() {Stop();}
    }
}
