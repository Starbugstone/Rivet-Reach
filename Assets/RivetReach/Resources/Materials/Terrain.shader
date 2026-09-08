Shader "RivetReach/VoxelTerrain"
{
    Properties
    {
        _Tiles("Block tiles", 2DArray) = "" {}
        [HideInInspector] _BaseMap("Shadow pass surface", 2D) = "white" {}
        [HideInInspector] _BaseColor("Shadow pass colour", Color) = (1,1,1,1)
        [HideInInspector] _Cutoff("Cutoff", Float) = 0.5
        [HideInInspector] _Cull("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D_ARRAY(_Tiles); SAMPLER(sampler_Tiles);
            float4 _RRFogColour; float4 _RRFogRange;
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float2 tile:TEXCOORD1; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; float tile:TEXCOORD2; float3 positionWS:TEXCOORD3; };
            Varyings Vert(Attributes v)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.uv=v.uv;o.tile=v.tile.x;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                half3 colour=SAMPLE_TEXTURE2D_ARRAY(_Tiles,sampler_Tiles,i.uv,i.tile).rgb;
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half diffuse=saturate(dot(normalize(i.normalWS),sun.direction));
                AmbientOcclusionFactor ao=GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS));
                half3 lighting=half3(.25,.30,.36)*ao.indirectAmbientOcclusion+sun.color*diffuse*sun.shadowAttenuation*.85*ao.directAmbientOcclusion;
                float fog=smoothstep(_RRFogRange.x,_RRFogRange.y,distance(i.positionWS,GetCameraPositionWS()));
                return half4(lerp(colour*lighting,_RRFogColour.rgb,fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
