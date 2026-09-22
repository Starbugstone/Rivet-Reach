using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace RivetReach.Editor
{
    public static class FluidRoutingChecks
    {
        sealed class World:IFluidWorld
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public readonly HashSet<ChunkPos> Unloaded=new HashSet<ChunkPos>();
            public readonly List<(BlockPos position,byte before,byte after)> Changes=new List<(BlockPos,byte,byte)>();
            public Action<IFluidWorld,BlockPos> Notify;
            public long Reads;
            public byte Get(BlockPos p)=>Cells.TryGetValue(p,out byte id)?id:p.Y<=0?BlockId.Stone:(byte)0;
            public bool TryRead(BlockPos p,out byte id){Reads++;id=Get(p);return !Unloaded.Contains(p.Chunk);}
            public bool ChangeFluid(BlockPos p,byte expected,byte replacement)
            {
                if(!TryRead(p,out byte cell)||cell!=expected)return false;
                Cells[p]=replacement;Changes.Add((p,expected,replacement));Notify?.Invoke(this,p);return true;
            }
            public void Put(BlockPos p,byte id){Cells[p]=id;Notify?.Invoke(this,p);}
        }
        sealed class Pair
        {
            public readonly World Current=new World(),Prior=new World();
            public readonly FluidSimulation Simulation=new FluidSimulation(Fluids.Registry);
            public readonly LegacyFluidRouting Reference=new LegacyFluidRouting(Fluids.Registry);
            public Pair(){Current.Notify=Simulation.Changed;Prior.Notify=Reference.Changed;}
            public void Put(BlockPos p,byte id){Current.Put(p,id);Prior.Put(p,id);}
            public void Sleep(ChunkPos chunk,bool sleeping)
            {
                if(sleeping){Current.Unloaded.Add(chunk);Prior.Unloaded.Add(chunk);}
                else{Current.Unloaded.Remove(chunk);Prior.Unloaded.Remove(chunk);Simulation.Ready(chunk);Reference.Ready(chunk);}
            }
        }
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static Func<IFluidWorld,BlockPos,BlockPos,FluidDefinition,byte,bool> Route(object simulation)
            =>(Func<IFluidWorld,BlockPos,BlockPos,FluidDefinition,byte,bool>)simulation.GetType().GetMethod("FlowsToward",Hidden)
                .CreateDelegate(typeof(Func<IFluidWorld,BlockPos,BlockPos,FluidDefinition,byte,bool>),simulation);
        static Action<IFluidWorld,BlockPos,byte,FluidDefinition> Spread(object simulation)
            =>(Action<IFluidWorld,BlockPos,byte,FluidDefinition>)simulation.GetType().GetMethod("Spread",Hidden)
                .CreateDelegate(typeof(Action<IFluidWorld,BlockPos,byte,FluidDefinition>),simulation);
        public static void Run()
        {
            var lines=new List<string>();int assertions=0;
            void Check(bool valid,string message){if(!valid)throw new InvalidOperationException("Fluid routing: "+message);assertions++;}
            bool SameCells(World a,World b)
            {
                if(a.Cells.Count!=b.Cells.Count)return false;
                foreach(var pair in a.Cells)if(!b.Cells.TryGetValue(pair.Key,out byte value)||pair.Value!=value)return false;
                return true;
            }
            bool SameChanges(World a,World b)
            {
                if(a.Changes.Count!=b.Changes.Count)return false;
                for(int i=0;i<a.Changes.Count;i++)if(!a.Changes[i].Equals(b.Changes[i]))return false;
                return true;
            }
            try
            {
                var from=new BlockPos(-1,1,-1);var random=new Random(246813);
                var current=Route(new FluidSimulation(Fluids.Registry));var prior=Route(new LegacyFluidRouting(Fluids.Registry));
                for(int fixture=0;fixture<48;fixture++)
                {
                    var world=new World();
                    // Radius exceeds the four-cell search and crosses negative
                    // coordinate seams. Mix floor drops, obstructions and liquids.
                    for(int z=-6;z<=6;z++)for(int x=-6;x<=6;x++)
                    {
                        var p=from.Offset(x,0,z);
                        int cell=fixture==0?0:random.Next(12);
                        world.Cells[p]=cell==1?BlockId.Stone:cell==2?Fluids.Water.Source:cell==3?Fluids.Lava.Source:
                            cell==4?Fluids.Water.Flow(2):cell==5?Fluids.Lava.Flow(2):(byte)0;
                        if(fixture>0&&random.Next(6)==0)world.Cells[p.Offset(0,-1,0)]=0;
                    }
                    if(fixture%3==1)world.Unloaded.Add(from.Offset(1,0,0).Chunk);
                    foreach(var fluid in new[]{Fluids.Water,Fluids.Lava})
                        foreach(byte cell in new[]{fluid.Source,fluid.Falling,fluid.Flow(1),fluid.Flow(fluid.Reach)})
                        {
                            world.Cells[from]=cell;
                            foreach(var side in FluidSimulation.Sides)
                            {
                                var target=from.Offset(side.x,0,side.z);
                                world.Reads=0;bool actual=current(world,from,target,fluid,cell);long reads=world.Reads;
                                world.Reads=0;bool expected=prior(world,from,target,fluid,cell);
                                Check(actual==expected,"Direction differs from prior routing at fixture "+fixture+" / "+fluid.DisplayName+" / "+cell+" / "+target);
                                Check(reads<=242&&reads<=world.Reads,"A local direction query stays within two 11x11 planes and never adds world reads");
                            }
                        }
                }
                lines.Add("PASS 1536 adjacent-direction comparisons preserve flat/no-drop ties, holes, obstacles, source exclusions, mixed liquids and unready seams");
                {
                    var world=new World();var source=new BlockPos(31,1,-1);
                    void CompareQuery(string name)
                    {
                        foreach(var side in FluidSimulation.Sides)
                        {
                            var target=source.Offset(side.x,0,side.z);
                            Check(current(world,source,target,Fluids.Water,Fluids.Water.Source)==prior(world,source,target,Fluids.Water,Fluids.Water.Source),name);
                        }
                    }
                    CompareQuery("Fresh flat query agrees with the prior algorithm");
                    foreach(var dropSide in FluidSimulation.Sides)
                    {
                        world.Cells.Clear();var edge=source.Offset(dropSide.x*5,-1,dropSide.z*5);world.Cells[edge]=0;
                        foreach(var targetSide in FluidSimulation.Sides)
                        {
                            var target=source.Offset(targetSide.x,0,targetSide.z);
                            bool actual=current(world,source,target,Fluids.Water,Fluids.Water.Source);
                            Check(actual==prior(world,source,target,Fluids.Water,Fluids.Water.Source)
                                &&actual==(dropSide.x==targetSide.x&&dropSide.z==targetSide.z),
                                "Maximum four-step lookahead reaches exactly its five-cell local edge without row wrap");
                        }
                        world.Cells.Remove(edge);world.Cells[source.Offset(dropSide.x*6,-1,dropSide.z*6)]=0;
                        foreach(var targetSide in FluidSimulation.Sides)
                            Check(current(world,source,source.Offset(targetSide.x,0,targetSide.z),Fluids.Water,Fluids.Water.Source),
                                "A drop beyond the bounded lookahead cannot influence a direction");
                    }
                    world.Cells.Clear();world.Cells[source.Offset(5,-1,0)]=0;
                    CompareQuery("A new floor opening invalidates the next query's local cache");
                    var page=source.Offset(1,0,0).Chunk;world.Unloaded.Add(page);
                    CompareQuery("A readiness change invalidates the next query's local cache");
                    world.Unloaded.Remove(page);world.Cells[source.Offset(1,0,0)]=Fluids.Lava.Source;
                    CompareQuery("An opposite-fluid source invalidates the next query's local cache");
                    world.Cells.Clear();world.Reads=0;
                    current(world,source,source.Offset(1,0,0),Fluids.Water,Fluids.Water.Source);long optimized=world.Reads;
                    world.Reads=0;prior(world,source,source.Offset(1,0,0),Fluids.Water,Fluids.Water.Source);
                    Check(optimized*2<world.Reads,"One direction decision shares its four BFS read caches and cuts flat lookahead reads by more than half");
                    lines.Add("PASS local-query bounds/mutation/readiness equivalence; flat decision reads current="+optimized+", prior="+world.Reads);
                }
                {
                    var pair=new Pair();var source=new BlockPos(0,1,0);pair.Put(source,Fluids.Water.Source);
                    pair.Current.Reads=pair.Prior.Reads=0;
                    Spread(pair.Simulation)(pair.Current,source,Fluids.Water.Source,Fluids.Water);
                    Spread(pair.Reference)(pair.Prior,source,Fluids.Water.Source,Fluids.Water);
                    Check(pair.Current.Reads*2<pair.Prior.Reads,"One flat four-way Spread reduces routing reads by more than half");
                    Check(pair.Simulation.Pending==pair.Reference.Pending,"Spread preserves scheduled work while sharing direction scores");
                    lines.Add("PASS flat Spread reads current="+pair.Current.Reads+", prior="+pair.Prior.Reads+"; exact same pending work");
                    foreach(var side in FluidSimulation.Sides)
                    {
                        var target=source.Offset(side.x,0,side.z);
                        pair.Current.Cells[target]=pair.Prior.Cells[target]=Fluids.Water.Flow(1);
                    }
                    pair.Current.Reads=pair.Prior.Reads=0;
                    Spread(pair.Simulation)(pair.Current,source,Fluids.Water.Source,Fluids.Water);
                    Spread(pair.Reference)(pair.Prior,source,Fluids.Water.Source,Fluids.Water);
                    Check(pair.Current.Reads==5&&pair.Current.Reads<pair.Prior.Reads&&pair.Simulation.Pending==pair.Reference.Pending,
                        "Already supplied neighbours only read their four cells and support, without unnecessary lookahead or changed scheduling");
                    lines.Add("PASS unchanged-neighbour Spread reads current="+pair.Current.Reads+", prior="+pair.Prior.Reads);
                }
                void Trace(string name,Action<Pair> setup,Action<Pair,int> edit,int ticks)
                {
                    var pair=new Pair();setup(pair);
                    for(int tick=0;tick<ticks;tick++)
                    {
                        edit?.Invoke(pair,tick);pair.Current.Changes.Clear();pair.Prior.Changes.Clear();
                        pair.Simulation.Step(pair.Current);pair.Reference.Step(pair.Prior);
                        Check(SameCells(pair.Current,pair.Prior),name+" cell state differs at tick "+tick);
                        Check(SameChanges(pair.Current,pair.Prior),name+" mutation order differs at tick "+tick);
                        Check(pair.Simulation.Pending==pair.Reference.Pending&&pair.Simulation.LastWork==pair.Reference.LastWork
                            &&pair.Simulation.LastWork<=FluidSimulation.WorkBudget,name+" timing/work queue differs at tick "+tick);
                    }
                    long savedReads=pair.Prior.Reads-pair.Current.Reads;
                    Check(savedReads>=0,name+" adds no world reads");
                    lines.Add("PASS "+name+": "+ticks+" identical tick states, mutation sequences, pending/work counts; reads avoided="+savedReads);
                }
                var origin=new BlockPos(0,1,0);
                Trace("flat source and drain",p=>p.Put(origin,Fluids.Water.Source),(p,t)=>{if(t==65)p.Put(origin,0);},160);
                Trace("equal drop routes and changed dam",p=>
                {
                    p.Put(origin.Offset(2,-1,0),0);p.Put(origin.Offset(-2,-1,0),0);
                    for(int z=-3;z<=3;z++)p.Put(origin.Offset(0,0,z+2),BlockId.Stone);
                    p.Put(origin,Fluids.Water.Source);
                },(p,t)=>{if(t==45)p.Put(origin.Offset(1,0,0),BlockId.Stone);if(t==75)p.Put(origin.Offset(1,0,0),0);},130);
                Trace("renewing source support",p=>{p.Put(origin,Fluids.Water.Source);p.Put(origin.Offset(2,0,0),Fluids.Water.Source);},
                    (p,t)=>{if(t==50)p.Put(origin.Offset(1,0,0),0);if(t==90)p.Put(origin.Offset(1,-1,0),0);},140);
                var seam=new BlockPos(31,1,0);var chunk=seam.Offset(1,0,0).Chunk;
                Trace("dormant frontier and reload",p=>{p.Sleep(chunk,true);p.Put(seam,Fluids.Water.Source);},
                    (p,t)=>{if(t==40)p.Sleep(chunk,false);if(t==75)p.Sleep(chunk,true);if(t==105)p.Sleep(chunk,false);if(t==130)p.Put(seam,0);},180);
                Trace("mixed water lava source reaction",p=>{p.Put(origin,Fluids.Water.Source);p.Put(origin.Offset(3,0,0),Fluids.Lava.Source);},null,180);
                Trace("falling lava and removal",p=>p.Put(origin.Offset(0,5,0),Fluids.Lava.Source),
                    (p,t)=>{if(t==150)p.Put(origin.Offset(0,5,0),0);},240);
                lines.Insert(0,"PASS "+assertions+" assertions; prior algorithm is a frozen test-only equivalence oracle, not a second runtime implementation");
            }
            catch(Exception error){lines.Add("FAIL "+error);throw;}
            finally{Directory.CreateDirectory("Logs/ReleaseReview");File.WriteAllLines("Logs/ReleaseReview/fluid-routing-checks.txt",lines);}
        }
    }
}

namespace RivetReach.Editor
{
    // Frozen before the direction-mask optimization. Keep this independent oracle
    // unchanged so routing, wake order and per-tick work regressions stay observable.
    // One world authority; deterministic scheduled work, no scan of settled fluid volumes.
    sealed class LegacyFluidRouting
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
        public LegacyFluidRouting(FluidRegistry registry){this.registry=registry??throw new ArgumentNullException(nameof(registry));}
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
        bool Open(IFluidWorld world,BlockPos p,FluidDefinition f)
            =>world.TryRead(p,out byte id)&&(Displaceable(id)||registry.Get(id)==f&&!f.IsSource(id));
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
            if(Displaceable(floor)||registry.Get(floor)==f&&!f.IsSource(floor)&&!f.IsFalling(floor))Wake(down,f.TickDelay);
            if(Displaceable(floor)||registry.Get(floor)==f&&!f.IsSource(floor))return;
            if(f.Level(cell)>=f.Reach)return;
            foreach(var d in Sides)
            {
                var n=p.Offset(d.x,0,d.z);
                if(!Read(world,n,p,out byte id))continue;
                if((Displaceable(id)||registry.Get(id)==f&&!f.IsSource(id))&&FlowsToward(world,p,n,f,cell))
                {
                    // Unchanged neighbours sleep; only weaker/empty cells need propagation.
                    if(Displaceable(id)||f.Level(id)>f.Level(cell)+1)Wake(n,f.TickDelay);
                }
            }
        }
    }
}
