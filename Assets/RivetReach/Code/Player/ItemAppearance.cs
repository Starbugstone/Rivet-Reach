using UnityEngine;

namespace RivetReach
{
    // Presentation includes crafting components; machine registration is not an art registry.
    public static class ItemAppearance
    {
        public static bool TryIndustryKey(byte id,out string key)
        {
            if(IndustryDefinition.All.TryGetValue(id,out var machine)){key=machine.Key;return true;}
            key=id switch
            {
                IndustryId.AzureCrystal=>"azure_crystal",
                IndustryId.CopperWire=>"copper_wire",
                IndustryId.CopperPlate=>"copper_plate",
                IndustryId.IronPlate=>"iron_plate",
                IndustryId.Cog=>"cog",
                IndustryId.Rivets=>"rivets",
                IndustryId.Casing=>"machine_casing",
                IndustryId.Glass=>"glass",
                IndustryId.CrushedCopper=>"crushed_copper",
                IndustryId.CrushedIron=>"crushed_iron",
                IndustryId.CrushedGold=>"crushed_gold",
                _=>null
            };
            return key!=null;
        }
        // Match the standalone icon, before the world adds connected or sealing parts.
        public static bool VisibleIndustryPart(byte id,string name)
        {
            if(id==IndustryId.TankFrame&&name.StartsWith("Skin",System.StringComparison.Ordinal))return false;
            if(id==IndustryId.SignalWire&&name.StartsWith("Arm",System.StringComparison.Ordinal))return name=="Arm0"||name=="Arm1";
            return true;
        }
        public static Color ToolTint(ItemDefinition item)=>item.tier==ToolTier.None?Color.white:Color.Lerp(Color.white,item.colour,.70f);
        public static string ToolPath(ToolCapability tool)=>(tool&ToolCapability.Axe)!=0?"Tools/StarterAxe":
            (tool&ToolCapability.Pickaxe)!=0?"Characters/GripPickaxe":
            (tool&ToolCapability.Shovel)!=0?"Characters/GripShovel":
            (tool&ToolCapability.Hoe)!=0?"Characters/GripHoe":"Characters/GripSword";
        public static bool BakedIcon(ItemDefinition item)=>item.toolCapabilities!=ToolCapability.None||item.runtimeId==BlockId.Torch||Fluids.IsBucket(item.runtimeId);
    }
}
