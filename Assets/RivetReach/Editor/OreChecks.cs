using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class OreChecks
    {
        public static void Run(Action<bool,string> check)
        {
            var registry=ItemRegistry.Load();
            foreach(var band in OreGenerator.Bands)
            {
                byte drop=registry.FistDrop(band.Block);
                check(BlockId.RawMaterial(drop)&&!BlockId.Placeable(drop),"Ore yields a distinct nonplaceable raw resource");
                foreach(var tool in new[]{ToolCapability.None,ToolCapability.Axe,ToolCapability.Blade})
                    check(!BlockId.Mineable(band.Block,tool)&&float.IsPositiveInfinity(registry.MiningSeconds(band.Block,tool)),"Wrong tool cannot accumulate ore mining progress");
                check(BlockId.Mineable(band.Block,ToolCapability.Pickaxe)&&registry.MiningSeconds(band.Block,ToolCapability.Pickaxe)>0,"Starter pickaxe can mine every current ore");
            }
            foreach(ToolCapability tool in Enum.GetValues(typeof(ToolCapability)))check(!BlockId.Mineable(BlockId.Bedrock,tool),"No tool can mine bedrock");
            var counts=new long[5];var sums=new long[5];int chunks=0;
            var timings=new List<double>();var report=new StringBuilder();
            foreach(int seed in new[]{246813,0,-777})
            {
                var generator=new TerrainGenerator(seed);var localCounts=new long[5];
                for(int cz=-2;cz<2;cz++)for(int cx=-2;cx<2;cx++)for(int cy=-8;cy<=2;cy++)
                {
                    var chunk=new ChunkPos(cx,cy,cz);var timer=Stopwatch.StartNew();var cells=generator.Generate(chunk);timings.Add(timer.Elapsed.TotalMilliseconds);chunks++;
                    bool valid=true,roof=true;var min=chunk.Min;
                    for(int z=0;z<32;z++)for(int x=0;x<32;x++)
                    {
                        int h=generator.Height(min.X+x,min.Z+z);
                        for(int y=0;y<32;y++)
                        {
                            byte id=cells[ChunkMesher.Index(x,y,z)];int wy=min.Y+y;
                            if(wy==TerrainGenerator.MinY)valid&=id==BlockId.Bedrock;
                            if(!BlockId.Ore(id))continue;
                            int index=Enumerable.Range(0,5).First(i=>OreGenerator.Bands[i].Block==id);var band=OreGenerator.Bands[index];
                            counts[index]++;localCounts[index]++;sums[index]+=wy;
                            valid&=wy>=band.MinY&&wy<=band.MaxY&&wy>TerrainGenerator.MinY;
                            roof&=wy<h-3;
                            if(wy>10&&wy<h-2)roof&=!(generator.Noise(min.X+x,wy,min.Z+z,14)>.71&&generator.Noise(min.X+x,wy,min.Z+z,40)>.42);
                        }
                    }
                    check(valid&&roof,"Generated ore respects height bands, bedrock, soil and cave air");
                }
                check(localCounts.All(n=>n>0),"Every ore exists within the sampled 128-block square for seed "+seed);
                // Fully compare neighbouring halos on all three axes, including the world floor.
                foreach(int y in new[]{1,0,-3,-6,-8})
                {
                    var chunk=new ChunkPos(-1,y,-1);var a=generator.Generate(chunk);
                    for(int axis=0;axis<3;axis++)
                    {
                        var b=generator.Generate(chunk.Offset(axis==0?1:0,axis==1?1:0,axis==2?1:0));bool equal=true;
                        for(int v=-1;v<=32;v++)for(int u=-1;u<=32;u++)for(int face=31;face<=32;face++)
                        {
                            int x=axis==0?face:u,yy=axis==1?face:axis==0?u:v,z=axis==2?face:v;
                            equal&=a[ChunkMesher.Index(x,yy,z)]==b[ChunkMesher.Index(x-(axis==0?32:0),yy-(axis==1?32:0),z-(axis==2?32:0))];
                        }
                        check(equal,"Ore/bedrock halos agree across axis "+axis+" at chunk Y "+y);
                    }
                }
            }
            var g=new TerrainGenerator(246813);
            var probes=new[]{new ChunkPos(-1,-8,0),new ChunkPos(0,-6,-1),new ChunkPos(1,-2,1),new ChunkPos(-1,1,0),new ChunkPos(31249999,-7,-31250000)};
            var expected=probes.Select(g.Generate).ToArray();
            Parallel.For(0,probes.Length,i=>{if(!expected[i].SequenceEqual(new TerrainGenerator(246813).Generate(probes[i])))throw new Exception("Concurrent ore generation differs");});
            check(true,"Concurrent workers reproduce isolated generation without discovery-order state");
            for(int i=probes.Length-1;i>=0;i--)
            {
                var chunk=probes[i];bool same=true;
                for(int z=-1;z<=32;z+=3)for(int y=-1;y<=32;y+=3)for(int x=-1;x<=32;x+=3)
                    same&=g.At(chunk.Min.Offset(x,y,z))==expected[i][ChunkMesher.Index(x,y,z)];
                check(same,"Point queries match stamped ore/bedrock at signed and distant coordinates");
            }
            check(!expected[1].SequenceEqual(new TerrainGenerator(246814).Generate(probes[1])),"Different seeds alter deep ore distribution");
            foreach(long x in new[]{-TerrainGenerator.HorizontalLimit,0,TerrainGenerator.HorizontalLimit})
            foreach(int y in new[]{TerrainGenerator.MinY-1,TerrainGenerator.MinY})
                check(g.At(new BlockPos(x,y,-x))==BlockId.Bedrock,"Bedrock continuously closes the supported world base");
            var tiles=Resources.Load<Texture2DArray>("Materials/BlockTiles");
            foreach(var item in registry.items)
                check(BlockId.Tile(item.runtimeId,0,1)<tiles.depth,"Every item texture references an imported layer");
            for(int i=0;i<5;i++)report.AppendLine(registry.Get(OreGenerator.Bands[i].Block).displayName+": "+counts[i]+" cells; mean Y "+(sums[i]/(double)counts[i]).ToString("F2"));
            check(sums[0]/(double)counts[0]>sums[1]/(double)counts[1]&&sums[1]/(double)counts[1]>sums[2]/(double)counts[2]&&sums[2]/(double)counts[2]>sums[3]/(double)counts[3]&&sums[3]/(double)counts[3]>sums[4]/(double)counts[4],"Measured ore depths descend from coal through diamond");
            timings.Sort();report.AppendLine(chunks+" generated chunks; generation median "+timings[timings.Count/2].ToString("F3")+" ms; p95 "+timings[(int)(timings.Count*.95)].ToString("F3")+" ms; max "+timings.Last().ToString("F3")+" ms (Editor, generation only).");
            File.WriteAllText("Logs/ore-checks.txt",report.ToString());
        }
    }
}
