using System;
using System.Collections.Generic;

namespace RivetReach
{
    public interface ITreeWorld
    {
        byte Get(BlockPos p);
        bool NaturalLeaf(BlockPos p);
        bool NaturalLog(BlockPos p);
        bool RemoveTreeBlock(BlockPos p,byte expected,bool drop);
    }

    // Event-driven tree work; no per-tree objects or whole-world leaf scans.
    public sealed partial class TreeSimulation
    {
        public const int SupportDistance=4,WorkPerStep=32,LeafWorkPerStep=8;
        public const float StepSeconds=.1f;
        static readonly (int x,int y,int z)[] neighbours={(1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1)};
        sealed class Fell
        {
            public int CutY;
            public readonly Queue<BlockPos> Pending=new Queue<BlockPos>();
            public readonly HashSet<BlockPos> Seen=new HashSet<BlockPos>();
        }
        readonly Queue<Fell> felling=new Queue<Fell>();
        readonly Queue<(BlockPos p,long due)> leaves=new Queue<(BlockPos,long)>();
        readonly HashSet<BlockPos> scheduledLeaves=new HashSet<BlockPos>();
        readonly Queue<(BlockPos p,int distance)> supportQueue=new Queue<(BlockPos,int)>();
        readonly HashSet<BlockPos> supportSeen=new HashSet<BlockPos>();
        long tick;
        public int PendingFells=>felling.Count;
        public int PendingLeaves=>leaves.Count;
        public void FellAbove(ITreeWorld world,BlockPos cut)
        {
            var job=new Fell{CutY=cut.Y};job.Seen.Add(cut);
            EnqueueNeighbours(world,job,cut);if(job.Pending.Count>0)felling.Enqueue(job);
        }
        static void EnqueueNeighbours(ITreeWorld world,Fell job,BlockPos p)
        {
            foreach(var n in neighbours)
            {
                var next=p.Offset(n.x,n.y,n.z);
                if(next.Y<job.CutY||next.Y>TerrainGenerator.MaxY||!job.Seen.Add(next))continue;
                if(world.NaturalLog(next))job.Pending.Enqueue(next);
            }
        }
        public void SupportRemoved(ITreeWorld world,BlockPos p)
        {
            for(int z=-SupportDistance;z<=SupportDistance;z++)for(int y=-SupportDistance;y<=SupportDistance;y++)for(int x=-SupportDistance;x<=SupportDistance;x++)
            {
                if(Math.Abs(x)+Math.Abs(y)+Math.Abs(z)>SupportDistance)continue;
                var leaf=p.Offset(x,y,z);
                if(world.NaturalLeaf(leaf)&&scheduledLeaves.Add(leaf))leaves.Enqueue((leaf,tick+20));
            }
        }
        bool Supported(ITreeWorld world,BlockPos leaf)
        {
            var queue=supportQueue;var seen=supportSeen;queue.Clear();seen.Clear();seen.Add(leaf);queue.Enqueue((leaf,0));
            while(queue.Count>0)
            {
                var current=queue.Dequeue();
                foreach(var n in neighbours)
                {
                    var next=current.p.Offset(n.x,n.y,n.z);if(!seen.Add(next))continue;
                    byte id=world.Get(next);if(id==BlockId.Log)return true;
                    if(id==BlockId.Leaves&&current.distance+1<SupportDistance)queue.Enqueue((next,current.distance+1));
                }
            }
            return false;
        }
        public void Step(ITreeWorld world)
        {
            tick++;
            for(int i=0;i<WorkPerStep&&felling.Count>0;i++)
            {
                var job=felling.Peek();var p=job.Pending.Dequeue();
                if(world.NaturalLog(p)&&world.RemoveTreeBlock(p,BlockId.Log,true))EnqueueNeighbours(world,job,p);
                if(job.Pending.Count==0)felling.Dequeue();
            }
            for(int i=0;i<LeafWorkPerStep&&leaves.Count>0&&leaves.Peek().due<=tick;i++)
            {
                var candidate=leaves.Dequeue();scheduledLeaves.Remove(candidate.p);
                if(world.NaturalLeaf(candidate.p)&&!Supported(world,candidate.p))world.RemoveTreeBlock(candidate.p,BlockId.Leaves,false);
            }
        }
    }
}
