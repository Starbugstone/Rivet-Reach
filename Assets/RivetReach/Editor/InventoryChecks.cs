using System;
using System.IO;
using System.Linq;

namespace RivetReach.Editor
{
    public static class InventoryChecks
    {
        public static void Run()
        {
            int checks=0;
            void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
            var registry=ItemRegistry.Load();
            Inventory Empty()=>new Inventory(id=>registry.Get(id).stackLimit);
            Check(Inventory.HotbarCount==15&&Inventory.MainCount==56&&Inventory.SlotCount==71,"Requested inventory capacity");
            for(int format=1;format<=SaveStore.Format;format++)
            {
                bool legacy=format<6;int count=legacy?60:71;
                var stacks=Enumerable.Range(0,count).Select(i=>new ItemStack(i%2==0?BlockId.Dirt:BlockId.Planks,1+i%64)).ToArray();
                using var stream=new MemoryStream();
                using(var writer=new SaveWriter(stream,format)){writer.Slots(stacks);writer.Write(1729);}
                stream.Position=0;var inventory=Empty();
                using var reader=new SaveReader(stream,registry,format);reader.PlayerInventory(inventory);
                for(int i=0;i<count;i++)
                {
                    int slot=legacy&&i>=12?i+3:i;
                    Check(inventory.Slots[slot].Id==stacks[i].Id&&inventory.Slots[slot].Count==stacks[i].Count,"Exact saved slot migration, schema "+format);
                }
                if(legacy)Check(inventory.Slots.Skip(12).Take(3).All(s=>s.Empty)&&inventory.Slots.Skip(63).All(s=>s.Empty),"Legacy expansion slots start empty");
                Check(reader.ReadInt32()==1729,"Inventory reader preserves the next section");
            }
            foreach(var test in new[]{(format:5,count:71),(format:6,count:60),(format:6,count:72)})
            {
                using var stream=new MemoryStream();using(var writer=new SaveWriter(stream))writer.Write(test.count);
                stream.Position=0;using var reader=new SaveReader(stream,registry,test.format);
                bool rejected=false;try{reader.PlayerInventory(Empty());}catch(InvalidDataException){rejected=true;}
                Check(rejected,"Reject wrong inventory layout for schema");
            }
            var full=Empty();Check(full.Add(BlockId.Dirt,71*64+1)==1,"Overflow after all 71 slots");
            full.Take(70,64);full.QuickTransfer(14);
            Check(full.Slots[14].Empty&&full.Slots[70].Count==64,"Last hotbar slot transfers to last backpack slot");
            full.QuickTransfer(70);
            Check(full.Slots[70].Empty&&full.Slots[14].Count==64&&full.Total(BlockId.Dirt)==70*64,"Reverse transfer conserves contents");
            File.WriteAllText("Logs/inventory-checks.txt","PASS: "+checks+" assertions; schemas 1–6, exact slots, malformed layouts, capacity and boundary transfers.");
        }
    }
}
