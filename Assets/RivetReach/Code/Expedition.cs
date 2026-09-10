using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RivetReach
{
    public enum ScreenMode { Title, Play, Inventory, Pause, Appearance, Settings, Controls, Death }
    public sealed partial class Expedition : MonoBehaviour
    {
        public static Expedition Instance;
        public VoxelWorld World {get;private set;}
        public MobSystem Mobs {get;private set;}
        public FirstPersonPlayer Player {get;private set;}
        public DroppedItems Items {get;private set;}
        public Inventory Inventory {get;private set;}
        public ItemRegistry Registry {get;private set;}
        public RecipeRegistry Recipes {get;private set;}
        public CraftingSession PersonalCrafting {get;private set;}
        public CraftingSession Crafting=>OpenStation?.Crafting??PersonalCrafting;
        public ProcessingRegistry Processing {get;private set;}
        public WorldSurvival Survival {get;private set;}
        public StationState OpenStation {get;private set;}
        public BlockPos StationPosition {get;private set;}
        public HungerState Hunger {get;private set;}
        public HealthState Health {get;private set;}
        public EquipmentState Equipment {get;private set;}
        public event Action Respawned;
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
        float invulnerableUntil;
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
            Input=new PlayerInput();Registry=ItemRegistry.Load();Recipes=RecipeCatalogAsset.Load().Compile(Registry);Processing=ProcessingCatalogAsset.Load().Compile(Registry);Sound=gameObject.AddComponent<WorldSound>();
            CreateSession(NewRandomSeed());
            var effects=new GameObject("Arcade presentation");effects.transform.SetParent(transform,false);effects.AddComponent<ArcadePresentation>();
            UI=gameObject.AddComponent<GameUI>();UI.Initialize(this);SetMode(ScreenMode.Title);
            if(Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-rr-verify"))gameObject.AddComponent<RuntimeVerification>();
        }
        void CreateSession(int seed)
        {
            Seed=seed;Creative=false;
            invulnerableUntil=0;
            Sky.ResetClock();
            Inventory=new Inventory(id=>Registry.Get(id).stackLimit);
            OpenMachine=null;OpenStation=null;PersonalCrafting=new CraftingSession(Recipes,2,id=>Registry.Get(id).stackLimit);
            Hunger=new HungerState();Health=new HealthState();Equipment=new EquipmentState(Registry.Get);
            var root=new GameObject("Surface world");root.transform.SetParent(transform,false);World=root.AddComponent<VoxelWorld>();World.Initialize(seed);World.ViewDistance=Mathf.Clamp(PlayerPrefs.GetInt("viewDistance.v2",10),4,14);
            RenderSettings.fogStartDistance=World.FogStart;RenderSettings.fogEndDistance=World.FogEnd;
            var p=new GameObject("Player");p.transform.SetParent(transform,false);Player=p.AddComponent<FirstPersonPlayer>();Player.Initialize(this);
            // Deterministic spawn remains on a supported surface with headroom.
            int h=World.Generator.Height(0,0);Player.transform.position=new Vector3(.5f,h+1.01f,.5f);World.Observer=Player.transform;
            var drops=new GameObject("World item stacks");drops.transform.SetParent(transform,false);Items=drops.AddComponent<DroppedItems>();Items.Initialize(this);
            World.BlockMined+=SpawnMinedDrop;
            World.OriginShifted+=Sound.ShiftOrigin;
            Survival=new WorldSurvival(this);Industry=new WorldIndustry(this);root.AddComponent<IndustryPresentation>().Initialize(this);root.AddComponent<MultiblockPresentation>().Initialize(this);
            Mobs=root.AddComponent<MobSystem>();Mobs.Initialize(this);
        }
        void SpawnMinedDrop(BlockPos pos,byte id)
        {
            byte drop=Registry.FistDrop(id);
            int count=id==BlockId.MaturePotatoPlant?2+(int)(TerrainGenerator.Hash(pos.X,pos.Y,pos.Z,Seed)%3):1;
            Items.Spawn(new ItemStack(drop,count),World.Local(pos)+new Vector3(.5f,.3f,.5f),Vector3.up*1.6f,actionCreated:true);
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
            if(Health?.Dead==true&&mode!=ScreenMode.Title)mode=ScreenMode.Death;
            if(InventoryOpen&&mode!=ScreenMode.Inventory)UI.ReturnHeld();
            if(mode!=ScreenMode.Inventory){OpenStation=null;OpenMachine=null;}
            Mode=mode;Time.timeScale=Paused?0:1;
            bool capture=Mode==ScreenMode.Play;if(Player!=null)Player.Arms.gameObject.SetActive(capture&&!Player.Inspecting);Cursor.lockState=capture?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!capture;
            UI?.Rebuild();
        }
        void Update()
        {
            if(Input==null)return;
            if(Started&&!Paused){Sky.Advance(Time.deltaTime);World.AdvanceGrass(Time.deltaTime);World.AdvanceTrees(Time.deltaTime);World.AdvanceFluids(Time.deltaTime);int ticks=Survival.Advance(Time.deltaTime);Industry.Advance(ticks);if(!Creative)Health.Advance(ticks,Hunger);}
            if((OpenStation!=null||OpenMachine!=null)&&(!World.Ready(StationPosition)||(World.Local(StationPosition)+Vector3.one*.5f-Player.transform.position).sqrMagnitude>36))SetMode(ScreenMode.Play);
            if(Health.Dead)return;
            if(Input.PollRebind()){UI.Rebuild();return;}
            if(Input.Pressed("Pause"))SetMode(Mode==ScreenMode.Play?ScreenMode.Pause:Started?ScreenMode.Play:ScreenMode.Title);
            if(Started&&!UI.EditingText&&Input.Pressed("Inventory"))SetMode(InventoryOpen?ScreenMode.Play:ScreenMode.Inventory);
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
        public bool TryInteractTarget()
        {
            if(Mode!=ScreenMode.Play||Health.Dead)return false;
            var eye=Player.Camera.transform;
            return World.Raycast(eye.position,eye.forward,5,out var position,out var id)&&(IndustryId.Placed(id)?TryOpenMachine(position):BlockId.Station(id)&&TryOpenStation(position));
        }
        public bool TryOpenStation(BlockPos position)
        {
            if(Mode!=ScreenMode.Play||Health.Dead||!World.Ready(position)||
                (World.Local(position)+Vector3.one*.5f-Player.Camera.transform.position).sqrMagnitude>36)return false;
            var station=Survival.At(position);if(station==null)return false;
            // An exposed edge/top is a valid target even when the block's center is hidden.
            // Direct commands also validate visibility; cached addresses cannot open through walls.
            var eye=Player.Camera.transform.position;var direction=(World.Local(position)+Vector3.one*.5f-eye).normalized;
            bool aimed=World.Raycast(eye,Player.Camera.transform.forward,5,out var visible,out var id)&&visible.Equals(position)&&id==station.Block;
            if(!aimed&&(!World.Raycast(eye,direction,5,out visible,out id)||!visible.Equals(position)||id!=station.Block))return false;
            OpenStation=station;StationPosition=position;SetMode(ScreenMode.Inventory);return true;
        }
        public float TakeDamage(float amount,DamageKind kind=DamageKind.Impact)
        {
            if(Creative||!Started||Paused||Health.Dead||Time.time<invulnerableUntil)return 0;
            float accepted=Health.Damage(amount,kind,Equipment.Protection);
            if(!Health.Dead)return accepted;
            SetMode(ScreenMode.Death);
            for(int i=0;i<Inventory.Count;i++)Drop(Inventory.Take(i,int.MaxValue));
            for(int i=0;i<PersonalCrafting.Grid.Count;i++)Drop(PersonalCrafting.Grid.Take(i,int.MaxValue));
            for(int i=0;i<4;i++)Drop(Equipment.Take(i));
            return accepted;
        }
        public void Respawn()
        {
            if(!Health.Dead)return;
            if(!TrySafeRespawn(out var spawn)){Notify("No safe respawn location found. Clear space near the original spawn.",8);return;}
            Health.Respawn();Hunger=new HungerState();invulnerableUntil=Time.time+2;
            Player.ResetMotion();Player.transform.position=World.Local(spawn)+new Vector3(.5f,.02f,.5f);SetMode(ScreenMode.Play);Respawned?.Invoke();
        }
        bool TrySafeRespawn(out BlockPos spawn)
        {
            spawn=default;
            // Search around the original spawn without deleting construction or creating blocks.
            for(int radius=0;radius<=16;radius++)for(int z=-radius;z<=radius;z++)for(int x=-radius;x<=radius;x++)
            {
                if(Math.Max(Math.Abs(x),Math.Abs(z))!=radius)continue;
                int h=World.Generator.Height(x,z);
                for(int y=Math.Min(TerrainGenerator.MaxY-2,h+32);y>=h-8;y--)
                {
                    var p=new BlockPos(x,y,z);
                    if(BlockId.Solid(World.Get(p.Offset(0,-1,0)))&&!BlockId.Solid(World.Get(p))&&!BlockId.Solid(World.Get(p.Offset(0,1,0)))){spawn=p;return true;}
                }
            }
            // An extensively excavated/covered spawn is recoverable in an untouched nearby column.
            for(int x=32;x<TerrainGenerator.HorizontalLimit;x*=2)
            {
                int h=World.Generator.Height(x,0);var p=new BlockPos(x,h+1,0);
                if(BlockId.Solid(World.Get(p.Offset(0,-1,0)))&&!BlockId.Solid(World.Get(p))&&!BlockId.Solid(World.Get(p.Offset(0,1,0)))){spawn=p;return true;}
            }
            return false;
        }
        public bool PlacementPreview(out BlockPos cell,out string reason)
        {
            cell=default;reason="Aim at a block face";
            if(!World.Raycast(Player.Camera.transform.position,Player.Camera.transform.forward,5,out var support,out _,out var face)||face==Vector3Int.zero)return false;
            cell=support.Offset(face.x,face.y,face.z);
            if(!CanPlace(cell,out reason))return false;
            if(Inventory.Slots[Selected].Id==BlockId.Torch&&!World.CanPlaceTorch(cell,support))
            {reason="Torches need a dry floor or wall face";return false;}
            return true;
        }
        public bool CanPlace(BlockPos cell,out string reason)
        {
            var selected=Inventory.Slots[Selected];reason="Select a terrain block in the hotbar";
            if(selected.Empty||!BlockId.Placeable(selected.Id))return false;
            reason="Waiting for nearby terrain";if(!World.Ready(cell))return false;
            reason="This cell is occupied";if(World.Get(cell)!=0&&!Fluids.IsFluid(World.Get(cell)))return false;
            if(selected.Id==IndustryId.SignalWire&&(!World.Ready(cell.Offset(0,-1,0))||!BlockId.Solid(World.Get(cell.Offset(0,-1,0))))){reason="Signal Wire needs a solid floor";return false;}
            if(selected.Id==BlockId.Torch)
            {bool dry=World.Get(cell)==BlockId.Air;reason=dry?"Place Torch":"Torches need a dry floor or wall face";return dry;}
            // Use the movement collider's exact occupied-cell rule, including its skin.
            // Touching the supporting face is legal; occupying the player's body is not.
            reason=PlayerOverlapReason;if(World.OccupiesCell(Player.transform.position,.6f,Player.Height,cell))return false;
            reason="Cannot place inside a creature";if(Mobs!=null&&Mobs.Occupies(cell))return false;
            reason="Place "+Registry.Get(selected.Id).displayName;return true;
        }
        public bool TryPlaceSelected()
        {
            if(Mode!=ScreenMode.Play||Player.Inspecting)return false;
            if(!PlacementPreview(out var cell,out string reason))
            {PlacementDiagnostic=reason;if(reason!=PlayerOverlapReason)Notify(reason,1);return false;}
            var selected=Inventory.Slots[Selected];
            // One local authority turn: recheck occupancy, commit the voxel, then consume exactly one.
            if(selected.Id==BlockId.Torch)
            {
                if(!World.Raycast(Player.Camera.transform.position,Player.Camera.transform.forward,5,out var support,out _)||!World.PlaceTorch(cell,support))return false;
            }
            else if(!World.Place(cell,selected.Id))return false;
            if(IndustryId.Placed(selected.Id)){var machine=Industry.Simulation.At(cell);machine.Rotation=(Mathf.RoundToInt(Player.transform.eulerAngles.y/90)%4);Industry.Simulation.Invalidate();}
            if(!Creative)Inventory.Take(Selected,1);Sound.Place(selected.Id,World.Local(cell)+Vector3.one*.5f);ArcadePresentation.Active?.Place(World.Local(cell)+Vector3.one*.5f,selected.Id);PlacementDiagnostic="Placed "+Registry.Get(selected.Id).displayName;Notify(PlacementDiagnostic,1);return true;
        }
        public bool TryUseBucket()
        {
            if(Mode!=ScreenMode.Play||Player.Inspecting||Health.Dead)return false;
            byte item=Inventory.Slots[Selected].Id;if(!Fluids.IsBucket(item))return false;
            bool collect=item==Fluids.EmptyBucket;
            if(!World.Raycast(Player.Camera.transform.position,Player.Camera.transform.forward,5,out var hit,out byte id,out var face,collect))
            {Notify(collect?"Aim at a fluid source":"Aim at a block face",1);return false;}
            var fluid=collect?Fluids.Registry.Get(id):Fluids.Registry.FromBucket(item);
            if(fluid==null||collect&&!fluid.IsSource(id))return false;
            var target=collect?hit:hit.Offset(face.x,face.y,face.z);
            if(!collect&&face==Vector3Int.zero)return false;
            bool success=BucketTransfer.TryUse(World,Inventory,Selected,target,Fluids.Registry);
            if(success){Sound.Pickup();Notify(collect?"Bucket filled":fluid.DisplayName+" placed",1);}
            return success;
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
