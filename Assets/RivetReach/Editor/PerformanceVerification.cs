using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RivetReach.Editor
{
    [InitializeOnLoad]
    public static class PerformanceVerification
    {
        const string Key="RivetReach.PerformanceVerification";
        const string Request="Logs/performance-request.txt";
        static Stack<IEnumerator> routines;
        static int frame;
        static double deadline,nextRequest;
        static string Output=>SessionState.GetString(Key,"");
        static PerformanceVerification(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;}
        static void Log(string message,string stack,LogType type)
        {
            if(Output.Length>0&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))
                File.AppendAllText(Output+"/errors.txt",message+"\n"+stack+"\n");
        }
        static void State(PlayModeStateChange state)
        {
            if(Output.Length==0)return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {routines=new Stack<IEnumerator>();routines.Push(Run());frame=-1;deadline=EditorApplication.timeSinceStartup+360;}
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                if(!File.Exists(Output+"/result.txt"))File.WriteAllText(Output+"/result.txt","FAIL: interrupted");
                if(PlaySessionReloadGuard.Locked)File.WriteAllText(Output+"/result.txt","FAIL: reload lock was not released");
                else if(File.Exists(Output+"/reload-check.txt"))File.AppendAllText(Output+"/reload-check.txt","PASS: Play exit released the reload lock.\n");
                SessionState.SetString(Key,"");routines=null;
            }
        }
        static IEnumerator Run()
        {
            yield return null;
            if(!PlaySessionReloadGuard.Locked)throw new InvalidOperationException("Live Play must defer assembly reloads");
            if(!Output.EndsWith("/Reload"))yield return PerformanceProbe.Run(Expedition.Instance,Output);
            // Request compilation during a live session. The loaded authorities must survive;
            // pending code is allowed to load only after the explicit ExitPlaymode below.
            var game=Expedition.Instance;var inventory=game.Inventory;var world=game.World;
            CompilationPipeline.RequestScriptCompilation();AssetDatabase.Refresh();
            // In this Editor the lock can defer compilation as well as reload. Waiting
            // for compiler completion here would deadlock the test against its own lock.
            for(int i=0;i<60;i++)yield return null;
            if(!ReferenceEquals(game,Expedition.Instance)||!ReferenceEquals(inventory,game.Inventory)||!ReferenceEquals(world,game.World)||!game.Player.Body.AnimationReady)
                throw new InvalidOperationException("Compilation corrupted the live session");
            File.WriteAllText(Output+"/reload-check.txt","PASS: compile request during locked Play preserved session authorities and animation graph for 60 frames.\n");
        }
        [MenuItem("Rivet Reach/Verify performance")]
        public static void Begin()=>Begin("Logs/Performance/Manual");
        static void Begin(string output)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before profiling; current gameplay is preserved.");
            var scene=SceneManager.GetActiveScene();
            if(SceneManager.sceneCount!=1||scene.path!="Assets/RivetReach/Scenes/Main.unity"||scene.isDirty)throw new InvalidOperationException("Open the saved Main scene before profiling.");
            Directory.CreateDirectory(output);foreach(var name in new[]{"errors.txt","result.txt","performance.json"})if(File.Exists(output+"/"+name))File.Delete(output+"/"+name);
            SessionState.SetString(Key,output);EditorApplication.EnterPlaymode();
        }
        public static void Build()
        {
            const string output="Builds/Performance";Directory.CreateDirectory(output);Directory.CreateDirectory("Logs/Performance");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                locationPathName=output+"/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development
            });
            File.WriteAllText("Logs/Performance/build-summary.txt",$"{report.summary.result}; errors {report.summary.totalErrors}; warnings {report.summary.totalWarnings}; seconds {report.summary.totalTime.TotalSeconds}\n");
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Performance build failed");
            File.Copy("LICENSE.md",output+"/LICENSE.md",true);File.Copy(".docs/THIRD_PARTY_NOTICES.md",output+"/THIRD_PARTY_NOTICES.md",true);
            foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories))
            {string target=Path.Combine(output,"licenses",Path.GetRelativePath(".docs/licenses",source));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);}
            File.WriteAllText("Logs/Performance/build-result.txt","PASS");
        }
        static void Poll()
        {
            if(Output.Length==0)
            {
                if(EditorApplication.timeSinceStartup<nextRequest||!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||BuildPipeline.isBuildingPlayer)return;
                string output=File.ReadAllText(Request).Trim();
                if(!output.EndsWith("|ready"))
                {File.WriteAllText(Request,output+"|ready");nextRequest=EditorApplication.timeSinceStartup+2;AssetDatabase.Refresh();return;}
                output=output.Substring(0,output.Length-6);File.Delete(Request);
                try
                {
                    if(output=="checks")
                    {
                        PerformanceChecks.Run();DomainChecks.Run();IndustryChecks.Run();MultiblockChecks.Run();FluidChecks.Run();
                        File.WriteAllText("Logs/Performance/checks-result.txt","PASS");
                    }
                    else if(output=="build")Build();
                    else Begin(output);
                }
                catch(Exception e){string directory=(output=="checks"||output=="build")?"Logs/Performance":output;Directory.CreateDirectory(directory);File.WriteAllText(directory+(output=="build"?"/build-result.txt":output=="checks"?"/checks-result.txt":"/result.txt"),"FAIL: "+e);}
                return;
            }
            if(routines==null||!EditorApplication.isPlaying||frame==Time.frameCount)return;
            frame=Time.frameCount;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Performance verification exceeded six minutes");
                while(routines.Count>0)
                {
                    if(!routines.Peek().MoveNext()){routines.Pop();continue;}
                    if(routines.Peek().Current is IEnumerator nested){routines.Push(nested);continue;}
                    return;
                }
                File.WriteAllText(Output+"/result.txt",File.Exists(Output+"/errors.txt")?"FAIL: runtime errors":"PASS");
            }
            catch(Exception e){File.WriteAllText(Output+"/result.txt","FAIL: "+e);}
            routines=null;EditorApplication.ExitPlaymode();
        }
    }
}
