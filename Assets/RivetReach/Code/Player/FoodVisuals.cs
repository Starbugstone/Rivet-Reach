using UnityEngine;

namespace RivetReach
{
    // Authored foods share cached mesh data and the palette for their material family.
    public static class FoodVisuals
    {
        public static bool UsesModel(byte id)=>id>=220&&id<=235||id==BlockId.Potato||id==BlockId.BakedPotato||id==BlockId.Apple;
        public static string Key(byte id)=>id>=220&&id<=235?id.ToString():id==BlockId.Apple?"Apple":id==BlockId.BakedPotato?"BakedPotato":"Potato";
        public static Texture2D Icon(byte id)=>Resources.Load<Texture2D>(Folder(id)+Key(id)+"Icon");
        static string Folder(byte id)=>id>=220&&id<=235?"Farming/":id==BlockId.Apple?"Orchard/":"Food/";
        public static Texture2D PaletteFor(byte id)=>Resources.Load<Texture2D>(Folder(id)+"Palette");
        public static Texture2D Palette=>Resources.Load<Texture2D>("Food/Palette");
        public static GameObject Create(byte id,Transform parent,Material heldMaterial=null)
        {
            var model=Object.Instantiate(Resources.Load<GameObject>(Folder(id)+Key(id)),parent,false);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial=heldMaterial!=null?heldMaterial:Resources.Load<Material>(id>=220&&id<=235?"Farming/Farm":id==BlockId.Apple?"Orchard/Apple":"Food/Potato");
                if(heldMaterial!=null)renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return model;
        }
    }
}
