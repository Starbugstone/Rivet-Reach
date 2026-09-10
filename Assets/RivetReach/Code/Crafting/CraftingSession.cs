using System;

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

        public RecipeFillStatus FillRecipe(string recipeId, Inventory inventory)
        {
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));
            RecipeInfo recipe = null;
            foreach (var entry in registry.Recipes) if (entry.Id == recipeId) { recipe = entry; break; }
            if (recipe == null) return RecipeFillStatus.UnknownRecipe;
            if (recipe.MinimumGridSize > Grid.Size) return RecipeFillStatus.RequiresLargerGrid;
            if (Preview == recipe) return RecipeFillStatus.AlreadyReady;
            var source = inventory.Snapshot(); var leftovers = Grid.Snapshot();
            var target = new ItemStack[Grid.Count];
            int Reserve(ItemContainer from, byte id, int count)
            {
                for (int i = 0; i < from.Count && count > 0; i++)
                    if (from.Slots[i].Id == id) count -= from.Take(i, count).Count;
                return count;
            }
            for (int i = 0; i < recipe.Ingredients.Count; i++)
            {
                var ingredient = recipe.Ingredients[i]; if (ingredient.Empty) continue;
                int remaining = Reserve(leftovers, ingredient.Id, ingredient.Count);
                if (Reserve(source, ingredient.Id, remaining) != 0) return RecipeFillStatus.MissingIngredients;
                int cell = recipe.Kind == RecipeKind.Shaped ? i % recipe.Width + i / recipe.Width * Grid.Size : i;
                target[cell] = ingredient;
            }
            // Reserve first: consumed inventory stacks can free room for old grid contents.
            for (int i = 0; i < leftovers.Count; i++)
            {
                var stack = leftovers.Slots[i];
                if (!stack.Empty && source.Add(stack.Id, stack.Count) != 0) return RecipeFillStatus.InventoryFull;
            }
            ItemContainer.CommitPair(inventory, source.Slots, Grid, target);
            return RecipeFillStatus.Filled;
        }

        // Partial returns keep the remainder in the grid, so a full inventory cannot lose it.
        public void ReturnIngredients(Inventory destination)
        {
            for (int i = 0; i < Grid.Count; i++) Grid.TransferTo(i, destination);
        }
    }
}
