using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewTerrain()
        {
            var world=game.World;var g=world.Generator;var player=game.Player;var sites=new StringBuilder();
            Check(TerrainGenerator.Version=="terrain-4-biomes-caves","Session uses the biome/cave generator version");
            var tiles=Resources.Load<Texture2DArray>("Materials/BlockTiles");
            Check(tiles.depth>=44&&tiles.width==64,"All four biome material layers load in the player");
            Check(!world.Overlaps(player.transform.position,.6f,1.8f),"Natural spawn has support and clear player volume");
            world.ViewDistance=6;game.Diagnostics=false;
            report.viewRadius=6;report.fogStart=world.FogStart;report.fogEnd=world.FogEnd;
            foreach(var biome in (BiomeId[])Enum.GetValues(typeof(BiomeId)))
            {
                var site=TerrainReviewSites.Biome(g,biome);sites.AppendLine(TerrainProfile.Name(biome)+": "+site);
                float yaw=biome==BiomeId.Desert?-90:biome==BiomeId.Alpine?-45:35;
                player.ResetMotion();player.transform.position=world.Local(site.Offset(0,1,0))+new Vector3(.5f,.01f,.5f);
                player.Pitch=8;player.Yaw=yaw;yield return null;yield return Settle(90);
                Check(world.Get(site)==g.At(site)&&BlockId.Solid(world.Get(site)),"Loaded "+biome+" preserves generated surface material and support");
                Check(!world.Overlaps(player.transform.position,.6f,1.8f),"Player fits the natural "+biome+" surface");
                bool loaded=true;
                // Check actual extrema across multiple streamed mountain columns, not only centre heights.
                for(int dz=-80;dz<=80;dz+=8)for(int dx=-80;dx<=80;dx+=8)
                {
                    long x=site.X+dx,z=site.Z+dz;int h=g.Height(x,z);
                    loaded&=world.Ready(new BlockPos(x,h,z))&&world.Ready(new BlockPos(x,h+TerrainGenerator.MaxTreeHeight,z));
                }
                Check(loaded,"Full nearby surface and tree height range is resident in "+biome);
                yield return Capture("terrain-ground-"+biome.ToString().ToLowerInvariant());
                // Explicit survey camera: generated world is unchanged; no gameplay flight is added.
                player.enabled=false;var feet=WorldPoint.FromLocal(player.transform.position,world.Origin);
                player.transform.position+=Vector3.up*34;player.Camera.transform.rotation=Quaternion.Euler(24,yaw,0);
                player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
                yield return null;yield return Settle(90);yield return Capture("terrain-survey-"+biome.ToString().ToLowerInvariant());
                player.transform.position=feet.Local(world.Origin);player.ResetMotion();player.enabled=true;
                player.Arms.gameObject.SetActive(true);player.Body.gameObject.SetActive(true);yield return null;
            }
            var cave=TerrainReviewSites.Cave(g);sites.AppendLine("Cave: "+cave);
            player.ResetMotion();player.transform.position=world.Local(cave)+new Vector3(.5f,.01f,.5f);player.Pitch=0;player.Yaw=0;
            yield return null;yield return Settle(90);
            Check(world.Get(cave)==0&&BlockId.Solid(world.Get(cave.Offset(0,-1,0)))&&!world.Overlaps(player.transform.position,.6f,1.8f),"Deep natural cavern supports a standing player");
            Check(world.Ready(cave.Offset(0,-80,0))&&world.Ready(cave.Offset(0,80,0)),"Cavern floor and ceiling depths beyond three local chunk layers are resident");
            yield return Capture("terrain-cave");
            // Exercise the actual swept collision path inside a generated passage.
            Vector3 before=player.transform.position,moved=world.Move(before,Vector3.forward*3,.6f,1.8f,out _);
            Check(moved.z-before.z>2.9f&&!world.Overlaps(moved,.6f,1.8f),"Generated cavern permits three metres of collision-checked traversal");
            var fixture=cave.Offset(0,1,3);
            foreach(byte id in new[]{BlockId.Sand,BlockId.Sandstone,BlockId.Snow,BlockId.RedClay})
            {
                Check(game.Registry.Get(id).runtimeId==id&&BlockId.Placeable(id),"Biome block definition is registered and placeable: "+id);
                Check(world.Place(fixture,id),"Biome material can be placed into a loaded cave cell: "+id);
                Vector3 aim=world.Local(fixture)+Vector3.one*.5f-player.Camera.transform.position;
                player.Yaw=Mathf.Atan2(aim.x,aim.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Atan2(aim.y,new Vector2(aim.x,aim.z).magnitude)*Mathf.Rad2Deg;
                game.Selected=0;game.Inventory.Take(0,int.MaxValue);yield return null;
                Check(player.HasTarget&&player.Target.Equals(fixture),"Placed biome block is targeted by the real mining ray");
                int drops=game.Items.TotalSpawned;player.VerificationMining=true;float until=Time.realtimeSinceStartup+4;
                while(world.Get(fixture)!=0&&Time.realtimeSinceStartup<until)yield return null;
                player.VerificationMining=false;InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
                Check(world.Get(fixture)==0&&game.Items.TotalSpawned==drops+1,"Fist extraction creates exactly one biome block drop: "+id);
            }
            Check(world.Place(fixture,BlockId.RedClay),"Clay placement is recorded before streaming away");
            var far=new BlockPos(800,g.Height(800,0)+1,0);
            player.ResetMotion();player.transform.position=world.Local(far)+new Vector3(.5f,.01f,.5f);yield return null;yield return Settle(90);
            Check(world.Origin.X!=0&&!world.Ready(fixture),"Terrain review crosses an origin shift and unloads the cave");
            player.ResetMotion();player.transform.position=world.Local(cave)+new Vector3(.5f,.01f,.5f);yield return null;yield return Settle(90);
            Check(world.Get(fixture)==BlockId.RedClay,"Biome placement survives unload, regeneration and origin shifts");
            var floor=new BlockPos(0,TerrainGenerator.MinY+1,0);
            // Inspect the protected base with locomotion disabled; do not alter the three stone layers above it.
            player.enabled=false;player.transform.position=world.Local(floor)+new Vector3(.5f,.01f,.5f);yield return null;yield return Settle(90);
            var bedrock=floor.Offset(0,-1,0);
            Check(world.Get(bedrock)==BlockId.Bedrock&&!world.Remove(bedrock,BlockId.Bedrock)&&!world.Mine(bedrock,BlockId.Bedrock,ToolCapability.Pickaxe),"Bedrock remains continuous and unbreakable after cave generation");
            Check(world.Error==null&&errors.Count==0,"Biome and cave review completes without Unity or worker errors");
            report.triangles=world.MeshTriangles;File.WriteAllText(Path.Combine(output,"terrain-sites.txt"),sites.ToString());
            game.SetMode(ScreenMode.Pause);
        }
    }
}
