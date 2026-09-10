using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RivetReach
{
    // Explicit visual regression: freeze the same terrain, camera and light, then
    // isolate ambient-occlusion noise from real directional shadows.
    public static class ShadowProbe
    {
        [Serializable] public sealed class Sample
        {
            public string mode;
            public int frames;
            public double meanChannelDelta,changedPixelsPercent;
        }
        [Serializable] public sealed class Report
        {
            public string timestamp,unity,gpu,result;
            public int width=960,height=540,activeDirectionalLights,duplicateChunkViews;
            public string antialiasing;
            public string aoMethod;
            public float originalShadowDepthBias;
            public List<Sample> samples=new List<Sample>();
        }
        public static Report LastReport {get;private set;}
        public static IEnumerator Run(Expedition game,string output)
        {
            game.StartSession(246813);game.World.ViewDistance=4;game.Mobs.NaturalSpawning=false;
            float until=Time.realtimeSinceStartup+120;
            while(!game.ReadyToPlay||game.World.PendingCount>0||game.World.RunningJobs>0)
            {yield return null;if(Time.realtimeSinceStartup>until)throw new TimeoutException("Shadow fixture residency");}
            game.SetMode(ScreenMode.Pause);game.Player.enabled=false;
            game.Player.Body.gameObject.SetActive(false);game.Player.Arms.gameObject.SetActive(false);
            game.Sky.Clock.SetTime(10.5/24);game.Sky.Apply();
            var world=game.World;
            int floor=world.Generator.Height(4,4)+2;
            // A terrain corner and block canopy make contact shading and cast shadows visible.
            for(int z=0;z<9;z++)for(int x=0;x<9;x++)
            {
                var p=new BlockPos(x,floor,z);byte old=world.Get(p);if(old!=0)world.Remove(p,old);world.Place(p,BlockId.Sandstone);
            }
            for(int y=1;y<=4;y++)world.Place(new BlockPos(4,floor+y,4),BlockId.Log);
            for(int z=3;z<=5;z++)for(int x=3;x<=5;x++)world.Place(new BlockPos(x,floor+5,z),BlockId.Leaves);
            for(int z=1;z<=4;z++)world.Place(new BlockPos(1,floor+1,z),BlockId.Sandstone);
            yield return null;
            var camera=game.Player.Camera;
            camera.transform.position=world.Local(new BlockPos(10,floor+8,-4));camera.transform.LookAt(world.Local(new BlockPos(4,floor+1,4)));
            var report=new Report{timestamp=DateTime.UtcNow.ToString("O"),unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,
                antialiasing=camera.GetUniversalAdditionalCameraData().antialiasing.ToString(),
                activeDirectionalLights=UnityEngine.Object.FindObjectsByType<Light>().Count(l=>l.enabled&&l.gameObject.activeInHierarchy&&l.type==LightType.Directional),
                duplicateChunkViews=world.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Chunk").GroupBy(r=>r.transform.position).Sum(g=>Math.Max(0,g.Count()-1))};
            var pipeline=(UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            var data=pipeline.rendererDataList[0];
            var feature=data.rendererFeatures.Single(f=>f is ScreenSpaceAmbientOcclusion);
            var settings=feature.GetType().GetField("m_Settings",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(feature);
            var field=settings.GetType().GetField("AOMethod",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            object originalMethod=field.GetValue(settings);bool originalActive=feature.isActive;
            var shadows=game.Sky.MainLight.shadows;var target=camera.targetTexture;
            var cameraData=camera.GetUniversalAdditionalCameraData();bool dither=cameraData.dithering;cameraData.dithering=false;
            report.aoMethod=originalMethod.ToString();
            float bias=pipeline.shadowDepthBias;report.originalShadowDepthBias=bias;
            var texture=new RenderTexture(report.width,report.height,24,RenderTextureFormat.ARGB32){antiAliasing=4};texture.Create();
            var readback=new Texture2D(report.width,report.height,TextureFormat.RGB24,false);
            camera.targetTexture=texture;
            try
            {
                foreach(string mode in new[]{"frozen-shadow","frozen-no-shadow","moving-shadow"})
                {
                    feature.SetActive(true);
                    game.Sky.MainLight.shadows=mode.Contains("no-shadow")?LightShadows.None:shadows;
                    game.Sky.Clock.SetTime(10.5/24);game.Sky.Apply();
                    for(int i=0;i<12;i++)yield return null;
                    var sample=new Sample{mode=mode,frames=mode.StartsWith("moving")?120:30};Color32[] previous=null;double delta=0,changed=0;
                    for(int i=0;i<sample.frames;i++)
                    {
                        if(mode.StartsWith("moving"))game.Sky.Advance(1.0/60);
                        yield return null;
                        var active=RenderTexture.active;RenderTexture.active=texture;
                        readback.ReadPixels(new Rect(0,0,report.width,report.height),0,0);readback.Apply();RenderTexture.active=active;
                        var pixels=readback.GetPixels32();
                        if(previous!=null)for(int p=0;p<pixels.Length;p++)
                        {
                            int r=Math.Abs(pixels[p].r-previous[p].r),g=Math.Abs(pixels[p].g-previous[p].g),b=Math.Abs(pixels[p].b-previous[p].b);
                            delta+=r+g+b;if(Math.Max(r,Math.Max(g,b))>2)changed++;
                        }
                        if(i==0||i==1)File.WriteAllBytes(output+"/"+mode+"-"+i+".png",readback.EncodeToPNG());
                        previous=pixels;
                    }
                    sample.meanChannelDelta=delta/((sample.frames-1)*report.width*report.height*3.0);
                    sample.changedPixelsPercent=changed*100/((sample.frames-1)*report.width*report.height);
                    report.samples.Add(sample);
                }
                report.result=report.activeDirectionalLights==1&&report.duplicateChunkViews==0?
                    (Convert.ToInt32(originalMethod)==0?"BASELINE":report.samples[0].meanChannelDelta<.01&&report.samples[1].meanChannelDelta<.01?"PASS":"FAIL"):"FAIL";
                LastReport=report;
                File.WriteAllText(output+"/shadow-report.json",JsonUtility.ToJson(report,true));
            }
            finally
            {
                field.SetValue(settings,originalMethod);feature.SetActive(originalActive);game.Sky.MainLight.shadows=shadows;
                camera.targetTexture=target;cameraData.dithering=dither;pipeline.shadowDepthBias=bias;texture.Release();UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(readback);
            }
        }
    }
}
