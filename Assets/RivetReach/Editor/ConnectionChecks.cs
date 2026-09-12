using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RivetReach.Editor
{
    public static class ConnectionChecks
    {
        sealed class World : IIndustryWorld
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public readonly Dictionary<BlockPos,ItemContainer> Chests=new Dictionary<BlockPos,ItemContainer>();
            public readonly HashSet<BlockPos> Sleeping=new HashSet<BlockPos>();
            public bool Ready(BlockPos p)=>!Sleeping.Contains(p);
            public byte Get(BlockPos p)=>Cells.TryGetValue(p,out var id)?id:(byte)0;
            public bool Remove(BlockPos p,byte expected){if(Get(p)!=expected)return false;Cells.Remove(p);return true;}
            public ItemContainer Storage(BlockPos p)=>Chests.TryGetValue(p,out var inventory)?inventory:null;
            public byte Drop(byte id)=>id;
            public bool PlayerInside(BlockPos p)=>false;
        }
        sealed class Fixture
        {
            public readonly World World=new World();public readonly IndustrySimulation Sim;
            public Fixture(){Sim=new IndustrySimulation(World,_=>64);}
            public MachineState Add(BlockPos p,byte id,int rotation=0){World.Cells[p]=id;var m=Sim.Add(p,id);for(int i=0;i<rotation;i++)Sim.Rotate(m);return m;}
            public ItemContainer Chest(BlockPos p){World.Cells[p]=BlockId.Chest;var c=new ItemContainer(27,_=>64);World.Chests.Add(p,c);Sim.Invalidate();return c;}
            public void Steps(int count=5){for(int i=0;i<count;i++)Sim.Step();}
            public void Settle(){for(int i=0;i<100;i++){Sim.Step();if(!Sim.Rebuilding&&Sim.Multiblocks.PendingCount==0)return;}throw new Exception("Connections did not settle");}
            public void Mode(MachineState pipe,int face,PortRole role){for(int i=0;i<3&&Sim.PipeEndRole(pipe,face)!=role;i++)if(!Sim.TogglePipeEnd(pipe,face))throw new Exception("Cannot set fixture end");if(Sim.PipeEndRole(pipe,face)!=role)throw new Exception("Fixture mode not reached");}
        }
        public static void Run()
        {
            var log=new StringBuilder();int count=0;
            void Check(bool ok,string message){if(!ok)throw new Exception("Connections: "+message);count++;log.AppendLine("PASS "+message);}
            var origin=new BlockPos(0,20,0);
            var registry=ItemRegistry.Load();var recipes=RecipeCatalogAsset.Load().Compile(registry);
            var craft=new CraftingSession(recipes,3,id=>registry.Get(id).stackLimit);
            foreach(int slot in new[]{0,1,4})craft.Grid.Add(BlockId.IronIngot,1,slot,slot+1);
            ItemStack wrench=default;
            Check(craft.CraftToCursor(ref wrench).Succeeded&&wrench.Id==IndustryId.Wrench&&wrench.Count==1&&craft.Grid.Slots.All(s=>s.Empty),"Wrench recipe consumes exactly three iron ingots and produces one tool");
            var personal=new CraftingSession(recipes,2,id=>registry.Get(id).stackLimit);
            foreach(int slot in new[]{0,1,3})personal.Grid.Add(BlockId.IronIngot,1,slot,slot+1);
            Check(personal.Preview==null&&registry.Get(IndustryId.Wrench).stackLimit==1&&!BlockId.Placeable(IndustryId.Wrench),"Wrench requires a workbench, does not stack and is not a placeable machine");
            {
                var f=new Fixture();var battery=f.Add(origin,IndustryId.Battery);var cable=f.Add(origin.Offset(1,0,0),IndustryId.PowerCable);
                var crusher=f.Add(origin.Offset(2,0,0),IndustryId.Crusher);crusher.Items.Add(BlockId.RawIron,4,0,1);f.Settle();
                var topology=f.Sim.Power.Topology;var graph=topology.Groups;int rebuilds=f.Sim.TopologyRebuilds;
                Check(topology.Connected(crusher)&&crusher.Status==MachineStatus.NoPower&&crusher.RequestedWatts==160,"Empty battery leaves the crusher connected but without electrical power");
                f.Steps(80);Check(f.Sim.TopologyRebuilds==rebuilds&&ReferenceEquals(graph,topology.Groups),"A sustained blackout does not re-register the power network");
                battery.EnergyCells[0].Charge(2500);f.Sim.Step();
                Check(topology.Connected(crusher)&&crusher.ReceivedWatts==50&&crusher.Status==MachineStatus.Underpowered,"Limited stored energy changes allocation without changing connection");
                f.Sim.Step();Check(topology.Connected(crusher)&&crusher.ReceivedWatts==0&&crusher.Status==MachineStatus.NoPower&&f.Sim.TopologyRebuilds==rebuilds,"Battery depletion retains registration and reports no power");
                var connections=topology.Connections;
                using(var pending=topology.Rebuild(new[]{crusher}).GetEnumerator())
                {Check(pending.MoveNext()&&ReferenceEquals(graph,topology.Groups)&&ReferenceEquals(connections,topology.Connections)&&topology.Connected(crusher),"Partial traversal leaves the last complete graph and cable connections visible");}
                Check(ReferenceEquals(graph,topology.Groups)&&topology.Connected(crusher),"Cancelled traversal cannot publish a half-built network");
                for(int i=0;i<800;i++)f.Add(origin.Offset(20+i,0,0),IndustryId.SignalConduit);
                f.Sim.Step();Check(f.Sim.Rebuilding&&topology.Connected(crusher)&&crusher.RequestedWatts==160&&crusher.Status==MachineStatus.NoPower,"Unrelated budgeted rebuild preserves connected blackout and last completed demand");
                f.Settle();Check(topology.Connected(crusher)&&crusher.ReceivedWatts==0,"Completed unrelated graph edits preserve a zero-power connection");
                f.Sim.Remove(cable.Position);f.Settle();Check(!topology.Connected(crusher)&&crusher.ReceivedWatts==0,"Removing the electrical link changes connectivity independently of zero supply");
                Check(!topology.Connected(new MachineState(battery.Position,IndustryId.Battery,_=>64)),"A replacement at the same position does not inherit another machine's registration");
            }
            for(int face=0;face<6;face++)for(int rotation=0;rotation<4;rotation++)
            {
                string label=$"face {face}, rotation {rotation}";
                var f=new Fixture();var battery=f.Add(origin,IndustryId.Battery);battery.EnergyCells[0].Charge(2150000);
                var cablePos=IndustryDefinition.Neighbor(origin,face);var crusherPos=IndustryDefinition.Neighbor(cablePos,face);
                f.Add(cablePos,IndustryId.PowerCable);var crusher=f.Add(crusherPos,IndustryId.Crusher,rotation);crusher.Items.Add(BlockId.RawIron,3,0,1);
                f.Settle();long before=battery.EnergyCells[0].Amount;f.Sim.Step();
                Check(crusher.ReceivedWatts==160&&before-battery.EnergyCells[0].Amount==8000,"Battery → cable → crusher delivers exactly 160 W: "+label);
                f.Sim.Remove(cablePos);f.Settle();Check(crusher.ReceivedWatts==0,"Removing cable disconnects power: "+label);

                f=new Fixture();crusher=f.Add(origin,IndustryId.Crusher,rotation);var pipe=f.Add(cablePos,IndustryId.ItemPipe);var chest=f.Chest(crusherPos);
                chest.Add(BlockId.RawIron,4);f.Mode(pipe,face^1,PortRole.Input);f.Mode(pipe,face,PortRole.Output);f.Settle();f.Steps();
                Check(crusher.Items.Total(BlockId.RawIron)>0&&crusher.Items.Total(BlockId.RawIron)+chest.Total(BlockId.RawIron)==4,"Configured item input accepts raw ore: "+label);
                chest.Take(0,64);crusher.Items.Add(IndustryId.CrushedIron,4,2,3);
                int raw=crusher.Items.Total(BlockId.RawIron);f.Mode(pipe,face^1,PortRole.Output);f.Mode(pipe,face,PortRole.Input);f.Settle();f.Steps();
                Check(chest.Total(IndustryId.CrushedIron)>0&&crusher.Items.Total(IndustryId.CrushedIron)+chest.Total(IndustryId.CrushedIron)==4&&crusher.Items.Total(BlockId.RawIron)==raw,"Reversing item end extracts only product without consuming input: "+label);

                f=new Fixture();var a=f.Add(origin,IndustryId.Tank,rotation);pipe=f.Add(cablePos,IndustryId.FluidPipe);var b=f.Add(crusherPos,IndustryId.Tank);
                f.Mode(pipe,face^1,PortRole.Output);f.Mode(pipe,face,PortRole.Input);a.WaterMl=1000;f.Settle();
                Check(a.WaterMl==900&&b.WaterMl==100,"Fluid output moves exactly 100 mL on any face: "+label);
                f.Mode(pipe,face^1,PortRole.Input);f.Mode(pipe,face,PortRole.Output);f.Settle();
                Check(a.WaterMl==1000&&b.WaterMl==0,"Reversing fluid ends sends stored water back without loss: "+label);
            }
            // Distinct faces are distinct terminals, including when they share the
            // same direction. A full machine cannot secretly join disconnected pipes.
            {
                var f=new Fixture();var machine=f.Add(origin,IndustryId.Crusher);machine.Items.Add(BlockId.RawIron,64,0,1);
                var left=f.Add(origin.Offset(-1,0,0),IndustryId.ItemPipe);var right=f.Add(origin.Offset(1,0,0),IndustryId.ItemPipe);
                var source=f.Chest(origin.Offset(-2,0,0));var dest=f.Chest(origin.Offset(2,0,0));source.Add(BlockId.RawGold,4);
                f.Mode(left,1,PortRole.Output);f.Mode(left,0,PortRole.Input);f.Mode(right,1,PortRole.Input);f.Settle();f.Steps(20);
                var groups=f.Sim.ItemNetwork.Groups.Where(g=>g.Ports.Any(p=>p.Machine==machine&&p.Port.Role==PortRole.Input)).ToArray();
                Check(source.Total(BlockId.RawGold)==4&&dest.Total(BlockId.RawGold)==0&&groups.Count(g=>g.Ports.Any(p=>p.Machine==left||p.Machine==right))==2,"Machine input faces do not bridge independent pipe networks");
                int directions=left.PipeDirections;f.Sim.Rotate(machine);f.Settle();Check(left.PipeDirections==directions&&f.Sim.PipeEndRole(left,0)==PortRole.Input,"Machine rotation preserves explicit world-facing pipe directions");
                f.World.Sleeping.Add(machine.Position);f.Sim.Invalidate();f.Settle();Check(!f.Sim.TogglePipeEnd(left,0),"Dormant endpoint rejects configuration");
            }
            {
                var f=new Fixture();var source=f.Chest(origin);source.Add(BlockId.Stone,10);var pipe=f.Add(origin.Offset(1,0,0),IndustryId.ItemPipe);
                var middle=f.Chest(origin.Offset(2,0,0));var pipe2=f.Add(origin.Offset(3,0,0),IndustryId.ItemPipe);var dest=f.Chest(origin.Offset(4,0,0));
                f.Mode(pipe,1,PortRole.Output);f.Mode(pipe2,1,PortRole.Output);f.Settle();while(f.Sim.Tick%5!=4)f.Sim.Step();f.Sim.Step();
                Check(middle.Total(BlockId.Stone)==1&&dest.Total(BlockId.Stone)==0,"Chest cannot forward a newly received item in the same transfer phase");
                f.Steps();Check(dest.Total(BlockId.Stone)==1&&source.Total(BlockId.Stone)+middle.Total(BlockId.Stone)+dest.Total(BlockId.Stone)==10,"Later chest forwarding preserves every item");
            }
            {
                var f=new Fixture();var a=f.Add(origin,IndustryId.Tank);var pipe=f.Add(origin.Offset(1,0,0),IndustryId.FluidPipe);var b=f.Add(origin.Offset(2,0,0),IndustryId.Tank);
                f.Add(origin.Offset(3,0,0),IndustryId.FluidPipe);var c=f.Add(origin.Offset(4,0,0),IndustryId.Tank);a.WaterMl=100;f.Settle();
                Check(a.WaterMl==0&&b.WaterMl==100&&c.WaterMl==0,"A tank cannot forward newly received fluid across graphs in the same phase");
                f.Sim.Step();Check(b.WaterMl==0&&c.WaterMl==100,"Stored fluid becomes eligible on the next fixed step");
            }
            {
                var f=new Fixture();var boiler=f.Add(origin,IndustryId.Boiler);var pipe=f.Add(origin.Offset(0,1,0),IndustryId.ItemPipe);var chest=f.Chest(origin.Offset(0,2,0));
                chest.Add(BlockId.Coal,2);f.Mode(pipe,2,PortRole.Output);f.Settle();f.Steps();Check(boiler.Items.Total(BlockId.Coal)>0,"Top item inlet fills boiler's existing fuel buffer");
                Check(!f.Sim.TogglePipeEnd(pipe,0),"An unattached end cannot be configured");
            }
            for(int face=0;face<6;face++)for(int rotation=0;rotation<4;rotation++)
            foreach(byte id in new[]{IndustryId.ItemPipe,IndustryId.FluidPipe})
            {
                var f=new Fixture();bool fluid=id==IndustryId.FluidPipe;
                var pipe=f.Add(origin,id,rotation);var pos=IndustryDefinition.Neighbor(origin,face);
                var otherPos=IndustryDefinition.Neighbor(origin,face^1);
                var a=fluid?f.Add(pos,IndustryId.Tank):null;var b=fluid?f.Add(otherPos,IndustryId.Tank):null;
                var source=fluid?null:f.Chest(pos);var dest=fluid?null:f.Chest(otherPos);
                f.Mode(pipe,face,PortRole.Input);f.Mode(pipe,face^1,PortRole.Input);
                Check(f.Sim.TogglePipeEnd(pipe,face)&&f.Sim.PipeEndRole(pipe,face)==PortRole.Output,"Input cycles to Output");
                Check(f.Sim.TogglePipeEnd(pipe,face)&&f.Sim.PipeEndRole(pipe,face)==PortRole.Disabled,"Output cycles to No connection");
                if(fluid)a.WaterMl=1000;else source.Add(BlockId.Stone,10);
                f.Settle();f.Steps(20);var network=fluid?f.Sim.FluidNetwork:f.Sim.ItemNetwork;
                network.Connections.TryGetValue(pipe.Position,out int mask);
                Check((mask&(1<<face))==0&&(mask&(1<<(face^1)))!=0,"Disconnected end removes only its own connection mask on all faces/rotations");
                Check(fluid?a.WaterMl==1000&&b.WaterMl==0:source.Total(BlockId.Stone)==10&&dest.Total(BlockId.Stone)==0,"No connection blocks extraction and conserves resources");
                Check(f.Sim.HasPipeEnd(pipe,face)&&f.Sim.TogglePipeEnd(pipe,face)&&f.Sim.PipeEndRole(pipe,face)==PortRole.Input,"Missing arm remains configurable and reconnects as Input");
                f.Mode(pipe,face,PortRole.Output);f.Settle();f.Steps(20);
                Check(fluid?b.WaterMl>0&&a.WaterMl+b.WaterMl==1000:dest.Total(BlockId.Stone)>0&&source.Total(BlockId.Stone)+dest.Total(BlockId.Stone)==10,"Reconnected output resumes exact transport");
                f.Mode(pipe,face^1,PortRole.Disabled);f.Settle();int before=fluid?b.WaterMl:dest.Total(BlockId.Stone);f.Steps(20);
                Check((fluid?b.WaterMl:dest.Total(BlockId.Stone))==before,"No connection also blocks destination insertion");
                f.Mode(pipe,face,PortRole.Disabled);int settings=pipe.PipeDirections;
                f.World.Sleeping.Add(origin);f.Sim.Invalidate();f.Settle();f.World.Sleeping.Clear();f.Sim.Invalidate();f.Settle();
                Check(pipe.PipeDirections==settings&&f.Sim.DisconnectedPipeFaces(pipe)==((1<<face)|(1<<(face^1))),"Both disabled ends survive residency rebuild without restoring defaults");
                var visual=ConnectedPipeVisuals.Create(fluid?"fluid_pipe":"item_pipe",null);
                ConnectedPipeVisuals.Set(visual,fluid?"fluid_pipe":"item_pipe",1<<(face^1),rotation,1<<face);
                var bounds=visual.GetComponentInChildren<UnityEngine.Renderer>().bounds;
                Check(face%2==0?bounds.max[face/2]<.8f:bounds.min[face/2]>.2f,"Rendered closed half retracts from the disabled boundary");
                ConnectedPipeVisuals.Set(visual,fluid?"fluid_pipe":"item_pipe",0,rotation,63);
                bounds=visual.GetComponentInChildren<UnityEngine.Renderer>().bounds;
                Check(bounds.min.x>0&&bounds.min.y>0&&bounds.min.z>0&&bounds.max.x<1&&bounds.max.y<1&&bounds.max.z<1,"Fully disconnected pipe leaves visible gaps at every boundary");
                UnityEngine.Object.DestroyImmediate(visual);
            }
            {
                var f=new Fixture();var tank=f.Add(origin,IndustryId.Tank);var pipe=f.Add(origin.Offset(1,0,0),IndustryId.FluidPipe);
                pipe.Additions=PipeAddition.Power|PipeAddition.Signal;var battery=f.Add(origin.Offset(1,0,1),IndustryId.Battery);var lever=f.Add(origin.Offset(1,0,-1),IndustryId.Lever);
                f.Mode(pipe,1,PortRole.Disabled);f.Settle();
                Check(f.Sim.Power.Topology.Connected(battery)&&f.Sim.Signals.Topology.Connections.ContainsKey(pipe.Position),"Transport disconnection preserves independent power and signal fittings");
                f.Sim.Remove(tank.Position);f.World.Cells.Remove(tank.Position);var next=f.Add(tank.Position,IndustryId.FluidPipe);f.Settle();
                Check(f.Sim.FluidNetwork.Connections.TryGetValue(pipe.Position,out int mask)&&(mask&2)!=0&&!f.Sim.TogglePipeEnd(pipe,1),"Stored disabled terminal does not disable a replacement pipe-to-pipe run");
            }
            Check(PipeConnections.ValidDirections(0)&&PipeConnections.ValidDirections(2730)&&PipeConnections.ValidDirections(4095)&&!PipeConnections.ValidDirections(-1)&&!PipeConnections.ValidDirections(4096),"Saved direction packing accepts disconnected ends and rejects out-of-range bits");
            Directory.CreateDirectory("Logs/Connections");File.WriteAllText("Logs/Connections/checks.txt",$"PASS {count} assertions\n"+log);
        }
    }
}
