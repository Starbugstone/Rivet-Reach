using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class MachineryShowcaseBuild
    {
        public static void PreparePipesAndBuild()
        {
            IndustryAssets.PrepareConnectedPipes();ConnectedPipeChecks.Run();
            MultiblockChecks.Run();IndustryChecks.Run();Build();
        }
        [MenuItem("Rivet Reach/Build Machinery Showcase")]
        public static void Build()
        {
            const string directory="Builds/MachineryShowcase";
            Directory.CreateDirectory(directory);Directory.CreateDirectory("Logs");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                locationPathName=directory+"/RivetReach.exe",target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.None
            });
            File.WriteAllText("Logs/showcase-build.txt",$"{result.summary.result}; errors {result.summary.totalErrors}; warnings {result.summary.totalWarnings}; seconds {result.summary.totalTime.TotalSeconds}");
            File.WriteAllLines("Logs/showcase-build-messages.txt",result.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Error||m.type==LogType.Warning).Select(m=>m.content));
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Showcase player build failed");
            File.Copy("LICENSE.md",directory+"/LICENSE.md",true);
            File.Copy(".docs/THIRD_PARTY_NOTICES.md",directory+"/THIRD_PARTY_NOTICES.md",true);
            foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories))
            {
                string target=Path.Combine(directory,"licenses",Path.GetRelativePath(".docs/licenses",source));
                Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);
            }
        }
    }
}
