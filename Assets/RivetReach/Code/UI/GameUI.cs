using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI : MonoBehaviour
    {
        Expedition game;
        Canvas canvas;
        RectTransform root;
        Font font;
        readonly Color ink=new Color(.045f,.075f,.09f,.97f),slate=new Color(.105f,.16f,.185f,.96f),gold=new Color(.85f,.72f,.47f),pale=new Color(.91f,.92f,.85f);
        readonly List<SlotView> slots=new List<SlotView>();
        readonly Dictionary<byte,Texture2D> icons=new Dictionary<byte,Texture2D>();
        Text message,diagnostics,targetLabel,heldLabel,selectedLabel,loading,tooltip;
        Text worldTime;
        GameObject diagnosticsPanel;
        Image progress;
        RectTransform heldRoot;
        RawImage heldIcon;
        public ItemStack HeldStack;
        long lastRevision=-1,lastCraftRevision=-1;
        int lastSelected=-1;
        const int CraftSlotStart=Inventory.SlotCount, CraftOutputSlot=Inventory.SlotCount+16;
        Text craftStatus,craftOutputName,inventoryHint;
        int hoveredSlot=-1;
        RenderTexture previewTexture;
        GameObject previewRoot;
        AvatarView preview;
        float frameAverage;
        string titleSeedText="";
        public void Initialize(Expedition expedition)
        {
            game=expedition;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go=new GameObject("Interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);
            canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            if(FindAnyObjectByType<EventSystem>()==null)
            {var events=new GameObject("Input events",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            BuildIcons();CreatePreview();Rebuild();
        }
        RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var o=new GameObject(name,typeof(RectTransform));var r=o.GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        Image Panel(Transform parent,float x,float y,float w,float h,Color colour)
        {var r=Rect(parent,"Panel",x,y,w,h);var image=r.gameObject.AddComponent<Image>();image.color=colour;return image;}
        Text Label(Transform parent,string content,float x,float y,float w,float h,int size=18,Color? colour=null)
        {
            var r=Rect(parent,"Label",x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=colour??pale;t.text=content;t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;
        }
        Button Button(Transform parent,string text,float x,float y,float w,float h,Action action,bool primary=false)
        {
            var image=Panel(parent,x,y,w,h,primary?new Color(.29f,.45f,.43f):slate);var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colours=button.colors;colours.highlightedColor=new Color(1.2f,1.2f,1.15f);colours.pressedColor=new Color(.7f,.8f,.75f);button.colors=colours;
            var label=Label(image.transform,text,10,0,w-20,h,18);label.alignment=TextAnchor.MiddleCenter;
            button.onClick.AddListener(()=>action());return button;
        }
        public void Rebuild()
        {
            if(canvas==null)return;
            ResetSurvivalUI();machineStatus=machineDetail=null;machineProgress=null;
            if(root!=null){root.gameObject.SetActive(false);Destroy(root.gameObject);}slots.Clear();message=null;diagnostics=null;diagnosticsPanel=null;targetLabel=null;progress=null;heldRoot=null;heldLabel=null;loading=null;tooltip=null;craftStatus=null;craftOutputName=null;inventoryHint=null;hoveredSlot=-1;lastRevision=-1;lastCraftRevision=-1;
            if(previewRoot!=null)previewRoot.SetActive(game.Mode==ScreenMode.Inventory||game.Mode==ScreenMode.Appearance);
            root=Rect(canvas.transform,"Screen",0,0,1280,720);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
            float scale=Mathf.Clamp(PlayerPrefs.GetFloat("uiScale",1),.85f,1);root.localScale=Vector3.one*scale;
            if(game.Mode==ScreenMode.Play)BuildHUD();
            else if(game.Mode==ScreenMode.Inventory)BuildInventory();
            else if(game.Mode==ScreenMode.Title)BuildTitle();
            else if(game.Mode==ScreenMode.Death)BuildDeath();
            else BuildMenu();
            RefreshSlots();
        }
        void BuildTitle()
        {
            var p=Panel(root,48,50,440,620,new Color(.045f,.09f,.105f,.95f));
            Label(p.transform,"RIVET\nREACH",34,28,380,175,64);
            Label(p.transform,"THE FIRST EXPEDITION",36,205,360,30,18,gold);
            Label(p.transform,"Explore the terrain. Mine with your fists.\nBring what you find back to your inventory.",36,252,360,68,19);
            Label(p.transform,"WORLD SEED",36,337,160,22,13,gold);
            var fieldBackground=Panel(p.transform,36,366,365,44,slate);
            var field=fieldBackground.gameObject.AddComponent<InputField>();field.textComponent=Label(fieldBackground.transform,"",12,5,340,35,21);field.contentType=InputField.ContentType.IntegerNumber;field.text=titleSeedText;field.characterLimit=11;
            field.placeholder=Label(fieldBackground.transform,"Random — leave blank",12,10,340,30,17,new Color(.65f,.71f,.70f));
            var seedError=Label(p.transform,"",36,411,365,18,12,gold);
            field.onValueChanged.AddListener(value=>{titleSeedText=value;seedError.text="";});
            Button(p.transform,"START EXPEDITION",36,430,365,50,()=>
            {
                if(string.IsNullOrWhiteSpace(field.text)){game.StartSession(game.Seed);return;}
                if(int.TryParse(field.text,out int seed))game.StartSession(seed);
                else seedError.text="Enter a whole number from -2147483648 to 2147483647.";
            },true);
            Button(p.transform,"PLAYER",36,494,112,40,()=>game.SetMode(ScreenMode.Appearance));
            Button(p.transform,"SETTINGS",161,494,114,40,()=>game.SetMode(ScreenMode.Settings));
            Button(p.transform,"QUIT",289,494,112,40,game.Quit);
            Label(p.transform,"Early POC · Session progress resets on quit",36,565,372,35,14,new Color(.72f,.76f,.73f));
            Label(root,"TERRAIN / MOVEMENT / DISCOVERY",735,632,480,24,16,gold);
            Label(root,"An open world, one playable step at a time.",735,661,480,28,20);
        }
        void BuildHUD()
        {
            Label(root,"RIVET REACH",28,22,300,28,19);
            worldTime=Label(root,"",870,22,380,52,16,pale);worldTime.alignment=TextAnchor.UpperRight;
            Label(root,game.Creative?"CREATIVE · INVINCIBLE":"FIRST EXPEDITION",29,52,280,22,11,gold);
            if(game.Creative)Label(root,$"{game.Input.Keys["Jump"]} Rise · {game.Input.Keys["Crouch"]} Descend · {game.Input.Keys["Sprint"]} Fly faster",29,76,600,22,13,gold);
            foreach(var bar in new[]{new Rect(632,359,5,2),new Rect(643,359,5,2),new Rect(639,352,2,5),new Rect(639,363,2,5)})
            {
                Panel(root,bar.x-1,bar.y-1,bar.width+2,bar.height+2,new Color(.025f,.04f,.035f,.65f));
                Panel(root,bar.x,bar.y,bar.width,bar.height,pale);
            }
            targetLabel=Label(root,"",370,395,540,52,17);targetLabel.alignment=TextAnchor.MiddleCenter;
            var track=Panel(root,580,385,120,3,new Color(.1f,.15f,.16f,.5f));progress=Panel(track.transform,0,0,0,3,gold);
            Panel(root,296,635,688,67,new Color(.025f,.045f,.055f,.75f));
            BuildHotbar(root,304,642,52,4);
            selectedLabel=Label(root,"",420,611,440,24,15);selectedLabel.alignment=TextAnchor.MiddleCenter;
            Label(root,$"{game.Input.Keys["Inventory"]}  Inventory    {game.Input.Keys["Interact"]}  Interact    {game.Input.UseButtonName}  Use / place    {game.Input.Keys["Drop"]}  Drop",28,694,850,22,13);
            BuildSurvivalHUD();
            Label(root,"Session-only world",1090,694,172,22,12,new Color(.75f,.77f,.73f));
            message=Label(root,"",330,555,620,40,18,gold);message.alignment=TextAnchor.MiddleCenter;
            loading=Label(root,"",435,457,410,50,20);loading.alignment=TextAnchor.MiddleCenter;
            var debug=Panel(root,20,94,680,210,new Color(.025f,.045f,.055f,.9f));diagnosticsPanel=debug.gameObject;
            diagnostics=Label(debug.transform,"",8,9,660,190,14);diagnosticsPanel.SetActive(game.Diagnostics);
        }
        void BuildInventory()
        {
            Panel(root,0,0,1280,720,new Color(0,0,0,.23f));var p=Panel(root,55,42,1170,634,ink);
            Label(p.transform,game.OpenMachine!=null?game.OpenMachine.Definition.Name.ToUpperInvariant():game.OpenStation==null?(game.Creative?"CREATIVE INVENTORY":"INVENTORY"):game.Registry.Get(game.OpenStation.Block).displayName.ToUpperInvariant(),28,20,500,45,30);Label(p.transform,"Drag stacks · Right-click split / place one · Shift-click transfer",28,66,880,28,15,gold);
            Button(p.transform,"CLOSE",1033,20,108,40,()=>game.SetMode(ScreenMode.Play));
            if(game.Creative&&game.OpenStation==null&&game.OpenMachine==null)Button(p.transform,creativeCrafting?"ALL ITEMS":"CRAFTING",821,20,192,40,()=>{creativeCrafting=!creativeCrafting;Rebuild();});
            var backdrop=Panel(p.transform,28,113,210,270,slate);var portrait=Rect(backdrop.transform,"Portrait",0,0,210,270).gameObject.AddComponent<RawImage>();portrait.texture=previewTexture;portrait.uvRect=PortraitUV(210,270);portrait.gameObject.AddComponent<PortraitDrag>().Owner=this;
            for(int row=0;row<6;row++)for(int col=0;col<8;col++)Slot(p.transform,12+row*8+col,266+col*62,113+row*62,56);
            BuildEquipment(p.transform);
            GameObject guide=null;
            if(game.OpenMachine!=null)BuildMachine(p.transform);
            else if(game.OpenStation?.Furnace!=null)BuildFurnace(p.transform);
            else if(game.OpenStation?.Storage!=null)BuildChest(p.transform);
            else if(game.Creative&&game.OpenStation==null&&game.OpenMachine==null&&!creativeCrafting)BuildCreativeCatalog(p.transform);
            else
            {
                int size=game.Crafting.Grid.Size,cell=size==2?60:size==4?38:48,gap=size==2?70:size==4?43:56;
                Label(p.transform,"CRAFTING",821,115,300,42,27);
                Label(p.transform,size==2?"Personal · 2 × 2":size==4?"Machinist · 4 × 4":"Workbench · 3 × 3",821,162,210,30,17,gold);
                Button(p.transform,"RECIPES",1031,163,108,32,()=>guide.SetActive(!guide.activeSelf));
                for(int row=0;row<size;row++)for(int col=0;col<size;col++)Slot(p.transform,CraftSlotStart+row*size+col,821+col*gap,222+row*gap,cell);
                Label(p.transform,"→",995,268,27,40,26,gold);Slot(p.transform,CraftOutputSlot,1030,256,64);
                craftOutputName=Label(p.transform,"",1010,333,125,44,14,gold);craftOutputName.alignment=TextAnchor.UpperCenter;
                craftStatus=Label(p.transform,"",829,407,290,40,15,gold);
                Label(p.transform,"Click result: craft one\nShift-click result: craft all that fit",829,451,300,44,14);
                Button(p.transform,"RETURN INGREDIENTS",829,502,282,32,()=>{game.Crafting.ReturnIngredients(game.Inventory);RefreshSlots();},false);
                if(size==2)Label(p.transform,$"4 planks → Workbench\nPlace it, then {game.Input.Keys["Interact"]} / {game.Input.UseButtonName} for 3 × 3",821,194,318,26,12,gold);
            }
            Label(p.transform,"HOTBAR",266,502,250,24,14,gold);BuildHotbar(p.transform,266,534,53,4);
            inventoryHint=Label(p.transform,game.OpenMachine!=null?MachinePortSummary(game.OpenMachine):game.Creative&&game.OpenStation==null&&game.OpenMachine==null&&!creativeCrafting?"Choose items from the catalog, then move them to your hotbar. Crafting opens the personal grid.":"Use Recipes to see available layouts. Leftover ingredients stay in the grid if your inventory is full.",28,604,1090,24,14);
            Button(p.transform,"APPEARANCE",28,527,210,40,()=>game.SetMode(ScreenMode.Appearance));
            heldRoot=Rect(root,"Held stack",0,0,52,65);heldRoot.gameObject.SetActive(false);heldIcon=heldRoot.gameObject.AddComponent<RawImage>();heldIcon.raycastTarget=false;heldLabel=Label(heldRoot,"",0,37,55,25,16);heldLabel.alignment=TextAnchor.LowerRight;
            tooltip=Label(p.transform,"",28,604,1090,24,14,gold);
            if(game.OpenMachine==null&&(!game.Creative||creativeCrafting||game.OpenStation!=null)&&game.OpenStation?.Furnace==null&&game.OpenStation?.Storage==null){guide=BuildCraftingGuide(p.transform);guide.SetActive(false);}
            RefreshPreview();
        }
        GameObject BuildCraftingGuide(Transform parent)
        {
            var panel=Panel(parent,816,204,329,330,ink);
            Label(panel.transform,game.Crafting.Grid.Size==2?"PERSONAL RECIPES":game.Crafting.Grid.Size==4?"MACHINIST RECIPES":"WORKBENCH RECIPES",10,8,300,25,16,gold);
            var viewport=Panel(panel.transform,7,40,315,280,slate);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic=true;
            var content=Rect(viewport.transform,"Recipe list",0,0,315,0);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport.rectTransform;scroll.content=content;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            int row=0;
            foreach(var recipe in game.Recipes.Recipes)
            {
                if(recipe.MinimumGridSize>game.Crafting.Grid.Size)continue;
                float y=row++*116+9;
                int width=recipe.Kind==RecipeKind.Shaped?recipe.Width:game.Crafting.Grid.Size;
                for(int i=0;i<recipe.Ingredients.Count;i++)
                {
                    var stack=recipe.Ingredients[i];var cell=Panel(content,9+i%width*25,y+i/width*25,24,24,ink);
                    if(stack.Empty)continue;
                    var icon=Rect(cell.transform,"Ingredient",2,2,20,20).gameObject.AddComponent<RawImage>();icon.texture=icons[stack.Id];icon.raycastTarget=false;
                    if(stack.Count>1)Label(cell.transform,stack.Count.ToString(),0,8,23,16,10).alignment=TextAnchor.LowerRight;
                }
                Label(content,"→",105,y+10,30,30,22,gold);
                var output=Rect(content,"Output",132,y+7,34,34).gameObject.AddComponent<RawImage>();output.texture=icons[recipe.Output.Id];output.raycastTarget=false;
                Label(content,game.Registry.Get(recipe.Output.Id).displayName+" × "+recipe.Output.Count,174,y+5,144,42,15,gold);
                Label(content,recipe.Kind==RecipeKind.Shapeless?"Any arrangement":recipe.AllowsMirroring?"Layout or mirror":"Shown layout",9,y+83,296,22,12);
            }
            content.sizeDelta=new Vector2(315,Mathf.Max(280,row*116+9));
            return panel.gameObject;
        }
        ItemStack StackAt(int index)
        {
            if(index>=MachineSlotStart&&index<MachineSlotStart+3&&game.OpenMachine!=null)return game.OpenMachine.Items.Slots[index-MachineSlotStart];
            if(index>=ArmorSlotStart&&index<ArmorSlotStart+4)return game.Equipment.Slots[index-ArmorSlotStart];
            if(index>=StationSlotStart&&game.OpenStation!=null)return StationStack(index-StationSlotStart);
            if(index==CraftOutputSlot)return game.Crafting.Preview?.Output??default;
            if(index>=CraftSlotStart&&index<CraftSlotStart+game.Crafting.Grid.Count)return game.Crafting.Grid.Slots[index-CraftSlotStart];
            return index>=0&&index<Inventory.SlotCount?game.Inventory.Slots[index]:default;
        }
        public void HoverSlot(int index){hoveredSlot=index;RefreshTooltip();}
        void RefreshTooltip()
        {
            if(tooltip==null)return;var stack=StackAt(hoveredSlot);
            tooltip.text=stack.Empty?"":game.Registry.Get(stack.Id).displayName+" · "+stack.Count+" / "+game.Registry.Get(stack.Id).stackLimit;
            if(hoveredSlot==CraftOutputSlot&&game.Crafting.Preview!=null)tooltip.text+=" · "+game.Crafting.MaximumCrafts+" craft(s) available";
            if(inventoryHint!=null)inventoryHint.enabled=tooltip.text.Length==0;
        }
        public void RotatePreview(float delta){if(preview!=null)preview.transform.Rotate(0,-delta*.6f,0,Space.World);}
        void BuildHotbar(Transform parent,float x,float y,int size,int gap)
        {for(int i=0;i<12;i++)Slot(parent,i,x+i*(size+gap),y,size);}
        void Slot(Transform parent,int index,float x,float y,int size)
        {
            var image=Panel(parent,x,y,size,size,slate);var slot=image.gameObject.AddComponent<SlotView>();slot.Owner=this;slot.Index=index;slot.Background=image;
            slot.Border=image.gameObject.AddComponent<Outline>();slot.Border.effectDistance=new Vector2(1,-1);slot.Border.useGraphicAlpha=false;
            var icon=Rect(image.transform,"Item",7,6,size-14,size-14).gameObject.AddComponent<RawImage>();icon.raycastTarget=false;slot.Icon=icon;
            slot.Count=Label(image.transform,"",3,size-22,size-7,22,14);slot.Count.alignment=TextAnchor.LowerRight;
            if(index<12)Label(image.transform,(index+1).ToString(),3,2,20,14,10,new Color(.55f,.64f,.64f));
            slots.Add(slot);
        }
        void BuildMenu()
        {
            Panel(root,0,0,1280,720,new Color(0,0,0,.4f));var p=Panel(root,245,48,790,624,ink);
            string title=game.Mode==ScreenMode.Pause?"EXPEDITION PAUSED":game.Mode.ToString().ToUpperInvariant();
            Label(p.transform,title,34,28,650,52,32);Button(p.transform,"BACK",650,26,106,38,()=>game.SetMode(game.Mode==ScreenMode.Pause?ScreenMode.Play:game.Started?ScreenMode.Pause:ScreenMode.Title));
            if(game.Mode==ScreenMode.Pause)
            {
                Button(p.transform,"RESUME EXPLORATION",180,130,430,54,()=>game.SetMode(ScreenMode.Play),true);
                Button(p.transform,"PLAYER & SKIN",180,204,430,46,()=>game.SetMode(ScreenMode.Appearance));
                Button(p.transform,"SETTINGS",180,268,430,46,()=>game.SetMode(ScreenMode.Settings));
                Button(p.transform,"CONTROLS",180,332,430,46,()=>game.SetMode(ScreenMode.Controls));
                Button(p.transform,game.Creative?"CREATIVE MODE: ON":"CREATIVE MODE: OFF",180,396,430,46,()=>game.SetCreative(!game.Creative),game.Creative);
                Button(p.transform,"QUIT — SESSION WILL RESET",180,466,430,46,game.Quit);
                Label(p.transform,"Terrain edits and inventory survive chunk unloading,\nbut this first slice does not yet save progress between sessions.",115,530,610,60,17);
            }
            else if(game.Mode==ScreenMode.Appearance)
            {
                var portrait=Rect(p.transform,"Portrait",35,105,290,465).gameObject.AddComponent<RawImage>();portrait.texture=previewTexture;portrait.uvRect=PortraitUV(290,465);portrait.gameObject.AddComponent<PortraitDrag>().Owner=this;
                Label(p.transform,"PLAYER MODEL",365,125,350,30,15,gold);
                Button(p.transform,"MALE",365,167,165,46,()=>{game.SetAppearance(false,game.Player.Skin);Rebuild();},!game.Player.Female);
                Button(p.transform,"FEMALE",548,167,165,46,()=>{game.SetAppearance(true,game.Player.Skin);Rebuild();},game.Player.Female);
                Label(p.transform,"SKIN",365,258,350,30,15,gold);
                Button(p.transform,"FIELD / TEAL",365,300,348,46,()=>{game.SetAppearance(game.Player.Female,0);Rebuild();},game.Player.Skin==0);
                Button(p.transform,"OCHRE / SLATE",365,362,348,46,()=>{game.SetAppearance(game.Player.Female,1);Rebuild();},game.Player.Skin==1);
                Label(p.transform,"Appearance is cosmetic.\nBoth players share the same reach and movement.\nYour choice is remembered on this computer.",365,453,350,110,17);
                Label(p.transform,"Drag the player to rotate",365,578,350,24,14,gold);
                RefreshPreview();
            }
            else if(game.Mode==ScreenMode.Settings)
            {
                Slider(p.transform,"Look sensitivity",94,game.Input.Sensitivity,.03f,.25f,v=>{game.Input.Sensitivity=v;PlayerPrefs.SetFloat("sensitivity",v);});
                Slider(p.transform,"Field of view",162,game.Player.Camera.fieldOfView,65,95,v=>{game.Player.Camera.fieldOfView=v;PlayerPrefs.SetFloat("fov",v);});
                Slider(p.transform,"View distance (chunks)",230,game.World.ViewDistance,4,14,v=>{game.World.ViewDistance=Mathf.RoundToInt(v);PlayerPrefs.SetInt("viewDistance.v2",game.World.ViewDistance);RenderSettings.fogStartDistance=game.World.FogStart;RenderSettings.fogEndDistance=game.World.FogEnd;},true);
                Slider(p.transform,"Interface scale",298,Mathf.Clamp(PlayerPrefs.GetFloat("uiScale",1),.85f,1),.85f,1,v=>{PlayerPrefs.SetFloat("uiScale",v);root.localScale=Vector3.one*v;});
                Slider(p.transform,"Sound volume",366,game.Sound.Master,0,1,game.Sound.SetMaster);
                Slider(p.transform,"Effect intensity",434,ArcadePresentation.Active.Intensity,0,1,ArcadePresentation.Active.SetIntensity);
                Button(p.transform,"REBIND CONTROLS",36,537,346,44,()=>game.SetMode(ScreenMode.Controls));
                Button(p.transform,"APPLY",407,537,346,44,()=>{PlayerPrefs.Save();game.SetMode(game.Started?ScreenMode.Pause:ScreenMode.Title);},true);
            }
            else if(game.Mode==ScreenMode.Controls)
            {
                Label(p.transform,game.Input.Rebinding==null?"Select an action, then press a key. Existing conflicts swap keys.":"Press a key for "+game.Input.Rebinding,36,87,718,45,16,gold);
                int i=0;foreach(var kv in game.Input.Keys)
                {
                    string name=kv.Key;int col=i%2,row=i/2;
                    Button(p.transform,name+"   ·   "+kv.Value,36+col*365,143+row*48,348,40,()=>{game.Input.BeginRebind(name);Rebuild();});i++;
                }
                Button(p.transform,"MINE: "+(PlayerPrefs.GetInt("mineButton",0)==0?"LEFT MOUSE":"RIGHT MOUSE"),36,530,348,44,()=>{PlayerPrefs.SetInt("mineButton",1-PlayerPrefs.GetInt("mineButton",0));PlayerPrefs.Save();Rebuild();});
                Label(p.transform,$"{game.Input.UseButtonName}: use / place\n{game.Input.Keys["Crouch"]} + {game.Input.UseButtonName}: place against stations",410,527,340,60,15);
            }
        }
        void Slider(Transform parent,string title,float y,float initial,float min,float max,Action<float> change,bool whole=false)
        {
            Label(parent,title,36,y,480,29,18);var value=Label(parent,initial.ToString("0.##"),640,y,110,29,18,gold);value.alignment=TextAnchor.UpperRight;
            var track=Panel(parent,36,y+43,714,8,slate);var slider=track.gameObject.AddComponent<Slider>();slider.minValue=min;slider.maxValue=max;slider.wholeNumbers=whole;
            var fill=Panel(track.transform,0,0,0,0,gold);
            fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.pivot=new Vector2(.5f,.5f);fill.rectTransform.anchoredPosition=Vector2.zero;
            slider.fillRect=fill.rectTransform;
            var handle=Panel(track.transform,0,0,18,24,pale);
            handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=handle.rectTransform.pivot=new Vector2(.5f,.5f);handle.rectTransform.anchoredPosition=Vector2.zero;
            slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
            slider.value=initial;slider.onValueChanged.AddListener(v=>{change(v);value.text=v.ToString("0.##");});
        }
        public void ClickSlot(int index,bool right,bool shift)
        {
            if(!game.InventoryOpen)return;
            if(ClickStationSlot(index,right,shift)){RefreshSlots();return;}
            if(index==CraftOutputSlot)
            {
                var recipe=game.Crafting.Preview;
                var result=shift?game.Crafting.CraftToInventory(game.Inventory):game.Crafting.CraftToCursor(ref HeldStack);
                RefreshSlots();
                if(craftStatus!=null)craftStatus.text=result.Succeeded?"Crafted "+(result.Crafts*recipe.Output.Count)+" × "+game.Registry.Get(recipe.Output.Id).displayName:CraftFailure(result.Status);
                return;
            }
            if(index>=CraftSlotStart&&index<CraftSlotStart+game.Crafting.Grid.Count)
            {
                int cell=index-CraftSlotStart;
                if(shift&&HeldStack.Empty)game.Crafting.Grid.TransferTo(cell,game.Inventory);
                else game.Crafting.Grid.Click(cell,ref HeldStack,right);
            }
            else if(index>=0&&index<Inventory.SlotCount)
            {
                if(shift&&HeldStack.Empty)QuickTransferInventory(index);
                else game.Inventory.Click(index,ref HeldStack,right);
            }
            RefreshSlots();
        }
        static string CraftFailure(CraftStatus status)=>status==CraftStatus.NoRecipe?"No matching recipe":status==CraftStatus.CursorOccupied?"Put down the held stack first":"Make room for the complete output";
        public void ReturnHeld()
        {
            if(!HeldStack.Empty)
            {
                int left=game.Inventory.Add(HeldStack.Id,HeldStack.Count);
                if(left>0)game.Drop(new ItemStack(HeldStack.Id,left));HeldStack.Clear();
            }
            if(game.OpenStation==null||game.OpenStation.Crafting!=null)game.Crafting.ReturnIngredients(game.Inventory);
        }
        void RefreshSlots()
        {
            foreach(var view in slots)
            {
                var stack=StackAt(view.Index);view.Icon.enabled=!stack.Empty;view.Count.text=stack.Empty?"":stack.Count.ToString();
                if(!stack.Empty)view.Icon.texture=icons[stack.Id];
                bool selected=view.Index<Inventory.HotbarCount&&view.Index==game.Selected;
                view.Background.color=selected?new Color(.40f,.43f,.31f):view.Index==CraftOutputSlot?new Color(.22f,.34f,.32f):slate;
                view.Border.effectColor=selected||view.Index==CraftOutputSlot?gold:new Color(.25f,.33f,.36f,.85f);
            }
            if(craftOutputName!=null)
            {
                var recipe=game.Crafting.Preview;
                craftOutputName.text=recipe==null?"RESULT":game.Registry.Get(recipe.Output.Id).displayName;
                craftStatus.text=recipe==null?"Place ingredients in the grid":"Ready · "+game.Crafting.MaximumCrafts+" craft(s)";
            }
            RefreshTooltip();
            lastRevision=game.Inventory.Revision;lastCraftRevision=game.Crafting.Grid.Revision;lastStationRevision=StationRevision;lastEquipmentRevision=game.Equipment.Revision;lastSelected=game.Selected;
        }
        void Update()
        {
            if(game==null)return;
            if(lastRevision!=game.Inventory.Revision||lastCraftRevision!=game.Crafting.Grid.Revision||lastStationRevision!=StationRevision||lastEquipmentRevision!=game.Equipment.Revision||lastSelected!=game.Selected)RefreshSlots();
            RefreshSurvival();RefreshMachine();
            if(message!=null)message.text=game.Message??"";
            if(loading!=null)loading.text=!game.ReadyToPlay?"Preparing nearby terrain…":"";
            if(targetLabel!=null)
            {
                string hint=BlockId.MiningHint(game.Player.TargetId,game.Registry.Capabilities(game.Inventory.Slots[game.Selected]),game.Registry.Tier(game.Inventory.Slots[game.Selected]));
                targetLabel.text=game.Player.HasTarget?(Fluids.Registry.Get(game.Player.TargetId) is FluidDefinition targetFluid?targetFluid.DisplayName+" source · Use bucket":game.Registry.Get(game.Player.TargetId).displayName)+((BlockId.Station(game.Player.TargetId)||IndustryId.Placed(game.Player.TargetId))?$"\n{game.Input.Keys["Interact"]} / {game.Input.UseButtonName} · Open"+(game.Player.TargetId==BlockId.Workbench?" 3 × 3 crafting":""):hint.Length>0?" · "+hint:""):"";
            }
            if(progress!=null)
            {
                progress.transform.parent.gameObject.SetActive(game.Player.HasTarget&&game.Player.MiningProgress>0);
                progress.rectTransform.sizeDelta=new Vector2(120*Mathf.Clamp01(game.Player.MiningProgress),3);
            }
            if(selectedLabel!=null){var s=game.Inventory.Slots[game.Selected];selectedLabel.text=s.Empty?"BARE HAND":game.Registry.Get(s.Id).displayName+"  ·  "+s.Count;}
            if(heldRoot!=null)
            {
                heldRoot.gameObject.SetActive(!HeldStack.Empty);
                if(!HeldStack.Empty&&Mouse.current!=null)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(root,Mouse.current.position.ReadValue(),null,out var point);
                    heldRoot.localPosition=new Vector3(point.x+10,point.y-10,0);heldIcon.texture=icons[HeldStack.Id];heldLabel.text=HeldStack.Count.ToString();
                }
            }
            frameAverage=Mathf.Lerp(frameAverage,Time.unscaledDeltaTime,.05f);
            if(diagnosticsPanel!=null)diagnosticsPanel.SetActive(game.Diagnostics);
            if(worldTime!=null)
            {
                var clock=game.Sky.Clock;int minute=(int)(clock.Hour*60);
                worldTime.text=$"Day {clock.DayNumber} · {minute/60:00}:{minute%60:00}\n{clock.MoonPhaseName}";
            }
            if(diagnostics!=null)diagnostics.text=game.Diagnostics?$"{1/Mathf.Max(.001f,frameAverage):0} fps · {frameAverage*1000:0.0} ms\nWorld: {TerrainGenerator.WorldId} · seed {game.Seed} · {game.World.Address(game.Player.transform.position)}\nChunks {game.World.ReadyCount}/{game.World.ResidentCount} · queue {game.World.PendingCount}\nGeneration + mesh {game.World.LastBuildMs:0.0} ms · edit mesh {game.World.LastEditMeshMs:0.0} ms\nTriangles {game.World.MeshTriangles:N0} · changes {game.World.EditCount} · piles {game.Items.Piles.Count}\nStale jobs rejected {game.World.RejectedJobs} · origin {game.World.Origin}\nPlacement: {game.PlacementDiagnostic??"No attempt yet"}\nIndustry: {game.Industry.Simulation.Machines.Count} assemblies · tick {game.Industry.Simulation.LastStepMs:0.00} ms · {(game.Industry.Simulation.Rebuilding?"Connecting":"Ready")}":"";
            if(preview!=null&&previewRoot.activeSelf)preview.Animate(.12f,false,Time.unscaledTime*2);
        }
        Rect PortraitUV(float width,float height)
        {
            // Crop the studio background instead of squeezing the character into narrow UI panels.
            float span=(width/height)/(previewTexture.width/(float)previewTexture.height);
            return new Rect((1-span)*.5f,0,span,1);
        }
        void CreatePreview()
        {
            previewRoot=new GameObject("Appearance preview");previewRoot.transform.position=new Vector3(0,-1000,0);
            var model=new GameObject("Preview model");model.transform.SetParent(previewRoot.transform,false);model.transform.localRotation=Quaternion.Euler(0,-18,0);preview=model.AddComponent<AvatarView>();preview.Preview=true;
            var cam=new GameObject("Portrait camera");cam.transform.SetParent(previewRoot.transform,false);cam.transform.localPosition=new Vector3(0,.94f,2.8f);cam.transform.LookAt(previewRoot.transform.position+Vector3.up*.92f);
            var camera=cam.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=slate;camera.fieldOfView=38;camera.nearClipPlane=.1f;camera.farClipPlane=8;camera.cullingMask=1<<30;
            var fillLight=new GameObject("Portrait fill");fillLight.transform.SetParent(previewRoot.transform,false);fillLight.transform.localPosition=new Vector3(.5f,1.5f,2);
            var lamp=fillLight.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=5;lamp.intensity=3;lamp.cullingMask=1<<30;lamp.shadows=LightShadows.None;
            previewTexture=new RenderTexture(768,1024,24){antiAliasing=4,filterMode=FilterMode.Bilinear};previewTexture.Create();camera.targetTexture=previewTexture;
            game.Player.Camera.cullingMask&=~(1<<30);RefreshPreview();
        }
        public void RefreshPreview()
        {
            if(preview==null)return;preview.Build(game.Player.Female,game.Player.Skin);
            foreach(var t in preview.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
        }
        readonly HashSet<Texture2D> sharedToolIcons=new HashSet<Texture2D>();
        void BuildIcons()
        {
            var tiles=Resources.Load<Texture2DArray>("Materials/BlockTiles");
            var swatches=Enumerable.Range(0,tiles.depth).Select(layer=>tiles.GetPixels(layer)).ToArray();
            Color Swatch(int layer,float u,float v)
            {int x=Mathf.Clamp((int)(u*tiles.width),0,tiles.width-1),y=Mathf.Clamp((int)(v*tiles.height),0,tiles.height-1);return swatches[layer][x+y*tiles.width];}
            foreach(var item in game.Registry.items)
            {
                var industrialIcon=Resources.Load<Texture2D>("Industry/Icons/"+item.runtimeId);if(industrialIcon!=null){icons[item.runtimeId]=industrialIcon;sharedToolIcons.Add(industrialIcon);continue;}
                if(item.runtimeId==BlockId.Torch||item.runtimeId>=20&&!BlockId.Placeable(item.runtimeId)&&item.runtimeId!=BlockId.Farmland&&!BlockId.Crop(item.runtimeId))
                {icons[item.runtimeId]=SurvivalItemArt.Icon(item);continue;}
                if(item.toolCapabilities!=ToolCapability.None)
                {
                    string name=(item.toolCapabilities&ToolCapability.Axe)!=0?"StarterAxeIcon":(item.toolCapabilities&ToolCapability.Pickaxe)!=0?"StarterPickaxeIcon":"StarterDaggerIcon";
                    var icon=Resources.Load<Texture2D>("Tools/"+name);sharedToolIcons.Add(icon);icons[item.runtimeId]=icon;continue;
                }
                var texture=new Texture2D(48,48,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Point;var pixels=new Color[48*48];
                for(int y=0;y<48;y++)for(int x=0;x<48;x++)
                {
                    float dx=x-24,dy=y-32;
                    int top=BlockId.Tile(item.runtimeId,1,1),side=BlockId.Tile(item.runtimeId,0,1);
                    Color c=Color.clear;
                    if(BlockId.RawMaterial(item.runtimeId))
                    {
                        float rx=x-24,ry=y-23;
                        bool crystal=item.runtimeId==BlockId.Diamond;
                        bool inside=crystal?Mathf.Abs(rx)/17+Mathf.Abs(ry)/21<1:Mathf.Abs(rx)<18&&Mathf.Abs(ry)<15&&Mathf.Abs(rx)+Mathf.Abs(ry)<26;
                        if(inside)c=Swatch(top,x/48f,y/48f)*(ry>rx*.5f?1.2f:.72f);
                    }
                    else if(Mathf.Abs(dx)/21+Mathf.Abs(dy)/12<1)c=Swatch(top,(dx/21+dy/12+1)*.5f,(dy/12-dx/21+1)*.5f)*1.12f;
                    else if(Mathf.Abs(dx)<21)
                    {
                        float bottom=2+Mathf.Abs(dx)*12/21;
                        if(y>=bottom&&y<bottom+18)c=Swatch(side,Mathf.Abs(dx)/21,(y-bottom)/18)*(dx<0?.68f:.88f);
                    }
                    if(c.a>0)c.a=1;pixels[x+y*48]=c;
                }
                texture.SetPixels(pixels);texture.Apply();icons[item.runtimeId]=texture;
            }
        }
        void OnDestroy(){foreach(var t in icons.Values)if(!sharedToolIcons.Contains(t))Destroy(t);if(previewTexture!=null){previewTexture.Release();Destroy(previewTexture);}if(previewRoot!=null)Destroy(previewRoot);}
    }
    public sealed class PortraitDrag : MonoBehaviour,IDragHandler
    {public GameUI Owner;public void OnDrag(PointerEventData e)=>Owner.RotatePreview(e.delta.x);}
    public sealed class SlotView : MonoBehaviour,IPointerClickHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerEnterHandler,IPointerExitHandler
    {
        public GameUI Owner;public int Index;public Image Background;public Outline Border;public RawImage Icon;public Text Count;
        public void OnPointerEnter(PointerEventData e)=>Owner.HoverSlot(Index);
        public void OnPointerExit(PointerEventData e)=>Owner.HoverSlot(-1);
        bool dragged;
        public void OnPointerClick(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left&&e.button!=PointerEventData.InputButton.Right)return;if(dragged){dragged=false;return;}Owner.ClickSlot(Index,e.button==PointerEventData.InputButton.Right,Keyboard.current?.shiftKey.isPressed==true);}
        public void OnBeginDrag(PointerEventData e){dragged=true;if(Owner.HeldStack.Empty)Owner.ClickSlot(Index,false,false);}
        public void OnDrag(PointerEventData e){}
        public void OnEndDrag(PointerEventData e)
        {var target=e.pointerCurrentRaycast.gameObject;if(target!=null){var slot=target.GetComponentInParent<SlotView>();if(slot!=null)Owner.ClickSlot(slot.Index,false,false);}dragged=false;}
    }
}
