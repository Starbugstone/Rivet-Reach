using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class FluidAssets
    {
        public static void Prepare(ItemRegistry items)
        {
            foreach(var item in new[]{
                new ItemDefinition{runtimeId=Fluids.EmptyBucket,stableId="rivet:bucket",displayName="Bucket",stackLimit=1,colour=new Color(.66f,.72f,.76f)},
                new ItemDefinition{runtimeId=Fluids.WaterBucket,stableId="rivet:water_bucket",displayName="Water bucket",stackLimit=1,colour=new Color(.12f,.56f,.77f)}})
                if(!items.items.Any(i=>i.runtimeId==item.runtimeId))items.items=items.items.Append(item).ToArray();
            var catalog=RecipeCatalogAsset.Load();
            const string path="Assets/RivetReach/Resources/Definitions/BucketRecipe.asset";
            var recipe=AssetDatabase.LoadAssetAtPath<RecipeAsset>(path);
            if(recipe==null)
            {
                recipe=ScriptableObject.CreateInstance<RecipeAsset>();recipe.stableId="rivet:bucket";recipe.minimumGridSize=3;recipe.width=3;recipe.height=2;
                recipe.ingredients=new[]{new RecipeCellData{itemId="rivet:iron_ingot",count=1},default,new RecipeCellData{itemId="rivet:iron_ingot",count=1},default,new RecipeCellData{itemId="rivet:iron_ingot",count=1},default};
                recipe.output=new RecipeCellData{itemId="rivet:bucket",count=1};AssetDatabase.CreateAsset(recipe,path);
            }
            if(!catalog.recipes.Contains(recipe)){catalog.recipes=catalog.recipes.Append(recipe).ToArray();EditorUtility.SetDirty(catalog);}
        }
    }
}
