using UnityEngine;

namespace RivetReach
{
    // A bounded asset cache, not one mesh/material allocation per ore instance.
    public static class OreVisuals
    {
        static GameObject prefab;
        static readonly Material[] materials=new Material[256];
        static readonly Texture2D[] palettes=new Texture2D[256];
        public static GameObject Prefab=>prefab!=null?prefab:prefab=Resources.Load<GameObject>("Industry/Runtime/azure_ore");
        public static Texture2D Palette(byte id)=>palettes[id]!=null?palettes[id]:palettes[id]=Resources.Load<Texture2D>(id==IndustryId.AzureOre?"Industry/Atlas":"Ores/"+id+"Atlas");
        public static Material Material(byte id)=>materials[id]!=null?materials[id]:materials[id]=Resources.Load<Material>(id==IndustryId.AzureOre?"Industry/Workshop":"Ores/"+id);
        public static GameObject Create(byte id,Transform parent)
        {
            var model=Object.Instantiate(Prefab,parent,false);
            model.GetComponentInChildren<MeshRenderer>().sharedMaterial=Material(id);
            return model;
        }
    }
}
