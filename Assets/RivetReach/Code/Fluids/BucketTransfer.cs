namespace RivetReach
{
    public static class BucketTransfer
    {
        // Both buckets are single-stack items. Replacement uses the same slot even in a full inventory.
        public static bool TryUse(IFluidWorld world,ItemContainer inventory,int slot,BlockPos target,FluidRegistry registry)
        {
            var held=inventory.Slots[slot];if(held.Count!=1||!world.TryRead(target,out byte cell))return false;
            byte output,replacement;var fluid=registry.Get(cell);
            if(held.Id==Fluids.EmptyBucket)
            {if(fluid==null||!fluid.IsSource(cell))return false;output=fluid.BucketItem;replacement=0;}
            else
            {
                var placed=registry.FromBucket(held.Id);
                if(placed==null||cell!=0&&(fluid!=placed||fluid.IsSource(cell)))return false;
                output=Fluids.EmptyBucket;replacement=placed.Source;
            }
            // Validate both phases before world callbacks. Inventory is private to this authority.
            if(!inventory.CanReplaceSingle(slot,held.Id,output))return false;
            if(!world.ChangeFluid(target,cell,replacement))return false;
            inventory.ReplaceSingle(slot,held.Id,output);return true;
        }
    }
}
