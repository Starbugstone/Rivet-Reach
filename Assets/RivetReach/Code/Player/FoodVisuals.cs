using UnityEngine;

namespace RivetReach
{
    // Two authored foods share one palette and world material. Instances share mesh data.
    public static class FoodVisuals
    {
        public static bool UsesModel(byte id)=>id==BlockId.Potato||id==BlockId.BakedPotato;
        public static string Key(byte id)=>id==BlockId.BakedPotato?"BakedPotato":"Potato";
        public static Texture2D Icon(byte id)=>Resources.Load<Texture2D>("Food/"+Key(id)+"Icon");
        public static Texture2D Palette=>Resources.Load<Texture2D>("Food/Palette");
        public static GameObject Create(byte id,Transform parent,Material heldMaterial=null)
        {
            var model=Object.Instantiate(Resources.Load<GameObject>("Food/"+Key(id)),parent,false);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial=heldMaterial!=null?heldMaterial:Resources.Load<Material>("Food/Potato");
                if(heldMaterial!=null)renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return model;
        }
    }
}
