using System.Collections.Generic;

namespace RivetReach
{
    public sealed partial class IndustrySimulation
    {
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
            return world.Ready(position)&&(PipeConnections.Supports(At(position),PipeConnections.TransportKind(pipe))||pipe.Definition.Id==IndustryId.ItemPipe&&world.Storage(position)!=null);
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

        readonly List<(NetworkTopology.Group graph,MachineState machine,ItemContainer container,int slot,ItemStack stack)> itemSources
            =new List<(NetworkTopology.Group,MachineState,ItemContainer,int,ItemStack)>();
        readonly HashSet<ItemContainer> itemSent=new HashSet<ItemContainer>();

        void TransferConfiguredItems()
        {
            if(Tick%5!=0)return;
            itemSources.Clear();itemSent.Clear();
            // Capture sources before delivery: an empty chest cannot forward an item
            // it receives later in this phase. One inventory emits at most 4 items/s,
            // however many output ends or disconnected networks it touches.
            foreach(var graph in ItemNetwork.Groups)
            {
                int count=graph.Ports.Count,start=(int)(Tick/5%System.Math.Max(1,count));
                for(int n=0;n<count;n++)
                {
                    var endpoint=graph.Ports[(start+n)%count];var m=endpoint.Machine;
                    if(endpoint.Port.Role==PortRole.Output)
                    {
                        var source=m.Items;int slot=2;
                        if(m.Definition.Id==IndustryId.Extractor)
                        {
                            if(!m.Enabled){m.Status=MachineStatus.DisabledBySignal;continue;}
                            var pos=Neighbor(m,1);source=world.Ready(pos)?world.Storage(pos):null;
                            slot=source?.FindSlot(s=>!s.Empty)??-1;
                        }
                        if(source==null||slot<0||source.Slots[slot].Empty)
                        {if(m.Definition.Id==IndustryId.Extractor)m.Status=MachineStatus.NoInput;continue;}
                        itemSources.Add((graph,m,source,slot,source.Slots[slot]));
                    }
                    else if(endpoint.Port.Role==PortRole.Route)
                    {
                        for(int face=0;face<6;face++)
                        {
                            if((endpoint.Faces&(1<<face))==0||PipeEndRole(m,face)!=PortRole.Output)continue;
                            var pos=IndustryDefinition.Neighbor(m.Position,face);var source=world.Ready(pos)?world.Storage(pos):null;
                            int slot=source?.FindSlot(s=>!s.Empty)??-1;
                            if(slot>=0)itemSources.Add((graph,null,source,slot,source.Slots[slot]));
                        }
                    }
                }
            }
            // Rotate source order across graphs as well as within a graph.
            int first=(int)(Tick/5%System.Math.Max(1,itemSources.Count));
            for(int i=0;i<itemSources.Count;i++)
            {
                var candidate=itemSources[(first+i)%itemSources.Count];var source=candidate.container;
                if(itemSent.Contains(source))continue;
                var current=source.Slots[candidate.slot];
                if(current.Empty||current.Id!=candidate.stack.Id)continue;
                bool sent=DeliverItem(candidate.graph,source,current.Id);
                if(sent){source.Take(candidate.slot,1);itemSent.Add(source);}
                if(candidate.machine?.Definition.Id==IndustryId.Extractor)candidate.machine.Status=sent?MachineStatus.Running:MachineStatus.OutputFull;
            }
        }
        bool DeliverItem(NetworkTopology.Group graph,ItemContainer source,byte id)
        {
            int count=graph.Ports.Count,start=(int)(Tick/5%System.Math.Max(1,count));
            for(int n=0;n<count;n++)
            {
                var endpoint=graph.Ports[(start+n)%count];var m=endpoint.Machine;
                if(endpoint.Port.Role!=PortRole.Input||ReferenceEquals(source,m.Items)||!m.Accepts(0,id)||m.Items.Capacity(id,0,1)==0)continue;
                m.Items.Add(id,1,0,1);return true;
            }
            foreach(var endpoint in graph.Ports)
            {
                if(endpoint.Port.Role!=PortRole.Route)continue;
                for(int face=0;face<6;face++)
                {
                    if((endpoint.Faces&(1<<face))==0||PipeEndRole(endpoint.Machine,face)!=PortRole.Input)continue;
                    var pos=IndustryDefinition.Neighbor(endpoint.Machine.Position,face);var dest=world.Ready(pos)?world.Storage(pos):null;
                    if(dest==null||ReferenceEquals(dest,source)||dest.Capacity(id)==0)continue;
                    dest.Add(id,1);return true;
                }
            }
            return false;
        }
    }
}
