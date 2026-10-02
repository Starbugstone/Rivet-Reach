Shader "RivetReach/ExplorerSkin"
{
    Properties
    {
        _BaseMap("Skin",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1)
        _SurfaceMap("Metal / roughness / skin",2D)="white" {}
        [Normal] _BumpMap("Surface normal",2D)="bump" {}
        _TintMap("Region tints",2D)="white" {}
        [HideInInspector] _FullDetail("Detail normals everywhere",Float)=0
        [HideInInspector] _VertexOcclusion("Baked vertex occlusion",Float)=0
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
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "WorldLighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            TEXTURE2D(_SurfaceMap);SAMPLER(sampler_SurfaceMap);
            TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
            // One texel per skin-atlas cell: independently chosen hair, skin and cloth colours.
            TEXTURE2D(_TintMap);SAMPLER(rr_point_clamp_sampler);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;float4 _BaseMap_ST;float _FirstPerson;float _Cutoff;float _Cull;float _FullDetail;float _VertexOcclusion;
            CBUFFER_END
            float4 _RRFogColour,_RRFogRange;
            struct A {float3 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;float4 colour:COLOR;};
            struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float4 tangentWS:TEXCOORD2;float2 uv:TEXCOORD3;float occlusion:TEXCOORD4;};
            V Vert(A i)
            {
                V o;VertexPositionInputs p=GetVertexPositionInputs(i.positionOS);VertexNormalInputs n=GetVertexNormalInputs(i.normalOS,i.tangentOS);
                o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=n.normalWS;
                o.tangentWS=float4(n.tangentWS,i.tangentOS.w*GetOddNegativeScale());o.uv=i.uv;
                o.occlusion=lerp(1,saturate(i.colour.r),_VertexOcclusion);
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
                if(region!=4&&region!=5&&_FullDetail<.5)normalTS=half3(0,0,1);
                half3 n=normalize(i.normalWS);half3 tangent=normalize(i.tangentWS.xyz);
                n=normalize(mul(normalTS,half3x3(tangent,cross(n,tangent)*i.tangentWS.w,n)));
                InputData input=(InputData)0;input.positionWS=i.positionWS;input.normalWS=n;
                input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                input.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                input.bakedGI=SampleSH(n);input.shadowMask=half4(1,1,1,1);
                input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                SurfaceData surface=(SurfaceData)0;
                half3 tint=SAMPLE_TEXTURE2D(_TintMap,rr_point_clamp_sampler,i.uv).rgb;
                // Baked creases darken ambient fully and direct light partially (cavity).
                half occlusion=i.occlusion;
                surface.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb*tint*lerp(1,occlusion,.55*_VertexOcclusion);
                surface.metallic=packed.r;surface.smoothness=1-packed.g;surface.occlusion=occlusion;surface.alpha=1;surface.normalTS=normalTS;
                // Restrained wrap on skin: highlights remain BRDF-based, clothing stays diffuse.
                Light sun=GetMainLight(input.shadowCoord);
                surface.emission=surface.albedo*half3(1,.39,.22)*packed.b*.055*saturate(.4-dot(n,sun.direction))*sun.color*sun.shadowAttenuation*RRSky(i.positionWS,n);
                half4 colour=RRFragmentPBR(input,surface);
                // Match the world's distance fog. URP fog keyword stripping must not
                // turn a nearby explorer into a solid fog-colour silhouette.
                float fog=_FirstPerson>.5||_RRFogRange.y<=_RRFogRange.x?0:smoothstep(_RRFogRange.x,_RRFogRange.y,distance(i.positionWS,GetCameraPositionWS()));
                colour.rgb=lerp(colour.rgb,RRCaveFog(_RRFogColour.rgb,i.positionWS),fog);return colour;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
