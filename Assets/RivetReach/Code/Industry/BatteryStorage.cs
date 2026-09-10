using System;
using System.Collections.Generic;

namespace RivetReach
{
    public enum BatteryMode { Automatic, ChargeOnly, DischargeOnly, Isolated }
    // Exact energy: one watt for one 50 ms simulation step is 50 millijoules.
    public sealed class BatteryStorage
    {
        public const long CellCapacity=100000000;
        public const int CellWatts=400, MillijoulesPerWattTick=50;
        public long Amount {get;private set;}
        public long Capacity=>CellCapacity;
        public bool Charge(long amount){if(amount<0||amount>Capacity-Amount)return false;Amount+=amount;return true;}
        public bool Discharge(long amount){if(amount<0||amount>Amount)return false;Amount-=amount;return true;}
    }
    public sealed class BatteryBankData : IMultiblockMachineData
    {
        public readonly List<BatteryStorage> Cells=new List<BatteryStorage>();
        // Charge belongs to the individually mineable cells, never to the controller.
        public bool CanDismantle=>true;
        public string TryForm(MultiblockValidation validation)=>null;
    }
    public static class BatteryPower
    {
        public static IReadOnlyList<BatteryStorage> Cells(MachineState m)
            =>m.Definition.Id==IndustryId.Battery?(m.Structure==null?m.EnergyCells:Array.Empty<BatteryStorage>()):m.Structure?.Formed==true?(m.Structure.MachineData as BatteryBankData)?.Cells??(IReadOnlyList<BatteryStorage>)Array.Empty<BatteryStorage>():Array.Empty<BatteryStorage>();
        public static long Amount(MachineState m){long sum=0;foreach(var cell in Cells(m))sum+=cell.Amount;return sum;}
        public static long Capacity(MachineState m)=>(long)Cells(m).Count*BatteryStorage.CellCapacity;
        public static int Available(MachineState m,bool charge)
        {
            if(m.BatteryMode==BatteryMode.Isolated||charge&&m.BatteryMode==BatteryMode.DischargeOnly||!charge&&m.BatteryMode==BatteryMode.ChargeOnly)return 0;
            int watts=0;foreach(var cell in Cells(m))watts+=(int)Math.Min(BatteryStorage.CellWatts,(charge?cell.Capacity-cell.Amount:cell.Amount)/BatteryStorage.MillijoulesPerWattTick);return watts;
        }
        public static void Transfer(MachineState m,int watts,bool charge)
        {
            foreach(var cell in Cells(m))
            {
                int take=(int)Math.Min(watts,Math.Min(BatteryStorage.CellWatts,(charge?cell.Capacity-cell.Amount:cell.Amount)/BatteryStorage.MillijoulesPerWattTick));
                long energy=(long)take*BatteryStorage.MillijoulesPerWattTick;
                if(!(charge?cell.Charge(energy):cell.Discharge(energy)))throw new InvalidOperationException("Invalid battery reservation");
                watts-=take;m.BatteryWatts+=charge?take:-take;if(watts==0)break;
            }
            if(watts!=0)throw new InvalidOperationException("Unfulfilled battery reservation");
        }
    }
}
