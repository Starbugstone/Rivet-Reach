using System.Collections.Generic;

namespace RivetReach
{
    public sealed partial class IndustrySimulation
    {
        IItemPipeInventory ItemEndpoint(BlockPos p)=>!world.Ready(p)?null:
            world is IIndustryItemEndpoints endpoints?endpoints.ItemEndpoint(p):world.Storage(p);
        void ItemEndpointChanged(BlockPos p)
        {if(world is IIndustryItemEndpoints endpoints)endpoints.ItemEndpointChanged(p);}

        void InitializePipeEnds()
        {
            foreach(var pipe in eligible)
            {
                if(!PipeConnections.IsTransport(pipe.Definition.Id))continue;
                for(int face=0;face<6;face++)
                    if(((pipe.PipeDirections>>(face*2))&3)==0&&HasPipeEnd(pipe,face))
                        pipe.PipeDirections|=(PipeEndRole(pipe,face)==PortRole.Input?1:2)<<(face*2);
            }
        }
        public bool HasPipeEnd(MachineState pipe,int face)
        {
            if(pipe==null||face<0||face>=6||!PipeConnections.IsTransport(pipe.Definition.Id)||At(pipe.Position)!=pipe||!world.Ready(pipe.Position))return false;
            var position=IndustryDefinition.Neighbor(pipe.Position,face);
            return world.Ready(position)&&(PipeConnections.Supports(At(position),PipeConnections.TransportKind(pipe))||pipe.Definition.Id==IndustryId.ItemPipe&&ItemEndpoint(position)!=null);
        }
        public PortRole PipeEndRole(MachineState pipe,int face)=>PipeConnections.EndRole(pipe,face,At(IndustryDefinition.Neighbor(pipe.Position,face)));
        public bool TogglePipeEnd(MachineState pipe,int face)
        {
            if(!HasPipeEnd(pipe,face))return false;
            var role=PipeEndRole(pipe,face);
            int value=role==PortRole.Input?2:role==PortRole.Output?3:1;
            pipe.PipeDirections=(pipe.PipeDirections&~(3<<(face*2)))|(value<<(face*2));
            Invalidate();return true;
        }
        public int DisconnectedPipeFaces(MachineState pipe)
        {
            int mask=0;
            for(int face=0;face<6;face++)
                if(HasPipeEnd(pipe,face)&&PipeEndRole(pipe,face)==PortRole.Disabled)mask|=1<<face;
            return mask;
        }
        IEnumerable<MachinePort> TransportPorts(MachineState m,NetworkKind kind)
        {
            bool active=false;
            foreach(var p in PipeConnections.Ports(m))
            {
                if(p.Kind!=kind)continue;
                if(p.Role==PortRole.Route)
                {
                    // Only transport terminals are disabled. Pipe runs and fitted
                    // power/signal channels retain their independent connections.
                    int faces=PipeConnections.WorldFaces(p,m.Rotation)&~DisconnectedPipeFaces(m),local=0;
                    for(int face=0;face<6;face++)if((faces&(1<<face))!=0)local|=1<<IndustryDefinition.RotateFace(face,(4-m.Rotation)%4);
                    yield return new MachinePort(kind,p.Role,local);yield break;
                }
                active=true;
            }
            if(!active)yield break;
            // A machine terminates each pipe graph. Separate face vertices must never
            // let two unconnected pipes use a machine's inventory as a hidden bridge.
            for(int face=0;face<6;face++)
            {
                var neighbor=At(IndustryDefinition.Neighbor(m.Position,face));
                var role=neighbor!=null&&PipeConnections.IsTransport(neighbor.Definition.Id)&&PipeConnections.TransportKind(neighbor)==kind
                    ?PipeConnections.EndRole(neighbor,face^1,m):PipeConnections.DefaultRole(m,kind,face);
                // Recovery remains an explicit drain-only operation, even if an end
                // has been set to input. A breached tank can never receive new fluid.
                if(role==PortRole.Disabled||m.Definition.Id==IndustryId.TankController&&role!=PortRole.Output)continue;
                int localFace=IndustryDefinition.RotateFace(face,(4-m.Rotation)%4);
                yield return new MachinePort(kind,role,1<<localFace);
            }
        }

        readonly List<(NetworkTopology.Group graph,MachineState machine,IItemPipeInventory inventory,BlockPos position,int slot,ItemStack stack,object identity)> itemSources
            =new List<(NetworkTopology.Group,MachineState,IItemPipeInventory,BlockPos,int,ItemStack,object)>();
        readonly HashSet<object> itemSent=new HashSet<object>();
        sealed class ItemReceiver
        {
            public IItemPipeInventory Inventory;public BlockPos Position;public int Priority;
            public long[] RejectedAt;
            public readonly List<int> Faces=new List<int>();
        }
        sealed class ItemRouteBucket
        {
            public readonly List<ItemReceiver> Receivers=new List<ItemReceiver>();
            public readonly int[] Next=new int[256];
        }
        sealed class ItemRoutes
        {
            public readonly List<(MachineState machine,IItemPipeInventory inventory,BlockPos position)> Sources=new List<(MachineState,IItemPipeInventory,BlockPos)>();
            public readonly List<ItemReceiver> Receivers=new List<ItemReceiver>();
            public readonly Dictionary<int,ItemRouteBucket> Buckets=new Dictionary<int,ItemRouteBucket>();
        }
        readonly Dictionary<NetworkTopology.Group,ItemRoutes> itemRoutes=new Dictionary<NetworkTopology.Group,ItemRoutes>();
        readonly List<int> itemPriorities=new List<int>();readonly bool[] itemPriorityPresent=new bool[101];
        int itemRoutesTopology=-1;
        public int ItemRouteCacheBuilds {get;private set;}
        public long ItemReceiverProbes {get;private set;}
        public double LastItemRoutingMs {get;private set;}
        public long LastItemRoutingAllocatedBytes {get;private set;}

        void RefreshItemRoutes()
        {
            if(itemRoutesTopology!=TopologyRebuilds)
            {
                itemRoutes.Clear();itemRoutesTopology=TopologyRebuilds;ItemRouteCacheBuilds++;
                foreach(var graph in ItemNetwork.Groups)
                {
                    var routes=new ItemRoutes();itemRoutes.Add(graph,routes);var receivers=new Dictionary<IItemPipeInventory,ItemReceiver>();
                    var sources=new HashSet<IItemPipeInventory>();
                    void Source(MachineState machine,IItemPipeInventory inventory,BlockPos position)
                    {if(inventory!=null&&sources.Add(inventory))routes.Sources.Add((machine,inventory,position));}
                    void Receiver(IItemPipeInventory inventory,BlockPos position,int face)
                    {
                        if(inventory==null)return;
                        if(!receivers.TryGetValue(inventory,out var receiver)){receiver=new ItemReceiver{Inventory=inventory,Position=position,Priority=-1};receivers.Add(inventory,receiver);routes.Receivers.Add(receiver);}
                        if(!receiver.Faces.Contains(face))receiver.Faces.Add(face);
                    }
                    foreach(var endpoint in graph.Ports)
                    {
                        var m=endpoint.Machine;
                        if(endpoint.Port.Role==PortRole.Output)
                        {
                            var pos=m.Position;IItemPipeInventory inventory=m;
                            if(m.Definition.Id==IndustryId.Extractor){pos=Neighbor(m,1);inventory=(IItemPipeInventory)world.Storage(pos)??ItemEndpoint(pos) as CrateEndpoint;}
                            Source(m,inventory,pos);
                        }
                        for(int face=0;face<6;face++)
                        {
                            if((endpoint.Faces&(1<<face))==0)continue;
                            if(endpoint.Port.Role==PortRole.Input)Receiver(m,m.Position,IndustryDefinition.RotateFace(face,(4-m.Rotation)%4));
                            else if(endpoint.Port.Role==PortRole.Route)
                            {
                                var pos=IndustryDefinition.Neighbor(m.Position,face);var role=PipeEndRole(m,face);
                                if(role==PortRole.Output)Source(null,ItemEndpoint(pos),pos);
                                else if(role==PortRole.Input){int rotation=world is IIndustryItemEndpoints oriented?oriented.ItemEndpointRotation(pos):0;Receiver(ItemEndpoint(pos),pos,IndustryDefinition.RotateFace(face^1,(4-rotation)%4));}
                            }
                        }
                    }
                }
            }
            // Settings changes rebuild only compact endpoint buckets, never pipe topology.
            System.Array.Clear(itemPriorityPresent,0,itemPriorityPresent.Length);
            foreach(var routes in itemRoutes.Values)
            {
                bool changed=false;foreach(var receiver in routes.Receivers)if(receiver.Priority!=ItemPipeRouting.Priority(receiver.Inventory)){changed=true;break;}
                if(changed)
                {
                    routes.Buckets.Clear();
                    foreach(var receiver in routes.Receivers)
                    {
                        int priority=receiver.Priority=ItemPipeRouting.Priority(receiver.Inventory);
                        if(!routes.Buckets.TryGetValue(priority,out var bucket))routes.Buckets.Add(priority,bucket=new ItemRouteBucket());bucket.Receivers.Add(receiver);
                    }
                }
                foreach(var priority in routes.Buckets.Keys)itemPriorityPresent[priority]=true;
            }
            itemPriorities.Clear();for(int p=100;p>=0;p--)if(itemPriorityPresent[p])itemPriorities.Add(p);
        }
        void CaptureItemSource(NetworkTopology.Group graph,MachineState machine,IItemPipeInventory source,BlockPos position)
        {
            if(source==null||!world.Ready(position))return;
            var slots=source.Slots;int count=slots.Count,start=(int)(Tick/5%System.Math.Max(1,count));
            for(int n=0;n<count;n++)
            {
                int slot=(start+n)%count;var stack=slots[slot];
                if(!stack.Empty&&source.CanExtract(slot))itemSources.Add((graph,machine,source,position,slot,stack,ItemPipeRouting.SourceIdentity(source,slot)));
            }
        }
        void TransferConfiguredItems()
        {
            if(Tick%5!=0)return;
            long began=System.Diagnostics.Stopwatch.GetTimestamp(),allocated=System.GC.GetAllocatedBytesForCurrentThread();
            RefreshItemRoutes();itemSources.Clear();itemSent.Clear();
            foreach(var entry in itemRoutes)
                foreach(var source in entry.Value.Sources)
                {
                    if(source.machine?.Definition.Id==IndustryId.Extractor)
                    {if(!source.machine.Enabled){source.machine.Status=MachineStatus.DisabledBySignal;continue;}source.machine.Status=MachineStatus.NoInput;}
                    CaptureItemSource(entry.Key,source.machine,source.inventory,source.position);
                }
            // All slots are snapshotted before delivery; one physical source emits once.
            // Higher priorities get every source's compatible cargo before overflow tiers.
            int first=(int)(Tick/5%System.Math.Max(1,itemSources.Count));
            foreach(int priority in itemPriorities)
            for(int i=0;i<itemSources.Count;i++)
            {
                var candidate=itemSources[(first+i)%itemSources.Count];var source=candidate.inventory;
                if(itemSent.Contains(candidate.identity)||!world.Ready(candidate.position))continue;
                var current=source is CrateEndpoint crate?crate.Store(candidate.slot)?.Stack??default:source.Slots[candidate.slot];
                if(current.Empty||current.Id!=candidate.stack.Id||!source.CanExtract(candidate.slot))continue;
                bool sent=DeliverItem(candidate.graph,source,candidate.identity,current.WithCount(1),priority);
                if(sent){source.Extract(candidate.slot,1);itemSent.Add(candidate.identity);ItemEndpointChanged(candidate.position);}
                if(candidate.machine?.Definition.Id==IndustryId.Extractor)candidate.machine.Status=sent?MachineStatus.Running:MachineStatus.OutputFull;
            }
            LastItemRoutingMs=(System.Diagnostics.Stopwatch.GetTimestamp()-began)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            LastItemRoutingAllocatedBytes=System.GC.GetAllocatedBytesForCurrentThread()-allocated;
        }
        bool DeliverItem(NetworkTopology.Group graph,IItemPipeInventory source,object identity,ItemStack stack,int priority)
        {
            if(!itemRoutes[graph].Buckets.TryGetValue(priority,out var bucket))return false;
            int count=bucket.Receivers.Count,start=bucket.Next[stack.Id]%count;
            for(int n=0;n<count;n++)
            {
                int index=(start+n)%count;var receiver=bucket.Receivers[index];
                if(!world.Ready(receiver.Position)||ReferenceEquals(receiver.Inventory,source)||receiver.Inventory is IItemPipeRoutingPolicy own&&own.SharesStorage(source))continue;
                int rejectionKey=stack.Id+(stack.HasInstanceState?256:0);
                if(receiver.RejectedAt!=null&&receiver.RejectedAt[rejectionKey]==Tick)continue;
                foreach(int face in receiver.Faces)
                {
                    ItemReceiverProbes++;
                    bool accepted=stack.HasInstanceState?receiver.Inventory is ItemContainer container&&container.Add(stack)==0:receiver.Inventory is IItemPipeRoutingPolicy policy?policy.TryInsertFrom(stack.Id,identity,face):receiver.Inventory.TryInsert(stack.Id,face);
                    if(!accepted)continue;
                    bucket.Next[stack.Id]=(index+1)%count;ItemEndpointChanged(receiver.Position);return true;
                }
                // A rejected item is rechecked next transfer phase, not once per source.
                // Source/self exclusions above never poison receiver compatibility.
                receiver.RejectedAt??=new long[512];receiver.RejectedAt[rejectionKey]=Tick;
            }
            return false;
        }
    }
}
