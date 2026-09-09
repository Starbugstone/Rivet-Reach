using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewOres()
        {
            var world=game.World;var player=game.Player;var mined=new List<BlockPos>();
            Check(TerrainGenerator.Version=="terrain-4-biomes-caves","Ore generation and base layer have a new deterministic version");
            game.Inventory.Add(BlockId.IronPickaxe,1,10,11); // Explicit review fixture, sufficient for every ore tier.
            foreach(var band in OreGenerator.Bands)
            {
                // Find actual generated ore. Only approach corridors are verification fixtures.
                var target=OreGenerator.Veins(game.Seed,new BlockPos(-64,band.PeakY-16,-64),new BlockPos(64,band.PeakY+16,64))
                    .Where(v=>v.Band.Block==band.Block&&world.Generator.At(v.Centre)==band.Block).Select(v=>v.Centre).First();
                player.enabled=false;player.ResetMotion();player.transform.position=world.Local(target)+new Vector3(.5f,-1,-2.5f);
                yield return null;yield return Settle();
                for(int z=-4;z<=-1;z++)for(int y=-1;y<=2;y++)for(int x=-1;x<=1;x++)
                {var p=target.Offset(x,y,z);byte id=world.Get(p);if(id!=0)world.Remove(p,id);}
                for(int y=1;y<=2;y++){var p=target.Offset(0,y,0);if(world.Get(p)!=0)world.Remove(p,world.Get(p));}
                // Keep the entire approach supported even where the new cavern profile is open.
                // Clear below the untouched ore so its drop can fall into the reachable corridor.
                for(int x=-1;x<=1;x++){var p=target.Offset(x,-1,0);if(world.Get(p)!=0)world.Remove(p,world.Get(p));}
                for(int z=-4;z<=0;z++)for(int x=-1;x<=1;x++)
                {var support=target.Offset(x,-2,z);if(world.Get(support)==0)world.Place(support,BlockId.Stone);}
                player.Yaw=0;player.Pitch=3;player.enabled=true;game.Selected=8;
                yield return new WaitForSecondsRealtime(.5f);
                sampling=true;
                Check(player.HasTarget&&player.Target.Equals(target),"Player can target natural "+game.Registry.Get(band.Block).displayName+" at Y "+target.Y);
                int spawned=game.Items.TotalSpawned;
                foreach(var tool in new[]{ToolCapability.None,ToolCapability.Axe,ToolCapability.Blade})
                    Check(!world.Mine(target,band.Block,tool)&&world.Get(target)==band.Block,"Authority rejects unsuitable tool for "+band.Block);
                player.VerificationMining=true;yield return new WaitForSecondsRealtime(.8f);player.VerificationMining=false;
                Check(world.Get(target)==band.Block&&player.MiningProgress==0&&game.Items.TotalSpawned==spawned,"Holding Mine barehanded leaves ore and drops unchanged");
                Check(game.UI.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("pickaxe or better")),"HUD explains ore's required tool");
                yield return Capture("ore-"+band.Block+"-natural");
                game.Selected=10;yield return new WaitForSecondsRealtime(.5f);
                Check(player.HeldBlock.Visible&&player.HeldBlock.ItemId==BlockId.IronPickaxe,"Selected iron pickaxe is displayed before extraction");
                byte raw=game.Registry.FistDrop(band.Block);int before=game.Inventory.Total(raw);
                player.VerificationMining=true;float until=Time.realtimeSinceStartup+5;
                while(world.Get(target)!=0&&Time.realtimeSinceStartup<until)yield return null;
                player.VerificationMining=false;
                Check(world.Get(target)==0&&game.Items.TotalSpawned==spawned+1,"Holding Mine with pickaxe removes one natural ore and produces one drop");
                Check(!world.Mine(target,band.Block,ToolCapability.Pickaxe)&&game.Items.TotalSpawned==spawned+1,"Stale repeated ore command cannot duplicate extraction");
                player.transform.position+=Vector3.forward*1.5f;yield return new WaitForSecondsRealtime(1);
                Check(game.Inventory.Total(raw)==before+1,"Walking close collects the matching raw resource exactly once");
                game.Selected=game.Inventory.FindSlot(s=>s.Id==raw);yield return new WaitForSecondsRealtime(.6f);
                Check(player.HeldBlock.Visible&&player.HeldBlock.ItemId==raw,"Collected raw resource has its own held presentation");
                Check(!game.CanPlace(target,out _)&&!world.Place(target,raw)&&game.Inventory.Total(raw)==before+1,"Raw resource cannot be placed as richer ore");
                yield return Capture("ore-"+band.Block+"-collected");
                mined.Add(target);sampling=false;
            }
            game.SetMode(ScreenMode.Inventory);yield return Capture("ore-inventory");game.SetMode(ScreenMode.Play);
            // The actual generator floor, opened from above to check collision and mining.
            var floor=new BlockPos(0,TerrainGenerator.MinY,0);player.enabled=false;
            player.transform.position=world.Local(floor)+new Vector3(.5f,1.001f,.5f);
            yield return null;yield return Settle();
            for(int z=-3;z<=3;z++)for(int x=-3;x<=3;x++)for(int y=1;y<=4;y++)
            {var p=floor.Offset(x,y,z);if(world.Get(p)!=0)world.Remove(p,world.Get(p));}
            player.Pitch=85;player.Yaw=0;player.enabled=true;game.Selected=10;
            yield return new WaitForSecondsRealtime(.5f);
            sampling=true;
            Check(player.HasTarget&&player.TargetId==BlockId.Bedrock,"Looking down at the world base targets bedrock");
            int floorDrops=game.Items.TotalSpawned,edits=world.EditCount;
            foreach(var tool in new[]{ToolCapability.None,ToolCapability.Axe,ToolCapability.Pickaxe,ToolCapability.Blade,ToolCapability.Axe|ToolCapability.Pickaxe|ToolCapability.Blade})
                Check(!world.Mine(floor,BlockId.Bedrock,tool),"Bedrock rejects mining with "+tool);
            Check(!world.Remove(floor,BlockId.Bedrock)&&!world.Place(floor,BlockId.Dirt)&&!world.Remove(floor.Offset(0,-1,0),BlockId.Bedrock),"Direct removal and placement cannot open or replace the world base");
            player.VerificationMining=true;yield return new WaitForSecondsRealtime(2.5f);player.VerificationMining=false;
            Check(world.EditCount==edits&&game.Items.TotalSpawned==floorDrops&&player.MiningProgress==0,"Sustained bedrock mining creates no edits, drops or progress");
            var moved=world.Move(player.transform.position,Vector3.down*10,.6f,1.8f,out bool grounded);
            float floorTop=world.Local(floor).y+1;
            // Swept collision intentionally permits the existing 1 mm skin at a face.
            Check(grounded&&moved.y>=floorTop-.0011f&&moved.y<floorTop+.01f&&!world.Overlaps(moved,.6f,1.8f)&&player.Grounded,"Bedrock collision stops at its top face within the movement skin: "+moved.y);
            Check(game.UI.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("Unbreakable")),"HUD explains bedrock is unbreakable");
            yield return Capture("bedrock-floor");
            player.enabled=false;player.transform.position+=Vector3.right*1024;yield return null;yield return Settle();
            Check(mined.All(p=>!world.Ready(p)&&world.Get(p)==0),"All mined ores stay depleted while their chunks are nonresident");
            var first=mined[0];player.transform.position=world.Local(first)+new Vector3(.5f,-1,-2.5f);
            yield return null;yield return Settle();
            Check(world.Ready(first)&&world.Get(first)==0,"Ore depletion survives chunk reload and floating-origin travel");
            player.transform.position=world.Local(floor)+new Vector3(.5f,1.001f,.5f);yield return null;yield return Settle();
            Check(world.Get(floor)==BlockId.Bedrock,"Bedrock remains intact after unloading and reloading");
            sampling=false;report.triangles=world.MeshTriangles;report.terrainTilePixels=Resources.Load<Texture2DArray>("Materials/BlockTiles").width;
        }
    }
}
