using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class DoorBuild
    {
        [MenuItem("Rivet Reach/Build doors review")]
        public static void Run()
        {
            Directory.CreateDirectory("Logs");AssetDatabase.Refresh();Prepare();DoorChecks.Run();
            DomainChecks.Run();IndustryChecks.Run();BatteryChecks.Run();MultiblockChecks.Run();
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName="Builds/Doors/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Logs/door-build.txt",result.summary.result+"; errors "+result.summary.totalErrors+"; warnings "+result.summary.totalWarnings+"; seconds "+result.summary.totalTime.TotalSeconds);
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Door build failed");
            File.Copy("LICENSE.md","Builds/Doors/LICENSE.md",true);
            File.Copy(".docs/THIRD_PARTY_NOTICES.md","Builds/Doors/THIRD_PARTY_NOTICES.md",true);
            foreach(string source in Directory.GetFiles(".docs/licenses","*",SearchOption.AllDirectories))
            {string target=Path.Combine("Builds/Doors/licenses",Path.GetRelativePath(".docs/licenses",source));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(source,target,true);}
        }
        public static void Prepare()
        {
            var items=ItemRegistry.Load();
            if(!items.items.Any(i=>i.runtimeId==IndustryId.WoodenDoor))
            {items.items=items.items.Append(new ItemDefinition{runtimeId=IndustryId.WoodenDoor,stableId="rivet:wooden_door",displayName="Wooden Door",colour=new Color(.58f,.34f,.15f),fistSeconds=1.5f}).ToArray();EditorUtility.SetDirty(items);items.InvalidateIndex();}
            var catalog=RecipeCatalogAsset.Load();const string path="Assets/RivetReach/Resources/Definitions/Recipes/WoodenDoor.asset";
            var recipe=AssetDatabase.LoadAssetAtPath<RecipeAsset>(path);
            if(recipe==null)
            {
                recipe=ScriptableObject.CreateInstance<RecipeAsset>();recipe.stableId="rivet:wooden_door";recipe.kind=RecipeKind.Shaped;recipe.minimumGridSize=3;recipe.width=2;recipe.height=3;
                recipe.ingredients=Enumerable.Range(0,6).Select(_=>new RecipeCellData{itemId="rivet:planks",count=1}).ToArray();recipe.output=new RecipeCellData{itemId="rivet:wooden_door",count=3};AssetDatabase.CreateAsset(recipe,path);
            }
            if(!catalog.recipes.Contains(recipe)){catalog.recipes=catalog.recipes.Append(recipe).ToArray();EditorUtility.SetDirty(catalog);}
            var material=Resources.Load<Material>("Industry/Workshop");
            var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/wooden_door.fbx");
            importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"WorkshopAtlas"),material);importer.SaveAndReimport();
            var icon=(TextureImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/Icons/171.png");icon.alphaIsTransparency=true;icon.mipmapEnabled=false;icon.SaveAndReimport();
            IndustryAssets.NormalizeModels(material,new[]{"wooden_door"});AssetDatabase.SaveAssets();catalog.Compile(items);
            var prefab=Resources.Load<GameObject>("Industry/Runtime/wooden_door");int triangles=0;var bounds=new Bounds();bool first=true;
            foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;triangles+=mesh.triangles.Length/3;
                if(mesh.uv.Length!=mesh.vertexCount||mesh.normals.Length!=mesh.vertexCount||filter.GetComponent<Renderer>().sharedMaterial!=material)throw new Exception("Invalid door UV/normals/material");
                foreach(var v in mesh.vertices){var point=filter.transform.TransformPoint(v);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
            }
            if(triangles<=0||prefab.GetComponentsInChildren<Renderer>().Length!=2||!prefab.GetComponentsInChildren<Transform>().Any(t=>t.name=="MotionDoor"))throw new Exception("Invalid door import/pivot");
            if(bounds.min.x<-.001f||bounds.min.y<-.001f||bounds.min.z<-.001f||bounds.max.x>1.001f||bounds.max.y>2.001f||bounds.max.z>1.001f)throw new Exception("Door escapes footprint: "+bounds);
            var leaf=prefab.GetComponentsInChildren<Transform>().Single(t=>t.name=="MotionDoor");
            foreach(var v in leaf.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                var point=leaf.localPosition+Quaternion.Euler(0,-90,0)*v;
                if(point.x<-.001f||point.z<-.001f||point.x>1.001f||point.z>1.001f)throw new Exception("Open door escapes footprint: "+point.ToString("F4"));
            }
            File.WriteAllText("Logs/door-import.txt",$"{triangles} triangles; 2 renderers; 1 shared material; bounds {bounds.min} to {bounds.max}; UVs, normals and hinge pivot present.\n");
        }
    }
}
