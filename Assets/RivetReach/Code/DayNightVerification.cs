using System;
using System.Collections;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        void SkyTime(double days){game.Sky.Clock.SetTime(days);game.Sky.Apply();}
        IEnumerator ReviewDayNight()
        {
            var sky=game.Sky;var clock=sky.Clock;var player=game.Player;
            Check(RenderSettings.sun==sky.MainLight&&sky.MainLight.shadows==LightShadows.Soft,"Celestial rig owns the shadowed URP main light");
            Check(RenderSettings.skybox.shader.name=="RivetReach/ExpeditionSky"&&RenderSettings.skybox!=Resources.Load<Material>("Materials/Sky"),"Session sky uses its own material instance");
            double before=clock.TotalDays;yield return new WaitForSecondsRealtime(.25f);
            Check(clock.TotalDays>before,"Ordinary play advances the world clock");
            foreach(var mode in new[]{ScreenMode.Pause,ScreenMode.Settings,ScreenMode.Appearance,ScreenMode.Controls,ScreenMode.Title})
            {
                game.SetMode(mode);before=clock.TotalDays;yield return new WaitForSecondsRealtime(.12f);
                Check(clock.TotalDays==before,mode+" freezes world time");
            }
            game.SetMode(ScreenMode.Inventory);before=clock.TotalDays;yield return new WaitForSecondsRealtime(.25f);
            Check(clock.TotalDays>before,"Inventory keeps world time running");game.SetMode(ScreenMode.Play);
            SkyTime(.5);float noonPower=sky.MainLight.intensity;Color noonAmbient=RenderSettings.ambientSkyColor,noonFog=RenderSettings.fogColor;
            Vector3 noonSun=sky.SunDirection;
            Check(noonSun.y>.9f&&noonPower>1.4f&&!clock.IsNight,"Noon sun is high and illuminates the world");
            Check(Vector3.Dot(-sky.MainLight.transform.forward,sky.SunDirection)>.9999f,"Daytime shadows follow visible sun direction");
            SkyTime(.75);Check(Mathf.Abs(sky.SunDirection.y)<.001&&sky.SunDirection.x<-.99f,"Sun reaches western horizon at dusk");
            SkyTime(1);Check(sky.MoonDirection.y>.9f&&clock.IsNight,"Moon rises to its midnight zenith");
            Check(Vector3.Dot(-sky.MainLight.transform.forward,sky.MoonDirection)>.9999f,"Night shadows follow visible moon direction");
            Check(sky.MainLight.intensity>.2f&&sky.MainLight.intensity<noonPower,"Full moon gives dimmer cool direct light");
            Check(RenderSettings.ambientSkyColor.grayscale<noonAmbient.grayscale*.3f&&RenderSettings.ambientSkyColor.grayscale>.02f,"Night ambient darkens terrain while retaining a visibility floor");
            Check(RenderSettings.fogColor.grayscale<noonFog.grayscale*.2f,"Distant fog follows night lighting");
            Check(Vector3.Dot(sky.SunDirection,sky.MoonDirection)<-.9999f,"Sun and moon occupy opposite sky directions");
            SkyTime(5);Check(clock.MoonPhase==0&&sky.MainLight.intensity<.00001f,"New-moon night has no direct moonlight");
            foreach(double boundary in new[]{.25,.75,1.25})
            {
                SkyTime(boundary-1e-6);Color fog=RenderSettings.fogColor;float daylight=sky.Daylight,power=sky.MainLight.intensity;
                SkyTime(boundary+1e-6);
                Check(Mathf.Abs(sky.Daylight-daylight)<.001f&&Mathf.Abs(sky.MainLight.intensity-power)<.001f&&Mathf.Abs(RenderSettings.fogColor.grayscale-fog.grayscale)<.001f,"Lighting stays continuous across horizon/phase boundary "+boundary);
            }
            float fogStart=game.World.FogStart,fogEnd=game.World.FogEnd;
            SkyTime(8.5);Check(game.World.FogStart==fogStart&&game.World.FogEnd==fogEnd,"Cycle preserves view-distance fog ranges");
            sampling=true;game.Diagnostics=false;
            player.Yaw=65;player.Pitch=-12;
            SkyTime(.5);yield return Capture("day-night-01-noon");
            player.Yaw=-90;player.Pitch=-7;SkyTime(17.6/24);yield return Capture("day-night-02-sunset");
            player.Yaw=65;player.Pitch=-12;SkyTime(1);yield return Capture("day-night-03-full-moon-landscape");
            SkyTime(5);yield return Capture("day-night-04-new-moon-landscape");
            player.Yaw=90;player.Pitch=-7;SkyTime(6.3/24);yield return Capture("day-night-05-sunrise");
            // Review all phase silhouettes at identical sky position and magnification.
            float fov=player.Camera.fieldOfView;int culling=player.Camera.cullingMask;
            player.Camera.fieldOfView=35;player.Camera.cullingMask=0;
            for(int phase=0;phase<8;phase++)
            {
                int day=(phase+4)%8;SkyTime(day+22.0/24);
                Vector3 moon=sky.MoonDirection;
                player.Yaw=Mathf.Atan2(moon.x,moon.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(moon.y)*Mathf.Rad2Deg;
                Check(clock.MoonPhase==phase&&Mathf.Abs(Shader.GetGlobalFloat("_RRMoonPhase")-phase/8f)<1e-6,"Sky receives phase "+phase+": "+clock.MoonPhaseName);
                yield return Capture("day-night-phase-"+phase);
            }
            player.Camera.fieldOfView=fov;player.Camera.cullingMask=culling;
            SkyTime(.95);int nightPhase=clock.MoonPhase;sky.Advance(100);
            Check(clock.DayNumber==2&&clock.MoonPhase==nightPhase,"Advancing through midnight preserves the same night's phase");
            sky.Advance(1200*3);Check(clock.MoonPhase==(nightPhase+3)%8,"Multi-day advance accounts for every phase");
            sampling=false;
            game.SetMode(ScreenMode.Title);game.StartSession(game.Seed+1);
            Check(game.Sky==sky&&sky.Clock!=clock&&sky.Clock.Hour==8&&sky.Clock.MoonPhase==4,"New world resets to morning and full-moon cycle without duplicating rig");
            Check(RenderSettings.skybox.shader.name=="RivetReach/ExpeditionSky","Sky remains attached after session replacement");
            game.SetMode(ScreenMode.Pause);
        }
    }
}
