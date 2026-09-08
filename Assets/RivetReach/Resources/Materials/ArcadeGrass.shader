Shader "RivetReach/ArcadeGrass"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="AlphaTest"}
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float _RRPresentationTime;float4 _RRWorldOffset;
            struct A {float3 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;};
            V Vert(A i)
            {
                V o;float3 p=TransformObjectToWorld(i.positionOS),stable=p+_RRWorldOffset.xyz;
                float wind=sin(stable.x*.785398163+stable.z*.392699082+_RRPresentationTime*1.7)*.055;
                p.x+=wind*i.uv.y*i.uv.y;p.z+=wind*.5*i.uv.y*i.uv.y;
                o.positionCS=TransformWorldToHClip(p);o.positionWS=p;o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;return o;
            }
            half4 Frag(V i):SV_Target
            {
                // Fade the short decorative fringe with distance using stable screen-door coverage.
                float distanceToEye=distance(i.positionWS,GetCameraPositionWS());float fade=1-smoothstep(12,16,distanceToEye);
                float dither=frac(52.9829189*frac(dot(floor(i.positionCS.xy),float2(.06711056,.00583715))));clip(fade-dither);
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half3 colour=lerp(half3(.055,.17,.065),half3(.32,.49,.14),pow(saturate(i.uv.y),.7));
                half light=.48+abs(dot(normalize(i.normalWS),sun.direction))*.40*sun.shadowAttenuation;
                return half4(colour*(half3(.50,.68,.80)+sun.color*light),1);
            }
            ENDHLSL
        }
    }
}
