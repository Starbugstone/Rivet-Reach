using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    // Connected surfaces are assembled from the actual Blender shell parts. Voxel cells remain untouched.
    public sealed class MultiblockPresentation : MonoBehaviour
    {
        sealed class View {public GameObject Root;public MachineState Machine;public string Key;}
        readonly Dictionary<BlockPos,View> views=new Dictionary<BlockPos,View>();
        readonly Dictionary<string,Mesh[]> cache=new Dictionary<string,Mesh[]>();
        readonly Dictionary<MultiblockInstance,GameObject> liquids=new Dictionary<MultiblockInstance,GameObject>();
        readonly HashSet<BlockPos> visible=new HashSet<BlockPos>();readonly List<BlockPos> remove=new List<BlockPos>();
        readonly List<MultiblockInstance> removeLiquid=new List<MultiblockInstance>();
        Expedition game;float next;Material metal,glass,water,status;GameObject fault;
        MaterialPropertyBlock properties;
        public int ViewCount=>views.Count;
        public IEnumerable<GameObject> RenderedRoots
        {get{foreach(var view in views.Values)yield return view.Root;foreach(var liquid in liquids.Values)yield return liquid;}}
        public void Initialize(Expedition value)
        {
            game=value;metal=Resources.Load<Material>("Industry/Workshop");glass=Resources.Load<Material>("Industry/TankGlass");water=Resources.Load<Material>("Industry/TankWater");status=Resources.Load<Material>("Industry/Status");properties=new MaterialPropertyBlock();
            game.World.OriginShifted+=Shift;
        }
        void Shift(Vector3 delta){foreach(var v in views.Values)v.Root.transform.position-=delta;foreach(var v in liquids.Values)v.transform.position-=delta;if(fault!=null)fault.transform.position-=delta;next=0;}
        void Update()
        {
            if(game==null||Time.unscaledTime<next)return;next=Time.unscaledTime+.1f;
            var sim=game.Industry.Simulation;visible.Clear();
            foreach(var m in sim.EligibleMachines)
            {
                if(!IndustryId.TankPart(m.Definition.Id)||!game.World.Ready(m.Position)||(game.World.Local(m.Position)-game.Player.transform.position).sqrMagnitude>64*64)continue;
                visible.Add(m.Position);string key=Key(m,out int faces,out int[] joins);
                if(!views.TryGetValue(m.Position,out var v)){v=new View{Root=new GameObject(m.Definition.Name),Machine=m};v.Root.transform.SetParent(transform,false);views.Add(m.Position,v);}
                if(v.Key!=key)
                {
                    for(int i=v.Root.transform.childCount-1;i>=0;i--)Destroy(v.Root.transform.GetChild(i).gameObject);
                    if(!cache.TryGetValue(key,out var meshes)){meshes=Assemble(m,faces,joins);cache.Add(key,meshes);}
                    for(int i=0;i<meshes.Length;i++)if(meshes[i]!=null){var part=new GameObject(i==0?"Connected metal":i==1?"Connected glass":"StatusLight");part.transform.SetParent(v.Root.transform,false);part.AddComponent<MeshFilter>().sharedMesh=meshes[i];var r=part.AddComponent<MeshRenderer>();r.sharedMaterial=i==0?metal:i==1?glass:status;r.shadowCastingMode=i==1?ShadowCastingMode.Off:ShadowCastingMode.On;}
                    v.Key=key;
                }
                v.Root.transform.rotation=Quaternion.Euler(0,m.Rotation*90,0);
                v.Root.transform.position=game.World.Local(m.Position)+Vector3.one*.5f-v.Root.transform.rotation*(Vector3.one*.5f);
                properties.SetColor("_BaseColor",m.Structure?.Formed==true?new Color(.15f,.85f,.56f):new Color(1,.26f,.06f));
                foreach(var r in v.Root.GetComponentsInChildren<Renderer>())if(r.name=="StatusLight")r.SetPropertyBlock(properties);
            }
            remove.Clear();foreach(var pair in views)if(!visible.Contains(pair.Key))remove.Add(pair.Key);
            foreach(var p in remove){Destroy(views[p].Root);views.Remove(p);}
            removeLiquid.Clear();
            foreach(var c in sim.Multiblocks.Instances)
            {
                if(c.Fluid==null)continue;
                bool show=c.Formed&&c.Fluid.Amount>0&&visible.Contains(c.Controller.Position);
                if(!show){if(liquids.TryGetValue(c,out var old)){Destroy(old);liquids.Remove(c);}continue;}
                if(!liquids.TryGetValue(c,out var liquid))
                {liquid=GameObject.CreatePrimitive(PrimitiveType.Cube);liquid.name="Contained liquid · presentation only";Destroy(liquid.GetComponent<Collider>());liquid.transform.SetParent(transform,false);liquid.GetComponent<Renderer>().sharedMaterial=water;liquids.Add(c,liquid);}
                float height=(c.Bounds.Height-2)*(float)(c.Fluid.Amount/(double)c.Fluid.Capacity);
                liquid.transform.position=game.World.Local(c.Bounds.Min)+new Vector3(c.Bounds.Width*.5f,1+height*.5f,c.Bounds.Depth*.5f);
                liquid.transform.localScale=new Vector3(c.Bounds.Width-2-.015f,Mathf.Max(.003f,height),c.Bounds.Depth-2-.015f);
                var fluid=c.Fluid.Fluid;properties.SetColor("_BaseColor",new Color(fluid.Red,fluid.Green,fluid.Blue,.88f));liquid.GetComponent<Renderer>().SetPropertyBlock(properties);
            }
            foreach(var pair in liquids)if(sim.Multiblocks.At(pair.Key.Controller.Position)!=pair.Key)removeLiquid.Add(pair.Key);
            foreach(var c in removeLiquid){Destroy(liquids[c]);liquids.Remove(c);}
            var inspect=game.OpenMachine?.Structure;
            bool highlight=inspect!=null&&!inspect.Formed&&inspect.Validation.Fault.HasValue&&game.World.Ready(inspect.Validation.Fault.Value);
            if(highlight)
            {
                if(fault==null){fault=new GameObject("Invalid structure coordinate");fault.transform.SetParent(transform,false);var lines=fault.AddComponent<LineRenderer>();lines.sharedMaterial=status;lines.startWidth=lines.endWidth=.025f;lines.useWorldSpace=false;lines.positionCount=16;lines.SetPositions(new[]{new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(1,1,0),new Vector3(0,1,0),new Vector3(0,0,0),new Vector3(0,0,1),new Vector3(1,0,1),new Vector3(1,1,1),new Vector3(0,1,1),new Vector3(0,0,1),new Vector3(1,0,1),new Vector3(1,0,0),new Vector3(1,1,0),new Vector3(1,1,1),new Vector3(0,1,1),new Vector3(0,1,0)});}
                fault.transform.position=game.World.Local(inspect.Validation.Fault.Value);fault.SetActive(true);
            }
            else if(fault!=null)fault.SetActive(false);
        }
        string Key(MachineState m,out int faces,out int[] joins)
        {
            faces=0;joins=new int[6];var c=m.Structure;bool formed=c?.Formed==true;
            for(int f=0;f<6;f++)
            {
                int wf=IndustryDefinition.RotateFace(f,m.Rotation);var n=IndustryDefinition.Neighbor(m.Position,wf);
                bool outside=!formed||!c.Bounds.Contains(n);if(!outside)continue;faces|=1<<f;
                if(!formed)continue;
                for(int t=0;t<6;t++)
                {
                    if(t==f||t==(f^1))continue;
                    var adjacent=IndustryDefinition.Neighbor(m.Position,IndustryDefinition.RotateFace(t,m.Rotation));
                    if(c.Validation.Members.ContainsKey(adjacent)&&game.World.Get(adjacent)==m.Definition.Id&&!c.Bounds.Contains(IndustryDefinition.Neighbor(adjacent,wf)))joins[f]|=1<<t;
                }
            }
            return m.Definition.Id+":"+faces+":"+string.Join(",",joins);
        }
        Mesh[] Assemble(MachineState m,int faces,int[] joins)
        {
            var prefab=Resources.Load<GameObject>("Industry/Runtime/"+m.Definition.Key);var groups=new[]{new List<CombineInstance>(),new List<CombineInstance>(),new List<CombineInstance>()};
            foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())
            {
                string name=filter.name;int group=0;
                if(name.StartsWith("Skin")&&m.Structure?.Formed!=true)continue;
                if(name.StartsWith("Face")||name.StartsWith("Glass")||name.StartsWith("Skin")){int face=name[name.Length-1]-'0';if((faces&(1<<face))==0)continue;if(name.StartsWith("Glass"))group=1;}
                if(name.StartsWith("Trim")){int face=name[4]-'0',side=name[6]-'0';if((faces&(1<<face))==0||(joins[face]&(1<<side))!=0)continue;}
                if(name=="StatusLight")group=2;
                groups[group].Add(new CombineInstance{mesh=filter.sharedMesh,transform=filter.transform.localToWorldMatrix});
            }
            var result=new Mesh[3];for(int i=0;i<3;i++)if(groups[i].Count>0){var mesh=new Mesh{name="Connected "+m.Definition.Key,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(groups[i].ToArray(),true,true);result[i]=mesh;}
            return result;
        }
        void OnDestroy(){if(game!=null)game.World.OriginShifted-=Shift;foreach(var group in cache.Values)foreach(var mesh in group)if(mesh!=null)Destroy(mesh);}
    }
}
