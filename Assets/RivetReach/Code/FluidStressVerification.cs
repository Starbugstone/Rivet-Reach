using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        [Serializable] sealed class FluidStressStage
        {
            public string name,startedUtc,auditedUtc;
            public int frames,over60Budget,over45Floor,pendingFluid,pendingMeshes;
            public int waterSources=-1,waterFlow=-1,lavaSources=-1,lavaFlow=-1,auditedPendingFluid=-1,auditedPendingMeshes=-1;
            public double medianMs,p95Ms,maxMs,gpuMedianMs;
        }
        [Serializable] sealed class FluidStressReport
        {
            public string result="INCOMPLETE",timestamp,unity,cpu,gpu,graphicsApi,conditions;
            public int seed,width,height,viewDistance,frameLimit;
            public List<FluidStressStage> stages=new List<FluidStressStage>();
        }
        // Source count, not occupied voxel count, is the finite-resource witness:
        // Minecraft-style flow creates derived cells without consuming its source.
        sealed class FluidStressBasin
        {
            public const int Width=48,Depth=24,Height=18;
            public readonly BlockPos Origin;
            public readonly FluidDefinition Fluid;
            public readonly BlockPos[] Sources=new BlockPos[16];
            public FluidStressBasin(BlockPos origin,FluidDefinition fluid)
            {
                Origin=origin;Fluid=fluid;
                for(int i=0;i<Sources.Length;i++)Sources[i]=origin.Offset(3+i%8*6,17,i<8?7:17);
            }
        }
        IEnumerator ReviewFluidStress()
        {
            var world=game.World;var player=game.Player;
            int previousLimit=Application.targetFrameRate,previousVsync=QualitySettings.vSyncCount,previousDistance=world.ViewDistance;
            bool previousDiagnostics=game.Diagnostics,previousCreative=game.Creative,previousPlayer=player.enabled;
            bool previousMobs=game.Mobs.NaturalSpawning,previousAnimals=game.Animals.NaturalSpawning;
            bool previousSampling=RuntimeCosts.SamplingRequested;
            var previousMode=game.Mode;var previousPosition=WorldPoint.FromLocal(player.transform.position,world.Origin);
            Quaternion previousCameraRotation=player.Camera.transform.rotation;
            Vector3 previousCameraLocalPosition=player.Camera.transform.localPosition;
            float fixtureDeadline=Time.realtimeSinceStartup+350;
            IEnumerator SettleBounded()
            {
                float remaining=fixtureDeadline-Time.realtimeSinceStartup;
                Check(remaining>0,"Fluid stress stays within its six-minute total budget");
                yield return Settle(Mathf.Min(60,remaining));
            }
            // Setup only: use the same checked edit authority without rebuilding
            // a whole chunk synchronously for each individual stone floor cell.
            var changeMethod=typeof(VoxelWorld).GetMethod("Change",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,
                new[]{typeof(BlockPos),typeof(byte),typeof(byte),typeof(bool),typeof(bool)},null);
            if(changeMethod==null||changeMethod.ReturnType!=typeof(bool))throw new InvalidOperationException("Fluid fixture requires the checked five-argument VoxelWorld.Change authority");
            var change=(Func<BlockPos,byte,byte,bool,bool,bool>)changeMethod.CreateDelegate(typeof(Func<BlockPos,byte,byte,bool,bool,bool>),world);
            var origin=new BlockPos(0,160,0);
            var basins=new[]{new FluidStressBasin(origin,Fluids.Water),new FluidStressBasin(origin.Offset(54,0,0),Fluids.Lava)};
            var summary=new FluidStressReport{timestamp=DateTime.UtcNow.ToString("O"),unity=Application.unityVersion,cpu=SystemInfo.processorType,
                gpu=SystemInfo.graphicsDeviceName,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),seed=game.Seed,width=Screen.width,height=Screen.height,viewDistance=4,frameLimit=90,
                conditions="Two separate 48x24x18 resident basins; 16 spaced sources each, 17m above floor. Normal water/lava timing, reach and source rules; no direct simulation stepping. Baseline 5s, water/lava 25s each, combined source pulses 36s, paused unload/return first 5s each (subsequent settle tails excluded), drain 35s plus bounded convergence wait if needed. Total fixture deadline 350s. Setup, full voxel audits, CSV writes and captures excluded. Marker columns are cumulative retail stopwatch deltas; nested scopes overlap and worker totals are parallel work, not frame critical-path time. FrameTimingManager samples may be delayed/repeated; timing_timestamp identifies samples. Missing counters and unaudited liquid counts are -1. Queue counts describe the sample end; audited counts have their separate UTC time and may follow additional settle/drain time. Liquid occupied cells are derived flow, not a volume-conservation measure."};
            string reportPath=Path.Combine(output,"fluid-stress.json");
            void SaveReport()=>File.WriteAllText(reportPath,JsonUtility.ToJson(summary,true));
            var markers=RuntimeCosts.Markers;
            const int capacity=12000;
            var frameMs=new double[capacity];var gpuMs=new double[capacity];var cpuMs=new double[capacity,3];var timestamps=new ulong[capacity];
            var scopeMs=new double[capacity,markers.Length];var scopeCalls=new long[capacity,markers.Length];
            var previousTicks=new long[markers.Length];var previousCalls=new long[markers.Length];
            var fluidPending=new int[capacity];var meshPending=new int[capacity];var fluidWork=new int[capacity];var allocation=new long[capacity];
            var sorted=new double[capacity];var uniqueGpu=new List<double>(capacity);var timing=new FrameTiming[1];
            using var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
            using var csv=new StreamWriter(Path.Combine(output,"fluid-stress-frames.csv"));
            csv.Write("stage,frame,frame_ms,gpu_ms,timing_timestamp,cpu_main_ms,cpu_render_ms,present_wait_ms,gc_bytes,pending_fluid,pending_meshes,last_fluid_tick_work");
            foreach(var marker in markers)csv.Write(","+marker.Name+"_ms,"+marker.Name+"_calls");csv.WriteLine();csv.Flush();
            void Sources(FluidStressBasin basin,bool on)
            {
                foreach(var p in basin.Sources)
                {
                    byte before=world.Get(p),after=on?basin.Fluid.Source:(byte)0;if(before==after)continue;
                    if(!world.Ready(p)||before!=0&&Fluids.Registry.Get(before)!=basin.Fluid||!world.ChangeFluid(p,before,after))
                        throw new InvalidOperationException("Fluid stress source transaction failed at "+p);
                }
            }
            byte[] Witness()
            {
                var bytes=new byte[basins.Length*FluidStressBasin.Width*FluidStressBasin.Depth*FluidStressBasin.Height];int index=0;
                foreach(var basin in basins)for(int y=0;y<FluidStressBasin.Height;y++)for(int z=0;z<FluidStressBasin.Depth;z++)for(int x=0;x<FluidStressBasin.Width;x++)
                    {
                        var p=basin.Origin.Offset(x,y,z);
                        if(!world.TryRead(p,out byte value)||value!=world.Get(p))throw new InvalidOperationException("Resident fluid reads must match authoritative cells: "+p);
                        bytes[index++]=value;
                    }
                return bytes;
            }
            void Audit(FluidStressStage stage,int waterSources,int lavaSources,bool requireWaterFlow=false,bool requireLavaFlow=false)
            {
                foreach(var basin in basins)
                {
                    int sources=0,flow=0,falling=0;
                    for(int y=0;y<FluidStressBasin.Height;y++)for(int z=0;z<FluidStressBasin.Depth;z++)for(int x=0;x<FluidStressBasin.Width;x++)
                    {
                        var p=basin.Origin.Offset(x,y,z);if(!world.Ready(p))throw new InvalidOperationException("Fluid audit requires resident cells: "+p);byte cell=world.Get(p);var liquid=Fluids.Registry.Get(cell);
                        if(liquid==null)continue;
                        if(liquid!=basin.Fluid)throw new InvalidOperationException("Unexpected liquid in isolated basin at "+p);
                        if(liquid.IsSource(cell))sources++;else flow++;
                        if(liquid.IsFalling(cell))falling++;
                    }
                    int expected=basin.Fluid==Fluids.Water?waterSources:lavaSources;
                    Check(sources==expected,basin.Fluid.DisplayName+" retains exactly "+expected+" authored sources after "+stage.name);
                    if(basin.Fluid==Fluids.Water){stage.waterSources=sources;stage.waterFlow=flow;if(requireWaterFlow)Check(flow>32&&falling>16,"Water cascade contains active falling and derived flow cells");}
                    else{stage.lavaSources=sources;stage.lavaFlow=flow;if(requireLavaFlow)Check(flow>32&&falling>16,"Lava cascade contains active falling and derived flow cells");}
                    // Inspect the immediately outside perimeter and underside, not the world.
                    for(int x=-1;x<=FluidStressBasin.Width;x++)for(int z=-1;z<=FluidStressBasin.Depth;z++)
                        if(x<0||x==FluidStressBasin.Width||z<0||z==FluidStressBasin.Depth)
                            for(int y=0;y<FluidStressBasin.Height;y++)
                                if(Fluids.IsFluid(world.Get(basin.Origin.Offset(x,y,z))))throw new InvalidOperationException("Fluid escaped fixture rim");
                    for(int x=0;x<FluidStressBasin.Width;x++)for(int z=0;z<FluidStressBasin.Depth;z++)
                        if(Fluids.IsFluid(world.Get(basin.Origin.Offset(x,-1,z))))throw new InvalidOperationException("Fluid escaped fixture floor");
                }
                stage.auditedUtc=DateTime.UtcNow.ToString("O");stage.auditedPendingFluid=world.FluidSimulation.Pending;stage.auditedPendingMeshes=world.PendingCount;
                Check(world.Error==null,"Fluid stage has no terrain worker errors: "+stage.name);SaveReport();
            }
            IEnumerator Sample(string name,float seconds,bool pulse=false)
            {
                // Discard the frame containing previous audit/CSV/capture work.
                yield return null;
                var stage=new FluidStressStage{name=name,startedUtc=DateTime.UtcNow.ToString("O")};
                for(int i=0;i<markers.Length;i++){var value=RuntimeCosts.Read(markers[i].Index);previousTicks[i]=value.Ticks;previousCalls[i]=value.Calls;}
                float began=Time.realtimeSinceStartup;int count=0,nextPulse=1;
                while(Time.realtimeSinceStartup-began<seconds&&count<capacity)
                {
                    if(Time.realtimeSinceStartup>=fixtureDeadline)throw new TimeoutException("Fluid stress exceeded its total fixture deadline");
                    if(pulse&&Time.realtimeSinceStartup-began>=nextPulse*12)
                    {bool on=(nextPulse&1)==0;foreach(var basin in basins)Sources(basin,on);nextPulse++;}
                    FrameTimingManager.CaptureFrameTimings();yield return null;
                    int frame=count++;double ms=Time.unscaledDeltaTime*1000;frameMs[frame]=ms;
                    if(ms>1000.0/60)stage.over60Budget++;if(ms>1000.0/45)stage.over45Floor++;
                    bool has=FrameTimingManager.GetLatestTimings(1,timing)>0;timestamps[frame]=has?timing[0].frameStartTimestamp:0;
                    gpuMs[frame]=has&&timing[0].gpuFrameTime>0?timing[0].gpuFrameTime:-1;
                    cpuMs[frame,0]=has&&timing[0].cpuMainThreadFrameTime>0?timing[0].cpuMainThreadFrameTime:-1;cpuMs[frame,1]=has&&timing[0].cpuRenderThreadFrameTime>0?timing[0].cpuRenderThreadFrameTime:-1;cpuMs[frame,2]=has&&timing[0].cpuMainThreadPresentWaitTime>0?timing[0].cpuMainThreadPresentWaitTime:-1;
                    allocation[frame]=gc.Valid?gc.LastValue:-1;fluidPending[frame]=world.FluidSimulation.Pending;meshPending[frame]=world.PendingCount;fluidWork[frame]=world.FluidSimulation.LastWork;
                    for(int i=0;i<markers.Length;i++)
                    {
                        var value=RuntimeCosts.Read(markers[i].Index);scopeMs[frame,i]=(value.Ticks-previousTicks[i])*1000.0/System.Diagnostics.Stopwatch.Frequency;
                        scopeCalls[frame,i]=value.Calls-previousCalls[i];previousTicks[i]=value.Ticks;previousCalls[i]=value.Calls;
                    }
                }
                Check(count>0&&Time.realtimeSinceStartup-began>=seconds,"Complete fluid sample duration: "+name);
                stage.pendingFluid=world.FluidSimulation.Pending;stage.pendingMeshes=world.PendingCount;
                stage.frames=count;Array.Copy(frameMs,sorted,count);Array.Sort(sorted,0,count);stage.medianMs=sorted[count/2];stage.p95Ms=sorted[(int)((count-1)*.95)];stage.maxMs=sorted[count-1];
                uniqueGpu.Clear();ulong last=0;
                for(int i=0;i<count;i++)if(gpuMs[i]>0&&timestamps[i]!=last){last=timestamps[i];uniqueGpu.Add(gpuMs[i]);}
                uniqueGpu.Sort();stage.gpuMedianMs=uniqueGpu.Count>0?uniqueGpu[uniqueGpu.Count/2]:-1;
                string Number(double value)=>value.ToString("F4",CultureInfo.InvariantCulture);
                for(int i=0;i<count;i++)
                {
                    csv.Write(name+","+i+","+Number(frameMs[i])+","+Number(gpuMs[i])+","+timestamps[i]);
                    for(int j=0;j<3;j++)csv.Write(","+Number(cpuMs[i,j]));csv.Write(","+allocation[i]+","+fluidPending[i]+","+meshPending[i]+","+fluidWork[i]);
                    for(int j=0;j<markers.Length;j++)csv.Write(","+Number(scopeMs[i,j])+","+scopeCalls[i,j]);csv.WriteLine();
                }
                csv.Flush();summary.stages.Add(stage);SaveReport();
            }
            void Aim(){player.Camera.transform.position=world.Local(origin)+new Vector3(50,34,-25);player.Camera.transform.LookAt(world.Local(origin)+new Vector3(50,8,12));}
            try
            {
                Application.targetFrameRate=90;QualitySettings.vSyncCount=0;world.ViewDistance=4;game.Diagnostics=false;game.SetCreative(true);
                game.Mobs.NaturalSpawning=false;game.Animals.NaturalSpawning=false;player.enabled=false;
                game.SetMode(ScreenMode.Pause);player.transform.position=world.Local(origin)+new Vector3(50,20,-10);player.ResetMotion();
                yield return null;yield return SettleBounded();Aim();
                var description=new System.Text.StringBuilder(summary.conditions+"\nSeed: "+game.Seed+"\n");
                foreach(var basin in basins)
                {
                    description.AppendLine(basin.Fluid.DisplayName+" floor origin: "+basin.Origin);
                    foreach(var p in basin.Sources)description.AppendLine("source "+p);
                    for(int z=0;z<FluidStressBasin.Depth;z++)
                    {
                        for(int x=0;x<FluidStressBasin.Width;x++)
                        {
                            var floor=basin.Origin.Offset(x,0,z);
                            if(!world.Ready(floor)||world.Get(floor)!=0||!change(floor,0,BlockId.Stone,false,true))throw new InvalidOperationException("Fluid fixture floor requires resident air: "+floor);
                            if(x==0||z==0||x==FluidStressBasin.Width-1||z==FluidStressBasin.Depth-1)
                                if(!change(floor.Offset(0,1,0),0,BlockId.Stone,false,true))throw new InvalidOperationException("Fluid fixture rim placement failed");
                        }
                        if(Time.realtimeSinceStartup>=fixtureDeadline)throw new TimeoutException("Fluid fixture setup exceeded its total deadline");
                        yield return null;
                    }
                    for(int y=1;y<FluidStressBasin.Height;y++)for(int z=0;z<FluidStressBasin.Depth;z++)for(int x=0;x<FluidStressBasin.Width;x++)
                    {
                        if(y==1&&(x==0||z==0||x==FluidStressBasin.Width-1||z==FluidStressBasin.Depth-1))continue;
                        if(!world.Ready(basin.Origin.Offset(x,y,z))||world.Get(basin.Origin.Offset(x,y,z))!=0)throw new InvalidOperationException("Fluid fixture overhead must be resident air");
                    }
                }
                File.WriteAllText(Path.Combine(output,"fluid-stress-fixture.txt"),description.ToString());yield return SettleBounded();
                game.Sky.Clock.SetTime(.5);game.Weather.SetWeather(WeatherKind.Clear,true);game.SetMode(ScreenMode.Play);Aim();
                RuntimeCosts.SamplingRequested=true;RuntimeCosts.SetSampling(true);
                for(int i=0;i<30;i++)yield return null;
                yield return Sample("empty-basins",5);Audit(summary.stages[summary.stages.Count-1],0,0);
                Sources(basins[0],true);yield return Sample("water-cascades",25);Audit(summary.stages[summary.stages.Count-1],16,0,true);
                yield return AlphaSceneCapture("fluid-stress-water");
                Sources(basins[1],true);yield return Sample("water-and-lava-cascades",25);Audit(summary.stages[summary.stages.Count-1],16,16,true,true);
                yield return AlphaSceneCapture("fluid-stress-water-lava");
                yield return Sample("pulsed-liquid-fronts",36,true);Audit(summary.stages[summary.stages.Count-1],16,16,true,true);
                game.Diagnostics=true;yield return new WaitForSecondsRealtime(1.1f);
                string overlay=string.Join("\n",System.Linq.Enumerable.Select(game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.Text>(),label=>label.text));
                Check(overlay.Contains("Thread avg/peak ms")&&overlay.Contains("World fluid tick")&&overlay.Contains("Terrain worker job")&&overlay.Contains("Fluid pipes"),"Native overlay exposes thread, tick, pipe and worker timing rows");
                File.WriteAllText(Path.Combine(output,"timing-overlay.txt"),overlay);
                yield return Capture("fluid-stress-timing-overlay");game.Diagnostics=false;
                game.SetMode(ScreenMode.Pause);var witness=Witness();var oldOrigin=world.Origin;
                player.transform.position=world.Local(origin.Offset(690,20,-10));
                yield return Sample("paused-unload",5);yield return SettleBounded();
                bool allUnloaded=true;
                foreach(var basin in basins)for(int z=0;z<FluidStressBasin.Depth;z++)for(int x=0;x<FluidStressBasin.Width;x++)
                    allUnloaded&=!world.Ready(basin.Origin.Offset(x,0,z))&&!world.Ready(basin.Origin.Offset(x,FluidStressBasin.Height-1,z));
                Check(allUnloaded&&!world.Origin.Equals(oldOrigin),"Every fluid fixture chunk unloads after actual origin shift");
                player.transform.position=world.Local(origin)+new Vector3(50,20,-10);
                yield return Sample("paused-return",5);yield return SettleBounded();Aim();var returned=Witness();
                Check(System.Linq.Enumerable.SequenceEqual(witness,returned),"All fixture sources and derived flow return byte-for-byte while paused");
                Audit(summary.stages[summary.stages.Count-1],16,16,true,true);
                game.SetMode(ScreenMode.Play);foreach(var basin in basins)Sources(basin,false);
                yield return Sample("source-removal-drain",35);Audit(summary.stages[summary.stages.Count-1],0,0);
                var final=summary.stages[summary.stages.Count-1];float drainWaitBegan=Time.realtimeSinceStartup;
                while((final.waterFlow>0||final.lavaFlow>0)&&Time.realtimeSinceStartup<Mathf.Min(fixtureDeadline-2,drainWaitBegan+45))
                {
                    yield return new WaitForSecondsRealtime(2);Audit(final,0,0);
                }
                File.AppendAllText(Path.Combine(output,"fluid-stress-fixture.txt"),"Extra drain convergence wait seconds: "+(Time.realtimeSinceStartup-drainWaitBegan).ToString("F3",CultureInfo.InvariantCulture)+"\n");
                Check(final.waterFlow==0&&final.lavaFlow==0,"Finite authored sources drain all dependent water and lava flow without source renewal");
                foreach(var marker in new[]{RuntimeCosts.WorldFluids,RuntimeCosts.TerrainWorker,RuntimeCosts.FluidMeshes,RuntimeCosts.LightWorker,RuntimeCosts.UI})
                    Check(RuntimeCosts.Read(marker.Index).Calls>0,"Release-capable scope recorded real work: "+marker.Name);
                yield return AlphaSceneCapture("fluid-stress-drained");summary.result="PASS";
            }
            finally
            {
                if(summary.result!="PASS")summary.result="FAIL";
                RuntimeCosts.SamplingRequested=previousSampling;RuntimeCosts.SetSampling(previousSampling||previousDiagnostics);
                Application.targetFrameRate=previousLimit;QualitySettings.vSyncCount=previousVsync;world.ViewDistance=previousDistance;
                game.Diagnostics=previousDiagnostics;game.Mobs.NaturalSpawning=previousMobs;game.Animals.NaturalSpawning=previousAnimals;
                game.SetCreative(previousCreative);game.SetMode(previousMode);player.transform.position=previousPosition.Local(world.Origin);player.ResetMotion();
                player.Camera.transform.localPosition=previousCameraLocalPosition;player.Camera.transform.rotation=previousCameraRotation;player.enabled=previousPlayer;
                SaveReport();
            }
        }
    }
}
