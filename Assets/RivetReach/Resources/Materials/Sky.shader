Shader "RivetReach/ExpeditionSky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; };
            struct V { float4 positionCS:SV_POSITION; float3 ray:TEXCOORD0; };
            V Vert(A i) { V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.ray=i.positionOS.xyz;return o; }
            half4 Frag(V i):SV_Target
            {
                float3 ray=normalize(i.ray);float up=saturate(ray.y);
                half3 colour=lerp(half3(.56,.68,.77),half3(.19,.38,.62),pow(up,.65));
                float sun=pow(saturate(dot(ray,normalize(float3(-.4,.65,.35)))),600);
                colour+=half3(1,.83,.55)*sun*.8;
                return half4(colour,1);
            }
            ENDHLSL
        }
    }
}
