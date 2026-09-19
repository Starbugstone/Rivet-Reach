using System;

namespace RivetReach
{
    // One player's authoritative food state. Rendering and input do not own food accounting.
    public sealed partial class HungerState
    {
        public const int Maximum=20;
        // Alpha tuning: ordinary activity costs 25% less; healing keeps its full cost.
        public const double ActivityMultiplier=.75;
        public const double PassiveExhaustionPerTick=ActivityMultiplier/512;
        public const double WalkExhaustionPerMetre=.01*ActivityMultiplier;
        public const double SprintExhaustionPerMetre=.1*ActivityMultiplier;
        public const double JumpExhaustion=.2*ActivityMultiplier;
        public const double BlockActionExhaustion=.05*ActivityMultiplier;
        public const double HealingExhaustion=6;
        public int Food {get;private set;}=Maximum;
        public double Exhaustion {get;private set;}
        public int Saturation {get;private set;}
        public bool CanSprint=>Food>6;
        public void Exert(double amount)
        {
            if(double.IsNaN(amount)||double.IsInfinity(amount)||amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
            Exhaustion+=amount;
            int spent=(int)Math.Min(Maximum*2,Math.Floor(Exhaustion/4));
            int reserve=Math.Min(Saturation,spent);Saturation-=reserve;spent-=reserve;
            Food=Math.Max(0,Food-spent);Exhaustion%=4;
        }
        public bool TryEat(Inventory inventory,int slot,int points,int saturation=0)
        {
            if(saturation<0||saturation>Maximum)throw new ArgumentOutOfRangeException(nameof(saturation));
            if(points<=0||Food>=Maximum||inventory.Slots[slot].Empty)return false;
            inventory.Take(slot,1);Food=Math.Min(Maximum,Food+points);Saturation=Math.Min(Maximum,Saturation+saturation);return true;
        }
    }
}
