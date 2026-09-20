using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RivetReach.Editor
{
    public static class ResourceTickChecks
    {
        sealed class World : IIndustryWorld
        {
            public readonly Dictionary<BlockPos,byte> Blocks=new Dictionary<BlockPos,byte>();
            public bool Ready(BlockPos position)=>true;
            public byte Get(BlockPos position)=>Blocks.TryGetValue(position,out byte id)?id:(byte)0;
            public bool Remove(BlockPos position,byte expected)=>Get(position)==expected&&Blocks.Remove(position);
            public ItemContainer Storage(BlockPos position)=>null;
            public byte Drop(byte block)=>block;
            public bool PlayerInside(BlockPos position)=>false;
        }
        sealed class Fixture
        {
            public readonly World World=new World();public readonly IndustrySimulation Sim;
            public Fixture(){Sim=new IndustrySimulation(World,_=>64);}
            public MachineState Add(BlockPos position,byte id){World.Blocks[position]=id;return Sim.Add(position,id);}
            public void Settle(){for(int i=0;i<1000;i++){Sim.Step();if(!Sim.Rebuilding&&Sim.Multiblocks.PendingCount==0)return;}throw new Exception("Resource topology failed to settle");}
        }
        public static void Run()
        {
            var report=new StringBuilder();int assertions=0;
            void Check(bool valid,string message){if(!valid)throw new Exception("Resource ticks: "+message);assertions++;report.AppendLine("PASS "+message);}
            using var allocationCounter=new AllocationCounter();report.AppendLine(allocationCounter.Description);
            void AllocationCheck(Action action,string message)
            {
                long result=allocationCounter.Measure(action);
                if(allocationCounter.Supported)Check(result==0,message+" (calibrated allocation detection)");
                else report.AppendLine("UNVERIFIED "+message+": no calibrated allocation counter");
            }
            {
                var fair=new FairAllocation<int>();var received=new long[8];
                Func<int,long,long> transfer=(identity,amount)=>{received[identity]+=amount;return amount;};
                for(int i=0;i<8;i++)fair.Add(i,100);fair.Distribute(80,0,transfer);fair.Clear();
                Array.Clear(received,0,received.Length);fair.Add(7,3);fair.Add(2,20);fair.Distribute(13,0,transfer);
                Check(received[7]==3&&received[2]==10&&received[0]==0,"Reused allocation entries discard prior identities/grants and redistribute fresh caps exactly");
                void Phase(){fair.Clear();for(int i=0;i<8;i++)fair.Add(i,100);fair.Distribute(79,5,transfer);}
                Phase();AllocationCheck(()=>{for(int i=0;i<1000;i++)Phase();},"Warmed capped allocation phases reuse share entries without allocation");
            }
            {
                var machines=new List<MachineState>();
                MachineState Add(int x,int z,byte id){var m=new MachineState(new BlockPos(x,20,z),id,_=>64);machines.Add(m);return m;}
                for(int x=0;x<24;x++)Add(x,0,IndustryId.PowerCable);
                var generator=Add(0,-1,IndustryId.Alternator);var loads=new[]{Add(4,1,IndustryId.Crusher),Add(13,1,IndustryId.Crusher),Add(22,1,IndustryId.Crusher)};
                var power=new PowerNetworkService();foreach(var _ in power.Topology.Rebuild(machines)){}
                NetworkTopology.Group connected=null;foreach(var group in power.Topology.Groups)if(group.Ports.Count>10)connected=group;
                bool exact=true;
                for(int tick=0;tick<connected.Ports.Count*2;tick++)
                {
                    generator.SupplyWatts=1;foreach(var load in loads)load.RequestedWatts=1;
                    MachineState expected=null;int start=tick%connected.Ports.Count;
                    for(int i=0;i<connected.Ports.Count;i++){var endpoint=connected.Ports[(start+i)%connected.Ports.Count];if(endpoint.Port.Role==PortRole.Input){expected=endpoint.Machine;break;}}
                    power.Allocate(tick);foreach(var load in loads)exact&=load.ReceivedWatts==(load==expected?1:0);
                }
                Check(exact,"Device-only power scans preserve the original full-port rotating leftover order across long cable runs");
                var battery=Add(10,-1,IndustryId.Battery);foreach(var _ in power.Topology.Rebuild(machines)){}
                generator.SupplyWatts=800;foreach(var load in loads)load.RequestedWatts=160;power.Allocate(0);
                long before=BatteryPower.Amount(battery);
                AllocationCheck(()=>{for(int i=0;i<200;i++)power.Allocate(i);},"Warmed power allocation and battery storage callbacks allocate no managed memory");
                Check(BatteryPower.Amount(battery)-before==200L*320*50,"Cached power endpoints retain exact surplus charging after topology replacement");
            }
            {
                var f=new Fixture();var min=new BlockPos(0,20,0);MachineState controller=null,valve=null;
                var bounds=new StructureBounds(min,min.Offset(4,2,2));
                for(int x=0;x<5;x++)for(int y=0;y<3;y++)for(int z=0;z<3;z++)
                {
                    var position=min.Offset(x,y,z);int edges=bounds.BoundaryAxes(position);if(edges==0)continue;
                    byte id=edges>1?IndustryId.TankFrame:IndustryId.TankWall;
                    if(y==1&&z==0)id=x==1?IndustryId.TankController:x==2?IndustryId.TankValve:x==3?IndustryId.TankPort:id;
                    var machine=f.Add(position,id);if(id==IndustryId.TankController)controller=machine;if(id==IndustryId.TankValve)valve=machine;
                }
                var source=f.Add(min.Offset(2,1,-2),IndustryId.Tank);
                var pipe=f.Add(min.Offset(2,1,-1),IndustryId.FluidPipe);pipe.PipeDirections=(2<<(5*2))|(1<<(4*2));
                var branch=f.Add(min.Offset(3,1,-1),IndustryId.FluidPipe);branch.PipeDirections=1<<(4*2);f.Settle();
                Check(controller.Structure.Formed&&!PipeConnections.FluidEnabled(valve),"Shared tank fixture forms with one closed valve and one ordinary open inlet");
                source.WaterMl=1000;f.Sim.Step();
                Check(source.WaterMl==900&&controller.Structure.Fluid.Amount==100,"A closed inlet cannot hide the receiving capacity of another open port sharing the same tank");
            }
            {
                var f=new Fixture();var start=new BlockPos(0,20,0);var a=f.Add(start,IndustryId.Tank);
                var pipe=f.Add(start.Offset(1,0,0),IndustryId.FluidPipe);pipe.PipeDirections=(2<<(1*2))|(1<<(0*2));
                var b=f.Add(start.Offset(2,0,0),IndustryId.Tank);f.Settle();int topology=f.Sim.TopologyRebuilds;
                a.Fluid.Deposit(Fluids.Water,1000);b.Fluid.Deposit(Fluids.Lava,1000);f.Sim.Step();
                Check(pipe.FluidConflict&&a.FluidConflict&&b.FluidConflict&&a.Fluid.Amount==1000&&b.Fluid.Amount==1000,"Conflicting liquids retain exact contents and mark both cached devices and route geometry");
                b.Fluid.Withdraw(1000);f.Sim.Step();
                Check(!pipe.FluidConflict&&!a.FluidConflict&&!b.FluidConflict&&b.Fluid.Fluid==Fluids.Water&&b.Fluid.Amount==100&&f.Sim.TopologyRebuilds==topology,
                    "Removing a fluid conflict refreshes device and route state without topology reconstruction");
                void FluidPhase(){a.WaterMl=1000;b.WaterMl=0;f.Sim.Step();}
                // Step also runs item routing every fifth tick. Warm complete
                // cadence cycles so initial route-cache publication and its
                // lazy dictionary views are outside the steady-state sample.
                for(int i=0;i<10;i++)FluidPhase();
                AllocationCheck(()=>{for(int i=0;i<200;i++)FluidPhase();},"Warmed ordinary fluid ticks reuse allocator entries, callbacks and identity scratch without allocation");
                Check(a.WaterMl==900&&b.WaterMl==100&&f.Sim.TopologyRebuilds==topology,"Reused fluid phase scratch retains the exact 100 mL end budget and stable topology");
            }
            Directory.CreateDirectory("Logs/ReleaseReview");File.WriteAllText("Logs/ReleaseReview/resource-tick-checks.txt",$"PASS {assertions} assertions\n"+report);
        }
    }
}
