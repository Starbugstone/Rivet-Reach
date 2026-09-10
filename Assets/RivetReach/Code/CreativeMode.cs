namespace RivetReach
{
    public sealed partial class Expedition
    {
        public bool Creative {get;private set;}

        public void SetCreative(bool enabled)
        {
            if(!Started||Health.Dead||Creative==enabled)return;
            Creative=enabled;
            Player.ResetMotion();
            UI.Rebuild();
            Notify(enabled?"Creative enabled · Open inventory for all items":"Survival enabled · Gravity and damage restored",4);
        }

        // The catalog uses the registered items and the normal container transaction.
        // A full inventory rejects the grant without deleting or dropping existing items.
        public bool TryGiveCreativeItem(byte id)
        {
            if(!Creative||!InventoryOpen||Health.Dead)return false;
            var item=Registry.Get(id);
            bool added=Inventory.TryAddExact(id,item.stackLimit);
            if(added)Sound.Pickup();
            return added;
        }
    }
}
