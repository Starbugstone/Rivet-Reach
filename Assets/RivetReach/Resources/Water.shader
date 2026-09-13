Shader "RivetReach/Fluid"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float4 _RRFogColour, _RRFogRange, _RRWorldOffset;
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; half4 color:COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings o;o.positionWS=TransformObjectToWorld(input.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.color=input.color;return o;
            }
            half4 Frag(Varyings input, bool front:SV_IsFrontFace):SV_Target
            {
                float3 p=input.positionWS+_RRWorldOffset.xyz;
                float ripple=sin(p.x*2.2+p.z*1.4+_Time.y*1.2)*cos(p.z*2.8-p.x*.8-_Time.y*.9);
                float3 n=normalize(input.normalWS+float3(ripple*.045,0,cos(p.x*3+_Time.y)*.04));
                if(!front)n=-n;
                Light light=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float3 view=normalize(_WorldSpaceCameraPos-input.positionWS);
                float fresnel=pow(1-saturate(dot(n,view)),4);
                half3 ambient=SampleSH(n);
                half3 color=input.color.rgb*(max(ambient,.18)+light.color*(.35+.65*saturate(dot(n,light.direction)))*light.shadowAttenuation);
                color+=light.color*pow(saturate(dot(n,normalize(view+light.direction))),90)*.45;
                color=lerp(color,half3(.40,.69,.77),fresnel*.28)+ripple*.012;
                // Opaque vertex alpha identifies emissive lava on the shared fluid mesh.
                if(input.color.a>.99)
                {
                    float crust=sin(p.x*3.1+sin(p.z*2.3+_Time.y*.25))*sin(p.z*2.7+sin(p.x*1.8-_Time.y*.2));
                    color=lerp(half3(.20,.025,.008),input.color.rgb*1.5,smoothstep(-.5,.4,crust));
                }
                float fog=saturate((distance(_WorldSpaceCameraPos,input.positionWS)-_RRFogRange.x)/max(1,_RRFogRange.y-_RRFogRange.x));
                return half4(lerp(color,_RRFogColour.rgb,fog),lerp(input.color.a,.90,fresnel));
            }
            ENDHLSL
        }
    }
}
