using System;
using System.Collections.Generic;

namespace RivetReach
{
    // Pure worker-side solve. High nibble is sky access, low nibble is placed light.
    // Every nonvertical sky/placed-light step loses one level; opaque voxels stop both.
    public static class ChunkLighting
    {
        public const int Count=32*32*32;
        public static readonly (int x,int y,int z)[] Faces={(1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1)};
        public static byte[] Solve(byte[] cells,int minY,int[] heights,byte[][] borders,byte[] emissions)
        {
            var result=new byte[Count];var queue=new Queue<int>();
            for(int z=0;z<32;z++)for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                int i=x+32*(y+32*z);if(BlockId.Opaque(cells[ChunkMesher.Index(x,y,z)]))continue;
                byte value=(byte)((minY+y>heights[x+32*z]?240:0)|emissions[i]);
                if(x==31)value=Merge(value,Fade(borders[0][y+32*z]));
                if(x==0)value=Merge(value,Fade(borders[1][y+32*z]));
                if(y==31)value=Merge(value,Fade(borders[2][x+32*z]));
                if(y==0)value=Merge(value,Fade(borders[3][x+32*z]));
                if(z==31)value=Merge(value,Fade(borders[4][x+32*y]));
                if(z==0)value=Merge(value,Fade(borders[5][x+32*y]));
                result[i]=value;if(value!=0)queue.Enqueue(i);
            }
            while(queue.Count>0)
            {
                int i=queue.Dequeue(),x=i%32,y=i/32%32,z=i/1024;byte value=Fade(result[i]);if(value==0)continue;
                foreach(var d in Faces)
                {
                    int nx=x+d.x,ny=y+d.y,nz=z+d.z;if((uint)nx>=32||(uint)ny>=32||(uint)nz>=32)continue;
                    int n=nx+32*(ny+32*nz);byte next=Merge(result[n],value);
                    if(next==result[n]||BlockId.Opaque(cells[ChunkMesher.Index(nx,ny,nz)]))continue;
                    result[n]=next;queue.Enqueue(n);
                }
            }
            return result;
        }
        public static byte Fade(byte value)=>(byte)((Math.Max(0,(value>>4)-1)<<4)|Math.Max(0,(value&15)-1));
        public static byte Merge(byte a,byte b)=>(byte)((Math.Max(a>>4,b>>4)<<4)|Math.Max(a&15,b&15));
    }
}
