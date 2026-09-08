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
            float4 _RRSunDirection;
            float CloudHash(float2 p){return frac(sin(p.x*37.117+p.y*13.713+3.71)*17321.43);}
            float CloudNoise(float2 p)
            {
                float2 cell=floor(p),blend=smoothstep(.25,.75,frac(p));
                return lerp(lerp(CloudHash(cell),CloudHash(cell+float2(1,0)),blend.x),
                    lerp(CloudHash(cell+float2(0,1)),CloudHash(cell+1),blend.x),blend.y);
            }
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 ray:TEXCOORD0; };
            V Vert(A i) { V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.ray=i.positionOS.xyz;return o; }
            half4 Frag(V i):SV_Target
            {
                float3 ray=normalize(i.ray);float up=saturate(ray.y);
                half3 colour=lerp(half3(.61,.71,.80),half3(.16,.34,.61),pow(up,.58));
                float towardSun=saturate(dot(ray,normalize(_RRSunDirection.xyz+float3(0,.0001,0))));
                colour+=half3(1,.77,.46)*pow(towardSun,18)*.12;
                float2 cloudPoint=ray.xz/max(ray.y,.06)*2.8+float2(_Time.y*.004,4.3);
                float density=CloudNoise(cloudPoint)*.68+CloudNoise(cloudPoint*2.7)*.32;
                float cloud=smoothstep(.65,.79,density)*smoothstep(.12,.28,ray.y);
                half3 cloudColour=lerp(half3(.65,.73,.82),half3(.96,.96,.90),smoothstep(.52,.78,density));
                colour=lerp(colour,cloudColour,cloud*.78);
                colour+=half3(1,.87,.65)*smoothstep(.9992,.99965,towardSun)*(1-cloud*.7)*1.1;
                return half4(colour,1);
            }
            ENDHLSL
        }
    }
}
