using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    // Opt-in player verification; normal sessions never receive these fixtures.
    public sealed class EatingVerification : MonoBehaviour
    {
        [Serializable] sealed class Report {public string result,timestamp;public string[] checks,errors;}
        readonly List<string> checks=new List<string>(),errors=new List<string>();
        Expedition game;string output;Mouse mouse;Keyboard keyboard;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {if(Environment.GetCommandLineArgs().Contains("-rr-eating-review"))new GameObject("Eating verification").AddComponent<EatingVerification>();}
        void Check(bool success,string message){if(!success)throw new Exception(message);checks.Add(message);}
        void Use(bool held)=>InputSystem.QueueStateEvent(mouse,held?new MouseState().WithButton(PlayerPrefs.GetInt("mineButton",0)==1?MouseButton.Left:MouseButton.Right):new MouseState());
        IEnumerator Start()
        {
            int femalePreference=PlayerPrefs.GetInt("female",0),skinPreference=PlayerPrefs.GetInt("skin",0);
            float effectsPreference=PlayerPrefs.GetFloat("visual.effects",1);
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rr-output");
            output=index>=0?args[index+1]:Path.Combine(Application.persistentDataPath,"EatingVerification");Directory.CreateDirectory(output);
            Application.logMessageReceived+=Log;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach(var device in InputSystem.devices)if(device is Keyboard||device is Mouse)InputSystem.DisableDevice(device);
            keyboard=InputSystem.AddDevice<Keyboard>("Eating keyboard");mouse=InputSystem.AddDevice<Mouse>("Eating mouse");
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                bool more;object current=null;
                try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}
                catch(Exception ex){errors.Add(ex.ToString());break;}
                if(!more){stack.Pop();continue;}
                if(current is IEnumerator nested){stack.Push(nested);continue;}
                yield return current;
            }
            if(errors.Count>0){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,"failure.png"));yield return new WaitForSecondsRealtime(.1f);}
            Use(false);
            ArcadePresentation.Active?.SetIntensity(effectsPreference);
            PlayerPrefs.SetInt("female",femalePreference);PlayerPrefs.SetInt("skin",skinPreference);PlayerPrefs.Save();
            File.WriteAllText(Path.Combine(output,"report.json"),JsonUtility.ToJson(new Report{result=errors.Count==0?"PASS":"FAIL",timestamp=DateTime.UtcNow.ToString("O"),checks=checks.ToArray(),errors=errors.ToArray()},true));
            Application.Quit(errors.Count==0?0:1);
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        IEnumerator Frames(int count){for(int i=0;i<count;i++)yield return null;}
        IEnumerator Shot(string name)
        {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));}
        IEnumerator Run()
        {
            yield return null;game=Expedition.Instance;game.World.ViewDistance=4;ArcadePresentation.Active.SetIntensity(1);
            float deadline=Time.realtimeSinceStartup+90;
            while(!game.ReadyToPlay&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(game.ReadyToPlay,"Spawn terrain ready");game.StartSession(game.Seed);game.Mobs.enabled=false;game.Diagnostics=false;
            var player=game.Player;player.Pitch=-25;
            var world=game.World;var baseCell=world.Address(player.transform.position).Offset(0,4,0);
            for(int z=-2;z<=4;z++)for(int x=-2;x<=2;x++)
            {
                var floor=baseCell.Offset(x,-1,z);if(world.Get(floor)!=0)world.Remove(floor,world.Get(floor));world.Place(floor,BlockId.Dirt);
                for(int y=0;y<4;y++){var cell=baseCell.Offset(x,y,z);if(world.Get(cell)!=0)world.Remove(cell,world.Get(cell));}
            }
            player.ResetMotion();player.transform.position=world.Local(baseCell)+new Vector3(.5f,.02f,.5f);player.Yaw=0;
            game.Inventory.Add(BlockId.BakedPotato,32,0,1);game.Inventory.Add(BlockId.Potato,32,1,2);game.Inventory.Add(BlockId.BakedPotato,4,2,3);
            game.Hunger.Exert(40);yield return Frames(35);
            bool originalFemale=player.Female;int originalSkin=player.Skin;float originalFov=player.Camera.fieldOfView;
            foreach(bool female in new[]{false,true})foreach(int skin in new[]{0,1})foreach(float fov in new[]{60f,78f,100f})
            {
                player.Female=female;player.Skin=skin;player.RefreshAppearance();player.Camera.fieldOfView=fov;game.Selected=skin;
                yield return Frames(35);var rest=player.Camera.WorldToViewportPoint(player.Arms.BonePosition("BlockSocket"));
                if(!female&&skin==0&&fov==60)yield return Shot("rest");
                int count=game.Inventory.Total(skin==0?BlockId.BakedPotato:BlockId.Potato),food=game.Hunger.Food;
                Use(true);yield return new WaitForSeconds(.45f);yield return new WaitForEndOfFrame();
                var mouth=player.Camera.WorldToViewportPoint(player.Arms.BonePosition("BlockSocket"));
                string label=$"{(female?"female":"male")} skin {skin} FOV {fov}";
                Check(player.EatingProgress>0&&player.EatingPoseWeight>.95f,label+" raises the eating hand");
                Check(mouth.x>.40f&&mouth.x<.64f&&mouth.y>.25f&&mouth.y<.46f&&mouth.z>.20f,label+" holds food below camera centre "+mouth.ToString("F3"));
                Check(mouth.x<rest.x&&mouth.z<rest.z,label+" brings food inward and toward the mouth from "+rest.ToString("F3"));
                Check(Vector3.Distance(player.HeldBlock.Centre,player.Arms.BonePosition("BlockSocket"))<.0001f,label+" preserves hand/item attachment");
                Check(player.EatingCrumbCount>0&&player.EatingCrumbCount<=EatingCrumbs.Limit,label+" emits bounded food crumbs");
                if(fov==78)yield return Shot(label.Replace(' ','-'));
                int emitted=player.EatingCrumbsEmitted;
                var particles=player.GetComponent<EatingCrumbs>().Particles;
                var beforeCrumbs=new ParticleSystem.Particle[EatingCrumbs.Limit];int crumbCount=particles.GetParticles(beforeCrumbs);
                Use(false);yield return new WaitForSeconds(.08f);
                if(!female&&skin==0&&fov==60)
                {
                    var afterCrumbs=new ParticleSystem.Particle[EatingCrumbs.Limit];int afterCount=particles.GetParticles(afterCrumbs);
                    Check(beforeCrumbs.Take(crumbCount).Any(a=>afterCrumbs.Take(afterCount).Any(b=>a.randomSeed==b.randomSeed&&b.position.y<a.position.y-.001f)),"Released crumbs fall under world gravity");
                }
                yield return new WaitForSeconds(.14f);
                Check(player.EatingCrumbsEmitted==emitted,label+" release stops new crumbs");
                Check(player.EatingProgress==0&&player.EatingPoseWeight==0&&game.Hunger.Food==food&&game.Inventory.Total(skin==0?BlockId.BakedPotato:BlockId.Potato)==count,label+" release recovers without consumption");
            }
            game.Selected=0;yield return Frames(30);int before=game.Inventory.Total(BlockId.BakedPotato),hunger=game.Hunger.Food;
            Use(true);yield return new WaitForSeconds(1.3f);Use(false);yield return Frames(3);
            Check(game.Inventory.Total(BlockId.BakedPotato)==before-1&&game.Hunger.Food==hunger+5,"Completed bite consumes exactly one item and restores five food");
            ArcadePresentation.Active.SetIntensity(0);int mutedCount=player.EatingCrumbsEmitted;Use(true);yield return new WaitForSeconds(.35f);
            Check(player.EatingCrumbCount==0&&player.EatingCrumbsEmitted==mutedCount,"Effect intensity zero suppresses eating crumbs");Use(false);ArcadePresentation.Active.SetIntensity(1);yield return Frames(3);
            Use(true);yield return new WaitForSeconds(.5f);game.Selected=2;yield return Frames(2);
            Check(player.EatingProgress<.15f,"Switching between slots of the same food restarts the bite");
            game.SetMode(ScreenMode.Inventory);yield return Frames(2);
            Check(player.EatingProgress==0&&player.EatingPoseWeight==0&&player.EatingCrumbCount==0,"Inventory cancels bite, pose and crumbs");
            Use(false);game.SetMode(ScreenMode.Play);yield return Frames(3);
            Use(true);yield return new WaitForSeconds(.3f);game.SetMode(ScreenMode.Pause);yield return Frames(2);
            Check(player.EatingProgress==0&&player.EatingPoseWeight==0&&player.EatingCrumbCount==0,"Pause cancels bite, pose and crumbs");
            Use(false);game.SetMode(ScreenMode.Play);game.SetCreative(true);yield return Frames(3);Use(true);yield return new WaitForSeconds(.3f);
            Check(player.EatingProgress==0&&player.EatingPoseWeight==0,"Creative does not eat");Use(false);game.SetCreative(false);
            while(game.Hunger.Food<HungerState.Maximum)game.Hunger.TryEat(game.Inventory,0,5);
            yield return Frames(3);Use(true);yield return new WaitForSeconds(.3f);
            Check(player.EatingProgress==0&&player.EatingPoseWeight==0,"Full hunger does not animate eating");Use(false);
            game.Hunger.Exert(40);game.Selected=1;yield return Frames(25);Use(true);yield return new WaitForSeconds(.4f);
            var soil=baseCell.Offset(0,-1,2);Check(world.Till(soil),"Planting fixture tills soil");
            void Aim(BlockPos cell)
            {
                var direction=(world.Local(cell)+Vector3.one*.5f-player.Camera.transform.position).normalized;
                player.Yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(direction.y)*Mathf.Rad2Deg;
            }
            int potatoes=game.Inventory.Total(BlockId.Potato);Aim(soil);yield return Frames(2);
            Check(world.Get(soil.Offset(0,1,0))==BlockId.PotatoPlant&&game.Inventory.Total(BlockId.Potato)==potatoes-1,"Planting takes precedence and spends only the planted potato");
            Check(player.EatingProgress<.1f,"Planting cancels the previous unfinished bite");Use(false);yield return Frames(3);
            player.Pitch=-25;game.Selected=0;yield return Frames(25);Use(true);yield return new WaitForSeconds(.4f);
            var bench=baseCell.Offset(1,0,2);Check(world.Place(bench,BlockId.Workbench),"Station fixture places workbench");Aim(bench);yield return Frames(2);
            Check(player.EatingProgress==0,"Aiming Use at a station cancels the bite");Use(false);yield return Frames(3);Use(true);yield return Frames(3);
            Check(game.InventoryOpen&&player.EatingPoseWeight==0,"Station Use opens crafting without eating");Use(false);game.SetMode(ScreenMode.Play);player.Pitch=-25;
            game.Selected=3;game.Inventory.Add(BlockId.BakedPotato,1,3,4);yield return Frames(30);Use(true);
            float finish=Time.time+2;while(!game.Inventory.Slots[3].Empty&&Time.time<finish)yield return null;
            yield return new WaitForEndOfFrame();
            Check(game.Inventory.Slots[3].Empty&&!player.HeldBlock.Visible&&player.EatingPoseWeight==0,"Last food disappears on its consumption frame with no ghost item or pose");Use(false);yield return Frames(3);
            if(Environment.GetCommandLineArgs().Contains("-rr-eating-video"))
            {
                player.Female=false;player.Skin=0;player.RefreshAppearance();player.Camera.fieldOfView=78;game.Selected=0;player.Pitch=0;yield return Frames(35);
                Directory.CreateDirectory(Path.Combine(output,"frames"));Time.captureFramerate=60;
                for(int frame=0;frame<110;frame++)
                {
                    if(frame==15)Use(true);if(frame==95)Use(false);
                    yield return null;yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"frames","eat-"+frame.ToString("D3")+".png"));
                }
                Time.captureFramerate=0;
            }
            player.Female=originalFemale;player.Skin=originalSkin;player.Camera.fieldOfView=originalFov;player.RefreshAppearance();
        }
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
