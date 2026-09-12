using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RivetReach.Editor
{
    [InitializeOnLoad]
    public static class StarterStationBuild
    {
        static StarterStationBuild(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Logs/starter-stations-build-request.txt";
            if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||BuildPipeline.isBuildingPlayer||File.Exists("Logs/build-request.txt"))return;
            File.Delete(request);
            try{Run();File.WriteAllText("Logs/starter-stations-build-result.txt","PASS");}
            catch(Exception error){Debug.LogException(error);File.WriteAllText("Logs/starter-stations-build-result.txt",error.ToString());}
        }
        [MenuItem("Rivet Reach/Build starter station graphics review")]
        public static void Run()
        {
            const string logs="Logs/StarterStations";Directory.CreateDirectory(logs);AssetDatabase.Refresh();
            var material=Resources.Load<Material>("Industry/Workshop");
            var keys=new[]{"workbench","furnace","chest"};
            foreach(string key in keys)
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/"+key+".fbx");
                importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"WorkshopAtlas"),material);importer.SaveAndReimport();
            }
            foreach(byte id in new[]{BlockId.Workbench,BlockId.Furnace,BlockId.Chest})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/Icons/"+id+".png");
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            IndustryAssets.NormalizeModels(material,keys);
            const string emberPath="Assets/RivetReach/Resources/Industry/StarterEmbers.mat";
            var ember=AssetDatabase.LoadAssetAtPath<Material>(emberPath);
            if(ember==null){ember=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(ember,emberPath);}
            ember.SetColor("_BaseColor",new Color(.16f,.055f,.018f));ember.SetFloat("_Smoothness",.12f);
            ember.SetColor("_EmissionColor",Color.black);ember.EnableKeyword("_EMISSION");ember.enableInstancing=true;EditorUtility.SetDirty(ember);
            const string furnacePath="Assets/RivetReach/Resources/Industry/Runtime/furnace.prefab";
            var root=PrefabUtility.LoadPrefabContents(furnacePath);
            try{root.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Embers").sharedMaterial=ember;PrefabUtility.SaveAsPrefabAsset(root,furnacePath);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
            int assertions=0;void Check(bool ok,string message){if(!ok)throw new Exception(message);assertions++;}
            var report=new System.Collections.Generic.List<string>();
            foreach(byte id in new[]{BlockId.Workbench,BlockId.Furnace,BlockId.Chest})
            {
                var prefab=StarterStationVisuals.Prefab(id);Check(prefab!=null,"Missing starter station prefab");
                int tris=0;var bounds=new Bounds();bool first=true;
                foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh=filter.sharedMesh;tris+=mesh.triangles.Length/3;
                    Check(mesh.normals.Length==mesh.vertexCount&&mesh.uv.Length==mesh.vertexCount,"Normals/UV stream missing");
                    Check(filter.GetComponent<Renderer>().sharedMaterial!=null,"Material missing");
                    foreach(var point in mesh.vertices){var v=filter.transform.TransformPoint(point);if(first){bounds=new Bounds(v,Vector3.zero);first=false;}else bounds.Encapsulate(v);}
                }
                Check(tris>100&&tris<5000,"Station geometry budget");
                Check(bounds.min.x>=-.001f&&bounds.min.y>=-.001f&&bounds.min.z>=-.001f&&bounds.max.x<=1.001f&&bounds.max.y<=1.001f&&bounds.max.z<=1.001f,"Metre grid bounds");
                var cells=new byte[34*34*34];cells[ChunkMesher.Index(0,0,0)]=id;
                Check(ChunkMesher.Build(default,0,cells).Triangles.Length==0,"Old station cube remains");
                cells[ChunkMesher.Index(1,0,0)]=BlockId.Stone;
                Check(ChunkMesher.Build(default,0,cells).Triangles.Length==36,"Station hides its neighbouring terrain faces");
                Check(BlockId.Solid(id)&&BlockId.Station(id)&&BlockId.Placeable(id),"Station gameplay categories changed");
                report.Add($"{StarterStationVisuals.Key(id)}: {tris} triangles; {prefab.GetComponentsInChildren<Renderer>().Length} renderers; bounds {bounds.min} to {bounds.max}");
            }
            File.WriteAllLines(logs+"/import-checks.txt",report.Append("PASS "+assertions+" import/mesh assertions"));
            SurvivalChecks.Run();StarterRecipeChecks.Run(ItemRegistry.Load(),RecipeCatalogAsset.Load().Compile(ItemRegistry.Load()));
            const string output="Builds/StarterStations";Directory.CreateDirectory(output);
            var build=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=output+"/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText(logs+"/build-summary.txt",$"{build.summary.result}; errors {build.summary.totalErrors}; warnings {build.summary.totalWarnings}; seconds {build.summary.totalTime.TotalSeconds}\n");
            if(build.summary.result!=BuildResult.Succeeded)throw new Exception("Starter station review build failed");
            File.Copy("LICENSE.md",output+"/LICENSE.md",true);File.Copy(".docs/THIRD_PARTY_NOTICES.md",output+"/THIRD_PARTY_NOTICES.md",true);
            foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories))
            {string target=Path.Combine(output,"licenses",Path.GetRelativePath(".docs/licenses",source));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);}
        }
    }
}
