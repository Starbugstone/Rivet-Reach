using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class VoxelWorld
    {
        internal bool GuidanceFixtureChange(BlockPos p,byte id)=>Change(p,Get(p),id,false,false);
    }

    // Opt-in native acceptance fixture. Never runs in an ordinary expedition.
    public sealed class AlphaGuidanceVerification : MonoBehaviour
    {
        [Serializable] sealed class Report {public string timestamp;public List<string> checks=new List<string>(),errors=new List<string>();}
        Expedition game;string output;readonly Report report=new Report();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install(){if(Environment.GetCommandLineArgs().Contains("-rr-guidance-review"))new GameObject("Guidance acceptance").AddComponent<AlphaGuidanceVerification>();}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rr-output");output=i>=0?args[i+1]:Path.Combine(Application.persistentDataPath,"GuidanceReview");Directory.CreateDirectory(output);
            Application.runInBackground=true;Application.logMessageReceived+=OnLog;yield return null;game=Expedition.Instance;
            yield return VerificationCoroutines.Run(Run(),e=>report.errors.Add(e.ToString()));
            report.timestamp=DateTime.UtcNow.ToString("O");File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
            Application.Quit(report.errors.Count==0?0:1);
        }
        void OnLog(string message,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)report.errors.Add(message+"\n"+stack);}
        void Check(bool ok,string label)
        {
            if(!ok)throw new Exception(label);report.checks.Add(label);Debug.Log("PASS "+label);
            File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(report,true));
        }
        IEnumerator Settle()
        {
            float end=Time.realtimeSinceStartup+180;
            while((!game.ReadyToPlay||game.World.PendingCount>0)&&Time.realtimeSinceStartup<end)yield return null;
            Check(game.ReadyToPlay&&game.World.PendingCount==0,"Terrain settles for acceptance viewpoint");yield return new WaitForSecondsRealtime(1);
        }
        IEnumerator Capture(string name)
        {game.Notify("",0);game.Player.HeldBlock.PrepareFrame(1);game.Player.Arms.gameObject.SetActive(!game.InventoryOpen&&!game.Paused);yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.4f);}
        void Freeze()
        {
            game.enabled=false;game.Player.enabled=false;game.Items.enabled=false;game.Mobs.enabled=false;game.Animals.enabled=false;
            game.Mobs.NaturalSpawning=false;game.Animals.NaturalSpawning=false;game.World.ViewDistance=4;game.Diagnostics=false;
            game.Player.Camera.transform.localPosition=Vector3.up*1.64f;
        }
        void Aim(Vector3 point,Vector3 feet)
        {
            var player=game.Player;player.transform.position=feet;Vector3 direction=point-(feet+Vector3.up*1.64f);
            var look=Quaternion.LookRotation(direction);player.Yaw=look.eulerAngles.y;player.Pitch=look.eulerAngles.x;
            player.transform.rotation=Quaternion.Euler(0,player.Yaw,0);player.Camera.transform.rotation=look;
        }
        void Put(BlockPos p,byte id)
        {byte before=game.World.Get(p);if(before!=id)Check(game.World.GuidanceFixtureChange(p,id),"Fixture cell "+p+" = "+id);}
        IEnumerator Ticks(int count)
        {for(int i=0;i<count;i+=5){game.Industry.Advance(Math.Min(5,count-i));yield return null;}}
        IEnumerator Run()
        {
            game.StartSession(246813);Freeze();game.SetMode(ScreenMode.Play);
            var world=game.World;int y=world.Generator.Height(31,18)+4;
            game.Player.transform.position=new Vector3(31.5f,y+.01f,12.5f);yield return Settle();
            game.Sky.Clock.SetTime(.34);game.Weather.SetWeather(WeatherKind.Clear,true);game.Sky.Apply();
            // A supplied construction fixture follows the separate empty-handed route.
            for(int x=24;x<=40;x++)for(int z=8;z<=26;z++)for(int h=-1;h<=5;h++)
            {var p=new BlockPos(x,y+h,z);byte id=h==-1?BlockId.Stone:(byte)0;if(world.Get(p)!=id)world.GuidanceFixtureChange(p,id);}
            yield return Settle();
            var lower=new BlockPos(31,y,12);game.Inventory.Add(BuildingBlocks.WoodenSlab,4,0,1);game.Selected=0;
            Aim(world.Local(lower)+new Vector3(.5f,0,.5f),world.Local(lower)+new Vector3(.5f,.01f,-2));yield return null;
            Check(game.TryPlaceSelected()&&world.Get(lower)==BuildingBlocks.WoodenSlab&&game.Inventory.Slots[0].Count==3,"Ordinary placement consumes one lower Wooden Slab");
            Check(!world.Overlaps(world.Local(lower)+new Vector3(.5f,.501f,.5f),.6f,1.8f)&&world.Overlaps(world.Local(lower)+new Vector3(.5f,.25f,.5f),.6f,1.8f),"Player stands on half height without occupying slab");
            var stepped=world.Move(world.Local(lower)+new Vector3(-.5f,.002f,.5f),new Vector3(1.1f,-.01f,0),.6f,1.8f,out _,.5f);
            Check(stepped.y-world.Local(lower).y>.49f&&stepped.x-world.Local(lower).x>.3f,"Walking steps onto a half slab without jumping");
            Aim(world.Local(lower)+new Vector3(.5f,.5f,.5f),world.Local(lower)+new Vector3(.5f,.01f,-2));yield return null;
            Check(game.TryPlaceSelected()&&world.Get(lower)==BuildingBlocks.WoodenDouble&&game.Inventory.Slots[0].Count==2,"Second matching slab combines and consumes exactly one");
            int drops=game.Items.Total(BuildingBlocks.WoodenSlab);
            Check(world.Mine(lower,BuildingBlocks.WoodenDouble,ToolCapability.None)&&game.Items.Total(BuildingBlocks.WoodenSlab)==drops+2,"Mining combined slab recovers exactly two public items");
            var upper=lower.Offset(2,0,0);Put(upper.Offset(0,1,0),BlockId.Stone);
            Aim(world.Local(upper)+new Vector3(.5f,1,.5f),world.Local(upper)+new Vector3(.5f,-1,-2));yield return null;
            Check(game.TryPlaceSelected()&&world.Get(upper)==BuildingBlocks.WoodenUpper,"Underside placement creates an upper slab across chunk boundary");
            Put(upper.Offset(0,1,0),0);
            Check(world.Select(world.Local(upper)+new Vector3(.5f,.25f,-1),Vector3.forward,3,out var gap)==false||!gap.Position.Equals(upper),"Aim passes through empty space below upper slab");
            Put(lower,BuildingBlocks.WoodenSlab);var stone=lower.Offset(0,0,2);Put(stone,BuildingBlocks.StoneSlab);
            Check(!world.Mine(stone,BuildingBlocks.StoneSlab,ToolCapability.None,ToolTier.None)&&world.Mine(stone,BuildingBlocks.StoneSlab,ToolCapability.Pickaxe,ToolTier.Wood),"Stone slab preserves wooden-pickaxe mining gate");Put(stone,BuildingBlocks.StoneSlab);
            // Connected window, wrapping corner, half-height roof and porch.
            for(int x=26;x<=38;x++)for(int h=0;h<=4;h++)Put(new BlockPos(x,y+h,18),h==0?BuildingBlocks.StoneDouble:h==4?BuildingBlocks.WoodenSlab:x==26||x==38?BlockId.Planks:IndustryId.Glass);
            for(int z=19;z<=24;z++)for(int h=0;h<=4;h++)Put(new BlockPos(38,y+h,z),h==0?BuildingBlocks.StoneDouble:h==4?BuildingBlocks.WoodenSlab:z==24?BlockId.Planks:IndustryId.Glass);
            for(int x=26;x<=38;x++)for(int z=19;z<=24;z++)Put(new BlockPos(x,y+4,z),BuildingBlocks.WoodenSlab);
            for(int x=26;x<=37;x++)Put(new BlockPos(x,y,17),BuildingBlocks.StoneSlab);
            for(int x=26;x<=38;x++)for(int z=19;z<=24;z++)Put(new BlockPos(x,y,z),BlockId.Stone);
            for(int x=26;x<=38;x++)for(int h=1;h<=3;h++)Put(new BlockPos(x,y+h,24),x>=30&&x<=34&&h>=2?IndustryId.Glass:BlockId.Planks);
            var boilerPos=new BlockPos(29,y+1,21);var altPos=boilerPos.Offset(1,0,0);var crusherPos=new BlockPos(33,y+1,21);
            Put(boilerPos,IndustryId.Boiler);Put(altPos,IndustryId.Alternator);Put(crusherPos,IndustryId.Crusher);
            for(int x=30;x<=33;x++)Put(new BlockPos(x,y+1,20),IndustryId.PowerCable);
            var sim=game.Industry.Simulation;var boiler=sim.At(boilerPos);var alternator=sim.At(altPos);var crusher=sim.At(crusherPos);
            crusher.Items.Add(BlockId.RawIron,8,0,1);yield return Ticks(100);
            Check(crusher.Status==MachineStatus.NoPower&&MachineSetupFeedback.For(crusher,sim).Contains("cable is connected"),"Connected cable without supply gives actionable power feedback");
            Aim(world.Local(crusherPos)+Vector3.one*.5f,world.Local(crusherPos)+new Vector3(.5f,.01f,2.5f));yield return null;
            Check(game.TryInteractTarget()&&game.OpenMachine==crusher,"Open actual machine interface through normal targeting");yield return Capture("crusher-connected-no-supply");game.SetMode(ScreenMode.Play);yield return null;
            boiler.Items.Add(BlockId.Coal,3,0,1);boiler.WaterMl=50000;yield return Ticks(300);
            Check(alternator.SupplyWatts>0&&crusher.Items.Slots[crusher.OutputSlot].Count>0,"Boiler, shaft, cable and Crusher produce actual output");
            var drillPos=new BlockPos(35,y+2,21);var minedSlab=drillPos.Offset(0,-1,0);
            Put(minedSlab,BuildingBlocks.WoodenDouble);Put(drillPos,IndustryId.Drill);
            for(int x=33;x<=35;x++)Put(new BlockPos(x,y+2,20),IndustryId.PowerCable);
            var drill=sim.At(drillPos);drill.Items.Add(BuildingBlocks.WoodenSlab,63,2,3);yield return Ticks(150);
            Check(world.Get(minedSlab)==BuildingBlocks.WoodenDouble&&drill.Status==MachineStatus.OutputFull,"Drill reserves room for both slabs before extracting a combined block");
            drill.Items.Take(2,1);yield return Ticks(160);
            Check(world.Get(minedSlab)==0&&drill.Items.Slots[2].Count==64,"Drill recovers exactly two slabs without overflow");
            Aim(world.Local(boilerPos)+Vector3.one*.5f,world.Local(boilerPos)+new Vector3(.5f,.01f,2.5f));yield return null;
            Check(game.TryInteractTarget()&&game.OpenMachine==boiler,"Open Boiler setup feedback");yield return Capture("boiler-shaft-fuel-water-guide");game.SetMode(ScreenMode.Play);yield return null;
            game.Selected=1;Aim(new Vector3(32,y+2.1f,21),new Vector3(30,y+1.25f,11));yield return Settle();yield return Capture("connected-glass-workshop");
            Aim(new Vector3(36,y+2,21),new Vector3(42,y+1.5f,15));yield return Capture("connected-glass-corner-and-slabs");
            var glassCell=new BlockPos(32,y+2,18);int glassDrops=game.Items.Total(IndustryId.Glass);
            Check(world.Mine(glassCell,IndustryId.Glass,ToolCapability.None)&&game.Items.Total(IndustryId.Glass)==glassDrops+1,"Glass mining recovers one existing Glass item");yield return Settle();
            Aim(new Vector3(32,y+2,18),new Vector3(31,y+.45f,14));yield return Capture("glass-removed-edge-update");Put(glassCell,IndustryId.Glass);
            // A real bound bed supplies Home; no separate waypoint authority.
            var bedPos=new BlockPos(26,y+1,21);Check(world.PlaceBed(bedPos,0),"Place home Bed on full supporting floor");
            Aim(world.Local(bedPos)+new Vector3(.5f,.6f,.5f),world.Local(bedPos)+new Vector3(2.5f,.01f,.5f));yield return null;
            Check(game.TryInteractTarget()&&game.Beds.Home.Equals(bedPos),"Normal bed use binds navigation to placed identity");
            var deathPos=new BlockPos(29,y,12);game.Player.transform.position=world.Local(deathPos)+new Vector3(.5f,.01f,.5f);game.SetMode(ScreenMode.Play);yield return null;
            Check(game.TakeDamage(1000,DamageKind.Impact)>0&&game.Health.Dead&&game.Navigation.LastDeath.Equals(deathPos),"Actual Survival death records the death marker once");
            game.Respawn();Freeze();game.SetMode(ScreenMode.Play);yield return null;
            Aim(new Vector3(32,y+2,18),new Vector3(31,y+1.25f,10));yield return Capture("home-and-death-navigation");
            game.SetMode(ScreenMode.Inventory);yield return null;game.UI.InspectBrowserItem(BlockId.DiamondOre,false);yield return Capture("diamond-depth-and-tool-cue");game.SetMode(ScreenMode.Pause);
            game.InitializeSaves(Path.Combine(output,"Saves"));Check(game.SaveGame("Guidance and building"),"Save current navigation, slabs, glass, home and machines: "+game.SaveStatus);
            var entry=game.Saves.List().First(e=>!e.Backup);long homeIdentity=game.Beds.HomeIdentity;
            Check(game.LoadGame(entry),"Load current full checkpoint: "+game.SaveStatus);Freeze();yield return Settle();
            Check(game.World.Get(lower)==BuildingBlocks.WoodenSlab&&game.World.Get(upper)==BuildingBlocks.WoodenUpper&&game.World.Get(glassCell)==IndustryId.Glass,"Load restores exact building variants and glass");
            Check(game.Navigation.LastDeath.Equals(deathPos)&&game.Beds.HomeIdentity==homeIdentity,"Load restores death marker and original bound home identity");
            byte[] bytes=File.ReadAllBytes(entry.Path),body;using(var reader=game.Saves.Open(bytes,out _))body=reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position));
            var intactWorld=game.World;var intactNavigation=game.Navigation;
            File.WriteAllBytes(entry.Path,game.Saves.Encode(entry,w=>w.Write(body,0,body.Length-1)));
            Check(!game.LoadGame(entry)&&game.World==intactWorld&&game.Navigation==intactNavigation&&game.Navigation.LastDeath.Equals(deathPos),"Late navigation corruption rolls back original world and markers");File.WriteAllBytes(entry.Path,bytes);
            game.Navigation.ClearDeath();Check(!game.Navigation.LastDeath.HasValue,"Dismiss death marker without changing dropped items");
            Check(game.World.Mine(bedPos,BedId.Bed,ToolCapability.None),"Remove home bed");Check(game.World.BedAt(bedPos)==null,"Removed home bed is not shown as a valid destination");
            // Immutable pre-building native checkpoint supplied by the launcher.
            string legacyFile=Path.Combine(output,"schema-18.rrsave");
            Check(File.Exists(legacyFile),"Historical native schema-18 checkpoint supplied");
            SaveEntry legacy;using(var reader=game.Saves.Open(File.ReadAllBytes(legacyFile),out legacy))Check(reader.Format==18,"Historical checkpoint retains its real schema-18 envelope");
            legacy.Path=legacyFile;Check(game.LoadGame(legacy),"Load actual pre-building full checkpoint: "+game.SaveStatus);Freeze();game.SetMode(ScreenMode.Pause);
            Check(!game.Navigation.LastDeath.HasValue,"Historical full checkpoint begins without a death marker");
            game.InitializeSaves(Path.Combine(output,"Migrated"));Check(game.SaveGame("Migrated schema 18",true),"Write migrated complete checkpoint");
            var migrated=game.Saves.List().First(e=>!e.Backup);byte[] migratedBytes=game.Saves.Read(migrated);
            Check(game.LoadGame(migrated),"Reload migrated checkpoint");Freeze();game.SetMode(ScreenMode.Pause);
            Check(game.CaptureSave(migrated).SequenceEqual(migratedBytes),"Migrated full checkpoint round-trips byte-for-byte");
        }
    }
}
