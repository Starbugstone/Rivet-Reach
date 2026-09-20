using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Stopwatch=System.Diagnostics.Stopwatch;

namespace RivetReach
{
    // Simulation and targeting remain authoritative while nearby views are reused
    // or built cooperatively. Existing views never wait behind new prefab work.
    public sealed class IndustryPresentation : MonoBehaviour
    {
        public const float LampRange=20,LampIntensity=30;
        public const int InactiveViewCapacity=512,DestroyBudgetPerFrame=4,CreateBudgetPerFrame=12;
        public const double CreateBudgetMilliseconds=1;
        struct Motion
        {
            public Transform Part;public Vector3 Rest,ShownPosition,ShownScale;public Quaternion Rotation,ShownRotation;public byte Kind;public bool ShownActive;
        }
        struct Arm {public Transform Part;public int Face;}
        struct Candidate {public MachineState State;public float Distance;}
        sealed class Addition
        {
            public GameObject Root;public Renderer Renderer;public Dictionary<BlockPos,int> Connections;public int Mask=-1;public bool Shown,Signal;
        }
        sealed class View
        {
            public TextMesh LinkLabel;public string LinkName,LinkStatus;public bool LoaderEnabled;
            public Dictionary<BlockPos,int> Connections;
            public int GeometryRotation=-1,GeometryDirections=-1,Disconnected=-1,PlacementRotation=-1;
            public GameObject Root;public Addition SignalAddition,PowerAddition;public Transform ChargeFill;public long ShownCharge=-1;public MachineState State;
            public Motion[] Motions;public Arm[] Arms;public Renderer[] Renderers,StatusRenderers;public Light Light;public bool LightWanted;public float LightBlend,Phase;
            public int Mask=-1;public bool Active,MaterialShown;public MachineStatus ShownStatus=(MachineStatus)(-1);
            public bool MotionShown,MotionRunning,MotionSource,MotionFilled,MotionSimulating,Settling;public int MotionCompost;public byte MotionInput;public float MotionPhase;
        }
        Expedition game;VoxelWorld world;float nextRefresh;long shownRevision=-1;
        readonly PresentationViewCache<MachineState,View> cache=new PresentationViewCache<MachineState,View>(InactiveViewCapacity);
        readonly Dictionary<BlockPos,View> views=new Dictionary<BlockPos,View>();
        readonly List<BlockPos> remove=new List<BlockPos>();
        readonly HashSet<MachineState> visible=new HashSet<MachineState>();
        readonly List<Candidate> nearby=new List<Candidate>(),pending=new List<Candidate>();
        static readonly Comparison<Candidate> nearest=(a,b)=>a.Distance.CompareTo(b.Distance);
        readonly MachineState[] nearestLamps=new MachineState[8];readonly float[] lampDistances=new float[8];int lampCount,pendingIndex;
        readonly GameObject[] prefabs=new GameObject[256];readonly bool[] prefabLoaded=new bool[256];
        Material batteryMaterial;IndustryMaterialVariants materials;
        public int MaterialVariantCount=>materials?.Count??0;
        public int ViewCount=>views.Count;
        public int CachedViewCount=>cache.CachedCount;
        public int PendingDestroyCount=>cache.PendingDestroyCount;
        public int PeakPendingDestroyCount=>cache.PeakPendingDestroyCount;
        public int PendingCreateCount=>pending.Count-pendingIndex;
        public int CreatedThisFrame {get;private set;}
        public int PeakCreatedPerFrame {get;private set;}
        public double LastCreateMilliseconds {get;private set;}
        public long CreatedViews=>cache.Created;
        public long ReusedViews=>cache.Reused;
        public long DestroyedViews=>cache.Destroyed;
        public GameObject ViewAt(BlockPos position)=>views.TryGetValue(position,out var view)?view.Root:null;
        public void Initialize(Expedition game)
        {
            this.game=game;world=game.World;world.OriginShifted+=Shift;
            materials=new IndustryMaterialVariants(Resources.Load<Material>("Industry/Workshop"),Resources.Load<Material>("Industry/Status"));batteryMaterial=Resources.Load<Material>("Industry/BatteryCharge");
        }
        void SetAddition(Addition addition,NetworkTopology topology,MachineState m)
        {
            if(addition==null)return;
            if(!ReferenceEquals(addition.Connections,topology.Connections))
            {
                topology.Connections.TryGetValue(m.Position,out int mask);
                if(addition.Mask!=mask)ConnectedPipeVisuals.Set(addition.Root,topology.Kind==NetworkKind.Signal?"pipe_signal_addition":"pipe_power_addition",mask);
                addition.Mask=mask;addition.Connections=topology.Connections;
            }
            bool signal=topology.Kind==NetworkKind.Signal&&m.Signal;
            if(!addition.Shown||addition.Signal!=signal)
            {
                addition.Shown=true;addition.Signal=signal;IndustryMaterialVariants.Apply(addition.Renderer,materials.Body(signal));
            }
        }
        Addition CreateAddition(string key,Transform parent)
        {var root=ConnectedPipeVisuals.Create(key,parent);return new Addition{Root=root,Renderer=root.GetComponentInChildren<Renderer>()};}
        void Shift(Vector3 delta){nextRefresh=0;foreach(var view in views.Values)view.Root.transform.position-=delta;}
        void SelectLamp(MachineState machine,float distance)
        {
            int index=lampCount;if(index==nearestLamps.Length){if(distance>=lampDistances[index-1])return;index--;}
            else lampCount++;
            while(index>0&&distance<lampDistances[index-1])
            {nearestLamps[index]=nearestLamps[index-1];lampDistances[index]=lampDistances[index-1];index--;}
            nearestLamps[index]=machine;lampDistances[index]=distance;
        }
        bool SelectedLamp(MachineState machine)
        {for(int i=0;i<lampCount;i++)if(ReferenceEquals(nearestLamps[i],machine))return true;return false;}
        float ViewRangeSquared(MachineState machine)=>machine.Definition.Id==IndustryId.Lamp?world.FogEnd*world.FogEnd:64*64;
        void HideView(BlockPos position)
        {
            var view=views[position];view.LightWanted=false;view.LightBlend=0;if(view.Light!=null)view.Light.enabled=false;
            view.Connections=null;if(view.SignalAddition!=null)view.SignalAddition.Connections=null;if(view.PowerAddition!=null)view.PowerAddition.Connections=null;
            cache.Hide(view.State);views.Remove(position);
        }
        void PruneReplacedViews(IndustrySimulation sim)
        {
            remove.Clear();
            foreach(var pair in views)if(!ReferenceEquals(sim.At(pair.Key),pair.Value.State))remove.Add(pair.Key);
            foreach(var position in remove)HideView(position);
        }
        void RefreshVisible(IndustrySimulation sim)
        {
            nextRefresh=Time.unscaledTime+.25f;nearby.Clear();visible.Clear();pending.Clear();pendingIndex=0;lampCount=0;
            var player=game.Player.transform.position;
            foreach(var machine in sim.EligibleMachines)
            {
                if(!ReferenceEquals(sim.At(machine.Position),machine)||IndustryId.TankPart(machine.Definition.Id)||!world.Ready(machine.Position))continue;
                float distance=(world.Local(machine.Position)-player).sqrMagnitude;if(distance>=ViewRangeSquared(machine))continue;
                nearby.Add(new Candidate{State=machine,Distance=distance});visible.Add(machine);
                if(machine.Definition.Id==IndustryId.Lamp&&machine.Running&&sim.IsSimulating(machine))SelectLamp(machine,distance);
            }
            remove.Clear();foreach(var pair in views)if(!visible.Contains(pair.Value.State))remove.Add(pair.Key);
            foreach(var position in remove)HideView(position);
            foreach(var candidate in nearby)
            {
                var machine=candidate.State;
                if(!views.TryGetValue(machine.Position,out var view)&&cache.TryActivate(machine,out view))
                {view.Mask=-1;view.Connections=null;view.PlacementRotation=-1;view.MotionShown=false;views.Add(machine.Position,view);}
                if(view==null){pending.Add(candidate);continue;}
                RefreshView(view,sim);
            }
            if(pending.Count>1)pending.Sort(nearest);
        }
        View CreateView(MachineState machine)
        {
            byte id=machine.Definition.Id;
            if(!prefabLoaded[id]){prefabs[id]=Resources.Load<GameObject>("Industry/Runtime/"+machine.Definition.Key);prefabLoaded[id]=true;}
            if(prefabs[id]==null)return null;
            var root=ConnectedPipeVisuals.UsesConnectedMesh(id)?ConnectedPipeVisuals.Create(machine.Definition.Key,transform):Instantiate(prefabs[id],transform,false);root.name=machine.Definition.Name;
            var view=new View{Root=root,State=machine};var motions=new List<Motion>();var arms=new List<Arm>();
            foreach(var part in root.GetComponentsInChildren<Transform>())
            {
                string name=part.name;byte kind=name=="CompostFill"?(byte)1:name.StartsWith("MotionSpinB")?(byte)3:name.StartsWith("MotionSpin")?(byte)2:name.StartsWith("MotionPiston")||name.StartsWith("MotionBob")?(byte)4:name.StartsWith("MotionLever")?(byte)5:name=="MotionDoor"?(byte)6:name.StartsWith("MotionHatch")?(byte)7:(byte)0;
                if(kind!=0)motions.Add(new Motion{Part=part,Rest=part.localPosition,Rotation=part.localRotation,Kind=kind,ShownPosition=part.localPosition,ShownScale=part.localScale,ShownRotation=part.localRotation,ShownActive=part.gameObject.activeSelf});
                if(name.StartsWith("Arm")&&name.Length>3&&char.IsDigit(name[3]))arms.Add(new Arm{Part=part,Face=name[3]-'0'});
            }
            view.Motions=motions.ToArray();view.Arms=arms.ToArray();var regular=new List<Renderer>();var status=new List<Renderer>();
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {
                bool indicator=renderer.name=="StatusLight";IndustryMaterialVariants.Apply(renderer,indicator?materials.Status(machine.Status,machine.Running):materials.Body(false));renderer.shadowCastingMode=ShadowCastingMode.On;
                (indicator?status:regular).Add(renderer);
            }
            view.Renderers=regular.ToArray();view.StatusRenderers=status.ToArray();
            if(id==IndustryId.Lamp)
            {var bulb=new GameObject("Workshop bulb");bulb.transform.SetParent(root.transform,false);bulb.transform.localPosition=new Vector3(.5f,.8f,.5f);view.Light=bulb.AddComponent<Light>();view.Light.type=LightType.Point;view.Light.color=new Color(1,.77f,.37f);view.Light.range=LampRange;view.Light.shadows=LightShadows.None;}
            if(id==IndustryId.Battery)
            {
                var fill=GameObject.CreatePrimitive(PrimitiveType.Cube);fill.name="Stored energy fill";Destroy(fill.GetComponent<Collider>());fill.transform.SetParent(root.transform,false);
                var renderer=fill.GetComponent<Renderer>();renderer.sharedMaterial=batteryMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;view.ChargeFill=fill.transform;fill.SetActive(false);
            }
            if(IndustryId.Bridge(id)||id==IndustryId.ChunkLoader)
            {
                var label=new GameObject("Network label");label.transform.SetParent(root.transform,false);view.LinkLabel=label.AddComponent<TextMesh>();
                view.LinkLabel.anchor=TextAnchor.MiddleCenter;view.LinkLabel.alignment=TextAlignment.Center;view.LinkLabel.fontSize=40;view.LinkLabel.characterSize=.035f;view.LinkLabel.richText=false;
            }
            cache.Register(machine,view,root);views.Add(machine.Position,view);return view;
        }
        void RefreshView(View view,IndustrySimulation sim)
        {
            var machine=view.State;
            if(view.PlacementRotation!=machine.Rotation)
            {
                var rotation=ConnectedPipeVisuals.UsesConnectedMesh(machine.Definition.Id)?Quaternion.identity:Quaternion.Euler(0,machine.Rotation*90,0);
                view.Root.transform.SetPositionAndRotation(world.Local(machine.Position)+Vector3.one*.5f-rotation*(Vector3.one*.5f),rotation);view.PlacementRotation=machine.Rotation;
            }
            if(PipeConnections.IsTransport(machine.Definition.Id))
            {
                if((machine.Additions&PipeAddition.Signal)!=0&&view.SignalAddition==null)view.SignalAddition=CreateAddition("pipe_signal_addition",view.Root.transform);
                if((machine.Additions&PipeAddition.Power)!=0&&view.PowerAddition==null)view.PowerAddition=CreateAddition("pipe_power_addition",view.Root.transform);
                SetAddition(view.SignalAddition,sim.Signals.Topology,machine);SetAddition(view.PowerAddition,sim.Power.Topology,machine);
            }
            if(view.Light!=null)view.LightWanted=SelectedLamp(machine);
            if(view.LinkLabel!=null)
            {
                bool loader=machine.Definition.Id==IndustryId.ChunkLoader;string status=loader?null:sim.BridgeStatus(machine);
                if(view.LinkName!=machine.LinkName||view.LinkStatus!=status||view.LoaderEnabled!=machine.LoaderEnabled)
                {
                    view.LinkName=machine.LinkName;view.LinkStatus=status;view.LoaderEnabled=machine.LoaderEnabled;
                    view.LinkLabel.text=loader?"CHUNK LOADER\n"+(machine.LoaderEnabled?"ACTIVE":"DISABLED"):machine.Definition.Name+" · "+(machine.LinkName.Length==0?"UNLINKED":machine.LinkName)+"\n"+status;
                    view.LinkLabel.color=loader?(machine.LoaderEnabled?new Color(.4f,1,.8f):Color.gray):sim.BridgePartner(machine)!=null?new Color(.4f,1,.8f):new Color(1,.73f,.3f);
                }
                bool show=game.Mode==ScreenMode.Play&&(world.Local(machine.Position)-game.Player.transform.position).sqrMagnitude<12*12;
                if(view.LinkLabel.gameObject.activeSelf!=show)view.LinkLabel.gameObject.SetActive(show);
            }
        }
        void BuildPending(IndustrySimulation sim)
        {
            CreatedThisFrame=0;LastCreateMilliseconds=0;if(pendingIndex>=pending.Count)return;long started=Stopwatch.GetTimestamp();int attempted=0;
            while(pendingIndex<pending.Count&&attempted<CreateBudgetPerFrame&&(attempted==0||(Stopwatch.GetTimestamp()-started)*1000.0/Stopwatch.Frequency<CreateBudgetMilliseconds))
            {
                var machine=pending[pendingIndex++].State;attempted++;
                if(!ReferenceEquals(sim.At(machine.Position),machine)||!machine.Eligible||!world.Ready(machine.Position)||(world.Local(machine.Position)-game.Player.transform.position).sqrMagnitude>=ViewRangeSquared(machine))continue;
                var view=CreateView(machine);if(view==null)continue;RefreshView(view,sim);CreatedThisFrame++;
            }
            LastCreateMilliseconds=(Stopwatch.GetTimestamp()-started)*1000.0/Stopwatch.Frequency;if(CreatedThisFrame>PeakCreatedPerFrame)PeakCreatedPerFrame=CreatedThisFrame;
        }
        void Update()
        {
            using var cost=RuntimeCosts.Machines.Auto();
            cache.DestroyPending(DestroyBudgetPerFrame);if(game==null)return;var sim=game.Industry.Simulation;
            // EligibleMachines remains the last atomic graph snapshot during a
            // rebuild. A mined/replaced identity must disappear before it settles.
            if(shownRevision!=sim.Revision)PruneReplacedViews(sim);
            if(Time.unscaledTime>=nextRefresh)RefreshVisible(sim);
            BuildPending(sim);bool revision=shownRevision!=sim.Revision;shownRevision=sim.Revision;
            foreach(var view in views.Values)
            {
                var machine=view.State;bool simulating=sim.IsSimulating(machine);
                if(view.LinkLabel!=null&&view.LinkLabel.gameObject.activeSelf)
                {view.LinkLabel.transform.position=world.Local(machine.Position)+new Vector3(.5f,1.16f,.5f);view.LinkLabel.transform.rotation=game.Player.Camera.transform.rotation;}
                if(view.Light!=null)
                {
                    view.LightBlend=machine.Running&&simulating?Mathf.MoveTowards(view.LightBlend,view.LightWanted?1:0,Time.unscaledDeltaTime*2):0;
                    view.Light.enabled=view.LightBlend>0;
                    if(view.Light.enabled)view.Light.intensity=LampIntensity*machine.ReceivedWatts/Mathf.Max(1,machine.Definition.Watts)*TorchPresentation.DistanceFade(Vector3.Distance(view.Light.transform.position,game.Player.transform.position))*Mathf.SmoothStep(0,1,view.LightBlend);
                }
                if(view.ChargeFill!=null&&view.ShownCharge!=machine.EnergyCells[0].Amount)
                {
                    var cell=machine.EnergyCells[0];view.ShownCharge=cell.Amount;float height=.72f*(float)(cell.Amount/(double)cell.Capacity);
                    view.ChargeFill.localScale=new Vector3(.76f,height,.76f);view.ChargeFill.localPosition=new Vector3(.5f,.14f+height*.5f,.5f);view.ChargeFill.gameObject.SetActive(cell.Amount>0);
                }
                bool active=machine.Definition.Id==IndustryId.ElectricFurnace||machine.Definition.Id==IndustryId.RangedPump?machine.Running:machine.Signal||machine.Source;
                if(revision||view.Mask<0)
                {
                    var topology=machine.Definition.Id==IndustryId.PowerCable?sim.Power.Topology:machine.Definition.Id==IndustryId.ItemPipe?sim.ItemNetwork:machine.Definition.Id==IndustryId.FluidPipe?sim.FluidNetwork:sim.Signals.Topology;
                    if(!ReferenceEquals(view.Connections,topology.Connections)||view.GeometryRotation!=machine.Rotation||view.GeometryDirections!=machine.PipeDirections)
                    {
                        topology.Connections.TryGetValue(machine.Position,out int mask);int disconnected=PipeConnections.IsTransport(machine.Definition.Id)?sim.DisconnectedPipeFaces(machine):0;
                        bool shapeChanged=view.Mask!=mask||view.GeometryRotation!=machine.Rotation||view.Disconnected!=disconnected;
                        if(shapeChanged&&ConnectedPipeVisuals.UsesConnectedMesh(machine.Definition.Id))ConnectedPipeVisuals.Set(view.Root,machine.Definition.Key,mask,machine.Rotation,disconnected);
                        if(shapeChanged)foreach(var arm in view.Arms){int rotated=IndustryDefinition.RotateFace(arm.Face,machine.Rotation);arm.Part.gameObject.SetActive((mask&(1<<rotated))!=0);}
                        view.Connections=topology.Connections;view.Mask=mask;view.GeometryRotation=machine.Rotation;view.GeometryDirections=machine.PipeDirections;view.Disconnected=disconnected;
                    }
                    if(!view.MaterialShown||view.Active!=active)
                    {view.MaterialShown=true;view.Active=active;var material=materials.Body(active);foreach(var renderer in view.Renderers)IndustryMaterialVariants.Apply(renderer,material);}
                }
                if(view.ShownStatus!=machine.Status)
                {
                    view.ShownStatus=machine.Status;var material=materials.Status(machine.Status,machine.Running);
                    foreach(var renderer in view.StatusRenderers)IndustryMaterialVariants.Apply(renderer,material);
                }
                if(view.Motions.Length==0||IndustryId.Route(machine.Definition.Id))continue;
                bool turning=machine.Definition.Id!=IndustryId.WindTurbine||machine.DeliveredWatts>0;
                if(!game.Paused&&simulating&&machine.Running&&turning)view.Phase+=Time.deltaTime*(machine.Definition.Id==IndustryId.HandCrank?720:180)*(machine.Definition.Id==IndustryId.WindTurbine?machine.SupplyWatts/(float)RenewableCatalog.Current.windPeakWatts:machine.Definition.Watts==0?1:machine.ReceivedWatts/(float)machine.Definition.Watts);
                Animate(view,simulating);
            }
        }
        static void Position(ref Motion motion,Vector3 value){if(motion.ShownPosition.Equals(value))return;motion.Part.localPosition=value;motion.ShownPosition=value;}
        static void Rotation(ref Motion motion,Quaternion value){if(motion.ShownRotation.Equals(value))return;motion.Part.localRotation=value;motion.ShownRotation=value;}
        void Animate(View view,bool simulating)
        {
            var machine=view.State;bool filled=machine.IsComposter&&(!machine.Items.Slots[0].Empty||!machine.Items.Slots[2].Empty);
            if(view.MotionShown&&!view.Settling&&view.MotionSimulating==simulating&&view.MotionPhase==view.Phase&&view.MotionRunning==machine.Running&&view.MotionSource==machine.Source&&view.MotionInput==machine.WorkInput&&view.MotionCompost==machine.CompostPoints&&view.MotionFilled==filled)return;
            view.MotionShown=true;view.MotionSimulating=simulating;view.Settling=false;view.MotionPhase=view.Phase;view.MotionRunning=machine.Running;view.MotionSource=machine.Source;view.MotionInput=machine.WorkInput;view.MotionCompost=machine.CompostPoints;view.MotionFilled=filled;
            for(int i=0;i<view.Motions.Length;i++)
            {
                ref var motion=ref view.Motions[i];byte kind=motion.Kind;
                if(kind==1)
                {
                    bool active=machine.CompostPoints>0||filled;if(motion.ShownActive!=active){motion.Part.gameObject.SetActive(active);motion.ShownActive=active;}
                    Position(ref motion,motion.Rest+Vector3.down*(.55f*(1-(filled?1:(float)machine.CompostPoints/CompostCatalog.Current.pointsPerCompost))));
                }
                else if(kind==2||kind==3)
                {var axis=machine.Definition.Id==IndustryId.Boiler||machine.Definition.Id==IndustryId.Alternator?Vector3.right:machine.Definition.Id==IndustryId.Crusher||machine.Definition.Id==IndustryId.Pump||machine.Definition.Id==IndustryId.HandCrank?Vector3.forward:Vector3.up;Rotation(ref motion,Quaternion.AngleAxis(view.Phase*(kind==3?-1:1),axis)*motion.Rotation);}
                else if(kind==4)Position(ref motion,motion.Rest+Vector3.up*(machine.Definition.Id==IndustryId.Button?(machine.Source?-.035f:0):machine.Running?Mathf.Sin(view.Phase*Mathf.Deg2Rad)*.035f:0));
                else if(kind==5)
                {
                    var target=Quaternion.Euler(machine.Source?30:-30,0,0);var value=Quaternion.Slerp(motion.ShownRotation,target,1-Mathf.Exp(-18*Time.deltaTime));
                    bool settling=Quaternion.Angle(value,target)>.01f;Rotation(ref motion,settling?value:target);view.Settling|=settling;
                }
                else if(kind==6)Rotation(ref motion,motion.Rotation*Quaternion.Euler(0,machine.WorkInput==1?-90:0,0));
                else if(kind==7)
                {
                    if(!simulating)continue;
                    var target=new Vector3(1,machine.Running?.08f:1,1);var scale=Vector3.Lerp(motion.ShownScale,target,1-Mathf.Exp(-12*Time.deltaTime));bool settling=(scale-target).sqrMagnitude>1e-8f;if(!settling)scale=target;
                    if(!motion.ShownScale.Equals(scale)){motion.Part.localScale=scale;motion.ShownScale=scale;}
                    Position(ref motion,motion.Rest+Vector3.up*(machine.Running?.37f:0));view.Settling|=settling;
                }
            }
        }
        void OnDestroy(){if(world!=null)world.OriginShifted-=Shift;cache.Clear();views.Clear();nearby.Clear();pending.Clear();visible.Clear();remove.Clear();materials?.Dispose();materials=null;batteryMaterial=null;game=null;world=null;}
    }

    // Hidden views stay bound to their exact session state; there is no rebinding
    // of inventories, child renderers or Canvas references. Only hidden roots are
    // evicted, and native teardown is spread over frames after immediate hiding.
    internal sealed class PresentationViewCache<TState,TView> where TState:class where TView:class
    {
        sealed class Entry
        {
            public TState State;public TView View;public GameObject Root;
            public LinkedListNode<Entry> InactiveNode;
        }
        sealed class IdentityComparer : IEqualityComparer<TState>
        {
            public bool Equals(TState a,TState b)=>ReferenceEquals(a,b);
            public int GetHashCode(TState state)=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(state);
        }
        readonly int capacity;
        readonly Dictionary<TState,Entry> entries=new Dictionary<TState,Entry>(new IdentityComparer());
        readonly LinkedList<Entry> inactive=new LinkedList<Entry>();
        readonly Queue<GameObject> retired=new Queue<GameObject>();
        public int CachedCount=>inactive.Count;
        public int PendingDestroyCount=>retired.Count;
        public int PeakPendingDestroyCount {get;private set;}
        public long Created {get;private set;}
        public long Reused {get;private set;}
        public long Destroyed {get;private set;}
        public PresentationViewCache(int capacity){this.capacity=capacity;}
        public void Register(TState state,TView view,GameObject root)
        {
            var entry=new Entry{State=state,View=view,Root=root};entry.InactiveNode=new LinkedListNode<Entry>(entry);
            entries.Add(state,entry);Created++;
        }
        public bool TryActivate(TState state,out TView view)
        {
            if(entries.TryGetValue(state,out var entry))
            {
                if(entry.InactiveNode.List!=null){inactive.Remove(entry.InactiveNode);entry.Root.SetActive(true);Reused++;}
                view=entry.View;return true;
            }
            view=null;return false;
        }
        public void Hide(TState state)
        {
            if(!entries.TryGetValue(state,out var entry)||entry.InactiveNode.List!=null)return;
            entry.Root.SetActive(false);inactive.AddLast(entry.InactiveNode);
            while(inactive.Count>capacity)
            {
                var oldest=inactive.First.Value;inactive.RemoveFirst();entries.Remove(oldest.State);retired.Enqueue(oldest.Root);if(retired.Count>PeakPendingDestroyCount)PeakPendingDestroyCount=retired.Count;
            }
        }
        public void DestroyPending(int budget)
        {
            while(budget-->0&&retired.Count>0){UnityEngine.Object.Destroy(retired.Dequeue());Destroyed++;}
        }
        // All roots remain children of their presentation owner, including retired
        // roots. Unity destroys that hierarchy on session disposal.
        public void Clear(){entries.Clear();inactive.Clear();retired.Clear();}
    }
}
