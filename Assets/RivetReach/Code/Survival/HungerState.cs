using System;

namespace RivetReach
{
    // One player's authoritative food state. Rendering and input do not own food accounting.
    public sealed partial class HungerState
    {
        public const int Maximum=20;
        public int Food {get;private set;}=Maximum;
        public double Exhaustion {get;private set;}
        public bool CanSprint=>Food>6;
        public void Exert(double amount)
        {
            if(double.IsNaN(amount)||double.IsInfinity(amount)||amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
            Exhaustion+=amount;
            int spent=(int)Math.Min(Maximum,Math.Floor(Exhaustion/4));
            Food=Math.Max(0,Food-spent);Exhaustion%=4;
        }
        public bool TryEat(Inventory inventory,int slot,int points)
        {
            if(points<=0||Food>=Maximum||inventory.Slots[slot].Empty)return false;
            inventory.Take(slot,1);Food=Math.Min(Maximum,Food+points);return true;
        }
    }
}
