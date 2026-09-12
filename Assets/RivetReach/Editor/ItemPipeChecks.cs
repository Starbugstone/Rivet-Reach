using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RivetReach.Editor
{
    public static class ItemPipeChecks
    {
        sealed class World : IIndustryWorld, IIndustryItemEndpoints
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public readonly Dictionary<BlockPos,IItemPipeInventory> Inventories=new Dictionary<BlockPos,IItemPipeInventory>();
            public readonly Dictionary<BlockPos,int> Rotations=new Dictionary<BlockPos,int>();
            public int ItemEndpointRotation(BlockPos p)=>Rotations.TryGetValue(p,out int rotation)?rotation:0;
            public readonly HashSet<BlockPos> Sleeping=new HashSet<BlockPos>(),Changed=new HashSet<BlockPos>();
            public bool Ready(BlockPos p)=>!Sleeping.Contains(p);
            public byte Get(BlockPos p)=>Cells.TryGetValue(p,out var b)?b:(byte)0;
            public bool Remove(BlockPos p,byte expected){if(Get(p)!=expected)return false;Cells.Remove(p);return true;}
            public ItemContainer Storage(BlockPos p)=>ItemEndpoint(p) as ItemContainer;
            public IItemPipeInventory ItemEndpoint(BlockPos p)=>Inventories.TryGetValue(p,out var inventory)?inventory:null;
            public void ItemEndpointChanged(BlockPos p)=>Changed.Add(p);
            public byte Drop(byte b)=>b;
            public bool PlayerInside(BlockPos p)=>false;
        }
        public static void BuildFuelFaceReview(){ConnectionBuild.Build();WikiExport.Export();}
        public static void Run()
        {
            int checks=0;var log=new StringBuilder();
            void Check(bool ok,string message){if(!ok)throw new Exception("Item pipes: "+message);checks++;log.AppendLine("PASS "+message);}
            var items=ItemRegistry.Load();var processing=ProcessingCatalogAsset.Load().Compile(items);
            int Limit(byte id)=>items.Get(id).stackLimit;
            var origin=new BlockPos(0,20,0);
            void Steps(IndustrySimulation sim,int count=20){for(int i=0;i<count;i++)sim.Step();}
            void Mode(IndustrySimulation sim,MachineState pipe,int face,PortRole role)
            {for(int i=0;i<3&&sim.PipeEndRole(pipe,face)!=role;i++)Check(sim.TogglePipeEnd(pipe,face),"Configure compatible endpoint");}
            foreach(int face in Enumerable.Range(0,6))
            foreach(byte crushed in new[]{IndustryId.CrushedCopper,IndustryId.CrushedIron,IndustryId.CrushedGold})
            {
                var world=new World();var sim=new IndustrySimulation(world,Limit);
                var crusher=sim.Add(origin,IndustryId.Crusher);crusher.Items.Add(crushed,4,2,3);
                var pipe=sim.Add(IndustryDefinition.Neighbor(origin,face),IndustryId.ItemPipe);
                var p=IndustryDefinition.Neighbor(pipe.Position,face);var furnace=new FurnaceState(processing,Limit);world.Inventories[p]=furnace;world.Rotations[p]=(face^1)==4?1:0;
                Mode(sim,pipe,face^1,PortRole.Output);Steps(sim);
                Check(sim.HasPipeEnd(pipe,face)&&sim.PipeEndRole(pipe,face)==PortRole.Input&&(sim.ItemNetwork.Connections[pipe.Position]&(1<<face))!=0,"Furnace has default blue inlet and geometry on face "+face);
                Check(furnace.Slots[0].Id==crushed&&furnace.Slots[0].Count>0&&furnace.Slots[0].Count+crusher.Items.Total(crushed)==4,"Crushed metal enters ingredients with exact conservation on face "+face);
                Check(furnace.Slots[1].Empty&&furnace.Slots[2].Empty&&world.Changed.Contains(p),"Ingredient transfer preserves fuel/result and notifies sleeping furnace");
                world.Sleeping.Add(p);int before=furnace.Slots[0].Count;Steps(sim);
                Check(furnace.Slots[0].Count==before,"Dormant furnace cannot receive through stale topology");
                world.Sleeping.Clear();sim.Invalidate();Steps(sim,30);
                Check(furnace.Slots[0].Count==4,"Residency restoration resumes transfer");
                var port=(IItemPipeInventory)furnace;
                Check(port.TryInsert(BlockId.Coal),"Furnace accepts pipe fuel");furnace.Advance(800);
                byte ingot=processing.Find(crushed).Output.Id;
                Check(furnace.Slots[2].Id==ingot&&furnace.Slots[2].Count==4,"Real processing recipe smelts all piped metal");
                // Replace the source with a chest; the configured furnace outlet extracts results only.
                sim.Remove(origin);var chest=new ItemContainer(27,Limit);world.Inventories[origin]=chest;
                Mode(sim,pipe,face,PortRole.Output);Mode(sim,pipe,face^1,PortRole.Input);world.Changed.Clear();Steps(sim,25);
                Check(chest.Total(ingot)==4&&furnace.Slots[2].Empty&&world.Changed.Contains(p),"Furnace result extraction conserves ingots and notifies owner");
                port.TryInsert(BlockId.RawIron);port.TryInsert(BlockId.Coal);Steps(sim,20);
                Check(chest.Total(BlockId.RawIron)==0&&chest.Total(BlockId.Coal)==0&&furnace.Slots[0].Count==1&&furnace.Slots[1].Count==1,"Output setting cannot steal ingredients or fuel");
            }
            {
                var world=new World();var sim=new IndustrySimulation(world,Limit);var furnace=new FurnaceState(processing,Limit);
                world.Inventories[origin]=furnace;var pipe=sim.Add(origin.Offset(1,0,0),IndustryId.ItemPipe);var chest=new ItemContainer(27,Limit);
                world.Inventories[origin.Offset(2,0,0)]=chest;chest.Add(BlockId.Dirt,8);chest.Add(IndustryId.CrushedIron,3);chest.Add(BlockId.Coal,2);
                Mode(sim,pipe,0,PortRole.Output);Steps(sim,60);
                Check(chest.Total(BlockId.Dirt)==8&&furnace.Slots[0].Id==IndustryId.CrushedIron&&furnace.Slots[0].Count==3&&furnace.Slots[1].Empty&&chest.Total(BlockId.Coal)==2,"Ingredient face rejects fuel without removing it from mixed chest");
                var port=(IItemPipeInventory)furnace;
                Check(!port.TryInsert(BlockId.Dirt)&&!port.TryInsert(BlockId.IronIngot)&&port.Extract(0,1).Empty&&port.Extract(1,1).Empty,"Furnace authority rejects incompatible inputs and protected extraction");
                for(int i=3;i<64;i++)Check(port.TryInsert(IndustryId.CrushedIron),"Fill ingredient buffer");
                chest.Add(IndustryId.CrushedIron,2);Steps(sim,20);
                Check(chest.Total(IndustryId.CrushedIron)==2&&furnace.Slots[0].Count==64,"Full ingredient slot retains excess at source");
                var logs=new FurnaceState(processing,Limit);Check(((IItemPipeInventory)logs).TryInsert(BlockId.Log)&&logs.Slots[0].Id==BlockId.Log&&logs.Slots[1].Empty,"Dual-purpose logs follow existing ingredient preference");
            }
            foreach(byte machineId in new[]{IndustryId.Crusher,IndustryId.Boiler,IndustryId.Drill})
            {
                var world=new World();var sim=new IndustrySimulation(world,Limit);var machine=sim.Add(origin,machineId);if(machineId==IndustryId.Boiler)sim.Rotate(machine);
                var pipe=sim.Add(origin.Offset(1,0,0),IndustryId.ItemPipe);var chest=new ItemContainer(27,Limit);world.Inventories[origin.Offset(2,0,0)]=chest;
                chest.Add(BlockId.Dirt,4);chest.Add(BlockId.Coal,4);chest.Add(BlockId.RawIron,4);
                Mode(sim,pipe,1,PortRole.Input);Mode(sim,pipe,0,PortRole.Output);Steps(sim,60);
                Check(machine.Items.Total(BlockId.Dirt)==0&&chest.Total(BlockId.Dirt)==4,"Machine rejects dirt: "+machineId);
                Check(machine.Items.Total(BlockId.Coal)==(machineId==IndustryId.Boiler?4:0)&&machine.Items.Total(BlockId.RawIron)==(machineId==IndustryId.Crusher?4:0),"Machine accepts only its supported recipe/fuel: "+machineId);
            }
            // Every horizontal rotation and all six incoming world faces use the
            // destination's local rear, including dual-purpose logs.
            for(int rotation=0;rotation<4;rotation++)
            for(int face=0;face<6;face++)
            {
                bool rear=face==IndustryDefinition.RotateFace(4,rotation);
                var world=new World();var sim=new IndustrySimulation(world,Limit);
                var furnace=new FurnaceState(processing,Limit);world.Inventories[origin]=furnace;world.Rotations[origin]=rotation;
                var pp=IndustryDefinition.Neighbor(origin,face);var pipe=sim.Add(pp,IndustryId.ItemPipe);
                var cp=IndustryDefinition.Neighbor(pp,face);var chest=new ItemContainer(27,Limit);world.Inventories[cp]=chest;chest.Add(BlockId.Log,6);
                Mode(sim,pipe,face,PortRole.Output);Steps(sim,20);
                int slot=rear?1:0;
                Check(furnace.Slots[slot].Id==BlockId.Log&&furnace.Slots[slot].Count==4&&furnace.Slots[1-slot].Empty&&chest.Total(BlockId.Log)==2,"Rotated logs enter only designated slot, preserving source budget: "+rotation+"/"+face);
                var held=new ItemStack(BlockId.Log,60);furnace.Click(slot,ref held,false);Steps(sim,20);
                Check(furnace.Slots[slot].Count==64&&furnace.Slots[1-slot].Empty&&chest.Total(BlockId.Log)==2,"Full designated slot cannot spill into other slot: "+rotation+"/"+face);
                furnace.Take(slot,64);chest.Take(0,64);chest.Add(rear?IndustryId.CrushedIron:BlockId.Coal,3);Steps(sim,20);
                Check(furnace.Slots[0].Empty&&furnace.Slots[1].Empty&&chest.Slots[0].Count==3,"Wrong-purpose cargo stays at source: "+rotation+"/"+face);
                foreach(byte id in new[]{IndustryId.Boiler,IndustryId.Crusher,IndustryId.Drill})
                {
                    var mw=new World();var ms=new IndustrySimulation(mw,Limit);var machine=ms.Add(origin,id);for(int turn=0;turn<rotation;turn++)ms.Rotate(machine);
                    var mp=ms.Add(pp,IndustryId.ItemPipe);var supply=new ItemContainer(27,Limit);mw.Inventories[cp]=supply;
                    byte cargo=id==IndustryId.Boiler?BlockId.Coal:BlockId.RawIron;supply.Add(cargo,4);
                    Mode(ms,mp,face,PortRole.Output);Mode(ms,mp,face^1,PortRole.Input);Steps(ms,20);
                    int expected=id==IndustryId.Crusher||id==IndustryId.Boiler&&rear?4:0;
                    Check(machine.Items.Total(cargo)==expected&&supply.Total(cargo)==4-expected,"Machine face capability and conservation: "+id+"/"+rotation+"/"+face);
                }
            }
            {
                var world=new World();var sim=new IndustrySimulation(world,Limit);var furnace=new FurnaceState(processing,Limit);world.Inventories[origin]=furnace;
                // One connected run supplies both the side and back from one chest.
                var side=sim.Add(origin.Offset(1,0,0),IndustryId.ItemPipe);sim.Add(origin.Offset(1,0,1),IndustryId.ItemPipe);sim.Add(origin.Offset(0,0,1),IndustryId.ItemPipe);
                var source=new ItemContainer(27,Limit);world.Inventories[origin.Offset(2,0,0)]=source;source.Add(BlockId.Log,68);Mode(sim,side,0,PortRole.Output);
                Steps(sim,20);Check(furnace.Slots[0].Count+furnace.Slots[1].Count==4&&source.Total(BlockId.Log)==64,"Two furnace inlets share a single source throughput budget");
                Steps(sim,320);Check(furnace.Slots[0].Id==BlockId.Log&&furnace.Slots[1].Id==BlockId.Log&&source.Total(BlockId.Log)==0,"Same log supply reaches fuel and charcoal ingredients through distinct faces");
                furnace.Advance(200);Check(furnace.Slots[2].Id==BlockId.Charcoal,"Back-fed logs fuel charcoal production from side-fed logs");
            }
            // Existing contents drive requests before source iteration order chooses new cargo.
            for(int offset=0;offset<5;offset++)
            {
                var world=new World();var sim=new IndustrySimulation(world,Limit);Steps(sim,offset);
                var furnace=new FurnaceState(processing,Limit);var port=(IItemPipeInventory)furnace;
                port.TryInsert(BlockId.RawIron);port.TryInsert(BlockId.Coal);furnace.Advance(200);
                world.Inventories[origin]=furnace;var pipe=sim.Add(origin.Offset(1,0,0),IndustryId.ItemPipe);
                var chest=new ItemContainer(27,Limit);world.Inventories[origin.Offset(2,0,0)]=chest;
                chest.Add(IndustryId.CrushedCopper,8);chest.Add(IndustryId.CrushedIron,8);
                Mode(sim,pipe,0,PortRole.Output);Steps(sim,20);
                Check(furnace.Slots[0].Id==IndustryId.CrushedIron&&chest.Total(IndustryId.CrushedCopper)==8,"Furnace prefers ingredients matching existing iron output across source ordering "+offset);
                Check(furnace.Slots[0].Count+chest.Total(IndustryId.CrushedIron)==8,"Output preference preserves every iron ingredient");
            }
            {
                var world=new World();var sim=new IndustrySimulation(world,Limit);var crusher=sim.Add(origin,IndustryId.Crusher);crusher.Items.Add(IndustryId.CrushedGold,1,2,3);
                var pipe=sim.Add(origin.Offset(1,0,0),IndustryId.ItemPipe);var chest=new ItemContainer(27,Limit);world.Inventories[origin.Offset(2,0,0)]=chest;
                chest.Add(BlockId.RawCopper,5);chest.Add(BlockId.RawGold,5);Mode(sim,pipe,1,PortRole.Input);Mode(sim,pipe,0,PortRole.Output);Steps(sim,25);
                Check(crusher.Items.Slots[0].Id==BlockId.RawGold&&chest.Total(BlockId.RawCopper)==5,"Crusher prefers the raw ore matching its existing crushed output");
            }
            {
                var world=new World();var sim=new IndustrySimulation(world,Limit);var source=new ItemContainer(27,Limit);var dest=new ItemContainer(27,Limit);
                world.Inventories[origin]=source;world.Inventories[origin.Offset(2,0,0)]=dest;
                source.Add(BlockId.Dirt,5);source.Add(BlockId.Stone,5);dest.Add(BlockId.Stone,1);
                var pipe=sim.Add(origin.Offset(1,0,0),IndustryId.ItemPipe);Mode(sim,pipe,1,PortRole.Output);Steps(sim,20);
                Check(dest.Total(BlockId.Stone)==5&&dest.Total(BlockId.Dirt)==0&&source.Total(BlockId.Stone)==1,"Chest fills an existing item type first and still emits at most four per second");
                Steps(sim,15);Check(dest.Total(BlockId.Stone)==6&&dest.Total(BlockId.Dirt)>0,"Receiver falls back to other compatible cargo once preferred source is exhausted");
            }
            var other=new FluidDefinition("test:incompatible",200,199,7,5,false,3,1,1,0,0);
            foreach(byte target in new[]{IndustryId.Boiler,IndustryId.Pump,IndustryId.Tank})
            {
                var world=new World();var sim=new IndustrySimulation(world,Limit);var source=sim.Add(origin,IndustryId.Tank);source.Fluid.Deposit(other,1000);
                var pipe=sim.Add(origin.Offset(1,0,0),IndustryId.FluidPipe);var dest=sim.Add(origin.Offset(2,0,0),target);
                Mode(sim,pipe,1,PortRole.Output);Mode(sim,pipe,0,PortRole.Input);Steps(sim);
                Check(source.Fluid.Amount==1000&&dest.Fluid.Amount==0,"Incompatible fluid retained at source for device "+target);
                source.Fluid.Withdraw(1000);source.Fluid.Deposit(Fluids.Water,1000);Steps(sim);
                Check(source.WaterMl==0&&dest.WaterMl==1000,"Supported water moves without loss for device "+target);
            }
            Check(!PipeConnections.Accepts(new MachineState(origin,IndustryId.Crusher,Limit),Fluids.Water)&&!PipeConnections.Accepts(new MachineState(origin,IndustryId.TankPort,Limit),null),"Fluid capability rejects unsupported machines and null fluids");
            Directory.CreateDirectory("Logs/Connections");File.WriteAllText("Logs/Connections/item-pipe-checks.txt",$"PASS {checks} assertions\n"+log);
        }
    }
}
