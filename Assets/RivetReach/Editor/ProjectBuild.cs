using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

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
            if(!command.EndsWith("|ready"))
            {
                File.WriteAllText(request,command+"|ready");nextPoll=EditorApplication.timeSinceStartup+10;
                AssetDatabase.Refresh();return;
            }
            command=command.Replace("|ready","");File.Delete(request);
            try
            {
                Prepare();DomainChecks.Run();
                if(command=="build")Build();
                File.WriteAllText("Logs/build-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));
            }
            catch(Exception ex){Debug.LogException(ex);File.WriteAllText("Logs/build-result.txt","FAILED\n"+ex);}
        }
        [MenuItem("Rivet Reach/Prepare assets and validate")]
        public static void Prepare()
        {
            foreach(string name in new[]{"ExplorerMale","ExplorerFemale"})
            {
                string path="Assets/RivetReach/Resources/Characters/"+name+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                if(!importer.isReadable||importer.materialImportMode!=ModelImporterMaterialImportMode.None||importer.animationType!=ModelImporterAnimationType.Generic)
                {importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.animationType=ModelImporterAnimationType.Generic;importer.optimizeGameObjects=false;importer.SaveAndReimport();}
            }
            foreach(string name in new[]{"SkinField","SkinOchre"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Characters/"+name+".png");
                if(importer.filterMode!=FilterMode.Point||importer.textureCompression!=TextureImporterCompression.Uncompressed)
                {importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();}
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
            const string tilesPath="Assets/RivetReach/Resources/Materials/BlockTiles.asset";
            var tiles=AssetDatabase.LoadAssetAtPath<Texture2DArray>(tilesPath);
            if(tiles!=null&&tiles.width!=32){AssetDatabase.DeleteAsset(tilesPath);tiles=null;}
            if(tiles==null)
            {
                tiles=new Texture2DArray(32,32,4,TextureFormat.RGBA32,true,false){name="Original block tiles",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
                var random=new System.Random(471);
                for(int layer=0;layer<4;layer++)
                {
                    var pixels=new Color[1024];
                    for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                    {
                        Color c=layer==0?registry.items[0].colour:layer==3?registry.items[2].colour:registry.items[1].colour;
                        if(layer==1&&y>26+(x*7%3))c=registry.items[0].colour;
                        float shade=.88f+(float)random.NextDouble()*.23f;
                        if(layer==3&&((x+y*3)%17==0))shade*=.83f;
                        pixels[x+y*32]=new Color(c.r*shade,c.g*shade,c.b*shade,1);
                    }
                    tiles.SetPixels(pixels,layer);
                }
                tiles.Apply(true,false);AssetDatabase.CreateAsset(tiles,tilesPath);
            }
            MaterialAsset("Terrain","RivetReach/VoxelTerrain").SetTexture("_Tiles",tiles);
            var player=MaterialAsset("Player","Universal Render Pipeline/Lit");player.SetFloat("_Smoothness",0);player.SetTexture("_BaseMap",Resources.Load<Texture2D>("Characters/SkinField"));
            MaterialAsset("Selection","Universal Render Pipeline/Unlit");MaterialAsset("Sky","RivetReach/ExpeditionSky");
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
            Directory.CreateDirectory("Builds/FirstPOC");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Select(s=>s.path).ToArray(),locationPathName="Builds/FirstPOC/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Logs/build-summary.txt",report.summary.result+"; errors "+report.summary.totalErrors+"; warnings "+report.summary.totalWarnings+"; seconds "+report.summary.totalTime.TotalSeconds+"; bytes "+report.summary.totalSize);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
            File.Copy("LICENSE.md","Builds/FirstPOC/LICENSE.md",true);
            File.Copy(".docs/THIRD_PARTY_NOTICES.md","Builds/FirstPOC/THIRD_PARTY_NOTICES.md",true);
            foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories))
            {
                string target=Path.Combine("Builds/FirstPOC/licenses",Path.GetRelativePath(".docs/licenses",source));
                Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);
            }
        }
    }
}
