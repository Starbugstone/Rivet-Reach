using System;
using System.IO;
using System.Linq;

namespace RivetReach.Editor
{
    public static class PortableStorageChecks
    {
        public static void Run()
        {
            int checks=0;void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
            var registry=ItemRegistry.Load();int Limit(byte id)=>registry.Get(id).stackLimit;
            foreach(var stack in new[]{new ItemStack(IndustryId.Battery,1){Energy=1234567},
                new ItemStack(IndustryId.Tank,1){FluidAmount=12345,FluidCapacity=100000,FluidId=Fluids.Water.Source},
                new ItemStack(IndustryId.Tank,1){FluidAmount=99999,FluidCapacity=100000,FluidId=Fluids.Lava.Source},
                new ItemStack(IndustryId.TankController,1){FluidAmount=250001,FluidCapacity=500000,FluidId=Fluids.Lava.Source}})
            {
                Check(stack.ValidContents,"Valid exact payload");
                var inventory=new Inventory(Limit);inventory.Add(stack.Id,2,0,1);Check(inventory.Add(stack)==0,"Insert filled item");
                Check(inventory.Slots[0].Count==2&&inventory.Slots[1].Equals(stack),"Filled item cannot merge with empty items");
                Check(inventory.Add(stack)==0&&inventory.Slots[2].Equals(stack),"Even identical filled items stay separate");
                var cursor=default(ItemStack);inventory.Click(1,ref cursor,true);Check(cursor.Equals(stack)&&inventory.Slots[1].Empty,"Right-click split retains payload");
                inventory.Click(0,ref cursor,true);Check(cursor.Equals(stack)&&inventory.Slots[0].Count==2,"Right merge into empty stack rejects");
                inventory.Click(0,ref cursor,false);Check(inventory.Slots[0].Equals(stack)&&cursor.Count==2&&!cursor.HasContents,"Left click swaps incompatible stacks");
                inventory.QuickTransfer(0);Check(inventory.Slots[Inventory.HotbarCount].Equals(stack),"Quick transfer keeps exact contents");
                var chest=new ItemContainer(2,Limit);inventory.TransferTo(Inventory.HotbarCount,chest);Check(chest.Slots[0].Equals(stack),"Chest transfer keeps exact contents");
                chest.Add(stack.Id,Limit(stack.Id),1,2);Check(chest.Add(stack)==1&&chest.Slots[0].Equals(stack),"Full container returns unchanged remainder");
                var machine=new MachineState(default,stack.Id,Limit);
                if(stack.Id==IndustryId.TankController)machine.Structure=new MultiblockInstance(Guid.NewGuid(),MultiblockDefinition.Tank,machine);
                PortableStorage.Restore(machine,stack);Check(PortableStorage.Capture(machine).Equals(stack),"Placement restores exact contents and capacity");
                PortableStorage.Drain(machine);Check(!PortableStorage.Capture(machine).HasContents,"Recovery removes original resource authority");
                using var bytes=new MemoryStream();using(var writer=new SaveWriter(bytes)){writer.Stack(stack);writer.Write(918);}
                bytes.Position=0;using(var reader=new SaveReader(bytes,registry)){Check(reader.Stack().Equals(stack)&&reader.ReadInt32()==918,"Schema 8 exact payload round trip");}
                var bad=stack;bad.Count=2;Check(!bad.ValidContents,"Reject stacked contents");
                using var invalid=new MemoryStream();using(var writer=new SaveWriter(invalid))writer.Stack(bad);invalid.Position=0;
                bool rejected=false;try{using var reader=new SaveReader(invalid,registry);reader.Stack();}catch(InvalidDataException){rejected=true;}Check(rejected,"Save loader rejects duplicated contents");
                Check(chest.EmptyContents(0)&&!chest.Slots[0].HasContents,"Explicit empty action discards payload");
                Check(!chest.EmptyContents(0),"Empty action is idempotent");
                var empty=chest.Take(0,1);chest.Take(1,1);chest.Click(1,ref empty,false);Check(empty.Empty&&chest.Slots[1].Count==Limit(stack.Id),"Emptied item stacks normally again");
                var session=new CraftingSession(RecipeCatalogAsset.Load().Compile(registry),4,Limit);session.Grid.Add(stack);Check(session.Preview==null,"Crafting cannot erase filled storage");
            }
            var tank=new MachineState(default,IndustryId.Tank,Limit);var boiler=new MachineState(default,IndustryId.Boiler,Limit);
            Check(PipeConnections.Accepts(tank,Fluids.Water)&&PipeConnections.Accepts(tank,Fluids.Lava)&&!PipeConnections.Accepts(boiler,Fluids.Lava),"Generic vessel accepts lava; boiler stays water-only");
            Check(tank.Fluid.Deposit(Fluids.Lava,10000)&&!tank.Fluid.Deposit(Fluids.Water,1)&&tank.Fluid.Amount==10000,"Different liquids never mix");
            Check(tank.Fluid.Withdraw(10000)&&tank.Fluid.Deposit(Fluids.Water,1),"Empty tank switches liquid type");
            for(int format=1;format<=7;format++)
            {using var stream=new MemoryStream();using(var writer=new SaveWriter(stream,format)){writer.Stack(new ItemStack(IndustryId.Tank,2));writer.Write(731);}stream.Position=0;using var reader=new SaveReader(stream,registry,format);var stack=reader.Stack();Check(stack.Count==2&&!stack.HasContents&&reader.ReadInt32()==731,"Legacy stack byte layout "+format);}
            Directory.CreateDirectory("Logs/PortableStorage");File.WriteAllText("Logs/PortableStorage/checks.txt",$"PASS {checks} portable storage assertions\n");
        }
    }
}
