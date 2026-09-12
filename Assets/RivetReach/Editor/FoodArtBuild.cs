using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class FoodArtBuild
    {
        [MenuItem("Rivet Reach/Prepare potato art")]
        public static void Prepare()
        {
            AssetDatabase.Refresh();
            const string folder="Assets/RivetReach/Resources/Food/";
            foreach(string name in new[]{"Palette","PotatoIcon","BakedPotatoIcon"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(folder+name+".png");
                importer.alphaIsTransparency=name!="Palette";importer.mipmapEnabled=name=="Palette";
                importer.filterMode=name=="Palette"?FilterMode.Point:FilterMode.Bilinear;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"Potato.mat");
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,folder+"Potato.mat");}
            material.SetTexture("_BaseMap",FoodVisuals.Palette);material.SetFloat("_Smoothness",.12f);material.SetFloat("_Metallic",0);
            EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Logs/PotatoArt");var report=new System.Text.StringBuilder();
            foreach(string key in new[]{"Potato","BakedPotato"})
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(folder+key+".fbx");
                importer.importAnimation=false;importer.isReadable=true;
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"PotatoPalette"),material);importer.SaveAndReimport();
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(folder+key+".fbx");
                var filters=model.GetComponentsInChildren<MeshFilter>();
                if(filters.Length!=1)throw new Exception(key+": expected one mesh");
                var mesh=filters[0].sharedMesh;var bounds=new Bounds();bool first=true;
                foreach(var vertex in mesh.vertices)
                {var p=filters[0].transform.TransformPoint(vertex);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
                if(mesh.subMeshCount!=1||mesh.normals.Length!=mesh.vertexCount||mesh.uv.Length!=mesh.vertexCount||mesh.triangles.Length<300)
                    throw new Exception(key+": missing geometry, normals, UVs or single-material contract");
                if(mesh.uv.Any(uv=>{int region=Mathf.FloorToInt(uv.x*4)+Mathf.FloorToInt(uv.y*4)*4;return region==12||region==15;}))
                    throw new Exception(key+": food samples a metallic held-tool palette region");
                if(bounds.size.x<.9f||bounds.size.x>1||bounds.size.y<.4f||bounds.size.y>.65f||bounds.size.z>.65f)
                    throw new Exception(key+": wrong scale/axis import "+bounds);
                if(model.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterial!=material))throw new Exception(key+": palette remap failed");
                report.AppendLine($"{key}: {mesh.triangles.Length/3} triangles; {mesh.vertexCount} vertices; one mesh/material; bounds {bounds}");
            }
            report.AppendLine("PASS: imported geometry, scale, axes, UVs, normals and material assignment");
            File.WriteAllText("Logs/PotatoArt/import-checks.txt",report.ToString());
        }
        public static void Run()
        {
            Prepare();
            const string output="Builds/PotatoArt";Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=output+"/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Logs/PotatoArt/build-summary.txt",$"{report.summary.result}; errors {report.summary.totalErrors}; warnings {report.summary.totalWarnings}; seconds {report.summary.totalTime.TotalSeconds}");
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Potato art review build failed");
        }
    }
}
