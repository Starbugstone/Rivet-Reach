namespace RivetReach
{
    public sealed partial class MachineState
    {
        public bool IsComposter=>Definition.Id==CompostId.Bin;
        public int CompostBatchCount=>CompostCatalog.Current.BatchCount(Items.Slots[0].Id);
        public void SynchronizeCompostInput()
        {if(IsComposter&&WorkInput!=Items.Slots[0].Id){Work=0;WorkInput=Items.Slots[0].Id;}}
    }
    public sealed partial class IndustrySimulation
    {
        void PrepareCompost(MachineState m)
        {
            m.SynchronizeCompostInput();
            if(!m.Enabled){m.Status=MachineStatus.DisabledBySignal;return;}
            int count=m.CompostBatchCount;
            if(count==0||m.Items.Slots[0].Count<count){m.Status=MachineStatus.NoInput;return;}
            if(m.Items.Capacity(CompostId.Compost,2,3)==0){m.Status=MachineStatus.OutputFull;return;}
            m.Status=MachineStatus.Ready;
        }
        void AdvanceCompost(MachineState m)
        {
            if(m.Status!=MachineStatus.Ready)return;
            m.Status=MachineStatus.Running;m.Work++;
            if(m.Work<m.ProcessingTicks)return;
            // Ingredients remain recoverable until the complete, capacity-checked transaction.
            int count=m.CompostBatchCount;
            if(count==0||m.WorkInput!=m.Items.Slots[0].Id||m.Items.Slots[0].Count<count||m.Items.Capacity(CompostId.Compost,2,3)==0)return;
            m.Items.Take(0,count);m.Items.Add(CompostId.Compost,1,2,3);m.Work=0;m.WorkInput=m.Items.Slots[0].Id;
        }
    }
}
