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
            public int FailedRevision=-1,Failures;
            public long PendingSequence;
            public byte[] Cells;
            public GameObject View;
            public Mesh Mesh,FluidMesh;
            public GameObject FluidView;
            public bool Busy,Dirty=true;
        }
        readonly Dictionary<ChunkPos,Resident> chunks=new Dictionary<ChunkPos,Resident>();
        // Only pending pages participate in worker selection; a settled world does
        // not rescan thousands of resident pages every rendered frame.
        readonly Dictionary<ChunkPos,Resident> pendingMeshes=new Dictionary<ChunkPos,Resident>();
        readonly Dictionary<(long x,long z),(int min,int max)> surfaceRanges=new Dictionary<(long,long),(int,int)>();
        readonly Dictionary<ChunkPos,Dictionary<int,byte>> edits=new Dictionary<ChunkPos,Dictionary<int,byte>>();
        readonly List<(Task<ChunkBuild> task,ChunkPos position,int token,int revision)> work=new List<(Task<ChunkBuild>,ChunkPos,int,int)>();
        int nextToken;long nextPendingSequence,meshDispatches;
        readonly HashSet<ChunkPos> wanted=new HashSet<ChunkPos>();
        readonly HashSet<(long,long)> wantedColumns=new HashSet<(long,long)>();
        readonly List<ChunkPos> releaseChunks=new List<ChunkPos>();
        readonly Queue<(GameObject view,Mesh terrain,Mesh fluid)> retiredViews=new Queue<(GameObject,Mesh,Mesh)>();
        public const int ViewTeardownBudget=4;
        public int PendingViewTeardowns=>retiredViews.Count;
        readonly List<(long,long)> releaseColumns=new List<(long,long)>();
        readonly List<(long x,long z)> releaseSky=new List<(long x,long z)>();
        public TerrainGenerator Generator { get; private set; }
        public BlockPos Origin { get; private set; }
        public Transform Observer,ViewObserver;
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
        public int PendingCount => pendingMeshes.Count;
        public int RunningJobs=>work.Count;
        public int EditCount => edits.Values.Sum(e=>e.Count);
        public int MeshTriangles => chunks.Values.Sum(c=>c.Mesh==null||c.Mesh.subMeshCount==0?0:(int)c.Mesh.GetIndexCount(0)/3);
        public double LastBuildMs { get; private set; }
        public int RejectedJobs { get; private set; }
        public int GenerationJobs {get;private set;}
        public int RemeshJobs {get;private set;}
        public int DemandPasses {get;private set;}
        public string Error { get; private set; }
        public event Action<Vector3> OriginShifted;
        public event Action<BlockPos> BlockChanged;
        public event Action ResidencyChanged;
        public event Action<ChunkPos> ChunkResidencyChanged;
        public event Action<ChunkPos,byte[]> ChunkReady;
        public Func<BlockPos,bool> IsOpenMachine;
        public Func<IEnumerable<ChunkPos>> PersistentChunkTickets;
        public void RefreshChunkTickets(){demandChanged=true;nextDemand=0;}
        readonly HashSet<ChunkPos> ticketedChunks=new HashSet<ChunkPos>();
        public event Action<BlockPos,byte> BlockMined;
        readonly HashSet<ChunkPos> treeMeshes=new HashSet<ChunkPos>();
        public TreeSimulation Trees {get;private set;}
        float treeAccumulator;
        public double LastTreeTickMs {get;private set;}
        public double MaxTreeTickMs {get;private set;}
        float nextDemand;
        int lastViewDistance=-1;
        bool demandChanged=true;
        ChunkPos lastCentre;
        bool stopped;
        readonly Dictionary<(long x,long z),SortedSet<int>> editedColumns=new Dictionary<(long,long),SortedSet<int>>();
        readonly Dictionary<(long x,long z),int> skyHeights=new Dictionary<(long,long),int>();
        readonly List<ChunkPos> grassChunks=new List<ChunkPos>();
        float grassAccumulator;
        public GrassSimulation Grass {get;private set;}
        public double LastGrassTickMs {get;private set;}

        public void Initialize(int seed,string generatorVersion=TerrainGenerator.Version)
        {
            Generator=new TerrainGenerator(seed,generatorVersion);Origin=new BlockPos(0,0,0);
            Grass=new GrassSimulation(seed);Trees=new TreeSimulation();
            FluidSimulation=new FluidSimulation(Fluids.Registry);
            fluidMaterial=Resources.Load<Material>("Materials/Water");
            TerrainMaterial=Resources.Load<Material>("Materials/Terrain");
            lightTableDirty=true;RenderPipelineManager.beginCameraRendering+=LightCamera;
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
            return GeneratorFor(p.Chunk).At(p);
        }
        public bool Solid(BlockPos p) => !Ready(p)||BlockId.Solid(Get(p))&&!(IsOpenMachine?.Invoke(p)??false);
        public Func<BlockPos,bool> CanRemoveMachine;
        public bool Remove(BlockPos p,byte expected) => BedId.Part(expected)?RemoveBed(p,expected): IndustryId.DoorPart(expected)?RemoveDoor(p,expected): (CanRemoveMachine?.Invoke(p)??true)&&expected!=0&&expected!=BlockId.Bedrock&&Change(p,expected,0);
        public bool Place(BlockPos p,byte id) => id==BedId.Bed?PlaceBed(p,0): id==BlockId.Sapling?PlantSapling(p): id==IndustryId.WoodenDoor?PlaceDoor(p): id==BlockId.Torch?PlaceTorch(p,p.Offset(0,-1,0)):BlockId.Placeable(id)&&(Get(p)==0||Fluids.IsFluid(Get(p)))&&Change(p,Get(p),id);
        public bool ChangeFluid(BlockPos p,byte expected,byte replacement)
            =>(expected==0||expected==BlockId.Torch||expected==BlockId.Sapling||Fluids.IsFluid(expected))&&(replacement==0||Fluids.IsFluid(replacement)||expected==Fluids.Lava.Source&&replacement==BlockId.LavaRock)&&Change(p,expected,replacement,false);
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
        public bool RecoveringMachine {get;private set;}
        public bool SuppressMiningDrops {get;private set;}
        public bool Mine(BlockPos p,byte expected,ToolCapability tool,ToolTier tier=ToolTier.Diamond,bool creative=false,bool drop=true)
        {
            if(!BlockId.Mineable(expected,creative?ToolCapability.Pickaxe:tool,creative?ToolTier.Diamond:tier))return false;
            bool fell=expected==BlockId.Log&&(tool&ToolCapability.Axe)!=0&&NaturalLog(p);
            bool recovering=RecoveringMachine,suppress=SuppressMiningDrops;
            RecoveringMachine=true;
            // Scope suppression to this removal and its synchronous attachment/content
            // callbacks. Environmental drops and later player actions keep normal rules.
            SuppressMiningDrops|=creative&&!drop;
            try
            {
                if(!Remove(p,expected))return false;
                RecoveringMachine=recovering;
                if((BlockDefinitions.Get(expected).Traits&BlockTraits.NoMiningDrop)==0)BlockMined?.Invoke(p,expected);
                if(fell)Trees.FellAbove(this,p);
                return true;
            }
            finally { RecoveringMachine=recovering;SuppressMiningDrops=suppress; }
        }
        public bool Till(BlockPos p)=>Ready(p.Offset(0,1,0))&&Get(p.Offset(0,1,0))==0&&
            (Get(p)==BlockId.Grass?Change(p,BlockId.Grass,BlockId.Farmland):Change(p,BlockId.Dirt,BlockId.Farmland));
        public bool Plant(BlockPos p,byte planting=BlockId.Potato)
        {var crop=CropRules.Planting(planting);return crop!=null&&Get(p.Offset(0,-1,0))==BlockId.Farmland&&Change(p,0,crop.first);}
        // Growth changes authoritative state now; all stages in a page share queued mesh work.
        public bool Grow(BlockPos p,byte expected)
        {var crop=CropRules.For(expected);return crop!=null&&crop.Supports(Get(p.Offset(0,-1,0)))&&expected<crop.Mature&&Change(p,expected,(byte)(expected+1),false);}
        public bool Uproot(BlockPos p,byte expected)
        {
            if((!BlockId.Crop(expected)&&expected!=BlockId.Sapling)||!Change(p,expected,0,false,false))return false;
            BlockMined?.Invoke(p,expected);return true;
        }
        public bool NaturalLog(BlockPos p)=>Get(p)==BlockId.Log&&
            (grownTreeCells.Contains(p)||!(edits.TryGetValue(p.Chunk,out var e)&&e.ContainsKey(p.Index)));
        public bool NaturalLeaf(BlockPos p)=>Get(p)==BlockId.Leaves&&
            (grownTreeCells.Contains(p)||!(edits.TryGetValue(p.Chunk,out var e)&&e.ContainsKey(p.Index)));
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
        {
            var key=p.Chunk;id=0;
            if(!chunks.TryGetValue(key,out var resident)||resident.Cells==null)return false;
            int index=p.Index;
            // Preserve edit authority and the closed nonresident boundary while
            // avoiding Ready/Get's repeated chunk and coordinate lookups.
            if(edits.TryGetValue(key,out var page)&&page.TryGetValue(index,out id))return true;
            id=resident.Cells[ChunkMesher.Index(index&31,(index>>5)&31,index>>10)];return true;
        }
        public byte SkyLight(BlockPos air)
        {
            var key=(air.X,air.Z);
            if(lightColumns.TryGetValue((air.Chunk.X,air.Chunk.Z),out var cached))
                return air.Y>cached.heights[(int)(air.X-air.Chunk.Min.X)+32*(int)(air.Z-air.Chunk.Min.Z)]?(byte)15:(byte)0;
            if(!skyHeights.TryGetValue(key,out int top))
            {
                // A retained column can contain several generator versions. Read their
                // height bounds without recording or upgrading unexplored terrain.
                top=TerrainGenerator.MinY;TerrainGenerator previous=null;
                for(int y=TerrainGenerator.MinY/32;y<=TerrainGenerator.MaxY/32;y++)
                {
                    var source=GeneratorFor(new ChunkPos(air.Chunk.X,y,air.Chunk.Z));
                    if(source!=previous)top=Math.Max(top,source.OpaqueHeight(air.X,air.Z));previous=source;
                }
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
                long began=Stopwatch.GetTimestamp();Grass.Step(this,grassChunks);LastGrassTickMs=(Stopwatch.GetTimestamp()-began)*1000.0/Stopwatch.Frequency;
                grassAccumulator-=GrassSimulation.StepSeconds;
            }
        }
        bool Change(BlockPos p,byte expected,byte replacement,bool immediate=true,bool requireReady=true)
        {
            if(p.Y<=TerrainGenerator.MinY||p.Y>TerrainGenerator.MaxY||Math.Abs(p.X)>TerrainGenerator.HorizontalLimit||Math.Abs(p.Z)>TerrainGenerator.HorizontalLimit||expected==BlockId.Bedrock)return false;
            if((requireReady&&!Ready(p))||Get(p)!=expected)return false;
            bool harvestLeaf=expected==BlockId.Leaves&&replacement!=expected&&NaturalLeaf(p);
            grownTreeCells.Remove(p);
            if(!edits.TryGetValue(p.Chunk,out var e)){e=new Dictionary<int,byte>();edits[p.Chunk]=e;}
            e[p.Index]=replacement;
            var columnKey=(p.X,p.Z);
            if(!editedColumns.TryGetValue(columnKey,out var column)){column=new SortedSet<int>();editedColumns[columnKey]=column;}
            if(BlockId.Opaque(replacement))column.Add(p.Y);else column.Remove(p.Y);
            if(BlockId.Opaque(expected)!=BlockId.Opaque(replacement))skyHeights.Remove(columnKey);
            LightingChanged(p,expected,replacement);
            // Update every resident halo touching the edit. Collision sees the change now.
            for(int cz=-1;cz<=1;cz++)for(int cy=-1;cy<=1;cy++)for(int cx=-1;cx<=1;cx++)
            {
                var key=p.Chunk.Offset(cx,cy,cz);if(!chunks.TryGetValue(key,out var resident))continue;
                var kv=new KeyValuePair<ChunkPos,Resident>(key,resident);
                var min=kv.Key.Min;long x=p.X-min.X,z=p.Z-min.Z;int y=p.Y-min.Y;
                if(x < -1 || x>32 || y < -1 || y>32 || z < -1 || z>32)continue;
                var c=kv.Value;c.Revision++;c.Dirty=true;QueueMesh(kv.Key,c);
                if(!requireReady)treeMeshes.Add(kv.Key);
                if(c.Cells!=null)c.Cells[ChunkMesher.Index((int)x,y,(int)z)]=replacement;
            }
            // Masking the old block requires fresh meshes; their local surface work is prioritized.
            // Hide stale chunks immediately, publishing a synchronous local rebuild for this edit only.
            // 32^3 bounded meshing is measured independently from asynchronous streaming.
            // Automatically decaying leaves cannot be part of another leaf's valid
            // support path. The original support removal already scheduled affected leaves.
            if((expected==BlockId.Log||expected==BlockId.Leaves&&requireReady)&&replacement!=expected)Trees.SupportRemoved(this,p);
            DoorSupportChanged(p,replacement);
            BedSupportChanged(p,replacement);
            if(harvestLeaf)HarvestLeaf(p);
            if(expected==BlockId.Sapling&&Fluids.IsFluid(replacement))BlockMined?.Invoke(p,BlockId.Sapling);
            TorchChanged(p,expected,replacement);
            FluidSimulation.Changed(this,p);
            if(!immediate){BlockChanged?.Invoke(p);return true;}
            long editBegan=Stopwatch.GetTimestamp();
            for(int z=-1;z<=1;z++)for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
            {
                var key=p.Chunk.Offset(x,y,z);if(!chunks.TryGetValue(key,out var c)||!c.Dirty||c.Cells==null)continue;
                var kv=new KeyValuePair<ChunkPos,Resident>(key,c);
                var min=kv.Key.Min;
                if(Math.Abs(p.X-min.X)>33||Math.Abs(p.Z-min.Z)>33||Math.Abs(p.Y-min.Y)>33)continue;
                ImmediateMeshBuilds++;var mesh=ChunkMesher.Build(kv.Key,c.Revision,c.Cells);Apply(mesh,c);
            }
            LastEditMeshMs=(Stopwatch.GetTimestamp()-editBegan)*1000.0/Stopwatch.Frequency;BlockChanged?.Invoke(p);return true;
        }
        public double LastEditMeshMs { get; private set; }
        public long ImmediateMeshBuilds {get;private set;}

        void Update()
        {
            using var cost=RuntimeCosts.Streaming.Auto();
            DrainRetiredViews(ViewTeardownBudget);
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
            if(!centre.Equals(lastCentre)||lastViewDistance!=ViewDistance||demandChanged&&Time.unscaledTime>=nextDemand)
            {nextDemand=Time.unscaledTime+0.4f;lastCentre=centre;lastViewDistance=ViewDistance;demandChanged=false;Demand(centre);}
            long began=Stopwatch.GetTimestamp();
            for(int i=work.Count-1;i>=0;i--)
            {
                var job=work[i];var t=job.task;if(!t.IsCompleted)continue;work.RemoveAt(i);
                if(t.IsFaulted||t.IsCanceled)
                {FailMesh(job.position,job.token,job.revision,t.Exception?.GetBaseException().Message??"Chunk worker cancelled");continue;}
                var result=t.Result;
                if(!chunks.TryGetValue(result.Position,out var c)||c.Token!=result.Token){RejectedJobs++;continue;}
                c.Busy=false;
                if(c.Revision!=result.Revision){RejectStaleMesh(result.Position,c);continue;}
                if(result.HasSurfaceRange&&!surfaceRanges.ContainsKey((result.Position.X,result.Position.Z)))
                {surfaceRanges[(result.Position.X,result.Position.Z)]=(result.SurfaceMin,result.SurfaceMax);demandChanged=true;}
                Apply(result,c);LastBuildMs=result.Milliseconds;
                if((Stopwatch.GetTimestamp()-began)*1000.0/Stopwatch.Frequency>4)break;
            }
            AdvanceLighting();
            if(work.Count<2)
            {
                var priority=new ChunkWorkPriority(centre,(ViewObserver!=null?ViewObserver:Observer).forward);
                // The two available workers each consume one actual dispatch slot.
                // Busy pages are skipped, so the first choice cannot be sent twice.
                while(work.Count<2)
                {
                    Resident next=null;ChunkWorkPriority.Candidate best=default;
                    bool oldest=ChunkWorkPriority.IsOldestSlot(meshDispatches);
                    foreach(var entry in pendingMeshes)
                    {
                        if(entry.Value.Busy)continue;
                        var rank=priority.Rank(entry.Key);
                        if(next!=null&&ChunkWorkPriority.Compare(rank,entry.Value.PendingSequence,best,next.PendingSequence,oldest)>=0)continue;
                        next=entry.Value;best=rank;
                    }
                    if(next==null)break;
                    Launch(best.Position,next);meshDispatches++;
                }
            }
        }
        // Regional light solves retain their distance ordering independently of mesh dispatch.
        static double Distance(ChunkPos a,ChunkPos b) {double x=a.X-b.X,z=a.Z-b.Z,y=(long)a.Y-b.Y;return x*x+z*z+y*y*.5;}
        void QueueMesh(ChunkPos position,Resident resident,bool retry=false)
        {
            // A stale completed job already consumed its turn. Requeue at the
            // tail so continuous edits cannot monopolize the oldest-work slot.
            if(retry||!pendingMeshes.ContainsKey(position))resident.PendingSequence=++nextPendingSequence;
            pendingMeshes[position]=resident;
        }
        void RejectStaleMesh(ChunkPos position,Resident resident)
        {
            RejectedJobs++;
            // A newer synchronous edit can already have published its mesh.
            // Only still-dirty data needs another worker after stale completion.
            if(resident.Dirty)QueueMesh(position,resident,true);
        }
        void FailMesh(ChunkPos position,int token,int revision,string message)
        {
            Error=$"Chunk {position.X},{position.Y},{position.Z}: {message}";
            if(!chunks.TryGetValue(position,out var resident)||resident.Token!=token){RejectedJobs++;return;}
            resident.Busy=false;
            // A synchronous edit may already have published a newer mesh while
            // this worker was running. Its late failure must not dirty that mesh.
            if(!resident.Dirty)return;
            if(resident.Revision!=revision){QueueMesh(position,resident,true);return;}
            if(resident.FailedRevision!=revision){resident.FailedRevision=revision;resident.Failures=0;}
            if(++resident.Failures<=1)QueueMesh(position,resident,true);
            else pendingMeshes.Remove(position); // Deterministic failures cannot retry forever.
        }
        void Demand(ChunkPos centre)
        {
            DemandPasses++;
            wanted.Clear();wantedColumns.Clear();
            for(int z=-ViewDistance;z<=ViewDistance;z++)for(int x=-ViewDistance;x<=ViewDistance;x++)
            {
                if(x*x+z*z>ViewDistance*ViewDistance+1)continue;
                long cx=centre.X+x,cz=centre.Z+z;
                if(cx < -TerrainGenerator.HorizontalLimit/32||cx>=TerrainGenerator.HorizontalLimit/32||cz < -TerrainGenerator.HorizontalLimit/32||cz>=TerrainGenerator.HorizontalLimit/32)continue;
                wantedColumns.Add((cx,cz));
                int lowest,highest;
                // Workers publish exact extrema with their first page. Mountain coverage expands
                // without a second synchronous column scan or an unbounded world-height cache.
                if(surfaceRanges.TryGetValue((cx,cz),out var range))
                {lowest=(int)BlockPos.FloorDiv(range.min,32)-1;highest=(int)BlockPos.FloorDiv(range.max,32);}
                else {int surface=Generator.Height(cx*32+16,cz*32+16)/32;lowest=surface-1;highest=surface+1;}
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
                    if(!chunks.ContainsKey(p)){var resident=new Resident{Token=++nextToken};chunks.Add(p,resident);QueueMesh(p,resident);}
                }
            }
            ticketedChunks.Clear();
            if(PersistentChunkTickets!=null)foreach(var p in PersistentChunkTickets())
            {ticketedChunks.Add(p);wanted.Add(p);wantedColumns.Add((p.X,p.Z));if(!chunks.ContainsKey(p)){var resident=new Resident{Token=++nextToken};chunks.Add(p,resident);QueueMesh(p,resident);}}
            releaseChunks.Clear();foreach(var key in chunks.Keys)if(!wanted.Contains(key))releaseChunks.Add(key);
            foreach(var key in releaseChunks)
            {
                var departed=chunks[key];if(departed.View!=null)departed.View.SetActive(false);
                if(departed.View!=null||departed.Mesh!=null||departed.FluidMesh!=null)retiredViews.Enqueue((departed.View,departed.Mesh,departed.FluidMesh));
                // Keep only native handles until teardown; resident cell buffers can be collected now.
                chunks.Remove(key);pendingMeshes.Remove(key);
            }
            LightingResidency();
            if(releaseChunks.Count>0)
            {
                // Every callback observes the completed batch, never a partially
                // unloaded network whose other pages still appear available.
                foreach(var key in releaseChunks)ChunkResidencyChanged?.Invoke(key);
                ResidencyChanged?.Invoke();
            }
            releaseColumns.Clear();foreach(var key in surfaceRanges.Keys)if(!wantedColumns.Contains(key))releaseColumns.Add(key);
            foreach(var key in releaseColumns)surfaceRanges.Remove(key);
            grassChunks.Clear();
            // Only a bounded neighbourhood ticks; unloaded/distant terrain receives no catch-up.
            grassChunks.AddRange(wanted.Where(p=>ticketedChunks.Contains(p)||Math.Abs(p.X-centre.X)<=2&&Math.Abs(p.Z-centre.Z)<=2&&Math.Abs(p.Y-centre.Y)<=1)
                .OrderBy(p=>p.X).ThenBy(p=>p.Y).ThenBy(p=>p.Z));
            releaseSky.Clear();foreach(var key in skyHeights.Keys)if(Math.Abs(BlockPos.FloorDiv(key.x,32)-centre.X)>2||Math.Abs(BlockPos.FloorDiv(key.z,32)-centre.Z)>2)releaseSky.Add(key);
            foreach(var key in releaseSky)skyHeights.Remove(key);
        }
        void Launch(ChunkPos p,Resident c)
        {
            c.Busy=true;int revision=c.Revision,token=c.Token;var generator=GeneratorFor(p);var haloGenerators=c.Cells==null?PinGeneration(p):null;
            // Immutable edit snapshot prevents worker/main-thread dictionary races.
            var snapshot=c.Cells==null?null:(byte[])c.Cells.Clone();
            if(snapshot==null)GenerationJobs++;else RemeshJobs++;
            var changes=new List<KeyValuePair<BlockPos,byte>>();
            if(snapshot==null)for(int z=-1;z<=1;z++)for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
            {
                var key=p.Offset(x,y,z);
                if(!edits.TryGetValue(key,out var e))continue;
                foreach(var v in e)changes.Add(new KeyValuePair<BlockPos,byte>(key.Min.Offset(v.Key%32,v.Key/32%32,v.Key/1024),v.Value));
            }
            var task=Task.Run(()=>
            {
                using var workerCost=RuntimeCosts.TerrainWorker.Auto();
                var sw=Stopwatch.StartNew();int surfaceMin=0,surfaceMax=0;
                // Loaded pages already contain current edits and halos. Remesh an immutable
                // copy; regenerate only genuinely new residency pages.
                byte[] cells=snapshot??generator.Generate(p,out surfaceMin,out surfaceMax);var min=p.Min;
                if(snapshot==null&&System.Array.Exists(haloGenerators,g=>g.GenerationVersion!=generator.GenerationVersion))
                    for(int z=-1;z<=32;z++)for(int y=-1;y<=32;y++)for(int x=-1;x<=32;x++)
                    {
                        if(x>=0&&x<32&&y>=0&&y<32&&z>=0&&z<32)continue;
                        int gx=x<0?0:x>31?2:1,gy=y<0?0:y>31?2:1,gz=z<0?0:z>31?2:1;
                        var source=haloGenerators[gx+3*(gy+3*gz)];
                        if(source.GenerationVersion!=generator.GenerationVersion)cells[ChunkMesher.Index(x,y,z)]=source.At(min.Offset(x,y,z));
                    }
                foreach(var e in changes)
                {
                    long x=e.Key.X-min.X,z=e.Key.Z-min.Z;int y=e.Key.Y-min.Y;
                    if(x>=-1&&x<=32&&y>=-1&&y<=32&&z>=-1&&z<=32)cells[ChunkMesher.Index((int)x,y,(int)z)]=e.Value;
                }
                var result=ChunkMesher.Build(p,revision,cells);result.Token=token;result.HasSurfaceRange=snapshot==null;result.SurfaceMin=surfaceMin;result.SurfaceMax=surfaceMax;result.Milliseconds=sw.Elapsed.TotalMilliseconds;return result;
            });
            work.Add((task,p,token,revision));
        }
        void Apply(ChunkBuild result,Resident c)
        {
            using var cost=RuntimeCosts.TerrainUpload.Auto();
            bool first=c.Cells==null;
            bool visibleTerrain=result.Triangles.Length>0,visibleFluid=result.FluidMesh.Indices.Length>0;
            c.Cells=result.Cells;c.Dirty=false;pendingMeshes.Remove(result.Position);
            // Empty underground/sky pages still provide collision and residency data,
            // but need neither an empty renderer nor an empty native Mesh.
            using(RuntimeCosts.TerrainViews.Auto())
            {
                if(c.View==null&&(visibleTerrain||visibleFluid))
                {
                    c.View=new GameObject("Chunk");c.View.transform.SetParent(transform,false);
                    c.View.AddComponent<MeshFilter>();var r=c.View.AddComponent<MeshRenderer>();r.sharedMaterial=TerrainMaterial;
                    r.shadowCastingMode=ShadowCastingMode.On;
                }
                if(c.View!=null)
                {
                    c.View.SetActive(visibleTerrain||visibleFluid);c.View.transform.position=Local(result.Position.Min);
                    c.View.GetComponent<MeshRenderer>().enabled=visibleTerrain;
                }
            }
            using(RuntimeCosts.TerrainMeshUpload.Auto())
            {
                if(visibleTerrain)
                {
                    c.Mesh=result.ToMesh(c.Mesh);
                    c.View.GetComponent<MeshFilter>().sharedMesh=c.Mesh;
                }
                else if(c.Mesh!=null){c.View.GetComponent<MeshFilter>().sharedMesh=null;Destroy(c.Mesh);c.Mesh=null;}
            }
            using(RuntimeCosts.FluidMeshUpload.Auto())
            {
                if(c.FluidView==null&&visibleFluid)
                {
                    c.FluidView=new GameObject("Fluid surface");c.FluidView.transform.SetParent(c.View.transform,false);
                    c.FluidView.AddComponent<MeshFilter>();var renderer=c.FluidView.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial=fluidMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;
                }
                if(visibleFluid)c.FluidMesh=result.FluidMesh.ToMesh(c.FluidMesh);
                else if(c.FluidMesh!=null){Destroy(c.FluidMesh);c.FluidMesh=null;}
                if(c.FluidView!=null)
                {c.FluidView.SetActive(visibleFluid);c.FluidView.GetComponent<MeshFilter>().sharedMesh=c.FluidMesh;}
            }
            if(first)
            {
                using var activationCost=RuntimeCosts.ChunkActivation.Auto();
                DirtyLight(result.Position);
                FluidSimulation.Ready(result.Position);ChunkResidencyChanged?.Invoke(result.Position);ResidencyChanged?.Invoke();ChunkReady?.Invoke(result.Position,c.Cells);
                // The worker identifies exposed/unsettled cells. Stable source interiors and
                // shared source boundaries never enter the scheduled queue on mere residency.
                var min=result.Position.Min;
                foreach(int i in result.FluidMesh.ActiveCells)
                    FluidSimulation.Changed(this,min.Offset(i%32,i/32%32,i/1024));
            }
        }
        void DrainRetiredViews(int budget)
        {
            while(budget-->0&&retiredViews.Count>0)
            {
                var retired=retiredViews.Dequeue();
                if(retired.view!=null)Destroy(retired.view);if(retired.terrain!=null)Destroy(retired.terrain);if(retired.fluid!=null)Destroy(retired.fluid);
            }
        }
        static void Release(Resident c)
        {if(c.View!=null)Destroy(c.View);if(c.Mesh!=null)Destroy(c.Mesh);if(c.FluidMesh!=null)Destroy(c.FluidMesh);}
        public bool Raycast(Vector3 start,Vector3 direction,float reach,out BlockPos hit,out byte id)
            => Raycast(start,direction,reach,out hit,out id,out _);
        public bool Raycast(Vector3 start,Vector3 direction,float reach,out BlockPos hit,out byte id,out Vector3Int face,bool fluidSources=false)
        {bool found=Trace(start,direction,reach,out var result,fluidSources,false);hit=result.Position;id=result.Block;face=result.Face;return found;}
        public bool Select(Vector3 start,Vector3 direction,float reach,out BlockSelectionHit hit,bool fluidSources=false)
            =>Trace(start,direction,reach,out hit,fluidSources,false);
        // Camera clearance shares movement's solid predicate, not interaction targeting.
        public bool RaycastSolid(Vector3 start,Vector3 direction,float reach,out BlockPos hit,out byte id)
        {bool found=Trace(start,direction,reach,out var result,false,true);hit=result.Position;id=result.Block;return found;}
        bool Trace(Vector3 start,Vector3 direction,float reach,out BlockSelectionHit hit,bool fluidSources,bool solidsOnly)
        {
            hit=default;if(reach<0||direction.sqrMagnitude<1e-12f)return false;
            direction.Normalize();var face=Vector3Int.zero;var cell=Address(start);
            Vector3 localCell=Local(cell),step=new Vector3(Math.Sign(direction.x),Math.Sign(direction.y),Math.Sign(direction.z));
            Vector3 delta=new Vector3(direction.x==0?float.PositiveInfinity:Mathf.Abs(1/direction.x),direction.y==0?float.PositiveInfinity:Mathf.Abs(1/direction.y),direction.z==0?float.PositiveInfinity:Mathf.Abs(1/direction.z));
            Vector3 t=new Vector3((direction.x>0?localCell.x+1-start.x:start.x-localCell.x)*delta.x,(direction.y>0?localCell.y+1-start.y:start.y-localCell.y)*delta.y,(direction.z>0?localCell.z+1-start.z:start.z-localCell.z)*delta.z);
            for(int axis=0;axis<3;axis++)if(direction[axis]==0)t[axis]=float.PositiveInfinity;
            float distance=0;
            for(int i=0;i<64&&distance<=reach;i++)
            {
                if(!Ready(cell)){if(solidsOnly){hit=new BlockSelectionHit(cell,0,start+direction*distance,face,distance);return true;}return false;}
                byte b=Get(cell);var fluid=Fluids.Registry.Get(b);
                if(solidsOnly?Solid(cell):b!=0&&(fluid==null||fluidSources&&fluid.IsSource(b)))
                {
                    var definition=BlockDefinitions.Get(b);float selectedDistance=distance;var selectedFace=face;
                    if(solidsOnly||(definition.Traits&BlockTraits.HasCustomSelectionShape)==0||
                        definition.Shape(this,cell).Intersect(start-Local(cell),direction,reach,out selectedDistance,out selectedFace))
                    {hit=new BlockSelectionHit(cell,b,start+direction*selectedDistance,selectedFace,selectedDistance);return true;}
                }
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
        public void Stop() {stopped=true;StopLighting();if(TorchView!=null)TorchView.Clear();foreach(var c in chunks.Values)Release(c);chunks.Clear();pendingMeshes.Clear();surfaceRanges.Clear();DrainRetiredViews(int.MaxValue);}
        void OnDestroy() {Stop();}
    }
}
