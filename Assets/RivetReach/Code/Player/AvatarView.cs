using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed class AvatarView : MonoBehaviour
    {
        readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
        readonly Dictionary<string,Quaternion> rest=new Dictionary<string,Quaternion>();
        GameObject model;
        Material material;
        Mesh derivedMesh;
        public bool FirstPersonArms;
        public bool HideHeadAndArms;
        public bool Preview;
        public int TriangleCount {get;private set;}
        public void Build(bool female,int skin)
        {
            if(model!=null)Destroy(model);if(material!=null)Destroy(material);if(derivedMesh!=null)Destroy(derivedMesh);
            bones.Clear();rest.Clear();TriangleCount=0;
            var asset=Resources.Load<GameObject>("Characters/"+(female?"ExplorerFemale":"ExplorerMale"));
            if(asset==null){Debug.LogError("Missing player model");return;}
            model=Instantiate(asset,transform);model.name=female?"Female":"Male";
            model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
            foreach(var t in model.GetComponentsInChildren<Transform>()){bones[t.name]=t;rest[t.name]=t.localRotation;}
            material=new Material(Resources.Load<Material>("Materials/Player"));
            material.SetTexture("_BaseMap",Resources.Load<Texture2D>("Characters/"+(skin==0?"SkinField":"SkinOchre")));
            foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                r.updateWhenOffscreen=true;
                if(FirstPersonArms||HideHeadAndArms)
                {
                    var source=r.sharedMesh;derivedMesh=Instantiate(source);
                    var weights=source.boneWeights;var sourceIndices=source.triangles;var indices=new List<int>();
                    for(int i=0;i<sourceIndices.Length;i+=3)
                    {
                        string bone=r.bones[weights[sourceIndices[i]].boneIndex0].name;
                        bool arm=bone.Contains("Arm")||bone.Contains("Forearm")||bone.Contains("Hand");
                        if(FirstPersonArms?!arm:arm||bone=="Head"||bone=="Chest")continue;
                        indices.Add(sourceIndices[i]);indices.Add(sourceIndices[i+1]);indices.Add(sourceIndices[i+2]);
                    }
                    derivedMesh.triangles=indices.ToArray();r.sharedMesh=derivedMesh;
                }
                TriangleCount+=r.sharedMesh.triangles.Length/3;
            }
        }
        void Rotate(string bone,float angle)
        {if(bones.TryGetValue(bone,out var t))t.localRotation=rest[bone]*Quaternion.Euler(angle,0,0);}
        void Aim(string bone,string child,Vector3 target)
        {
            if(!bones.TryGetValue(bone,out var t)||!bones.TryGetValue(child,out var end))return;
            var direction=end.position-t.position;
            if(direction.sqrMagnitude>.00001f)t.rotation=Quaternion.FromToRotation(direction,target-t.position)*t.rotation;
        }
        public void Animate(float motion,bool mining,float phase)
        {
            if(model==null)return;
            foreach(var kv in rest)if(bones.TryGetValue(kv.Key,out var t))t.localRotation=kv.Value;
            float swing=Mathf.Sin(phase)*motion*25;
            if(FirstPersonArms)
            {
                // Targets are expressed in the camera-attached avatar root's space.
                float strike=mining?Mathf.Pow(Mathf.Max(0,Mathf.Sin(Time.time*12)),2):0;
                for(int i=0;i<2;i++)
                {
                    string side=i==0?"L":"R";float sign=i==0?-1:1;
                    // Imported L is mirrored on screen depending on FBX basis; keep targets on its own side.
                    if(bones.TryGetValue("UpperArm"+side,out var shoulder))sign=Mathf.Sign(transform.InverseTransformPoint(shoulder.position).x);
                    Aim("UpperArm"+side,"Forearm"+side,transform.TransformPoint(new Vector3(sign*.30f,1.24f,.20f)));
                    Aim("Forearm"+side,"Hand"+side,transform.TransformPoint(new Vector3(sign*.25f,1.30f,.44f+(i==1?strike*.12f:0))));
                }
            }
            else
            {
                Rotate("ThighL",swing);Rotate("ThighR",-swing);Rotate("ShinL",Mathf.Max(0,-swing)*.7f);Rotate("ShinR",Mathf.Max(0,swing)*.7f);
                Rotate("UpperArmL",-swing*.7f);Rotate("UpperArmR",swing*.7f+(mining?-35*Mathf.Max(0,Mathf.Sin(Time.time*12)):0));
            }
        }
        void OnDestroy(){if(material!=null)Destroy(material);if(derivedMesh!=null)Destroy(derivedMesh);}
    }
}
