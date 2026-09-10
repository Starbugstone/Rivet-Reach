using System;
using System.Collections.Generic;

namespace RivetReach
{
    public interface IFluidWorld
    {
        bool TryRead(BlockPos position,out byte cell);
        bool ChangeFluid(BlockPos position,byte expected,byte replacement);
    }
    // One world authority; deterministic scheduled work, no scan of settled fluid volumes.
    public sealed class FluidSimulation
    {
        public const float StepSeconds=.05f;
        public const int WorkBudget=512;
        public static readonly (int x,int z)[] Sides={(1,0),(-1,0),(0,1),(0,-1)};
        readonly FluidRegistry registry;
        readonly SortedDictionary<long,Queue<BlockPos>> due=new SortedDictionary<long,Queue<BlockPos>>();
        readonly HashSet<BlockPos> scheduled=new HashSet<BlockPos>();
        readonly Dictionary<ChunkPos,HashSet<BlockPos>> sleeping=new Dictionary<ChunkPos,HashSet<BlockPos>>();
        long tick;
        public int Pending=>scheduled.Count;
        public int LastWork {get;private set;}
        public FluidSimulation(FluidRegistry registry){this.registry=registry??throw new ArgumentNullException(nameof(registry));}
        public void Wake(BlockPos p,int delay=5)
        {
            if(p.Y<=TerrainGenerator.MinY||p.Y>TerrainGenerator.MaxY||Math.Abs(p.X)>TerrainGenerator.HorizontalLimit||Math.Abs(p.Z)>TerrainGenerator.HorizontalLimit||!scheduled.Add(p))return;
            long when=tick+delay;if(!due.TryGetValue(when,out var queue)){queue=new Queue<BlockPos>();due.Add(when,queue);}queue.Enqueue(p);
        }
        public void Changed(IFluidWorld world,BlockPos p)
        {
            ScheduleAffected(world,p);ScheduleAffected(world,p.Offset(0,1,0));ScheduleAffected(world,p.Offset(0,-1,0));
            foreach(var d in Sides)ScheduleAffected(world,p.Offset(d.x,0,d.z));
        }
        void ScheduleAffected(IFluidWorld world,BlockPos p)
        {
            if(!world.TryRead(p,out byte cell)){Sleep(p,p.Chunk);return;}
            var own=registry.Get(cell);if(cell!=0&&own==null)return;
            int delay=own?.TickDelay??int.MaxValue;
            void Neighbour(BlockPos n)
            {if(world.TryRead(n,out byte id)&&registry.Get(id) is FluidDefinition f)delay=Math.Min(delay,f.TickDelay);}
            if(own==null)
            {
                Neighbour(p.Offset(0,1,0));Neighbour(p.Offset(0,-1,0));
                foreach(var d in Sides)Neighbour(p.Offset(d.x,0,d.z));
            }
            if(delay!=int.MaxValue)Wake(p,delay);
        }
        public void Ready(ChunkPos chunk)
        {
            if(!sleeping.TryGetValue(chunk,out var pending))return;
            sleeping.Remove(chunk);foreach(var p in pending)Wake(p,1);
        }
        void Sleep(BlockPos p,ChunkPos chunk)
        {if(!sleeping.TryGetValue(chunk,out var set)){set=new HashSet<BlockPos>();sleeping.Add(chunk,set);}set.Add(p);}
        bool Read(IFluidWorld world,BlockPos p,BlockPos requester,out byte cell)
        {if(world.TryRead(p,out cell))return true;Sleep(requester,p.Chunk);return false;}
        public void Step(IFluidWorld world)
        {
            tick++;LastWork=0;
            while(LastWork<WorkBudget&&due.Count>0)
            {
                var iterator=due.GetEnumerator();iterator.MoveNext();var entry=iterator.Current;iterator.Dispose();
                if(entry.Key>tick)break;
                var p=entry.Value.Dequeue();if(entry.Value.Count==0)due.Remove(entry.Key);scheduled.Remove(p);LastWork++;
                Evaluate(world,p);
            }
        }
        void Evaluate(IFluidWorld world,BlockPos p)
        {
            if(!Read(world,p,p,out byte cell))return;
            var own=registry.Get(cell);if(cell!=0&&own==null)return;
            // Sources remain explicit; unsupported sources still produce waterfalls.
            if(own!=null&&own.IsSource(cell)){Spread(world,p,cell,own);return;}
            if(!Read(world,p.Offset(0,1,0),p,out byte above)||!Read(world,p.Offset(0,-1,0),p,out byte below))return;
            var type=own??registry.Get(above);int best=99,sources=0;bool complete=true;
            foreach(var d in Sides)
            {
                var neighbour=p.Offset(d.x,0,d.z);
                if(!Read(world,neighbour,p,out byte n)){complete=false;continue;}
                var f=registry.Get(n);if(f==null)continue;if(type==null)type=f;if(type!=f)continue;
                if(f.IsSource(n))sources++;
                // A falling stream spreads sideways only on a supported level.
                if(!Read(world,neighbour.Offset(0,-1,0),p,out byte floor)){complete=false;continue;}
                if(floor==0||registry.Get(floor)==f&&!f.IsSource(floor))continue;
                if(FlowsToward(world,neighbour,p,f,n))best=Math.Min(best,f.Level(n)+1);
            }
            if(!complete)return; // Closed unready frontiers never drain an otherwise supplied stream.
            if(type==null)return;
            byte next=0;
            bool support=below!=0&&registry.Get(below)==null||type.IsSource(below);
            if(type.RenewsSources&&sources>=2&&support)next=type.Source;
            else if(registry.Get(above)==type)next=type.Falling;
            else if(best<=type.Reach)next=type.Flow(best);
            if(next!=cell&&world.ChangeFluid(p,cell,next))Changed(world,p);
            if(next!=0)Spread(world,p,next,type);
        }
        bool Open(IFluidWorld world,BlockPos p,FluidDefinition f)
            =>world.TryRead(p,out byte id)&&(id==0||registry.Get(id)==f&&!f.IsSource(id));
        // Bounded lookahead chooses equally short routes to a drop, otherwise spreads on the flat.
        readonly Queue<(BlockPos p,int distance)> routes=new Queue<(BlockPos,int)>();
        readonly HashSet<BlockPos> visited=new HashSet<BlockPos>();
        int DropDistance(IFluidWorld world,BlockPos p,BlockPos from,FluidDefinition f,int remaining)
        {
            routes.Clear();visited.Clear();visited.Add(from);visited.Add(p);routes.Enqueue((p,0));
            while(routes.Count>0)
            {
                var node=routes.Dequeue();
                if(Open(world,node.p.Offset(0,-1,0),f))return node.distance;
                if(node.distance==remaining)continue;
                foreach(var d in Sides)
                {
                    var n=node.p.Offset(d.x,0,d.z);
                    if(visited.Contains(n)||!Open(world,n,f))continue;
                    visited.Add(n);routes.Enqueue((n,node.distance+1));
                }
            }
            return 99;
        }
        bool FlowsToward(IFluidWorld world,BlockPos from,BlockPos target,FluidDefinition f,byte cell)
        {
            if(f.Level(cell)>=f.Reach)return false;
            int best=99,targetScore=99;
            foreach(var d in Sides)
            {
                var n=from.Offset(d.x,0,d.z);if(!Open(world,n,f))continue;
                int score=DropDistance(world,n,from,f,Math.Min(4,f.Reach-f.Level(cell)-1));
                best=Math.Min(best,score);if(n.Equals(target))targetScore=score;
            }
            return targetScore==best;
        }
        void Spread(IFluidWorld world,BlockPos p,byte cell,FluidDefinition f)
        {
            var down=p.Offset(0,-1,0);
            if(!Read(world,down,p,out byte floor))return;
            if(floor==0||registry.Get(floor)==f&&!f.IsSource(floor)&&!f.IsFalling(floor))Wake(down,f.TickDelay);
            if(floor==0||registry.Get(floor)==f&&!f.IsSource(floor))return;
            if(f.Level(cell)>=f.Reach)return;
            foreach(var d in Sides)
            {
                var n=p.Offset(d.x,0,d.z);
                if(!Read(world,n,p,out byte id))continue;
                if((id==0||registry.Get(id)==f&&!f.IsSource(id))&&FlowsToward(world,p,n,f,cell))
                {
                    // Unchanged neighbours sleep; only weaker/empty cells need propagation.
                    if(id==0||f.Level(id)>f.Level(cell)+1)Wake(n,f.TickDelay);
                }
            }
        }
    }
}
