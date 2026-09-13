using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewCompost()
        {
            FreezeSaveFixture();game.enabled=false;game.SetCreative(false);yield return Settle();
            var world=game.World;var player=game.Player;var sim=game.Industry.Simulation;
            void AdvanceCrops(int ticks)
            {
                game.Survival.AdvanceTicks(ticks);
                // Service the bounded growth queue at the same tick; natural plants may precede the fixture.
                int passes=(game.Survival.ScheduledCrops+15)/16+1;
                for(int i=0;i<passes;i++)game.Survival.AdvanceTicks(0);
            }
            var p=world.Address(player.transform.position).Offset(0,3,4);
            void Put(BlockPos pos,byte id)
            {byte old=world.Get(pos);if(old==id)return;if(old!=0)Check(world.Remove(pos,old),"Clear compost fixture cell");if(id!=0)Check(world.Place(pos,id),"Place compost fixture "+id);}
            for(int x=-5;x<=5;x++)for(int z=-3;z<=4;z++)
            {Put(p.Offset(x,-1,z),BlockId.Dirt);for(int y=0;y<=3;y++)Put(p.Offset(x,y,z),0);}
            player.transform.position=world.Local(p)+new Vector3(.5f,.02f,-2.2f);player.ResetMotion();game.Sky.Clock.SetTime(.4);game.Sky.Apply();
            void Aim(BlockPos cell)
            {player.Camera.transform.position=world.Local(cell)+new Vector3(.5f,1.2f,-2);player.Camera.transform.LookAt(world.Local(cell)+new Vector3(.5f,.35f,.5f));}
            var binPos=p;Put(binPos,CompostId.Bin);var bin=sim.At(binPos);bin.Items.Add(BlockId.Potato,24,0,1);for(int i=0;i<80;i++)sim.Step();
            Check(bin.Work==80&&bin.Items.Slots[0].Count==24&&bin.Items.Slots[2].Empty,"Bin retains complete ingredients during partial work");
            game.SetMode(ScreenMode.Play);Aim(binPos);yield return null;Check(game.TryInteractTarget(),"Interact opens compost bin through normal targeting");yield return new WaitForSecondsRealtime(.4f);
            Check(game.OpenMachine==bin&&game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text=="ORGANIC INPUT"),"Bin interface exposes organic input and compost output");yield return Capture("compost-bin-processing");
            game.SetMode(ScreenMode.Play);player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);player.HeldBlock.enabled=false;
            player.Camera.transform.position=world.Local(binPos)+new Vector3(2.3f,1.8f,-2.4f);player.Camera.transform.LookAt(world.Local(binPos)+Vector3.one*.45f);yield return new WaitForSecondsRealtime(.5f);yield return Capture("compost-bin-filled");
            var wild=p.Offset(-3,0,1);var farm=p.Offset(-2,0,1);Check(world.Till(wild.Offset(0,-1,0))&&world.Plant(wild,FarmId.WheatSeed),"Plant timed wheat fixture");Check(world.Till(farm.Offset(0,-1,0))&&world.Plant(farm,FarmId.CarrotSeed),"Cultivated carrot planted beside timed wheat");
            AdvanceCrops(1199);Check(world.Get(wild)==FarmId.WheatPlant&&world.Get(farm)==FarmId.CarrotPlant,"Crops still immature just before original deadline");
            game.Inventory.Add(CompostId.Compost,8,0,1);game.Selected=0;Aim(wild);Check(game.TryUseCompost(),"Selected compost accelerates immature wheat through raycast");
            Check(world.Get(wild)==FarmId.WheatPlant+1&&game.Inventory.Total(CompostId.Compost)==7,"Successful use consumes exactly one compost and one stage");
            AdvanceCrops(1);Check(world.Get(wild)==FarmId.WheatPlant+1&&world.Get(farm)==FarmId.CarrotPlant+1,"Old wheat deadline was replaced while ordinary cultivated growth continued");
            Aim(farm);Check(game.TryUseCompost(),"Compost also accelerates cultivated crop");Check(world.Get(farm)==FarmId.CarrotPlant+2,"Cultivated growth advances exactly one stage");
            player.Camera.transform.position=world.Local(wild)+new Vector3(-1.5f,1.4f,-2.6f);player.Camera.transform.LookAt(world.Local(wild)+new Vector3(1,.25f,.5f));yield return Capture("compost-crop-growth");
            // Save the partially processed batch and replaced crop deadline together.
            game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"Saves"));Check(game.SaveGame("Compost checkpoint"),"Save bin inputs, partial processing and accelerated crop schedule: "+game.SaveStatus);
            var stone=game.Registry.Get(BlockId.Stone);float originalSeconds=stone.fistSeconds;
            try{stone.fistSeconds+=.125f;Check(new SaveStore(game.Saves.DirectoryPath,game.Registry).List().Count==0,"Compost compatibility still rejects unrelated changed item definitions");}
            finally{stone.fistSeconds=originalSeconds;}
            var entry=game.Saves.List().First(e=>!e.Backup);Check(game.LoadGame(entry),"Reload compost checkpoint: "+game.SaveStatus);FreezeSaveFixture();game.enabled=false;world=game.World;sim=game.Industry.Simulation;player=game.Player;yield return Settle(120);game.enabled=true;yield return null;game.enabled=false;bin=sim.At(binPos);
            Check(bin.Work==80&&bin.Items.Slots[0].Count==24&&game.Inventory.Total(CompostId.Compost)==6,"Save restores exact machine and carried compost state");
            AdvanceCrops(1198);Check(world.Get(wild)==FarmId.WheatPlant+1,"Reload preserves the replacement deadline before its due tick");AdvanceCrops(1);Check(world.Get(wild)==FarmId.WheatPlant+2,"Accelerated wheat crop resumes natural growth at correct tick");AdvanceCrops(1);
            for(int i=0;i<120;i++)sim.Step();Check(bin.Items.Slots[2].Count==1&&bin.Items.Slots[0].Count==12,"Loaded batch produces exactly one compost");
            game.SetMode(ScreenMode.Play);Aim(farm);int count=game.Inventory.Total(CompostId.Compost);Check(!game.TryUseCompost()&&game.Inventory.Total(CompostId.Compost)==count,"Mature crop rejects compost without consuming it");
            Aim(p.Offset(4,0,1));Check(!game.TryUseCompost()&&game.Inventory.Total(CompostId.Compost)==count,"Invalid target consumes nothing");
            BlockPos? natural=null;
            for(int z=-55;z<=55&&!natural.HasValue;z++)for(int x=-55;x<=55;x++)
            {var cell=new BlockPos(x,world.Generator.Height(x,z)+1,z);byte id=world.Get(cell);var crop=CropRules.For(id);if(world.Ready(cell)&&crop!=null&&id<crop.Mature&&world.Get(cell.Offset(0,-1,0))==BlockId.Grass&&world.SkyLight(cell)>=9){natural=cell;break;}}
            Check(natural.HasValue,"Find genuinely generated immature wild plant on natural grass");byte naturalStage=world.Get(natural.Value);Aim(natural.Value);count=game.Inventory.Total(CompostId.Compost);
            Check(game.TryUseCompost()&&world.Get(natural.Value)==naturalStage+1&&game.Inventory.Total(CompostId.Compost)==count-1,"Real wild plant accepts exactly one compost without farmland");yield return Capture("compost-wild-growth");
            game.Selected=1;Aim(wild);Check(!game.TryUseCompost(),"Empty hand cannot accelerate crops");game.Selected=0;
            var shaded=p.Offset(-4,0,1);Check(world.Till(shaded.Offset(0,-1,0))&&world.Plant(shaded,FarmId.FlaxSeed),"Plant flax use fixture");Put(shaded.Offset(0,2,0),BlockId.Stone);Aim(shaded);Check(!game.TryUseCompost()&&world.Get(shaded)==FarmId.FlaxPlant,"Compost respects crop skylight requirement");Put(shaded.Offset(0,2,0),0);
            // Real input dispatch: a held mouse press applies once; a second click is required.
            player.transform.position=world.Local(shaded)+new Vector3(.5f,.02f,-2);player.enabled=true;player.Yaw=0;player.Pitch=25;player.ResetMotion();yield return null;
            Vector3 direction=(world.Local(shaded)+new Vector3(.5f,.35f,.5f)-player.Camera.transform.position).normalized;player.Yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(direction.y)*Mathf.Rad2Deg;yield return null;
            count=game.Inventory.Total(CompostId.Compost);InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSecondsRealtime(.5f);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;player.enabled=false;
            Check(world.Get(shaded)==FarmId.FlaxPlant+1&&game.Inventory.Total(CompostId.Compost)==count-1,"Actual held right-click applies compost once, without repeated stage spending");yield return Capture("compost-use-in-hand");
            game.SetCreative(true);Aim(shaded);count=game.Inventory.Total(CompostId.Compost);Check(game.TryUseCompost()&&game.Inventory.Total(CompostId.Compost)==count,"Creative accelerates without consuming compost");game.SetCreative(false);
            // Shared pipes: source chest -> input end, output end -> destination chest.
            Put(binPos.Offset(-1,0,0),IndustryId.ItemPipe);Put(binPos.Offset(-2,0,0),BlockId.Chest);Put(binPos.Offset(1,0,0),IndustryId.ItemPipe);Put(binPos.Offset(2,0,0),BlockId.Chest);
            var source=game.Survival.At(binPos.Offset(-2,0,0)).Storage;source.Add(BlockId.Potato,24);var destination=game.Survival.At(binPos.Offset(2,0,0)).Storage;
            void SetEnd(MachineState pipe,int face,PortRole role){for(int i=0;i<3&&sim.PipeEndRole(pipe,face)!=role;i++)Check(sim.TogglePipeEnd(pipe,face),"Configure compost pipe direction");Check(sim.PipeEndRole(pipe,face)==role,"Requested pipe role applied");}
            var ip=sim.At(binPos.Offset(-1,0,0));var op=sim.At(binPos.Offset(1,0,0));SetEnd(ip,1,PortRole.Output);SetEnd(ip,0,PortRole.Input);SetEnd(op,1,PortRole.Output);SetEnd(op,0,PortRole.Input);
            for(int i=0;i<1200;i++)sim.Step();Check(source.Total(BlockId.Potato)==0&&bin.Items.Slots[0].Empty&&destination.Total(CompostId.Compost)==4,"Shared pipes conserve 48 potatoes into four compost across input and output storage");
            game.SetMode(ScreenMode.Pause);player.enabled=true;yield return null;player.enabled=false;
            game.Inventory.Add(IndustryId.Wrench,1,1,2);game.Selected=1;game.SetMode(ScreenMode.Play);
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);player.HeldBlock.enabled=false;player.HeldBlock.PrepareFrame(0);
            game.Notify("",0);Check(player.Camera.isActiveAndEnabled&&game.Mode==ScreenMode.Play,"Connection capture retains active gameplay camera");
            player.Camera.transform.position=world.Local(binPos)+new Vector3(3.6f,2.5f,-4.7f);player.Camera.transform.LookAt(world.Local(binPos)+new Vector3(.5f,.35f,.5f));yield return new WaitForSecondsRealtime(.5f);yield return Capture("compost-pipe-setup");
            bin.Items.Add(FarmId.WheatSeed,24,0,1);for(int i=0;i<50;i++)sim.Step();bin.Items.Add(CompostId.Compost,2,2,3);int beforeCompost=game.Items.Total(CompostId.Compost);int beforeSeeds=game.Items.Total(FarmId.WheatSeed),beforeBins=game.Items.Total(CompostId.Bin);Check(world.Mine(binPos,CompostId.Bin,ToolCapability.None,ToolTier.None),"Mine compost bin with unpaid partial work");
            Check(game.Items.Total(FarmId.WheatSeed)==beforeSeeds+24&&game.Items.Total(CompostId.Bin)==beforeBins+1&&game.Items.Total(CompostId.Compost)==beforeCompost+2&&sim.At(binPos)==null,"Mining returns all queued inputs, finished compost and exactly one bin");
            Check(!world.Mine(binPos,CompostId.Bin,ToolCapability.None,ToolTier.None)&&game.Items.Total(FarmId.WheatSeed)==beforeSeeds+24,"Repeated removal cannot duplicate recovery");
            game.SetMode(ScreenMode.Inventory);game.UI.InspectBrowserItem(CompostId.Bin,false);yield return Capture("compost-bin-recipe");game.UI.CloseBrowserRecipe();
            game.UI.InspectBrowserItem(CompostId.Compost,false);yield return Capture("compost-conversion-recipe");game.UI.CloseBrowserRecipe();
        }
    }
}
