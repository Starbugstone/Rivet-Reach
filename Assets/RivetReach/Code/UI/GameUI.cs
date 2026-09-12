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
        Font font;
        readonly Color ink=new Color(.045f,.075f,.09f,.97f),slate=new Color(.105f,.16f,.185f,.96f),gold=new Color(.85f,.72f,.47f),pale=new Color(.91f,.92f,.85f);
        readonly Dictionary<byte,Texture2D> icons=new Dictionary<byte,Texture2D>();
        public ItemStack HeldStack;
        const int CraftSlotStart=Inventory.SlotCount, CraftOutputSlot=Inventory.SlotCount+16;
        RenderTexture previewTexture;
        GameObject previewRoot;
        AvatarView preview;
        bool previewBuilt,previewFemale;
        int previewSkin;
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
            CreatedWidgets++;
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
            var image=Panel(parent,x,y,w,h,primary?new Color(.29f,.45f,.43f):slate);var button=image.gameObject.AddComponent<BoundUIButton>();button.Owner=this;button.targetGraphic=image;
            var colours=button.colors;colours.highlightedColor=new Color(1.2f,1.2f,1.15f);colours.pressedColor=new Color(.7f,.8f,.75f);button.colors=colours;
            ImmediateFeedback(button);
            var label=Label(image.transform,text,10,0,w-20,h,18);label.alignment=TextAnchor.MiddleCenter;
            button.onClick.AddListener(()=>{if(AcceptWidget(button))action();});return button;
        }
        static void ImmediateFeedback(Selectable widget)
        {
            var colours=widget.colors;colours.fadeDuration=0;widget.colors=colours;
        }
        static readonly Unity.Profiling.ProfilerMarker rebuildMarker = new Unity.Profiling.ProfilerMarker("RivetReach.UI.Rebuild");
        static readonly Unity.Profiling.ProfilerMarker slotInputMarker = new Unity.Profiling.ProfilerMarker("RivetReach.UI.SlotInput");
        static readonly Unity.Profiling.ProfilerMarker slotRefreshMarker = new Unity.Profiling.ProfilerMarker("RivetReach.UI.RefreshSlots");
        public void Rebuild()
        {
            using var measurement = rebuildMarker.Auto();
            SwitchScreen();
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
            Label(p.transform,"0.0.1 Alpha · Save and resume expeditions",36,565,372,35,14,new Color(.72f,.76f,.73f));
            var saves=game.Saves.List();
            Button(root,"CONTINUE LATEST SAVE",735,365,480,54,()=>{if(!game.ContinueLatestSave())Rebuild();},true).interactable=saves.Count>0;
            Button(root,"LOAD GAME",735,435,480,50,()=>game.SetMode(ScreenMode.Load));
            Label(root,game.SaveStatus??game.Saves.ScanWarning??(saves.Count>0?"Latest: "+saves[0].Name:"No saves yet. Start your first expedition."),735,508,480,95,18,gold);
            Label(root,"TERRAIN / MOVEMENT / DISCOVERY",735,632,480,24,16,gold);
            Label(root,"An open world, one playable step at a time.",735,661,480,28,20);
        }
        void BuildHUD()
        {
            Label(root,"RIVET REACH",28,22,300,28,19);
            worldTime=Label(root,"",870,22,380,52,16,pale);worldTime.alignment=TextAnchor.UpperRight;
            currentScreen.hudMode=Label(root,"",29,52,280,22,11,gold);
            currentScreen.hudFlight=Label(root,"",29,76,950,22,13,gold);
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
            currentScreen.hudControls=Label(root,"",28,694,850,22,13);
            BuildSurvivalHUD();
            Label(root,"Escape · Save game",1090,694,172,22,12,new Color(.75f,.77f,.73f));
            message=Label(root,"",330,555,620,40,18,gold);message.alignment=TextAnchor.MiddleCenter;
            loading=Label(root,"",435,457,410,50,20);loading.alignment=TextAnchor.MiddleCenter;
            var debug=Panel(root,20,94,680,210,new Color(.025f,.045f,.055f,.9f));diagnosticsPanel=debug.gameObject;
            diagnostics=Label(debug.transform,"",8,9,660,190,14);diagnosticsPanel.SetActive(game.Diagnostics);
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
                Button(p.transform,"RESUME EXPLORATION",36,112,716,48,()=>game.SetMode(ScreenMode.Play),true);
                Button(p.transform,"SAVE GAME",36,178,346,48,()=>{saveNameText=null;game.SetMode(ScreenMode.Save);},true);
                Button(p.transform,"LOAD GAME",406,178,346,48,()=>game.SetMode(ScreenMode.Load));
                Button(p.transform,"PLAYER & SKIN",36,244,346,46,()=>game.SetMode(ScreenMode.Appearance));
                Button(p.transform,"SETTINGS",406,244,346,46,()=>game.SetMode(ScreenMode.Settings));
                Button(p.transform,"CONTROLS",36,308,346,46,()=>game.SetMode(ScreenMode.Controls));
                Button(p.transform,game.Creative?"CREATIVE MODE: ON":"CREATIVE MODE: OFF",406,308,346,46,()=>game.SetCreative(!game.Creative),game.Creative);
                Button(p.transform,"SAVE & TITLE",36,388,346,48,game.SaveAndTitle);
                Button(p.transform,"SAVE & QUIT",406,388,346,48,game.SaveAndQuit);
                Button(p.transform,"QUIT WITHOUT SAVING",406,456,346,44,game.Quit);
                Label(p.transform,game.SaveStatus??"Save your expedition before leaving.\nClosing the window does not automatically save.",36,529,716,68,17,gold);
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
            slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;ImmediateFeedback(slider);
            slider.value=initial;slider.onValueChanged.AddListener(v=>{change(v);value.text=v.ToString("0.##");});
        }
        bool rightPainting;
        readonly HashSet<int> paintedSlots = new HashSet<int>();
        bool PaintableSlot(int index) => index >= 0 && (index < Inventory.SlotCount ||
            index >= CraftSlotStart && index < CraftSlotStart + game.Crafting.Grid.Count) && !RecipeVisible;
        public bool BeginRightPaint(int index, bool shift)
        {
            EndRightPaint();
            if (!HasInventoryBinding || !PaintableSlot(index)) return false;
            paintedSlots.Add(index);
            ClickSlot(index, true, shift);
            rightPainting = !HeldStack.Empty;
            return true;
        }
        public void PaintSlot(int index)
        {
            if (!rightPainting || Mouse.current?.rightButton.isPressed != true || HeldStack.Empty ||
                !HasInventoryBinding || !PaintableSlot(index) || !paintedSlots.Add(index)) return;
            // Deposit only. An exhausted cursor must never pick ingredients back up.
            ClickSlot(index, true, false);
        }
        public void EndRightPaint() { rightPainting = false; paintedSlots.Clear(); }
        void OnApplicationFocus(bool focus) { if (!focus) {BindingVersion++;EndRightPaint();creativeDrag=default;} }

        public void ClickSlot(int index,bool right,bool shift)
        {
            using var measurement = slotInputMarker.Auto();
            if(!HasInventoryBinding || !slots.Exists(v=>v.Index==index && v.gameObject.activeInHierarchy))return;
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
            using var measurement = slotRefreshMarker.Auto();
            foreach(var view in slots)
            {
                var stack=StackAt(view.Index);
                bool selected=view.Index<Inventory.HotbarCount&&view.Index==game.Selected;
                if(view.Shown&&view.ShownId==stack.Id&&view.ShownCount==stack.Count&&view.ShownSelected==selected)continue;
                view.Shown=true;view.ShownId=stack.Id;view.ShownCount=stack.Count;view.ShownSelected=selected;
                view.Icon.enabled=!stack.Empty;view.Count.text=stack.Empty?"":stack.Count.ToString();
                if(!stack.Empty)view.Icon.texture=icons[stack.Id];
                view.Background.color=selected?new Color(.40f,.43f,.31f):view.Index==CraftOutputSlot?new Color(.22f,.34f,.32f):slate;
                view.Border.effectColor=selected||view.Index==CraftOutputSlot?gold:new Color(.25f,.33f,.36f,.85f);
            }
            if(craftOutputName!=null)
            {
                var recipe=game.Crafting.Preview;
                craftOutputName.text=recipe==null?"RESULT":game.Registry.Get(recipe.Output.Id).displayName;
                craftStatus.text=recipe==null?"Place ingredients in the grid":"Ready · "+game.Crafting.MaximumCrafts+" craft(s)";
            }
            if(selectedLabel!=null){var stack=game.Inventory.Slots[game.Selected];selectedLabel.text=stack.Empty?"BARE HAND":game.Registry.Get(stack.Id).displayName+"  ·  "+stack.Count;}
            RefreshTooltip();
            lastRevision=game.Inventory.Revision;lastCraftRevision=game.Crafting.Grid.Revision;lastStationRevision=StationRevision;lastEquipmentRevision=game.Equipment.Revision;lastSelected=game.Selected;
        }
        void Update()
        {
            if(game==null)return;
            if(game.InventoryOpen && !HasInventoryBinding) { game.SetMode(ScreenMode.Play); return; }
            PrewarmInventory();
            if (Mouse.current?.rightButton.isPressed != true) EndRightPaint();
            UpdateBrowserInput();
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
        void LateUpdate()
        {
            // EventSystem may run after Update. Publish the held item after this frame's
            // clicks/drags so pickup and count changes do not wait another rendered frame.
            if(game==null)return;
            if(heldRoot!=null)
            {
                var cursorStack=creativeDrag.Empty?HeldStack:creativeDrag;
                heldRoot.gameObject.SetActive(!cursorStack.Empty);
                if(!cursorStack.Empty&&Mouse.current!=null)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(root,Mouse.current.position.ReadValue(),null,out var point);
                    heldRoot.localPosition=new Vector3(point.x+10,point.y-10,0);
                    if (shownHeld.Id != cursorStack.Id || shownHeld.Count != cursorStack.Count)
                    { heldIcon.texture=icons[cursorStack.Id];heldLabel.text=cursorStack.Count.ToString();shownHeld=cursorStack; }
                }
            }
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
            if(preview==null)return;
            if(previewBuilt&&previewFemale==game.Player.Female&&previewSkin==game.Player.Skin&&preview.AnimationReady)return;
            preview.Build(game.Player.Female,game.Player.Skin);previewBuilt=preview.AnimationReady;previewFemale=game.Player.Female;previewSkin=game.Player.Skin;
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
                if(FoodVisuals.UsesModel(item.runtimeId))
                {var foodIcon=FoodVisuals.Icon(item.runtimeId);icons[item.runtimeId]=foodIcon;sharedToolIcons.Add(foodIcon);continue;}
                var industrialIcon=Resources.Load<Texture2D>("Industry/Icons/"+OreVisuals.VisualId(item.runtimeId));if(industrialIcon!=null){icons[item.runtimeId]=industrialIcon;sharedToolIcons.Add(industrialIcon);continue;}
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
    public sealed class SlotView : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerClickHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerEnterHandler,IPointerExitHandler
    {
        public bool Shown,ShownSelected;public byte ShownId;public int ShownCount;
        public GameUI Owner;public int Index;public Image Background;public Outline Border;public RawImage Icon;public Text Count;
        bool dragged, rightPressHandled;
        long pressVersion=-1;
        public void OnPointerEnter(PointerEventData e) { if(Owner.AcceptWidget(this)){Owner.HoverSlot(Index); Owner.PaintSlot(Index);} }
        public void OnPointerExit(PointerEventData e) { if(Owner.AcceptWidget(this))Owner.HoverSlot(-1); }
        public void OnPointerDown(PointerEventData e)
        {
            pressVersion=Owner.AcceptWidget(this)?Owner.BindingVersion:-1;
            if(pressVersion<0)return;
            dragged = false;
            if (e.button == PointerEventData.InputButton.Right)
                rightPressHandled = Owner.BeginRightPaint(Index, Keyboard.current?.shiftKey.isPressed == true);
        }
        public void OnPointerUp(PointerEventData e) { if (e.button == PointerEventData.InputButton.Right) Owner.EndRightPaint(); }
        public void OnPointerClick(PointerEventData e)
        {
            if (!Owner.AcceptGesture(this,pressVersion) || (e.button != PointerEventData.InputButton.Left && e.button != PointerEventData.InputButton.Right)) return;
            if (dragged || e.dragging || e.button == PointerEventData.InputButton.Right && rightPressHandled) return;
            Owner.ClickSlot(Index, e.button == PointerEventData.InputButton.Right, Keyboard.current?.shiftKey.isPressed == true);
        }
        public void OnBeginDrag(PointerEventData e)
        {
            if (!Owner.AcceptGesture(this,pressVersion) || (e.button != PointerEventData.InputButton.Left && e.button != PointerEventData.InputButton.Right)) return;
            dragged = true;
            if (e.button == PointerEventData.InputButton.Left && Owner.HeldStack.Empty) Owner.ClickSlot(Index, false, false);
        }
        public void OnDrag(PointerEventData e)
        {
            if (!Owner.AcceptGesture(this,pressVersion) || e.button != PointerEventData.InputButton.Right) return;
            var slot = e.pointerCurrentRaycast.gameObject?.GetComponentInParent<SlotView>();
            if (slot != null && slot.Owner == Owner) Owner.PaintSlot(slot.Index);
        }
        public void OnEndDrag(PointerEventData e)
        {
            if (!Owner.AcceptGesture(this,pressVersion))return;
            if (e.button == PointerEventData.InputButton.Right) { Owner.EndRightPaint(); return; }
            if (e.button != PointerEventData.InputButton.Left) return;
            var slot = e.pointerCurrentRaycast.gameObject?.GetComponentInParent<SlotView>();
            if (slot != null && slot.Owner == Owner) Owner.ClickSlot(slot.Index, false, false);
        }
        void OnDisable() { pressVersion=-1; if (rightPressHandled && Owner != null) Owner.EndRightPaint(); rightPressHandled = dragged = false; }
    }
}
