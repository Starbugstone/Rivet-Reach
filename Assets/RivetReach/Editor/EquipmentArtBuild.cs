using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RivetReach.Editor
{
    [InitializeOnLoad]
    public static class EquipmentArtBuild
    {
        static EquipmentArtBuild(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Logs/equipment-art-request.txt";
            if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||BuildPipeline.isBuildingPlayer||File.Exists("Logs/build-request.txt"))return;
            string command=File.ReadAllText(request).Trim();File.Delete(request);
            try{Prepare();if(command=="build")Build();File.WriteAllText("Logs/ArmorArt/result.txt","PASS "+DateTime.UtcNow.ToString("O"));}
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/ArmorArt/result.txt","FAIL "+e);}
        }
        public static void Snapshot()
        {Prepare();DomainChecks.Run();SurvivalChecks.Run();Build();WikiExport.Export();}
        [MenuItem("Rivet Reach/Prepare armor and ingot art")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Logs/ArmorArt");AssetDatabase.Refresh();const string folder="Assets/RivetReach/Resources/Equipment/";
            foreach(var path in Directory.GetFiles(folder,"*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);bool palette=path.Contains("Palette");
                importer.alphaIsTransparency=!palette;importer.mipmapEnabled=palette;importer.filterMode=palette?FilterMode.Point:FilterMode.Bilinear;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            foreach(string tier in new[]{"Copper","Iron","Gold","Diamond"})
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(folder+tier+".mat");
                if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,folder+tier+".mat");}
                mat.SetTexture("_BaseMap",Resources.Load<Texture2D>("Equipment/"+tier+"Palette"));mat.SetFloat("_Metallic",.55f);mat.SetFloat("_Smoothness",.4f);EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
            var report=new System.Text.StringBuilder();
            foreach(var path in Directory.GetFiles(folder,"*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.importAnimation=false;
                importer.animationType=path.Contains("Armor")?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
                importer.optimizeGameObjects=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);int triangles=0;
                foreach(var mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())
                {
                    if(mesh.vertexCount==0||mesh.normals.Length!=mesh.vertexCount||mesh.uv.Length!=mesh.vertexCount||mesh.subMeshCount!=1)throw new Exception("Invalid equipment mesh "+path);
                    if(mesh.vertices.Any(p=>!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z)))throw new Exception("Nonfinite vertex "+path);
                    triangles+=mesh.triangles.Length/3;
                }
                if(path.Contains("Armor"))
                {
                    var rs=asset.GetComponentsInChildren<SkinnedMeshRenderer>();if(rs.Length!=4||triangles>12000)throw new Exception("Four fitted slots and 12000 triangle ceiling: "+path);
                    foreach(var r in rs)if(r.sharedMesh.boneWeights.Length!=r.sharedMesh.vertexCount||r.bones.Any(b=>b==null))throw new Exception("Missing weights or bones: "+path);
                }
                report.AppendLine(Path.GetFileName(path)+": "+triangles+" triangles");
            }
            var registry=ItemRegistry.Load();
            foreach(var item in registry.items.Where(i=>EquipmentVisuals.UsesModel(i.runtimeId)))
            {
                if(EquipmentVisuals.Icon(item.runtimeId)==null||EquipmentVisuals.Material(item.runtimeId)==null)throw new Exception("Missing equipment palette/icon "+item.displayName);
                if(item.armorSlot!=ArmorSlot.None&&item.armorSlot.ToString()!=EquipmentVisuals.Slot(item.runtimeId))throw new Exception("Equipment slot mapping "+item.displayName);
            }
            File.WriteAllText("Logs/ArmorArt/import.txt",report.ToString());
        }
        [MenuItem("Rivet Reach/Build armor and ingot art review")]
        public static void Build()
        {
            const string output="Builds/EquipmentArt";Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=output+"/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Logs/ArmorArt/build.txt",$"{report.summary.result}; errors {report.summary.totalErrors}; warnings {report.summary.totalWarnings}; seconds {report.summary.totalTime.TotalSeconds}");
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Equipment review build failed");
        }
    }
}
