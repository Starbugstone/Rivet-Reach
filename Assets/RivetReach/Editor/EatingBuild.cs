using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RivetReach.Editor
{
    [InitializeOnLoad]
    public static class EatingBuild
    {
        const string Request="Logs/eating-build-request.txt";
        static double next;
        static EatingBuild(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||BuildPipeline.isBuildingPlayer)return;
            next=EditorApplication.timeSinceStartup+2;
            if(!File.Exists(Request)||File.Exists("Logs/build-request.txt")||File.Exists("Logs/avatar-build-request.txt"))return;
            if(File.ReadAllText(Request).Trim()!="ready")
            {File.WriteAllText(Request,"ready");AssetDatabase.Refresh();next=EditorApplication.timeSinceStartup+10;return;}
            File.Delete(Request);
            try{Build();}
            catch(Exception ex){Debug.LogException(ex);File.WriteAllText("Logs/Eating/build-result.txt","FAIL\n"+ex);}
        }
        public static void Build()
        {
            Directory.CreateDirectory("Logs/Eating");
            const string output="Builds/Eating";Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=output+"/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Logs/Eating/build-summary.txt",report.summary.result+"; errors "+report.summary.totalErrors+"; warnings "+report.summary.totalWarnings+"; seconds "+report.summary.totalTime.TotalSeconds);
            File.WriteAllLines("Logs/Eating/build-messages.txt",report.steps.SelectMany(step=>step.messages).Where(message=>message.type==LogType.Warning||message.type==LogType.Error).Select(message=>message.type+": "+message.content));
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Eating review build failed");
            File.Copy("LICENSE.md",output+"/LICENSE.md",true);File.Copy(".docs/THIRD_PARTY_NOTICES.md",output+"/THIRD_PARTY_NOTICES.md",true);
            foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories))
            {string target=Path.Combine(output,"licenses",Path.GetRelativePath(".docs/licenses",source));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);}
            File.WriteAllText("Logs/Eating/build-result.txt","PASS "+DateTime.UtcNow.ToString("O"));
        }
    }
}
