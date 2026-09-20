using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class TagReviewChecks
    {
        public static void Run()
        {
            var lines=new List<string>();
            void Check(bool ok,string message){if(!ok)throw new Exception("Tag review: "+message);lines.Add("PASS "+message);}
            void Reject(Action action,string message){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}catch(InvalidOperationException){rejected=true;}Check(rejected,message);}
            using var allocationCounter=new AllocationCounter();lines.Add(allocationCounter.Description);
            void AllocationCheck(Action action,string message)
            {
                long allocated=allocationCounter.Measure(action);
                if(allocationCounter.Supported)Check(allocated==0,message+" (calibrated counter)");
                else lines.Add("UNVERIFIED "+message+": no calibrated allocation counter; behavior executed, zero-allocation claim withheld.");
            }
            var items=ItemRegistry.Load();
            Check(ReferenceEquals(items.Select("#vegetable"),items.Select("#vegetable")),"Selectors are compiled once per registry");
            Check(items.Select("#raw_ore").Choices.Count==3&&items.Select("#ingot").Choices.Count==3,"Material tags group raw metal and refined metal without crossing identities");
            Check(items.MatchesSearch(BlockId.IronIngot,new[]{"#ingot","iron"})&&!items.MatchesSearch(BlockId.RawIron,new[]{"#ingot"}),"Tag and name search terms intersect exactly");
            Check(items.MatchesSearch(BlockId.Potato,new[]{"#EDIBLE","#vegetable"})&&!items.MatchesSearch(BlockId.Stone,new[]{"#edible"}),"Multiple tags and case-insensitive searches");
            Reject(()=>items.Select("#missing_tag"),"Unknown tag rejects recipe authoring");Reject(()=>items.Select("#Bad Tag"),"Malformed tag rejects recipe authoring");
            var test=ScriptableObject.CreateInstance<ItemRegistry>();
            try
            {
                test.items=new[]{new ItemDefinition{runtimeId=1,stableId="test:food",displayName="Food",foodPoints=1,tags=new[]{"edible","test_food"}}};
                var catalog=new CookingCatalog{recipes=new[]{new CookingRecipe{id="test:recipe",name="Test",ingredients=new[]{new FoodIngredient{selector="#test_food"}},output=1}}};
                catalog.Validate(test);Check(catalog.Choices("#test_food").Single()==1,"Injected catalog validation never resolves selectors through global Resources");
                test.items[0].tags=new[]{"edible","edible"};test.InvalidateIndex();Reject(()=>test.Get(1),"Duplicate tags rejected");
                test.items[0].tags=new[]{"not a tag"};test.InvalidateIndex();Reject(()=>test.Get(1),"Whitespace tags rejected");
            }
            finally{UnityEngine.Object.DestroyImmediate(test);}
            Check(items.Capability<IEdible>(BlockId.Potato)?.FoodPoints==items.Get(BlockId.Potato).foodPoints&&items.Capability<IVegetable>(BlockId.Potato)!=null,"One item exposes independent edible and vegetable interfaces");
            Check(items.Capability<IBurnable>(BlockId.Coal) is IBoilerFuel&&items.Capability<IBurnable>(BlockId.Log) is not IBoilerFuel,"Multiple interfaces distinguish boiler fuel from ordinary combustible wood");
            Check(items.Capability<ICompostable>(BlockId.Leaves)!=null&&items.Capability<IEdible>(BlockId.Stone)==null,"Absent capabilities remain absent");
            var foodCapability=items.Capability<IEdible>(BlockId.Potato);items.FoodPoints(BlockId.Potato);
            AllocationCheck(()=>{for(int n=0;n<10000;n++){items.Capability<IEdible>(BlockId.Potato);items.Capability<IBurnable>(BlockId.Coal);items.FoodPoints(BlockId.Potato);}},
                "10000 typed capability query groups allocate no managed memory");
            Check(ReferenceEquals(foodCapability,items.Capability<IEdible>(BlockId.Potato)),"Capability queries reuse immutable components");
            var mappedLabels=new HashSet<string>(StringComparer.Ordinal);
            void Mapping<T>(string label) where T:class,IItemCapability
            {
                mappedLabels.Add(label);var typed=items.Select<T>();
                Check(ReferenceEquals(typed,items.Select("#"+label)),"Typed and authored selectors share their membership authority: "+label);
                Check(items.items.All(item=>item.tags.Contains(label)==(items.Capability<T>(item.runtimeId)!=null)&&
                    typed.Matches(item.runtimeId)==(items.Capability<T>(item.runtimeId)!=null)),"Capability membership preserves every authored item: "+label);
            }
            Mapping<IEdible>("edible");Mapping<IBurnable>("burnable");Mapping<IBoilerFuel>("boiler_fuel");
            Mapping<IVegetable>("vegetable");Mapping<IFruit>("fruit");Mapping<IGrain>("grain");Mapping<IMushroom>("mushroom");Mapping<IPreparedFood>("prepared_food");
            Mapping<IMeat>("meat");Mapping<IRawMeat>("raw_meat");Mapping<IFish>("fish");Mapping<IRawFish>("raw_fish");Mapping<IEgg>("egg");Mapping<IRawEgg>("raw_egg");
            Mapping<ISeed>("seed");Mapping<IFibre>("fibre");Mapping<ICordage>("cordage");Mapping<IFabric>("fabric");Mapping<IFeather>("feather");
            Mapping<IRawOre>("raw_ore");Mapping<IIngot>("ingot");Mapping<ILogMaterial>("log");Mapping<IPlankMaterial>("planks");Mapping<IFishingRod>("fishing_rod");Mapping<ICompostable>("compostable");
            Check(mappedLabels.Count==25&&mappedLabels.SetEquals(items.items.SelectMany(item=>item.tags)),"All 25 shipped categories have explicit capability mappings");
            var vegetableSelector=items.Select<IVegetable>();
            Check(items.Capability<IEdible>(0)==null&&items.Capability<IBurnable>(0)==null&&!vegetableSelector.Matches(0),"Empty item identity supplies no capability or ingredient");
            for(int n=0;n<100;n++){items.Capability<IEdible>(BlockId.Stone);items.Select<IVegetable>().Matches(BlockId.Potato);items.Capability<IBurnable>(BlockId.Coal);}
            AllocationCheck(()=>{for(int n=0;n<10000;n++){items.Capability<IEdible>(BlockId.Stone);items.Select<IVegetable>().Matches(BlockId.Potato);items.Capability<IBurnable>(BlockId.Coal);}},
                "Missing capabilities, inherited interfaces and typed selector lookup allocate no managed memory after warm-up");
            test=ScriptableObject.CreateInstance<ItemRegistry>();
            try
            {
                test.items=new[]{new ItemDefinition{runtimeId=1,stableId="test:mutable",foodPoints=2,tags=new[]{"edible","vegetable"}}};
                var oldFood=test.Capability<IEdible>(1);var oldSelector=test.Select<IVegetable>();
                test.items[0].foodPoints=4;test.items[0].tags=new[]{"edible","fruit"};test.InvalidateIndex();
                Check(test.Capability<IEdible>(1).FoodPoints==4&&test.Capability<IVegetable>(1)==null&&test.Capability<IFruit>(1)!=null&&
                    !test.HasTag(1,"vegetable")&&ReferenceEquals(test.Select<IFruit>(),test.Select("#fruit")),"Invalidation rebuilds values, interface discovery and recipe membership together");
                Check(oldFood.FoodPoints==2&&oldSelector.Matches(1),"Previously compiled capabilities and selectors remain immutable snapshots");
                Reject(()=>test.Select<IVegetable>(),"Empty typed ingredient categories reject authoring");
                Check(ReferenceEquals(foodCapability,items.Capability<IEdible>(BlockId.Potato))&&ReferenceEquals(vegetableSelector,items.Select<IVegetable>()),"Invalidating an injected registry does not mutate the live registry");
                test.items[0].tags=new[]{"boiler_fuel"};test.InvalidateIndex();
                Reject(()=>test.Get(1),"Inherited boiler fuel requires an explicitly authored burnable capability");
                test.items[0].tags=new[]{"boiler_fuel","burnable"};
                Check(test.Capability<IBurnable>(1) is IBoilerFuel&&ReferenceEquals(test.Capability<IBurnable>(1),test.Capability<IBoilerFuel>(1)),"Failed registry compilation publishes nothing; repair rebuilds both inherited interfaces with the same component");
                test.items[0].tags=Array.Empty<string>();test.InvalidateIndex();
                Check(test.Capability<IBoilerFuel>(1)==null&&test.Capability<IBurnable>(1)==null&&test.FoodPoints(1)==0,"Removed capabilities cannot survive invalidation or authorize behavior");
            }
            finally{UnityEngine.Object.DestroyImmediate(test);}
            IngredientMatcher Matcher(params RecipeIngredient[] recipe)=>new IngredientMatcher(recipe,items);
            var overlap=Matcher(new RecipeIngredient("#edible",1),new RecipeIngredient("#vegetable",2));
            var input=new[]{new ItemStack(BlockId.Potato,1),new ItemStack(FarmId.Carrot,1),default(ItemStack)};
            Check(overlap.CanStage(input,BlockId.Apple),"Overlapping tags accept an ingredient that completes the broader requirement");
            input[2]=new ItemStack(BlockId.Apple,1);var plan=new int[3];Check(overlap.TryPlan(input,plan)&&plan.SequenceEqual(new[]{1,1,1}),"Overlapping recipe consumes every staged item exactly once");
            input=new[]{new ItemStack(BlockId.Apple,2),default(ItemStack),default(ItemStack)};
            Check(overlap.CanStage(input,FarmId.Carrot)&&!overlap.CanStage(input,BlockId.Apple),"Manual surplus may remain, while pipes add only still-needed ingredients");
            var distinct=Matcher(new RecipeIngredient("#vegetable",2),new RecipeIngredient("#grain",1),new RecipeIngredient("#fruit",1));
            input=new[]{new ItemStack(BlockId.Potato,1),default(ItemStack),default(ItemStack)};
            Check(!distinct.CanStage(input,FarmId.Carrot),"Do not fill two slots with mixed vegetables when two different ingredients still need space");
            Check(distinct.CanStage(input,BlockId.Potato)&&distinct.CanStage(input,FarmId.Grain),"Same-stack vegetables and the next ingredient remain insertable");
            input=new[]{default(ItemStack),new ItemStack(BlockId.Potato,1),new ItemStack(FarmId.Grain,1)};
            Check(distinct.CanStage(input,BlockId.Potato),"An earlier empty slot does not hide reusable occupied slots");
            input=new[]{new ItemStack(BlockId.Potato,2),new ItemStack(FarmId.Grain,1),new ItemStack(BlockId.Apple,1)};
            Check(distinct.TryPlan(input,plan)&&plan.SequenceEqual(new[]{2,1,1})&&!distinct.CanStage(input,FarmId.Carrot),"Complete staged batch rejects extra material");
            Reject(()=>Matcher(new RecipeIngredient("rivet:potato"),new RecipeIngredient("rivet:carrot"),new RecipeIngredient("rivet:grain"),new RecipeIngredient("rivet:apple")),"Impossible four-type recipe rejects three-slot station");
            Reject(()=>Matcher(new RecipeIngredient("#vegetable",10)),"Bounded matcher rejects unbounded recipe quantities");
            var crops=JsonUtility.FromJson<FarmingCatalog>(Resources.Load<TextAsset>("Definitions/Crops").text);
            int original=CropRules.Definitions[0].stageTicks;crops.crops[0].stageTicks=original+20;crops.crops[crops.crops.Length-1]=null;
            Reject(crops.Apply,"Malformed crop catalog rejects before mutation");Check(CropRules.Definitions[0].stageTicks==original,"Failed crop compilation leaves all live growth rules intact");
            input=new[]{new ItemStack(BlockId.Potato,1),new ItemStack(FarmId.Carrot,1),default(ItemStack)};
            for(int i=0;i<100;i++){overlap.TryPlan(input,plan);overlap.CanStage(input,BlockId.Apple);}
            var watch=new System.Diagnostics.Stopwatch();watch.Restart();watch.Stop();
            Action measureMatcher=()=>{watch.Restart();for(int i=0;i<10000;i++){overlap.TryPlan(input,plan);overlap.CanStage(input,BlockId.Apple);}watch.Stop();};
            AllocationCheck(measureMatcher,"10,000 planning + staging pairs allocate no managed memory after warm-up");
            lines.Add("Bounded matcher sample: "+watch.Elapsed.TotalMilliseconds+" ms / 10,000 pairs; excludes station/network/UI costs.");
            Directory.CreateDirectory("Logs/TagReview");File.WriteAllLines("Logs/TagReview/checks.txt",new[]{"PASS "+lines.Count(line=>line.StartsWith("PASS ",StringComparison.Ordinal))+" assertions; "+lines.Count(line=>line.StartsWith("UNVERIFIED ",StringComparison.Ordinal))+" unverified allocation results"}.Concat(lines));
        }
    }
}
