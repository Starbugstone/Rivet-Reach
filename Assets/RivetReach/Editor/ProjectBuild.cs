using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RivetReach.Editor
{
    [InitializeOnLoad]
    public static class ProjectBuild
    {
        static double nextPoll;
        static ProjectBuild(){nextPoll=EditorApplication.timeSinceStartup+10;EditorApplication.update+=Poll;}
        // A local, explicit file request allows this project to build in its already-open Editor.
        // No network service and no changes to open scenes or unsaved user work.
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<nextPoll||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;
            nextPoll=EditorApplication.timeSinceStartup+2;
            const string request="Logs/build-request.txt";
            if(!File.Exists(request))return;
            string command=File.ReadAllText(request).Trim();
            if(command=="verify-editor")
            {
                File.Delete(request);
                try{EditorPlayVerification.Begin();}
                catch(Exception ex){Debug.LogException(ex);File.WriteAllText("Logs/build-result.txt","FAILED\n"+ex);}
                return;
            }
            if(!command.EndsWith("|ready"))
            {
                File.WriteAllText(request,command+"|ready");nextPoll=EditorApplication.timeSinceStartup+10;
                UnityEditor.PackageManager.Client.Resolve();AssetDatabase.Refresh();return;
            }
            command=command.Replace("|ready","");File.Delete(request);
            try
            {
                if(command=="craft-checks")
                {
                    CraftingChecks.Run();File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));return;
                }
                Prepare();DomainChecks.Run();
                if(command=="build")Build();
                File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));
            }
            catch(Exception ex){Debug.LogException(ex);File.WriteAllText("Logs/build-result.txt","FAILED\n"+ex);}
        }
        [MenuItem("Rivet Reach/Prepare assets and validate")]
        public static void Prepare()
        {
            foreach(string name in new[]{"GripSword","GripPickaxe"})
            {
                var prop=(ModelImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Characters/"+name+".fbx");
                if(prop.materialImportMode!=ModelImporterMaterialImportMode.None||prop.importAnimation)
                {prop.materialImportMode=ModelImporterMaterialImportMode.None;prop.importAnimation=false;prop.SaveAndReimport();}
            }
            foreach(string name in new[]{"ExplorerMale","ExplorerFemale"})
            {
                string path="Assets/RivetReach/Resources/Characters/"+name+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                if(!importer.isReadable||importer.materialImportMode!=ModelImporterMaterialImportMode.None||importer.animationType!=ModelImporterAnimationType.Generic)
                {importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.animationType=ModelImporterAnimationType.Generic;importer.optimizeGameObjects=false;importer.SaveAndReimport();}
            }
            foreach(string name in new[]{"ExplorerMale","ExplorerFemale"})
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Characters/"+name+".fbx");
                var clips=importer.defaultClipAnimations;
                foreach(var clip in clips)
                {
                    clip.name=clip.takeName.Substring(clip.takeName.LastIndexOf('|')+1);
                    clip.loopTime=!clip.name.Contains("Mine")&&clip.name!="Land"&&clip.name!="FP_Land";
                    clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;
                    clip.keepOriginalOrientation=true;clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;
                }
                var current=importer.clipAnimations;
                bool changed=!importer.importAnimation||current.Length!=clips.Length||importer.animationCompression!=ModelImporterAnimationCompression.Off;
                if(!changed)changed=current.Where((c,i)=>c.name!=clips[i].name||c.takeName!=clips[i].takeName||c.firstFrame!=clips[i].firstFrame||c.lastFrame!=clips[i].lastFrame||c.loopTime!=clips[i].loopTime||!c.lockRootHeightY||!c.lockRootPositionXZ||!c.lockRootRotation).Any();
                if(changed){importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;importer.clipAnimations=clips;importer.SaveAndReimport();}
            }
            foreach(string name in new[]{"SkinField","SkinOchre"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Characters/"+name+".png");
                if(importer.filterMode!=FilterMode.Bilinear||importer.textureCompression!=TextureImporterCompression.Uncompressed)
                {importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();}
            }
            var axeImporter=(ModelImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Tools/StarterAxe.fbx");
            if(axeImporter!=null&&(!axeImporter.useFileScale||axeImporter.importAnimation||axeImporter.materialImportMode!=ModelImporterMaterialImportMode.None||!axeImporter.isReadable))
            {axeImporter.useFileScale=true;axeImporter.importAnimation=false;axeImporter.materialImportMode=ModelImporterMaterialImportMode.None;axeImporter.isReadable=true;axeImporter.SaveAndReimport();}
            foreach(string toolTexture in new[]{"StarterAxe","StarterAxeIcon","StarterPickaxeIcon","StarterDaggerIcon"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Tools/"+toolTexture+".png");
                if(importer!=null&&(importer.textureShape!=TextureImporterShape.Texture2D||importer.filterMode!=FilterMode.Point||importer.textureCompression!=TextureImporterCompression.Uncompressed))
                {importer.textureShape=TextureImporterShape.Texture2D;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=!toolTexture.EndsWith("Icon");importer.alphaIsTransparency=toolTexture.EndsWith("Icon");importer.SaveAndReimport();}
            }
            const string definitions="Assets/RivetReach/Resources/Definitions/Items.asset";
            var registry=AssetDatabase.LoadAssetAtPath<ItemRegistry>(definitions);
            if(registry==null)
            {
                registry=ScriptableObject.CreateInstance<ItemRegistry>();registry.items=new[]{
                    new ItemDefinition{runtimeId=1,stableId="rivet:grass",displayName="Grass",fistSeconds=.45f,colour=new Color(.43f,.56f,.26f)},
                    new ItemDefinition{runtimeId=2,stableId="rivet:dirt",displayName="Dirt",fistSeconds=.6f,colour=new Color(.44f,.30f,.19f)},
                    new ItemDefinition{runtimeId=3,stableId="rivet:stone",displayName="Stone",fistSeconds=.95f,colour=new Color(.48f,.51f,.51f)}};
                AssetDatabase.CreateAsset(registry,definitions);
            }
            foreach(var item in new[]{
                new ItemDefinition{runtimeId=BlockId.Log,stableId="rivet:log",displayName="Log",fistSeconds=1.5f,colour=new Color(.40f,.27f,.14f)},
                new ItemDefinition{runtimeId=BlockId.Leaves,stableId="rivet:leaves",displayName="Leaves",fistSeconds=.2f,colour=new Color(.28f,.45f,.18f)},
                new ItemDefinition{runtimeId=BlockId.StarterAxe,stableId="rivet:starter_axe",displayName="Starter axe",stackLimit=1,toolCapabilities=ToolCapability.Axe,colour=new Color(.6f,.65f,.67f)},
                new ItemDefinition{runtimeId=BlockId.StarterPickaxe,stableId="rivet:starter_pickaxe",displayName="Starter pickaxe",stackLimit=1,toolCapabilities=ToolCapability.Pickaxe,colour=new Color(.6f,.65f,.67f)},
                new ItemDefinition{runtimeId=BlockId.StarterDagger,stableId="rivet:starter_dagger",displayName="Starter dagger",stackLimit=1,toolCapabilities=ToolCapability.Blade,colour=new Color(.6f,.65f,.67f)}})
                if(!registry.items.Any(i=>i.runtimeId==item.runtimeId))registry.items=registry.items.Append(item).ToArray();
            registry.Get(1).fistDropId=2;EditorUtility.SetDirty(registry);
            var tiles=TerrainTiles.Build();
            MaterialAsset("Terrain","RivetReach/VoxelTerrain").SetTexture("_Tiles",tiles);
            var player=MaterialAsset("Player","Universal Render Pipeline/Lit");player.SetFloat("_Smoothness",.12f);player.SetTexture("_BaseMap",Resources.Load<Texture2D>("Characters/SkinField"));
            MaterialAsset("Selection","Universal Render Pipeline/Unlit");MaterialAsset("Sky","RivetReach/ExpeditionSky");
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/RivetReach/Settings/SampleSceneProfile.asset");
            if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.08f);bloom.threshold.Override(1.05f);EditorUtility.SetDirty(bloom);}
            if(profile.TryGet<Vignette>(out var vignette)){vignette.intensity.Override(.10f);vignette.smoothness.Override(.45f);EditorUtility.SetDirty(vignette);}
            if(!profile.TryGet<ColorAdjustments>(out var grading))
            {grading=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(grading,profile);}
            grading.postExposure.Override(.10f);grading.contrast.Override(6);grading.saturation.Override(2);EditorUtility.SetDirty(grading);EditorUtility.SetDirty(profile);
            PlayerSettings.companyName="Starbugstone";PlayerSettings.productName="Rivet Reach";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/RivetReach/Scenes/Main.unity",true)};
            AssetDatabase.SaveAssets();
            var models=new System.Text.StringBuilder();
            foreach(string name in new[]{"ExplorerMale","ExplorerFemale"})
            {
                var prefab=Resources.Load<GameObject>("Characters/"+name);
                foreach(var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>())models.AppendLine(name+": "+renderer.sharedMesh.triangles.Length/3+" triangles; "+renderer.bones.Length+" bones; bounds "+renderer.bounds+"; scale "+prefab.transform.localScale);
            }
            File.WriteAllText("Logs/model-report.txt",models.ToString());
        }
        static Material MaterialAsset(string name,string shader)
        {
            string path="Assets/RivetReach/Resources/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){var found=Shader.Find(shader);if(found==null)throw new Exception("Missing shader "+shader);material=new Material(found);AssetDatabase.CreateAsset(material,path);}
            EditorUtility.SetDirty(material);return material;
        }
        [MenuItem("Rivet Reach/Build Windows first POC")]
        public static void PrepareAndBuild(){Prepare();DomainChecks.Run();Build();}
        static void Build()
        {
            Directory.CreateDirectory("Builds/PlayerRevision4");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Select(s=>s.path).ToArray(),locationPathName="Builds/PlayerRevision4/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Logs/build-summary.txt",report.summary.result+"; errors "+report.summary.totalErrors+"; warnings "+report.summary.totalWarnings+"; seconds "+report.summary.totalTime.TotalSeconds+"; bytes "+report.summary.totalSize);
            if(report.summary.result!=BuildResult.Succeeded||report.summary.totalErrors>0)throw new Exception("Windows build failed: "+report.summary.result+"; errors "+report.summary.totalErrors);
            File.Copy("LICENSE.md","Builds/PlayerRevision4/LICENSE.md",true);
            File.Copy(".docs/THIRD_PARTY_NOTICES.md","Builds/PlayerRevision4/THIRD_PARTY_NOTICES.md",true);
            foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories))
            {
                string target=Path.Combine("Builds/PlayerRevision4/licenses",Path.GetRelativePath(".docs/licenses",source));
                Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);
            }
        }
    }
}
