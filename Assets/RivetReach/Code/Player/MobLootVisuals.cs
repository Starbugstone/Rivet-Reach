using UnityEngine;

namespace RivetReach
{
    public static class MobLootVisuals
    {
        public static bool UsesModel(byte id)=>id==BlockId.FloaterRock;
        public static Texture2D Palette=>Resources.Load<Texture2D>("Mobs/CreaturePalette");
        public static Texture2D Icon=>Resources.Load<Texture2D>("MobLoot/FloaterRockIcon");
        public static GameObject Create(Transform parent,Material heldMaterial=null)
        {
            var model=Object.Instantiate(Resources.Load<GameObject>("MobLoot/FloaterRock"),parent,false);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial=heldMaterial!=null?heldMaterial:Resources.Load<Material>("Mobs/CreatureMaterial");
                if(heldMaterial!=null)renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return model;
        }
    }
}
