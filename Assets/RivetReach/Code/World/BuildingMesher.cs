using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    public sealed class GlassMeshData
    {
        public static readonly GlassMeshData Empty=new GlassMeshData(Array.Empty<TerrainVertex>(),Array.Empty<int>(),new Bounds());
        public readonly TerrainVertex[] Vertices;
        public readonly int[] Indices;
        public readonly Bounds Bounds;
        public GlassMeshData(TerrainVertex[] vertices,int[] indices,Bounds bounds){Vertices=vertices;Indices=indices;Bounds=bounds;}
        public Mesh ToMesh(Mesh mesh=null)=>ChunkMeshUpload.Terrain(Vertices,Indices,Bounds,mesh);
    }
    public static class BuildingMesher
    {
        static readonly int[] stride={1,34,1156};
        public static void Slab(byte id,Vector3 position,byte[] cells,List<Vector3> vertices,List<Vector3> normals,List<Vector2> uv,List<Vector2> tiles,List<int> indices)
        {
            var bounds=BuildingBlocks.Bounds(id);int address=ChunkMesher.Index((int)position.x,(int)position.y,(int)position.z);
            for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
            {
                byte neighbor=cells[address+sign*stride[axis]];
                bool atBoundary=axis!=1||sign<0&&!BuildingBlocks.Upper(id)||sign>0&&BuildingBlocks.Upper(id);
                if(atBoundary&&(ChunkMesher.FullTerrainCube(neighbor)||BuildingBlocks.Half(neighbor)&&
                    (axis!=1?BuildingBlocks.Upper(id)==BuildingBlocks.Upper(neighbor):sign>0?!BuildingBlocks.Upper(neighbor):BuildingBlocks.Upper(neighbor))))continue;
                int u=(axis+1)%3,v=(axis+2)%3;var p=position+bounds.min;var du=Vector3.zero;var dv=Vector3.zero;var normal=Vector3.zero;
                if(sign>0)p[axis]+=bounds.size[axis];du[u]=bounds.size[u];dv[v]=bounds.size[v];normal[axis]=sign;
                int start=vertices.Count;vertices.Add(p);vertices.Add(p+du);vertices.Add(p+du+dv);vertices.Add(p+dv);
                uv.Add(new Vector2(bounds.min[u],bounds.min[v]));uv.Add(new Vector2(bounds.max[u],bounds.min[v]));uv.Add(new Vector2(bounds.max[u],bounds.max[v]));uv.Add(new Vector2(bounds.min[u],bounds.max[v]));
                for(int n=0;n<4;n++){normals.Add(normal);tiles.Add(new Vector2(BlockId.Tile(id,axis,sign),0));}
                FaceIndices(indices,start,sign);
            }
        }
        public static int Connections(byte[] cells,int address,int axis,int sign)
        {
            int u=stride[(axis+1)%3],v=stride[(axis+2)%3],normal=sign*stride[axis];
            bool Joins(int offset)=>cells[address+offset]==IndustryId.Glass&&cells[address+offset+normal]!=IndustryId.Glass&&!ChunkMesher.FullTerrainCube(cells[address+offset+normal]);
            return (Joins(-u)?1:0)|(Joins(u)?2:0)|(Joins(-v)?4:0)|(Joins(v)?8:0)|
                (Joins(-u-v)?16:0)|(Joins(u-v)?32:0)|(Joins(-u+v)?64:0)|(Joins(u+v)?128:0);
        }
        public static GlassMeshData Glass(byte[] cells,List<(Vector3 position,byte id)> special)
        {
            List<TerrainVertex> vertices=null;List<int> indices=null;var bounds=new MeshBounds();
            foreach(var cell in special)
            {
                if(cell.id!=IndustryId.Glass)continue;
                vertices??=new List<TerrainVertex>();indices??=new List<int>();
                var pos=cell.position;int address=ChunkMesher.Index((int)pos.x,(int)pos.y,(int)pos.z);
                for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
                {
                    byte neighbor=cells[address+sign*stride[axis]];
                    if(neighbor==IndustryId.Glass||ChunkMesher.FullTerrainCube(neighbor))continue;
                    int u=(axis+1)%3,v=(axis+2)%3;var p=pos;var du=Vector3.zero;var dv=Vector3.zero;var normal=Vector3.zero;
                    if(sign>0)p[axis]++;du[u]=1;dv[v]=1;normal[axis]=sign;
                    var mask=new Vector2(Connections(cells,address,axis,sign),axis);
                    int start=vertices.Count;
                    void Vertex(Vector3 point,Vector2 tex){vertices.Add(new TerrainVertex(point,normal,tex,mask));bounds.Add(point);}
                    Vertex(p,Vector2.zero);Vertex(p+du,Vector2.right);Vertex(p+du+dv,Vector2.one);Vertex(p+dv,Vector2.up);
                    FaceIndices(indices,start,sign);
                }
            }
            return vertices==null?GlassMeshData.Empty:new GlassMeshData(vertices.ToArray(),indices.ToArray(),bounds.Value);
        }
        static void FaceIndices(List<int> indices,int start,int sign)
        {
            if(sign>0){indices.Add(start);indices.Add(start+1);indices.Add(start+2);indices.Add(start);indices.Add(start+2);indices.Add(start+3);}
            else {indices.Add(start);indices.Add(start+2);indices.Add(start+1);indices.Add(start);indices.Add(start+3);indices.Add(start+2);}
        }
    }
}
