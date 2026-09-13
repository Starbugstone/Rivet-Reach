using System;
using System.Collections.Generic;
using System.Linq;

namespace RivetReach
{
    // A session snapshot for discovery; never supplies authority to a crafting transaction.
    public sealed class BrowserRecipe
    {
        public string Id { get; }
        public string StationName { get; }
        public byte Station { get; }
        public ItemStack Output { get; }
        public IReadOnlyList<ItemStack> Ingredients { get; }
        public IReadOnlyList<byte> Fuels { get; }
        public RecipeInfo GridRecipe { get; }
        public int Ticks { get; }
        public int Watts { get; }
        public BrowserRecipe(string id, string stationName, byte station, ItemStack output,
            IEnumerable<ItemStack> ingredients, RecipeInfo gridRecipe = null, int ticks = 0,
            int watts = 0, IEnumerable<byte> fuels = null)
        {
            Id = id; StationName = stationName; Station = station; Output = output;
            Ingredients = Array.AsReadOnly(ingredients.ToArray()); GridRecipe = gridRecipe;
            Ticks = ticks; Watts = watts; Fuels = Array.AsReadOnly((fuels ?? Array.Empty<byte>()).ToArray());
        }
    }

    public sealed class RecipeBrowserIndex
    {
        static readonly IReadOnlyList<BrowserRecipe> Empty = Array.Empty<BrowserRecipe>();
        readonly Dictionary<byte, IReadOnlyList<BrowserRecipe>> outputs = new Dictionary<byte, IReadOnlyList<BrowserRecipe>>();
        readonly Dictionary<byte, IReadOnlyList<BrowserRecipe>> uses = new Dictionary<byte, IReadOnlyList<BrowserRecipe>>();
        public IReadOnlyList<BrowserRecipe> Recipes { get; }
        public IReadOnlyList<BrowserRecipe> Find(byte item, bool usages) =>
            (usages ? uses : outputs).TryGetValue(item, out var result) ? result : Empty;

        public RecipeBrowserIndex(ItemRegistry items, RecipeRegistry crafting, ProcessingRegistry processing)
        {
            var all = new List<BrowserRecipe>();
            foreach (var recipe in crafting.Recipes)
            {
                byte station = recipe.MinimumGridSize == 4 ? IndustryId.Bench : recipe.MinimumGridSize == 3 ? BlockId.Workbench : (byte)0;
                string name = station == 0 ? "Personal crafting · 2 × 2" : items.Get(station).displayName + " · " + recipe.MinimumGridSize + " × " + recipe.MinimumGridSize;
                all.Add(new BrowserRecipe(recipe.Id, name, station, recipe.Output, recipe.Ingredients, recipe));
            }
            byte[] fuels = items.items.Where(i => processing.FuelTicks(i.runtimeId) > 0).Select(i => i.runtimeId).ToArray();
            foreach (var recipe in processing.Recipes)
                all.Add(new BrowserRecipe(recipe.Id, items.Get(BlockId.Furnace).displayName, BlockId.Furnace,
                    recipe.Output, new[] { recipe.Input }, ticks: recipe.Ticks, fuels: fuels));
            // Share output identities and quantities with processing and input acceptance.
            foreach (var item in items.items)
            {
                var crushed = MachineState.CrusherOutput(item.runtimeId);
                if (crushed.Empty) continue;
                all.Add(new BrowserRecipe("crusher:" + item.stableId, items.Get(IndustryId.Crusher).displayName,
                    IndustryId.Crusher, crushed, new[] { new ItemStack(item.runtimeId, 1) },
                    ticks: MachineState.CrusherTicks, watts: IndustryDefinition.All[IndustryId.Crusher].Watts));
            }
            foreach (var recipe in processing.Recipes)
                all.Add(new BrowserRecipe("electric:" + recipe.Id, items.Get(IndustryId.ElectricFurnace).displayName, IndustryId.ElectricFurnace,
                    recipe.Output, new[] { recipe.Input }, ticks: recipe.Ticks, watts: IndustryDefinition.All[IndustryId.ElectricFurnace].Watts));
            Recipes = all.AsReadOnly();
            foreach (var group in all.GroupBy(r => r.Output.Id)) outputs.Add(group.Key, Array.AsReadOnly(group.ToArray()));
            var consuming = new Dictionary<byte, List<BrowserRecipe>>();
            foreach (var recipe in all)
            {
                var seen = new HashSet<byte>(recipe.Ingredients.Where(s => !s.Empty).Select(s => s.Id));
                seen.UnionWith(recipe.Fuels);
                if (recipe.Station != 0) seen.Add(recipe.Station);
                foreach (byte id in seen)
                {
                    if (!consuming.TryGetValue(id, out var list)) consuming.Add(id, list = new List<BrowserRecipe>());
                    list.Add(recipe);
                }
            }
            foreach (var pair in consuming) uses.Add(pair.Key, pair.Value.AsReadOnly());
        }
    }
}
