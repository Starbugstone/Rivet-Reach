using System;
using UnityEngine;

namespace RivetReach
{
    [Serializable] public sealed class FarmingCatalog
    {
        public CropDefinition[] crops;
        public static void Load()
        {
            var asset=Resources.Load<TextAsset>("Definitions/Crops");if(asset==null)throw new InvalidOperationException("Missing crop catalog.");
            var catalog=JsonUtility.FromJson<FarmingCatalog>(asset.text);
            if(catalog.crops==null||catalog.crops.Length!=CropRules.Definitions.Length)throw new ArgumentException("Invalid crop catalog.");
            for(int i=0;i<catalog.crops.Length;i++)
            {
                var c=catalog.crops[i];var expected=CropRules.Definitions[i];
                if(c.first!=expected.first||c.stages!=expected.stages||c.planting!=expected.planting||c.produce!=expected.produce||c.stageTicks<20||c.stageTicks>24000||c.minYield<1||c.maxYield<c.minYield||c.maxYield>64||c.seedYield<0||c.seedYield>64)throw new ArgumentException("Invalid crop definition: "+c.key);
                // Identity/layout is stable; working growth/drop balance is authored in the catalog.
                expected.stageTicks=c.stageTicks;expected.minYield=c.minYield;expected.maxYield=c.maxYield;expected.seedYield=c.seedYield;expected.naturalSoil=c.naturalSoil;
            }
        }
    }
}
