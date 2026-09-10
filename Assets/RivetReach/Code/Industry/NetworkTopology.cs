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
        public readonly List<Group> Groups=new List<Group>();
        public readonly Dictionary<BlockPos,int> Connections=new Dictionary<BlockPos,int>();
        public readonly NetworkKind Kind;
        public Func<BlockPos,int> ExternalEndpointFaces;
        public NetworkTopology(NetworkKind kind){Kind=kind;}
        public IEnumerable<int> Rebuild(IReadOnlyList<MachineState> machines)
        {
            Groups.Clear();Connections.Clear();
            var nodes=new List<Endpoint>();var byPosition=new Dictionary<BlockPos,List<int>>();
            foreach(var m in machines)
            {
                foreach(var p in PipeConnections.Ports(m))
                {
                    if(p.Kind!=Kind)continue;int faces=PipeConnections.WorldFaces(p,m.Rotation);
                    if(!byPosition.TryGetValue(m.Position,out var list))byPosition.Add(m.Position,list=new List<int>());
                    list.Add(nodes.Count);nodes.Add(new Endpoint{Machine=m,Port=p,Faces=faces,Group=-1});
                }
                yield return 0;
            }
            var queue=new Queue<int>();
            for(int i=0;i<nodes.Count;i++)
            {
                if(nodes[i].Group>=0)continue;
                var group=new Group{Id=Groups.Count};Groups.Add(group);nodes[i].Group=group.Id;queue.Enqueue(i);
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
                            {Connections.TryGetValue(a.Machine.Position,out int externalMask);Connections[a.Machine.Position]=externalMask|(1<<face);}
                            continue;
                        }
                        foreach(int j in candidates)
                        {
                            var b=nodes[j];if(!PipeConnections.Matches(a.Faces,b.Faces,face))continue;
                            Connections.TryGetValue(a.Machine.Position,out int mask);Connections[a.Machine.Position]=mask|(1<<face);
                            if(b.Group>=0)continue;b.Group=group.Id;queue.Enqueue(j);
                        }
                    }
                    yield return 0;
                }
            }
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
        // Whole watts with deterministic rotating residuals; priority 0 is served before 1 and 2.
        public void Allocate(long tick)
        {
            foreach(var group in Topology.Groups)
            {
                int supply=0,demand=0;
                foreach(var p in group.Ports)
                {if(p.Port.Role==PortRole.Output)supply+=p.Machine.SupplyWatts;if(p.Port.Role==PortRole.Input){demand+=p.Machine.RequestedWatts;p.Machine.ReceivedWatts=0;}}
                group.Supply=supply;group.Demand=demand;
                for(int priority=0;priority<3&&supply>0;priority++)
                {
                    int requested=0;
                    foreach(var p in group.Ports)if(p.Port.Role==PortRole.Input&&p.Machine.Priority==priority)requested+=p.Machine.RequestedWatts;
                    if(requested==0)continue;int available=Math.Min(supply,requested),used=0;
                    foreach(var p in group.Ports)if(p.Port.Role==PortRole.Input&&p.Machine.Priority==priority)
                    {int amount=(int)((long)p.Machine.RequestedWatts*available/requested);p.Machine.ReceivedWatts=amount;used+=amount;}
                    int residual=available-used,count=group.Ports.Count,start=(int)(tick%count);
                    for(int n=0;n<count&&residual>0;n++)
                    {var p=group.Ports[(start+n)%count];if(p.Port.Role==PortRole.Input&&p.Machine.Priority==priority&&p.Machine.ReceivedWatts<p.Machine.RequestedWatts){p.Machine.ReceivedWatts++;residual--;}}
                    supply-=available;
                }
            }
        }
    }
}
