using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    public sealed partial class AvatarView
    {
        public EquipmentState Equipment;
        readonly List<SkinnedMeshRenderer> armorRenderers=new List<SkinnedMeshRenderer>();
        readonly Dictionary<SkinnedMeshRenderer,Mesh> hairMeshes=new Dictionary<SkinnedMeshRenderer,Mesh>();
        readonly Dictionary<SkinnedMeshRenderer,Mesh> helmetMeshes=new Dictionary<SkinnedMeshRenderer,Mesh>();
        readonly byte[] equipped=new byte[4];
        GameObject armorRoot;
        Material heldArmorMaterial;
        long equipmentRevision=long.MinValue;
        public int ArmorTriangles=>armorRenderers.Where(r=>r.enabled&&r.shadowCastingMode!=ShadowCastingMode.ShadowsOnly).Sum(r=>r.sharedMesh.triangles.Length/3);
        public int ArmorShadowTriangles=>armorRenderers.Where(r=>r.enabled&&r.shadowCastingMode!=ShadowCastingMode.Off).Sum(r=>r.sharedMesh.triangles.Length/3);
        public int EquippedVisualCount=>equipped.Count(id=>id!=0);
        public byte EquippedVisual(int slot)=>equipped[slot];
        void BuildArmor(bool female)
        {
            equipmentRevision=long.MinValue;
            var asset=Resources.Load<GameObject>("Equipment/"+(female?"Female":"Male")+"Armor");
            if(asset==null)return;
            // Imported armor uses the exact source rig and bind transforms. Rebind each
            // renderer to the live explorer bones; no second Animator or simulation state.
            armorRoot=Instantiate(asset,transform,false);armorRoot.name="Fitted armor";
            foreach(var animator in armorRoot.GetComponentsInChildren<Animator>()){animator.enabled=false;Destroy(animator);}
            foreach(var r in armorRoot.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                r.bones=r.bones.Select(b=>bones[b.name]).ToArray();r.rootBone=bones[r.rootBone.name];
                r.localBounds=new Bounds(Vector3.up*.9f,new Vector3(3,4,3));
                r.gameObject.layer=gameObject.layer;r.enabled=false;armorRenderers.Add(r);
                if(FirstPersonArms)
                {
                    r.sharedMesh=ExtractMesh(r.sharedMesh,r.bones,b=>IsArm(b)&&b.EndsWith("R"));
                    r.shadowCastingMode=ShadowCastingMode.Off;
                    if(r.name.StartsWith("Chest"))
                    {
                        var source=asset.GetComponentsInChildren<SkinnedMeshRenderer>().Single(a=>a.name.StartsWith("Chest"));
                        var support=Instantiate(r.gameObject,r.transform.parent,false).GetComponent<SkinnedMeshRenderer>();support.name="ChestSupport";
                        support.sharedMesh=ExtractMesh(source.sharedMesh,r.bones,b=>IsArm(b)&&b.EndsWith("L"));armorRenderers.Add(support);
                    }
                }
                else if(HideHeadAndArms)
                {
                    var shadow=Instantiate(r.gameObject,r.transform.parent,false).GetComponent<SkinnedMeshRenderer>();shadow.name=r.name+"Shadow";
                    shadow.shadowCastingMode=ShadowCastingMode.ShadowsOnly;armorRenderers.Add(shadow);
                    r.sharedMesh=ExtractMesh(r.sharedMesh,r.bones,b=>!IsArm(b)&&b!="Head");r.shadowCastingMode=ShadowCastingMode.Off;
                }
            }
            // Keep only mesh holders after rebinding; discard the imported duplicate rig.
            foreach(var r in armorRenderers)r.transform.SetParent(armorRoot.transform,true);
            foreach(Transform child in armorRoot.transform)
                if(!armorRenderers.Any(r=>r.transform==child))Destroy(child.gameObject);
            if(FirstPersonArms){heldArmorMaterial=new Material(Shader.Find("RivetReach/HeldTool"));heldArmorMaterial.SetFloat("_FirstPerson",1);}
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var source=r.sharedMesh;var uv=source.uv;var indices=source.triangles;var positions=source.vertices;
                bool AboveBrow(int index)=>transform.InverseTransformPoint(r.transform.TransformPoint(positions[index])).y>1.678f;
                bool Hair(int index)=>Mathf.FloorToInt(uv[index].x*4)+Mathf.FloorToInt(uv[index].y*4)*4==9;
                var keep=new List<int>();
                for(int i=0;i<indices.Length;i+=3)if(!Hair(indices[i])||!Hair(indices[i+1])||!Hair(indices[i+2])||!(AboveBrow(indices[i])||AboveBrow(indices[i+1])||AboveBrow(indices[i+2]))){keep.Add(indices[i]);keep.Add(indices[i+1]);keep.Add(indices[i+2]);}
                if(keep.Count==indices.Length)continue;
                var covered=Instantiate(source);covered.name=source.name+" under helmet";covered.triangles=keep.ToArray();derivedMeshes.Add(covered);hairMeshes[r]=source;helmetMeshes[r]=covered;
            }
            RefreshEquipment();
        }
        void LateUpdate()=>RefreshEquipment();
        public void RefreshEquipment()
        {
            if(model==null||Equipment==null||armorRoot==null)return;
            bool changed=equipmentRevision!=Equipment.Revision;
            if(changed)
            {
                equipmentRevision=Equipment.Revision;
                for(int i=0;i<4;i++)equipped[i]=Equipment.Slots[i].Empty?(byte)0:Equipment.Slots[i].Id;
                foreach(var r in hairMeshes.Keys)r.sharedMesh=equipped[0]!=0?helmetMeshes[r]:hairMeshes[r];
            }
            foreach(var r in armorRenderers)
            {
                int slot=r.name.StartsWith("Head")?0:r.name.StartsWith("Chest")?1:r.name.StartsWith("Legs")?2:3;
                var id=equipped[slot];r.enabled=id!=0&&r.sharedMesh.vertexCount>0&&(!r.name.Contains("Support")||SupportHandVisible);
                if(id==0||!changed||r.sharedMesh.vertexCount==0)continue;
                if(FirstPersonArms){heldArmorMaterial.SetTexture("_BaseMap",EquipmentVisuals.Palette(id));r.sharedMaterial=heldArmorMaterial;}
                else r.sharedMaterial=EquipmentVisuals.Material(id);
            }
        }
        void ReleaseArmor()
        {
            if(armorRoot!=null){armorRoot.SetActive(false);Destroy(armorRoot);}armorRenderers.Clear();hairMeshes.Clear();helmetMeshes.Clear();
            if(heldArmorMaterial!=null)Destroy(heldArmorMaterial);heldArmorMaterial=null;
            for(int i=0;i<4;i++)equipped[i]=0;
        }
    }
}
