using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    // Persistent passive population. Ambient hostile despawning never owns these records.
    public sealed partial class PassiveSystem : MonoBehaviour, IInteractionTargetSource
    {
        public const int MaximumPopulation=4096,MaximumActive=128,NaturalLocalPopulation=12;
        public const float SleepDistance=68;
        public readonly List<ChickenState> Animals=new List<ChickenState>();
        public ChickenCatalog Catalog {get;private set;}
        public ChickenState Target {get;private set;}
        public bool NaturalSpawning=true;
        public int TotalEggs {get;private set;}
        public int TotalBorn {get;private set;}
        public int PathSearches {get;private set;}
        public int LastSearches {get;private set;}
        public int ActiveCount=>active.Count;
        public double MaximumTickMs {get;private set;}
        readonly List<ChickenState> active=new List<ChickenState>();
        readonly Dictionary<(long,long),HashSet<ChickenState>> columns=new Dictionary<(long,long),HashSet<ChickenState>>();
        static (long,long) Column(BlockPos p)=>(p.Chunk.X,p.Chunk.Z);
        void Register(ChickenState c){var key=Column(c.Position.Cell);if(!columns.TryGetValue(key,out var entries))columns[key]=entries=new HashSet<ChickenState>();entries.Add(c);}
        void Unregister(ChickenState c){var key=Column(c.Position.Cell);if(columns.TryGetValue(key,out var entries)){entries.Remove(c);if(entries.Count==0)columns.Remove(key);}}
        IEnumerable<ChickenState> Near(Vector3 p,float radius)
        {
            var min=world.Address(p-new Vector3(radius,0,radius)).Chunk;var max=world.Address(p+new Vector3(radius,0,radius)).Chunk;
            for(long z=min.Z;z<=max.Z;z++)for(long x=min.X;x<=max.X;x++)if(columns.TryGetValue((x,z),out var entries))foreach(var c in entries)yield return c;
        }
        void Position(ChickenState c,Vector3 local){var p=WorldPoint.FromLocal(local,world.Origin);if(Column(p.Cell)!=Column(c.Position.Cell)){Unregister(c);c.Position=p;Register(c);}else c.Position=p;}
        readonly MobNavigation navigation=new MobNavigation();
        Expedition game;VoxelWorld world;MobDefinition adultBody,chickBody;ItemSelector[] feed;
        long nextId=1;uint spawnRandom;float accumulator,nextStrike;int spawnTicks=100,refreshTicks,searchBudget;
        public void Initialize(Expedition owner)
        {
            game=owner;world=game.World;Catalog=ChickenCatalog.Load();feed=Catalog.feed.Select(game.Registry.Select).ToArray();
            adultBody=Body(Catalog.width,Catalog.height);chickBody=Body(Catalog.chickWidth,Catalog.chickHeight);
            spawnRandom=unchecked((uint)game.Seed)^0x613BA721u;if(spawnRandom==0)spawnRandom=1;
            NaturalSpawning=!Environment.GetCommandLineArgs().Contains("-rr-verify");world.OriginShifted+=ShiftOrigin;
        }
        MobDefinition Body(float width,float height)
        {var d=ScriptableObject.CreateInstance<MobDefinition>();d.width=width;d.height=height;d.speed=Catalog.speed;return d;}
        MobDefinition Body(ChickenState c)=>c.Adult?adultBody:chickBody;
        uint Random(){uint x=spawnRandom;x^=x<<13;x^=x>>17;x^=x<<5;return spawnRandom=x==0?1:x;}
        bool IsActive(ChickenState c)=>c.Alive&&world.Ready(c.Position.Cell)&&(c.Position.Local(world.Origin)-game.Player.transform.position).sqrMagnitude<SleepDistance*SleepDistance;
        public bool IsFeed(byte id)=>id!=0&&feed.Any(f=>f.Matches(id));
        void ShiftOrigin(Vector3 shift){foreach(var c in Animals)if(c.View!=null)c.View.transform.position=c.Position.Local(world.Origin);refreshTicks=0;}
        void Update()
        {
            if(game==null||!game.Started||game.Paused)return;
            accumulator+=Mathf.Min(Time.deltaTime,.2f);
            while(accumulator>=.05f){accumulator-=.05f;Step();}
            foreach(var c in active)if(c.Alive&&c.View!=null)c.View.Present(c,Time.deltaTime,world.Origin);
            if(game.Mode!=ScreenMode.Play||game.Player.Inspecting)Target=null;
            else {var eye=game.Player.Camera.transform;Target=game.SelectInteraction(eye.position,eye.forward,3.2f,out var hit)&&ReferenceEquals(hit.Source,this)?hit.Entity.Target as ChickenState:null;}
        }
        void RefreshActive()
        {
            var previous=active.ToArray();active.Clear();var player=game.Player.transform.position;
            foreach(var c in Near(player,SleepDistance).Where(IsActive).OrderBy(c=>(c.Position.Local(world.Origin)-player).sqrMagnitude).Take(MaximumActive))active.Add(c);
            foreach(var c in previous)if(!active.Contains(c)&&c.View!=null){Destroy(c.View.gameObject);c.View=null;}
            foreach(var c in active)if(c.View==null)CreateView(c);
        }
        void CreateView(ChickenState c)
        {var obj=new GameObject((c.Adult?"Chicken":"Chick")+" #"+c.Id);obj.transform.SetParent(transform,false);obj.transform.position=c.Position.Local(world.Origin);c.View=obj.AddComponent<ChickenView>();c.View.Initialize(c);}
        public void Step()
        {
            using var cost=RuntimeCosts.Animals.Auto();
            long began=System.Diagnostics.Stopwatch.GetTimestamp();searchBudget=2;LastSearches=0;
            if(--refreshTicks<=0){refreshTicks=20;RefreshActive();}
            if(--spawnTicks<=0){spawnTicks=200;if(NaturalSpawning&&game.ReadyToPlay&&!game.Health.Dead)TryNaturalSpawn();}
            for(int i=active.Count-1;i>=0;i--)
            {
                var c=active[i];
                if(!IsActive(c))continue;
                bool wasAdult=c.Adult;bool fits=wasAdult||c.GrowthTicks>1||CanFit(c.Position.Local(world.Origin),adultBody,c);
                c.Advance(1,fits,Catalog);
                if(c.Adult!=wasAdult){if(c.View!=null)Destroy(c.View.gameObject);c.View=null;CreateView(c);c.Path.Clear();}
                if(c.Adult&&c.EggTicks==0)
                {Drop(c,ChickenId.Egg,1);c.ScheduleEgg(Catalog);c.PeckTicks=30;TotalEggs++;}
                if(world.TouchesFluid(c.Position.Local(world.Origin),Body(c).width,Body(c).height,Fluids.Lava)){Damage(c,Catalog.health,Vector3.zero);continue;}
                Think(c);Move(c);
            }
            // A pair consumes its readiness only after a real, clear chick site exists.
            for(int i=0;i<active.Count;i++)
            {
                var a=active[i];if(!a.ReadyToBreed||!IsActive(a))continue;
                for(int j=i+1;j<active.Count;j++)
                {
                    var b=active[j];if(!b.ReadyToBreed||!IsActive(b))continue;
                    var p=a.Position.Local(world.Origin);var q=b.Position.Local(world.Origin);
                    if((p-q).sqrMagnitude>6.25f||!ClearSight(p+Vector3.up*.5f,q+Vector3.up*.5f)||!TryBirth(a,b))continue;
                    break;
                }
            }
            MaximumTickMs=Math.Max(MaximumTickMs,(System.Diagnostics.Stopwatch.GetTimestamp()-began)*1000.0/System.Diagnostics.Stopwatch.Frequency);
        }
        bool Dry(Vector3 feet,MobDefinition body)
        {
            var min=world.Address(feet+new Vector3(-body.width*.5f,.01f,-body.width*.5f));
            var max=world.Address(feet+new Vector3(body.width*.5f,body.height,body.width*.5f));
            for(long z=min.Z;z<=max.Z;z++)for(long x=min.X;x<=max.X;x++)for(int y=min.Y;y<=max.Y;y++)
            {var p=new BlockPos(x,y,z);if(!world.Ready(p)||Fluids.IsFluid(world.Get(p)))return false;}
            return true;
        }
        public bool BlocksBody(Vector3 feet,float width,float height,ChickenState except=null)
        {
            foreach(var c in Near(feet,2))
            {
                if(c==except||!c.Alive)continue;var p=c.Position.Local(world.Origin);var d=Body(c);float r=(width+d.width)*.5f;
                if(feet.y+height>p.y+.02f&&feet.y<p.y+d.height-.02f&&Mathf.Abs(feet.x-p.x)<r&&Mathf.Abs(feet.z-p.z)<r)return true;
            }
            return false;
        }
        bool CanFit(Vector3 p,MobDefinition body,ChickenState except=null)
        {
            if(!MobNavigation.Standable(world,p,body)||!Dry(p,body)||BlocksBody(p,body.width,body.height,except))return false;
            var player=game.Player.transform.position;float r=(body.width+.6f)*.5f;
            if(p.y+body.height>player.y&&p.y<player.y+game.Player.Height&&Mathf.Abs(p.x-player.x)<r&&Mathf.Abs(p.z-player.z)<r)return false;
            foreach(var m in game.Mobs.Mobs)if(m.Alive)
            {var q=m.Position.Local(world.Origin);float mr=(body.width+m.Definition.width)*.5f;if(p.y+body.height>q.y&&p.y<q.y+m.Definition.height&&Mathf.Abs(p.x-q.x)<mr&&Mathf.Abs(p.z-q.z)<mr)return false;}
            return true;
        }
        public ChickenState Spawn(Vector3 feet,bool chick=false)
        {
            if(Animals.Count>=MaximumPopulation||!CanFit(feet,chick?chickBody:adultBody))return null;
            var c=new ChickenState{Id=nextId++,Position=WorldPoint.FromLocal(feet,world.Origin),Home=WorldPoint.FromLocal(feet,world.Origin),Health=chick?Catalog.chickHealth:Catalog.health,GrowthTicks=chick?Catalog.growthTicks:0,RandomState=Random(),Grounded=true};
            c.ScheduleEgg(Catalog);Animals.Add(c);Register(c);refreshTicks=0;return c;
        }
        public bool CanSpawnNatural(Vector3 feet)
        {
            float distance=Vector3.Distance(feet,game.Player.transform.position);
            if(game.Sky.Clock.IsNight||distance<20||distance>44||Animals.Count>=MaximumPopulation||
               Near(feet,48).Count(c=>(c.Position.Local(world.Origin)-feet).sqrMagnitude<48*48)>=NaturalLocalPopulation||
               !Catalog.spawnRules.AllowsSite(world,game.Registry,feet,Catalog.width,Catalog.height,0,game.Sky.Clock.IsNight)||!CanFit(feet,adultBody))return false;
            var v=game.Player.Camera.WorldToViewportPoint(feet+Vector3.up*.6f);
            return !(v.z>0&&v.x>-.1f&&v.x<1.1f&&v.y>-.1f&&v.y<1.1f&&ClearSight(game.Player.Camera.transform.position,feet+Vector3.up*.6f));
        }
        public bool TryNaturalSpawn()
        {
            for(int attempt=0;attempt<8;attempt++)
            {
                float angle=Random()%6284/1000f,radius=20+Random()%2401/100f;
                var pos=world.Address(game.Player.transform.position+new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius));
                Catalog.spawnRules.SearchHeights(world,pos,44,out int low,out int high);
                for(int y=high;y>=low;y--)
                {
                    var ground=new BlockPos(pos.X,y,pos.Z);if(!world.Ready(ground)||!Catalog.spawnRules.AllowsSupport(game.Registry,world.Get(ground)))continue;
                    var feet=world.Local(ground)+new Vector3(.5f,1.006f,.5f);if(!CanSpawnNatural(feet))continue;
                    if(Spawn(feet)==null)continue;
                    // Try a nearby second adult without bypassing light, terrain, view or population checks.
                    for(int z=-2;z<=2;z++)for(int x=-2;x<=2;x++)
                    {var companion=feet+new Vector3(x,0,z);if(CanSpawnNatural(companion)&&Spawn(companion)!=null)return true;}
                    return true;
                }
            }
            return false;
        }
        bool TryBirth(ChickenState a,ChickenState b)
        {
            if(Animals.Count>=MaximumPopulation)return false;
            var centre=world.Address((a.Position.Local(world.Origin)+b.Position.Local(world.Origin))*.5f+Vector3.up*.01f);
            for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)
            {
                var position=MobNavigation.Feet(world,centre.Offset(x,0,z));var child=Spawn(position,true);if(child==null)continue;
                a.Bred(Catalog);b.Bred(Catalog);TotalBorn++;return true;
            }
            return false;
        }
        public bool TryFeed(ChickenState c)
        {
            if(c==null||!Animals.Contains(c)||!c.Alive||game.Mode!=ScreenMode.Play||game.Health.Dead||game.Player.Inspecting)return false;
            var eye=game.Player.Camera.transform.position;var p=c.Position.Local(world.Origin)+Vector3.up*Body(c).height*.5f;
            if((p-eye).sqrMagnitude>16||!ClearSight(eye,p))return false;
            var stack=game.Inventory.Slots[game.Selected];
            if(!IsFeed(stack.Id)){game.Notify("Hold seeds or grain to lead and feed chickens",3);return false;}
            if(!c.Adult||c.CooldownTicks>0||c.LoveTicks>0){game.Notify(!c.Adult?"This chick is still growing":c.LoveTicks>0?"Ready for a nearby fed partner":"This chicken needs time before breeding again",3);return false;}
            if(game.Inventory.Take(game.Selected,1).Count!=1)return false;
            if(!c.Feed(Catalog))throw new InvalidOperationException("Chicken feeding transaction changed unexpectedly.");
            c.Home=c.Position;game.Sound.Pickup();game.Notify("Chicken fed — feed a nearby adult to breed",3);return true;
        }
        bool ClearSight(Vector3 from,Vector3 to)
        {var d=to-from;return d.sqrMagnitude<.0001f||!world.Raycast(from,d.normalized,d.magnitude,out _,out _);}
        void Think(ChickenState c)
        {
            c.PanicTicks=Math.Max(0,c.PanicTicks-1);c.PeckTicks=Math.Max(0,c.PeckTicks-1);c.ThinkTicks--;
            var p=c.Position.Local(world.Origin);var player=game.Player.transform.position;Vector3 goal=p;
            bool follow=!game.Health.Dead&&game.Mode==ScreenMode.Play&&IsFeed(game.Inventory.Slots[game.Selected].Id)&&(p-player).sqrMagnitude<Catalog.followRange*Catalog.followRange&&ClearSight(p+Vector3.up*.5f,player+Vector3.up*.8f);
            if(c.PanicTicks>0)goal=p+c.Flee*4;
            else if(follow){c.Home=c.Position;goal=player;if((p-player).sqrMagnitude<2.25f){c.Path.Clear();return;}}
            else if(c.ReadyToBreed)
            {
                var partner=active.FirstOrDefault(a=>a!=c&&a.ReadyToBreed&&(a.Position.Local(world.Origin)-p).sqrMagnitude<64);
                if(partner!=null)goal=partner.Position.Local(world.Origin);
            }
            if(c.ThinkTicks>0)return;
            if(goal==p){if(c.PathIndex<c.Path.Count)return;goal=c.Home.Local(world.Origin)+new Vector3((int)(c.NextRandom()%9)-4,0,(int)(c.NextRandom()%9)-4);}
            if(searchBudget<=0)return;
            searchBudget--;LastSearches++;PathSearches++;navigation.Find(world,p,goal,Body(c),c.Path);c.PathIndex=0;c.ThinkTicks=follow||c.PanicTicks>0||c.ReadyToBreed?14:40+(int)(c.NextRandom()%60);
        }
        void Move(ChickenState c)
        {
            var body=Body(c);var local=c.Position.Local(world.Origin);Vector3 horizontal=Vector3.zero;
            if(c.PathIndex<c.Path.Count)
            {
                var next=MobNavigation.Feet(world,c.Path[c.PathIndex]);var direction=next-local;direction.y=0;
                if(direction.magnitude<.12f&&Mathf.Abs(next.y-local.y)<.3f)c.PathIndex++;
                else
                {
                    c.Yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
                    horizontal=direction.normalized*Mathf.Min(Catalog.speed*(c.PanicTicks>0?1.5f:1)*.05f,direction.magnitude);
                    if(next.y-local.y>.3f&&c.Grounded&&!world.Overlaps(local+Vector3.up*1.02f,body.width,body.height))c.Vertical=7.2f;
                    if(!Dry(next,body)||c.Grounded&&next.y<=local.y+.3f&&!world.Overlaps(local+horizontal+direction.normalized*body.width*.55f-Vector3.up*1.12f,.12f,1.15f))
                    {horizontal=Vector3.zero;c.Path.Clear();c.ThinkTicks=0;}
                }
            }
            c.Vertical=Mathf.Max(-25,c.Vertical-1);var result=world.Move(local,horizontal+Vector3.up*c.Vertical*.05f,body.width,body.height,out bool grounded);
            if(BlocksBody(result,body.width,body.height,c)){result.x=local.x;result.z=local.z;}
            var player=game.Player.transform.position;float r=(body.width+.6f)*.5f;
            if(result.y+body.height>player.y&&result.y<player.y+game.Player.Height&&Mathf.Abs(result.x-player.x)<r&&Mathf.Abs(result.z-player.z)<r){result.x=local.x;result.z=local.z;}
            Position(c,result);c.Grounded=grounded;if(grounded)c.Vertical=-1;
        }
        void Drop(ChickenState c,byte id,int count)=>game.Items.Spawn(new ItemStack(id,count),c.Position.Local(world.Origin)+Vector3.up*.35f,Vector3.up*1.5f,.4f,true);
        public bool Damage(ChickenState c,int amount,Vector3 direction)
        {
            if(c==null||amount<=0||!c.Alive||!Animals.Contains(c))return false;
            c.Health=Math.Max(0,c.Health-amount);c.PanicTicks=100;c.Flee=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;c.Path.Clear();c.ThinkTicks=0;c.View?.Hit();
            if(c.Alive)return true;
            if(c.Adult){Drop(c,ChickenId.Raw,1);Drop(c,ChickenId.Feather,1+(int)(c.NextRandom()%2));}
            Unregister(c);Animals.Remove(c);active.Remove(c);if(Target==c)Target=null;
            if(c.View!=null)c.View.Die();refreshTicks=0;return true;
        }
        public ChickenState RayTarget(Vector3 origin,Vector3 direction,out float nearest,float reach=3.2f)
        {
            nearest=reach;ChickenState target=null;var ray=new Ray(origin,direction.normalized);
            foreach(var c in active)
            {
                if(!IsActive(c))continue;var d=Body(c);var bounds=new Bounds(c.Position.Local(world.Origin)+Vector3.up*d.height*.5f,new Vector3(d.width,d.height,d.width));
                if(bounds.IntersectRay(ray,out float distance)&&distance<nearest&&ClearSight(origin,ray.GetPoint(distance+.001f))){nearest=distance;target=c;}
            }
            return target;
        }
        public bool TrySelect(Vector3 origin,Vector3 direction,float reach,out EntitySelectionHit hit)
        {
            var target=RayTarget(origin,direction,out float distance,Mathf.Min(3.2f,reach));
            hit=target==null?default:new EntitySelectionHit(target,distance,origin+direction.normalized*distance);return target!=null;
        }
        public void Interact(object target,bool attack,bool usePressed,bool useHeld)
        {
            Target=target as ChickenState;if(Target==null||!Target.Alive||!Animals.Contains(Target)){Target=null;return;}
            if(usePressed)TryFeed(Target);
            if(attack&&Time.time>=nextStrike&&game.Mode==ScreenMode.Play&&!game.Player.Inspecting&&!game.Health.Dead)
            {
                var held=game.Inventory.Slots[game.Selected];var cap=game.Registry.Capabilities(held);
                nextStrike=Time.time+((cap&ToolCapability.Blade)!=0?.30f:(cap&ToolCapability.Axe)!=0?.55f:.40f);
                if(Damage(Target,held.Empty?1:Math.Max(1,game.Registry.Get(held.Id).attackDamage),game.Player.Camera.transform.forward))game.WearSelectedTool();
            }
        }
        public bool Occupies(BlockPos cell)=>Near(world.Local(cell)+Vector3.one*.5f,2).Any(c=>c.Alive&&world.OccupiesCell(c.Position.Local(world.Origin),Body(c).width,Body(c).height,cell));
        public Vector3 ConstrainPlayer(Vector3 previous,Vector3 desired)
        {
            if(BlocksBody(previous,.6f,game.Player.Height))return desired;
            var result=previous;result.y=desired.y;var axis=result;axis.x=desired.x;if(!BlocksBody(axis,.6f,game.Player.Height))result=axis;
            axis=result;axis.z=desired.z;if(!BlocksBody(axis,.6f,game.Player.Height))result=axis;return result;
        }
        public void Clear(){foreach(var c in Animals)if(c.View!=null)Destroy(c.View.gameObject);Animals.Clear();columns.Clear();active.Clear();Target=null;refreshTicks=0;}
        void OnGUI()
        {
            if(game==null||game.Mode!=ScreenMode.Play||Target==null||!Target.Alive)return;
            string text=Target.Adult?(Target.LoveTicks>0?"Chicken — ready to breed":Target.CooldownTicks>0?"Chicken — resting":"Chicken — feed seeds or grain"):"Chick — growing";
            var style=new GUIStyle(GUI.skin.box){fontSize=16,alignment=TextAnchor.MiddleCenter};GUI.Box(new Rect(Screen.width*.5f-175,Screen.height*.5f+35,350,36),text+" · "+Target.Health+" HP",style);
        }
        void OnDestroy(){if(world!=null)world.OriginShifted-=ShiftOrigin;if(adultBody!=null)Destroy(adultBody);if(chickBody!=null)Destroy(chickBody);}
    }
}
