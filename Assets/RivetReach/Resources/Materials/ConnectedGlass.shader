Shader "RivetReach/ConnectedGlass"
{
    Properties { _Tint("Glass tint",Color)=(.67,.86,.88,.10) _Edge("Polished rim",Color)=(.47,.68,.70,.7) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "VoxelLight.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint, _Edge;
            CBUFFER_END
            float4 _RRFogColour, _RRFogRange, _RRWorldOffset;
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;float2 connection:TEXCOORD1;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;nointerpolation float2 connection:TEXCOORD3;};
            Varyings Vert(Attributes input)
            {
                Varyings o;o.positionWS=TransformObjectToWorld(input.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.uv=input.uv;o.connection=input.connection;return o;
            }
            half4 Frag(Varyings input,bool front:SV_IsFrontFace):SV_Target
            {
                uint mask=(uint)round(input.connection.x);
                float2 tex=input.uv;float aa=max(max(fwidth(tex.x),fwidth(tex.y)),.0015);
                float4 edgeDistance=float4(tex.x,1-tex.x,tex.y,1-tex.y);
                float4 enabled=float4((mask&1)==0,(mask&2)==0,(mask&4)==0,(mask&8)==0);
                float4 edge=(1-smoothstep(.018-aa,.018+aa,edgeDistance))*enabled;
                float rim=max(max(edge.x,edge.y),max(edge.z,edge.w));
                // Complete concave corners without reintroducing seams along a joined edge.
                float4 corners=float4(max(tex.x,tex.y),max(1-tex.x,tex.y),max(tex.x,1-tex.y),max(1-tex.x,1-tex.y));
                float4 missing=float4((mask&16)==0,(mask&32)==0,(mask&64)==0,(mask&128)==0);
                float4 corner=(1-smoothstep(.02-aa,.02+aa,corners))*missing;
                rim=max(rim,max(max(corner.x,corner.y),max(corner.z,corner.w)));
                float3 normal=normalize(input.normalWS)*(front?1:-1);
                float3 view=normalize(_WorldSpaceCameraPos-input.positionWS);
                float fresnel=pow(1-saturate(dot(normal,view)),5);
                float2 field=RRSurfaceLight(input.positionWS,normal);float sky=field.x*field.x;
                Light sun=GetMainLight();
                half3 illumination=max(RRCaveAmbient(normal),SampleSH(normal)*sky)+RRBlockAmbient(field.y,normal)+RRHeldAmbient(input.positionWS);
                float3 world=input.positionWS+_RRWorldOffset.xyz;
                // Long, quiet streaks are world-aligned, continuous over joined blocks.
                float stripe=pow(saturate(.5+.5*sin((world.x+world.z)*.65+world.y*.9)),36)*.04*sky;
                float glint=pow(saturate(dot(normal,normalize(view+sun.direction))),160)*.24*sky;
                half3 color=lerp(_Tint.rgb,_Edge.rgb,rim)*max(illumination,.10)+sun.color*(glint+stripe);
                color+=half3(.28,.38,.42)*fresnel*sky;
                float alpha=saturate(lerp(_Tint.a,_Edge.a,rim)+fresnel*.20+glint+stripe);
                float fog=saturate((distance(_WorldSpaceCameraPos,input.positionWS)-_RRFogRange.x)/max(1,_RRFogRange.y-_RRFogRange.x));
                return half4(lerp(color,RRCaveFog(_RRFogColour.rgb,input.positionWS+normal*.035),fog),alpha);
            }
            ENDHLSL
        }
    }
}
