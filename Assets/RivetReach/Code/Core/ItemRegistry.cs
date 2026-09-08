using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    [Flags]
    public enum ToolCapability { None=0, Axe=1, Pickaxe=2, Blade=4 }

    public static class BlockId
    {
        public const byte Air=0,Grass=1,Dirt=2,Stone=3,Log=4,Leaves=5,StarterAxe=6,StarterPickaxe=7,StarterDagger=8;
        public static bool Placeable(byte id)=>id>=Grass&&id<=Leaves;
        public static bool Opaque(byte id)=>id!=Air&&id!=Leaves;
        public static int Tile(byte id,int axis,int sign)=>id==Grass?(axis==1?(sign>0?0:2):1):id==Dirt?2:id==Log?(axis==1?5:4):id==Leaves?6:3;
    }

    [Serializable]
    public sealed class ItemDefinition
    {
        public byte runtimeId;
        public string stableId;
        public string displayName;
        public int stackLimit = 500;
        public float fistSeconds = 0.6f;
        public Color colour = Color.white;
        public byte fistDropId;
        public ToolCapability toolCapabilities;
    }

    [CreateAssetMenu(menuName="Rivet Reach/Block and item registry")]
    public sealed class ItemRegistry : ScriptableObject
    {
        public ItemDefinition[] items;
        ItemDefinition[] byId;
        Dictionary<string, byte> byStableId;
        void OnEnable() { byId=null; byStableId=null; }
        void OnValidate() { byId=null; byStableId=null; }
        void BuildIndex()
        {
            var ids=new ItemDefinition[256];
            var stable=new Dictionary<string,byte>(StringComparer.Ordinal);
            if(items!=null)foreach(var item in items)
            {
                if(item==null||item.runtimeId==0||string.IsNullOrWhiteSpace(item.stableId)||item.stackLimit<=0)
                    throw new InvalidOperationException("Each item needs nonzero runtime ID, stable ID and positive stack limit.");
                if(ids[item.runtimeId]!=null||stable.ContainsKey(item.stableId))
                    throw new InvalidOperationException("Duplicate item identity: "+item.stableId);
                ids[item.runtimeId]=item;stable.Add(item.stableId,item.runtimeId);
            }
            byId=ids;byStableId=stable;
        }
        public ItemDefinition Get(byte id)
        {
            if(byId==null)BuildIndex();
            return byId[id]??throw new ArgumentOutOfRangeException(nameof(id), $"Unknown item {id}");
        }
        public byte ResolveId(string stableId)
        {
            if(byStableId==null)BuildIndex();
            return byStableId.TryGetValue(stableId,out byte id)?id:throw new ArgumentException("Unknown item "+stableId);
        }
        public static ItemRegistry Load() => Resources.Load<ItemRegistry>("Definitions/Items");
        public ToolCapability Capabilities(ItemStack stack)=>stack.Empty?ToolCapability.None:Get(stack.Id).toolCapabilities;
        public float MiningSeconds(byte blockId,ToolCapability tool)=>Get(blockId).fistSeconds*
            (blockId==BlockId.Log&&(tool&ToolCapability.Axe)!=0?.3f:1f);
        public byte FistDrop(byte blockId){var item=Get(blockId);return item.fistDropId==0?blockId:item.fistDropId;}
    }

}
