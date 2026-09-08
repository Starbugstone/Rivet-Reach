using System;
using System.Collections.Generic;

namespace RivetReach
{
    public enum RecipeKind { Shaped, Shapeless }

    public readonly struct RecipeIngredient
    {
        public readonly string ItemId;
        public readonly int Count;
        public RecipeIngredient(string itemId, int count = 1) { ItemId = itemId; Count = count; }
    }

    // Authoring/transport data only. Compile copies and validates it before publishing a registry.
    public sealed class RecipeSpec
    {
        public string Id;
        public RecipeKind Kind;
        public int MinimumGridSize = 2;
        public int Width = 1, Height = 1;
        public bool Mirror;
        public RecipeIngredient[] Ingredients;
        public RecipeIngredient Output;
    }

    public sealed class RecipeInfo
    {
        public string Id { get; }
        public RecipeKind Kind { get; }
        public int MinimumGridSize { get; }
        public ItemStack Output { get; }
        public int OutputStackLimit { get; }
        public int Width { get; }
        public int Height { get; }
        public bool AllowsMirroring { get; }
        public IReadOnlyList<ItemStack> Ingredients { get; }
        internal RecipeInfo(RecipeSpec spec, int minimumGridSize, ItemStack output, int limit, int width, int height, ItemStack[] ingredients)
        {
            Id = spec.Id; Kind = spec.Kind; MinimumGridSize = minimumGridSize;
            Output = output; OutputStackLimit = limit;
            Width = width; Height = height; AllowsMirroring = spec.Mirror;
            Ingredients = Array.AsReadOnly(ingredients);
        }
    }

    public sealed class CraftingGrid : ItemContainer
    {
        public int Size { get; }
        public CraftingGrid(int size, Func<byte, int> stackLimit) : base(ValidateSize(size), stackLimit) { Size = size; }
        static int ValidateSize(int size)
        {
            if (size < 2 || size > 4) throw new ArgumentOutOfRangeException(nameof(size), "Crafting grids must be 2x2, 3x3 or 4x4.");
            return size * size;
        }
    }
}
