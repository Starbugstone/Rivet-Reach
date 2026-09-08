using System;
using UnityEngine;

namespace RivetReach
{
    [Serializable]
    public struct RecipeCellData
    {
        public string itemId;
        public int count;
        public RecipeIngredient ToIngredient() => new RecipeIngredient(itemId, count);
    }

    [CreateAssetMenu(menuName = "Rivet Reach/Crafting/Recipe")]
    public sealed class RecipeAsset : ScriptableObject
    {
        public string stableId;
        public RecipeKind kind;
        [Range(2, 4)] public int minimumGridSize = 2;
        [Range(1, 4)] public int width = 1, height = 1;
        public bool mirror;
        public RecipeCellData[] ingredients = new RecipeCellData[1];
        public RecipeCellData output;

        public RecipeSpec ToSpec()
        {
            if (ingredients == null) throw new ArgumentException(name + " has no ingredient array.");
            var cells = new RecipeIngredient[ingredients.Length];
            for (int i = 0; i < cells.Length; i++) cells[i] = ingredients[i].ToIngredient();
            return new RecipeSpec
            {
                Id = stableId, Kind = kind, MinimumGridSize = minimumGridSize,
                Width = width, Height = height, Mirror = mirror, Ingredients = cells,
                Output = output.ToIngredient()
            };
        }
    }
}
