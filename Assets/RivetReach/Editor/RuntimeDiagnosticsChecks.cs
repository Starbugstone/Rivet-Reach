using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RivetReach.Editor
{
    public static class RuntimeDiagnosticsChecks
    {
        // Observe the monotonic clock changing, without asserting a machine-speed
        // threshold or relying on a particular scheduler/sleep resolution.
        static void ClockAdvances()
        {
            long start=Stopwatch.GetTimestamp();
            while(Stopwatch.GetTimestamp()==start)Thread.SpinWait(16);
        }
        public static void Run()
        {
            var lines=new List<string>();bool wasSampling=RuntimeCosts.Sampling,wasMeasuring=RuntimeCosts.MeasureAllocations;
            void Check(bool valid,string message)
            {if(!valid)throw new InvalidOperationException("Runtime diagnostics: "+message);lines.Add("PASS "+message);}
            void Fresh(){RuntimeCosts.SetSampling(false);RuntimeCosts.SetSampling(true);}
            RuntimeCosts.MeasureAllocations=false;
            try
            {
                var names=new HashSet<string>();
                for(int i=0;i<RuntimeCosts.Markers.Length;i++)
                    Check(RuntimeCosts.Markers[i].Index==i&&names.Add(RuntimeCosts.Markers[i].Name),"Marker index and unique name agree with counter slot "+i);
                Check(Math.Abs(RuntimeCosts.Milliseconds(Stopwatch.Frequency)-1000)<1e-9,"Stopwatch ticks convert to milliseconds without a frame-rate assumption");
                Fresh();
                using(RuntimeCosts.Industry.Auto())
                {
                    ClockAdvances();
                    using(RuntimeCosts.IndustryPower.Auto())ClockAdvances();
                    ClockAdvances();
                }
                var parent=RuntimeCosts.Read(RuntimeCosts.Industry.Index);var child=RuntimeCosts.Read(RuntimeCosts.IndustryPower.Index);
                Check(parent.Calls==1&&child.Calls==1&&child.Ticks>0&&parent.Ticks>=child.Ticks,
                    "Nested scopes record actual positive elapsed time and retain inclusive parent duration");
                Check(parent.PeakTicks==parent.Ticks&&parent.WindowPeakTicks==parent.Ticks,
                    "First completed scope initializes lifetime and window peaks");
                int epoch=RuntimeCosts.Epoch;RuntimeCosts.SetSampling(true);
                Check(RuntimeCosts.Epoch==epoch&&RuntimeCosts.Read(RuntimeCosts.Industry.Index).Calls==1,
                    "Repeated enabling preserves the current epoch and collected totals");
                var read=RuntimeCosts.Read(RuntimeCosts.Industry.Index,true);var after=RuntimeCosts.Read(RuntimeCosts.Industry.Index);
                Check(read.Calls==parent.Calls&&read.Ticks==parent.Ticks&&read.WindowPeakTicks==parent.WindowPeakTicks
                    &&after.Calls==parent.Calls&&after.Ticks==parent.Ticks&&after.PeakTicks==parent.PeakTicks&&after.WindowPeakTicks==0,
                    "Reading/resetting the window peak preserves call totals, total time and lifetime peak");
                using(RuntimeCosts.Industry.Auto())ClockAdvances();
                var next=RuntimeCosts.Read(RuntimeCosts.Industry.Index);
                Check(next.Calls==2&&next.Ticks>parent.Ticks&&next.WindowPeakTicks==next.Ticks-parent.Ticks
                    &&next.PeakTicks==Math.Max(parent.PeakTicks,next.WindowPeakTicks),
                    "The next window measures only new completed scope peaks while totals accumulate");
                RuntimeCosts.SetSampling(false);epoch=RuntimeCosts.Epoch;
                using(RuntimeCosts.Industry.Auto())ClockAdvances();RuntimeCosts.SetSampling(false);
                after=RuntimeCosts.Read(RuntimeCosts.Industry.Index);
                Check(RuntimeCosts.Epoch==epoch&&after.Calls==next.Calls&&after.Ticks==next.Ticks,
                    "Disabled scopes and repeated disabling do not add calls or time");
                var disabledScope=RuntimeCosts.Industry.Auto();
                try{RuntimeCosts.SetSampling(true);}finally{disabledScope.Dispose();}
                Check(RuntimeCosts.Read(RuntimeCosts.Industry.Index).Calls==0,
                    "A scope opened while disabled cannot enter a later enabled window");
                var crossing=RuntimeCosts.Industry.Auto();
                try{ClockAdvances();RuntimeCosts.SetSampling(false);RuntimeCosts.SetSampling(true);}finally{crossing.Dispose();}
                Check(RuntimeCosts.Read(RuntimeCosts.Industry.Index).Calls==0,
                    "A scope spanning disable/re-enable is rejected by the new epoch");
                Fresh();
                const int workers=4,perWorker=128;
                var jobs=new Task[workers];
                using(var ready=new CountdownEvent(workers))
                using(var release=new ManualResetEventSlim())
                {
                    for(int worker=0;worker<workers;worker++)jobs[worker]=Task.Factory.StartNew(()=>
                    {
                        ready.Signal();release.Wait();
                        for(int i=0;i<perWorker;i++)using(RuntimeCosts.TerrainWorker.Auto())
                            using(RuntimeCosts.LightWorker.Auto())ClockAdvances();
                    },CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);
                    try{Check(ready.Wait(TimeSpan.FromSeconds(10)),"Concurrent workers reach the shared start barrier");}
                    finally{release.Set();Task.WaitAll(jobs);}
                }
                var terrain=RuntimeCosts.Read(RuntimeCosts.TerrainWorker.Index);var light=RuntimeCosts.Read(RuntimeCosts.LightWorker.Index);
                Check(terrain.Calls==workers*perWorker&&light.Calls==workers*perWorker,
                    "Concurrent worker scopes preserve every call without lost updates");
                Check(light.Ticks>0&&terrain.Ticks>=light.Ticks&&terrain.PeakTicks>0&&terrain.PeakTicks<=terrain.Ticks,
                    "Concurrent nested worker elapsed totals and peaks remain internally consistent");
                Fresh();
                using(var entered=new ManualResetEventSlim())
                using(var release=new ManualResetEventSlim())
                {
                    var job=Task.Factory.StartNew(()=>
                    {
                        using(RuntimeCosts.TerrainWorker.Auto()){entered.Set();release.Wait();ClockAdvances();}
                    },CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);
                    try
                    {
                        Check(entered.Wait(TimeSpan.FromSeconds(10)),"Worker opens its scope before the sampling transition");
                        RuntimeCosts.SetSampling(false);RuntimeCosts.SetSampling(true);
                    }
                    finally{release.Set();job.GetAwaiter().GetResult();}
                }
                Check(RuntimeCosts.Read(RuntimeCosts.TerrainWorker.Index).Calls==0,
                    "An old worker finishing after re-enable cannot contaminate the new epoch");
                using(RuntimeCosts.TerrainWorker.Auto())ClockAdvances();
                Check(RuntimeCosts.Read(RuntimeCosts.TerrainWorker.Index).Calls==1,
                    "The new epoch continues accepting newly started worker scopes");
            }
            catch(Exception error){lines.Add("FAIL "+error);throw;}
            finally
            {
                RuntimeCosts.SetSampling(false);RuntimeCosts.MeasureAllocations=wasMeasuring;
                if(wasSampling)RuntimeCosts.SetSampling(true);
                Directory.CreateDirectory("Logs/ReleaseReview");File.WriteAllLines("Logs/ReleaseReview/runtime-diagnostics-checks.txt",lines);
            }
        }
    }
}
