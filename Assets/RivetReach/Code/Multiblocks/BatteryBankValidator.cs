using System;
using System.Collections.Generic;

namespace RivetReach
{
    // A filled rectangular pack: battery blocks plus one outward-facing controller.
    public sealed class BatteryBankValidator : IMultiblockValidator
    {
        public MultiblockValidation Validate(MultiblockDefinition d,MachineState controller,IIndustryWorld world,Func<BlockPos,MachineState> machine,Func<BlockPos,bool> claimed)
        {
            var r=new MultiblockValidation();var visited=new HashSet<BlockPos>();var q=new Queue<BlockPos>();q.Enqueue(controller.Position);
            var min=controller.Position;var max=min;int controllers=0;
            while(q.Count>0)
            {
                var p=q.Dequeue();if(!visited.Add(p))continue;
                if(++r.Reads>d.CellBudget)return r.Fail("Battery bank scan budget exceeded",p);
                if(!world.Ready(p))return r.Fail("Waiting for neighbouring chunk",p,true);
                var role=d.Role(world.Get(p));if(role==MultiblockRole.None)continue;
                if(claimed(p))return r.Fail("Battery belongs to another bank",p);
                if(machine(p)==null)return r.Fail("Waiting for battery state",p,true);
                r.Members.Add(p,role);if(role==MultiblockRole.Controller)controllers++;
                min=new BlockPos(Math.Min(min.X,p.X),Math.Min(min.Y,p.Y),Math.Min(min.Z,p.Z));max=new BlockPos(Math.Max(max.X,p.X),Math.Max(max.Y,p.Y),Math.Max(max.Z,p.Z));
                r.Bounds=new StructureBounds(min,max);
                if(r.Bounds.Width>d.MaxDimension||r.Bounds.Height>d.MaxDimension||r.Bounds.Depth>d.MaxDimension)return r.Fail("Battery bank maximum is 5 blocks per axis",p);
                for(int face=0;face<6;face++)q.Enqueue(IndustryDefinition.Neighbor(p,face));
            }
            if(controllers!=1)return r.Fail("Separate banks: exactly one controller per connected pack",controller.Position);
            if(r.Members.Count<2)return r.Fail("Add at least one Battery Block behind the controller",controller.Position);
            for(int x=0;x<r.Bounds.Width;x++)for(int y=0;y<r.Bounds.Height;y++)for(int z=0;z<r.Bounds.Depth;z++)
            {var p=min.Offset(x,y,z);if(!r.Members.ContainsKey(p))return r.Fail("Fill every bank cell with a Battery Block",p);}
            if(r.Bounds.Contains(IndustrySimulation.Neighbor(controller,5)))return r.Fail("Rotate controller to face outside the bank",controller.Position);
            r.Valid=true;r.Message="FORMED";return r;
        }
    }
}
