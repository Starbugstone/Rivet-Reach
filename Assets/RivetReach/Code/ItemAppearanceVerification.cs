using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RivetReach
{
    // Opt-in native-player audit. No fixture or resource changes in ordinary play.
    public sealed class ItemAppearanceVerification : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {if(Environment.GetCommandLineArgs().Contains("-rr-item-appearance"))new GameObject("Item appearance verification").AddComponent<ItemAppearanceVerification>();}
        [Serializable] sealed class Entry {public int id,renderers,triangles;public string name,kind,icon;}
        [Serializable] sealed class Report {public string result,unity,timestamp;public int assertions,width,height;public Entry[] items;public string[] errors;}
        readonly List<string> errors=new List<string>();readonly List<Entry> entries=new List<Entry>();int checks;
        string output;
        void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text+"\n"+stack);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rr-output");
            output=index>=0?args[index+1]:Path.Combine(Application.persistentDataPath,"ItemAppearance");Directory.CreateDirectory(output);
            Application.logMessageReceived+=Log;
            Application.runInBackground=true;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach(var device in InputSystem.devices)if(device is Keyboard||device is Mouse)InputSystem.DisableDevice(device);
            InputSystem.AddDevice<Keyboard>("Appearance audit keyboard");InputSystem.AddDevice<Mouse>("Appearance audit mouse");
            var routines=new Stack<IEnumerator>();routines.Push(Run());
            while(routines.Count>0)
            {
                bool more;object current=null;
                try{more=routines.Peek().MoveNext();if(more)current=routines.Peek().Current;}
                catch(Exception error){errors.Add(error.ToString());break;}
                if(!more){routines.Pop();continue;}if(current is IEnumerator nested){routines.Push(nested);continue;}yield return current;
            }
            File.WriteAllText(Path.Combine(output,"report.json"),JsonUtility.ToJson(new Report{result=errors.Count==0?"PASS":"FAIL",unity=Application.unityVersion,timestamp=DateTime.UtcNow.ToString("O"),width=Screen.width,height=Screen.height,assertions=checks,items=entries.ToArray(),errors=errors.ToArray()},true));
            Application.Quit(errors.Count==0?0:1);
        }
        IEnumerator Run()
        {
            yield return null;var game=Expedition.Instance;Check(game!=null,"Missing expedition");
            game.StartSession(246813);game.World.ViewDistance=4;game.SetCreative(true);game.Mobs.enabled=false;game.Diagnostics=false;
            float timeout=Time.realtimeSinceStartup+90;
            while(!game.ReadyToPlay&&Time.realtimeSinceStartup<timeout)yield return null;
            Check(game.ReadyToPlay,"World did not become ready");
            game.Sky.Clock.SetTime(.4);game.Sky.Apply();game.Player.Yaw=0;game.Player.Pitch=0;
            foreach(var item in game.Registry.items.OrderBy(i=>i.runtimeId))
            {
                byte id=item.runtimeId;game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(id,1,0,1);game.Selected=0;
                yield return new WaitForSecondsRealtime(.5f);
                var held=game.Player.HeldBlock;Check(held.Visible&&held.ItemId==id,"Held selection failed: "+id);
                var root=held.Socket.GetComponentsInChildren<Transform>().Single(t=>t.name=="Selected item in hand");
                var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                var meshes=renderers.Select(r=>r.GetComponent<MeshFilter>().sharedMesh).ToArray();
                Check(meshes.Length>0&&meshes.All(m=>m!=null&&m.vertexCount>0),"Empty held geometry: "+id);
                var icon=game.UI.ItemIcon(id);Check(icon!=null,"Missing UI icon: "+id);
                string kind="shared card or terrain";
                if(ItemAppearance.TryIndustryKey(id,out string key))
                {
                    kind="industrial model";var prefab=Resources.Load<GameObject>("Industry/Runtime/"+key);
                    Check(prefab!=null,"Missing industrial prefab: "+key);
                    var expected=ConnectedPipeVisuals.UsesConnectedMesh(id)?new[]{ConnectedPipeVisuals.Shape(key,0)}:prefab.GetComponentsInChildren<MeshFilter>().Where(f=>ItemAppearance.VisibleIndustryPart(id,f.name)).Select(f=>f.sharedMesh).ToArray();
                    Check(expected.Length==meshes.Length&&expected.All(meshes.Contains),"Held geometry differs from icon's industrial asset: "+id);
                    Check(icon==Resources.Load<Texture2D>("Industry/Icons/"+id),"Industrial UI icon differs: "+id);
                    if(id==IndustryId.TankFrame)Check(renderers.All(r=>!r.name.StartsWith("Skin"))&&renderers.Any(r=>r.name.StartsWith("Trim")),"Tank frame is open rails, not solid wall panels");
                    if(id==IndustryId.SignalWire)Check(renderers.Count(r=>r.name.StartsWith("Arm"))==2&&renderers.Any(r=>r.name=="Arm0")&&renderers.Any(r=>r.name=="Arm1"),"Signal wire uses the same two connection arms as its icon");
                }
                else if(ItemAppearance.BakedIcon(item))
                {
                    kind="baked held model";
                    var path=Fluids.IsBucket(id)?"Characters/PalmBucket":id==BlockId.Torch?"Characters/GripTorch":ItemAppearance.ToolPath(item.toolCapabilities);
                    var expected=Resources.Load<GameObject>(path).GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh).ToArray();
                    Check(expected.All(meshes.Contains),"Held/baked source geometry differs: "+id);
                    Check(icon==Resources.Load<Texture2D>("ItemIcons/"+id),"Baked UI icon not used: "+id);
                    if(item.toolCapabilities!=ToolCapability.None)Check(renderers.All(r=>r.sharedMaterial.GetColor("_BaseColor")==ItemAppearance.ToolTint(item)),"Tool tint differs: "+id);
                }
                var card=renderers.SingleOrDefault(r=>r.GetComponent<MeshFilter>().sharedMesh.name=="Quad");
                if(card!=null)Check(card.sharedMaterial.GetTexture("_BaseMap")==icon,"Held card does not share UI icon: "+id);
                entries.Add(new Entry{id=id,name=item.displayName,kind=kind,icon=icon.name,renderers=renderers.Length,triangles=meshes.Sum(m=>Enumerable.Range(0,m.subMeshCount).Sum(i=>(int)m.GetIndexCount(i))/3)});
                game.Player.enabled=false;held.enabled=false;
                yield return new WaitForEndOfFrame();var shot=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(output,"held-"+id+".png"),shot.EncodeToPNG());
                if(id==IndustryId.CopperWire)
                {
                    foreach(var r in renderers)r.forceRenderingOff=true;yield return new WaitForEndOfFrame();var hidden=ScreenCapture.CaptureScreenshotAsTexture();
                    foreach(var r in renderers)r.forceRenderingOff=false;
                    var a=shot.GetPixels32();var b=hidden.GetPixels32();int pixels=0;
                    for(int y=Screen.height/5;y<Screen.height*4/5;y++)for(int x=Screen.width/3;x<Screen.width;x++)
                    {int i=y*Screen.width+x;if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>90)pixels++;}
                    Check(pixels>35,"Copper spool contributes no visible pixels");Destroy(hidden);
                }
                Destroy(shot);game.Player.enabled=true;held.enabled=true;
            }
            // Verify support for the other appearance as well as re-selection after UI transitions.
            bool female=game.Player.Female;game.Player.Female=!female;game.Player.RefreshAppearance();
            game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(IndustryId.CopperWire,1,0,1);
            game.SetMode(ScreenMode.Inventory);yield return null;game.SetMode(ScreenMode.Play);yield return new WaitForSecondsRealtime(.8f);
            Check(game.Player.HeldBlock.Visible&&game.Player.HeldBlock.ItemId==IndustryId.CopperWire,"Copper wire after appearance/UI change");
            yield return new WaitForEndOfFrame();var other=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,"copper-other-appearance.png"),other.EncodeToPNG());Destroy(other);
            game.Player.Female=female;game.Player.RefreshAppearance();
        }
    }
}
