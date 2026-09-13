using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    // Chunk-batched models and a bounded pool of optional nearby lighting detail.
    public sealed class TorchPresentation : MonoBehaviour
    {
        public const int LightLimit=8;
        public const float LightRange=14,LightIntensity=27,LightFadeStart=48,LightViewRange=64;
        public float ViewRange=>world==null?64:world.FogEnd;
        sealed class Batch
        {public GameObject Root;public Mesh Mesh;public KeyValuePair<BlockPos,BlockPos>[] Entries;}
        readonly Dictionary<ChunkPos,Batch> views=new Dictionary<ChunkPos,Batch>();
        readonly BlockPos?[] assigned=new BlockPos?[LightLimit];
        readonly float[] blend=new float[LightLimit];
        readonly Dictionary<BlockPos,BlockPos> wantedLights=new Dictionary<BlockPos,BlockPos>();
        Mesh cube;
        readonly List<Light> lights=new List<Light>();
        VoxelWorld world;
        Material wood,band,flame,core;
        float refreshAt;
        public int ViewCount=>views.Values.Sum(b=>b.Entries.Length);
        public int ModelBatchCount=>views.Count;
        public double LastRefreshMs {get;private set;}
        public double MaxRefreshMs {get;private set;}
        public bool HasView(BlockPos cell)=>views.TryGetValue(cell.Chunk,out var batch)&&batch.Entries.Any(e=>e.Key.Equals(cell));
        public int ActiveLightCount=>lights.Count(l=>l.enabled);
        public IEnumerable<Light> Lights=>lights;
        public void Initialize(VoxelWorld owner)
        {
            world=owner;
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=primitive.GetComponent<MeshFilter>().sharedMesh;
            primitive.SetActive(false);Destroy(primitive);
            wood=Surface("Torch wood",new Color(.30f,.13f,.045f));
            band=Surface("Torch binding",new Color(.11f,.075f,.045f));
            flame=Surface("Torch flame",new Color(1,.30f,.025f),true);
            core=Surface("Torch flame core",new Color(1,.82f,.24f),true);
            for(int i=0;i<LightLimit;i++)
            {
                var source=Instantiate(Resources.Load<GameObject>("TorchLight"),transform,false);
                var light=source.GetComponent<Light>();
                light.enabled=false;lights.Add(light);
            }
            world.OriginShifted+=Shift;
        }
        Material Surface(string name,Color colour,bool emissive=false)
        {
            var material=new Material(Shader.Find("RivetReach/WorldLit")){name=name};
            material.SetColor("_BaseColor",colour);material.SetFloat("_Smoothness",.15f);
            if(emissive){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",colour*2);}
            return material;
        }
        static Vector3 Base(BlockPos cell,BlockPos support)
        {
            if(cell.Y>support.Y)return new Vector3(.5f,.04f,.5f);
            return new Vector3(.5f+(support.X-cell.X)*.43f,.18f,.5f+(support.Z-cell.Z)*.43f);
        }
        static Vector3 Axis(BlockPos cell,BlockPos support)=>cell.Y>support.Y?Vector3.up:
            new Vector3((cell.X-support.X)*.36f,1,(cell.Z-support.Z)*.36f).normalized;
        public Vector3 FlamePosition(BlockPos cell,BlockPos support)=>world.Local(cell)+Base(cell,support)+Axis(cell,support)*.70f;
        Batch CreateBatch(ChunkPos chunk,KeyValuePair<BlockPos,BlockPos>[] entries)
        {
            var combines=new List<CombineInstance>[] {new List<CombineInstance>(),new List<CombineInstance>(),new List<CombineInstance>(),new List<CombineInstance>()};
            foreach(var entry in entries)
            {
                var basis=Matrix4x4.TRS(world.Local(entry.Key)-world.Local(chunk.Min)+Base(entry.Key,entry.Value),Quaternion.FromToRotation(Vector3.up,Axis(entry.Key,entry.Value)),Vector3.one);
                void Part(int material,Vector3 at,Vector3 scale)=>combines[material].Add(new CombineInstance{mesh=cube,transform=basis*Matrix4x4.TRS(at,Quaternion.identity,scale)});
                Part(0,new Vector3(0,.28f,0),new Vector3(.10f,.56f,.10f));
                Part(1,new Vector3(0,.52f,0),new Vector3(.14f,.16f,.14f));
                Part(2,new Vector3(0,.67f,0),new Vector3(.13f,.21f,.13f));
                Part(2,new Vector3(.015f,.80f,0),new Vector3(.065f,.12f,.065f));
                Part(3,new Vector3(0,.62f,-.01f),new Vector3(.145f,.10f,.145f));
            }
            var parts=new CombineInstance[4];
            for(int i=0;i<4;i++){var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combines[i].ToArray(),true,true);parts[i]=new CombineInstance{mesh=mesh,transform=Matrix4x4.identity};}
            var combined=new Mesh{name="Chunk torch models",indexFormat=IndexFormat.UInt32};combined.CombineMeshes(parts,false,false);
            foreach(var part in parts)Destroy(part.mesh);
            var root=new GameObject("Chunk torches");root.transform.SetParent(transform,false);root.transform.position=world.Local(chunk.Min);
            root.AddComponent<MeshFilter>().sharedMesh=combined;
            var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{wood,band,flame,core};renderer.shadowCastingMode=ShadowCastingMode.Off;
            return new Batch{Root=root,Mesh=combined,Entries=entries};
        }
        void RemoveBatch(ChunkPos chunk)
        {var batch=views[chunk];batch.Root.SetActive(false);Destroy(batch.Root);Destroy(batch.Mesh);views.Remove(chunk);}
        void LateUpdate()
        {if(world==null)return;if(Time.unscaledTime>=refreshAt)Refresh();UpdateLightStrength();}
        void Shift(Vector3 shift)
        {foreach(var pair in views)pair.Value.Root.transform.position=world.Local(pair.Key.Min);Refresh();}
        public void Refresh()
        {
            if(world==null||world.Observer==null)return;
            var timer=System.Diagnostics.Stopwatch.StartNew();
            refreshAt=Time.unscaledTime+.2f;
            // Include a whole edge chunk; terrain fog hides its eventual residency boundary.
            var near=world.NearbyTorches(world.Address(world.Observer.position),ViewRange+56)
                .OrderBy(t=>(FlamePosition(t.Key,t.Value)-world.Observer.position).sqrMagnitude).ToArray();
            var groups=near.GroupBy(t=>t.Key.Chunk).ToDictionary(g=>g.Key,g=>g.OrderBy(t=>t.Key.Index).ToArray());
            foreach(var chunk in views.Keys.Where(p=>!groups.ContainsKey(p)).ToArray())RemoveBatch(chunk);
            foreach(var group in groups)
            {
                if(views.TryGetValue(group.Key,out var batch))
                {if(batch.Entries.SequenceEqual(group.Value))continue;RemoveBatch(group.Key);}
                views.Add(group.Key,CreateBatch(group.Key,group.Value));
            }
            wantedLights.Clear();
            foreach(var entry in near.Where(t=>(FlamePosition(t.Key,t.Value)-world.Observer.position).sqrMagnitude<LightViewRange*LightViewRange).Take(LightLimit))wantedLights.Add(entry.Key,entry.Value);
            // Stable slots fade out before reassignment: camera movement never teleports a full light.
            for(int i=0;i<LightLimit;i++)
            {
                if(assigned[i].HasValue&&(!world.Ready(assigned[i].Value)||!world.TorchSupport(assigned[i].Value,out _)))
                {assigned[i]=null;blend[i]=0;lights[i].enabled=false;}
                if(!assigned[i].HasValue)
                    foreach(var entry in wantedLights)if(!assigned.Any(p=>p.HasValue&&p.Value.Equals(entry.Key)))
                    {assigned[i]=entry.Key;lights[i].transform.position=FlamePosition(entry.Key,entry.Value);break;}
                if(assigned[i].HasValue&&world.TorchSupport(assigned[i].Value,out var support))lights[i].transform.position=FlamePosition(assigned[i].Value,support);
            }
            timer.Stop();LastRefreshMs=timer.Elapsed.TotalMilliseconds;MaxRefreshMs=System.Math.Max(MaxRefreshMs,LastRefreshMs);
        }
        public static float DistanceFade(float distance)=>1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(LightFadeStart,LightViewRange,distance));
        void UpdateLightStrength()
        {
            if(world.Observer==null)return;
            for(int i=0;i<LightLimit;i++)
            {
                bool wanted=assigned[i].HasValue&&wantedLights.ContainsKey(assigned[i].Value);
                blend[i]=Mathf.MoveTowards(blend[i],wanted?1:0,Time.unscaledDeltaTime*2);
                lights[i].enabled=assigned[i].HasValue&&blend[i]>0;
                if(lights[i].enabled)lights[i].intensity=LightIntensity*DistanceFade(Vector3.Distance(lights[i].transform.position,world.Observer.position))*Mathf.SmoothStep(0,1,blend[i]);
                if(!wanted&&blend[i]==0)assigned[i]=null;
            }
        }
        public void Clear()
        {
            enabled=false;foreach(var light in lights)if(light!=null)light.enabled=false;
            foreach(var chunk in views.Keys.ToArray())RemoveBatch(chunk);
            wantedLights.Clear();for(int i=0;i<LightLimit;i++){assigned[i]=null;blend[i]=0;}
        }

        void OnDestroy()
        {
            if(world!=null)world.OriginShifted-=Shift;Clear();
            foreach(var material in new[]{wood,band,flame,core})if(material!=null)Destroy(material);
        }
    }
}
