using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class HandCrankBuild
    {
        [MenuItem("Rivet Reach/Build hand crank review")]
        public static void Run()
        {
            Directory.CreateDirectory("Logs");AssetDatabase.Refresh();Prepare();
            HandCrankChecks.Run();BatteryChecks.Run();IndustryChecks.Run();MultiblockChecks.Run();DomainChecks.Run();
            WikiExport.Export();
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName="Builds/HandCrank/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Logs/hand-crank-build.txt",result.summary.result+"; errors "+result.summary.totalErrors+"; warnings "+result.summary.totalWarnings+"; seconds "+result.summary.totalTime.TotalSeconds);
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Hand crank build failed");
            File.Copy("LICENSE.md","Builds/HandCrank/LICENSE.md",true);
            File.Copy(".docs/THIRD_PARTY_NOTICES.md","Builds/HandCrank/THIRD_PARTY_NOTICES.md",true);
            foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories))
            {string target=Path.Combine("Builds/HandCrank/licenses",Path.GetRelativePath(".docs/licenses",source));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);}
        }
        static void Prepare()
        {
            var items=ItemRegistry.Load();
            if(!items.items.Any(i=>i.runtimeId==IndustryId.HandCrank))
            {items.items=items.items.Append(new ItemDefinition{runtimeId=IndustryId.HandCrank,stableId="rivet:hand_crank",displayName="Hand Crank",colour=new Color(.75f,.44f,.22f),fistSeconds=1.2f}).ToArray();EditorUtility.SetDirty(items);items.InvalidateIndex();}
            var catalog=RecipeCatalogAsset.Load();const string path="Assets/RivetReach/Resources/Definitions/Recipes/Industry170.asset";
            var recipe=AssetDatabase.LoadAssetAtPath<RecipeAsset>(path);
            RecipeCellData Cell(byte id,int count)=>new RecipeCellData{itemId=items.Get(id).stableId,count=count};
            if(recipe==null)
            {recipe=ScriptableObject.CreateInstance<RecipeAsset>();recipe.stableId="rivet:industry_170";recipe.kind=RecipeKind.Shapeless;recipe.minimumGridSize=3;recipe.width=recipe.height=1;recipe.ingredients=new[]{Cell(BlockId.CopperIngot,2),Cell(BlockId.IronIngot,1),Cell(BlockId.Stick,1),Cell(BlockId.Planks,2)};recipe.output=Cell(IndustryId.HandCrank,1);AssetDatabase.CreateAsset(recipe,path);}
            if(!catalog.recipes.Contains(recipe)){catalog.recipes=catalog.recipes.Append(recipe).ToArray();EditorUtility.SetDirty(catalog);}
            var material=Resources.Load<Material>("Industry/Workshop");
            var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/hand_crank.fbx");
            importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"WorkshopAtlas"),material);importer.SaveAndReimport();
            var icon=(TextureImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/Icons/170.png");icon.alphaIsTransparency=true;icon.mipmapEnabled=false;icon.SaveAndReimport();
            IndustryAssets.NormalizeModels(material,new[]{"hand_crank"});AssetDatabase.SaveAssets();catalog.Compile(items);
            var prefab=Resources.Load<GameObject>("Industry/Runtime/hand_crank");int triangles=0;var bounds=new Bounds();bool first=true;
            foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;triangles+=mesh.triangles.Length/3;
                if(mesh.uv.Length!=mesh.vertexCount||mesh.normals.Length!=mesh.vertexCount||filter.GetComponent<Renderer>().sharedMaterial!=material)throw new Exception("Invalid crank UV/normals/material");
                foreach(var v in mesh.vertices){var point=filter.transform.TransformPoint(v);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
            }
            if(triangles<=0||prefab.GetComponentsInChildren<Renderer>().Length!=2||!prefab.GetComponentsInChildren<Transform>().Any(t=>t.name=="MotionSpinCrank"))throw new Exception("Invalid crank import/pivot");
            if(bounds.min.x<-.001f||bounds.min.y<-.001f||bounds.min.z<-.001f||bounds.max.x>1.001f||bounds.max.y>1.001f||bounds.max.z>1.001f)throw new Exception("Crank escapes one-cell bounds: "+bounds);
            File.WriteAllText("Logs/hand-crank-import.txt",$"{triangles} triangles; 2 renderers; 1 shared material; bounds {bounds.min} to {bounds.max}; UVs, normals and moving pivot present.\n");
        }
    }
}
