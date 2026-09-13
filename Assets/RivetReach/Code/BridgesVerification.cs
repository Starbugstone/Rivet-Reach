using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewBridges()
        {
            FreezeSaveFixture();game.SetCreative(true);game.enabled=false;game.Diagnostics=false;
            var world=game.World;var player=game.Player;var sim=game.Industry.Simulation;
            var start=world.Address(player.transform.position);var a=new BlockPos(start.Chunk.Min.X+8,start.Y+2,start.Chunk.Min.Z+8);var b=a.Offset(64,0,0);
            void Put(BlockPos p,byte id)
            {byte old=world.Get(p);if(old!=0)Check(world.Remove(p,old),"Clear bridge fixture");if(id!=0)Check(world.Place(p,id),"Place bridge fixture "+id);}
            foreach(var anchor in new[]{a,b})
                for(int x=-4;x<=6;x++)for(int z=-4;z<=10;z++)for(int y=-1;y<=3;y++)Put(anchor.Offset(x,y,z),y==-1?BlockId.Stone:(byte)0);
            MachineState Place(BlockPos p,byte id){Put(p,id);return sim.At(p);}
            foreach(var anchor in new[]{a,b})
            {
                Place(anchor,IndustryId.PowerBridge);Place(anchor.Offset(0,0,4),IndustryId.ItemBridge);Place(anchor.Offset(0,0,8),IndustryId.FluidBridge);
                Place(anchor.Offset(3,0,4),IndustryId.ChunkLoader);
            }
            foreach(int z in new[]{0,4,8})
            {Check(sim.ConfigureBridge(sim.At(a.Offset(0,0,z)),sim.LocalOwnerId,"Quarry",out _),"Name first bridge");Check(sim.ConfigureBridge(sim.At(b.Offset(0,0,z)),sim.LocalOwnerId,"Quarry",out _),"Name second bridge");}
            Place(a.Offset(-1,0,0),IndustryId.PowerCable);var battery=Place(a.Offset(-2,0,0),IndustryId.Battery);battery.EnergyCells[0].Charge(100000000);
            Place(b.Offset(1,0,0),IndustryId.PowerCable);var lamp=Place(b.Offset(2,0,0),IndustryId.Lamp);
            var sourcePipe=Place(a.Offset(-1,0,4),IndustryId.ItemPipe);Place(b.Offset(1,0,4),IndustryId.ItemPipe);
            Put(a.Offset(-2,0,4),BlockId.Chest);Put(b.Offset(2,0,4),BlockId.Chest);
            var source=game.Survival.At(a.Offset(-2,0,4)).Storage;source.Add(BlockId.FloaterRock,12);Check(sim.TogglePipeEnd(sourcePipe,1),"Configure cargo source end");
            Place(a.Offset(-1,0,8),IndustryId.FluidPipe);Place(b.Offset(1,0,8),IndustryId.FluidPipe);
            var tankA=Place(a.Offset(-2,0,8),IndustryId.Tank);var tankB=Place(b.Offset(2,0,8),IndustryId.Tank);tankA.Fluid.Deposit(Fluids.Lava,10000);
            for(int i=0;i<20;i++)sim.Step();
            Check(lamp.ReceivedWatts==20&&battery.EnergyCells[0].Amount==99980000,"Native remote grid conserves exactly 20 J over one second");
            Check(source.Total(BlockId.FloaterRock)==8&&game.Survival.At(b.Offset(2,0,4)).Storage.Total(BlockId.FloaterRock)==4,"Native item bridges transfer four exact rocks");
            Check(tankA.Fluid.Amount==8000&&tankB.Fluid.Amount==2000&&tankB.Fluid.Fluid==Fluids.Lava,"Native liquid bridges transfer two exact litres of lava");
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);player.HeldBlock.enabled=false;
            player.transform.position=world.Local(a)+new Vector3(.5f,0,-2);
            player.Camera.transform.position=world.Local(a)+new Vector3(5,5,-5);player.Camera.transform.LookAt(world.Local(a)+new Vector3(0,.6f,3));
            game.SetMode(ScreenMode.Play);game.Sky.Clock.SetTime(.35);game.Sky.Apply();yield return new WaitForSecondsRealtime(.6f);
            var presentation=UnityEngine.Object.FindAnyObjectByType<IndustryPresentation>();Check(presentation.ViewAt(a)!=null,"Imported bridge renders in actual player");
            yield return Capture("bridges-source-networks");
            player.Camera.transform.position=world.Local(a)+new Vector3(.5f,1.6f,-2);player.Camera.transform.LookAt(world.Local(a)+Vector3.one*.5f);
            Check(game.TryOpenMachine(a),"Interact opens bridge name controls");yield return new WaitForSecondsRealtime(.3f);
            var field=game.UI.VisibleRoot.GetComponentsInChildren<InputField>().Single(f=>f.characterLimit==32);Check(field.text=="Quarry","Bridge field binds actual saved name");
            field.text="Temporary";game.UI.VisibleRoot.GetComponentsInChildren<Button>().Single(x=>x.GetComponentInChildren<Text>().text=="APPLY NAME").onClick.Invoke();
            Check(sim.At(a).LinkName=="Temporary","UI apply button invokes authoritative rename");field.text="Quarry";
            game.UI.VisibleRoot.GetComponentsInChildren<Button>().Single(x=>x.GetComponentInChildren<Text>().text=="APPLY NAME").onClick.Invoke();sim.Step();yield return new WaitForSecondsRealtime(.3f);
            yield return Capture("bridge-name-and-partner");game.SetMode(ScreenMode.Play);
            var loader=sim.At(a.Offset(3,0,4));player.transform.position=world.Local(loader.Position)+new Vector3(.5f,0,-2);
            player.Camera.transform.position=world.Local(loader.Position)+new Vector3(.5f,1.6f,-2);player.Camera.transform.LookAt(world.Local(loader.Position)+Vector3.one*.5f);
            Check(game.TryOpenMachine(loader.Position),"Interact opens chunk loader controls");yield return new WaitForSecondsRealtime(.3f);yield return Capture("chunk-loader-controls");game.SetMode(ScreenMode.Play);
            // An unrelated resident chunk must unload while both installations stay active.
            var ordinary=a.Offset(32,0,0);Check(world.Ready(ordinary),"Unticketed comparison chunk initially resident");
            player.transform.position=world.Local(a)+new Vector3(768,2,0);yield return Settle(120);
            Check(world.Ready(a)&&world.Ready(b)&&!world.Ready(ordinary),"Distant loader tickets retain both installations while ordinary terrain unloads");
            long before=battery.EnergyCells[0].Amount;for(int i=0;i<20;i++)sim.Step();Check(lamp.ReceivedWatts==20&&battery.EnergyCells[0].Amount==before-20000,"Distant electrical network remains active beyond player range");
            string owner=sim.LocalOwnerId;game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"Saves"));
            long savedEnergy=battery.EnergyCells[0].Amount;Check(game.SaveGame("Remote bridge installation"),"Save bridge ownership, names and loader tickets");
            Check(game.LoadGame(game.Saves.List().First(e=>!e.Backup)),"Restore distant installation checkpoint");FreezeSaveFixture();game.enabled=false;
            world=game.World;player=game.Player;sim=game.Industry.Simulation;world.ViewDistance=4;yield return Settle(120);
            Check(game.Survival.At(a.Offset(-2,0,4)).Storage.Total(BlockId.FloaterRock)+game.Survival.At(b.Offset(2,0,4)).Storage.Total(BlockId.FloaterRock)==12&&sim.At(a.Offset(-2,0,8)).Fluid.Amount+sim.At(b.Offset(2,0,8)).Fluid.Amount==10000&&sim.At(b.Offset(2,0,8)).Fluid.Fluid==Fluids.Lava,"Save/load conserves all linked cargo and liquid contents");
            Check(sim.LocalOwnerId==owner&&sim.At(a).OwnerId==owner&&sim.At(a).LinkName=="Quarry","Durable player and bridge identities survive reload");
            Check(world.Ready(a)&&world.Ready(b)&&!world.Ready(ordinary),"Saved loader tickets restore distant residency without a player visit");
            battery=sim.At(a.Offset(-2,0,0));lamp=sim.At(b.Offset(2,0,0));Check(battery.EnergyCells[0].Amount==savedEnergy,"Reload adds no offline energy or work");sim.Step();Check(lamp.ReceivedWatts==20&&battery.EnergyCells[0].Amount==savedEnergy-1000,"Restored remote graph resumes exact allocation");
            // Return to the receiver for visual evidence and disable its loader through authority.
            player.transform.position=world.Local(b)+new Vector3(.5f,0,-2);yield return Settle(120);sim.Step();
            player.Camera.transform.position=world.Local(b)+new Vector3(5,5,-5);player.Camera.transform.LookAt(world.Local(b)+new Vector3(0,.6f,3));game.SetMode(ScreenMode.Play);
            yield return new WaitForSecondsRealtime(.6f);sim.Step();Check(!sim.Rebuilding&&lamp.ReceivedWatts==20,"Receiver capture shows the settled, powered bridge network");yield return new WaitForSecondsRealtime(.3f);yield return Capture("bridges-receiver-networks");
            var remoteLoader=sim.At(b.Offset(3,0,4));Check(sim.ConfigureLoader(remoteLoader,sim.LocalOwnerId,false),"Owner disables receiver loader");world.RefreshChunkTickets();
            player.transform.position=world.Local(a)+new Vector3(768,2,0);yield return Settle(120);sim.Step();before=battery.EnergyCells[0].Amount;sim.Step();
            Check(world.Ready(a)&&!world.Ready(b)&&lamp.ReceivedWatts==0&&battery.EnergyCells[0].Amount==before,"Disabled remote loader releases residency and stops bridge transfer safely");
            game.SetMode(ScreenMode.Pause);
            var preservedWorld=world;var bridge=sim.At(a);string validName=bridge.LinkName;bridge.LinkName="<invalid>";
            Check(game.SaveGame("Invalid bridge fixture",true),"Write checksum-valid invalid bridge fixture");
            var invalid=game.Saves.List().First(e=>e.Id==game.SaveId&&!e.Backup);bridge.LinkName=validName;
            Check(!game.LoadGame(invalid)&&game.World==preservedWorld&&sim.At(a).LinkName==validName,"Invalid bridge save rejects restoration and rolls back live world");
            var args=Environment.GetCommandLineArgs();int legacyIndex=Array.IndexOf(args,"-rr-bridge-legacy-directory");
            if(legacyIndex>=0)
            {
                game.InitializeSaves(args[legacyIndex+1]);var oldSave=game.Saves.List().FirstOrDefault(e=>!e.Backup);
                Check(oldSave!=null,"Historical pre-bridge schema-8 checkpoint passes full content fingerprint");
                Check(game.LoadGame(oldSave),"Actual pre-bridge checkpoint loads with additive owner defaults");FreezeSaveFixture();game.enabled=false;
                yield return Settle(120);Check(!game.Industry.Simulation.LoaderChunks().Any()&&game.Industry.Simulation.LocalOwnerId.Length==32,"Legacy world gains no accidental loader tickets and has a valid owner identity");
            }
            game.enabled=true;
        }
    }
}
