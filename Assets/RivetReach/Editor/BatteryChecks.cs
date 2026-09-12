using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RivetReach.Editor
{
    public static class BatteryChecks
    {
        sealed class World : IIndustryWorld
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public readonly HashSet<BlockPos> Sleeping=new HashSet<BlockPos>();
            public bool Ready(BlockPos p)=>!Sleeping.Contains(p);
            public byte Get(BlockPos p)=>Cells.TryGetValue(p,out byte b)?b:(byte)0;
            public bool Remove(BlockPos p,byte expected){if(!Ready(p)||Get(p)!=expected)return false;Cells.Remove(p);return true;}
            public ItemContainer Storage(BlockPos p)=>null;
            public byte Drop(byte b)=>b;
            public bool PlayerInside(BlockPos p)=>false;
        }
        public static void Run()
        {
            var report=new StringBuilder();int assertions=0;
            void Check(bool ok,string text){if(!ok)throw new Exception("Battery: "+text);assertions++;report.AppendLine("PASS "+text);}
            SeparateGrids(Check);
            var w=new World();var s=new IndustrySimulation(w,id=>64);
            MachineState Add(int x,int y,int z,byte id){var p=new BlockPos(x,y,z);w.Cells[p]=id;return s.Add(p,id);}
            void Settle(){for(int i=0;i<100;i++){s.Step();if(!s.Rebuilding&&s.Multiblocks.PendingCount==0)return;}throw new Exception("Battery topology stalled");}
            var b=Add(0,0,0,IndustryId.Battery);var lamp=Add(0,0,-1,IndustryId.Lamp);Settle();
            Check(b.EnergyCells[0].Amount==0&&lamp.ReceivedWatts==0,"New batteries start empty and cannot generate energy");
            Check(b.EnergyCells[0].Charge(100000)&&!b.EnergyCells[0].Charge(-1)&&!b.EnergyCells[0].Discharge(-1),"Exact storage rejects negative transfers");
            long before=b.EnergyCells[0].Amount;s.Step();Check(lamp.ReceivedWatts==20&&before-b.EnergyCells[0].Amount==1000,"20 W consumes exactly 1 J per fixed tick");
            Check(s.Remove(b.Position)==null&&w.Get(b.Position)==IndustryId.Battery,"Charged cell removal is refused");
            b.BatteryMode=BatteryMode.ChargeOnly;before=b.EnergyCells[0].Amount;s.Step();Check(lamp.ReceivedWatts==0&&before==b.EnergyCells[0].Amount,"Charge-only blocks discharge");
            b.BatteryMode=BatteryMode.Automatic;w.Sleeping.Add(b.Position);s.Invalidate();Settle();Check(lamp.ReceivedWatts==0&&before==b.EnergyCells[0].Amount,"Dormant battery provides no energy");w.Sleeping.Clear();s.Invalidate();Settle();
            s.Remove(lamp.Position);w.Cells.Remove(lamp.Position);Settle();before=b.EnergyCells[0].Amount;for(int i=0;i<100;i++)s.Step();Check(before==b.EnergyCells[0].Amount,"Idle battery retains all charge");
            var boiler=Add(3,0,0,IndustryId.Boiler);boiler.WaterMl=100000;boiler.Items.Add(BlockId.Coal,1);var alt=Add(4,0,0,IndustryId.Alternator);
            for(int x=0;x<=4;x++)Add(x,0,1,IndustryId.PowerCable);Settle();before=b.EnergyCells[0].Amount;s.Step();Check(b.EnergyCells[0].Amount-before==40000&&b.BatteryWatts==800,"Surplus generation charges at 800 W");
            b.BatteryMode=BatteryMode.DischargeOnly;before=b.EnergyCells[0].Amount;s.Step();Check(before==b.EnergyCells[0].Amount,"Discharge-only rejects generator surplus");
            b.BatteryMode=BatteryMode.Automatic;lamp=Add(0,0,-1,IndustryId.Lamp);Settle();before=b.EnergyCells[0].Amount;s.Step();Check(lamp.ReceivedWatts==20&&b.EnergyCells[0].Amount-before==39000,"Live load gets power before surplus charges battery");
            b.EnergyCells[0].Charge(b.EnergyCells[0].Capacity-b.EnergyCells[0].Amount-50);s.Step();Check(b.EnergyCells[0].Amount==b.EnergyCells[0].Capacity,"Near-full cell accepts only remaining capacity");
            // Split source and test finite depletion with proportional underpower on the last tick.
            s.Remove(alt.Position);w.Cells.Remove(alt.Position);Settle();b.EnergyCells[0].Discharge(b.EnergyCells[0].Amount-500);s.Step();Check(lamp.ReceivedWatts==10&&b.EnergyCells[0].Amount==0,"Last 0.5 J delivers 10 W without overdraft");s.Step();Check(lamp.ReceivedWatts==0,"Empty battery stops output immediately");
            Check(s.Remove(b.Position)==b,"Empty cell can be recovered");w.Cells.Remove(b.Position);
            // Bank claim/lifecycle and dimensional validation use the same service as tanks.
            var c=Add(20,0,0,IndustryId.BatteryController);var cells=new List<MachineState>();
            for(int x=20;x<=21;x++)for(int z=0;z<=1;z++)if(x!=20||z!=0)cells.Add(Add(x,0,z,IndustryId.Battery));
            foreach(var cell in cells)cell.EnergyCells[0].Charge(100000);
            Settle();Check(c.Structure.Formed&&BatteryPower.Capacity(c)==300000000,"2×1×2 solid bank combines three cells into 300 kJ");
            Check(cells.All(cell=>BatteryPower.Cells(cell).Count==0)&&s.Power.Topology.Groups.SelectMany(g=>g.Ports).Where(p=>p.Port.Role==PortRole.Storage&&p.Machine.Structure==c.Structure).Select(p=>p.Machine).Distinct().Count()==1,"Formed bank exposes exactly one power endpoint");
            var bankLamp=Add(20,0,-1,IndustryId.Lamp);Settle();before=BatteryPower.Amount(c);s.Step();Check(bankLamp.ReceivedWatts==20&&before-BatteryPower.Amount(c)==1000,"Bank delivers through outward controller socket");
            c.BatteryMode=BatteryMode.Isolated;before=BatteryPower.Amount(c);s.Step();Check(before==BatteryPower.Amount(c)&&bankLamp.ReceivedWatts==0,"Isolated bank stops all transfers");
            w.Sleeping.Add(cells[0].Position);s.Multiblocks.ResidencyChanged();Settle();Check(!c.Structure.Formed&&bankLamp.ReceivedWatts==0,"Incomplete residency suspends bank output");w.Sleeping.Clear();s.Multiblocks.ResidencyChanged();Settle();Check(c.Structure.Formed&&before==BatteryPower.Amount(c),"Residency restores exact stored energy without offline credit");
            var victim=cells[0];victim.EnergyCells[0].Discharge(victim.EnergyCells[0].Amount);long total=cells.Sum(m=>m.EnergyCells[0].Amount);Check(s.Remove(victim.Position)==victim,"Empty bank cell can be removed individually");w.Cells.Remove(victim.Position);s.Multiblocks.Changed(victim.Position);Settle();Check(!c.Structure.Formed&&cells.Sum(m=>m.EnergyCells[0].Amount)==total,"Missing cell invalidates bank without losing other cells' charge");
            var replacement=Add((int)victim.Position.X,victim.Position.Y,(int)victim.Position.Z,IndustryId.Battery);Settle();Check(c.Structure.Formed&&BatteryPower.Amount(c)==total,"Repair with empty cell preserves charge");
            Check(s.Remove(c.Position)==c,"Controller can be removed because it owns no charge");w.Cells.Remove(c.Position);Settle();Check(cells.Skip(1).All(m=>m.Structure==null)&&cells.Sum(m=>m.EnergyCells[0].Amount)==total,"Dismantling returns cells to independent operation without duplication");
            c=Add(20,0,0,IndustryId.BatteryController);Settle();s.Rotate(c);s.Rotate(c);Settle();Check(!c.Structure.Formed,"Inward controller is rejected");s.Rotate(c);s.Rotate(c);Settle();Check(c.Structure.Formed,"Outward rotation repairs bank");
            var other=Add(22,0,0,IndustryId.BatteryController);Settle();Check(!other.Structure.Formed&&!c.Structure.Formed,"Touching controllers cannot claim the same battery pack");
            // A pump resumes retained partial work without depending on battery power.
            var pump=Add(40,0,0,IndustryId.Pump);w.Cells[pump.Position.Offset(0,-1,0)]=Fluids.Water.Source;pump.Work=39;Settle();for(int i=0;i<100;i++)s.Step();Check(pump.Status==MachineStatus.OutputFull&&pump.WaterMl==10000&&pump.Work==0&&pump.RequestedWatts==0&&pump.ReceivedWatts==0&&w.Get(pump.Position.Offset(0,-1,0))==0,"Pump completes retained partial work without electricity and stops at capacity");
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/battery-checks.txt",report+"Assertions: "+assertions+"\n");
        }
        static void SeparateGrids(Action<bool,string> check)
        {
            var w=new World();var s=new IndustrySimulation(w,_=>64);
            MachineState Add(int x,int z,byte id){var p=new BlockPos(x,10,z);w.Cells[p]=id;return s.Add(p,id);}
            void Remove(int x,int z){var p=new BlockPos(x,10,z);s.Remove(p);w.Cells.Remove(p);}
            void Settle(){for(int i=0;i<100;i++){s.Step();if(!s.Rebuilding&&s.Multiblocks.PendingCount==0)return;}throw new Exception("Power grids stalled");}
            var boiler=Add(-4,0,IndustryId.Boiler);boiler.WaterMl=100000;boiler.Items.Add(BlockId.Coal,2);
            var alt=Add(-3,0,IndustryId.Alternator);Add(-2,0,IndustryId.PowerCable);Add(-1,0,IndustryId.PowerCable);
            var battery=Add(0,0,IndustryId.Battery);Add(1,0,IndustryId.PowerCable);var crusher=Add(2,0,IndustryId.Crusher);crusher.Items.Add(BlockId.RawIron,64,0,1);
            Settle();long before=BatteryPower.Amount(battery);s.Step();
            check(alt.SupplyWatts==800&&crusher.ReceivedWatts==160&&BatteryPower.Amount(battery)-before==32000&&battery.BatteryInputWatts==800&&battery.BatteryOutputWatts==160,"Separate generator and crusher grids share battery energy: 800 W in, 160 W out, exactly 32 J stored per tick");
            check(!s.Power.Topology.Groups.Any(g=>g.Ports.Any(p=>p.Machine==alt)&&g.Ports.Any(p=>p.Machine==crusher)),"Battery faces do not join disconnected cable grids");
            battery.EnergyCells[0].Discharge(BatteryPower.Amount(battery));s.Step();
            check(crusher.ReceivedWatts==160&&BatteryPower.Amount(battery)==32000,"An empty battery supplies a separate grid in the same tick it charges");
            Add(-1,1,IndustryId.PowerCable);Settle();Remove(-1,1);Settle();before=BatteryPower.Amount(battery);s.Step();
            check(crusher.ReceivedWatts==160&&BatteryPower.Amount(battery)-before==32000,"Breaking an unused cable branch recalculates and preserves generation and charging");
            Remove(-2,0);Settle();before=BatteryPower.Amount(battery);s.Step();
            check(battery.BatteryInputWatts==0&&crusher.ReceivedWatts==160&&before-BatteryPower.Amount(battery)==8000,"Breaking the generator cable leaves the independent output grid powered from storage");
            Add(-2,0,IndustryId.PowerCable);Settle();before=BatteryPower.Amount(battery);s.Step();
            check(BatteryPower.Amount(battery)-before==32000,"Replacing a broken cable restores charging without replacing any machine");
            Remove(1,0);Settle();before=BatteryPower.Amount(battery);s.Step();
            check(crusher.ReceivedWatts==0&&BatteryPower.Amount(battery)-before==40000,"Breaking the load cable stores the entire 800 W surplus");
            Add(1,0,IndustryId.PowerCable);Settle();
            battery.BatteryMode=BatteryMode.Isolated;before=BatteryPower.Amount(battery);s.Step();
            check(crusher.ReceivedWatts==0&&BatteryPower.Amount(battery)==before,"Isolated battery cannot act as a hidden cable bridge");
            battery.BatteryMode=BatteryMode.ChargeOnly;s.Step();check(crusher.ReceivedWatts==0&&battery.BatteryInputWatts==800,"Charge-only obeyed across separate grids");
            battery.BatteryMode=BatteryMode.DischargeOnly;s.Step();check(crusher.ReceivedWatts==160&&battery.BatteryInputWatts==0,"Discharge-only obeyed across separate grids");
            battery.BatteryMode=BatteryMode.Automatic;
            battery.EnergyCells[0].Charge(BatteryStorage.CellCapacity-BatteryPower.Amount(battery));s.Step();
            check(BatteryPower.Amount(battery)==BatteryStorage.CellCapacity&&crusher.ReceivedWatts==160&&battery.BatteryInputWatts==160,"Full battery supplies other grid and replaces exactly the consumed charge");
            // Join the actual cable runs around the battery, including two faces on one grid.
            for(int x=-1;x<=1;x++)Add(x,1,IndustryId.PowerCable);Settle();
            check(s.Power.Topology.Groups.Any(g=>g.Ports.Any(p=>p.Machine==alt)&&g.Ports.Any(p=>p.Machine==crusher)),"Only a physical cable path joins the generator and crusher grids");
            battery.EnergyCells[0].Discharge(100000);before=BatteryPower.Amount(battery);s.Step();
            check(battery.BatteryInputWatts==640&&battery.BatteryOutputWatts==0&&BatteryPower.Amount(battery)-before==32000,"Multiple battery faces on one grid count capacity once and serve loads directly first");
            Remove(0,1);Settle();check(!s.Power.Topology.Groups.Any(g=>g.Ports.Any(p=>p.Machine==alt)&&g.Ports.Any(p=>p.Machine==crusher)),"Removing the joining cable splits the grids again");
            // Two real generators, one cell: all surplus is captured above the former 400 W cap.
            var boiler2=Add(-4,-2,IndustryId.Boiler);boiler2.WaterMl=100000;boiler2.Items.Add(BlockId.Coal,1);
            Add(-3,-2,IndustryId.Alternator);Add(-3,-1,IndustryId.PowerCable);Add(-2,-1,IndustryId.PowerCable);battery.EnergyCells[0].Discharge(1000000);crusher.Items.Take(0,64);Settle();
            before=BatteryPower.Amount(battery);s.Step();check(battery.BatteryInputWatts==1600&&BatteryPower.Amount(battery)-before==80000,"One cell captures all 1600 W from two generators without the former rate cap");
            // Shared generator budgets across multiple disconnected terminal faces.
            var extra=Add(-3,1,IndustryId.Battery);Settle();
            battery.EnergyCells[0].Discharge(BatteryPower.Amount(battery));extra.EnergyCells[0].Discharge(BatteryPower.Amount(extra));s.Step();
            check(BatteryPower.Amount(battery)+BatteryPower.Amount(extra)==80000,"A generator attached to separate grids cannot duplicate its output");
            boiler.WaterMl=boiler2.WaterMl=0;s.Step();before=BatteryPower.Amount(battery)+BatteryPower.Amount(extra);s.Step();
            check(BatteryPower.Amount(battery)+BatteryPower.Amount(extra)==before&&battery.BatteryInputWatts==0&&extra.BatteryInputWatts==0,"Stored charge never circulates between batteries without a machine load");
            // Shared demand and finite energy: a load connected on several grids is paid once.
            var f=new World();var sim=new IndustrySimulation(f,_=>64);
            MachineState Put(int x,int z,byte id){var p=new BlockPos(x,0,z);f.Cells[p]=id;return sim.Add(p,id);}
            var load=Put(0,0,IndustryId.Crusher);load.Items.Add(BlockId.RawIron,64,0,1);
            var a=Put(-1,0,IndustryId.Battery);var b=Put(1,0,IndustryId.Battery);a.EnergyCells[0].Charge(4000);b.EnergyCells[0].Charge(4000);
            for(int i=0;i<100;i++){sim.Step();if(!sim.Rebuilding&&sim.Multiblocks.PendingCount==0)break;}
            check(load.ReceivedWatts==160&&BatteryPower.Amount(a)+BatteryPower.Amount(b)==0,"A multi-grid load combines exact remaining energy without duplicate demand or overdraft");
            // One cell can meet combined demand above the old discharge cap.
            a.EnergyCells[0].Charge(100000);var c=Put(-2,0,IndustryId.Crusher);var d=Put(-1,1,IndustryId.Crusher);c.Items.Add(BlockId.RawIron,64,0,1);d.Items.Add(BlockId.RawIron,64,0,1);
            for(int i=0;i<100;i++){sim.Step();if(!sim.Rebuilding&&sim.Multiblocks.PendingCount==0)break;}
            check(load.ReceivedWatts+c.ReceivedWatts+d.ReceivedWatts==480&&BatteryPower.Amount(a)==76000,"One cell serves all 480 W of connected demand with exact energy conservation");
        }
    }
}
