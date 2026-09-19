using System;
using System.IO;

namespace RivetReach
{
    public enum WeatherKind : byte { Clear, Rain, Storm }

    // Pure world authority. Presentation samples the smoothed values but cannot alter
    // timing, RNG or persistence. Time advances only through the active 20 Hz session.
    public sealed class WeatherState
    {
        public const int TicksPerSecond=20;
        public const int TransitionDurationTicks=15*TicksPerSecond;
        const int ClearMinimumTicks=4*60*TicksPerSecond,ClearMaximumTicks=8*60*TicksPerSecond;
        const int RainMinimumTicks=3*60*TicksPerSecond,RainMaximumTicks=5*60*TicksPerSecond;
        const int StormMinimumTicks=2*60*TicksPerSecond,StormMaximumTicks=4*60*TicksPerSecond;

        readonly uint initialRandomState;
        uint randomState;
        float startCloud,startRain,startWind;

        // Kind is the target/current weather. PreviousKind identifies the completed
        // state that a transition started from, and is useful to presentation/UI.
        public WeatherKind Kind {get;private set;}
        public WeatherKind PreviousKind {get;private set;}
        public int RemainingTicks {get;private set;}
        public int TransitionTicks {get;private set;}
        public float CloudCover {get;private set;}
        public float RainStrength {get;private set;}
        public float WindStrength {get;private set;}
        public bool Transitioning=>TransitionTicks>0;

        public WeatherState(int seed)
        {
            initialRandomState=Seed(seed);Reset();
        }
        public void Reset()
        {
            randomState=initialRandomState;Kind=PreviousKind=WeatherKind.Clear;TransitionTicks=0;
            Profile(WeatherKind.Clear,out startCloud,out startRain,out startWind);
            CloudCover=startCloud;RainStrength=startRain;WindStrength=startWind;
            RemainingTicks=Duration(WeatherKind.Clear);
        }
        public void SetWeather(WeatherKind kind,bool immediate=false)
        {
            RequireKind(kind);
            if(immediate)
            {
                Kind=PreviousKind=kind;TransitionTicks=0;Profile(kind,out startCloud,out startRain,out startWind);
                CloudCover=startCloud;RainStrength=startRain;WindStrength=startWind;RemainingTicks=Duration(kind);return;
            }
            if(Transitioning&&Kind==kind)return;
            Begin(kind);
        }
        // One roll after a successful night skip. Retaining weather also retains its
        // current schedule; changing it blends from the current presentation values.
        public void RecheckAfterSleep()
        {
            if(Next(2)!=0)Begin(NextKind());
        }
        public void Advance(int ticks)
        {
            if(ticks<0)throw new ArgumentOutOfRangeException(nameof(ticks));
            while(ticks>0)
            {
                int step=Math.Min(ticks,RemainingTicks);RemainingTicks-=step;ticks-=step;Refresh();
                if(RemainingTicks!=0)continue;
                if(Transitioning)FinishTransition();else Begin(NextKind());
            }
        }
        void Begin(WeatherKind next)
        {
            RequireKind(next);PreviousKind=Kind;Kind=next;startCloud=CloudCover;startRain=RainStrength;startWind=WindStrength;
            TransitionTicks=TransitionDurationTicks;RemainingTicks=TransitionDurationTicks;Refresh();
        }
        void FinishTransition()
        {
            TransitionTicks=0;Profile(Kind,out startCloud,out startRain,out startWind);
            CloudCover=startCloud;RainStrength=startRain;WindStrength=startWind;PreviousKind=Kind;RemainingTicks=Duration(Kind);
        }
        void Refresh()
        {
            if(!Transitioning)return;
            Profile(Kind,out float cloud,out float rain,out float wind);
            float progress=1f-(float)RemainingTicks/TransitionTicks;
            // Smoothstep preserves a continuous rate at both transition endpoints.
            progress=progress*progress*(3f-2f*progress);
            CloudCover=Lerp(startCloud,cloud,progress);RainStrength=Lerp(startRain,rain,progress);WindStrength=Lerp(startWind,wind,progress);
        }
        WeatherKind NextKind()
        {
            int roll=Next(4);
            if(Kind==WeatherKind.Clear)return roll==0?WeatherKind.Storm:WeatherKind.Rain;
            if(Kind==WeatherKind.Rain)return roll<2?WeatherKind.Clear:WeatherKind.Storm;
            return roll==0?WeatherKind.Clear:WeatherKind.Rain;
        }
        int Duration(WeatherKind kind)
        {
            if(kind==WeatherKind.Clear)return Between(ClearMinimumTicks,ClearMaximumTicks);
            if(kind==WeatherKind.Rain)return Between(RainMinimumTicks,RainMaximumTicks);
            return Between(StormMinimumTicks,StormMaximumTicks);
        }
        int Between(int minimum,int maximum)=>minimum+Next(maximum-minimum+1);
        int Next(int maximum)
        {
            if(maximum<=0)throw new ArgumentOutOfRangeException(nameof(maximum));
            uint x=randomState;x^=x<<13;x^=x>>17;x^=x<<5;randomState=x==0?0x6D2B79F5u:x;
            return (int)(randomState%(uint)maximum);
        }
        static uint Seed(int seed)
        {
            uint value=(uint)seed^0xA511E9B3u;return value==0?0x6D2B79F5u:value;
        }
        static int MaximumDuration(WeatherKind kind)=>kind==WeatherKind.Clear?ClearMaximumTicks:kind==WeatherKind.Rain?RainMaximumTicks:StormMaximumTicks;
        public static void Profile(WeatherKind kind,out float cloud,out float rain,out float wind)
        {
            if(kind==WeatherKind.Clear){cloud=.08f;rain=0;wind=.18f;return;}
            if(kind==WeatherKind.Rain){cloud=.72f;rain=.75f;wind=.42f;return;}
            cloud=1;rain=1;wind=1;
        }
        static float Lerp(float a,float b,float t)=>a+(b-a)*t;
        static void RequireKind(WeatherKind kind)
        {
            if(kind<WeatherKind.Clear||kind>WeatherKind.Storm)throw new ArgumentOutOfRangeException(nameof(kind));
        }
        static void Require(bool valid,string message)
        {
            if(!valid)throw new InvalidDataException(message);
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        public void WriteSave(BinaryWriter writer)
        {
            if(writer==null)throw new ArgumentNullException(nameof(writer));
            writer.Write((byte)Kind);writer.Write((byte)PreviousKind);writer.Write(RemainingTicks);writer.Write(TransitionTicks);writer.Write(randomState);
            writer.Write(startCloud);writer.Write(startRain);writer.Write(startWind);
        }
        public void ReadSave(BinaryReader reader,int format)
        {
            if(reader==null)throw new ArgumentNullException(nameof(reader));
            if(format<=15){Reset();return;}
            int kind=reader.ReadByte(),previous=reader.ReadByte(),remaining=reader.ReadInt32(),transition=reader.ReadInt32();uint state=reader.ReadUInt32();
            float cloud=reader.ReadSingle(),rain=reader.ReadSingle(),wind=reader.ReadSingle();
            Require(kind>=0&&kind<=2&&previous>=0&&previous<=2,"Invalid saved weather kind.");
            Require(state!=0,"Invalid saved weather random state.");
            Require(transition==0||transition==TransitionDurationTicks,"Invalid saved weather transition duration.");
            Require(transition==0?remaining>0&&remaining<=MaximumDuration((WeatherKind)kind):remaining>0&&remaining<=transition,"Invalid saved weather remaining duration.");
            Require(Finite(cloud)&&Finite(rain)&&Finite(wind)&&cloud>=0&&cloud<=1&&rain>=0&&rain<=1&&wind>=0&&wind<=1,"Invalid saved weather transition values.");
            Kind=(WeatherKind)kind;PreviousKind=(WeatherKind)previous;RemainingTicks=remaining;TransitionTicks=transition;randomState=state;startCloud=cloud;startRain=rain;startWind=wind;
            if(transition==0)
            {
                Require(Kind==PreviousKind,"Invalid stable saved weather state.");
                Profile(Kind,out float expectedCloud,out float expectedRain,out float expectedWind);
                Require(cloud==expectedCloud&&rain==expectedRain&&wind==expectedWind,"Invalid stable saved weather profile.");
                CloudCover=expectedCloud;RainStrength=expectedRain;WindStrength=expectedWind;
            }
            else Refresh();
        }
    }
}
