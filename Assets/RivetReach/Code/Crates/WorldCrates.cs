using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    public sealed class WorldCrates
    {
        readonly Expedition game;
        readonly Dictionary<(long,long),HashSet<BlockPos>> columns=new Dictionary<(long,long),HashSet<BlockPos>>();
        readonly HashSet<BlockPos> positions=new HashSet<BlockPos>();
        public CrateNetwork Network {get;}
        public WorldCrates(Expedition game)
        {
            this.game=game;Network=new CrateNetwork(p=>game.World.Get(p),p=>game.Survival.At(p)?.Crate,p=>game.World.Ready(p));
            Network.GetPriority=p=>game.Survival.At(p)?.ItemInputPriority??30;Network.SetPriority=(p,value)=>{if(game.Survival.At(p) is StationState s)s.ItemInputPriority=value;};
            game.World.BlockChanged+=Changed;game.World.ResidencyChanged+=Residency;
        }
        void Residency(){Network.Invalidate();game.Industry?.Simulation.Invalidate();}
        void Register(BlockPos p)
        {if(!positions.Add(p))return;var key=(p.Chunk.X,p.Chunk.Z);if(!columns.TryGetValue(key,out var set))columns[key]=set=new HashSet<BlockPos>();set.Add(p);}
        void Changed(BlockPos p)
        {
            bool added=CrateId.Part(game.World.Get(p));if(!added&&!positions.Contains(p))return;
            if(added)Register(p);else{positions.Remove(p);var key=(p.Chunk.X,p.Chunk.Z);columns[key].Remove(p);if(columns[key].Count==0)columns.Remove(key);Network.Removed(p);}
            Network.Invalidate();game.Industry?.Simulation.Invalidate();
        }
        public void Restored(){foreach(var s in game.Survival.Stations)if(CrateId.Part(s.Value.Block))Register(s.Key);Network.Invalidate();}
        public CrateEndpoint At(BlockPos p)=>Network.At(p);
        public bool CanRemove(BlockPos p)
        {
            if(game.Survival.At(p)?.Crate?.Count>0){game.Notify("Empty this crate before mining it. Its stored items are safe.",4);return false;}return true;
        }
        public IEnumerable<BlockPos> Nearby(BlockPos center)
        {
            var c=center.Chunk;for(long x=c.X-3;x<=c.X+3;x++)for(long z=c.Z-3;z<=c.Z+3;z++)
                if(columns.TryGetValue((x,z),out var set))foreach(var p in set)yield return p;
        }
    }
}
