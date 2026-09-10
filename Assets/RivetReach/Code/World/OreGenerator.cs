using System;
using System.Collections.Generic;

namespace RivetReach
{
    // Immutable generation data: changes to these defaults require a terrain version bump.
    public readonly struct OreBand
    {
        public readonly byte Block;
        public readonly int MinY,MaxY,PeakY,Chance,Radius;
        public OreBand(byte block,int minY,int maxY,int peakY,int chance,int radius)
        {Block=block;MinY=minY;MaxY=maxY;PeakY=peakY;Chance=chance;Radius=radius;}
    }

    public static class OreGenerator
    {
        public const int Spacing=16,MaxRadius=5;
        public static readonly IReadOnlyList<OreBand> Bands=Array.AsReadOnly(new[]{
            new OreBand(BlockId.CoalOre,0,80,40,60,5),
            new OreBand(BlockId.CopperOre,-48,64,16,55,4),
            new OreBand(BlockId.IronOre,-160,48,-48,60,4),
            new OreBand(BlockId.GoldOre,-240,-64,-160,28,3),
            new OreBand(BlockId.DiamondOre,-255,-160,-224,16,2),
            new OreBand(IndustryId.AzureOre,-96,8,-40,38,3)});

        public readonly struct Vein
        {
            public readonly OreBand Band;
            public readonly BlockPos Centre;
            public readonly int XRadius,YRadius,ZRadius;
            readonly int seed;
            public Vein(OreBand band,BlockPos centre,uint hash,int seed)
            {
                Band=band;Centre=centre;this.seed=seed;
                XRadius=band.Radius;YRadius=Math.Max(1,band.Radius-2);ZRadius=Math.Max(2,band.Radius-1);
                if((hash&1)!=0){XRadius=ZRadius;ZRadius=band.Radius;}
            }
            public bool Contains(BlockPos p)
            {
                if(p.Y<Band.MinY||p.Y>Band.MaxY)return false;
                double x=(p.X-Centre.X)/(double)XRadius,y=(p.Y-Centre.Y)/(double)YRadius,z=(p.Z-Centre.Z)/(double)ZRadius;
                double d=x*x+y*y+z*z;
                // Keep a solid core; seeded surface chips avoid identical smooth ellipsoids.
                return d<=.72||(d<=1&&TerrainGenerator.Hash(p.X,p.Y,p.Z,seed^Band.Block)%100>=28);
            }
        }

        public static IEnumerable<Vein> Veins(int seed,BlockPos min,BlockPos max)
        {
            foreach(var band in Bands)
            {
                if(max.Y<band.MinY||min.Y>band.MaxY)continue;
                for(long z=BlockPos.FloorDiv(min.Z-MaxRadius,Spacing);z<=BlockPos.FloorDiv(max.Z+MaxRadius,Spacing);z++)
                for(long y=BlockPos.FloorDiv(Math.Max(min.Y-MaxRadius,band.MinY),Spacing);y<=BlockPos.FloorDiv(Math.Min(max.Y+MaxRadius,band.MaxY),Spacing);y++)
                for(long x=BlockPos.FloorDiv(min.X-MaxRadius,Spacing);x<=BlockPos.FloorDiv(max.X+MaxRadius,Spacing);x++)
                {
                    uint h=TerrainGenerator.Hash(x,y,z,seed^(band.Block*7919));
                    var centre=new BlockPos(x*Spacing+(h>>8)%Spacing,(int)(y*Spacing+(h>>16)%Spacing),z*Spacing+(h>>24)%Spacing);
                    if(centre.Y<band.MinY||centre.Y>band.MaxY)continue;
                    double side=centre.Y<band.PeakY?band.PeakY-band.MinY:band.MaxY-band.PeakY;
                    double weight=1-Math.Abs(centre.Y-band.PeakY)/side;
                    if(h%10000>=band.Chance*100*(.35+.65*weight))continue;
                    yield return new Vein(band,centre,h,seed);
                }
            }
        }

        // Caller supplies stone only. Cave air, soil, trees and bedrock never become ore.
        public static byte At(int seed,BlockPos p)
        {
            foreach(var vein in Veins(seed,p,p))if(vein.Contains(p))return vein.Band.Block;
            return BlockId.Stone;
        }
        public static void Stamp(int seed,ChunkPos chunk,byte[] cells)
        {
            var min=chunk.Min;
            foreach(var vein in Veins(seed,min.Offset(-1,-1,-1),min.Offset(32,32,32)))
            for(int z=(int)Math.Max(-1,vein.Centre.Z-min.Z-vein.ZRadius);z<=Math.Min(32,vein.Centre.Z-min.Z+vein.ZRadius);z++)
            for(int y=Math.Max(-1,vein.Centre.Y-min.Y-vein.YRadius);y<=Math.Min(32,vein.Centre.Y-min.Y+vein.YRadius);y++)
            for(int x=(int)Math.Max(-1,vein.Centre.X-min.X-vein.XRadius);x<=Math.Min(32,vein.Centre.X-min.X+vein.XRadius);x++)
            {
                int i=ChunkMesher.Index(x,y,z);
                if(cells[i]==BlockId.Stone&&vein.Contains(min.Offset(x,y,z)))cells[i]=vein.Band.Block;
            }
        }
    }
}
