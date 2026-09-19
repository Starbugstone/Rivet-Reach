using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class FoodBalanceChecks
    {
        public static void Run()
        {
            int count=0;var report=new StringBuilder();var items=ItemRegistry.Load();var config=FoodBalanceCatalog.Current;config.Validate(items);
            void Check(bool ok,string message){if(!ok)throw new Exception("Food balance: "+message);count++;report.AppendLine("PASS "+message);}
            var inventory=new Inventory(id=>items.Get(id).stackLimit);byte stew=FarmId.Stew;int stewReserve=config.Saturation(stew);
            inventory.Add(stew,8);var food=new HungerState();
            Check(!food.TryEat(inventory,0,12,stewReserve)&&inventory.Total(stew)==8,"Full hunger cannot consume food or repeatedly refill reserve");
            food.Exert(48);Check(food.TryEat(inventory,0,12,stewReserve)&&food.Food==20&&food.Saturation==stewReserve&&inventory.Total(stew)==7,"Stew consumes exactly one and provides authored food and reserve");
            food.Exert(4*(stewReserve-1)+3.5);Check(food.Food==20&&food.Saturation==1&&food.Exhaustion==3.5,"Paid exhaustion drains reserve before food and retains fractional work");
            food.Exert(.5);Check(food.Food==20&&food.Saturation==0,"Final reserve unit absorbs exactly four exhaustion");
            food.Exert(4);Check(food.Food==19&&food.Saturation==0,"Ordinary food drain resumes after reserve");
            Check(food.TryEat(inventory,0,12,20)&&food.Saturation==20,"Reserve has an explicit twenty-point cap");food.Exert(1000);Check(food.Food==0&&food.Saturation==0,"Large exertion drains both bounded balances without overflow credit");
            food=new HungerState();food.Exert(48);food.TryEat(inventory,0,12,stewReserve);var health=new HealthState();health.Damage(2,DamageKind.Fall,0);health.Advance(80,food);
            Check(health.Hearts==19&&food.Food==20&&food.Saturation==stewReserve-1,"Healing retains its full six-exhaustion cost while using stored nutrition");
            Check(HungerState.ActivityMultiplier==.75&&HungerState.HealingExhaustion==6,"Later issue12 activity reduction and healing cost are preserved");
            byte[] saved;using(var stream=new MemoryStream()){using(var writer=new SaveWriter(stream,17))Save(food,"WriteSave",writer);saved=stream.ToArray();}
            var restored=new HungerState();using(var reader=new SaveReader(new MemoryStream(saved),items,17))Save(restored,"ReadSave",reader);
            Check(restored.Food==food.Food&&restored.Saturation==food.Saturation&&restored.Exhaustion==food.Exhaustion,"Schema17 retains exact reserve and fractional exhaustion");
            using(var stream=new MemoryStream()){using(var writer=new SaveWriter(stream,16))Save(food,"WriteSave",writer);using var reader=new SaveReader(new MemoryStream(stream.ToArray()),items,16);Save(restored,"ReadSave",reader);}
            Check(restored.Saturation==0&&restored.Food==food.Food&&restored.Exhaustion==food.Exhaustion,"Old hunger payload starts with zero reserve and keeps prior food/exhaustion");
            saved[saved.Length-4]=21;bool rejected=false;try{using var reader=new SaveReader(new MemoryStream(saved),items,17);Save(restored,"ReadSave",reader);}catch(InvalidDataException){rejected=true;}
            Check(rejected,"Out-of-range saved saturation rejects");
            var a=new HungerState();var b=new HungerState();a.Exert(48);b.Exert(48);a.TryEat(inventory,0,12,stewReserve);b.TryEat(inventory,0,12,stewReserve);var ha=new HealthState();var hb=new HealthState();ha.Advance(12000,a);for(int i=0;i<12000;i++)hb.Advance(1,b);
            Check(a.Food==b.Food&&a.Saturation==b.Saturation&&a.Exhaustion==b.Exhaustion,"Food and reserve are independent of fixed-tick batching");
            Check(config.Saturation(BlockId.Potato)==0&&config.Saturation(BlockId.Apple)==0&&config.Saturation(FarmId.Carrot)==0&&config.Saturation(FarmId.Berries)==0&&config.Saturation(FarmId.Mushroom)==0&&config.Saturation(FishId.Raw)==0&&config.Saturation(ChickenId.Raw)==0,"Raw food has no reserve");
            Check(config.Saturation(BlockId.BakedPotato)==1&&config.Saturation(FarmId.RoastCarrot)==1&&config.Saturation(FarmId.CookedMushroom)==1&&config.Saturation(ChickenId.CookedEgg)==1,"Basic cooked food provides one reserve point");
            Check(config.Saturation(FishId.Cooked)==2&&config.Saturation(ChickenId.Cooked)==2&&config.Saturation(FarmId.Bread)==2,"Fish, chicken and bread provide two reserve points");
            Check(config.Saturation(FarmId.Porridge)==3,"Fruit porridge provides its explicit three reserve points");
            Check(config.Saturation(FarmId.Stew)==4&&config.Saturation(FishId.Stew)==4&&config.Saturation(ChickenId.Stew)==4,"All stews provide four reserve points");
            ReplayNativeCadence(Check);
            var csv=new StringBuilder("workload,item,food,saturation,seconds,meals_before,meals_after,min_food_after,mean_repeat_interval_seconds\n");
            foreach(string workload in new[]{"base","expedition","sprint"})
            foreach(var item in items.items.Where(i=>items.FoodPoints(i.runtimeId)>0))
            {
                var before=Measure(workload,item.runtimeId,0,items);var after=Measure(workload,item.runtimeId,config.Saturation(item.runtimeId),items);
                csv.AppendLine($"{workload},{item.stableId},{item.foodPoints},{config.Saturation(item.runtimeId)},3600,{before.meals},{after.meals},{after.minFood},{after.interval.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)}");
                Check(after.meals<=before.meals&&after.minFood>=8,"One-hour "+workload+" workload conserves supplied "+item.displayName+" and prevents starvation");
                if(workload=="base"&&item.runtimeId==FarmId.Porridge)Check(after.meals<=4&&after.interval>=600,"Renewable porridge supports at least ten minutes between meals during base activity");
            }
            Directory.CreateDirectory("Logs/FoodBalance");File.WriteAllText("Logs/FoodBalance/domain-checks.txt",report+"Assertions: "+count+"\n");File.WriteAllText("Logs/FoodBalance/cadence.csv",csv.ToString());
        }
        readonly struct NativeCadence
        {
            public readonly int Seconds,Food,Saturation;public readonly double Exhaustion,Metres;
            public NativeCadence(int seconds,int food,int saturation,double exhaustion,double metres){Seconds=seconds;Food=food;Saturation=saturation;Exhaustion=exhaustion;Metres=metres;}
        }
        static void ReplayNativeCadence(Action<bool,string> check)
        {
            const string path=".docs/verification/food-balance-2026-09-19/interrupted-native-cadence.csv";
            var rows=File.ReadAllLines(path).Skip(1).Where(line=>!string.IsNullOrWhiteSpace(line)).Select(line=>
            {
                var fields=line.Split(',');if(fields.Length!=5)throw new InvalidDataException("Invalid native cadence row.");
                return new NativeCadence(int.Parse(fields[0],CultureInfo.InvariantCulture),int.Parse(fields[1],CultureInfo.InvariantCulture),int.Parse(fields[2],CultureInfo.InvariantCulture),double.Parse(fields[3],CultureInfo.InvariantCulture),double.Parse(fields[4],CultureInfo.InvariantCulture));
            }).ToArray();
            if(rows.Length==0||rows[0].Seconds!=0||rows[0].Food!=HungerState.Maximum||rows[0].Saturation!=8)throw new InvalidDataException("Native cadence baseline does not start at the recorded eight-point reserve.");
            HungerState Replay(int startingReserve,StringBuilder csv,Action<NativeCadence,HungerState> inspect=null)
            {
                var replay=new HungerState();var seed=new Inventory(id=>ItemRegistry.Load().Get(id).stackLimit);seed.Add(BlockId.Potato,1);replay.Exert(4);
                if(!replay.TryEat(seed,0,1,startingReserve))throw new InvalidDataException("Cannot establish recorded replay reserve.");
                replay.Exert(rows[0].Exhaustion);
                var previous=rows[0];
                for(int index=0;index<rows.Length;index++)
                {
                    var row=rows[index];
                    if(index>0)
                    {
                        double exertion=4*((previous.Food-row.Food)+(previous.Saturation-row.Saturation))+row.Exhaustion-previous.Exhaustion;
                        if(exertion<-1e-9)throw new InvalidDataException("Native cadence regresses exhaustion.");
                        replay.Exert(Math.Max(0,exertion));
                    }
                    csv?.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3:R},{4:F2}",row.Seconds,replay.Food,replay.Saturation,replay.Exhaustion,row.Metres));
                    inspect?.Invoke(row,replay);previous=row;
                }
                return replay;
            }
            var old=Replay(8,null,(recorded,replayed)=>check(replayed.Food==recorded.Food&&replayed.Saturation==recorded.Saturation&&Math.Abs(replayed.Exhaustion-recorded.Exhaustion)<1e-9,"Recorded native cadence replays exact legacy reserve state at "+recorded.Seconds+" seconds"));
            var currentCsv=new StringBuilder("active_seconds,food,saturation,exhaustion,walked_metres\n");var current=Replay(3,currentCsv);
            var minuteTwelve=currentCsv.ToString().Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Single(line=>line.StartsWith("720,"));
            check(minuteTwelve.Split(',')[1]=="14","Three-point reserve reaches Food 14 after the recorded twelve-minute workload");
            check(old.Food==19&&current.Food==14,"Legacy eight-point and current three-point replays preserve the recorded thirteen-minute comparison");
            Directory.CreateDirectory("Logs/FoodBalance");File.WriteAllText("Logs/FoodBalance/rebalanced-native-replay.csv",currentCsv.ToString());
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
