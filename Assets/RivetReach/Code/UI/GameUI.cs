using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed class GameUI : MonoBehaviour
    {
        Expedition game;
        Canvas canvas;
        RectTransform root;
        Font font;
        readonly Color ink=new Color(.065f,.105f,.12f,.97f),slate=new Color(.13f,.19f,.21f,1),gold=new Color(.85f,.69f,.38f),pale=new Color(.91f,.92f,.85f);
        readonly List<SlotView> slots=new List<SlotView>();
        readonly Dictionary<byte,Texture2D> icons=new Dictionary<byte,Texture2D>();
        Text message,diagnostics,targetLabel,heldLabel,selectedLabel,loading,tooltip;
        Image progress;
        RectTransform heldRoot;
        RawImage heldIcon;
        public ItemStack HeldStack;
        int lastRevision=-1;
        RenderTexture previewTexture;
        GameObject previewRoot;
        AvatarView preview;
        float frameAverage;
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
            if(root!=null)Destroy(root.gameObject);slots.Clear();message=null;diagnostics=null;targetLabel=null;progress=null;heldRoot=null;heldLabel=null;loading=null;tooltip=null;lastRevision=-1;
            if(previewRoot!=null)previewRoot.SetActive(game.Mode==ScreenMode.Inventory||game.Mode==ScreenMode.Appearance);
            root=Rect(canvas.transform,"Screen",0,0,1280,720);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
            float scale=Mathf.Clamp(PlayerPrefs.GetFloat("uiScale",1),.85f,1);root.localScale=Vector3.one*scale;
            if(game.Mode==ScreenMode.Play)BuildHUD();
            else if(game.Mode==ScreenMode.Inventory)BuildInventory();
            else if(game.Mode==ScreenMode.Title)BuildTitle();
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
            var field=fieldBackground.gameObject.AddComponent<InputField>();field.textComponent=Label(fieldBackground.transform,"",12,5,340,35,21);field.contentType=InputField.ContentType.IntegerNumber;field.text=game.Seed.ToString();field.characterLimit=10;
            Button(p.transform,"START EXPEDITION",36,430,365,50,()=>{int seed=int.TryParse(field.text,out int n)?n:game.Seed;game.StartSession(seed);},true);
            Button(p.transform,"PLAYER",36,494,112,40,()=>game.SetMode(ScreenMode.Appearance));
            Button(p.transform,"SETTINGS",161,494,114,40,()=>game.SetMode(ScreenMode.Settings));
            Button(p.transform,"QUIT",289,494,112,40,game.Quit);
            Label(p.transform,"Early POC · Session progress resets on quit",36,565,372,35,14,new Color(.72f,.76f,.73f));
            Label(root,"TERRAIN / MOVEMENT / DISCOVERY",735,632,480,24,16,gold);
            Label(root,"An open world, one playable step at a time.",735,661,480,28,20);
        }
        void BuildHUD()
        {
            Label(root,"RIVET REACH",28,22,300,34,24);
            Label(root,"FIRST EXPEDITION",29,58,280,22,12,gold);
            Label(root,"+",625,340,30,30,24).alignment=TextAnchor.MiddleCenter;
            targetLabel=Label(root,"",475,395,330,28,17);targetLabel.alignment=TextAnchor.MiddleCenter;
            var track=Panel(root,580,385,120,3,new Color(.1f,.15f,.16f,.5f));progress=Panel(track.transform,0,0,0,3,gold);
            BuildHotbar(root,256,628,60,4);
            selectedLabel=Label(root,"",420,605,440,28,18);selectedLabel.alignment=TextAnchor.MiddleCenter;
            Label(root,$"{game.Input.Keys["Inventory"]}  Inventory    {game.Input.Keys["Drop"]}  Drop    {game.Input.Keys["Inspect"]}  Inspect player",28,694,650,22,13);
            Label(root,"Session-only world",1090,694,172,22,12,new Color(.75f,.77f,.73f));
            message=Label(root,"",330,555,620,40,18,gold);message.alignment=TextAnchor.MiddleCenter;
            loading=Label(root,"",435,457,410,50,20);loading.alignment=TextAnchor.MiddleCenter;
            diagnostics=Label(root,"",28,103,650,190,14);
        }
        void BuildInventory()
        {
            Panel(root,0,0,1280,720,new Color(0,0,0,.23f));var p=Panel(root,55,42,1170,634,ink);
            Label(p.transform,"INVENTORY",28,20,500,45,30);Label(p.transform,"Drag stacks · Right-click split / place one · Shift-click transfer",28,66,880,28,15,gold);
            Button(p.transform,"CLOSE",1033,20,108,40,()=>game.SetMode(ScreenMode.Play));
            var backdrop=Panel(p.transform,28,113,210,388,slate);var portrait=Rect(backdrop.transform,"Portrait",0,0,210,388).gameObject.AddComponent<RawImage>();portrait.texture=previewTexture;portrait.gameObject.AddComponent<PortraitDrag>().Owner=this;
            for(int row=0;row<6;row++)for(int col=0;col<8;col++)Slot(p.transform,12+row*8+col,266+col*62,113+row*62,56);
            Label(p.transform,"CRAFTING",821,115,300,42,27);Label(p.transform,"Coming later",821,162,290,35,18,new Color(.55f,.61f,.6f));
            for(int row=0;row<2;row++)for(int col=0;col<2;col++)Panel(p.transform,829+col*70,222+row*70,60,60,new Color(.16f,.20f,.21f,.6f));
            Label(p.transform,"Placeholder only",829,385,275,30,16,new Color(.55f,.61f,.6f));
            Label(p.transform,"HOTBAR",266,502,250,24,14,gold);BuildHotbar(p.transform,266,534,53,4);
            Label(p.transform,"Select a stack, then click another slot to move it. Crafting slots are inactive.",28,604,1090,24,14);
            Button(p.transform,"APPEARANCE",28,527,210,40,()=>game.SetMode(ScreenMode.Appearance));
            heldRoot=Rect(root,"Held stack",0,0,52,65);heldRoot.gameObject.SetActive(false);heldIcon=heldRoot.gameObject.AddComponent<RawImage>();heldIcon.raycastTarget=false;heldLabel=Label(heldRoot,"",0,37,55,25,16);heldLabel.alignment=TextAnchor.LowerRight;
            tooltip=Label(p.transform,"",28,576,900,24,16,gold);
            RefreshPreview();
        }
        public void HoverSlot(int index)
        {if(tooltip==null)return;var stack=index>=0?game.Inventory.Slots[index]:default;tooltip.text=stack.Empty?"":game.Registry.Get(stack.Id).displayName+" · "+stack.Count+" / "+game.Registry.Get(stack.Id).stackLimit;}
        public void RotatePreview(float delta){if(preview!=null)preview.transform.Rotate(0,-delta*.6f,0,Space.World);}
        void BuildHotbar(Transform parent,float x,float y,int size,int gap)
        {for(int i=0;i<12;i++)Slot(parent,i,x+i*(size+gap),y,size);}
        void Slot(Transform parent,int index,float x,float y,int size)
        {
            var image=Panel(parent,x,y,size,size,slate);var slot=image.gameObject.AddComponent<SlotView>();slot.Owner=this;slot.Index=index;slot.Background=image;
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
                Button(p.transform,"QUIT — SESSION WILL RESET",180,442,430,46,game.Quit);
                Label(p.transform,"Terrain edits and inventory survive chunk unloading,\nbut this first slice does not yet save progress between sessions.",115,530,610,60,17);
            }
            else if(game.Mode==ScreenMode.Appearance)
            {
                var portrait=Rect(p.transform,"Portrait",35,105,290,465).gameObject.AddComponent<RawImage>();portrait.texture=previewTexture;portrait.gameObject.AddComponent<PortraitDrag>().Owner=this;
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
                Slider(p.transform,"Look sensitivity",112,game.Input.Sensitivity,.03f,.25f,v=>{game.Input.Sensitivity=v;PlayerPrefs.SetFloat("sensitivity",v);});
                Slider(p.transform,"Field of view",212,game.Player.Camera.fieldOfView,65,95,v=>{game.Player.Camera.fieldOfView=v;PlayerPrefs.SetFloat("fov",v);});
                Slider(p.transform,"View distance (chunks)",312,game.World.ViewDistance,2,6,v=>{game.World.ViewDistance=Mathf.RoundToInt(v);PlayerPrefs.SetInt("viewDistance",game.World.ViewDistance);RenderSettings.fogStartDistance=game.World.ViewDistance*16;RenderSettings.fogEndDistance=game.World.ViewDistance*32+7;},true);
                Slider(p.transform,"Interface scale",412,Mathf.Clamp(PlayerPrefs.GetFloat("uiScale",1),.85f,1),.85f,1,v=>{PlayerPrefs.SetFloat("uiScale",v);root.localScale=Vector3.one*v;});
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
                Label(p.transform,"Mouse wheel: all 12 hotbar slots\n1–0: first 10 slots · Shift + drop: full stack",410,527,340,60,15);
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
        {if(!game.InventoryOpen)return;if(shift&&HeldStack.Empty)game.Inventory.QuickTransfer(index);else game.Inventory.Click(index,ref HeldStack,right);RefreshSlots();}
        public void ReturnHeld()
        {
            if(HeldStack.Empty)return;int left=game.Inventory.Add(HeldStack.Id,HeldStack.Count);if(left>0)game.Drop(new ItemStack(HeldStack.Id,left));HeldStack.Clear();
        }
        void RefreshSlots()
        {
            foreach(var view in slots)
            {
                var stack=game.Inventory.Slots[view.Index];view.Icon.enabled=!stack.Empty;view.Count.text=stack.Empty?"":stack.Count.ToString();
                if(!stack.Empty)view.Icon.texture=icons[stack.Id];
                view.Background.color=view.Index==game.Selected?new Color(.40f,.43f,.31f):slate;
            }
            lastRevision=game.Inventory.Revision;
        }
        void Update()
        {
            if(game==null)return;
            if(lastRevision!=game.Inventory.Revision||game.Mode==ScreenMode.Play)RefreshSlots();
            if(message!=null)message.text=game.Message??"";
            if(loading!=null)loading.text=!game.ReadyToPlay?"Preparing nearby terrain…":"";
            if(targetLabel!=null)targetLabel.text=game.Player.HasTarget?game.Registry.Get(game.Player.TargetId).displayName:"";
            if(progress!=null)progress.rectTransform.sizeDelta=new Vector2(120*Mathf.Clamp01(game.Player.MiningProgress),3);
            if(selectedLabel!=null){var s=game.Inventory.Slots[game.Selected];selectedLabel.text=s.Empty?"BARE HANDS":game.Registry.Get(s.Id).displayName+"  ·  "+s.Count;}
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
            if(diagnostics!=null)diagnostics.text=game.Diagnostics?$"{1/Mathf.Max(.001f,frameAverage):0} fps · {frameAverage*1000:0.0} ms\nWorld: {TerrainGenerator.WorldId} · seed {game.Seed} · {game.World.Address(game.Player.transform.position)}\nChunks {game.World.ReadyCount}/{game.World.ResidentCount} · queue {game.World.PendingCount}\nGeneration + mesh {game.World.LastBuildMs:0.0} ms · edit mesh {game.World.LastEditMeshMs:0.0} ms\nTriangles {game.World.MeshTriangles:N0} · changes {game.World.EditCount} · piles {game.Items.Piles.Count}\nStale jobs rejected {game.World.RejectedJobs} · origin {game.World.Origin}":"";
            if(preview!=null&&previewRoot.activeSelf)preview.Animate(.12f,false,Time.unscaledTime*2);
        }
        void CreatePreview()
        {
            previewRoot=new GameObject("Appearance preview");previewRoot.transform.position=new Vector3(0,-1000,0);
            var model=new GameObject("Preview model");model.transform.SetParent(previewRoot.transform,false);model.transform.localRotation=Quaternion.Euler(0,-18,0);preview=model.AddComponent<AvatarView>();preview.Preview=true;
            var cam=new GameObject("Portrait camera");cam.transform.SetParent(previewRoot.transform,false);cam.transform.localPosition=new Vector3(0,.94f,2.8f);cam.transform.LookAt(previewRoot.transform.position+Vector3.up*.92f);
            var camera=cam.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=slate;camera.fieldOfView=38;camera.nearClipPlane=.1f;camera.farClipPlane=8;camera.cullingMask=1<<30;
            var fillLight=new GameObject("Portrait fill");fillLight.transform.SetParent(previewRoot.transform,false);fillLight.transform.localPosition=new Vector3(.5f,1.5f,2);
            var lamp=fillLight.AddComponent<Light>();lamp.type=LightType.Point;lamp.range=5;lamp.intensity=3;lamp.cullingMask=1<<30;lamp.shadows=LightShadows.None;
            previewTexture=new RenderTexture(384,512,24);previewTexture.Create();camera.targetTexture=previewTexture;
            game.Player.Camera.cullingMask&=~(1<<30);RefreshPreview();
        }
        public void RefreshPreview()
        {
            if(preview==null)return;preview.Build(game.Player.Female,game.Player.Skin);
            foreach(var t in preview.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
        }
        void BuildIcons()
        {
            foreach(var item in game.Registry.items)
            {
                var texture=new Texture2D(48,48,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Point;var pixels=new Color[48*48];
                for(int y=0;y<48;y++)for(int x=0;x<48;x++)
                {
                    float dx=x-24,dy=y-27;
                    if(Mathf.Abs(dx)/21+Mathf.Abs(dy)/11<1)pixels[x+y*48]=item.colour*1.15f;
                    else if(y>=6&&y<27-Mathf.Abs(dx)*.5f&&Mathf.Abs(dx)<21)pixels[x+y*48]=item.colour*(x<24?.75f:.93f);
                }
                texture.SetPixels(pixels);texture.Apply();icons[item.runtimeId]=texture;
            }
        }
        void OnDestroy(){foreach(var t in icons.Values)Destroy(t);if(previewTexture!=null){previewTexture.Release();Destroy(previewTexture);}if(previewRoot!=null)Destroy(previewRoot);}
    }
    public sealed class PortraitDrag : MonoBehaviour,IDragHandler
    {public GameUI Owner;public void OnDrag(PointerEventData e)=>Owner.RotatePreview(e.delta.x);}
    public sealed class SlotView : MonoBehaviour,IPointerClickHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerEnterHandler,IPointerExitHandler
    {
        public GameUI Owner;public int Index;public Image Background;public RawImage Icon;public Text Count;
        public void OnPointerEnter(PointerEventData e)=>Owner.HoverSlot(Index);
        public void OnPointerExit(PointerEventData e)=>Owner.HoverSlot(-1);
        bool dragged;
        public void OnPointerClick(PointerEventData e){if(dragged){dragged=false;return;}Owner.ClickSlot(Index,e.button==PointerEventData.InputButton.Right,Keyboard.current?.shiftKey.isPressed==true);}
        public void OnBeginDrag(PointerEventData e){dragged=true;if(Owner.HeldStack.Empty)Owner.ClickSlot(Index,false,false);}
        public void OnDrag(PointerEventData e){}
        public void OnEndDrag(PointerEventData e)
        {var target=e.pointerCurrentRaycast.gameObject;if(target!=null){var slot=target.GetComponentInParent<SlotView>();if(slot!=null)Owner.ClickSlot(slot.Index,false,false);}dragged=false;}
    }
}
