using System;
using Unity.Profiling;

namespace RivetReach.Editor
{
    // Some Unity/Mono players return a constant zero from the managed byte
    // counter. Never treat that as evidence of allocation-free code.
    internal sealed class AllocationCounter : IDisposable
    {
        readonly bool managed;
        ProfilerRecorder recorder;
        bool profiler;
        public bool Supported=>managed||profiler;
        public string Description {get;}
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static void AllocateWitness(){var witness=new byte[4096];witness[0]=1;GC.KeepAlive(witness);}
        static void Empty(){ }
        public AllocationCounter()
        {
            AllocateWitness();GC.GetAllocatedBytesForCurrentThread();
            long before=GC.GetAllocatedBytesForCurrentThread();AllocateWitness();
            long observed=GC.GetAllocatedBytesForCurrentThread()-before;
            if(observed>=4096)
            {managed=true;Description="Managed allocation counter calibrated: retained 4096-byte array measured "+observed+" bytes.";return;}
            recorder=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC.Alloc",1,
                ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.CollectOnlyOnCurrentThread|ProfilerRecorderOptions.WrapAroundWhenCapacityReached);
            if(recorder.Valid)
            {
                // Omit SumAllSamplesInFrame: synchronous Editor checks require
                // individual events, not a value finalized at the next frame.
                Record(Empty);int empty=Record(Empty),positive=Record(AllocateWitness);
                profiler=empty==0&&positive>0;
                if(profiler){Description="GC.Alloc recorder calibrated: empty control emitted no event; retained 4096-byte array emitted an event. Byte counter unsupported ("+observed+").";return;}
                Description="UNVERIFIED allocation counter: managed witness measured "+observed+" bytes; GC.Alloc controls empty="+empty+", allocation="+positive+". Zero allocation is not established.";
            }
            else Description="UNVERIFIED allocation counter: managed witness measured "+observed+" bytes and GC.Alloc recorder is unavailable. Zero allocation is not established.";
        }
        int Record(Action action)
        {recorder.Reset();recorder.Start();try{action();}finally{recorder.Stop();}return recorder.Count;}
        public long Measure(Action action)
        {
            if(managed){long before=GC.GetAllocatedBytesForCurrentThread();action();return GC.GetAllocatedBytesForCurrentThread()-before;}
            if(profiler)return Record(action); // Capacity one means any allocation fails; do not infer an event total.
            action();return -1;
        }
        public void Dispose(){recorder.Dispose();}
    }
}
