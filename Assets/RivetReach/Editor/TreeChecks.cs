using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class TreeChecks
    {
        public static void Run(Action<bool,string> check)
        {
            foreach(int seed in new[]{246813,777,-917,int.MinValue})
            {
                var generator=new TerrainGenerator(seed);
                var trees=generator.Trees(-40,-40,40,40).ToArray();check(trees.Length>0,"Seed generates trees "+seed);
                foreach(var tree in trees)
                {
                    check(generator.At(tree.Root)==BlockId.Log,"Generated root is a log");
                    check(generator.At(tree.Root.Offset(0,-1,0))==BlockId.Grass,"Generated tree has ground support");
                    check(generator.At(tree.Root.Offset(0,tree.Logs,0))==BlockId.Leaves,"Generated tree has a leafy crown");
                }
                check(generator.At(new BlockPos(0,generator.Height(0,0)+1,0))==0&&generator.At(new BlockPos(0,generator.Height(0,0)+2,0))==0,"Spawn stays clear");
                // Compare point queries and independently generated halo pages on all six faces,
                // choosing tree-bearing pages rather than a terrain-only fixture.
                var selected=trees.First(t=>t.Root.X<0&&t.Root.Z<0).Root.Chunk;
                var first=generator.Generate(selected);
                check(first.SequenceEqual(new TerrainGenerator(seed).Generate(selected)),"Trees repeat with same seed");
                foreach(var offset in new[]{(1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1)})
                {
                    var neighbour=selected.Offset(offset.Item1,offset.Item2,offset.Item3);var page=generator.Generate(neighbour);
                    for(int z=-1;z<=32;z++)for(int y=-1;y<=32;y++)for(int x=-1;x<=32;x++)
                    {
                        if(x!=-1&&x!=32&&y!=-1&&y!=32&&z!=-1&&z!=32)continue;
                        var p=selected.Min.Offset(x,y,z);long nx=p.X-neighbour.Min.X,nz=p.Z-neighbour.Min.Z;int ny=p.Y-neighbour.Min.Y;
                        if(nx< -1||nx>32||ny< -1||ny>32||nz< -1||nz>32)continue;
                        byte id=first[ChunkMesher.Index(x,y,z)];
                        check(id==page[ChunkMesher.Index((int)nx,ny,(int)nz)]&&id==generator.At(p),"Tree halo matches adjacent page and point query");
                    }
                }
            }
            var cached=new TerrainGenerator(246813);var cachePage=new ChunkPos(-1,2,-1);var cacheReference=cached.Generate(cachePage);
            for(int seed=0;seed<20;seed++)new TerrainGenerator(seed).Generate(new ChunkPos(seed-10,2,seed+7));
            check(cacheReference.SequenceEqual(cached.Generate(cachePage)),"Candidate-cache eviction and interleaved seeds preserve generation");
            var workers=Enumerable.Range(0,4).Select(_=>System.Threading.Tasks.Task.Run(()=>new TerrainGenerator(246813).Generate(cachePage))).ToArray();
            System.Threading.Tasks.Task.WaitAll(workers);
            check(workers.All(t=>cacheReference.SequenceEqual(t.Result)),"Thread-local candidate caches preserve parallel generation results");
            var registry=ItemRegistry.Load();
            check(registry.Capabilities(new ItemStack(BlockId.StarterAxe,1))==ToolCapability.Axe,"Starter axe capability comes from definition");
            check(registry.Capabilities(default)==ToolCapability.None&&registry.Capabilities(new ItemStack(BlockId.Log,1))==ToolCapability.None,"Empty hands and held blocks have no axe capability");
            check(!BlockId.Placeable(BlockId.StarterAxe)&&BlockId.Placeable(BlockId.Log)&&BlockId.Placeable(BlockId.Leaves),"Tools cannot be placed as voxels");
            check(registry.MiningSeconds(BlockId.Log,ToolCapability.Axe)<registry.MiningSeconds(BlockId.Log,ToolCapability.Pickaxe),"Only axe capability accelerates logs");
            check(registry.FistDrop(BlockId.Log)==BlockId.Log&&registry.FistDrop(BlockId.Leaves)==BlockId.Leaves,"Hand mining tree blocks conserves their items");
            var world=new Fixture();var cut=new BlockPos(31,31,-1);
            for(int y=-2;y<=4;y++)world.cells[cut.Offset(0,y,0)]=BlockId.Log;
            world.cells[cut.Offset(1,2,0)]=BlockId.Log;world.cells[cut.Offset(2,2,0)]=BlockId.Log;
            world.cells[cut.Offset(3,2,0)]=BlockId.Leaves;world.cells[cut.Offset(4,2,0)]=BlockId.Log;
            world.RemoveTreeBlock(cut,BlockId.Log,true);world.sim.FellAbove(world,cut);
            for(int i=0;i<5;i++)world.sim.Step(world);
            check(world.logs==7,"Upward felling conserves one drop per removed trunk/branch log across x/y/z seams");
            check(world.Get(cut.Offset(0,-1,0))==BlockId.Log&&world.Get(cut.Offset(0,-2,0))==BlockId.Log,"Felling leaves logs below cut intact");
            check(world.Get(cut.Offset(4,2,0))==BlockId.Log,"Leaves do not connect felling to another tree");
            world.sim.FellAbove(world,cut);world.sim.Step(world);check(world.logs==7,"Repeated felling cannot duplicate drops");
            // A connected leaf path protects the canopy; removed support schedules decay.
            world=new Fixture();var log=new BlockPos(0,0,0);world.cells[log]=BlockId.Log;
            for(int x=1;x<=4;x++)world.cells[log.Offset(x,0,0)]=BlockId.Leaves;
            var placed=log.Offset(0,1,0);world.cells[placed]=BlockId.Leaves;world.placed.Add(placed);
            world.sim.SupportRemoved(world,log);for(int i=0;i<25;i++)world.sim.Step(world);
            check(world.Get(log.Offset(4,0,0))==BlockId.Leaves,"Nearby logs support leaves through a bounded connected leaf path");
            world.RemoveTreeBlock(log,BlockId.Log,true);for(int i=0;i<25;i++)world.sim.Step(world);
            check(Enumerable.Range(1,4).All(x=>world.Get(log.Offset(x,0,0))==0),"Natural unsupported leaves decay");
            check(world.Get(placed)==BlockId.Leaves&&world.logs==1,"Placed leaves persist and decay does not create log drops");
            world=new Fixture();world.cells[log]=BlockId.Log;
            for(int x=1;x<=4;x++)world.cells[log.Offset(x,0,0)]=BlockId.Leaves;
            world.RemoveTreeBlock(log.Offset(1,0,0),BlockId.Leaves,false);
            for(int i=0;i<30;i++)world.sim.Step(world);
            check(world.Get(log.Offset(4,0,0))==0&&world.Get(log)==BlockId.Log,"Removing a leaf bridge also schedules unsupported foliage decay");
            world=new Fixture();for(int y=0;y<100;y++)world.cells[log.Offset(0,y,0)]=BlockId.Log;
            world.RemoveTreeBlock(log,BlockId.Log,true);world.sim.FellAbove(world,log);world.sim.Step(world);
            check(world.logs==1+TreeSimulation.WorkPerStep&&world.sim.PendingFells>0,"Large generated-log fixtures obey the per-step felling budget");
            for(int i=0;i<5;i++)world.sim.Step(world);
            check(world.logs==100&&world.sim.PendingFells==0,"Bounded felling eventually removes the entire upward generated-log fixture");
            world=new Fixture();for(int y=0;y<5;y++)world.cells[log.Offset(0,y,0)]=BlockId.Log;
            var construction=log.Offset(1,1,0);world.cells[construction]=BlockId.Log;world.placed.Add(construction);
            world.RemoveTreeBlock(log,BlockId.Log,true);world.sim.FellAbove(world,log);
            // Replace a queued natural log with a player log before the next step.
            world.placed.Add(log.Offset(0,1,0));world.sim.Step(world);
            check(world.logs==1&&world.Get(log.Offset(0,1,0))==BlockId.Log&&world.Get(construction)==BlockId.Log,"Queued felling rechecks provenance and protects replacement/base logs");
            world=new Fixture();for(int y=0;y<5;y++)world.cells[log.Offset(0,y,0)]=BlockId.Log;
            world.cells[construction]=BlockId.Log;world.placed.Add(construction);
            world.RemoveTreeBlock(log,BlockId.Log,true);world.sim.FellAbove(world,log);world.sim.Step(world);
            check(world.logs==5&&world.Get(construction)==BlockId.Log,"Generated-tree felling cannot enter touching player-placed wood");
            var cells=new byte[34*34*34];cells[ChunkMesher.Index(0,0,0)]=BlockId.Log;
            var mesh=ChunkMesher.Build(default,0,cells);check(mesh.Tiles.Any(t=>t.x==4)&&mesh.Tiles.Any(t=>t.x==5),"Logs render bark and end grain on distinct faces");
            cells[ChunkMesher.Index(0,0,0)]=BlockId.Leaves;check(ChunkMesher.Build(default,0,cells).Tiles.All(t=>t.x==6),"Leaves use their own tile");
            var axeTexture=Resources.Load<Texture2D>("Tools/StarterAxe");var axeIcon=Resources.Load<Texture2D>("Tools/StarterAxeIcon");
            check(axeTexture!=null&&axeTexture.width==128&&axeIcon!=null&&axeIcon.width==64,"Axe palette and icon import as ordinary 2D textures");
            var axe=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Tools/StarterAxe"));
            check(axe.GetComponentsInChildren<Transform>().Any(t=>t.name=="BladeForward")&&axe.GetComponentsInChildren<Transform>().Any(t=>t.name=="HandleUp"),"Axe import preserves explicit blade/handle orientation markers");
            var axeBounds=axe.GetComponentInChildren<MeshRenderer>().bounds;
            check(axeBounds.size.y>.50f&&axeBounds.size.y<.60f&&axeBounds.size.x<.30f,"Imported axe retains metre scale rather than FBX centimetres");
            UnityEngine.Object.DestroyImmediate(axe);
            var tiles=Resources.Load<Texture2DArray>("Materials/BlockTiles");check(tiles.depth==18,"Terrain, tree, ore, bedrock and raw resource texture layers are available");
        }
        sealed class Fixture : ITreeWorld
        {
            public readonly Dictionary<BlockPos,byte> cells=new Dictionary<BlockPos,byte>();
            public readonly HashSet<BlockPos> placed=new HashSet<BlockPos>();
            public readonly TreeSimulation sim=new TreeSimulation();public int logs;
            public byte Get(BlockPos p)=>cells.TryGetValue(p,out var id)?id:(byte)0;
            public bool NaturalLog(BlockPos p)=>Get(p)==BlockId.Log&&!placed.Contains(p);
            public bool NaturalLeaf(BlockPos p)=>Get(p)==BlockId.Leaves&&!placed.Contains(p);
            public bool RemoveTreeBlock(BlockPos p,byte expected,bool drop)
            {
                if(Get(p)!=expected)return false;cells.Remove(p);
                if(expected==BlockId.Log||expected==BlockId.Leaves)sim.SupportRemoved(this,p);
                if(drop&&expected==BlockId.Log)logs++;return true;
            }
        }
    }
}
