using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        [Serializable] sealed class FrameDistribution
        {
            public string workload,startedUtc;
            public int frames,over16Ms,over22Ms,over33Ms,over50Ms,resident,triangles,machines,pendingLight,hostiles,chickens,crops;
            public double medianMs,p95Ms,p99Ms,maxMs,meanGcBytes,gpuMedianMs;
            public double cpuMainMedianMs,cpuRenderMedianMs,presentWaitMedianMs,meanDrawCalls,meanBatches,meanSetPass,meanSrpDrawCalls,meanShadowCasters;
            public int gpuSamples;
            public long nativeMemory;
            public long maxMachineViewsCreated,maxCrateViewsCreated;
            public int pendingMachineViews,pendingCrateViews;
            public bool meets60AtP95,meets45Floor;
        }
        [Serializable] sealed class ReleasePerformanceReport
        {
            public string timestamp,build,graphicsApi,cpu,gpu;
            public int width,height,viewDistance,seed,frameLimit;
            public string conditions="User target: above 60 FPS normally; 45 FPS worst gameplay floor. P95 under 16.67 ms is the reported normal-play indicator; every frame over 22.22 ms breaches the floor. Frame limit -1 means uncapped; ordinary play uses 90. VSync off; default quality; diagnostics off except the named diagnostic workload; synthetic fixtures; no natural creatures. GPU/CPU timing samples may arrive late: CSV timing_timestamp identifies the returned sample, not the current gameplay frame. GPU -1 means unavailable; repeated timing samples are excluded from GPU medians. Legacy profiler columns and GC may be unavailable in retail. The wall_ms/calls columns use release-capable cumulative Stopwatch deltas over consecutive coroutine observations; they are inclusive, nested scopes overlap, and completed worker-job latency is not main-thread CPU time. The frame_ms interval precedes the observation, so inspect neighbouring rows when correlating spikes. Sampling is enabled throughout; overlay text is shown only in the diagnostic workload. Allocation scope -1 means the managed counter failed calibration. Frame-time results include rendering.";
            public List<FrameDistribution> workloads=new List<FrameDistribution>();
        }
        IEnumerator ReviewReleaseLegacy()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-rr-fixture-path");
            Check(at>=0&&at+1<args.Length,"Immutable historical fixture directory supplied");
            var fixtures=ReleaseLegacyFixtures.Validate(args[at+1],game.Saves);
            Check(fixtures.Length==17,"Pinned historical manifests, bytes and envelope identities cover schemas 1 through 17");
            foreach(var fixture in fixtures)
            {
                byte[] original=fixture.Data;var entry=fixture.Identity;int schema=fixture.Schema;
                string isolated=Path.Combine(output,"Schema"+schema);Directory.CreateDirectory(isolated);
                File.WriteAllBytes(Path.Combine(isolated,entry.Id+".rrsave"),original);game.InitializeSaves(isolated);
                Check(game.LoadGame(entry),"Full historical schema "+schema+" loads: "+game.SaveStatus);
                FreezeSaveFixture();game.enabled=false;game.Animals.enabled=false;
                Check(game.SaveGame("Migrated schema "+schema,true),"Save migrated complete world");
                var migrated=game.Saves.List().Find(e=>e.Id==game.SaveId);byte[] saved=game.Saves.Read(migrated);
                Check(game.LoadGame(migrated),"Reload schema "+schema+" migration");FreezeSaveFixture();game.enabled=false;game.Animals.enabled=false;
                Check(System.Linq.Enumerable.SequenceEqual(saved,game.CaptureSave(migrated)),"Schema "+schema+" migrated full state round-trips exactly");
                yield return Settle(120);
            }
        }
        IEnumerator ReviewReleasePerformance()
        {
            var arguments=Environment.GetCommandLineArgs();int frameLimit=-1,limitIndex=Array.IndexOf(arguments,"-rr-frame-limit");
            if(limitIndex>=0)Check(limitIndex+1<arguments.Length&&int.TryParse(arguments[limitIndex+1],out frameLimit)&&(frameLimit==-1||frameLimit>=60&&frameLimit<=360),"Valid review frame limit supplied");
            Application.targetFrameRate=frameLimit;QualitySettings.vSyncCount=0;game.Diagnostics=false;
            game.World.ViewDistance=10;game.SetCreative(true);game.Mobs.NaturalSpawning=false;game.Animals.NaturalSpawning=false;
            yield return Settle(180);
            float lightDeadline=Time.realtimeSinceStartup+180;
            while(game.World.PendingLightChunks>0&&Time.realtimeSinceStartup<lightDeadline)yield return null;
            var summary=new ReleasePerformanceReport{timestamp=DateTime.UtcNow.ToString("O"),build=Debug.isDebugBuild?"Development":"Release",
                graphicsApi=SystemInfo.graphicsDeviceType.ToString(),cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,
                width=Screen.width,height=Screen.height,viewDistance=game.World.ViewDistance,seed=game.Seed,frameLimit=frameLimit};
            var names=new[]{"RR.UI","RR.Streaming","RR.IndustryTick","RR.MachineViews","RR.WeatherViews","RR.TorchRefresh","RR.PassiveTick","RR.HostileTick","RR.SurvivalTick"};
            bool previousSampling=RuntimeCosts.SamplingRequested;
            RuntimeCosts.SamplingRequested=true;RuntimeCosts.SetSampling(true);
            RuntimeCosts.CalibrateAllocations();RuntimeCosts.MeasureAllocations=true;
            var scopes=new ProfilerRecorder[names.Length];
            for(int i=0;i<scopes.Length;i++)scopes[i]=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,names[i]);
            using var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
            var renderNames=new[]{"Draw Calls Count","Batches Count","SetPass Calls Count","SRP Batcher Draw Calls Count","Shadow Casters Count"};
            var renderCounters=new ProfilerRecorder[renderNames.Length];
            for(int i=0;i<renderCounters.Length;i++)renderCounters[i]=ProfilerRecorder.StartNew(ProfilerCategory.Render,renderNames[i]);
            using var csv=new StreamWriter(Path.Combine(output,"frames.csv"));
            csv.Write("workload,frame,frame_ms,gc_bytes,gpu_ms,"+string.Join(",",names)+","+string.Join(",",Array.ConvertAll(names,n=>"alloc_"+n))+",machine_views_created,crate_views_created,timing_timestamp,cpu_main_ms,cpu_render_ms,present_wait_ms,draw_calls,batches,set_pass,srp_draw_calls,shadow_casters,sample_time_s,unity_frame,pending_light,pending_terrain,pending_fluid");
            var wallMarkers=RuntimeCosts.Markers;
            foreach(var marker in wallMarkers)csv.Write(","+marker.Name+"_wall_ms,"+marker.Name+"_calls");
            csv.WriteLine();
            var timing=new FrameTiming[1];
            // Reuse the recorder storage across workloads; allocating a new long-soak
            // buffer at sample start must not create a benchmark-induced hitch.
            const int sampleCapacity=120000;
            var times=new double[sampleCapacity];var gpuTimes=new List<double>(sampleCapacity);var gpuFrames=new double[sampleCapacity];
            var allocatedFrames=new long[sampleCapacity];var scopeFrames=new long[sampleCapacity,scopes.Length];var allocationScopes=new long[sampleCapacity,scopes.Length];
            var createdViews=new long[sampleCapacity,2];
            var wallTicks=new long[sampleCapacity,wallMarkers.Length];var wallCalls=new long[sampleCapacity,wallMarkers.Length];
            var previousWall=new RuntimeCosts.Sample[wallMarkers.Length];
            var sampleTimes=new double[sampleCapacity];var frameIds=new int[sampleCapacity];var queues=new int[sampleCapacity,3];
            var timingIds=new ulong[sampleCapacity];var cpuTimings=new double[sampleCapacity,3];var renders=new long[sampleCapacity,5];
            IEnumerator Sample(string workload,int requestedCount)
            {
                for(int warm=0;warm<90;warm++)yield return null;
                int count=requestedCount>0?Math.Min(requestedCount,sampleCapacity):sampleCapacity;float duration=requestedCount>0?float.PositiveInfinity:-requestedCount;
                gpuTimes.Clear();double allocations=0;bool gcAvailable=false;
                var machinePresentation=FindAnyObjectByType<IndustryPresentation>();var cratePresentation=FindAnyObjectByType<CratePresentation>();
                if(workload!="streaming"&&workload!="active-factory-unload-return")
                {
                    float presentationDeadline=Time.realtimeSinceStartup+30;
                    while((machinePresentation.PendingCreateCount>0||cratePresentation.PendingCreateCount>0)&&Time.realtimeSinceStartup<presentationDeadline)yield return null;
                    Check(machinePresentation.PendingCreateCount==0&&cratePresentation.PendingCreateCount==0,"Steady workload begins after bounded view creation drains: "+workload);
                }
                long previousMachineCreates=machinePresentation.CreatedViews,previousCrateCreates=cratePresentation.CreatedViews;
                var result=new FrameDistribution{workload=workload,frames=count,startedUtc=DateTime.UtcNow.ToString("O")};ulong lastTiming=0;
                float until=Time.realtimeSinceStartup+duration;int sampled=0;
                for(int i=0;i<wallMarkers.Length;i++)previousWall[i]=RuntimeCosts.Read(wallMarkers[i].Index);
                for(int frame=0;frame<count&&Time.realtimeSinceStartup<until;frame++)
                {
                    sampled++;Array.Clear(RuntimeCosts.Allocated,0,RuntimeCosts.Allocated.Length);FrameTimingManager.CaptureFrameTimings();yield return null;
                    double ms=Time.unscaledDeltaTime*1000;times[frame]=ms;
                    sampleTimes[frame]=Time.realtimeSinceStartupAsDouble;frameIds[frame]=Time.frameCount;
                    queues[frame,0]=game.World.PendingLightChunks;queues[frame,1]=game.World.PendingCount;queues[frame,2]=game.World.FluidSimulation.Pending;
                    for(int i=0;i<wallMarkers.Length;i++)
                    {
                        var current=RuntimeCosts.Read(wallMarkers[i].Index);var previous=previousWall[i];
                        wallTicks[frame,i]=current.Ticks-previous.Ticks;wallCalls[frame,i]=current.Calls-previous.Calls;previousWall[i]=current;
                    }
                    if(ms>1000.0/60)result.over16Ms++;if(ms>1000.0/45)result.over22Ms++;if(ms>1000.0/30)result.over33Ms++;if(ms>50)result.over50Ms++;
                    long bytes=gc.Valid?gc.LastValue:-1;if(bytes>=0){allocations+=bytes;gcAvailable=true;}
                    bool hasTiming=FrameTimingManager.GetLatestTimings(1,timing)>0;
                    double gpu=hasTiming&&timing[0].gpuFrameTime>0?timing[0].gpuFrameTime:-1;
                    timingIds[frame]=hasTiming?timing[0].frameStartTimestamp:0;
                    if(gpu>0&&timingIds[frame]!=lastTiming){gpuTimes.Add(gpu);lastTiming=timingIds[frame];}
                    cpuTimings[frame,0]=hasTiming?timing[0].cpuMainThreadFrameTime:-1;
                    cpuTimings[frame,1]=hasTiming?timing[0].cpuRenderThreadFrameTime:-1;
                    cpuTimings[frame,2]=hasTiming?timing[0].cpuMainThreadPresentWaitTime:-1;
                    for(int i=0;i<renderCounters.Length;i++)renders[frame,i]=renderCounters[i].Valid?renderCounters[i].LastValue:-1;
                    gpuFrames[frame]=gpu;allocatedFrames[frame]=bytes;
                    long machineCreates=machinePresentation.CreatedViews,crateCreates=cratePresentation.CreatedViews;
                    createdViews[frame,0]=machineCreates-previousMachineCreates;createdViews[frame,1]=crateCreates-previousCrateCreates;
                    result.maxMachineViewsCreated=Math.Max(result.maxMachineViewsCreated,createdViews[frame,0]);result.maxCrateViewsCreated=Math.Max(result.maxCrateViewsCreated,createdViews[frame,1]);
                    previousMachineCreates=machineCreates;previousCrateCreates=crateCreates;
                    for(int i=0;i<scopes.Length;i++){scopeFrames[frame,i]=scopes[i].Valid?scopes[i].LastValue:-1;allocationScopes[frame,i]=RuntimeCosts.AllocationCounterSupported?RuntimeCosts.Allocated[i]:-1;}
                }
                count=sampled;result.frames=count;
                for(int frame=0;frame<count;frame++)
                {
                    csv.Write(workload+","+frame+","+times[frame].ToString("F4",CultureInfo.InvariantCulture)+","+allocatedFrames[frame]+","+gpuFrames[frame].ToString("F4",CultureInfo.InvariantCulture));
                    for(int i=0;i<scopes.Length;i++)csv.Write(","+(scopeFrames[frame,i]<0?-1:scopeFrames[frame,i]/1000000.0).ToString("F4",CultureInfo.InvariantCulture));for(int i=0;i<scopes.Length;i++)csv.Write(","+allocationScopes[frame,i]);csv.Write(","+createdViews[frame,0]+","+createdViews[frame,1]+","+timingIds[frame]);
                    for(int i=0;i<3;i++)csv.Write(","+cpuTimings[frame,i].ToString("F4",CultureInfo.InvariantCulture));for(int i=0;i<5;i++)csv.Write(","+renders[frame,i]);
                    csv.Write(","+sampleTimes[frame].ToString("F6",CultureInfo.InvariantCulture)+","+frameIds[frame]);
                    for(int i=0;i<3;i++)csv.Write(","+queues[frame,i]);
                    for(int i=0;i<wallMarkers.Length;i++)csv.Write(","+RuntimeCosts.Milliseconds(wallTicks[frame,i]).ToString("F4",CultureInfo.InvariantCulture)+","+wallCalls[frame,i]);
                    csv.WriteLine();
                }
                Array.Sort(times,0,count);gpuTimes.Sort();result.medianMs=times[count/2];result.p95Ms=times[(int)((count-1)*.95)];result.p99Ms=times[(int)((count-1)*.99)];result.maxMs=times[count-1];result.meets60AtP95=result.p95Ms<1000.0/60;result.meets45Floor=result.maxMs<=1000.0/45;
                result.meanGcBytes=gcAvailable?allocations/count:-1;result.gpuMedianMs=gpuTimes.Count>0?gpuTimes[gpuTimes.Count/2]:-1;
                result.gpuSamples=gpuTimes.Count;
                double CpuMedian(int column){int used=0;for(int i=0;i<count;i++)if(cpuTimings[i,column]>=0)times[used++]=cpuTimings[i,column];if(used==0)return -1;Array.Sort(times,0,used);return times[used/2];}
                double RenderMean(int column){double total=0;int used=0;for(int i=0;i<count;i++)if(renders[i,column]>=0){total+=renders[i,column];used++;}return used>0?total/used:-1;}
                result.cpuMainMedianMs=CpuMedian(0);result.cpuRenderMedianMs=CpuMedian(1);result.presentWaitMedianMs=CpuMedian(2);
                result.meanDrawCalls=RenderMean(0);result.meanBatches=RenderMean(1);result.meanSetPass=RenderMean(2);result.meanSrpDrawCalls=RenderMean(3);result.meanShadowCasters=RenderMean(4);
                result.resident=game.World.ResidentCount;result.triangles=game.World.MeshTriangles;result.machines=game.Industry.Simulation.Machines.Count;result.pendingLight=game.World.PendingLightChunks;result.hostiles=game.Mobs.Mobs.Count;result.chickens=game.Animals.ActiveCount;result.crops=game.Survival.ScheduledCrops;
                result.pendingMachineViews=machinePresentation.PendingCreateCount;result.pendingCrateViews=cratePresentation.PendingCreateCount;
                result.nativeMemory=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();summary.workloads.Add(result);csv.Flush();
                File.WriteAllText(Path.Combine(output,"performance.json"),JsonUtility.ToJson(summary,true));
                if(!workload.StartsWith("gpu-",StringComparison.Ordinal))yield return Capture(workload);
            }
            try
            {
                game.Sky.Clock.SetTime(.5);game.Weather.SetWeather(WeatherKind.Clear,true);
                yield return Sample("terrain-day",600);
                game.SetMode(ScreenMode.Inventory);yield return Sample("inventory-idle",600);
                game.SetMode(ScreenMode.Play);game.Sky.Clock.SetTime(.9);yield return Sample("terrain-night",600);
                game.Sky.Clock.SetTime(.5);game.Weather.SetWeather(WeatherKind.Storm,true);yield return Sample("terrain-storm",600);
                game.Weather.SetWeather(WeatherKind.Clear,true);
                // Identical fixed array of real machine blocks in both builds. Place well
                // above the surface so setup cannot alter generated resources below it.
                var anchor=game.World.Address(game.Player.transform.position).Offset(-12,12,12);
                var player=game.Player;player.enabled=false;
                player.transform.position=game.World.Local(anchor)+new Vector3(12,5,-10);player.ResetMotion();
                yield return Settle(180);
                for(int x=0;x<24;x++)for(int z=0;z<8;z++)
                {
                    var p=anchor.Offset(x,0,z);if(!game.World.Ready(p))throw new Exception("Factory fixture outside resident terrain");
                    byte id=game.World.Get(p);if(id!=0)game.World.Remove(p,id);
                    Check(game.World.Place(p,z%2==0?IndustryId.PowerCable:IndustryId.Battery),"Place benchmark assembly");
                    if(x%6==0)yield return null;
                }
                player.Camera.transform.LookAt(game.World.Local(anchor)+new Vector3(12,0,4));
                yield return Settle(180);yield return Sample("factory-192",600);
                yield return ReviewStressFactory(Sample);
                player=game.Player;
                // Horizontal travel exercises generation, light caches and invalidation
                // while retaining the same view distance. Creative removes survival variance.
                IEnumerator Travel()
                {
                    for(int i=0;i<1200;i++){player.transform.position+=Vector3.right*(8*Time.unscaledDeltaTime);yield return null;}
                }
                var travel=Travel();
                IEnumerator SampleTravel(){var sample=Sample("streaming",1000);while(sample.MoveNext()){travel.MoveNext();yield return sample.Current;}}
                yield return SampleTravel();yield return Settle(180);
                Check(game.World.Error==null,"Full-distance rendering and streaming finish without worker errors");
                // Capture the entire stress world, including dormant factories and animals.
                game.SetMode(ScreenMode.Pause);game.Player.enabled=false;game.Animals.enabled=false;game.Mobs.enabled=false;
                int savedMachines=game.Industry.Simulation.Machines.Count,savedAnimals=game.Animals.Animals.Count;
                Check(savedMachines>=1500&&savedAnimals>=64,"Checkpoint still contains the large factory and persistent farm");
                game.InitializeSaves(Path.Combine(output,"StressSaves"));
                var checkpointTimer=System.Diagnostics.Stopwatch.StartNew();
                Check(game.SaveGame("Large factory stress",true),"Save the complete stress world: "+game.SaveStatus);
                double saveMilliseconds=checkpointTimer.Elapsed.TotalMilliseconds;
                var savedEntry=game.Saves.List().Find(e=>e.Id==game.SaveId);byte[] savedState=game.Saves.Read(savedEntry);
                checkpointTimer.Restart();
                Check(game.LoadGame(savedEntry),"Reload complete stress factory: "+game.SaveStatus);
                double loadMilliseconds=checkpointTimer.Elapsed.TotalMilliseconds;
                FreezeSaveFixture();game.Animals.enabled=false;
                Check(System.Linq.Enumerable.SequenceEqual(savedState,game.CaptureSave(savedEntry)),"Complete stress state reloads byte-for-byte before resuming");
                Check(game.Industry.Simulation.Machines.Count==savedMachines&&game.Animals.Animals.Count==savedAnimals,"All stress machines and persistent animals survive reload");
                File.WriteAllText(Path.Combine(output,"stress-save.txt"),"Complete stress checkpoint: "+savedState.Length+" bytes, "+savedMachines+" industrial assemblies, "+savedAnimals+" persistent animals; exact state round-trip passed.\nPaused save: "+saveMilliseconds.ToString("F2",CultureInfo.InvariantCulture)+" ms; synchronous load transaction: "+loadMilliseconds.ToString("F2",CultureInfo.InvariantCulture)+" ms (subsequent streaming excluded).\n");
                // This probe owns a fresh session, so it must follow the full-factory checkpoint.
                yield return PerformanceProbe.Run(game,Path.Combine(output,"interactions"));
            }
            finally{RuntimeCosts.SamplingRequested=previousSampling;RuntimeCosts.SetSampling(previousSampling||game.Diagnostics);RuntimeCosts.MeasureAllocations=false;foreach(var scope in scopes)scope.Dispose();foreach(var counter in renderCounters)counter.Dispose();}
        }
    }
}
