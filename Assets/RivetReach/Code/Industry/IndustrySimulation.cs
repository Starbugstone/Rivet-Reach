using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RivetReach
{
    public interface IIndustryWorld
    {
        bool Ready(BlockPos p);byte Get(BlockPos p);bool Remove(BlockPos p,byte expected);
        ItemContainer Storage(BlockPos p);byte Drop(byte block);bool PlayerInside(BlockPos p);
    }
    public sealed partial class IndustrySimulation
    {
        readonly IIndustryWorld world;readonly Func<byte,int> limit;
        readonly Dictionary<BlockPos,MachineState> machines=new Dictionary<BlockPos,MachineState>();
        readonly List<MachineState> eligible=new List<MachineState>();
        IEnumerator<int> rebuild;bool dirty=true,signalsDirty=true;
        readonly List<MachineState> devices=new List<MachineState>();
        readonly Dictionary<FluidStorage,long> fluidAvailable=new Dictionary<FluidStorage,long>(),fluidCapacity=new Dictionary<FluidStorage,long>();
        readonly Dictionary<FluidStorage,FluidDefinition> fluidTypes=new Dictionary<FluidStorage,FluidDefinition>();
        readonly List<(FluidStorage from,FluidStorage to,FluidDefinition fluid,long amount)> fluidTransfers=new List<(FluidStorage,FluidStorage,FluidDefinition,long)>();
        public readonly MultiblockService Multiblocks;
        public readonly SignalNetworkService Signals=new SignalNetworkService();
        public readonly PowerNetworkService Power=new PowerNetworkService();
        public readonly NetworkTopology ItemNetwork=new NetworkTopology(NetworkKind.Item),FluidNetwork=new NetworkTopology(NetworkKind.Fluid);
        public IReadOnlyDictionary<BlockPos,MachineState> Machines=>machines;
        public IReadOnlyList<MachineState> EligibleMachines=>eligible;
        public long Tick {get;private set;}
        public long Revision {get;private set;}
        public int TopologyRebuilds {get;private set;}
        public bool Rebuilding=>dirty||rebuild!=null;
        public double LastStepMs {get;private set;}
        public IndustrySimulation(IIndustryWorld world,Func<byte,int> limit)
        {
            this.world=world;this.limit=limit;Multiblocks=new MultiblockService(world,this);
            ItemNetwork.ExternalEndpointFaces=p=>world.Ready(p)&&world.Storage(p)!=null?63:0;
            ItemNetwork.ResolvePorts=m=>TransportPorts(m,NetworkKind.Item);
            FluidNetwork.ResolvePorts=m=>TransportPorts(m,NetworkKind.Fluid);
        }
        public MachineState At(BlockPos p)=>machines.TryGetValue(p,out var m)?m:null;
        public void Invalidate(){dirty=true;signalsDirty=true;Revision++;}
        public MachineState Add(BlockPos p,byte id)
        {if(machines.ContainsKey(p))throw new InvalidOperationException("Occupied machine anchor");var m=new MachineState(p,id,limit);machines.Add(p,m);if(id==IndustryId.TankController)Multiblocks.Register(m,MultiblockDefinition.Tank);if(id==IndustryId.BatteryController)Multiblocks.Register(m,MultiblockDefinition.BatteryBank);Multiblocks.Changed(p);Invalidate();return m;}
        public MachineState Remove(BlockPos p)
        {var m=At(p);if(m!=null){if(!Multiblocks.CanRemove(p))return null;Multiblocks.RemoveController(p);machines.Remove(p);Multiblocks.Changed(p);Invalidate();}return m;}
        public void Rotate(MachineState m){m.Rotation=(m.Rotation+1)%4;Multiblocks.Changed(m.Position);Invalidate();}
        public void Activate(MachineState m)
        {if(m.Definition.Id==IndustryId.Lever)m.Source=!m.Source;else if(m.Definition.Id==IndustryId.Button){m.Source=true;m.PulseTicks=20;}signalsDirty=true;Revision++;}
        public void ToggleDoor(MachineState m)
        {
            if(m==null||m.Definition.Id!=IndustryId.WoodenDoor||At(m.Position)!=m||!world.Ready(m.Position)||!world.Ready(m.Position.Offset(0,1,0)))return;
            m.Source=m.WorkInput==0;ApplyDoor(m);Revision++;
        }
        void ApplyDoor(MachineState m)
        {
            // Source is the requested opening; WorkInput stores the physical latch independently
            // of transient Running/Dormant status. Both fields already persist in machine saves.
            if(m.Source)m.WorkInput=1;
            else if(!world.PlayerInside(m.Position)&&!world.PlayerInside(m.Position.Offset(0,1,0)))m.WorkInput=0;
            m.Status=m.WorkInput==1?MachineStatus.Running:MachineStatus.Ready;
        }
        public const int CrankTicks=10,CrankWatts=100;
        // One paid stroke at a time; the fixed simulation clock also bounds rapid clicks.
        public bool TryCrank(MachineState m)
        {
            if(m==null||m.Definition.Id!=IndustryId.HandCrank||At(m.Position)!=m||!world.Ready(m.Position)||Rebuilding||m.PulseTicks!=0)return false;
            m.PulseTicks=CrankTicks;Revision++;return true;
        }
        public void Step()
        {
            long start=Stopwatch.GetTimestamp();Tick++;Multiblocks.Step();
            if(dirty)
            {
                rebuild?.Dispose();dirty=false;eligible.Clear();devices.Clear();
                foreach(var m in machines.Values)
                {m.Eligible=world.Ready(m.Position)&&(m.Definition.Id!=IndustryId.WoodenDoor||world.Ready(m.Position.Offset(0,1,0)));m.ReceivedWatts=m.RequestedWatts=m.SupplyWatts=0;m.Signal=false;m.SignalAttached=false;m.FluidConflict=false;if(m.Eligible)eligible.Add(m);else m.Status=MachineStatus.Dormant;}
                eligible.Sort((a,b)=>Compare(a.Position,b.Position));InitializePipeEnds();foreach(var m in eligible)if(!IndustryId.Route(m.Definition.Id))devices.Add(m);rebuild=Rebuild().GetEnumerator();TopologyRebuilds++;
            }
            if(rebuild!=null)
            {
                bool done=false;for(int budget=0;budget<2048;budget++)if(!rebuild.MoveNext()){done=true;break;}
                if(!done){LastStepMs=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;return;}
                rebuild.Dispose();rebuild=null;Revision++;
            }
            foreach(var m in devices)
            {
                m.RequestedWatts=m.ReceivedWatts=m.SupplyWatts=m.BatteryWatts=0;
                if(IndustryId.BatteryPart(m.Definition.Id))m.Status=m.Definition.Id==IndustryId.BatteryController&&m.Structure?.Formed!=true?MachineStatus.StructureInvalid:MachineStatus.Ready;
                if(m.Definition.Id==IndustryId.Relay&&m.Source!=m.NextSource){m.Source=m.NextSource;signalsDirty=true;}
                if(IndustryId.TankPart(m.Definition.Id))
                {
                    m.Status=m.Structure?.Formed==true?MachineStatus.Ready:MachineStatus.StructureInvalid;
                    if(m.Definition.Id==IndustryId.TankSensor){var tank=m.Structure;bool source=tank?.Formed==true&&tank.Fluid.Amount*100>=tank.Fluid.Capacity*m.LevelThreshold;if(m.Source!=source){m.Source=source;signalsDirty=true;}}
                }
                if(m.Definition.Id==IndustryId.Sensor)
                {var c=world.Storage(Neighbor(m,4));int count=0;if(world.Ready(Neighbor(m,4))&&c!=null)foreach(var s in c.Slots)count+=s.Count;bool source=count>=32;if(source!=m.Source){m.Source=source;signalsDirty=true;}}
            }
            if(signalsDirty){Signals.Evaluate();signalsDirty=false;}
            foreach(var m in devices)
            {
                byte id=m.Definition.Id;
                if(id==IndustryId.Relay)m.NextSource=m.Signal;
                if(id==IndustryId.Button&&m.PulseTicks>0&&--m.PulseTicks==0){m.Source=false;signalsDirty=true;}
                if(id==IndustryId.Boiler)Boiler(m);
            }
            foreach(var m in devices)
            {
                if(m.Definition.Id==IndustryId.Alternator)
                {
                    var engine=At(Neighbor(m,1));
                    bool coupled=engine!=null&&engine.Eligible&&engine.Definition.Id==IndustryId.Boiler&&Neighbor(engine,0).Equals(m.Position)&&engine.Running;
                    m.SupplyWatts=coupled?400:0;m.Status=coupled?MachineStatus.Running:MachineStatus.NoShaft;
                }
                if(m.Definition.Id==IndustryId.HandCrank)
                {
                    m.SupplyWatts=m.PulseTicks>0?CrankWatts:0;
                    m.Status=m.PulseTicks>0?MachineStatus.Running:MachineStatus.Ready;
                    if(m.PulseTicks>0)m.PulseTicks--;
                }
                Prepare(m);
            }
            Power.Allocate(Tick);
            foreach(var m in devices)if(IndustryId.BatteryPart(m.Definition.Id)&&m.BatteryWatts!=0)m.Status=MachineStatus.Running;
            // Transfers precede processing: new products cannot be forwarded in their producing tick.
            TransferConfiguredItems();TransferFluids();
            foreach(var m in devices)Advance(m);
            Revision++;LastStepMs=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
        }
        IEnumerable<int> Rebuild()
        {foreach(var graph in new[]{Signals.Topology,Power.Topology,ItemNetwork,FluidNetwork})foreach(var unit in graph.Rebuild(eligible))yield return unit;}
        static int Compare(BlockPos a,BlockPos b){int c=a.X.CompareTo(b.X);if(c!=0)return c;c=a.Y.CompareTo(b.Y);return c!=0?c:a.Z.CompareTo(b.Z);}
        public static BlockPos Neighbor(MachineState m,int face)=>IndustryDefinition.Neighbor(m.Position,face,m.Rotation);
        void Boiler(MachineState m)
        {
            if(m.WaterMl<5){m.Status=MachineStatus.NoWater;return;}
            if(m.BurnTicks==0)
            {
                var fuel=m.Items.Slots[0];if(fuel.Empty){m.Status=MachineStatus.NoFuel;return;}
                if(!m.Accepts(0,fuel.Id)){m.Status=MachineStatus.NoFuel;return;}
                m.Items.Take(0,1);m.BurnTicks=1600;
            }
            m.BurnTicks--;m.WaterMl-=5;m.Status=MachineStatus.Running;
        }
        void Prepare(MachineState m)
        {
            if(m.Definition.Watts==0)return;
            if(!m.Enabled){m.Status=MachineStatus.DisabledBySignal;return;}
            byte id=m.Definition.Id;
            if(id==IndustryId.Crusher)
            {
                var input=m.Items.Slots[0];byte output=MachineState.Crushed(input.Id);
                if(input.Id!=m.WorkInput){m.Work=0;m.WorkInput=input.Id;}
                if(output==0||input.Empty){m.Status=MachineStatus.NoInput;return;}
                if(m.Items.Capacity(output,2,3)<2){m.Status=MachineStatus.OutputFull;return;}
            }
            if(id==IndustryId.Pump)
            {
                if(m.WaterMl>m.Definition.WaterCapacity-10000){m.Status=MachineStatus.OutputFull;return;}
                var p=Neighbor(m,3);if(!world.Ready(p)){m.Status=MachineStatus.Dormant;return;}
                if(world.Get(p)!=Fluids.Water.Source){m.Work=0;m.Status=MachineStatus.NoWater;return;}
            }
            if(id==IndustryId.Drill)
            {
                var p=m.Position.Offset(0,-m.DrillDepth,0);
                if(!world.Ready(p)){m.Status=MachineStatus.Dormant;return;}
                byte block=world.Get(p);
                if(block==BlockId.Air){m.Work=0;m.DrillDepth++;m.Status=MachineStatus.Ready;return;}
                if(block==BlockId.Bedrock||p.Y<TerrainGenerator.MinY){m.Status=MachineStatus.Depleted;return;}
                if(!BlockId.Mineable(block,ToolCapability.Pickaxe,ToolTier.Iron)||IndustryId.Placed(block)||BlockId.Station(block)){m.Status=MachineStatus.NoInput;return;}
                if(m.WorkInput!=block){m.Work=0;m.WorkInput=block;}
                if(m.Items.Capacity(world.Drop(block),2,3)<1){m.Status=MachineStatus.OutputFull;return;}
            }
            m.RequestedWatts=m.Definition.Watts;m.Status=MachineStatus.NoPower;
        }
        void Advance(MachineState m)
        {
            byte id=m.Definition.Id;
            if(id==IndustryId.WoodenDoor)
            {
                // Signal edges set the requested position; manual use remains available between edges.
                if(m.Signal!=m.NextSource){m.Source=m.Signal;m.NextSource=m.Signal;}
                ApplyDoor(m);return;
            }
            if(id==IndustryId.Door){m.Status=m.Signal&&!world.PlayerInside(m.Position)?MachineStatus.Running:world.PlayerInside(m.Position)?m.Status:MachineStatus.Ready;return;}
            if(id==IndustryId.Indicator||id==IndustryId.Relay||id==IndustryId.Lever||id==IndustryId.Button||id==IndustryId.Sensor)
            {m.Status=(id==IndustryId.Lever||id==IndustryId.Button||id==IndustryId.Sensor?m.Source:m.Signal)?MachineStatus.Running:MachineStatus.Ready;return;}
            if(m.RequestedWatts==0||m.ReceivedWatts==0)return;
            m.Status=m.ReceivedWatts<m.RequestedWatts?MachineStatus.Underpowered:MachineStatus.Running;
            if(id==IndustryId.Lamp)return;
            m.Work+=m.ReceivedWatts/(double)m.Definition.Watts;
            int duration=id==IndustryId.Crusher?MachineState.CrusherTicks:id==IndustryId.Pump?40:120;
            if(m.Work+1e-9<duration)return;
            if(id==IndustryId.Crusher)
            {byte output=MachineState.Crushed(m.Items.Slots[0].Id);if(output==0||m.Items.Capacity(output,2,3)<2)return;m.Items.Take(0,1);m.Items.Add(output,2,2,3);}
            if(id==IndustryId.Pump)
            {var p=Neighbor(m,3);if(world.Get(p)!=Fluids.Water.Source||!world.Remove(p,Fluids.Water.Source))return;m.WaterMl+=10000;}
            if(id==IndustryId.Drill)
            {var p=m.Position.Offset(0,-m.DrillDepth,0);byte b=world.Get(p);if(b!=m.WorkInput||!world.Remove(p,b))return;m.Items.Add(world.Drop(b),1,2,3);m.DrillDepth++;}
            m.Work-=duration;
        }
        void TransferFluids()
        {
            // Reservations are keyed by storage identity across ALL ports and graphs, never by shell block.
            var available=fluidAvailable;var capacity=fluidCapacity;var transfers=fluidTransfers;
            available.Clear();capacity.Clear();transfers.Clear();fluidTypes.Clear();
            foreach(var g in FluidNetwork.Groups)
            foreach(var p in g.Ports)
            {
                p.Machine.FluidConflict=false;
                if(p.Port.Role==PortRole.Route||!PipeConnections.FluidEnabled(p.Machine))continue;
                var storage=PipeConnections.Storage(p.Machine);if(storage==null||available.ContainsKey(storage))continue;
                available[storage]=storage.Amount;capacity[storage]=storage.Capacity-storage.Amount;fluidTypes[storage]=storage.Fluid;
            }
            foreach(var g in FluidNetwork.Groups)
            {
                FluidDefinition networkFluid=null;bool conflict=false;
                foreach(var endpoint in g.Ports)
                {
                    if(endpoint.Port.Role==PortRole.Route||!PipeConnections.FluidEnabled(endpoint.Machine))continue;
                    var storage=PipeConnections.Storage(endpoint.Machine);if(storage?.Fluid==null)continue;
                    if(networkFluid!=null&&networkFluid.StableId!=storage.Fluid.StableId)conflict=true;
                    networkFluid=storage.Fluid;
                }
                if(conflict){foreach(var endpoint in g.Ports)endpoint.Machine.FluidConflict=true;continue;}
                int count=g.Ports.Count,start=(int)(Tick%Math.Max(1,count));
                for(int n=0;n<count;n++)
                {
                    var source=g.Ports[(start+n)%count];
                    if(source.Port.Role!=PortRole.Output||!PipeConnections.FluidEnabled(source.Machine))continue;
                    var from=PipeConnections.Storage(source.Machine);
                    if(from==null||!available.TryGetValue(from,out long amount)||amount==0)continue;
                    long budget=Math.Min(100,amount);var fluid=from.Fluid;
                    for(int k=0;k<count&&budget>0;k++)
                    {
                        var dest=g.Ports[(start+k)%count];
                        if(dest.Port.Role!=PortRole.Input||!PipeConnections.FluidEnabled(dest.Machine)||!PipeConnections.Accepts(dest.Machine,fluid))continue;
                        var to=PipeConnections.Storage(dest.Machine);
                        if(to==null||to==from||!capacity.ContainsKey(to)||fluidTypes[to]!=null&&fluidTypes[to].StableId!=fluid.StableId)continue;
                        long take=Math.Min(budget,capacity[to]);if(take<=0)continue;
                        transfers.Add((from,to,fluid,take));available[from]-=take;capacity[to]-=take;fluidTypes[to]=fluid;budget-=take;
                    }
                }
            }
            // Synchronous authority turn: edits cannot interleave reservation and commit.
            foreach(var t in transfers)if(!t.from.Withdraw(t.amount))throw new InvalidOperationException("Stale fluid withdrawal");
            foreach(var t in transfers)if(!t.to.Deposit(t.fluid,t.amount))throw new InvalidOperationException("Stale fluid deposit");
        }
    }
}
