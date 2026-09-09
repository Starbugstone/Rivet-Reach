using System;

namespace RivetReach
{
    // Session/world state, independent of cameras, chunks, frame rate and wall-clock time.
    public sealed class WorldClock
    {
        public const double DefaultDaySeconds=1200;
        static readonly string[] PhaseNames={"New moon","Waxing crescent","First quarter","Waxing gibbous","Full moon","Waning gibbous","Last quarter","Waning crescent"};
        public double DaySeconds {get;}
        public double TotalDays {get;private set;}
        public long DayNumber=>(long)Math.Floor(TotalDays)+1;
        public double Hour=>(TotalDays-Math.Floor(TotalDays))*24;
        public bool IsNight=>Hour<6||Hour>=18;
        // Dawn, rather than midnight, advances the phase: one uninterrupted phase per night.
        public int MoonPhase=>(int)(((long)Math.Floor(TotalDays-.25)+4+8)%8);
        public string MoonPhaseName=>PhaseNames[MoonPhase];
        public double MoonIllumination=>(1-Math.Cos(MoonPhase*Math.PI/4))*.5;

        public WorldClock(double daySeconds=DefaultDaySeconds)
        {
            if(!Finite(daySeconds)||daySeconds<1)throw new ArgumentOutOfRangeException(nameof(daySeconds));
            DaySeconds=daySeconds;TotalDays=8.0/24;
        }
        public void Advance(double seconds)
        {
            if(!Finite(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));
            SetTime(TotalDays+seconds/DaySeconds);
        }
        // Explicit world-time restoration/verification boundary; no operating-system catch-up.
        public void SetTime(double totalDays)
        {
            if(!Finite(totalDays)||totalDays<0||totalDays>1e9)throw new ArgumentOutOfRangeException(nameof(totalDays));
            TotalDays=totalDays;
        }
        static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
    }
}
