Shader "RivetReach/HeldTool"
{
    Properties { _BaseColor("Tint",Color)=(1,1,1,1) _BaseMap("Tool palette",2D)="white" {} [HideInInspector] _FirstPerson("First person",Float)=1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+50" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            ZTest LEqual
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            float _FirstPerson;float4 _BaseColor;
            struct A {float3 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;float2 tile:TEXCOORD1;};
            struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float2 uv:TEXCOORD1;float tile:TEXCOORD2;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;o.tile=i.tile.x;
                // Reserve a near depth interval for hands AND items, preserving self-occlusion.
                if(_FirstPerson>.5)
                {
                    #if UNITY_REVERSED_Z
                    o.positionCS.z=lerp(o.positionCS.w,o.positionCS.z,.02);
                    #else
                    o.positionCS.z=lerp(UNITY_NEAR_CLIP_VALUE*o.positionCS.w,o.positionCS.z,.02);
                    #endif
                }
                return o;}
            half4 Frag(V i):SV_Target
            {
                Light sun=GetMainLight();float3 normal=normalize(i.normalWS);
                half3 light=lerp(half3(.28,.29,.25),half3(.47,.53,.59),normal.y*.5+.5)+sun.color*saturate(dot(normal,sun.direction))*.65;
                return half4(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb*light,1);
            }
            ENDHLSL
        }
    }
}
