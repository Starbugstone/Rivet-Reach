using System;
using System.Collections.Generic;

namespace RivetReach
{
    // Stable identity is independent of the compact session voxel encoding. No item is a fluid cell.
    public sealed class FluidDefinition
    {
        public readonly string StableId, DisplayName;
        public readonly byte FirstCell, BucketItem;
        public readonly int Reach, TickDelay;
        public readonly bool RenewsSources;
        public readonly float Drag, CurrentSpeed, Red, Green, Blue;
        public FluidDefinition(string stableId, byte firstCell, byte bucketItem, int reach, int tickDelay,
            bool renewsSources, float drag, float currentSpeed, float red, float green, float blue, string displayName=null)
        {
            if(string.IsNullOrWhiteSpace(stableId)||firstCell<100||firstCell>247||bucketItem==0||reach<1||reach>7||tickDelay<1||float.IsNaN(drag)||float.IsInfinity(drag)||drag<=0||float.IsNaN(currentSpeed)||float.IsInfinity(currentSpeed)||currentSpeed<0)
                throw new ArgumentException("Invalid fluid definition");
            DisplayName=displayName??stableId;StableId=stableId;FirstCell=firstCell;BucketItem=bucketItem;Reach=reach;TickDelay=tickDelay;
            RenewsSources=renewsSources;Drag=drag;CurrentSpeed=currentSpeed;Red=red;Green=green;Blue=blue;
        }
        public byte Source=>FirstCell;
        public byte Falling=>(byte)(FirstCell+8);
        public byte Flow(int level)=>(byte)(FirstCell+level);
        public bool IsSource(byte cell)=>cell==Source;
        public bool IsFalling(byte cell)=>cell==Falling;
        public int Level(byte cell)=>IsFalling(cell)?0:cell-FirstCell;
        public float Height(byte cell)=>IsFalling(cell)?1: (8-Level(cell))/9f;
    }
    public sealed class FluidRegistry
    {
        readonly FluidDefinition[] cells=new FluidDefinition[256], buckets=new FluidDefinition[256];
        public FluidRegistry(params FluidDefinition[] definitions)
        {
            var names=new HashSet<string>(StringComparer.Ordinal);var reserved=new bool[256];
            foreach(var f in definitions)
            {
                if(f==null||!names.Add(f.StableId)||buckets[f.BucketItem]!=null)throw new ArgumentException("Duplicate fluid identity/container");
                buckets[f.BucketItem]=f;
                for(int i=0;i<=8;i++)
                {int id=f.FirstCell+i;if(reserved[id])throw new ArgumentException("Overlapping fluid encoding");reserved[id]=true;if(i<=f.Reach||i==8)cells[id]=f;}
            }
        }
        public FluidDefinition Get(byte cell)=>cells[cell];
        public FluidDefinition FromBucket(byte item)=>buckets[item];
    }
    public static class Fluids
    {
        public const byte EmptyBucket=94, WaterBucket=95;
        public static readonly FluidDefinition Water=new FluidDefinition("rivet:water",100,WaterBucket,7,5,renewsSources:true,3.5f,1.2f,.08f,.48f,.66f,displayName:"Water");
        public static readonly FluidRegistry Registry=new FluidRegistry(Water);
        public static bool IsFluid(byte cell)=>Registry.Get(cell)!=null;
        public static bool IsBucket(byte item)=>item==EmptyBucket||Registry.FromBucket(item)!=null;
    }
}
