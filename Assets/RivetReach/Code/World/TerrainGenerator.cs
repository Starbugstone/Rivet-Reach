using System;

namespace RivetReach
{
    public sealed class TerrainGenerator
    {
        public const string Version="terrain-1";
        public const string WorldId="surface";
        public const int MinY=-256,MaxY=767;
        public const long HorizontalLimit=1000000000;
        public readonly int Seed;
        public TerrainGenerator(int seed) { Seed=seed; }
        static double Smooth(double t) => t*t*(3-2*t);
        static double Lerp(double a,double b,double t) => a+(b-a)*t;
        public static uint Hash(long x,long y,long z,int seed)
        {
            unchecked
            {
                ulong h=(ulong)x*0x9E3779B185EBCA87UL ^ (ulong)y*0xC2B2AE3D27D4EB4FUL ^ (ulong)z*0x165667B19E3779F9UL ^ (uint)seed;
                h^=h>>30;h*=0xBF58476D1CE4E5B9UL;h^=h>>27;h*=0x94D049BB133111EBUL;h^=h>>31;
                return (uint)h;
            }
        }
        double Value(long x,long y,long z) => Hash(x,y,z,Seed)/(double)uint.MaxValue;
        public double Noise(long x,int y,long z,int scale)
        {
            long ix=BlockPos.FloorDiv(x,scale),iy=BlockPos.FloorDiv(y,scale),iz=BlockPos.FloorDiv(z,scale);
            double tx=Smooth((x-ix*scale)/(double)scale),ty=Smooth((y-iy*scale)/(double)scale),tz=Smooth((z-iz*scale)/(double)scale);
            double a=Lerp(Value(ix,iy,iz),Value(ix+1,iy,iz),tx),b=Lerp(Value(ix,iy+1,iz),Value(ix+1,iy+1,iz),tx);
            double c=Lerp(Value(ix,iy,iz+1),Value(ix+1,iy,iz+1),tx),d=Lerp(Value(ix,iy+1,iz+1),Value(ix+1,iy+1,iz+1),tx);
            return Lerp(Lerp(a,b,ty),Lerp(c,d,ty),tz);
        }
        public int Height(long x,long z) => 32+(int)(Noise(x,0,z,96)*42+Noise(x,0,z,32)*14+Noise(x,0,z,12)*3);
        public byte At(BlockPos p)
        {
            if(p.Y<MinY || Math.Abs(p.X)>HorizontalLimit || Math.Abs(p.Z)>HorizontalLimit)return 3;
            if(p.Y>MaxY)return 0;
            return At(p,Height(p.X,p.Z));
        }
        byte At(BlockPos p,int h)
        {
            if(p.Y>h)return 0;
            // A protective surface roof keeps the initial spawn supported; fist mining opens caves.
            if(p.Y>10 && p.Y<h-2 && Noise(p.X,p.Y,p.Z,14)>0.71 && Noise(p.X,p.Y,p.Z,40)>0.42)return 0;
            if(p.Y==h)return 1;
            return p.Y>=h-3?(byte)2:(byte)3;
        }
        public byte[] Generate(ChunkPos chunk)
        {
            var cells=new byte[34*34*34];var min=chunk.Min;
            for(int z=0;z<34;z++)for(int x=0;x<34;x++)
            {
                long wx=min.X+x-1,wz=min.Z+z-1;int h=Height(wx,wz);
                for(int y=0;y<34;y++)
                {
                    int wy=min.Y+y-1;
                    cells[x+34*(y+34*z)]=wy<MinY?(byte)3:wy>MaxY?(byte)0:At(new BlockPos(wx,wy,wz),h);
                }
            }
            return cells;
        }
    }
}
