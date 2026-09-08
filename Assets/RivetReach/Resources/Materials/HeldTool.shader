Shader "RivetReach/HeldTool"
{
    Properties { _BaseColor("Tint",Color)=(1,1,1,1) _BaseMap("Tool palette",2D)="white" {} [HideInInspector] _FirstPerson("First person",Float)=1 [HideInInspector] _AxePalette("Axe palette",Float)=0 }
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
            float _FirstPerson,_AxePalette;float4 _BaseColor;
            struct A {float3 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;float2 tile:TEXCOORD1;};
            struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float2 uv:TEXCOORD1;float tile:TEXCOORD2;float3 positionWS:TEXCOORD3;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS);o.positionWS=TransformObjectToWorld(i.positionOS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;o.tile=i.tile.x;
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
                float region=floor(i.uv.x*4)+floor(i.uv.y*4)*4;
                half metal=_AxePalette>.5?((i.uv.x>=.25&&i.uv.x<.75)||i.uv.x>.875?1:0):(region==12||region==15?1:0);
                InputData input=(InputData)0;input.positionWS=i.positionWS;input.normalWS=normalize(i.normalWS);
                input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);input.bakedGI=SampleSH(input.normalWS);
                input.shadowMask=half4(1,1,1,1);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                SurfaceData surface=(SurfaceData)0;surface.albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                surface.metallic=metal*.8;surface.smoothness=lerp(.23,.66,metal);surface.alpha=1;surface.occlusion=1;
                return UniversalFragmentPBR(input,surface);
            }
            ENDHLSL
        }
    }
}
