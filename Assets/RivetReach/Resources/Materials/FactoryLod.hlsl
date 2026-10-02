#ifndef RR_FACTORY_LOD
#define RR_FACTORY_LOD
float3 _RRPresentationEye;
float4 _RRPresentationFade;
void RRPresentationClip(float4 screen,float3 world)
{
    float fade=saturate((distance(world,_RRPresentationEye)-_RRPresentationFade.x)/max(.001,_RRPresentationFade.y-_RRPresentationFade.x));
    float noise=InterleavedGradientNoise(screen.xy,0);
    #if defined(RR_MACHINE_NEAR)
    clip(noise-fade);
    #elif defined(RR_MACHINE_FAR)
    clip(fade-noise-.00001);
    #endif
}
#endif
