Shader "RivetReach/ExpeditionSky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 _RRSunDirection,_RRMoonDirection;
            float _RRPresentationTime,_RRDaylight,_RRTwilight,_RRMoonPhase;
            float CloudHash(float2 p){return frac(sin(p.x*37.117+p.y*13.713+3.71)*17321.43);}
            float CloudNoise(float2 p)
            {
                float2 cell=floor(p),blend=frac(p);blend=blend*blend*(3-2*blend);
                return lerp(lerp(CloudHash(cell),CloudHash(cell+float2(1,0)),blend.x),
                    lerp(CloudHash(cell+float2(0,1)),CloudHash(cell+1),blend.x),blend.y);
            }
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 ray:TEXCOORD0; };
            V Vert(A i) { V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.ray=i.positionOS.xyz;return o; }
            half4 Frag(V i):SV_Target
            {
                float3 ray=normalize(i.ray);float up=saturate(ray.y);
                float nightVisibility=1-smoothstep(.02,.30,_RRDaylight);
                half3 horizon=lerp(half3(.022,.034,.071),half3(.72,.82,.87),_RRDaylight);
                horizon=lerp(horizon,half3(.66,.31,.22),_RRTwilight*.65);
                half3 zenith=lerp(half3(.004,.009,.029),half3(.075,.31,.64),_RRDaylight);
                half3 colour=lerp(horizon,zenith,pow(up,.52));
                float towardSun=saturate(dot(ray,normalize(_RRSunDirection.xyz+float3(0,.0001,0))));
                colour+=half3(1,.49,.18)*pow(towardSun,12)*(.18*_RRDaylight+_RRTwilight*.6);
                // Direction-based stars stay fixed through camera translation and origin shifts.
                float2 starPoint=float2(atan2(ray.z,ray.x)*90,asin(clamp(ray.y,-1,1))*90);
                float starHash=CloudHash(floor(starPoint));
                float star=(1-smoothstep(0,.12,length(frac(starPoint)-.5)))*step(.974,starHash);
                colour+=half3(.66,.78,1)*star*nightVisibility*smoothstep(.01,.16,ray.y);
                // Analytic sphere illumination gives distinct waxing/waning terminators.
                float3 moon=normalize(_RRMoonDirection.xyz+float3(0,.0001,0));
                float3 moonRight=normalize(cross(float3(0,1,0),moon));
                float3 moonUp=cross(moon,moonRight);
                float2 moonUV=float2(dot(ray,moonRight),dot(ray,moonUp))/.038;
                float radius2=dot(moonUV,moonUV);
                float moonMask=(1-smoothstep(.96,1,radius2))*step(0,dot(ray,moon))*smoothstep(-.025,.015,ray.y);
                float phase=_RRMoonPhase*6.283185307;
                float3 moonNormal=float3(moonUV,sqrt(saturate(1-radius2)));
                float lit=smoothstep(-.025,.025,dot(moonNormal,float3(sin(phase),0,-cos(phase))));
                float craters=lerp(.65,1,CloudNoise(moonUV*8)*.6+CloudNoise(moonUV*19)*.4);
                half3 moonColour=lerp(half3(.025,.035,.065),half3(1.05,1.13,1.26)*craters,lit);
                colour=lerp(colour,moonColour,moonMask*nightVisibility);
                float2 cloudPoint=ray.xz/max(ray.y,.06)*2.4+float2(_RRPresentationTime*.008,4.3);
                float broad=CloudNoise(cloudPoint),puffs=CloudNoise(cloudPoint*2.3);
                float density=broad*.65+puffs*.25+CloudNoise(cloudPoint*5.2)*.10;
                float cloud=smoothstep(.53,.64,density)*smoothstep(.09,.23,ray.y);
                float body=smoothstep(.54,.79,density);
                half3 cloudColour=lerp(half3(.014,.023,.045),half3(.047,.066,.11),body);
                cloudColour=lerp(cloudColour,lerp(half3(.48,.64,.80),half3(1.15,1.10,.94),body),_RRDaylight);
                cloudColour=lerp(cloudColour,half3(.78,.35,.23),_RRTwilight*(1-body)*.65);
                cloudColour+=half3(1,.80,.48)*pow(towardSun,10)*(1-body)*.27*_RRDaylight;
                colour=lerp(colour,cloudColour,cloud*lerp(.82,.96,_RRDaylight));
                colour+=half3(1,.85,.55)*smoothstep(.9986,.9995,towardSun)*(1-cloud*.94)*2.0*smoothstep(-.025,.015,ray.y);
                return half4(colour,1);
            }
            ENDHLSL
        }
    }
}
