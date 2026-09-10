using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RivetReach
{
    // Explicit disposable review session. Readback stability and GPU timing are separate phases.
    public static class SunShadowProbe
    {
        [Serializable] public sealed class Sample
        {
            public string mode;
            public int frames,rotationChanges,heldPairs,changedHeldPairs,gpuSamples;
            public double meanDelta,heldMeanDelta,gpuRecorderMedianMs,gpuMedianMs,gpuP95Ms,cpuMedianMs,drawCallsMedian,shadowCastersMedian;
        }
        [Serializable] public sealed class Report
        {
            public string timestamp,unity,gpu,result;
            public bool frameTimingEnabled;
            public int width=960,height=540,edgeRegionHeight=270,timingWidth,timingHeight,directionalLights,torchLightLimit=TorchPresentation.LightLimit;
            public double secondsPerShadowTick;
            public List<string> checks=new List<string>();
            public List<Sample> samples=new List<Sample>();
        }
        public static Report LastReport {get;private set;}
        static void Check(Report report,bool value,string message)
        {if(!value)throw new Exception(message);report.checks.Add(message);}
        public static IEnumerator Run(Expedition game,string output)
        {
            game.StartSession(246813);game.World.ViewDistance=4;game.Mobs.NaturalSpawning=false;
            float deadline=Time.realtimeSinceStartup+120;
            while(!game.ReadyToPlay||game.World.PendingCount>0||game.World.RunningJobs>0)
            {yield return null;if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Sun shadow terrain residency");}
            game.SetMode(ScreenMode.Pause);game.Player.enabled=false;
            game.Player.Body.gameObject.SetActive(false);game.Player.Arms.gameObject.SetActive(false);
            var world=game.World;int floor=world.Generator.Height(4,4)+3;
            for(int z=-4;z<=12;z++)for(int x=-4;x<=12;x++)
            {var p=new BlockPos(x,floor,z);byte old=world.Get(p);if(old!=0)world.Remove(p,old);world.Place(p,BlockId.Grass);}
            for(int z=3;z<=7;z++)for(int x=3;x<=7;x++)world.Place(new BlockPos(x,floor+5,z),BlockId.Leaves);
            for(int y=1;y<=4;y++)world.Place(new BlockPos(5,floor+y,5),BlockId.Log);
            while(world.PendingCount>0||world.RunningJobs>0){yield return null;if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Sun shadow canopy mesh");}
            var camera=game.Player.Camera;var sky=game.Sky;
            camera.transform.position=world.Local(new BlockPos(1,floor+3,0))+new Vector3(.5f,-.3f,.5f);
            camera.transform.LookAt(world.Local(new BlockPos(3,floor+1,4)));
            var report=new Report{timestamp=DateTime.UtcNow.ToString("O"),unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,
                frameTimingEnabled=FrameTimingManager.IsFeatureEnabled(),secondsPerShadowTick=sky.Clock.DaySeconds/DayNightCycle.ShadowTicksPerDay,
                directionalLights=UnityEngine.Object.FindObjectsByType<Light>().Count(l=>l.enabled&&l.gameObject.activeInHierarchy&&l.type==LightType.Directional)};
            Check(report,report.directionalLights==1,"One shadowed celestial light");
            // Real clock boundaries, arbitrary restoration and night handoff must remain intact.
            foreach(double day in new[]{.0,6.0/24,12.0/24,18.0/24,1.0,500000000.5})
            {
                sky.Clock.SetTime(day+.2/DayNightCycle.ShadowTicksPerDay);sky.Apply();
                var held=sky.MainLight.transform.rotation;long tick=sky.ShadowTick;double time=sky.Clock.TotalDays;
                sky.Advance(report.secondsPerShadowTick*.2);
                Check(report,sky.Clock.TotalDays>time&&sky.ShadowTick==tick&&held.Equals(sky.MainLight.transform.rotation),"Clock advances while light holds at day "+day);
                sky.Advance(report.secondsPerShadowTick);
                Check(report,sky.ShadowTick>tick&&!held.Equals(sky.MainLight.transform.rotation),"Light advances on tick at day "+day);
                Check(report,Vector3.Dot(-sky.MainLight.transform.forward,sky.SunDirection.y>=0?sky.SunDirection:sky.MoonDirection)>.9999,"Light remains aligned with visible celestial source");
            }
            var data=camera.GetUniversalAdditionalCameraData();bool dither=data.dithering;data.dithering=false;
            var interfaceView=game.transform.Find("Interface").gameObject;bool interfaceActive=interfaceView.activeSelf;
            var target=camera.targetTexture;var texture=new RenderTexture(report.width,report.height,24,RenderTextureFormat.ARGB32){antiAliasing=4};texture.Create();
            var readback=new Texture2D(report.width,report.height,TextureFormat.RGB24,false);camera.targetTexture=texture;
            var pipeline=(UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            float bias=pipeline.shadowDepthBias;var shadows=sky.MainLight.shadows;int sync=QualitySettings.vSyncCount,rate=Application.targetFrameRate;
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            try
            {
                foreach(string mode in new[]{"continuous","ticked","ticked-bias-review"})
                {
                    pipeline.shadowDepthBias=mode.EndsWith("bias-review")?.5f:bias;
                    sky.Clock.SetTime(10.5/24);sky.Apply();
                    // Eliminate gradual ambient/colour changes from the edge-isolation measurement.
                    var colour=sky.MainLight.color;float intensity=sky.MainLight.intensity;
                    var ambientSky=Shader.GetGlobalVector("_RRAmbientSky");var ambientGround=Shader.GetGlobalVector("_RRAmbientGround");var fog=Shader.GetGlobalVector("_RRFogColour");
                    for(int i=0;i<12;i++)yield return null;
                    var sample=new Sample{mode=mode,frames=240};Color32[] previous=null;Quaternion previousRotation=sky.MainLight.transform.rotation;double heldDelta=0,totalDelta=0;
                    for(int i=0;i<sample.frames;i++)
                    {
                        // Absolute time removes accumulated-step roundoff from the A/B schedule.
                        sky.Clock.SetTime(10.5/24+(i+1)/60.0/sky.Clock.DaySeconds);sky.Apply();
                        if(mode=="continuous")sky.MainLight.transform.rotation=Quaternion.LookRotation(-sky.SunDirection,Vector3.up);
                        sky.MainLight.color=colour;sky.MainLight.intensity=intensity;
                        Shader.SetGlobalVector("_RRAmbientSky",ambientSky);Shader.SetGlobalVector("_RRAmbientGround",ambientGround);Shader.SetGlobalVector("_RRFogColour",fog);
                        bool moved=!previousRotation.Equals(sky.MainLight.transform.rotation);if(moved)sample.rotationChanges++;
                        yield return new WaitForEndOfFrame();
                        var active=RenderTexture.active;RenderTexture.active=texture;readback.ReadPixels(new Rect(0,0,report.width,report.height),0,0);readback.Apply();RenderTexture.active=active;
                        var pixels=readback.GetPixels32();
                        if(previous!=null)
                        {
                            double delta=0;
                            // Bottom half contains the close terrain edge, excluding the continuous sky.
                            for(int p=0;p<report.width*report.edgeRegionHeight;p++)delta+=Math.Abs(pixels[p].r-previous[p].r)+Math.Abs(pixels[p].g-previous[p].g)+Math.Abs(pixels[p].b-previous[p].b);
                            totalDelta+=delta;
                            if(!moved){sample.heldPairs++;heldDelta+=delta;if(delta>0)sample.changedHeldPairs++;}
                        }
                        if(i==0||i==1||i==49||i==50||i==239)File.WriteAllBytes(output+"/"+mode+"-"+i+".png",readback.EncodeToPNG());
                        previous=pixels;previousRotation=sky.MainLight.transform.rotation;
                        yield return null;
                    }
                    sample.meanDelta=totalDelta/((sample.frames-1)*report.width*report.edgeRegionHeight*3.0);
                    sample.heldMeanDelta=sample.heldPairs==0?0:heldDelta/(sample.heldPairs*report.width*report.edgeRegionHeight*3.0);report.samples.Add(sample);
                }
                pipeline.shadowDepthBias=bias;
                // Measure normal screen rendering, not just presentation of an offscreen target.
                camera.targetTexture=null;interfaceView.SetActive(false);data.dithering=dither;
                report.timingWidth=Screen.width;report.timingHeight=Screen.height;
                var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
                File.WriteAllLines(output+"/available-counters.txt",handles.Select(h=>ProfilerRecorderHandle.GetDescription(h)).Where(d=>d.Name.Contains("GPU")||d.Name.Contains("Draw")||d.Name.Contains("Shadow")).Select(d=>d.Category.Name+": "+d.Name));
                // No screenshots/readbacks or pixel arrays while measuring render cost.
                using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SRP Batcher Draw Calls Count"))
                using(var gpuCounter=ProfilerRecorder.StartNew(ProfilerCategory.Render,"GPU Frame Time"))
                using(var casters=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Shadow Casters Count"))
                {
                    foreach(string mode in new[]{"cost-continuous","cost-ticked","cost-no-shadow","cost-ticked-repeat"})
                    {
                        sky.MainLight.shadows=mode=="cost-no-shadow"?LightShadows.None:shadows;
                        sky.Clock.SetTime(10.5/24);sky.Apply();
                        for(int i=0;i<60;i++){FrameTimingManager.CaptureFrameTimings();yield return null;}
                        var sample=new Sample{mode=mode,frames=240};var timings=new FrameTiming[1];var gpu=new List<double>();var cpu=new List<double>();var draw=new List<double>();var caster=new List<double>();var gpuRecorded=new List<double>();ulong last=0;
                        for(int i=0;i<sample.frames;i++)
                        {
                            sky.Clock.SetTime(10.5/24+(i+1)/60.0/sky.Clock.DaySeconds);sky.Apply();
                            if(mode=="cost-continuous")sky.MainLight.transform.rotation=Quaternion.LookRotation(-sky.SunDirection,Vector3.up);
                            FrameTimingManager.CaptureFrameTimings();yield return null;
                            if(FrameTimingManager.GetLatestTimings(1,timings)>0&&timings[0].frameStartTimestamp!=last)
                            {last=timings[0].frameStartTimestamp;if(timings[0].gpuFrameTime>0)gpu.Add(timings[0].gpuFrameTime);cpu.Add(timings[0].cpuFrameTime);}
                            if(gpuCounter.Valid&&gpuCounter.LastValue>0)gpuRecorded.Add(gpuCounter.LastValue/1000000.0);
                            if(draws.Valid)draw.Add(draws.LastValue);if(casters.Valid)caster.Add(casters.LastValue);
                        }
                        sample.gpuRecorderMedianMs=Percentile(gpuRecorded,.5);sample.gpuSamples=gpu.Count;sample.gpuMedianMs=Percentile(gpu,.5);sample.gpuP95Ms=Percentile(gpu,.95);sample.cpuMedianMs=Percentile(cpu,.5);sample.drawCallsMedian=Percentile(draw,.5);sample.shadowCastersMedian=Percentile(caster,.5);report.samples.Add(sample);
                    }
                }
                var fixedSample=report.samples.Single(s=>s.mode=="ticked");
                Check(report,fixedSample.rotationChanges==4&&fixedSample.heldPairs>=230,"Four sun updates in four seconds with more than 230 held frame pairs");
                Check(report,fixedSample.changedHeldPairs==0,"Shadow edge remains stable between sun ticks");
                report.result="PASS";LastReport=report;File.WriteAllText(output+"/sun-shadow-report.json",JsonUtility.ToJson(report,true));
            }
            finally
            {pipeline.shadowDepthBias=bias;sky.MainLight.shadows=shadows;sky.Apply();camera.targetTexture=target;data.dithering=dither;interfaceView.SetActive(interfaceActive);QualitySettings.vSyncCount=sync;Application.targetFrameRate=rate;texture.Release();UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(readback);}
        }
        static double Percentile(List<double> values,double fraction){if(values.Count==0)return -1;values.Sort();return values[(int)((values.Count-1)*fraction)];}
    }
}
