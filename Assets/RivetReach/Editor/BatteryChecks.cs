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
            for(int x=0;x<=4;x++)Add(x,0,1,IndustryId.PowerCable);Settle();before=b.EnergyCells[0].Amount;s.Step();Check(b.EnergyCells[0].Amount-before==20000&&b.BatteryWatts==400,"Surplus generation charges at 400 W");
            b.BatteryMode=BatteryMode.DischargeOnly;before=b.EnergyCells[0].Amount;s.Step();Check(before==b.EnergyCells[0].Amount,"Discharge-only rejects generator surplus");
            b.BatteryMode=BatteryMode.Automatic;lamp=Add(0,0,-1,IndustryId.Lamp);Settle();before=b.EnergyCells[0].Amount;s.Step();Check(lamp.ReceivedWatts==20&&b.EnergyCells[0].Amount-before==19000,"Live load gets power before surplus charges battery");
            b.EnergyCells[0].Charge(b.EnergyCells[0].Capacity-b.EnergyCells[0].Amount-50);s.Step();Check(b.EnergyCells[0].Amount==b.EnergyCells[0].Capacity,"Near-full cell accepts only remaining capacity");
            // Split source and test finite depletion with proportional underpower on the last tick.
            s.Remove(alt.Position);w.Cells.Remove(alt.Position);Settle();b.EnergyCells[0].Discharge(b.EnergyCells[0].Amount-500);s.Step();Check(lamp.ReceivedWatts==10&&b.EnergyCells[0].Amount==0,"Last 0.5 J delivers 10 W without overdraft");s.Step();Check(lamp.ReceivedWatts==0,"Empty battery stops output immediately");
            Check(s.Remove(b.Position)==b,"Empty cell can be recovered");w.Cells.Remove(b.Position);
            // Bank claim/lifecycle and dimensional validation use the same service as tanks.
            var c=Add(20,0,0,IndustryId.BatteryController);var cells=new List<MachineState>();
            for(int x=20;x<=21;x++)for(int z=0;z<=1;z++)if(x!=20||z!=0)cells.Add(Add(x,0,z,IndustryId.Battery));
            foreach(var cell in cells)cell.EnergyCells[0].Charge(100000);
            Settle();Check(c.Structure.Formed&&BatteryPower.Capacity(c)==300000000,"2×1×2 solid bank combines three cells into 300 kJ");
            Check(cells.All(cell=>BatteryPower.Cells(cell).Count==0)&&s.Power.Topology.Groups.SelectMany(g=>g.Ports).Count(p=>p.Port.Role==PortRole.Storage&&p.Machine.Structure==c.Structure)==1,"Formed bank exposes exactly one power endpoint");
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
    }
}
