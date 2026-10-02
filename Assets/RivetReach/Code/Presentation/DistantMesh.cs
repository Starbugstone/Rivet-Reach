using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    // Derived from our own imported meshes. Fixed position cells keep separate
    // UV/normal seams at identical positions. No source asset is modified.
    public static class DistantMesh
    {
        public const float Cell=.08f;
        public static Mesh Simplify(Mesh source)
        {
            var positions=source.vertices;var normals=source.normals;var uv=source.uv;
            var vertices=new List<Vector3>();var ns=new List<Vector3>();var uvs=new List<Vector2>();
            var map=new Dictionary<(int,int,int,int,int,int,int,int),int>();var remap=new int[positions.Length];
            for(int i=0;i<positions.Length;i++)
            {
                var p=positions[i];var n=normals.Length==positions.Length?normals[i]:Vector3.up;var t=uv.Length==positions.Length?uv[i]:Vector2.zero;
                int x=Mathf.RoundToInt(p.x/Cell),y=Mathf.RoundToInt(p.y/Cell),z=Mathf.RoundToInt(p.z/Cell);
                var key=(x,y,z,Mathf.RoundToInt(t.x*128),Mathf.RoundToInt(t.y*128),Mathf.RoundToInt(n.x*2),Mathf.RoundToInt(n.y*2),Mathf.RoundToInt(n.z*2));
                if(!map.TryGetValue(key,out int index)){index=vertices.Count;map.Add(key,index);vertices.Add(new Vector3(x,y,z)*Cell);ns.Add(n);uvs.Add(t);}
                remap[i]=index;
            }
            var output=new Mesh{name=source.name+" distant",indexFormat=IndexFormat.UInt32};output.SetVertices(vertices);output.SetNormals(ns);output.SetUVs(0,uvs);output.subMeshCount=source.subMeshCount;
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                var input=source.GetTriangles(sub);var triangles=new List<int>(input.Length);var seen=new HashSet<(int,int,int)>();
                for(int i=0;i<input.Length;i+=3)
                {
                    int a=remap[input[i]],b=remap[input[i+1]],c=remap[input[i+2]];
                    if(a==b||b==c||a==c||Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).sqrMagnitude<1e-12f)continue;
                    // Cyclic order preserves winding and deliberately keeps back faces.
                    var key=a<b&&a<c?(a,b,c):b<c?(b,c,a):(c,a,b);if(!seen.Add(key))continue;
                    triangles.Add(a);triangles.Add(b);triangles.Add(c);
                }
                output.SetTriangles(triangles,sub,false);
            }
            long indices=0;for(int s=0;s<output.subMeshCount;s++)indices+=output.GetIndexCount(s);
            if(indices==0)
            {
                // Very thin wire must not vanish when all of its triangles collapse.
                if(Application.isPlaying)UnityEngine.Object.Destroy(output);else UnityEngine.Object.DestroyImmediate(output);
                output=UnityEngine.Object.Instantiate(source);output.name=source.name+" distant thin fallback";
            }
            output.RecalculateBounds();output.RecalculateTangents();return output;
        }
        public static Mesh Combine(GameObject prefab,Func<Transform,bool> include=null,Func<Transform,Matrix4x4> transform=null)
        {
            var parts=new List<CombineInstance>();
            foreach(var f in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if(f.sharedMesh==null||f.name=="StatusLight"||(include!=null&&!include(f.transform)))continue;
                for(int s=0;s<f.sharedMesh.subMeshCount;s++)parts.Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=s,transform=transform!=null?transform(f.transform):prefab.transform.worldToLocalMatrix*f.transform.localToWorldMatrix});
            }
            var source=new Mesh{name=prefab.name+" combined",indexFormat=IndexFormat.UInt32};source.CombineMeshes(parts.ToArray(),true,true);
            var result=Simplify(source);if(Application.isPlaying)UnityEngine.Object.Destroy(source);else UnityEngine.Object.DestroyImmediate(source);return result;
        }
    }
}
