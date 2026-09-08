Shader "RivetReach/ArcadeCracks"
{
    Properties { _Progress("Mining progress",Float)=0 _Pulse("Contact pulse",Float)=0 }
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
            float _Progress,_Pulse;
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
                float edge=min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y));
                float flash=_Pulse*(1-smoothstep(.012,.06,edge))*.3;
                return half4(lerp(half3(.055,.07,.09),half3(1.8,1.15,.38),_Pulse*.70),max(cracks*.83,flash));
            }
            ENDHLSL
        }
    }
}
