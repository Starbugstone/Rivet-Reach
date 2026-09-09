using System;
using UnityEngine;

namespace RivetReach
{
    [Serializable] public struct FuelData {public string itemId;public int ticks;}
    [CreateAssetMenu(menuName="Rivet Reach/Crafting/Furnace catalog")]
    public sealed class ProcessingCatalogAsset : ScriptableObject
    {
        public ProcessingRecipeAsset[] recipes;
        public FuelData[] fuels;
        public ProcessingRegistry Compile(ItemRegistry items)
        {
            if(recipes==null||fuels==null)throw new ArgumentException("Furnace catalog needs recipes and fuels.");
            var specs=new ProcessingSpec[recipes.Length];var fuelSpecs=new FuelSpec[fuels.Length];
            for(int i=0;i<specs.Length;i++)specs[i]=recipes[i]!=null?recipes[i].ToSpec():throw new ArgumentException("Missing furnace recipe asset "+i);
            for(int i=0;i<fuels.Length;i++)fuelSpecs[i]=new FuelSpec(fuels[i].itemId,fuels[i].ticks);
            return ProcessingRegistry.Compile(specs,fuelSpecs,items.ResolveId,id=>items.Get(id).stackLimit);
        }
        public static ProcessingCatalogAsset Load()=>Resources.Load<ProcessingCatalogAsset>("Definitions/Processing")??throw new InvalidOperationException("Missing Definitions/Processing catalog.");
    }
}
