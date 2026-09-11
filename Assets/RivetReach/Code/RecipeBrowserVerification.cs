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
            InputSystem.QueueStateEvent(Keyboard.current, control && shift ? new KeyboardState(Key.LeftCtrl, Key.LeftShift) : control ? new KeyboardState(Key.LeftCtrl) : shift ? new KeyboardState(Key.LeftShift) : new KeyboardState());
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }); yield return null; yield return null;
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Check(hits.Count > 0 && (hits[0].gameObject == target.gameObject || hits[0].gameObject.transform.IsChildOf(target.transform)), "Pointer reaches " + target.name);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }.WithButton(right ? MouseButton.Right : MouseButton.Left)); yield return null; yield return null;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }); yield return null; yield return null;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); yield return null;
        }
        IEnumerator PaintCraftUI(int[] cells)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            var point = CraftPoint(cells[0]);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }); yield return null; yield return null;
            foreach (int cell in cells)
            {
                point = CraftPoint(cell);
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }.WithButton(MouseButton.Right));
                yield return null; yield return null; yield return null;
            }
            // Remaining on a cell does not repeat deposits.
            yield return null; yield return null;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }); yield return null; yield return null;
        }
        IEnumerator MeasureCraftingInterface()
        {
            var lines = new List<string> { "Unity " + Application.unityVersion + " | " + SystemInfo.processorType + " | " + SystemInfo.graphicsDeviceName,
                "Stopwatch action CPU only; Canvas.ForceUpdateCanvases includes managed layout/geometry updates, not subsequent native batching or GPU work.",
                "name,samples,median_ms,p95_ms,max_ms" };
            IEnumerator Measure(string name, int count, System.Action setup, System.Action action)
            {
                var times = new List<double>();
                for (int i = 0; i < count; i++)
                {
                    setup?.Invoke(); yield return null;
                    long start = System.Diagnostics.Stopwatch.GetTimestamp(); action();
                    times.Add((System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
                    yield return null;
                }
                times.Sort(); lines.Add(System.FormattableString.Invariant($"{name},{count},{times[count / 2]:F6},{times[(int)((count - 1) * .95)]:F6},{times[count - 1]:F6}"));
            }
            yield return Measure("open inventory and flush canvas", 12, () => game.SetMode(ScreenMode.Play), () => { game.SetMode(ScreenMode.Inventory); Canvas.ForceUpdateCanvases(); });
            void Prepare()
            {
                game.UI.HeldStack = default;
                for (int i = 0; i < game.Crafting.Grid.Count; i++) game.Crafting.Grid.Take(i, int.MaxValue);
                game.Crafting.Grid.Add(BlockId.Log, 1);
            }
            yield return Measure("craft transaction only", 48, Prepare, () => game.Crafting.CraftToCursor(ref game.UI.HeldStack));
            yield return Measure("craft UI and flush canvas", 48, Prepare, () => { game.UI.ClickSlot(CraftResultSlot, false, false); Canvas.ForceUpdateCanvases(); });
            yield return Measure("open recipe detail and flush canvas", 12, () => game.UI.CloseBrowserRecipe(), () => { game.UI.InspectBrowserItem(BlockId.Workbench, false); Canvas.ForceUpdateCanvases(); });
            game.UI.CloseBrowserRecipe(); game.UI.HeldStack = default;
            for (int i = 0; i < game.Crafting.Grid.Count; i++) game.Crafting.Grid.Take(i, int.MaxValue);
            System.IO.File.WriteAllLines(System.IO.Path.Combine(output, "crafting-timings.csv"), lines);
            yield return MeasureCraftingFrames();
        }
        IEnumerator MeasureCraftingFrames()
        {
            var handles = new List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle>();
            Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable(handles);
            var selected = handles.Select(Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetDescription)
                .Where(d => d.Name.Contains("Canvas") || d.Name.Contains("RivetReach.UI") || d.Name == "Main Thread" || d.Name == "GPU Frame Time" || d.Name == "GC.Alloc" || d.Name == "EventSystem.Update").ToArray();
            var recorders = selected.Select(d => Unity.Profiling.ProfilerRecorder.StartNew(d.Category, d.Name, 1)).ToArray();
            var lines = new List<string> { "phase,counter,unit,samples,median,p95,max", "Raw profiler units are retained; zero or missing GPU counters do not establish GPU cost." };
            try
            {
                foreach (int phase in new[] { 0, 1, 2 })
                {
                    game.UI.HeldStack=default;
                    var samples = recorders.Select(r => new List<long>()).ToArray();
                    for (int frame = 0; frame < 150; frame++)
                    {
                        if (phase==1)
                        {
                            game.UI.HeldStack = default;
                            game.Crafting.Grid.Add(BlockId.Log, 1);
                            game.UI.ClickSlot(CraftResultSlot, false, false);
                        }
                        if(phase==2){game.SetMode(ScreenMode.Play);game.SetMode(ScreenMode.Inventory);}
                        yield return null;
                        if (frame < 30) continue;
                        for (int i = 0; i < recorders.Length; i++) if (recorders[i].Valid) samples[i].Add(recorders[i].LastValue);
                    }
                    for (int i = 0; i < recorders.Length; i++)
                    {
                        var values = samples[i]; if (values.Count == 0) continue; values.Sort();
                        lines.Add($"{(phase==1 ? "craft every frame" : phase==2 ? "open every frame" : "idle inventory")},{selected[i].Name},{selected[i].UnitType},{values.Count},{values[values.Count / 2]},{values[(int)((values.Count - 1) * .95)]},{values[values.Count - 1]}");
                    }
                }
            }
            finally { foreach (var recorder in recorders) recorder.Dispose(); }
            game.UI.HeldStack = default;
            System.IO.File.WriteAllLines(System.IO.Path.Combine(output, "crafting-frame-counters.csv"), lines);
        }
        IEnumerator MeasurePointerFrames()
        {
            int cap=Application.targetFrameRate;
            Application.targetFrameRate=-1;
            var lines=new List<string>{"Unity "+Application.unityVersion+" | "+SystemInfo.processorType+" | "+SystemInfo.graphicsDeviceName,
                "Uncapped moving-pointer workload; 60 warmup + 240 measured frames per phase. Profiler times overlap; frame time is not physical mouse-to-display latency.",
                "phase,counter,median_ms,p95_ms,max_ms"};
            string[] names={"Main Thread","GPU Frame Time","Canvas.GeometryJob","Canvas.BuildBatch","UIEvents.WillRenderCanvases"};
            var handles=new List<Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle>();
            Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable(handles);
            var descriptions=handles.Select(Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetDescription).Where(d=>names.Contains(d.Name)).ToArray();
            var recorders=descriptions.Select(d=>Unity.Profiling.ProfilerRecorder.StartNew(d.Category,d.Name,1)).ToArray();
            Vector2 Center(Component widget)
            {
                var rect=(RectTransform)widget.transform;
                return RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            }
            try
            {
                foreach(string phase in new[]{"menu hover","sidebar hover","held stack motion","crafting hover"})
                {
                    game.UI.HeldStack=default;
                    game.SetMode(phase=="menu hover"?ScreenMode.Pause:ScreenMode.Inventory);
                    Canvas.ForceUpdateCanvases();yield return null;
                    Vector2[] points;
                    if(phase=="menu hover")points=game.UI.VisibleRoot.GetComponentsInChildren<Button>().Select(Center).ToArray();
                    else if(phase=="sidebar hover")points=game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Where(v=>v.Item!=0).Select(Center).ToArray();
                    else points=game.UI.VisibleRoot.GetComponentsInChildren<SlotView>().Where(v=>v.Index<Inventory.SlotCount).Select(Center).ToArray();
                    if(phase=="held stack motion")game.UI.HeldStack=new ItemStack(BlockId.Log,32);
                    var frameTimes=new List<double>();
                    var samples=recorders.Select(r=>new List<double>()).ToArray();
                    for(int frame=0;frame<300;frame++)
                    {
                        if(phase=="crafting hover")
                        {
                            game.UI.HeldStack=default;game.Crafting.Grid.Add(BlockId.Log,1);
                            game.UI.ClickSlot(CraftResultSlot,false,false);
                        }
                        InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=points[frame%points.Length]});
                        yield return null;
                        if(frame<60)continue;
                        frameTimes.Add(Time.unscaledDeltaTime*1000.0);
                        for(int i=0;i<recorders.Length;i++)if(recorders[i].Valid)samples[i].Add(recorders[i].LastValue/1000000.0);
                    }
                    void Record(string name,List<double> values)
                    {
                        if(values.Count==0)return;values.Sort();
                        lines.Add(System.FormattableString.Invariant($"{phase},{name},{values[values.Count/2]:F6},{values[(int)((values.Count-1)*.95)]:F6},{values[values.Count-1]:F6}"));
                    }
                    Record("Frame interval",frameTimes);
                    for(int i=0;i<recorders.Length;i++)Record(descriptions[i].Name,samples[i]);
                }
            }
            finally
            {
                foreach(var recorder in recorders)recorder.Dispose();
                Application.targetFrameRate=cap;game.UI.HeldStack=default;
            }
            System.IO.File.WriteAllLines(System.IO.Path.Combine(output,"pointer-timings.csv"),lines);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=Vector2.zero});yield return null;
        }
        IEnumerator ReviewRecipeBrowser()
        {
            game.Mobs.enabled = false; game.Diagnostics = false;
            game.SetMode(ScreenMode.Inventory); yield return null; yield return null;
            yield return MeasurePointerFrames();
            yield return MeasureCraftingInterface();
            InputField Search() => game.UI.VisibleRoot.GetComponentsInChildren<InputField>().Single(f => f.name == "Item browser search");
            BrowserItemView Item(byte id) => game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().First(v => v.Item == id && v.name.StartsWith("Browse "));
            Button Named(string name) => game.UI.VisibleRoot.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == name);
            bool TextContains(string value) => game.UI.VisibleRoot.GetComponentsInChildren<Text>().Any(t => t.text.Contains(value));
            var seen = new HashSet<byte>();
            for (int page = 0; page < (game.Registry.items.Length + 59) / 60; page++)
            {
                var cells = game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Where(v => v.name.StartsWith("Browse ")).ToArray();
                Check(cells.Length <= 60 && cells.All(v => v.Icon.texture != null), "Sidebar page has at most 60 rendered, textured icons");
                foreach (var cell in cells) seen.Add(cell.Item);
                game.UI.ChangeBrowserPage(1); yield return null;
            }
            Check(seen.SetEquals(game.Registry.items.Select(i => i.runtimeId)), "Every registered item is reachable across sidebar pages");
            game.UI.ChangeBrowserPage(-100); yield return null;
            yield return Capture("sidebar-inventory");
            Search().text = "RAW IRON"; yield return null;
            Check(game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Count(v => v.name.StartsWith("Browse ")) == 1, "Search matches case-insensitive multiword item names");
            long inventoryRevision = game.Inventory.Revision, gridRevision = game.Crafting.Grid.Revision;
            yield return BrowserPointer(Item(BlockId.RawIron), right: true);
            Check(TextContains("Furnace") && TextContains("1 / 2"), "Right-click shows raw iron uses including direct smelting");
            yield return Capture("raw-iron-uses");
            // Recipe navigation arrows are scoped to the detail panel, independently of item pages.
            var detail = game.UI.VisibleRoot.GetComponentsInChildren<Transform>().Single(t => t.name == "Recipe detail");
            var next = detail.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == "›");
            yield return BrowserPointer(next);
            Check(TextContains("Crusher") && TextContains("160 W") && TextContains("5 seconds"), "Next use shows crusher, duration and full-power requirement");
            yield return Capture("crusher-recipe");
            var machine = game.UI.VisibleRoot.GetComponentsInChildren<Transform>().Single(t => t.name == "Recipe detail");
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
                var current = game.UI.VisibleRoot.GetComponentsInChildren<Transform>().Single(t => t.name == "Recipe detail");
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
            Check(TextContains("No matching items") && !game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Any(v => v.name.StartsWith("Browse ")), "Empty search hides all cells and shows useful feedback");
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
            Check(TextContains("Iron ingot") && game.UI.VisibleRoot.GetComponentsInChildren<Transform>().Any(t => t.name == "Recipe detail") && game.Inventory.Total(BlockId.IronIngot) == 3, "R on an inventory stack opens recipes without moving it");
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
            var exactOutput = game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Single(v => v.RecipeId == firstTorch.Id);
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
            for (int i = 0; i < 4 && !game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Any(v => v.RecipeId == furnaceRecipeId); i++)
            {
                var current = game.UI.VisibleRoot.GetComponentsInChildren<Transform>().Single(t => t.name == "Recipe detail");
                yield return BrowserPointer(current.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == "›"));
            }
            yield return BrowserPointer(game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Single(v => v.RecipeId == furnaceRecipeId), control: true);
            Check(TextContains("No grid recipe is available") && game.Inventory.Total(BlockId.IronBlock) == 1 && game.Crafting.Grid.Slots.All(s => s.Empty),
                "Ctrl-click on a smelting output cannot silently unpack a metal block through an unrelated grid recipe");
            game.UI.CloseBrowserRecipe(); game.Inventory.Take(game.Inventory.FindSlot(s => s.Id == BlockId.IronBlock), 1);
            for (int i = 0; i < game.Inventory.Count; i++) game.Inventory.Take(i, int.MaxValue);
            game.Crafting.ReturnIngredients(game.Inventory);
            game.Inventory.Add(BlockId.Planks, 19);
            Search().text = "workbench"; yield return null;
            yield return BrowserPointer(Item(BlockId.Workbench), shift: true);
            Check(game.Crafting.MaximumCrafts == 1 && game.Crafting.Grid.Total(BlockId.Planks) == 4, "Shift-click sidebar prepares one correctly arranged recipe");
            yield return BrowserPointer(Item(BlockId.Workbench), shift: true, control: true);
            Check(game.Crafting.MaximumCrafts == 4 && game.Inventory.Total(BlockId.Planks) == 3, "Ctrl+Shift-click tops up the ready grid to the maximum complete batch");
            yield return Capture("maximum-recipe-fill");
            game.Crafting.ReturnIngredients(game.Inventory);
            yield return ClickCraftUI(game.Inventory.FindSlot(s => s.Id == BlockId.Planks));
            yield return PaintCraftUI(new[] { CraftCell, CraftCell + 1, CraftCell + 3, CraftCell + 2, CraftCell });
            Check(game.Crafting.Preview?.Output.Id == BlockId.Workbench && game.Crafting.Grid.Slots.All(s => s.Count == 1) && game.UI.HeldStack.Count == 15,
                "Held right-drag paints each crossed crafting cell once without a release deposit or revisit duplication");
            yield return Capture("right-drag-pattern");
            game.UI.ReturnHeld();
            for (int i = 0; i < game.Inventory.Count; i++) game.Inventory.Take(i, int.MaxValue);
            game.UI.HeldStack = new ItemStack(BlockId.Planks, 2);
            yield return PaintCraftUI(new[] { CraftCell, CraftCell + 1, CraftCell + 3, CraftCell });
            Check(game.UI.HeldStack.Empty && game.Crafting.Grid.Total(BlockId.Planks) == 2 && game.Crafting.Grid.Slots[3].Empty,
                "Exhausted right-drag cannot pick placed ingredients back up");
            game.Crafting.ReturnIngredients(game.Inventory);
            for (int i = 0; i < game.Inventory.Count; i++) game.Inventory.Take(i, int.MaxValue);
            game.Crafting.Grid.Add(BlockId.Log, 1, 1, 2); game.Crafting.Grid.Add(BlockId.Planks, 64, 2, 3);
            game.UI.HeldStack = new ItemStack(BlockId.Planks, 4);
            yield return PaintCraftUI(new[] { CraftCell, CraftCell + 1, CraftCell + 2, CraftCell + 3 });
            Check(game.UI.HeldStack.Count == 2 && game.Crafting.Grid.Slots[1].Id == BlockId.Log && game.Crafting.Grid.Slots[2].Count == 64,
                "Right-drag skips incompatible and full cells without swapping or losing held items");
            yield return ClickCraftUI(CraftCell, right: true);
            Check(game.UI.HeldStack.Count == 1 && game.Crafting.Grid.Slots[0].Count == 2, "A new right press can deposit into a previously visited cell");
            game.UI.ReturnHeld();
            for (int i = 0; i < game.Inventory.Count; i++) game.Inventory.Take(i, int.MaxValue);
            game.Inventory.Add(BlockId.Planks, 7, 12, 13);
            yield return PaintCraftUI(new[] { 12, CraftCell, CraftCell + 1 });
            Check(game.Inventory.Slots[12].Count == 3 && game.Crafting.Grid.Total(BlockId.Planks) == 2 && game.UI.HeldStack.Count == 2,
                "Right-drag starting empty-handed splits the source once and paints the picked-up half");
            game.UI.ReturnHeld();
            for (int i = 0; i < game.Inventory.Count; i++) game.Inventory.Take(i, int.MaxValue);
            Search().text = "";
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
                Check(Search() != null && game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Any(v => v.name.StartsWith("Browse ")), "Sidebar remains available at station " + stationId);
                foreach (var slot in game.UI.VisibleRoot.GetComponentsInChildren<SlotView>())
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
                    else yield return BrowserPointer(game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Single(v => v.RecipeId == toFill.Id), control: true);
                    Check(game.Crafting.Preview?.Id == toFill.Id && game.Inventory.Total(outputItem) == 0,
                        "Fill prepares the actual open " + game.Crafting.Grid.Size + " × " + game.Crafting.Grid.Size + " bench without crafting the output");
                    foreach (var ingredient in toFill.Ingredients) if (!ingredient.Empty) game.Inventory.Add(ingredient.Id, ingredient.Count * 2);
                    game.UI.InspectBrowserItem(outputItem, false); yield return null;
                    yield return BrowserPointer(game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Single(v => v.RecipeId == toFill.Id), shift: true, control: true);
                    Check(game.Crafting.Preview?.Id == toFill.Id && game.Crafting.MaximumCrafts == 3, "Exact recipe Ctrl+Shift-click fills maximum at bench size " + game.Crafting.Grid.Size);
                    yield return Capture(stationId == BlockId.Workbench ? "ctrl-fill-workbench" : "ctrl-fill-machinist");
                }
                if (stationId == BlockId.Furnace || stationId == BlockId.Chest || stationId == IndustryId.Crusher)
                {
                    game.Inventory.Add(BlockId.Planks, 4); Search().text = "workbench"; yield return null;
                    long bagRevision = game.Inventory.Revision, craftRevision = game.Crafting.Grid.Revision;
                    yield return BrowserPointer(Item(BlockId.Workbench), shift: true);
                    yield return BrowserPointer(Item(BlockId.Workbench), shift: true, control: true);
                    Check(game.Inventory.Revision == bagRevision && game.Crafting.Grid.Revision == craftRevision && TextContains("Open a crafting grid"),
                        "Fill shortcuts reject non-crafting station " + stationId + " without moving materials");
                    Search().text = "";
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
            var sidebar = game.UI.VisibleRoot.GetComponentsInChildren<Transform>().Single(t => t.name == "Item browser");
            var corners = new Vector3[4]; ((RectTransform)sidebar).GetWorldCorners(corners);
            Check(corners.All(c => c.x >= 0 && c.x <= Screen.width && c.y >= 0 && c.y <= Screen.height), "Sidebar stays inside the 1024 × 768 window");
            Screen.SetResolution(1280, 720, false); yield return new WaitForSecondsRealtime(.5f);
            game.SetMode(ScreenMode.Play);
            yield return ReviewScreenReuse();
        }
    }
}
