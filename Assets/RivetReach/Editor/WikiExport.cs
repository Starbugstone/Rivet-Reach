using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RivetReach.Editor
{
    // Read-only documentation export. No scene, definition, importer or gameplay changes.
    [InitializeOnLoad]
    public static class WikiExport
    {
        const string Request = "Logs/wiki-export-request.txt";
        static WikiExport() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            try { Export(); File.WriteAllText("Logs/wiki-export-result.txt", "SUCCESS"); }
            catch (Exception error) { Debug.LogException(error); File.WriteAllText("Logs/wiki-export-result.txt", error.ToString()); }
        }

        [Serializable] public sealed class Stack { public string item; public int count; }
        [Serializable] public sealed class Item
        {
            public ItemDefinition definition;
            public string drop, requiredTier, help;
            public bool placeable, ore, crop;
            public int fuelTicks, watts, waterMl;
        }
        [Serializable] public sealed class Recipe
        {
            public string id, station, kind;
            public Stack output;
            public Stack[] ingredients;
            public string[] fuels;
            public int grid, width, height, ticks, watts;
            public bool mirror;
        }
        [Serializable] public sealed class Source { public string path, sha256; }
        [Serializable] public sealed class Catalog
        {
            public string unityVersion;
            public int ticksPerSecond;
            public Item[] items;
            public Recipe[] recipes;
            public Source[] sources;
        }

        [MenuItem("Rivet Reach/Export wiki items and icons")]
        public static void Export()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Export in Edit mode.");
            var registry = ItemRegistry.Load();
            var processing = ProcessingCatalogAsset.Load().Compile(registry);
            var index = new RecipeBrowserIndex(registry, RecipeCatalogAsset.Load().Compile(registry), processing);
            string Id(byte id) => id == 0 ? "" : registry.Get(id).stableId;
            Stack Entry(ItemStack stack) => new Stack { item = Id(stack.Id), count = stack.Count };
            var catalog = new Catalog
            {
                unityVersion = Application.unityVersion,
                ticksPerSecond = WorldSurvival.TicksPerSecond,
                items = registry.items.Select(item =>
                {
                    IndustryDefinition.All.TryGetValue(item.runtimeId, out var machine);
                    return new Item { definition = item, drop = Id(registry.FistDrop(item.runtimeId)),
                        requiredTier = BlockId.RequiredTier(item.runtimeId).ToString(), placeable = BlockId.Placeable(item.runtimeId),
                        ore = BlockId.Ore(item.runtimeId), crop = BlockId.Crop(item.runtimeId),
                        fuelTicks = processing.FuelTicks(item.runtimeId), help = machine?.Help ?? "",
                        watts = machine?.Watts ?? 0, waterMl = machine?.WaterCapacity ?? 0 };
                }).ToArray(),
                recipes = index.Recipes.Select(recipe => new Recipe
                {
                    id = recipe.Id, station = Id(recipe.Station), output = Entry(recipe.Output),
                    ingredients = recipe.Ingredients.Select(Entry).ToArray(), fuels = recipe.Fuels.Select(Id).ToArray(),
                    kind = recipe.GridRecipe?.Kind.ToString() ?? "Processing", grid = recipe.GridRecipe?.MinimumGridSize ?? 0,
                    width = recipe.GridRecipe?.Width ?? 0, height = recipe.GridRecipe?.Height ?? 0,
                    mirror = recipe.GridRecipe?.AllowsMirroring ?? false, ticks = recipe.Ticks, watts = recipe.Watts
                }).ToArray()
            };
            Directory.CreateDirectory(".docs/wiki/icons");
            Directory.CreateDirectory(".docs/wiki-data");

            // Invoke the actual inventory renderer, including procedural terrain/survival art.
            // Keep its temporary components in a preview scene, away from the user's scene.
            var scene = EditorSceneManager.NewPreviewScene();
            Dictionary<byte, Texture2D> icons = null;
            try
            {
                var root = new GameObject("Wiki icon export") { hideFlags = HideFlags.HideAndDontSave };
                root.SetActive(false);
                SceneManager.MoveGameObjectToScene(root, scene);
                var game = root.AddComponent<Expedition>();
                typeof(Expedition).GetProperty("Registry").SetValue(game, registry);
                var ui = root.AddComponent<GameUI>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(GameUI).GetField("game", flags).SetValue(ui, game);
                typeof(GameUI).GetMethod("BuildIcons", flags).Invoke(ui, null);
                icons = (Dictionary<byte, Texture2D>)typeof(GameUI).GetField("icons", flags).GetValue(ui);
                var settings = Resources.Load<ItemBrowserSettings>("Definitions/ItemBrowser");
                foreach (var item in registry.items)
                {
                    var texture = settings?.entries?.LastOrDefault(entry => entry.itemId == item.stableId && entry.icon != null)?.icon ?? icons[item.runtimeId];
                    if (texture == null) throw new InvalidOperationException("Missing icon: " + item.stableId);
                    string path = AssetDatabase.GetAssetPath(texture);
                    byte[] png = path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? File.ReadAllBytes(path) : texture.EncodeToPNG();
                    File.WriteAllBytes(".docs/wiki/icons/" + item.runtimeId + ".png", png);
                }
            }
            finally
            {
                // Inactive preview components need explicit cleanup of generated textures.
                if (icons != null)
                {
                    foreach (var texture in icons.Values.Distinct())
                        if (texture != null && !AssetDatabase.Contains(texture)) UnityEngine.Object.DestroyImmediate(texture);
                    icons.Clear();
                }
                EditorSceneManager.ClosePreviewScene(scene);
            }

            // Detect stale exports before documentation generation/publishing.
            var sources = Directory.GetFiles("Assets/RivetReach/Resources/Definitions", "*", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles("Assets/RivetReach/Code", "*.cs", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles("Assets/RivetReach/Resources/Industry/Icons", "*.png"))
                .Concat(Directory.GetFiles("Assets/RivetReach/Resources/Food", "*.png"))
                .Concat(Directory.GetFiles("Assets/RivetReach/Resources/Orchard", "*.png"))
                .Concat(Directory.GetFiles("Assets/RivetReach/Resources/Tools", "*Icon.png"))
                .Concat(Directory.GetFiles("Assets/RivetReach/Resources/ItemIcons", "*.png"))
                .Concat(new[] { "Assets/RivetReach/Resources/Materials/BlockTiles.asset", "Assets/RivetReach/Editor/WikiExport.cs", "Assets/RivetReach/Editor/ItemAppearanceBuild.cs", "Assets/RivetReach/Editor/ItemIcon.shader" })
                .Where(File.Exists).Select(path => path.Replace('\\', '/')).OrderBy(path => path, StringComparer.Ordinal);
            using (var sha = SHA256.Create())
                catalog.sources = sources.Select(path => new Source { path = path,
                    sha256 = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant() }).ToArray();
            File.WriteAllText(".docs/wiki-data/catalog.json", JsonUtility.ToJson(catalog, true) + "\n");
            Debug.Log($"Wiki export: {catalog.items.Length} item icons and {catalog.recipes.Length} recipes.");
        }
    }
}
