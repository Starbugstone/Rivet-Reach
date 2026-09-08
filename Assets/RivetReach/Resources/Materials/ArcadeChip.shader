Shader "RivetReach/ArcadeChip"
{
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct A {float3 positionOS:POSITION;float3 normalOS:NORMAL;half4 colour:COLOR;};
            struct V {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;half4 colour:COLOR;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.colour=i.colour;return o;}
            half4 Frag(V i):SV_Target
            {
                float3 n=normalize(i.normalWS);Light sun=GetMainLight();
                half3 light=lerp(half3(.32,.32,.26),half3(.52,.67,.80),n.y*.5+.5)+sun.color*saturate(dot(n,sun.direction))*.65;
                return half4(i.colour.rgb*light,i.colour.a);
            }
            ENDHLSL
        }
    }
}
