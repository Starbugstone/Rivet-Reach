using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class MobVerification
    {
        IEnumerator FloaterChecks()
        {
            mobs.Clear();PlayerAt(0,0);game.Player.ResetMotion();game.Sky.Clock.SetTime(12.0/24);game.Selected=14;
            var d=mobs.Definitions.Single(m=>m.stableId=="rivet:floater");
            Vector3 Hover(float x,float z)=>At(x,z)+Vector3.up*d.hoverHeight;
            Check(d.nocturnal&&!d.territorial&&d.hoverHeight>.4f&&!d.climbsWalls,"Floater is an authored hostile low-hovering night mob");
            Check(game.Registry.ResolveId(d.deathDropId)==BlockId.FloaterRock&&game.Registry.Get(BlockId.FloaterRock).stackLimit==64,"Floater drop resolves through the shared item registry");
            Check(!mobs.CanSpawn(d,At(5,5),false)&&mobs.CanSpawn(d,Hover(5,5),false),"Floater spawn validates its actual hovering collider and ground support");
            Check(!mobs.CanSpawn(d,Hover(300,0),false),"Floater cannot spawn in unloaded air");
            Block(5,floor+2,5,BlockId.Stone);
            Check(!mobs.CanSpawn(d,Hover(5,5),false),"Floater spawn rejects a ceiling intersecting its hovering body");
            Block(5,floor+2,5,0);
            var floater=mobs.Spawn(d,Hover(0,7));Check(floater!=null,"Floater spawns above real loaded ground");
            Check(floater.View.Triangles>1000&&floater.View.Triangles<10000,"Imported Floater fits the creature geometry contract");
            Check(floater.View.GetComponentsInChildren<SkinnedMeshRenderer>().Length==1,"Floater uses one shared-material skinned renderer");
            Check(new[]{"Idle","Walk","Attack","Death"}.All(n=>Resources.LoadAll<AnimationClip>("Mobs/Floater").Any(c=>c.name==n)),"Floater imports hover, pursuit, punch and defeat actions");
            floater.Yaw=180;floater.Timer=30;
            yield return new WaitForSeconds(.25f);
            Check(Mathf.Abs(floater.Position.Local(game.World.Origin).y-Hover(0,7).y)<.03f,"Idle Floater maintains authoritative clearance above the ground");
            mobs.enabled=false;PlayerAt(0,3.5f);Aim(floater);game.Player.Yaw+=12;yield return Capture("floater-hover");mobs.enabled=true;PlayerAt(0,0);
            var nav=new MobNavigation();var path=new List<BlockPos>();
            for(int x=-2;x<=2;x++)for(int y=floor+1;y<=floor+3;y++)Block(x,y,3,BlockId.Stone);
            nav.Find(game.World,Hover(0,0),Hover(0,7),d,path);
            Check(path.Count>0&&path.Any(p=>Math.Abs(p.X)>=3)&&nav.LastExpanded<=96,"Floater uses a bounded detour around tall voxel walls");
            Check(mobs.RayTarget(At(0,0)+Vector3.up,Vector3.forward,10)==null,"Floater melee targeting cannot pass through terrain");
            for(int x=-2;x<=2;x++)for(int y=floor+1;y<=floor+3;y++)Block(x,y,3,0);
            mobs.Clear();
            for(int z=3;z<=5;z++)for(int x=-1;x<=1;x++)Block(x,floor+1,z,BlockId.Stone);
            floater=mobs.Spawn(d,Hover(0,1));floater.Home=WorldPoint.FromLocal(Hover(0,4)+Vector3.up,game.World.Origin);floater.Intent=MobIntent.Return;floater.PathAt=0;PlayerAt(-5,0);
            yield return Until(()=>floater.Position.Local(game.World.Origin).z>3&&floater.Position.Local(game.World.Origin).y>floor+2.5f,7,"Floater rises and crosses a one-block terrain step",()=>floater.Position.Local(game.World.Origin)+" path "+floater.PathIndex+"/"+floater.Path.Count);
            Check(!game.World.Overlaps(floater.Position.Local(game.World.Origin),d.width,d.height),"Hover traversal respects full-body voxel collision");
            for(int z=3;z<=5;z++)for(int x=-1;x<=1;x++)Block(x,floor+1,z,0);
            float high=floater.Position.Local(game.World.Origin).y;floater.Intent=MobIntent.Idle;floater.Timer=30;floater.Path.Clear();
            yield return new WaitForSeconds(.4f);
            Check(floater.Position.Local(game.World.Origin).y<high-.3f,"Removing raised support makes the Floater descend toward the lower ground");
            mobs.Clear();PlayerAt(0,0);floater=mobs.Spawn(d,Hover(0,8));
            yield return Until(()=>floater.Intent==MobIntent.Chase,13,"Hostile Floater detects and pursues a visible player");
            var begin=floater.Position.Local(game.World.Origin);
            yield return Until(()=>floater.Position.Local(game.World.Origin).z<begin.z-.3f,2,"Floater pursuit moves its real hovering body toward the player",()=>floater.Intent+" "+floater.Position.Local(game.World.Origin)+" path "+floater.PathIndex+"/"+floater.Path.Count);
            yield return Until(()=>floater.Intent==MobIntent.Windup,8,"Floater visibly prepares a melee punch");
            float hp=game.Health.Hearts;PlayerAt(-6,0);yield return new WaitForSeconds(.9f);
            Check(game.Health.Hearts==hp,"Moving away during Floater wind-up avoids damage");
            mobs.Clear();PlayerAt(0,0);floater=mobs.Spawn(d,Hover(0,2));int hits=mobs.AttackHits;
            yield return Until(()=>mobs.AttackHits>hits,5,"Floater punch reaches the shared player damage authority");
            mobs.Clear();mobs.GiveRespawnGrace();PlayerAt(0,0);floater=mobs.Spawn(d,Hover(0,5));floater.Timer=30;floater.Yaw=180;
            mobs.Damage(floater,3,Vector3.zero);floater.Intent=MobIntent.Idle;floater.Timer=30;
            game.InitializeSaves(Path.Combine(output,"FloaterSaves"));game.SetMode(ScreenMode.Pause);
            long id=floater.Id;var position=floater.Position;
            Check(game.SaveGame("Living Floater",true),"Named save captures a damaged hovering Floater");
            var entry=game.Saves.List().First(e=>e.Id==game.SaveId&&!e.Backup);
            Check(game.LoadGame(entry),"Save reloads the new mob through the existing entity payload");mobs=game.Mobs;mobs.NaturalSpawning=false;game.World.ViewDistance=4;game.SetMode(ScreenMode.Pause);
            floater=mobs.Mobs.Single(m=>m.Id==id);
            Check(floater.Health==d.health-3&&floater.Position.Cell.Equals(position.Cell)&&floater.Position.Fraction==position.Fraction,"Floater save restores exact identity, damage and hovering position");
            game.SetMode(ScreenMode.Play);yield return Until(()=>game.ReadyToPlay&&game.World.PendingCount==0,60,"Restored Floater world streams");
            mobs.enabled=false;PlayerAt(0,1);Aim(floater);yield return null;
            int before=game.Items.Total(BlockId.FloaterRock)+game.Inventory.Total(BlockId.FloaterRock);
            Check(mobs.Damage(floater,100,Vector3.zero),"Lethal Floater hit is accepted");
            Check(game.Items.Total(BlockId.FloaterRock)+game.Inventory.Total(BlockId.FloaterRock)==before+1,"Floater defeat creates exactly one collectible rock");
            Check(!mobs.Damage(floater,100,Vector3.zero)&&game.Items.Total(BlockId.FloaterRock)+game.Inventory.Total(BlockId.FloaterRock)==before+1,"Repeated lethal hits cannot duplicate Floater loot");
            Check(!mobs.Occupies(floater.Position.Cell),"Defeated Floater releases construction occupancy");
            game.SetMode(ScreenMode.Pause);Check(game.SaveGame("Defeated Floater",true),"Death checkpoint captures rock and defeated mob together");entry=game.Saves.List().First(e=>e.Id==game.SaveId&&!e.Backup);
            Check(game.LoadGame(entry),"Death checkpoint reloads");mobs=game.Mobs;mobs.NaturalSpawning=false;game.World.ViewDistance=4;
            Check(!mobs.Mobs.Single(m=>m.Id==id).Alive&&game.Items.Total(BlockId.FloaterRock)+game.Inventory.Total(BlockId.FloaterRock)==before+1,"Loading a defeated Floater preserves one drop without respawning loot");
            yield return Until(()=>game.ReadyToPlay&&game.World.PendingCount==0,60,"Death checkpoint terrain streams");
            game.SetMode(ScreenMode.Play);PlayerAt(0,1);yield return new WaitForSeconds(1.7f);
            Check(!mobs.Mobs.Any(m=>m.Id==id),"Floater death animation completes and releases the view");
            var pile=game.Items.Piles.Single(p=>p.Stack.Id==BlockId.FloaterRock);var rockPosition=pile.Position.Local(game.World.Origin);
            game.Items.enabled=false;game.Selected=14;game.Player.transform.position=rockPosition+new Vector3(0,0,-1.6f);game.Player.ResetMotion();
            var dir=rockPosition-game.Player.Camera.transform.position;game.Player.Yaw=Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg;game.Player.Pitch=-Mathf.Atan2(dir.y,new Vector2(dir.x,dir.z).magnitude)*Mathf.Rad2Deg;
            yield return Capture("floater-rock-drop");game.Items.enabled=true;
            game.Player.transform.position=rockPosition+Vector3.back*.5f;game.Player.ResetMotion();
            yield return Until(()=>game.Inventory.Total(BlockId.FloaterRock)==before+1,4,"Floater Rock uses ordinary proximity pickup");
            game.Selected=Array.FindIndex(game.Inventory.Slots.ToArray(),s=>s.Id==BlockId.FloaterRock);game.Player.Pitch=10;
            yield return new WaitForSeconds(.7f);yield return Capture("floater-rock-held");
            game.SetMode(ScreenMode.Inventory);yield return Capture("floater-rock-inventory");game.SetMode(ScreenMode.Play);
            game.SetMode(ScreenMode.Pause);Check(game.SaveGame("Collected Floater Rock",true),"Collected rock can be saved in inventory");entry=game.Saves.List().First(e=>e.Id==game.SaveId&&!e.Backup);
            Check(game.LoadGame(entry),"Collected rock checkpoint reloads");mobs=game.Mobs;mobs.NaturalSpawning=false;game.World.ViewDistance=4;
            Check(game.Inventory.Total(BlockId.FloaterRock)==before+1&&game.Items.Total(BlockId.FloaterRock)==0,"Save/load conserves the collected Floater Rock");
            game.SetMode(ScreenMode.Play);yield return Until(()=>game.ReadyToPlay&&game.World.PendingCount==0,60,"Collected checkpoint world streams");
            mobs.Clear();PlayerAt(0,0);game.Player.ResetMotion();
        }
    }
}
