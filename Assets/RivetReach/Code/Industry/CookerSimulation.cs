namespace RivetReach
{
    public sealed partial class IndustrySimulation
    {
        void PrepareCooker(MachineState m)
        {
            var recipe=m.FoodRecipe;
            if(m.CookingSignature!=m.FoodSignature){m.Work=0;m.CookingSignature=m.FoodSignature;}
            if(!m.Enabled){m.Status=MachineStatus.DisabledBySignal;return;}
            if(recipe==null||!recipe.Matcher.TryPlan(m.Items.Slots,m.CookingPlan)){m.Status=MachineStatus.NoInput;return;}
            if(m.Items.Capacity(recipe.output,4,5)<recipe.count){m.Status=MachineStatus.OutputFull;return;}
            if(m.Definition.Id==FarmId.Cooker)
            {
                if(m.BurnTicks==0)
                {
                    byte fuel=m.Items.Slots[3].Id;
                    if(!m.CookerAccepts(3,fuel)){m.Status=MachineStatus.NoFuel;return;}
                    m.Items.Take(3,1);m.BurnTicks=m.FuelTicks(fuel);
                }
                m.Status=MachineStatus.Ready;
            }
            else {m.RequestedWatts=CookingCatalog.Current.electricWatts;m.Status=MachineStatus.NoPower;}
        }
        void AdvanceCooker(MachineState m)
        {
            bool electric=m.Definition.Id==FarmId.ElectricCooker;
            if(electric?(m.RequestedWatts==0||m.ReceivedWatts==0):m.Status!=MachineStatus.Ready)return;
            var recipe=m.FoodRecipe;var plan=m.CookingPlan;
            if(!recipe.Matcher.TryPlan(m.Items.Slots,plan)||m.Items.Capacity(recipe.output,4,5)<recipe.count)return;
            // Pipes may have filled an empty ingredient slot since preparation.
            if(m.CookingSignature!=m.FoodSignature){m.Work=0;m.CookingSignature=m.FoodSignature;}
            if(!electric)m.BurnTicks--;
            m.Work+=electric?m.ReceivedWatts/(double)CookingCatalog.Current.electricWatts:1;
            m.Status=electric&&m.ReceivedWatts<m.RequestedWatts?MachineStatus.Underpowered:MachineStatus.Running;
            if(m.Work+1e-9<recipe.ticks)return;
            // One authority turn: validate all ingredients and output capacity before consuming anything.
            for(int slot=0;slot<3;slot++)if(plan[slot]>0)m.Items.Take(slot,plan[slot]);
            m.Items.Add(recipe.output,recipe.count,4,5);m.Work-=recipe.ticks;m.CookingSignature=m.FoodSignature;
        }
    }
}
