using System.Collections;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewEquipmentArt()
        {
            bool originalFemale=game.Player.Female;int originalSkin=game.Player.Skin;
            game.Diagnostics=false;game.SetCreative(true);var player=game.Player;player.Pitch=8;player.Yaw=0;
            var all=game.Registry.items.Where(i=>EquipmentVisuals.UsesModel(i.runtimeId)).ToArray();
            foreach(var item in all)game.Inventory.Add(item.runtimeId,1);
            foreach(bool female in new[]{false,true})foreach(int skin in new[]{0,1})
            {
                player.Female=female;player.Skin=skin;player.RefreshAppearance();
                foreach(int tier in new[]{70,74,78})
                {
                    for(int slot=0;slot<4;slot++){game.Equipment.Take(slot);var stack=new ItemStack((byte)(tier+slot),1);game.Equipment.Click(slot,ref stack,false);Check(stack.Empty,"Equip slot "+slot);}
                    yield return null;player.Body.RefreshEquipment();player.Arms.RefreshEquipment();
                    Check(player.Body.EquippedVisualCount==4&&player.Arms.EquippedVisualCount==4,"Equipment sync for both views");
                    game.SetMode(ScreenMode.Inventory);game.UI.RefreshPreview();yield return new WaitForSeconds(.2f);
                    var preview=Object.FindObjectsByType<AvatarView>().Single(v=>v.Preview);
                    Check(preview.EquippedVisualCount==4&&preview.ArmorTriangles>8000,"Portrait wears all four authored slots");
                    Check(preview.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled&&r.transform.IsChildOf(preview.transform.Find("Fitted armor"))).All(r=>r.bones.All(b=>!b.IsChildOf(preview.transform.Find("Fitted armor")))),"Armor follows explorer bones rather than duplicate skeleton");
                    if(skin==0)yield return Capture((female?"female-":"male-")+EquipmentVisuals.Tier((byte)tier)+"-inventory");
                    game.SetMode(ScreenMode.Play);yield return null;
                }
            }
            foreach(var item in all)
            {
                int index=Enumerable.Range(0,game.Inventory.Slots.Count).First(i=>game.Inventory.Slots[i].Id==item.runtimeId);game.Selected=index;
                yield return new WaitForSeconds(.35f);
                var held=player.HeldBlock;Check(held.Visible&&held.ItemId==item.runtimeId,"Held equipment visible: "+item.displayName);
                Check(held.Socket.GetComponentsInChildren<MeshFilter>().Any(f=>f.gameObject.activeInHierarchy&&f.sharedMesh!=null&&f.sharedMesh.triangles.Length>300&&f.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap")==EquipmentVisuals.Palette(item.runtimeId)),"Held model and material agree: "+item.displayName);
                if(item.runtimeId==27||item.runtimeId==28||item.runtimeId==29||item.runtimeId>=74&&item.runtimeId<=77)yield return Capture("held-"+item.runtimeId);
                game.Items.Spawn(new ItemStack(item.runtimeId,1),player.transform.position+player.transform.forward*3+Vector3.up,Vector3.zero,120);
            }
            yield return new WaitForSeconds(.5f);
            Check(game.Items.Piles.Where(p=>EquipmentVisuals.UsesModel(p.Stack.Id)).All(p=>p.View!=null&&p.View.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial==EquipmentVisuals.Material(p.Stack.Id))),"Drops use shared authored equipment materials");
            for(int slot=0;slot<4;slot++)game.Equipment.Take(slot);yield return null;
            Check(player.Body.EquippedVisualCount==0&&player.Arms.EquippedVisualCount==0,"Unequipping removes body and arm armor");
            game.SetMode(ScreenMode.Inventory);game.UI.RefreshPreview();yield return null;
            Check(Object.FindObjectsByType<AvatarView>().Single(v=>v.Preview).EquippedVisualCount==0,"Portrait removes unequipped armor");
            yield return Capture("unequipped");game.SetMode(ScreenMode.Play);
            // Mixed tiers must keep the chest palette on first-person bracers.
            foreach(var pair in new[]{(slot:0,id:78),(slot:1,id:75),(slot:2,id:72),(slot:3,id:81)})
            {var stack=new ItemStack((byte)pair.id,1);game.Equipment.Click(pair.slot,ref stack,false);}
            yield return null;player.Arms.RefreshEquipment();
            Check(player.Arms.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Chest")).All(r=>r.sharedMaterial.GetTexture("_BaseMap")==EquipmentVisuals.Palette(75)),"Mixed tiers retain iron bracers with diamond boots");
            player.Female=originalFemale;player.Skin=originalSkin;player.RefreshAppearance();
            yield return ReviewArmorStudio();
        }
        IEnumerator ReviewArmorStudio()
        {
            foreach(var c in Object.FindObjectsByType<Camera>())c.enabled=false;
            foreach(var c in Object.FindObjectsByType<Canvas>())c.enabled=false;
            RenderSettings.fog=false;
            var camera=new GameObject("Armor studio camera").AddComponent<Camera>();camera.cullingMask=1<<29;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.19f,.23f,.25f);camera.nearClipPlane=.05f;camera.farClipPlane=10;camera.fieldOfView=38;
            Vector3 centre=new Vector3(0,-800,.0f);
            var light=new GameObject("Armor studio fill").AddComponent<Light>();light.type=LightType.Point;light.range=8;light.intensity=4;light.cullingMask=1<<29;light.transform.position=centre+new Vector3(1,2,2);
            var avatars=new AvatarView[2];
            for(int i=0;i<2;i++)
            {
                var obj=new GameObject("Armor studio explorer");obj.transform.position=centre+Vector3.right*(i==0?-.42f:.42f);
                avatars[i]=obj.AddComponent<AvatarView>();avatars[i].Equipment=game.Equipment;avatars[i].Build(i==1,i);
                foreach(var t in obj.GetComponentsInChildren<Transform>())t.gameObject.layer=29;
            }
            camera.transform.position=centre+new Vector3(0,1,3.3f);camera.transform.LookAt(centre+Vector3.up*.94f);
            yield return null;
            foreach(var a in avatars)Check(a.GetComponentsInChildren<Animator>().Length==1,"Worn armor has no duplicate Animator");
            foreach(string pose in new[]{"Idle","Walk","Run","CrouchIdle","MineTwoHandTool"})
            {
                foreach(var a in avatars)a.SamplePose(pose,.2f);
                yield return Capture("studio-"+pose);
                foreach(var a in avatars)
                {
                    var rs=a.transform.Find("Fitted armor").GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).ToArray();
                    var bounds=new Bounds();bool first=true;
                    foreach(var r in rs)
                    {
                        var mesh=r.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;
                        var matrices=r.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
                        for(int v=0;v<vertices.Length;v++)
                        {
                            var w=weights[v];var p=vertices[v];
                            var world=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;
                            var point=a.transform.InverseTransformPoint(world);if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
                        }
                    }
                    Check(bounds.size.y<2.1f&&bounds.size.y>.8f&&bounds.size.x<1.8f,"Bound armor remains at explorer scale through "+pose+": "+bounds);
                }
            }
            foreach(var a in avatars){a.SamplePose("Idle",0);a.transform.rotation=Quaternion.Euler(0,180,0);}
            yield return Capture("studio-back");
        }
    }
}
