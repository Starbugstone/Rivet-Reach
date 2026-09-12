using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class WrenchAssets
    {
        public static void Prepare()
        {
            var registry=ItemRegistry.Load();
            if(!registry.items.Any(i=>i.runtimeId==IndustryId.Wrench))
            {
                registry.items=registry.items.Append(new ItemDefinition{runtimeId=IndustryId.Wrench,stableId="rivet:wrench",displayName="Wrench",stackLimit=1,colour=new Color(.65f,.72f,.74f)}).ToArray();
                registry.InvalidateIndex();EditorUtility.SetDirty(registry);
            }
            const string path="Assets/RivetReach/Resources/Definitions/Recipes/Wrench.asset";
            var recipe=AssetDatabase.LoadAssetAtPath<RecipeAsset>(path);
            if(recipe==null)
            {
                RecipeCellData Iron()=>new RecipeCellData{itemId="rivet:iron_ingot",count=1};
                recipe=ScriptableObject.CreateInstance<RecipeAsset>();recipe.stableId="rivet:wrench";recipe.kind=RecipeKind.Shaped;
                recipe.minimumGridSize=3;recipe.width=recipe.height=2;recipe.mirror=true;
                recipe.ingredients=new[]{Iron(),Iron(),new RecipeCellData(),Iron()};recipe.output=new RecipeCellData{itemId="rivet:wrench",count=1};
                AssetDatabase.CreateAsset(recipe,path);
            }
            var catalog=RecipeCatalogAsset.Load();
            if(!catalog.recipes.Contains(recipe)){catalog.recipes=catalog.recipes.Append(recipe).ToArray();EditorUtility.SetDirty(catalog);}
            const string modelPath="Assets/RivetReach/Resources/Tools/Wrench.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
            if(importer==null)throw new Exception("Generate the Blender wrench before preparing its import");
            importer.importAnimation=false;importer.isReadable=true;importer.useFileScale=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"WorkshopAtlas"),Resources.Load<Material>("Industry/Workshop"));importer.SaveAndReimport();
            var icon=(TextureImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/Icons/173.png");icon.alphaIsTransparency=true;icon.mipmapEnabled=false;icon.textureCompression=TextureImporterCompression.Uncompressed;icon.SaveAndReimport();
            AssetDatabase.SaveAssets();catalog.Compile(registry);
            var model=Resources.Load<GameObject>("Tools/Wrench");var filters=model.GetComponentsInChildren<MeshFilter>();
            int triangles=filters.Sum(f=>f.sharedMesh.triangles.Length/3);
            if(triangles==0||filters.Any(f=>f.sharedMesh.uv.Length!=f.sharedMesh.vertexCount||f.sharedMesh.normals.Length!=f.sharedMesh.vertexCount))throw new Exception("Invalid wrench geometry");
            var bounds=new Bounds();bool first=true;
            foreach(var f in filters)foreach(var v in f.sharedMesh.vertices)
            {var point=f.transform.TransformPoint(v);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);}
            if(bounds.size.y<.5f||bounds.size.y>.6f||bounds.size.x>.25f||bounds.size.z>.06f)throw new Exception("Wrench import does not retain its metric grip geometry: "+bounds);
            Directory.CreateDirectory("Logs/Connections");File.WriteAllText("Logs/Connections/wrench-import.txt",$"{triangles} triangles; {model.GetComponentsInChildren<Renderer>().Length} renderers; shared workshop material; bounds {bounds}; UVs and normals present.\n");
        }
    }
}
