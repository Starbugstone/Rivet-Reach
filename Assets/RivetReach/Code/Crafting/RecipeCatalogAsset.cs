using System;
using UnityEngine;

namespace RivetReach
{
    // Composition root / factory. An explicit asset list controls which recipes ship.
    // A JSON/mod adapter can supply RecipeSpec to the same compiler later.
    [CreateAssetMenu(menuName = "Rivet Reach/Crafting/Recipe catalog")]
    public sealed class RecipeCatalogAsset : ScriptableObject
    {
        public RecipeAsset[] recipes = Array.Empty<RecipeAsset>();
        public RecipeRegistry Compile(ItemRegistry items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (recipes == null) throw new ArgumentException("Recipe catalog has no recipe list.");
            var specs = new RecipeSpec[recipes.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                if (recipes[i] == null) throw new ArgumentException("Missing recipe asset at catalog index " + i);
                specs[i] = recipes[i].ToSpec();
            }
            return RecipeRegistry.Compile(specs, items.ResolveId, id => items.Get(id).stackLimit);
        }
        public static RecipeCatalogAsset Load()
        {
            var catalog = Resources.Load<RecipeCatalogAsset>("Definitions/Recipes");
            if (catalog == null) throw new InvalidOperationException("Missing Definitions/Recipes catalog.");
            return catalog;
        }
    }
}
