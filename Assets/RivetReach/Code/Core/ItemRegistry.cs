using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    [Flags]
    public enum ToolCapability { None=0, Axe=1, Pickaxe=2, Blade=4, Shovel=8, Hoe=16 }
    public enum ToolTier { None, Wood, Stone, Copper, Iron, Diamond }
    public enum ArmorSlot { None, Head, Chest, Legs, Feet }

    public static class BlockId
    {
        public const byte Air=0,Grass=1,Dirt=2,Stone=3,Log=4,Leaves=5,StarterAxe=6,StarterPickaxe=7,StarterDagger=8;
        public const byte IronOre=9,CopperOre=10,CoalOre=11,GoldOre=12,DiamondOre=13,Bedrock=14;
        public const byte RawIron=15,RawCopper=16,Coal=17,RawGold=18,Diamond=19;
        public const byte Torch=96;
        public const byte Sand=90,Sandstone=91,Snow=92,RedClay=93;
        public static bool BiomeBlock(byte id)=>id>=Sand&&id<=RedClay;
        public const byte Planks=20,Stick=21,Cobblestone=22,Workbench=23,Furnace=24,Chest=25,Charcoal=26,CopperIngot=27,IronIngot=28,GoldIngot=29;
        public const byte Potato=30,BakedPotato=31,Farmland=32,PotatoPlant=33,MaturePotatoPlant=36;
        public const byte WoodAxe=40,WoodPickaxe=41,WoodSword=42,WoodShovel=43,WoodHoe=44;
        public const byte StoneAxe=45,StonePickaxe=46,StoneSword=47,StoneShovel=48,StoneHoe=49;
        public const byte CopperAxe=50,CopperPickaxe=51,CopperSword=52,CopperShovel=53,CopperHoe=54;
        public const byte IronAxe=55,IronPickaxe=56,IronSword=57,IronShovel=58,IronHoe=59;
        public const byte DiamondAxe=60,DiamondPickaxe=61,DiamondSword=62,DiamondShovel=63,DiamondHoe=64;
        public const byte CoalBlock=65,IronBlock=66,CopperBlock=67,GoldBlock=68,DiamondBlock=69;
        public static bool Ore(byte id)=>id>=IronOre&&id<=DiamondOre||id==IndustryId.AzureOre;
        public static bool RawMaterial(byte id)=>id>=RawIron&&id<=Diamond;
        public static bool Crop(byte id)=>id>=PotatoPlant&&id<=MaturePotatoPlant;
        public static bool Station(byte id)=>id==Workbench||id==Furnace||id==Chest||id==IndustryId.Bench;
        public static ToolTier RequiredTier(byte id)=>id==IndustryId.AzureOre?ToolTier.Copper:id==DiamondOre||id==GoldOre||id==GoldBlock||id==DiamondBlock?ToolTier.Iron:
            id==IronOre||id==CopperOre||id==IronBlock||id==CopperBlock?ToolTier.Stone:
            id==Stone||id==Cobblestone||id==CoalOre||id==Furnace||id==CoalBlock?ToolTier.Wood:ToolTier.None;
        public static bool Mineable(byte id,ToolCapability tool,ToolTier tier=ToolTier.Diamond)=>id!=Air&&id!=Bedrock&&(Placeable(id)||Ore(id)||id==Farmland||Crop(id))&&
            (RequiredTier(id)==ToolTier.None||(tool&ToolCapability.Pickaxe)!=0&&tier>=RequiredTier(id));
        public static string MiningHint(byte id,ToolCapability tool,ToolTier tier=ToolTier.Diamond)=>id==Bedrock?"Unbreakable":!Mineable(id,tool,tier)&&RequiredTier(id)!=ToolTier.None?"Requires "+RequiredTier(id).ToString().ToLowerInvariant()+" pickaxe or better":"";
        public static bool Placeable(byte id)=>IndustryId.Placed(id)||id==Torch||id>=Grass&&id<=Leaves||BiomeBlock(id)||id==Planks||id==Cobblestone||Station(id)||id>=CoalBlock&&id<=DiamondBlock;
        public static bool Solid(byte id)=>id!=Air&&id!=Torch&&!IndustryId.Thin(id)&&!Crop(id)&&!Fluids.IsFluid(id);
        public static bool Opaque(byte id)=>Solid(id)&&id!=Leaves&&!IndustryId.Placed(id);
        public static int Tile(byte id,int axis,int sign)=>id==IndustryId.AzureOre?44:id==Planks?18:id==Cobblestone?19:id==Workbench?(axis==1&&sign>0?20:21):id==Furnace?(axis==1?19:22):id==Chest?23:id==Farmland?(axis==1&&sign>0?24:2):Crop(id)?25+id-PotatoPlant:id>=CoalBlock&&id<=DiamondBlock?29+id-CoalBlock:
            BiomeBlock(id)?40+id-Sand:Ore(id)?7+id-IronOre:RawMaterial(id)?13+id-RawIron:id==Bedrock?12:id==Grass?(axis==1?(sign>0?0:2):1):id==Dirt?2:id==Log?(axis==1?5:4):id==Leaves?6:3;
    }

    [Serializable]
    public sealed class ItemDefinition
    {
        public byte runtimeId;
        public string stableId;
        public string displayName;
        public int stackLimit = 64;
        public float fistSeconds = 0.6f;
        public Color colour = Color.white;
        public byte fistDropId;
        public ToolCapability toolCapabilities;
        public ToolTier tier;
        public float miningSpeed=1;
        public int foodPoints;
        public ArmorSlot armorSlot;
        public int armorPoints;
        public int attackDamage=1;
        public bool buoyant;
    }

    [CreateAssetMenu(menuName="Rivet Reach/Block and item registry")]
    public sealed class ItemRegistry : ScriptableObject
    {
        public ItemDefinition[] items;
        ItemDefinition[] byId;
        Dictionary<string, byte> byStableId;
        void OnEnable() => InvalidateIndex();
        public void InvalidateIndex(){byId=null;byStableId=null;}
        void OnValidate() => InvalidateIndex();
        void BuildIndex()
        {
            var ids=new ItemDefinition[256];
            var stable=new Dictionary<string,byte>(StringComparer.Ordinal);
            if(items!=null)foreach(var item in items)
            {
                if(item==null||item.runtimeId==0||string.IsNullOrWhiteSpace(item.stableId)||item.stackLimit<=0)
                    throw new InvalidOperationException("Each item needs nonzero runtime ID, stable ID and positive stack limit.");
                if(Fluids.IsFluid(item.runtimeId))throw new InvalidOperationException("Fluid cell encoding cannot be an item: "+item.stableId);
                if(ids[item.runtimeId]!=null||stable.ContainsKey(item.stableId))
                    throw new InvalidOperationException("Duplicate item identity: "+item.stableId);
                if(item.attackDamage<1||item.foodPoints<0||item.foodPoints>HungerState.Maximum||
                    item.armorSlot<ArmorSlot.None||item.armorSlot>ArmorSlot.Feet||item.armorPoints<0||item.armorPoints>20||
                    item.armorSlot==ArmorSlot.None&&item.armorPoints!=0||item.armorSlot!=ArmorSlot.None&&item.stackLimit!=1||
                    item.toolCapabilities!=ToolCapability.None&&(item.stackLimit!=1||item.tier<ToolTier.Wood||item.tier>ToolTier.Diamond||
                        float.IsNaN(item.miningSpeed)||float.IsInfinity(item.miningSpeed)||item.miningSpeed<=0))
                    throw new InvalidOperationException("Invalid tool, food or armor statistics: "+item.stableId);
                ids[item.runtimeId]=item;stable.Add(item.stableId,item.runtimeId);
            }
            byId=ids;byStableId=stable;
        }
        public ItemDefinition Get(byte id)
        {
            if(byId==null)BuildIndex();
            return byId[id]??throw new ArgumentOutOfRangeException(nameof(id), $"Unknown item {id}");
        }
        public byte ResolveId(string stableId)
        {
            if(byStableId==null)BuildIndex();
            return byStableId.TryGetValue(stableId,out byte id)?id:throw new ArgumentException("Unknown item "+stableId);
        }
        public static ItemRegistry Load() => Resources.Load<ItemRegistry>("Definitions/Items");
        public ToolCapability Capabilities(ItemStack stack)=>stack.Empty?ToolCapability.None:Get(stack.Id).toolCapabilities;
        public ToolTier Tier(ItemStack stack)=>stack.Empty?ToolTier.None:Get(stack.Id).tier;
        public float MiningSeconds(byte blockId,ItemStack held)
        {
            var tool=Capabilities(held);
            if(!BlockId.Mineable(blockId,tool,Tier(held)))return float.PositiveInfinity;
            bool effective=(BlockId.RequiredTier(blockId)!=ToolTier.None&&(tool&ToolCapability.Pickaxe)!=0)||
                ((blockId==BlockId.Log||blockId==BlockId.Planks||blockId==BlockId.Workbench||blockId==BlockId.Chest)&&(tool&ToolCapability.Axe)!=0)||
                ((blockId==BlockId.Dirt||blockId==BlockId.Grass||blockId==BlockId.Farmland)&&(tool&ToolCapability.Shovel)!=0)||
                (blockId==BlockId.Leaves&&(tool&(ToolCapability.Hoe|ToolCapability.Blade))!=0);
            return MiningWorkSeconds(blockId)/(effective?Math.Max(.1f,Get(held.Id).miningSpeed):1);
        }
        // Ores embedded in stone take at least 25% more work than their host.
        // Keep harder authored ores and apply the held tool's speed after this floor.
        float MiningWorkSeconds(byte blockId)=>BlockId.Ore(blockId)?Math.Max(Get(blockId).fistSeconds,Get(BlockId.Stone).fistSeconds*1.25f):Get(blockId).fistSeconds;
        public float MiningSeconds(byte blockId,ToolCapability tool)=>!BlockId.Mineable(blockId,tool)?float.PositiveInfinity:MiningWorkSeconds(blockId)*
            (blockId==BlockId.Log&&(tool&ToolCapability.Axe)!=0?.3f:1f);
        public byte FistDrop(byte blockId){var item=Get(blockId);return item.fistDropId==0?blockId:item.fistDropId;}
    }

}
