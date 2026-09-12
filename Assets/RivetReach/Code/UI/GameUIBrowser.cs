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
        bool inventoryShellBuilt,inventorySidebarBuilt,inventoryCursorBuilt;
        static readonly Unity.Profiling.ProfilerMarker shellMarker=new Unity.Profiling.ProfilerMarker("RivetReach.UI.BuildInventoryShell");
        void BuildItemBrowserInventory()
        {
            BuildInventoryShell();
            if(!inventorySidebarBuilt){BuildItemSidebar();inventorySidebarBuilt=true;}
            BuildInventoryCursor();
        }
        void BuildInventoryShell()
        {
            if(inventoryShellBuilt)return;
            using var measurement=shellMarker.Auto();
            inventoryShellBuilt=true;
            EnsureBrowserIndex();
            Panel(root, 0, 0, 1280, 720, new Color(0, 0, 0, .36f));
            var p = Panel(root, 18, 42, 948, 634, ink); browserHost = p.rectTransform;
            currentScreen.inventoryTitle=Label(p.transform,"INVENTORY",28,20,620,44,28);
            Label(p.transform,"Drag stacks · Right-drag: place one per slot · Shift-click: transfer",28,70,880,25,14,gold);
            Button(p.transform,"CLOSE",822,22,104,38,()=>game.SetMode(ScreenMode.Play));
            currentScreen.creativeToggle=Button(p.transform,"CRAFTING",658,22,148,38,()=>{creativeCrafting=!creativeCrafting;Rebuild();});
            var backdrop = Panel(p.transform, 28, 113, 210, 270, slate);
            var portrait = Rect(backdrop.transform, "Portrait", 0, 0, 210, 270).gameObject.AddComponent<RawImage>();
            portrait.texture = previewTexture; portrait.uvRect = PortraitUV(210, 270); portrait.gameObject.AddComponent<PortraitDrag>().Owner = this;
            currentScreen.inventoryPortrait = portrait.gameObject;
            BuildStationIllustration(backdrop.transform);
            Label(p.transform, "BACKPACK", 250, 103, 380, 20, 12, gold);
            for (int row = 0; row < Inventory.MainRows; row++) for (int col = 0; col < Inventory.MainColumns; col++) Slot(p.transform, Inventory.HotbarCount + row * Inventory.MainColumns + col, 250 + col * 50, 131 + row * 50, 44);
            BuildEquipment(p.transform);
            // Existing station controls retain their own layouts inside this bounded column.
            currentScreen.stationHost=Rect(p.transform,"Station controls",-14,28,1170,634);currentScreen.stationHost.localScale=Vector3.one*.82f;
            IsolateCanvas(currentScreen.stationHost,true);
            Label(p.transform, "HOTBAR", 250, 500, 400, 24, 14, gold); BuildHotbar(p.transform, 250, 534, 42, 4);
            Button(p.transform, "APPEARANCE", 28, 534, 210, 40, () => game.SetMode(ScreenMode.Appearance));
            var hints=Rect(p.transform,"Inventory hints",28,604,892,24);IsolateCanvas(hints);
            inventoryHint = Label(hints, "Hover an item: R recipes · U uses. Browse all items in the sidebar →", 0, 0, 892, 24, 14);
            tooltip = Label(hints, "", 0, 0, 892, 24, 14, gold);
            currentScreen.commonSlots=slots.Count;
        }
        void BuildInventoryCursor()
        {
            if(inventoryCursorBuilt)return;inventoryCursorBuilt=true;
            heldRoot = Rect(root, "Held stack", 0, 0, 52, 65); IsolateCanvas(heldRoot); heldRoot.gameObject.SetActive(false);
            heldIcon = heldRoot.gameObject.AddComponent<RawImage>(); heldIcon.raycastTarget = false;
            heldLabel = Label(heldRoot, "", 0, 37, 55, 25, 16); heldLabel.alignment = TextAnchor.LowerRight;
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
            IsolateCanvas(panel.rectTransform,true);
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
            browserTip = Label(panel.transform, "Click: recipes · Right-click: uses\nShift-click: fill one recipe\nCtrl+Shift-click: fill maximum", 16, 580, 252, 50, 12, gold);
            IsolateCanvas(browserTip.rectTransform);
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
            if(!constructing){BindingVersion++;EndRightPaint();creativeDrag=default;}
            int pages = Math.Max(1, (browserFiltered.Count + BrowserPageSize - 1) / BrowserPageSize);
            browserPage = Mathf.Clamp(browserPage, 0, pages - 1);
            for (int i = 0; i < browserCells.Count; i++)
            {
                int index = browserPage * BrowserPageSize + i;
                var view = browserCells[i]; view.gameObject.SetActive(index < browserFiltered.Count);
                if (index >= browserFiltered.Count) {BindBrowserIcon(view,default);continue;}
                var item = browserFiltered[index]; BindBrowserIcon(view,new ItemStack(item.runtimeId,1));
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
            ImmediateFeedback(selectable);
            view.Icon = Rect(panel.transform, "Icon", 4, 4, size - 8, size - 8).gameObject.AddComponent<RawImage>(); view.Icon.raycastTarget = false;
            if (!stack.Empty) { view.Icon.texture = BrowserTexture(stack.Id); panel.gameObject.name = "Inspect " + game.Registry.Get(stack.Id).stableId; }
            view.CountLabel=Label(panel.transform,stack.Count>1?stack.Count.ToString():"",1,size-19,size-3,19,13);view.CountLabel.alignment=TextAnchor.LowerRight;
            return view;
        }
        ItemStack creativeDrag;
        public bool BeginCreativeDrag(byte id)
        {
            if(!game.Creative||!HasInventoryBinding||!HeldStack.Empty||id==0)return false;
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
            if (browserTip != null) browserTip.text = (id == 0 ? "Click: recipes · Right-click: uses" : game.Registry.Get(id).displayName) + "\nShift-click: fill one · Ctrl+Shift: max\nRight-click / U: uses" + BrowserDragHint;
        }
        public void InspectBrowserItem(byte id, bool usages)
        {
            if (!HasInventoryBinding || id == 0) return;
            if (browserItem != 0) browserHistory.Push((browserItem, browserUses, recipePage));
            browserSearchField?.DeactivateInputField();
            browserItem = id; browserUses = usages; recipePage = 0; DrawBrowserRecipe();
        }
        public void CloseBrowserRecipe()
        {
            if (recipePanel != null) recipePanel.SetActive(false);
            shownRecipe=null;
            browserItem = 0; recipeTransferStatus = null; browserHistory.Clear(); HoverBrowserItem(0);
        }
        static readonly Unity.Profiling.ProfilerMarker recipeViewMarker = new Unity.Profiling.ProfilerMarker("RivetReach.UI.RecipeView");
        static readonly Unity.Profiling.ProfilerMarker recipeFillMarker = new Unity.Profiling.ProfilerMarker("RivetReach.UI.RecipeFill");
        void DrawBrowserRecipe()
        {
            using var measurement=recipeViewMarker.Auto();
            BindRecipeDetails();
        }
        public void FillBrowserRecipe(byte item, string recipeId = null, bool maximum = false)
        {
            using var measurement = recipeFillMarker.Auto();
            if (!HasInventoryBinding) return;
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
            BrowserRecipe failedRecipe = null;
            foreach (var recipe in candidates)
            {
                var result = game.Crafting.FillRecipe(recipe.Id, game.Inventory, maximum);
                if (result == RecipeFillStatus.Filled || result == RecipeFillStatus.AlreadyReady)
                {
                    CloseBrowserRecipe(); RefreshSlots();
                    if (craftStatus != null) craftStatus.text = result == RecipeFillStatus.AlreadyReady ? "Recipe already ready" : "Recipe placed · Click result to craft";
                    return;
                }
                if (result != RecipeFillStatus.RequiresLargerGrid) { failure = result; failedRecipe = recipe; }
            }
            if (failure == RecipeFillStatus.MissingIngredients && failedRecipe != null)
            {
                // Sidebar shortcuts may try several variants; show the one being diagnosed.
                if (shownRecipe != failedRecipe || !RecipeVisible)
                {
                    InspectBrowserItem(item, false);
                    recipePage=browserIndex.Find(item,false).ToList().IndexOf(failedRecipe);
                    DrawBrowserRecipe();
                }
                RefreshMissingIngredients();
                return;
            }
            if (!RecipeVisible) InspectBrowserItem(item, false);
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
    public sealed class BrowserItemView : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public GameUI Owner; public byte Item; public string RecipeId; public RawImage Icon; public Text CountLabel;
        public GameObject MissingBorder;
        public bool CatalogSource;
        bool dragging;
        long pressVersion=-1;
        public void OnPointerDown(PointerEventData e) {pressVersion=Owner.AcceptWidget(this)?Owner.BindingVersion:-1;}
        public void OnBeginDrag(PointerEventData e)
        {dragging=Owner.AcceptGesture(this,pressVersion)&&CatalogSource&&e.button==PointerEventData.InputButton.Left&&Owner.BeginCreativeDrag(Item);}
        public void OnDrag(PointerEventData e){}
        public void OnEndDrag(PointerEventData e)
        {if(dragging&&Owner.AcceptGesture(this,pressVersion))Owner.EndCreativeDrag(e.pointerCurrentRaycast.gameObject?.GetComponentInParent<SlotView>());dragging=false;}
        void OnDisable(){pressVersion=-1;if(dragging)Owner.EndCreativeDrag(null);dragging=false;}
        public void OnPointerEnter(PointerEventData e) {if(Owner.AcceptWidget(this))Owner.HoverBrowserItem(Item);}
        public void OnPointerExit(PointerEventData e) {if(Owner.AcceptWidget(this))Owner.HoverBrowserItem(0);}
        public void OnPointerClick(PointerEventData e)
        {
            if (Item==0 || !Owner.AcceptGesture(this,pressVersion) || dragging || e.dragging || (e.button != PointerEventData.InputButton.Left && e.button != PointerEventData.InputButton.Right)) return;
            bool shift = Keyboard.current?.shiftKey.isPressed == true, control = Keyboard.current?.ctrlKey.isPressed == true;
            if (e.button == PointerEventData.InputButton.Left && (shift || control))
                Owner.FillBrowserRecipe(Item, RecipeId, shift && control);
            else Owner.InspectBrowserItem(Item, e.button == PointerEventData.InputButton.Right);
        }
    }
    public sealed class BrowserPageScroll : MonoBehaviour, IScrollHandler
    {
        public GameUI Owner;
        public void OnScroll(PointerEventData e) { if (Owner.AcceptWidget(this) && e.scrollDelta.y != 0) Owner.ChangeBrowserPage(e.scrollDelta.y > 0 ? -1 : 1); }
    }
}
