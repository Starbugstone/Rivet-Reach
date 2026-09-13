using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    // One nearby view per placed assembly; no per-wire simulation component or material instance.
    public sealed class IndustryPresentation : MonoBehaviour
    {
        sealed class View
        {
            public GameObject Root,SignalAddition,PowerAddition;public Transform ChargeFill;public long ShownCharge=-1;public MachineState State;public Transform[] Parts;public Vector3[] Rest;public Quaternion[] RestRotation;
            public Renderer[] Renderers;public Light Light;public float Phase;public int Mask=-1;public bool Active,MaterialShown;public MachineStatus ShownStatus=(MachineStatus)(-1);
        }
        Expedition game;float nextRefresh;long shownRevision=-1;
        readonly Dictionary<BlockPos,View> views=new Dictionary<BlockPos,View>();
        readonly List<BlockPos> remove=new List<BlockPos>();
        readonly HashSet<MachineState> visible=new HashSet<MachineState>();
        readonly List<MachineState> nearby=new List<MachineState>();
        MaterialPropertyBlock properties,statusProperties;
        public int ViewCount=>views.Count;
        public GameObject ViewAt(BlockPos position)=>views.TryGetValue(position,out var view)?view.Root:null;
        public void Initialize(Expedition game){this.game=game;properties=new MaterialPropertyBlock();statusProperties=new MaterialPropertyBlock();game.World.OriginShifted+=Shift;}
        void SetAddition(GameObject root,NetworkTopology topology,MachineState m)
        {
            if(root==null)return;topology.Connections.TryGetValue(m.Position,out int mask);
            properties.SetColor("_EmissionColor",topology.Kind==NetworkKind.Signal&&m.Signal?new Color(1.5f,1.5f,1.5f):new Color(.12f,.12f,.12f));
            if(ConnectedPipeVisuals.UsesConnectedMesh(m.Definition.Id))
            {
                ConnectedPipeVisuals.Set(root,topology.Kind==NetworkKind.Signal?"pipe_signal_addition":"pipe_power_addition",mask);
                root.GetComponentInChildren<Renderer>().SetPropertyBlock(properties);return;
            }
            foreach(Transform t in root.transform)
            {
                if(t.name.StartsWith("Arm")&&t.name.Length>3){int face=t.name[3]-'0';t.gameObject.SetActive((mask&(1<<IndustryDefinition.RotateFace(face,m.Rotation)))!=0);}
                var r=t.GetComponent<Renderer>();if(r!=null)r.SetPropertyBlock(properties);
            }
        }
        void Shift(Vector3 delta){nextRefresh=0;foreach(var v in views.Values)v.Root.transform.position-=delta;}
        void Update()
        {
            if(game==null)return;var sim=game.Industry.Simulation;
            if(Time.unscaledTime>=nextRefresh)
            {
                nextRefresh=Time.unscaledTime+.25f;nearby.Clear();visible.Clear();
                foreach(var m in sim.EligibleMachines)if(!IndustryId.TankPart(m.Definition.Id)&&game.World.Ready(m.Position)&&(game.World.Local(m.Position)-game.Player.transform.position).sqrMagnitude<64*64){nearby.Add(m);visible.Add(m);}
                nearby.Sort((a,b)=>(game.World.Local(a.Position)-game.Player.transform.position).sqrMagnitude.CompareTo((game.World.Local(b.Position)-game.Player.transform.position).sqrMagnitude));
                remove.Clear();foreach(var pair in views)if(!visible.Contains(pair.Value.State))remove.Add(pair.Key);
                foreach(var p in remove){Destroy(views[p].Root);views.Remove(p);}
                int lights=0;
                for(int i=0;i<nearby.Count;i++)
                {
                    var m=nearby[i];
                    if(!views.TryGetValue(m.Position,out var v))
                    {
                        var prefab=Resources.Load<GameObject>("Industry/Runtime/"+m.Definition.Key);if(prefab==null)continue;
                        var root=ConnectedPipeVisuals.UsesConnectedMesh(m.Definition.Id)?ConnectedPipeVisuals.Create(m.Definition.Key,transform):Instantiate(prefab,transform,false);root.name=m.Definition.Name;
                        v=new View{Root=root,State=m,Parts=root.GetComponentsInChildren<Transform>(),Renderers=root.GetComponentsInChildren<Renderer>()};
                        v.Rest=new Vector3[v.Parts.Length];v.RestRotation=new Quaternion[v.Parts.Length];for(int j=0;j<v.Parts.Length;j++){v.Rest[j]=v.Parts[j].localPosition;v.RestRotation[j]=v.Parts[j].localRotation;}
                        foreach(var r in v.Renderers){r.sharedMaterial=Resources.Load<Material>(r.name=="StatusLight"?"Industry/Status":"Industry/Workshop");r.shadowCastingMode=ShadowCastingMode.On;}
                        if(m.Definition.Id==IndustryId.Lamp){var bulb=new GameObject("Workshop bulb");bulb.transform.SetParent(root.transform,false);bulb.transform.localPosition=new Vector3(.5f,.8f,.5f);v.Light=bulb.AddComponent<Light>();v.Light.type=LightType.Point;v.Light.color=new Color(1,.77f,.37f);v.Light.range=7;v.Light.shadows=LightShadows.None;}
                        if(m.Definition.Id==IndustryId.Battery)
                        {
                            var fill=GameObject.CreatePrimitive(PrimitiveType.Cube);fill.name="Stored energy fill";
                            Destroy(fill.GetComponent<Collider>());fill.transform.SetParent(root.transform,false);
                            var renderer=fill.GetComponent<Renderer>();renderer.sharedMaterial=Resources.Load<Material>("Industry/BatteryCharge");
                            renderer.shadowCastingMode=ShadowCastingMode.Off;v.ChargeFill=fill.transform;fill.SetActive(false);
                        }
                        views.Add(m.Position,v);
                    }
                    v.Root.transform.position=game.World.Local(m.Position)+Vector3.one*.5f;
                    v.Root.transform.rotation=ConnectedPipeVisuals.UsesConnectedMesh(m.Definition.Id)?Quaternion.identity:Quaternion.Euler(0,m.Rotation*90,0);
                    // Exported geometry has a [0,1] horizontal footprint. Rotate about its centre.
                    v.Root.transform.position-=v.Root.transform.rotation*(Vector3.one*.5f);
                    if(PipeConnections.IsTransport(m.Definition.Id))
                    {
                        if((m.Additions&PipeAddition.Signal)!=0&&v.SignalAddition==null)v.SignalAddition=ConnectedPipeVisuals.Create("pipe_signal_addition",v.Root.transform);
                        if((m.Additions&PipeAddition.Power)!=0&&v.PowerAddition==null)v.PowerAddition=ConnectedPipeVisuals.Create("pipe_power_addition",v.Root.transform);
                        SetAddition(v.SignalAddition,sim.Signals.Topology,m);SetAddition(v.PowerAddition,sim.Power.Topology,m);
                    }
                    if(v.Light!=null){v.Light.enabled=m.Running&&lights++<8;v.Light.intensity=2.5f*m.ReceivedWatts/Mathf.Max(1,m.Definition.Watts);}
                }
            }
            bool revision=shownRevision!=sim.Revision;shownRevision=sim.Revision;
            foreach(var v in views.Values)
            {
                var m=v.State;bool active=m.Definition.Id==IndustryId.ElectricFurnace?m.Running:m.Signal||m.Source;
                if(v.ChargeFill!=null)
                {
                    // Read the physical cell even when a bank owns its electrical endpoint.
                    var cell=m.EnergyCells[0];
                    if(v.ShownCharge!=cell.Amount)
                    {
                        v.ShownCharge=cell.Amount;float height=.72f*(float)(cell.Amount/(double)cell.Capacity);
                        v.ChargeFill.localScale=new Vector3(.76f,height,.76f);
                        v.ChargeFill.localPosition=new Vector3(.5f,.14f+height*.5f,.5f);
                        v.ChargeFill.gameObject.SetActive(cell.Amount>0);
                    }
                }
                if(!game.Paused&&m.Running)v.Phase+=Time.deltaTime*(m.Definition.Id==IndustryId.HandCrank?720:180)*(m.Definition.Watts==0?1:m.ReceivedWatts/(float)m.Definition.Watts);
                if(revision||v.Mask<0)
                {
                    var topology=m.Definition.Id==IndustryId.PowerCable?sim.Power.Topology:m.Definition.Id==IndustryId.ItemPipe?sim.ItemNetwork:m.Definition.Id==IndustryId.FluidPipe?sim.FluidNetwork:sim.Signals.Topology;
                    topology.Connections.TryGetValue(m.Position,out int mask);
                    if(ConnectedPipeVisuals.UsesConnectedMesh(m.Definition.Id))ConnectedPipeVisuals.Set(v.Root,m.Definition.Key,mask,m.Rotation,PipeConnections.IsTransport(m.Definition.Id)?sim.DisconnectedPipeFaces(m):0);
                    if(v.Mask!=mask)
                    {v.Mask=mask;foreach(var t in v.Parts)if(t.name.StartsWith("Arm")&&t.name.Length>3&&char.IsDigit(t.name[3])){int face=t.name[3]-'0';int rotated=IndustryDefinition.RotateFace(face,m.Rotation);t.gameObject.SetActive((mask&(1<<rotated))!=0);}}
                    if(!v.MaterialShown||v.Active!=active)
                    {v.MaterialShown=true;v.Active=active;properties.SetColor("_EmissionColor",active?new Color(1.5f,1.5f,1.5f):new Color(.12f,.12f,.12f));foreach(var r in v.Renderers)if(r.name!="StatusLight")r.SetPropertyBlock(properties);}
                }
                if(v.ShownStatus!=m.Status)
                {
                    v.ShownStatus=m.Status;var color=m.Running?(m.Status==MachineStatus.Underpowered?new Color(1,.68f,.12f):new Color(.24f,1,.61f)):m.Status==MachineStatus.DisabledBySignal?new Color(.2f,.25f,.28f):m.Status==MachineStatus.Ready?new Color(.30f,.52f,.62f):new Color(1,.31f,.08f);
                    statusProperties.SetColor("_BaseColor",color);
                    foreach(var r in v.Renderers)if(r.name=="StatusLight")r.SetPropertyBlock(statusProperties);
                }
                if(IndustryId.Route(m.Definition.Id))continue;
                for(int i=0;i<v.Parts.Length;i++)
                {
                    var t=v.Parts[i];string n=t.name;
                    if(n.StartsWith("MotionSpin"))
                    {var axis=m.Definition.Id==IndustryId.Boiler||m.Definition.Id==IndustryId.Alternator?Vector3.right:m.Definition.Id==IndustryId.Crusher||m.Definition.Id==IndustryId.Pump?Vector3.forward:m.Definition.Id==IndustryId.HandCrank?Vector3.forward:Vector3.up;t.localRotation=Quaternion.AngleAxis(v.Phase*(n.StartsWith("MotionSpinB")?-1:1),axis)*v.RestRotation[i];}
                    else if(n.StartsWith("MotionPiston"))t.localPosition=v.Rest[i]+Vector3.up*(m.Definition.Id==IndustryId.Button?(m.Source?-.035f:0):m.Running?Mathf.Sin(v.Phase*Mathf.Deg2Rad)*.035f:0);
                    else if(n.StartsWith("MotionLever"))t.localRotation=Quaternion.Slerp(t.localRotation,Quaternion.Euler(m.Source?30:-30,0,0),1-Mathf.Exp(-18*Time.deltaTime));
                    else if(n=="MotionDoor")t.localRotation=v.RestRotation[i]*Quaternion.Euler(0,m.WorkInput==1?-90:0,0);
                    else if(n.StartsWith("MotionHatch")){t.localScale=Vector3.Lerp(t.localScale,new Vector3(1,m.Running?.08f:1,1),1-Mathf.Exp(-12*Time.deltaTime));t.localPosition=v.Rest[i]+Vector3.up*(m.Running?.37f:0);}
                }
            }
        }
    }
}
