using System;

namespace RivetReach
{
    // World addresses, independent of death position, render origin and chunk residency.
    // A future saved bed binding can select a different policy before this fallback.
    public static class RespawnPlacement
    {
        public const int Radius=100;
        public static bool TryFind(Func<BlockPos,byte> get,out BlockPos spawn)
        {
            spawn=default;
            for(int radius=0;radius<=Radius;radius++)
            for(int z=-radius;z<=radius;z++)for(int x=-radius;x<=radius;x++)
            {
                if(Math.Max(Math.Abs(x),Math.Abs(z))!=radius)continue;
                // Check the actual player centre, including the half-block placement offset.
                if((x+.5)*(x+.5)+(z+.5)*(z+.5)>Radius*Radius)continue;
                // Start above all legal support, including player-built roofs. Carry air
                // clearance down the column to avoid repeated world lookups.
                int clear=0;
                for(int y=TerrainGenerator.MaxY;y>=TerrainGenerator.MinY;y--)
                {
                    var cell=new BlockPos(x,y,z);byte id=get(cell);
                    if(BlockId.Solid(id))
                    {
                        if(clear>=2){spawn=cell.Offset(0,1,0);return true;}
                        // A flooded/blocked surface rejects the column. Do not choose
                        // a cave below it when an adjacent surface can be used instead.
                        break;
                    }
                    clear=!BlockId.Solid(id)&&!Fluids.IsFluid(id)?clear+1:0;
                }
            }
            return false;
        }
    }
}
