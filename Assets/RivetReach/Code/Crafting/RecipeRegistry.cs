using System;
using System.Collections.Generic;

namespace RivetReach
{
    // Immutable compiled definitions; safe to share between independently owned crafting sessions.
    public sealed class RecipeRegistry
    {
        // The full <=16 byte pattern is the key, not a lossy fingerprint. Dictionary hash
        // collisions still compare both words and dimensions for exact equality.
        readonly struct PatternKey : IEquatable<PatternKey>
        {
            readonly ulong low, high;
            readonly int width, height;
            public PatternKey(ReadOnlySpan<ItemStack> cells, int width, int height)
            {
                low = high = 0; this.width = width; this.height = height;
                for (int i = 0; i < cells.Length; i++)
                    if (i < 8) low |= (ulong)cells[i].Id << (i * 8);
                    else high |= (ulong)cells[i].Id << ((i - 8) * 8);
            }
            public bool Equals(PatternKey other) => low == other.low && high == other.high && width == other.width && height == other.height;
            public override bool Equals(object obj) => obj is PatternKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    // Avalanche all 128 pattern bits, including the sparse last row of 4x4 grids.
                    ulong hash = low ^ (high + 0x9e3779b97f4a7c15UL + (low << 6) + (low >> 2));
                    hash ^= (uint)(width * 31 + height);
                    hash = (hash ^ (hash >> 30)) * 0xbf58476d1ce4e5b9UL;
                    hash = (hash ^ (hash >> 27)) * 0x94d049bb133111ebUL;
                    return (int)(hash ^ (hash >> 31));
                }
            }
        }

        sealed class Pattern
        {
            public readonly RecipeInfo Recipe;
            public readonly ItemStack[] Cells;
            public Pattern(RecipeInfo recipe, ItemStack[] cells) { Recipe = recipe; Cells = cells; }
        }

        readonly Dictionary<PatternKey, Pattern> shaped;
        readonly Dictionary<PatternKey, Pattern> shapeless;
        public IReadOnlyList<RecipeInfo> Recipes { get; }

        RecipeRegistry(Dictionary<PatternKey, Pattern> shaped, Dictionary<PatternKey, Pattern> shapeless, List<RecipeInfo> recipes)
        {
            this.shaped = shaped; this.shapeless = shapeless; Recipes = recipes.AsReadOnly();
        }

        public static RecipeRegistry Compile(IEnumerable<RecipeSpec> definitions, Func<string, byte> resolveItem, Func<byte, int> stackLimit)
        {
            if (definitions == null || resolveItem == null || stackLimit == null) throw new ArgumentNullException();
            var shaped = new Dictionary<PatternKey, Pattern>();
            var shapeless = new Dictionary<PatternKey, Pattern>();
            var shapedBags = new HashSet<PatternKey>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var recipes = new List<RecipeInfo>();
            Span<ItemStack> bag = stackalloc ItemStack[16];
            Span<int> bagIndices = stackalloc int[16];
            foreach (var spec in definitions)
            {
                if (spec == null || string.IsNullOrWhiteSpace(spec.Id)) throw new ArgumentException("Each recipe needs a stable ID.");
                if (!ids.Add(spec.Id)) throw Invalid(spec, "Duplicate recipe ID.");
                if (spec.Kind != RecipeKind.Shaped && spec.Kind != RecipeKind.Shapeless) throw Invalid(spec, "Unknown recipe kind.");
                if (spec.MinimumGridSize < 2 || spec.MinimumGridSize > 4) throw Invalid(spec, "Minimum grid size must be 2, 3 or 4.");
                if (spec.Ingredients == null || spec.Ingredients.Length == 0 || spec.Ingredients.Length > 16) throw Invalid(spec, "Supply 1 to 16 ingredient cells.");
                if (spec.Kind == RecipeKind.Shaped && (spec.Width < 1 || spec.Width > 4 || spec.Height < 1 || spec.Height > 4 || spec.Width * spec.Height != spec.Ingredients.Length))
                    throw Invalid(spec, "Shaped cells must equal width times height, each dimension from 1 to 4.");
                if (spec.Kind == RecipeKind.Shapeless && spec.Mirror) throw Invalid(spec, "Mirroring only applies to shaped recipes.");

                ItemStack Resolve(RecipeIngredient ingredient, bool emptyAllowed)
                {
                    if (string.IsNullOrWhiteSpace(ingredient.ItemId))
                    {
                        if (!emptyAllowed || ingredient.Count != 0) throw Invalid(spec, "Empty cells must have a blank item ID and zero count.");
                        return default;
                    }
                    byte item;
                    try { item = resolveItem(ingredient.ItemId); }
                    catch (Exception ex) { throw Invalid(spec, "Unknown item " + ingredient.ItemId + ": " + ex.Message); }
                    if (item == 0 || ingredient.Count <= 0 || ingredient.Count > stackLimit(item))
                        throw Invalid(spec, "Invalid count or item: " + ingredient.ItemId + ". Each ingredient/output must fit one stack.");
                    return new ItemStack(item, ingredient.Count);
                }

                var output = Resolve(spec.Output, false);
                var cells = new ItemStack[spec.Ingredients.Length];
                for (int i = 0; i < cells.Length; i++) cells[i] = Resolve(spec.Ingredients[i], spec.Kind == RecipeKind.Shaped);
                int occupied = CompactSorted(cells, bag, bagIndices);
                if (occupied == 0) throw Invalid(spec, "Recipes must consume ingredients.");
                var bagKey = new PatternKey(bag.Slice(0, occupied), 0, occupied);
                int width = 0, height = 0;
                if (spec.Kind == RecipeKind.Shaped)
                {
                    Bounds(cells, spec.Width, spec.Height, out int x, out int y, out width, out height);
                    var cropped = new ItemStack[width * height];
                    for (int row = 0; row < height; row++)
                    for (int col = 0; col < width; col++) cropped[row * width + col] = cells[(row + y) * spec.Width + col + x];
                    cells = cropped;
                }
                else cells = bag.Slice(0, occupied).ToArray();
                int minimum = Math.Max(spec.MinimumGridSize, Math.Max(width, height));
                if (occupied > minimum * minimum) minimum = occupied <= 9 ? 3 : 4;
                var info = new RecipeInfo(spec, minimum, output, stackLimit(output.Id), width, height, cells);
                var pattern = new Pattern(info, cells);

                if (spec.Kind == RecipeKind.Shapeless)
                {
                    if (shapedBags.Contains(bagKey)) throw Invalid(spec, "Shapeless recipe overlaps a shaped recipe's ingredient set.");
                    Insert(shapeless, bagKey, pattern, spec);
                }
                else
                {
                    if (shapeless.ContainsKey(bagKey)) throw Invalid(spec, "Shaped recipe overlaps a shapeless recipe's ingredient set.");
                    shapedBags.Add(bagKey);
                    var key = new PatternKey(cells, width, height);
                    Insert(shaped, key, pattern, spec);
                    if (spec.Mirror)
                    {
                        var mirrored = new ItemStack[cells.Length];
                        for (int row = 0; row < height; row++)
                        for (int col = 0; col < width; col++) mirrored[row * width + col] = cells[row * width + width - 1 - col];
                        var mirrorKey = new PatternKey(mirrored, width, height);
                        if (!mirrorKey.Equals(key)) Insert(shaped, mirrorKey, new Pattern(info, mirrored), spec);
                        else for (int i = 0; i < cells.Length; i++)
                            if (cells[i].Count != mirrored[i].Count) throw Invalid(spec, "Mirroring identical item positions with different counts is ambiguous.");
                    }
                }
                recipes.Add(info);
            }
            return new RecipeRegistry(shaped, shapeless, recipes);
        }

        static ArgumentException Invalid(RecipeSpec spec, string message) => new ArgumentException("Recipe '" + spec.Id + "': " + message);
        static void Insert(Dictionary<PatternKey, Pattern> index, PatternKey key, Pattern pattern, RecipeSpec spec)
        {
            if (index.TryGetValue(key, out var existing)) throw Invalid(spec, "Ambiguous layout with '" + existing.Recipe.Id + "' (including mirrors, quantities and grid gates).");
            index.Add(key, pattern);
        }

        static void Bounds(ReadOnlySpan<ItemStack> cells, int sizeX, int sizeY, out int x, out int y, out int width, out int height)
        {
            x = sizeX; y = sizeY; int maxX = -1, maxY = -1;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].Empty) continue;
                x = Math.Min(x, i % sizeX); y = Math.Min(y, i / sizeX);
                maxX = Math.Max(maxX, i % sizeX); maxY = Math.Max(maxY, i / sizeX);
            }
            width = maxX < 0 ? 0 : maxX - x + 1; height = maxY < 0 ? 0 : maxY - y + 1;
        }

        // Bounded insertion sort (at most sixteen cells). Counts break ties so duplicate
        // shapeless ingredients consume deterministically even when their stack sizes differ.
        static int CompactSorted(ReadOnlySpan<ItemStack> cells, Span<ItemStack> sorted, Span<int> indices)
        {
            int count = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                var stack = cells[i]; if (stack.Empty) continue;
                int j = count;
                while (j > 0 && (sorted[j - 1].Id > stack.Id || (sorted[j - 1].Id == stack.Id && sorted[j - 1].Count > stack.Count)))
                { sorted[j] = sorted[j - 1]; indices[j] = indices[j - 1]; j--; }
                sorted[j] = stack; indices[j] = i; count++;
            }
            return count;
        }

        public bool TryMatch(CraftingGrid grid, Span<int> consumption, out RecipeInfo recipe, out int maximumCrafts)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (consumption.Length < grid.Count) throw new ArgumentException("Consumption buffer is smaller than the grid.");
            consumption.Clear(); recipe = null; maximumCrafts = 0;
            Span<ItemStack> cells = stackalloc ItemStack[16];
            for (int i = 0; i < grid.Count; i++) cells[i] = grid.Slots[i];
            var active = cells.Slice(0, grid.Count);
            Bounds(active, grid.Size, grid.Size, out int x, out int y, out int width, out int height);
            if (width == 0) return false;
            Span<ItemStack> normalized = stackalloc ItemStack[16];
            Span<int> indices = stackalloc int[16];
            for (int row = 0; row < height; row++)
            for (int col = 0; col < width; col++)
            {
                int i = row * width + col, source = (row + y) * grid.Size + col + x;
                normalized[i] = cells[source]; indices[i] = source;
            }
            int length = width * height;
            var key = new PatternKey(normalized.Slice(0, length), width, height);
            if (shaped.TryGetValue(key, out var pattern) && Accept(pattern, grid.Size, normalized, indices, consumption, out maximumCrafts))
            { recipe = pattern.Recipe; return true; }
            length = CompactSorted(active, normalized, indices);
            key = new PatternKey(normalized.Slice(0, length), 0, length);
            if (shapeless.TryGetValue(key, out pattern) && Accept(pattern, grid.Size, normalized, indices, consumption, out maximumCrafts))
            { recipe = pattern.Recipe; return true; }
            return false;
        }

        static bool Accept(Pattern pattern, int size, ReadOnlySpan<ItemStack> stacks, ReadOnlySpan<int> indices, Span<int> consumption, out int maximumCrafts)
        {
            maximumCrafts = 0;
            if (size < pattern.Recipe.MinimumGridSize) return false;
            int maximum = int.MaxValue;
            for (int i = 0; i < pattern.Cells.Length; i++)
            {
                int required = pattern.Cells[i].Count;
                if (required == 0) continue;
                maximum = Math.Min(maximum, stacks[i].Count / required);
                if (maximum == 0) return false;
            }
            for (int i = 0; i < pattern.Cells.Length; i++) consumption[indices[i]] = pattern.Cells[i].Count;
            maximumCrafts = maximum;
            return true;
        }
    }
}
