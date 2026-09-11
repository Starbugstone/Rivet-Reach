using System;
using System.Collections.Generic;

namespace RivetReach
{
    public interface IGrassWorld
    {
        bool TryRead(BlockPos position,out byte id);
        byte SkyLight(BlockPos air);
        bool ChangeGrass(BlockPos position,byte expected,byte replacement);
    }

    // World-addressed random ticks, independent of render frames and mesh job completion.
    // Three samples per 16^3 section; no per-block objects or scans of all resident voxels.
    public sealed partial class GrassSimulation
    {
        public const float StepSeconds=.05f;
        public long Tick {get;private set;}
        public int LastSamples {get;private set;}
        public int Changes {get;private set;}
        readonly int seed;
        readonly List<(BlockPos position,byte expected,byte replacement)> pending=new List<(BlockPos,byte,byte)>();
        readonly HashSet<BlockPos> queued=new HashSet<BlockPos>();
        public GrassSimulation(int seed){this.seed=seed;}
        static uint Next(ref uint state)
        {state^=state<<13;state^=state>>17;state^=state<<5;return state;}
        public void Step(IGrassWorld world,IReadOnlyList<ChunkPos> eligible)
        {
            Tick++;LastSamples=0;pending.Clear();queued.Clear();
            foreach(var chunk in eligible)
            {
                uint state=TerrainGenerator.Hash(chunk.X,Tick,chunk.Z,seed^chunk.Y*7919)|1u;
                var min=chunk.Min;
                for(int section=0;section<8;section++)for(int sample=0;sample<3;sample++)
                {
                    var p=min.Offset((section&1)*16+(int)(Next(ref state)%16),((section>>1)&1)*16+(int)(Next(ref state)%16),((section>>2)&1)*16+(int)(Next(ref state)%16));
                    LastSamples++;Evaluate(world,p,ref state);
                }
            }
            // Publish only after sampling: new grass cannot spread again in this same tick.
            foreach(var change in pending)
                if(world.ChangeGrass(change.position,change.expected,change.replacement))Changes++;
        }
        void Evaluate(IGrassWorld world,BlockPos p,ref uint state)
        {
            if(!world.TryRead(p,out byte id)||id!=1)return;
            var above=p.Offset(0,1,0);
            if(!world.TryRead(above,out byte roof))return;
            if(roof!=0&&!BlockId.Crop(roof)||world.SkyLight(above)<4){Queue(p,1,2);return;}
            if(world.SkyLight(above)<9)return;
            for(int attempt=0;attempt<4;attempt++)
            {
                var target=p.Offset((int)(Next(ref state)%3)-1,(int)(Next(ref state)%5)-3,(int)(Next(ref state)%3)-1);
                if(!world.TryRead(target,out byte dirt)||dirt!=2)continue;
                var top=target.Offset(0,1,0);
                if(world.TryRead(top,out byte cover)&&cover==0&&world.SkyLight(top)>=9)Queue(target,2,1);
            }
        }
        void Queue(BlockPos p,byte from,byte to)
        {
            // Bound changed-chunk pressure in dense fixtures; later random ticks retry omitted cells.
            if(pending.Count<8&&queued.Add(p))pending.Add((p,from,to));
        }
    }
}
