using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewRenewablesResume()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rr-save-directory");Check(index>=0,"Renewable resume uses isolated saved fixture");
            game.InitializeSaves(args[index+1]);Check(game.ContinueLatestSave(),"Fresh process continues renewable checkpoint: "+game.SaveStatus);
            game.enabled=false;FreezeSaveFixture();game.Animals.enabled=false;
            using(var reader=new BinaryReader(File.OpenRead(Path.Combine(args[index+1],"renewable-fixture.bin"))))
            {
                Check(game.Survival.Tick==reader.ReadInt64(),"Fresh process retains exact active survival tick");
                Check(game.Industry.WindGust==reader.ReadDouble(),"Fresh process reproduces exact saved gust without offline change");
                Check(game.Industry.Simulation.Machines.Values.Where(m=>m.Definition.Id==IndustryId.Battery).Sum(BatteryPower.Amount)==reader.ReadInt64(),"Fresh process retains exact battery energy");
            }
            Check(game.Industry.Simulation.Machines.Values.Count(m=>IndustryId.Renewable(m.Definition.Id))==2&&!game.Creative,"Fresh process restores both renewable machines in Survival");
            yield return Settle(120);
        }
        IEnumerator ReviewRenewables()
        {
            Check(!game.Creative&&game.Inventory.Slots.All(s=>s.Empty),"Ordinary Survival starts empty-handed before renewable fixture");
            yield return new WaitForSecondsRealtime(2);
            FreezeSaveFixture();game.Animals.enabled=false;game.Animals.Clear();game.enabled=false;yield return Settle();
            var world=game.World;var player=game.Player;var p=world.Address(player.transform.position).Offset(2,4,4);
            void Put(BlockPos cell,byte id){byte old=world.Get(cell);if(old==id)return;if(old!=0)Check(world.Remove(cell,old),"Clear renewable fixture");if(id!=0)Check(world.Place(cell,id),"Place renewable fixture "+id);}
            void Aim(Vector3 at,Vector3 from){player.transform.position=from;player.ResetMotion();player.Camera.transform.position=from+Vector3.up*1.5f;player.Camera.transform.LookAt(at);var d=player.Camera.transform.forward;player.Yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(d.y)*Mathf.Rad2Deg;}
            void AimAt(BlockPos cell)=>Aim(world.Local(cell)+Vector3.one*.5f,world.Local(cell)+new Vector3(.5f,.02f,-2));
            for(int x=-4;x<=7;x++)for(int z=-3;z<=4;z++){Put(p.Offset(x,-1,z),BlockId.Planks);for(int y=0;y<4;y++)Put(p.Offset(x,y,z),0);}
            var bench=p.Offset(-2,0,2);Put(bench,IndustryId.Bench);game.Sky.Clock.SetTime(.5);game.Weather.SetWeather(WeatherKind.Clear,true);game.Sky.Apply();
            foreach(byte id in new[]{IndustryId.SolarPanel,IndustryId.WindTurbine})
            {
                for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,64);
                var recipe=RecipeCatalogAsset.Load().recipes.Single(r=>r.stableId=="rivet:industry_"+id);
                foreach(var ingredient in recipe.ingredients)game.Inventory.Add(game.Registry.ResolveId(ingredient.itemId),ingredient.count);
                game.SetMode(ScreenMode.Play);AimAt(bench);yield return null;Check(game.TryInteractTarget(),"Open actual Machinist's Bench for "+id);
                game.UI.InspectBrowserItem(id,false);yield return Capture(id==178?"solar-recipe":"wind-recipe");game.UI.CloseBrowserRecipe();
                Check(game.Crafting.FillRecipe(recipe.stableId,game.Inventory)==RecipeFillStatus.Filled,"Fill advanced renewable recipe");yield return Capture(id==178?"solar-crafting":"wind-crafting");
                Check(game.Crafting.CraftToInventory(game.Inventory,1).Succeeded&&game.Inventory.Total(id)==1,"Craft one renewable machine with exact ingredients");
                game.SetMode(ScreenMode.Play);var cell=p.Offset(id==178?0:3,0,0);game.Selected=game.Inventory.FindSlot(s=>s.Id==id);
                Aim(world.Local(cell)+new Vector3(.5f,-.01f,.5f),world.Local(cell)+new Vector3(.5f,.02f,-2));yield return null;
                Check(game.TryPlaceSelected()&&world.Get(cell)==id&&game.Inventory.Total(id)==0,"Place crafted renewable through ordinary placement transaction");
            }
            var sim=game.Industry.Simulation;var solar=sim.At(p);var wind=sim.At(p.Offset(3,0,0));
            for(int x=0;x<=5;x++)Put(p.Offset(x,0,1),IndustryId.PowerCable);
            Put(p.Offset(1,0,2),IndustryId.Battery);Put(p.Offset(3,0,2),IndustryId.Battery);Put(p.Offset(5,0,2),IndustryId.Battery);Put(p.Offset(5,0,0),IndustryId.Lamp);
            var batteries=new[]{sim.At(p.Offset(1,0,2)),sim.At(p.Offset(3,0,2)),sim.At(p.Offset(5,0,2))};var lamp=sim.At(p.Offset(5,0,0));
            game.SetMode(ScreenMode.Play);Aim(world.Local(p)+new Vector3(2.5f,.5f,1),world.Local(p)+new Vector3(7,2,-6));
            yield return Settle(180);float deadline=Time.realtimeSinceStartup+30;
            while(!game.Industry.SkyExposed(p)&&Time.realtimeSinceStartup<deadline)yield return null;
            sim.Step();Check(solar.SupplyWatts==400&&wind.SupplyWatts>=40&&wind.SupplyWatts<=140,"Clear noon generates 400 W solar and smoothly gusting 40–140 W wind using cached world sky exposure");
            Check(lamp.ReceivedWatts==20&&batteries.Sum(b=>b.BatteryInputWatts)==380+wind.SupplyWatts&&batteries.Max(b=>b.BatteryInputWatts)-batteries.Min(b=>b.BatteryInputWatts)<=1,"Machine receives 20 W first; three batteries share the exact generation surplus in parallel");
            yield return new WaitForSecondsRealtime(.5f);yield return Capture("renewables-clear");
            foreach(WeatherKind kind in new[]{WeatherKind.Rain,WeatherKind.Storm})
            {
                game.Weather.SetWeather(kind,true);game.Sky.Apply();sim.Step();Check(solar.SupplyWatts==(kind==WeatherKind.Rain?200:60)&&(kind==WeatherKind.Rain?wind.SupplyWatts>=240&&wind.SupplyWatts<=360:wind.SupplyWatts==400),kind+" changes real generator outputs");
                yield return new WaitForSecondsRealtime(1);yield return Capture("renewables-"+kind.ToString().ToLowerInvariant());
            }
            game.Sky.Clock.SetTime(.92);game.Weather.SetWeather(WeatherKind.Clear,true);game.Sky.Apply();sim.Step();Check(solar.SupplyWatts==0&&wind.SupplyWatts>=40&&wind.SupplyWatts<=140&&lamp.ReceivedWatts==20,"At night wind supplies the machine and solar stops");yield return Capture("renewables-night");
            var rotor=world.GetComponent<IndustryPresentation>().ViewAt(wind.Position).GetComponentsInChildren<Transform>().Single(t=>t.name.StartsWith("MotionSpin"));
            var rotation=rotor.localRotation;yield return new WaitForSecondsRealtime(.25f);Check(Quaternion.Angle(rotation,rotor.localRotation)>1,"Imported turbine rotor turns while wind supplies real demand");
            foreach(var b in batteries)b.EnergyCells[0].Charge(BatteryStorage.CellCapacity-BatteryPower.Amount(b));lamp.SignalAttached=true;lamp.Signal=false;sim.Step();
            Check(wind.DeliveredWatts==0,"Full batteries and disabled lamp leave no wind power demand");rotation=rotor.localRotation;yield return new WaitForSecondsRealtime(.25f);Check(Quaternion.Angle(rotation,rotor.localRotation)<.001f,"Turbine stops spinning when its grid draws no power");yield return Capture("wind-idle");
            batteries[0].EnergyCells[0].Discharge(50000);sim.Step();rotation=rotor.localRotation;yield return new WaitForSecondsRealtime(.25f);Check(wind.DeliveredWatts>0&&Quaternion.Angle(rotation,rotor.localRotation)>1,"Battery charging counts as demand and restarts the turbine");
            lamp.SignalAttached=false;foreach(var b in batteries)b.EnergyCells[0].Discharge(BatteryPower.Amount(b));
            double gustBefore=game.Industry.WindGust;game.Survival.AdvanceTicks(120);sim.Step();Check(game.Industry.WindGust!=gustBefore&&wind.SupplyWatts>=40&&wind.SupplyWatts<=140,"Active survival ticks move the seeded gust without advancing celestial time");
            game.Sky.Clock.SetTime(.5);game.Sky.Apply();Put(p.Offset(0,2,0),BlockId.Planks);sim.Step();Check(solar.SupplyWatts==0,"Roof invalidation immediately stops solar");
            Put(p.Offset(0,2,0),0);deadline=Time.realtimeSinceStartup+30;while(!game.Industry.SkyExposed(p)&&Time.realtimeSinceStartup<deadline)yield return null;sim.Step();Check(solar.SupplyWatts==400,"Removing roof restores output after column cache rebuild");
            AimAt(p);yield return null;Check(game.TryInteractTarget(),"Open solar controls through normal interaction");yield return Capture("solar-controls");game.SetMode(ScreenMode.Play);
            AimAt(p.Offset(3,0,0));yield return null;Check(game.TryInteractTarget(),"Open wind controls through normal interaction");yield return Capture("wind-controls");game.SetMode(ScreenMode.Play);
            AimAt(p.Offset(1,0,2));yield return null;Check(game.TryInteractTarget(),"Open charging battery");yield return Capture("renewable-battery");game.SetMode(ScreenMode.Play);
            // With no generation, a charged battery can feed this lamp but never its empty peers.
            Put(p.Offset(0,1,0),BlockId.Planks);Put(p.Offset(3,1,0),BlockId.Planks);
            foreach(var b in batteries)b.EnergyCells[0].Discharge(BatteryPower.Amount(b));batteries[0].EnergyCells[0].Charge(100000);
            sim.Step();Check(lamp.ReceivedWatts==20&&BatteryPower.Amount(batteries[0])==99000&&batteries.Skip(1).All(b=>BatteryPower.Amount(b)==0),"Battery output powers only the lamp; empty peer batteries never charge from discharge");
            Put(p.Offset(0,1,0),0);Put(p.Offset(3,1,0),0);
            game.InitializeSaves(Path.Combine(output,"Saves"));game.SetMode(ScreenMode.Pause);Check(game.SaveGame("Renewable checkpoint"),"Save renewable machines and exact stored energy: "+game.SaveStatus);
            long stored=batteries.Sum(BatteryPower.Amount);
            using(var writer=new BinaryWriter(File.Create(Path.Combine(output,"Saves","renewable-fixture.bin")))){writer.Write(game.Survival.Tick);writer.Write(game.Industry.WindGust);writer.Write(stored);}
            var entry=game.Saves.List().First(e=>!e.Backup);Check(game.LoadGame(entry),"Load renewable checkpoint: "+game.SaveStatus);FreezeSaveFixture();game.enabled=false;game.Animals.enabled=false;
            Check(game.Industry.Simulation.Machines.Values.Count(m=>IndustryId.Renewable(m.Definition.Id))==2&&game.Industry.Simulation.Machines.Values.Where(m=>m.Definition.Id==IndustryId.Battery).Sum(BatteryPower.Amount)==stored,"Save/load retains both renewable identities and exact battery energy without offline generation");
        }
    }
}
