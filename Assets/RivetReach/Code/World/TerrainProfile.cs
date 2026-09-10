using System;

namespace RivetReach
{
    public enum BiomeId { Grassland,Forest,Desert,Badlands,Alpine,Sea,River }

    public readonly struct TerrainColumn
    {
        public readonly int Height,SoilDepth,WaterLevel;
        public readonly BiomeId Biome;
        public readonly byte Surface,Subsoil;
        public readonly double Entrance;
        public TerrainColumn(int height,BiomeId biome,byte surface,byte subsoil,int soilDepth,double entrance,int waterLevel=int.MinValue)
        {WaterLevel=waterLevel;Height=height;Biome=biome;Surface=surface;Subsoil=subsoil;SoilDepth=soilDepth;Entrance=entrance;}
    }

    // Immutable initial surface-world profile. Changing these defaults bumps the generator version.
    public static class TerrainProfile
    {
        public const int BiomeSpacing=224,SeaLevel=32;
        public static string Name(BiomeId id)=>id==BiomeId.Sea?"Sea":id==BiomeId.River?"River":id==BiomeId.Grassland?"Grassland":id==BiomeId.Forest?"Forest":id==BiomeId.Desert?"Dunes":id==BiomeId.Badlands?"Badlands":"Alpine";
        public static int TreeChance(BiomeId id)=>id==BiomeId.Forest?68:id==BiomeId.Grassland?24:id==BiomeId.Alpine?12:0;
        public static TerrainColumn Sample(long x,long z,int seed)
        {
            double wx=x+(WorldNoise.Two(x,z,320,seed^1201)-.5)*64,wz=z+(WorldNoise.Two(x,z,320,seed^1213)-.5)*64;
            double rolling=WorldNoise.Two(wx,wz,180,seed^1223)*2-1,detail=WorldNoise.Two(wx,wz,48,seed^1229)*2-1;
            double ridge=1-Math.Abs(WorldNoise.Two(wx,wz,100,seed^1231)*2-1);
            double terrace=WorldNoise.Two(wx,wz,96,seed^1237)*5;
            terrace=Math.Floor(terrace)+WorldNoise.Ramp(.76,1,terrace-Math.Floor(terrace));
            double plains=44+rolling*14+detail*3;
            double total=0,height=0,best=-1;BiomeId dominant=BiomeId.Grassland;
            long gx=(long)Math.Floor(wx/BiomeSpacing),gz=(long)Math.Floor(wz/BiomeSpacing);
            for(long cz=gz-1;cz<=gz+1;cz++)for(long cx=gx-1;cx<=gx+1;cx++)
            {
                uint hash=TerrainGenerator.Hash(cx,937,cz,seed);
                // Every point is within its own cell's site radius: .7 * sqrt(2) < 1.
                // Wider jitter can leave holes in kernel coverage and create fallback cliffs.
                double sx=(cx+.3+(hash>>8&255)/255.0*.4)*BiomeSpacing,sz=(cz+.3+(hash>>16&255)/255.0*.4)*BiomeSpacing;
                double dx=(wx-sx)/BiomeSpacing,dz=(wz-sz)/BiomeSpacing,weight=1-dx*dx-dz*dz;
                if(weight<=0)continue;weight=weight*weight*weight;
                var biome=(BiomeId)(hash%5);
                double shaped=biome==BiomeId.Grassland?plains:biome==BiomeId.Forest?54+rolling*23+detail*6:
                    biome==BiomeId.Desert?40+rolling*10+(Math.Sin(wx*.065+wz*.024+detail*1.8)+1)*6:
                    biome==BiomeId.Badlands?42+terrace*16:64+ridge*ridge*118+rolling*24+detail*9;
                height+=shaped*weight;total+=weight;
                if(weight>best){best=weight;dominant=biome;}
            }
            // A smoothly blended temperate start, not a flat platform or an exploration-order repair.
            double distance=Math.Sqrt((double)x*x+(double)z*z),spawn=1-WorldNoise.Ramp(24,96,distance);
            height=WorldNoise.Blend(total>0?height/total:plains,plains,spawn);
            if(spawn>.5)dominant=BiomeId.Grassland;
            // Broad seeded basins and continuous meandering channels share a fixed datum.
            // Spawn exclusion blends the land before carving; banks end above the datum.
            double sea=WorldNoise.Two(x,z,720,seed^8101);
            double ocean=WorldNoise.Ramp(.51,.65,sea)*(1-spawn);
            height=WorldNoise.Blend(height,8+rolling*8,ocean);
            double river=Math.Abs(WorldNoise.Two(wx,wz,220,seed^8111)-.5);
            double uncutHeight=height;
            double riverBed=SeaLevel-4+Math.Max(0,river-.012)*600;
            height=WorldNoise.Blend(height,Math.Min(height,riverBed),1-spawn);
            int waterLevel=int.MinValue;
            if(height<SeaLevel){waterLevel=SeaLevel;dominant=riverBed<uncutHeight&&riverBed<SeaLevel?BiomeId.River:BiomeId.Sea;}
            int h=(int)Math.Floor(height);byte surface=BlockId.Grass,subsoil=BlockId.Dirt;int depth=3;
            if(dominant==BiomeId.Sea||dominant==BiomeId.River){surface=BlockId.Sand;subsoil=BlockId.Sandstone;depth=4;}
            if(dominant==BiomeId.Desert){surface=BlockId.Sand;subsoil=BlockId.Sandstone;depth=6;}
            if(dominant==BiomeId.Badlands){surface=subsoil=BlockId.RedClay;depth=12;}
            if(dominant==BiomeId.Alpine)
            {
                if(h>=112+(int)(detail*8)){surface=BlockId.Snow;subsoil=BlockId.Stone;depth=1;}
                else if(ridge>.8){surface=subsoil=BlockId.Stone;depth=0;}
            }
            return new TerrainColumn(h,dominant,surface,subsoil,depth,WorldNoise.Ramp(.56,.72,WorldNoise.Two(wx,wz,96,seed^1249)),waterLevel);
        }
    }
}
