using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    public sealed class ChunkBuild
    {
        public ChunkPos Position;
        public int Revision,Token;
        public bool HasSurfaceRange;
        public int SurfaceMin,SurfaceMax;
        public byte[] Cells;
        public TerrainVertex[] Vertices;
        public Bounds Bounds;
        public int[] Triangles;
        public FluidMeshData FluidMesh;
        public double Milliseconds;
        public Mesh ToMesh(Mesh mesh=null)=>ChunkMeshUpload.Terrain(Vertices,Triangles,Bounds,mesh);
        public void Translate(Vector3 offset)
        {
            for(int i=0;i<Vertices.Length;i++)Vertices[i].Position+=offset;
            if(Vertices.Length>0)Bounds.center+=offset;
        }
    }

    public static class ChunkMesher
    {
        // Registry and block identities are immutable for the process lifetime.
        static readonly bool[] terrainSolid=BuildSolidTable();
        static bool[] BuildSolidTable()
        {
            var table=new bool[256];for(int i=0;i<table.Length;i++)table[i]=BlockId.Solid((byte)i)&&!CrateId.Part((byte)i)&&!BedId.Part((byte)i)&&i!=BlockId.MobSpawner&&!IndustryId.Placed((byte)i)&&i!=IndustryId.DoorUpper&&!StarterStationVisuals.UsesModel((byte)i);return table;
        }
        // Each concurrent build exclusively owns its scratch until publication.
        // Keep the two workers plus synchronous edit path warm, but do not retain
        // pathological checkerboard/editor-fixture buffers for the whole session.
        sealed class Scratch
        {
            public readonly List<Vector3> Vertices=new List<Vector3>(),Normals=new List<Vector3>();
            public readonly List<Vector2> UV=new List<Vector2>(),Tiles=new List<Vector2>();
            public readonly List<int> Indices=new List<int>();
            public readonly List<(Vector3 position,byte id)> Plants=new List<(Vector3,byte)>();
            public readonly byte[] Mask=new byte[1024];
            public void Clear(){Vertices.Clear();Normals.Clear();UV.Clear();Tiles.Clear();Indices.Clear();Plants.Clear();}
            public long Bytes=>1024L+12L*(Vertices.Capacity+Normals.Capacity)+8L*(UV.Capacity+Tiles.Capacity)+4L*Indices.Capacity+16L*Plants.Capacity;
            long UsedBytes=>1024L+12L*(Vertices.Count+Normals.Count)+8L*(UV.Count+Tiles.Count)+4L*Indices.Count+16L*Plants.Count;
            public bool Retainable=>Bytes<=32L*1024*1024;
            public void CompactForRetention()
            {
                // Dense crop pages can fit the byte budget while List's doubled
                // capacities exceed it. Keep up to 12.5% headroom within the same
                // byte budget so gradual growth does not compact every revision.
                if(Retainable||UsedBytes>32L*1024*1024)return;
                long used=UsedBytes-1024;
                double growth=used==0?1:System.Math.Min(1.125,(32L*1024*1024-1024)/(double)used);
                Vertices.Capacity=(int)(Vertices.Count*growth);Normals.Capacity=(int)(Normals.Count*growth);
                UV.Capacity=(int)(UV.Count*growth);Tiles.Capacity=(int)(Tiles.Count*growth);
                Indices.Capacity=(int)(Indices.Count*growth);Plants.Capacity=(int)(Plants.Count*growth);
            }
        }
        const int ScratchLimit=3;
        static readonly Stack<Scratch> scratchPool=new Stack<Scratch>(ScratchLimit);
        const long ScratchByteLimit=64L*1024*1024;
        static long retainedScratchBytes;
        static readonly int[] stride={1,34,1156};
        static Scratch RentScratch()
        {lock(scratchPool){if(scratchPool.Count>0){var scratch=scratchPool.Pop();retainedScratchBytes-=scratch.Bytes;return scratch;}}return new Scratch();}
        static void ReturnScratch(Scratch scratch)
        {
            scratch.CompactForRetention();
            scratch.Clear();
            if(!scratch.Retainable)return;
            lock(scratchPool)
            {
                while(scratchPool.Count>0&&(scratchPool.Count>=ScratchLimit||retainedScratchBytes+scratch.Bytes>ScratchByteLimit))
                    retainedScratchBytes-=scratchPool.Pop().Bytes;
                scratchPool.Push(scratch);retainedScratchBytes+=scratch.Bytes;
            }
        }
        public static int Index(int x,int y,int z) => x+1+34*(y+1+34*(z+1));
        public static ChunkBuild Build(ChunkPos pos,int revision,byte[] cells)
        {
            var scratch=RentScratch();
            try{return Build(pos,revision,cells,scratch);}
            finally{ReturnScratch(scratch);}
        }
        static ChunkBuild Build(ChunkPos pos,int revision,byte[] cells,Scratch scratch)
        {
            var vertices=scratch.Vertices;var normals=scratch.Normals;var uv=scratch.UV;
            var tiles=scratch.Tiles;var indices=scratch.Indices;var mask=scratch.Mask;var plants=scratch.Plants;
            // Every mask slot is overwritten before each layer consumes it.
            for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
            {
                int u=(axis+1)%3,v=(axis+2)%3;
                for(int layer=0;layer<32;layer++)
                {
                    for(int j=0;j<32;j++)for(int i=0;i<32;i++)
                    {
                        int address=Index(0,0,0)+layer*stride[axis]+i*stride[u]+j*stride[v];
                        byte a=cells[address],b=cells[address+sign*stride[axis]];
                        if(axis==0&&sign==-1&&(BlockId.Crop(a)||a==BlockId.Sapling))plants.Add((new Vector3(layer,i,j),a));
                        mask[i+j*32]=terrainSolid[a]&&!terrainSolid[b]?a:(byte)0;
                    }
                    for(int j=0;j<32;j++)for(int i=0;i<32;)
                    {
                        byte id=mask[i+j*32];if(id==0){i++;continue;}
                        int width=1;while(i+width<32&&mask[i+width+j*32]==id)width++;
                        int height=1;bool stop=false;
                        while(j+height<32&&!stop)
                        {
                            for(int k=0;k<width;k++)if(mask[i+k+(j+height)*32]!=id){stop=true;break;}
                            if(!stop)height++;
                        }
                        Vector3 p=Vector3.zero,du=Vector3.zero,dv=Vector3.zero,n=Vector3.zero;
                        p[axis]=layer+(sign>0?1:0);p[u]=i;p[v]=j;du[u]=width;dv[v]=height;n[axis]=sign;
                        int start=vertices.Count;
                        vertices.Add(p);vertices.Add(p+du);vertices.Add(p+du+dv);vertices.Add(p+dv);
                        uv.Add(Vector2.zero);
                        if(axis==0){uv.Add(new Vector2(0,width));uv.Add(new Vector2(height,width));uv.Add(new Vector2(height,0));}
                        else {uv.Add(new Vector2(width,0));uv.Add(new Vector2(width,height));uv.Add(new Vector2(0,height));}
                        int tile=BlockId.Tile(id,axis,sign);
                        for(int k=0;k<4;k++){normals.Add(n);tiles.Add(new Vector2(tile,0));}
                        if(sign>0){indices.Add(start);indices.Add(start+1);indices.Add(start+2);indices.Add(start);indices.Add(start+2);indices.Add(start+3);}
                        else {indices.Add(start);indices.Add(start+2);indices.Add(start+1);indices.Add(start);indices.Add(start+3);indices.Add(start+2);}
                        for(int y=0;y<height;y++)for(int x=0;x<width;x++)mask[i+x+(j+y)*32]=0;
                        i+=width;
                    }
                }
            }
            foreach(var plant in plants)
            {
                if(FarmingMeshes.Append(plant.id,plant.position+new Vector3(.5f,0,.5f),vertices,normals,uv,tiles,indices))continue;
                float h=plant.id==BlockId.Sapling?.8f:.22f+(plant.id-BlockId.PotatoPlant)*.16f;var centre=plant.position+new Vector3(.5f,0,.5f);
                void Leaf(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool stem=false)
                {
                    Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
                    for(int side=0;side<2;side++)
                    {
                        int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
                        uv.Add(Vector2.zero);uv.Add(Vector2.right);uv.Add(Vector2.one);uv.Add(Vector2.up);
                        for(int k=0;k<4;k++){normals.Add(side==0?normal:-normal);tiles.Add(new Vector2(stem&&plant.id==BlockId.Sapling?4:BlockId.Tile(plant.id,1,1),0));}
                        if(side==0){indices.Add(start);indices.Add(start+1);indices.Add(start+2);indices.Add(start);indices.Add(start+2);indices.Add(start+3);}
                        else {indices.Add(start);indices.Add(start+2);indices.Add(start+1);indices.Add(start);indices.Add(start+3);indices.Add(start+2);}
                    }
                }
                Leaf(centre+Vector3.left*.025f,centre+Vector3.right*.025f,centre+new Vector3(.025f,h,0),centre+new Vector3(-.025f,h,0),true);
                for(int leaf=0;leaf<6;leaf++)
                {
                    float angle=leaf*Mathf.PI/3;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var across=new Vector3(-direction.z,0,direction.x);
                    var start=centre+Vector3.up*h*(.3f+leaf%2*.28f);var end=start+direction*h*.65f+Vector3.up*.06f;
                    Leaf(start,Vector3.Lerp(start,end,.5f)-across*h*.19f,end,Vector3.Lerp(start,end,.5f)+across*h*.19f);
                }
            }
            var packed=new TerrainVertex[vertices.Count];var bounds=new MeshBounds();
            for(int i=0;i<packed.Length;i++)
            {var position=vertices[i];packed[i]=new TerrainVertex(position,normals[i],uv[i],tiles[i]);bounds.Add(position);}
            return new ChunkBuild{FluidMesh=FluidMesher.Build(cells),Position=pos,Revision=revision,Cells=cells,Vertices=packed,Bounds=bounds.Value,Triangles=indices.ToArray()};
        }
    }
}
