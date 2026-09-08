Shader "RivetReach/VoxelTerrain"
{
    Properties
    {
        _Tiles("Block tiles", 2DArray) = "" {}
        _DetailTiles("Terrain normals and roughness", 2DArray) = "" {}
        [HideInInspector] _BaseMap("Shadow pass surface", 2D) = "white" {}
        [HideInInspector] _BaseColor("Shadow pass colour", Color) = (1,1,1,1)
        [HideInInspector] _Cutoff("Cutoff", Float) = 0.5
        [HideInInspector] _Cull("Cull", Float) = 2
    }
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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D_ARRAY(_Tiles); SAMPLER(sampler_Tiles);
            TEXTURE2D_ARRAY(_DetailTiles); SAMPLER(sampler_DetailTiles);
            float4 _RRFogColour; float4 _RRFogRange; float4 _RRWorldOffset; float _RRPresentationTime;float4 _RRImpactLight,_RRImpactColour;
            float PaletteHash(float2 cell)
            {
                cell=cell-floor(cell/64)*64;
                return frac(sin(cell.x*17.713+cell.y*43.117+9.31)*19341.71);
            }
            float PaletteNoise(float2 coordinate)
            {
                float2 cell=floor(coordinate),blend=frac(coordinate);blend=blend*blend*(3-2*blend);
                return lerp(lerp(PaletteHash(cell),PaletteHash(cell+float2(1,0)),blend.x),
                    lerp(PaletteHash(cell+float2(0,1)),PaletteHash(cell+1),blend.x),blend.y);
            }
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float2 tile:TEXCOORD1; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; float tile:TEXCOORD2; float3 positionWS:TEXCOORD3; };
            Varyings Vert(Attributes v)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(v.normalOS);
                o.uv=v.uv;o.tile=v.tile.x;o.positionWS=TransformObjectToWorld(v.positionOS.xyz);return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float3 normal=normalize(i.normalWS);
                float3 world=i.positionWS+_RRWorldOffset.xyz;
                half3 colour=SAMPLE_TEXTURE2D_ARRAY(_Tiles,sampler_Tiles,i.uv,i.tile).rgb;
                half4 detail=SAMPLE_TEXTURE2D_ARRAY(_DetailTiles,sampler_DetailTiles,i.uv,i.tile);
                float3 dp1=ddx(i.positionWS),dp2=ddy(i.positionWS);float2 duv1=ddx(i.uv),duv2=ddy(i.uv);
                float3 a=cross(dp2,normal),b=cross(normal,dp1);
                float3 tangent=a*duv1.x+b*duv2.x,bitangent=a*duv1.y+b*duv2.y;
                float norm=rsqrt(max(max(dot(tangent,tangent),dot(bitangent,bitangent)),.00000001));
                normal=normalize(normal*(detail.b*2-1)+(tangent*(detail.r*2-1)+bitangent*(detail.g*2-1))*norm);
                float patch=PaletteNoise(world.xz/16);
                colour*=lerp(.91,1.10,patch);
                if(i.tile<1.5)colour*=lerp(half3(1.08,1.02,.83),half3(.87,1.03,1.01),patch);
                // Light moving across leaves conveys wind without moving voxel collision or seams.
                if(i.tile==6)colour*=1+sin(world.x*.785398163+world.z*.392699082+_RRPresentationTime*1.7)*.065;
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half diffuse=saturate(dot(normal,sun.direction));
                AmbientOcclusionFactor ao=GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS));
                half3 ambient=lerp(half3(.22,.21,.17),half3(.37,.48,.62),normal.y*.5+.5);
                float clouds=PaletteNoise(world.xz/16+float2(_RRPresentationTime*.016,0));
                float cloudLight=lerp(.84,1,smoothstep(.35,.68,clouds));
                half3 lighting=ambient*ao.indirectAmbientOcclusion+sun.color*diffuse*sun.shadowAttenuation*.82*ao.directAmbientOcclusion*cloudLight;
                if(i.tile==6)lighting+=half3(.30,.42,.12)*saturate(dot(-normal,sun.direction))*.32*sun.shadowAttenuation;
                float3 view=GetWorldSpaceNormalizeViewDir(i.positionWS),halfVector=normalize(view+sun.direction);
                float sheen=pow(saturate(dot(normal,halfVector)),lerp(18,48,1-detail.a))*.035*sun.shadowAttenuation;
                colour=colour*lighting+sun.color*sheen;
                if(_RRImpactLight.w>0)
                {
                    float3 toLight=_RRImpactLight.xyz-i.positionWS;float distanceSquared=max(dot(toLight,toLight),.01);
                    colour+=_RRImpactColour.rgb*pow(saturate(1-sqrt(distanceSquared)/2.4),2)*saturate(dot(normal,toLight*rsqrt(distanceSquared)))*_RRImpactLight.w*.19;
                }
                float range=distance(i.positionWS,GetCameraPositionWS());
                float fog=smoothstep(_RRFogRange.x,_RRFogRange.y,range);
                // A small amount of aerial perspective separates ridges before the streaming fade.
                float haze=(1-exp(-range*.0014))*(1-fog);
                colour=lerp(colour,_RRFogColour.rgb,haze);
                return half4(lerp(colour,_RRFogColour.rgb,fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
