using System;

namespace RivetReach
{
    public static class FishId
    {
        public const byte Rod=244,Raw=245,Cooked=246,Stew=247;
        public static bool Added(byte id)=>id>=Rod&&id<=Stew;
    }
    public enum FishingPhase { Idle, Waiting, Bite }
    // One bounded transaction per player. No fish population, background timer or voxel mutation.
    public sealed class FishingCast
    {
        public const int MinimumWait=160,MaximumWait=320,BiteTicks=60;
        public FishingPhase Phase {get;private set;}
        public BlockPos Target {get;private set;}
        public int Remaining {get;private set;}
        public bool Active=>Phase!=FishingPhase.Idle;
        public void Start(BlockPos target,int wait)
        {
            if(Active||wait<MinimumWait||wait>MaximumWait)throw new ArgumentException("Invalid fishing cast.");
            Target=target;Remaining=wait;Phase=FishingPhase.Waiting;
        }
        public void Cancel(){Phase=FishingPhase.Idle;Remaining=0;}
        public void Advance(int ticks)
        {
            if(ticks<0)throw new ArgumentOutOfRangeException(nameof(ticks));
            if(!Active)return;
            if(ticks<Remaining){Remaining-=ticks;return;}
            ticks-=Remaining;
            if(Phase==FishingPhase.Bite||ticks>=BiteTicks){Cancel();return;}
            Phase=FishingPhase.Bite;Remaining=BiteTicks-ticks;
        }
        public bool Reel()
        {bool caught=Phase==FishingPhase.Bite;Cancel();return caught;}
    }
    public static class FishingWater
    {
        public const int Radius=3,Width=Radius*2+1;
        // Two source-water layers in a 7x7 square, with an exposed surface. Check residency first.
        public static bool Suitable(BlockPos centre,Func<BlockPos,bool> resident,Func<BlockPos,byte> get)
        {
            for(int x=-Radius;x<=Radius;x++)for(int z=-Radius;z<=Radius;z++)for(int y=-1;y<=1;y++)
            {
                var cell=centre.Offset(x,y,z);
                if(!resident(cell)||get(cell)!=(y==1?BlockId.Air:Fluids.Water.Source))return false;
            }
            return true;
        }
    }
}
