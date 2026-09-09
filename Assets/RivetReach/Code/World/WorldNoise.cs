using System;

namespace RivetReach
{
    // Integer lattice addresses retain precision across the supported billion-block world.
    public static class WorldNoise
    {
        public static double Smooth(double t)=>t*t*t*(t*(t*6-15)+10);
        public static double Blend(double a,double b,double t)=>a+(b-a)*t;
        public static double Ramp(double lo,double hi,double v)=>Smooth(Math.Max(0,Math.Min(1,(v-lo)/(hi-lo))));
        static double Sample(long x,long y,long z,int seed)=>TerrainGenerator.Hash(x,y,z,seed)/(double)uint.MaxValue;
        public static double Two(double x,double z,int scale,int seed)
        {
            double px=x/scale,pz=z/scale;long ix=(long)Math.Floor(px),iz=(long)Math.Floor(pz);
            double u=Smooth(px-ix),v=Smooth(pz-iz);
            return Blend(Blend(Sample(ix,0,iz,seed),Sample(ix+1,0,iz,seed),u),Blend(Sample(ix,0,iz+1,seed),Sample(ix+1,0,iz+1,seed),u),v);
        }
        public static double Three(double x,double y,double z,int scale,int seed)
        {
            double px=x/scale,py=y/scale,pz=z/scale;long ix=(long)Math.Floor(px),iy=(long)Math.Floor(py),iz=(long)Math.Floor(pz);
            double u=Smooth(px-ix),v=Smooth(py-iy),w=Smooth(pz-iz);
            double a=Blend(Sample(ix,iy,iz,seed),Sample(ix+1,iy,iz,seed),u),b=Blend(Sample(ix,iy+1,iz,seed),Sample(ix+1,iy+1,iz,seed),u);
            double c=Blend(Sample(ix,iy,iz+1,seed),Sample(ix+1,iy,iz+1,seed),u),d=Blend(Sample(ix,iy+1,iz+1,seed),Sample(ix+1,iy+1,iz+1,seed),u);
            return Blend(Blend(a,b,v),Blend(c,d,v),w);
        }
    }
}
