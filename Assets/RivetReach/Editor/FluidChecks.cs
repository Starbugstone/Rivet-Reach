using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class FluidChecks
    {
        sealed class TestWorld:IFluidWorld
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public readonly HashSet<ChunkPos> Unloaded=new HashSet<ChunkPos>();
            public FluidSimulation Simulation;
            public int Floor=0;
            public bool Reject;
            public byte Get(BlockPos p)=>Cells.TryGetValue(p,out byte id)?id:p.Y<=Floor?BlockId.Stone:(byte)0;
            public bool TryRead(BlockPos p,out byte id){id=Get(p);return !Unloaded.Contains(p.Chunk);}
            public bool ChangeFluid(BlockPos p,byte expected,byte replacement)
            {if(Reject||!TryRead(p,out byte cell)||cell!=expected)return false;Cells[p]=replacement;Simulation.Changed(this,p);return true;}
            public void Put(BlockPos p,byte id){Cells[p]=id;Simulation.Changed(this,p);}
            public void Run(int ticks=500){for(int i=0;i<ticks;i++)Simulation.Step(this);}
        }
        [MenuItem("Rivet Reach/Validate fluids")]
        public static void Run()
        {
            var lines=new List<string>();int checks=0;
            void Check(bool valid,string message){if(!valid)throw new Exception("Fluid check: "+message);checks++;lines.Add("PASS "+message);}
            TestWorld World(FluidRegistry registry=null)=>new TestWorld{Simulation=new FluidSimulation(registry??Fluids.Registry)};
            var water=Fluids.Water;var origin=new BlockPos(0,1,0);var w=World();var watch=Stopwatch.StartNew();
            w.Put(origin,water.Source);w.Run();
            Check(w.Get(origin.Offset(7,0,0))==water.Flow(7)&&w.Get(origin.Offset(8,0,0))==0,"Flat source spreads exactly seven cells");
            Check(w.Get(origin.Offset(3,0,4))==water.Flow(7)&&w.Get(origin.Offset(4,0,4))==0,"Horizontal attenuation uses path length");
            Check(w.Simulation.Pending==0,"Settled water sleeps");
            w.Put(origin,0);w.Run();Check(w.Cells.Values.All(id=>!Fluids.IsFluid(id)),"Removing a source drains all dependent flow");
            w=World();w.Put(origin,water.Source);w.Put(origin.Offset(2,0,0),water.Source);w.Run();
            Check(w.Get(origin.Offset(1,0,0))==water.Source,"Two horizontal sources renew supported water");
            w.Put(origin.Offset(1,0,0),0);w.Run();Check(w.Get(origin.Offset(1,0,0))==water.Source,"Collected renewable source refills");
            w=World();w.Put(origin,BlockId.Torch);w.Simulation.Wake(origin,1);w.Run();
            Check(w.Get(origin)==BlockId.Torch,"Dry scheduled torch is preserved");
            w.Put(origin.Offset(0,1,0),water.Source);w.Run(6);
            Check(w.Get(origin)==water.Falling,"Falling water displaces a torch");
            w=World();w.Put(origin.Offset(1,0,0),BlockId.Torch);w.Put(origin,water.Source);w.Run();
            Check(w.Get(origin.Offset(1,0,0))==water.Flow(1),"Horizontal water flows through a torch");
            var testFluid=new FluidDefinition("test:nonrenewing",120,97,3,9,false,6,.4f,.8f,.3f,.1f);
            var registry=new FluidRegistry(water,testFluid);w=World(registry);
            w.Put(origin,testFluid.Source);w.Put(origin.Offset(2,0,0),testFluid.Source);w.Run();
            Check(w.Get(origin.Offset(1,0,0))==testFluid.Flow(1),"Second fluid disables renewal independently");
            Check(w.Get(origin.Offset(-3,0,0))==testFluid.Flow(3)&&w.Get(origin.Offset(-4,0,0))==0,"Second fluid owns its horizontal reach");
            w=World(registry);w.Put(origin,testFluid.Source);w.Run(8);Check(w.Get(origin.Offset(1,0,0))==0,"Second fluid respects a slower tick delay");
            w.Run();Check(w.Get(origin.Offset(1,0,0))!=0,"Slower fluid eventually propagates");
            w=World();w.Floor=-5;w.Put(origin,water.Source);w.Put(origin.Offset(2,0,0),water.Source);w.Run();
            Check(w.Get(origin.Offset(1,0,0))!=water.Source,"Unsupported adjacent sources do not renew in midair");
            Check(w.Get(origin.Offset(0,-4,0))==water.Falling,"Water descends vertically");
            Check(w.Get(origin.Offset(-7,-5,0))==water.Flow(7),"Falling column resets horizontal reach on landing");
            w=World();w.Put(origin.Offset(2,-1,0),0);w.Floor=0;w.Put(origin,water.Source);w.Run(20);
            Check(w.Get(origin.Offset(1,0,0))!=0&&w.Get(origin.Offset(-1,0,0))==0,"Nearby drop attracts initial flow");
            w=World();for(int z=-8;z<=8;z++)w.Put(new BlockPos(1,1,z),BlockId.Stone);w.Put(origin,water.Source);w.Run();
            Check(w.Get(origin.Offset(2,0,0))==0,"Solid wall blocks flow");
            w.Put(new BlockPos(1,1,0),0);w.Run();Check(w.Get(origin.Offset(2,0,0))!=0,"Opening a dam wakes neighbouring water");
            w.Put(new BlockPos(1,1,0),BlockId.Stone);w.Run();Check(w.Get(origin.Offset(2,0,0))==0,"Rebuilding a dam drains disconnected flow");
            w=World();var seam=new BlockPos(31,1,0);var unloaded=new ChunkPos(1,0,0);w.Unloaded.Add(unloaded);w.Put(seam,water.Source);w.Run();
            Check(w.Get(seam.Offset(1,0,0))==0,"Unready chunks are closed to flow");
            w.Unloaded.Remove(unloaded);w.Simulation.Ready(unloaded);w.Run();Check(w.Get(seam.Offset(1,0,0))!=0,"Chunk arrival resumes blocked flow");
            w=World();w.Put(new BlockPos(-32,1,-32),water.Source);w.Run();Check(w.Get(new BlockPos(-33,1,-32))==water.Flow(1),"Flow crosses negative-coordinate chunk seams");
            var bag=new ItemContainer(1,id=>1);bag.Add(Fluids.EmptyBucket,1);w=World();w.Put(origin,water.Source);
            Check(BucketTransfer.TryUse(w,bag,0,origin,Fluids.Registry)&&bag.Slots[0].Id==Fluids.WaterBucket&&w.Get(origin)==0,"Bucket collection replaces its slot in a full inventory");
            Check(BucketTransfer.TryUse(w,bag,0,origin,Fluids.Registry)&&bag.Slots[0].Id==Fluids.EmptyBucket&&w.Get(origin)==water.Source,"Bucket placement returns one empty bucket");
            w.Run();Check(!BucketTransfer.TryUse(w,bag,0,origin.Offset(1,0,0),Fluids.Registry)&&bag.Slots[0].Id==Fluids.EmptyBucket,"Buckets reject flowing cells without item loss");
            w.Reject=true;Check(!BucketTransfer.TryUse(w,bag,0,origin,Fluids.Registry)&&bag.Slots[0].Id==Fluids.EmptyBucket&&w.Get(origin)==water.Source,"Rejected world mutation preserves bucket and source");
            bag.Take(0,1);bag.Add(Fluids.WaterBucket,1);w.Reject=false;
            Check(!BucketTransfer.TryUse(w,bag,0,new BlockPos(0,0,0),Fluids.Registry)&&bag.Slots[0].Id==Fluids.WaterBucket,"Cannot empty a bucket inside solid terrain");
            var cells=new byte[34*34*34];cells[ChunkMesher.Index(0,0,0)]=water.Source;
            cells[ChunkMesher.Index(1,0,0)]=water.Source;
            var mesh=ChunkMesher.Build(default,0,cells);
            Check(mesh.Vertices.Length==0&&mesh.FluidMesh.Indices.Length==60,"Fluid mesh excludes shared faces and opaque terrain mesh");
            Array.Fill(cells,water.Source);var ocean=FluidMesher.Build(cells);
            Check(ocean.ActiveCells.Length==0&&ocean.Indices.Length==0,"Settled ocean interiors and shared source seams create no activation work or faces");
            Check(!BlockId.Solid(water.Source)&&!BlockId.Placeable(water.Source)&&!BlockId.Mineable(water.Source,ToolCapability.Pickaxe),"Fluid cells are neither solid nor mineable inventory blocks");
            int seas=0,rivers=0;
            foreach(int seed in new[]{246813,17,-99})
            {
                var g=new TerrainGenerator(seed);
                Check(g.Column(0,0).WaterLevel==int.MinValue,"Dry supported spawn, seed "+seed);
                foreach(var biome in new[]{BiomeId.Sea,BiomeId.River})
                {
                    var site=TerrainReviewSites.Biome(g,biome);var column=g.Column(site.X,site.Z);
                    Check(column.Biome==biome&&column.WaterLevel==TerrainProfile.SeaLevel,"Generated "+biome+" has shared sea datum, seed "+seed);
                    var surface=new BlockPos(site.X,TerrainProfile.SeaLevel,site.Z);var generated=g.Generate(surface.Chunk);
                    Check(g.At(surface)==water.Source&&generated[ChunkMesher.Index(surface.Index%32,surface.Index/32%32,surface.Index/1024)]==water.Source,"Fluid point and chunk reads agree for "+biome);
                    Check(g.At(new BlockPos(site.X,column.Height,site.Z))==BlockId.Sand&&g.At(new BlockPos(site.X,TerrainGenerator.MinY,site.Z))==BlockId.Bedrock,"Water bed and protected bedrock retained for "+biome);
                    if(biome==BiomeId.Sea)seas++;else rivers++;
                }
            }
            lines.Add($"Checks: {checks}; generated sea sites: {seas}; river sites: {rivers}; elapsed ms: {watch.Elapsed.TotalMilliseconds:F1}");
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/fluid-checks.txt",lines);UnityEngine.Debug.Log(string.Join("\n",lines));
        }
    }
}
