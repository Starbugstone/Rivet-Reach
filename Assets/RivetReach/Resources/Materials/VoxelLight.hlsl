#ifndef RR_VOXEL_LIGHT_INCLUDED
#define RR_VOXEL_LIGHT_INCLUDED
StructuredBuffer<int4> _RRLightTable;
StructuredBuffer<uint> _RRLightCells;
int _RRLightMask;
float _RRLightEnabled;
float4 _RRHeldTorchAmbient;
int RRLightPage(int3 chunk)
{
    uint hash=((uint)chunk.x*73856093u)^((uint)chunk.y*19349663u)^((uint)chunk.z*83492791u);
    uint slot=hash&(uint)_RRLightMask;
    [loop] for(uint probe=0;probe<=(uint)_RRLightMask;probe++)
    {
        int4 entry=_RRLightTable[slot];if(entry.w==0)return -1;
        if(all(entry.xyz==chunk))return entry.w-1;
        slot=(slot+1)&(uint)_RRLightMask;
    }
    return -1;
}
float2 RRLightCell(int3 cell,int3 commonChunk,int commonPage)
{
    int3 chunk=(int3)floor((float3)cell/32.0);
    int page=all(chunk==commonChunk)?commonPage:RRLightPage(chunk);
    if(page==-1)return 0;
    if(page< -1){uint uniformValue=(uint)(-page-2);return float2(uniformValue>>4,uniformValue&15)/15.0;}
    int3 local=cell-chunk*32;uint index=local.x+32*(local.y+32*local.z);
    uint packed=_RRLightCells[page*8192+index/4];uint value=(packed>>((index&3)*8))&255;
    return float2(value>>4,value&15)/15.0;
}
float2 RRLight(float3 positionWS)
{
    if(_RRLightEnabled<.5)return float2(1,0);
    float3 grid=positionWS-.5;int3 cell=(int3)floor(grid);float3 f=frac(grid);
    int3 chunk=(int3)floor((float3)cell/32.0);int page=RRLightPage(chunk);
    // Eight packed reads share one hash lookup except at chunk boundaries.
    float2 a=lerp(RRLightCell(cell,chunk,page),RRLightCell(cell+int3(1,0,0),chunk,page),f.x);
    float2 b=lerp(RRLightCell(cell+int3(0,1,0),chunk,page),RRLightCell(cell+int3(1,1,0),chunk,page),f.x);
    float2 c=lerp(RRLightCell(cell+int3(0,0,1),chunk,page),RRLightCell(cell+int3(1,0,1),chunk,page),f.x);
    float2 d=lerp(RRLightCell(cell+int3(0,1,1),chunk,page),RRLightCell(cell+int3(1,1,1),chunk,page),f.x);
    return lerp(lerp(a,b,f.y),lerp(c,d,f.y),f.z);
}
float RRSky(float3 positionWS,float3 normalWS)
{
    // Sample the neighbouring air centre for smooth faces. Models in a pass-through cell
    // use that same field without changing their gameplay dimensions.
    float sky=RRLight(positionWS+normalWS*.5).x;return sky*sky;
}
// Soft, short-range carried fill approximates bounce light in direct-light shadows.
// It is presentation only and is suppressed for item/portrait preview cameras.
float3 RRHeldAmbient(float3 positionWS)
{
    if(_RRHeldTorchAmbient.w<.5||_RRLightEnabled<.5)return 0;
    float fade=saturate(1-distance(positionWS,_RRHeldTorchAmbient.xyz)/7);
    return float3(.14,.075,.025)*fade*fade;
}
// A faint, time-independent visual floor; never enters gameplay light queries.
float3 RRCaveAmbient(float3 normalWS)
{return float3(.025,.028,.035)*lerp(.7,1,saturate(normalWS.y*.5+.5));}
float3 RRCaveFog(float3 outdoor,float3 positionWS)
{return lerp(float3(.012,.015,.020),outdoor,RRSky(positionWS,float3(0,0,0)));}
#endif
