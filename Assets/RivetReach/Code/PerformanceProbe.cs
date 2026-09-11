using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    // Explicit Editor/command-line verification only. Never added to ordinary sessions.
    public static class PerformanceProbe
    {
        [Serializable] public sealed class Measurement
        {
            public string name;public int samples;public double medianMs,p95Ms,maxMs,medianAllocatedBytes;
        }
        [Serializable] public sealed class Report
        {
            public string timestamp,unity,cpu,gpu,context,result,probeVersion="2";
            public int seed=246813,viewDistance=10,inventoryObjects,guideObjects,residentCount,width,height;
            public List<Measurement> measurements=new List<Measurement>();
            public List<string> checks=new List<string>();
        }
        static void Check(Report report,bool value,string message)
        {if(!value)throw new InvalidOperationException(message);report.checks.Add(message);}
        static Measurement Summarize(string name,List<double> times,List<double> allocations=null)
        {
            times.Sort();allocations?.Sort();return new Measurement{name=name,samples=times.Count,medianMs=times[times.Count/2],p95Ms=times[(int)((times.Count-1)*.95)],maxMs=times[times.Count-1],medianAllocatedBytes=allocations==null?-1:allocations[allocations.Count/2]};
        }
        static IEnumerator Measure(Report report,string name,int count,Action action)
        {
            var times=new List<double>();var allocations=new List<double>();
            for(int i=0;i<count;i++)
            {
                long bytes=GC.GetAllocatedBytesForCurrentThread(),start=Stopwatch.GetTimestamp();action();
                times.Add((Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency);allocations.Add(GC.GetAllocatedBytesForCurrentThread()-bytes);
                yield return null;
            }
            report.measurements.Add(Summarize(name,times,allocations));
        }
        static IEnumerator Frames(Report report,string name,int count)
        {
            var times=new List<double>();for(int i=0;i<count;i++){yield return null;times.Add(Time.unscaledDeltaTime*1000.0);}report.measurements.Add(Summarize(name,times));
        }
        public static IEnumerator Run(Expedition game,string output)
        {
            Directory.CreateDirectory(output);
            var report=new Report{timestamp=DateTime.UtcNow.ToString("O"),unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,context=Application.isEditor?"Unity Editor Play mode":"Windows Development player",result="RUNNING",width=Screen.width,height=Screen.height};
            game.StartSession(report.seed);game.World.ViewDistance=report.viewDistance;game.Mobs.NaturalSpawning=false;
            float deadline=Time.realtimeSinceStartup+180;
            do{yield return null;if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Terrain did not settle");}while(!game.ReadyToPlay||game.World.PendingCount>0);
            report.residentCount=game.World.ResidentCount;
            yield return Frames(report,"settled gameplay frames",180);
            yield return Measure(report,"open personal inventory",10,()=>{game.SetMode(ScreenMode.Play);game.SetMode(ScreenMode.Inventory);});
            report.inventoryObjects=game.UI.VisibleRoot.GetComponentsInChildren<Transform>().Length;
            yield return Measure(report,"unchanged portrait refresh",10,game.UI.RefreshPreview);
            yield return Frames(report,"inventory idle frames",180);
            game.Inventory.Add(BlockId.Log,32);
            int slot=game.Inventory.FindSlot(s=>s.Id==BlockId.Log);
            game.UI.ClickSlot(slot,false,false);game.UI.ClickSlot(Inventory.SlotCount,false,false);
            Check(report,game.Crafting.Preview?.Output.Id==BlockId.Planks,"Personal recipe preview matches logs to planks");
            yield return Measure(report,"craft one via UI",32,()=>game.UI.ClickSlot(Inventory.SlotCount+16,false,false));
            Check(report,game.Crafting.Grid.Slots[0].Count==16&&game.UI.HeldStack.Count==64,"Output-full cursor preserves remaining ingredients");
            game.UI.ClickSlot(slot,false,false);game.UI.ClickSlot(Inventory.SlotCount+16,false,true);
            Check(report,game.Crafting.Grid.Slots[0].Empty&&game.Inventory.Slots.Where(s=>s.Id==BlockId.Planks).Sum(s=>s.Count)==128,"Shift craft preserves exact input/output quantities");
            // The separately authored browser can land independently of this performance pass.
            // Keep the probe runnable on both its API and the previous guide UI.
            var inspect=typeof(GameUI).GetMethod("InspectBrowserItem");
            if(inspect!=null)
            {
                yield return Measure(report,"open recipe detail",6,()=>inspect.Invoke(game.UI,new object[]{BlockId.Planks,false}));
                yield return null;
                Check(report,game.UI.VisibleRoot.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Planks")),"Browser displays the selected recipe");
            }
            else
            {
                var button=game.UI.VisibleRoot.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="RECIPES");
                button.onClick.Invoke();yield return null;
                Check(report,game.UI.VisibleRoot.GetComponentsInChildren<ScrollRect>().Length==1,"Recipe guide is available");
            }
            report.guideObjects=game.UI.VisibleRoot.GetComponentsInChildren<Transform>().Length;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"crafting.png"));yield return Frames(report,"recipe detail frames",180);
            typeof(GameUI).GetMethod("CloseBrowserRecipe")?.Invoke(game.UI,null);
            game.SetMode(ScreenMode.Play);
            var p=new BlockPos(2,game.World.Generator.Height(2,2)+5,2);
            Check(report,game.World.Ready(p)&&game.World.Get(p)==0,"Terrain edit fixture is ready and empty");
            yield return Measure(report,"place and remove block",12,()=>{if(!game.World.Place(p,BlockId.Cobblestone)||!game.World.Remove(p,BlockId.Cobblestone))throw new Exception("Edit rejected");});
            yield return Frames(report,"post-edit gameplay frames",180);
            Check(report,game.World.Get(p)==0&&game.World.Error==null,"Terrain edits and workers finish without errors");
            game.SetMode(ScreenMode.Pause);
            int demandPasses=game.World.DemandPasses;
            yield return Frames(report,"paused settled frames",60);
            Check(report,game.World.DemandPasses==demandPasses,"Stationary settled residency does not rebuild its demand plan");
            int generated=game.World.GenerationJobs,remeshed=game.World.RemeshJobs;
            Check(report,game.World.ChangeFluid(p,0,Fluids.Water.Source),"Deferred fluid edit is accepted");
            deadline=Time.realtimeSinceStartup+10;
            while(game.World.PendingCount>0||game.World.RunningJobs>0){yield return null;if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Deferred mesh did not settle");}
            Check(report,game.World.RemeshJobs>remeshed&&game.World.GenerationJobs==generated,"Dirty resident pages remesh without terrain regeneration");
            Check(report,game.World.ChangeFluid(p,Fluids.Water.Source,0),"Deferred fluid source can be removed");
            // Edit again while a worker owns its immutable snapshot; collision is authoritative now.
            yield return null;
            Check(report,game.World.Place(p,BlockId.Cobblestone),"Immediate edit supersedes pending remesh");
            deadline=Time.realtimeSinceStartup+10;
            while(game.World.PendingCount>0||game.World.RunningJobs>0){yield return null;if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Revision-safe remesh did not settle");}
            Check(report,game.World.Get(p)==BlockId.Cobblestone&&game.World.Solid(p),"Worker completion cannot overwrite a newer collision edit");
            game.World.Remove(p,BlockId.Cobblestone);
            Check(report,game.Player.Body.AnimationReady&&game.Player.Arms.AnimationReady,"Live session animation graphs remain valid");
            report.result="PASS";File.WriteAllText(Path.Combine(output,"performance.json"),JsonUtility.ToJson(report,true));
        }
    }
}
