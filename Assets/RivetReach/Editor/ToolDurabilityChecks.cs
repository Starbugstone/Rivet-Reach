using System;
using System.IO;
using System.Linq;
using System.Text;

namespace RivetReach.Editor
{
    public static class ToolDurabilityChecks
    {
        public static void Run()
        {
            var registry=ItemRegistry.Load();var rules=ToolDurability.Current;var report=new StringBuilder();int checks=0;
            void Check(bool ok,string message){if(!ok)throw new Exception("Tools: "+message);report.AppendLine("PASS "+message);checks++;}
            int Limit(byte id)=>registry.Get(id).stackLimit;
            var csv=new StringBuilder("tier,speed,uses,stone_seconds,log_seconds\n");
            float previousPick=float.PositiveInfinity,previousAxe=float.PositiveInfinity;int previousUses=0;
            for(int tier=0;tier<5;tier++)
            {
                byte axe=(byte)(BlockId.WoodAxe+tier*5),pick=(byte)(axe+1);
                float stone=registry.MiningSeconds(BlockId.Stone,new ItemStack(pick,1)),log=registry.MiningSeconds(BlockId.Log,new ItemStack(axe,1));
                int maximum=rules.Maximum(registry.Get(pick));
                Check(stone<previousPick&&log<previousAxe&&maximum>previousUses,"Tier "+(tier+1)+" improves pick speed, axe speed and durability");
                csv.AppendLine(FormattableString.Invariant($"{registry.Get(pick).tier},{registry.Get(pick).miningSpeed},{maximum},{stone},{log}"));
                previousPick=stone;previousAxe=log;previousUses=maximum;
                for(int kind=0;kind<5;kind++)
                {
                    byte id=(byte)(axe+kind);var bag=new Inventory(Limit);bag.Add(id,1);long revision=bag.Revision;
                    Check(rules.Maximum(registry.Get(id))==maximum,"All five tool types use the authored tier capacity");
                    bool exact=true;
                    for(int use=1;use<=maximum;use++)
                    {
                        exact &= bag.UseTool(0,maximum,out bool broken)&&broken==(use==maximum);
                        exact &= use==maximum?bag.Slots[0].Empty:bag.Slots[0].Wear==use;
                    }
                    Check(exact,"Every use and final break boundary for "+registry.Get(id).displayName+" ("+maximum+" uses)");
                    Check(bag.Revision==revision+maximum&&!bag.UseTool(0,maximum,out _),"Every use publishes one revision; broken tool cannot be used again");
                }
            }
            Check(float.IsPositiveInfinity(registry.MiningSeconds(BlockId.DiamondOre,new ItemStack(BlockId.WoodPickaxe,1))),"Durability preserves ore grade gates");
            Check(registry.MiningSeconds(BlockId.Log,new ItemStack(BlockId.DiamondPickaxe,1))==registry.MiningSeconds(BlockId.Log,default(ItemStack)),"Wrong tool does not gain wood speed");
            var worn=new ItemStack(BlockId.IronPickaxe,1){Wear=237};var fresh=new ItemStack(worn.Id,1);var inventory=new Inventory(Limit);inventory.Add(worn);inventory.Add(fresh);
            Check(!worn.Equals(fresh)&&!worn.CanStack(fresh)&&!worn.CanStack(worn),"Wear participates in identity and worn tools never merge");
            inventory.QuickTransfer(0);var chest=new ItemContainer(3,Limit);inventory.TransferTo(Inventory.HotbarCount,chest);
            Check(chest.Slots[0].Equals(worn),"Quick transfer and chest transfer retain exact wear");
            ItemStack cursor=default;chest.Click(0,ref cursor,true);chest.Click(2,ref cursor,false);chest.Organize();
            Check(cursor.Empty&&chest.Slots[0].Equals(worn),"Cursor, right-click and organizing retain exact wear");
            var crate=new CrateStorage(Limit);Check(crate.Insert(worn)==0&&crate.Count==0,"Bulk storage rejects worn tools without erasing wear");
            Check(!chest.EmptyContents(0)&&chest.Slots[0].Equals(worn),"Storage emptying cannot repair tools");
            for(int format=1;format<=SaveStore.Format;format++)
            {
                var expected=format<18?fresh:worn;using var bytes=new MemoryStream();using(var writer=new SaveWriter(bytes,format)){writer.Stack(expected);writer.Write(731);}
                bytes.Position=0;using var reader=new SaveReader(bytes,registry,format);
                Check(reader.Stack().Equals(expected)&&reader.ReadInt32()==731,"Stack layout and legacy pristine default schema "+format);
            }
            foreach(var bad in new[]{new ItemStack(worn.Id,1){Wear=-1},new ItemStack(worn.Id,1){Wear=rules.Maximum(worn,registry)},new ItemStack(BlockId.Dirt,1){Wear=1},new ItemStack(worn.Id,2){Wear=1}})
            {
                using var bytes=new MemoryStream();using(var writer=new SaveWriter(bytes))writer.Stack(bad);bytes.Position=0;bool rejected=false;
                try{using var reader=new SaveReader(bytes,registry);reader.Stack();}catch(InvalidDataException){rejected=true;}
                Check(rejected,"Reject corrupt or impossible tool wear "+bad.Id+":"+bad.Wear+":"+bad.Count);
            }
            using(var bytes=new MemoryStream()){bool rejected=false;try{using var writer=new SaveWriter(bytes,17);writer.Stack(worn);}catch(InvalidDataException){rejected=true;}Check(rejected,"Legacy writer cannot silently discard wear");}
            Check(rules.Maximum(registry.Get(IndustryId.Wrench))==256&&rules.Maximum(registry.Get(FishId.Rod))==128,"Utility tools have authored capacity");
            Directory.CreateDirectory("Logs/ToolDurability");File.WriteAllText("Logs/ToolDurability/domain-checks.txt",report+"Assertions: "+checks+"\n");File.WriteAllText("Logs/ToolDurability/tier-times.csv",csv.ToString());
        }
    }
}
