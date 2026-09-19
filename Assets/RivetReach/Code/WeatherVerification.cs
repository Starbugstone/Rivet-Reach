using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        static byte[] WeatherBytes(WeatherState state){using var memory=new MemoryStream();using var writer=new BinaryWriter(memory);state.WriteSave(writer);return memory.ToArray();}
        IEnumerator ReviewWeatherResume()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rr-save-directory");
            game.InitializeSaves(args[index+1]);Check(game.ContinueLatestSave(),"Fresh process continues weather checkpoint: "+game.SaveStatus);
            game.enabled=false;FreezeSaveFixture();game.Animals.enabled=false;
            if(Array.IndexOf(args,"-rr-weather-legacy-review")>=0){Check(game.Weather.Kind==WeatherKind.Clear&&!game.Weather.Transitioning,"Real schema15 crate checkpoint starts clear without changing saved contents");yield break;}
            Check(game.Weather.Kind==WeatherKind.Storm&&game.Weather.Transitioning&&game.Weather.RemainingTicks==180,"Fresh process restores exact storm transition without offline advance");
            yield return Settle(120);game.enabled=true;yield return null;game.enabled=false;
            Check(!game.Creative&&game.Weather.RemainingTicks==180,"Loading residency gate preserves ordinary survival and exact weather timer");
        }
        IEnumerator ReviewWeather()
        {
            // First exercise ordinary active Survival, before controlled presentation fixtures.
            Check(!game.Creative&&game.Inventory.Slots.All(s=>s.Empty),"Ordinary weather session starts in Survival without supplied items");
            int before=game.Weather.RemainingTicks;yield return new WaitForSecondsRealtime(2);
            Check(game.Weather.RemainingTicks<before,"Active Survival advances the authoritative weather timer");
            game.SetMode(ScreenMode.Pause);var paused=WeatherBytes(game.Weather);yield return new WaitForSecondsRealtime(.5f);
            Check(paused.SequenceEqual(WeatherBytes(game.Weather)),"Pause freezes weather authority exactly");
            FreezeSaveFixture();game.Animals.enabled=false;game.enabled=false;yield return Settle();
            var world=game.World;var player=game.Player;var start=world.Address(player.transform.position);
            player.transform.position+=Vector3.up*3;player.Camera.transform.position=player.transform.position+Vector3.up*1.5f;
            player.Camera.transform.rotation=Quaternion.Euler(5,35,0);game.SetMode(ScreenMode.Play);
            game.Sky.Clock.SetTime(.43);game.Weather.SetWeather(WeatherKind.Clear,true);game.Sky.Apply();
            yield return new WaitForSecondsRealtime(2);yield return Capture("weather-clear");
            var presentation=world.GetComponent<WeatherPresentation>();
            Check(presentation.AudioReady&&presentation.VisibleStreaks==0,"Clear weather has procedural audio available and no rain streaks");
            var sunny=game.Sky.MainLight.intensity;bool night=game.Sky.Clock.IsNight;
            game.Weather.SetWeather(WeatherKind.Rain);game.Weather.Advance(150);game.Sky.Apply();
            Check(game.Weather.RainStrength>0&&game.Weather.RainStrength<.75,"Half-transition interpolates rainfall");yield return Capture("weather-transition");
            game.Weather.Advance(150);game.Sky.Apply();yield return new WaitForSecondsRealtime(2);yield return Capture("weather-rain");
            Check(presentation.VisibleStreaks>0&&presentation.VisibleStreaks<=WeatherPresentation.MaximumStreaks,"Rain renders a bounded visible streak mesh");
            Check(presentation.RoofQueries==WeatherPresentation.Diameter*WeatherPresentation.Diameter&&presentation.Exposure==1,"Rain uses bounded cached columns and recognises open sky");
            Check(game.Sky.MainLight.intensity<sunny&&game.Sky.Clock.IsNight==night,"Clouds dim presentation without changing hostile day/night authority");
            game.Weather.SetWeather(WeatherKind.Storm,true);game.Sky.Apply();yield return new WaitForSecondsRealtime(2);yield return Capture("weather-storm");
            var coverage=new System.Text.StringBuilder();
            foreach(float yaw in new[]{35f,90f,180f,270f})
            {
                player.Camera.transform.rotation=Quaternion.Euler(5,yaw,0);var bins=new int[8];
                for(int frame=0;frame<60;frame++){yield return null;presentation.CountScreenCoverage(bins);}
                coverage.AppendLine($"Yaw {yaw}: known columns {presentation.KnownColumns}/{WeatherPresentation.Diameter*WeatherPresentation.Diameter}, screen bands {string.Join(",",bins)}");
                yield return Capture("weather-coverage-"+yaw);
            }
            File.WriteAllText(Path.Combine(output,"weather-coverage.txt"),coverage.ToString());
            player.Camera.transform.rotation=Quaternion.Euler(5,35,0);
            double meshTotal=0,meshMax=0;for(int i=0;i<120;i++){yield return null;meshTotal+=presentation.LastMeshMilliseconds;meshMax=Math.Max(meshMax,presentation.LastMeshMilliseconds);}
            File.WriteAllText(Path.Combine(output,"weather-cost.txt"),$"Local rain mesh: {WeatherPresentation.MaximumStreaks} maximum streaks; {WeatherPresentation.Diameter*WeatherPresentation.Diameter} cached roof queries per refresh (4 Hz while stationary).\n120-frame CPU mesh update mean {meshTotal/120:F4} ms; max {meshMax:F4} ms. This is a focused fixture, not a full-game performance claim.\n");
            yield return new WaitForSecondsRealtime(21);Check(presentation.ThunderEvents>0,"Storm schedules delayed thunder through its bounded audio source");
            // Build an actual opaque roof above the camera; invalidation suppresses stale rain.
            var roof=world.Address(player.Camera.transform.position).Offset(0,3,0);
            for(int z=-3;z<=3;z++)for(int x=-3;x<=3;x++)
            {var p=roof.Offset(x,0,z);byte id=world.Get(p);if(id!=0)Check(world.Remove(p,id),"Clear weather shelter fixture");Check(world.Place(p,BlockId.Planks),"Place shelter roof");}
            yield return new WaitForSecondsRealtime(1);Check(presentation.Exposure==0,"New roof immediately shelters the listener even before lighting cache resolves");
            player.Camera.transform.rotation=Quaternion.Euler(-12,35,0);yield return Capture("weather-shelter");
            byte[] weather=WeatherBytes(game.Weather);long tick=game.Survival.Tick;
            game.Sky.Clock.SetTime(game.Sky.Clock.TotalDays+1);game.Sky.Apply();
            Check(weather.SequenceEqual(WeatherBytes(game.Weather))&&game.Survival.Tick==tick,"Celestial sleep-style jump does not advance weather or production ticks");
            float master=game.Sound.Master;game.Sound.SetMaster(0);yield return null;Check(presentation.RainVolume==0,"Master mute silences weather rain");game.Sound.SetMaster(master);
            game.Sky.Clock.SetTime(.92);game.Sky.Apply();yield return Capture("weather-night-storm");
            game.Weather.SetWeather(WeatherKind.Clear,true);game.Weather.SetWeather(WeatherKind.Storm);game.Weather.Advance(120);
            game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"Saves"));Check(game.SaveGame("Weather checkpoint"),"Save a partly transitioned storm: "+game.SaveStatus);
            var entry=game.Saves.List().First(e=>!e.Backup);var saved=WeatherBytes(game.Weather);
            game.Weather.Advance(2000);Check(game.LoadGame(entry),"Load weather checkpoint: "+game.SaveStatus);game.enabled=false;FreezeSaveFixture();game.Animals.enabled=false;
            Check(saved.SequenceEqual(WeatherBytes(game.Weather)),"Load restores exact transition phase, profile and RNG");
            yield return Settle(120);game.enabled=true;yield return null;game.enabled=false;
            byte[] original=File.ReadAllBytes(entry.Path),body;using(var r=game.Saves.Open(original,out _))body=r.ReadBytes((int)(r.BaseStream.Length-r.BaseStream.Position));
            var intactWorld=game.World;var intactWeather=game.Weather;
            File.WriteAllBytes(entry.Path,game.Saves.Encode(entry,w=>w.Write(body,0,body.Length-1)));
            Check(!game.LoadGame(entry)&&game.World==intactWorld&&game.Weather==intactWeather&&saved.SequenceEqual(WeatherBytes(game.Weather)),"Late malformed weather payload rolls back the complete live session");File.WriteAllBytes(entry.Path,original);
        }
    }
}
