using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class RecipeBrowserBuild
    {
        [MenuItem("Rivet Reach/Build recipe browser review")]
        public static void Run()
        {
            Directory.CreateDirectory("Logs/RecipeBrowser");
            var items = ItemRegistry.Load();
            var crafting = RecipeCatalogAsset.Load().Compile(items);
            var processing = ProcessingCatalogAsset.Load().Compile(items);
            var index = new RecipeBrowserIndex(items, crafting, processing);
            int assertions = 0;
            void Check(bool ok, string message) { if (!ok) throw new Exception(message); assertions++; }
            foreach (var recipe in crafting.Recipes)
            {
                Check(index.Find(recipe.Output.Id, false).Any(r => r.Id == recipe.Id && r.Output.Count == recipe.Output.Count), "Output lookup: " + recipe.Id);
                foreach (var group in recipe.Ingredients.Where(s => !s.Empty).GroupBy(s => s.Id))
                    Check(index.Find(group.Key, true).Count(r => r.Id == recipe.Id) == 1, "Unique ingredient usage: " + recipe.Id);
                Check(index.Recipes.Single(r => r.Id == recipe.Id).GridRecipe == recipe, "Preserve canonical grid: " + recipe.Id);
            }
            foreach (var recipe in processing.Recipes)
                Check(index.Find(recipe.Output.Id, false).Any(r => r.Id == recipe.Id && r.Station == BlockId.Furnace && r.Ticks == recipe.Ticks && r.Ingredients[0].Count == recipe.Input.Count), "Furnace source: " + recipe.Id);
            Check(index.Find(IndustryId.CrushedIron, false).Single().Station == IndustryId.Crusher, "Crushed iron requires crusher");
            Check(index.Find(BlockId.RawIron, true).Any(r => r.Station == IndustryId.Crusher) && index.Find(BlockId.RawIron, true).Any(r => r.Station == BlockId.Furnace), "Raw iron direct and doubled processing paths");
            Check(index.Find(BlockId.Coal, true).Any(r => r.Fuels.Contains(BlockId.Coal)), "Fuel uses are indexed");
            Check(index.Find(BlockId.Furnace, true).Count(r => r.Station == BlockId.Furnace) == processing.Recipes.Count, "Station usage includes every furnace process");
            Check(index.Find(BlockId.Dirt, false).Count == 0, "Gathered resource has no invented recipe");
            Check(index.Find(0, true).Count == 0, "Empty cells are not ingredients");
            File.WriteAllText("Logs/RecipeBrowser/index-checks.txt", $"PASS {assertions} assertions; {items.items.Length} items; {crafting.Recipes.Count} grid recipes; {processing.Recipes.Count} furnace recipes; {index.Recipes.Count} total indexed recipes\n");
            File.WriteAllText("Logs/RecipeBrowser/transfer-checks.txt", "PASS " + RecipeTransferChecks.Run(items, crafting) + " transfer assertions\n");
            CraftingChecks.Run(); StarterRecipeChecks.Run(items, crafting);
            const string output = "Builds/RecipeBrowser"; Directory.CreateDirectory(output);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = output + "/RivetReach.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            File.WriteAllText("Logs/RecipeBrowser/build-summary.txt", $"{report.summary.result}; errors {report.summary.totalErrors}; warnings {report.summary.totalWarnings}; seconds {report.summary.totalTime.TotalSeconds}\n");
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Recipe browser build failed");
            File.Copy("LICENSE.md", output + "/LICENSE.md", true); File.Copy(".docs/THIRD_PARTY_NOTICES.md", output + "/THIRD_PARTY_NOTICES.md", true);
            foreach (string source in Directory.GetFiles(".docs/licenses", "*", SearchOption.AllDirectories))
            { string target = Path.Combine(output, "licenses", Path.GetRelativePath(".docs/licenses", source)); Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(source, target, true); }
        }
    }
}
