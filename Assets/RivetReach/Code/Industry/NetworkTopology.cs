using System;
using System.Collections.Generic;

namespace RivetReach
{
    // Shared topology machinery; family-specific services own signal and energy semantics.
    // Ports are separate vertices: a relay input and output never become an implicit short.
    public sealed class NetworkTopology
    {
        public sealed class Endpoint
        {
            public MachineState Machine;public MachinePort Port;public int Faces,Group;
        }
        public sealed class Group
        {
            public readonly List<Endpoint> Ports=new List<Endpoint>();
            public int Id;public bool Signal;public int Supply,Demand;
        }
        public List<Group> Groups {get;private set;}=new List<Group>();
        public Dictionary<BlockPos,int> Connections {get;private set;}=new Dictionary<BlockPos,int>();
        HashSet<MachineState> registered=new HashSet<MachineState>();
        public bool Connected(MachineState machine)=>machine!=null&&registered.Contains(machine)&&Connections.TryGetValue(machine.Position,out int faces)&&faces!=0;
        public readonly NetworkKind Kind;
        public Func<BlockPos,int> ExternalEndpointFaces;
        public Func<MachineState,IEnumerable<MachinePort>> ResolvePorts;
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
            for(int i=0;i<nodes.Count;i++)
            {
                if(nodes[i].Group>=0)continue;
                var group=new Group{Id=groups.Count};groups.Add(group);nodes[i].Group=group.Id;queue.Enqueue(i);
                while(queue.Count>0)
                {
                    var a=nodes[queue.Dequeue()];group.Ports.Add(a);
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
            if(Kind==NetworkKind.Power)
            {
                // Several faces on the same cable grid still represent one device budget.
                foreach(var group in groups)
                {
                    var seen=new HashSet<MachineState>();
                    group.Ports.RemoveAll(p=>!seen.Add(p.Machine));
                }
            }
            Groups=groups;Connections=connections;registered=members;
        }
    }
    public sealed class SignalNetworkService
    {
        public readonly NetworkTopology Topology=new NetworkTopology(NetworkKind.Signal);
        public void Evaluate()
        {
            foreach(var group in Topology.Groups)
            {
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
        // Shared device budgets prevent multiple faces/grids from duplicating generation,
        // demand or storage. Only cable vertices connect grids, never a device interior.
        public void Allocate(long tick)
        {
            generation.Clear();
            foreach(var group in Topology.Groups)
            {
                group.Supply=group.Demand=0;
                foreach(var p in group.Ports)
                {
                    var m=p.Machine;
                    if(p.Port.Role==PortRole.Output)generation[m]=m.SupplyWatts;
                    if(p.Port.Role==PortRole.Input){m.ReceivedWatts=0;group.Demand+=m.RequestedWatts;}
                    if(p.Port.Role==PortRole.Storage)m.BatteryWatts=m.BatteryInputWatts=m.BatteryOutputWatts=0;
                }
            }
            // First all ordinary loads consume generation, before any storage charge.
            foreach(var group in Topology.Groups)
            {
                int used=Serve(group,AvailableGeneration(group),tick);
                ConsumeGeneration(group,used);group.Supply+=used;
            }
            // Charge before discharge so even an empty battery can relay this tick's
            // generation to a separate load grid, independent of traversal order.
            ChargeSurplus(tick);
            foreach(var group in Topology.Groups)
            {
                int available=0;
                foreach(var p in group.Ports)if(p.Port.Role==PortRole.Storage)available=(int)Math.Min(int.MaxValue,(long)available+BatteryPower.Available(p.Machine,false));
                int used=Serve(group,available,tick);group.Supply+=used;
                TransferStorage(group,used,false,tick);
            }
            // A full battery may have freed room while feeding another grid.
            ChargeSurplus(tick);
        }
        int AvailableGeneration(NetworkTopology.Group group)
        {
            int watts=0;
            foreach(var p in group.Ports)if(p.Port.Role==PortRole.Output)watts+=generation[p.Machine];
            return watts;
        }
        void ConsumeGeneration(NetworkTopology.Group group,int watts)
        {
            foreach(var p in group.Ports)if(p.Port.Role==PortRole.Output&&watts>0)
            {int take=Math.Min(watts,generation[p.Machine]);generation[p.Machine]-=take;watts-=take;}
        }
        int TransferStorage(NetworkTopology.Group group,int watts,bool charge,long tick)
        {
            storageShares.Clear();
            foreach(var p in group.Ports)if(p.Port.Role==PortRole.Storage)storageShares.Add(p.Machine,BatteryPower.Available(p.Machine,charge));
            return (int)storageShares.Distribute(watts,tick,(m,take)=>{BatteryPower.Transfer(m,(int)take,charge);return take;});
        }
        void ChargeSurplus(long tick)
        {
            foreach(var group in Topology.Groups)
            {
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
                foreach(var p in group.Ports)if(p.Port.Role==PortRole.Input&&p.Machine.Priority==priority)requested+=Need(p.Machine);
                if(requested==0)continue;
                int available=Math.Min(supply,requested),used=0;
                foreach(var p in group.Ports)if(p.Port.Role==PortRole.Input&&p.Machine.Priority==priority)
                {int amount=(int)((long)Need(p.Machine)*available/requested);p.Machine.ReceivedWatts+=amount;used+=amount;}
                int residual=available-used,count=group.Ports.Count,start=(int)(tick%count);
                for(int n=0;n<count&&residual>0;n++)
                {
                    var p=group.Ports[(start+n)%count];
                    if(p.Port.Role==PortRole.Input&&p.Machine.Priority==priority&&Need(p.Machine)>0)
                    {p.Machine.ReceivedWatts++;residual--;}
                }
                supply-=available;total+=available;
            }
            return total;
        }
    }
}
