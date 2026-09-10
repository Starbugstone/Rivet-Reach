Shader "RivetReach/HeldGlass"
{
    Properties { _BaseColor("Tint",Color)=(.37,.68,.76,.22) _FirstPerson("First person",Float)=1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+20" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float _FirstPerson;float4 _BaseColor;
            struct A {float3 positionOS:POSITION;};
            float4 Vert(A i):SV_POSITION
            {
                float4 p=TransformObjectToHClip(i.positionOS);
                if(_FirstPerson>.5)
                {
                    #if UNITY_REVERSED_Z
                    p.z=lerp(p.w,p.z,.02);
                    #else
                    p.z=lerp(UNITY_NEAR_CLIP_VALUE*p.w,p.z,.02);
                    #endif
                }
                return p;
            }
            half4 Frag():SV_Target{return _BaseColor;}
            ENDHLSL
        }
    }
}
