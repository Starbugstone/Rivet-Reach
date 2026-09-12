using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach.Editor
{
    [InitializeOnLoad]
    public static class ConnectionBuild
    {
        static double nextPoll;
        static ConnectionBuild(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Logs/connections-build-request.txt";
            if(EditorApplication.timeSinceStartup<nextPoll||!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||BuildPipeline.isBuildingPlayer||File.Exists("Logs/build-request.txt"))return;
            string command=File.ReadAllText(request).Trim();
            if(!command.EndsWith("|ready")){File.WriteAllText(request,command+"|ready");nextPoll=EditorApplication.timeSinceStartup+5;AssetDatabase.Refresh();return;}
            command=command.Replace("|ready","");File.Delete(request);
            try{Run(command!="checks");File.WriteAllText("Logs/connections-build-result.txt","PASS");}
            catch(Exception error){Debug.LogException(error);File.WriteAllText("Logs/connections-build-result.txt",error.ToString());}
        }
        [MenuItem("Rivet Reach/Build configurable connections review")]
        public static void Build()=>Run(true);
        static void Run(bool build)
        {
            Directory.CreateDirectory("Logs/Connections");
            WrenchAssets.Prepare();
            ConnectionChecks.Run();BatteryChecks.Run();HandCrankChecks.Run();DoorChecks.Run();MultiblockChecks.Run();ConnectedPipeChecks.Run();DomainChecks.Run();
            if(!build)return;
            // Same verified renderer as the alpha release; preserve the user's
            // editor settings after building this review player.
            bool automatic=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
            var graphics=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);BuildReport result;
            try
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
                result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName="Builds/Connections/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            }
            finally{PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,graphics);PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,automatic);}
            File.WriteAllText("Logs/Connections/build.txt",result.summary.result+"; errors "+result.summary.totalErrors+"; warnings "+result.summary.totalWarnings+"; seconds "+result.summary.totalTime.TotalSeconds);
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Connections build failed");
            File.Copy("LICENSE.md","Builds/Connections/LICENSE.md",true);File.Copy(".docs/THIRD_PARTY_NOTICES.md","Builds/Connections/THIRD_PARTY_NOTICES.md",true);
            foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories))
            {string target=Path.Combine("Builds/Connections/licenses",Path.GetRelativePath(".docs/licenses",source));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);}
        }
    }
}
