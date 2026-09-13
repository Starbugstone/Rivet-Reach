using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    [Serializable] public sealed class FoodIngredient { public string selector;public int count=1; }
    [Serializable] public sealed class CookingRecipe
    {
        public string id,name;
        public FoodIngredient[] ingredients;
        public byte output;
        public int count=1,ticks=200;
        [NonSerialized] internal IngredientMatcher Matcher;
    }
    [Serializable] public sealed class CookingCatalog
    {

        public CookingRecipe[] recipes;
        public int electricWatts=200;
        static CookingCatalog cached;
        public static CookingCatalog Current=>cached??=Load();
        static CookingCatalog Load()
        {
            var asset=Resources.Load<TextAsset>("Definitions/Cooking");
            if(asset==null)throw new InvalidOperationException("Missing food cooker catalog.");
            var result=JsonUtility.FromJson<CookingCatalog>(asset.text)??throw new ArgumentException("Invalid food catalog.");result.Validate(ItemRegistry.Load());return result;
        }
        ItemRegistry items;
        Dictionary<string,CookingRecipe> byId;
        public void Validate(ItemRegistry registry)
        {
            if(registry==null)throw new ArgumentNullException(nameof(registry));
            if(electricWatts<1||electricWatts>10000||recipes==null||recipes.Length==0||recipes.Length>32)throw new ArgumentException("Invalid cooker configuration.");
            var compiled=new Dictionary<string,CookingRecipe>(StringComparer.Ordinal);
            var matchers=new IngredientMatcher[recipes.Length];
            for(int i=0;i<recipes.Length;i++)
            {
                var recipe=recipes[i];
                if(recipe==null||string.IsNullOrWhiteSpace(recipe.id)||string.IsNullOrWhiteSpace(recipe.name)||!compiled.TryAdd(recipe.id,recipe)||recipe.ticks<1||recipe.ticks>12000||recipe.count<1||recipe.count>registry.Get(recipe.output).stackLimit||registry.FoodPoints(recipe.output)==0||recipe.ingredients==null||recipe.ingredients.Length==0||recipe.ingredients.Length>3||recipe.ingredients.Any(input=>input==null))throw new ArgumentException("Invalid food recipe.");
                matchers[i]=new IngredientMatcher(recipe.ingredients.Select(input=>new RecipeIngredient(input.selector,input.count)),registry);
            }
            // Failed compilation cannot publish a partly compiled catalog.
            for(int i=0;i<recipes.Length;i++)recipes[i].Matcher=matchers[i];
            items=registry;byId=compiled;
        }
        public IReadOnlyList<byte> Choices(string selector)=>items.Select(selector).Choices;
        public CookingRecipe Find(string id)=>id!=null&&byId.TryGetValue(id,out var recipe)?recipe:null;
        public int[] Plan(CookingRecipe recipe,IReadOnlyList<ItemStack> slots)
        {var used=new int[3];return recipe.Matcher.TryPlan(slots,used)?used:null;}
    }
}
