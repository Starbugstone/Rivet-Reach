using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class HandCrankChecks
    {
        sealed class World : IIndustryWorld
        {
            public readonly Dictionary<BlockPos,byte> Blocks=new Dictionary<BlockPos,byte>();
            public readonly HashSet<BlockPos> Sleeping=new HashSet<BlockPos>();
            public bool Ready(BlockPos p)=>!Sleeping.Contains(p);
            public byte Get(BlockPos p)=>Blocks.TryGetValue(p,out byte id)?id:(byte)0;
            public bool Remove(BlockPos p,byte id)=>Blocks.Remove(p);
            public ItemContainer Storage(BlockPos p)=>null;
            public byte Drop(byte id)=>id;
            public bool PlayerInside(BlockPos p)=>false;
        }
        public static void Run()
        {
            var report=new StringBuilder();int count=0;
            void Check(bool ok,string message){if(!ok)throw new Exception("Hand crank: "+message);count++;report.AppendLine("PASS "+message);}
            var world=new World();var sim=new IndustrySimulation(world,id=>64);
            MachineState Add(int x,int z,byte id){var p=new BlockPos(x,0,z);world.Blocks[p]=id;return sim.Add(p,id);}
            void Steps(int n){for(int i=0;i<n;i++)sim.Step();}
            void Settle(){for(int i=0;i<100;i++){sim.Step();if(!sim.Rebuilding&&sim.Multiblocks.PendingCount==0)return;}throw new Exception("Topology stalled");}
            var battery=Add(0,1,IndustryId.Battery);var crank=Add(0,0,IndustryId.HandCrank);
            Check(!sim.TryCrank(crank),"Topology rebuild rejects activation");Settle();Steps(20);
            Check(BatteryPower.Amount(battery)==0,"Idle crank and new battery produce no free energy");
            Check(sim.TryCrank(crank),"One manual stroke accepted");
            for(int i=0;i<10;i++){Check(!sim.TryCrank(crank),"Repeat cannot stack paid stroke at tick "+i);sim.Step();}
            Check(BatteryPower.Amount(battery)==50000,"One turn stores exactly 50 J over ten 20 Hz steps");Steps(30);
            Check(BatteryPower.Amount(battery)==50000&&crank.SupplyWatts==0,"Released crank stops and retains exact charge");
            for(int i=0;i<40;i++){sim.TryCrank(crank);sim.Step();}
            Check(BatteryPower.Amount(battery)==250000,"Continuous activation supplies at most 100 W");
            foreach(var mode in new[]{BatteryMode.Isolated,BatteryMode.DischargeOnly})
            {battery.BatteryMode=mode;sim.TryCrank(crank);Steps(10);Check(BatteryPower.Amount(battery)==250000,"Manual generation obeys "+mode);}
            battery.BatteryMode=BatteryMode.ChargeOnly;sim.TryCrank(crank);Steps(10);Check(BatteryPower.Amount(battery)==300000,"Charge-only accepts manual generation");
            battery.EnergyCells[0].Charge(BatteryStorage.CellCapacity-BatteryPower.Amount(battery)-50);sim.TryCrank(crank);Steps(10);
            Check(BatteryPower.Amount(battery)==BatteryStorage.CellCapacity,"Near-full battery clamps without overflowing");
            battery.EnergyCells[0].Discharge(BatteryPower.Amount(battery));sim.Rotate(crank);Settle();sim.TryCrank(crank);Steps(10);
            Check(BatteryPower.Amount(battery)==0,"Incorrectly facing rear socket cannot transfer power");
            for(int i=0;i<3;i++)sim.Rotate(crank);Settle();
            var lamp=Add(1,1,IndustryId.Lamp);for(int i=0;i<3;i++)sim.Rotate(lamp);Settle();sim.TryCrank(crank);Steps(10);
            Check(lamp.ReceivedWatts==20&&BatteryPower.Amount(battery)==40000,"Lamp receives 20 W before 80 W surplus charges battery");
            world.Sleeping.Add(crank.Position);sim.Invalidate();Settle();
            Check(!sim.TryCrank(crank),"Dormant crank rejects use");long before=BatteryPower.Amount(battery);Steps(20);Check(before==BatteryPower.Amount(battery),"Dormant generator earns no offline charge");
            world.Sleeping.Clear();sim.Invalidate();Settle();
            Check(sim.Remove(crank.Position)==crank&&!sim.TryCrank(crank),"Removed crank cannot be activated through stale reference");
            // Existing bank endpoint rules apply unchanged to the new generator.
            var controller=Add(10,1,IndustryId.BatteryController);var cell=Add(10,2,IndustryId.Battery);var bankCrank=Add(10,0,IndustryId.HandCrank);Settle();
            Check(controller.Structure.Formed,"Minimal battery pack forms next to crank");sim.TryCrank(bankCrank);Steps(10);
            Check(BatteryPower.Amount(controller)==50000&&cell.EnergyCells[0].Amount==50000,"Crank charges bank only through outward controller socket");
            controller.BatteryMode=BatteryMode.Isolated;sim.TryCrank(bankCrank);Steps(10);Check(BatteryPower.Amount(controller)==50000,"Bank mode blocks crank transfers");
            // Real catalogs: recipe dependency and save compatibility checks.
            var items=ItemRegistry.Load();var catalog=RecipeCatalogAsset.Load();var recipe=catalog.recipes.Single(r=>r.stableId=="rivet:industry_170");
            Check(recipe.minimumGridSize==3&&recipe.output.count==1&&recipe.ingredients.Length==4,"Crank is a four-ingredient workbench recipe");
            Check(recipe.ingredients.All(c=>new[]{BlockId.CopperIngot,BlockId.IronIngot,BlockId.Stick,BlockId.Planks}.Contains(items.ResolveId(c.itemId))),"Recipe needs only ordinary early-game materials");
            var store=new SaveStore("unused",items);var legacyItems=ScriptableObject.CreateInstance<ItemRegistry>();legacyItems.items=items.items.Where(i=>i.runtimeId!=IndustryId.HandCrank).ToArray();
            var original=catalog.recipes;
            try
            {
                catalog.recipes=original.Where(r=>r!=recipe).ToArray();var legacy=new SaveStore("unused",legacyItems);
                var entry=new SaveEntry{Id=Guid.NewGuid().ToString("N"),WorldId=Guid.NewGuid().ToString("N"),Name="Legacy crank compatibility",UtcTicks=DateTime.UtcNow.Ticks,Seed=17};
                byte[] bytes=legacy.Encode(entry,w=>w.Write(314));
                using(var reader=store.Open(bytes,out var restored))Check(reader.ReadInt32()==314&&restored.Seed==17,"Pre-crank save fingerprint loads without rewriting old state");
                legacyItems.items=legacyItems.items.Select(i=>JsonUtility.FromJson<ItemDefinition>(JsonUtility.ToJson(i))).ToArray();legacyItems.items[0].attackDamage++;
                var incompatible=new SaveStore("unused",legacyItems);bytes=incompatible.Encode(entry,w=>w.Write(314));bool rejected=false;
                try{using var reader=store.Open(bytes,out _);}catch(InvalidDataException){rejected=true;}
                Check(rejected,"Unrelated legacy definition changes are still rejected");
                catalog.recipes=original;
                using(var reader=store.Open(store.Encode(entry,w=>w.Write(271)),out _))Check(reader.ReadInt32()==271,"Current content save round-trips");
            }
            finally{catalog.recipes=original;UnityEngine.Object.DestroyImmediate(legacyItems);}
            report.AppendLine("Assertions: "+count);File.WriteAllText("Logs/hand-crank-checks.txt",report.ToString());
        }
    }
}
