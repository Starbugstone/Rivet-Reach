using System;
using System.Collections.Generic;

namespace RivetReach
{
    public static class FarmId
    {
        public const byte WheatPlant=200,FlaxPlant=204,CarrotPlant=208,BerryPlant=212,MushroomPlant=216;
        public const byte WheatSeed=220,Grain=221,FlaxSeed=222,Fibre=223,String=224,Cloth=225,CarrotSeed=226,Carrot=227,BerrySeed=228,Berries=229,Mushroom=230,Bread=231,RoastCarrot=232,Stew=233,CookedMushroom=234,Porridge=235;
        public const byte Cooker=240,ElectricCooker=241;
        public static bool CookerBlock(byte id)=>id==Cooker||id==ElectricCooker;
        public static bool Added(byte id)=>id>=200&&id<=216||id>=220&&id<=235||CookerBlock(id);
    }
    [Serializable]
    public sealed class CropDefinition
    {
        public string key;
        public byte first,stages,planting,produce;
        public int stageTicks=1200,minYield=2,maxYield=3,seedYield=2;
        public bool naturalSoil=true;
        public byte Mature=>(byte)(first+stages-1);
        public bool Supports(byte ground)=>ground==BlockId.Farmland||naturalSoil&&(ground==BlockId.Grass||ground==BlockId.Dirt);
    }
    // Immutable during a session. Pure data is also safe for terrain/mesh workers.
    public static class CropRules
    {
        public static readonly CropDefinition[] Definitions={
            new CropDefinition{key="potato",first=33,stages=4,planting=30,produce=30,minYield=2,maxYield=4,seedYield=0},
            new CropDefinition{key="wheat",first=200,stages=4,planting=220,produce=221},
            new CropDefinition{key="flax",first=204,stages=4,planting=222,produce=223},
            new CropDefinition{key="carrot",first=208,stages=4,planting=226,produce=227},
            new CropDefinition{key="berry",first=212,stages=4,planting=228,produce=229},
            new CropDefinition{key="mushroom",first=216,stages=1,produce=230,minYield=1,maxYield=2,seedYield=0}
        };
        public static CropDefinition For(byte id)
        {foreach(var d in Definitions)if(id>=d.first&&id<=d.Mature)return d;return null;}
        public static CropDefinition Planting(byte id)
        {if(id==0)return null;foreach(var d in Definitions)if(d.planting==id)return d;return null;}
        public static IEnumerable<ItemStack> Harvest(byte id,uint random)
        {
            var d=For(id);if(d==null)yield break;
            if(id!=d.Mature){if(d.planting!=0&&d.planting!=d.produce)yield return new ItemStack(d.planting,1);yield break;}
            yield return new ItemStack(d.produce,d.minYield+(int)(random%(d.maxYield-d.minYield+1)));
            if(d.planting!=0&&d.seedYield>0)yield return new ItemStack(d.planting,d.seedYield);
        }
    }
}
