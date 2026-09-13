using System;

namespace RivetReach
{
    [Serializable]
    public struct ItemStack : IEquatable<ItemStack>
    {
        public byte Id;
        public int Count;
        public long Energy, FluidAmount, FluidCapacity;
        public byte FluidId;
        public bool Empty => Id == 0 || Count <= 0;
        public bool HasContents => Energy != 0 || FluidAmount != 0;
        public bool IsStorage => Id == IndustryId.Battery || Id == IndustryId.Tank || Id == IndustryId.TankController;
        public bool Equals(ItemStack other) => Id==other.Id && Count==other.Count && Energy==other.Energy && FluidAmount==other.FluidAmount && FluidCapacity==other.FluidCapacity && FluidId==other.FluidId;
        public override bool Equals(object other) => other is ItemStack stack && Equals(stack);
        public override int GetHashCode() => HashCode.Combine(Id,Count,Energy,FluidAmount,FluidCapacity,FluidId);
        public bool CanStack(ItemStack other) => Id == other.Id && !HasContents && !other.HasContents;
        public int Limit(int ordinary) => HasContents ? 1 : ordinary;
        public ItemStack WithCount(int count) { var copy=this; copy.Count=count; if(count<=0)copy.Clear(); return copy; }
        public ItemStack(byte id, int count)
        {
            Id = count > 0 ? id : (byte)0;
            Count = id == 0 ? 0 : Math.Max(0, count);
            Energy=FluidAmount=FluidCapacity=0;FluidId=0;
        }
        public bool ValidContents => Energy>=0 && FluidAmount>=0 && FluidCapacity>=0 &&
            (HasContents ? Count==1 && (Id==IndustryId.Battery
                ? Energy>0 && Energy<=BatteryStorage.CellCapacity && FluidAmount==0 && FluidCapacity==0 && FluidId==0
                : (Id==IndustryId.Tank || Id==IndustryId.TankController) && Energy==0 && FluidAmount>0 &&
                  FluidAmount<=FluidCapacity && FluidCapacity<=(Id==IndustryId.Tank?IndustryDefinition.All[Id].WaterCapacity:85750000) &&
                  (Id!=IndustryId.Tank || FluidCapacity==IndustryDefinition.All[Id].WaterCapacity) && Fluids.Registry.Get(FluidId)?.Source==FluidId)
                : Energy==0 && FluidAmount==0 && FluidCapacity==0 && FluidId==0);
        public string ContentsText => Energy>0 ? $"{Energy/1000000.0:0.######} / 100 kJ" : FluidAmount>0
            ? $"{Fluids.Registry.Get(FluidId).DisplayName}: {FluidAmount/1000.0:0.###} / {FluidCapacity/1000.0:0.###} L" : "Empty";
        public bool EmptyContents() { if(!IsStorage||!HasContents)return false;Energy=FluidAmount=FluidCapacity=0;FluidId=0;return true; }
        public void Clear() { this=default; }
    }
}
