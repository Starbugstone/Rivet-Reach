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
        public PlayerInput Input {get;private set;}
        public GameUI UI {get;private set;}
        public WorldSound Sound {get;private set;}
        public ScreenMode Mode {get;private set;}=ScreenMode.Title;
        public bool Started {get;private set;}
        public bool InventoryOpen=>Mode==ScreenMode.Inventory;
        public bool Paused=>Mode!=ScreenMode.Play&&Mode!=ScreenMode.Inventory;
        public bool Diagnostics;
        public int Selected;
        public int Seed=246813;
        public string Message {get;private set;}
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
            foreach(var light in FindObjectsByType<Light>())if(light.type==LightType.Directional)
            {
                light.transform.rotation=Quaternion.Euler(42,-35,0);light.intensity=1.35f;light.color=new Color(1,.94f,.83f);
                light.shadows=LightShadows.Soft;Shader.SetGlobalVector("_RRSunDirection",-light.transform.forward);
            }
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.58f,.69f,.82f);RenderSettings.ambientEquatorColor=new Color(.52f,.58f,.63f);RenderSettings.ambientGroundColor=new Color(.34f,.35f,.29f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.61f,.71f,.80f);RenderSettings.fogStartDistance=65;RenderSettings.fogEndDistance=135;
            RenderSettings.skybox=Resources.Load<Material>("Materials/Sky");
            Input=new PlayerInput();Registry=ItemRegistry.Load();Sound=gameObject.AddComponent<WorldSound>();
            CreateSession(Seed);UI=gameObject.AddComponent<GameUI>();UI.Initialize(this);SetMode(ScreenMode.Title);
            if(Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-rr-verify"))gameObject.AddComponent<RuntimeVerification>();
        }
        void CreateSession(int seed)
        {
            Seed=seed;
            Inventory=new Inventory(id=>Registry.Get(id).stackLimit);
            var root=new GameObject("Surface world");root.transform.SetParent(transform,false);World=root.AddComponent<VoxelWorld>();World.Initialize(seed);World.ViewDistance=Mathf.Clamp(PlayerPrefs.GetInt("viewDistance.v2",10),4,14);
            RenderSettings.fogStartDistance=World.FogStart;RenderSettings.fogEndDistance=World.FogEnd;
            var p=new GameObject("Player");p.transform.SetParent(transform,false);Player=p.AddComponent<FirstPersonPlayer>();Player.Initialize(this);
            // Deterministic spawn remains on a supported surface with headroom.
            int h=World.Generator.Height(0,0);Player.transform.position=new Vector3(.5f,h+1.01f,.5f);World.Observer=Player.transform;
            var drops=new GameObject("World item stacks");drops.transform.SetParent(transform,false);Items=drops.AddComponent<DroppedItems>();Items.Initialize(this);
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
            if(Started&&!Paused)World.AdvanceGrass(Time.deltaTime);
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
            if(selected.Empty)return false;
            reason="Waiting for nearby terrain";if(!World.Ready(cell))return false;
            reason="This cell is occupied";if(World.Get(cell)!=0)return false;
            var block=new Bounds(World.Local(cell)+Vector3.one*.5f,Vector3.one*.998f);
            var player=new Bounds(Player.transform.position+Vector3.up*(Player.Height*.5f),new Vector3(.6f,Player.Height,.6f));
            reason="Cannot place inside the player";if(block.Intersects(player))return false;
            reason="Collect the item in this space first";
            foreach(var pile in Items.Piles)
                if(block.Intersects(new Bounds(pile.Position.Local(World.Origin)+Vector3.up*.115f,Vector3.one*.23f)))return false;
            reason="Place "+Registry.Get(selected.Id).displayName;return true;
        }
        public bool TryPlaceSelected()
        {
            if(Mode!=ScreenMode.Play||Player.Inspecting)return false;
            if(!PlacementPreview(out var cell,out string reason)){Notify(reason,1);return false;}
            var selected=Inventory.Slots[Selected];
            // One local authority turn: recheck occupancy, commit the voxel, then consume exactly one.
            if(!World.Place(cell,selected.Id))return false;
            Inventory.Take(Selected,1);Sound.Mine();Notify("Placed "+Registry.Get(selected.Id).displayName,1);return true;
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

    public sealed class WorldSound : MonoBehaviour
    {
        AudioSource source;AudioClip step,mine,pickup;float lastPickup;
        void Awake()
        {
            source=gameObject.AddComponent<AudioSource>();source.spatialBlend=0;source.volume=.26f;
            step=Make("Stone footstep",120,.07f,.6f);mine=Make("Fist impact",75,.16f,.8f);pickup=Make("Pickup",720,.065f,.1f);
        }
        static AudioClip Make(string name,float hz,float duration,float noise)
        {
            int n=(int)(22050*duration);float[] data=new float[n];var random=new System.Random(83);
            for(int i=0;i<n;i++){float envelope=Mathf.Pow(1-i/(float)n,2);data[i]=(Mathf.Sin(i*hz*2*Mathf.PI/22050)*(1-noise)+(float)(random.NextDouble()*2-1)*noise)*envelope*.4f;}
            var clip=AudioClip.Create(name,n,1,22050,false);clip.SetData(data,0);return clip;
        }
        public void Step()=>source.PlayOneShot(step);
        public void Mine()=>source.PlayOneShot(mine);
        public void Pickup(){if(Time.unscaledTime-lastPickup<.08f)return;lastPickup=Time.unscaledTime;source.PlayOneShot(pickup);}
        void OnDestroy(){if(step!=null)Destroy(step);if(mine!=null)Destroy(mine);if(pickup!=null)Destroy(pickup);}
    }
}
