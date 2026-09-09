using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    // One celestial rig for the viewed world; no per-chunk lighting simulation or sky geometry.
    public sealed class DayNightCycle : MonoBehaviour
    {
        [Min(1),Tooltip("Real minutes per full day; applied when the session clock is reset.")] public float DayLengthMinutes=20;
        public WorldClock Clock {get;private set;}
        public Light MainLight {get;private set;}
        public Vector3 SunDirection {get;private set;}
        public Vector3 MoonDirection=>-SunDirection;
        public float Daylight {get;private set;}
        Material sky,previousSky;
        Light previousSun;

        public void Initialize()
        {
            previousSky=RenderSettings.skybox;previousSun=RenderSettings.sun;
            foreach(var light in FindObjectsByType<Light>())
                if(light.type==LightType.Directional){MainLight=light;break;}
            if(MainLight==null){var source=new GameObject("Celestial light");source.transform.SetParent(transform,false);MainLight=source.AddComponent<Light>();MainLight.type=LightType.Directional;}
            MainLight.shadows=LightShadows.Soft;RenderSettings.sun=MainLight;
            sky=new Material(Resources.Load<Material>("Materials/Sky")){name="Session sky"};
            RenderSettings.skybox=sky;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            ResetClock();
        }
        public void ResetClock(){Clock=new WorldClock(DayLengthMinutes*60);Apply();}
        public void Advance(double seconds){Clock.Advance(seconds);Apply();}
        public void Apply()
        {
            float angle=(float)((Clock.Hour-6)/24*System.Math.PI*2);
            SunDirection=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle)*.9063078f,Mathf.Sin(angle)*.4226183f).normalized;
            Daylight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.16f,.22f,SunDirection.y));
            float twilight=(1-Mathf.SmoothStep(0,1,Mathf.Abs(SunDirection.y)/.30f))*Daylight;
            float sunPower=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.35f,SunDirection.y));
            float moonPower=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.30f,MoonDirection.y))*(float)Clock.MoonIllumination;
            // URP's existing custom surfaces consume one shadowed main directional light.
            // Switch only at the horizon where both direct intensities are zero.
            bool sunUp=SunDirection.y>=0;
            MainLight.transform.rotation=Quaternion.LookRotation(-(sunUp?SunDirection:MoonDirection),Vector3.up);
            MainLight.color=sunUp?Color.Lerp(new Color(1,.40f,.17f),new Color(1,.91f,.72f),sunPower):new Color(.48f,.63f,1);
            MainLight.intensity=sunUp?1.45f*sunPower:.24f*moonPower;
            var nightSky=new Color(.055f,.078f,.135f);
            var nightGround=new Color(.027f,.035f,.055f);
            RenderSettings.ambientSkyColor=Color.Lerp(nightSky,new Color(.50f,.70f,.94f),Daylight);
            RenderSettings.ambientEquatorColor=Color.Lerp(new Color(.045f,.058f,.092f),new Color(.48f,.59f,.71f),Daylight);
            RenderSettings.ambientGroundColor=Color.Lerp(nightGround,new Color(.36f,.32f,.23f),Daylight);
            RenderSettings.reflectionIntensity=Mathf.Lerp(.08f,1,Daylight);
            RenderSettings.fogColor=Color.Lerp(new Color(.022f,.034f,.071f),new Color(.72f,.82f,.87f),Daylight);
            RenderSettings.fogColor=Color.Lerp(RenderSettings.fogColor,new Color(.66f,.31f,.22f),twilight*.65f);
            Shader.SetGlobalVector("_RRSunDirection",SunDirection);
            Shader.SetGlobalVector("_RRMoonDirection",MoonDirection);
            Shader.SetGlobalFloat("_RRDaylight",Daylight);
            Shader.SetGlobalFloat("_RRTwilight",twilight);
            Shader.SetGlobalFloat("_RRMoonPhase",Clock.MoonPhase/8f);
            Shader.SetGlobalColor("_RRAmbientSky",Color.Lerp(nightSky,new Color(.37f,.48f,.62f),Daylight));
            Shader.SetGlobalColor("_RRAmbientGround",Color.Lerp(nightGround,new Color(.22f,.21f,.17f),Daylight));
            Shader.SetGlobalColor("_RRFogColour",RenderSettings.fogColor);
        }
        void OnDestroy()
        {
            if(RenderSettings.skybox==sky)RenderSettings.skybox=previousSky;
            if(RenderSettings.sun==MainLight)RenderSettings.sun=previousSun;
            if(sky!=null)Destroy(sky);
        }
    }
}
