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
            var dim=ScreenCapture.CaptureScreenshotAsTexture();double dimSum=0;int dimSamples=0;
            for(int y=dim.height/2;y<dim.height*3/4;y++)for(int x=dim.width/4;x<dim.width*3/4;x++)
            {dimSum+=dim.GetPixel(x,y).grayscale;dimSamples++;}
            Destroy(dim);double dimMean=dimSum/dimSamples;
            Check(dimMean>.025&&dimMean<.25,"Sealed cave keeps a faint visible terrain floor: "+dimMean.ToString("F4"));
            game.Sky.Clock.SetTime(.9);game.Sky.Apply();yield return Capture("sealed-cave-night");
            var night=ScreenCapture.CaptureScreenshotAsTexture();double nightSum=0;
            for(int y=night.height/2;y<night.height*3/4;y++)for(int x=night.width/4;x<night.width*3/4;x++)nightSum+=night.GetPixel(x,y).grayscale;
            Destroy(night);Check(Math.Abs(nightSum/dimSamples-dimMean)<.01,"Cave visibility floor remains steady at night");
            game.Sky.Clock.SetTime(.5);game.Sky.Apply();
            double ForegroundMean(Texture2D texture)
            {
                double sum=0;int count=0;
                for(int y=texture.height/4;y<texture.height*9/20;y++)for(int x=texture.width/4;x<texture.width*3/4;x++)
                {sum+=texture.GetPixel(x,y).grayscale;count++;}
                Destroy(texture);return sum/count;
            }
            yield return new WaitForEndOfFrame();double unheldMean=ForegroundMean(ScreenCapture.CaptureScreenshotAsTexture());
            int previousSelected=game.Selected,torchSlot=game.Inventory.FindSlot(s=>s.Empty);
            game.Inventory.Add(BlockId.Torch,1,torchSlot,torchSlot+1);game.Selected=torchSlot;player.HeldBlock.enabled=true;
            yield return null;yield return new WaitForEndOfFrame();
            var heldLight=player.GetComponentsInChildren<Light>().Single(l=>l.name=="Held torch light");
            Check(heldLight.intensity==27&&Shader.GetGlobalVector("_RRHeldTorchAmbient").w==1,"Selected torch enables boosted point light and local ambient fill");
            heldLight.intensity=0;yield return Capture("held-torch-ambient-only");
            double fillMean=ForegroundMean(ScreenCapture.CaptureScreenshotAsTexture());
            Check(fillMean>unheldMean+.008,"Held ambient alone reveals nearby shadowed terrain: "+unheldMean.ToString("F4")+" -> "+fillMean.ToString("F4"));
            heldLight.intensity=27;yield return Capture("held-torch-full");
            foreach(var at in crops)Check(world.GrowthLight(at)==0,"Held visual fill does not grow underground crops");
            game.Inventory.Take(torchSlot,1);game.Selected=previousSelected;yield return null;yield return new WaitForEndOfFrame();
            Check(!heldLight.enabled&&Shader.GetGlobalVector("_RRHeldTorchAmbient").w==0,"Emptying selected torch clears direct light and ambient fill");
            player.HeldBlock.enabled=false;
            var left=p.Offset(-3,0,1);var right=p.Offset(3,0,1);Put(left,BlockId.Torch);Put(right,BlockId.Torch);
            Check(!world.TryGetSpawnLight(crops[0],true,out _),"Pending source updates cannot be mistaken for spawn darkness");
            yield return SettleLighting();
            foreach(var at in crops)Check(world.GrowthLight(at)>=9&&world.PropagatedSkyLight(at)==0,"Placed torch lights crop without sunlight");
            GrowTicks(20);for(int i=0;i<crops.Length;i++)Check(world.Get(crops[i])==CropRules.Planting(seeds[i]).first+1,"Torch-lit crop advances at pending retry");
            world.TorchView.enabled=false;foreach(var light in world.TorchView.Lights)light.enabled=false;
            GrowTicks(1200);for(int i=0;i<crops.Length;i++)Check(world.Get(crops[i])==CropRules.Planting(seeds[i]).first+2,"Crop growth independent of camera light pool");
            world.TorchView.enabled=true;world.TorchView.Refresh();yield return Capture("torch-farm-growing");
            GrowTicks(1200);for(int i=0;i<crops.Length;i++)Check(world.Get(crops[i])==CropRules.Planting(seeds[i]).Mature,"Every cultivated species reaches maturity underground");
            yield return Capture("torch-farm-mature");
            // Hold the camera fixed to compare the actual pooled light output at approach distances.
            var originalObserver=world.Observer;bool worldEnabled=world.enabled;
            var lightObserver=new GameObject("Torch distance review observer");
            var flame=world.TorchView.FlamePosition(left,left.Offset(0,-1,0));
            try
            {
                world.enabled=false;world.Observer=lightObserver.transform;
                lightObserver.transform.position=flame+Vector3.back*65;world.TorchView.Refresh();
                Check(world.TorchView.ActiveLightCount==0,"Placed lights sleep beyond 64 blocks");
                lightObserver.transform.position=flame+Vector3.back*63.5f;world.TorchView.Refresh();
                Light near=world.TorchView.Lights.First(l=>l.enabled&&Vector3.Distance(l.transform.position,flame)<.01f);
                Check(near.intensity>0&&near.intensity<.1f,"Distant torch enters with negligible intensity instead of popping on");
                lightObserver.transform.position=flame+Vector3.back*56;world.TorchView.Refresh();
                Check(Math.Abs(near.intensity-13.5f)<.01f,"Approaching torch reaches half intensity at 56 blocks");
                yield return Capture("torch-distance-56");
                lightObserver.transform.position=flame+Vector3.back*47;world.TorchView.Refresh();
                Check(near.intensity==27&&world.TorchView.ActiveLightCount<=8,"Torch reaches boosted full intensity before 48 blocks within fixed pool");
                yield return Capture("torch-distance-47");
            }
            finally{world.Observer=originalObserver;world.enabled=worldEnabled;Destroy(lightObserver);world.TorchView.Refresh();}
            var extra=p.Offset(0,0,2);Check(world.Till(extra.Offset(0,-1,0))&&world.Plant(extra,FarmId.WheatSeed),"Plant compost light fixture");
            Check(game.Survival.AccelerateCrop(extra)&&world.Get(extra)==FarmId.WheatPlant+1,"Compost accepts torch-lit underground crop");
            foreach(var d in ChunkLighting.Faces)if(d.y!=-1)Put(crops[0].Offset(d.x,d.y,d.z),BlockId.Stone);
            yield return SettleLighting();Check(world.GrowthLight(crops[0])==0,"A solid enclosure blocks a nearby torch across chunk boundaries");
            foreach(var d in ChunkLighting.Faces)if(d.y!=-1)Put(crops[0].Offset(d.x,d.y,d.z),0);
            yield return SettleLighting();Check(world.GrowthLight(crops[0])>=9,"Removing opaque enclosure restores torch light");
            Put(left,0);Put(right,0);yield return SettleLighting();foreach(var at in crops)Check(world.GrowthLight(at)==0,"Removing torches removes cached source light");
            // A neighbouring page can still look clean before the source publishes its border.
            var sourceEdge=new BlockPos(p.Chunk.Min.X+63,p.Y,p.Z);var acrossEdge=sourceEdge.Offset(1,0,0);var edgeFloor=sourceEdge.Offset(0,-1,0);
            byte oldEdge=world.Get(sourceEdge),oldAcross=world.Get(acrossEdge),oldFloor=world.Get(edgeFloor);
            Put(edgeFloor,BlockId.Stone);Put(sourceEdge,0);Put(acrossEdge,0);yield return SettleLighting();
            Put(sourceEdge,BlockId.Torch);
            Check(world.LightingReady(acrossEdge)&&!world.TryGetSpawnLight(acrossEdge,true,out _),"Regional freshness rejects newly added light across a still-clean chunk border");
            yield return SettleLighting();
            Check(world.TryGetSpawnLight(acrossEdge,true,out byte edgeLight)&&edgeLight==13,"Settled cross-border torch provides block level 13 at night");
            Put(sourceEdge,0);
            Check(world.LightingReady(acrossEdge)&&!world.TryGetSpawnLight(acrossEdge,true,out _),"Regional freshness rejects stale removed light across a chunk border");
            yield return SettleLighting();
            Check(world.TryGetSpawnLight(acrossEdge,true,out edgeLight)&&edgeLight==0,"Settled removal restores authoritative nighttime darkness");
            Put(sourceEdge,oldEdge);Put(acrossEdge,oldAcross);Put(edgeFloor,oldFloor);yield return SettleLighting();
            var remote=p.Offset(-96,0,0);Check(world.Ready(remote),"Remote readiness fixture is resident");world.LightSourceChanged(remote);
            Check(world.PendingLightChunks>0&&world.TryGetSpawnLight(crops[0],true,out byte localLight)&&localLight==0,"Distant pending light work does not block local spawning queries");
            yield return SettleLighting();
            // Real lamp power transitions invalidate growth light, independently of its view.
            Put(left,IndustryId.Lamp);Put(left.Offset(-1,0,0),IndustryId.PowerCable);Put(left.Offset(-2,0,0),IndustryId.Battery);
            var sim=game.Industry.Simulation;var battery=sim.At(left.Offset(-2,0,0));battery.EnergyCells[0].Charge(1000000);
            for(int i=0;i<10;i++)sim.Step();yield return SettleLighting();
            Check(sim.At(left).Running&&world.GrowthLight(crops[0])>=9,"Powered Workshop Lamp supports nearby underground crops");
            yield return Capture("workshop-lamp-wide");
            var lampView=world.GetComponent<IndustryPresentation>().ViewAt(left);var lampLight=lampView.GetComponentInChildren<Light>();
            Check(lampLight.enabled&&lampLight.range==20&&Math.Abs(lampLight.intensity-30)<.01f,"Powered Workshop Lamp casts boosted light over a twenty-block range");
            battery.BatteryMode=BatteryMode.Isolated;for(int i=0;i<10;i++)sim.Step();yield return SettleLighting();
            Check(!sim.At(left).Running&&world.GrowthLight(crops[0])==0,"Losing lamp power removes growth light");
            yield return Capture("workshop-lamp-unpowered");
            Check(!lampLight.enabled,"Unpowered lamp removes its broad visual light");
            Put(left,0);Put(left.Offset(-1,0,0),0);battery.EnergyCells[0].Discharge(battery.EnergyCells[0].Amount);Put(left.Offset(-2,0,0),0);

            Put(p.Offset(0,5,0),0);yield return SettleLighting();
            Check(world.PropagatedSkyLight(p)==15,"Opening shaft brings full sky access to cave floor");
            Check(world.TryGetSpawnLight(p,false,out byte daySpawn)&&daySpawn==15&&world.TryGetSpawnLight(p,true,out byte nightSpawn)&&nightSpawn==0,"Spawn query applies shared day/night state to sky access only");
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
            game.SetMode(ScreenMode.Play);game.Sky.Clock.SetTime(5);game.Sky.Apply();
            var surface=new BlockPos(p.X+20,world.Generator.Height(p.X+20,p.Z+20),p.Z+20);
            game.Player.Camera.transform.position=world.Local(surface)+new Vector3(.5f,18,.5f);
            game.Player.Camera.transform.LookAt(world.Local(surface)+Vector3.one*.5f,Vector3.forward);
            yield return Capture("surface-new-moon-dim");
            double surfaceMean=ForegroundMean(ScreenCapture.CaptureScreenshotAsTexture());
            Check(game.Sky.MainLight.intensity<.00001f&&surfaceMean>.025,"Outdoor terrain remains dimly visible without moonlight: "+surfaceMean.ToString("F4"));
        }
    }
}
