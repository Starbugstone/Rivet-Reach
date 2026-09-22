Shader "RivetReach/HeldBlock"
{
    Properties { _Tiles("Terrain tiles",2DArray)="" {} [HideInInspector] _FirstPerson("First person",Float)=1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+50" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            ZTest LEqual
            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldLighting.hlsl"
            TEXTURE2D_ARRAY(_Tiles);SAMPLER(sampler_Tiles);
            float _FirstPerson;
            float4 _RRAmbientSky,_RRAmbientGround;
            struct A {float3 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;float2 tile:TEXCOORD1;};
            struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float2 uv:TEXCOORD1;float tile:TEXCOORD2;float3 positionWS:TEXCOORD3;};
            V Vert(A i){V o;o.positionWS=TransformObjectToWorld(i.positionOS);o.positionCS=TransformObjectToHClip(i.positionOS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;o.tile=i.tile.x;
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
                InputData input=(InputData)0;input.positionWS=i.positionWS;input.normalWS=normalize(i.normalWS);
                input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);input.bakedGI=SampleSH(input.normalWS);
                input.shadowMask=half4(1,1,1,1);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                SurfaceData surface=(SurfaceData)0;surface.albedo=SAMPLE_TEXTURE2D_ARRAY(_Tiles,sampler_Tiles,i.uv,i.tile).rgb;surface.occlusion=1;surface.alpha=1;
                return RRFragmentPBR(input,surface);
            }
            ENDHLSL
        }
    }
}
