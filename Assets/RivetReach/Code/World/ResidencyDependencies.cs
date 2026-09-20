using System.Collections.Generic;

namespace RivetReach
{
    // Registers an owner's cell and its six face neighbours. Shared pages remain
    // watched until their last owner is removed; queries allocate nothing.
    public sealed class ResidencyDependencies
    {
        readonly Dictionary<ChunkPos,int> references=new Dictionary<ChunkPos,int>();
        public bool Contains(ChunkPos chunk)=>references.ContainsKey(chunk);
        public void Add(BlockPos position)=>Change(position,1);
        public void Remove(BlockPos position)=>Change(position,-1);
        void Change(BlockPos position,int delta)
        {
            var chunk=position.Chunk;Change(chunk,delta);
            int x=(int)(position.X-chunk.X*32),y=position.Y-chunk.Y*32,z=(int)(position.Z-chunk.Z*32);
            if(x==0)Change(chunk.Offset(-1,0,0),delta);else if(x==31)Change(chunk.Offset(1,0,0),delta);
            if(y==0)Change(chunk.Offset(0,-1,0),delta);else if(y==31)Change(chunk.Offset(0,1,0),delta);
            if(z==0)Change(chunk.Offset(0,0,-1),delta);else if(z==31)Change(chunk.Offset(0,0,1),delta);
        }
        void Change(ChunkPos chunk,int delta)
        {
            references.TryGetValue(chunk,out int count);count+=delta;
            if(count<=0)references.Remove(chunk);else references[chunk]=count;
        }
    }
}
