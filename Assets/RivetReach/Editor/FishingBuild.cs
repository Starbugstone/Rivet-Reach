using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class FishingBuild
    {
        public static void Prepare()
        {
            var items=ItemRegistry.Load();
            void Item(byte id,string key,string name,int food,params string[] tags)
            {
                if(items.items.Any(i=>i.runtimeId==id))return;
                items.items=items.items.Append(new ItemDefinition{runtimeId=id,stableId="rivet:"+key,displayName=name,stackLimit=id==FishId.Rod?1:64,fistSeconds=.15f,foodPoints=food,tags=tags,colour=new Color(.35f,.6f,.57f)}).ToArray();
            }
            Item(FishId.Rod,"fishing_rod","Fishing Rod",0,"fishing_rod");
            Item(FishId.Raw,"raw_fish","Raw Fish",2,"edible","fish","raw_fish");
            Item(FishId.Cooked,"cooked_fish","Cooked Fish",6,"edible","fish","prepared_food");
            Item(FishId.Stew,"fish_stew","Fish Stew",12,"edible","prepared_food");
            items.InvalidateIndex();EditorUtility.SetDirty(items);
            const string path="Assets/RivetReach/Resources/Definitions/Recipes/FishingRod.asset";
            var recipe=AssetDatabase.LoadAssetAtPath<RecipeAsset>(path);
            if(recipe==null)
            {
                recipe=ScriptableObject.CreateInstance<RecipeAsset>();recipe.stableId="rivet:fishing_rod";recipe.kind=RecipeKind.Shaped;recipe.minimumGridSize=3;recipe.width=recipe.height=3;recipe.mirror=true;
                byte[] layout={0,0,BlockId.Stick,0,BlockId.Stick,FarmId.String,BlockId.Stick,0,FarmId.String};
                recipe.ingredients=layout.Select(id=>new RecipeCellData{itemId=id==0?"":items.Get(id).stableId,count=id==0?0:1}).ToArray();recipe.output=new RecipeCellData{itemId=items.Get(FishId.Rod).stableId,count=1};AssetDatabase.CreateAsset(recipe,path);
            }
            var catalog=RecipeCatalogAsset.Load();if(!catalog.recipes.Contains(recipe)){catalog.recipes=catalog.recipes.Append(recipe).ToArray();EditorUtility.SetDirty(catalog);}
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            const string folder="Assets/RivetReach/Resources/Fishing/";var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"Fishing.mat");
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,folder+"Fishing.mat");}
            material.SetTexture("_BaseMap",Resources.Load<Texture2D>("Fishing/Palette"));material.SetFloat("_Smoothness",.15f);EditorUtility.SetDirty(material);
            foreach(string modelPath in Directory.GetFiles(folder,"*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"FishingPalette"),material);importer.SaveAndReimport();
            }
            foreach(string imagePath in Directory.GetFiles(folder,"*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(imagePath);importer.isReadable=true;importer.alphaIsTransparency=!imagePath.EndsWith("Palette.png");importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();catalog.Compile(items);CookingCatalog.Current.Validate(items);
            Directory.CreateDirectory("Logs/Fishing");
            File.WriteAllLines("Logs/Fishing/imports.txt",Directory.GetFiles(folder,"*.fbx").Select(modelPath=>
            {
                var root=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);var filters=root.GetComponentsInChildren<MeshFilter>();
                var points=filters.SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);
                if(bounds.size.magnitude<.02f||bounds.size.magnitude>2)throw new Exception("Invalid fishing model bounds: "+modelPath);
                return Path.GetFileName(modelPath)+" | triangles="+filters.Sum(f=>f.sharedMesh.triangles.Length/3)+" | renderers="+root.GetComponentsInChildren<Renderer>().Length+" | bounds="+bounds;
            }));
        }
    }
}
