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
            float4 _RRSunDirection;float _RRPresentationTime;
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
                half3 colour=lerp(half3(.72,.82,.87),half3(.075,.31,.64),pow(up,.52));
                float towardSun=saturate(dot(ray,normalize(_RRSunDirection.xyz+float3(0,.0001,0))));
                colour+=half3(1,.69,.31)*pow(towardSun,16)*.18;
                float2 cloudPoint=ray.xz/max(ray.y,.06)*2.4+float2(_RRPresentationTime*.008,4.3);
                float broad=CloudNoise(cloudPoint),puffs=CloudNoise(cloudPoint*2.3);
                float density=broad*.65+puffs*.25+CloudNoise(cloudPoint*5.2)*.10;
                float cloud=smoothstep(.53,.64,density)*smoothstep(.09,.23,ray.y);
                float body=smoothstep(.54,.79,density);
                half3 cloudColour=lerp(half3(.48,.64,.80),half3(1.15,1.10,.94),body);
                cloudColour+=half3(1,.80,.48)*pow(towardSun,10)*(1-body)*.27;
                colour=lerp(colour,cloudColour,cloud*.96);
                colour+=half3(1,.85,.55)*smoothstep(.9986,.9995,towardSun)*(1-cloud*.94)*2.0;
                return half4(colour,1);
            }
            ENDHLSL
        }
    }
}
