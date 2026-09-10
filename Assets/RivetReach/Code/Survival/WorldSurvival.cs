using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    public sealed class StationState
    {
        public byte Block {get;}
        public CraftingSession Crafting {get;}
        public FurnaceState Furnace {get;}
        public ItemContainer Storage {get;}
        public StationState(byte block,RecipeRegistry crafting,ProcessingRegistry processing,Func<byte,int> limit)
        {
            Block=block;
            if(block==BlockId.Workbench||block==IndustryId.Bench)Crafting=new CraftingSession(crafting,block==IndustryId.Bench?4:3,limit);
            else if(block==BlockId.Furnace)Furnace=new FurnaceState(processing,limit);
            else if(block==BlockId.Chest)Storage=new ItemContainer(27,limit);
            else throw new ArgumentException("Unsupported station block.");
        }
    }
    // Owned by one world/session. Compact block entities outlive their streamed presentation.
    // Idle furnaces never enter the active set; crops schedule their next stage, not frame updates.
    public sealed class WorldSurvival
    {
        public const int TicksPerSecond=20,CropStageTicks=1200;
        readonly Expedition game;
        readonly Dictionary<BlockPos,StationState> stations=new Dictionary<BlockPos,StationState>();
        readonly HashSet<BlockPos> active=new HashSet<BlockPos>();
        readonly List<BlockPos> sleep=new List<BlockPos>();
        readonly SortedSet<CropJob> crops=new SortedSet<CropJob>();
        readonly Dictionary<BlockPos,CropJob> scheduled=new Dictionary<BlockPos,CropJob>();
        double fraction;
        public long Tick {get;private set;}
        public int StationCount=>stations.Count;
        public int ActiveFurnaces=>active.Count;
        public int ScheduledCrops=>scheduled.Count;
        readonly struct CropJob : IComparable<CropJob>
        {
            public readonly long Due;public readonly BlockPos Position;
            public CropJob(long due,BlockPos position){Due=due;Position=position;}
            public int CompareTo(CropJob other)
            {
                int result=Due.CompareTo(other.Due);if(result!=0)return result;
                result=Position.X.CompareTo(other.Position.X);if(result!=0)return result;
                result=Position.Z.CompareTo(other.Position.Z);return result!=0?result:Position.Y.CompareTo(other.Position.Y);
            }
        }
        public WorldSurvival(Expedition game){this.game=game;game.World.BlockChanged+=Changed;}
        public StationState At(BlockPos position)=>stations.TryGetValue(position,out var station)?station:null;
        public void Wake(BlockPos position){if(At(position)?.Furnace?.NeedsTick==true)active.Add(position);}
        void Schedule(BlockPos p,long due)
        {
            if(scheduled.TryGetValue(p,out var old))crops.Remove(old);
            var job=new CropJob(due,p);scheduled[p]=job;crops.Add(job);
        }
        void DropContents(BlockPos p,StationState station)
        {
            void Drop(ItemStack stack){if(!stack.Empty)game.Items.Spawn(stack,game.World.Local(p)+Vector3.one*.5f,Vector3.up);}
            if(station.Crafting!=null)for(int i=0;i<station.Crafting.Grid.Count;i++)Drop(station.Crafting.Grid.Take(i,int.MaxValue));
            if(station.Storage!=null)for(int i=0;i<station.Storage.Count;i++)Drop(station.Storage.Take(i,int.MaxValue));
            if(station.Furnace!=null)for(int i=0;i<3;i++)Drop(station.Furnace.Take(i,int.MaxValue));
        }
        void Changed(BlockPos p)
        {
            byte id=game.World.Get(p);
            if(stations.TryGetValue(p,out var station)&&station.Block!=id)
            {
                // Close before draining so cursor/remaining crafting ingredients have one owner.
                if(game.OpenStation==station)game.SetMode(ScreenMode.Play);
                stations.Remove(p);active.Remove(p);DropContents(p,station);
            }
            if(BlockId.Station(id)&&!stations.ContainsKey(p))stations.Add(p,new StationState(id,game.Recipes,game.Processing,item=>game.Registry.Get(item).stackLimit));
            if(BlockId.Crop(id)&&id<BlockId.MaturePotatoPlant)
            {if(!scheduled.ContainsKey(p))Schedule(p,Tick+CropStageTicks);}
            else if(scheduled.TryGetValue(p,out var old)){crops.Remove(old);scheduled.Remove(p);}
            var above=p.Offset(0,1,0);byte plant=game.World.Get(above);
            if(id!=BlockId.Farmland&&BlockId.Crop(plant))game.World.Uproot(above,plant);
        }
        public int Advance(float seconds)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            fraction+=seconds*TicksPerSecond;int ticks=(int)Math.Min(100,fraction);fraction-=ticks;
            if(ticks==0)return 0;AdvanceTicks(ticks);return ticks;
        }
        public void AdvanceTicks(int ticks)
        {
            if(ticks<0)throw new ArgumentOutOfRangeException(nameof(ticks));Tick=checked(Tick+ticks);
            sleep.Clear();
            foreach(var p in active)
            {
                var furnace=stations[p].Furnace;
                if(game.World.Ready(p))furnace.Advance(ticks);
                if(!furnace.NeedsTick)sleep.Add(p);
            }
            foreach(var p in sleep)active.Remove(p);
            // Bound mesh-producing growth changes even after a long frame or mass planting.
            for(int budget=0;budget<16&&crops.Count>0;budget++)
            {
                var job=crops.Min;if(job.Due>Tick)break;crops.Remove(job);scheduled.Remove(job.Position);
                byte id=game.World.Get(job.Position);
                if(!BlockId.Crop(id)||id==BlockId.MaturePotatoPlant)continue;
                if(!game.World.Ready(job.Position)||game.World.SkyLight(job.Position)<9)Schedule(job.Position,Tick+20);
                else game.World.Grow(job.Position,id);
            }
        }
    }
}
