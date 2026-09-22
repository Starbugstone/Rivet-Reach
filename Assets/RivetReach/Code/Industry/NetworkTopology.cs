using System;
using System.Collections.Generic;

namespace RivetReach
{
    public sealed class NetworkActivity {public bool Active=true;}
    // Shared topology machinery; family-specific services own signal and energy semantics.
    // Ports are separate vertices: a relay input and output never become an implicit short.
    public sealed class NetworkTopology
    {
        public sealed class Endpoint
        {
            public MachineState Machine;public MachinePort Port;public int Faces,Group;
            internal int Order;
        }
        public sealed class Group
        {
            public readonly List<Endpoint> Ports=new List<Endpoint>();
            internal readonly List<Endpoint> Devices=new List<Endpoint>();
            internal bool HadFluidConflict;
            public int Id;public bool Signal;public int Supply,Demand;
            public NetworkActivity Activity;
            public bool Active=>Activity==null||Activity.Active;
        }
        internal sealed class Snapshot
        {
            public readonly List<Group> Groups=new List<Group>();
            public readonly Dictionary<BlockPos,int> Connections=new Dictionary<BlockPos,int>();
            public readonly HashSet<MachineState> Registered=new HashSet<MachineState>();
        }
        public List<Group> Groups {get;private set;}=new List<Group>();
        public Dictionary<BlockPos,int> Connections {get;private set;}=new Dictionary<BlockPos,int>();
        HashSet<MachineState> registered=new HashSet<MachineState>();
        public long Revision {get;private set;}
        public bool Connected(MachineState machine)=>machine!=null&&registered.Contains(machine)&&Connections.TryGetValue(machine.Position,out int faces)&&faces!=0;
        public readonly NetworkKind Kind;
        public Func<BlockPos,int> ExternalEndpointFaces;
        public Func<MachineState,IEnumerable<MachinePort>> ResolvePorts;
        public Func<MachineState,MachineState> RemotePartner;
        public NetworkTopology(NetworkKind kind){Kind=kind;}
        public IEnumerable<int> Rebuild(IReadOnlyList<MachineState> machines)
        {
            // Build privately: yielding or cancelling must not erase the registered
            // network, its connection geometry or its last allocation snapshot.
            var groups=new List<Group>();var connections=new Dictionary<BlockPos,int>();
            var members=new HashSet<MachineState>();
            var nodes=new List<Endpoint>();var byPosition=new Dictionary<BlockPos,List<int>>();
            foreach(var m in machines)
            {
                foreach(var p in ResolvePorts!=null?ResolvePorts(m):PipeConnections.Ports(m))
                {
                    if(p.Kind!=Kind)continue;members.Add(m);int faces=PipeConnections.WorldFaces(p,m.Rotation);
                    if(!byPosition.TryGetValue(m.Position,out var list))byPosition.Add(m.Position,list=new List<int>());
                    // Electrical devices terminate each face; only conductors join cable runs.
                    if(Kind==NetworkKind.Power&&p.Role!=PortRole.Route)
                    {
                        for(int face=0;face<6;face++)if((faces&(1<<face))!=0)
                        {list.Add(nodes.Count);nodes.Add(new Endpoint{Machine=m,Port=p,Faces=1<<face,Group=-1});}
                    }
                    else {list.Add(nodes.Count);nodes.Add(new Endpoint{Machine=m,Port=p,Faces=faces,Group=-1});}
                }
                yield return 0;
            }
            var queue=new Queue<int>();
            var groupMembers=Kind==NetworkKind.Power?new HashSet<MachineState>():null;
            int visitedSinceYield=0;
            for(int i=0;i<nodes.Count;i++)
            {
                if(nodes[i].Group>=0)
                {
                    // Finishing a large connected component leaves a long run
                    // of visited vertices. Account for that scan in the budget.
                    if(++visitedSinceYield==64){visitedSinceYield=0;yield return 0;}
                    continue;
                }
                visitedSinceYield=0;groupMembers?.Clear();
                var group=new Group{Id=groups.Count};groups.Add(group);nodes[i].Group=group.Id;queue.Enqueue(i);
                while(queue.Count>0)
                {
                    var a=nodes[queue.Dequeue()];
                    // Several device faces on one power grid share one budget.
                    // Keep the first BFS occurrence while still visiting every
                    // face for connectivity, rather than an unbounded final pass.
                    if(groupMembers==null||groupMembers.Add(a.Machine))
                    {a.Order=group.Ports.Count;group.Ports.Add(a);if(a.Port.Role!=PortRole.Route)group.Devices.Add(a);}
                    var remote=RemotePartner?.Invoke(a.Machine);
                    if(remote!=null&&byPosition.TryGetValue(remote.Position,out var remoteNodes))
                        foreach(int j in remoteNodes)if(nodes[j].Group<0&&nodes[j].Port.Role==PortRole.Route)
                        {nodes[j].Group=group.Id;queue.Enqueue(j);}
                    for(int face=0;face<6;face++)
                    {
                        if((a.Faces&(1<<face))==0)continue;
                        var next=IndustryDefinition.Neighbor(a.Machine.Position,face);
                        if(!byPosition.TryGetValue(next,out var candidates))
                        {
                            // External inventories terminate a route; they never become a hidden bridge.
                            if(PipeConnections.Matches(a.Faces,ExternalEndpointFaces?.Invoke(next)??0,face))
                            {connections.TryGetValue(a.Machine.Position,out int externalMask);connections[a.Machine.Position]=externalMask|(1<<face);}
                            continue;
                        }
                        foreach(int j in candidates)
                        {
                            var b=nodes[j];if(!PipeConnections.Matches(a.Faces,b.Faces,face))continue;
                            connections.TryGetValue(a.Machine.Position,out int mask);connections[a.Machine.Position]=mask|(1<<face);
                            if(b.Group>=0)continue;b.Group=group.Id;queue.Enqueue(j);
                        }
                    }
                    yield return 0;
                }
            }
            Groups=groups;Connections=connections;registered=members;Revision++;
        }
        internal IEnumerable<int> Combine(NetworkTopology replacement,HashSet<NetworkActivity> removed,HashSet<BlockPos> affected,
            Func<MachineState,NetworkActivity> activity,Snapshot snapshot)
        {
            int nextId=0;
            foreach(var group in Groups)
            {
                nextId=Math.Max(nextId,group.Id+1);
                if(group.Activity==null||!removed.Contains(group.Activity))
                {snapshot.Groups.Add(group);foreach(var port in group.Ports){snapshot.Registered.Add(port.Machine);yield return 0;}}
                yield return 0;
            }
            foreach(var pair in Connections){if(!affected.Contains(pair.Key))snapshot.Connections.Add(pair.Key,pair.Value);yield return 0;}
            foreach(var group in replacement.Groups)
            {
                group.Id=nextId++;group.Activity=activity(group.Ports[0].Machine);snapshot.Groups.Add(group);
                foreach(var port in group.Ports){snapshot.Registered.Add(port.Machine);yield return 0;}
            }
            foreach(var pair in replacement.Connections){snapshot.Connections[pair.Key]=pair.Value;yield return 0;}
        }
        internal void Publish(Snapshot snapshot)
        {Groups=snapshot.Groups;Connections=snapshot.Connections;registered=snapshot.Registered;Revision++;}
    }
    public sealed class SignalNetworkService
    {
        public readonly NetworkTopology Topology=new NetworkTopology(NetworkKind.Signal);
        public void Evaluate()
        {
            foreach(var group in Topology.Groups)
            {
                if(!group.Active)continue;
                bool value=false,attached=false;
                foreach(var p in group.Ports)
                {if(p.Port.Role!=PortRole.Input)attached=true;if(p.Port.Role==PortRole.Output&&p.Machine.Source)value=true;}
                group.Signal=value;
                foreach(var p in group.Ports)
                {
                    if(p.Port.Role==PortRole.Output)continue;
                    p.Machine.Signal=value;
                    if(p.Port.Role==PortRole.Input)p.Machine.SignalAttached=attached;
                }
            }
        }
    }
    public sealed class PowerNetworkService
    {
        public readonly NetworkTopology Topology=new NetworkTopology(NetworkKind.Power);
        readonly Dictionary<MachineState,int> generation=new Dictionary<MachineState,int>();
        readonly FairAllocation<MachineState> storageShares=new FairAllocation<MachineState>();
        readonly Func<MachineState,long,long> storageTransfer;
        bool storageCharge;
        public PowerNetworkService(){storageTransfer=TransferReservedStorage;}
        long TransferReservedStorage(MachineState machine,long amount)
        {BatteryPower.Transfer(machine,(int)amount,storageCharge);return amount;}
        // Shared device budgets prevent multiple faces/grids from duplicating generation,
        // demand or storage. Only cable vertices connect grids, never a device interior.
        public void Allocate(long tick)
        {
            using(RuntimeCosts.PowerPrepare.Auto())
            {
                generation.Clear();
                foreach(var group in Topology.Groups)
                {
                    if(!group.Active)continue;
                    group.Supply=group.Demand=0;
                    foreach(var p in group.Devices)
                    {
                        var m=p.Machine;
                        m.DeliveredWatts=0;
                        if(p.Port.Role==PortRole.Output)generation[m]=m.SupplyWatts;
                        if(p.Port.Role==PortRole.Input){m.ReceivedWatts=0;group.Demand+=m.RequestedWatts;}
                        if(p.Port.Role==PortRole.Storage)m.BatteryWatts=m.BatteryInputWatts=m.BatteryOutputWatts=0;
                    }
                }
            }
            // First all ordinary loads consume generation, before any storage charge.
            using(RuntimeCosts.PowerLoads.Auto())
            {
                foreach(var group in Topology.Groups)
                {
                    if(!group.Active)continue;
                    int used=Serve(group,AvailableGeneration(group),tick);
                    ConsumeGeneration(group,used);group.Supply+=used;
                }
            }
            // Charge before discharge so even an empty battery can relay this tick's
            // generation to a separate load grid, independent of traversal order.
            using(RuntimeCosts.PowerCharge.Auto())ChargeSurplus(tick);
            using(RuntimeCosts.PowerDischarge.Auto())
            {
                foreach(var group in Topology.Groups)
                {
                    if(!group.Active)continue;
                    int available=0;
                    foreach(var p in group.Devices)if(p.Port.Role==PortRole.Storage)available=(int)Math.Min(int.MaxValue,(long)available+BatteryPower.Available(p.Machine,false));
                    int used=Serve(group,available,tick);group.Supply+=used;
                    TransferStorage(group,used,false,tick);
                }
            }
            // A full battery may have freed room while feeding another grid.
            using(RuntimeCosts.PowerCharge.Auto())ChargeSurplus(tick);
        }
        int AvailableGeneration(NetworkTopology.Group group)
        {
            int watts=0;
            foreach(var p in group.Devices)if(p.Port.Role==PortRole.Output)watts+=generation[p.Machine];
            return watts;
        }
        void ConsumeGeneration(NetworkTopology.Group group,int watts)
        {
            foreach(var p in group.Devices)if(p.Port.Role==PortRole.Output&&watts>0)
            {int take=Math.Min(watts,generation[p.Machine]);generation[p.Machine]-=take;p.Machine.DeliveredWatts+=take;watts-=take;}
        }
        int TransferStorage(NetworkTopology.Group group,int watts,bool charge,long tick)
        {
            if(watts<=0)return 0;
            storageShares.Clear();
            foreach(var p in group.Devices)if(p.Port.Role==PortRole.Storage)storageShares.Add(p.Machine,BatteryPower.Available(p.Machine,charge));
            storageCharge=charge;
            return (int)storageShares.Distribute(watts,tick,storageTransfer);
        }
        void ChargeSurplus(long tick)
        {
            foreach(var group in Topology.Groups)
            {
                if(!group.Active)continue;
                int used=TransferStorage(group,AvailableGeneration(group),true,tick);
                ConsumeGeneration(group,used);group.Supply+=used;
            }
        }
        static int Need(MachineState m)=>Math.Max(0,m.RequestedWatts-m.ReceivedWatts);
        // Whole watts, priorities and proportional shortfall within each cable grid.
        static int Serve(NetworkTopology.Group group,int supply,long tick)
        {
            int total=0;
            for(int priority=0;priority<3&&supply>0;priority++)
            {
                int requested=0;
                foreach(var p in group.Devices)if(p.Port.Role==PortRole.Input&&p.Machine.Priority==priority)requested+=Need(p.Machine);
                if(requested==0)continue;
                int available=Math.Min(supply,requested),used=0;
                foreach(var p in group.Devices)if(p.Port.Role==PortRole.Input&&p.Machine.Priority==priority)
                {int amount=(int)((long)Need(p.Machine)*available/requested);p.Machine.ReceivedWatts+=amount;used+=amount;}
                int residual=available-used,start=(int)(tick%group.Ports.Count);
                // Preserve the original all-port cursor, including route positions,
                // while visiting only devices on either side of its wrap point.
                for(int pass=0;pass<2&&residual>0;pass++)
                foreach(var p in group.Devices)
                {
                    if(residual==0)break;
                    if(pass==0?p.Order<start:p.Order>=start)continue;
                    if(p.Port.Role==PortRole.Input&&p.Machine.Priority==priority&&Need(p.Machine)>0)
                    {p.Machine.ReceivedWatts++;residual--;}
                }
                supply-=available;total+=available;
            }
            return total;
        }
    }
}
