using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class FoodBalanceChecks
    {
        public static void Run()
        {
            int count=0;var report=new StringBuilder();var items=ItemRegistry.Load();var config=FoodBalanceCatalog.Current;config.Validate(items);
            void Check(bool ok,string message){if(!ok)throw new Exception("Food balance: "+message);count++;report.AppendLine("PASS "+message);}
            var inventory=new Inventory(id=>items.Get(id).stackLimit);byte stew=FarmId.Stew;
            inventory.Add(stew,8);var food=new HungerState();
            Check(!food.TryEat(inventory,0,12,12)&&inventory.Total(stew)==8,"Full hunger cannot consume food or repeatedly refill reserve");
            food.Exert(48);Check(food.TryEat(inventory,0,12,12)&&food.Food==20&&food.Saturation==12&&inventory.Total(stew)==7,"Stew consumes exactly one and provides authored food and reserve");
            food.Exert(47.5);Check(food.Food==20&&food.Saturation==1&&food.Exhaustion==3.5,"Paid exhaustion drains reserve before food and retains fractional work");
            food.Exert(.5);Check(food.Food==20&&food.Saturation==0,"Final reserve unit absorbs exactly four exhaustion");
            food.Exert(4);Check(food.Food==19&&food.Saturation==0,"Ordinary food drain resumes after reserve");
            Check(food.TryEat(inventory,0,12,20)&&food.Saturation==20,"Reserve has an explicit twenty-point cap");food.Exert(1000);Check(food.Food==0&&food.Saturation==0,"Large exertion drains both bounded balances without overflow credit");
            food=new HungerState();food.Exert(48);food.TryEat(inventory,0,12,12);var health=new HealthState();health.Damage(2,DamageKind.Fall,0);health.Advance(80,food);
            Check(health.Hearts==19&&food.Food==20&&food.Saturation==11,"Healing retains its full six-exhaustion cost while using stored nutrition");
            Check(HungerState.ActivityMultiplier==.75&&HungerState.HealingExhaustion==6,"Later issue12 activity reduction and healing cost are preserved");
            byte[] saved;using(var stream=new MemoryStream()){using(var writer=new SaveWriter(stream))Save(food,"WriteSave",writer);saved=stream.ToArray();}
            var restored=new HungerState();using(var reader=new SaveReader(new MemoryStream(saved),items))Save(restored,"ReadSave",reader);
            Check(restored.Food==food.Food&&restored.Saturation==food.Saturation&&restored.Exhaustion==food.Exhaustion,"Schema17 retains exact reserve and fractional exhaustion");
            using(var stream=new MemoryStream()){using(var writer=new SaveWriter(stream,16))Save(food,"WriteSave",writer);using var reader=new SaveReader(new MemoryStream(stream.ToArray()),items,16);Save(restored,"ReadSave",reader);}
            Check(restored.Saturation==0&&restored.Food==food.Food&&restored.Exhaustion==food.Exhaustion,"Old hunger payload starts with zero reserve and keeps prior food/exhaustion");
            saved[saved.Length-4]=21;bool rejected=false;try{using var reader=new SaveReader(new MemoryStream(saved),items);Save(restored,"ReadSave",reader);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"Out-of-range saved saturation rejects");
            var a=new HungerState();var b=new HungerState();a.Exert(48);b.Exert(48);a.TryEat(inventory,0,12,12);b.TryEat(inventory,0,12,12);var ha=new HealthState();var hb=new HealthState();ha.Advance(12000,a);for(int i=0;i<12000;i++)hb.Advance(1,b);
            Check(a.Food==b.Food&&a.Saturation==b.Saturation&&a.Exhaustion==b.Exhaustion,"Food and reserve are independent of fixed-tick batching");
            Check(config.Saturation(BlockId.Potato)==0&&config.Saturation(BlockId.Apple)==0&&config.Saturation(BlockId.BakedPotato)==2,"Wild food remains immediately edible; baking adds a lasting reserve");
            var csv=new StringBuilder("workload,item,food,saturation,seconds,meals_before,meals_after,min_food_after,mean_repeat_interval_seconds\n");
            foreach(string workload in new[]{"base","expedition","sprint"})
            foreach(var item in items.items.Where(i=>items.FoodPoints(i.runtimeId)>0))
            {
                var before=Measure(workload,item.runtimeId,0,items);var after=Measure(workload,item.runtimeId,config.Saturation(item.runtimeId),items);
                csv.AppendLine($"{workload},{item.stableId},{item.foodPoints},{config.Saturation(item.runtimeId)},3600,{before.meals},{after.meals},{after.minFood},{after.interval.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)}");
                Check(after.meals<=before.meals&&after.minFood>=8,"One-hour "+workload+" workload conserves supplied "+item.displayName+" and prevents starvation");
                if(workload=="base"&&item.runtimeId==FarmId.Porridge)Check(after.meals<=3&&after.interval>=900,"Renewable porridge supports at least fifteen minutes between meals during base activity");
            }
            Directory.CreateDirectory("Logs/FoodBalance");File.WriteAllText("Logs/FoodBalance/domain-checks.txt",report+"Assertions: "+count+"\n");File.WriteAllText("Logs/FoodBalance/cadence.csv",csv.ToString());
        }
        static void Save(HungerState food,string method,object stream)
        {
            try{typeof(HungerState).GetMethod(method,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(food,new[]{stream});}
            catch(System.Reflection.TargetInvocationException ex){throw ex.InnerException;}
        }
        static (int meals,int minFood,double interval) Measure(string workload,byte id,int reserve,ItemRegistry items)
        {
            var food=new HungerState();var health=new HealthState();var inventory=new Inventory(item=>items.Get(item).stackLimit);inventory.Add(id,1024);
            int meals=0,minFood=20,last=-1,intervals=0,elapsed=0,points=items.FoodPoints(id);
            for(int second=0;second<3600;second++)
            {
                int phase=second%60;health.Advance(20,food);
                if(workload=="sprint")food.Exert(6.5*HungerState.SprintExhaustionPerMetre);
                else
                {
                    if(phase<(workload=="base"?20:40))food.Exert(4.5*HungerState.WalkExhaustionPerMetre);
                    if(workload=="expedition"&&phase>=40&&phase<50)food.Exert(6.5*HungerState.SprintExhaustionPerMetre);
                    if(phase<(workload=="base"?2:4))food.Exert(HungerState.JumpExhaustion);
                    if(second%10==0)food.Exert(HungerState.BlockActionExhaustion*(workload=="base"?3:1));
                }
                minFood=Math.Min(minFood,food.Food);
                if(food.Food<=HungerState.Maximum-points)
                {
                    int slot=inventory.FindSlot(s=>s.Id==id&&!s.Empty);if(slot<0||!food.TryEat(inventory,slot,points,reserve))throw new Exception("Food fixture ran out");
                    meals++;if(last>=0){elapsed+=second-last;intervals++;}last=second;
                }
            }
            if(inventory.Total(id)!=1024-meals)throw new Exception("Diet accounting mismatch");
            return (meals,minFood,intervals==0?0:elapsed/(double)intervals);
        }
    }
}
