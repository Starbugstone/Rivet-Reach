using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        sealed class ScreenWidgets
        {
            public readonly List<SlotView> slots = new List<SlotView>();
            public Text hudMode, hudFlight, hudControls, inventoryTitle;
            public Button creativeToggle;
            public GameObject inventoryPortrait, stationIllustration;
            public RawImage stationImage;
            public Text stationImageName;
            public Canvas canvas;
            public GraphicRaycaster raycaster;
            public readonly List<Canvas> childCanvases=new List<Canvas>();
            public readonly List<GraphicRaycaster> childRaycasters=new List<GraphicRaycaster>();
            public readonly List<Selectable> selectables=new List<Selectable>();
            public int selectableRevision=-1;
            public RectTransform stationHost;
            public int commonSlots;
            public RectTransform root, heldRoot;
            public Text message, diagnostics, targetLabel, heldLabel, selectedLabel, loading, tooltip, worldTime, inventoryHint, healthText, hungerText, armorText;
            public GameObject diagnosticsPanel;
            public Image progress;
            public RawImage heldIcon;
            public long lastRevision, lastCraftRevision, lastStationRevision, lastEquipmentRevision;
            public int lastSelected, hoveredSlot, shownFood, shownArmor;
            public float shownHealth;
            public bool shownEating;
            public ItemStack shownHeld;
        }
        sealed class StationWidgets
        {
            public RectTransform root;
            public Text creativeStatus;
            public InputField bridgeNameField;
            public readonly List<SlotView> slots = new List<SlotView>();
            public readonly List<(Text label, Func<MachineState,string> value)> controls = new List<(Text,Func<MachineState,string>)>();
            public Text craftStatus, craftOutputName, furnaceText, machineStatus, machineDetail;
            public Image cookBar, burnBar, machineProgress;
            public int shownProgress, shownBurn;
            public long shownFurnaceRevision;
            public float nextMachineRefresh;
        }
        ScreenWidgets currentScreen = new ScreenWidgets(), hudScreen, inventoryScreen;
        StationWidgets currentStation = new StationWidgets();
        readonly Dictionary<int,StationWidgets> stationScreens = new Dictionary<int,StationWidgets>();
        Inventory boundInventory;
        CraftingSession boundCrafting;
        StationState boundStation;
        MachineState boundMachine;
        BlockPos boundPosition;
        public long BindingVersion { get; private set; }
        bool constructing, prewarmed, recipeWarmed;
        public RectTransform VisibleRoot => root;
        public int CreatedWidgets { get; private set; }
        public int RetainedStationLayouts => stationScreens.Count;
        List<SlotView> slots => currentScreen.slots;
        RectTransform root { get => currentScreen.root; set => currentScreen.root = value; }
        RectTransform heldRoot { get => currentScreen.heldRoot; set => currentScreen.heldRoot = value; }
        Text message { get => currentScreen.message; set => currentScreen.message = value; }
        Text diagnostics { get => currentScreen.diagnostics; set => currentScreen.diagnostics = value; }
        Text targetLabel { get => currentScreen.targetLabel; set => currentScreen.targetLabel = value; }
        Text heldLabel { get => currentScreen.heldLabel; set => currentScreen.heldLabel = value; }
        Text selectedLabel { get => currentScreen.selectedLabel; set => currentScreen.selectedLabel = value; }
        Text loading { get => currentScreen.loading; set => currentScreen.loading = value; }
        Text tooltip { get => currentScreen.tooltip; set => currentScreen.tooltip = value; }
        Text worldTime { get => currentScreen.worldTime; set => currentScreen.worldTime = value; }
        Text inventoryHint { get => currentScreen.inventoryHint; set => currentScreen.inventoryHint = value; }
        Text healthText { get => currentScreen.healthText; set => currentScreen.healthText = value; }
        Text hungerText { get => currentScreen.hungerText; set => currentScreen.hungerText = value; }
        Text armorText { get => currentScreen.armorText; set => currentScreen.armorText = value; }
        GameObject diagnosticsPanel { get => currentScreen.diagnosticsPanel; set => currentScreen.diagnosticsPanel = value; }
        Image progress { get => currentScreen.progress; set => currentScreen.progress = value; }
        RawImage heldIcon { get => currentScreen.heldIcon; set => currentScreen.heldIcon = value; }
        long lastRevision { get => currentScreen.lastRevision; set => currentScreen.lastRevision = value; }
        long lastCraftRevision { get => currentScreen.lastCraftRevision; set => currentScreen.lastCraftRevision = value; }
        long lastStationRevision { get => currentScreen.lastStationRevision; set => currentScreen.lastStationRevision = value; }
        long lastEquipmentRevision { get => currentScreen.lastEquipmentRevision; set => currentScreen.lastEquipmentRevision = value; }
        int lastSelected { get => currentScreen.lastSelected; set => currentScreen.lastSelected = value; }
        int hoveredSlot { get => currentScreen.hoveredSlot; set => currentScreen.hoveredSlot = value; }
        int shownFood { get => currentScreen.shownFood; set => currentScreen.shownFood = value; }
        int shownArmor { get => currentScreen.shownArmor; set => currentScreen.shownArmor = value; }
        float shownHealth { get => currentScreen.shownHealth; set => currentScreen.shownHealth = value; }
        bool shownEating { get => currentScreen.shownEating; set => currentScreen.shownEating = value; }
        ItemStack shownHeld { get => currentScreen.shownHeld; set => currentScreen.shownHeld = value; }
        Text craftStatus { get => currentStation.craftStatus; set => currentStation.craftStatus = value; }
        Text craftOutputName { get => currentStation.craftOutputName; set => currentStation.craftOutputName = value; }
        Text furnaceText { get => currentStation.furnaceText; set => currentStation.furnaceText = value; }
        Text machineStatus { get => currentStation.machineStatus; set => currentStation.machineStatus = value; }
        Text machineDetail { get => currentStation.machineDetail; set => currentStation.machineDetail = value; }
        Image cookBar { get => currentStation.cookBar; set => currentStation.cookBar = value; }
        Image burnBar { get => currentStation.burnBar; set => currentStation.burnBar = value; }
        Image machineProgress { get => currentStation.machineProgress; set => currentStation.machineProgress = value; }
        int shownProgress { get => currentStation.shownProgress; set => currentStation.shownProgress = value; }
        int shownBurn { get => currentStation.shownBurn; set => currentStation.shownBurn = value; }
        long shownFurnaceRevision { get => currentStation.shownFurnaceRevision; set => currentStation.shownFurnaceRevision = value; }
        float nextMachineRefresh { get => currentStation.nextMachineRefresh; set => currentStation.nextMachineRefresh = value; }

        bool RecipeVisible => recipePanel != null && recipePanel.activeSelf;
        public bool BoundToCurrentSession => ReferenceEquals(boundInventory,game.Inventory);
        public void DiscardSessionInput() { CancelScreenInput();HeldStack=default; }
        public bool HasInventoryBinding
        {
            get
            {
                if (!game.InventoryOpen || game.Health.Dead || game.LoadingSave || currentScreen != inventoryScreen || constructing ||
                    !ReferenceEquals(boundInventory,game.Inventory) || !ReferenceEquals(boundCrafting,game.Crafting) ||
                    !ReferenceEquals(boundStation,game.OpenStation) || !ReferenceEquals(boundMachine,game.OpenMachine)) return false;
                if (boundStation == null && boundMachine == null) return true;
                if (!boundPosition.Equals(game.StationPosition) || !game.World.Ready(boundPosition) ||
                    (game.World.Local(boundPosition)+Vector3.one*.5f-game.Player.transform.position).sqrMagnitude>36) return false;
                return boundStation != null ? ReferenceEquals(game.Survival.At(boundPosition),boundStation) :
                    game.Industry.Simulation.Machines.TryGetValue(boundPosition,out var machine) && ReferenceEquals(machine,boundMachine);
            }
        }
        public bool AcceptWidget(Component widget) => widget != null && widget.gameObject.activeInHierarchy &&
            root != null && currentScreen.canvas.enabled && widget.transform.IsChildOf(root) && (game.Mode != ScreenMode.Inventory || HasInventoryBinding);
        public bool AcceptGesture(Component widget,long version) => version == BindingVersion && AcceptWidget(widget);

        void CancelScreenInput()
        {
            BindingVersion++;
            EndRightPaint(); creativeDrag=default;
            browserSearchField?.DeactivateInputField();
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);
            CloseBrowserRecipe();
            hoveredSlot=-1; browserHovered=0;
            if(heldRoot!=null)heldRoot.gameObject.SetActive(false);
            boundInventory=null;boundCrafting=null;boundStation=null;boundMachine=null;
        }
        static readonly Unity.Profiling.ProfilerMarker hideScreenMarker=new Unity.Profiling.ProfilerMarker("RivetReach.UI.HideScreen");
        static readonly Unity.Profiling.ProfilerMarker bindStationMarker=new Unity.Profiling.ProfilerMarker("RivetReach.UI.BindStation");
        static readonly Unity.Profiling.ProfilerMarker refreshBindingMarker=new Unity.Profiling.ProfilerMarker("RivetReach.UI.RefreshBinding");
        static readonly Unity.Profiling.ProfilerMarker createScreenMarker=new Unity.Profiling.ProfilerMarker("RivetReach.UI.CreateScreen");
        void SwitchScreen()
        {
            if(canvas==null)return;
            CancelScreenInput();
            if(currentStation.root!=null)currentStation.root.gameObject.SetActive(false);
            currentStation=new StationWidgets();
            if(root!=null)
            {
                if(currentScreen==hudScreen || currentScreen==inventoryScreen)HideRetainedScreen(currentScreen);
                else {root.gameObject.SetActive(false);Destroy(root.gameObject);}
            }
            if(game.Mode==ScreenMode.Play)
            {
                currentScreen=hudScreen??=CreateScreen();
                if(root.childCount==0)BuildHUD();
                currentScreen.hudMode.text=game.Creative?"CREATIVE · INVINCIBLE":"FIRST EXPEDITION";
                currentScreen.hudFlight.text=game.Creative?$"Double-tap {game.Input.Keys["Jump"]}: toggle flight · {game.Input.Keys["Crouch"]}: crouch / descend · {game.Input.Keys["Sprint"]}: run":"";
                currentScreen.hudControls.text=$"{game.Input.Keys["Inventory"]}  Inventory    {game.Input.Keys["Interact"]}  Interact    {game.Input.UseButtonName}  Use / place    {game.Input.Keys["Drop"]}  Drop";
            }
            else if(game.Mode==ScreenMode.Inventory)
            {
                currentScreen=inventoryScreen??=CreateScreen();
                BuildItemBrowserInventory();
                BindStationPanel();
                boundInventory=game.Inventory;boundCrafting=game.Crafting;boundStation=game.OpenStation;boundMachine=game.OpenMachine;boundPosition=game.StationPosition;
                currentScreen.inventoryTitle.text=(game.OpenMachine!=null?game.OpenMachine.Definition.Name:game.OpenStation!=null?game.Registry.Get(game.OpenStation.Block).displayName:game.Creative?"Creative inventory":"Inventory").ToUpperInvariant();
                currentScreen.creativeToggle.gameObject.SetActive(game.Creative && game.OpenStation==null && game.OpenMachine==null);
                currentScreen.creativeToggle.GetComponentInChildren<Text>(true).text=creativeCrafting?"ALL ITEMS":"CRAFTING";
                HoverBrowserItem(0);RefreshPreview();
            }
            else
            {
                currentScreen=CreateScreen();
                currentStation=new StationWidgets();
                if(game.Mode==ScreenMode.Title)BuildTitle();
                else if(game.Mode==ScreenMode.Death)BuildDeath();
                else if(game.Mode==ScreenMode.Save||game.Mode==ScreenMode.Load)BuildSaveMenu();
                else BuildMenu();
            }
            root.localScale=Vector3.one*Mathf.Clamp(PlayerPrefs.GetFloat("uiScale",1),.85f,1);
            using(refreshBindingMarker.Auto()){ResetDisplayedState();RefreshSlots();RefreshSurvival();RefreshMachine();}
            if(previewRoot!=null)previewRoot.SetActive(game.Mode==ScreenMode.Inventory||game.Mode==ScreenMode.Appearance);
            root.gameObject.SetActive(true);
            RefreshSelectableCache(currentScreen);
            foreach(var selectable in currentScreen.selectables)if(selectable!=null)selectable.enabled=true;
            currentScreen.canvas.enabled=true;currentScreen.raycaster.enabled=true;
            foreach(var child in currentScreen.childCanvases)child.enabled=true;
            foreach(var child in currentScreen.childRaycasters)child.enabled=true;
        }
        void HideRetainedScreen(ScreenWidgets screen)
        {
            using var measurement=hideScreenMarker.Auto();
            screen.canvas.enabled=false;screen.raycaster.enabled=false;
            foreach(var child in screen.childCanvases)child.enabled=false;
            foreach(var child in screen.childRaycasters)child.enabled=false;
            RefreshSelectableCache(screen);
            foreach(var selectable in screen.selectables)if(selectable!=null)selectable.enabled=false;
        }
        void RefreshSelectableCache(ScreenWidgets screen)
        {
            if(screen.selectableRevision==CreatedWidgets)return;
            screen.selectables.Clear();screen.root.GetComponentsInChildren(true,screen.selectables);screen.selectableRevision=CreatedWidgets;
        }
        ScreenWidgets CreateScreen()
        {
            using var measurement=createScreenMarker.Auto();
            var screen=new ScreenWidgets();
            screen.root=Rect(canvas.transform,"Screen",0,0,1280,720);screen.root.gameObject.SetActive(false);
            screen.canvas=screen.root.gameObject.AddComponent<Canvas>();screen.canvas.enabled=false;
            screen.raycaster=screen.root.gameObject.AddComponent<GraphicRaycaster>();screen.raycaster.enabled=false;
            screen.root.anchorMin=screen.root.anchorMax=screen.root.pivot=new Vector2(.5f,.5f);screen.root.anchoredPosition=Vector2.zero;
            return screen;
        }
        void IsolateCanvas(RectTransform region,bool interactive=false)
        {
            // Keep frequently changing controls/text from rebatching the entire inventory.
            // Retained child canvases must follow the published screen's visibility/input.
            currentScreen.childCanvases.Add(region.gameObject.AddComponent<Canvas>());
            if(interactive)currentScreen.childRaycasters.Add(region.gameObject.AddComponent<GraphicRaycaster>());
        }
        void ResetDisplayedState()
        {
            lastRevision=lastCraftRevision=lastStationRevision=lastEquipmentRevision=-1;lastSelected=hoveredSlot=-1;shownHeld=default;
            shownHealth=float.NaN;shownFood=shownArmor=-1;shownEating=false;
            shownProgress=shownBurn=-1;shownFurnaceRevision=-1;nextMachineRefresh=0;
            // Force a bind even when different inventories happen to share revision numbers.
            foreach(var slot in slots)slot.Shown=false;
        }
        int StationLayout => game.OpenMachine!=null ? 1000+game.OpenMachine.Definition.Id : game.OpenStation?.Furnace!=null ? 100 :
            game.OpenStation?.Storage!=null ? 101 : game.Creative&&game.OpenStation==null&&!creativeCrafting ? 102 : game.Crafting.Grid.Size;
        void BindStationPanel()
        {
            using var measurement=bindStationMarker.Auto();
            if(currentStation.root!=null)currentStation.root.gameObject.SetActive(false);
            if(slots.Count>currentScreen.commonSlots)slots.RemoveRange(currentScreen.commonSlots,slots.Count-currentScreen.commonSlots);
            int key=StationLayout;
            if(!stationScreens.TryGetValue(key,out var panel))
            {
                panel=new StationWidgets();currentStation=panel;
                panel.root=Rect(currentScreen.stationHost,"Station layout "+key,0,0,1170,634);panel.root.gameObject.SetActive(false);
                int first=slots.Count;
                if(game.OpenMachine!=null)BuildMachine(panel.root);
                else if(key==100)BuildFurnace(panel.root);
                else if(key==101)BuildChest(panel.root);
                else if(key==102)BuildCreativeCatalog(panel.root);
                else BuildCraftingStation(panel.root);
                panel.slots.AddRange(slots.GetRange(first,slots.Count-first));stationScreens.Add(key,panel);
            }
            else {currentStation=panel;slots.AddRange(panel.slots);}
            if(panel.bridgeNameField!=null)panel.bridgeNameField.SetTextWithoutNotify(game.OpenMachine.LinkName);
            foreach(var control in panel.controls)control.label.text=control.value(game.OpenMachine);
            if(panel.creativeStatus!=null)panel.creativeStatus.text="Click an item to receive a full stack";
            panel.root.gameObject.SetActive(true);
            BindStationIllustration();
        }
        void RefreshMachineControls()
        {
            if(!HasInventoryBinding || game.OpenMachine==null)return;
            foreach(var control in currentStation.controls)control.label.text=control.value(game.OpenMachine);
            nextMachineRefresh=0;RefreshMachine();RefreshSlots();
        }
        Button MachineButton(Transform parent,Func<MachineState,string> text,float x,float y,float w,float h,Action<MachineState> command)
        {
            var button=Button(parent,text(game.OpenMachine),x,y,w,h,()=>
            {
                if(!HasInventoryBinding || game.OpenMachine==null)return;
                command(game.OpenMachine);RefreshMachineControls();
            });
            currentStation.controls.Add((button.GetComponentInChildren<Text>(true),text));return button;
        }
        void PrewarmInventory()
        {
            // Each hidden preparation stage runs on a separate loading frame. Opening early
            // finishes any remaining shell stages synchronously before publishing the screen.
            if(prewarmed || game.Mode!=ScreenMode.Title && (game.Mode!=ScreenMode.Play || game.ReadyToPlay && game.World.PendingCount==0))return;
            constructing=true;var previous=currentScreen;var previousStation=currentStation;
            try
            {
                currentScreen=inventoryScreen??=CreateScreen();
                if(!inventoryShellBuilt)BuildInventoryShell();
                else if(!inventorySidebarBuilt){BuildItemSidebar();inventorySidebarBuilt=true;}
                else if(!inventoryCursorBuilt)BuildInventoryCursor();
                else if(recipeWidgets==null)BuildRecipeDetails();
                else if(!recipeWarmed)
                {
                    browserItem=BlockId.Planks;browserUses=false;recipePage=0;
                    BindRecipeDetails();CloseBrowserRecipe();recipeWarmed=true;
                }
                else
                {
                    BindStationPanel();currentStation.root.gameObject.SetActive(false);prewarmed=true;
                }
                HideRetainedScreen(currentScreen);root.gameObject.SetActive(true);
            }
            finally {currentScreen=previous;currentStation=previousStation;constructing=false;}

        }
    }

    public sealed class BoundUIButton : Button
    {
        public GameUI Owner;
        long pressVersion=-1;
        public override void OnPointerDown(PointerEventData e) { pressVersion=Owner.BindingVersion;base.OnPointerDown(e); }
        public override void OnPointerClick(PointerEventData e) {if(Owner.AcceptGesture(this,pressVersion))base.OnPointerClick(e);}
        public override void OnSubmit(BaseEventData e) {if(Owner.AcceptWidget(this))base.OnSubmit(e);}
        protected override void OnDisable() {pressVersion=-1;base.OnDisable();}
    }
}
