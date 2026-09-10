using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RivetReach.Editor
{
    // Independent request/output files let player art iterate alongside other features.
    [InitializeOnLoad]
    public static class AvatarReworkBuild
    {
        const string Request="Logs/avatar-build-request.txt";
        static double next;
        static AvatarReworkBuild(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||BuildPipeline.isBuildingPlayer)return;
            next=EditorApplication.timeSinceStartup+2;
            if(!File.Exists(Request)||File.Exists("Logs/build-request.txt"))return;
            if(File.ReadAllText(Request).Trim()!="ready")
            {File.WriteAllText(Request,"ready");AssetDatabase.Refresh();next=EditorApplication.timeSinceStartup+10;return;}
            File.Delete(Request);
            try{Build();}
            catch(Exception ex){Debug.LogException(ex);File.WriteAllText("Logs/AvatarRework/build-result.txt","FAIL\n"+ex);}
        }
        [MenuItem("Rivet Reach/Build avatar rework review")]
        public static void Build()
        {
            Directory.CreateDirectory("Logs/AvatarRework");
            foreach(string name in new[]{"ExplorerMale","ExplorerFemale"})
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Characters/"+name+".fbx");
                importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;
                importer.animationType=ModelImporterAnimationType.Generic;importer.optimizeGameObjects=false;
                importer.animationCompression=ModelImporterAnimationCompression.Off;
                var clips=importer.defaultClipAnimations;
                foreach(var clip in clips)
                {
                    clip.name=clip.takeName.Substring(clip.takeName.LastIndexOf('|')+1);
                    clip.loopTime=!clip.name.Contains("Mine")&&clip.name!="Land"&&clip.name!="FP_Land";
                    clip.lockRootRotation=clip.lockRootHeightY=clip.lockRootPositionXZ=true;
                    clip.keepOriginalOrientation=clip.keepOriginalPositionY=clip.keepOriginalPositionXZ=true;
                }
                importer.clipAnimations=clips;importer.SaveAndReimport();
            }
            const string output="Builds/AvatarRework";Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=output+"/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Logs/AvatarRework/build-summary.txt",report.summary.result+"; errors "+report.summary.totalErrors+"; warnings "+report.summary.totalWarnings+"; seconds "+report.summary.totalTime.TotalSeconds+"; bytes "+report.summary.totalSize);
            File.WriteAllLines("Logs/AvatarRework/build-messages.txt",report.steps.SelectMany(s=>s.messages).Where(m=>m.type==LogType.Warning||m.type==LogType.Error).Select(m=>m.type+": "+m.content));
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Avatar review build failed");
            File.Copy("LICENSE.md",output+"/LICENSE.md",true);File.Copy(".docs/THIRD_PARTY_NOTICES.md",output+"/THIRD_PARTY_NOTICES.md",true);
            foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories))
            {string target=Path.Combine(output,"licenses",Path.GetRelativePath(".docs/licenses",source));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);}
            File.WriteAllText("Logs/AvatarRework/build-result.txt","PASS "+DateTime.UtcNow.ToString("O"));
        }
    }
}
