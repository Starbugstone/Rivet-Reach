using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    // Opt-in standalone integration checks against the same authority and actual imported assets.
    public sealed partial class MobVerification : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public string result, timestamp, unity, cpu, gpu;
            public string[] checks, errors;
            public int assertions,beetleTriangles,prowlerTriangles,pathSearches;
            public double maximumTickMs;
        }
        readonly List<string> checks=new List<string>(),errors=new List<string>();
        Expedition game;MobSystem mobs;string output;int floor;
        Report report=new Report();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-rr-mob-verify"))new GameObject("Mob verification").AddComponent<MobVerification>();
        }
        IEnumerator Start()
        {
            Application.runInBackground=true;
            Application.logMessageReceived+=Log;
            foreach(var device in InputSystem.devices.ToArray())InputSystem.DisableDevice(device);
            InputSystem.AddDevice<Keyboard>("Mob verification keyboard");InputSystem.AddDevice<Mouse>("Mob verification mouse");
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rr-output");
            output=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.persistentDataPath,"MobVerification");Directory.CreateDirectory(output);
            var routines=new Stack<IEnumerator>();routines.Push(Run());
            while(routines.Count>0)
            {
                bool more;object current=null;
                try{more=routines.Peek().MoveNext();if(more)current=routines.Peek().Current;}
                catch(Exception ex){errors.Add(ex.ToString());break;}
                if(!more){routines.Pop();continue;}
                if(current is IEnumerator child){routines.Push(child);continue;}
                yield return current;
            }
            report.result=errors.Count==0?"PASS":"FAIL";report.timestamp=DateTime.UtcNow.ToString("O");report.unity=Application.unityVersion;
            report.cpu=SystemInfo.processorType;report.gpu=SystemInfo.graphicsDeviceName;report.checks=checks.ToArray();report.errors=errors.ToArray();report.assertions=checks.Count;
            if(mobs!=null){report.maximumTickMs=mobs.MaximumTickMs;report.pathSearches=mobs.PathSearches;}
            File.WriteAllText(Path.Combine(output,"mob-runtime-report.json"),JsonUtility.ToJson(report,true));
            Application.Quit(errors.Count==0?0:1);
        }
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);if(output!=null)File.WriteAllLines(Path.Combine(output,"progress.txt"),checks);}
        IEnumerator Until(Func<bool> predicate,float timeout,string message,Func<string> diagnostic=null)
        {float end=Time.realtimeSinceStartup+timeout;while(!predicate()&&Time.realtimeSinceStartup<end)yield return null;
            bool passed=predicate();Check(passed,!passed&&diagnostic!=null?message+": "+diagnostic():message);}
        IEnumerator Capture(string name)
        {yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.4f);}
        Vector3 At(float x,float z)=>new Vector3(x+.5f,floor+1.006f,z+.5f);
        void PlayerAt(float x,float z,float yaw=0,float pitch=10)
        {game.Player.transform.position=At(x,z);game.Player.Yaw=yaw;game.Player.Pitch=pitch;}
        void Block(int x,int y,int z,byte block)
        {
            var p=new BlockPos(x,y,z);byte existing=game.World.Get(p);
            if(existing!=block){if(existing!=0&&!game.World.Remove(p,existing))throw new Exception("Fixture could not clear "+p);if(block!=0&&!game.World.Place(p,block))throw new Exception("Fixture could not place "+p);}
        }
        void Aim(MobState mob)
        {
            var direction=mob.Position.Local(game.World.Origin)+Vector3.up*mob.Definition.height*.55f-game.Player.Camera.transform.position;
            game.Player.Yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            game.Player.Pitch=-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg;
        }
        IEnumerator WallChecks(MobDefinition beetle,MobDefinition prowler)
        {
            mobs.Clear();PlayerAt(0,0);
            Check(beetle.climbsWalls&&!prowler.climbsWalls,"Wall climbing is authored for beetles only");
            // Four-block vertical faces and a broad, reachable top. All edits are real voxels.
            void Wall(byte block)
            {for(int x=-3;x<=3;x++)for(int z=3;z<=7;z++)for(int y=floor+1;y<=floor+4;y++)Block(x,y,z,block);}
            Wall(BlockId.Stone);
            var nav=new MobNavigation();var path=new List<BlockPos>();
            Vector3 goal=At(0,6)+Vector3.up*4;
            nav.Find(game.World,At(0,1),goal,beetle,path);
            Check(path.Any(p=>p.Y>=floor+5)&&path.Zip(path.Skip(1),(a,b)=>a.X==b.X&&a.Z==b.Z&&b.Y>a.Y).Any(b=>b),"Beetle navigation finds vertical wall edges and the top ledge");
            Check(nav.LastExpanded<=96,"Wall navigation retains the bounded search budget");
            nav.Find(game.World,goal-Vector3.up*.0065f,At(0,1),beetle,path);
            Check(path.Any(p=>p.Y<floor+4),"Wall descent routes tolerate resting collider feet below the top face");
            nav.Find(game.World,At(0,1),goal,prowler,path);
            Check(path.All(p=>p.Y<floor+5),"Prowlers cannot use the beetle wall route");
            foreach(var feet in new[]{At(0,2),At(0,8),At(-4,5),At(4,5)})
                Check(MobNavigation.WallNormal(game.World,feet+Vector3.up,beetle)!=Vector3.zero,"Beetle can grip a cardinal wall face");
            Check(MobNavigation.WallNormal(game.World,At(300,0),beetle)==Vector3.zero,"Unloaded terrain never supplies wall grip");
            // A missing horizontal band cannot be crossed by a vertical climbing edge.
            for(int x=-3;x<=3;x++)for(int z=3;z<=7;z++)Block(x,floor+3,z,0);
            Check(MobNavigation.WallNormal(game.World,At(0,2)+Vector3.up*2,beetle)==Vector3.zero,"A mined wall band removes grip immediately");
            nav.Find(game.World,At(0,1),goal,beetle,path);
            Check(path.All(p=>p.Y<floor+4),"Navigation cannot climb through a missing wall band");
            for(int x=-3;x<=3;x++)for(int z=3;z<=7;z++)Block(x,floor+3,z,BlockId.Stone);
            // Low overhang across the outside face: the beetle cannot move through it.
            for(int x=-3;x<=3;x++)Block(x,floor+3,2,BlockId.Stone);
            nav.Find(game.World,At(0,2),At(0,2)+Vector3.up*3,beetle,path);
            Check(!path.Any(p=>p.X==0&&p.Z==2&&p.Y==floor+3),"Wall route rejects a solid overhang in the body volume");
            for(int x=-3;x<=3;x++)Block(x,floor+3,2,0);
            var bug=mobs.Spawn(beetle,At(0,1));Check(bug!=null,"Wall encounter starts with a normally grounded beetle");
            mobs.Damage(bug,1,Vector3.zero);PlayerAt(0,6);game.Player.transform.position+=Vector3.up*4;game.Player.ResetMotion();
            yield return Until(()=>bug.Climbing&&bug.Position.Local(game.World.Origin).y>floor+2,6,"Provoked beetle physically climbs toward the player above the wall");
            Check(!game.World.Overlaps(bug.Position.Local(game.World.Origin),beetle.width,beetle.height),"Climbing respects the voxel body collider");
            game.SetMode(ScreenMode.Pause);var paused=bug.Position;
            yield return new WaitForSecondsRealtime(.25f);
            Check(bug.Position.Cell.Equals(paused.Cell)&&bug.Position.Fraction==paused.Fraction,"Pause freezes a beetle attached to a wall");
            game.SetMode(ScreenMode.Play);
            // Freeze this actual climbed pose while moving only the review camera/player.
            mobs.enabled=false;PlayerAt(-4,0);Aim(bug);
            yield return Capture("beetle-wall-climb");
            var pose=bug.View.transform.Find("Surface pose");
            Check(Vector3.Dot(pose.up,bug.WallNormal)>.95f&&Vector3.Dot(pose.forward,Vector3.up)>.9f,"Imported beetle turns against the wall and faces upward");
            PlayerAt(0,6);game.Player.transform.position+=Vector3.up*4;game.Player.ResetMotion();mobs.enabled=true;
            yield return Until(()=>bug.Position.Local(game.World.Origin).y>floor+4.98f&&MobNavigation.Standable(game.World,bug.Position.Local(game.World.Origin),beetle),7,"Beetle pulls onto the wall top during pursuit");
            yield return new WaitForSeconds(.2f);
            Check(!bug.Climbing,"Beetle returns to ground movement on the top surface");
            mobs.GiveRespawnGrace();PlayerAt(-5,0);
            // Return follows the same wall graph down to its original home.
            yield return Until(()=>bug.Climbing&&bug.ClimbDirection.y<-.5f&&bug.Position.Local(game.World.Origin).y<floor+4.5f,7,"Returning beetle descends a vertical wall",
                ()=>$"{bug.Intent}, feet={bug.Position.Local(game.World.Origin)}, floor={floor}, climbing={bug.Climbing}, path={bug.PathIndex}/{bug.Path.Count}");
            float height=bug.Position.Local(game.World.Origin).y;
            Wall(0);yield return new WaitForSeconds(.45f);
            Check(!bug.Climbing&&bug.Position.Local(game.World.Origin).y<height-.15f,"Removing wall support detaches the beetle into gravity");
            Check(!game.World.Overlaps(bug.Position.Local(game.World.Origin),beetle.width,beetle.height),"Support loss cannot push the beetle inside terrain");
            mobs.Clear();PlayerAt(0,0);game.Player.ResetMotion();
        }
        IEnumerator Run()
        {
            yield return null;game=Expedition.Instance;Check(game!=null,"Real expedition bootstraps");
            game.StartSession(73519);mobs=game.Mobs;mobs.NaturalSpawning=false;game.World.ViewDistance=4;
            yield return Until(()=>game.ReadyToPlay&&game.World.PendingCount==0,90,"Terrain streams before combat checks");
            floor=game.World.Generator.Height(0,0)+7;
            // A loaded, bounded elevated pad isolates collision/AI from random terrain slopes.
            for(int z=-12;z<=16;z++)for(int x=-12;x<=12;x++)
            {
                Block(x,floor,z,BlockId.Stone);
                for(int y=floor+1;y<=floor+5;y++)Block(x,y,z,0);
            }
            PlayerAt(0,0);yield return new WaitForSeconds(.4f);
            if(Array.Exists(Environment.GetCommandLineArgs(),s=>s=="-rr-floater-only")){yield return FloaterChecks();yield break;}
            var beetle=mobs.Definitions.Single(d=>d.territorial);var prowler=mobs.Definitions.Single(d=>d.stableId=="rivet:dusk_prowler");
            Check(!mobs.IsNight,"Prowler clock starts in daylight");
            game.Sky.Clock.SetTime(18.0/24);Check(mobs.IsNight,"Prowler follows authoritative 18:00 night boundary");
            game.Sky.Clock.SetTime(6.0/24);Check(!mobs.IsNight,"Prowler follows authoritative 06:00 dawn boundary");
            game.Sky.Clock.SetTime(12.0/24);
            Check(!mobs.CanSpawn(prowler,At(4,0)),"Natural spawn rejects near-player cells");
            Check(!mobs.CanSpawn(beetle,At(300,0),false),"Unloaded terrain cannot spawn mobs");
            Check(!mobs.CanSpawn(beetle,At(0,0)+Vector3.up*2,false),"Unsupported air cannot spawn mobs");
            for(int i=0;i<32;i++)mobs.TryNaturalSpawn();
            Check(mobs.Mobs.Count>0&&mobs.Mobs.All(m=>m.Definition.territorial),"Natural daylight spawning creates beetles and excludes prowlers");
            game.Sky.Clock.SetTime(20.0/24);
            for(int i=0;i<64;i++)mobs.TryNaturalSpawn();
            Check(mobs.Mobs.Any(m=>m.Definition.nocturnal),"Natural night spawning admits prowlers on loaded terrain");
            Check(mobs.Mobs.Count<=MobSystem.MaximumPopulation&&mobs.Mobs.Count(m=>m.Definition==prowler)<=prowler.population,"Spawner obeys global and species population caps");
            mobs.Clear();game.Sky.Clock.SetTime(12.0/24);
            var bug=mobs.Spawn(beetle,At(-4,6));var cat=mobs.Spawn(prowler,At(4,6));
            Check(bug!=null&&cat!=null,"Both original species spawn with clear supported colliders");
            Check(bug.Id!=cat.Id,"Mob instances have distinct authoritative identities");
            report.beetleTriangles=bug.View.Triangles;report.prowlerTriangles=cat.View.Triangles;
            Check(report.beetleTriangles==904&&report.prowlerTriangles==1476,"Unity imports measured Blender topology intact");
            foreach(var mob in mobs.Mobs)
            {
                Check(mob.View.GetComponentsInChildren<SkinnedMeshRenderer>().Length==1,"Creature uses one skinned renderer");
                var animation=mob.View.GetComponentInChildren<Animator>();
                var actions=Resources.LoadAll<AnimationClip>("Mobs/"+mob.Definition.model);
                Check(animation!=null&&new[]{"Idle","Walk","Attack","Death"}.All(c=>actions.Any(a=>a.name==c)),"Imported creature has all four authored actions");
            }
            Check(mobs.Occupies(game.World.Address(At(-4,6))),"Placement detects creature body occupancy");
            game.Inventory.Add(BlockId.Dirt,2);game.Selected=Array.FindIndex(game.Inventory.Slots.ToArray(),s=>!s.Empty&&s.Id==BlockId.Dirt);
            Check(!game.CanPlace(game.World.Address(At(-4,6)),out _),"Real placement rejects building inside a living mob");
            PlayerAt(0,-2,0,8);bug.Yaw=165;cat.Yaw=195;
            yield return Capture("mobs-import-day");
            foreach(var mob in mobs.Mobs)
            {
                var bones=mob.View.GetComponentsInChildren<Transform>();
                Vector3 facing=bones.Single(b=>b.name=="Head").position-bones.Single(b=>b.name=="Body").position;facing.y=0;
                Check(Vector3.Dot(facing.normalized,mob.View.transform.forward)>.98f,"Animated model faces its authoritative movement direction");
            }
            game.Sky.Clock.SetTime(20.0/24);yield return Capture("mobs-import-night");
            game.Sky.Clock.SetTime(12.0/24);
            game.SetMode(ScreenMode.Pause);
            var frozen=cat.Position;
            yield return new WaitForSecondsRealtime(.4f);
            Check(cat.Position.Cell.Equals(frozen.Cell)&&cat.Position.Fraction==frozen.Fraction,"Pause freezes mob simulation");
            game.SetMode(ScreenMode.Play);
            var nav=new MobNavigation();var path=new List<BlockPos>();
            mobs.Clear();cat=mobs.Spawn(prowler,At(0,7));
            Check(mobs.RayTarget(At(0,0)+Vector3.up,Vector3.forward,10)==cat,"Unobstructed ray reaches test creature before wall placement");
            // Two-block-high wall, open route around either end; path must use the real detour.
            for(int x=-2;x<=2;x++)for(int y=floor+1;y<=floor+2;y++)Block(x,y,3,BlockId.Stone);
            nav.Find(game.World,At(0,0),At(0,7),prowler,path);
            Check(path.Count>0&&path.Any(p=>Math.Abs(p.X)>=3),"Bounded voxel navigation routes around a new wall");
            Check(nav.LastExpanded<=96,"Each path query obeys its expansion budget");
            Check(mobs.RayTarget(At(0,0)+Vector3.up,Vector3.forward,10)==null,"Voxel wall occludes melee targeting");
            for(int x=-2;x<=2;x++)for(int y=floor+1;y<=floor+2;y++)Block(x,y,3,0);
            nav.Find(game.World,At(0,0),At(0,7),prowler,path);
            Check(path.Count>0&&path.All(p=>Math.Abs(p.X)<=1),"Navigation observes removed terrain immediately");
            for(int z=3;z<=5;z++)for(int x=-1;x<=1;x++)Block(x,floor+1,z,BlockId.Stone);
            nav.Find(game.World,At(0,0),At(0,4)+Vector3.up,prowler,path);
            Check(path.Any(p=>p.Y==floor+2),"Local navigation finds a one-block climb with headroom");
            cat.Position=WorldPoint.FromLocal(At(0,0),game.World.Origin);
            cat.Home=WorldPoint.FromLocal(At(0,4)+Vector3.up,game.World.Origin);
            cat.Intent=MobIntent.Return;cat.Path.Clear();cat.PathAt=0;PlayerAt(-5,0);
            yield return Until(()=>cat.Position.Local(game.World.Origin).y>floor+1.95f,6,"Creature physically climbs the voxel step through normal AI movement");
            Check(!game.World.Overlaps(cat.Position.Local(game.World.Origin),prowler.width,prowler.height),"Step traversal preserves full-body voxel collision");
            for(int z=3;z<=5;z++)for(int x=-1;x<=1;x++)Block(x,floor+1,z,0);
            Block(0,floor+2,4,BlockId.Stone);
            Check(!MobNavigation.Standable(game.World,At(0,4),prowler),"Low overhead terrain rejects full-body clearance");
            Block(0,floor+2,4,0);
            for(int z=3;z<=5;z++)for(int x=-1;x<=1;x++)Block(x,floor,z,0);
            Check(!MobNavigation.Standable(game.World,At(0,4),prowler),"A mined shaft is not walkable support");
            for(int z=3;z<=5;z++)for(int x=-1;x<=1;x++)Block(x,floor,z,BlockId.Stone);
            mobs.Clear();PlayerAt(0,0);yield return null;
            cat=mobs.Spawn(prowler,At(0,10));
            // Allow initial spawn sanctuary grace to expire; stationary player is actual target.
            yield return Until(()=>cat.Intent==MobIntent.Chase||cat.Intent==MobIntent.Windup,13,"Prowler detects player and enters pursuit");
            Vector3 begin=cat.Position.Local(game.World.Origin);
            yield return new WaitForSeconds(.6f);
            Check(Vector3.Distance(cat.Position.Local(game.World.Origin),game.Player.transform.position)<Vector3.Distance(begin,game.Player.transform.position),"Pursuit physically approaches using voxel collision");
            yield return Until(()=>cat.Intent==MobIntent.Windup,8,"Prowler telegraphs before melee damage");
            float before=game.Health.Hearts;
            yield return new WaitForSeconds(.2f);Check(game.Health.Hearts==before,"Wind-up provides time to react");
            PlayerAt(-7,0);yield return new WaitForSeconds(.65f);
            Check(game.Health.Hearts==before,"Moving out of reach avoids a committed bite");
            mobs.Clear();PlayerAt(0,0);cat=mobs.Spawn(prowler,At(0,2));
            yield return Until(()=>mobs.AttackHits>0,5,"Enemy bite reaches shared player damage authority");
            Check(game.Health.Hearts<before&&!game.Health.Dead,"Enemy damage reduces health without bypassing survival");
            yield return WallChecks(beetle,prowler);
            yield return FloaterChecks();
            mobs.GiveRespawnGrace();mobs.Clear();PlayerAt(0,0);bug=mobs.Spawn(beetle,At(0,2));
            yield return null;Aim(bug);yield return null;
            Check(mobs.RayTarget(game.Player.Camera.transform.position,game.Player.Camera.transform.forward)==bug,"First-person ray selects the aimed creature");
            game.Selected=9;game.Inventory.Add(BlockId.StarterDagger,1,9,10);
            if(game.Inventory.Slots[9].Id!=BlockId.StarterDagger){game.Inventory.Take(9,int.MaxValue);game.Inventory.Add(BlockId.StarterDagger,1,9,10);}
            int health=bug.Health;game.Player.VerificationMining=true;yield return new WaitForSeconds(.12f);game.Player.VerificationMining=false;
            Check(bug.Health==health-game.Registry.Get(BlockId.StarterDagger).attackDamage,"Actual held blade input uses authored item damage for one melee strike");
            Check(!game.Player.HasTarget&&game.Player.MiningProgress==0,"Hitting a mob suppresses terrain mining behind it");
            game.Hunger.Exert(8);game.Inventory.Add(BlockId.BakedPotato,2,8,9);game.Selected=8;
            Aim(bug);yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));
            yield return new WaitForSeconds(.3f);
            Check(game.Player.EatingProgress>0,"Food use can begin while a creature is in view");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;yield return null;
            Check(game.Player.EatingProgress==0&&game.Inventory.Slots[8].Count==2,"Aiming at a mob still cancels an unfinished bite on Use release");
            game.Selected=9;
            Aim(bug);yield return null;yield return Capture("mob-combat-target");
            int defeated=mobs.TotalDefeated;Check(mobs.Damage(bug,100,Vector3.forward),"Lethal damage commits once");
            Check(!mobs.Damage(bug,100,Vector3.forward)&&mobs.TotalDefeated==defeated+1,"Dead entities reject repeated hits and duplicate defeat");
            Check(!mobs.Occupies(bug.Position.Cell),"Dead mob releases build occupancy");
            yield return new WaitForSeconds(1.6f);Check(!mobs.Mobs.Contains(bug),"Death animation completes then presentation is released");
            mobs.Clear();mobs.GiveRespawnGrace();PlayerAt(0,0);
            // Start this proximity encounter after grace; an earlier spawn can wander
            // outside warning range while aggression is intentionally disabled.
            yield return new WaitForSeconds(8.1f);bug=mobs.Spawn(beetle,At(0,4));
            yield return Until(()=>bug.Intent==MobIntent.Warning,3,"Territorial beetle warns before aggression");
            PlayerAt(0,-4);yield return new WaitForSeconds(.3f);
            Check(bug.Intent!=MobIntent.Chase,"Backing out of warning range avoids beetle combat");
            // Origin shifts preserve WorldPoint, HP and identity, with views following their data.
            var playerBefore=game.Player.transform.position;
            var constrained=mobs.ConstrainPlayer(playerBefore,bug.Position.Local(game.World.Origin));
            Check((constrained-bug.Position.Local(game.World.Origin)).sqrMagnitude>.2f,"Player body cannot walk through a living creature");
            bug.Position=WorldPoint.FromLocal(bug.Position.Local(game.World.Origin)+Vector3.right*640,game.World.Origin);
            bug.Home=bug.Position;
            var saved=bug.Position;long identity=bug.Id;int hp=bug.Health;
            game.Player.transform.position+=Vector3.right*640;
            yield return null;yield return null;
            Check(mobs.Mobs.Contains(bug)&&saved.Cell.Equals(bug.Position.Cell)&&bug.Id==identity&&bug.Health==hp,"Floating-origin travel cannot rewrite creature identity or HP");
            Check(Vector3.Distance(bug.View.transform.position,bug.Position.Local(game.World.Origin))<.01f,"Origin shift repositions the actual imported creature view");
            Check(!bug.View.gameObject.activeSelf,"Creature presentation sleeps at an unloaded frontier");
            game.Player.transform.position+=Vector3.right*200;
            yield return new WaitForSeconds(.2f);
            Check(mobs.Mobs.Count==0,"Distant ambient entities release bounded runtime population");
        }
    }
}
