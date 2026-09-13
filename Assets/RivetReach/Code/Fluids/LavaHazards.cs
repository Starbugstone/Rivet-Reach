using System;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class HealthState
    {
        public const int BurnDuration=80; // Four seconds after leaving lava, at the shared 20 Hz clock.
        public int BurnTicks {get;private set;}
        public bool Burning=>BurnTicks>0;
        int heatCooldown;
        public void Extinguish(){BurnTicks=0;heatCooldown=0;}
        public void AdvanceHeat(int ticks,bool lava,bool water,Action<float,DamageKind> damage)
        {
            if(ticks<0)throw new ArgumentOutOfRangeException(nameof(ticks));
            if(water&&!lava){Extinguish();return;}
            for(int i=0;i<ticks&&!Dead;i++)
            {
                if(lava){if(!Burning)heatCooldown=0;BurnTicks=BurnDuration;heatCooldown=Math.Min(heatCooldown,10);}
                else if(BurnTicks>0)BurnTicks--;
                if(!Burning){heatCooldown=0;continue;}
                if(heatCooldown>0)heatCooldown--;
                if(heatCooldown==0){damage(lava?4:1,DamageKind.Heat);heatCooldown=lava?10:20;}
            }
        }
    }
    public sealed partial class VoxelWorld
    {
        // Actual body/surface overlap includes shallow flow and the edges of the player's feet.
        public bool TouchesFluid(Vector3 feet,float width,float height,FluidDefinition fluid)
        {
            var min=Address(feet+new Vector3(-width*.5f,.001f,-width*.5f));
            var max=Address(feet+new Vector3(width*.5f-.001f,height-.001f,width*.5f-.001f));
            for(long z=min.Z;z<=max.Z;z++)for(int y=min.Y;y<=max.Y;y++)for(long x=min.X;x<=max.X;x++)
            {
                var p=new BlockPos(x,y,z);if(!Ready(p))continue;
                byte id=Get(p);if(Fluids.Registry.Get(id)!=fluid)continue;
                float surface=Local(p).y+(Fluids.Registry.Get(Get(p.Offset(0,1,0)))==fluid?1:fluid.Height(id));
                if(feet.y+.001f<surface)return true;
            }
            return false;
        }
    }
    public sealed partial class Expedition
    {
        internal void AdvanceLava(int ticks)
        {
            if(ticks<=0||Creative||Paused||Health.Dead)return;
            bool lava=World.TouchesFluid(Player.transform.position,.6f,Player.Height,Fluids.Lava);
            bool water=World.TouchesFluid(Player.transform.position,.6f,Player.Height,Fluids.Water);
            Health.AdvanceHeat(ticks,lava,water,(amount,kind)=>TakeDamage(amount,kind));
        }
    }
}
