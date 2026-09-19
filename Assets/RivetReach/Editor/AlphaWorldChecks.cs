using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class AlphaWorldChecks
    {
        sealed class ReactionWorld:IFluidWorld
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public readonly HashSet<BlockPos> Unloaded=new HashSet<BlockPos>();
            public readonly FluidSimulation Solver=new FluidSimulation(Fluids.Registry);
            public int Reactions;
            public byte Get(BlockPos p)=>Cells.TryGetValue(p,out byte id)?id:BlockId.Stone;
            public bool TryRead(BlockPos p,out byte id){id=Get(p);return !Unloaded.Contains(p);}
            public bool ChangeFluid(BlockPos p,byte before,byte after)
            {if(Unloaded.Contains(p)||Get(p)!=before)return false;Cells[p]=after;if(after==BlockId.LavaRock)Reactions++;return true;}
            public void Put(BlockPos p,byte id){Cells[p]=id;Solver.Changed(this,p);}
            public void Run(int ticks){for(int i=0;i<ticks;i++)Solver.Step(this);}
        }
        static Bounds BoundsIn(Transform frame,Renderer[] renderers)
        {
            bool found=false;Bounds bounds=default;
            foreach(var renderer in renderers)if(renderer.enabled)
            {
                var local=renderer.localBounds;
                for(int z=0;z<2;z++)for(int y=0;y<2;y++)for(int x=0;x<2;x++)
                {
                    var point=frame.InverseTransformPoint(renderer.transform.TransformPoint(new Vector3(x==0?local.min.x:local.max.x,y==0?local.min.y:local.max.y,z==0?local.min.z:local.max.z)));
                    if(found)bounds.Encapsulate(point);else{bounds=new Bounds(point,Vector3.zero);found=true;}
                }
            }
            if(!found)throw new Exception("Alpha world: display has no enabled renderers.");
            return bounds;
        }
        public static void Run()
        {
            int checks=0;var report=new List<string>();
            void Check(bool ok,string name){if(!ok)throw new Exception("Alpha world: "+name);checks++;}
            var p=new BlockPos(0,8,0);
            // A real sustained water stream arrives from a separate source, rather
            // than a permanently hand-injected flow cell that the solver would drain.
            foreach(var direction in new[]{new BlockPos(1,0,0),new BlockPos(-1,0,0),new BlockPos(0,0,1),new BlockPos(0,0,-1)})
            {
                var w=new ReactionWorld();
                w.Put(p.Offset((int)direction.X,0,(int)direction.Z),0);
                w.Put(p.Offset((int)direction.X*2,0,(int)direction.Z*2),Fluids.Water.Source);w.Run(40);
                w.Put(p,Fluids.Lava.Source);w.Run(200);
                Check(w.Get(p)==BlockId.LavaRock&&w.Reactions==1,"Side water flow converts one source exactly once");
                w.Run(200);Check(w.Get(p)==BlockId.LavaRock&&w.Reactions==1,"Lava Rock settles permanently");
            }
            {
                var w=new ReactionWorld();w.Put(p,Fluids.Lava.Source);w.Put(p.Offset(0,1,0),0);w.Put(p.Offset(0,2,0),Fluids.Water.Source);w.Run(200);
                Check(w.Get(p)==BlockId.LavaRock&&w.Reactions==1,"Falling water converts the source below");
            }
            {
                var w=new ReactionWorld();w.Put(p,Fluids.Lava.Source);w.Put(p.Offset(1,0,0),Fluids.Water.Source);w.Run(200);
                Check(w.Get(p)==Fluids.Lava.Source&&w.Reactions==0,"Direct source-only contact does not qualify");
            }
            {
                var w=new ReactionWorld();w.Put(p,Fluids.Lava.Flow(1));w.Put(p.Offset(0,0,-1),Fluids.Lava.Source);
                w.Put(p.Offset(1,0,0),0);w.Put(p.Offset(2,0,0),Fluids.Water.Source);w.Run(200);
                Check(w.Get(p)!=BlockId.LavaRock&&w.Reactions==0,"Flowing lava is not converted");
            }
            {
                var w=new ReactionWorld();w.Put(p,Fluids.Lava.Source);w.Put(p.Offset(1,0,0),Fluids.Water.Flow(1));w.Unloaded.Add(p.Offset(1,0,0));w.Run(40);
                Check(w.Get(p)==Fluids.Lava.Source&&w.Reactions==0,"Unreadable neighbouring fluid is not guessed");
            }
            var registry=ItemRegistry.Load();
            foreach(var tier in new[]{ToolTier.None,ToolTier.Wood,ToolTier.Stone,ToolTier.Copper,ToolTier.Iron})
                Check(!BlockId.Mineable(BlockId.LavaRock,ToolCapability.Pickaxe,tier),"Lower-tier pick cannot harvest Lava Rock");
            Check(BlockId.Mineable(BlockId.LavaRock,ToolCapability.Pickaxe,ToolTier.Diamond)&&!BlockId.Mineable(BlockId.LavaRock,ToolCapability.Axe,ToolTier.Diamond),"Diamond pick only");
            Check(registry.Get(BlockId.LavaRock).stableId=="rivet:lava_rock"&&BlockId.Tile(BlockId.LavaRock,0,1)==45,"Lava Rock has its own item and texture");
            Check((BlockDefinitions.Get(BlockId.MobSpawner).Traits&BlockTraits.NoMiningDrop)!=0,"Spawner removal has no collectible drop");
            var generator=new TerrainGenerator(246813);var profile=new SpawnerRoomProfile(128,100,100,96);bool hasRoom=false;
            for(int z=-2;z<2&&!hasRoom;z++)for(int x=-2;x<2&&!hasRoom;x++)if(SpawnerRooms.TryRoom(generator,x,z,out var room,profile))
            {
                hasRoom=true;Check(room.Buried,"Forced buried profile selects buried template");
                for(int dz=-4;dz<=4;dz++)for(int dy=0;dy<=5;dy++)for(int dx=-4;dx<=4;dx++)
                {
                    var cell=room.FloorCentre.Offset(dx,dy,dz);Check(cell.Chunk.Equals(room.Spawner.Chunk),"Room fits one generated chunk");
                    Check(room.TryCell(cell,out byte id),"Template includes the complete shell and interior");
                    bool shell=dy==0||dy==5||Math.Abs(dx)==4||Math.Abs(dz)==4;
                    Check(id==(shell?BlockId.Cobblestone:cell.Equals(room.Spawner)?BlockId.MobSpawner:(byte)0),"Buried template has cobblestone shell, hollow interior and one spawner");
                }
            }
            Check(hasRoom,"A viable buried site exists");
            Check(SpawnerRooms.TryRoom(generator,0,-8,out var connected),"Known connected room remains deterministic");
            Check(!connected.Buried&&connected.CorridorLength>=1&&connected.CorridorLength<=6,"Ordinary room has a bounded cave entrance");
            var cells=generator.Generate(connected.Spawner.Chunk);
            for(int z=-1;z<=32;z++)for(int y=-1;y<=32;y++)for(int x=-1;x<=32;x++)
            {var cell=connected.Spawner.Chunk.Min.Offset(x,y,z);Check(cells[ChunkMesher.Index(x,y,z)]==generator.At(cell),"Room point/chunk/halo reads agree");}
            var previous=new TerrainGenerator(generator.Seed,TerrainGenerator.FarmVersion);
            Check(previous.At(connected.Spawner)!=BlockId.MobSpawner,"Previous generator never introduces a spawner");
            var cage=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Spawners/Cage"));
            try
            {
                var renderers=cage.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                int triangles=cage.GetComponentsInChildren<MeshFilter>().Sum(filter=>filter.sharedMesh.triangles.Length/3);
                Check(triangles==3472&&renderers.All(r=>r.sharedMaterials.Length==1&&r.sharedMaterial.shader.name=="RivetReach/WorldLit"),"Imported cage retains authored geometry and cave-aware material");
                Check(bounds.size.x<1&&bounds.size.y<1&&bounds.size.z<1&&bounds.size.y>.9f,"Imported cage fits one metre voxel with correct scale");
                report.Add("Unity cage import: "+triangles+" triangles; bounds metres "+bounds.size+"; one material per renderer.");
            }
            finally{UnityEngine.Object.DestroyImmediate(cage);}
            var floater=Resources.LoadAll<MobDefinition>("Mobs/Definitions").Single(d=>d.stableId=="rivet:floater");
            var spawner=SpawnerVisuals.Create(null,floater);
            try
            {
                var miniature=spawner.transform.Find("Species display (visual only)");
                var worldDisplayBounds=miniature.GetComponentsInChildren<Renderer>()[0].bounds;
                Check((spawner.transform.lossyScale-Vector3.one).sqrMagnitude<.000001f&&worldDisplayBounds.size.magnitude>.1f&&
                    Mathf.Max(worldDisplayBounds.size.x,Mathf.Max(worldDisplayBounds.size.y,worldDisplayBounds.size.z))<.581f&&
                    (worldDisplayBounds.center-Vector3.up*.5f).magnitude<.001f,"Neutral cage root gives the miniature real metre-scale world bounds and centre");
                Check(miniature!=null,"Spawner cage includes its configured species display");
                miniature.Rotate(0,137,0);
                var rotatedBounds=BoundsIn(spawner.transform,miniature.GetComponentsInChildren<Renderer>());
                Check((rotatedBounds.center-Vector3.up*.5f).magnitude<.001f,"Rotating a miniature keeps its actual geometry centred in the cage");
                miniature.localRotation=Quaternion.identity;
                var renderers=miniature.GetComponentsInChildren<Renderer>();Check(renderers.Length>0,"Spawner miniature has render geometry");var bounds=renderers[0].bounds;
                bounds=BoundsIn(spawner.transform,renderers);
                Check(bounds.size.magnitude>.1f&&Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))<=.581f,
                    "Spawner miniature evaluates and fits a visible frozen Floater inside the cage");
                Check(miniature.GetComponentsInChildren<Animator>().Length==0&&miniature.GetComponentsInChildren<SkinnedMeshRenderer>().Length==0&&
                    miniature.GetComponentsInChildren<Collider>().Length==0&&miniature.GetComponent<MobView>()==null,
                    "Spawner miniature is baked render-only geometry without live creature components");
            }
            finally{UnityEngine.Object.DestroyImmediate(spawner);}
            var parent=new GameObject("Scaled rotated spawner parent");parent.transform.localScale=Vector3.one*.2f;parent.transform.localRotation=Quaternion.Euler(30,70,15);
            spawner=SpawnerVisuals.Create(parent.transform,floater);
            try
            {
                var miniature=spawner.transform.Find("Species display (visual only)");
                var bounds=BoundsIn(spawner.transform,miniature.GetComponentsInChildren<Renderer>());
                Check((bounds.center-new Vector3(0,.5f,0)).sqrMagnitude<.000001f&&Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))<=.581f,
                    "Spawner miniature fits in cage-local bounds under a scaled rotated parent");
            }
            finally{UnityEngine.Object.DestroyImmediate(parent);}
            report.Add("PASS "+checks+" alpha world assertions");Directory.CreateDirectory("Logs/AlphaPlaytest");File.WriteAllLines("Logs/AlphaPlaytest/world-checks.txt",report);Debug.Log(report[0]);
        }
    }
}
