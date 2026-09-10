using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewIndustry()
        {
            var world=game.World;var player=game.Player;var sim=game.Industry.Simulation;
            game.Mobs.enabled=false;player.enabled=false;game.Items.enabled=false;game.Diagnostics=false;
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
            Check(game.Inventory.Slots.All(s=>s.Empty),"Ordinary session starts without free industrial items");
            if(CreativeWorkshop){game.SetCreative(true);Check(game.Creative,"Workshop runs with Creative enabled");}
            var origin=world.Address(player.transform.position).Offset(-5,0,6);
            for(int x=-2;x<=13;x++)for(int z=-5;z<=5;z++)for(int y=-1;y<=3;y++)
            {
                var p=origin.Offset(x,y,z);byte b=world.Get(p);if(b!=0)world.Remove(p,b);
                if(y==-1)world.Place(p,BlockId.Stone);
            }
            MachineState Place(int x,int z,byte id,int rotation=0)
            {var p=origin.Offset(x,0,z);Check(PlaceWorkshopItem(p,id),"Place "+game.Registry.Get(id).displayName);var m=sim.At(p);for(int i=0;i<rotation;i++)sim.Rotate(m);return m;}
            var engine=Place(0,0,IndustryId.Boiler);var alternator=Place(1,0,IndustryId.Alternator);
            var crusher=Place(5,0,IndustryId.Crusher);var lever=Place(4,-2,IndustryId.Lever);Place(5,-2,IndustryId.SignalWire);Place(5,-1,IndustryId.SignalWire);Place(4,-3,IndustryId.Indicator,2);
            var hatch=Place(2,-3,IndustryId.Door);var pulse=Place(4,-4,IndustryId.Button);Place(3,-4,IndustryId.Relay,1);Place(2,-4,IndustryId.SignalWire);
            var lamp=Place(3,2,IndustryId.Lamp,2);var drill=Place(7,0,IndustryId.Drill);var pump=Place(9,0,IndustryId.Pump);var tank=Place(10,0,IndustryId.Tank);
            for(int x=1;x<=9;x++)Place(x,1,IndustryId.PowerCable);
            for(int z=-1;z>=-3;z--)Place(6,z,IndustryId.ItemPipe);
            var chest=origin.Offset(7,0,-3);Check(PlaceWorkshopItem(chest,BlockId.Chest),"Place output chest");
            Place(6,0,IndustryId.ItemPipe);
            var bench=origin.Offset(0,0,-3);PlaceWorkshopItem(bench,IndustryId.Bench);PlaceWorkshopItem(origin.Offset(1,0,-3),BlockId.Furnace);
            for(int x=0;x<=11;x++)Place(x,4,IndustryId.FluidPipe);
            for(int z=0;z<4;z++)Place(11,z,IndustryId.FluidPipe);
            for(int z=1;z<4;z++)Place(0,z,IndustryId.FluidPipe);
            world.Remove(origin.Offset(9,-1,0),BlockId.Stone);world.ChangeFluid(origin.Offset(9,-1,0),0,Fluids.Water.Source);
            if(CreativeWorkshop)for(int fixtureSlot=0;fixtureSlot<game.Inventory.Count;fixtureSlot++)game.Inventory.Take(fixtureSlot,int.MaxValue);
            engine.Items.Add(BlockId.Charcoal,8,0,1);engine.WaterMl=10000;crusher.Items.Add(BlockId.RawIron,16,0,1);sim.Activate(lever);
            player.transform.position=world.Local(origin)+new Vector3(6.5f,2.3f,-5);
            player.Camera.transform.position=world.Local(origin)+new Vector3(12,6,-9);player.Camera.transform.LookAt(world.Local(origin)+new Vector3(4.5f,.3f,.8f));
            game.Sky.Clock.SetTime(.40);game.Sky.Apply();
            yield return new WaitForSecondsRealtime(2);Check(!sim.Rebuilding,"Workshop topology settles in the running player");
            Check(alternator.SupplyWatts==400&&crusher.ReceivedWatts>0,"Blender workshop runs on actual boiler/alternator power");
            sim.Activate(pulse);yield return new WaitForSecondsRealtime(.15f);Check(hatch.Running,"Button through delayed relay and wire opens signal-only hatch");
            yield return Capture("industry-workshop-running");
            player.Camera.transform.position=world.Local(origin)+new Vector3(3.1f,2.7f,-4.4f);player.Camera.transform.LookAt(world.Local(origin)+new Vector3(2.7f,.5f,.1f));
            yield return Capture("industry-workshop-close");
            var crusherView=world.GetComponent<IndustryPresentation>().GetComponentsInChildren<Transform>().First(t=>t.name=="Crusher");
            var rotor=crusherView.GetComponentsInChildren<Transform>().First(t=>t.name.StartsWith("MotionSpin"));var moving=rotor.localRotation;
            yield return new WaitForSecondsRealtime(.15f);Check(Quaternion.Angle(moving,rotor.localRotation)>5,"Imported crusher rollers animate while processing");
            sampling=true;yield return new WaitForSecondsRealtime(8);sampling=false;
            Check(game.Survival.At(chest).Storage.Total(IndustryId.CrushedIron)>0,"Powered crusher produces ore delivered through item pipes into chest");
            // Deterministic rendered footage; image I/O is outside the performance sample.
            Time.captureFramerate=30;
            for(int frame=0;frame<180;frame++)
            {
                if(frame==60||frame==105)sim.Activate(lever);
                player.Camera.transform.position=world.Local(origin)+new Vector3(2.9f+frame*.008f,2.4f,-4.9f);
                player.Camera.transform.LookAt(world.Local(origin)+new Vector3(4.6f,.4f,-.2f));
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"industry-motion-"+frame.ToString("000")+".png"));
            }
            Time.captureFramerate=0;
            sim.Activate(lever);yield return new WaitForSecondsRealtime(.15f);
            Check(crusher.Status==MachineStatus.DisabledBySignal&&crusher.RequestedWatts==0,"Player workshop: OFF signal consumes no processing power");
            var stopped=rotor.localRotation;yield return new WaitForSecondsRealtime(.15f);Check(Quaternion.Angle(stopped,rotor.localRotation)<.01f,"Signal-disabled machine stops its actual animated rollers");
            // Reach and ray visibility use the same interaction authority as regular play.
            void Aim(MachineState m)
            {
                player.transform.position=world.Local(m.Position)+new Vector3(.5f,.1f,-2.4f);
                player.Camera.transform.position=world.Local(m.Position)+new Vector3(.5f,3,.5f);
                player.Camera.transform.LookAt(world.Local(m.Position)+Vector3.one*.5f);
            }
            Aim(crusher);Check(game.TryOpenMachine(crusher.Position),"Open machine through normal reach and visibility checks");
            yield return Capture("industry-crusher-disabled-ui");game.SetMode(ScreenMode.Play);
            sim.Activate(lever);yield return new WaitForSecondsRealtime(.15f);Aim(crusher);Check(game.TryOpenMachine(crusher.Position),"Reopen crusher interface");
            game.Inventory.Add(BlockId.RawCopper,4);int slot=game.Inventory.FindSlot(s=>s.Id==BlockId.RawCopper);
            game.UI.ClickSlot(slot,false,true);Check(game.Inventory.Total(BlockId.RawCopper)==4,"Incompatible full input identity does not destroy quick-transferred items");
            yield return Capture("industry-crusher-running-ui");game.SetMode(ScreenMode.Play);
            var breakCable=origin.Offset(4,0,1);Check(world.Remove(breakCable,IndustryId.PowerCable),"Disconnect powered branch");yield return new WaitForSecondsRealtime(.15f);
            Check(crusher.Signal&&crusher.Status==MachineStatus.NoPower&&crusher.ReceivedWatts==0,"Signal can remain ON independently of missing electricity");
            Aim(crusher);Check(game.TryInteractTarget(),"Interact action opens the unpowered machine");yield return Capture("industry-crusher-no-power-ui");game.SetMode(ScreenMode.Play);PlaceWorkshopItem(breakCable,IndustryId.PowerCable);
            yield return new WaitForSecondsRealtime(.25f);Check(crusher.ReceivedWatts>0,"Reconnected cable restores electrical allocation");
            var breakSignal=origin.Offset(5,0,-2);
            Check(world.Remove(breakSignal,IndustryId.SignalWire),"Disconnect Blue Signal branch");yield return new WaitForSecondsRealtime(.2f);
            Check(crusher.SignalAttached&&!crusher.Signal&&crusher.Status==MachineStatus.DisabledBySignal&&alternator.SupplyWatts>0,"Broken signal stops the machine while electricity generation remains available");
            PlaceWorkshopItem(breakSignal,IndustryId.SignalWire);yield return new WaitForSecondsRealtime(.25f);
            Check(crusher.Signal&&crusher.ReceivedWatts>0,"Reconnected Blue Signal restores enabled powered machine");
            foreach(var m in new[]{engine,alternator,pump,drill,tank,lamp})
            {
                Aim(m);Check(game.TryOpenMachine(m.Position),"Open "+m.Definition.Name+" interface");
                yield return Capture("industry-"+m.Definition.Key+"-ui");game.SetMode(ScreenMode.Play);
            }
            Aim(sim.At(bench));Check(game.TryOpenStation(bench)&&game.Crafting.Grid.Size==4,"Machinist opens the real 4 × 4 crafting interface");
            game.Crafting.Grid.Add(BlockId.CopperIngot,1,0,1);ItemStack held=default;
            Check(game.Crafting.CraftToCursor(ref held).Succeeded&&held.Id==IndustryId.CopperWire&&held.Count==4,"Machinist recipe produces four copper wire from one ingot");
            game.Crafting.Grid.Add(IndustryId.CopperWire,1,0,1);game.Crafting.Grid.Add(IndustryId.AzureCrystal,1,1,2);held=default;
            Check(game.Crafting.CraftToCursor(ref held).Succeeded&&held.Id==IndustryId.SignalWire&&held.Count==4,"Copper Wire and Azure Crystal craft four Signal Wire");
            yield return Capture("industry-machinist-ui");game.SetMode(ScreenMode.Play);
            Aim(tank);Check(game.TryOpenMachine(tank.Position),"Open tank for bucket transfer");game.Inventory.Add(Fluids.WaterBucket,1);int before=tank.WaterMl;
            // Pause simulation ticks during pointer transactions so the connected pump cannot change the exact amount.
            float transactionScale=Time.timeScale;Time.timeScale=0;
            if(CreativeWorkshop)yield return BrowserPointer(game.UI.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text=="ADD 10 L"));
            else Check(game.Industry.Bucket(tank,true),"Tank accepts filled bucket");
            Check(tank.WaterMl==before+10000&&game.Inventory.Total(Fluids.EmptyBucket)==1,"Bucket transfer conserves 10 L and returns empty container");
            if(CreativeWorkshop)yield return BrowserPointer(game.UI.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text=="TAKE 10 L"));
            else Check(game.Industry.Bucket(tank,false),"Tank accepts empty bucket");
            Check(tank.WaterMl==before&&game.Inventory.Total(Fluids.WaterBucket)==1,"Tank fill/empty round trip conserves water and bucket");Time.timeScale=transactionScale;game.SetMode(ScreenMode.Play);
            // Stored machine state survives unload. Clock is paused so this measures lifecycle only.
            game.SetMode(ScreenMode.Pause);double savedWork=crusher.Work;int savedOre=crusher.Items.Total(BlockId.RawIron);var saved=WorldPoint.FromLocal(player.transform.position,world.Origin);
            player.transform.position+=Vector3.right*640;yield return null;yield return Settle(120);sim.Step();
            Check(!world.Ready(crusher.Position)&&sim.At(crusher.Position)==crusher&&crusher.Work==savedWork&&crusher.Items.Total(BlockId.RawIron)==savedOre,"Unloaded machine retains its state without offline production");
            player.transform.position=saved.Local(world.Origin);yield return null;yield return Settle(120);sim.Step();
            Check(world.Ready(crusher.Position)&&sim.At(crusher.Position)==crusher,"Machine survives residency and floating-origin round trip");
            Check(errors.Count==0,"Industrial scenario completes without Unity errors");
        }
    }
}
