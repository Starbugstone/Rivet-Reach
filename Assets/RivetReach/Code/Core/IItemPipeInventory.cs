using System.Collections.Generic;

namespace RivetReach
{
    // An allocation-free declaration of item/face interest, not a reservation.
    // TryInsert remains the authority for live capacity and transaction validation.
    public enum ItemInputRequest : byte { Reject,Accept,Prefer }

    // Transport asks the inventory owner to validate insertion and extraction.
    // A direction setting never grants access to recipe inputs or result slots.
    public interface IItemPipeInventory
    {
        int ItemInputPriority {get;set;}
        IReadOnlyList<ItemStack> Slots {get;}
        // Read live slot state without materializing an aggregate warehouse snapshot.
        ItemStack ReadSlot(int slot);
        bool CanExtract(int slot);
        // localFace uses right, left, top, bottom, back, front.
        // -1 is an unsided inventory operation; transport always supplies a face.
        ItemInputRequest QueryInput(byte id,int localFace=-1);
        bool TryInsert(byte id,int localFace=-1);
        ItemStack Extract(int slot,int count);
    }
    // Optional behavior for inventories that preserve instance metadata. Ordinary
    // recipe/fuel consumers deliberately do not opt in to accepting worn tools or cargo.
    public interface IItemPipeStackReceiver
    {
        bool TryInsertStack(ItemStack stack,int localFace);
    }
    public interface IIndustryItemEndpoints
    {
        IItemPipeInventory ItemEndpoint(BlockPos position);
        int ItemEndpointRotation(BlockPos position);
        void ItemEndpointChanged(BlockPos position);
    }
}
