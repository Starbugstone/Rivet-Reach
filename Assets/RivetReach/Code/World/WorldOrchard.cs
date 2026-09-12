using System;
using System.Collections.Generic;

namespace RivetReach
{
    public static class LeafHarvest
    {
        // Independent, reproducible rolls. The world saves the sequence with its edits.
        public static bool Sapling(BlockPos p,int seed,long sequence)=>TerrainGenerator.Hash(p.X,sequence,p.Z,seed^p.Y^137)%100<5;
        public static bool Apple(BlockPos p,int seed,long sequence)=>TerrainGenerator.Hash(p.X,sequence,p.Z,seed^p.Y^7919)%100<2;
    }

    public sealed partial class VoxelWorld
    {
        readonly HashSet<BlockPos> grownTreeCells=new HashSet<BlockPos>();
        long leafHarvestSequence;
        public Func<BlockPos,bool> GrowthObstructed;
        public bool CanPlantSapling(BlockPos p)=>Ready(p)&&Ready(p.Offset(0,-1,0))&&Get(p)==0&&BlockId.SaplingSoil(Get(p.Offset(0,-1,0)));
        public bool PlantSapling(BlockPos p)=>CanPlantSapling(p)&&Change(p,0,BlockId.Sapling);
        void HarvestLeaf(BlockPos p)
        {
            long sequence=leafHarvestSequence++;
            if(LeafHarvest.Sapling(p,Generator.Seed,sequence))BlockMined?.Invoke(p,BlockId.Sapling);
            if(LeafHarvest.Apple(p,Generator.Seed,sequence))BlockMined?.Invoke(p,BlockId.Apple);
        }
        public bool GrowSapling(BlockPos root)
        {
            if(Get(root)!=BlockId.Sapling||!Ready(root.Offset(0,-1,0))||!BlockId.SaplingSoil(Get(root.Offset(0,-1,0)))||SkyLight(root)<9)return false;
            int height=4+(int)(TerrainGenerator.Hash(root.X,root.Y,root.Z,Generator.Seed)%3);
            var tree=new TerrainGenerator.Tree(root,height);
            var cells=new List<(BlockPos p,byte id)>();
            // Validate the entire footprint before mutating anything. Construction, water,
            // unloaded cells, world bounds and actors all defer growth without consuming it.
            for(int y=0;y<=height;y++)for(int z=-2;z<=2;z++)for(int x=-2;x<=2;x++)
            {
                var p=root.Offset(x,y,z);byte id=tree.At(p);if(id==0)continue;
                if(p.Y>TerrainGenerator.MaxY||Math.Abs(p.X)>TerrainGenerator.HorizontalLimit||Math.Abs(p.Z)>TerrainGenerator.HorizontalLimit||
                    !Ready(p)||Get(p)!=(p.Equals(root)?BlockId.Sapling:BlockId.Air)||(GrowthObstructed?.Invoke(p)??false))return false;
                cells.Add((p,id));
            }
            foreach(var cell in cells)
            {
                Change(cell.p,cell.p.Equals(root)?BlockId.Sapling:BlockId.Air,cell.id,false,false);
                grownTreeCells.Add(cell.p);
            }
            return true;
        }
    }
}
