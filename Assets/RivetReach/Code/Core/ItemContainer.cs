using System;
using System.Collections.Generic;

namespace RivetReach
{
    // One local authority owns mutations. Callers receive value copies, never the backing array.
    public class ItemContainer
    {
        readonly ItemStack[] slots;
        readonly Func<byte, int> stackLimit;
        public IReadOnlyList<ItemStack> Slots { get; }
        public int Count => slots.Length;
        public long Revision { get; private set; }

        public ItemContainer(int count, Func<byte, int> stackLimit)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            this.stackLimit = stackLimit ?? throw new ArgumentNullException(nameof(stackLimit));
            slots = new ItemStack[count];
            Slots = Array.AsReadOnly(slots);
        }

        int Limit(byte id)
        {
            if (id == 0) throw new ArgumentOutOfRangeException(nameof(id));
            int limit = stackLimit(id);
            if (limit <= 0) throw new InvalidOperationException("Item stack limits must be positive.");
            return limit;
        }

        void Range(int start, int end)
        {
            if (start < 0 || end < start || end > Count) throw new ArgumentOutOfRangeException(nameof(start));
        }

        public int FindSlot(Predicate<ItemStack> predicate) => Array.FindIndex(slots, predicate);
        public int Total(byte id)
        {
            int total = 0;
            foreach (var stack in slots) if (stack.Id == id) total = checked(total + stack.Count);
            return total;
        }

        public int Capacity(byte id, int start = 0, int end = -1)
        {
            if (end == -1) end = Count;
            Range(start, end);
            int limit = Limit(id);
            return CapacityAtLimit(id, limit, start, end);
        }

        int CapacityAtLimit(byte id, int limit, int start, int end)
        {
            long capacity = 0;
            for (int i = start; i < end; i++)
                if (slots[i].Empty || slots[i].Id == id) capacity += Math.Max(0, limit - slots[i].Count);
            return (int)Math.Min(int.MaxValue, capacity);
        }

        // Returns the exact remainder; both passes use a single validated limit.
        public int Add(byte id, int count, int start = 0, int end = -1)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (end == -1) end = Count;
            Range(start, end);
            if (count == 0) return 0;
            return AddAtLimit(id, count, Limit(id), start, end);
        }

        // All-or-nothing insertion: resolve the limit once, check space, then publish.
        // Even a changed authoring limit cannot turn a crafting output into a partial write.
        public bool TryAddExact(byte id, int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            int limit = Limit(id);
            if (CapacityAtLimit(id, limit, 0, Count) < count) return false;
            AddAtLimit(id, count, limit, 0, Count);
            return true;
        }

        int AddAtLimit(byte id, int count, int limit, int start, int end)
        {
            int remaining = count;
            for (int pass = 0; pass < 2; pass++)
            for (int i = start; i < end && remaining > 0; i++)
            {
                var stack = slots[i];
                if (pass == 0 ? stack.Empty || stack.Id != id : !stack.Empty) continue;
                int take = Math.Min(remaining, Math.Max(0, limit - stack.Count));
                slots[i] = new ItemStack(id, stack.Count + take);
                remaining -= take;
            }
            if (remaining != count) Revision++;
            return remaining;
        }

        public bool CanReplaceSingle(int index,byte expected,byte replacement)
            =>slots[index].Id==expected&&slots[index].Count==1&&Limit(replacement)>=1;
        public bool ReplaceSingle(int index,byte expected,byte replacement)
        {
            if(!CanReplaceSingle(index,expected,replacement))return false;
            slots[index]=new ItemStack(replacement,1);Revision++;return true;
        }

        public ItemStack Take(int index, int count)
        {
            var stack = slots[index];
            int taken = Math.Min(stack.Count, Math.Max(0, count));
            if (taken == 0) return default;
            slots[index] = new ItemStack(stack.Id, stack.Count - taken);
            Revision++;
            return new ItemStack(stack.Id, taken);
        }

        public void Click(int index, ref ItemStack held, bool right)
        {
            var stack = slots[index];
            if (held.Empty)
            {
                if (!stack.Empty) held = Take(index, right ? stack.Count / 2 + stack.Count % 2 : stack.Count);
                return;
            }
            int limit = Limit(held.Id);
            if (held.Count > limit) throw new ArgumentOutOfRangeException(nameof(held));
            if (stack.Empty || stack.Id == held.Id)
            {
                int take = Math.Min(right ? 1 : held.Count, Math.Max(0, limit - stack.Count));
                if (take == 0) return;
                slots[index] = new ItemStack(held.Id, stack.Count + take);
                held = new ItemStack(held.Id, held.Count - take);
            }
            else if (!right) { slots[index] = held; held = stack; }
            else return;
            Revision++;
        }

        public void TransferTo(int index, ItemContainer destination, int start = 0, int end = -1)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (ReferenceEquals(this, destination)) throw new ArgumentException("Use disjoint inventory sections for an internal transfer.");
            var stack = slots[index];
            if (stack.Empty) return;
            int left = destination.Add(stack.Id, stack.Count, start, end);
            Take(index, stack.Count - left);
        }
    }

    public sealed class Inventory : ItemContainer
    {
        public const int HotbarCount = 12, MainCount = 48, SlotCount = 60;
        public Inventory(Func<byte, int> stackLimit) : base(SlotCount, stackLimit) { }

        public void QuickTransfer(int index)
        {
            var stack = Slots[index];
            if (stack.Empty) return;
            int left = Add(stack.Id, stack.Count, index < HotbarCount ? HotbarCount : 0,
                index < HotbarCount ? SlotCount : HotbarCount);
            Take(index, stack.Count - left);
        }
    }
}
