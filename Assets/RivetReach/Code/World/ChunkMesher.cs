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
        public Vector3[] Vertices,Normals;
        public Vector2[] UV, Tiles;
        public int[] Triangles;
        public FluidMeshData FluidMesh;
        public double Milliseconds;
    }

    public static class ChunkMesher
    {
        // Registry and block identities are immutable for the process lifetime.
        static readonly bool[] terrainSolid=BuildSolidTable();
        static bool[] BuildSolidTable()
        {
            var table=new bool[256];for(int i=0;i<table.Length;i++)table[i]=BlockId.Solid((byte)i)&&!IndustryId.Placed((byte)i)&&i!=IndustryId.DoorUpper&&!StarterStationVisuals.UsesModel((byte)i);return table;
        }
        public static int Index(int x,int y,int z) => x+1+34*(y+1+34*(z+1));
        public static ChunkBuild Build(ChunkPos pos,int revision,byte[] cells)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();
            var tiles=new List<Vector2>();var indices=new List<int>();var mask=new byte[1024];
            var plants=new List<(Vector3 position,byte id)>();
            int[] stride={1,34,1156};
            for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
            {
                int u=(axis+1)%3,v=(axis+2)%3;
                for(int layer=0;layer<32;layer++)
                {
                    for(int j=0;j<32;j++)for(int i=0;i<32;i++)
                    {
                        int address=Index(0,0,0)+layer*stride[axis]+i*stride[u]+j*stride[v];
                        byte a=cells[address],b=cells[address+sign*stride[axis]];
                        if(axis==0&&sign==-1&&BlockId.Crop(a))plants.Add((new Vector3(layer,i,j),a));
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
                float h=.22f+(plant.id-BlockId.PotatoPlant)*.16f;var centre=plant.position+new Vector3(.5f,0,.5f);
                void Leaf(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
                {
                    Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
                    for(int side=0;side<2;side++)
                    {
                        int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
                        uv.Add(Vector2.zero);uv.Add(Vector2.right);uv.Add(Vector2.one);uv.Add(Vector2.up);
                        for(int k=0;k<4;k++){normals.Add(side==0?normal:-normal);tiles.Add(new Vector2(BlockId.Tile(plant.id,1,1),0));}
                        if(side==0)indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
                        else indices.AddRange(new[]{start,start+2,start+1,start,start+3,start+2});
                    }
                }
                Leaf(centre+Vector3.left*.025f,centre+Vector3.right*.025f,centre+new Vector3(.025f,h,0),centre+new Vector3(-.025f,h,0));
                for(int leaf=0;leaf<6;leaf++)
                {
                    float angle=leaf*Mathf.PI/3;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var across=new Vector3(-direction.z,0,direction.x);
                    var start=centre+Vector3.up*h*(.3f+leaf%2*.28f);var end=start+direction*h*.65f+Vector3.up*.06f;
                    Leaf(start,Vector3.Lerp(start,end,.5f)-across*h*.19f,end,Vector3.Lerp(start,end,.5f)+across*h*.19f);
                }
            }
            return new ChunkBuild{FluidMesh=FluidMesher.Build(cells),Position=pos,Revision=revision,Cells=cells,Vertices=vertices.ToArray(),Normals=normals.ToArray(),UV=uv.ToArray(),Tiles=tiles.ToArray(),Triangles=indices.ToArray()};
        }
    }
}
