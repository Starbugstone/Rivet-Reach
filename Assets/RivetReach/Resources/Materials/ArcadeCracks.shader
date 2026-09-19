Shader "RivetReach/ArcadeCracks"
{
    Properties { _Progress("Mining progress",Float)=0 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent"}
        Pass
        {
            ZWrite Off Blend SrcAlpha OneMinusSrcAlpha Offset -1,-1
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _Progress;
            struct A {float3 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
            V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS);o.uv=i.uv;return o;}
            half4 Frag(V i):SV_Target
            {
                float2 p=i.uv-.5;float radius=length(p),angle=atan2(p.y,p.x);
                float arms=abs(sin(angle*3+sin(radius*39)*.25+sin(radius*17)*.36))*radius;
                float branch=abs(sin(angle*6.5+radius*28))*radius;
                float reach=saturate((_Progress*.73-radius)*12);
                float cracks=(1-smoothstep(.004,.013,arms))*reach;
                cracks=max(cracks,(1-smoothstep(.002,.006,branch))*saturate((_Progress-.45)*2)*reach);
                return half4(half3(.012,.015,.019),cracks*.83);
            }
            ENDHLSL
        }
    }
}
