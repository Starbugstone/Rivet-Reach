using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainVertex
    {
        public Vector3 Position,Normal;
        public Vector2 UV,Tile;
        public TerrainVertex(Vector3 position,Vector3 normal,Vector2 uv,Vector2 tile)
        {Position=position;Normal=normal;UV=uv;Tile=tile;}
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FluidVertex
    {
        public Vector3 Position,Normal;
        public Color Colour;
        public FluidVertex(Vector3 position,Vector3 normal,Color colour)
        {Position=position;Normal=normal;Colour=colour;}
    }

    // Managed worker results own their packed arrays. Native Mesh mutation stays on
    // the main thread, after VoxelWorld has rejected stale tokens and revisions.
    public static class ChunkMeshUpload
    {
        static readonly VertexAttributeDescriptor[] terrainLayout={
            new VertexAttributeDescriptor(VertexAttribute.Position,VertexAttributeFormat.Float32,3),
            new VertexAttributeDescriptor(VertexAttribute.Normal,VertexAttributeFormat.Float32,3),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float32,2),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord1,VertexAttributeFormat.Float32,2)};
        static readonly VertexAttributeDescriptor[] fluidLayout={
            new VertexAttributeDescriptor(VertexAttribute.Position,VertexAttributeFormat.Float32,3),
            new VertexAttributeDescriptor(VertexAttribute.Normal,VertexAttributeFormat.Float32,3),
            new VertexAttributeDescriptor(VertexAttribute.Color,VertexAttributeFormat.Float32,4)};
        public static Mesh Terrain(TerrainVertex[] vertices,int[] indices,Bounds bounds,Mesh mesh=null)
            =>Upload(vertices,indices,bounds,terrainLayout,"Voxel chunk",mesh);
        public static Mesh Fluid(FluidVertex[] vertices,int[] indices,Bounds bounds,Mesh mesh=null)
            =>Upload(vertices,indices,bounds,fluidLayout,"Fluid surfaces",mesh);
        static Mesh Upload<T>(T[] vertices,int[] indices,Bounds bounds,VertexAttributeDescriptor[] layout,string name,Mesh mesh) where T:struct
        {
            if(mesh==null)mesh=new Mesh{name=name};
            // Declare and replace both complete buffers. Preserve normal validation;
            // only the already-computed bounds pass is skipped.
            const MeshUpdateFlags flags=MeshUpdateFlags.DontRecalculateBounds;
            mesh.SetVertexBufferParams(vertices.Length,layout);
            if(vertices.Length>0)mesh.SetVertexBufferData(vertices,0,0,vertices.Length,0,flags);
            mesh.SetIndexBufferParams(indices.Length,IndexFormat.UInt32);
            if(indices.Length>0)mesh.SetIndexBufferData(indices,0,0,indices.Length,flags);
            mesh.subMeshCount=1;
            mesh.SetSubMesh(0,new SubMeshDescriptor(0,indices.Length,MeshTopology.Triangles)
                {bounds=bounds,firstVertex=0,vertexCount=vertices.Length},flags);
            mesh.bounds=bounds;
            return mesh;
        }
    }

    public struct MeshBounds
    {
        Vector3 min,max;
        bool populated;
        public void Add(Vector3 position)
        {
            if(!populated){min=max=position;populated=true;}
            else {min=Vector3.Min(min,position);max=Vector3.Max(max,position);}
        }
        public Bounds Value=>populated?new Bounds((min+max)*.5f,max-min):new Bounds();
    }
}
