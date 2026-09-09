using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RivetReach.Editor
{
    public static class TerrainGenerationChecks
    {
        public static void Run(Action<bool,string> check)
        {
            var report=new StringBuilder();int overallMin=int.MaxValue,overallMax=int.MinValue,entrances=0;
            foreach(int seed in new[]{246813,777,-917,int.MinValue})
            {
                var g=new TerrainGenerator(seed);var biomes=new int[5];int low=999,high=-999;
                for(int z=-768;z<=768;z+=16)for(int x=-768;x<=768;x+=16)
                {
                    var column=g.Column(x,z);biomes[(int)column.Biome]++;low=Math.Min(low,column.Height);high=Math.Max(high,column.Height);
                    check(Math.Abs(column.Height-g.Height(x+1,z))<=12,"Adjacent terrain columns avoid discontinuous biome walls");
                    if(g.GroundAt(new BlockPos(x,column.Height,z))==0)entrances++;
                }
                check(biomes.All(n=>n>0),"Sample contains every biome for seed "+seed);
                check(high-low>100,"Sample contains lowlands and high relief for seed "+seed);
                overallMin=Math.Min(low,overallMin);overallMax=Math.Max(high,overallMax);
                var spawn=new BlockPos(0,g.Height(0,0),0);
                check(g.At(spawn)==BlockId.Grass&&g.At(spawn.Offset(0,1,0))==0&&g.At(spawn.Offset(0,2,0))==0,"Spawn has temperate solid support and clear headroom");
                check(g.Trees(-48,-48,48,48).Any(),"Wood remains available near the initial spawn");
                report.AppendLine("Seed "+seed+": heights "+low+".."+high+"; grassland/forest/dunes/badlands/alpine samples "+string.Join(", ",biomes));
            }
            check(entrances>10,"Natural surface entrances exist across the sampled worlds");
            var generator=new TerrainGenerator(246813);var pages=new List<ChunkPos>();
            int potatoes=0;var plants=new List<BlockPos>();
            for(int z=-64;z<=64;z++)for(int x=-64;x<=64;x++)
            {
                var p=new BlockPos(x,generator.Height(x,z)+1,z);
                if(generator.At(p)!=BlockId.MaturePotatoPlant)continue;potatoes++;plants.Add(p);
                check(generator.At(p.Offset(0,-1,0))==BlockId.Grass&&generator.At(p.Offset(0,1,0))==0&&
                    (Math.Abs(x)>8||Math.Abs(z)>8),"Wild potatoes have grass support, clearance and spawn separation");
                if(!pages.Contains(p.Chunk))pages.Add(p.Chunk);
            }
            check(potatoes>0&&potatoes<60,"Sparse wild food exists near spawn for the survival integration");
            report.AppendLine("Wild mature potato plants in 129-square spawn sample: "+potatoes);
            foreach(var band in OreGenerator.Bands)
            {
                bool found=false;
                foreach(var vein in OreGenerator.Veins(246813,new BlockPos(-64,band.MinY,-64),new BlockPos(64,band.MaxY,64)))
                {
                    if(vein.Band.Block!=band.Block||generator.At(vein.Centre)!=band.Block)continue;
                    pages.Add(vein.Centre.Chunk);found=true;break;
                }
                check(found,"Natural ore remains discoverable after carving: "+band.Block);
            }
            foreach(var biome in (BiomeId[])Enum.GetValues(typeof(BiomeId)))
            {
                var p=TerrainReviewSites.Biome(generator,biome);pages.Add(p.Chunk);
                check(generator.Biome(p.X,p.Z)==biome,"Review site represents "+biome);
                report.AppendLine(TerrainProfile.Name(biome)+" review site: "+p);
                foreach(var tree in generator.Trees(p.X-32,p.Z-32,p.X+32,p.Z+32))
                    check(generator.At(tree.Root.Offset(0,-1,0))==BlockId.Grass,"Trees never root in cave air, dunes or clay");
            }
            pages.AddRange(new[]{new ChunkPos(-1,-8,-1),new ChunkPos(0,-5,0),new ChunkPos(-3,-3,2),new ChunkPos(31249999,-2,-31250000)});
            var times=new List<double>();int caveCells=0,oreCells=0;var expected=new List<byte[]>();
            foreach(var chunk in pages)
            {
                var timer=Stopwatch.StartNew();var cells=generator.Generate(chunk,out int low,out int high);times.Add(timer.Elapsed.TotalMilliseconds);expected.Add(cells);
                bool coherent=true,range=true,bedrock=true,oreHosts=true;
                for(int z=-1;z<=32;z++)for(int x=-1;x<=32;x++)
                {
                    var p=chunk.Min.Offset(x,0,z);int h=generator.Height(p.X,p.Z);range&=h>=low&&h+TerrainGenerator.MaxTreeHeight<=high;
                    for(int y=-1;y<=32;y++)
                    {
                        p=chunk.Min.Offset(x,y,z);byte id=cells[ChunkMesher.Index(x,y,z)];
                        if(p.Y<=TerrainGenerator.MinY)bedrock&=id==BlockId.Bedrock;
                        if(id==0&&p.Y<h-12&&p.Y>TerrainGenerator.MinY)caveCells++;
                        if(BlockId.Ore(id))
                        {
                            oreCells++;var band=OreGenerator.Bands.First(b=>b.Block==id);
                            oreHosts&=p.Y>=band.MinY&&p.Y<=band.MaxY&&generator.GroundAt(p)==BlockId.Stone;
                        }
                        if(x%5==0&&y%5==0&&z%5==0)coherent&=generator.At(p)==id;
                    }
                }
                check(coherent&&range&&bedrock&&oreHosts,"Point samples, surface extrema, protected bedrock and ore host/bands agree with generated page");
                foreach(var axis in new[]{(1,0,0),(0,1,0),(0,0,1)})
                {
                    var neighbour=generator.Generate(chunk.Offset(axis.Item1,axis.Item2,axis.Item3));bool same=true;
                    for(int u=-1;u<=32;u++)for(int v=-1;v<=32;v++)for(int edge=31;edge<=32;edge++)
                    {
                        int x=axis.Item1!=0?edge:u,y=axis.Item2!=0?edge:axis.Item1!=0?u:v,z=axis.Item3!=0?edge:v;
                        same&=cells[ChunkMesher.Index(x,y,z)]==neighbour[ChunkMesher.Index(x-axis.Item1*32,y-axis.Item2*32,z-axis.Item3*32)];
                    }
                    check(same,"All shared halo cells agree across varied terrain/cave seams");
                }
            }
            check(caveCells>1000&&oreCells>0,"Deep caves and finite ore coexist in generated pages");
            foreach(var p in plants)
            {
                var min=p.Chunk.Min;
                check(expected[pages.IndexOf(p.Chunk)][ChunkMesher.Index((int)(p.X-min.X),p.Y-min.Y,(int)(p.Z-min.Z))]==BlockId.MaturePotatoPlant,
                    "Every discovered wild plant matches the worker page");
            }
            Parallel.For(0,pages.Count,i=>{if(!expected[i].SequenceEqual(new TerrainGenerator(246813).Generate(pages[i])))throw new Exception("Concurrent terrain differs");});
            for(int i=pages.Count-1;i>=0;i--)check(expected[i].SequenceEqual(generator.Generate(pages[i])),"Reordered generation and bounded-cache eviction preserve pages");
            var room=TerrainReviewSites.Cave(generator);
            check(generator.At(room)==0&&generator.At(room.Offset(0,1,0))==0&&generator.At(room.Offset(0,-1,0))!=0&&room.Y<0,"A supported deep cavern fits a player");
            report.AppendLine("Deep cave review site: "+room);
            ConnectedCaves(generator,check,report);
            times.Sort();report.AppendLine("Page generation ms: median "+times[times.Count/2].ToString("F3")+", max "+times.Last().ToString("F3")+". Includes caves/ores/trees, excludes meshing.");
            report.AppendLine("All-seed sampled height range "+overallMin+".."+overallMax+"; surface entrance samples "+entrances+".");
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/terrain-generation-checks.txt",report.ToString());
        }
        static void ConnectedCaves(TerrainGenerator generator,Action<bool,string> check,StringBuilder report)
        {
            const int side=64;var air=new bool[side*side*side];
            for(int cz=0;cz<2;cz++)for(int cy=0;cy<2;cy++)for(int cx=0;cx<2;cx++)
            {
                var cells=generator.Generate(new ChunkPos(cx,cy-5,cz));
                for(int z=0;z<32;z++)for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                    air[x+cx*32+side*(y+cy*32+side*(z+cz*32))]=cells[ChunkMesher.Index(x,y,z)]==0;
            }
            var queue=new int[air.Length];int largest=0,span=0;
            for(int start=0;start<air.Length;start++)
            {
                if(!air[start])continue;int read=0,write=1,minY=side,maxY=0;queue[0]=start;air[start]=false;
                while(read<write)
                {
                    int i=queue[read++],x=i%side,y=i/side%side,z=i/(side*side);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);
                    void Add(int n){if(air[n]){air[n]=false;queue[write++]=n;}}
                    if(x>0)Add(i-1);if(x<side-1)Add(i+1);if(y>0)Add(i-side);if(y<side-1)Add(i+side);if(z>0)Add(i-side*side);if(z<side-1)Add(i+side*side);
                }
                largest=Math.Max(largest,write);span=Math.Max(span,maxY-minY);
            }
            check(largest>3000&&span>=48,"Face-connected deep cave network spans several vertical chunks");
            report.AppendLine("64-cubed deep sample: largest connected air volume "+largest+"; maximum component vertical span "+span+" blocks.");
        }
    }
}
