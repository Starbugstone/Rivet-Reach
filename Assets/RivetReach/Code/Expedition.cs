using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RivetReach
{
    public enum ScreenMode { Title, Play, Inventory, Pause, Appearance, Settings, Controls }
    public sealed class Expedition : MonoBehaviour
    {
        public static Expedition Instance;
        public VoxelWorld World {get;private set;}
        public FirstPersonPlayer Player {get;private set;}
        public DroppedItems Items {get;private set;}
        public Inventory Inventory {get;private set;}
        public ItemRegistry Registry {get;private set;}
        public RecipeRegistry Recipes {get;private set;}
        public CraftingSession Crafting {get;private set;}
        public PlayerInput Input {get;private set;}
        public GameUI UI {get;private set;}
        public WorldSound Sound {get;private set;}
        public DayNightCycle Sky {get;private set;}
        public ScreenMode Mode {get;private set;}=ScreenMode.Title;
        public bool Started {get;private set;}
        public bool InventoryOpen=>Mode==ScreenMode.Inventory;
        public bool Paused=>Mode!=ScreenMode.Play&&Mode!=ScreenMode.Inventory;
        public bool Diagnostics;
        public int Selected;
        public int Seed {get;private set;}
        public static int NewRandomSeed()=>BitConverter.ToInt32(Guid.NewGuid().ToByteArray(),0)&int.MaxValue;
        public string Message {get;private set;}
        public string PlacementDiagnostic {get;private set;}
        const string PlayerOverlapReason="Cannot place inside the player";
        float messageUntil;
        public bool ReadyToPlay => Player!=null&&World.Ready(World.Address(Player.transform.position))&&World.Ready(World.Address(Player.transform.position+Vector3.up*2));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-rr-avatar-verify")){new GameObject("Player art verification").AddComponent<AvatarVerification>();return;}
            if(FindAnyObjectByType<Expedition>()==null)new GameObject("Rivet Reach").AddComponent<Expedition>();
        }
        void Awake()
        {
            Instance=this;Application.targetFrameRate=90;QualitySettings.vSyncCount=0;
            foreach(var camera in FindObjectsByType<Camera>())camera.gameObject.SetActive(false);
            Sky=gameObject.AddComponent<DayNightCycle>();Sky.Initialize();
            Input=new PlayerInput();Registry=ItemRegistry.Load();Recipes=RecipeCatalogAsset.Load().Compile(Registry);Sound=gameObject.AddComponent<WorldSound>();
            CreateSession(NewRandomSeed());
            var effects=new GameObject("Arcade presentation");effects.transform.SetParent(transform,false);effects.AddComponent<ArcadePresentation>();
            UI=gameObject.AddComponent<GameUI>();UI.Initialize(this);SetMode(ScreenMode.Title);
            if(Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-rr-verify"))gameObject.AddComponent<RuntimeVerification>();
        }
        void CreateSession(int seed)
        {
            Seed=seed;
            Sky.ResetClock();
            Inventory=new Inventory(id=>Registry.Get(id).stackLimit);
            Crafting=new CraftingSession(Recipes,2,id=>Registry.Get(id).stackLimit);
            Inventory.Add(BlockId.StarterDagger,1,9,10);
            Inventory.Add(BlockId.StarterPickaxe,1,10,11);
            Inventory.Add(BlockId.StarterAxe,1,11,12);
            var root=new GameObject("Surface world");root.transform.SetParent(transform,false);World=root.AddComponent<VoxelWorld>();World.Initialize(seed);World.ViewDistance=Mathf.Clamp(PlayerPrefs.GetInt("viewDistance.v2",10),4,14);
            RenderSettings.fogStartDistance=World.FogStart;RenderSettings.fogEndDistance=World.FogEnd;
            var p=new GameObject("Player");p.transform.SetParent(transform,false);Player=p.AddComponent<FirstPersonPlayer>();Player.Initialize(this);
            // Deterministic spawn remains on a supported surface with headroom.
            int h=World.Generator.Height(0,0);Player.transform.position=new Vector3(.5f,h+1.01f,.5f);World.Observer=Player.transform;
            var drops=new GameObject("World item stacks");drops.transform.SetParent(transform,false);Items=drops.AddComponent<DroppedItems>();Items.Initialize(this);
            World.BlockMined+=SpawnMinedDrop;
            World.OriginShifted+=Sound.ShiftOrigin;
        }
        void SpawnMinedDrop(BlockPos pos,byte id)
        {
            byte drop=Registry.FistDrop(id);
            Items.Spawn(new ItemStack(drop,1),World.Local(pos)+new Vector3(.5f,.3f,.5f),Vector3.up*1.6f);
        }
        public void StartSession(int seed)
        {
            if(seed!=Seed)
            {
                World.Stop();Destroy(World.gameObject);Destroy(Player.gameObject);Destroy(Items.gameObject);CreateSession(seed);UI.RefreshPreview();
            }
            Started=true;SetMode(ScreenMode.Play);
        }
        public void SetMode(ScreenMode mode)
        {
            if(InventoryOpen&&mode!=ScreenMode.Inventory)UI.ReturnHeld();
            Mode=mode;Time.timeScale=Paused?0:1;
            bool capture=Mode==ScreenMode.Play;if(Player!=null)Player.Arms.gameObject.SetActive(capture&&!Player.Inspecting);Cursor.lockState=capture?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!capture;
            UI?.Rebuild();
        }
        void Update()
        {
            if(Input==null)return;
            if(Started&&!Paused){Sky.Advance(Time.deltaTime);World.AdvanceGrass(Time.deltaTime);World.AdvanceTrees(Time.deltaTime);}
            if(Input.PollRebind()){UI.Rebuild();return;}
            if(Input.Pressed("Pause"))SetMode(Mode==ScreenMode.Play?ScreenMode.Pause:Started?ScreenMode.Play:ScreenMode.Title);
            if(Started&&Input.Pressed("Inventory"))SetMode(InventoryOpen?ScreenMode.Play:ScreenMode.Inventory);
            if(Input.Pressed("Diagnostics"))Diagnostics=!Diagnostics;
            if(Mode==ScreenMode.Play)
            {
                if(Input.Pressed("Previous slot"))Selected=(Selected+11)%12;
                if(Input.Pressed("Next slot"))Selected=(Selected+1)%12;
                if(Mouse.current!=null){float scroll=Mouse.current.scroll.ReadValue().y;if(scroll!=0)Selected=(Selected+(scroll>0?-1:1)+12)%12;}
                if(Keyboard.current!=null)
                {
                    Key[] digits={Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6,Key.Digit7,Key.Digit8,Key.Digit9,Key.Digit0};
                    for(int i=0;i<digits.Length;i++)if(Keyboard.current[digits[i]].wasPressedThisFrame)Selected=i;
                }
                if(Input.Pressed("Drop"))Drop(Inventory.Take(Selected,Keyboard.current?.shiftKey.isPressed==true?int.MaxValue:1));
            }
            if(Time.unscaledTime>messageUntil)Message=null;
            if(World.Error!=null)Notify("Terrain worker error: "+World.Error,10);
        }
        public bool PlacementPreview(out BlockPos cell,out string reason)
        {
            cell=default;reason="Aim at a block face";
            if(!World.Raycast(Player.Camera.transform.position,Player.Camera.transform.forward,5,out var support,out _,out var face)||face==Vector3Int.zero)return false;
            cell=support.Offset(face.x,face.y,face.z);
            return CanPlace(cell,out reason);
        }
        public bool CanPlace(BlockPos cell,out string reason)
        {
            var selected=Inventory.Slots[Selected];reason="Select a terrain block in the hotbar";
            if(selected.Empty||!BlockId.Placeable(selected.Id))return false;
            reason="Waiting for nearby terrain";if(!World.Ready(cell))return false;
            reason="This cell is occupied";if(World.Get(cell)!=0)return false;
            // Use the movement collider's exact occupied-cell rule, including its skin.
            // Touching the supporting face is legal; occupying the player's body is not.
            reason=PlayerOverlapReason;if(World.OccupiesCell(Player.transform.position,.6f,Player.Height,cell))return false;
            reason="Place "+Registry.Get(selected.Id).displayName;return true;
        }
        public bool TryPlaceSelected()
        {
            if(Mode!=ScreenMode.Play||Player.Inspecting)return false;
            if(!PlacementPreview(out var cell,out string reason))
            {PlacementDiagnostic=reason;if(reason!=PlayerOverlapReason)Notify(reason,1);return false;}
            var selected=Inventory.Slots[Selected];
            // One local authority turn: recheck occupancy, commit the voxel, then consume exactly one.
            if(!World.Place(cell,selected.Id))return false;
            Inventory.Take(Selected,1);Sound.Place(selected.Id,World.Local(cell)+Vector3.one*.5f);ArcadePresentation.Active?.Place(World.Local(cell)+Vector3.one*.5f,selected.Id);PlacementDiagnostic="Placed "+Registry.Get(selected.Id).displayName;Notify(PlacementDiagnostic,1);return true;
        }
        public void Drop(ItemStack stack)
        {
            if(stack.Empty)return;
            Vector3 pos=Player.Camera.transform.position+Player.Camera.transform.forward*.6f;
            if(World.Overlaps(pos,.2f,.2f))pos=Player.transform.position+Vector3.up*.7f;
            Items.Spawn(stack,pos,Player.Camera.transform.forward*3+Vector3.up,0.75f);Sound.Pickup();
        }
        public void Notify(string text,float seconds)
        {if(Message==text&&Time.unscaledTime<messageUntil)return;Message=text;messageUntil=Time.unscaledTime+seconds;}
        public void SetAppearance(bool female,int skin)
        {Player.Female=female;Player.Skin=skin;Player.RefreshAppearance();UI.RefreshPreview();}
        public void Quit(){PlayerPrefs.Save();Application.Quit();}
        void OnDestroy(){Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;if(Instance==this)Instance=null;}
    }

}
