using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class MobBuild
    {
        [MenuItem("Rivet Reach/Mobs/Build Windows review")]
        public static void Build()
        {
            ProjectBuild.Prepare();
            MobAssetImport.Prepare();
            DomainChecks.Run();
            Directory.CreateDirectory("Builds/Mobs");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{"Assets/RivetReach/Scenes/Main.unity"},
                locationPathName="Builds/Mobs/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Logs/mob-build-summary.txt",report.summary.result+"; errors "+report.summary.totalErrors+"; warnings "+report.summary.totalWarnings+"; seconds "+report.summary.totalTime.TotalSeconds);
            File.WriteAllLines("Logs/mob-build-messages.txt",report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.type+": "+m.content));
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Mob review build failed.");
            File.Copy("LICENSE.md","Builds/Mobs/LICENSE.md",true);
            File.Copy(".docs/THIRD_PARTY_NOTICES.md","Builds/Mobs/THIRD_PARTY_NOTICES.md",true);
            foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories))
            {
                string target=Path.Combine("Builds/Mobs/licenses",Path.GetRelativePath(".docs/licenses",source));
                Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);
            }
        }
    }
}
