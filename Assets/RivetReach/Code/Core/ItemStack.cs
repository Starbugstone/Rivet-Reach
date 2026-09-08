using System;

namespace RivetReach
{
    [Serializable]
    public struct ItemStack
    {
        public byte Id;
        public int Count;
        public bool Empty => Id == 0 || Count <= 0;
        public ItemStack(byte id, int count)
        {
            Id = count > 0 ? id : (byte)0;
            Count = id == 0 ? 0 : Math.Max(0, count);
        }
        public void Clear() { Id = 0; Count = 0; }
    }
}
