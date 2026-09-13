using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator SettleLighting(float timeout=150)
        {
            float start=Time.realtimeSinceStartup;
            while(game.World.PendingLightChunks>0||game.World.PendingCount>0||game.World.RunningJobs>0)
            {if(Time.realtimeSinceStartup-start>timeout)throw new Exception("Lighting did not settle: "+game.World.PendingLightChunks);yield return null;}
            yield return null;
        }
        IEnumerator ReviewLighting()
        {
            FreezeSaveFixture();game.enabled=false;game.SetCreative(false);yield return Settle();yield return SettleLighting();
            var world=game.World;var player=game.Player;var p=world.Address(player.transform.position).Offset(0,3,4);
            void Put(BlockPos at,byte id)
            {byte old=world.Get(at);if(old==id)return;if(old!=0)Check(world.Remove(at,old),"Clear lighting fixture");if(id!=0)Check(world.Place(at,id),"Place lighting fixture");}
            for(int z=-5;z<=5;z++)for(int y=-1;y<=5;y++)for(int x=-7;x<=7;x++)
                Put(p.Offset(x,y,z),y==-1?BlockId.Dirt:y==5||x==-7||x==7||z==-5||z==5?BlockId.Stone:(byte)0);
            player.transform.position=world.Local(p)+new Vector3(.5f,.02f,-3.4f);player.ResetMotion();
            player.Camera.transform.position=world.Local(p)+new Vector3(.5f,1.8f,-3.4f);player.Camera.transform.LookAt(world.Local(p)+new Vector3(.5f,.5f,1.2f));
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);player.HeldBlock.enabled=false;
            game.SetMode(ScreenMode.Play);game.Sky.Clock.SetTime(.5);game.Sky.Apply();
            var seeds=new[]{BlockId.Potato,FarmId.WheatSeed,FarmId.FlaxSeed,FarmId.CarrotSeed,FarmId.BerrySeed};
            var crops=new[]{p.Offset(-2,0,0),p,p.Offset(2,0,0),p.Offset(-2,0,2),p.Offset(2,0,2)};
            for(int i=0;i<seeds.Length;i++)Check(world.Till(crops[i].Offset(0,-1,0))&&world.Plant(crops[i],seeds[i]),"Plant farm species "+seeds[i]);
            yield return SettleLighting();
            foreach(var at in crops)Check(world.GrowthLight(at)==0&&world.PropagatedSkyLight(at)==0,"Sealed cave has no sky or placed light");
            void GrowTicks(int ticks){game.Survival.AdvanceTicks(ticks);for(int i=0;i<(game.Survival.ScheduledCrops+15)/16+2;i++)game.Survival.AdvanceTicks(0);}
            GrowTicks(1200);for(int i=0;i<crops.Length;i++)Check(world.Get(crops[i])==CropRules.Planting(seeds[i]).first,"Unlit seeds wait at their due deadline");
            yield return Capture("sealed-cave-no-light");
            var left=p.Offset(-3,0,1);var right=p.Offset(3,0,1);Put(left,BlockId.Torch);Put(right,BlockId.Torch);yield return SettleLighting();
            foreach(var at in crops)Check(world.GrowthLight(at)>=9&&world.PropagatedSkyLight(at)==0,"Placed torch lights crop without sunlight");
            GrowTicks(20);for(int i=0;i<crops.Length;i++)Check(world.Get(crops[i])==CropRules.Planting(seeds[i]).first+1,"Torch-lit crop advances at pending retry");
            world.TorchView.enabled=false;foreach(var light in world.TorchView.Lights)light.enabled=false;
            GrowTicks(1200);for(int i=0;i<crops.Length;i++)Check(world.Get(crops[i])==CropRules.Planting(seeds[i]).first+2,"Crop growth independent of camera light pool");
            world.TorchView.enabled=true;world.TorchView.Refresh();yield return Capture("torch-farm-growing");
            GrowTicks(1200);for(int i=0;i<crops.Length;i++)Check(world.Get(crops[i])==CropRules.Planting(seeds[i]).Mature,"Every cultivated species reaches maturity underground");
            yield return Capture("torch-farm-mature");
            var extra=p.Offset(0,0,2);Check(world.Till(extra.Offset(0,-1,0))&&world.Plant(extra,FarmId.WheatSeed),"Plant compost light fixture");
            Check(game.Survival.AccelerateCrop(extra)&&world.Get(extra)==FarmId.WheatPlant+1,"Compost accepts torch-lit underground crop");
            foreach(var d in ChunkLighting.Faces)if(d.y!=-1)Put(crops[0].Offset(d.x,d.y,d.z),BlockId.Stone);
            yield return SettleLighting();Check(world.GrowthLight(crops[0])==0,"A solid enclosure blocks a nearby torch across chunk boundaries");
            foreach(var d in ChunkLighting.Faces)if(d.y!=-1)Put(crops[0].Offset(d.x,d.y,d.z),0);
            yield return SettleLighting();Check(world.GrowthLight(crops[0])>=9,"Removing opaque enclosure restores torch light");
            Put(left,0);Put(right,0);yield return SettleLighting();foreach(var at in crops)Check(world.GrowthLight(at)==0,"Removing torches removes cached source light");
            // Real lamp power transitions invalidate growth light, independently of its view.
            Put(left,IndustryId.Lamp);Put(left.Offset(-1,0,0),IndustryId.PowerCable);Put(left.Offset(-2,0,0),IndustryId.Battery);
            var sim=game.Industry.Simulation;var battery=sim.At(left.Offset(-2,0,0));battery.EnergyCells[0].Charge(1000000);
            for(int i=0;i<10;i++)sim.Step();yield return SettleLighting();
            Check(sim.At(left).Running&&world.GrowthLight(crops[0])>=9,"Powered Workshop Lamp supports nearby underground crops");
            battery.BatteryMode=BatteryMode.Isolated;for(int i=0;i<10;i++)sim.Step();yield return SettleLighting();
            Check(!sim.At(left).Running&&world.GrowthLight(crops[0])==0,"Losing lamp power removes growth light");
            Put(left,0);Put(left.Offset(-1,0,0),0);battery.EnergyCells[0].Discharge(battery.EnergyCells[0].Amount);Put(left.Offset(-2,0,0),0);

            Put(p.Offset(0,5,0),0);yield return SettleLighting();
            Check(world.PropagatedSkyLight(p)==15,"Opening shaft brings full sky access to cave floor");
            Check(world.PropagatedSkyLight(p.Offset(4,0,0))==11,"Sunlight fades away from shaft through open cave cells");
            var cameraPosition=player.Camera.transform.position;var cameraRotation=player.Camera.transform.rotation;
            player.Camera.transform.position=world.Local(p)+new Vector3(-3,2,-3.4f);player.Camera.transform.LookAt(world.Local(p)+new Vector3(.5f,2.8f,.5f));
            yield return Capture("cave-open-skylight");player.Camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
            Put(p.Offset(0,5,0),BlockId.Stone);yield return SettleLighting();Check(world.PropagatedSkyLight(p)==0,"Closing shaft removes indirect skylight");
            // Side entrance exercises lateral sunlight and roof shielding together.
            for(int y=0;y<3;y++)Put(p.Offset(7,y,0),0);yield return SettleLighting();
            Check(world.PropagatedSkyLight(p.Offset(6,1,0))>=12&&world.PropagatedSkyLight(p.Offset(-5,1,0))<9,"Side entrance fades into the cave");
            player.Camera.transform.position=world.Local(p)+new Vector3(-3,1.8f,-3.4f);player.Camera.transform.LookAt(world.Local(p)+new Vector3(3,1.2f,0));
            yield return Capture("cave-side-entrance");player.Camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);
            for(int y=0;y<3;y++)Put(p.Offset(7,y,0),BlockId.Stone);Put(left,BlockId.Torch);Put(right,BlockId.Torch);yield return SettleLighting();
            int solves=world.LightSolveCount;double total=0,max=0;var samples=new List<double>();
            for(int i=0;i<180;i++){yield return null;double ms=world.LastLightMainMs;total+=ms;max=Math.Max(max,ms);samples.Add(ms);}
            Check(world.LightSolveCount==solves,"Stationary unchanged world launches zero light solves over 180 frames");
            game.Sky.Clock.SetTime(.9);game.Sky.Apply();yield return null;Check(world.LightSolveCount==solves,"Day/night changes reuse the light field");
            samples.Sort();File.WriteAllText(Path.Combine(output,"lighting-performance.txt"),$"Idle 180 frames: mean {total/180:F4} ms; p95 {samples[171]:F4} ms; max {max:F4} ms\nResident chunks {world.ResidentCount}; solves {solves}; light GPU bytes {world.LightGpuBytes}; last worker {world.LastLightWorkerMs:F3} ms; maximum light main {world.MaxLightMainMs:F3} ms; total worker {world.TotalLightWorkerMs:F3} ms\n");
            // Same stationary scene, uncapped presentation comparison. This is total
            // frame time, not a claim of isolated GPU cost or whole-game performance.
            int oldVsync=QualitySettings.vSyncCount,oldTarget=Application.targetFrameRate;
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            string comparison="";
            foreach(bool bypass in new[]{false,true,false})
            {
                world.BypassLightForReview=bypass;for(int i=0;i<20;i++)yield return null;
                var frameTimes=new List<float>();for(int i=0;i<120;i++){yield return null;frameTimes.Add(Time.unscaledDeltaTime*1000);}
                frameTimes.Sort();comparison+=$"Sky sampling bypass={bypass}: median {frameTimes[60]:F3} ms; p95 {frameTimes[114]:F3} ms\n";
            }
            world.BypassLightForReview=false;QualitySettings.vSyncCount=oldVsync;Application.targetFrameRate=oldTarget;
            File.AppendAllText(Path.Combine(output,"lighting-performance.txt"),comparison);
            game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"Saves"));Check(game.SaveGame("Cave farm"),"Save cave farm and torch attachments");
            var entry=game.Saves.List().First(e=>!e.Backup);Check(game.LoadGame(entry),"Reload cave farm");FreezeSaveFixture();game.enabled=false;yield return Settle(120);yield return SettleLighting();world=game.World;
            foreach(var at in crops)Check(world.GrowthLight(at)>=9&&world.PropagatedSkyLight(at)==0,"Reload rebuilds torch lighting without saved derived data");
            // Exercise normal view distance as well as the focused fixture radius.
            world.ViewDistance=10;yield return null;yield return SettleLighting(240);
            int defaultSolves=world.LightSolveCount;double defaultMain=0;
            for(int i=0;i<120;i++){yield return null;defaultMain+=world.LastLightMainMs;}
            Check(world.LightSolveCount==defaultSolves,"Normal view distance also sleeps unchanged light fields");
            File.AppendAllText(Path.Combine(output,"lighting-performance.txt"),$"View distance 10: residents {world.ResidentCount}; GPU bytes {world.LightGpuBytes}; idle mean {defaultMain/120:F4} ms; max main including streaming {world.MaxLightMainMs:F3} ms; total worker {world.TotalLightWorkerMs:F3} ms; solves {world.LightSolveCount}\n");
        }
    }
}
