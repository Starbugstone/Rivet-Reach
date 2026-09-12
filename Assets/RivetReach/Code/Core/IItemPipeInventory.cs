using System.Collections.Generic;

namespace RivetReach
{
    // Transport asks the inventory owner to validate insertion and extraction.
    // A direction setting never grants access to recipe inputs or result slots.
    public interface IItemPipeInventory
    {
        IReadOnlyList<ItemStack> Slots {get;}
        bool CanExtract(int slot);
        // localFace uses right, left, top, bottom, back, front.
        // -1 is an unsided inventory operation; transport always supplies a face.
        bool Prefers(byte id,int localFace=-1);
        bool TryInsert(byte id,int localFace=-1);
        ItemStack Extract(int slot,int count);
    }
    public interface IIndustryItemEndpoints
    {
        IItemPipeInventory ItemEndpoint(BlockPos position);
        int ItemEndpointRotation(BlockPos position);
        void ItemEndpointChanged(BlockPos position);
    }
}
