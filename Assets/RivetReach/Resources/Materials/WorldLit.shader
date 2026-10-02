Shader "RivetReach/WorldLit"
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
            #include "WorldLitForward.hlsl"
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
