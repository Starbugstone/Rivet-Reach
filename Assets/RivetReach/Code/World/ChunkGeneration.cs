using System.Collections.Generic;
using System.Linq;

namespace RivetReach
{
    public sealed partial class VoxelWorld
    {
        readonly Dictionary<ChunkPos,string> generatedVersions=new Dictionary<ChunkPos,string>();
        readonly Dictionary<(long x,long z),string> legacyColumns=new Dictionary<(long,long),string>();
        readonly Dictionary<string,TerrainGenerator> generators=new Dictionary<string,TerrainGenerator>();
        public int RecordedChunks=>generatedVersions.Count;
        public string GenerationAt(ChunkPos p)=>generatedVersions.TryGetValue(p,out var version)?version:
            legacyColumns.TryGetValue((p.X,p.Z),out version)?version:Generator.GenerationVersion;
        TerrainGenerator GeneratorFor(ChunkPos p)
        {
            string version=GenerationAt(p);
            if(version==Generator.GenerationVersion)return Generator;
            if(!generators.TryGetValue(version,out var generator))generators[version]=generator=new TerrainGenerator(Generator.Seed,version);
            return generator;
        }
        // Halo samples count as generated: a later release cannot change a previously sampled boundary.
        TerrainGenerator[] PinGeneration(ChunkPos p)
        {
            var result=new TerrainGenerator[27];int i=0;
            for(int z=-1;z<=1;z++)for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
            {var key=p.Offset(x,y,z);var generator=GeneratorFor(key);generatedVersions.TryAdd(key,generator.GenerationVersion);result[i++]=generator;}
            return result;
        }
        internal void ProtectLegacy(BlockPos anchor,int radius=1)
        {
            var p=anchor.Chunk;
            for(int z=-radius;z<=radius;z++)for(int x=-radius;x<=radius;x++)legacyColumns.TryAdd((p.X+x,p.Z+z),Generator.GenerationVersion);
        }
        internal void CompleteLegacyGeneration(BlockPos player)
        {
            ProtectLegacy(player,ViewDistance+1);ProtectLegacy(new BlockPos(0,0,0),ViewDistance+1);
            foreach(var p in edits.Keys)ProtectLegacy(p.Min);
            Generator=new TerrainGenerator(Generator.Seed);skyHeights.Clear();
        }
        void WriteGeneration(SaveWriter w)
        {
            w.Write(generatedVersions.Count);
            foreach(var p in generatedVersions.OrderBy(p=>p.Key.X).ThenBy(p=>p.Key.Y).ThenBy(p=>p.Key.Z)){w.Pos(p.Key.Min);w.Write(p.Value);}
            w.Write(legacyColumns.Count);
            foreach(var p in legacyColumns.OrderBy(p=>p.Key.x).ThenBy(p=>p.Key.z)){w.Write(p.Key.x);w.Write(p.Key.z);w.Write(p.Value);}
        }
        void ReadGeneration(SaveReader r)
        {
            int n=r.Count();for(int i=0;i<n;i++)
            {var p=r.Pos();string version=r.Text();SaveReader.Require(p.Equals(p.Chunk.Min)&&TerrainGenerator.Supported(version)&&generatedVersions.TryAdd(p.Chunk,version),"Invalid generated chunk record.");}
            n=r.Count();for(int i=0;i<n;i++)
            {long x=r.ReadInt64(),z=r.ReadInt64();string version=r.Text();SaveReader.Require(System.Math.Abs(x)<=TerrainGenerator.HorizontalLimit/32+64&&System.Math.Abs(z)<=TerrainGenerator.HorizontalLimit/32+64&&TerrainGenerator.Supported(version)&&legacyColumns.TryAdd((x,z),version),"Invalid legacy generation region.");}
        }
    }
}
