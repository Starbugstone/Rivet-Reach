using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RivetReach.Editor
{
    [InitializeOnLoad]
    public static class DayNightChecks
    {
        static int assertions;
        static double nextPoll;
        static DayNightChecks(){EditorApplication.update+=Poll;}
        // Separate channel/output lets this review preserve an already running POC player.
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<nextPoll||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            nextPoll=EditorApplication.timeSinceStartup+2;
            const string request="Logs/day-night-request.txt";
            if(!File.Exists(request))return;
            string command=File.ReadAllText(request).Trim();
            if(command!="build"&&command!="checks")return;
            File.Delete(request);
            try
            {
                if(command=="build")BuildReview();else Run();
                File.WriteAllText("Logs/day-night-result.txt","PASS "+DateTime.UtcNow.ToString("O"));
            }
            catch(Exception ex){Debug.LogException(ex);File.WriteAllText("Logs/day-night-result.txt","FAIL\n"+ex);}
        }
        static void Check(bool value,string message){if(!value)throw new Exception("Clock: "+message);assertions++;}
        static void Reject(Action action)
        {
            try{action();}catch(ArgumentOutOfRangeException){assertions++;return;}
            throw new Exception("Clock accepted invalid state");
        }
        [MenuItem("Rivet Reach/Verify day and night clock")]
        public static void Run()
        {
            assertions=0;var clock=new WorldClock();
            Check(clock.Hour==8&&clock.DayNumber==1&&clock.MoonPhase==4&&!clock.IsNight,"Morning/full-moon new world");
            double initial=clock.TotalDays;clock.Advance(0);Check(clock.TotalDays==initial,"Zero eligible time freezes clock");
            clock.Advance(1200);Check(Math.Abs(clock.Hour-8)<1e-10&&clock.DayNumber==2&&clock.MoonPhase==5,"One full day advances phase");
            var stepped=new WorldClock();var batched=new WorldClock();
            for(int i=0;i<72000;i++)stepped.Advance(1.0/60);
            batched.Advance(1200);Check(Math.Abs(stepped.TotalDays-batched.TotalDays)<1e-10,"60 Hz steps agree with elapsed-time advance");
            var phases=new System.Collections.Generic.HashSet<int>();
            for(int day=0;day<24;day++)
            {
                clock.SetTime(day+18.0/24);int phase=clock.MoonPhase;phases.Add(phase);
                Check(clock.IsNight,"Dusk enters night");
                clock.SetTime(day+23.999/24);Check(clock.MoonPhase==phase&&clock.IsNight,"Phase stable before midnight");
                clock.SetTime(day+1);Check(clock.DayNumber==day+2&&clock.MoonPhase==phase&&clock.IsNight,"Civil day advances without splitting night");
                clock.SetTime(day+1+5.999/24);Check(clock.MoonPhase==phase&&clock.IsNight,"Phase stable until dawn");
                clock.SetTime(day+1+6.0/24);Check(clock.MoonPhase==(phase+1)%8&&!clock.IsNight,"Dawn advances exactly one phase");
            }
            Check(phases.Count==8,"All eight phases recur over three lunar cycles");
            clock.SetTime(4.5);Check(clock.MoonPhase==0&&Math.Abs(clock.MoonIllumination)<1e-10,"New moon has no direct illumination");
            clock.SetTime(.5);Check(clock.MoonPhase==4&&Math.Abs(clock.MoonIllumination-1)<1e-10,"Full moon fully illuminated");
            clock.SetTime(2.5);Check(Math.Abs(clock.MoonIllumination-.5)<1e-10,"Quarter moon half illuminated");
            clock.SetTime(500000000.5);clock.Advance(1);Check(clock.Hour>12&&clock.Hour<12.1,"Long-running world retains sub-hour precision");
            var fast=new WorldClock(60);fast.Advance(60);Check(fast.DayNumber==2,"Configurable duration");
            foreach(double invalid in new[]{-1.0,double.NaN,double.PositiveInfinity,double.NegativeInfinity})
            {Reject(()=>clock.Advance(invalid));Reject(()=>clock.SetTime(invalid));Reject(()=>new WorldClock(invalid));}
            Reject(()=>new WorldClock(0));Reject(()=>clock.SetTime(1e9+1));
            Directory.CreateDirectory("Logs/DayNightVerification");
            File.WriteAllText("Logs/DayNightVerification/domain-report.txt",$"PASS: {assertions} assertions\nUnity {Application.unityVersion}\nClock rollover, three lunar cycles, midnight stability, frame batching, duration, precision and invalid-input boundaries.\n");
            Debug.Log($"Day/night clock: {assertions} assertions passed.");
        }
        [MenuItem("Rivet Reach/Build day and night review")]
        public static void BuildReview()
        {
            Run();ProjectBuild.Prepare();
            const string directory="Builds/DayNight";Directory.CreateDirectory(directory);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/RivetReach/Scenes/Main.unity"},locationPathName=directory+"/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Logs/DayNightVerification/build-summary.txt",$"{report.summary.result}; errors {report.summary.totalErrors}; warnings {report.summary.totalWarnings}; seconds {report.summary.totalTime.TotalSeconds}");
            File.WriteAllLines("Logs/DayNightVerification/build-messages.txt",report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.type+": "+m.content));
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Day/night build failed");
            File.Copy("LICENSE.md",directory+"/LICENSE.md",true);File.Copy(".docs/THIRD_PARTY_NOTICES.md",directory+"/THIRD_PARTY_NOTICES.md",true);
            foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories))
            {string target=Path.Combine(directory,"licenses",Path.GetRelativePath(".docs/licenses",source));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);}
        }
    }
}
