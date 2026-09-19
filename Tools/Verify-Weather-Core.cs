using System;
using System.IO;
using System.Linq;
using RivetReach;

static class VerifyWeatherCore
{
    static int assertions;
    static void Check(bool value,string message){if(!value)throw new Exception(message);assertions++;}
    static void Same(WeatherState a,WeatherState b,string label)
    {
        Check(a.Kind==b.Kind&&a.PreviousKind==b.PreviousKind&&a.RemainingTicks==b.RemainingTicks&&a.TransitionTicks==b.TransitionTicks,label+" discrete state");
        Check(a.CloudCover==b.CloudCover&&a.RainStrength==b.RainStrength&&a.WindStrength==b.WindStrength,label+" smoothed values");
    }
    static void Main()
    {
        var a=new WeatherState(12345);var b=new WeatherState(12345);Same(a,b,"seeded initial");
        a.Advance(120000);b.Advance(120000);Same(a,b,"deterministic long advance");
        var bulk=new WeatherState(913);var chunked=new WeatherState(913);bulk.Advance(150000);foreach(int step in new[]{1,19,300,7,2400,33,12000,135240})chunked.Advance(step);Same(bulk,chunked,"chunked and bulk advance");
        var idle=new WeatherState(44);byte[] idleBefore;using(var memory=new MemoryStream()){using var writer=new BinaryWriter(memory);idle.WriteSave(writer);idleBefore=memory.ToArray();}idle.Advance(0);using(var memory=new MemoryStream()){using var writer=new BinaryWriter(memory);idle.WriteSave(writer);Check(idleBefore.SequenceEqual(memory.ToArray()),"zero-tick advance is a no-op");}
        var forced=new WeatherState(7);forced.SetWeather(WeatherKind.Storm);Check(forced.Transitioning&&forced.Kind==WeatherKind.Storm&&forced.PreviousKind==WeatherKind.Clear,"non-immediate request begins clear-to-storm transition");
        float before=forced.RainStrength;forced.Advance(WeatherState.TransitionDurationTicks/2);Check(forced.RainStrength>before&&forced.RainStrength<1,"transition has smooth intermediate rain strength");
        forced.Advance(WeatherState.TransitionDurationTicks/2);Check(!forced.Transitioning&&forced.Kind==WeatherKind.Storm&&forced.RainStrength==1,"transition ends at exact storm profile");
        forced.SetWeather(WeatherKind.Clear,true);Check(!forced.Transitioning&&forced.Kind==WeatherKind.Clear&&forced.RainStrength==0,"immediate testing weather applies exact clear profile");
        var saved=new WeatherState(-99);saved.SetWeather(WeatherKind.Rain);saved.Advance(137);byte[] bytes;
        using(var memory=new MemoryStream()){using(var writer=new BinaryWriter(memory))saved.WriteSave(writer);bytes=memory.ToArray();}
        var loaded=new WeatherState(1);using(var memory=new MemoryStream(bytes))using(var reader=new BinaryReader(memory))loaded.ReadSave(reader,16);Same(saved,loaded,"schema16 round trip");
        saved.Advance(50000);loaded.Advance(50000);Same(saved,loaded,"restored exact next RNG schedule");
        var interrupted=new WeatherState(81);interrupted.SetWeather(WeatherKind.Storm);interrupted.Advance(113);float interruptedCloud=interrupted.CloudCover,interruptedRain=interrupted.RainStrength,interruptedWind=interrupted.WindStrength;interrupted.SetWeather(WeatherKind.Rain);
        Check(interrupted.Kind==WeatherKind.Rain&&interrupted.Transitioning&&interrupted.CloudCover==interruptedCloud&&interrupted.RainStrength==interruptedRain&&interrupted.WindStrength==interruptedWind,"retargeting an active transition preserves its current presentation state");
        byte[] interruptedBytes;using(var memory=new MemoryStream()){using var writer=new BinaryWriter(memory);interrupted.WriteSave(writer);interruptedBytes=memory.ToArray();}var interruptedLoad=new WeatherState(1);using(var reader=new BinaryReader(new MemoryStream(interruptedBytes)))interruptedLoad.ReadSave(reader,16);interrupted.Advance(90000);interruptedLoad.Advance(90000);Same(interrupted,interruptedLoad,"interrupted transition persists and continues deterministically");
        foreach(var sample in new[]{(WeatherKind.Clear,8*60*WeatherState.TicksPerSecond),(WeatherKind.Rain,5*60*WeatherState.TicksPerSecond)})
        {
            var payload=new byte[26];payload[0]=(byte)sample.Item1;payload[1]=(byte)sample.Item1;BitConverter.GetBytes(sample.Item2).CopyTo(payload,2);BitConverter.GetBytes(0).CopyTo(payload,6);BitConverter.GetBytes(1u).CopyTo(payload,10);
            float cloud=sample.Item1==WeatherKind.Clear?.08f:.72f,rain=sample.Item1==WeatherKind.Clear?0:.75f,wind=sample.Item1==WeatherKind.Clear?.18f:.42f;
            BitConverter.GetBytes(cloud).CopyTo(payload,14);BitConverter.GetBytes(rain).CopyTo(payload,18);BitConverter.GetBytes(wind).CopyTo(payload,22);
            using var reader=new BinaryReader(new MemoryStream(payload));new WeatherState(1).ReadSave(reader,16);Check(true,"maximum "+sample.Item1+" stable duration is accepted");
        }
        var legacy=new WeatherState(42);legacy.SetWeather(WeatherKind.Storm,true);using(var empty=new MemoryStream())using(var reader=new BinaryReader(empty))legacy.ReadSave(reader,15);Check(legacy.Kind==WeatherKind.Clear&&!legacy.Transitioning&&legacy.RainStrength==0,"legacy read consumes no payload and resets seeded clear state");
        bool rejected=false;try{var payload=new byte[26];payload[0]=9;using var bad=new BinaryReader(new MemoryStream(payload));new WeatherState(1).ReadSave(bad,16);}catch(InvalidDataException){rejected=true;}Check(rejected,"invalid weather kind is rejected");
        byte[] stable=bytes.Clone() as byte[];var stableState=new WeatherState(17);stableState.SetWeather(WeatherKind.Rain,true);using(var memory=new MemoryStream()){using var writer=new BinaryWriter(memory);stableState.WriteSave(writer);stable=memory.ToArray();}BitConverter.GetBytes(.25f).CopyTo(stable,14);rejected=false;try{using var bad=new BinaryReader(new MemoryStream(stable));new WeatherState(1).ReadSave(bad,16);}catch(InvalidDataException){rejected=true;}Check(rejected,"corrupt stable weather profile is rejected");
        stable[10]=stable[11]=stable[12]=stable[13]=0;rejected=false;try{using var bad=new BinaryReader(new MemoryStream(stable));new WeatherState(1).ReadSave(bad,16);}catch(InvalidDataException){rejected=true;}Check(rejected,"zero weather RNG state is rejected");
        Console.WriteLine("PASS "+assertions+" weather core assertions");
    }
}
