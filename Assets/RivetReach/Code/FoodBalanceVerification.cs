using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewFoodBalanceResume()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-rr-save-directory");Check(at>=0,"Food resume uses an isolated checkpoint");
            game.InitializeSaves(args[at+1]);Check(game.ContinueLatestSave(),"Fresh process loads food checkpoint: "+game.SaveStatus);
            game.enabled=false;FreezeSaveFixture();game.Animals.enabled=false;
            using(var reader=new BinaryReader(File.OpenRead(Path.Combine(args[at+1],"food-fixture.bin"))))
            {
                Check(game.Hunger.Food==reader.ReadInt32()&&game.Hunger.Saturation==reader.ReadInt32()&&game.Hunger.Exhaustion==reader.ReadDouble(),"Fresh process preserves exact food, reserve and fractional exhaustion");
                Check(game.Survival.Tick==reader.ReadInt64(),"No offline survival or crop advance");
            }
            yield return Settle(120);
            game.Hunger.Exert(200);for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            game.Inventory.Add(FarmId.CookedMushroom,1);Check(game.Hunger.TryEat(game.Inventory,0,4,2),"Eat a small cooked meal while critically hungry");
            game.SetMode(ScreenMode.Play);yield return null;yield return null;
            Check(!game.Hunger.CanSprint&&game.Hunger.Saturation==2&&game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text=="Eat to sprint"),"Low-food sprint warning takes precedence over saturation label");
            yield return Capture("low-food-reserve-warning");
            game.Hunger.Exert(200);game.Inventory.Add(FarmId.Porridge,1);Check(game.Hunger.TryEat(game.Inventory,0,9,8),"Prepared meal restores sprint eligibility");yield return null;yield return null;
            Check(game.Hunger.CanSprint&&game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text=="Well fed · 8"),"HUD displays the exact reserve once sprint is available");
            yield return Capture("saved-reserve-hud");
        }
        IEnumerator ReviewFoodBalance()
        {
            if(!Environment.GetCommandLineArgs().Contains("-rr-food-kitchen-review"))yield return ReviewAlphaSurvival();
            // Controlled established-base fixture follows the unassisted route.
            // Setup supplies infrastructure only; crop growth, cooking and eating
            // below use ordinary active-time simulation and paid transactions.
            FreezeSaveFixture();game.enabled=false;game.Animals.enabled=false;
            var world=game.World;var player=game.Player;var p=world.Address(player.transform.position).Offset(0,4,0);
            void Put(BlockPos cell,byte id){byte old=world.Get(cell);if(old==id)return;if(old!=0)Check(world.Remove(cell,old),"Clear kitchen fixture");if(id!=0)Check(world.Place(cell,id),"Place kitchen fixture");}
            for(int x=-3;x<=14;x++)for(int z=-3;z<=14;z++){Put(p.Offset(x,-1,z),BlockId.Planks);for(int y=0;y<4;y++)Put(p.Offset(x,y,z),0);}
            var wheat=p.Offset(-2,0,2);var berry=p.Offset(-2,0,4);var cooker=p.Offset(-2,0,6);
            foreach(var cell in new[]{wheat,berry}){Put(cell.Offset(0,-1,0),BlockId.Dirt);Check(world.Till(cell.Offset(0,-1,0)),"Till kitchen garden");}
            Check(world.Plant(wheat,FarmId.WheatSeed)&&world.Plant(berry,FarmId.BerrySeed),"Plant renewable grain and fruit");Put(cooker,FarmId.Cooker);
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            player.ResetMotion();player.transform.position=world.Local(p)+new Vector3(.5f,.02f,.5f);player.Yaw=-45;player.Pitch=15;player.enabled=true;
            game.Sky.Clock.SetTime(.35);game.Weather.SetWeather(WeatherKind.Clear,true);game.Sky.Apply();game.SetMode(ScreenMode.Play);game.enabled=true;
            long started=game.Survival.Tick;float deadline=Time.realtimeSinceStartup+300;
            while((world.Get(wheat)!=FarmId.WheatPlant+3||world.Get(berry)!=FarmId.BerryPlant+3)&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(world.Get(wheat)==FarmId.WheatPlant+3&&world.Get(berry)==FarmId.BerryPlant+3,"Kitchen crops mature through real active time without compost or irrigation");
            Check(game.TryHarvestCrop(wheat,world.Get(wheat),true)&&game.TryHarvestCrop(berry,world.Get(berry),true),"Mature hoe harvest pays ingredients and replants through issue12 transaction");
            Check(world.Get(wheat)==FarmId.WheatPlant&&world.Get(berry)==FarmId.BerryPlant,"Harvest leaves both next crops planted");
            var machine=game.Industry.Simulation.At(cooker);machine.SelectCooking("rivet:cook_fruit_porridge");
            foreach(var pair in new[]{(id:FarmId.Grain,slot:0),(id:FarmId.Berries,slot:1)})
            {int slot=game.Inventory.FindSlot(s=>s.Id==pair.id);Check(slot>=0,"Harvest supplies porridge ingredient");var stack=game.Inventory.Take(slot,1);machine.Click(pair.slot,ref stack,false);Check(stack.Empty,"Cooker accepts harvested ingredient");}
            var fuel=new ItemStack(BlockId.Log,1);machine.Click(3,ref fuel,false);Check(fuel.Empty,"Cooker charges one supplied log as fixture fuel");
            player.ResetMotion();player.transform.position=world.Local(cooker)+new Vector3(.5f,.02f,-2);player.Yaw=0;player.Pitch=30;yield return null;yield return null;
            Check(game.TryOpenMachine(cooker),"Open operating kitchen cooker");yield return Capture("harvest-cooking");game.SetMode(ScreenMode.Play);
            deadline=Time.realtimeSinceStartup+30;while(machine.Items.Total(FarmId.Porridge)==0&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(machine.Items.Total(FarmId.Porridge)==1&&machine.Items.Slots.Take(3).All(s=>s.Empty),"Active cooker produces exactly one meal from harvested grain and fruit");
            var meal=machine.Items.Take(4,1);Check(game.Inventory.Add(meal)==0,"Recover cooked meal without loss");
            game.Hunger.Exert(4*(game.Hunger.Food-11)+4*game.Hunger.Saturation);game.Selected=game.Inventory.FindSlot(s=>s.Id==FarmId.Porridge);
            player.ResetMotion();player.transform.position=world.Local(p)+new Vector3(.5f,.02f,.5f);player.Yaw=-45;player.Pitch=10;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSecondsRealtime(.6f);yield return Capture("eating-prepared-meal");yield return new WaitForSecondsRealtime(.3f);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(game.Inventory.Total(FarmId.Porridge)==0&&game.Hunger.Saturation==8&&game.Hunger.Food>=19,"Completed real bite consumes meal and grants nine food plus eight reserve");
            yield return Capture("well-fed-garden");
            game.SetMode(ScreenMode.Pause);int pausedFood=game.Hunger.Food,pausedReserve=game.Hunger.Saturation;double pausedExhaustion=game.Hunger.Exhaustion;
            yield return new WaitForSecondsRealtime(2);Check(game.Hunger.Food==pausedFood&&game.Hunger.Saturation==pausedReserve&&game.Hunger.Exhaustion==pausedExhaustion,"Pause freezes both food balances exactly");
            game.InitializeSaves(Path.Combine(output,"Saves"));Check(game.SaveGame("Food reserve and kitchen"),"Save reserve and kitchen checkpoint");
            using(var writer=new BinaryWriter(File.Create(Path.Combine(output,"Saves","food-fixture.bin")))){writer.Write(game.Hunger.Food);writer.Write(game.Hunger.Saturation);writer.Write(game.Hunger.Exhaustion);writer.Write(game.Survival.Tick);}
            // Fifteen actual minutes: walk sixteen seconds per minute around a square,
            // two jumps per minute, otherwise tend/observe the base. No supplied food.
            game.SetMode(ScreenMode.Play);long start=game.Survival.Tick;int lastSecond=-1,minFood=20;double distance=0;var previous=player.transform.position;
            var csv=new StringBuilder("active_seconds,food,saturation,exhaustion,walked_metres\n");
            while(game.Survival.Tick-start<18000)
            {
                int second=(int)((game.Survival.Tick-start)/20),phase=second%60;
                if(second!=lastSecond)
                {
                    lastSecond=second;player.Yaw=(phase/2%4)*90;player.Pitch=10;
                    InputSystem.QueueStateEvent(Keyboard.current,phase<16?new KeyboardState(Key.W):phase==21||phase==23?new KeyboardState(Key.Space):new KeyboardState());
                    if(second%60==0){csv.AppendLine(FormattableString.Invariant($"{second},{game.Hunger.Food},{game.Hunger.Saturation},{game.Hunger.Exhaustion},{distance:F2}"));File.WriteAllText(Path.Combine(output,"native-cadence.csv"),csv.ToString());}
                }
                var delta=player.transform.position-previous;delta.y=0;distance+=delta.magnitude;previous=player.transform.position;minFood=Math.Min(minFood,game.Hunger.Food);yield return null;
            }
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            Check(!game.Creative&&!game.Health.Dead&&minFood>=12&&distance>900,"Fifteen active minutes of base walking/jumping need no further meal and retain healing eligibility");
            Check(game.Hunger.Saturation==0,"Sustained activity spends the paid reserve before ordinary hunger");
            File.AppendAllText(Path.Combine(output,"native-cadence.csv"),FormattableString.Invariant($"900,{game.Hunger.Food},{game.Hunger.Saturation},{game.Hunger.Exhaustion},{distance:F2}\n"));
            yield return Capture("base-fifteen-minutes");
        }
    }
}
