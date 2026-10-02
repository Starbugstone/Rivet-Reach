Shader "RivetReach/MachineLit"
{
    Properties
    {
        _BaseMap("Surface",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1)
        _BumpMap("Normal",2D)="bump" {} _BumpScale("Normal scale",Float)=1
        _MetallicGlossMap("Metal and smoothness",2D)="white" {}
        _Metallic("Metal",Range(0,1))=0 _Smoothness("Smoothness",Range(0,1))=.3
        _EmissionMap("Emission",2D)="white" {} _EmissionColor("Emission",Color)=(0,0,0,0)
        _Cutoff("Cutoff",Float)=.5 _Cull("Cull",Float)=2
        _SrcBlend("Source blend",Float)=1 _DstBlend("Destination blend",Float)=0 _ZWrite("Depth write",Float)=1
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Pass
        {
            Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
            Blend [_SrcBlend] [_DstBlend] ZWrite [_ZWrite] Cull [_Cull]
            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ RR_MACHINE_NEAR RR_MACHINE_FAR
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _METALLICSPECGLOSSMAP
            #pragma shader_feature_local _ALPHAPREMULTIPLY_ON
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            // Match the shared material buffer used by the inherited URP passes.
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "WorldLighting.hlsl"
            #include "FactoryLod.hlsl"
            #include "WorldLitForward.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
            ZWrite On Cull [_Cull] ColorMask 0
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex LodVert
            #pragma fragment LodFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ RR_MACHINE_NEAR RR_MACHINE_FAR
            #pragma shader_feature_local _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "FactoryLod.hlsl"
            float3 _LightDirection;
            struct LA {float3 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct LV {float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;};
            LV LodVert(LA i)
            {
                UNITY_SETUP_INSTANCE_ID(i);LV o;o.world=TransformObjectToWorld(i.p);o.normal=TransformObjectToWorldNormal(i.n);
                o.p=TransformWorldToHClip(ApplyShadowBias(o.world,o.normal,_LightDirection));o.uv=i.uv*_BaseMap_ST.xy+_BaseMap_ST.zw;
                #if UNITY_REVERSED_Z
                o.p.z=min(o.p.z,UNITY_NEAR_CLIP_VALUE);
                #else
                o.p.z=max(o.p.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
            half4 LodFrag(LV i):SV_Target
            {
                RRPresentationClip(i.p,i.world);
                #if defined(_ALPHATEST_ON)
                clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a*_BaseColor.a-_Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"}
            ZWrite On Cull [_Cull] ColorMask 0
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex LodVert
            #pragma fragment LodFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ RR_MACHINE_NEAR RR_MACHINE_FAR
            #pragma shader_feature_local _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "FactoryLod.hlsl"
            float3 _LightDirection;
            struct LA {float3 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct LV {float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;};
            LV LodVert(LA i)
            {
                UNITY_SETUP_INSTANCE_ID(i);LV o;o.world=TransformObjectToWorld(i.p);o.normal=TransformObjectToWorldNormal(i.n);
                o.p=TransformWorldToHClip(o.world);o.uv=i.uv*_BaseMap_ST.xy+_BaseMap_ST.zw;

                return o;
            }
            half4 LodFrag(LV i):SV_Target
            {
                RRPresentationClip(i.p,i.world);
                #if defined(_ALPHATEST_ON)
                clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a*_BaseColor.a-_Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals" Tags {"LightMode"="DepthNormals"}
            ZWrite On Cull [_Cull]
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex LodVert
            #pragma fragment LodFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ RR_MACHINE_NEAR RR_MACHINE_FAR
            #pragma shader_feature_local _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "FactoryLod.hlsl"
            float3 _LightDirection;
            struct LA {float3 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct LV {float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;};
            LV LodVert(LA i)
            {
                UNITY_SETUP_INSTANCE_ID(i);LV o;o.world=TransformObjectToWorld(i.p);o.normal=TransformObjectToWorldNormal(i.n);
                o.p=TransformWorldToHClip(o.world);o.uv=i.uv*_BaseMap_ST.xy+_BaseMap_ST.zw;

                return o;
            }
            half4 LodFrag(LV i):SV_Target
            {
                RRPresentationClip(i.p,i.world);
                #if defined(_ALPHATEST_ON)
                clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a*_BaseColor.a-_Cutoff);
                #endif
                return half4(normalize(i.normal),0);
            }
            ENDHLSL
        }
    }
}
