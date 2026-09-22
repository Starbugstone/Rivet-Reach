using System;
using System.Text;
using UnityEngine;

namespace RivetReach
{
    // One retained sampler per UI; no arrays, recorders or strings created per frame.
    public sealed class RuntimeDiagnostics
    {
        readonly RuntimeCosts.Sample[] previous=new RuntimeCosts.Sample[RuntimeCosts.Markers.Length];
        readonly RuntimeCosts.Sample[] window=new RuntimeCosts.Sample[RuntimeCosts.Markers.Length];
        readonly FrameTiming[] timing=new FrameTiming[1];
        readonly double[] sums=new double[4],peaks=new double[4];
        readonly int[] counts=new int[4];
        readonly StringBuilder text=new StringBuilder(1600);
        static readonly int[] simulation={21,8,20,2,13,12,9,10,11,7,22,6,24,23};
        static readonly int[] presentation={1,26,14,16,3,18,19,4,5,0,17,15,25};
        double lastRefresh,windowSeconds;
        ulong lastTimestamp;
        int session=-1,frames;
        bool wasEnabled;
        double frameTotal,framePeak;
        public string Threads {get;private set;}="Thread timing: waiting for samples";
        public string Simulation {get;private set;}="";
        public string Presentation {get;private set;}="";
        public string Frames {get;private set;}="";
        public void Tick(bool enabled)
        {
            RuntimeCosts.SetSampling(enabled||RuntimeCosts.SamplingRequested);
            if(!enabled){wasEnabled=false;return;}
            if(!wasEnabled||session!=RuntimeCosts.Epoch)
            {
                session=RuntimeCosts.Epoch;lastRefresh=Time.realtimeSinceStartupAsDouble;lastTimestamp=0;
                for(int i=0;i<previous.Length;i++)previous[i]=RuntimeCosts.Read(i,true);
                ResetFrameWindow();
            }
            wasEnabled=true;
            double ms=Time.unscaledDeltaTime*1000;frameTotal+=ms;framePeak=Math.Max(framePeak,ms);frames++;
            FrameTimingManager.CaptureFrameTimings();
            if(FrameTimingManager.GetLatestTimings(1,timing)==0)return;
            var t=timing[0];if(t.frameStartTimestamp==0||t.frameStartTimestamp==lastTimestamp)return;
            lastTimestamp=t.frameStartTimestamp;
            Add(0,t.cpuMainThreadFrameTime);Add(1,t.cpuRenderThreadFrameTime);Add(2,t.gpuFrameTime);Add(3,t.cpuMainThreadPresentWaitTime);
        }
        void Add(int i,double value)
        {
            // Unity uses zero for unsupported timings. Never report that as free work.
            if(value<=0||double.IsNaN(value)||double.IsInfinity(value))return;
            sums[i]+=value;peaks[i]=Math.Max(peaks[i],value);counts[i]++;
        }
        string TimePair(int i)=>counts[i]==0?"n/a":$"{sums[i]/counts[i]:0.00}/{peaks[i]:0.00}";
        public void Refresh()
        {
            double now=Time.realtimeSinceStartupAsDouble;windowSeconds=Math.Max(.001,now-lastRefresh);lastRefresh=now;
            for(int i=0;i<previous.Length;i++)
            {
                var sample=RuntimeCosts.Read(i,true);var old=previous[i];
                window[i]=new RuntimeCosts.Sample(sample.Calls-old.Calls,sample.Ticks-old.Ticks,sample.PeakTicks,sample.WindowPeakTicks);previous[i]=sample;
            }
            Frames=frames==0?"Frame: waiting":$"{1000/Math.Max(.001,frameTotal/frames):0} FPS · frame avg {frameTotal/frames:0.00} / worst {framePeak:0.00} ms · target 16.67 / floor 22.22 ms";
            Threads=$"Thread avg/peak ms: Main {TimePair(0)} · Render {TimePair(1)} · GPU {TimePair(2)} · Present wait {TimePair(3)}";
            Simulation=Format("MAIN THREAD · avg/call | peak ms | calls/s",simulation);
            Presentation=Format("PRESENTATION / JOBS · avg/call | peak ms | calls/s",presentation);
            ResetFrameWindow();
        }
        string Format(string heading,int[] indices)
        {
            text.Clear();text.AppendLine(heading);
            foreach(int i in indices)
            {
                var v=window[i];text.Append(RuntimeCosts.Markers[i].Label).Append(": ");
                if(v.Calls==0)text.AppendLine("idle (0 calls)");
                else text.Append((v.TotalMs/v.Calls).ToString("0.00")).Append(" | ").Append(v.WindowPeakMs.ToString("0.00")).Append(" | ").Append((v.Calls/windowSeconds).ToString("0.0")).AppendLine();
            }
            return text.ToString();
        }
        void ResetFrameWindow()
        {
            Array.Clear(sums,0,sums.Length);Array.Clear(peaks,0,peaks.Length);Array.Clear(counts,0,counts.Length);
            frames=0;frameTotal=framePeak=0;
        }
    }
}
