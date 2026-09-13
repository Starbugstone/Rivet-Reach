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
            long before=GC.GetAllocatedBytesForCurrentThread();var watch=System.Diagnostics.Stopwatch.StartNew();
            before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++){overlap.TryPlan(input,plan);overlap.CanStage(input,BlockId.Apple);}long allocated=GC.GetAllocatedBytesForCurrentThread()-before;watch.Stop();
            Check(allocated==0,"10,000 planning + staging pairs allocate zero managed bytes after warm-up");
            lines.Add("Bounded matcher sample: "+watch.Elapsed.TotalMilliseconds+" ms / 10,000 pairs; excludes station/network/UI costs.");
            Directory.CreateDirectory("Logs/TagReview");File.WriteAllLines("Logs/TagReview/checks.txt",new[]{"PASS "+(lines.Count-1)+" assertions"}.Concat(lines));
        }
    }
}
