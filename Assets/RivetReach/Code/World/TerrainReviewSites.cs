using System;

namespace RivetReach
{
    // Verification helpers discover generated sites; ordinary sessions never use these scans.
    public static class TerrainReviewSites
    {
        public static BlockPos Biome(TerrainGenerator generator,BiomeId biome)
        {
            BlockPos result=default;double best=double.MaxValue;
            for(int z=-768;z<=768;z+=16)for(int x=-768;x<=768;x+=16)
            {
                var c=generator.Column(x,z);
                if(c.Biome!=biome||biome==BiomeId.Alpine&&c.Surface!=BlockId.Snow)continue;
                // A standing review should look across the landscape, not into a steep
                // adjacent voxel wall. This only selects a camera site; it edits no terrain.
                bool cliff=false;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)if(generator.Height(x+dx,z+dz)>c.Height+1)cliff=true;
                if(cliff)continue;
                double score=x*x+z*z;
                for(int dz=-32;dz<=32;dz+=32)for(int dx=-32;dx<=32;dx+=32)if(generator.Biome(x+dx,z+dz)!=biome)score+=100000;
                var p=new BlockPos(x,c.Height,z);
                if(score>=best||generator.GroundAt(p)==0)continue;
                best=score;result=p;
            }
            if(best==double.MaxValue)throw new InvalidOperationException("No review site for "+biome);return result;
        }
        public static BlockPos Cave(TerrainGenerator generator)
        {
            for(int cy=-4;cy>=-7;cy--)for(int cz=-1;cz<=1;cz++)for(int cx=-1;cx<=1;cx++)
            {
                var chunk=new ChunkPos(cx,cy,cz);var cells=generator.Generate(chunk);
                for(int z=3;z<24;z++)for(int y=2;y<27;y++)for(int x=3;x<29;x++)
                {
                    if(cells[ChunkMesher.Index(x,y-1,z)]==0)continue;
                    bool clear=true;
                    for(int dz=-1;dz<=6&&clear;dz++)for(int dy=0;dy<4&&clear;dy++)for(int dx=-1;dx<=1;dx++)
                        if(cells[ChunkMesher.Index(x+dx,y+dy,z+dz)]!=0){clear=false;break;}
                    if(clear)return chunk.Min.Offset(x,y,z);
                }
            }
            throw new InvalidOperationException("No supported deep cave review site");
        }
    }
}
