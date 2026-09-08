Shader "RivetReach/BlockOutline"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            ZWrite Off ZTest LEqual Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            Offset -1,-1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float3 positionOS:POSITION;float3 other:TEXCOORD0;float side:TEXCOORD1;};
            struct V {float4 positionCS:SV_POSITION;};
            V Vert(A i)
            {
                V o;o.positionCS=TransformObjectToHClip(i.positionOS);float4 other=TransformObjectToHClip(i.other);
                float2 delta=(other.xy/max(other.w,.0001)-o.positionCS.xy/max(o.positionCS.w,.0001))*_ScreenParams.xy;
                float2 direction=delta/max(length(delta),.001);
                o.positionCS.xy+=float2(-direction.y,direction.x)*i.side*1.7/_ScreenParams.xy*o.positionCS.w;
                return o;
            }
            half4 Frag(V i):SV_Target{return half4(.015,.02,.025,.78);}
            ENDHLSL
        }
    }
}
