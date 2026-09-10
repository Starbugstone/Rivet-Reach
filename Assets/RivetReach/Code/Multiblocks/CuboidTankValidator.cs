using System;
using System.Collections.Generic;

namespace RivetReach
{
    // Bounded cavity discovery; no connected-component scan through touching shells.
    public sealed class CuboidTankValidator : IMultiblockValidator
    {
        public MultiblockValidation Validate(MultiblockDefinition d,MachineState controller,IIndustryWorld world,Func<BlockPos,MachineState> machine,Func<BlockPos,bool> claimed)
        {
            var result=new MultiblockValidation();if(d.MaxDimension<3)return result.Fail("Tank requires dimensions of at least three");var cache=new Dictionary<BlockPos,byte>();
            byte Read(BlockPos p)
            {
                if(cache.TryGetValue(p,out byte value))return value;
                if(++result.Reads>d.CellBudget)throw new ScanStop("Structure scan budget exceeded",p,false);
                if(!world.Ready(p))throw new ScanStop("Waiting for neighbouring chunk",p,true);
                value=world.Get(p);cache.Add(p,value);return value;
            }
            try
            {
                var seed=IndustryDefinition.Neighbor(controller.Position,IndustryDefinition.RotateFace(5,controller.Rotation)^1);
                if(Read(seed)!=BlockId.Air)return result.Fail("Interior must be empty behind controller",seed);
                var seen=new HashSet<BlockPos>{seed};var queue=new Queue<BlockPos>();queue.Enqueue(seed);
                long minX=seed.X,maxX=seed.X,minZ=seed.Z,maxZ=seed.Z;int minY=seed.Y,maxY=seed.Y;
                while(queue.Count>0)
                {
                    var p=queue.Dequeue();
                    for(int f=0;f<6;f++)
                    {
                        var n=IndustryDefinition.Neighbor(p,f);byte cell=Read(n);
                        if(cell!=BlockId.Air)continue;
                        if(!seen.Add(n))continue;
                        minX=Math.Min(minX,n.X);maxX=Math.Max(maxX,n.X);minY=Math.Min(minY,n.Y);maxY=Math.Max(maxY,n.Y);minZ=Math.Min(minZ,n.Z);maxZ=Math.Max(maxZ,n.Z);
                        if(maxX-minX>d.MaxDimension-3||maxY-minY>d.MaxDimension-3||maxZ-minZ>d.MaxDimension-3)
                            return result.Fail("Interior open to world or exceeds maximum size",n);
                        queue.Enqueue(n);
                    }
                }
                var bounds=new StructureBounds(new BlockPos(minX,minY,minZ).Offset(-1,-1,-1),new BlockPos(maxX,maxY,maxZ).Offset(1,1,1));result.Bounds=bounds;
                int controllers=0;
                for(int x=0;x<bounds.Width;x++)for(int y=0;y<bounds.Height;y++)for(int z=0;z<bounds.Depth;z++)
                {
                    var p=bounds.Min.Offset(x,y,z);byte cell=Read(p);int axes=bounds.BoundaryAxes(p);var role=d.Role(cell);
                    if(axes==0){if(cell!=BlockId.Air)return result.Fail("Invalid block or world fluid inside tank",p);continue;}
                    if(axes>=2&&role!=MultiblockRole.Frame)return result.Fail("Missing Reinforced Tank Frame",p);
                    if(axes==1)
                    {
                        if(role==MultiblockRole.None||role==MultiblockRole.Frame)return result.Fail("Missing or invalid tank face panel",p);
                        if((p.Y==bounds.Min.Y||p.Y==bounds.Max.Y)&&role!=MultiblockRole.Wall)return result.Fail("Floor and roof require solid Tank Wall",p);
                        if(role==MultiblockRole.Controller)controllers++;
                        if(role==MultiblockRole.Controller||role==MultiblockRole.FluidPort||role==MultiblockRole.Hatch||role==MultiblockRole.Valve||role==MultiblockRole.Sensor)
                        {
                            var m=machine(p);if(m==null||IndustryDefinition.RotateFace(5,m.Rotation)!=bounds.Face(p))return result.Fail("Rotate component to face outside",p);
                        }
                    }
                    if(claimed(p))return result.Fail("Member already belongs to another multiblock",p);
                    result.Members.Add(p,role);
                }
                if(controllers!=1||!result.Members.ContainsKey(controller.Position))return result.Fail("Exactly one Tank Controller required",controller.Position);
                result.Valid=true;result.Message="FORMED";return result;
            }
            catch(ScanStop e){return result.Fail(e.Message,e.Position,e.Waiting);}
        }
        sealed class ScanStop : Exception
        {public readonly BlockPos Position;public readonly bool Waiting;public ScanStop(string message,BlockPos p,bool waiting):base(message){Position=p;Waiting=waiting;}}
    }
}
