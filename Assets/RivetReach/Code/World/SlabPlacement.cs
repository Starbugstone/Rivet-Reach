using UnityEngine;

namespace RivetReach
{
    public sealed partial class VoxelWorld
    {
        public bool PlaceSlab(BlockPos cell,byte expected,byte replacement)
        {
            var slab=BuildingBlocks.SlabFor(replacement);
            if(slab==null||!Ready(cell)||Get(cell)!=expected)return false;
            bool merge=replacement==slab.Double&&(expected==slab.Lower||expected==slab.Upper);
            bool fresh=(replacement==slab.Lower||replacement==slab.Upper)&&(expected==0||Fluids.IsFluid(expected));
            return (merge||fresh)&&Change(cell,expected,replacement);
        }
    }
    public sealed partial class Expedition
    {
        bool SlabPlacement(IHalfBlock slab,out BlockPos cell,out byte expected,out byte replacement,out string reason)
        {
            cell=default;expected=replacement=0;reason="Aim at a block face";
            if(!World.Select(Player.Camera.transform.position,Player.Camera.transform.forward,5,out var hit)||hit.Face==Vector3Int.zero)return false;
            BuildingBlocks.Placement(slab,hit,hit.Point-World.Local(hit.Position),out cell,out replacement,out expected);
            if(!World.Ready(cell)){reason="Waiting for nearby terrain";return false;}
            byte occupied=World.Get(cell);
            if(expected==0)
            {
                expected=occupied;
                if(occupied==slab.Lower&&replacement==slab.Upper||occupied==slab.Upper&&replacement==slab.Lower)replacement=slab.Double;
                else if(occupied!=0&&!Fluids.IsFluid(occupied)){reason="This half is occupied";return false;}
            }
            if(occupied!=expected){reason="The supporting block changed";return false;}
            reason=PlayerOverlapReason;if(World.OccupiesBlock(Player.transform.position,.6f,Player.Height,cell,replacement))return false;
            reason="Cannot place inside a creature";if((Mobs?.Occupies(cell)??false)||(Animals?.Occupies(cell)??false))return false;
            reason=replacement==slab.Double?"Combine matching slabs":"Place "+(replacement==slab.Upper?"upper":"lower")+" slab";return true;
        }
    }
}
