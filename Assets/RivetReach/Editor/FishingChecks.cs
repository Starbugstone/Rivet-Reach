using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class FishingChecks
    {
        public static void Run()
        {
            int checks=0;void Check(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
            var p=new BlockPos(2000000000,30,-2000000000);var cast=new FishingCast();
            Check(!cast.Reel()&&!cast.Active,"Idle reel cannot create a fish");
            for(int wait=FishingCast.MinimumWait;wait<=FishingCast.MaximumWait;wait++)
            {
                cast.Start(p,wait);cast.Advance(wait-1);Check(cast.Phase==FishingPhase.Waiting&&cast.Remaining==1,"No early bite");
                cast.Advance(1);Check(cast.Phase==FishingPhase.Bite&&cast.Remaining==60,"Exact bite boundary");
                cast.Advance(59);Check(cast.Reel()&&!cast.Reel(),"Last live bite tick awards once");
                cast.Start(p,wait);cast.Advance(wait+60);Check(!cast.Reel()&&!cast.Active,"Expired bite cannot award");
            }
            cast.Start(p,160);cast.Advance(0);Check(cast.Remaining==160,"Zero ticks preserve cast");cast.Cancel();Check(!cast.Reel(),"Cancellation awards nothing");
            cast.Start(p,160);Check(!cast.Reel(),"Early reel awards nothing");
            cast.Start(p,160);cast.Advance(int.MaxValue);Check(!cast.Active,"A long hitch does not retain a missed bite");
            bool threw=false;try{cast.Advance(-1);}catch(ArgumentOutOfRangeException){threw=true;}Check(threw,"Reject negative time");
            var water=new Dictionary<BlockPos,byte>();for(int x=-FishingWater.Radius;x<=FishingWater.Radius;x++)for(int z=-FishingWater.Radius;z<=FishingWater.Radius;z++)for(int y=-1;y<=1;y++)water[p.Offset(x,y,z)]=y==1?BlockId.Air:Fluids.Water.Source;
            byte Read(BlockPos cell)=>water[cell];bool Resident(BlockPos cell)=>water.ContainsKey(cell);
            Check(FishingWater.Suitable(p,Resident,Read),"Resident deep water is fishable at remote integer coordinates");
            foreach(var cell in water.Keys.ToArray())
            {
                byte value=water[cell];water.Remove(cell);Check(!FishingWater.Suitable(p,Resident,Read),"Missing cell rejected before read");water[cell]=BlockId.Stone;Check(!FishingWater.Suitable(p,Resident,Read),"Blocked or shallow water rejected");water[cell]=value;
            }
            foreach(byte fluid in new[]{Fluids.Water.Flow(1),Fluids.Water.Falling,Fluids.Lava.Source})
            {water[p]=fluid;Check(!FishingWater.Suitable(p,Resident,Read),"Flowing water and lava cannot grant fish");}water[p]=Fluids.Water.Source;
            var items=ItemRegistry.Load();var recipes=RecipeCatalogAsset.Load().Compile(items);var rod=recipes.Recipes.Single(r=>r.Id=="rivet:fishing_rod");
            Check(rod.MinimumGridSize==3&&rod.Output.Id==FishId.Rod&&rod.Output.Count==1,"Workbench makes one rod");
            var authored=RecipeCatalogAsset.Load().recipes.Single(r=>r.stableId==rod.Id);
            byte[] layout={0,0,BlockId.Stick,0,BlockId.Stick,FarmId.String,BlockId.Stick,0,FarmId.String};
            Check(authored.ingredients.Select(c=>string.IsNullOrEmpty(c.itemId)?(byte)0:items.ResolveId(c.itemId)).SequenceEqual(layout),"Three diagonal sticks and two string retain beginning recipe layout");
            Check(items.Get(FishId.Rod).stackLimit==1,"Rods do not stack");
            Check(items.FoodPoints(FishId.Raw)==2&&items.FoodPoints(FishId.Cooked)==6&&items.FoodPoints(FishId.Stew)==12,"Raw, cooked and meal progression");
            var food=CookingCatalog.Current;var cook=food.Find("rivet:cook_fish");var stew=food.Find("rivet:cook_fish_stew");
            Check(food.Plan(cook,new[]{new ItemStack(FishId.Raw,1),default,default})!=null,"Raw fish cooks through shared planner");
            Check(food.Plan(cook,new[]{new ItemStack(FishId.Cooked,1),default,default})==null,"Already cooked fish cannot be recooked");
            foreach(byte fish in new[]{FishId.Raw,FishId.Cooked})foreach(byte veg in new[]{BlockId.Potato,FarmId.Carrot})
                Check(food.Plan(stew,new[]{new ItemStack(fish,1),new ItemStack(veg,2),default})!=null,"Fish stew uses shared fish and vegetable tags");
            Check(food.Plan(stew,new[]{new ItemStack(FishId.Raw,1),new ItemStack(BlockId.Potato,1),default})==null,"Incomplete meal does not consume inputs");
            var browser=new RecipeBrowserIndex(items,recipes,ProcessingCatalogAsset.Load().Compile(items));Check(browser.Recipes.Count(r=>r.FoodRecipe!=null&&r.Output.Id==FishId.Cooked)==2&&browser.Recipes.Count(r=>r.FoodRecipe!=null&&r.Output.Id==FishId.Stew)==2,"Both cookers expose fish recipes");
            File.WriteAllText("Logs/Fishing/checks.txt",checks+" fishing assertions passed\n");Debug.Log(checks+" fishing assertions passed");
        }
    }
}
