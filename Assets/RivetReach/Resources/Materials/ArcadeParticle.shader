Shader "RivetReach/ArcadeParticle"
{
    Properties { _Mode("Shape",Float)=0 _FirstPerson("First person",Float)=0 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            float _Mode,_FirstPerson;
            struct A {float3 positionOS:POSITION;float2 uv:TEXCOORD0;half4 colour:COLOR;};
            struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 colour:COLOR;};
            V Vert(A i)
            {
                V o;o.positionCS=TransformObjectToHClip(i.positionOS);o.uv=i.uv;o.colour=i.colour;
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
                float2 p=i.uv*2-1;float radius=length(p);float alpha;
                if(_Mode<.5)alpha=pow(saturate(1-radius*radius),2)*.75;
                else if(_Mode<1.5)alpha=(1-smoothstep(.045,.095,abs(radius-.76)))*.75;
                else if(_Mode<2.5)
                {float diamond=abs(p.x)+abs(p.y);alpha=pow(saturate(1-diamond),2)+saturate(1-abs(p.x)*12)*saturate(1-abs(p.y))* .45+saturate(1-abs(p.y)*12)*saturate(1-abs(p.x))*.45;}
                else alpha=pow(saturate(1-abs(p.y)),1.8);
                if(_FirstPerson<.5)
                {
                    float depth=LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(i.positionCS)),_ZBufferParams);
                    float here=LinearEyeDepth(i.positionCS.z,_ZBufferParams);
                    alpha*=saturate((depth-here)/.16);
                }
                return half4(i.colour.rgb*(_Mode>.5?2.2:1),i.colour.a*alpha);
            }
            ENDHLSL
        }
    }
}
