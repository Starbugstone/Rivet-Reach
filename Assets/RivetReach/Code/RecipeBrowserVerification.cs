using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator BrowserPointer(Component target, bool right = false, bool shift = false, bool control = false)
        {
            var rect = (RectTransform)target.transform;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(Keyboard.current, control ? new KeyboardState(Key.LeftCtrl) : shift ? new KeyboardState(Key.LeftShift) : new KeyboardState());
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }); yield return null; yield return null;
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Check(hits.Count > 0 && (hits[0].gameObject == target.gameObject || hits[0].gameObject.transform.IsChildOf(target.transform)), "Pointer reaches " + target.name);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }.WithButton(right ? MouseButton.Right : MouseButton.Left)); yield return null; yield return null;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }); yield return null; yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
        }
        IEnumerator ReviewRecipeBrowser()
        {
            game.Mobs.enabled = false; game.Diagnostics = false;
            game.SetMode(ScreenMode.Inventory); yield return null; yield return null;
            InputField Search() => game.UI.GetComponentsInChildren<InputField>().Single(f => f.name == "Item browser search");
            BrowserItemView Item(byte id) => game.UI.GetComponentsInChildren<BrowserItemView>().First(v => v.Item == id && v.name.StartsWith("Browse "));
            Button Named(string name) => game.UI.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == name);
            bool TextContains(string value) => game.UI.GetComponentsInChildren<Text>().Any(t => t.text.Contains(value));
            var seen = new HashSet<byte>();
            for (int page = 0; page < (game.Registry.items.Length + 59) / 60; page++)
            {
                var cells = game.UI.GetComponentsInChildren<BrowserItemView>().Where(v => v.name.StartsWith("Browse ")).ToArray();
                Check(cells.Length <= 60 && cells.All(v => v.Icon.texture != null), "Sidebar page has at most 60 rendered, textured icons");
                foreach (var cell in cells) seen.Add(cell.Item);
                game.UI.ChangeBrowserPage(1); yield return null;
            }
            Check(seen.SetEquals(game.Registry.items.Select(i => i.runtimeId)), "Every registered item is reachable across sidebar pages");
            game.UI.ChangeBrowserPage(-100); yield return null;
            yield return Capture("sidebar-inventory");
            Search().text = "RAW IRON"; yield return null;
            Check(game.UI.GetComponentsInChildren<BrowserItemView>().Count(v => v.name.StartsWith("Browse ")) == 1, "Search matches case-insensitive multiword item names");
            long inventoryRevision = game.Inventory.Revision, gridRevision = game.Crafting.Grid.Revision;
            yield return BrowserPointer(Item(BlockId.RawIron), shift: true);
            Check(TextContains("Furnace") && TextContains("1 / 2"), "Shift-click shows raw iron uses including direct smelting");
            yield return Capture("raw-iron-uses");
            // Recipe navigation arrows are scoped to the detail panel, independently of item pages.
            var detail = game.UI.GetComponentsInChildren<Transform>().Single(t => t.name == "Recipe detail");
            var next = detail.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == "›");
            yield return BrowserPointer(next);
            Check(TextContains("Crusher") && TextContains("160 W") && TextContains("5 seconds"), "Next use shows crusher, duration and full-power requirement");
            yield return Capture("crusher-recipe");
            var machine = game.UI.GetComponentsInChildren<Transform>().Single(t => t.name == "Recipe detail");
            yield return BrowserPointer(machine.GetComponentsInChildren<BrowserItemView>().First(v => v.Item == IndustryId.Crusher));
            Check(TextContains("4 × 4"), "Clicking the crusher station follows its construction recipe at the Machinist bench");
            yield return Capture("machine-construction");
            yield return BrowserPointer(Named("‹ BACK"));
            Check(TextContains("Crusher") && TextContains("160 W"), "Back restores the previous item, uses mode and recipe page");
            yield return BrowserPointer(Named("DONE"));
            Search().text = "rivet:iron_ingot"; yield return null;
            Check(Item(BlockId.IronIngot) != null, "Stable item IDs are searchable");
            yield return BrowserPointer(Item(BlockId.IronIngot));
            for (int i = 0; i < 4 && !TextContains("10 seconds"); i++)
            {
                var current = game.UI.GetComponentsInChildren<Transform>().Single(t => t.name == "Recipe detail");
                yield return BrowserPointer(current.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == "›"));
            }
            Check(TextContains("Furnace") && TextContains("10 seconds"), "Left-click shows iron production and processing time");
            Check(game.Inventory.Revision == inventoryRevision && game.Crafting.Grid.Revision == gridRevision && game.UI.HeldStack.Empty, "Browsing and chain navigation do not grant or consume items");
            yield return BrowserPointer(Named("DONE"));
            Search().text = "plank"; yield return null;
            yield return BrowserPointer(Item(BlockId.Planks), right: true);
            Check(TextContains("MATERIALS / CRAFT"), "Right-click is an alternate usage lookup");
            yield return BrowserPointer(Named("DONE"));
            Search().text = "dirt"; yield return null; yield return BrowserPointer(Item(BlockId.Dirt));
            Check(TextContains("No crafting or processing recipe"), "Gathered resources show an explicit empty recipe state");
            yield return BrowserPointer(Named("DONE"));
            Search().text = "zz nonexistent"; yield return null;
            Check(TextContains("No matching items") && !game.UI.GetComponentsInChildren<BrowserItemView>().Any(v => v.name.StartsWith("Browse ")), "Empty search hides all cells and shows useful feedback");
            Search().text = ""; yield return BrowserPointer(Search());
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(game.Input.Keys["Inventory"])); yield return null; yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
            Check(game.Mode == ScreenMode.Inventory && game.UI.EditingText, "Typing the inventory binding in search does not close the inventory");
            Search().text = ""; Search().DeactivateInputField(); EventSystem.current.SetSelectedGameObject(null);
            game.Inventory.Add(BlockId.IronIngot, 3, 12, 13); yield return null;
            Vector2 slotPoint = CraftPoint(12);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = slotPoint }); yield return null; yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.R)); yield return null; yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
            Check(TextContains("Iron ingot") && game.UI.GetComponentsInChildren<Transform>().Any(t => t.name == "Recipe detail") && game.Inventory.Total(BlockId.IronIngot) == 3, "R on an inventory stack opens recipes without moving it");
            game.UI.CloseBrowserRecipe(); yield return null;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = slotPoint }); yield return null; yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.U)); yield return null; yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
            Check(TextContains("MATERIALS / CRAFT") && !TextContains("No registered recipe"), "U on an inventory stack opens its uses");
            game.UI.CloseBrowserRecipe(); yield return null;
            game.UI.ClickSlot(12, false, false); var held = game.UI.HeldStack;
            Search().text = "torch"; yield return null; yield return BrowserPointer(Item(BlockId.Torch));
            Check(game.UI.HeldStack.Id == held.Id && game.UI.HeldStack.Count == held.Count && game.Crafting.Grid.Revision == gridRevision, "Recipe clicks preserve a held cursor stack and cannot fall through into crafting");
            Check(TextContains("1 / 2"), "Alternative coal and charcoal torch recipes are both discoverable");
            yield return Capture("torch-alternatives");
            game.UI.CloseBrowserRecipe(); game.UI.ClickSlot(12, false, false); game.Inventory.Take(12, int.MaxValue);
            Search().text = "iron"; game.SetMode(ScreenMode.Play); game.SetMode(ScreenMode.Inventory); yield return null; yield return null;
            Check(Search().text == "iron", "Search survives inventory close and reopen");
            Search().text = "";
            Search().text = "workbench"; game.Inventory.Add(BlockId.Planks, 4, 12, 13); game.UI.HeldStack = new ItemStack(BlockId.Dirt, 2); yield return null;
            yield return BrowserPointer(Item(BlockId.Workbench), control: true);
            Check(game.Crafting.Preview?.Output.Id == BlockId.Workbench && game.Crafting.Grid.Total(BlockId.Planks) == 4 && game.Inventory.Total(BlockId.Planks) == 0,
                "Ctrl-click places a complete personal recipe using inventory ingredients");
            Check(game.Inventory.Total(BlockId.Workbench) == 0 && game.UI.HeldStack.Id == BlockId.Dirt && game.UI.HeldStack.Count == 2,
                "Recipe placement does not craft output or spend the held cursor stack");
            yield return Capture("ctrl-fill-personal");
            long filledInventory = game.Inventory.Revision, filledGrid = game.Crafting.Grid.Revision;
            yield return BrowserPointer(Item(BlockId.Workbench), control: true);
            Check(game.Inventory.Revision == filledInventory && game.Crafting.Grid.Revision == filledGrid, "Repeated Ctrl-click leaves an already ready recipe unchanged");
            game.UI.HeldStack.Clear();
            for (int i = 0; i < game.Crafting.Grid.Count; i++) game.Crafting.Grid.Take(i, int.MaxValue);
            Search().text = "rivet:wood_pickaxe"; yield return null;
            yield return BrowserPointer(Item(BlockId.WoodPickaxe), control: true);
            Check(TextContains("Requires Workbench") && game.Crafting.Grid.Slots.All(s => s.Empty), "Ctrl-click cannot fill a larger recipe into the personal grid");
            game.UI.CloseBrowserRecipe();
            var firstTorch = game.Recipes.Recipes.First(r => r.Output.Id == BlockId.Torch);
            byte otherFuel = firstTorch.Ingredients.Any(s => s.Id == BlockId.Coal) ? BlockId.Charcoal : BlockId.Coal;
            game.Inventory.Add(otherFuel, 1); game.Inventory.Add(BlockId.Stick, 1);
            Search().text = "torch"; yield return null; yield return BrowserPointer(Item(BlockId.Torch));
            var exactOutput = game.UI.GetComponentsInChildren<BrowserItemView>().Single(v => v.RecipeId == firstTorch.Id);
            yield return BrowserPointer(exactOutput, control: true);
            Check(TextContains("Missing ingredients") && game.Crafting.Grid.Slots.All(s => s.Empty) && game.Inventory.Total(otherFuel) == 1,
                "Ctrl-click in recipe details respects the exact shown variant and fails without partial changes");
            game.UI.CloseBrowserRecipe(); yield return null;
            yield return BrowserPointer(Item(BlockId.Torch), control: true);
            Check(game.Crafting.Preview?.Output.Id == BlockId.Torch && game.Crafting.Grid.Total(otherFuel) == 1,
                "Ctrl-click in the sidebar chooses an available compatible recipe variant");
            for (int i = 0; i < game.Crafting.Grid.Count; i++) game.Crafting.Grid.Take(i, int.MaxValue);
            for (int i = 0; i < game.Inventory.Count; i++) game.Inventory.Take(i, int.MaxValue);
            Search().text = "";
            // A processing output must not transfer a different grid recipe for the same item.
            game.Inventory.Add(BlockId.IronBlock, 1); game.UI.InspectBrowserItem(BlockId.IronIngot, false); yield return null;
            string furnaceRecipeId = game.Processing.Find(BlockId.RawIron).Id;
            for (int i = 0; i < 4 && !game.UI.GetComponentsInChildren<BrowserItemView>().Any(v => v.RecipeId == furnaceRecipeId); i++)
            {
                var current = game.UI.GetComponentsInChildren<Transform>().Single(t => t.name == "Recipe detail");
                yield return BrowserPointer(current.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == "›"));
            }
            yield return BrowserPointer(game.UI.GetComponentsInChildren<BrowserItemView>().Single(v => v.RecipeId == furnaceRecipeId), control: true);
            Check(TextContains("No grid recipe is available") && game.Inventory.Total(BlockId.IronBlock) == 1 && game.Crafting.Grid.Slots.All(s => s.Empty),
                "Ctrl-click on a smelting output cannot silently unpack a metal block through an unrelated grid recipe");
            game.UI.CloseBrowserRecipe(); game.Inventory.Take(game.Inventory.FindSlot(s => s.Id == BlockId.IronBlock), 1);
            // Open real placed stations so the sidebar is checked against each inventory layout.
            game.SetMode(ScreenMode.Play); var savedPosition = game.Player.transform.position;
            game.Player.enabled = false;
            var fixture = game.World.Address(savedPosition).Offset(0, 12, 0);
            game.Player.transform.position = game.World.Local(fixture) + new Vector3(.5f, 0, .5f);
            game.Player.Camera.transform.position = game.Player.transform.position + Vector3.up * 1.64f;
            yield return Settle(60);
            var stationPosition = fixture.Offset(0, 0, 2);
            foreach (byte stationId in new[] { BlockId.Workbench, IndustryId.Bench, BlockId.Furnace, BlockId.Chest, IndustryId.Crusher })
            {
                byte existing = game.World.Get(stationPosition);
                if (existing != 0) game.World.Remove(stationPosition, existing);
                Check(game.World.Place(stationPosition, stationId), "Place browser fixture: " + game.Registry.Get(stationId).displayName);
                game.Player.Camera.transform.LookAt(game.World.Local(stationPosition) + Vector3.one * .5f);
                Check(stationId == IndustryId.Crusher ? game.TryOpenMachine(stationPosition) : game.TryOpenStation(stationPosition), "Open actual station with sidebar: " + stationId);
                yield return null; yield return null;
                Check(Search() != null && game.UI.GetComponentsInChildren<BrowserItemView>().Any(v => v.name.StartsWith("Browse ")), "Sidebar remains available at station " + stationId);
                foreach (var slot in game.UI.GetComponentsInChildren<SlotView>())
                {
                    var bounds = new Vector3[4]; ((RectTransform)slot.transform).GetWorldCorners(bounds);
                    Check(bounds.All(c => c.x >= 0 && c.x < Screen.width * .77f && c.y >= 0 && c.y <= Screen.height), "Station slot remains inside inventory column: " + stationId + "/" + slot.Index);
                }
                if (stationId == BlockId.Workbench || stationId == IndustryId.Bench)
                {
                    byte outputItem = stationId == BlockId.Workbench ? BlockId.WoodPickaxe : IndustryId.Crusher;
                    var toFill = game.Recipes.Recipes.Single(r => r.Output.Id == outputItem);
                    foreach (var ingredient in toFill.Ingredients) if (!ingredient.Empty) game.Inventory.Add(ingredient.Id, ingredient.Count);
                    game.UI.InspectBrowserItem(outputItem, false); yield return null;
                    if (stationId == BlockId.Workbench) yield return BrowserPointer(Named("FILL GRID"));
                    else yield return BrowserPointer(game.UI.GetComponentsInChildren<BrowserItemView>().Single(v => v.RecipeId == toFill.Id), control: true);
                    Check(game.Crafting.Preview?.Id == toFill.Id && game.Inventory.Total(outputItem) == 0,
                        "Fill prepares the actual open " + game.Crafting.Grid.Size + " × " + game.Crafting.Grid.Size + " bench without crafting the output");
                    yield return Capture(stationId == BlockId.Workbench ? "ctrl-fill-workbench" : "ctrl-fill-machinist");
                }
                if (stationId == IndustryId.Bench) { Check(game.Crafting.Grid.Size == 4, "Machinist retains its 4 × 4 crafting grid"); yield return Capture("machinist-sidebar"); }
                if (stationId == BlockId.Furnace) { yield return Capture("furnace-sidebar"); yield return BrowserPointer(Named("RECIPES & FUEL")); Check(TextContains("Used here as the station"), "Furnace guide opens station uses in the shared browser"); game.UI.CloseBrowserRecipe(); }
                game.SetMode(ScreenMode.Play); game.World.Remove(stationPosition, stationId);
            }
            for (int i = 0; i < game.Inventory.Count; i++) game.Inventory.Take(i, int.MaxValue);
            game.Player.transform.position = savedPosition; game.Player.ResetMotion(); game.Player.enabled = true;
            game.SetMode(ScreenMode.Inventory); yield return null; yield return null;
            // Verify an actual small-window render, then restore the acceptance resolution.
            Screen.SetResolution(1024, 768, false); yield return new WaitForSecondsRealtime(1);
            yield return Capture("sidebar-1024x768");
            var sidebar = game.UI.GetComponentsInChildren<Transform>().Single(t => t.name == "Item browser");
            var corners = new Vector3[4]; ((RectTransform)sidebar).GetWorldCorners(corners);
            Check(corners.All(c => c.x >= 0 && c.x <= Screen.width && c.y >= 0 && c.y <= Screen.height), "Sidebar stays inside the 1024 × 768 window");
            Screen.SetResolution(1280, 720, false); yield return new WaitForSecondsRealtime(.5f);
            game.SetMode(ScreenMode.Play);
        }
    }
}
