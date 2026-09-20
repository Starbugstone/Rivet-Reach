using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RivetReach.Editor
{
    public static class RenewableChecks
    {
        sealed class World:IIndustryWorld,IRenewableEnvironment
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public bool Open=true,Resident=true;public double Hour {get;set;}=12;
            public double WindGust=>1;
            public float CloudCover {get;set;}=.08f;public float WindStrength {get;set;}=.18f;
            public bool SkyExposed(BlockPos p)=>Open;public bool Ready(BlockPos p)=>Resident;
            public byte Get(BlockPos p)=>Cells.TryGetValue(p,out byte id)?id:(byte)0;
            public bool Remove(BlockPos p,byte expected)=>false;public ItemContainer Storage(BlockPos p)=>null;
            public byte Drop(byte block)=>block;public bool PlayerInside(BlockPos p)=>false;
        }
        public static void Run()
        {
            int assertions=0;var report=new StringBuilder();
            void Check(bool ok,string message){if(!ok)throw new Exception(message);assertions++;report.AppendLine("PASS "+message);}
            var config=RenewableCatalog.Current;config.Validate();
            int[] expectedSolar={400,200,60},expectedWind={140,360,400};
            foreach(WeatherKind kind in Enum.GetValues(typeof(WeatherKind)))
            {
                WeatherState.Profile(kind,out float cloud,out _,out float wind);
                Check(config.Output(IndustryId.SolarPanel,12,cloud,wind)==expectedSolar[(int)kind],kind+" noon solar output");
                Check(config.Output(IndustryId.WindTurbine,0,cloud,wind)==expectedWind[(int)kind],kind+" midnight wind output");
                foreach(double hour in new[]{0,5.99,6,18,23.99})Check(config.Output(IndustryId.SolarPanel,hour,cloud,wind)==0,kind+" solar off at "+hour);
            }
            Check(config.Output(IndustryId.SolarPanel,9,.08f,.18f)==282,"Solar follows daylight elevation");
            Check(config.Output(IndustryId.SolarPanel,15,.08f,.18f)==282,"Evening mirrors morning output");
            int previous=0;for(int minute=360;minute<=720;minute++){int watts=config.Output(IndustryId.SolarPanel,minute/60.0,.08f,.18f);Check(watts>=previous,"Solar rises smoothly at minute "+minute);previous=watts;}
            for(int minute=721;minute<=1080;minute++){int watts=config.Output(IndustryId.SolarPanel,minute/60.0,.08f,.18f);Check(watts<=previous,"Solar falls smoothly at minute "+minute);previous=watts;}
            var weather=new WeatherState(12);weather.SetWeather(WeatherKind.Storm);int lastSolar=400,lastWind=140;
            for(int i=0;i<WeatherState.TransitionDurationTicks;i++)
            {
                weather.Advance(1);int solar=config.Output(IndustryId.SolarPanel,12,weather.CloudCover,weather.WindStrength),wind=config.Output(IndustryId.WindTurbine,12,weather.CloudCover,weather.WindStrength);
                Check(solar<=lastSolar&&wind>=lastWind,"Monotonic weather transition tick "+i);lastSolar=solar;lastWind=wind;
            }
            double last=RenewableWind.Sample(123,0,config.windGustTicks),min=1,max=0;
            for(int tick=1;tick<=4800;tick++)
            {
                double gust=RenewableWind.Sample(123,tick,config.windGustTicks);
                if(gust<0||gust>1||Math.Abs(gust-last)>.007)throw new Exception("Abrupt or invalid gust");
                min=Math.Min(min,gust);max=Math.Max(max,gust);last=gust;
            }
            Check(max-min>.4,"Seeded gusts fluctuate gradually over four minutes");
            Check(RenewableWind.Sample(123,4799,config.windGustTicks)==RenewableWind.Sample(123,4799,config.windGustTicks),"Saved seed and active tick reproduce gust exactly");
            Check(config.Output(IndustryId.WindTurbine,0,.08f,.18f,0)==40&&config.Output(IndustryId.WindTurbine,0,.72f,.42f,0)==240,"Clear and rain retain small and stronger baseline wind");
            Check(config.Output(IndustryId.WindTurbine,0,1,1,0)==400&&config.Output(IndustryId.WindTurbine,0,1,1,1)==400,"Storm maintains maximum output throughout gust cycle");
            var world=new World();var sim=new IndustrySimulation(world,_=>64);
            MachineState Add(int x,byte id){var p=new BlockPos(x,10,0);world.Cells[p]=id;return sim.Add(p,id);}
            var sun=Add(0,IndustryId.SolarPanel);Add(1,IndustryId.PowerCable);var battery=Add(2,IndustryId.Battery);sim.Step();
            long before=BatteryPower.Amount(battery);sim.Step();Check(sun.SupplyWatts==400&&BatteryPower.Amount(battery)-before==20000,"400 W solar stores exactly 20 J each fixed tick");
            world.Hour=0;before=BatteryPower.Amount(battery);sim.Step();Check(sun.SupplyWatts==0&&BatteryPower.Amount(battery)==before,"Night creates no free energy");
            world.Hour=12;world.Open=false;sim.Step();Check(sun.Status==MachineStatus.Sheltered&&sun.SupplyWatts==0,"Roof blocks renewable output");
            world.Open=true;sim.Step();Check(sun.SupplyWatts==400,"Removing roof recovers output");
            sun.SignalAttached=true;sun.Signal=false;sim.Step();Check(sun.SupplyWatts==0&&sun.Status==MachineStatus.DisabledBySignal,"Signal gates generation");
            world.Resident=false;sim.Invalidate();before=BatteryPower.Amount(battery);sim.Step();Check(sun.Status==MachineStatus.Dormant&&BatteryPower.Amount(battery)==before,"Dormant machines produce no energy");
            world.Resident=true;sim.Invalidate();sim.Step();Check(sun.DeliveredWatts==400,"Generation resumes after residency returns");
            for(int i=0;i<2500;i++)Add(100+i,IndustryId.PowerCable);
            before=BatteryPower.Amount(battery);sim.Step();
            Check(sim.Rebuilding&&sun.DeliveredWatts==400&&sun.SupplyWatts==400&&BatteryPower.Amount(battery)-before==20000,"Unrelated component reconstruction preserves exact renewable generation and storage");
            sim.Invalidate(sun.Position);before=BatteryPower.Amount(battery);sim.Step();
            Check(sim.Rebuilding&&sun.DeliveredWatts==0&&sun.SupplyWatts==0&&BatteryPower.Amount(battery)==before,"Affected component reconstruction clears stale generation and rotor demand without spending storage");
            Directory.CreateDirectory("Logs/Renewables");File.WriteAllText("Logs/Renewables/domain-checks.txt",report+"Assertions: "+assertions+"\n");
        }
    }
}
