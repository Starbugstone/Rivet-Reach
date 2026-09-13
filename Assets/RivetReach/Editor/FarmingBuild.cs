using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class FarmingBuild
    {
        public static void Prepare()
        {
            var items=ItemRegistry.Load();var catalog=RecipeCatalogAsset.Load();
            void Item(byte id,string key,string name,int food=0,params string[] tags)
            {
                var item=items.items.FirstOrDefault(i=>i.runtimeId==id);
                if(item==null){item=new ItemDefinition{runtimeId=id,stableId="rivet:"+key,displayName=name,colour=new Color(.65f,.7f,.3f),fistSeconds=.15f};items.items=items.items.Append(item).ToArray();}
                item.foodPoints=food;item.tags=tags;
            }
            Item(220,"wheat_seed","Wheat Seeds",0,"seed");Item(221,"grain","Grain",0,"grain");
            Item(222,"flax_seed","Flax Seeds",0,"seed");Item(223,"flax_fibre","Flax Fibre",0,"fibre");
            Item(224,"string","String",0,"cordage");Item(225,"cloth","Cloth",0,"fabric");
            Item(226,"carrot_seed","Carrot Seeds",0,"seed");Item(227,"carrot","Carrot",2,"vegetable");
            Item(228,"berry_seed","Berry Seeds",0,"seed");Item(229,"berries","Berries",1,"fruit");
            Item(230,"mushroom","Mushroom",1,"mushroom");Item(231,"bread","Bread",7,"prepared_food");
            Item(232,"roast_carrot","Roasted Carrot",5,"prepared_food");Item(233,"vegetable_stew","Vegetable Stew",12,"prepared_food");
            Item(234,"cooked_mushroom","Cooked Mushrooms",4,"prepared_food");Item(235,"fruit_porridge","Fruit Porridge",9,"prepared_food");
            foreach(var crop in CropRules.Definitions.Where(c=>c.first>=200))for(byte stage=0;stage<crop.stages;stage++)
                Item((byte)(crop.first+stage),crop.key+"_plant_"+stage,crop.key.Substring(0,1).ToUpperInvariant()+crop.key.Substring(1)+(crop.stages==1?" Cluster":stage==crop.stages-1?" · Mature":" · Stage "+(stage+1)));
            Item(FarmId.Cooker,"cooker","Cooker");Item(FarmId.ElectricCooker,"electric_cooker","Electric Cooker");
            foreach(var fuel in ProcessingCatalogAsset.Load().fuels)
            {
                var item=items.items.Single(i=>i.stableId==fuel.itemId);
                item.tags=(item.tags??Array.Empty<string>()).Append("burnable").Distinct().OrderBy(t=>t).ToArray();
            }
            foreach(var pair in new[]{(BlockId.Potato,"vegetable"),(BlockId.Apple,"fruit"),(BlockId.Coal,"boiler_fuel"),(BlockId.Charcoal,"boiler_fuel")})
            {var item=items.items.Single(i=>i.runtimeId==pair.Item1);item.tags=(item.tags??Array.Empty<string>()).Append(pair.Item2).Distinct().OrderBy(t=>t).ToArray();}
            foreach(var item in items.items.Where(i=>i.foodPoints>0))item.tags=(item.tags??Array.Empty<string>()).Append("edible").Distinct().OrderBy(t=>t).ToArray();
            items.InvalidateIndex();EditorUtility.SetDirty(items);
            RecipeCellData Cell(byte id,int count=1)=>new RecipeCellData{itemId=items.Get(id).stableId,count=count};
            void Recipe(byte output,int size,params (byte id,int count)[] input)
            {
                string path="Assets/RivetReach/Resources/Definitions/Recipes/Farm"+output+".asset";
                var recipe=AssetDatabase.LoadAssetAtPath<RecipeAsset>(path);
                if(recipe==null){recipe=ScriptableObject.CreateInstance<RecipeAsset>();recipe.stableId="rivet:farm_"+output;recipe.kind=RecipeKind.Shapeless;recipe.minimumGridSize=size;recipe.width=recipe.height=1;recipe.ingredients=input.Select(i=>Cell(i.id,i.count)).ToArray();recipe.output=Cell(output);AssetDatabase.CreateAsset(recipe,path);}
                if(!catalog.recipes.Contains(recipe)){catalog.recipes=catalog.recipes.Append(recipe).ToArray();EditorUtility.SetDirty(catalog);}
            }
            Recipe(FarmId.String,2,(FarmId.Fibre,3));Recipe(FarmId.Cloth,2,(FarmId.String,4));
            Recipe(FarmId.Cooker,3,(BlockId.Furnace,1),(BlockId.CopperIngot,2));
            Recipe(FarmId.ElectricCooker,4,(FarmId.Cooker,1),(IndustryId.Casing,1),(IndustryId.CopperWire,4));
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            PrepareArt();TerrainTiles.Build();AssetDatabase.SaveAssets();FarmingMeshes.Initialize();CookingCatalog.Current.Validate(items);catalog.Compile(items);ProcessingCatalogAsset.Load().Compile(items);
        }
        public static void ReviewImports()
        {
            var lines=new System.Collections.Generic.List<string>();
            foreach(string path in Directory.GetFiles("Assets/RivetReach/Resources/Farming/","*.fbx").Concat(new[]{"Assets/RivetReach/Resources/Industry/Runtime/cooker.prefab","Assets/RivetReach/Resources/Industry/Runtime/electric_cooker.prefab"}))
            {
                var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var points=root.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToArray();
                if(points.Length==0)throw new Exception("Empty farming model: "+path);
                var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);
                int triangles=root.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);
                if(bounds.size.x>1.05f||bounds.size.y>1.05f||bounds.size.z>1.05f||bounds.size.magnitude<.02f)throw new Exception("Invalid farming model bounds: "+path+" "+bounds);
                lines.Add(Path.GetFileName(path)+" | triangles="+triangles+" | bounds="+bounds+" | renderers="+root.GetComponentsInChildren<Renderer>().Length);
            }
            Directory.CreateDirectory("Logs/Farming");File.WriteAllLines("Logs/Farming/imports.txt",lines);
        }
        static void PrepareArt()
        {
            const string folder="Assets/RivetReach/Resources/Farming/";
            var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"Farm.mat");
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,folder+"Farm.mat");}
            material.SetTexture("_BaseMap",Resources.Load<Texture2D>("Farming/Palette"));material.SetFloat("_Smoothness",.12f);EditorUtility.SetDirty(material);
            foreach(string path in Directory.GetFiles(folder,"*.fbx"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"FarmPalette"),material);importer.SaveAndReimport();
            }
            foreach(string path in Directory.GetFiles(folder,"*.png"))
            {var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.alphaIsTransparency=!path.EndsWith("Palette.png");importer.mipmapEnabled=path.EndsWith("Palette.png");importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
            foreach(string key in new[]{"cooker","electric_cooker"})
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/"+key+".fbx");importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"WorkshopAtlas"),Resources.Load<Material>("Industry/Workshop"));importer.SaveAndReimport();
            }
            IndustryAssets.NormalizeModels(Resources.Load<Material>("Industry/Workshop"),new[]{"cooker","electric_cooker"});AssetDatabase.SaveAssets();
        }
    }
}
