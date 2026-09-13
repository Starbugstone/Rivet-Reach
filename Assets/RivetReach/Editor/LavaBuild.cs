using System;
using System.IO;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach.Editor
{
    [InitializeOnLoad]
    public static class LavaBuild
    {
        static double nextPoll;
        static LavaBuild()=>EditorApplication.update+=Poll;
        static void Poll()
        {
            const string request="Logs/lava-build-request.txt";
            if(EditorApplication.timeSinceStartup<nextPoll||!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||BuildPipeline.isBuildingPlayer||File.Exists("Logs/build-request.txt"))return;
            string command=File.ReadAllText(request).Trim();
            if(!command.EndsWith("|ready")){File.WriteAllText(request,command+"|ready");nextPoll=EditorApplication.timeSinceStartup+5;AssetDatabase.Refresh();return;}
            File.Delete(request);
            try
            {
                var registry=ItemRegistry.Load();FluidAssets.Prepare(registry);EditorUtility.SetDirty(registry);AssetDatabase.SaveAssets();
                FluidChecks.Run();LavaChecks.Run();TerrainGenerationChecks.Run((ok,message)=>{if(!ok)throw new Exception(message);});
                if(command.StartsWith("build")){Directory.CreateDirectory("Builds/Lava");ItemAppearanceBuild.BakeOnly(Fluids.LavaBucket);WikiExport.Export();bool automatic=PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
                    var graphics=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);BuildReport result;
                    try
                    {
                        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
                        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
                        result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName="Builds/Lava/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                    }
                    finally{PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,graphics);PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,automatic);}

                    File.WriteAllText("Logs/lava-build-summary.txt",$"{result.summary.result}; errors {result.summary.totalErrors}; warnings {result.summary.totalWarnings}; seconds {result.summary.totalTime.TotalSeconds}; bytes {result.summary.totalSize}");
                    if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Lava build failed: "+result.summary.result);
                    File.Copy("LICENSE.md","Builds/Lava/LICENSE.md",true);File.Copy(".docs/THIRD_PARTY_NOTICES.md","Builds/Lava/THIRD_PARTY_NOTICES.md",true);
                    foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories)){string target=Path.Combine("Builds/Lava/licenses",Path.GetRelativePath(".docs/licenses",source));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);}}
                File.WriteAllText("Logs/lava-build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));
            }
            catch(Exception ex){Debug.LogException(ex);File.WriteAllText("Logs/lava-build-result.txt","FAILED\n"+ex);}
        }
    }
}
