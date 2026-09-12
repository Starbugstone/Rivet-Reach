using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class OrchardBuild
    {
        [MenuItem("Rivet Reach/Build saplings and apples review")]
        public static void Run()
        {
            Directory.CreateDirectory("Logs/Orchard");AssetDatabase.Refresh();
            const string folder="Assets/RivetReach/Resources/Orchard/";
            foreach(string name in new[]{"Palette","AppleIcon"})
            {
                var texture=(TextureImporter)AssetImporter.GetAtPath(folder+name+".png");
                texture.alphaIsTransparency=name!="Palette";texture.mipmapEnabled=name=="Palette";
                texture.filterMode=name=="Palette"?FilterMode.Point:FilterMode.Bilinear;
                texture.textureCompression=TextureImporterCompression.Uncompressed;texture.SaveAndReimport();
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"Apple.mat");
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,folder+"Apple.mat");}
            material.SetTexture("_BaseMap",FoodVisuals.PaletteFor(BlockId.Apple));material.SetFloat("_Smoothness",.25f);material.SetFloat("_Metallic",0);
            EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
            var importer=(ModelImporter)AssetImporter.GetAtPath(folder+"Apple.fbx");
            importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"ApplePalette"),material);importer.SaveAndReimport();
            var model=Resources.Load<GameObject>("Orchard/Apple");var mesh=model.GetComponentInChildren<MeshFilter>().sharedMesh;
            Check(mesh.triangles.Length/3==782&&mesh.normals.Length==mesh.vertexCount&&mesh.uv.Length==mesh.vertexCount,"Apple geometry, UVs and normals import");
            Check(model.GetComponentsInChildren<Renderer>().Length==1&&model.GetComponentInChildren<Renderer>().sharedMaterial==material,"Apple uses one mesh and material");
            var instance=UnityEngine.Object.Instantiate(model);var bounds=instance.GetComponentInChildren<Renderer>().bounds;
            Check(bounds.size.x>.8f&&bounds.size.x<1&&bounds.size.y>.8f&&bounds.size.y<1&&bounds.size.z<1,"Apple import scale and upright axis");UnityEngine.Object.DestroyImmediate(instance);
            Check(!mesh.uv.Any(uv=>{int region=Mathf.FloorToInt(uv.x*4)+Mathf.FloorToInt(uv.y*4)*4;return region==12||region==15;}),"Apple avoids metallic palette regions");
            int saplings=0,apples=0,both=0;
            for(int i=0;i<100000;i++)
            {var p=new BlockPos(i%127-63,64,i/127-200);bool s=LeafHarvest.Sapling(p,246813,i),a=LeafHarvest.Apple(p,246813,i);if(s)saplings++;if(a)apples++;if(s&&a)both++;}
            Check(saplings>4700&&saplings<5300&&apples>1800&&apples<2200&&both>50,"Independent occasional sapling and apple rolls");
            var items=ItemRegistry.Load();Check(items.Get(BlockId.Apple).foodPoints==4&&items.Get(BlockId.Sapling).stackLimit==64,"Authored item definitions");
            var inventory=new Inventory(id=>items.Get(id).stackLimit);inventory.Add(BlockId.Apple,2);var hunger=new HungerState();
            Check(!hunger.TryEat(inventory,0,4)&&inventory.Total(BlockId.Apple)==2,"Full hunger preserves apples");hunger.Exert(16);
            Check(hunger.TryEat(inventory,0,4)&&hunger.Food==20&&inventory.Total(BlockId.Apple)==1,"One apple restores four food points");
            var cells=new byte[34*34*34];cells[ChunkMesher.Index(0,0,0)]=BlockId.Sapling;var plant=ChunkMesher.Build(default,0,cells);
            Check(!BlockId.Solid(BlockId.Sapling)&&BlockId.Mineable(BlockId.Sapling,ToolCapability.None)&&plant.Triangles.Length>0&&plant.Tiles.Any(t=>t.x==4)&&plant.Tiles.Any(t=>t.x==6),"Sapling is mineable, pass-through, with wood and leaf surfaces");
            File.WriteAllText("Logs/Orchard/checks.txt",$"PASS: apple import 782 triangles, one mesh/material, bounds {bounds}; 100000 leaf rolls: {saplings} saplings, {apples} apples, {both} both; food consumption and sapling meshing.\n");
            DomainChecks.Run();FluidChecks.Run();WikiExport.Export();
            const string output="Builds/Orchard";Directory.CreateDirectory(output);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=output+"/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Logs/Orchard/build.txt",$"{report.summary.result}; errors {report.summary.totalErrors}; warnings {report.summary.totalWarnings}; seconds {report.summary.totalTime.TotalSeconds}");
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Orchard review build failed");
        }
        static void Check(bool value,string message){if(!value)throw new Exception("Orchard check failed: "+message);}
    }
}
