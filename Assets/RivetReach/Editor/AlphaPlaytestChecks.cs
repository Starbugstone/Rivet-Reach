using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class AlphaPlaytestChecks
    {
        public static void Run()
        {
            int count=0;
            void Check(bool value,string message){count++;if(!value)throw new Exception("Alpha playtest: "+message);}
            var registry=ItemRegistry.Load();var inventory=new Inventory(id=>registry.Get(id).stackLimit);
            inventory.Add(BlockId.Dirt,35,15,16);inventory.Add(BlockId.Dirt,40,20,21);
            inventory.Add(BlockId.Planks,7,25,26);inventory.Add(BlockId.Log,2,0,1);
            inventory.Add(new ItemStack(IndustryId.Battery,1){Energy=1234567},30,31);
            inventory.Add(new ItemStack(IndustryId.Battery,1){Energy=7654321},31,32);
            Check(inventory.Organize(15),"backpack changes on sort");
            Check(inventory.Total(BlockId.Dirt)==75&&inventory.Slots.Count(s=>s.Id==BlockId.Dirt)==2&&inventory.Slots[15].Count==64,"consolidation conserves exact quantities");
            Check(inventory.Slots[0].Id==BlockId.Log&&inventory.Slots[0].Count==2,"sorting backpack preserves hotbar");
            Check(inventory.Slots.Count(s=>s.Id==IndustryId.Battery)==2&&inventory.Slots.Sum(s=>s.Energy)==8888888,"portable contents stay separate and exact");
            Check(!inventory.Organize(15),"repeated sort is stable");
            Check(InventoryActions.Pick(inventory,registry,BlockId.Log,4,false)==0,"Survival selects existing hotbar item");
            var before=inventory.Slots.ToArray();
            Check(InventoryActions.Pick(inventory,registry,BlockId.Stone,4,false)==4&&inventory.Slots.SequenceEqual(before),"unowned Survival pick creates nothing");
            InventoryActions.Pick(inventory,registry,BlockId.Planks,0,false);
            Check(inventory.Slots[0].Id==BlockId.Planks&&inventory.Total(BlockId.Log)==2&&inventory.Total(BlockId.Planks)==7,"backpack pick swaps without loss");
            InventoryActions.Pick(inventory,registry,BlockId.Stone,4,true);
            Check(inventory.Slots[4].Id==BlockId.Stone&&inventory.Slots[4].Count==registry.Get(BlockId.Stone).stackLimit,"Creative provides a full stack");
            Check(BlockDefinitions.Get(BlockId.Stone).Traits==BlockTraits.None,"full cubes use default path");
            foreach(var crop in CropRules.Definitions)for(byte id=crop.first;id<=crop.Mature;id++)
            {
                var shape=BlockDefinitions.Get(id).Selection;
                Check(shape!=null&&!shape.Intersect(new Vector3(.99f,.95f,-1),Vector3.forward,5,out _,out _),"crop empty corner is transparent");
                Check(shape.Intersect(new Vector3(.5f,shape.Bounds.center.y,-1),Vector3.forward,5,out float distance,out var face)&&distance>=1&&face==Vector3Int.back,"crop center returns correct hit and face");
            }
            var narrow=SelectionShape.Box(.4f,0,.4f,.6f,.9f,.6f);
            Check(!narrow.Intersect(new Vector3(.1f,.5f,-1),Vector3.forward,5,out _,out _),"parallel ray misses narrow shape");
            Check(!narrow.Intersect(new Vector3(.5f,.5f,-1),Vector3.forward,1.3f,out _,out _),"shape respects reach");
            Check(narrow.Intersect(new Vector3(.5f,.5f,.5f),Vector3.up,5,out float inside,out _)&&inside==0,"inside shape selects immediately");
            var food=new HungerState();var health=new HealthState();health.Advance(2048,food);
            Check(food.Food==20&&Math.Abs(food.Exhaustion-3)<1e-9,"ordinary idle drain is 25 percent lower");
            Check(HungerState.HealingExhaustion==6,"healing retains original cost");
            var moving=new HungerState();moving.Exert(10*HungerState.SprintExhaustionPerMetre+5*HungerState.JumpExhaustion);
            Check(Math.Abs(moving.Exhaustion-1.5)<1e-9&&moving.Food==20,"Ten sprint metres plus five legal jumps cost 25 percent less without doubling the sprint jump");
            Check(Math.Abs(HungerState.WalkExhaustionPerMetre-.0075)<1e-9&&Math.Abs(HungerState.BlockActionExhaustion-.0375)<1e-9,"Walking and successful block actions share ordinary reduction");

            Directory.CreateDirectory("Logs/AlphaPlaytest");File.WriteAllText("Logs/AlphaPlaytest/checks.txt","PASS: "+count+" interaction/conservation assertions.\n");
        }
    }
}
