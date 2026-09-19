using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class ChickenChecks
    {
        public static void Run()
        {
            int count=0;void Check(bool ok,string message){if(!ok)throw new Exception("Chicken check: "+message);count++;}
            var items=ItemRegistry.Load();var config=ChickenCatalog.Load();
            var child=new ChickenState{Health=config.chickHealth,GrowthTicks=config.growthTicks,RandomState=17};child.ScheduleEgg(config);int initialEgg=child.EggTicks;
            Check(!child.Feed(config),"Chicks cannot breed or consume feed");child.Advance(config.growthTicks-1,true,config);
            Check(!child.Adult&&child.GrowthTicks==1&&child.EggTicks==initialEgg,"Chicks retain exact growth deadline without laying");
            child.Advance(100,false,config);Check(child.GrowthTicks==1&&child.Health==config.chickHealth,"Blocked adult footprint defers growth");
            child.Advance(1,true,config);Check(child.Adult&&child.Health==config.health&&child.EggTicks==initialEgg,"Clear growth makes one healthy adult with full egg interval");
            Check(child.Feed(config)&&child.LoveTicks==config.loveTicks,"Adult accepts feed once");Check(!child.Feed(config),"Repeated feed cannot extend readiness or consume another seed");
            child.Advance(config.loveTicks-1,true,config);Check(child.ReadyToBreed,"Last ready tick remains eligible");child.Advance(1,true,config);Check(!child.ReadyToBreed,"Readiness expires exactly");
            Check(child.Feed(config),"Expired readiness can be fed again");child.Bred(config);Check(!child.ReadyToBreed&&child.CooldownTicks==config.breedCooldownTicks&&!child.Feed(config),"Pair completion starts cooldown and clears love");
            child.Advance(config.breedCooldownTicks-1,true,config);Check(!child.Feed(config),"Cooldown cannot be bypassed one tick early");child.Advance(1,true,config);Check(child.Feed(config),"Cooldown ends exactly");
            for(int seed=1;seed<=256;seed++)
            {
                ChickenState State()=>new ChickenState{Health=config.chickHealth,GrowthTicks=101,RandomState=(uint)seed};
                var one=State();var split=State();one.ScheduleEgg(config);split.ScheduleEgg(config);
                Check(one.EggTicks==split.EggTicks&&one.EggTicks>=config.eggMinimumTicks&&one.EggTicks<=config.eggMaximumTicks,"Saved random sequence gives bounded reproducible egg deadline");
                one.Advance(233,true,config);for(int n=0;n<233;n++)split.Advance(1,true,config);
                Check(one.GrowthTicks==split.GrowthTicks&&one.EggTicks==split.EggTicks&&one.Health==split.Health,"Growth/egg timer partition conserves time");
            }
            child.Health=0;int egg=child.EggTicks;child.Advance(100,true,config);Check(!child.Feed(config)&&child.EggTicks==egg,"Dead animals cannot advance or breed");
            Check(config.spawnRules.habitat==MobHabitat.Surface&&config.spawnRules.supportBlocks.SequenceEqual(new[]{"rivet:grass"}),"Natural chickens use shared surface grass profile");
            for(int light=0;light<16;light++)Check(config.spawnRules.AllowsLight(light)==(light>=9),"Natural light range is independent of hostile darkness");
            var feeds=config.feed.Select(items.Select).ToArray();
            foreach(byte id in new[]{FarmId.WheatSeed,FarmId.FlaxSeed,FarmId.CarrotSeed,FarmId.BerrySeed,FarmId.Grain})Check(feeds.Any(f=>f.Matches(id)),"Seed/grain feeding is shared item selection");
            foreach(byte id in new[]{BlockId.Stone,FarmId.Carrot,FarmId.String,ChickenId.Egg})Check(!feeds.Any(f=>f.Matches(id)),"Unrelated items cannot breed animals");
            var cooking=CookingCatalog.Current;var meat=cooking.Find("rivet:cook_chicken");var cookedEgg=cooking.Find("rivet:cook_egg");var stew=cooking.Find("rivet:cook_chicken_stew");
            Check(cooking.Plan(meat,new[]{new ItemStack(ChickenId.Raw,1),default,default})!=null,"Raw meat cooks");
            Check(cooking.Plan(meat,new[]{new ItemStack(ChickenId.Cooked,1),default,default})==null,"Cooked meat cannot be recooked");
            Check(cooking.Plan(cookedEgg,new[]{new ItemStack(ChickenId.Egg,1),default,default})!=null&&cooking.Plan(cookedEgg,new[]{new ItemStack(ChickenId.CookedEgg,1),default,default})==null,"Only raw eggs cook");
            foreach(byte meatId in new[]{ChickenId.Raw,ChickenId.Cooked})
            {Check(cooking.Plan(stew,new[]{new ItemStack(meatId,1),new ItemStack(BlockId.Potato,1),new ItemStack(FarmId.Carrot,1)})!=null,"Meat stew accepts raw/cooked meat and mixed vegetables");Check(cooking.Plan(stew,new[]{new ItemStack(meatId,1),new ItemStack(BlockId.Potato,1),default})==null,"Stew needs both vegetables");}
            Check(items.FoodPoints(ChickenId.Raw)==2&&items.FoodPoints(ChickenId.Cooked)==6&&items.FoodPoints(ChickenId.CookedEgg)==4&&items.FoodPoints(ChickenId.Stew)==12&&items.FoodPoints(ChickenId.Egg)==0,"Foods have explicit progression; raw egg is an ingredient");
            var browser=new RecipeBrowserIndex(items,RecipeCatalogAsset.Load().Compile(items),ProcessingCatalogAsset.Load().Compile(items));
            foreach(byte id in new[]{ChickenId.Cooked,ChickenId.CookedEgg,ChickenId.Stew})Check(browser.Recipes.Count(r=>r.Output.Id==id)==2,"Both cookers expose every new food recipe");
            Check(SaveStore.Format>=12,"Chicken persistent section uses schema 12");
            Directory.CreateDirectory("Logs/Chickens");File.WriteAllText("Logs/Chickens/checks.txt",count+" chicken assertions passed\n");Debug.Log(count+" chicken assertions passed");
        }
    }
}
