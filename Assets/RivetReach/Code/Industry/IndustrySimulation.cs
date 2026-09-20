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
        readonly IIndustryWorld world;readonly Func<byte,int> limit;readonly ProcessingRegistry processing;
        readonly Dictionary<BlockPos,MachineState> machines=new Dictionary<BlockPos,MachineState>();
        List<MachineState> eligible=new List<MachineState>();
        IEnumerator<int> rebuild;bool dirty=true,signalsDirty=true;
        List<MachineState> devices=new List<MachineState>();
        readonly Dictionary<FluidStorage,long> fluidAvailable=new Dictionary<FluidStorage,long>(),fluidCapacity=new Dictionary<FluidStorage,long>();
        readonly FairAllocation<FluidStorage> fluidSources=new FairAllocation<FluidStorage>(),fluidSinks=new FairAllocation<FluidStorage>();
        readonly Dictionary<FluidStorage,long> fluidSourceLimits=new Dictionary<FluidStorage,long>();
        readonly Dictionary<FluidStorage,FluidDefinition> fluidTypes=new Dictionary<FluidStorage,FluidDefinition>();
        readonly HashSet<FluidStorage> fluidSinkIdentities=new HashSet<FluidStorage>();
        readonly List<(FluidStorage from,FluidStorage to,FluidDefinition fluid,long amount)> fluidTransfers=new List<(FluidStorage,FluidStorage,FluidDefinition,long)>();
        readonly Func<FluidStorage,long,long> reserveFluidSource,reserveFluidSink;
        readonly Predicate<FluidStorage> otherFluidSink;
        FluidStorage reservedFluidSource;FluidDefinition reservedFluidType;
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
        public IndustrySimulation(IIndustryWorld world,Func<byte,int> limit,ProcessingRegistry processing=null)
        {
            this.world=world;this.limit=limit;this.processing=processing;Multiblocks=new MultiblockService(world,this);
            reserveFluidSource=ReserveFluidSource;reserveFluidSink=ReserveFluidSink;otherFluidSink=OtherFluidSink;
            ItemNetwork.ExternalEndpointFaces=p=>ItemEndpoint(p)!=null?63:0;
            ItemNetwork.ResolvePorts=m=>TransportPorts(m,NetworkKind.Item);
            FluidNetwork.ResolvePorts=m=>TransportPorts(m,NetworkKind.Fluid);
            foreach(var graph in new[]{Power.Topology,ItemNetwork,FluidNetwork})graph.RemotePartner=BridgePartner;
        }
        public MachineState At(BlockPos p)=>machines.TryGetValue(p,out var m)?m:null;
        public void Invalidate()=>InvalidateAllComponents();
        public void ResidencyChanged(ChunkPos chunk)
        {
            Multiblocks.ResidencyChanged(chunk);
            // Include adjacent external inventories and a door's upper cell,
            // even when this page does not contain an industry anchor itself.
            InvalidateComponentPage(chunk);
        }
        public MachineState Add(BlockPos p,byte id)
        {if(machines.ContainsKey(p))throw new InvalidOperationException("Occupied machine anchor");var m=new MachineState(p,id,limit,processing);machines.Add(p,m);RegisterComponent(m);if(m.IsComposter)m.CompostChanged=machine=>CompostChanged?.Invoke(machine);if(IndustryId.Bridge(id)||id==IndustryId.ChunkLoader)m.OwnerId=LocalOwnerId;if(id==IndustryId.TankController)Multiblocks.Register(m,MultiblockDefinition.Tank);if(id==IndustryId.BatteryController)Multiblocks.Register(m,MultiblockDefinition.BatteryBank);Multiblocks.Changed(p);Invalidate(p);return m;}
        public MachineState Remove(BlockPos p)
        {var m=At(p);if(m!=null){if(!Multiblocks.CanRemove(p))return null;Multiblocks.RemoveController(p);machines.Remove(p);lampLightStates.Remove(p);Multiblocks.Changed(p);Invalidate(p);}return m;}
        public void Rotate(MachineState m){m.Rotation=(m.Rotation+1)%4;Multiblocks.Changed(m.Position);Invalidate(m.Position);}
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
        public const int CrankTicks=10,CrankWatts=100,AlternatorWatts=800;
        // One paid stroke at a time; the fixed simulation clock also bounds rapid clicks.
        public bool TryCrank(MachineState m)
        {
            if(m==null||m.Definition.Id!=IndustryId.HandCrank||At(m.Position)!=m||!world.Ready(m.Position)||!CanSimulate(m)||m.PulseTicks!=0)return false;
            m.PulseTicks=CrankTicks;Revision++;return true;
        }
        public event System.Action<BlockPos> LightChanged;
        readonly Dictionary<BlockPos,bool> lampLightStates=new Dictionary<BlockPos,bool>();
        public void Step()
        {
            using var cost=RuntimeCosts.Industry.Auto();
            long start=Stopwatch.GetTimestamp();Tick++;AdvanceStructureValidation();
            AdvanceReconstruction();
            foreach(var m in devices)
            {
                if(CanSimulate(m))continue;
                ResetTransferredPower(m);
                if(m.Definition.Id==IndustryId.Lamp&&lampLightStates.TryGetValue(m.Position,out bool lit)&&lit)
                {lampLightStates[m.Position]=false;LightChanged?.Invoke(m.Position);}
                if(!world.Ready(m.Position)||m.Definition.Id==IndustryId.WoodenDoor&&!world.Ready(m.Position.Offset(0,1,0)))
                {m.Eligible=false;ResetNetworkState(m);m.Status=MachineStatus.Dormant;}
            }
            foreach(var m in devices)
            {
                if(!CanSimulate(m))continue;
                m.RequestedWatts=0;ResetTransferredPower(m);
                if(IndustryId.BatteryPart(m.Definition.Id))m.Status=m.Definition.Id==IndustryId.BatteryController&&m.Structure?.Formed!=true?MachineStatus.StructureInvalid:MachineStatus.Ready;
                if(m.Definition.Id==IndustryId.Relay&&m.Source!=m.NextSource){m.Source=m.NextSource;signalsDirty=true;}
                if(IndustryId.TankPart(m.Definition.Id))
                {
                    m.Status=m.Structure?.Formed==true?MachineStatus.Ready:MachineStatus.StructureInvalid;
                    if(m.Definition.Id==IndustryId.TankSensor){var tank=m.Structure;bool source=tank?.Formed==true&&tank.Fluid.Amount*100>=tank.Fluid.Capacity*m.LevelThreshold;if(m.Source!=source){m.Source=source;signalsDirty=true;}}
                }
                if(m.Definition.Id==IndustryId.Sensor)
                {var c=ItemEndpoint(Neighbor(m,4));int count=0;if(world.Ready(Neighbor(m,4))&&c!=null)foreach(var s in c.Slots)count+=s.Count;bool source=count>=32;if(source!=m.Source){m.Source=source;signalsDirty=true;}}
            }
            if(signalsDirty){Signals.Evaluate();signalsDirty=false;}
            foreach(var m in devices)
            {
                if(!CanSimulate(m))continue;
                byte id=m.Definition.Id;
                if(id==IndustryId.Relay)m.NextSource=m.Signal;
                if(id==IndustryId.Button&&m.PulseTicks>0&&--m.PulseTicks==0){m.Source=false;signalsDirty=true;}
                if(id==IndustryId.Boiler)Boiler(m);
            }
            foreach(var m in devices)
            {
                if(!CanSimulate(m))continue;
                if(m.Definition.Id==IndustryId.Alternator)
                {
                    var engine=At(Neighbor(m,1));
                    bool coupled=engine!=null&&engine.Eligible&&engine.Definition.Id==IndustryId.Boiler&&Neighbor(engine,0).Equals(m.Position)&&engine.Running;
                    m.SupplyWatts=coupled?AlternatorWatts:0;m.Status=coupled?MachineStatus.Running:MachineStatus.NoShaft;
                }
                if(m.Definition.Id==IndustryId.HandCrank)
                {
                    m.SupplyWatts=m.PulseTicks>0?CrankWatts:0;
                    m.Status=m.PulseTicks>0?MachineStatus.Running:MachineStatus.Ready;
                    if(m.PulseTicks>0)m.PulseTicks--;
                }
                if(IndustryId.Renewable(m.Definition.Id))PrepareRenewable(m);
                Prepare(m);
            }
            Power.Allocate(Tick);
            foreach(var m in devices)if(CanSimulate(m)&&IndustryId.BatteryPart(m.Definition.Id)&&(m.BatteryInputWatts>0||m.BatteryOutputWatts>0))m.Status=MachineStatus.Running;
            // Transfers precede processing: new products cannot be forwarded in their producing tick.
            TransferConfiguredItems();TransferFluids();
            foreach(var m in devices)
            {
                if(!CanSimulate(m))continue;
                Advance(m);
                if(m.Definition.Id==IndustryId.Lamp)
                {
                    lampLightStates.TryGetValue(m.Position,out bool wasLit);
                    if(wasLit!=m.Running){lampLightStates[m.Position]=m.Running;LightChanged?.Invoke(m.Position);}
                }
            }
            Revision++;LastStepMs=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;
        }
        // A suspended component cannot transfer while reconstruction spans ticks;
        // its last completed request remains useful to UI and diagnostics.
        static void ResetTransferredPower(MachineState m)
        {m.ReceivedWatts=m.SupplyWatts=m.DeliveredWatts=m.BatteryWatts=m.BatteryInputWatts=m.BatteryOutputWatts=0;}
        static void ResetNetworkState(MachineState m)
        {ResetTransferredPower(m);m.RequestedWatts=0;m.Signal=false;m.SignalAttached=false;m.FluidConflict=false;}
        static int Compare(BlockPos a,BlockPos b){int c=a.X.CompareTo(b.X);if(c!=0)return c;c=a.Y.CompareTo(b.Y);return c!=0?c:a.Z.CompareTo(b.Z);}
        public static BlockPos Neighbor(MachineState m,int face)=>IndustryDefinition.Neighbor(m.Position,face,m.Rotation);
        void Boiler(MachineState m)
        {
            if(m.WaterMl<5){m.Status=MachineStatus.NoWater;return;}
            if(m.BurnTicks==0)
            {
                var fuel=m.Items.Slots[0];if(fuel.Empty){m.Status=MachineStatus.NoFuel;return;}
                if(!m.Accepts(0,fuel.Id)){m.Status=MachineStatus.NoFuel;return;}
                m.Items.Take(0,1);m.BurnTicks=m.FuelTicks(fuel.Id);
            }
            m.BurnTicks--;m.WaterMl-=5;m.Status=MachineStatus.Running;
        }
        void Prepare(MachineState m)
        {
            if(m.IsComposter){PrepareCompost(m);return;}
            if(m.IsCooker){PrepareCooker(m);return;}
            if(m.Definition.Watts==0&&m.Definition.Id!=IndustryId.Pump&&m.Definition.Id!=IndustryId.RangedPump)return;
            if(!m.Enabled){m.Status=MachineStatus.DisabledBySignal;return;}
            byte id=m.Definition.Id;
            if(id==IndustryId.Crusher||id==IndustryId.ElectricFurnace)
            {
                var input=m.Items.Slots[0];var output=m.ProcessingOutput(input.Id);
                if(input.Id!=m.WorkInput){m.Work=0;m.WorkInput=input.Id;}
                if(output.Empty||input.Empty||input.Count<(m.FurnaceRecipe?.Input.Count??1)){m.Status=MachineStatus.NoInput;return;}
                if(m.Items.Capacity(output.Id,2,3)<output.Count){m.Status=MachineStatus.OutputFull;return;}
            }
            if(id==IndustryId.RangedPump){PrepareRangedPump(m);return;}
            if(id==IndustryId.Pump)
            {
                if(m.WaterMl>m.Definition.WaterCapacity-10000){m.Status=MachineStatus.OutputFull;return;}
                var p=Neighbor(m,3);if(!world.Ready(p)){m.Status=MachineStatus.Dormant;return;}
                if(world.Get(p)!=Fluids.Water.Source){m.Work=0;m.Status=MachineStatus.NoWater;return;}
                m.Status=MachineStatus.Ready;return;
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
            if(m.IsComposter){AdvanceCompost(m);return;}
            if(m.IsCooker){AdvanceCooker(m);return;}
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
            if(id==IndustryId.Pump||id==IndustryId.RangedPump)
            {
                // Water supply must be able to start and recover the boiler without electricity.
                if(m.Status!=MachineStatus.Ready)return;
                m.Status=MachineStatus.Running;m.Work=Math.Min(m.Work+1,m.ProcessingTicks);
            }
            else
            {
                if(m.RequestedWatts==0||m.ReceivedWatts==0)return;
                m.Status=m.ReceivedWatts<m.RequestedWatts?MachineStatus.Underpowered:MachineStatus.Running;
                if(id!=IndustryId.Lamp)m.Work+=m.ReceivedWatts/(double)m.Definition.Watts;
            }
            if(id==IndustryId.Lamp)return;
            int duration=m.ProcessingTicks;
            if(m.Work+1e-9<duration)return;
            if(id==IndustryId.Crusher||id==IndustryId.ElectricFurnace)
            {
                var input=m.Items.Slots[0];var output=m.ProcessingOutput(input.Id);int count=m.FurnaceRecipe?.Input.Count??1;
                if(input.Id!=m.WorkInput){m.Work=0;m.WorkInput=input.Id;return;}
                if(output.Empty||input.Count<count||m.Items.Capacity(output.Id,2,3)<output.Count)return;
                m.Items.Take(0,count);m.Items.Add(output.Id,output.Count,2,3);
            }
            if(id==IndustryId.Pump)
            {var p=Neighbor(m,3);if(world.Get(p)!=Fluids.Water.Source||!world.Remove(p,Fluids.Water.Source))return;m.WaterMl+=10000;}
            if(id==IndustryId.RangedPump&&!CollectRangedSource(m))return;
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
            if(g.Active)
            foreach(var p in g.Devices)
            {
                p.Machine.FluidConflict=false;
                if(!PipeConnections.FluidEnabled(p.Machine))continue;
                var storage=PipeConnections.Storage(p.Machine);if(storage==null||available.ContainsKey(storage))continue;
                available[storage]=storage.Amount;capacity[storage]=storage.Capacity-storage.Amount;fluidTypes[storage]=storage.Fluid;
            }
            foreach(var g in FluidNetwork.Groups)
            {
                if(!g.Active)continue;
                FluidDefinition networkFluid=null;bool conflict=false;
                foreach(var endpoint in g.Devices)
                {
                    if(!PipeConnections.FluidEnabled(endpoint.Machine))continue;
                    var storage=PipeConnections.Storage(endpoint.Machine);if(storage?.Fluid==null)continue;
                    if(networkFluid!=null&&networkFluid.StableId!=storage.Fluid.StableId)conflict=true;
                    networkFluid=storage.Fluid;
                }
                if(g.HadFluidConflict!=conflict)
                {
                    // Route vertices belong to one graph. Shared device faces
                    // accumulate conflict separately across all their graphs.
                    foreach(var endpoint in g.Ports)if(endpoint.Port.Role==PortRole.Route)endpoint.Machine.FluidConflict=conflict;
                    g.HadFluidConflict=conflict;
                }
                if(conflict){foreach(var endpoint in g.Devices)endpoint.Machine.FluidConflict=true;continue;}
                if(networkFluid==null)continue;
                fluidSources.Clear();fluidSinks.Clear();fluidSourceLimits.Clear();fluidSinkIdentities.Clear();long room=0;
                foreach(var endpoint in g.Devices)
                {
                    if(!PipeConnections.FluidEnabled(endpoint.Machine))continue;
                    var storage=PipeConnections.Storage(endpoint.Machine);
                    if(storage==null||!available.ContainsKey(storage))continue;
                    if(endpoint.Port.Role==PortRole.Output)
                    {
                        fluidSourceLimits.TryGetValue(storage,out long limit);
                        fluidSourceLimits[storage]=limit+100;
                    }
                    if(endpoint.Port.Role==PortRole.Input&&PipeConnections.Accepts(endpoint.Machine,networkFluid)&&
                        (fluidTypes[storage]==null||fluidTypes[storage].StableId==networkFluid.StableId))
                    {
                        fluidSinks.Add(storage,capacity[storage]);
                        if(fluidSinkIdentities.Add(storage))room+=capacity[storage];
                    }
                }
                if(room==0)continue;
                long supply=0;
                foreach(var entry in fluidSourceLimits)
                {
                    long limit=Math.Min(entry.Value,available[entry.Key]);
                    fluidSources.Add(entry.Key,limit);supply+=limit;
                }
                // Synchronous callbacks reuse context; no delegate/closure or
                // temporary identity set is allocated for an ordinary phase.
                reservedFluidType=networkFluid;
                fluidSources.Distribute(Math.Min(supply,room),Tick,reserveFluidSource);
            }
            // Synchronous authority turn: edits cannot interleave reservation and commit.
            foreach(var t in transfers)if(!t.from.Withdraw(t.amount))throw new InvalidOperationException("Stale fluid withdrawal");
            foreach(var t in transfers)if(!t.to.Deposit(t.fluid,t.amount))throw new InvalidOperationException("Stale fluid deposit");
            reservedFluidSource=null;reservedFluidType=null;
        }
        long ReserveFluidSource(FluidStorage source,long offer)
        {reservedFluidSource=source;return fluidSinks.Distribute(offer,Tick,reserveFluidSink,otherFluidSink);}
        bool OtherFluidSink(FluidStorage sink)=>sink!=reservedFluidSource;
        long ReserveFluidSink(FluidStorage sink,long amount)
        {
            fluidTransfers.Add((reservedFluidSource,sink,reservedFluidType,amount));
            fluidAvailable[reservedFluidSource]-=amount;fluidCapacity[sink]-=amount;fluidTypes[sink]=reservedFluidType;return amount;
        }
    }
}
