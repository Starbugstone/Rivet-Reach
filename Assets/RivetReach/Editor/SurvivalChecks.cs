using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class SurvivalChecks
    {
        static int checks;
        static void Check(bool condition,string message){checks++;if(!condition)throw new Exception("Survival check failed: "+message);}
        static void Put(ItemContainer container,int slot,byte id,int count=1){Check(container.Add(id,count,slot,slot+1)==0,"Fixture fits");}
        static void Put(FurnaceState furnace,int slot,byte id,int count=1){var held=new ItemStack(id,count);furnace.Click(slot,ref held,false);Check(held.Empty,"Furnace fixture fits");}
        static void Reject(Action action,string message){try{action();}catch(ArgumentException){checks++;return;}throw new Exception(message);}
        [UnityEditor.MenuItem("Rivet Reach/Validate survival progression")]
        public static void Run()
        {
            checks=0;var items=ItemRegistry.Load();var recipes=RecipeCatalogAsset.Load().Compile(items);var processing=ProcessingCatalogAsset.Load().Compile(items);
            int Limit(byte id)=>items.Get(id).stackLimit;
            FurnaceState Furnace()=>new FurnaceState(processing,Limit);
            foreach(var tier in new[]{ToolTier.Wood,ToolTier.Stone,ToolTier.Copper,ToolTier.Iron,ToolTier.Diamond})
            {
                byte pick=(byte)(BlockId.WoodPickaxe+((int)tier-1)*5);var held=new ItemStack(pick,1);
                Check(items.Tier(held)==tier&&items.Capabilities(held)==ToolCapability.Pickaxe,"Each authored pick has its tier and capability");
                Check(items.Get((byte)(pick+1)).attackDamage==new[]{4,5,5,6,7}[(int)tier-1],"Authored sword damage follows its material tier");
                Check(BlockId.Mineable(BlockId.Stone,ToolCapability.Pickaxe,tier),"Every tier bootstraps stone");
                Check(BlockId.Mineable(BlockId.IronOre,ToolCapability.Pickaxe,tier)==(tier>=ToolTier.Stone),"Iron extraction gate");
                Check(BlockId.Mineable(BlockId.DiamondOre,ToolCapability.Pickaxe,tier)==(tier>=ToolTier.Iron),"Diamond extraction gate");
                Check(!BlockId.Mineable(BlockId.Bedrock,ToolCapability.Pickaxe,tier),"All tiers preserve bedrock");
                if(tier>ToolTier.Wood)Check(items.MiningSeconds(BlockId.Stone,held)<items.MiningSeconds(BlockId.Stone,new ItemStack((byte)(pick-5),1)),"Higher tiers mine faster");
            }
            Check(!BlockId.Mineable(BlockId.Stone,ToolCapability.None,ToolTier.None)&&BlockId.Mineable(BlockId.Log,ToolCapability.None,ToolTier.None),"Bare hands gather wood before stone");
            Bootstrap(items,recipes,processing);
            checks+=StarterRecipeChecks.Run(items,recipes);
            var furnace=Furnace();Put(furnace,0,BlockId.RawIron,8);Put(furnace,1,BlockId.Coal);
            furnace.Advance(199);Check(furnace.Slots[2].Empty&&furnace.Slots[0].Count==8&&furnace.BurnTicks==1401,"No early output or early input consumption");
            furnace.Advance(1);Check(furnace.Slots[2].Count==1&&furnace.Slots[0].Count==7,"Exactly one result at recipe boundary");
            furnace.Advance(1400);Check(furnace.Slots[2].Count==8&&furnace.Slots[0].Empty&&furnace.BurnTicks==0&&furnace.Slots[1].Empty,"One coal smelts eight items exactly");
            furnace=Furnace();Put(furnace,0,BlockId.RawCopper,2);Put(furnace,1,BlockId.Planks);furnace.Advance(300);
            Check(furnace.Slots[2].Count==1&&furnace.ProgressTicks==100&&furnace.Slots[0].Count==1,"Partial fuel work remains in furnace");
            Put(furnace,1,BlockId.Stick);furnace.Advance(100);Check(furnace.Slots[2].Count==2&&furnace.BurnTicks==0,"Fuel items combine without rounding credits");
            furnace=Furnace();Put(furnace,0,BlockId.RawIron,64);Put(furnace,1,BlockId.Coal,9);furnace.Advance(12800);
            Put(furnace,0,BlockId.RawIron);furnace.Advance(200);
            Check(furnace.Slots[2].Count==64&&furnace.Slots[0].Count==1&&furnace.Slots[1].Count==1,"Full output never starts fresh fuel or consumes input");
            var bad=new ItemStack(BlockId.Dirt,1);furnace.Click(2,ref bad,false);Check(bad.Count==1&&furnace.Slots[2].Id==BlockId.IronIngot,"Output slot rejects insertion");
            furnace.Click(0,ref bad,false);Check(bad.Id==BlockId.Dirt&&furnace.Slots[0].Id==BlockId.RawIron,"Input filter rejects uncookable material");
            furnace.Click(1,ref bad,false);Check(furnace.Slots[1].Id==BlockId.Coal,"Fuel filter rejects dirt");
            var full=new Inventory(Limit);full.Add(BlockId.Dirt,64*60);furnace.TransferOut(2,full);Check(furnace.Slots[2].Count==64,"Full inventory cannot delete furnace output");
            full.Take(0,1);furnace.TransferOut(2,full);Check(furnace.Slots[2].Count==64,"Incompatible partial space cannot take output");
            full.Take(0,64);furnace.TransferOut(2,full);Check(furnace.Slots[2].Empty&&full.Total(BlockId.IronIngot)==64,"Output transfer conserves complete stacks");
            furnace.Advance(1);furnace.Take(0,1);furnace.Advance(1599);Check(furnace.BurnTicks==0&&furnace.ProgressTicks==0,"Lit fuel burns when input is removed, with work reset");
            foreach(var recipe in processing.Recipes)
            {
                furnace=Furnace();Put(furnace,0,recipe.Input.Id,recipe.Input.Count);Put(furnace,1,BlockId.Coal);furnace.Advance(recipe.Ticks);
                Check(furnace.Slots[2].Id==recipe.Output.Id&&furnace.Slots[2].Count==recipe.Output.Count&&furnace.Slots[0].Empty,"Authored processing recipe: "+recipe.Id);
            }
            var random=new System.Random(612);
            for(int trial=0;trial<200;trial++)
            {
                var a=Furnace();var b=Furnace();Put(a,0,BlockId.RawIron,64);Put(b,0,BlockId.RawIron,64);Put(a,1,BlockId.Coal,8);Put(b,1,BlockId.Coal,8);
                int remaining=random.Next(1,15000),total=remaining;a.Advance(total);
                while(remaining>0){int step=Math.Min(remaining,random.Next(1,73));b.Advance(step);remaining-=step;Check(b.Slots[0].Count+b.Slots[2].Count==64,"Partitioned processing conserves ore");}
                Check(a.Slots.SequenceEqual(b.Slots)&&a.BurnTicks==b.BurnTicks&&a.ProgressTicks==b.ProgressTicks,"Large and subdivided time advances agree");
            }
            var spec=new ProcessingSpec{Id="test",Input="rivet:raw_iron",Output="rivet:iron_ingot"};
            var fuels=new[]{new FuelSpec("rivet:coal",1600)};
            Reject(()=>ProcessingRegistry.Compile(new[]{spec,spec},fuels,items.ResolveId,Limit),"Duplicate input accepted");
            Reject(()=>ProcessingRegistry.Compile(new[]{spec},new[]{fuels[0],fuels[0]},items.ResolveId,Limit),"Duplicate fuel accepted");
            spec.Ticks=0;Reject(()=>ProcessingRegistry.Compile(new[]{spec},fuels,items.ResolveId,Limit),"Zero duration accepted");
            spec.Ticks=200;spec.InputCount=65;Reject(()=>ProcessingRegistry.Compile(new[]{spec},fuels,items.ResolveId,Limit),"Oversized ingredient accepted");
            HealthAndArmor(items);
            var clock=Stopwatch.StartNew();var machines=new FurnaceState[1000];
            for(int i=0;i<machines.Length;i++){machines[i]=Furnace();Put(machines[i],0,BlockId.RawIron,64);Put(machines[i],1,BlockId.Coal,8);}
            clock.Restart();long before=GC.GetAllocatedBytesForCurrentThread();
            for(int tick=0;tick<200;tick++)foreach(var machine in machines)machine.Advance(1);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;clock.Stop();
            string report=$"PASS: {checks} assertions. {recipes.Recipes.Count} crafting recipes, {processing.Recipes.Count} furnace recipes; bootstrap graph, tier gates, furnace boundaries/filters/conservation/time partition equivalence, hunger, health, equipment.\n1000 active furnaces × 200 one-tick advances: {clock.Elapsed.TotalMilliseconds:F3} ms total; {allocated} managed bytes in measured loop.\n";
            File.WriteAllText("Logs/survival-checks.txt",report);UnityEngine.Debug.Log(report);
        }
        static void Bootstrap(ItemRegistry items,RecipeRegistry registry,ProcessingRegistry processing)
        {
            var reachable=new System.Collections.Generic.HashSet<byte>{BlockId.Log,BlockId.Dirt,BlockId.Grass,BlockId.Potato,BlockId.Sand};
            bool changed;
            do
            {
                changed=false;int grid=reachable.Contains(IndustryId.Bench)?4:reachable.Contains(BlockId.Workbench)?3:2;
                foreach(var recipe in registry.Recipes)if(recipe.MinimumGridSize<=grid&&recipe.Ingredients.All(s=>s.Empty||reachable.Contains(s.Id)))changed|=reachable.Add(recipe.Output.Id);
                foreach(var item in items.items)if((item.toolCapabilities&ToolCapability.Pickaxe)!=0&&reachable.Contains(item.runtimeId))
                    foreach(byte block in new[]{BlockId.Stone,BlockId.IronOre,BlockId.CopperOre,BlockId.CoalOre,BlockId.GoldOre,BlockId.DiamondOre,IndustryId.AzureOre})
                        if(BlockId.Mineable(block,item.toolCapabilities,item.tier))changed|=reachable.Add(items.FistDrop(block));
                if(reachable.Contains(BlockId.Furnace)&&reachable.Any(id=>processing.FuelTicks(id)>0))foreach(var recipe in processing.Recipes)if(reachable.Contains(recipe.Input.Id))changed|=reachable.Add(recipe.Output.Id);
            }while(changed);
            foreach(var recipe in registry.Recipes)Check(reachable.Contains(recipe.Output.Id),"Gathering bootstraps "+recipe.Id+" without supplied tools");
            Check(reachable.Contains(BlockId.BakedPotato),"Gathering bootstraps cooked food");
        }
        static void HealthAndArmor(ItemRegistry items)
        {
            var inventory=new Inventory(id=>items.Get(id).stackLimit);var hunger=new HungerState();Put(inventory,0,BlockId.BakedPotato,2);
            Check(!hunger.TryEat(inventory,0,5)&&inventory.Slots[0].Count==2,"Full food bar does not consume a meal");
            hunger.Exert(60);Check(hunger.Food==5&&!hunger.CanSprint,"Exertion drains hunger and gates sprinting");
            Check(hunger.TryEat(inventory,0,5)&&hunger.Food==10&&inventory.Slots[0].Count==1&&hunger.CanSprint,"A meal restores food and sprint capability once");
            var equipment=new EquipmentState(items.Get);
            var wrong=new ItemStack(BlockId.Dirt,1);equipment.Click(0,ref wrong,false);Check(equipment.Slots[0].Empty&&wrong.Count==1,"Equipment rejects ordinary items");
            for(int slot=0;slot<4;slot++)
            {
                var piece=new ItemStack((byte)(78+slot),1);equipment.Click(slot,ref piece,false);Check(piece.Empty,"Diamond armor equips in its designated slot");
            }
            Check(equipment.Protection==20,"Complete diamond set gives twenty protection points");
            var health=new HealthState();Check(Math.Abs(health.Damage(10,DamageKind.Impact,equipment.Protection)-2)<.001f,"Armor mitigates contact damage");
            Check(health.Damage(3,DamageKind.Fall,20)==3,"Armor does not erase fall damage");
            hunger=new HungerState();health.Advance(80,hunger);Check(health.Hearts==16&&hunger.Food==19,"High food heals a heart point and spends exhaustion");
            var idleFood=new HungerState();var idleHealth=new HealthState();idleHealth.Advance(2047,idleFood);
            Check(idleFood.Food==20,"Passive hunger waits for the complete food-point cost");
            idleHealth.Advance(1,idleFood);Check(idleFood.Food==19&&idleFood.Exhaustion==0,"Idle survival drains food after 102.4 seconds without a healing surcharge");
            var bandFood=new HungerState();bandFood.Exert(32);var bandHealth=new HealthState();bandHealth.Damage(10,DamageKind.Fall,0);
            bandHealth.Advance(79,bandFood);Check(bandHealth.Hearts==10&&bandHealth.Regenerating,"Healing starts at 60 percent, but waits four seconds");
            bandHealth.Advance(1,bandFood);Check(bandHealth.Hearts==11&&bandFood.Food==11&&bandHealth.Regenerating,"Healing costs extra food and continues at 55 percent");
            bandHealth.Advance(80,bandFood);Check(bandHealth.Hearts==12&&bandFood.Food==9&&!bandHealth.Regenerating,"Healing stops once its food cost reaches the lower threshold");
            bandHealth.Advance(80,bandFood);Check(bandHealth.Hearts==12,"Low food cannot continue healing");
            var coldFood=new HungerState();coldFood.Exert(36);var coldHealth=new HealthState();coldHealth.Damage(10,DamageKind.Fall,0);
            coldHealth.Advance(80,coldFood);Check(coldHealth.Hearts==10&&!coldHealth.Regenerating,"55 percent food cannot start a fresh healing cycle");
            coldFood.TryEat(inventory,0,1);coldHealth.Advance(1,coldFood);
            Check(coldFood.Food==12&&coldHealth.Regenerating,"Eating back to 60 percent restarts healing");
            coldFood.Exert(8);coldHealth.Advance(80,coldFood);Check(coldFood.Food==10&&coldHealth.Hearts==10&&!coldHealth.Regenerating,"Exactly 50 percent food stops healing immediately");
            var oneFood=new HungerState();var manyFood=new HungerState();var oneHealth=new HealthState();var manyHealth=new HealthState();
            oneHealth.Damage(15,DamageKind.Fall,0);manyHealth.Damage(15,DamageKind.Fall,0);
            oneHealth.Advance(5000,oneFood);for(int i=0;i<5000;i++)manyHealth.Advance(1,manyFood);
            Check(oneHealth.Hearts==manyHealth.Hearts&&oneFood.Food==manyFood.Food&&oneFood.Exhaustion==manyFood.Exhaustion&&oneHealth.Regenerating==manyHealth.Regenerating,"Batched and individual survival ticks agree across healing and passive hunger boundaries");
            foreach(float incoming in new[]{.01f,1f,10f})
            {
                float previous=incoming;
                for(int armor=-1;armor<=100;armor++)
                {
                    float taken=new HealthState().Damage(incoming,DamageKind.Impact,armor);
                    Check(taken>0&&taken<=previous&&taken>=incoming*.1999f,"Armor remains monotonic, positive and capped, including excess protection");previous=taken;
                }
            }
            hunger.Exert(200);health.Advance(80*30,hunger);Check(health.Hearts==1&&!health.Dead,"Starvation reaches one health point");
            health.Damage(1,DamageKind.Fall,0);Check(health.Dead,"Lethal damage reaches death");health.Respawn();Check(health.Hearts==20&&!health.Dead,"Respawn restores health");
            Reject(()=>health.Damage(float.NaN,DamageKind.Impact,0),"Invalid damage accepted");
            Reject(()=>hunger.Exert(double.PositiveInfinity),"Invalid exhaustion accepted");
        }
    }
}
