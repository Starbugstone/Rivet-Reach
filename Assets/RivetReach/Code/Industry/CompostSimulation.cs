using System;
namespace RivetReach
{
    public sealed partial class MachineState
    {
        public bool IsComposter=>Definition.Id==CompostId.Bin||Definition.Id==CompostId.Auto;
        public bool IsAutoComposter=>Definition.Id==CompostId.Auto;
        public int CompostPoints,CompostCharge;
        public const int CompostWatts=160,CompostItemCharge=160,CompostChargeCapacity=64*CompostItemCharge;
        public long CompostBatches;
        public Action<MachineState> CompostChanged;
        public int CompostBatchCount=>CompostCatalog.Current.BatchCount(Items.Slots[0].Id);
        // Kept for legacy capture callers; elapsed processing no longer funds output.
        public void SynchronizeCompostInput() { }
        public int NextCompostYield
        {
            get
            {
                unchecked
                {
                    ulong n=(ulong)CompostBatches+0x9e3779b97f4a7c15UL;
                    n^=(ulong)Position.X*0xbf58476d1ce4e5b9UL^(ulong)Position.Y*0x94d049bb133111ebUL^(ulong)Position.Z;
                    n=(n^(n>>30))*0xbf58476d1ce4e5b9UL;n=(n^(n>>27))*0x94d049bb133111ebUL;
                    return 1+(int)((n^(n>>31))&3);
                }
            }
        }
        public bool CanCompost(byte id)
        {
            int points=CompostCatalog.Current.Points(id);
            return IsComposter&&Eligible&&points>0&&(!IsAutoComposter||Eligible&&Enabled&&CompostCharge>=CompostItemCharge)&&
                (CompostPoints+points<CompostCatalog.Current.pointsPerCompost||
                 CompostBatches<long.MaxValue&&Items.Capacity(CompostId.Compost,2,3)>=NextCompostYield);
        }
        public int AddCompost(byte id,int count)
        {
            if(count<1)return 0;
            int consumed=0;
            // A deposit is bounded by one ordinary item stack. Excess points carry forward.
            for(int i=0;i<Math.Min(count,Items.StackLimit(id))&&CanCompost(id);i++)
            {
                CompostPoints+=CompostCatalog.Current.Points(id);if(IsAutoComposter)CompostCharge-=CompostItemCharge;consumed++;
                if(CompostPoints>=CompostCatalog.Current.pointsPerCompost)
                {
                    int yield=NextCompostYield;
                    CompostPoints-=CompostCatalog.Current.pointsPerCompost;CompostBatches++;
                    Items.Add(CompostId.Compost,yield,2,3);
                }
            }
            if(consumed>0){Status=MachineStatus.Ready;CompostChanged?.Invoke(this);}
            return consumed;
        }
    }
    public sealed partial class IndustrySimulation
    {
        public event Action<MachineState> CompostChanged;
        void PrepareCompost(MachineState m)
        {
            if(!m.Enabled&&m.IsAutoComposter){m.Status=MachineStatus.DisabledBySignal;return;}
            m.Status=MachineStatus.Ready;
            if(m.IsAutoComposter)
            {
                if(m.Items.Capacity(CompostId.Compost,2,3)<m.NextCompostYield){m.Status=MachineStatus.OutputFull;return;}
                m.RequestedWatts=Math.Min(MachineState.CompostWatts,MachineState.CompostChargeCapacity-m.CompostCharge);
                if(m.CompostCharge<MachineState.CompostItemCharge)m.Status=MachineStatus.NoPower;
            }
        }
        void AdvanceCompost(MachineState m)
        {
            // Only legacy saved input can occupy this slot. Preserve it until accepted.
            if(!m.IsAutoComposter||!m.Enabled)return;
            m.CompostCharge+=m.ReceivedWatts;
            if(m.ReceivedWatts>0)m.Status=m.ReceivedWatts<m.RequestedWatts?MachineStatus.Underpowered:MachineStatus.Running;
            var input=m.Items.Slots[0];if(!input.Empty)m.Items.Take(0,m.AddCompost(input.Id,input.Count));
        }
    }
}
