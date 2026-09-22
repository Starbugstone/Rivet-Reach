using System;
using System.Diagnostics;
using System.Threading;
using Unity.Profiling;

namespace RivetReach
{
    // Release-capable, opt-in wall timings. Worker scopes contain no Unity API calls.
    // Named Profiler markers remain available independently in Development players.
    public static class RuntimeCosts
    {
        public static bool MeasureAllocations, SamplingRequested;
        public static bool AllocationCounterSupported {get;private set;}
        static int epoch;
        public static bool Sampling => (Volatile.Read(ref epoch)&1)!=0;
        public static int Epoch => Volatile.Read(ref epoch);
        public static readonly long[] Allocated=new long[9];
        sealed class Counter { public long Calls,Ticks,Peak,WindowPeak; }
        public readonly struct Sample
        {
            public readonly long Calls,Ticks,PeakTicks,WindowPeakTicks;
            public Sample(long calls,long ticks,long peak,long windowPeak){Calls=calls;Ticks=ticks;PeakTicks=peak;WindowPeakTicks=windowPeak;}
            public double TotalMs=>Milliseconds(Ticks);
            public double PeakMs=>Milliseconds(PeakTicks);
            public double WindowPeakMs=>Milliseconds(WindowPeakTicks);
        }
        public static double Milliseconds(long ticks)=>ticks*1000.0/Stopwatch.Frequency;
        public readonly struct Marker
        {
            readonly ProfilerMarker marker;
            public readonly int Index;
            public readonly string Name,Label;
            public Marker(string name,int index,string label){Name=name;Index=index;Label=label;marker=new ProfilerMarker(name);}
            public Scope Auto()=>new Scope(marker,Index);
        }
        public readonly struct Scope : IDisposable
        {
            readonly ProfilerMarker.AutoScope scope;
            readonly long before,start;
            readonly int index,session;
            readonly bool measure;
            public Scope(ProfilerMarker marker,int index)
            {
                scope=marker.Auto();this.index=index;
                measure=index<Allocated.Length&&MeasureAllocations&&AllocationCounterSupported;
                before=measure?GC.GetAllocatedBytesForCurrentThread():0;
                session=Volatile.Read(ref epoch);start=(session&1)!=0?Stopwatch.GetTimestamp():0;
            }
            public void Dispose()
            {
                if((session&1)!=0)Record(index,session,Stopwatch.GetTimestamp()-start);
                if(measure)Allocated[index]+=GC.GetAllocatedBytesForCurrentThread()-before;
                scope.Dispose();
            }
        }
        // Keep the first nine indices stable for historical release-review recordings.
        public static readonly Marker UI=new Marker("RR.UI",0,"UI"),Streaming=new Marker("RR.Streaming",1,"Streaming"),Industry=new Marker("RR.IndustryTick",2,"Factory tick"),Machines=new Marker("RR.MachineViews",3,"Machine views"),Weather=new Marker("RR.WeatherViews",4,"Weather views"),Torches=new Marker("RR.TorchRefresh",5,"Torch refresh"),Animals=new Marker("RR.PassiveTick",6,"Passive tick"),Mobs=new Marker("RR.HostileTick",7,"Hostile tick"),Survival=new Marker("RR.SurvivalTick",8,"Survival tick");
        public static readonly Marker IndustryPower=new Marker("RR.IndustryPower",9,"  Power"),IndustryItems=new Marker("RR.IndustryItems",10,"  Item pipes"),IndustryFluids=new Marker("RR.IndustryFluids",11,"  Fluid pipes"),IndustryMachines=new Marker("RR.IndustryMachines",12,"  Processing phases"),Topology=new Marker("RR.Topology",13,"  Network rebuild"),LightSnapshot=new Marker("RR.LightSnapshot",14,"Light snapshot"),LightWorker=new Marker("RR.LightWorker",15,"Light worker job"),LightUpload=new Marker("RR.LightUpload",16,"Light upload CPU"),TerrainWorker=new Marker("RR.TerrainWorker",17,"Terrain worker job");
        public static readonly Marker MobViews=new Marker("RR.MobViews",18,"Hostile views"),AnimalViews=new Marker("RR.AnimalViews",19,"Passive views"),WorldFluids=new Marker("RR.WorldFluids",20,"World fluid tick"),SimulationAdvance=new Marker("RR.SimulationAdvance",21,"Simulation advance"),MobSpawning=new Marker("RR.MobSpawning",22,"  Hostile spawn"),AnimalActivation=new Marker("RR.AnimalActivation",23,"  Passive residency"),AnimalSpawning=new Marker("RR.AnimalSpawning",24,"  Passive spawn"),FluidMeshes=new Marker("RR.FluidMeshes",25,"Fluid mesh CPU"),TerrainUpload=new Marker("RR.TerrainUpload",26,"Terrain upload CPU");
        // Detailed capture scopes; the compact overlay retains its existing rows.
        public static readonly Marker TerrainViews=new Marker("RR.TerrainViews",27,"TerrainViews"),TerrainMeshUpload=new Marker("RR.TerrainMeshUpload",28,"TerrainMeshUpload"),FluidMeshUpload=new Marker("RR.FluidMeshUpload",29,"FluidMeshUpload"),ChunkActivation=new Marker("RR.ChunkActivation",30,"ChunkActivation"),MachineStateSetup=new Marker("RR.MachineStateSetup",31,"MachineStateSetup"),MachinePreparation=new Marker("RR.MachinePreparation",32,"MachinePreparation"),MachineAdvance=new Marker("RR.MachineAdvance",33,"MachineAdvance"),PowerPrepare=new Marker("RR.PowerPrepare",34,"PowerPrepare"),PowerLoads=new Marker("RR.PowerLoads",35,"PowerLoads"),PowerCharge=new Marker("RR.PowerCharge",36,"PowerCharge"),PowerDischarge=new Marker("RR.PowerDischarge",37,"PowerDischarge");
        public static readonly Marker RangedPumpPreparation=new Marker("RR.RangedPumpPreparation",38,"Ranged pump preparation");
        public static readonly Marker[] Markers={UI,Streaming,Industry,Machines,Weather,Torches,Animals,Mobs,Survival,IndustryPower,IndustryItems,IndustryFluids,IndustryMachines,Topology,LightSnapshot,LightWorker,LightUpload,TerrainWorker,MobViews,AnimalViews,WorldFluids,SimulationAdvance,MobSpawning,AnimalActivation,AnimalSpawning,FluidMeshes,TerrainUpload,TerrainViews,TerrainMeshUpload,FluidMeshUpload,ChunkActivation,MachineStateSetup,MachinePreparation,MachineAdvance,PowerPrepare,PowerLoads,PowerCharge,PowerDischarge,RangedPumpPreparation};
        static readonly Counter[] counters=CreateCounters();
        static Counter[] CreateCounters(){var result=new Counter[Markers.Length];for(int i=0;i<result.Length;i++)result[i]=new Counter();return result;}
        // Main-thread lifecycle only; the epoch rejects scopes spanning a disable/re-enable.
        public static void SetSampling(bool enabled)
        {
            if(enabled==Sampling)return;
            if(!enabled){Interlocked.Increment(ref epoch);return;}
            foreach(var c in counters)lock(c){c.Calls=c.Ticks=c.Peak=c.WindowPeak=0;}
            Interlocked.Increment(ref epoch);
        }
        static void Record(int index,int session,long elapsed)
        {
            var c=counters[index];lock(c)
            {
                if(session!=Volatile.Read(ref epoch))return;
                c.Calls++;c.Ticks+=elapsed;c.Peak=Math.Max(c.Peak,elapsed);c.WindowPeak=Math.Max(c.WindowPeak,elapsed);
            }
        }
        public static Sample Read(int index,bool resetWindowPeak=false)
        {
            var c=counters[index];lock(c)
            {
                var result=new Sample(c.Calls,c.Ticks,c.Peak,c.WindowPeak);
                if(resetWindowPeak)c.WindowPeak=0;return result;
            }
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static void AllocationWitness(){var witness=new byte[4096];witness[0]=1;GC.KeepAlive(witness);}
        public static void CalibrateAllocations()
        {
            AllocationWitness();GC.GetAllocatedBytesForCurrentThread();
            long before=GC.GetAllocatedBytesForCurrentThread();AllocationWitness();
            AllocationCounterSupported=GC.GetAllocatedBytesForCurrentThread()-before>=4096;
        }
    }
}
