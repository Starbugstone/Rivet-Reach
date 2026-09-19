using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    public static class BedId
    {
        public const byte Bed=254,Head=255;
        public static bool Part(byte id)=>id==Bed||id==Head;
        public static BlockPos HeadAt(BlockPos foot,int rotation)=>rotation switch
        {0=>foot.Offset(0,0,1),1=>foot.Offset(1,0,0),2=>foot.Offset(0,0,-1),_=>foot.Offset(-1,0,0)};
        public static bool Floor(byte id)=>VoxelWorld.DoorFloor(id)&&!Part(id)&&!BlockId.Station(id)&&id!=BlockId.MobSpawner;
    }
    public sealed class BedState
    {
        public readonly BlockPos Foot;
        public readonly int Rotation;
        public readonly long Identity;
        public BlockPos Head=>BedId.HeadAt(Foot,Rotation);
        public BedState(BlockPos foot,int rotation,long identity){Foot=foot;Rotation=rotation;Identity=identity;}
    }
    public sealed partial class VoxelWorld
    {
        readonly Dictionary<BlockPos,BedState> beds=new Dictionary<BlockPos,BedState>();
        readonly Dictionary<BlockPos,BedState> bedCells=new Dictionary<BlockPos,BedState>();
        readonly Dictionary<(long,long),HashSet<BedState>> bedColumns=new Dictionary<(long,long),HashSet<BedState>>();
        long nextBedIdentity=1;
        public IReadOnlyDictionary<BlockPos,BedState> Beds=>beds;
        internal bool IssuedBedIdentity(long id)=>id>0&&id<nextBedIdentity;
        public BedState BedAt(BlockPos cell)=>bedCells.TryGetValue(cell,out var bed)?bed:null;
        public IEnumerable<BedState> NearbyBeds(BlockPos center)
        {
            var c=center.Chunk;
            for(long x=c.X-3;x<=c.X+3;x++)for(long z=c.Z-3;z<=c.Z+3;z++)
                if(bedColumns.TryGetValue((x,z),out var page))foreach(var bed in page)yield return bed;
        }
        public bool CanPlaceBed(BlockPos foot,int rotation)
        {
            if(rotation<0||rotation>3||beds.Count>=65536||nextBedIdentity>=long.MaxValue-1000000)return false;
            var head=BedId.HeadAt(foot,rotation);
            foreach(var p in new[]{foot,head})
                if(p.Y<=TerrainGenerator.MinY||p.Y>TerrainGenerator.MaxY||Math.Abs(p.X)>TerrainGenerator.HorizontalLimit||Math.Abs(p.Z)>TerrainGenerator.HorizontalLimit||
                    !Ready(p)||!Ready(p.Offset(0,-1,0))||Get(p)!=0||!BedId.Floor(Get(p.Offset(0,-1,0))))return false;
            return true;
        }
        void IndexBed(BedState bed)
        {
            beds.Add(bed.Foot,bed);bedCells.Add(bed.Foot,bed);bedCells.Add(bed.Head,bed);
            var key=(bed.Foot.Chunk.X,bed.Foot.Chunk.Z);
            if(!bedColumns.TryGetValue(key,out var page))bedColumns[key]=page=new HashSet<BedState>();page.Add(bed);
        }
        public bool PlaceBed(BlockPos foot,int rotation)
        {
            if(!CanPlaceBed(foot,rotation))return false;
            var head=BedId.HeadAt(foot,rotation);
            if(!Change(head,0,BedId.Head))return false;
            if(!Change(foot,0,BedId.Bed)){Change(head,BedId.Head,0,false,false);return false;}
            IndexBed(new BedState(foot,rotation,nextBedIdentity++));return true;
        }
        bool RemoveBed(BlockPos cell,byte expected,bool requireReady=true)
        {
            if((requireReady&&!Ready(cell))||Get(cell)!=expected||BedAt(cell) is not BedState bed)return false;
            if(Get(bed.Foot)!=BedId.Bed||Get(bed.Head)!=BedId.Head)return false;
            beds.Remove(bed.Foot);bedCells.Remove(bed.Foot);bedCells.Remove(bed.Head);
            var key=(bed.Foot.Chunk.X,bed.Foot.Chunk.Z);bedColumns[key].Remove(bed);if(bedColumns[key].Count==0)bedColumns.Remove(key);
            Change(bed.Head,BedId.Head,0,false,false);Change(bed.Foot,BedId.Bed,0,false,false);return true;
        }
        void BedSupportChanged(BlockPos p,byte replacement)
        {
            if(BedId.Floor(replacement))return;
            var cell=p.Offset(0,1,0);var bed=BedAt(cell);
            if(bed!=null&&RemoveBed(cell,Get(cell),false)&&!SuppressMiningDrops)BlockMined?.Invoke(bed.Foot,BedId.Bed);
        }
        internal void WriteBeds(SaveWriter w)
        {
            w.Write(nextBedIdentity);w.Write(beds.Count);
            foreach(var bed in beds.Values){w.Pos(bed.Foot);w.Write(bed.Rotation);w.Write(bed.Identity);}
        }
        internal void ReadBeds(SaveReader r)
        {
            SaveReader.Require(beds.Count==0&&bedCells.Count==0&&bedColumns.Count==0&&nextBedIdentity==1,"Bed restore requires a fresh world.");
            if(r.Format<14)return;
            nextBedIdentity=r.Long(1);int count=r.Count(65536);var identities=new HashSet<long>();
            for(int i=0;i<count;i++)
            {
                var bed=new BedState(r.Pos(),r.Int(0,3),r.Long(1));
                SaveReader.Require(bed.Identity<nextBedIdentity&&identities.Add(bed.Identity)&&!bedCells.ContainsKey(bed.Foot)&&!bedCells.ContainsKey(bed.Head),"Duplicate saved bed identity or footprint.");
                SaveReader.Require(Get(bed.Foot)==BedId.Bed&&Get(bed.Head)==BedId.Head&&BedId.Floor(Get(bed.Foot.Offset(0,-1,0)))&&BedId.Floor(Get(bed.Head.Offset(0,-1,0))),"Invalid saved bed footprint/support.");IndexBed(bed);
            }
            foreach(var p in SavedBlocks())if(BedId.Part(p.Value))SaveReader.Require(BedAt(p.Key)!=null,"Orphaned bed cell.");
        }
    }
}
