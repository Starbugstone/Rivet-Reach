Shader "RivetReach/WeatherRain"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 vertex:POSITION;half4 colour:COLOR;float2 uv:TEXCOORD0;};
            struct V {float4 position:SV_POSITION;half4 colour:COLOR;float2 uv:TEXCOORD0;};
            V Vert(A i){V o;o.position=TransformObjectToHClip(i.vertex.xyz);o.colour=i.colour;o.uv=i.uv;return o;}
            half4 Frag(V i):SV_Target{i.colour.a*=saturate(1-abs(i.uv.x*2-1))*sin(i.uv.y*3.14159265);return i.colour;}
            ENDHLSL
        }
    }
}
