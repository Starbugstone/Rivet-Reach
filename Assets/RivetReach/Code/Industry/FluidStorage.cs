using System;

namespace RivetReach
{
    // Exact quantities and stable identities shared by small vessels and multiblock endpoints.
    public sealed class FluidStorage
    {
        public FluidDefinition Fluid {get;private set;}
        public long Amount {get;private set;}
        public long Capacity {get;private set;}
        public FluidStorage(long capacity){if(capacity<0)throw new ArgumentOutOfRangeException(nameof(capacity));Capacity=capacity;}
        public bool Accepts(FluidDefinition fluid)=>fluid!=null&&(Fluid==null||Fluid.StableId==fluid.StableId);
        public bool Deposit(FluidDefinition fluid,long amount)
        {if(amount<=0||!Accepts(fluid)||amount>Capacity-Amount)return false;Fluid=fluid;Amount+=amount;return true;}
        public bool Withdraw(long amount)
        {if(amount<=0||amount>Amount)return false;Amount-=amount;if(Amount==0)Fluid=null;return true;}
        public bool Resize(long capacity){if(capacity<Amount||capacity<0)return false;Capacity=capacity;return true;}
        // Water-only legacy machine adapters still go through exact storage accounting.
        internal void SetWater(int value)
        {if(value<0||value>Capacity||Fluid!=null&&Fluid.StableId!=Fluids.Water.StableId)throw new InvalidOperationException("Invalid machine water amount");Amount=value;Fluid=value==0?null:Fluids.Water;}
    }
}
