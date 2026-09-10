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
    public sealed class IndustrySimulation
    {
        readonly IIndustryWorld world;readonly Func<byte,int> limit;
        readonly Dictionary<BlockPos,MachineState> machines=new Dictionary<BlockPos,MachineState>();
        readonly List<MachineState> eligible=new List<MachineState>();
        IEnumerator<int> rebuild;bool dirty=true,signalsDirty=true;
        readonly List<MachineState> devices=new List<MachineState>();
        readonly Dictionary<MachineState,int> fluidAvailable=new Dictionary<MachineState,int>(),fluidCapacity=new Dictionary<MachineState,int>();
        readonly List<(MachineState from,MachineState to,int amount)> fluidTransfers=new List<(MachineState,MachineState,int)>();
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
        public IndustrySimulation(IIndustryWorld world,Func<byte,int> limit){this.world=world;this.limit=limit;}
        public MachineState At(BlockPos p)=>machines.TryGetValue(p,out var m)?m:null;
        public void Invalidate(){dirty=true;signalsDirty=true;Revision++;}
        public MachineState Add(BlockPos p,byte id)
        {if(machines.ContainsKey(p))throw new InvalidOperationException("Occupied machine anchor");var m=new MachineState(p,id,limit);machines.Add(p,m);Invalidate();return m;}
        public MachineState Remove(BlockPos p)
        {var m=At(p);if(m!=null){machines.Remove(p);Invalidate();}return m;}
        public void Rotate(MachineState m){m.Rotation=(m.Rotation+1)%4;Invalidate();}
        public void Activate(MachineState m)
        {if(m.Definition.Id==IndustryId.Lever)m.Source=!m.Source;else if(m.Definition.Id==IndustryId.Button){m.Source=true;m.PulseTicks=20;}signalsDirty=true;Revision++;}
        public void Step()
        {
            long start=Stopwatch.GetTimestamp();Tick++;
            if(dirty)
            {
                rebuild?.Dispose();dirty=false;eligible.Clear();devices.Clear();
                foreach(var m in machines.Values)
                {m.Eligible=world.Ready(m.Position);m.ReceivedWatts=m.RequestedWatts=m.SupplyWatts=0;m.Signal=false;m.SignalAttached=false;if(m.Eligible)eligible.Add(m);else m.Status=MachineStatus.Dormant;}
                eligible.Sort((a,b)=>Compare(a.Position,b.Position));foreach(var m in eligible)if(!IndustryId.Route(m.Definition.Id))devices.Add(m);rebuild=Rebuild().GetEnumerator();TopologyRebuilds++;
            }
            if(rebuild!=null)
            {
                bool done=false;for(int budget=0;budget<2048;budget++)if(!rebuild.MoveNext()){done=true;break;}
                if(!done){LastStepMs=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;return;}
                rebuild.Dispose();rebuild=null;Revision++;
            }
            foreach(var m in devices)
            {
                m.RequestedWatts=m.ReceivedWatts=m.SupplyWatts=0;
                if(m.Definition.Id==IndustryId.Relay&&m.Source!=m.NextSource){m.Source=m.NextSource;signalsDirty=true;}
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
                Prepare(m);
            }
            Power.Allocate(Tick);
            // Transfers precede processing: new products cannot be forwarded in their producing tick.
            TransferItems();TransferFluids();
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
            if(id==IndustryId.Door){m.Status=m.Signal&&!world.PlayerInside(m.Position)?MachineStatus.Running:world.PlayerInside(m.Position)?m.Status:MachineStatus.Ready;return;}
            if(id==IndustryId.Indicator||id==IndustryId.Relay||id==IndustryId.Lever||id==IndustryId.Button||id==IndustryId.Sensor)
            {m.Status=(id==IndustryId.Lever||id==IndustryId.Button||id==IndustryId.Sensor?m.Source:m.Signal)?MachineStatus.Running:MachineStatus.Ready;return;}
            if(m.RequestedWatts==0||m.ReceivedWatts==0)return;
            m.Status=m.ReceivedWatts<m.RequestedWatts?MachineStatus.Underpowered:MachineStatus.Running;
            if(id==IndustryId.Lamp)return;
            m.Work+=m.ReceivedWatts/(double)m.Definition.Watts;
            int duration=id==IndustryId.Crusher?100:id==IndustryId.Pump?40:120;
            if(m.Work+1e-9<duration)return;
            if(id==IndustryId.Crusher)
            {byte output=MachineState.Crushed(m.Items.Slots[0].Id);if(output==0||m.Items.Capacity(output,2,3)<2)return;m.Items.Take(0,1);m.Items.Add(output,2,2,3);}
            if(id==IndustryId.Pump)
            {var p=Neighbor(m,3);if(world.Get(p)!=Fluids.Water.Source||!world.Remove(p,Fluids.Water.Source))return;m.WaterMl+=10000;}
            if(id==IndustryId.Drill)
            {var p=m.Position.Offset(0,-m.DrillDepth,0);byte b=world.Get(p);if(b!=m.WorkInput||!world.Remove(p,b))return;m.Items.Add(world.Drop(b),1,2,3);m.DrillDepth++;}
            m.Work-=duration;
        }
        void TransferItems()
        {
            if(Tick%5!=0)return;
            foreach(var g in ItemNetwork.Groups)
            {
                int start=(int)(Tick/5%Math.Max(1,g.Ports.Count));
                for(int n=0;n<g.Ports.Count;n++)
                {
                    var p=g.Ports[(start+n)%g.Ports.Count];if(p.Port.Role!=PortRole.Output)continue;
                    var m=p.Machine;ItemContainer source=m.Items;int slot=2;
                    if(m.Definition.Id==IndustryId.Extractor)
                    {if(!m.Enabled){m.Status=MachineStatus.DisabledBySignal;continue;}var pos=Neighbor(m,1);source=world.Ready(pos)?world.Storage(pos):null;slot=source?.FindSlot(s=>!s.Empty)??-1;}
                    if(source==null||slot<0||source.Slots[slot].Empty){if(m.Definition.Id==IndustryId.Extractor)m.Status=MachineStatus.NoInput;continue;}
                    var stack=source.Slots[slot];bool sent=false;
                    for(int k=0;k<g.Ports.Count;k++)
                    {
                        var dest=g.Ports[(start+k)%g.Ports.Count];if(dest.Port.Role!=PortRole.Input||dest.Machine==m||!dest.Machine.Accepts(0,stack.Id)||dest.Machine.Items.Capacity(stack.Id,0,1)==0)continue;
                        dest.Machine.Items.Add(stack.Id,1,0,1);source.Take(slot,1);sent=true;break;
                    }
                    // Any pipe face can deliver to an adjacent chest; chests never bridge networks.
                    if(!sent)foreach(var route in g.Ports)
                    {
                        if(route.Port.Role!=PortRole.Route)continue;
                        for(int face=0;face<6;face++)
                        {var pos=Neighbor(route.Machine,face);var dest=world.Ready(pos)?world.Storage(pos):null;if(dest==null||ReferenceEquals(dest,source)||dest.Capacity(stack.Id)==0)continue;dest.Add(stack.Id,1);source.Take(slot,1);sent=true;break;}
                        if(sent)break;
                    }
                    if(m.Definition.Id==IndustryId.Extractor)m.Status=sent?MachineStatus.Running:MachineStatus.OutputFull;
                }
            }
        }
        void TransferFluids()
        {
            // Reserve all transfers from the pre-transfer amounts, preventing same-step multi-hop forwarding.
            var available=fluidAvailable;var capacity=fluidCapacity;var transfers=fluidTransfers;available.Clear();capacity.Clear();transfers.Clear();
            foreach(var g in FluidNetwork.Groups)
            foreach(var p in g.Ports)if(p.Port.Role!=PortRole.Route){available[p.Machine]=p.Machine.WaterMl;capacity[p.Machine]=p.Machine.Definition.WaterCapacity-p.Machine.WaterMl;}
            foreach(var g in FluidNetwork.Groups)
            {
                int count=g.Ports.Count,start=(int)(Tick%Math.Max(1,count));
                for(int n=0;n<count;n++)
                {
                    var source=g.Ports[(start+n)%count];if(source.Port.Role!=PortRole.Output||available[source.Machine]==0)continue;
                    int budget=Math.Min(100,available[source.Machine]);
                    for(int k=0;k<count&&budget>0;k++)
                    {var dest=g.Ports[(start+k)%count];if(dest.Port.Role!=PortRole.Input||dest.Machine==source.Machine)continue;int take=Math.Min(budget,capacity[dest.Machine]);if(take<=0)continue;transfers.Add((source.Machine,dest.Machine,take));available[source.Machine]-=take;capacity[dest.Machine]-=take;budget-=take;}
                }
            }
            foreach(var t in transfers){t.from.WaterMl-=t.amount;t.to.WaterMl+=t.amount;}
        }
    }
}
