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
    public sealed partial class FluidSimulation
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
        static bool Displaceable(byte cell)=>cell==0||cell==BlockId.Torch||cell==BlockId.Sapling;
        void ScheduleAffected(IFluidWorld world,BlockPos p)
        {
            if(!world.TryRead(p,out byte cell)){Sleep(p,p.Chunk);return;}
            var own=registry.Get(cell);if(!Displaceable(cell)&&own==null)return;
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
            using var cost=RuntimeCosts.WorldFluids.Auto();
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
            var own=registry.Get(cell);if(!Displaceable(cell)&&own==null)return;
            // Sources remain explicit; unsupported sources still produce waterfalls.
            if(own!=null&&own.IsSource(cell))
            {
                if(TrySourceReaction(world,p,cell))return;
                Spread(world,p,cell,own);return;
            }
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
                if(Displaceable(floor)||registry.Get(floor)==f&&!f.IsSource(floor))continue;
                if(FlowsToward(world,neighbour,p,f,n))best=Math.Min(best,f.Level(n)+1);
            }
            if(!complete)return; // Closed unready frontiers never drain an otherwise supplied stream.
            if(type==null)return;
            byte next=0;
            bool support=!Displaceable(below)&&registry.Get(below)==null||type.IsSource(below);
            if(type.RenewsSources&&sources>=2&&support)next=type.Source;
            else if(registry.Get(above)==type)next=type.Falling;
            else if(best<=type.Reach)next=type.Flow(best);
            if(next!=cell&&(next!=0||cell!=BlockId.Torch&&cell!=BlockId.Sapling)&&world.ChangeFluid(p,cell,next))Changed(world,p);
            if(next!=0)Spread(world,p,next,type);
        }
        bool TrySourceReaction(IFluidWorld world,BlockPos p,byte cell)
        {
            if(cell!=Fluids.Lava.Source||registry.Get(cell)!=Fluids.Lava)return false;
            // Existing moving water must touch the source from above or from a side.
            // Source water alone and flowing lava are deliberately not this reaction.
            bool Incoming(BlockPos from)
                =>Read(world,from,p,out byte id)&&registry.Get(id)==Fluids.Water&&!Fluids.Water.IsSource(id);
            bool touches=Incoming(p.Offset(0,1,0));
            foreach(var d in Sides)touches|=Incoming(p.Offset(d.x,0,d.z));
            if(!touches||!world.ChangeFluid(p,cell,BlockId.LavaRock))return false;
            Changed(world,p);return true;
        }
        // Each direction starts one cell from the origin and looks ahead at
        // most four more cells. All reads fit an 11x11 square at Y and Y-1.
        // Scratch belongs to one synchronous query, never a world/tick cache.
        const int RouteWidth=11,RouteCells=RouteWidth*RouteWidth,RouteCenter=RouteCells/2;
        readonly byte[] routeOpen=new byte[RouteCells*2],routeVisited=new byte[RouteCells];
        readonly int[] routeQueue=new int[RouteCells];
        bool OpenRoute(IFluidWorld world,BlockPos from,int index,bool below,FluidDefinition f)
        {
            int cached=index+(below?RouteCells:0);byte value=routeOpen[cached];
            if(value==0)
            {
                var p=from.Offset(index%RouteWidth-5,below?-1:0,index/RouteWidth-5);
                bool open=world.TryRead(p,out byte id)&&(Displaceable(id)||registry.Get(id)==f&&!f.IsSource(id));
                routeOpen[cached]=value=open?(byte)2:(byte)1;
            }
            return value==2;
        }
        // Bounded BFS retains the same neighbour order and blocks return through
        // the origin. Per-direction stamps avoid clearing visited between routes.
        int DropDistance(IFluidWorld world,BlockPos from,int first,FluidDefinition f,int remaining,byte stamp)
        {
            int head=0,tail=1;routeQueue[0]=first;
            routeVisited[RouteCenter]=stamp;routeVisited[first]=stamp;
            while(head<tail)
            {
                int entry=routeQueue[head++],index=entry&127,distance=entry>>7;
                if(OpenRoute(world,from,index,true,f))return distance;
                if(distance==remaining)continue;
                foreach(var d in Sides)
                {
                    int next=index+d.x+d.z*RouteWidth;
                    if(routeVisited[next]==stamp||!OpenRoute(world,from,next,false,f))continue;
                    routeVisited[next]=stamp;routeQueue[tail++]=next|((distance+1)<<7);
                }
            }
            return 99;
        }
        int FlowDirections(IFluidWorld world,BlockPos from,FluidDefinition f,byte cell)
        {
            if(f.Level(cell)>=f.Reach)return 0;
            Array.Clear(routeOpen,0,routeOpen.Length);Array.Clear(routeVisited,0,routeVisited.Length);
            // Without a reachable drop every target ties at 99, including closed
            // targets; callers separately enforce compatibility and readiness.
            int best=99,mask=15;
            for(int side=0;side<Sides.Length;side++)
            {
                var d=Sides[side];int first=RouteCenter+d.x+d.z*RouteWidth;
                if(!OpenRoute(world,from,first,false,f))continue;
                int score=DropDistance(world,from,first,f,Math.Min(4,f.Reach-f.Level(cell)-1),(byte)(side+1));
                if(score<best){best=score;mask=1<<side;}
                else if(score==best)mask|=1<<side;
            }
            return mask;
        }
        bool FlowsToward(IFluidWorld world,BlockPos from,BlockPos target,FluidDefinition f,byte cell)
        {
            int mask=FlowDirections(world,from,f,cell);
            for(int side=0;side<Sides.Length;side++)
            {var d=Sides[side];if(from.Offset(d.x,0,d.z).Equals(target))return (mask&(1<<side))!=0;}
            return false; // Every caller supplies one of the four adjacent targets.
        }
        void Spread(IFluidWorld world,BlockPos p,byte cell,FluidDefinition f)
        {
            var down=p.Offset(0,-1,0);
            if(!Read(world,down,p,out byte floor))return;
            if(Displaceable(floor)||registry.Get(floor)==f&&!f.IsSource(floor)&&!f.IsFalling(floor))Wake(down,f.TickDelay);
            if(Displaceable(floor)||registry.Get(floor)==f&&!f.IsSource(floor))return;
            if(f.Level(cell)>=f.Reach)return;
            int directions=-1;
            for(int side=0;side<Sides.Length;side++)
            {
                var d=Sides[side];var n=p.Offset(d.x,0,d.z);
                if(!Read(world,n,p,out byte id))continue;
                if(!Displaceable(id)&&(registry.Get(id)!=f||f.IsSource(id)))continue;
                // Unchanged neighbours need no wake and therefore no route search.
                if(!Displaceable(id)&&f.Level(id)<=f.Level(cell)+1)continue;
                // Wake only schedules future work; no cells change during this
                // loop, so all neighbours share these same four route scores.
                if(directions<0)directions=FlowDirections(world,p,f,cell);
                if((directions&(1<<side))==0)continue;
                Wake(n,f.TickDelay);
            }
        }
    }
}
