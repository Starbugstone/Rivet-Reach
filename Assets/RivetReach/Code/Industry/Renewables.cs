using System;
using UnityEngine;

namespace RivetReach
{
    public interface IRenewableEnvironment
    {
        double Hour {get;}
        float CloudCover {get;}
        float WindStrength {get;}
        double WindGust {get;}
        bool SkyExposed(BlockPos position);
    }
    [Serializable] public sealed class RenewableCatalog
    {
        public int solarPeakWatts,windPeakWatts;
        public int[] solarPermille,windPermille,windMinimumPermille;
        public int windGustTicks;
        static RenewableCatalog cached;
        public static RenewableCatalog Current=>cached??=Load();
        static RenewableCatalog Load()
        {
            var asset=Resources.Load<TextAsset>("Definitions/Renewables");
            if(asset==null)throw new InvalidOperationException("Missing renewable configuration.");
            var result=JsonUtility.FromJson<RenewableCatalog>(asset.text);result.Validate();return result;
        }
        public void Validate()
        {
            if(solarPeakWatts<1||solarPeakWatts>10000||windPeakWatts<1||windPeakWatts>10000)throw new ArgumentException("Invalid renewable peak output.");
            foreach(var factors in new[]{solarPermille,windPermille,windMinimumPermille})
            {if(factors==null||factors.Length!=3)throw new ArgumentException("Renewables require clear/rain/storm factors.");foreach(int factor in factors)if(factor<0||factor>1000)throw new ArgumentException("Invalid renewable weather factor.");}
            if(windGustTicks<20||windGustTicks>12000)throw new ArgumentException("Invalid gust duration.");
            for(int i=0;i<3;i++)if(windMinimumPermille[i]>windPermille[i])throw new ArgumentException("Wind minimum exceeds maximum.");
        }
        public int Peak(byte id)=>id==IndustryId.SolarPanel?solarPeakWatts:windPeakWatts;
        public int Output(byte id,double hour,float cloud,float wind,double gust=1)
        {
            bool solar=id==IndustryId.SolarPanel;
            if(solar&&(hour<=6||hour>=18))return 0;
            // Use the authoritative, smoothly transitioning weather profiles, independent of cameras.
            WeatherState.Profile(WeatherKind.Clear,out float clearCloud,out _,out float clearWind);
            WeatherState.Profile(WeatherKind.Rain,out float rainCloud,out _,out float rainWind);
            WeatherState.Profile(WeatherKind.Storm,out float stormCloud,out _,out float stormWind);
            double value=solar?cloud:wind,a=solar?clearCloud:clearWind,b=solar?rainCloud:rainWind,c=solar?stormCloud:stormWind;
            var factors=solar?solarPermille:windPermille;
            double factor=value<=b?Blend(factors[0],factors[1],(value-a)/(b-a)):Blend(factors[1],factors[2],(value-b)/(c-b));
            if(!solar)
            {
                double minimum=value<=b?Blend(windMinimumPermille[0],windMinimumPermille[1],(value-a)/(b-a)):Blend(windMinimumPermille[1],windMinimumPermille[2],(value-b)/(c-b));
                factor=Blend(minimum,factor,gust);
            }
            double daylight=solar?Math.Sin((hour-6)*Math.PI/12):1;
            return (int)Math.Floor(Peak(id)*factor/1000*daylight+1e-6);
        }
        static double Blend(double a,double b,double t)=>a+(b-a)*Math.Clamp(t,0,1);
    }
    // A seeded, smooth world-wide gust field sampled from the saved active-survival tick.
    // No extra RNG state, per-turbine timers, wall clock, or skipped-night production.
    public static class RenewableWind
    {
        public static double Sample(int seed,long tick,int period)
        {
            long segment=tick/period;double t=(double)(tick%period)/period;t=t*t*(3-2*t);
            double a=Target(seed,segment),b=Target(seed,segment+1);return a+(b-a)*t;
        }
        static double Target(int seed,long segment)
        {
            unchecked
            {
                ulong n=(ulong)segment^((ulong)(uint)seed<<32)^0xD1B54A32D192ED03UL;
                n=(n^(n>>30))*0xBF58476D1CE4E5B9UL;n=(n^(n>>27))*0x94D049BB133111EBUL;n^=n>>31;
                return (n>>11)*(1.0/9007199254740991.0);
            }
        }
    }
    public sealed partial class IndustrySimulation
    {
        void PrepareRenewable(MachineState m)
        {
            if(!m.Enabled){m.Status=MachineStatus.DisabledBySignal;return;}
            if(!(world is IRenewableEnvironment environment)){m.Status=MachineStatus.Dormant;return;}
            if(!environment.SkyExposed(m.Position)){m.Status=MachineStatus.Sheltered;return;}
            m.SupplyWatts=RenewableCatalog.Current.Output(m.Definition.Id,environment.Hour,environment.CloudCover,environment.WindStrength,environment.WindGust);
            m.Status=m.SupplyWatts>0?MachineStatus.Running:m.Definition.Id==IndustryId.SolarPanel?MachineStatus.NoSunlight:MachineStatus.Ready;
        }
    }
}
