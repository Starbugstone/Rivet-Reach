using System;
using System.Collections;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class MobVerification
    {
        IEnumerator SpawnLightChecks(Vector3 cave,MobDefinition floater)
        {
            var world=game.World;var originalPlayer=game.Player.transform.position;
            int groundY=world.Address(cave).Y-1;
            // Resident underground room straddles x=32. Real source/collision APIs,
            // shared light solve and natural eligibility are exercised together.
            for(int z=19;z<=29;z++)for(int x=27;x<=40;x++)for(int y=groundY;y<=groundY+5;y++)
                Block(x,y,z,y==groundY||y==groundY+5||x==27||x==40||z==19||z==29?BlockId.Stone:BlockId.Air);
            var sample=new BlockPos(31,groundY+1,24);
            var feet=world.Local(sample)+new Vector3(.5f,.006f+floater.hoverHeight,.5f);
            game.Player.transform.position=feet+Vector3.back*30;game.Player.ResetMotion();
            game.Player.Yaw=180;game.Player.Camera.transform.rotation=Quaternion.LookRotation(Vector3.back);
            game.Sky.Clock.SetTime(12.0/24);
            byte level=255;
            IEnumerator Settled(string message)=>Until(()=>world.TryGetSpawnLight(sample,false,out level),120,message,
                ()=>"pending "+world.PendingLightChunks+", level "+level);
            yield return Settled("Underground spawn room obtains settled gameplay lighting");
            Check(level==0&&mobs.CanSpawn(floater,feet),"Dark underground ground permits natural Floaters in daylight");
            var source=new BlockPos(33,sample.Y,24);
            Check(world.PlaceTorch(source,source.Offset(0,-1,0)),"Real placed torch attaches across a chunk boundary from the spawn floor");
            Check(!world.TryGetSpawnLight(sample,false,out _)&&!mobs.CanSpawn(floater,feet),"Cross-chunk torch addition immediately defers spawning before its light propagates");
            yield return Settled("Cross-chunk torch propagation settles for spawning");
            Check(level==12&&!mobs.CanSpawn(floater,feet),"Level-12 torch light on the floor blocks a hovering hostile body");
            Check(world.TryGetSpawnLight(sample,true,out byte night)&&night==level,"Cave torch spawn light remains authoritative at night");
            var bright=new MobSpawnRules{habitat=MobHabitat.Underground,minimumLight=8};
            Check(bright.AllowsSite(world,game.Registry,feet,floater.width,floater.height,floater.hoverHeight),"Shared bright-ground profile permits lit cave ground independently of hostile rules");
            world.TorchView.enabled=false;foreach(var light in world.TorchView.Lights)light.enabled=false;
            Check(world.TryGetSpawnLight(sample,false,out byte pooled)&&pooled==level&&!mobs.CanSpawn(floater,feet),"Disabling the presentation light pool cannot permit hostile spawning");
            world.TorchView.enabled=true;world.TorchView.Refresh();
            var existing=mobs.Spawn(floater,feet);
            Check(existing!=null&&existing.Health==floater.health,"Explicit encounters remain valid on lit ground; lighting only restricts natural spawning");
            existing.Yaw=180;existing.Timer=30;existing.View.Present(feet,1f);
            game.Player.transform.position=feet+Vector3.back*3-Vector3.up*floater.hoverHeight;game.Player.ResetMotion();Aim(existing);
            yield return Capture("lit-cave-spawn-floor");
            Check(existing.Health==floater.health,"Lighting a room does not remove or damage an existing Floater");
            mobs.Clear();game.Player.transform.position=feet+Vector3.back*30;game.Player.ResetMotion();
            game.Player.Yaw=180;game.Player.Camera.transform.rotation=Quaternion.LookRotation(Vector3.back);
            Check(world.Remove(source,BlockId.Torch),"Placed torch is removed through the ordinary world transaction");
            Check(!world.TryGetSpawnLight(sample,false,out _),"Cross-chunk source removal defers stale spawn-light results");
            yield return Settled("Removed source light fully leaves the spawn floor");
            Check(level==0&&mobs.CanSpawn(floater,feet),"Removing the torch restores natural spawning after darkness returns");
            source=new BlockPos(38,sample.Y,24);
            Check(world.PlaceTorch(source,source.Offset(0,-1,0)),"Boundary-light fixture places a torch seven steps from the floor");
            yield return Settled("Level-7 boundary settles");
            Check(level==7&&mobs.CanSpawn(floater,feet),"Actual propagated level 7 is included in hostile spawning");
            Check(!mobs.CanSpawn(floater,feet+Vector3.right*.49f),"A footprint spanning level-7 and level-8 ground is rejected if any supporting cell is bright");
            world.Remove(source,BlockId.Torch);source=source.Offset(-1,0,0);world.PlaceTorch(source,source.Offset(0,-1,0));
            yield return Settled("Level-8 boundary settles");
            Check(level==8&&!mobs.CanSpawn(floater,feet),"Moving a real torch one cell closer produces level 8 and blocks hostile spawning");
            world.Remove(source,BlockId.Torch);
            yield return Settled("Spawn-light fixture finishes without retained sources");
            game.Player.transform.position=originalPlayer;game.Player.ResetMotion();
            game.Player.Yaw=180;game.Player.Camera.transform.rotation=Quaternion.LookRotation(Vector3.back);
        }
    }
}
