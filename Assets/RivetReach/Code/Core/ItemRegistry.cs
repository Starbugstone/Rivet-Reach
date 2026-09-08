using System;
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
        public ItemDefinition Get(byte id)
        {
            foreach (var item in items) if (item.runtimeId == id) return item;
            throw new ArgumentOutOfRangeException(nameof(id), $"Unknown item {id}");
        }
        public static ItemRegistry Load() => Resources.Load<ItemRegistry>("Definitions/Items");
        public ToolCapability Capabilities(ItemStack stack)=>stack.Empty?ToolCapability.None:Get(stack.Id).toolCapabilities;
        public float MiningSeconds(byte blockId,ToolCapability tool)=>Get(blockId).fistSeconds*
            (blockId==BlockId.Log&&(tool&ToolCapability.Axe)!=0?.3f:1f);
        public byte FistDrop(byte blockId){var item=Get(blockId);return item.fistDropId==0?blockId:item.fistDropId;}
    }

    [Serializable]
    public struct ItemStack
    {
        public byte Id;
        public int Count;
        public bool Empty => Id == 0 || Count <= 0;
        public ItemStack(byte id,int count) { Id=count > 0 ? id : (byte)0; Count=Math.Max(0,count); }
        public void Clear() { Id=0; Count=0; }
    }

    public sealed class Inventory
    {
        public const int HotbarCount=12, MainCount=48, SlotCount=60;
        public readonly ItemStack[] Slots=new ItemStack[SlotCount];
        readonly Func<byte,int> limit;
        public int Revision { get; private set; }
        public Inventory(Func<byte,int> stackLimit) { limit=stackLimit; }
        public int Total(byte id) { int n=0; foreach(var s in Slots) if(s.Id==id) n+=s.Count; return n; }
        public int Add(byte id,int count,int start=0,int end=SlotCount)
        {
            int remaining=count;
            for(int pass=0;pass<2;pass++)
            for(int i=start;i<end && remaining>0;i++)
            {
                var s=Slots[i];
                if(pass==0 ? s.Empty || s.Id!=id : !s.Empty) continue;
                int take=Math.Min(remaining,limit(id)-s.Count);
                Slots[i]=new ItemStack(id,s.Count+take);remaining-=take;
            }
            if(remaining!=count) Revision++;
            return remaining;
        }
        public ItemStack Take(int index,int count)
        {
            var s=Slots[index]; int taken=Math.Min(s.Count,Math.Max(count,0));
            if(taken==0) return default;
            Slots[index]=new ItemStack(s.Id,s.Count-taken);Revision++;
            return new ItemStack(s.Id,taken);
        }
        public void QuickTransfer(int index)
        {
            var s=Slots[index];if(s.Empty)return;
            int left=Add(s.Id,s.Count,index<12?12:0,index<12?60:12);
            Slots[index]=new ItemStack(s.Id,left);Revision++;
        }
        public void Click(int index,ref ItemStack held,bool right)
        {
            var s=Slots[index];
            if(held.Empty)
            {
                if(!s.Empty) held=Take(index,right?(s.Count+1)/2:s.Count);
                return;
            }
            if(s.Empty || s.Id==held.Id)
            {
                int n=Math.Min(right?1:held.Count,limit(held.Id)-s.Count);
                Slots[index]=new ItemStack(held.Id,s.Count+n);held=new ItemStack(held.Id,held.Count-n);
            }
            else if(!right) { Slots[index]=held; held=s; }
            Revision++;
        }
    }
}
