Shader "RivetReach/VoxelTerrain"
{
    Properties { _Tiles("Block tiles", 2DArray) = "" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D_ARRAY(_Tiles); SAMPLER(sampler_Tiles);
            float4 _RRFogColour; float4 _RRFogRange;
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float2 tile:TEXCOORD1; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; float tile:TEXCOORD2; float fog:TEXCOORD3; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.uv=v.uv;o.tile=v.tile.x;o.fog=distance(TransformObjectToWorld(v.positionOS.xyz),GetCameraPositionWS());return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                half3 colour=SAMPLE_TEXTURE2D_ARRAY(_Tiles,sampler_Tiles,i.uv,i.tile).rgb;
                Light sun=GetMainLight();half diffuse=saturate(dot(normalize(i.normalWS),sun.direction));
                half3 lighting=half3(.37,.42,.46)+sun.color*diffuse*.65;
                return half4(lerp(colour*lighting,_RRFogColour.rgb,saturate((i.fog-_RRFogRange.x)/max(1,_RRFogRange.y-_RRFogRange.x))),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
