using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        const int BrowserPageSize = 60;
        RecipeBrowserIndex browserIndex;
        readonly Dictionary<byte, ItemBrowserSettings.Entry> browserSettings = new Dictionary<byte, ItemBrowserSettings.Entry>();
        ItemDefinition[] browserItems;
        readonly List<ItemDefinition> browserFiltered = new List<ItemDefinition>();
        readonly List<BrowserItemView> browserCells = new List<BrowserItemView>();
        readonly Stack<(byte item, bool uses, int page)> browserHistory = new Stack<(byte, bool, int)>();
        string browserSearch = "";
        int browserPage, recipePage;
        byte browserItem, browserHovered;
        bool browserUses;
        RectTransform browserHost;
        GameObject recipePanel;
        Text browserPageLabel, browserTip, recipeTransferStatus;
        Button browserPrevious, browserNext;
        InputField browserSearchField;
        public bool EditingText => EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() is InputField input && input.isFocused;

        void ResetBrowserUI()
        {
            creativeDrag = default;
            browserHost = null; recipePanel = null; recipeTransferStatus = null; browserTip = null; browserSearchField = null;
            browserCells.Clear(); browserHovered = 0; browserItem = 0; browserHistory.Clear();
        }
        void EnsureBrowserIndex()
        {
            if (browserIndex != null) return;
            browserIndex = new RecipeBrowserIndex(game.Registry, game.Recipes, game.Processing);
            var settings = Resources.Load<ItemBrowserSettings>("Definitions/ItemBrowser");
            if (settings != null) foreach (var entry in settings.entries)
            {
                byte id = game.Registry.ResolveId(entry.itemId);
                if (browserSettings.ContainsKey(id)) throw new InvalidOperationException("Duplicate browser entry: " + entry.itemId);
                browserSettings.Add(id, entry);
            }
            browserItems = game.Registry.items.OrderBy(i => browserSettings.TryGetValue(i.runtimeId, out var entry) ? entry.sortOrder : 0)
                .ThenBy(i => i.displayName, StringComparer.OrdinalIgnoreCase).ThenBy(i => i.stableId, StringComparer.Ordinal).ToArray();
        }
        void BuildItemBrowserInventory()
        {
            EnsureBrowserIndex();
            Panel(root, 0, 0, 1280, 720, new Color(0, 0, 0, .36f));
            var p = Panel(root, 18, 42, 948, 634, ink); browserHost = p.rectTransform;
            string title = game.OpenMachine != null ? game.OpenMachine.Definition.Name : game.OpenStation != null ?
                game.Registry.Get(game.OpenStation.Block).displayName : game.Creative ? "Creative inventory" : "Inventory";
            Label(p.transform, title.ToUpperInvariant(), 28, 20, 620, 44, 28);
            Label(p.transform, "Drag stacks · Right-click split / place one · Shift-click transfer", 28, 70, 880, 25, 14, gold);
            Button(p.transform, "CLOSE", 822, 22, 104, 38, () => game.SetMode(ScreenMode.Play));
            if (game.Creative && game.OpenStation == null && game.OpenMachine == null)
                Button(p.transform, creativeCrafting ? "ALL ITEMS" : "CRAFTING", 658, 22, 148, 38, () => { creativeCrafting = !creativeCrafting; Rebuild(); });
            var backdrop = Panel(p.transform, 28, 113, 210, 270, slate);
            var portrait = Rect(backdrop.transform, "Portrait", 0, 0, 210, 270).gameObject.AddComponent<RawImage>();
            portrait.texture = previewTexture; portrait.uvRect = PortraitUV(210, 270); portrait.gameObject.AddComponent<PortraitDrag>().Owner = this;
            Label(p.transform, "BACKPACK", 250, 103, 380, 20, 12, gold);
            for (int row = 0; row < 6; row++) for (int col = 0; col < 8; col++) Slot(p.transform, 12 + row * 8 + col, 250 + col * 50, 131 + row * 50, 44);
            BuildEquipment(p.transform);
            // Existing station controls retain their own layouts inside this bounded column.
            var station = Rect(p.transform, "Station controls", -14, 28, 1170, 634); station.localScale = Vector3.one * .82f;
            if (game.OpenMachine != null) BuildMachine(station);
            else if (game.OpenStation?.Furnace != null) BuildFurnace(station);
            else if (game.OpenStation?.Storage != null) BuildChest(station);
            else if (game.Creative && game.OpenStation == null && !creativeCrafting) BuildCreativeCatalog(station);
            else BuildCraftingStation(station);
            Label(p.transform, "HOTBAR", 250, 500, 400, 24, 14, gold); BuildHotbar(p.transform, 250, 534, 42, 4);
            Button(p.transform, "APPEARANCE", 28, 534, 210, 40, () => game.SetMode(ScreenMode.Appearance));
            inventoryHint = Label(p.transform, "Hover an item: R recipes · U uses. Browse all items in the sidebar →", 28, 604, 892, 24, 14);
            tooltip = Label(p.transform, "", 28, 604, 892, 24, 14, gold);
            BuildItemSidebar();
            heldRoot = Rect(root, "Held stack", 0, 0, 52, 65); heldRoot.gameObject.AddComponent<Canvas>(); heldRoot.gameObject.SetActive(false);
            heldIcon = heldRoot.gameObject.AddComponent<RawImage>(); heldIcon.raycastTarget = false;
            heldLabel = Label(heldRoot, "", 0, 37, 55, 25, 16); heldLabel.alignment = TextAnchor.LowerRight;
            RefreshPreview();
        }
        void BuildCraftingStation(Transform parent)
        {
            int size = game.Crafting.Grid.Size, cell = size == 2 ? 60 : size == 4 ? 38 : 48, gap = size == 2 ? 70 : size == 4 ? 43 : 56;
            Label(parent, "CRAFTING", 821, 115, 300, 42, 27);
            Label(parent, size == 2 ? "Personal · 2 × 2" : size == 4 ? "Machinist · 4 × 4" : "Workbench · 3 × 3", 821, 162, 300, 30, 17, gold);
            for (int row = 0; row < size; row++) for (int col = 0; col < size; col++) Slot(parent, CraftSlotStart + row * size + col, 821 + col * gap, 222 + row * gap, cell);
            Label(parent, "→", 995, 268, 27, 40, 26, gold); Slot(parent, CraftOutputSlot, 1030, 256, 64);
            craftOutputName = Label(parent, "", 1010, 333, 125, 44, 14, gold); craftOutputName.alignment = TextAnchor.UpperCenter;
            craftStatus = Label(parent, "", 821, 407, 310, 40, 16, gold);
            Label(parent, "Click result: craft one\nShift-click result: craft all that fit", 821, 455, 310, 44, 16);
            Button(parent, "RETURN INGREDIENTS", 821, 519, 303, 36, () => { game.Crafting.ReturnIngredients(game.Inventory); RefreshSlots(); });
            if (size == 2) Label(parent, "4 planks → Workbench for 3 × 3", 821, 194, 318, 26, 13, gold);
        }
        void BuildItemSidebar()
        {
            var panel = Panel(root, 978, 42, 284, 634, ink); panel.gameObject.name = "Item browser";
            Label(panel.transform, "ITEMS", 16, 16, 250, 30, 23);
            var searchPanel = Panel(panel.transform, 14, 57, 214, 35, slate);
            browserSearchField = searchPanel.gameObject.AddComponent<InputField>(); browserSearchField.name = "Item browser search";
            browserSearchField.textComponent = Label(searchPanel.transform, "", 9, 7, 196, 25, 15);
            browserSearchField.placeholder = Label(searchPanel.transform, "Search items…", 9, 7, 196, 25, 15, gold);
            browserSearchField.characterLimit = 80; browserSearchField.text = browserSearch;
            Button(panel.transform, "×", 234, 57, 36, 35, () => browserSearchField.text = "");
            var grid = Rect(panel.transform, "Item pages", 16, 108, 252, 420);
            var wheel = grid.gameObject.AddComponent<BrowserPageScroll>(); wheel.Owner = this;
            for (int i = 0; i < BrowserPageSize; i++) { var view=BrowserIcon(grid, default, i % 6 * 42, i / 6 * 42, 38); view.CatalogSource=true; browserCells.Add(view); }
            browserPrevious = Button(panel.transform, "‹", 14, 539, 38, 34, () => ChangeBrowserPage(-1));
            browserNext = Button(panel.transform, "›", 232, 539, 38, 34, () => ChangeBrowserPage(1));
            browserPageLabel = Label(panel.transform, "", 54, 542, 176, 30, 13, gold); browserPageLabel.alignment = TextAnchor.MiddleCenter;
            browserTip = Label(panel.transform, "Click: recipes\nShift / right-click: uses\nCtrl-click: fill crafting grid", 16, 580, 252, 50, 12, gold);
            browserSearchField.onValueChanged.AddListener(value => { browserSearch = value; browserPage = 0; FilterBrowser(); });
            FilterBrowser();
        }
        void FilterBrowser()
        {
            browserFiltered.Clear();
            string[] terms = browserSearch.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in browserItems)
            {
                string text = item.displayName + " " + item.stableId + " " + (browserSettings.TryGetValue(item.runtimeId, out var entry) ? entry.searchKeywords : "");
                if (terms.All(term => text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)) browserFiltered.Add(item);
            }
            PopulateBrowserPage();
        }
        public void ChangeBrowserPage(int delta) { browserPage += delta; PopulateBrowserPage(); }
        void PopulateBrowserPage()
        {
            int pages = Math.Max(1, (browserFiltered.Count + BrowserPageSize - 1) / BrowserPageSize);
            browserPage = Mathf.Clamp(browserPage, 0, pages - 1);
            for (int i = 0; i < browserCells.Count; i++)
            {
                int index = browserPage * BrowserPageSize + i;
                var view = browserCells[i]; view.gameObject.SetActive(index < browserFiltered.Count);
                if (index >= browserFiltered.Count) continue;
                var item = browserFiltered[index]; view.Item = item.runtimeId; view.Icon.texture = BrowserTexture(item.runtimeId);
                view.gameObject.name = "Browse " + item.stableId;
            }
            browserPageLabel.text = browserFiltered.Count == 0 ? "No matching items" : $"{browserPage + 1} / {pages} · {browserFiltered.Count} items";
            browserPrevious.interactable = browserPage > 0; browserNext.interactable = browserPage < pages - 1;
            HoverBrowserItem(0);
        }
        Texture2D BrowserTexture(byte id) => browserSettings.TryGetValue(id, out var entry) && entry.icon != null ? entry.icon : icons[id];
        BrowserItemView BrowserIcon(Transform parent, ItemStack stack, float x, float y, int size)
        {
            var panel = Panel(parent, x, y, size, size, slate);
            var view = panel.gameObject.AddComponent<BrowserItemView>(); view.Owner = this; view.Item = stack.Id;
            var selectable = panel.gameObject.AddComponent<Selectable>(); selectable.targetGraphic = panel;
            view.Icon = Rect(panel.transform, "Icon", 4, 4, size - 8, size - 8).gameObject.AddComponent<RawImage>(); view.Icon.raycastTarget = false;
            if (!stack.Empty) { view.Icon.texture = BrowserTexture(stack.Id); panel.gameObject.name = "Inspect " + game.Registry.Get(stack.Id).stableId; }
            if (stack.Count > 1) Label(panel.transform, stack.Count.ToString(), 1, size - 19, size - 3, 19, 13).alignment = TextAnchor.LowerRight;
            return view;
        }
        ItemStack creativeDrag;
        public bool BeginCreativeDrag(byte id)
        {
            if(!game.Creative||!game.InventoryOpen||!HeldStack.Empty||id==0)return false;
            creativeDrag=new ItemStack(id,game.Registry.Get(id).stackLimit);return true;
        }
        public void EndCreativeDrag(SlotView slot)
        {
            byte id=creativeDrag.Id;creativeDrag=default;
            if(id==0||slot==null||slot.Owner!=this)return;
            if(!game.TryGiveCreativeItemToSlot(id,slot.Index)&&browserTip!=null)
                browserTip.text="Drop into an empty inventory slot\nor a matching stack with room.";
            RefreshSlots();
        }
        string BrowserDragHint=>game.Creative?" · Drag: give stack":"";
        public void HoverBrowserItem(byte id)
        {
            browserHovered = id;
            if (browserTip != null) browserTip.text = (id == 0 ? "Click: recipes · Right-click: uses" : game.Registry.Get(id).displayName) + "\nCtrl-click: fill crafting grid\nShift-click: uses" + BrowserDragHint;
        }
        public void InspectBrowserItem(byte id, bool usages)
        {
            if (!game.InventoryOpen || id == 0) return;
            if (browserItem != 0) browserHistory.Push((browserItem, browserUses, recipePage));
            browserSearchField?.DeactivateInputField();
            browserItem = id; browserUses = usages; recipePage = 0; DrawBrowserRecipe();
        }
        public void CloseBrowserRecipe()
        {
            if (recipePanel != null) { recipePanel.SetActive(false); Destroy(recipePanel); recipePanel = null; }
            browserItem = 0; recipeTransferStatus = null; browserHistory.Clear(); HoverBrowserItem(0);
        }
        void DrawBrowserRecipe()
        {
            if (recipePanel != null) { recipePanel.SetActive(false); Destroy(recipePanel); }
            HoverBrowserItem(0); hoveredSlot = -1; RefreshTooltip();
            var panel = Panel(browserHost, 246, 104, 688, 418, new Color(ink.r, ink.g, ink.b, 1)); recipePanel = panel.gameObject; recipePanel.name = "Recipe detail";
            // Opaque, raycastable backing prevents clicks from falling through to held stacks or the grid.
            Panel(panel.transform, 0, 0, 688, 3, gold);
            var back = Button(panel.transform, "‹ BACK", 12, 14, 98, 32, () =>
            {
                var previous = browserHistory.Pop(); browserItem = previous.item; browserUses = previous.uses; recipePage = previous.page; DrawBrowserRecipe();
            }); back.interactable = browserHistory.Count > 0;
            Label(panel.transform, game.Registry.Get(browserItem).displayName, 124, 15, 424, 30, 22);
            Button(panel.transform, "DONE", 578, 14, 98, 32, CloseBrowserRecipe);
            Button(panel.transform, "RECIPES", 12, 58, 130, 34, () => { browserUses = false; recipePage = 0; DrawBrowserRecipe(); }, !browserUses);
            Button(panel.transform, "USES", 150, 58, 130, 34, () => { browserUses = true; recipePage = 0; DrawBrowserRecipe(); }, browserUses);
            var matches = browserIndex.Find(browserItem, browserUses);
            recipePage = Mathf.Clamp(recipePage, 0, Math.Max(0, matches.Count - 1));
            var previousButton = Button(panel.transform, "‹", 514, 58, 36, 34, () => { recipePage--; DrawBrowserRecipe(); }); previousButton.interactable = recipePage > 0;
            var nextButton = Button(panel.transform, "›", 640, 58, 36, 34, () => { recipePage++; DrawBrowserRecipe(); }); nextButton.interactable = recipePage < matches.Count - 1;
            Label(panel.transform, matches.Count == 0 ? "0 / 0" : $"{recipePage + 1} / {matches.Count}", 555, 65, 82, 26, 15, gold).alignment = TextAnchor.UpperCenter;
            if (matches.Count == 0)
            {
                BrowserIcon(panel.transform, new ItemStack(browserItem, 1), 30, 130, 64);
                Label(panel.transform, browserUses ? "No registered recipe uses this item." : "No crafting or processing recipe.\nFind this item through exploration or other world interactions.", 116, 134, 532, 88, 19);
                Label(panel.transform, "Browse another item, or select the other tab.", 30, 272, 620, 50, 16, gold); return;
            }
            var recipe = matches[recipePage];
            if (recipe.GridRecipe != null)
                Button(panel.transform, "FILL GRID", 294, 58, 204, 34, () => FillBrowserRecipe(recipe.Output.Id, recipe.Id));
            recipeTransferStatus = Label(panel.transform, "", 16, 140, 658, 17, 12, gold);
            if (recipe.Station != 0) BrowserIcon(panel.transform, new ItemStack(recipe.Station, 1), 16, 106, 34);
            Label(panel.transform, recipe.StationName, recipe.Station == 0 ? 16 : 60, 111, 445, 26, 18, gold);
            string role = recipe.Station == browserItem && browserUses ? "Used here as the station" : recipe.Fuels.Contains(browserItem) && browserUses ? "Used here as fuel" : "";
            Label(panel.transform, role, 410, 116, 260, 26, 13, gold);
            var gridRecipe = recipe.GridRecipe;
            int width = gridRecipe == null ? 1 : gridRecipe.MinimumGridSize;
            int cellCount = gridRecipe == null ? recipe.Ingredients.Count : width * width;
            for (int i = 0; i < cellCount; i++)
            {
                int col = i % width, row = i / width;
                ItemStack stack = default;
                if (gridRecipe == null || gridRecipe.Kind == RecipeKind.Shapeless)
                { if (i < recipe.Ingredients.Count) stack = recipe.Ingredients[i]; }
                else if (col < gridRecipe.Width && row < gridRecipe.Height)
                    stack = recipe.Ingredients[row * gridRecipe.Width + col];
                float x = 24 + col * 42, y = 158 + row * 42;
                if (stack.Empty) Panel(panel.transform, x, y, 38, 38, slate); else BrowserIcon(panel.transform, stack, x, y, 38);
            }
            float centreY = 156 + width * 21;
            Label(panel.transform, "→", 207, centreY - 22, 45, 45, 32, gold);
            BrowserIcon(panel.transform, recipe.Output, 261, centreY - 30, 60).RecipeId = recipe.Id;
            Label(panel.transform, game.Registry.Get(recipe.Output.Id).displayName + " × " + recipe.Output.Count, 232, centreY + 38, 130, 50, 16, gold).alignment = TextAnchor.UpperCenter;
            string method = gridRecipe != null ? gridRecipe.Kind == RecipeKind.Shapeless ? "Any arrangement" : gridRecipe.AllowsMirroring ? "Shown layout or mirror" : "Shown layout" :
                (recipe.Ticks / 20f).ToString("0.#") + " seconds" + (recipe.Watts > 0 ? " · " + recipe.Watts + " W at full power" : " · Requires fuel");
            Label(panel.transform, method, 24, 334, 640, 24, 14, gold);
            Label(panel.transform, "MATERIALS / CRAFT", 386, 155, 276, 24, 13, gold);
            var totals = recipe.Ingredients.Where(s => !s.Empty).GroupBy(s => s.Id).Select(g => new ItemStack(g.Key, g.Sum(s => s.Count))).ToArray();
            var viewport = Panel(panel.transform, 382, 185, 286, 138, ink); viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect(viewport.transform, "Material totals", 0, 0, 282, Math.Max(138, totals.Length * 38));
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport.rectTransform; scroll.content = content; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 25;
            for (int i = 0; i < totals.Length; i++)
            {
                var stack = totals[i]; BrowserIcon(content, stack, 2, i * 38, 32);
                Label(content, stack.Count + " × " + game.Registry.Get(stack.Id).displayName, 42, i * 38 + 6, 234, 30, 14);
            }
            if (recipe.Fuels.Count > 0)
            {
                Label(panel.transform, "FUEL · choose one", 24, 376, 160, 24, 13, gold);
                for (int i = 0; i < recipe.Fuels.Count; i++) BrowserIcon(panel.transform, new ItemStack(recipe.Fuels[i], (recipe.Ticks + game.Processing.FuelTicks(recipe.Fuels[i]) - 1) / game.Processing.FuelTicks(recipe.Fuels[i])), 190 + i * 36, 369, 32);
            }
            else Label(panel.transform, "Click: recipes · Shift-click: uses · Ctrl-click output: fill this recipe", 24, 380, 640, 24, 13, gold);
        }
        public void FillBrowserRecipe(byte item, string recipeId = null)
        {
            if (!game.InventoryOpen) return;
            void Notice(string text)
            {
                if (recipeTransferStatus != null) recipeTransferStatus.text = text;
                if (browserTip != null) browserTip.text = text;
            }
            if (game.OpenMachine != null || game.OpenStation != null && game.OpenStation.Crafting == null ||
                game.Creative && game.OpenStation == null && !creativeCrafting)
            { Notice("Open a crafting grid or crafting bench first."); return; }
            var candidates = browserIndex.Find(item, false).Where(r => r.GridRecipe != null && (recipeId == null || r.Id == recipeId)).ToArray();
            if (candidates.Length == 0) { Notice("No grid recipe is available for this selection."); return; }
            RecipeFillStatus failure = RecipeFillStatus.RequiresLargerGrid;
            foreach (var recipe in candidates)
            {
                var result = game.Crafting.FillRecipe(recipe.Id, game.Inventory);
                if (result == RecipeFillStatus.Filled || result == RecipeFillStatus.AlreadyReady)
                {
                    CloseBrowserRecipe(); RefreshSlots();
                    if (craftStatus != null) craftStatus.text = result == RecipeFillStatus.AlreadyReady ? "Recipe already ready" : "Recipe placed · Click result to craft";
                    return;
                }
                if (result != RecipeFillStatus.RequiresLargerGrid) failure = result;
            }
            if (recipePanel == null) InspectBrowserItem(item, false);
            Notice(failure == RecipeFillStatus.RequiresLargerGrid ? "Requires " + candidates.OrderBy(r => r.GridRecipe.MinimumGridSize).First().StationName :
                failure == RecipeFillStatus.InventoryFull ? "Make room in your inventory for the ingredients already in the grid." :
                "Missing ingredients · Inventory and crafting grid are unchanged.");
        }
        void UpdateBrowserInput()
        {
            if (!game.InventoryOpen || Keyboard.current == null || EditingText) return;
            byte item = browserHovered != 0 ? browserHovered : StackAt(hoveredSlot).Id;
            if (item == 0) return;
            if (Keyboard.current.rKey.wasPressedThisFrame) InspectBrowserItem(item, false);
            else if (Keyboard.current.uKey.wasPressedThisFrame) InspectBrowserItem(item, true);
        }
    }
    public sealed class BrowserItemView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public GameUI Owner; public byte Item; public string RecipeId; public RawImage Icon;
        public bool CatalogSource;
        bool dragging;
        public void OnBeginDrag(PointerEventData e)
        {dragging=CatalogSource&&e.button==PointerEventData.InputButton.Left&&Owner.BeginCreativeDrag(Item);}
        public void OnDrag(PointerEventData e){}
        public void OnEndDrag(PointerEventData e)
        {if(dragging)Owner.EndCreativeDrag(e.pointerCurrentRaycast.gameObject?.GetComponentInParent<SlotView>());dragging=false;}
        void OnDisable(){if(dragging)Owner.EndCreativeDrag(null);dragging=false;}
        public void OnPointerEnter(PointerEventData e) => Owner.HoverBrowserItem(Item);
        public void OnPointerExit(PointerEventData e) => Owner.HoverBrowserItem(0);
        public void OnPointerClick(PointerEventData e)
        {
            if (dragging || e.dragging || (e.button != PointerEventData.InputButton.Left && e.button != PointerEventData.InputButton.Right)) return;
            if (e.button == PointerEventData.InputButton.Left && Keyboard.current?.ctrlKey.isPressed == true)
                Owner.FillBrowserRecipe(Item, RecipeId);
            else Owner.InspectBrowserItem(Item, e.button == PointerEventData.InputButton.Right || Keyboard.current?.shiftKey.isPressed == true);
        }
    }
    public sealed class BrowserPageScroll : MonoBehaviour, IScrollHandler
    {
        public GameUI Owner;
        public void OnScroll(PointerEventData e) { if (e.scrollDelta.y != 0) Owner.ChangeBrowserPage(e.scrollDelta.y > 0 ? -1 : 1); }
    }
}
