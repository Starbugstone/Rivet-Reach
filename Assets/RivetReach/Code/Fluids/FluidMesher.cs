using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    public sealed class FluidMeshData
    {
        public FluidVertex[] Vertices;
        public Bounds Bounds;
        public int[] Indices,ActiveCells;
        public Mesh ToMesh(Mesh mesh=null)=>ChunkMeshUpload.Fluid(Vertices,Indices,Bounds,mesh);
    }
    public static class FluidMesher
    {
        public static FluidMeshData Build(byte[] cells)
        {
            using var cost=RuntimeCosts.FluidMeshes.Auto();
            var active=new List<int>();
            var vertices=new List<FluidVertex>();var indices=new List<int>();var bounds=new MeshBounds();
            void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Color color)
            {
                int start=vertices.Count;
                vertices.Add(new FluidVertex(a,normal,color));vertices.Add(new FluidVertex(b,normal,color));
                vertices.Add(new FluidVertex(c,normal,color));vertices.Add(new FluidVertex(d,normal,color));
                bounds.Add(a);bounds.Add(b);bounds.Add(c);bounds.Add(d);
                indices.Add(start);indices.Add(start+1);indices.Add(start+2);indices.Add(start);indices.Add(start+2);indices.Add(start+3);
            }
            for(int z=0;z<32;z++)for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                int index=ChunkMesher.Index(x,y,z);byte cell=cells[index];var f=Fluids.Registry.Get(cell);if(f==null)continue;
                bool NeedsFlow(int offset)=>cells[index+offset]==0||Fluids.Registry.Get(cells[index+offset])==f&&!f.IsSource(cells[index+offset]);
                if(!f.IsSource(cell)||NeedsFlow(-34)||NeedsFlow(-1)||NeedsFlow(1)||NeedsFlow(-1156)||NeedsFlow(1156))active.Add(x+32*(y+32*z));
                float h=Fluids.Registry.Get(cells[index+34])==f?1:f.Height(cell);
                var color=new Color(f.Red,f.Green,f.Blue,f==Fluids.Lava?1f:.68f);
                var p=new Vector3(x,y,z);var a=p;var b=p+Vector3.right;var c=p+new Vector3(1,0,1);var d=p+Vector3.forward;
                var up=Vector3.up*h;
                if(Fluids.Registry.Get(cells[index+34])!=f&&!BlockId.Solid(cells[index+34]))Face(a+up,d+up,c+up,b+up,Vector3.up,color);
                if(cells[index-34]==0)Face(a,b,c,d,Vector3.down,color);
                void Side(int offset,Vector3 left,Vector3 right,Vector3 normal)
                {
                    byte n=cells[index+offset];var other=Fluids.Registry.Get(n);
                    if(BlockId.Solid(n))return;
                    float low=other==f?(Fluids.Registry.Get(cells[index+offset+34])==f?1:f.Height(n)):0;
                    if(low>=h)return;
                    Face(left+Vector3.up*low,right+Vector3.up*low,right+up,left+up,normal,color);
                }
                Side(-1156,b,a,Vector3.back);Side(1156,d,c,Vector3.forward);
                Side(-1,a,d,Vector3.left);Side(1,c,b,Vector3.right);
            }
            return new FluidMeshData{ActiveCells=active.ToArray(),Vertices=vertices.ToArray(),Bounds=bounds.Value,Indices=indices.ToArray()};
        }
    }
}
