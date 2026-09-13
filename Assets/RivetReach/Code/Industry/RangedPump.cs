namespace RivetReach
{
    public sealed partial class IndustrySimulation
    {
        // Inclusive eight-block reach along each axis; rotation changes ports only.
        public const int RangedPumpMin=-8,RangedPumpMax=8,RangedPumpScanBudget=256;
        const int RangedPumpWidth=17,RangedPumpCells=17*17*17;
        bool RangedSource(MachineState m,BlockPos p,out FluidDefinition fluid)
        {
            fluid=null;if(!world.Ready(p))return false;
            byte cell=world.Get(p);fluid=Fluids.Registry.Get(cell);
            return fluid!=null&&fluid.IsSource(cell)&&m.Fluid.Accepts(fluid);
        }
        void PrepareRangedPump(MachineState m)
        {
            if(m.Fluid.Capacity-m.Fluid.Amount<10000){m.Status=MachineStatus.OutputFull;return;}
            if(m.PumpTarget is BlockPos target)
            {
                if(!world.Ready(target)){m.Status=MachineStatus.Dormant;return;}
                if(RangedSource(m,target,out var cached)&&cached.Source==m.WorkInput){m.Status=MachineStatus.Ready;return;}
                m.PumpTarget=null;m.Work=0;
            }
            if(Tick<m.PumpRetryTick){m.Status=MachineStatus.NoInput;return;}

            // Drain upper layers first. Stop on the first eligible source; no temporary
            // lists, chunk loads or scans while disabled/full. Empty areas retry at 2 s.
            for(int budget=0;budget<RangedPumpScanBudget&&m.PumpScanIndex<RangedPumpCells;budget++)
            {
                int index=m.PumpScanIndex++;
                int x=RangedPumpMin+index%RangedPumpWidth;
                int z=RangedPumpMin+index/RangedPumpWidth%RangedPumpWidth;
                int y=RangedPumpMax-index/(RangedPumpWidth*RangedPumpWidth);
                var p=m.Position.Offset(x,y,z);
                if(!world.Ready(p)){m.PumpScanUnloaded=true;continue;}
                if(!RangedSource(m,p,out var fluid))continue;
                if(m.WorkInput!=fluid.Source)m.Work=0;
                m.WorkInput=fluid.Source;m.PumpTarget=p;m.PumpScanIndex=0;m.PumpScanUnloaded=false;m.Status=MachineStatus.Ready;return;
            }
            m.Status=m.PumpScanUnloaded?MachineStatus.Dormant:MachineStatus.NoInput;
            if(m.PumpScanIndex<RangedPumpCells)return;
            m.Work=0;m.PumpScanIndex=0;m.PumpScanUnloaded=false;m.PumpRetryTick=Tick+40;
        }
        bool CollectRangedSource(MachineState m)
        {
            if(!(m.PumpTarget is BlockPos p)||!RangedSource(m,p,out var fluid)||fluid.Source!=m.WorkInput||m.Fluid.Capacity-m.Fluid.Amount<10000)return false;
            // Both checks and mutation run on the authoritative simulation thread.
            // A competing pump can remove the source first, but cannot create output twice.
            if(!world.Remove(p,fluid.Source))return false;
            if(!m.Fluid.Deposit(fluid,10000))throw new System.InvalidOperationException("Ranged pump reservation changed during extraction.");
            m.PumpTarget=null;return true;
        }
    }
}
