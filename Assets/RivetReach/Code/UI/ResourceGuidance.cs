using System;
using System.Collections.Generic;

namespace RivetReach
{
    // The generator bands and mining authority own the values. Guidance never
    // grants discoveries, scans for deposits, or changes resource availability.
    public static class ResourceGuidance
    {
        public static byte OreFor(byte id)=>id switch
        {
            BlockId.Coal or BlockId.CoalBlock=>BlockId.CoalOre,
            BlockId.RawCopper or BlockId.CopperIngot or BlockId.CopperBlock=>BlockId.CopperOre,
            BlockId.RawIron or BlockId.IronIngot or BlockId.IronBlock=>BlockId.IronOre,
            BlockId.RawGold or BlockId.GoldIngot or BlockId.GoldBlock=>BlockId.GoldOre,
            BlockId.Diamond or BlockId.DiamondBlock=>BlockId.DiamondOre,
            IndustryId.AzureCrystal=>IndustryId.AzureOre,
            _=>BlockId.Ore(id)?id:(byte)0
        };
        public static string Source(byte id,ItemRegistry registry)
        {
            byte ore=OreFor(id);
            foreach(var band in OreGenerator.Bands)if(band.Block==ore)
                return $"{registry.Get(ore).displayName}: Y {band.MinY} to {band.MaxY}; most likely near Y {band.PeakY}.\nRequired tool: {BlockId.RequiredTier(ore).ToString().ToLowerInvariant()} pickaxe or better. Deposits are finite; cave exposure varies.";
            if(id==BlockId.FloaterRock)return "Defeat a Floater in a dark underground cave. Each drops one Floater Rock. Bring a weapon, food and placed lights.";
            if(id==BlockId.Stone)return "Mine stone with a wooden pickaxe or better for Cobblestone. Smelt Cobblestone back into Stone.";
            if(id==IndustryId.Glass)return "Smelt Sand in a Furnace or Electric Furnace. Place adjacent Glass blocks to join their window edges.";
            if(id==BuildingBlocks.WoodenSlab||id==BuildingBlocks.StoneSlab)return "Place on a lower or upper half. Use another matching slab on its exposed flat face to combine two halves.";
            return "";
        }
        public static string AtDepth(int y,ItemRegistry registry)
        {
            var names=new List<string>();
            foreach(var band in OreGenerator.Bands)if(y>=band.MinY&&y<=band.MaxY)
                names.Add(registry.Get(band.Block).displayName.Replace(" Ore","").Replace(" ore",""));
            return names.Count==0?"Explore caves or dig lower for ores":"At this level: "+string.Join(" · ",names);
        }
        public static string ToolHint(byte id,ToolCapability tool,ToolTier tier)
        {
            string blocked=BlockId.MiningHint(id,tool,tier);
            if(blocked.Length>0)return blocked;
            var required=BlockId.RequiredTier(id);
            return required==ToolTier.None?"":required+" pickaxe or better · ready";
        }
    }
}
