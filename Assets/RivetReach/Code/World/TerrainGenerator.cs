using System;
using System.Collections.Generic;

namespace RivetReach
{
    public sealed class TerrainGenerator
    {
        public const string Version="terrain-5-seas-rivers";
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
        struct ColumnSample { public bool Valid;public long X,Z;public int Seed;public TerrainColumn Column; }
        [ThreadStatic] static ColumnSample[] columnSamples;
        public TerrainColumn Column(long x,long z)
        {
            if(columnSamples==null)columnSamples=new ColumnSample[4096];
            int index=(int)(Hash(x,1831,z,Seed)&4095);ref var sample=ref columnSamples[index];
            if(sample.Valid&&sample.X==x&&sample.Z==z&&sample.Seed==Seed)return sample.Column;
            var column=TerrainProfile.Sample(x,z,Seed);sample=new ColumnSample{Valid=true,X=x,Z=z,Seed=Seed,Column=column};return column;
        }
        public int Height(long x,long z) => Column(x,z).Height;
        public BiomeId Biome(long x,long z)=>Column(x,z).Biome;
        public const int TreeSpacing=8,CanopyRadius=2,MaxTreeHeight=10;
        public readonly struct Tree
        {
            public readonly BlockPos Root;
            public readonly int Logs;
            public readonly bool Pine;
            public Tree(BlockPos root,int logs,bool pine=false){Root=root;Logs=logs;Pine=pine;}
            public byte At(BlockPos p)
            {
                long dx=Math.Abs(p.X-Root.X),dz=Math.Abs(p.Z-Root.Z);int dy=p.Y-Root.Y;
                if(dx==0&&dz==0&&dy>=0&&dy<Logs)return BlockId.Log;
                if(Pine)
                {
                    if(dy<2||dy>Logs)return 0;
                    int spread=Math.Min(2,(Logs-dy+1)/2);
                    return dx<=spread&&dz<=spread&&dx+dz<=spread+1?BlockId.Leaves:(byte)0;
                }
                int layer=dy-(Logs-1);int radius=layer<0?2:1;
                if(layer < -2||layer>1||dx>radius||dz>radius||dx+dz>radius+1)return 0;
                if(layer==1&&dx+dz>1)return 0;
                return BlockId.Leaves;
            }
        }
        struct TreeSample
        {
            public bool Valid,Present;
            public long X,Z;
            public int Seed;
            public Tree Tree;
        }
        // Pure candidate results, bounded to 256 entries per worker/main thread.
        // Include seed in the key; no locks, world references or expanding world cache.
        [ThreadStatic] static TreeSample[] treeSamples;
        public bool TryTree(long gridX,long gridZ,out Tree tree)
        {
            if(treeSamples==null)treeSamples=new TreeSample[256];
            int index=(int)(Hash(gridX,171,gridZ,Seed)&255);
            ref var sample=ref treeSamples[index];
            if(sample.Valid&&sample.X==gridX&&sample.Z==gridZ&&sample.Seed==Seed)
            {tree=sample.Tree;return sample.Present;}
            bool present=ComputeTree(gridX,gridZ,out tree);
            sample=new TreeSample{Valid=true,Present=present,X=gridX,Z=gridZ,Seed=Seed,Tree=tree};return present;
        }
        bool ComputeTree(long gridX,long gridZ,out Tree tree)
        {
            tree=default;uint hash=Hash(gridX,719,gridZ,Seed);
            long x=gridX*TreeSpacing+2+(hash>>8)%4,z=gridZ*TreeSpacing+2+(hash>>16)%4;
            // Preserve the supported, clear spawn for every seed.
            if(Math.Abs(x)<=5&&Math.Abs(z)<=5)return false;
            if(Math.Abs(x)>HorizontalLimit-CanopyRadius||Math.Abs(z)>HorizontalLimit-CanopyRadius)return false;
            var column=Column(x,z);int h=column.Height;
            if(hash%100>=TerrainProfile.TreeChance(column.Biome)||column.Surface!=BlockId.Grass||CaveGenerator.Air(Seed,new BlockPos(x,h,z),column))return false;
            for(int dz=-2;dz<=2;dz++)for(int dx=-2;dx<=2;dx++)if(Math.Abs(Height(x+dx,z+dz)-h)>2)return false;
            bool pine=column.Biome==BiomeId.Alpine;
            tree=new Tree(new BlockPos(x,h+1,z),(pine?7:4)+(int)((hash>>24)%3),pine);return true;
        }
        public IEnumerable<Tree> Trees(long minX,long minZ,long maxX,long maxZ)
        {
            for(long z=BlockPos.FloorDiv(minZ-CanopyRadius,TreeSpacing);z<=BlockPos.FloorDiv(maxZ+CanopyRadius,TreeSpacing);z++)
            for(long x=BlockPos.FloorDiv(minX-CanopyRadius,TreeSpacing);x<=BlockPos.FloorDiv(maxX+CanopyRadius,TreeSpacing);x++)
                if(TryTree(x,z,out var tree))yield return tree;
        }
        public int OpaqueHeight(long x,long z)
        {
            int h=Height(x,z);
            if(TryTree(BlockPos.FloorDiv(x,TreeSpacing),BlockPos.FloorDiv(z,TreeSpacing),out var t)&&t.Root.X==x&&t.Root.Z==z)
                h=Math.Max(h,t.Root.Y+t.Logs-1);
            return h;
        }
        public byte At(BlockPos p)
        {
            if(p.Y<=MinY || Math.Abs(p.X)>HorizontalLimit || Math.Abs(p.Z)>HorizontalLimit)return BlockId.Bedrock;
            if(p.Y>MaxY)return 0;
            var column=Column(p.X,p.Z);int h=column.Height;byte ground=GroundAt(p,column);
            if(ground==BlockId.Stone)return OreGenerator.At(Seed,p);
            if(ground!=0||p.Y<=h||p.Y>h+MaxTreeHeight+2)return ground;
            byte result=0;
            foreach(var tree in Trees(p.X,p.Z,p.X,p.Z))
            {byte id=tree.At(p);if(id==BlockId.Log)return id;if(id==BlockId.Leaves)result=id;}
            return result==0&&p.Y==h+1&&WildPotato(p.X,p.Z,column)?BlockId.MaturePotatoPlant:result;
        }
        bool WildPotato(long x,long z,TerrainColumn column)
        {
            if(column.Surface!=BlockId.Grass||Math.Abs(x)<=8&&Math.Abs(z)<=8)return false;
            long gx=BlockPos.FloorDiv(x,8),gz=BlockPos.FloorDiv(z,8);uint hash=Hash(gx,7151,gz,Seed);
            if(hash%100>=12||x!=gx*8+2+(hash>>8)%4||z!=gz*8+2+(hash>>16)%4)return false;
            if(GroundAt(new BlockPos(x,column.Height,z),column)!=BlockId.Grass)return false;
            // Keep wild food out of trunks and low canopies. Trees retain stamping priority.
            foreach(var tree in Trees(x,z,x,z))for(int dy=1;dy<=3;dy++)
                if(tree.At(new BlockPos(x,column.Height+dy,z))!=0)return false;
            return true;
        }
        public byte GroundAt(BlockPos p)=>GroundAt(p,Column(p.X,p.Z));
        byte GroundAt(BlockPos p,TerrainColumn column)
        {
            byte id=SolidAt(p,column);
            return id!=0&&id!=BlockId.Bedrock&&!Fluids.IsFluid(id)&&Carvable(p,column)&&CaveGenerator.Air(Seed,p,column)?(byte)0:id;
        }
        static bool Carvable(BlockPos p,TerrainColumn column)=>column.WaterLevel==int.MinValue||p.Y<column.Height-4;
        static byte SolidAt(BlockPos p,TerrainColumn column)
        {
            if(p.Y<=MinY||Math.Abs(p.X)>HorizontalLimit||Math.Abs(p.Z)>HorizontalLimit)return BlockId.Bedrock;
            if(p.Y>MaxY)return BlockId.Air;
            if(p.Y>column.Height)return p.Y<=column.WaterLevel?Fluids.Water.Source:(byte)0;
            if(p.Y==column.Height)return column.Surface;
            return p.Y>=column.Height-column.SoilDepth?column.Subsoil:BlockId.Stone;
        }
        public byte[] Generate(ChunkPos chunk)=>Generate(chunk,out _,out _);
        public byte[] Generate(ChunkPos chunk,out int surfaceMin,out int surfaceMax)
        {
            var cells=new byte[34*34*34];var min=chunk.Min;
            surfaceMin=int.MaxValue;surfaceMax=int.MinValue;
            CaveGenerator.ChunkSampler caves=null;
            for(int z=0;z<34;z++)for(int x=0;x<34;x++)
            {
                long wx=min.X+x-1,wz=min.Z+z-1;var column=Column(wx,wz);
                surfaceMin=Math.Min(surfaceMin,column.Height);surfaceMax=Math.Max(surfaceMax,Math.Max(column.Height+MaxTreeHeight+2,column.WaterLevel+1));
                for(int y=0;y<34;y++)
                {
                    int wy=min.Y+y-1;
                    var p=new BlockPos(wx,wy,wz);byte id=SolidAt(p,column);
                    if(id!=0&&id!=BlockId.Bedrock&&!Fluids.IsFluid(id)&&Carvable(p,column))
                    {
                        if(caves==null)caves=new CaveGenerator.ChunkSampler(Seed,chunk);
                        if(caves.Air(x-1,y-1,z-1,p,column))id=0;
                    }
                    cells[x+34*(y+34*z)]=id;
                }
                int plantY=column.Height+1-min.Y;
                if(plantY>=-1&&plantY<=32&&WildPotato(wx,wz,column))
                    cells[ChunkMesher.Index(x-1,plantY,z-1)]=BlockId.MaturePotatoPlant;
            }
            OreGenerator.Stamp(Seed,chunk,cells);
            // Stamp each candidate once into the chunk and its halo. Discovery order is irrelevant.
            foreach(var tree in Trees(min.X-1,min.Z-1,min.X+32,min.Z+32))
            for(int z=(int)Math.Max(-1,tree.Root.Z-min.Z-CanopyRadius);z<=Math.Min(32,tree.Root.Z-min.Z+CanopyRadius);z++)
            for(int x=(int)Math.Max(-1,tree.Root.X-min.X-CanopyRadius);x<=Math.Min(32,tree.Root.X-min.X+CanopyRadius);x++)
            for(int y=Math.Max(-1,tree.Root.Y-min.Y);y<=Math.Min(32,tree.Root.Y-min.Y+tree.Logs+1);y++)
            {
                var p=min.Offset(x,y,z);byte id=tree.At(p);int i=ChunkMesher.Index(x,y,z);
                if(id!=0&&(cells[i]==0||cells[i]==BlockId.Leaves&&id==BlockId.Log))cells[i]=id;
            }
            return cells;
        }
    }
}
