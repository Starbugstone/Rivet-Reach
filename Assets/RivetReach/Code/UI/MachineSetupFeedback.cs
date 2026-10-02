namespace RivetReach
{
    // Read-only explanations of authoritative states. No extra polling of terrain
    // or network search; the UI refreshes these at four Hz while a station is open.
    public static class MachineSetupFeedback
    {
        public static string For(Expedition game)
        {
            var m=game.OpenMachine;
            if(m!=null)return For(m,game.Industry.Simulation);
            var station=game.OpenStation;
            if(station?.Furnace!=null)
            {
                var f=station.Furnace;
                if(f.Recipe==null)return "Add an ingredient. For pipes: ingredients enter every face except the rear; fuel enters only the rear.";
                if(!f.CanWork)return "Clear the result slot. Use a Wrench to set the output pipe end red, then connect a receiving chest.";
                if(f.BurnTicks==0&&ProcessingCatalogAsset.Current.FuelTicks(f.Slots[1].Id)==0)return "Add fuel to the lower slot, or pipe it into the rear. Coal and charcoal are accepted.";
                return "Smelting. Blue pipe arrows feed this furnace; red arrows extract only finished results. Configure with a Wrench.";
            }
            return station?.Block==BlockId.Chest?"Pipes default to blue Input. Hold a Wrench and use the chest-facing pipe end to select red Output for extraction.":
                station?.Crafting!=null?"Find ingredients in the item sidebar. Open a recipe to see its station, quantities and missing materials.":"";
        }
        public static string For(MachineState m,IndustrySimulation simulation)
        {
            if(m.FluidConflict)return "Drain the conflicting liquid before connecting these vessels. Different liquids cannot mix.";
            if(!simulation.IsSimulating(m))return "Wait for nearby connections and terrain. Factories need player or Chunk Loader coverage along the complete route.";
            if(m.Status==MachineStatus.DisabledBySignal)return "The connected Blue Signal is OFF. Turn on its switch, or disconnect that control. Electricity is a separate channel.";
            if(m.Status==MachineStatus.OutputFull)return "Make room in the output. Set its machine-facing pipe end red Output, and the receiver blue Input, using a Wrench.";
            if(m.Status==MachineStatus.NoPower||m.Status==MachineStatus.Underpowered)
                return simulation.Power.Topology.Connected(m)?"A cable is connected. Start generation or charge a supplying battery on this grid. Add supply if work remains slow.":
                    "Connect a Power Cable to any face and route it to a working generator or charged battery. Item/fluid pipes need power fittings.";
            if(m.Definition.Id==IndustryId.Alternator)
            {
                var engine=simulation.At(IndustrySimulation.Neighbor(m,1));
                if(engine==null||engine.Definition.Id!=IndustryId.Boiler)return "Place the Boiler beside the Alternator's LEFT shaft. Align the Boiler's RIGHT shaft using Rotate ports.";
                if(!IndustrySimulation.Neighbor(engine,0).Equals(m.Position))return "Rotate the Boiler so its RIGHT shaft points into this Alternator's LEFT shaft. Both machines must align.";
                if(!engine.Running)return "Shafts align. Inspect the Boiler: it needs coal/charcoal, water and an enabled signal before generating power.";
                return "Shafts align and generation is available. Connect a Power Cable from any face to your machines or battery.";
            }
            if(m.Status==MachineStatus.NoFuel)return "Add coal or charcoal. Automatic fuel must enter the rotated REAR through a blue Input pipe end.";
            if(m.Status==MachineStatus.NoWater&&m.Definition.Id==IndustryId.Boiler)return "Add a Water Bucket, or connect a Pump with Fluid Pipes: red Output at Pump, blue Input at Boiler. Pump needs no power.";
            if(m.Definition.Id==IndustryId.Pump)return m.Status==MachineStatus.NoInput||m.Status==MachineStatus.NoWater?
                "Put a WATER SOURCE directly below the Pump. Flowing water or a pipe blocks intake. No electricity is needed.":
                "Water intake is below. Send water through a red Output pipe end to the Boiler's blue Input. Configure ends with a Wrench.";
            if(m.Definition.Id==IndustryId.RangedPump)return "Collects compatible liquid sources within eight blocks. No power required. A full or differently filled buffer stops collection.";
            if(m.Definition.Id==IndustryId.Boiler)return "RIGHT shaft drives the Alternator's LEFT. Fuel enters REAR; water enters any face. Fuel burns while enabled, even without demand.";
            if(m.Status==MachineStatus.NoInput)return m.Definition.Id==IndustryId.Drill?"Clear the cutting path below. The Drill mines a finite column and cannot cut protected or higher-tier blocks.":
                "Add a compatible ingredient. Set the source pipe end red Output and this machine's end blue Input with a Wrench.";
            if(m.Status==MachineStatus.Depleted)return "This column is finished. Move the Drill to another deposit; resources are finite.";
            if(m.Status==MachineStatus.NoSunlight||m.Status==MachineStatus.Sheltered)return "Keep the sky above clear. Solar output follows daylight; wind output varies with the weather.";
            if(PipeConnections.IsTransport(m.Definition.Id))return "Hold a Wrench to see arrows. Use a machine-facing end: blue Input → red Output → No connection. Pipe-to-pipe links stay automatic.";
            if(m.Definition.Id==IndustryId.Crusher)return "Raw metals yield two crushed ore. Pipe results into a Furnace ingredient face. One Crusher can feed four full-speed Furnaces.";
            if(m.Definition.Id==IndustryId.ElectricFurnace)return "Connect electricity on any face. All item input faces accept ingredients; this Furnace needs no fuel.";
            if(IndustryId.BatteryPart(m.Definition.Id))return "Batteries start empty. Connect generation to charge; connect a load to supply it. Check the battery mode and cable routes.";
            return m.Definition.Help;
        }
    }
}
