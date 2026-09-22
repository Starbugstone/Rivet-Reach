using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RivetReach.Editor
{
    public static class GridAllocationChecks
    {
        sealed class World : IIndustryWorld
        {
            public readonly Dictionary<BlockPos,byte> Blocks=new Dictionary<BlockPos,byte>();
            public readonly Dictionary<BlockPos,ItemContainer> Chests=new Dictionary<BlockPos,ItemContainer>();
            public bool Ready(BlockPos p)=>true;
            public byte Get(BlockPos p)=>Blocks.TryGetValue(p,out var b)?b:(byte)0;
            public bool Remove(BlockPos p,byte expected)=>Blocks.Remove(p);
            public ItemContainer Storage(BlockPos p)=>Chests.TryGetValue(p,out var c)?c:null;
            public byte Drop(byte b)=>b;
            public bool PlayerInside(BlockPos p)=>false;
        }
        public static void Run()
        {
            var log=new StringBuilder();int assertions=0;
            void Check(bool ok,string message){if(!ok)throw new Exception("Grid allocation: "+message);assertions++;log.AppendLine("PASS "+message);}
            // Generated caps exercise redistribution, exact totals and integer fairness.
            var random=new Random(173);var fair=new FairAllocation<int>();
            for(int trial=0;trial<300;trial++)
            {
                int count=random.Next(1,16);var caps=new long[count];var got=new long[count];fair.Clear();
                for(int i=0;i<count;i++){caps[i]=random.Next(0,1000);fair.Add(i,caps[i]);}
                int budget=random.Next(0,10000);long used=fair.Distribute(budget,trial,(i,n)=>{got[i]+=n;return n;});
                Check(used==Math.Min(budget,caps.Sum())&&got.Sum()==used&&got.Select((n,i)=>n<=caps[i]).All(x=>x),"Conserved capped allocation "+trial);
                Check(got.Select((n,i)=>(n,i)).Where(x=>x.n<caps[x.i]).All(x=>got.All(n=>n<=x.n+1)),"Equal uncapped shares "+trial);
            }
            var totals=new long[3];for(int tick=0;tick<3;tick++){fair.Clear();for(int i=0;i<3;i++)fair.Add(i,10);fair.Distribute(1,tick,(i,n)=>{totals[i]+=n;return n;});}
            Check(totals.All(n=>n==1),"Indivisible residual rotates between eligible identities");
            Power(Check);PowerReferenceChecks(Check);Pipes(Check,false);Pipes(Check,true);
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/grid-allocation-checks.txt",log+"Assertions: "+assertions+"\n");
        }
        static void Power(Action<bool,string> check)
        {
            var machines=new List<MachineState>();
            MachineState Add(int x,int z,byte id){var m=new MachineState(new BlockPos(x,0,z),id,_=>64);machines.Add(m);return m;}
            for(int x=0;x<=4;x++)Add(x,0,IndustryId.PowerCable);
            var cells=new[]{Add(0,1,IndustryId.Battery),Add(2,1,IndustryId.Battery),Add(4,1,IndustryId.Battery)};
            var alt=Add(0,-1,IndustryId.Alternator);var load=Add(4,-1,IndustryId.Crusher);
            var power=new PowerNetworkService();foreach(var _ in power.Topology.Rebuild(machines)){}
            void Empty(){foreach(var cell in cells)cell.EnergyCells[0].Discharge(BatteryPower.Amount(cell));}
            alt.SupplyWatts=800;load.RequestedWatts=200;power.Allocate(0);
            check(cells.All(c=>c.BatteryInputWatts==200&&BatteryPower.Amount(c)==10000)&&load.ReceivedWatts==200,"600 W surplus splits 200 W into each of three batteries after demand");
            alt.SupplyWatts=0;load.RequestedWatts=300;power.Allocate(1);
            check(cells.All(c=>c.BatteryOutputWatts==100&&BatteryPower.Amount(c)==5000),"300 W deficit draws 100 W from each battery");
            Empty();cells[0].EnergyCells[0].Charge(500);cells[1].EnergyCells[0].Charge(10000);power.Allocate(2);
            check(cells[0].BatteryOutputWatts==10&&cells[1].BatteryOutputWatts==200&&cells[2].BatteryOutputWatts==0&&load.ReceivedWatts==210,"Empty and exhausted batteries redistribute deficit without overdraft");
            Empty();alt.SupplyWatts=800;load.RequestedWatts=0;cells[0].EnergyCells[0].Charge(BatteryStorage.CellCapacity-500);cells[1].EnergyCells[0].Charge(BatteryStorage.CellCapacity);power.Allocate(3);
            check(cells[0].BatteryInputWatts==10&&cells[1].BatteryInputWatts==0&&cells[2].BatteryInputWatts==790,"Near-full and full batteries redistribute all excess");
            Empty();cells[0].BatteryMode=BatteryMode.DischargeOnly;power.Allocate(4);
            check(cells[0].BatteryInputWatts==0&&cells[1].BatteryInputWatts==400&&cells[2].BatteryInputWatts==400,"Equal charging excludes discharge-only battery");
            cells[0].BatteryMode=BatteryMode.Automatic;Empty();
            var sum=new int[3];for(int t=0;t<3;t++){power.Allocate(t);for(int i=0;i<3;i++)sum[i]+=cells[i].BatteryInputWatts;}
            check(sum.All(n=>n==800),"800 W divided over three batteries rotates fractional-watt leftovers");
        }
        static void PowerReferenceChecks(Action<bool,string> check)
        {
            var machines=new List<MachineState>();
            MachineState Add(int x,int z,byte id){var m=new MachineState(new BlockPos(x,0,z),id,_=>64);machines.Add(m);return m;}
            for(int x=0;x<=8;x++){Add(x,0,IndustryId.PowerCable);Add(x,2,IndustryId.PowerCable);}
            foreach(int x in new[]{0,4,8})Add(x,1,IndustryId.Battery);
            Add(0,-1,IndustryId.Alternator);Add(8,3,IndustryId.Alternator);Add(2,1,IndustryId.Alternator);
            foreach(int x in new[]{2,4,6}){Add(x,-1,IndustryId.Crusher);Add(x,3,IndustryId.Lamp);}
            Add(40,0,IndustryId.Crusher);Add(42,0,IndustryId.Alternator);Add(44,0,IndustryId.Battery);
            var power=new PowerNetworkService();var reference=new LegacyPowerAllocator(power.Topology);var random=new Random(2761);
            void Rebuild(){foreach(var unit in power.Topology.Rebuild(machines)){}}
            Rebuild();
            string State()=>string.Join(";",machines.Select(m=>$"{m.ReceivedWatts},{m.DeliveredWatts},{m.BatteryWatts},{m.BatteryInputWatts},{m.BatteryOutputWatts}:{string.Join(",",m.EnergyCells.Select(c=>c.Amount))}"))+
                "|"+string.Join(";",power.Topology.Groups.Select(g=>$"{g.Supply},{g.Demand}"));
            for(int trial=0;trial<160;trial++)
            {
                if(trial%20==0)
                {
                    // Changed BFS start order stresses the historical all-port remainder cursor.
                    var first=machines[0];machines.RemoveAt(0);machines.Add(first);Rebuild();
                }
                if(trial%20==10)
                {
                    var published=power.Topology.Groups;
                    using(var pending=power.Topology.Rebuild(machines).GetEnumerator())for(int n=0;n<5;n++)pending.MoveNext();
                    check(ReferenceEquals(published,power.Topology.Groups),"Cancelled power rebuild retains published groups "+trial);
                }
                foreach(var m in machines)
                {
                    m.Priority=random.Next(3);m.SupplyWatts=random.Next(801);m.RequestedWatts=random.Next(401);
                    m.ReceivedWatts=17;m.DeliveredWatts=19;m.BatteryWatts=23;m.BatteryInputWatts=29;m.BatteryOutputWatts=31;
                    m.BatteryMode=(BatteryMode)random.Next(4);
                    foreach(var cell in m.EnergyCells){cell.Discharge(cell.Amount);long amount=trial%3==0?random.Next(20000):trial%3==1?cell.Capacity-random.Next(20000):random.Next((int)cell.Capacity);cell.Charge(amount);}
                }
                foreach(var group in power.Topology.Groups){group.Activity=new NetworkActivity{Active=random.Next(5)!=0};group.Supply=37;group.Demand=41;}
                var initial=machines.Select(m=>(m,received:m.ReceivedWatts,delivered:m.DeliveredWatts,battery:m.BatteryWatts,input:m.BatteryInputWatts,output:m.BatteryOutputWatts,energy:m.EnergyCells.Select(c=>c.Amount).ToArray())).ToArray();
                reference.Allocate(trial);string expected=State();
                foreach(var saved in initial)
                {
                    var m=saved.m;m.ReceivedWatts=saved.received;m.DeliveredWatts=saved.delivered;m.BatteryWatts=saved.battery;m.BatteryInputWatts=saved.input;m.BatteryOutputWatts=saved.output;
                    for(int i=0;i<m.EnergyCells.Length;i++){m.EnergyCells[i].Discharge(m.EnergyCells[i].Amount);m.EnergyCells[i].Charge(saved.energy[i]);}
                }
                foreach(var group in power.Topology.Groups){group.Supply=37;group.Demand=41;}
                power.Allocate(trial);
                check(State()==expected,"Cached power roles match pre-optimization allocation, shared budgets, cell rounding and display state "+trial);
            }
        }
        static void Pipes(Action<bool,string> check,bool fluid)
        {
            var w=new World();var sim=new IndustrySimulation(w,_=>64);var center=new BlockPos(0,20,0);
            byte type=fluid?IndustryId.FluidPipe:IndustryId.ItemPipe;
            w.Blocks[center]=type;var pipe=sim.Add(center,type);
            var tanks=new MachineState[4];var chests=new ItemContainer[4];
            for(int face=0;face<4;face++)
            {
                var pos=IndustryDefinition.Neighbor(center,face);
                if(fluid){w.Blocks[pos]=IndustryId.Tank;tanks[face]=sim.Add(pos,IndustryId.Tank);}
                else{w.Blocks[pos]=BlockId.Chest;chests[face]=new ItemContainer(1,_=>64);w.Chests[pos]=chests[face];}
                // Two sources, two sinks, each a separate identity on the same run.
                pipe.PipeDirections|=(face<2?2:1)<<(face*2);
            }
            for(int i=0;i<100;i++){sim.Step();if(!sim.Rebuilding&&sim.Multiblocks.PendingCount==0)break;}
            void Phase(){if(fluid)sim.Step();else do{sim.Step();}while(sim.Tick%5!=0);}
            if(fluid)
            {
                tanks[0].WaterMl=tanks[1].WaterMl=1000;Phase();
                check(tanks[0].WaterMl==900&&tanks[1].WaterMl==900&&tanks[2].WaterMl==100&&tanks[3].WaterMl==100,"Fluid grid equally draws from two outputs and fills two inputs");
                tanks[2].WaterMl=(int)tanks[2].Fluid.Capacity-30;tanks[3].WaterMl=(int)tanks[3].Fluid.Capacity-70;Phase();
                check(tanks[0].WaterMl==850&&tanks[1].WaterMl==850&&tanks[2].Fluid.Amount==tanks[2].Fluid.Capacity&&tanks[3].Fluid.Amount==tanks[3].Fluid.Capacity,"Limited fluid demand draws equally and redistributes near-full destination shares");
                tanks[0].WaterMl=10;tanks[1].WaterMl=1000;tanks[2].WaterMl=tanks[3].WaterMl=0;Phase();
                check(tanks[0].WaterMl==0&&tanks[1].WaterMl==900&&tanks[2].WaterMl==55&&tanks[3].WaterMl==55,"Fluid source shortage redistributes exact 110 mL equally");
            }
            else
            {
                chests[0].Add(BlockId.Stone,10);chests[1].Add(BlockId.Stone,10);Phase();
                check(chests[0].Total(BlockId.Stone)==9&&chests[1].Total(BlockId.Stone)==9&&chests[2].Total(BlockId.Stone)==1&&chests[3].Total(BlockId.Stone)==1,"Two item sources share both compatible inputs in one phase");
                chests[2].Add(BlockId.Stone,63);int before=chests[3].Total(BlockId.Stone);Phase();
                check(chests[2].Total(BlockId.Stone)==64&&chests[3].Total(BlockId.Stone)==before+2,"Full item destination redistributes to remaining receiver");
                chests[2].Take(0,64);chests[3].Take(0,64);chests[1].Take(0,64);
                for(int i=0;i<4;i++)Phase();
                check(chests[2].Total(BlockId.Stone)==2&&chests[3].Total(BlockId.Stone)==2,"Single item source alternates indivisible cargo equally across phases");
            }
            // Editing an unused branch rebuilds without interrupting the surviving run.
            var branch=IndustryDefinition.Neighbor(center,4);w.Blocks[branch]=type;sim.Add(branch,type);
            for(int i=0;i<100;i++){sim.Step();if(!sim.Rebuilding&&sim.Multiblocks.PendingCount==0)break;}
            sim.Remove(branch);w.Blocks.Remove(branch);
            for(int i=0;i<100;i++){sim.Step();if(!sim.Rebuilding&&sim.Multiblocks.PendingCount==0)break;}
            long stored=fluid?tanks[2].WaterMl+tanks[3].WaterMl:chests[2].Total(BlockId.Stone)+chests[3].Total(BlockId.Stone);Phase();
            check((fluid?tanks[2].WaterMl+tanks[3].WaterMl:chests[2].Total(BlockId.Stone)+chests[3].Total(BlockId.Stone))>stored,"Removing unused "+(fluid?"fluid":"item")+" branch preserves transport");
        }
        // Frozen pre-optimization allocator used as an independent behavioral oracle.
        sealed class LegacyPowerAllocator
    {
        public readonly NetworkTopology Topology;
        readonly Dictionary<MachineState,int> generation=new Dictionary<MachineState,int>();
        readonly FairAllocation<MachineState> storageShares=new FairAllocation<MachineState>();
        readonly Func<MachineState,long,long> storageTransfer;
        bool storageCharge;
        public LegacyPowerAllocator(NetworkTopology topology){Topology=topology;storageTransfer=TransferReservedStorage;}
        long TransferReservedStorage(MachineState machine,long amount)
        {BatteryPower.Transfer(machine,(int)amount,storageCharge);return amount;}
        // Shared device budgets prevent multiple faces/grids from duplicating generation,
        // demand or storage. Only cable vertices connect grids, never a device interior.
        public void Allocate(long tick)
        {

            {
                generation.Clear();
                foreach(var group in Topology.Groups)
                {
                    if(!group.Active)continue;
                    group.Supply=group.Demand=0;
                    foreach(var p in group.Ports)
                    {
                        if(p.Port.Role==PortRole.Route)continue;
                        var m=p.Machine;
                        m.DeliveredWatts=0;
                        if(p.Port.Role==PortRole.Output)generation[m]=m.SupplyWatts;
                        if(p.Port.Role==PortRole.Input){m.ReceivedWatts=0;group.Demand+=m.RequestedWatts;}
                        if(p.Port.Role==PortRole.Storage)m.BatteryWatts=m.BatteryInputWatts=m.BatteryOutputWatts=0;
                    }
                }
            }
            // First all ordinary loads consume generation, before any storage charge.

            {
                foreach(var group in Topology.Groups)
                {
                    if(!group.Active)continue;
                    int used=Serve(group,AvailableGeneration(group),tick);
                    ConsumeGeneration(group,used);group.Supply+=used;
                }
            }
            // Charge before discharge so even an empty battery can relay this tick's
            // generation to a separate load grid, independent of traversal order.
            ChargeSurplus(tick);

            {
                foreach(var group in Topology.Groups)
                {
                    if(!group.Active)continue;
                    int available=0;
                    foreach(var p in group.Ports)if(p.Port.Role==PortRole.Storage)available=(int)Math.Min(int.MaxValue,(long)available+BatteryPower.Available(p.Machine,false));
                    int used=Serve(group,available,tick);group.Supply+=used;
                    TransferStorage(group,used,false,tick);
                }
            }
            // A full battery may have freed room while feeding another grid.
            ChargeSurplus(tick);
        }
        int AvailableGeneration(NetworkTopology.Group group)
        {
            int watts=0;
            foreach(var p in group.Ports)if(p.Port.Role==PortRole.Output)watts+=generation[p.Machine];
            return watts;
        }
        void ConsumeGeneration(NetworkTopology.Group group,int watts)
        {
            foreach(var p in group.Ports)if(p.Port.Role==PortRole.Output&&watts>0)
            {int take=Math.Min(watts,generation[p.Machine]);generation[p.Machine]-=take;p.Machine.DeliveredWatts+=take;watts-=take;}
        }
        int TransferStorage(NetworkTopology.Group group,int watts,bool charge,long tick)
        {
            if(watts<=0)return 0;
            storageShares.Clear();
            foreach(var p in group.Ports)if(p.Port.Role==PortRole.Storage)storageShares.Add(p.Machine,BatteryPower.Available(p.Machine,charge));
            storageCharge=charge;
            return (int)storageShares.Distribute(watts,tick,storageTransfer);
        }
        void ChargeSurplus(long tick)
        {
            foreach(var group in Topology.Groups)
            {
                if(!group.Active)continue;
                int used=TransferStorage(group,AvailableGeneration(group),true,tick);
                ConsumeGeneration(group,used);group.Supply+=used;
            }
        }
        static int Need(MachineState m)=>Math.Max(0,m.RequestedWatts-m.ReceivedWatts);
        // Whole watts, priorities and proportional shortfall within each cable grid.
        static int Serve(NetworkTopology.Group group,int supply,long tick)
        {
            int total=0;
            for(int priority=0;priority<3&&supply>0;priority++)
            {
                int requested=0;
                foreach(var p in group.Ports)if(p.Port.Role==PortRole.Input&&p.Machine.Priority==priority)requested+=Need(p.Machine);
                if(requested==0)continue;
                int available=Math.Min(supply,requested),used=0;
                foreach(var p in group.Ports)if(p.Port.Role==PortRole.Input&&p.Machine.Priority==priority)
                {int amount=(int)((long)Need(p.Machine)*available/requested);p.Machine.ReceivedWatts+=amount;used+=amount;}
                int residual=available-used,start=(int)(tick%group.Ports.Count);
                // Preserve the original all-port cursor, including route positions,
                // while visiting only devices on either side of its wrap point.
                for(int pass=0;pass<2&&residual>0;pass++)
                foreach(var p in group.Ports)
                {
                    if(residual==0)break;
                    if(pass==0?group.Ports.IndexOf(p)<start:group.Ports.IndexOf(p)>=start)continue;
                    if(p.Port.Role==PortRole.Input&&p.Machine.Priority==priority&&Need(p.Machine)>0)
                    {p.Machine.ReceivedWatts++;residual--;}
                }
                supply-=available;total+=available;
            }
            return total;
        }
    }
    }
}
