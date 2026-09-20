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
            float4 _RRFogColour,_RRFogRange;
            struct A {float3 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float4 tangentWS:TEXCOORD2;float2 uv:TEXCOORD3;};
            V Vert(A i)
            {
                V o;o.positionWS=TransformObjectToWorld(i.positionOS);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.tangentWS=float4(TransformObjectToWorldDir(i.tangentOS.xyz),i.tangentOS.w*GetOddNegativeScale());o.uv=i.uv*_BaseMap_ST.xy+_BaseMap_ST.zw;return o;
            }
            half4 Frag(V i):SV_Target
            {
                half4 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
                #if defined(_ALPHATEST_ON)
                clip(albedo.a-_Cutoff);
                #endif
                float3 n=normalize(i.normalWS);
                #if defined(_NORMALMAP)
                half3 nt=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
                float3 tangent=normalize(i.tangentWS.xyz);n=normalize(mul(nt,float3x3(tangent,cross(n,tangent)*i.tangentWS.w,n)));
                #endif
                InputData input=(InputData)0;input.positionWS=i.positionWS;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                input.shadowCoord=TransformWorldToShadowCoord(i.positionWS);input.shadowMask=half4(1,1,1,1);input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                SurfaceData surface=(SurfaceData)0;surface.albedo=albedo.rgb;surface.alpha=albedo.a;surface.metallic=_Metallic;surface.smoothness=_Smoothness;surface.occlusion=1;
                #if defined(_METALLICSPECGLOSSMAP)
                half4 metal=SAMPLE_TEXTURE2D(_MetallicGlossMap,sampler_MetallicGlossMap,i.uv);surface.metallic=metal.r;surface.smoothness*=metal.a;
                #endif
                #if defined(_EMISSION)
                surface.emission=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,i.uv).rgb*_EmissionColor.rgb;
                #endif
                half4 colour=UniversalFragmentPBR(input,surface);
                float fog=smoothstep(_RRFogRange.x,_RRFogRange.y,distance(i.positionWS,GetCameraPositionWS()));
                colour.rgb=lerp(colour.rgb,RRCaveFog(_RRFogColour.rgb,i.positionWS+n*.035),fog);return colour;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
