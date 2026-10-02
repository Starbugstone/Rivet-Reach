using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Stopwatch=System.Diagnostics.Stopwatch;

namespace RivetReach
{
    // Shared instanced distant geometry: no per-machine GameObject, light, label,
    // animation or shadow caster. Authoritative state is only read here.
    public sealed class FactoryDistancePresentation : MonoBehaviour
    {
        sealed class Batch
        {
            public Mesh Mesh;public Material Material;public readonly List<Matrix4x4> Matrices=new List<Matrix4x4>();
        }
        struct Request {public int Key;public byte Id;public int Mask,Pose;public Mesh Source;}
        Expedition game;float next;MultiblockPresentation tanks;IndustryPresentation detailed;
        readonly Dictionary<int,Mesh> shapes=new Dictionary<int,Mesh>();
        readonly Dictionary<Mesh,Mesh> derived=new Dictionary<Mesh,Mesh>();
        readonly Queue<Request> pending=new Queue<Request>();readonly HashSet<int> requested=new HashSet<int>();readonly HashSet<Mesh> requestedSources=new HashSet<Mesh>();
        readonly Dictionary<(Mesh,Material),Batch> batches=new Dictionary<(Mesh,Material),Batch>();
        readonly Dictionary<Material,Material> farMaterials=new Dictionary<Material,Material>(),nearMaterials=new Dictionary<Material,Material>();
        static readonly PipeAddition[] additions={PipeAddition.Power,PipeAddition.Signal};
        readonly Matrix4x4[] instances=new Matrix4x4[511];readonly Plane[] planes=new Plane[6];
        readonly HashSet<BlockPos> represented=new HashSet<BlockPos>();
        public bool Represents(BlockPos p)=>represented.Contains(p);
        Material workshop,glass,crateMaterial,bedMaterial;readonly GameObject[] prefabs=new GameObject[256];
        public int InstanceCount {get;private set;}public int DrawCount {get;private set;}public int PendingCount=>pending.Count;
        public long SourceTriangles {get;private set;}public long ReducedTriangles {get;private set;}
        public double PeakBuildMilliseconds {get;private set;}
        public void Initialize(Expedition value){game=value;workshop=Resources.Load<Material>("Industry/Workshop");glass=Resources.Load<Material>("Industry/TankGlass");crateMaterial=Resources.Load<Material>("Industry/Crates");bedMaterial=Resources.Load<Material>("Industry/Bed");game.World.OriginShifted+=Shift;game.World.BlockChanged+=Changed;}
        void Shift(Vector3 delta){next=0;foreach(var b in batches.Values)b.Matrices.Clear();}
        void Changed(BlockPos p){byte id=game.World.Get(p);if(IndustryId.Placed(id)||CrateId.Part(id)||BedId.Part(id)||represented.Contains(p))next=0;}
        Material Variant(Material source,bool far)
        {
            var cache=far?farMaterials:nearMaterials;if(cache.TryGetValue(source,out var found))return found;
            found=new Material(source){name=source.name+(far?" distant":" detailed"),hideFlags=HideFlags.DontSave,enableInstancing=far};
            found.shader=Resources.Load<Shader>("Materials/MachineLit");found.shaderKeywords=source.shaderKeywords;found.renderQueue=source.renderQueue;
            found.DisableKeyword(far?"RR_MACHINE_NEAR":"RR_MACHINE_FAR");
            found.EnableKeyword(far?"RR_MACHINE_FAR":"RR_MACHINE_NEAR");
            if(far){found.DisableKeyword("_NORMALMAP");found.DisableKeyword("_METALLICSPECGLOSSMAP");}
            cache.Add(source,found);return found;
        }
        public Material Near(Material source)=>source.shader.name=="RivetReach/WorldLit"?Variant(source,false):source;
        public void Detail(GameObject root)
        {foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){var m=renderer.sharedMaterial;if(m!=null&&m.shader.name=="RivetReach/WorldLit")renderer.sharedMaterial=Near(m);}}
        Mesh Shape(byte id,int mask=0,int pose=0)
        {
            int key=id|(mask<<8)|(pose<<14);if(shapes.TryGetValue(key,out var mesh))return mesh;
            if(requested.Add(key))pending.Enqueue(new Request{Key=key,Id=id,Mask=mask,Pose=pose});return null;
        }
        Mesh Derived(Mesh source)
        {
            if(derived.TryGetValue(source,out var mesh))return mesh;
            if(requestedSources.Add(source))pending.Enqueue(new Request{Source=source});return null;
        }
        void Build()
        {
            long start=Stopwatch.GetTimestamp();int count=0;
            while(pending.Count>0&&count<2&&(count==0||(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency<1))
            {
                var request=pending.Dequeue();count++;Mesh result;
                if(request.Source!=null){result=DistantMesh.Simplify(request.Source);derived.Add(request.Source,result);SourceTriangles+=Triangles(request.Source);}
                else if(ConnectedPipeVisuals.UsesConnectedMesh(request.Id))
                {var source=ConnectedPipeVisuals.Shape(IndustryDefinition.All[request.Id].Key,request.Mask);result=DistantMesh.Simplify(source);SourceTriangles+=Triangles(source);shapes.Add(request.Key,result);}
                else
                {
                    string key=request.Id==CrateId.Crate?"bulk_crate":request.Id==CrateId.Controller?"crate_controller":request.Id==BedId.Bed?"bed":IndustryDefinition.All[request.Id].Key;
                    var prefab=prefabs[request.Id];if(prefab==null)prefabs[request.Id]=prefab=Resources.Load<GameObject>("Industry/Runtime/"+key);
                    if(prefab==null)throw new InvalidOperationException("Missing distant model: "+key);
                    bool Include(Transform t)
                    {
                        for(var node=t;node!=null&&node!=prefab.transform;node=node.parent)
                            if(node.name.StartsWith("Arm")&&node.name.Length>3&&char.IsDigit(node.name[3])&&(request.Mask&(1<<(node.name[3]-'0')))==0)return false;
                        return true;
                    }
                    Matrix4x4 TransformPart(Transform t)
                    {
                        if(t==prefab.transform||t==null)return Matrix4x4.identity;
                        var position=t.localPosition;var rotation=t.localRotation;var scale=t.localScale;
                        if(t.name=="MotionDoor"&&request.Pose!=0)rotation*=Quaternion.Euler(0,-90,0);
                        if(t.name.StartsWith("MotionHatch")&&request.Pose!=0){position+=Vector3.up*.37f;scale=new Vector3(1,.08f,1);}
                        return TransformPart(t.parent)*Matrix4x4.TRS(position,rotation,scale);
                    }
                    foreach(var f in prefab.GetComponentsInChildren<MeshFilter>(true))if(Include(f.transform)&&f.name!="StatusLight")SourceTriangles+=Triangles(f.sharedMesh);
                    result=DistantMesh.Combine(prefab,Include,TransformPart);shapes.Add(request.Key,result);
                }
                ReducedTriangles+=Triangles(result);next=0;
            }
            PeakBuildMilliseconds=Math.Max(PeakBuildMilliseconds,(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency);
        }
        static long Triangles(Mesh m){long n=0;for(int i=0;i<m.subMeshCount;i++)n+=(long)m.GetIndexCount(i)/3;return n;}
        void Add(Mesh mesh,Material material,Matrix4x4 matrix)
        {
            if(mesh==null)return;material=Variant(material,true);var key=(mesh,material);
            if(!batches.TryGetValue(key,out var batch)){batch=new Batch{Mesh=mesh,Material=material};batches.Add(key,batch);}
            batch.Matrices.Add(matrix);InstanceCount++;
        }
        bool InRange(BlockPos p)
        {
            var world=game.World;if(!world.Ready(p))return false;float d=(world.Local(p)+Vector3.one*.5f-game.Player.transform.position).sqrMagnitude;
            return d>36*36&&d<(world.FogEnd+4)*(world.FogEnd+4);
        }
        void Refresh()
        {
            next=Time.unscaledTime+.25f;InstanceCount=0;represented.Clear();foreach(var batch in batches.Values)batch.Matrices.Clear();
            var sim=game.Industry.Simulation;var world=game.World;tanks??=GetComponent<MultiblockPresentation>();detailed??=GetComponent<IndustryPresentation>();
            foreach(var m in sim.EligibleMachines)
            {
                if(!ReferenceEquals(sim.At(m.Position),m)||!InRange(m.Position))continue;
                var matrix=FactoryVisibility.Placement(world.Local(m.Position),m.Rotation);byte id=m.Definition.Id;
                if(IndustryId.TankPart(id))
                {
                    int before=InstanceCount;var shell=tanks.ShellMeshes(m);for(int i=0;i<Math.Min(2,shell.Length);i++)if(shell[i]!=null)Add(i==1?shell[i]:Derived(shell[i]),i==1?glass:workshop,matrix);
                    if(InstanceCount>before)represented.Add(m.Position);continue;
                }
                int previous=InstanceCount;
                bool active=id==IndustryId.ElectricFurnace||id==IndustryId.RangedPump?m.Running:m.Signal||m.Source;
                var body=detailed.BodyMaterial(active);
                var topology=id==IndustryId.PowerCable?sim.Power.Topology:id==IndustryId.ItemPipe?sim.ItemNetwork:id==IndustryId.FluidPipe?sim.FluidNetwork:sim.Signals.Topology;
                topology.Connections.TryGetValue(m.Position,out int mask);
                if(ConnectedPipeVisuals.UsesConnectedMesh(id))
                {
                    int disconnected=PipeConnections.IsTransport(id)?sim.DisconnectedPipeFaces(m):0;
                    matrix=Matrix4x4.Translate(world.Local(m.Position))*ConnectedPipeVisuals.ShapeTransform(mask,m.Rotation,disconnected);
                    Add(Shape(id,mask),body,matrix);
                    foreach(var addition in additions)
                    {
                        if((m.Additions&addition)==0)continue;var network=addition==PipeAddition.Power?sim.Power.Topology:sim.Signals.Topology;network.Connections.TryGetValue(m.Position,out int a);
                        var source=ConnectedPipeVisuals.Shape(addition==PipeAddition.Power?"pipe_power_addition":"pipe_signal_addition",a);
                        Add(Derived(source),detailed.BodyMaterial(addition==PipeAddition.Signal&&m.Signal),Matrix4x4.Translate(world.Local(m.Position)));
                    }
                }
                else
                {
                    int local=0;for(int f=0;f<6;f++)if((mask&(1<<IndustryDefinition.RotateFace(f,m.Rotation)))!=0)local|=1<<f;
                    int pose=id==IndustryId.WoodenDoor?(m.WorkInput==1?1:0):id==IndustryId.Door&&m.Running?1:0;
                    Add(Shape(id,local,pose),body,matrix);
                }
                if(InstanceCount>previous)represented.Add(m.Position);
            }
            var center=world.Address(game.Player.transform.position);int radius=Mathf.CeilToInt(world.FogEnd/32)+1;
            foreach(var p in game.Crates.Nearby(center,radius))if(InRange(p)&&game.Survival.At(p) is StationState state)Add(Shape(state.Block),crateMaterial,FactoryVisibility.Placement(world.Local(p),state.Rotation));
            foreach(var bed in world.NearbyBeds(center,radius))if(InRange(bed.Foot)&&world.Ready(bed.Head))
            {
                var q=Quaternion.Euler(0,bed.Rotation*90,0);Add(Shape(BedId.Bed),bedMaterial,Matrix4x4.TRS(world.Local(bed.Foot)+new Vector3(.5f,0,.5f)-q*new Vector3(.5f,0,.5f),q,Vector3.one));
            }
        }
        void LateUpdate()
        {
            if(game==null)return;FactoryVisibility.Globals(game.Player.transform.position,game.World.FogEnd);DrawCount=0;
            if(FactoryVisibility.LegacyReview||FactoryVisibility.FullDetailReview)return;
            Build();if(Time.unscaledTime>=next)Refresh();
            GeometryUtility.CalculateFrustumPlanes(game.Player.Camera,planes);
            foreach(var batch in batches.Values)
            {
                int count=0;
                foreach(var matrix in batch.Matrices)
                {
                    var bounds=batch.Mesh.bounds;var center=matrix.MultiplyPoint3x4(bounds.center);var e=bounds.extents;
                    var x=matrix.MultiplyVector(new Vector3(e.x,0,0));var y=matrix.MultiplyVector(new Vector3(0,e.y,0));var z=matrix.MultiplyVector(new Vector3(0,0,e.z));
                    bounds=new Bounds(center,2*new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z)));
                    if(!FactoryVisibility.Visible(game.Player.transform.position,bounds,game.World.FogEnd)||!GeometryUtility.TestPlanesAABB(planes,bounds))continue;
                    instances[count++]=matrix;if(count==instances.Length){Draw(batch,count);count=0;}
                }
                if(count>0)Draw(batch,count);
            }
        }
        void Draw(Batch batch,int count)
        {
            var parameters=new RenderParams(batch.Material){camera=game.Player.Camera,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false,lightProbeUsage=LightProbeUsage.Off};
            Graphics.RenderMeshInstanced(parameters,batch.Mesh,0,instances,count);DrawCount++;
        }
        void OnDestroy()
        {
            if(game!=null){game.World.OriginShifted-=Shift;game.World.BlockChanged-=Changed;}
            foreach(var m in shapes.Values)Destroy(m);foreach(var m in derived.Values)Destroy(m);
            foreach(var m in farMaterials.Values)Destroy(m);foreach(var m in nearMaterials.Values)Destroy(m);
        }
    }
}
