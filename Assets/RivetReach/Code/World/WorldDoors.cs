using UnityEngine;

namespace RivetReach
{
    public sealed partial class VoxelWorld
    {
        public BlockPos DoorAnchor(BlockPos p)=>Get(p)==IndustryId.DoorUpper?p.Offset(0,-1,0):p;
        internal static bool DoorFloor(byte id)=>BlockId.Solid(id)&&!IndustryId.Placed(id)&&id!=IndustryId.DoorUpper;
        public bool CanPlaceDoor(BlockPos p)
        {
            var top=p.Offset(0,1,0);var floor=p.Offset(0,-1,0);
            return p.Y>TerrainGenerator.MinY&&top.Y<=TerrainGenerator.MaxY&&Ready(p)&&Ready(top)&&Ready(floor)&&
                Get(p)==BlockId.Air&&Get(top)==BlockId.Air&&DoorFloor(Get(floor));
        }
        bool PlaceDoor(BlockPos p)
        {
            if(!CanPlaceDoor(p))return false;
            // Both cells are validated before this synchronous authority turn publishes either edit.
            if(!Change(p.Offset(0,1,0),0,IndustryId.DoorUpper))return false;
            if(Change(p,0,IndustryId.WoodenDoor))return true;
            Change(p.Offset(0,1,0),IndustryId.DoorUpper,0);return false;
        }
        bool RemoveDoor(BlockPos p,byte expected,bool requireReady=true)
        {
            if((requireReady&&!Ready(p))||Get(p)!=expected)return false;
            var anchor=DoorAnchor(p);var upper=anchor.Offset(0,1,0);
            if(Get(anchor)!=IndustryId.WoodenDoor||Get(upper)!=IndustryId.DoorUpper)return false;
            // Counterpart edits remain authoritative even across a dormant chunk boundary.
            Change(upper,IndustryId.DoorUpper,0,false,false);
            return Change(anchor,IndustryId.WoodenDoor,0,false,false);
        }
        void DoorSupportChanged(BlockPos p,byte replacement)
        {
            if(DoorFloor(replacement))return;
            var door=p.Offset(0,1,0);
            if(Get(door)==IndustryId.WoodenDoor&&RemoveDoor(door,IndustryId.WoodenDoor,false))
                BlockMined?.Invoke(door,IndustryId.WoodenDoor);
        }
    }
}
