using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RivetReach.Editor
{
    public static class RenewablePowerChecks
    {
        sealed class World : IIndustryWorld
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public bool Ready(BlockPos p)=>true;
            public byte Get(BlockPos p)=>Cells.TryGetValue(p,out var id)?id:(byte)0;
            public bool Remove(BlockPos p,byte expected)=>Cells.Remove(p);
            public ItemContainer Storage(BlockPos p)=>null;
            public byte Drop(byte block)=>block;
            public bool PlayerInside(BlockPos p)=>false;
        }
        public static void Run()
        {
            var passed=new List<string>();
            void Check(bool ok,string message){if(!ok)throw new Exception("Renewable power: "+message);passed.Add("PASS "+message);}
            MachineState Add(List<MachineState> machines,int x,int z,byte id)
            {
                var machine=new MachineState(new BlockPos(x,0,z),id,_=>64);
                machines.Add(machine);
                return machine;
            }
            PowerNetworkService Build(List<MachineState> machines)
            {
                var power=new PowerNetworkService();
                foreach(var _ in power.Topology.Rebuild(machines)){}
                return power;
            }

            // With no generation, a stored battery can pay a real load but cannot
            // charge the empty batteries beside it, even around a circular cable run.
            {
                var machines=new List<MachineState>();
                for(int x=0;x<=4;x++){Add(machines,x,0,IndustryId.PowerCable);Add(machines,x,2,IndustryId.PowerCable);}
                for(int z=0;z<=2;z++){Add(machines,-1,z,IndustryId.PowerCable);Add(machines,5,z,IndustryId.PowerCable);}
                var source=Add(machines,0,1,IndustryId.Battery);var emptyA=Add(machines,2,1,IndustryId.Battery);var emptyB=Add(machines,4,1,IndustryId.Battery);
                var load=Add(machines,4,-1,IndustryId.Lamp);source.EnergyCells[0].Charge(10000);load.RequestedWatts=20;
                var power=Build(machines);
                power.Allocate(0);
                Check(load.ReceivedWatts==20&&source.BatteryOutputWatts==20&&source.BatteryInputWatts==0&&BatteryPower.Amount(source)==9000&&
                    emptyA.BatteryInputWatts==0&&emptyB.BatteryInputWatts==0&&BatteryPower.Amount(emptyA)==0&&BatteryPower.Amount(emptyB)==0,
                    "Stored charge pays only the real load; circular cables do not create battery-to-battery charging");
            }

            // The same rule applies when the charged source is a formed bank rather
            // than an individual cell.
            {
                var world=new World();var simulation=new IndustrySimulation(world,_=>64);
                MachineState AddPlaced(int x,int z,byte id){var position=new BlockPos(x,0,z);world.Cells[position]=id;return simulation.Add(position,id);}
                var bank=AddPlaced(10,0,IndustryId.BatteryController);var cells=new[]{AddPlaced(10,1,IndustryId.Battery),AddPlaced(11,0,IndustryId.Battery),AddPlaced(11,1,IndustryId.Battery)};
                foreach(var position in new[]{(9,-1),(10,-1),(11,-1),(12,-1),(12,0),(12,1),(12,2),(11,2),(10,2),(9,2),(9,1),(9,0)})AddPlaced(position.Item1,position.Item2,IndustryId.PowerCable);
                var empty=AddPlaced(8,-1,IndustryId.Battery);var load=AddPlaced(10,-2,IndustryId.Lamp);
                for(int step=0;step<100&&(simulation.Rebuilding||!bank.Structure.Formed);step++)simulation.Step();
                Check(bank.Structure.Formed,"Battery bank forms before its no-loop allocation check");
                cells[0].EnergyCells[0].Charge(10000);load.RequestedWatts=20;long before=BatteryPower.Amount(bank);simulation.Power.Allocate(1);
                Check(load.ReceivedWatts==20&&before-BatteryPower.Amount(bank)==1000&&empty.BatteryInputWatts==0&&BatteryPower.Amount(empty)==0,
                    "A formed bank drains only for its machine load and never charges an adjacent empty battery without generation");
            }

            // Generation from distinct producers combines only on its physical cable
            // grid. Machine demand comes first; the surplus then shares evenly and
            // redistributes around full batteries.
            {
                var machines=new List<MachineState>();
                for(int x=0;x<=4;x++)Add(machines,x,0,IndustryId.PowerCable);
                var first=Add(machines,0,1,IndustryId.Battery);var second=Add(machines,2,1,IndustryId.Battery);var third=Add(machines,4,1,IndustryId.Battery);
                var solar=Add(machines,0,-1,IndustryId.Alternator);var wind=Add(machines,2,-1,IndustryId.Alternator);var load=Add(machines,4,-1,IndustryId.Lamp);
                var power=Build(machines);
                solar.SupplyWatts=300;wind.SupplyWatts=500;load.RequestedWatts=200;power.Allocate(2);
                Check(load.ReceivedWatts==200&&new[]{first,second,third}.All(b=>b.BatteryInputWatts==200&&BatteryPower.Amount(b)==10000),
                    "Mixed 300 W and 500 W generation serves demand then shares its 600 W surplus equally across one cable grid");
                Check(solar.DeliveredWatts+wind.DeliveredWatts==800,
                    "Delivered generator watts equal machine demand plus charged surplus");
                first.EnergyCells[0].Charge(BatteryStorage.CellCapacity-BatteryPower.Amount(first)-500);second.EnergyCells[0].Charge(BatteryStorage.CellCapacity-BatteryPower.Amount(second));
                solar.SupplyWatts=300;wind.SupplyWatts=500;load.RequestedWatts=0;power.Allocate(3);
                Check(first.BatteryInputWatts==10&&second.BatteryInputWatts==0&&third.BatteryInputWatts==790,
                    "Full batteries relinquish their parallel renewable shares to eligible storage");
                foreach(var battery in new[]{first,second,third})battery.EnergyCells[0].Charge(BatteryStorage.CellCapacity-BatteryPower.Amount(battery));
                solar.SupplyWatts=300;wind.SupplyWatts=500;power.Allocate(4);
                Check(solar.DeliveredWatts==0&&wind.DeliveredWatts==0,
                    "Fully charged storage with no machine demand accepts no renewable generation");
                third.EnergyCells[0].Discharge(BatteryPower.Amount(third));solar.SupplyWatts=300;wind.SupplyWatts=500;power.Allocate(5);
                Check(solar.DeliveredWatts+wind.DeliveredWatts==800&&third.BatteryInputWatts==800,
                    "An empty battery creates real renewable demand and receives the conserved delivered total");
            }
            Directory.CreateDirectory("Logs/Renewables");
            File.WriteAllLines("Logs/Renewables/power-checks.txt",new[]{"PASS "+passed.Count+" assertions"}.Concat(passed));
        }
    }
}
