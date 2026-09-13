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

        readonly List<(NetworkTopology.Group graph,MachineState machine,IItemPipeInventory inventory,BlockPos position,int slot,ItemStack stack)> itemSources
            =new List<(NetworkTopology.Group,MachineState,IItemPipeInventory,BlockPos,int,ItemStack)>();
        readonly HashSet<IItemPipeInventory> itemSent=new HashSet<IItemPipeInventory>();
        readonly List<IItemPipeInventory> itemSourceOrder=new List<IItemPipeInventory>();
        readonly Dictionary<IItemPipeInventory,List<int>> itemSourceSlots=new Dictionary<IItemPipeInventory,List<int>>();
        sealed class ItemReceiver
        {
            public IItemPipeInventory Inventory;public BlockPos Position;
            public readonly List<int> Faces=new List<int>();
        }
        readonly Dictionary<(NetworkTopology.Group,byte,bool),FairAllocation<ItemReceiver>> itemShares
            =new Dictionary<(NetworkTopology.Group,byte,bool),FairAllocation<ItemReceiver>>();

        void CaptureItemSource(NetworkTopology.Group graph,MachineState machine,IItemPipeInventory source,BlockPos position)
        {
            if(source==null)return;
            // Snapshot every eligible stack so an incompatible first slot cannot
            // starve compatible cargo elsewhere in the same chest.
            int count=source.Slots.Count,start=(int)(Tick/5%System.Math.Max(1,count));
            for(int n=0;n<count;n++)
            {
                int slot=(start+n)%count;var stack=source.Slots[slot];
                if(!stack.Empty&&source.CanExtract(slot))itemSources.Add((graph,machine,source,position,slot,stack));
            }
        }
        void TransferConfiguredItems()
        {
            if(Tick%5!=0)return;
            itemSources.Clear();itemSent.Clear();itemShares.Clear();
            // Capture before delivery. An inventory emits at most one item per phase,
            // and cannot forward newly received items in that same phase.
            foreach(var graph in ItemNetwork.Groups)
            {
                int count=graph.Ports.Count,start=(int)(Tick/5%System.Math.Max(1,count));
                for(int n=0;n<count;n++)
                {
                    var endpoint=graph.Ports[(start+n)%count];var m=endpoint.Machine;
                    if(endpoint.Port.Role==PortRole.Output)
                    {
                        IItemPipeInventory source=m;var pos=m.Position;
                        if(m.Definition.Id==IndustryId.Extractor)
                        {
                            if(!m.Enabled){m.Status=MachineStatus.DisabledBySignal;continue;}
                            pos=Neighbor(m,1);source=world.Ready(pos)?world.Storage(pos):null;
                            m.Status=MachineStatus.NoInput;
                        }
                        CaptureItemSource(graph,m,source,pos);
                    }
                    else if(endpoint.Port.Role==PortRole.Route)
                    {
                        for(int face=0;face<6;face++)
                        {
                            if((endpoint.Faces&(1<<face))==0||PipeEndRole(m,face)!=PortRole.Output)continue;
                            var pos=IndustryDefinition.Neighbor(m.Position,face);
                            CaptureItemSource(graph,null,ItemEndpoint(pos),pos);
                        }
                    }
                }
            }
            itemSourceOrder.Clear();itemSourceSlots.Clear();
            for(int i=0;i<itemSources.Count;i++)
            {
                var source=itemSources[i].inventory;
                if(!itemSourceSlots.TryGetValue(source,out var slots))
                {slots=new List<int>();itemSourceSlots.Add(source,slots);itemSourceOrder.Add(source);}
                slots.Add(i);
            }
            int first=(int)(Tick/5%System.Math.Max(1,itemSourceOrder.Count));
            // Give every receiver a chance to continue its existing input/product
            // before any source offers it a new type. Keep the same source budget.
            for(int priority=0;priority<2;priority++)
            for(int i=0;i<itemSourceOrder.Count;i++)
            foreach(int slot in itemSourceSlots[itemSourceOrder[(first+i)%itemSourceOrder.Count]])
            {
                var candidate=itemSources[slot];var source=candidate.inventory;
                if(itemSent.Contains(source)||!world.Ready(candidate.position))continue;
                var current=source.Slots[candidate.slot];
                if(current.Empty||current.Id!=candidate.stack.Id||!source.CanExtract(candidate.slot))continue;
                bool sent=DeliverItem(candidate.graph,source,current.WithCount(1),priority==0);
                if(sent){source.Extract(candidate.slot,1);itemSent.Add(source);ItemEndpointChanged(candidate.position);}
                if(candidate.machine?.Definition.Id==IndustryId.Extractor)candidate.machine.Status=sent?MachineStatus.Running:MachineStatus.OutputFull;
            }
        }
        bool DeliverItem(NetworkTopology.Group graph,IItemPipeInventory source,ItemStack stack,bool preferredOnly)
        {
            byte id=stack.Id;
            var key=(graph,id,preferredOnly);
            if(!itemShares.TryGetValue(key,out var shares))
            {
                shares=new FairAllocation<ItemReceiver>();itemShares.Add(key,shares);
                var receivers=new Dictionary<IItemPipeInventory,ItemReceiver>();
                void Add(IItemPipeInventory inventory,BlockPos position,int face)
                {
                    if(inventory==null||!world.Ready(position)||preferredOnly&&(inventory is ItemContainer||!inventory.Prefers(id,face)))return;
                    if(!receivers.TryGetValue(inventory,out var receiver))
                    {
                        receiver=new ItemReceiver{Inventory=inventory,Position=position};receivers.Add(inventory,receiver);
                        shares.Add(receiver,long.MaxValue);
                    }
                    if(!receiver.Faces.Contains(face))receiver.Faces.Add(face);
                }
                foreach(var endpoint in graph.Ports)
                {
                    var m=endpoint.Machine;
                    for(int face=0;face<6;face++)
                    {
                        if((endpoint.Faces&(1<<face))==0)continue;
                        if(endpoint.Port.Role==PortRole.Input)
                            Add(m,m.Position,IndustryDefinition.RotateFace(face,(4-m.Rotation)%4));
                        else if(endpoint.Port.Role==PortRole.Route&&PipeEndRole(m,face)==PortRole.Input)
                        {
                            var pos=IndustryDefinition.Neighbor(m.Position,face);
                            int rotation=world is IIndustryItemEndpoints oriented?oriented.ItemEndpointRotation(pos):0;
                            Add(ItemEndpoint(pos),pos,IndustryDefinition.RotateFace(face^1,(4-rotation)%4));
                        }
                    }
                }
            }
            return shares.Distribute(1,Tick/5,(receiver,offer)=>
            {
                foreach(int face in receiver.Faces)
                    if(stack.HasContents ? receiver.Inventory is ItemContainer container && container.Add(stack)==0 : receiver.Inventory.TryInsert(id,face)){ItemEndpointChanged(receiver.Position);return 1;}
                return 0;
            },receiver=>!ReferenceEquals(receiver.Inventory,source))==1;
        }
    }
}
