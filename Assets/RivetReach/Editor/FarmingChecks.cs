using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class FarmingChecks
    {
        sealed class World:IIndustryWorld
        {
            public bool Sleeping;
            public bool Ready(BlockPos p)=>!Sleeping;
            public byte Get(BlockPos p)=>0;
            public bool Remove(BlockPos p,byte expected)=>false;
            public ItemContainer Storage(BlockPos p)=>null;
            public byte Drop(byte id)=>id;
            public bool PlayerInside(BlockPos p)=>false;
        }
        public static void Run()
        {
            Directory.CreateDirectory("Logs/Farming");var lines=new List<string>();
            void Check(bool ok,string message){if(!ok)throw new Exception("Farming: "+message);lines.Add("PASS "+message);}
            FarmingCatalog.Load();var items=ItemRegistry.Load();var processing=ProcessingCatalogAsset.Load().Compile(items);var food=CookingCatalog.Current;
            foreach(var item in items.items)Check(items.HasTag(item.runtimeId,"edible")== (item.foodPoints>0),"Edible tag matches configured food: "+item.stableId);
            var carrot=items.Get(FarmId.Carrot);var tags=carrot.tags;
            try{carrot.tags=tags.Where(t=>t!="edible").ToArray();Check(items.FoodPoints(FarmId.Carrot)==0,"Food value without edible tag cannot authorize eating");}
            finally{carrot.tags=tags;}
            foreach(var crop in CropRules.Definitions)
            {
                for(byte stage=crop.first;stage<crop.Mature;stage++)
                {var drops=CropRules.Harvest(stage,0).ToArray();Check(drops.Sum(s=>s.Count)<=1&&drops.All(s=>s.Id==crop.planting),crop.key+" immature stage returns only planting stock, without multiplication");}
                for(uint random=0;random<10;random++)
                {var drops=CropRules.Harvest(crop.Mature,random).ToArray();Check(drops.Any(s=>s.Id==crop.produce&&s.Count>=crop.minYield)&& (crop.planting==0||drops.Any(s=>s.Id==crop.planting&&s.Count>=1)),crop.key+" mature resource and renewable planting stock, sample "+random);}
            }
            foreach(byte fuel in new[]{BlockId.Coal,BlockId.Charcoal,BlockId.Log,BlockId.Planks})Check(items.HasTag(fuel,"burnable")&&processing.FuelTicks(fuel)>0,"Tagged fuel has explicit duration: "+fuel);
            Check(!items.HasTag(BlockId.Stone,"burnable")&&processing.FuelTicks(BlockId.Stone)==0,"Inert material cannot supply energy");
            foreach(var recipe in food.recipes)foreach(byte cooker in new[]{FarmId.Cooker,FarmId.ElectricCooker})
            {
                var world=new World();var sim=new IndustrySimulation(world,id=>items.Get(id).stackLimit,processing);var p=new BlockPos(0,0,0);var m=sim.Add(p,cooker);m.SelectCooking(recipe.id);
                var battery=sim.Add(p.Offset(0,0,2),IndustryId.Battery);battery.EnergyCells[0].Charge(BatteryStorage.CellCapacity);sim.Add(p.Offset(0,0,1),IndustryId.PowerCable);
                int slot=0;foreach(var input in recipe.ingredients)m.Items.Add(food.Choices(input.selector)[0],input.count,slot,++slot);
                if(cooker==FarmId.Cooker)m.Items.Add(BlockId.Coal,1,3,4);
                for(int tick=0;tick<recipe.ticks-1;tick++)sim.Step();
                Check(m.Items.Slots[4].Empty&&m.Work==recipe.ticks-1,"No premature food: "+recipe.id+" on "+cooker);
                sim.Step();Check(m.Items.Slots.Take(3).All(s=>s.Empty)&&m.Items.Slots[4].Id==recipe.output&&m.Items.Slots[4].Count==recipe.count,"Exact recipe transaction: "+recipe.id+" on "+cooker);
                Check(cooker==FarmId.Cooker?m.BurnTicks==processing.FuelTicks(BlockId.Coal)-recipe.ticks&&battery.EnergyCells[0].Amount==BatteryStorage.CellCapacity:BatteryStorage.CellCapacity-battery.EnergyCells[0].Amount==recipe.ticks*food.electricWatts*50L,"Exact heat/electricity accounting: "+recipe.id+" on "+cooker);
                long energy=battery.EnergyCells[0].Amount;int heat=m.BurnTicks;for(int tick=0;tick<10;tick++)sim.Step();Check(energy==battery.EnergyCells[0].Amount&&heat==m.BurnTicks,"Idle cooker consumes nothing");
            }
            {
                var world=new World();var sim=new IndustrySimulation(world,id=>items.Get(id).stackLimit,processing);var p=new BlockPos(0,0,0);var m=sim.Add(p,FarmId.Cooker);m.SelectCooking("rivet:cook_vegetable_stew");
                var port=(IItemPipeInventory)m;
                Check(!port.TryInsert(BlockId.Log,0)&&port.TryInsert(BlockId.Log,4)&&!port.TryInsert(FarmId.Carrot,4),"Rear-only tagged fuel; ingredients reject rear");
                Check(port.TryInsert(FarmId.Carrot,0)&&port.TryInsert(BlockId.Potato,2)&&port.TryInsert(FarmId.Mushroom,5),"Mixed tag ingredients arrive on other faces");
                Check(food.Plan(m.FoodRecipe,m.Items.Slots).SequenceEqual(new[]{1,1,1}),"Mixed vegetables satisfy the same food recipe");
                for(int i=0;i<30;i++)sim.Step();double work=m.Work;int heat=m.BurnTicks;world.Sleeping=true;sim.Invalidate();for(int i=0;i<30;i++)sim.Step();
                Check(m.Work==work&&m.BurnTicks==heat&&m.Status==MachineStatus.Dormant,"Dormant cooker preserves work and heat");
                world.Sleeping=false;sim.Invalidate();m.Items.Add(FarmId.Stew,64,4,5);sim.Step();Check(m.Work==work&&m.BurnTicks==heat&&m.Status==MachineStatus.OutputFull,"Blocked output consumes neither ingredients nor heat");
                Check(port.Extract(0,64).Empty&&port.Extract(3,64).Empty&&port.Extract(4,64).Count==64,"Pipes extract only finished food");
                var lever=sim.Add(p.Offset(0,0,-1),IndustryId.Lever);sim.Step();Check(m.Work==work&&m.BurnTicks==heat&&m.Status==MachineStatus.DisabledBySignal,"Signal OFF pauses cooking");
                sim.Activate(lever);sim.Step();Check(m.Work==work+1,"Signal ON resumes");
                m.SelectCooking("rivet:cook_bread");Check(m.Work==0&&m.Items.Slots.Take(3).Sum(s=>s.Count)==3,"Recipe change clears work and preserves ingredients");
            }
            {
                var sim=new IndustrySimulation(new World(),id=>items.Get(id).stackLimit,processing);var p=new BlockPos(0,0,0);var m=sim.Add(p,FarmId.ElectricCooker);m.Items.Add(FarmId.Grain,3,0,1);
                var crank=sim.Add(p.Offset(0,0,2),IndustryId.HandCrank);sim.Add(p.Offset(0,0,1),IndustryId.PowerCable);sim.Step();sim.TryCrank(crank);for(int i=0;i<10;i++)sim.Step();
                Check(m.Work==5&&m.Items.Slots[0].Count==3,"Half electricity gives half speed without early consumption");
                sim.Step();Check(m.Work==5&&m.Status==MachineStatus.NoPower,"No electricity retains partial work");
                m.Items.Take(0,64);Check(!((IItemPipeInventory)m).TryInsert(BlockId.Coal,4)&&((IItemPipeInventory)m).TryInsert(FarmId.Grain,4),"Electric cooker has rear ingredient input and no fuel slot");
            }
            var browser=new RecipeBrowserIndex(items,RecipeCatalogAsset.Load().Compile(items),processing);
            Check(browser.Find(FarmId.Carrot,true).Any(r=>r.FoodRecipe?.id=="rivet:cook_vegetable_stew")&&browser.Find(BlockId.Apple,true).Any(r=>r.FoodRecipe?.id=="rivet:cook_fruit_porridge"),"Recipe uses include every food tag alternative");
            Check(browser.Recipes.Count(r=>r.FoodRecipe!=null)==food.recipes.Length*2,"Both cookers expose every food recipe");
            var generated=new TerrainGenerator(219);
            var species=new HashSet<string>();bool immature=false;
            for(int z=-320;z<320;z++)for(int x=-320;x<320;x++)
            {var p=new BlockPos(x,generated.Height(x,z)+1,z);byte id=generated.At(p);var c=CropRules.For(id);if(c!=null){species.Add(c.key);immature|=id<c.Mature;}}
            Check(species.Count==6&&immature,"New terrain discovers all six species and immature stages: "+string.Join(",",species));
            foreach(string version in new[]{TerrainGenerator.Version,TerrainGenerator.LavaVersion,TerrainGenerator.LegacyVersion})
            {
                var g=new TerrainGenerator(219,version);var p=new BlockPos(40,g.Height(40,40),40);var chunk=p.Chunk;var cells=g.Generate(chunk);
                for(int z=0;z<32;z+=3)for(int x=0;x<32;x+=3)for(int y=0;y<32;y+=3)Check(g.At(chunk.Min.Offset(x,y,z))==cells[ChunkMesher.Index(x,y,z)],"Point/chunk agreement "+version);
            }
            File.WriteAllLines("Logs/Farming/checks.txt",new[]{"PASS "+lines.Count+" assertions"}.Concat(lines));
        }
    }
}
