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
            int value=PipeEndRole(pipe,face)==PortRole.Input?2:1;
            pipe.PipeDirections=(pipe.PipeDirections&~(3<<(face*2)))|(value<<(face*2));
            Invalidate();return true;
        }
        IEnumerable<MachinePort> TransportPorts(MachineState m,NetworkKind kind)
        {
            bool active=false;
            foreach(var p in PipeConnections.Ports(m))
            {
                if(p.Kind!=kind)continue;
                if(p.Role==PortRole.Route){yield return p;yield break;}
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
                if(m.Definition.Id==IndustryId.TankController&&role!=PortRole.Output)continue;
                int localFace=IndustryDefinition.RotateFace(face,(4-m.Rotation)%4);
                yield return new MachinePort(kind,role,1<<localFace);
            }
        }

        readonly List<(NetworkTopology.Group graph,MachineState machine,IItemPipeInventory inventory,BlockPos position,int slot,ItemStack stack)> itemSources
            =new List<(NetworkTopology.Group,MachineState,IItemPipeInventory,BlockPos,int,ItemStack)>();
        readonly HashSet<IItemPipeInventory> itemSent=new HashSet<IItemPipeInventory>();

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
            itemSources.Clear();itemSent.Clear();
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
            int first=(int)(Tick/5%System.Math.Max(1,itemSources.Count));
            // Give every receiver a chance to continue its existing input/product
            // before any source offers it a new type. Keep the same source budget.
            for(int priority=0;priority<2;priority++)
            for(int i=0;i<itemSources.Count;i++)
            {
                var candidate=itemSources[(first+i)%itemSources.Count];var source=candidate.inventory;
                if(itemSent.Contains(source)||!world.Ready(candidate.position))continue;
                var current=source.Slots[candidate.slot];
                if(current.Empty||current.Id!=candidate.stack.Id||!source.CanExtract(candidate.slot))continue;
                bool sent=DeliverItem(candidate.graph,source,current.Id,priority==0);
                if(sent){source.Extract(candidate.slot,1);itemSent.Add(source);ItemEndpointChanged(candidate.position);}
                if(candidate.machine?.Definition.Id==IndustryId.Extractor)candidate.machine.Status=sent?MachineStatus.Running:MachineStatus.OutputFull;
            }
        }
        bool DeliverItem(NetworkTopology.Group graph,IItemPipeInventory source,byte id,bool preferredOnly)
        {
            int count=graph.Ports.Count,start=(int)(Tick/5%System.Math.Max(1,count));
            for(int n=0;n<count;n++)
            {
                var endpoint=graph.Ports[(start+n)%count];var m=endpoint.Machine;
                if(endpoint.Port.Role!=PortRole.Input||ReferenceEquals(source,m)||!world.Ready(m.Position))continue;
                var destination=(IItemPipeInventory)m;
                if((!preferredOnly||destination.Prefers(id))&&destination.TryInsert(id))return true;
            }
            foreach(var endpoint in graph.Ports)
            {
                if(endpoint.Port.Role!=PortRole.Route)continue;
                for(int face=0;face<6;face++)
                {
                    if((endpoint.Faces&(1<<face))==0||PipeEndRole(endpoint.Machine,face)!=PortRole.Input)continue;
                    var pos=IndustryDefinition.Neighbor(endpoint.Machine.Position,face);var dest=ItemEndpoint(pos);
                    if(dest==null||ReferenceEquals(dest,source)||preferredOnly&&!dest.Prefers(id)||!dest.TryInsert(id))continue;
                    ItemEndpointChanged(pos);return true;
                }
            }
            return false;
        }
    }
}
