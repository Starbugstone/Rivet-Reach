using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    // Copy Blender import data on the main thread; workers consume ordinary immutable arrays.
    public static class FarmingMeshes
    {
        sealed class Shape { public Vector3[] positions,normals;public Vector2[] uv;public int[] indices; }
        static readonly Dictionary<byte,Shape> shapes=new Dictionary<byte,Shape>();
        public static void Initialize()
        {
            if(shapes.Count!=0)return;
            foreach(var crop in CropRules.Definitions)
            {
                if(crop.first<200)continue;
                for(byte id=crop.first;id<=crop.Mature;id++)
                {
                    var prefab=Resources.Load<GameObject>("Farming/"+id);if(prefab==null)throw new System.InvalidOperationException("Missing crop model "+id);
                    var filter=prefab.GetComponentInChildren<MeshFilter>();var mesh=filter.sharedMesh;
                    var positions=mesh.vertices;var normals=mesh.normals;
                    for(int i=0;i<positions.Length;i++){positions[i]=filter.transform.TransformPoint(positions[i]);normals[i]=filter.transform.TransformDirection(normals[i]).normalized;}
                    shapes.Add(id,new Shape{positions=positions,normals=normals,uv=mesh.uv,indices=mesh.triangles});
                }
            }
        }
        public static bool Append(byte id,Vector3 origin,List<Vector3> vertices,List<Vector3> normals,List<Vector2> uv,List<Vector2> tiles,List<int> indices)
        {
            if(!shapes.TryGetValue(id,out var shape))return false;int start=vertices.Count;
            for(int i=0;i<shape.positions.Length;i++)
            {
                vertices.Add(origin+shape.positions[i]);normals.Add(shape.normals[i]);uv.Add(Vector2.zero);
                int palette=Mathf.Clamp((int)(shape.uv[i].x*4),0,3)+4*Mathf.Clamp((int)(shape.uv[i].y*4),0,3);
                tiles.Add(new Vector2(48+palette,0));
            }
            foreach(int index in shape.indices)indices.Add(start+index);return true;
        }
    }
}
