using System;

namespace RivetReach
{
    public static class CaveGenerator
    {
        const int Spacing=8;
        readonly struct Fields
        {
            public readonly float A,B,Room,Detail;
            public Fields(double a,double b,double room,double detail){A=(float)a;B=(float)b;Room=(float)room;Detail=(float)detail;}
        }
        struct Corner {public bool Valid;public long X,Y,Z;public int Seed;public Fields Value;}
        [ThreadStatic] static Corner[] corners;
        static Fields Sample(int seed,long x,long y,long z)
        {
            if(corners==null)corners=new Corner[4096];
            int index=(int)(TerrainGenerator.Hash(x,y,z,seed)&4095);ref var entry=ref corners[index];
            if(entry.Valid&&entry.Seed==seed&&entry.X==x&&entry.Y==y&&entry.Z==z)return entry.Value;
            var value=new Fields(WorldNoise.Three(x*Spacing,y*Spacing,z*Spacing,48,seed^3001)-.5,
                WorldNoise.Three(x*Spacing,y*Spacing,z*Spacing,48,seed^3011)-.5,
                WorldNoise.Three(x*Spacing,y*Spacing,z*Spacing,72,seed^3019),WorldNoise.Three(x*Spacing,y*Spacing,z*Spacing,28,seed^3023));
            entry=new Corner{Valid=true,Seed=seed,X=x,Y=y,Z=z,Value=value};return value;
        }
        static float Mix(float a,float b,float c,float d,float e,float f,float g,float h,float x,float y,float z)
        {
            float ab=a+(b-a)*x,cd=c+(d-c)*x,ef=e+(f-e)*x,gh=g+(h-g)*x;
            float front=ab+(cd-ab)*y,back=ef+(gh-ef)*y;return front+(back-front)*z;
        }
        static Fields Interpolate(Fields a,Fields b,Fields c,Fields d,Fields e,Fields f,Fields g,Fields h,float x,float y,float z)
            =>new Fields(Mix(a.A,b.A,c.A,d.A,e.A,f.A,g.A,h.A,x,y,z),Mix(a.B,b.B,c.B,d.B,e.B,f.B,g.B,h.B,x,y,z),
                Mix(a.Room,b.Room,c.Room,d.Room,e.Room,f.Room,g.Room,h.Room,x,y,z),Mix(a.Detail,b.Detail,c.Detail,d.Detail,e.Detail,f.Detail,g.Detail,h.Detail,x,y,z));
        static bool Eligible(BlockPos p,TerrainColumn column,out double opening)
        {
            opening=0;int cover=column.Height-p.Y;
            if(cover<0||p.Y<=TerrainGenerator.MinY+3)return false;
            if(cover>=12){opening=1;return true;}
            if(Math.Abs(p.X)<=8&&Math.Abs(p.Z)<=8)return false;
            opening=Math.Max(WorldNoise.Ramp(0,12,cover),column.Entrance);
            return opening>=.05;
        }
        static bool Carve(Fields f,double opening)=>Math.Abs(f.A)<.055*opening&&Math.Abs(f.B)<.065*opening||
            f.Room>WorldNoise.Blend(.85,.74,opening)&&f.Detail>.48;
        // Point reads interpolate the same globally aligned density lattice as workers.
        public static bool Air(int seed,BlockPos p,TerrainColumn column)
        {
            if(!Eligible(p,column,out double opening))return false;
            long x=BlockPos.FloorDiv(p.X,Spacing),y=BlockPos.FloorDiv(p.Y,Spacing),z=BlockPos.FloorDiv(p.Z,Spacing);
            var f=Interpolate(Sample(seed,x,y,z),Sample(seed,x+1,y,z),Sample(seed,x,y+1,z),Sample(seed,x+1,y+1,z),
                Sample(seed,x,y,z+1),Sample(seed,x+1,y,z+1),Sample(seed,x,y+1,z+1),Sample(seed,x+1,y+1,z+1),
                (p.X-x*Spacing)/(float)Spacing,(p.Y-y*Spacing)/(float)Spacing,(p.Z-z*Spacing)/(float)Spacing);
            return Carve(f,opening);
        }
        // One bounded immutable 7-cubed snapshot per page, reused for every voxel and its halo.
        // No repeated noise hashes in the dense inner loop and no growing exploration cache.
        public sealed class ChunkSampler
        {
            const int Width=7;
            readonly Fields[] values=new Fields[Width*Width*Width];
            readonly long gx,gy,gz;
            public ChunkSampler(int seed,ChunkPos chunk)
            {
                var min=chunk.Min;gx=BlockPos.FloorDiv(min.X-1,Spacing);gy=BlockPos.FloorDiv(min.Y-1,Spacing);gz=BlockPos.FloorDiv(min.Z-1,Spacing);
                for(int z=0;z<Width;z++)for(int y=0;y<Width;y++)for(int x=0;x<Width;x++)values[x+Width*(y+Width*z)]=Sample(seed,gx+x,gy+y,gz+z);
            }
            public bool Air(int lx,int ly,int lz,BlockPos p,TerrainColumn column)
            {
                if(!Eligible(p,column,out double opening))return false;
                int x=lx+8,y=ly+8,z=lz+8,i=(x>>3)+Width*((y>>3)+Width*(z>>3));
                float u=(x&7)/8f,v=(y&7)/8f,w=(z&7)/8f;
                ref var a=ref values[i];ref var b=ref values[i+1];ref var c=ref values[i+Width];ref var d=ref values[i+Width+1];
                ref var e=ref values[i+Width*Width];ref var f=ref values[i+Width*Width+1];ref var g=ref values[i+Width*Width+Width];ref var h=ref values[i+Width*Width+Width+1];
                if(Math.Abs(Mix(a.A,b.A,c.A,d.A,e.A,f.A,g.A,h.A,u,v,w))<.055*opening&&
                    Math.Abs(Mix(a.B,b.B,c.B,d.B,e.B,f.B,g.B,h.B,u,v,w))<.065*opening)return true;
                if(Mix(a.Room,b.Room,c.Room,d.Room,e.Room,f.Room,g.Room,h.Room,u,v,w)<=WorldNoise.Blend(.85,.74,opening))return false;
                return Mix(a.Detail,b.Detail,c.Detail,d.Detail,e.Detail,f.Detail,g.Detail,h.Detail,u,v,w)>.48;
            }
        }
    }
}
