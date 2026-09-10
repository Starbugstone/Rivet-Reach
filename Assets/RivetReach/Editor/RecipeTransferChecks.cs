using System;
using System.Linq;

namespace RivetReach.Editor
{
    public static class RecipeTransferChecks
    {
        public static int Run(ItemRegistry items, RecipeRegistry registry)
        {
            int assertions = 0;
            void Check(bool ok, string message) { if (!ok) throw new Exception("Recipe transfer: " + message); assertions++; }
            int Limit(byte id) => items.Get(id).stackLimit;
            int[] Totals(Inventory inventory, CraftingSession session) => items.items.Select(i => inventory.Total(i.runtimeId) + session.Grid.Total(i.runtimeId)).ToArray();
            foreach (var recipe in registry.Recipes)
            foreach (int size in Enumerable.Range(recipe.MinimumGridSize, 5 - recipe.MinimumGridSize))
            {
                var inventory = new Inventory(Limit); var session = new CraftingSession(registry, size, Limit);
                foreach (var ingredient in recipe.Ingredients) if (!ingredient.Empty) inventory.Add(ingredient.Id, ingredient.Count);
                var totals = Totals(inventory, session);
                Check(session.FillRecipe(recipe.Id, inventory) == RecipeFillStatus.Filled, recipe.Id + " loads into " + size + " × " + size);
                Check(session.Preview?.Id == recipe.Id && session.MaximumCrafts == 1, recipe.Id + " produces exactly one craft's canonical preview");
                Check(Totals(inventory, session).SequenceEqual(totals), recipe.Id + " conserves every material");
                long inventoryRevision = inventory.Revision, gridRevision = session.Grid.Revision;
                Check(session.FillRecipe(recipe.Id, inventory) == RecipeFillStatus.AlreadyReady && inventory.Revision == inventoryRevision && session.Grid.Revision == gridRevision,
                    recipe.Id + " repeated fill preserves already prepared stacks");
            }
            var workbench = registry.Recipes.Single(r => r.Output.Id == BlockId.Workbench);
            var personal = new CraftingSession(registry, 2, Limit); var bag = new Inventory(Limit);
            personal.Grid.Add(BlockId.Log, 64);
            bag.Add(BlockId.Planks, 3);
            long beforeBag = bag.Revision, beforeGrid = personal.Grid.Revision;
            Check(personal.FillRecipe(workbench.Id, bag) == RecipeFillStatus.MissingIngredients && bag.Revision == beforeBag && personal.Grid.Revision == beforeGrid,
                "Missing materials never partially return or consume existing grid contents");
            Check(personal.FillRecipe("missing:recipe", bag) == RecipeFillStatus.UnknownRecipe && bag.Revision == beforeBag && personal.Grid.Revision == beforeGrid, "Unknown IDs cannot supply a recipe");
            var pickaxe = registry.Recipes.Single(r => r.Output.Id == BlockId.WoodPickaxe);
            Check(personal.FillRecipe(pickaxe.Id, bag) == RecipeFillStatus.RequiresLargerGrid && bag.Revision == beforeBag && personal.Grid.Revision == beforeGrid, "Station size gates reject before inventory edits");
            bag = new Inventory(Limit); bag.Add(BlockId.Planks, 5, 0, 1);
            for (int i = 1; i < bag.Count; i++) bag.Add(BlockId.Dirt, 64, i, i + 1);
            beforeBag = bag.Revision; beforeGrid = personal.Grid.Revision;
            Check(personal.FillRecipe(workbench.Id, bag) == RecipeFillStatus.InventoryFull && bag.Revision == beforeBag && personal.Grid.Revision == beforeGrid,
                "No room for displaced grid materials rejects the entire operation");
            bag.Take(0, 1); var combined = Totals(bag, personal);
            Check(personal.FillRecipe(workbench.Id, bag) == RecipeFillStatus.Filled && bag.Total(BlockId.Log) == 64 && personal.Preview?.Id == workbench.Id,
                "Reserving recipe ingredients frees room for displaced grid materials in an otherwise full inventory");
            Check(Totals(bag, personal).SequenceEqual(combined), "Full-inventory exchange conserves all items");
            personal = new CraftingSession(registry, 2, Limit); bag = new Inventory(Limit);
            personal.Grid.Add(BlockId.Planks, 2, 3, 4); bag.Add(BlockId.Planks, 2);
            Check(personal.FillRecipe(workbench.Id, bag) == RecipeFillStatus.Filled && personal.Preview?.Id == workbench.Id && bag.Total(BlockId.Planks) == 0,
                "Ingredients already in a differently arranged grid are reused");
            var repeated = RecipeRegistry.Compile(new[] { new RecipeSpec { Id = "test:repeated", Kind = RecipeKind.Shapeless, MinimumGridSize = 4,
                Ingredients = new[] { new RecipeIngredient("rivet:dirt", 2), new RecipeIngredient("rivet:dirt", 1), new RecipeIngredient("rivet:stone", 3) },
                Output = new RecipeIngredient("rivet:log", 1) } }, items.ResolveId, Limit);
            var four = new CraftingSession(repeated, 4, Limit); bag = new Inventory(Limit); bag.Add(BlockId.Dirt, 3); bag.Add(BlockId.Stone, 3);
            Check(four.FillRecipe("test:repeated", bag) == RecipeFillStatus.Filled && four.Preview?.Id == "test:repeated" && four.Grid.Slots.Count(s => s.Id == BlockId.Dirt) == 2,
                "Repeated shapeless ingredients retain separate slots and quantities");
            return assertions;
        }
    }
}
