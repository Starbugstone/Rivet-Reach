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
                if(command=="multiblock-player")
                {Build("Multiblocks");File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));return;}
                if(command=="multiblock-logic")
                {MultiblockChecks.Run();File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));return;}
                if(command=="multiblock-build"||command=="multiblock-checks")
                {IndustryAssets.Prepare();MultiblockChecks.Run();IndustryChecks.Run();DomainChecks.Run();StarterRecipeChecks.Run(ItemRegistry.Load(),RecipeCatalogAsset.Load().Compile(ItemRegistry.Load()));FluidChecks.Run();if(command=="multiblock-build")Build("Multiblocks");File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));return;}
                if(command=="industry-regression")
                {DomainChecks.Run();StarterRecipeChecks.Run(ItemRegistry.Load(),RecipeCatalogAsset.Load().Compile(ItemRegistry.Load()));FluidChecks.Run();File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));return;}
                if(command=="industry-logic")
                {IndustryChecks.Run();File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));return;}
                if(command=="industry-build"||command=="industry-checks")
                {IndustryAssets.Prepare();IndustryChecks.Run();if(command=="industry-build")Build("Industry");File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));return;}
                if(command=="torch-build")
                {TorchAssets.Prepare();DomainChecks.Run();FluidChecks.Run();Build("Torches");File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));return;}
                if(command=="creative-build")
                {DomainChecks.Run();MultiblockChecks.Run();IndustryChecks.Run();ConnectedPipeChecks.Run();File.WriteAllText("Logs/creative-transfer-checks.txt","PASS "+RecipeTransferChecks.Run(ItemRegistry.Load(),RecipeCatalogAsset.Load().Compile(ItemRegistry.Load()))+" recipe transfer assertions");Build("Creative");File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));return;}
                if(command=="fluid-checks")
                {FluidChecks.Run();File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));return;}
                if(command=="craft-checks")
                {
                    CraftingChecks.Run();File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));return;
                }
                if(command=="terrain-build"||command=="terrain-checks")
                {
                    Prepare();TerrainGenerationChecks.Run((ok,message)=>{if(!ok)throw new Exception(message);});
                    if(command=="terrain-build")Build("Terrain");
                    File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));return;
                }
                Prepare();DomainChecks.Run();FluidChecks.Run();
                if(command=="build")Build();
                if(command=="fluid-build")Build("Fluids");
                if(command=="survival-build")Build("Survival");
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
            foreach(string name in new[]{"SkinNormal","SkinSurface"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Characters/"+name+".png");
                var type=name=="SkinNormal"?TextureImporterType.NormalMap:TextureImporterType.Default;
                if(importer.textureType!=type||importer.sRGBTexture||importer.textureCompression!=TextureImporterCompression.Uncompressed)
                {importer.textureType=type;importer.sRGBTexture=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();}
            }
            foreach(string path in Directory.GetFiles("Assets/RivetReach/Resources/Audio","*.wav"))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
                bool ambience=path.EndsWith("WindCanopy.wav");
                var load=ambience?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;
                var format=ambience?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;
                if(settings.loadType!=load||settings.compressionFormat!=format||settings.sampleRateSetting!=AudioSampleRateSetting.PreserveSampleRate)
                {settings.loadType=load;settings.compressionFormat=format;settings.quality=.85f;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;importer.defaultSampleSettings=settings;importer.SaveAndReimport();}
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
                new ItemDefinition{runtimeId=BlockId.StarterDagger,stableId="rivet:starter_dagger",displayName="Starter dagger",stackLimit=1,toolCapabilities=ToolCapability.Blade,colour=new Color(.6f,.65f,.67f)},
                new ItemDefinition{runtimeId=BlockId.IronOre,stableId="rivet:iron_ore",displayName="Iron ore",fistSeconds=1.5f,fistDropId=BlockId.RawIron,colour=new Color(.58f,.34f,.24f)},
                new ItemDefinition{runtimeId=BlockId.CopperOre,stableId="rivet:copper_ore",displayName="Copper ore",fistSeconds=1.3f,fistDropId=BlockId.RawCopper,colour=new Color(.75f,.43f,.21f)},
                new ItemDefinition{runtimeId=BlockId.CoalOre,stableId="rivet:coal_ore",displayName="Coal ore",fistSeconds=1.1f,fistDropId=BlockId.Coal,colour=new Color(.16f,.18f,.21f)},
                new ItemDefinition{runtimeId=BlockId.GoldOre,stableId="rivet:gold_ore",displayName="Gold ore",fistSeconds=1.8f,fistDropId=BlockId.RawGold,colour=new Color(.85f,.65f,.19f)},
                new ItemDefinition{runtimeId=BlockId.DiamondOre,stableId="rivet:diamond_ore",displayName="Diamond ore",fistSeconds=2.1f,fistDropId=BlockId.Diamond,colour=new Color(.44f,.84f,.88f)},
                new ItemDefinition{runtimeId=BlockId.Bedrock,stableId="rivet:bedrock",displayName="Bedrock",colour=new Color(.18f,.20f,.22f)},
                new ItemDefinition{runtimeId=BlockId.RawIron,stableId="rivet:raw_iron",displayName="Raw iron",colour=new Color(.58f,.34f,.24f)},
                new ItemDefinition{runtimeId=BlockId.RawCopper,stableId="rivet:raw_copper",displayName="Raw copper",colour=new Color(.75f,.43f,.21f)},
                new ItemDefinition{runtimeId=BlockId.Coal,stableId="rivet:coal",displayName="Coal",colour=new Color(.16f,.18f,.21f)},
                new ItemDefinition{runtimeId=BlockId.RawGold,stableId="rivet:raw_gold",displayName="Raw gold",colour=new Color(.85f,.65f,.19f)},
                new ItemDefinition{runtimeId=BlockId.Diamond,stableId="rivet:diamond",displayName="Diamond",colour=new Color(.44f,.84f,.88f)}})
                if(!registry.items.Any(i=>i.runtimeId==item.runtimeId))registry.items=registry.items.Append(item).ToArray();
            BiomeTerrainAssets.Items(registry);FluidAssets.Prepare(registry);
            registry.Get(1).fistDropId=2;EditorUtility.SetDirty(registry);
            MaterialAsset("Water","RivetReach/Fluid");
            var tiles=TerrainTiles.Build();
            MaterialAsset("Terrain","RivetReach/VoxelTerrain").SetTexture("_Tiles",tiles);
            var player=MaterialAsset("Player","RivetReach/ExplorerSkin");player.shader=Shader.Find("RivetReach/ExplorerSkin");
            player.SetTexture("_BaseMap",Resources.Load<Texture2D>("Characters/SkinField"));
            player.SetTexture("_SurfaceMap",Resources.Load<Texture2D>("Characters/SkinSurface"));
            player.SetTexture("_BumpMap",Resources.Load<Texture2D>("Characters/SkinNormal"));
            player.SetFloat("_FirstPerson",0);player.SetColor("_BaseColor",Color.white);
            MaterialAsset("Selection","Universal Render Pipeline/Unlit");MaterialAsset("Sky","RivetReach/ExpeditionSky");
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/RivetReach/Settings/SampleSceneProfile.asset");
            if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.08f);bloom.threshold.Override(1.05f);EditorUtility.SetDirty(bloom);}
            if(profile.TryGet<Vignette>(out var vignette)){vignette.intensity.Override(.10f);vignette.smoothness.Override(.45f);EditorUtility.SetDirty(vignette);}
            if(!profile.TryGet<ColorAdjustments>(out var grading))
            {grading=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(grading,profile);}
            grading.postExposure.Override(.10f);grading.contrast.Override(6);grading.saturation.Override(2);EditorUtility.SetDirty(grading);EditorUtility.SetDirty(profile);
            ArcadeTerrainArt.Apply(tiles,MaterialAsset("Terrain","RivetReach/VoxelTerrain"),profile);
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
        public static void PrepareAndBuildMultiblocks(){IndustryAssets.Prepare();MultiblockChecks.Run();IndustryChecks.Run();DomainChecks.Run();StarterRecipeChecks.Run(ItemRegistry.Load(),RecipeCatalogAsset.Load().Compile(ItemRegistry.Load()));FluidChecks.Run();Build("Multiblocks");}
        public static void PrepareAndBuild(){Prepare();DomainChecks.Run();FluidChecks.Run();Build();}
        public static void PrepareAndBuildSurvival(){Prepare();DomainChecks.Run();FluidChecks.Run();Build("Survival");}
        static void Build(string outputFolder="PlayerRevision4")
        {
            string output=Path.Combine("Builds",outputFolder);Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Select(s=>s.path).ToArray(),locationPathName=Path.Combine(output,"RivetReach.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Logs/build-summary.txt",report.summary.result+"; errors "+report.summary.totalErrors+"; warnings "+report.summary.totalWarnings+"; seconds "+report.summary.totalTime.TotalSeconds+"; bytes "+report.summary.totalSize);
            File.WriteAllLines("Logs/build-messages.txt",report.steps.SelectMany(step=>step.messages).Where(message=>message.type==LogType.Warning||message.type==LogType.Error).Select(message=>message.type+": "+message.content));
            if(report.summary.result!=BuildResult.Succeeded||report.summary.totalErrors>0)throw new Exception("Windows build failed: "+report.summary.result+"; errors "+report.summary.totalErrors);
            File.Copy("LICENSE.md",Path.Combine(output,"LICENSE.md"),true);
            File.Copy(".docs/THIRD_PARTY_NOTICES.md",Path.Combine(output,"THIRD_PARTY_NOTICES.md"),true);
            foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories))
            {
                string target=Path.Combine(output,"licenses",Path.GetRelativePath(".docs/licenses",source));
                Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);
            }
        }
    }
}
