using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    // One session authority with bounded simulation and searches; views contain no combat state.
    public sealed class MobSystem : MonoBehaviour
    {
        public const int MaximumPopulation=14;
        public const float SpawnMinimum=24, SpawnMaximum=48, SleepDistance=68, DespawnDistance=112;
        public Func<bool> NightProvider;
        public bool NaturalSpawning=true;
        public readonly List<MobState> Mobs=new List<MobState>();
        public MobDefinition[] Definitions {get;private set;}
        public MobState Target {get;private set;}
        public int TotalSpawned {get;private set;}
        public int TotalDefeated {get;private set;}
        public int AttackHits {get;private set;}
        public int PathSearches {get;private set;}
        public double MaximumTickMs {get;private set;}
        public bool IsNight=>NightProvider?.Invoke()==true;
        Expedition game;
        VoxelWorld world;
        Material material;
        System.Random random;
        readonly MobNavigation navigation=new MobNavigation();
        long nextId=1;
        float accumulator,spawnAt=4,elapsed,nextStrike,nextPlayerHit,graceUntil=10;
        Vector3 previousPlayer;
        int searchBudget;
        public void Initialize(Expedition expedition)
        {
            game=expedition;world=game.World;random=new System.Random(game.Seed^0x372198);
            Definitions=Resources.LoadAll<MobDefinition>("Mobs/Definitions");
            Array.Sort(Definitions,(a,b)=>string.CompareOrdinal(a.stableId,b.stableId));
            if(Definitions.Length==0)throw new InvalidOperationException("No authored mob definitions found.");
            var identities=new HashSet<string>();
            foreach(var definition in Definitions)
            {definition.Validate();if(!identities.Add(definition.stableId))throw new InvalidOperationException("Duplicate mob identity: "+definition.stableId);}
            material=Resources.Load<Material>("Mobs/CreatureMaterial");
            if(material==null)throw new InvalidOperationException("Missing imported mob material.");
            previousPlayer=game.Player.transform.position;
            NightProvider=()=>game.Sky!=null&&game.Sky.Clock.IsNight;
            NaturalSpawning=!Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-rr-verify"||s=="-rr-mob-verify");
            game.World.OriginShifted+=ShiftOrigin;
            game.Respawned+=GiveRespawnGrace;
            gameObject.AddComponent<MobHUD>().Initialize(this,game);
        }
        void ShiftOrigin(Vector3 shift)
        {
            previousPlayer-=shift;
            foreach(var mob in Mobs)if(mob.View!=null)mob.View.transform.position=mob.Position.Local(game.World.Origin);
        }
        void Update()
        {
            if(game==null||!game.Started||game.Paused)return;
            Vector3 player=game.Player.transform.position;
            if((player-previousPlayer).sqrMagnitude>144)GiveRespawnGrace();
            previousPlayer=player;
            accumulator+=Mathf.Min(Time.deltaTime,.2f);
            while(accumulator>=.05f)
            {
                accumulator-=.05f;
                var watch=System.Diagnostics.Stopwatch.StartNew();Tick(.05f);
                MaximumTickMs=Math.Max(MaximumTickMs,watch.Elapsed.TotalMilliseconds);
            }
            foreach(var mob in Mobs)
            {
                if(mob.View==null)continue;
                bool visible=(mob.Position.Local(game.World.Origin)-player).sqrMagnitude<SleepDistance*SleepDistance&&game.World.Ready(mob.Position.Cell);
                mob.View.gameObject.SetActive(visible);
                if(visible)mob.View.Present(mob.Position.Local(game.World.Origin),Time.deltaTime);
            }
            if(game.Mode!=ScreenMode.Play||game.Player.Inspecting)Target=null;
        }
        public void GiveRespawnGrace()
        {
            graceUntil=elapsed+8;nextPlayerHit=elapsed+1;
            foreach(var mob in Mobs)if(mob.Alive){mob.Intent=MobIntent.Return;mob.Anger=0;mob.Timer=0;mob.Path.Clear();}
        }
        void Tick(float dt)
        {
            elapsed+=dt;searchBudget=2;
            if(NaturalSpawning&&elapsed>=spawnAt&&game.ReadyToPlay&&!game.Health.Dead)
            {spawnAt=elapsed+2;TryNaturalSpawn();}
            for(int i=Mobs.Count-1;i>=0;i--)
            {
                var mob=Mobs[i];Vector3 local=mob.Position.Local(game.World.Origin);
                if(!mob.Alive){mob.Timer-=dt;if(mob.Timer<=0)RemoveAt(i);continue;}
                float distance=Vector3.Distance(local,game.Player.transform.position);
                if(distance>DespawnDistance){RemoveAt(i);continue;}
                if(distance>SleepDistance||!game.World.Ready(mob.Position.Cell))continue;
                Think(mob,local,distance,dt);
                Move(mob,dt);
            }
        }
        public bool CanSpawn(MobDefinition definition,Vector3 local,bool distanceRule=true)
        {
            if(Mobs.Count>=MaximumPopulation||!MobNavigation.Standable(game.World,local,definition))return false;
            int count=0;
            foreach(var mob in Mobs)
            {
                if(mob.Definition==definition)count++;
                if((mob.Position.Local(game.World.Origin)-local).sqrMagnitude<9)return false;
            }
            if(count>=definition.population)return false;
            if(distanceRule)
            {
                float distance=Vector3.Distance(local,game.Player.transform.position);
                if(distance<SpawnMinimum||distance>SpawnMaximum)return false;
                var cell=game.World.Address(local);
                if(cell.X* (double)cell.X+cell.Z*(double)cell.Z<16*16)return false;
                if(definition.nocturnal&&!IsNight)return false;
                if(game.World.SkyLight(game.World.Address(local+Vector3.up*definition.height))==0)return false;
                // Never materialize in the player's current view, even outside melee range.
                var view=game.Player.Camera.WorldToViewportPoint(local+Vector3.up*definition.height*.5f);
                if(view.z>0&&view.x>-.1f&&view.x<1.1f&&view.y>-.1f&&view.y<1.1f&&ClearSight(game.Player.Camera.transform.position,local+Vector3.up*.5f))return false;
            }
            return true;
        }
        public bool TryNaturalSpawn()
        {
            if(Mobs.Count>=MaximumPopulation)return false;
            for(int attempt=0;attempt<8;attempt++)
            {
                var definition=Definitions[random.Next(Definitions.Length)];
                if(definition.nocturnal&&!IsNight)continue;
                float angle=(float)random.NextDouble()*Mathf.PI*2,radius=Mathf.Lerp(SpawnMinimum,SpawnMaximum,(float)random.NextDouble());
                var candidate=game.Player.transform.position+new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius);
                var cell=game.World.Address(candidate);
                int surface=game.World.Generator.Height(cell.X,cell.Z);
                // Search actual loaded support near the generated surface; no spawning on leaves,
                // cave holes, thin ledges, occupied blocks or unloaded borders.
                for(int y=surface+5;y>=surface-7;y--)
                {
                    var ground=new BlockPos(cell.X,y,cell.Z);
                    if(!game.World.Ready(ground))continue;
                    byte block=game.World.Get(ground);
                    if(block!=BlockId.Grass&&block!=BlockId.Dirt&&block!=BlockId.Stone&&
                       block!=BlockId.Sand&&block!=BlockId.Sandstone&&block!=BlockId.Snow&&block!=BlockId.RedClay)continue;
                    Vector3 feet=game.World.Local(ground)+new Vector3(.5f,1.006f,.5f);
                    if(!CanSpawn(definition,feet))continue;
                    Spawn(definition,feet);return true;
                }
            }
            return false;
        }
        // Used by authored encounter/verification callers; obeys support, clearance and population.
        public MobState Spawn(MobDefinition definition,Vector3 local)
        {
            if(!CanSpawn(definition,local,false))return null;
            var state=new MobState{Id=nextId++,Definition=definition,Position=WorldPoint.FromLocal(local,game.World.Origin),
                Home=WorldPoint.FromLocal(local,game.World.Origin),Health=definition.health,Intent=MobIntent.Idle,Timer=2+(float)random.NextDouble()*3,Grounded=true};
            var root=new GameObject(definition.displayName+" #"+state.Id);root.transform.SetParent(transform,false);root.transform.position=local;
            state.View=root.AddComponent<MobView>();state.View.Initialize(state,material);Mobs.Add(state);TotalSpawned++;return state;
        }
        bool ClearSight(Vector3 from,Vector3 to)
        {
            var ray=to-from;return ray.sqrMagnitude<.001f||!game.World.Raycast(from,ray.normalized,ray.magnitude,out _,out _);
        }
        void Think(MobState mob,Vector3 local,float distance,float dt)
        {
            var d=mob.Definition;mob.Timer-=dt;mob.Anger=Mathf.Max(0,mob.Anger-dt);
            Vector3 player=game.Player.transform.position,home=mob.Home.Local(game.World.Origin);
            bool allowed=elapsed>=graceUntil&&!game.Health.Dead&&game.ReadyToPlay;
            bool sees=allowed&&distance<d.noticeRange&&ClearSight(local+Vector3.up*d.height*.65f,player+Vector3.up*.85f);
            mob.Unseen=sees?0:mob.Unseen+dt;
            if(mob.Intent==MobIntent.Windup)
            {
                if(mob.Timer>0)return;
                if(allowed&&distance<=d.attackRange&&Mathf.Abs(local.y-player.y)<1.4f&&elapsed>=nextPlayerHit&&
                   ClearSight(local+Vector3.up*d.height*.65f,player+Vector3.up*.85f))
                {if(game.TakeDamage(d.damage)>0){nextPlayerHit=elapsed+.65f;AttackHits++;}}
                mob.Intent=MobIntent.Recovery;mob.Timer=d.recovery;return;
            }
            if(mob.Intent==MobIntent.Recovery&&mob.Timer>0)return;
            if(mob.Intent==MobIntent.Chase||mob.Intent==MobIntent.Recovery)
            {
                if(!allowed||Vector3.Distance(local,home)>d.leashRange||mob.Unseen>4)
                {mob.Intent=MobIntent.Return;mob.Anger=0;mob.Path.Clear();}
                else if(sees&&distance<=d.attackRange)
                {mob.Intent=MobIntent.Windup;mob.Timer=d.windup;mob.Path.Clear();Face(mob,player-local);return;}
                else mob.Intent=MobIntent.Chase;
            }
            else if(mob.Intent==MobIntent.Warning)
            {
                Face(mob,player-local);
                if(!sees||distance>=5){mob.Intent=MobIntent.Idle;mob.Timer=2;}
                else if(mob.Timer<=0||mob.Anger>0){mob.Intent=MobIntent.Chase;mob.Path.Clear();}
                return;
            }
            else if(sees&&mob.Intent!=MobIntent.Return&&(mob.Anger>0||!d.territorial))
            {mob.Intent=MobIntent.Chase;mob.Path.Clear();}
            else if(sees&&d.territorial&&distance<5&&mob.Intent!=MobIntent.Return)
            {mob.Intent=MobIntent.Warning;mob.Timer=1.4f;mob.Path.Clear();game.Notify(mob.Definition.displayName+" warns you — back away",2);return;}
            if(mob.Intent==MobIntent.Return&&Vector3.Distance(local,home)<1.5f)
            {mob.Intent=MobIntent.Idle;mob.Timer=3;mob.Path.Clear();}
            if(mob.Intent==MobIntent.Idle&&mob.Timer<=0)
            {
                var wander=home+new Vector3(random.Next(-5,6),0,random.Next(-5,6));
                RequestPath(mob,local,wander);mob.Intent=MobIntent.Wander;mob.Timer=5;
            }
            if(mob.Intent==MobIntent.Wander&&(mob.Timer<=0||mob.PathIndex>=mob.Path.Count))
            {mob.Intent=MobIntent.Idle;mob.Timer=2+(float)random.NextDouble()*4;mob.Path.Clear();}
            if((mob.Intent==MobIntent.Chase||mob.Intent==MobIntent.Return)&&elapsed>=mob.PathAt)
                RequestPath(mob,local,mob.Intent==MobIntent.Chase?player:home);
        }
        void RequestPath(MobState mob,Vector3 start,Vector3 goal)
        {
            if(searchBudget<=0)return;
            searchBudget--;navigation.Find(game.World,start,goal,mob.Definition,mob.Path);mob.PathIndex=0;mob.PathAt=elapsed+.65f;PathSearches++;
        }
        static void Face(MobState mob,Vector3 direction)
        {if(direction.x*direction.x+direction.z*direction.z>.001f)mob.Yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;}
        bool WallMove(MobState mob,Vector3 local,float dt,out Vector3 delta)
        {
            delta=Vector3.zero;var d=mob.Definition;
            if(!d.climbsWalls)return false;
            bool hasPath=(mob.Intent==MobIntent.Chase||mob.Intent==MobIntent.Wander||mob.Intent==MobIntent.Return)&&mob.PathIndex<mob.Path.Count;
            Vector3 next=hasPath?MobNavigation.Feet(world,mob.Path[mob.PathIndex]):local;
            bool onFloor=MobNavigation.Standable(world,local,d);
            bool top=hasPath&&MobNavigation.Standable(world,next,d);
            Vector3 toward=next-local;
            // Retain the last wall's lower grip points for the short crest onto its checked
            // top. This cannot bridge a missing wall or attach to the underside of a ceiling.
            bool lip=mob.Climbing&&top&&toward.y>=-.02f&&toward.y<.55f&&
                new Vector2(toward.x,toward.z).magnitude<1.15f&&Vector3.Dot(toward,-mob.WallNormal)>.05f;
            Vector3 normal=MobNavigation.WallNormal(world,local,d,mob.WallNormal,lip);
            if(normal==Vector3.zero||onFloor&&(!hasPath||Mathf.Abs(toward.y)<.15f))return false;
            if(hasPath&&(world.Overlaps(next,d.width,d.height)||!top&&MobNavigation.WallNormal(world,next,d)==Vector3.zero))
            {mob.Path.Clear();mob.PathAt=elapsed;hasPath=false;}
            mob.Climbing=true;mob.WallNormal=normal;mob.Vertical=0;
            if(hasPath)
            {
                if(toward.magnitude<.12f)mob.PathIndex++;
                else
                {
                    delta=toward.normalized*Mathf.Min(d.climbSpeed*dt,toward.magnitude);
                    if(top&&toward.y>.01f&&Vector3.Dot(toward,-normal)>.05f)
                        delta=Vector3.up*Mathf.Min(d.climbSpeed*dt,toward.y);
                    Vector3 along=Vector3.ProjectOnPlane(toward,normal);
                    if(along.sqrMagnitude>.01f)mob.ClimbDirection=along.normalized;
                }
            }
            return true;
        }
        void Move(MobState mob,float dt)
        {
            var d=mob.Definition;Vector3 local=mob.Position.Local(game.World.Origin),horizontal=Vector3.zero;
            bool climbing=WallMove(mob,local,dt,out Vector3 movement);
            if(!climbing)
            {
                mob.Climbing=false;mob.WallNormal=Vector3.zero;
                if((mob.Intent==MobIntent.Chase||mob.Intent==MobIntent.Wander||mob.Intent==MobIntent.Return)&&mob.PathIndex<mob.Path.Count)
                {
                    var next=MobNavigation.Feet(game.World,mob.Path[mob.PathIndex]);Vector3 direction=next-local;direction.y=0;
                    if(direction.magnitude<.14f&&Mathf.Abs(next.y-local.y)<.3f)mob.PathIndex++;
                    else
                    {
                        Face(mob,direction);float speed=d.speed*(mob.Intent==MobIntent.Chase?1:.45f);
                        horizontal=direction.normalized*Mathf.Min(speed*dt,direction.magnitude);
                        if(next.y-local.y>.3f&&mob.Grounded&&
                           !game.World.Overlaps(local+Vector3.up*1.02f,d.width,d.height))mob.Vertical=7.2f;
                        // Refuse walking into a newly mined pit. Replan against edits.
                        Vector3 edge=local+horizontal+direction.normalized*d.width*.55f;
                        bool wallDescent=d.climbsWalls&&next.y>=local.y-1.05f&&MobNavigation.WallNormal(world,next,d)!=Vector3.zero;
                        if(mob.Grounded&&next.y<=local.y+.3f&&!wallDescent&&!game.World.Overlaps(edge-Vector3.up*1.12f,.12f,1.15f))
                        {horizontal=Vector3.zero;mob.Path.Clear();mob.PathAt=elapsed;}
                    }
                }
                mob.Vertical=Mathf.Max(-25,mob.Vertical-20*dt);
                movement=horizontal+Vector3.up*mob.Vertical*dt;
            }
            Vector3 result=game.World.Move(local,movement+mob.Knockback*dt,d.width,d.height,out bool grounded);
            mob.Grounded=grounded;if(grounded)mob.Vertical=-1;
            mob.Knockback=Vector3.MoveTowards(mob.Knockback,Vector3.zero,10*dt);
            // Keep creatures from occupying the player or each other while retaining vertical support.
            Vector3 pd=result-game.Player.transform.position;
            if(Mathf.Abs(pd.y)<game.Player.Height&&new Vector2(pd.x,pd.z).magnitude<(d.width+.6f)*.5f)
            {result.x=local.x;result.z=local.z;}
            foreach(var other in Mobs)
            {
                if(other==mob||!other.Alive)continue;
                var separation=result-other.Position.Local(game.World.Origin);
                if(Mathf.Abs(separation.y)<d.height&&new Vector2(separation.x,separation.z).magnitude<(d.width+other.Definition.width)*.45f)
                {result.x=local.x;result.z=local.z;break;}
            }
            mob.Position=WorldPoint.FromLocal(result,game.World.Origin);
        }
        public MobState RayTarget(Vector3 origin,Vector3 direction,float reach=3.2f)
        {
            MobState target=null;float nearest=reach;var ray=new Ray(origin,direction.normalized);
            foreach(var mob in Mobs)
            {
                if(!mob.Alive)continue;var d=mob.Definition;var local=mob.Position.Local(game.World.Origin);
                var bounds=new Bounds(local+Vector3.up*d.height*.5f,new Vector3(d.width,d.height,d.width));
                if(bounds.IntersectRay(ray,out float distance)&&distance<nearest&&ClearSight(origin,ray.GetPoint(distance+.001f)))
                {nearest=distance;target=mob;}
            }
            return target;
        }
        public bool HandlePlayerTarget(bool attack)
        {
            Target=RayTarget(game.Player.Camera.transform.position,game.Player.Camera.transform.forward);
            if(Target==null)return false;
            if(attack&&elapsed>=nextStrike&&game.Mode==ScreenMode.Play&&!game.Player.Inspecting&&!game.Health.Dead)
            {
                var held=game.Inventory.Slots[game.Selected];
                var capabilities=game.Registry.Capabilities(held);
                bool blade=(capabilities&ToolCapability.Blade)!=0,axe=(capabilities&ToolCapability.Axe)!=0;
                int damage=held.Empty?1:Math.Max(1,game.Registry.Get(held.Id).attackDamage);
                nextStrike=elapsed+(blade?.30f:axe?.55f:.40f);
                Damage(Target,damage,game.Player.Camera.transform.forward);
            }
            return true;
        }
        public bool Damage(MobState mob,int amount,Vector3 direction)
        {
            if(amount<=0||mob==null||!mob.Alive||!Mobs.Contains(mob))return false;
            mob.Health=Math.Max(0,mob.Health-amount);mob.Anger=12;mob.View.Hit();
            direction.y=0;mob.Knockback=direction.normalized*2.7f;
            mob.Path.Clear();mob.PathAt=elapsed;mob.Intent=mob.Alive?MobIntent.Chase:MobIntent.Dead;
            if(!mob.Alive){mob.Timer=1.4f;TotalDefeated++;game.Notify(mob.Definition.displayName+" defeated",2);}
            return true;
        }
        public bool Occupies(BlockPos cell)
        {
            foreach(var mob in Mobs)if(mob.Alive&&game.World.OccupiesCell(mob.Position.Local(game.World.Origin),mob.Definition.width,mob.Definition.height,cell))return true;
            return false;
        }
        public Vector3 ConstrainPlayer(Vector3 previous,Vector3 desired)
        {
            bool Blocked(Vector3 position)
            {
                foreach(var mob in Mobs)
                {
                    if(!mob.Alive)continue;
                    Vector3 p=mob.Position.Local(game.World.Origin);
                    float radius=(mob.Definition.width+.6f)*.5f;
                    if(position.y+game.Player.Height>p.y+.03f&&position.y<p.y+mob.Definition.height-.03f&&
                       Mathf.Abs(position.x-p.x)<radius&&Mathf.Abs(position.z-p.z)<radius)return true;
                }
                return false;
            }
            // Resolve lateral body contact independently of vertical voxel collision. Escape from
            // an already overlapping body is always allowed (e.g. external respawn/teleport).
            if(Blocked(previous))return desired;
            Vector3 result=previous;result.y=desired.y;
            var axis=result;axis.x=desired.x;if(!Blocked(axis))result=axis;
            axis=result;axis.z=desired.z;if(!Blocked(axis))result=axis;
            return result;
        }
        void RemoveAt(int index){var mob=Mobs[index];if(Target==mob)Target=null;if(mob.View!=null)Destroy(mob.View.gameObject);Mobs.RemoveAt(index);}
        public void Clear(){for(int i=Mobs.Count-1;i>=0;i--)RemoveAt(i);}
        void OnDestroy(){if(world!=null)world.OriginShifted-=ShiftOrigin;if(game!=null)game.Respawned-=GiveRespawnGrace;}
    }
}
