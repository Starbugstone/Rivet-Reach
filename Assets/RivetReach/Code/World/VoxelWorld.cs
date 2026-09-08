using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    public sealed class VoxelWorld : MonoBehaviour
    {
        sealed class Resident
        {
            public int Revision, Token;
            public byte[] Cells;
            public GameObject View;
            public Mesh Mesh;
            public bool Busy,Dirty=true;
        }
        readonly Dictionary<ChunkPos,Resident> chunks=new Dictionary<ChunkPos,Resident>();
        readonly Dictionary<ChunkPos,Dictionary<int,byte>> edits=new Dictionary<ChunkPos,Dictionary<int,byte>>();
        readonly List<Task<ChunkBuild>> work=new List<Task<ChunkBuild>>();
        int nextToken;
        public TerrainGenerator Generator { get; private set; }
        public BlockPos Origin { get; private set; }
        public Transform Observer;
        public int ViewDistance=4;
        public Material TerrainMaterial;
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
        float nextDemand;
        ChunkPos lastCentre;
        bool stopped;

        public void Initialize(int seed)
        {
            Generator=new TerrainGenerator(seed);Origin=new BlockPos(0,0,0);
            TerrainMaterial=Resources.Load<Material>("Materials/Terrain");
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
        public bool Solid(BlockPos p) => !Ready(p)||Get(p)!=0;
        public bool Remove(BlockPos p,byte expected)
        {
            if(!Ready(p)||expected==0||Get(p)!=expected)return false;
            if(!edits.TryGetValue(p.Chunk,out var e)){e=new Dictionary<int,byte>();edits[p.Chunk]=e;}
            e[p.Index]=0;
            // Update every resident halo touching the edit. Collision sees the change now.
            foreach(var kv in chunks)
            {
                var min=kv.Key.Min;long x=p.X-min.X,z=p.Z-min.Z;int y=p.Y-min.Y;
                if(x < -1 || x>32 || y < -1 || y>32 || z < -1 || z>32)continue;
                var c=kv.Value;c.Revision++;c.Dirty=true;
                if(c.Cells!=null)c.Cells[ChunkMesher.Index((int)x,y,(int)z)]=0;
            }
            // Masking the old block requires fresh meshes; their local surface work is prioritized.
            // Hide stale chunks immediately, publishing a synchronous local rebuild for this edit only.
            // 32^3 bounded meshing is measured independently from asynchronous streaming.
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
            Shader.SetGlobalColor("_RRFogColour",new Color(.56f,.68f,.77f));
            Shader.SetGlobalVector("_RRFogRange",new Vector4(ViewDistance*12,ViewDistance*24,0,0));
            if(Mathf.Abs(Observer.position.x)>512||Mathf.Abs(Observer.position.z)>512)
            {
                var shift=new Vector3(Mathf.Floor(Observer.position.x/32)*32,0,Mathf.Floor(Observer.position.z/32)*32);
                Origin=Origin.Offset((int)shift.x,0,(int)shift.z);
                foreach(var kv in chunks)if(kv.Value.View!=null)kv.Value.View.transform.position=Local(kv.Key.Min);
                OriginShifted?.Invoke(shift);
            }
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
            for(int z=-ViewDistance;z<=ViewDistance;z++)for(int x=-ViewDistance;x<=ViewDistance;x++)
            {
                if(x*x+z*z>ViewDistance*ViewDistance+1)continue;
                long cx=centre.X+x,cz=centre.Z+z;
                if(cx < -TerrainGenerator.HorizontalLimit/32||cx>=TerrainGenerator.HorizontalLimit/32||cz < -TerrainGenerator.HorizontalLimit/32||cz>=TerrainGenerator.HorizontalLimit/32)continue;
                int surface=Generator.Height(cx*32+16,cz*32+16)/32;
                int low=Math.Max(-8,Math.Min(surface-1,centre.Y-1)),high=Math.Min(23,Math.Max(surface,centre.Y+1));
                // Deep travel does not fill the whole column between the player and surface.
                for(int y=low;y<=high;y++)
                {
                    if(Math.Abs(y-centre.Y)>1&&Math.Abs(y-surface)>1)continue;
                    var p=new ChunkPos(cx,y,cz);wanted.Add(p);
                    if(!chunks.ContainsKey(p))chunks.Add(p,new Resident{Token=++nextToken});
                }
            }
            foreach(var key in chunks.Keys.Where(k=>!wanted.Contains(k)).ToArray())
            {Release(chunks[key]);chunks.Remove(key);}
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
                var sw=Stopwatch.StartNew();byte[] cells=generator.Generate(p);var min=p.Min;
                foreach(var e in changes)
                {
                    long x=e.Key.X-min.X,z=e.Key.Z-min.Z;int y=e.Key.Y-min.Y;
                    if(x>=-1&&x<=32&&y>=-1&&y<=32&&z>=-1&&z<=32)cells[ChunkMesher.Index((int)x,y,(int)z)]=e.Value;
                }
                var result=ChunkMesher.Build(p,revision,cells);result.Token=token;result.Milliseconds=sw.Elapsed.TotalMilliseconds;return result;
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
            c.Mesh=mesh;c.Cells=result.Cells;c.Dirty=false;
        }
        static void Release(Resident c)
        {if(c.View!=null)Destroy(c.View);if(c.Mesh!=null)Destroy(c.Mesh);}
        public bool Raycast(Vector3 start,Vector3 direction,float reach,out BlockPos hit,out byte id)
        {
            hit=default;id=0;var cell=Address(start);
            Vector3 localCell=Local(cell),step=new Vector3(Math.Sign(direction.x),Math.Sign(direction.y),Math.Sign(direction.z));
            Vector3 delta=new Vector3(direction.x==0?float.PositiveInfinity:Mathf.Abs(1/direction.x),direction.y==0?float.PositiveInfinity:Mathf.Abs(1/direction.y),direction.z==0?float.PositiveInfinity:Mathf.Abs(1/direction.z));
            Vector3 t=new Vector3((direction.x>0?localCell.x+1-start.x:start.x-localCell.x)*delta.x,(direction.y>0?localCell.y+1-start.y:start.y-localCell.y)*delta.y,(direction.z>0?localCell.z+1-start.z:start.z-localCell.z)*delta.z);
            for(int axis=0;axis<3;axis++)if(direction[axis]==0)t[axis]=float.PositiveInfinity;
            float distance=0;
            for(int i=0;i<64&&distance<=reach;i++)
            {
                if(!Ready(cell))return false;
                byte b=Get(cell);if(b!=0){hit=cell;id=b;return true;}
                int axis=t.x<t.y?(t.x<t.z?0:2):(t.y<t.z?1:2);
                distance=t[axis];t[axis]+=delta[axis];
                cell=cell.Offset(axis==0?(int)step.x:0,axis==1?(int)step.y:0,axis==2?(int)step.z:0);
            }
            return false;
        }
        public bool Overlaps(Vector3 feet,float width,float height)
        {
            var min=Address(feet+new Vector3(-width/2+0.001f,0.001f,-width/2+0.001f));
            var max=Address(feet+new Vector3(width/2-0.001f,height-0.001f,width/2-0.001f));
            for(long z=min.Z;z<=max.Z;z++)for(int y=min.Y;y<=max.Y;y++)for(long x=min.X;x<=max.X;x++)if(Solid(new BlockPos(x,y,z)))return true;
            return false;
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
        public void Stop() {stopped=true;foreach(var c in chunks.Values)Release(c);chunks.Clear();}
        void OnDestroy() {Stop();}
    }
}
