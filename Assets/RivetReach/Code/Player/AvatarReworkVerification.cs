using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    // Explicit review mode only; exercises the live player, inventory and held renderer.
    public sealed class AvatarReworkVerification : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public string result,unity;
            public string[] checks,errors;
            public int firstPersonPairTriangles,visibleBodyTriangles,bodyShadowTriangles;
            public float frameMedianMs,frameP95Ms,maxSupportContactErrorMetres,maxBodySupportContactErrorMetres,maxBodySupportWristDegrees;
            public string workload="1280x720, view radius 4, both appearances/skins, seven grips, live selection and swing sequence; shared workstation";
        }
        readonly List<string> checks=new List<string>(),errors=new List<string>();
        Expedition game;string output;bool originalFemale;int originalSkin;Report report=new Report();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {if(Environment.GetCommandLineArgs().Contains("-rr-avatar-rework"))new GameObject("Avatar rework review").AddComponent<AvatarReworkVerification>();}
        void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add(message);}
        void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+trace);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rr-output");output=i>=0?args[i+1]:"Logs/AvatarRework/gameplay";Directory.CreateDirectory(output);
            Application.logMessageReceived+=Log;
            var routines=new Stack<IEnumerator>();routines.Push(Run());
            while(routines.Count>0)
            {
                bool more;object current=null;
                try{more=routines.Peek().MoveNext();if(more)current=routines.Peek().Current;}
                catch(Exception ex){errors.Add(ex.ToString());break;}
                if(!more){routines.Pop();continue;}if(current is IEnumerator nested){routines.Push(nested);continue;}yield return current;
            }
            Time.captureDeltaTime=0;InputSystem.QueueStateEvent(Mouse.current,new MouseState());
            if(game!=null){game.Player.VerificationMining=false;game.SetAppearance(originalFemale,originalSkin);}
            report.result=errors.Count==0?"PASS":"FAIL";report.unity=Application.unityVersion;report.checks=checks.ToArray();report.errors=errors.ToArray();
            File.WriteAllText(Path.Combine(output,"avatar-report.json"),JsonUtility.ToJson(report,true));Application.Quit(errors.Count==0?0:1);
        }
        IEnumerator Capture(string label)
        {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,label+".png"));yield return null;}
        IEnumerator Run()
        {
            yield return null;game=FindAnyObjectByType<Expedition>();Check(game!=null,"Live expedition initializes");
            originalFemale=game.Player.Female;originalSkin=game.Player.Skin;
            game.StartSession(246813);game.SetCreative(true);game.World.ViewDistance=4;
            Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
            yield return new WaitForSecondsRealtime(10);
            var player=game.Player;player.Pitch=0;player.Yaw=125;
            var ids=new byte[]{0,BlockId.Stone,BlockId.IronSword,BlockId.IronPickaxe,BlockId.IronAxe,BlockId.IronShovel,BlockId.IronHoe};
            var expected=new[]{GripPose.Empty,GripPose.Block,GripPose.Tool,GripPose.TwoHandTool,GripPose.Axe,GripPose.Shovel,GripPose.Hoe};
            for(int slot=0;slot<ids.Length;slot++)if(ids[slot]!=0)game.Inventory.Add(ids[slot],1,slot,slot+1);
            foreach(bool female in new[]{false,true})foreach(int skin in new[]{0,1})
            {
                game.SetAppearance(female,skin);yield return null;
                for(int slot=0;slot<ids.Length;slot++)
                {
                    game.Selected=slot;
                    // Appearance rebuilding/import warm-up can make a wall-clock delay
                    // contain too few capped simulation steps to finish the equip animation.
                    float settledTime=0;int settleFrames=0;
                    while(settledTime<.35f&&settleFrames++<120)
                    {yield return null;settledTime+=Mathf.Min(Time.deltaTime,.05f);}
                    yield return new WaitForEndOfFrame();
                    Check(player.Arms.Grip==expected[slot]&&player.Body.Grip==expected[slot],"Matching live body/arm pose: "+female+"/"+skin+"/"+expected[slot]);
                    Check(player.HeldBlock.ItemId==ids[slot]&&player.HeldBlock.Visible==(slot>0),"Selected item presentation settles: "+ids[slot]);
                    Check(player.Arms.SupportHandVisible==(slot==3),"Support-hand visibility matches pickaxe");
                    if(slot==3)
                    {
                        report.firstPersonPairTriangles=Mathf.Max(report.firstPersonPairTriangles,player.Arms.TriangleCount);
                        Check(player.Arms.TriangleCount<=68000,"The visible arm pair stays within its triangle review budget");
                    }
                    report.visibleBodyTriangles=Mathf.Max(report.visibleBodyTriangles,player.Body.TriangleCount);
                    report.bodyShadowTriangles=Mathf.Max(report.bodyShadowTriangles,player.Body.ShadowTriangleCount);
                    if(slot>0)Check(Vector3.Distance(player.HeldBlock.Centre,player.HeldBlock.Socket.position)<.0001f,"Held origin stays on the authored attachment");
                    if(slot==6)
                    {
                        var socket=player.HeldBlock.Socket;Vector3 axis=socket.up;float forward=0;int vertices=0;
                        foreach(var mesh in socket.GetComponentsInChildren<MeshFilter>())
                        {
                            var offset=mesh.transform.TransformPoint(mesh.sharedMesh.bounds.center)-socket.position;float height=Vector3.Dot(offset,axis);
                            forward+=Vector3.Dot(offset-axis*height,player.Camera.transform.forward);vertices++;
                        }
                        Check(vertices>0&&forward/vertices>.005f,"Hoe head projects forward from its handle, away from the player");
                    }
                    if(slot>1)
                    {
                        var shaft=player.Camera.transform.InverseTransformDirection(player.Arms.Bone("ToolSocket").up);
                        Check(Mathf.Abs(shaft.y)<.4f&&Mathf.Abs(shaft.x)>.65f,"Idle tool shaft lies horizontally across the view: "+expected[slot]);
                        Check(player.Arms.ToolUseWeight<.03f,"Idle tool has settled into its rest pose");
                    }
                    yield return Capture((female?"female":"male")+"-skin"+skin+"-"+expected[slot]);
                    if(skin==0&&slot>1)
                    {
                        player.VerificationMining=true;float deadline=Time.realtimeSinceStartup+1.2f;float maximumStep=0,maxForward=-1,minUp=1;Vector3 previous=player.Arms.BonePosition("HandR");
                        while(Time.realtimeSinceStartup<deadline)
                        {
                            yield return null;Vector3 current=player.Arms.BonePosition("HandR");maximumStep=Mathf.Max(maximumStep,Vector3.Distance(previous,current));previous=current;
                            if(player.Arms.ToolUseWeight>.97f)
                            {
                                var axis=player.Camera.transform.InverseTransformDirection(player.Arms.Bone("ToolSocket").up);
                                maxForward=Mathf.Max(maxForward,axis.z);minUp=Mathf.Min(minUp,axis.y);
                            }
                            Check(Vector3.Distance(player.HeldBlock.Centre,player.HeldBlock.Socket.position)<.0001f,"Live strike retains prop attachment");
                            if(slot==4)
                            {
                                var edge=player.HeldBlock.Socket.GetComponentsInChildren<Transform>().First(t=>t.name=="BladeForward");
                                Check(Vector3.Dot((edge.position-edge.parent.position).normalized,player.HeldBlock.Hand.up)>.99f,"Axe edge stays fixed to the grip throughout the rotational stroke");
                            }
                            if(slot==3)
                            {
                                report.maxSupportContactErrorMetres=Mathf.Max(report.maxSupportContactErrorMetres,player.Arms.SupportContactError);
                                report.maxBodySupportContactErrorMetres=Mathf.Max(report.maxBodySupportContactErrorMetres,player.Body.SupportContactError);
                                report.maxBodySupportWristDegrees=Mathf.Max(report.maxBodySupportWristDegrees,Vector3.Angle(player.Body.Bone("HandL").up,player.Body.Bone("ForearmL").up));
                                Check(player.Arms.SupportContactError<.002f&&player.Body.SupportContactError<.002f,"Both live pickaxe rigs retain support-hand contact through the strike: "+player.Arms.SupportContactError+" / "+player.Body.SupportContactError);
                                Check(report.maxBodySupportWristDegrees<35,"Body support wrist stays within 35 degrees during the transition");
                            }
                        }
                        Check(player.Arms.ToolUseWeight>.99f,"Active tool transitions into the vertical action pose");
                        Check(maxForward>.65f&&minUp<.65f,"Tool rotates forwards and down through its strike: "+expected[slot]+" / forward "+maxForward+" / up "+minUp);
                        Check(maximumStep<.16f,"Live strike has no arm teleport: "+expected[slot]);
                        yield return Capture((female?"female":"male")+"-swing-"+expected[slot]);player.VerificationMining=false;yield return new WaitForSecondsRealtime(.4f);
                    }
                }
            }
            game.Selected=2;yield return new WaitForSecondsRealtime(.8f);
            var useButton=PlayerPrefs.GetInt("mineButton",0)==0?MouseButton.Right:MouseButton.Left;
            string placementBeforeGuard=game.PlacementDiagnostic;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(useButton));
            yield return new WaitForSecondsRealtime(.25f);
            Check(player.Arms.ToolUseWeight>.99f&&player.Arms.MiningWeight<.03f,"Bound Use holds the weapon vertically in guard without swinging");
            Check(Mathf.Abs(player.Camera.transform.InverseTransformDirection(player.Arms.Bone("ToolSocket").up).y)>.65f,"Weapon guard shaft is vertical");
            Check(game.PlacementDiagnostic==placementBeforeGuard,"Weapon guard does not attempt terrain placement or show placement feedback");
            yield return Capture("weapon-guard");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForSecondsRealtime(.5f);
            Check(player.Arms.ToolUseWeight<.03f,"Releasing guard returns the weapon to horizontal rest");
            yield return Capture("weapon-rest-after-guard");
            foreach(byte utility in new[]{BlockId.Torch,Fluids.EmptyBucket,Fluids.WaterBucket})
            {
                game.Inventory.Take(7,64);game.Inventory.Add(utility,1,7,8);game.Selected=7;yield return new WaitForSecondsRealtime(.35f);
                Check(player.HeldBlock.Visible&&player.HeldBlock.ItemId==utility,"3D utility item follows the selected stack: "+utility);
                yield return Capture("utility-"+utility);
            }
            game.Inventory.Take(game.Selected,64);yield return null;yield return new WaitForEndOfFrame();
            Check(!player.HeldBlock.Visible,"Consuming the last selected item hides it immediately");
            // Rapid selections during an active strike must converge on the last item.
            player.VerificationMining=true;
            for(int frame=0;frame<24;frame++){game.Selected=frame%ids.Length;yield return null;}
            game.Selected=3;yield return new WaitForSecondsRealtime(.4f);
            Check(player.HeldBlock.ItemId==ids[3]&&player.Arms.Grip==GripPose.TwoHandTool,"Rapid selection while swinging converges on the final item");
            player.VerificationMining=false;yield return new WaitForSecondsRealtime(.7f);
            foreach(float fov in new[]{60f,78f,100f})
            {player.Camera.fieldOfView=fov;yield return null;Check(player.Arms.SupportContactError<.002f,"Support contact survives FOV compensation");yield return Capture("pickaxe-fov-"+fov);}
            player.Camera.fieldOfView=78;
            var frames=new List<float>();
            for(int frame=0;frame<180;frame++){yield return null;frames.Add(Time.unscaledDeltaTime*1000);}
            frames.Sort();report.frameMedianMs=frames[frames.Count/2];report.frameP95Ms=frames[(int)(frames.Count*.95f)];
            // A timed source sequence records actual live playback for human motion review.
            game.Selected=4;yield return new WaitForSecondsRealtime(.4f);Time.captureDeltaTime=1f/60;
            for(int frame=0;frame<180;frame++)
            {if(frame==60)game.Selected=2;if(frame==120)game.Selected=3;player.VerificationMining=frame%60>=15&&frame%60<40;yield return Capture("motion-"+frame.ToString("D3"));}
            Time.captureDeltaTime=0;player.VerificationMining=false;Check(errors.Count==0,"No Unity errors during live equipment review");
        }
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
