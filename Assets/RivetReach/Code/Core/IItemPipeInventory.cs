using System.Collections.Generic;

namespace RivetReach
{
    // Transport asks the inventory owner to validate insertion and extraction.
    // A direction setting never grants access to recipe inputs or result slots.
    public interface IItemPipeInventory
    {
        IReadOnlyList<ItemStack> Slots {get;}
        bool CanExtract(int slot);
        bool Prefers(byte id);
        bool TryInsert(byte id);
        ItemStack Extract(int slot,int count);
    }
    public interface IIndustryItemEndpoints
    {
        IItemPipeInventory ItemEndpoint(BlockPos position);
        void ItemEndpointChanged(BlockPos position);
    }
}
