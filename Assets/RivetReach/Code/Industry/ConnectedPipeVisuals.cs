using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    // One selected Blender mesh per channel, shared by every instance with the same mask.
    // All faces are in world orientation, so rotated neighbors have identical boundary anchors.
    public static class ConnectedPipeVisuals
    {
        static readonly Dictionary<string,Mesh[]> families=new Dictionary<string,Mesh[]>();
        public static bool UsesConnectedMesh(byte id)=>PipeConnections.IsTransport(id)||id==IndustryId.PowerCable||id==IndustryId.SignalConduit;
        public static Mesh Shape(string key,int mask)
        {
            if(!families.TryGetValue(key,out var shapes))
            {
                shapes=new Mesh[64];var prefab=Resources.Load<GameObject>("Industry/Runtime/"+key);
                if(prefab==null)throw new InvalidOperationException("Missing connected pipe family "+key);
                foreach(var f in prefab.GetComponentsInChildren<MeshFilter>())
                    if(f.name.StartsWith("Mask")&&int.TryParse(f.name.Substring(4),out int i)&&i>=0&&i<64)shapes[i]=f.sharedMesh;
                for(int i=0;i<64;i++)if(shapes[i]==null)throw new InvalidOperationException("Missing "+key+" connection mask "+i);
                families.Add(key,shapes);
            }
            return shapes[mask&63];
        }
        public static GameObject Create(string key,Transform parent)
        {
            var root=new GameObject(key);root.transform.SetParent(parent,false);
            var body=new GameObject("Connected surface");body.transform.SetParent(root.transform,false);
            body.AddComponent<MeshFilter>().sharedMesh=Shape(key,0);
            body.AddComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("Industry/Workshop");
            return root;
        }
        public static void Set(GameObject root,string key,int mask,int isolatedRotation=0,int disconnectedFaces=0)
        {
            if(root==null)return;var body=root.transform.GetChild(0);var filter=body.GetComponent<MeshFilter>();
            var shape=Shape(key,mask);if(filter.sharedMesh!=shape)filter.sharedMesh=shape;
            body.localRotation=mask==0?Quaternion.Euler(0,isolatedRotation*90,0):Quaternion.identity;
            var scale=Vector3.one;var center=Vector3.one*.5f;
            if(disconnectedFaces!=0)
            {
                // The authored zero/one-end variants span a full straight cell.
                // Retract the closed half while retaining the live boundary anchor.
                if(mask==0)scale=Vector3.one*.4f;
                else if((mask&(mask-1))==0)
                    for(int face=0;face<6;face++)if((mask&(1<<face))!=0&&(disconnectedFaces&(1<<(face^1)))!=0)
                    {scale[face/2]=.65f;var d=IndustryDefinition.Directions[face];center+=new Vector3(d.x,d.y,d.z)*.175f;}
            }
            body.localScale=scale;
            body.localPosition=center-body.localRotation*Vector3.Scale(Vector3.one*.5f,scale);
        }
    }
}
