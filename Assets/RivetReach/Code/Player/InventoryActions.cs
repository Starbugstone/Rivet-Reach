using UnityEngine;

namespace RivetReach
{
    public static class InventoryActions
    {
        public static int Pick(Inventory inventory,ItemRegistry registry,byte block,int selected,bool creative)
        {
            if(block==0||Fluids.IsFluid(block))return selected;
            var crop=CropRules.For(block);
            byte item=BedId.Part(block)?BedId.Bed:crop!=null?(crop.planting!=0?crop.planting:crop.produce):block==IndustryId.DoorUpper?IndustryId.WoodenDoor:block;
            for(int i=0;i<Inventory.HotbarCount;i++)if(inventory.Slots[i].Id==item)return i;
            for(int i=Inventory.HotbarCount;i<inventory.Count;i++)if(inventory.Slots[i].Id==item)
            {inventory.Swap(i,selected);return selected;}
            if(creative)
            {
                var replacement=new ItemStack(item,registry.Get(item).stackLimit);
                inventory.Take(selected,int.MaxValue);inventory.Add(replacement,selected,selected+1);
            }
            return selected;
        }
    }

    public sealed partial class Expedition
    {
        public bool TryHarvestCrop(BlockPos position,byte expected,bool hoe)
        {
            var crop=CropRules.For(expected);
            if(crop==null||expected!=crop.Mature||!World.Ready(position)||!World.Ready(position.Offset(0,-1,0))||
                World.Get(position.Offset(0,-1,0))!=BlockId.Farmland)return false;
            if(!hoe)return World.Mine(position,expected,ToolCapability.None);
            // Removal schedules the ordinary growth invalidation, but deliberately emits
            // no world mining-drop event: this command owns the one harvest transaction.
            if(!World.Remove(position,expected))return false;
            foreach(var stack in CropRules.Harvest(expected,TerrainGenerator.Hash(position.X,position.Y,position.Z,Seed)))
            {
                int remainder=Inventory.Add(stack);
                if(remainder>0)Items.Spawn(stack.WithCount(remainder),World.Local(position)+new Vector3(.5f,.3f,.5f),Vector3.up*1.6f,actionCreated:true);
            }
            if(crop.planting!=0)
            {
                int seed=Inventory.FindSlot(stack=>!stack.Empty&&stack.Id==crop.planting);
                if(seed>=0&&World.Plant(position,crop.planting))Inventory.Take(seed,1);
            }
            return true;
        }
    }
}
