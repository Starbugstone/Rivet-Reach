using System;
using System.Collections.Generic;

namespace RivetReach
{
    public enum CraftStatus { Crafted, NoRecipe, OutputFull, CursorOccupied, InvalidRequest }
    public enum RecipeFillStatus { Filled, AlreadyReady, UnknownRecipe, RequiresLargerGrid, MissingIngredients, InventoryFull }

    public readonly struct CraftResult
    {
        public readonly CraftStatus Status;
        public readonly int Crafts;
        public bool Succeeded => Status == CraftStatus.Crafted;
        public CraftResult(CraftStatus status, int crafts = 0) { Status = status; Crafts = crafts; }
    }

    // Authority-side service, independent of Unity/UI. One owner executes synchronously;
    // a future server should route commands here on its authority thread, not mutate from workers.
    public sealed class CraftingSession
    {
        readonly RecipeRegistry registry;
        readonly int[] consumption;
        long matchedRevision = -1;
        RecipeInfo preview;
        int maximumCrafts;
        public CraftingGrid Grid { get; }
        public RecipeInfo Preview { get { Refresh(); return preview; } }
        public int MaximumCrafts { get { Refresh(); return maximumCrafts; } }

        public CraftingSession(RecipeRegistry registry, int size, Func<byte, int> stackLimit)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            Grid = new CraftingGrid(size, stackLimit); consumption = new int[Grid.Count];
        }

        void Refresh()
        {
            if (matchedRevision == Grid.Revision) return;
            registry.TryMatch(Grid, consumption, out preview, out maximumCrafts);
            matchedRevision = Grid.Revision;
        }

        public CraftResult CraftToCursor(ref ItemStack cursor)
        {
            Refresh();
            if (preview == null) return new CraftResult(CraftStatus.NoRecipe);
            var output = preview.Output;
            if (!cursor.Empty && cursor.Id != output.Id) return new CraftResult(CraftStatus.CursorOccupied);
            int held = cursor.Empty ? 0 : cursor.Count;
            if (output.Count > preview.OutputStackLimit - held) return new CraftResult(CraftStatus.OutputFull);
            // All validation precedes this commit; no callback/event can interleave its writes.
            cursor = new ItemStack(output.Id, held + output.Count);
            Consume(1);
            return new CraftResult(CraftStatus.Crafted, 1);
        }

        public CraftResult CraftToInventory(Inventory destination, int requestedCrafts = int.MaxValue)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (requestedCrafts <= 0) return new CraftResult(CraftStatus.InvalidRequest);
            Refresh();
            if (preview == null) return new CraftResult(CraftStatus.NoRecipe);
            var output = preview.Output;
            int crafts = Math.Min(requestedCrafts, Math.Min(maximumCrafts, destination.Capacity(output.Id) / output.Count));
            if (crafts == 0) return new CraftResult(CraftStatus.OutputFull);
            // Exact insertion validates once, then writes without invoking callbacks. Capacity includes only
            // the destination, never ingredient slots. A batch moves only whole recipe outputs.
            if (!destination.TryAddExact(output.Id, checked(output.Count * crafts))) return new CraftResult(CraftStatus.OutputFull);
            Consume(crafts);
            return new CraftResult(CraftStatus.Crafted, crafts);
        }

        void Consume(int crafts)
        {
            for (int i = 0; i < Grid.Count; i++)
                if (consumption[i] != 0) Grid.Take(i, consumption[i] * crafts);
        }

        public RecipeFillStatus FillRecipe(string recipeId, Inventory inventory, bool maximum = false)
            => FillRecipe(recipeId, new ItemContainer[] { inventory }, maximum);

        // Ordered, authority-approved ingredient sources. Only the backpack is wired today.
        // Source 0 also receives displaced grid ingredients. Adjacent discovery/access/wake
        // policies remain WIP; future stations can supply additional sources here.
        public RecipeFillStatus FillRecipe(string recipeId, IReadOnlyList<ItemContainer> sources, bool maximum = false)
        {
            if (sources == null || sources.Count == 0) throw new ArgumentException("At least one ingredient source is required.");
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] == null) throw new ArgumentNullException(nameof(sources));
                if (ReferenceEquals(sources[i], Grid)) throw new ArgumentException("The grid is already an ingredient source.");
                for (int j = 0; j < i; j++)
                    if (ReferenceEquals(sources[i], sources[j])) throw new ArgumentException("Duplicate ingredient source.");
            }
            RecipeInfo recipe = null;
            foreach (var entry in registry.Recipes) if (entry.Id == recipeId) { recipe = entry; break; }
            if (recipe == null) return RecipeFillStatus.UnknownRecipe;
            if (recipe.MinimumGridSize > Grid.Size) return RecipeFillStatus.RequiresLargerGrid;
            if (!maximum && Preview == recipe) return RecipeFillStatus.AlreadyReady;
            int crafts = 1;
            if (maximum)
            {
                Span<long> available = stackalloc long[256]; available.Clear();
                Span<int> required = stackalloc int[256]; required.Clear();
                foreach (var source in sources) foreach (var stack in source.Slots) available[stack.Id] += stack.Count;
                foreach (var stack in Grid.Slots) available[stack.Id] += stack.Count;
                crafts = int.MaxValue;
                foreach (var ingredient in recipe.Ingredients)
                {
                    if (ingredient.Empty) continue;
                    required[ingredient.Id] = checked(required[ingredient.Id] + ingredient.Count);
                    crafts = Math.Min(crafts, Grid.StackLimit(ingredient.Id) / ingredient.Count);
                }
                for (int id = 1; id < required.Length; id++)
                    if (required[id] > 0) crafts = (int)Math.Min(crafts, available[id] / required[id]);
                if (crafts == 0) return RecipeFillStatus.MissingIngredients;
                if (Preview == recipe && MaximumCrafts == crafts) return RecipeFillStatus.AlreadyReady;
            }
            var plans = new ItemContainer[sources.Count];
            for (int i = 0; i < sources.Count; i++) plans[i] = sources[i].Snapshot();
            var leftovers = Grid.Snapshot(); var target = new ItemStack[Grid.Count];
            int Reserve(ItemContainer from, byte id, int count)
            {
                for (int i = 0; i < from.Count && count > 0; i++)
                    if (from.Slots[i].Id == id) count -= from.Take(i, count).Count;
                return count;
            }
            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                var ingredient = recipe.Ingredients[i]; if (ingredient.Empty) continue;
                int count = checked(ingredient.Count * crafts);
                int remaining = Reserve(leftovers, ingredient.Id, count);
                foreach (var plan in plans) remaining = Reserve(plan, ingredient.Id, remaining);
                if (remaining != 0) return RecipeFillStatus.MissingIngredients;
                int cell = recipe.Kind == RecipeKind.Shaped ? i % recipe.Width + i / recipe.Width * Grid.Size : i;
                target[cell] = new ItemStack(ingredient.Id, count);
            }
            // Reserve first: consumed backpack stacks can free room for old grid contents.
            foreach (var stack in leftovers.Slots)
                if (!stack.Empty && plans[0].Add(stack.Id, stack.Count) != 0) return RecipeFillStatus.InventoryFull;
            var containers = new ItemContainer[sources.Count + 1];
            var contents = new IReadOnlyList<ItemStack>[containers.Length];
            for (int i = 0; i < sources.Count; i++) { containers[i] = sources[i]; contents[i] = plans[i].Slots; }
            containers[sources.Count] = Grid; contents[sources.Count] = target;
            ItemContainer.CommitMany(containers, contents);
            return RecipeFillStatus.Filled;
        }

        // Partial returns keep the remainder in the grid, so a full inventory cannot lose it.
        public void ReturnIngredients(Inventory destination)
        {
            for (int i = 0; i < Grid.Count; i++) Grid.TransferTo(i, destination);
        }
    }
}
