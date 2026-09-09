using UnityEngine;

namespace RivetReach
{
    [CreateAssetMenu(menuName="Rivet Reach/Crafting/Furnace recipe")]
    public sealed class ProcessingRecipeAsset : ScriptableObject
    {
        public string stableId;
        public RecipeCellData input,output;
        [Min(1)] public int ticks=200;
        public ProcessingSpec ToSpec()=>new ProcessingSpec{Id=stableId,Input=input.itemId,InputCount=input.count,Output=output.itemId,OutputCount=output.count,Ticks=ticks};
    }
}
