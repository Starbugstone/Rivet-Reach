// Original deterministic studio shading for baked icons. Runtime lighting is independent.
Shader "RivetReach/ItemIcon"
{
    Properties { _BaseMap("Held palette",2D)="white" {} _BaseColor("Held tint",Color)=(1,1,1,1) _Torch("Torch flame",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            float4 _BaseColor;float _Torch;
            struct A {float3 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float2 uv:TEXCOORD1;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;return o;}
            half4 Frag(V i):SV_Target
            {
                half3 colour=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                float3 n=normalize(i.normalWS);
                half light=.52+.48*saturate(dot(n,normalize(float3(-.45,.75,-.65))))+.16*saturate(dot(n,normalize(float3(.8,.3,.6))));
                if(_Torch>.5&&floor(i.uv.x*4)+floor(i.uv.y*4)*4==12)colour=half3(1,.48,.08);
                return half4(colour*light,1);
            }
            ENDHLSL
        }
    }
}
