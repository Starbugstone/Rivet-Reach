using System;
using UnityEngine;

namespace RivetReach
{
    // Wear is uses already spent: default/legacy stacks are pristine without migration writes.
    [Serializable]
    public sealed class ToolDurability
    {
        public int[] tierUses;
        public int wrenchUses, fishingRodUses;
        static ToolDurability cached;
        public static ToolDurability Current => cached ??= Load();
        static ToolDurability Load()
        {
            var asset=Resources.Load<TextAsset>("Definitions/ToolDurability");
            if(asset==null)throw new InvalidOperationException("Missing tool durability configuration.");
            var value=JsonUtility.FromJson<ToolDurability>(asset.text);
            if(value==null||value.tierUses==null||value.tierUses.Length!=6||value.tierUses[0]!=0||value.wrenchUses<=0||value.fishingRodUses<=0)
                throw new InvalidOperationException("Invalid tool durability configuration.");
            for(int i=1;i<value.tierUses.Length;i++)
                if(value.tierUses[i]<=value.tierUses[i-1])throw new InvalidOperationException("Better tool tiers must have more durability.");
            return value;
        }
        public int Maximum(ItemDefinition item) => item.toolCapabilities!=ToolCapability.None ? tierUses[(int)item.tier] :
            item.runtimeId==IndustryId.Wrench?wrenchUses:item.runtimeId==FishId.Rod?fishingRodUses:0;
        public int Maximum(ItemStack stack,ItemRegistry registry) => stack.Empty?0:Maximum(registry.Get(stack.Id));
        public string Text(ItemStack stack,ItemRegistry registry)
        {int maximum=Maximum(stack,registry);return maximum==0?"":$"Durability {maximum-stack.Wear} / {maximum}";}
        public bool Valid(ItemStack stack,ItemRegistry registry)
        {int maximum=Maximum(stack,registry);return stack.Wear>=0&&(maximum==0?stack.Wear==0:stack.Wear<maximum&&stack.Count==1);}
    }

    public sealed partial class Expedition
    {
        // Called only after the authoritative action succeeds, once per action (including a whole tree cut).
        public bool WearSelectedTool()
        {
            if(Creative)return false;
            var stack=Inventory.Slots[Selected];int maximum=ToolDurability.Current.Maximum(stack,Registry);
            if(!Inventory.UseTool(Selected,maximum,out bool broken))return false;
            if(broken)Notify(Registry.Get(stack.Id).displayName+" broke",3);
            return true;
        }
    }
}
