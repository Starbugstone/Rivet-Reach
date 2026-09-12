using System;
using UnityEngine;

namespace RivetReach
{
    // One artwork contract for inventory, palm and physical item displays.
    public static class EquipmentVisuals
    {
        static readonly string[] Slots={"Head","Chest","Legs","Feet"};
        public static bool UsesModel(byte id)=>id>=BlockId.CopperIngot&&id<=BlockId.GoldIngot||id>=70&&id<=81;
        public static string Slot(byte id)=>id<70?"Ingot":Slots[(id-70)%4];
        public static string Tier(byte id)=>id<70?(id==27?"Copper":id==28?"Iron":"Gold"):(id<74?"Copper":id<78?"Iron":"Diamond");
        public static Texture2D Palette(byte id)=>Resources.Load<Texture2D>("Equipment/"+Tier(id)+"Palette");
        public static Texture2D Icon(byte id)=>Resources.Load<Texture2D>("Equipment/"+Tier(id)+Slot(id)+"Icon");
        public static Material Material(byte id)=>Resources.Load<Material>("Equipment/"+Tier(id));
        public static GameObject Create(byte id,Transform parent,Material held=null)
        {
            var asset=Resources.Load<GameObject>("Equipment/"+Slot(id)+(id<70?"":"Item"));
            if(asset==null)throw new InvalidOperationException("Missing equipment artwork: "+id);
            var model=UnityEngine.Object.Instantiate(asset,parent,false);
            foreach(var r in model.GetComponentsInChildren<Renderer>())
            {r.sharedMaterial=held!=null?held:Material(id);if(held!=null)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
            return model;
        }
    }
}
