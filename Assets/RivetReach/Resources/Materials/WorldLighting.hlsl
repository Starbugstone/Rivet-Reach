#ifndef RR_WORLD_LIGHTING_INCLUDED
#define RR_WORLD_LIGHTING_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "VoxelLight.hlsl"
// Adapt URP's public light/GI queries while retaining its BRDF, local lights,
// shadowing and surface response. No package files or copied package shaders.
Light RRMainLight(InputData inputData,half4 shadowMask,AmbientOcclusionFactor ao)
{
    Light light=GetMainLight(inputData,shadowMask,ao);
    light.color*=RRSky(inputData.positionWS,inputData.normalWS);return light;
}
half3 RRGlobalIllumination(BRDFData brdf,BRDFData coat,float coatMask,half3 bakedGI,half occlusion,float3 positionWS,half3 normalWS,half3 viewDirectionWS,float2 screenUV)
{
    float sky=RRSky(positionWS,normalWS);
    return max(GlobalIllumination(brdf,coat,coatMask,bakedGI,occlusion,positionWS,normalWS,viewDirectionWS,screenUV)*sky,
        brdf.diffuse*RRCaveAmbient(normalWS)*occlusion)+brdf.diffuse*RRHeldAmbient(positionWS)*occlusion;
}
#define GetMainLight RRMainLight
#define GlobalIllumination RRGlobalIllumination
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#undef GetMainLight
#undef GlobalIllumination
#endif
