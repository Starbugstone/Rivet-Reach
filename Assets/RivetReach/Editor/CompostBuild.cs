using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class CompostBuild
    {
        public static void Prepare()
        {
            var items=ItemRegistry.Load();var catalog=RecipeCatalogAsset.Load();
            void Add(byte id,string key,string name)
            {if(!items.items.Any(i=>i.runtimeId==id))items.items=items.items.Append(new ItemDefinition{runtimeId=id,stableId="rivet:"+key,displayName=name,colour=new Color(.36f,.22f,.10f),fistSeconds=id==CompostId.Bin?1.4f:.15f}).ToArray();}
            Add(CompostId.Bin,"compost_bin","Compost Bin");Add(CompostId.Compost,"compost","Compost");Add(CompostId.Auto,"auto_composter","Autocomposter");
            foreach(var item in items.items.Where(i=>CompostId.OriginalInput(i.runtimeId)))item.tags=item.tags.Append("compostable").Distinct().OrderBy(t=>t).ToArray();
            items.InvalidateIndex();EditorUtility.SetDirty(items);
            const string path="Assets/RivetReach/Resources/Definitions/Recipes/CompostBin.asset";
            var recipe=AssetDatabase.LoadAssetAtPath<RecipeAsset>(path);
            if(recipe==null)
            {
                recipe=ScriptableObject.CreateInstance<RecipeAsset>();recipe.stableId="rivet:compost_bin";recipe.kind=RecipeKind.Shaped;recipe.minimumGridSize=3;recipe.width=recipe.height=3;
                recipe.ingredients=new[]{1,0,1,1,0,1,1,1,1}.Select(n=>new RecipeCellData{itemId=n==1?items.Get(BlockId.Planks).stableId:"",count=n}).ToArray();recipe.output=new RecipeCellData{itemId=items.Get(CompostId.Bin).stableId,count=1};AssetDatabase.CreateAsset(recipe,path);
            }
            if(!catalog.recipes.Contains(recipe)){catalog.recipes=catalog.recipes.Append(recipe).ToArray();EditorUtility.SetDirty(catalog);}
            const string autoPath="Assets/RivetReach/Resources/Definitions/Recipes/AutoComposter.asset";
            var auto=AssetDatabase.LoadAssetAtPath<RecipeAsset>(autoPath);
            if(auto==null)
            {
                auto=ScriptableObject.CreateInstance<RecipeAsset>();auto.stableId="rivet:auto_composter";auto.kind=RecipeKind.Shapeless;auto.minimumGridSize=4;auto.width=auto.height=1;
                auto.ingredients=new[]{(CompostId.Bin,1),(IndustryId.Casing,1),(IndustryId.Cog,2),(IndustryId.ItemPipe,2),(BlockId.GoldIngot,1)}.Select(v=>new RecipeCellData{itemId=items.Get(v.Item1).stableId,count=v.Item2}).ToArray();
                auto.output=new RecipeCellData{itemId=items.Get(CompostId.Auto).stableId,count=1};AssetDatabase.CreateAsset(auto,autoPath);
            }
            if(!catalog.recipes.Contains(auto)){catalog.recipes=catalog.recipes.Append(auto).ToArray();EditorUtility.SetDirty(catalog);}
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            foreach(string key in new[]{"compost_bin","compost","auto_composter"})
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/"+key+".fbx");
                importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"WorkshopAtlas"),Resources.Load<Material>("Industry/Workshop"));importer.SaveAndReimport();
            }
            foreach(byte id in new[]{CompostId.Bin,CompostId.Compost,CompostId.Auto})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/Icons/"+id+".png");importer.isReadable=true;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            IndustryAssets.NormalizeModels(Resources.Load<Material>("Industry/Workshop"),new[]{"compost_bin","compost","auto_composter"});AssetDatabase.SaveAssets();
            CompostCatalog.Current.Validate(items);catalog.Compile(items);
            Directory.CreateDirectory("Logs/Compost");
            File.WriteAllLines("Logs/Compost/imports.txt",new[]{"compost_bin","compost","auto_composter"}.Select(key=>
            {
                var root=Resources.Load<GameObject>("Industry/Runtime/"+key);var filters=root.GetComponentsInChildren<MeshFilter>();var points=filters.SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToArray();
                var bounds=new Bounds(points[0],Vector3.zero);foreach(var v in points)bounds.Encapsulate(v);
                if(bounds.min.x<-.01f||bounds.min.y<-.01f||bounds.min.z<-.01f||bounds.max.x>1.01f||bounds.max.y>1.01f||bounds.max.z>1.01f)throw new Exception("Compost bounds: "+bounds);
                return key+" | triangles="+filters.Sum(f=>f.sharedMesh.triangles.Length/3)+" | renderers="+root.GetComponentsInChildren<Renderer>().Length+" | bounds="+bounds;
            }));
        }
    }
}
