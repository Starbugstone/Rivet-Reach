Shader "RivetReach/ExplorerSkin"
{
    Properties
    {
        _BaseMap("Skin",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1)
        _SurfaceMap("Metal / roughness / skin",2D)="white" {}
        [Normal] _BumpMap("Surface normal",2D)="bump" {}
        [HideInInspector] _FirstPerson("First person",Float)=0
        [HideInInspector] _Cutoff("Cutoff",Float)=0.5
        [HideInInspector] _Cull("Cull",Float)=2
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+50"}
        Pass
        {
            Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            TEXTURE2D(_SurfaceMap);SAMPLER(sampler_SurfaceMap);
            TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;float4 _BaseMap_ST;float _FirstPerson;float _Cutoff;float _Cull;
            CBUFFER_END
            struct A {float3 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float4 tangentWS:TEXCOORD2;float2 uv:TEXCOORD3;float fog:TEXCOORD4;};
            V Vert(A i)
            {
                V o;VertexPositionInputs p=GetVertexPositionInputs(i.positionOS);VertexNormalInputs n=GetVertexNormalInputs(i.normalOS,i.tangentOS);
                o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=n.normalWS;
                o.tangentWS=float4(n.tangentWS,i.tangentOS.w*GetOddNegativeScale());o.uv=i.uv;o.fog=ComputeFogFactor(p.positionCS.z);
                if(_FirstPerson>.5)
                {
                    #if UNITY_REVERSED_Z
                    o.positionCS.z=lerp(o.positionCS.w,o.positionCS.z,.02);
                    #else
                    o.positionCS.z=lerp(UNITY_NEAR_CLIP_VALUE*o.positionCS.w,o.positionCS.z,.02);
                    #endif
                }
                return o;
            }
            half4 Frag(V i):SV_Target
            {
                half3 packed=SAMPLE_TEXTURE2D(_SurfaceMap,sampler_SurfaceMap,i.uv).rgb;
                half3 normalTS=UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv));
                float region=floor(i.uv.x*4)+floor(i.uv.y*4)*4;
                // Only the continuously mapped hands have a coherent tangent-space detail UV.
                if(region!=4&&region!=5)normalTS=half3(0,0,1);
                half3 n=normalize(i.normalWS);half3 tangent=normalize(i.tangentWS.xyz);
                n=normalize(mul(normalTS,half3x3(tangent,cross(n,tangent)*i.tangentWS.w,n)));
                InputData input=(InputData)0;input.positionWS=i.positionWS;input.normalWS=n;
                input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                input.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                input.bakedGI=SampleSH(n);input.shadowMask=half4(1,1,1,1);
                input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                surface.metallic=packed.r;surface.smoothness=1-packed.g;surface.occlusion=1;surface.alpha=1;surface.normalTS=normalTS;
                // Restrained wrap on skin: highlights remain BRDF-based, clothing stays diffuse.
                Light sun=GetMainLight(input.shadowCoord);
                surface.emission=surface.albedo*half3(1,.39,.22)*packed.b*.055*saturate(.4-dot(n,sun.direction))*sun.color*sun.shadowAttenuation;
                half4 colour=UniversalFragmentPBR(input,surface);
                colour.rgb=MixFog(colour.rgb,_FirstPerson>.5?0:i.fog);return colour;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
