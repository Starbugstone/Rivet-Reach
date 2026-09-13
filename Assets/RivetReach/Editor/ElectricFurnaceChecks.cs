using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class ElectricFurnaceChecks
    {
        sealed class World : IIndustryWorld
        {
            public readonly HashSet<BlockPos> Sleeping=new HashSet<BlockPos>();
            public bool Ready(BlockPos p)=>!Sleeping.Contains(p);
            public byte Get(BlockPos p)=>0;
            public bool Remove(BlockPos p,byte expected)=>false;
            public ItemContainer Storage(BlockPos p)=>null;
            public byte Drop(byte block)=>block;
            public bool PlayerInside(BlockPos p)=>false;
        }
        public static void Run()
        {
            var lines=new List<string>();
            void Check(bool ok,string message){if(!ok)throw new Exception("Electric furnace: "+message);lines.Add("PASS "+message);}
            var items=ItemRegistry.Load();var processing=ProcessingCatalogAsset.Load().Compile(items);
            var recipes=RecipeCatalogAsset.Load().Compile(items);var browser=new RecipeBrowserIndex(items,recipes,processing);
            var electric=browser.Recipes.Where(r=>r.Station==IndustryId.ElectricFurnace).ToArray();
            Check(electric.Length==processing.Recipes.Count,"Every normal furnace recipe is discoverable on the electric furnace");
            Check(electric.All(r=>r.Watts==200&&r.Fuels.Count==0),"Browser describes electricity with no item fuel");
            Check(recipes.Recipes.Single(r=>r.Output.Id==IndustryId.ElectricFurnace).MinimumGridSize==4,"Upgrade requires the Machinist bench");
            foreach(var recipe in processing.Recipes)
            {
                var world=new World();var sim=new IndustrySimulation(world,id=>items.Get(id).stackLimit,processing);
                var p=new BlockPos(0,0,0);var m=sim.Add(p,IndustryId.ElectricFurnace);
                var battery=sim.Add(p.Offset(0,0,2),IndustryId.Battery);battery.EnergyCells[0].Charge(BatteryStorage.CellCapacity);
                sim.Add(p.Offset(0,0,1),IndustryId.PowerCable);
                m.Items.Add(recipe.Input.Id,recipe.Input.Count,0,1);sim.Step();
                Check(m.ReceivedWatts==200&&m.Work==1,"Full power advances one tick: "+recipe.Id);
                for(int i=1;i<recipe.Ticks-1;i++)sim.Step();
                Check(m.Items.Slots[2].Empty&&m.Items.Slots[0].Count==recipe.Input.Count,"No early output or consumed input: "+recipe.Id);
                sim.Step();
                Check(m.Items.Slots[0].Empty&&m.Items.Slots[2].Id==recipe.Output.Id&&m.Items.Slots[2].Count==recipe.Output.Count,"Exact shared output: "+recipe.Id);
                Check(BatteryStorage.CellCapacity-battery.EnergyCells[0].Amount==recipe.Ticks*10000L&&m.BurnTicks==0&&m.Items.Slots[1].Empty,"Exact 200 W energy and no item fuel: "+recipe.Id);
                sim.Step();Check(m.RequestedWatts==0,"Empty input requests no power: "+recipe.Id);
            }
            {
                var world=new World();var sim=new IndustrySimulation(world,id=>64,processing);var p=new BlockPos(0,0,0);
                var m=sim.Add(p,IndustryId.ElectricFurnace);var battery=sim.Add(p.Offset(0,0,2),IndustryId.Battery);
                battery.EnergyCells[0].Charge(BatteryStorage.CellCapacity);sim.Add(p.Offset(0,0,1),IndustryId.PowerCable);
                var port=(IItemPipeInventory)m;
                for(int f=0;f<6;f++)Check(port.TryInsert(BlockId.Log,f),"Logs accepted only as ingredients on face "+f);
                Check(m.Items.Slots[0].Count==6&&m.Items.Slots[1].Empty&&!m.Accepts(1,BlockId.Coal),"No fuel slot, including dual-purpose logs");
                Check(!port.TryInsert(BlockId.Coal)&&!port.TryInsert(BlockId.Dirt),"Reject fuel-only and incompatible cargo");
                m.Items.Take(0,64);m.Items.Add(BlockId.RawIron,2,0,1);
                for(int i=0;i<151;i++)sim.Step();double work=m.Work;long energy=battery.EnergyCells[0].Amount;
                sim.Remove(p.Offset(0,0,1));for(int i=0;i<20;i++)sim.Step();
                Check(m.Work==work&&m.Status==MachineStatus.NoPower&&battery.EnergyCells[0].Amount==energy,"Disconnected power preserves partial work and battery");
                sim.Add(p.Offset(0,0,1),IndustryId.PowerCable);sim.Step();Check(m.Work==work+1,"Cable replacement resumes work");
                var lever=sim.Add(p.Offset(0,0,-1),IndustryId.Lever);sim.Step();work=m.Work;energy=battery.EnergyCells[0].Amount;
                for(int i=0;i<10;i++)sim.Step();Check(m.Status==MachineStatus.DisabledBySignal&&m.Work==work&&battery.EnergyCells[0].Amount==energy,"OFF signal stops work and demand");
                sim.Activate(lever);sim.Step();Check(m.Work==work+1,"ON signal resumes work");
                m.Items.Add(BlockId.IronIngot,64,2,3);sim.Step();work=m.Work;energy=battery.EnergyCells[0].Amount;
                for(int i=0;i<10;i++)sim.Step();Check(m.Status==MachineStatus.OutputFull&&m.RequestedWatts==0&&m.Work==work&&battery.EnergyCells[0].Amount==energy,"Full output uses no electricity");
                Check(port.Prefers(IndustryId.CrushedIron,4)&&!port.Prefers(BlockId.RawCopper,4),"Pipe preference matches retained product");
                Check(!port.CanExtract(0)&&port.CanExtract(2)&&port.Extract(0,1).Empty,"Pipes can extract only completed output");
                port.Extract(2,64);sim.Step();Check(m.Work==work+1,"Output extraction resumes work");
                world.Sleeping.Add(p);sim.Invalidate();sim.Step();work=m.Work;energy=battery.EnergyCells[0].Amount;
                for(int i=0;i<10;i++)sim.Step();Check(m.Status==MachineStatus.Dormant&&m.Work==work&&battery.EnergyCells[0].Amount==energy,"Dormant furnace receives no offline credit");
                world.Sleeping.Clear();sim.Invalidate();sim.Step();Check(m.Work==work+1,"Residency resumes work");
                m.Items.Take(0,64);m.Items.Add(BlockId.Potato,1,0,1);sim.Step();Check(m.Work==1,"Changing ingredients resets old recipe progress");
            }
            {
                var sim=new IndustrySimulation(new World(),id=>64,processing);var p=new BlockPos(0,0,0);
                var m=sim.Add(p,IndustryId.ElectricFurnace);sim.Add(p.Offset(0,0,1),IndustryId.PowerCable);
                var crank=sim.Add(p.Offset(0,0,2),IndustryId.HandCrank);m.Items.Add(BlockId.RawIron,1,0,1);sim.Step();
                Check(sim.TryCrank(crank),"Start limited 100 W source");for(int i=0;i<10;i++)sim.Step();
                Check(m.Work==5&&m.Status==MachineStatus.Underpowered&&m.Items.Slots[0].Count==1,"Half power gives half speed and preserves input");
                sim.Step();Check(m.Work==5&&m.Status==MachineStatus.NoPower,"Exhausted source pauses fractional progress");
            }
            for(int face=0;face<6;face++)for(int rotation=0;rotation<4;rotation++)
            {
                var world=new World();var sim=new IndustrySimulation(world,id=>64,processing);var p=new BlockPos(0,0,0);
                var m=sim.Add(p,IndustryId.ElectricFurnace);for(int r=0;r<rotation;r++)sim.Rotate(m);
                var cable=IndustryDefinition.Neighbor(p,face);var bp=IndustryDefinition.Neighbor(cable,face);
                sim.Add(cable,IndustryId.PowerCable);var battery=sim.Add(bp,IndustryId.Battery);battery.EnergyCells[0].Charge(BatteryStorage.CellCapacity);
                m.Items.Add(BlockId.RawIron,1,0,1);sim.Step();Check(m.ReceivedWatts==200,"All-face electrical connection, face "+face+", rotation "+rotation);
            }
            // A variable-duration/count fixture catches accidental hard-coded 1:1 / 200-tick behavior.
            {
                var custom=ProcessingRegistry.Compile(new[]{new ProcessingSpec{Id="test:batch",Input="rivet:raw_iron",Output="rivet:iron_ingot",InputCount=2,OutputCount=3,Ticks=7}},Array.Empty<FuelSpec>(),items.ResolveId,id=>64);
                var sim=new IndustrySimulation(new World(),id=>64,custom);var p=new BlockPos(0,0,0);var m=sim.Add(p,IndustryId.ElectricFurnace);
                var battery=sim.Add(p.Offset(0,0,2),IndustryId.Battery);battery.EnergyCells[0].Charge(BatteryStorage.CellCapacity);sim.Add(p.Offset(0,0,1),IndustryId.PowerCable);
                m.Items.Add(BlockId.RawIron,1,0,1);sim.Step();Check(m.RequestedWatts==0,"Incomplete multi-item recipe consumes no electricity");
                m.Items.Add(BlockId.RawIron,1,0,1);for(int i=0;i<7;i++)sim.Step();Check(m.Items.Slots[0].Empty&&m.Items.Slots[2].Count==3&&m.Work==0,"Registry owns batch amounts and duration");
            }
            Check(RecipeTransferChecks.Run(items,recipes)>0,"All registered crafting recipes transfer and conserve inputs, including the furnace upgrade");
            Compatibility(items,Check);
            File.WriteAllLines("Logs/electric-furnace-checks.txt",new[]{"PASS "+lines.Count+" assertions"}.Concat(lines));
        }
        static void Compatibility(ItemRegistry items,Action<bool,string> check)
        {
            var store=new SaveStore("unused",items);var old=ScriptableObject.CreateInstance<ItemRegistry>();var catalog=RecipeCatalogAsset.Load();var original=catalog.recipes;
            try
            {
                old.items=items.items.Where(i=>i.runtimeId!=IndustryId.ElectricFurnace).Select(i=>JsonUtility.FromJson<ItemDefinition>(JsonUtility.ToJson(i))).ToArray();
                catalog.recipes=original.Where(r=>r.stableId!="rivet:industry_174").ToArray();
                var entry=new SaveEntry{Id=Guid.NewGuid().ToString("N"),WorldId=Guid.NewGuid().ToString("N"),Name="Pre-electric content",UtcTicks=DateTime.UtcNow.Ticks};
                var data=new SaveStore("unused",old).Encode(entry,w=>w.Write(314));using(var r=store.Open(data,out _))check(r.ReadInt32()==314,"Pre-electric current-schema content remains compatible");
                old.items[0].attackDamage++;data=new SaveStore("unused",old).Encode(entry,w=>w.Write(314));bool rejected=false;
                try{using var r=store.Open(data,out _);}catch(InvalidDataException){rejected=true;}check(rejected,"Unrelated old content changes still reject");
            }
            finally{catalog.recipes=original;UnityEngine.Object.DestroyImmediate(old);}
        }
    }
}
