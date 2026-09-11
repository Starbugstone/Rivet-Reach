using System;
using System.Collections.Generic;

namespace RivetReach
{
    // One service per session world. Definitions/validators are replaceable; lifecycle and claims are shared.
    public sealed class MultiblockService
    {
        readonly IIndustryWorld world;readonly IndustrySimulation simulation;
        readonly Dictionary<BlockPos,MultiblockInstance> controllers=new Dictionary<BlockPos,MultiblockInstance>();
        readonly Dictionary<BlockPos,MultiblockInstance> claims=new Dictionary<BlockPos,MultiblockInstance>();
        readonly Dictionary<ChunkPos,HashSet<MultiblockInstance>> watchers=new Dictionary<ChunkPos,HashSet<MultiblockInstance>>();
        readonly Queue<MultiblockInstance> pending=new Queue<MultiblockInstance>();
        readonly HashSet<MultiblockInstance> queued=new HashSet<MultiblockInstance>();
        public Guid WorldId {get;internal set;}=Guid.NewGuid();
        public IEnumerable<MultiblockInstance> Instances=>controllers.Values;
        public int PendingCount=>queued.Count;
        public MultiblockService(IIndustryWorld world,IndustrySimulation simulation){this.world=world;this.simulation=simulation;}
        public MultiblockInstance At(BlockPos p)
        {if(controllers.TryGetValue(p,out var c))return c;if(claims.TryGetValue(p,out c))return c;return null;}
        public void Register(MachineState m,MultiblockDefinition definition)
        {var instance=new MultiblockInstance(WorldId,definition,m);controllers.Add(m.Position,instance);m.Structure=instance;Watch(instance,true);Request(instance);}
        public bool CanRemove(BlockPos p)=>(simulation.At(p)?.EnergyCells.Length!=1||simulation.At(p).EnergyCells[0].Amount==0)&&(!controllers.TryGetValue(p,out var c)||c.MachineData.CanDismantle);
        void Release(MultiblockInstance c)
        {
            foreach(var p in c.Validation.Members.Keys)
                if(claims.TryGetValue(p,out var owner)&&owner==c){claims.Remove(p);var m=simulation.At(p);if(m!=null&&m!=c.Controller)m.Structure=null;}
        }
        public void RemoveController(BlockPos p)
        {if(!controllers.TryGetValue(p,out var c))return;if(!c.MachineData.CanDismantle)throw new InvalidOperationException("Drain tank before removing controller");Release(c);Watch(c,false);controllers.Remove(p);queued.Remove(c);}
        void Watch(MultiblockInstance c,bool add)
        {
            int r=c.Definition.MaxDimension-1;var min=c.Controller.Position.Offset(-r,-r,-r).Chunk;var max=c.Controller.Position.Offset(r,r,r).Chunk;
            for(long x=min.X;x<=max.X;x++)for(int y=min.Y;y<=max.Y;y++)for(long z=min.Z;z<=max.Z;z++)
            {
                var p=new ChunkPos(x,y,z);
                if(add){if(!watchers.TryGetValue(p,out var set))watchers[p]=set=new HashSet<MultiblockInstance>();set.Add(c);}
                else if(watchers.TryGetValue(p,out var set)){set.Remove(c);if(set.Count==0)watchers.Remove(p);}
            }
        }
        public void Request(MultiblockInstance c)
        {
            c.Revision++;c.State=MultiblockState.Pending;
            if(queued.Add(c))pending.Enqueue(c);
            simulation.Invalidate();
        }
        public void Changed(BlockPos p)
        {
            // Controllers index their bounded potential footprint, including currently missing/interior cells.
            if(!watchers.TryGetValue(p.Chunk,out var local))return;
            foreach(var c in local)
            {
                int radius=c.Definition.MaxDimension;
                if(Math.Abs(p.X-c.Controller.Position.X)<radius&&Math.Abs((long)p.Y-c.Controller.Position.Y)<radius&&Math.Abs(p.Z-c.Controller.Position.Z)<radius)Request(c);
            }
        }
        public void ResidencyChanged(){foreach(var c in controllers.Values)Request(c);simulation.Invalidate();}
        public void Step()
        {
            // At most one hard-budget scan per eligible simulation tick. Pending structures cannot transfer.
            while(pending.Count>0)
            {
                var c=pending.Dequeue();if(!queued.Remove(c)||!controllers.ContainsKey(c.Controller.Position))continue;
                long revision=c.Revision;
                var result=c.Definition.Validator.Validate(c.Definition,c.Controller,world,simulation.At,p=>claims.TryGetValue(p,out var other)&&other!=c&&other.Formed);
                if(revision!=c.Revision){Request(c);return;}
                if(!result.Valid&&result.Message.StartsWith("Interior open")&&c.LastFormed!=null)
                    foreach(var p in c.LastFormed.Members.Keys)
                    {
                        if(result.Reads>=c.Definition.CellBudget)break;result.Reads++;
                        if(world.Ready(p)&&c.Definition.Role(world.Get(p))==MultiblockRole.None){result.Fail("Shell incomplete: replace missing tank member",p);break;}
                    }
                Release(c);c.Validation=result;
                if(result.Valid){string failure=c.MachineData.TryForm(result);if(failure!=null)result.Fail(failure,c.Controller.Position);}
                c.State=result.Valid?MultiblockState.Formed:result.Waiting?MultiblockState.Waiting:MultiblockState.Invalid;
                // An invalid candidate never claims members. Recovery always remains available at its controller.
                if(c.Formed){c.LastFormed=result;foreach(var p in result.Members.Keys){claims[p]=c;var m=simulation.At(p);if(m!=null)m.Structure=c;}}
                if(c.MachineData is BatteryBankData bank)
                {bank.Cells.Clear();if(c.Formed){var positions=new List<BlockPos>(result.Members.Keys);positions.Sort((a,b)=>{int n=a.X.CompareTo(b.X);if(n!=0)return n;n=a.Y.CompareTo(b.Y);return n!=0?n:a.Z.CompareTo(b.Z);});foreach(var p in positions){var cell=simulation.At(p);if(cell?.EnergyCells.Length==1)bank.Cells.Add(cell.EnergyCells[0]);}}}
                c.Controller.Structure=c;c.Revision++;simulation.Invalidate();return;
            }
        }
    }
}
