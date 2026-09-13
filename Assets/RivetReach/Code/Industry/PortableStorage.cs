using System;

namespace RivetReach
{
    // Value payload moves with the recovered item; placed authorities never share it.
    public static class PortableStorage
    {
        public static ItemStack Capture(MachineState machine)
        {
            var stack=new ItemStack(machine.Definition.Id,1);
            if(stack.Id==IndustryId.Battery)stack.Energy=machine.EnergyCells[0].Amount;
            var fluid=stack.Id==IndustryId.Tank?machine.Fluid:stack.Id==IndustryId.TankController?machine.Structure.Fluid:null;
            if(fluid?.Amount>0){stack.FluidAmount=fluid.Amount;stack.FluidCapacity=fluid.Capacity;stack.FluidId=fluid.Fluid.Source;}
            return stack;
        }
        public static void Drain(MachineState machine)
        {
            if(machine.Definition.Id==IndustryId.Battery)machine.EnergyCells[0].Discharge(machine.EnergyCells[0].Amount);
            var fluid=machine.Definition.Id==IndustryId.Tank?machine.Fluid:machine.Definition.Id==IndustryId.TankController?machine.Structure.Fluid:null;
            if(fluid?.Amount>0)fluid.Withdraw(fluid.Amount);
        }
        public static void Restore(MachineState machine,ItemStack stack)
        {
            if(!stack.HasContents)return;
            if(!stack.ValidContents||machine.Definition.Id!=stack.Id)throw new InvalidOperationException("Invalid portable storage placement.");
            if(stack.Energy>0){if(!machine.EnergyCells[0].Charge(stack.Energy))throw new InvalidOperationException("Battery placement overflow.");return;}
            var fluid=stack.Id==IndustryId.Tank?machine.Fluid:machine.Structure.Fluid;
            if(!fluid.Resize(stack.FluidCapacity)||!fluid.Deposit(Fluids.Registry.Get(stack.FluidId),stack.FluidAmount))throw new InvalidOperationException("Tank placement overflow.");
        }
    }
    public sealed partial class Expedition
    {
        public bool TryEmptySelectedStorage()
        {
            if(Mode!=ScreenMode.Play||Health.Dead||Player.Inspecting)return false;
            if(!Inventory.EmptyContents(Selected))return false;
            Notify("Stored contents discarded",2);return true;
        }
    }
}
