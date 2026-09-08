using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    // Explicit -rr-avatar-verify mode: deterministic imported-mesh/animation review.
    public sealed class AvatarVerification : MonoBehaviour
    {
        [Serializable] sealed class Result
        {
            public string result,unity;
            public string[] checks,errors;
            public int maleTriangles,femaleTriangles,armsTriangles,bones,clips;
            public Vector3 leftWristViewport,rightWristViewport;
        }
        readonly List<string> checks=new List<string>(),errors=new List<string>();
        Result result=new Result();string output;Camera camera;AvatarView male,female,hands;
        void Awake(){Application.logMessageReceived+=Log;}
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rr-output");output=i>=0?args[i+1]:"AvatarVerification";Directory.CreateDirectory(output);
            var routines=new Stack<IEnumerator>();routines.Push(Run());
            while(routines.Count>0)
            {
                bool more;object current=null;
                try{more=routines.Peek().MoveNext();if(more)current=routines.Peek().Current;}
                catch(Exception ex){errors.Add(ex.ToString());break;}
                if(!more){routines.Pop();continue;}
                if(current is IEnumerator nested){routines.Push(nested);continue;}yield return current;
            }
            result.result=errors.Count==0?"PASS":"FAIL";result.unity=Application.unityVersion;result.checks=checks.ToArray();result.errors=errors.ToArray();
            File.WriteAllText(Path.Combine(output,"avatar-report.json"),JsonUtility.ToJson(result,true));Application.Quit(errors.Count==0?0:1);
        }
        void Check(bool value,string label){if(!value)throw new Exception(label);checks.Add(label);}
        AvatarView Avatar(string label,Vector3 position,bool isFemale)
        {
            var o=new GameObject(label);o.transform.position=position;var avatar=o.AddComponent<AvatarView>();avatar.Build(isFemale,0);return avatar;
        }
        IEnumerator Capture(string name)
        {yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.15f);}
        IEnumerator Run()
        {
            Time.timeScale=1;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
            foreach(var c in FindObjectsByType<Camera>())c.gameObject.SetActive(false);
            foreach(var l in FindObjectsByType<Light>())l.gameObject.SetActive(false);
            RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.58f,.62f,.68f);
            var light=new GameObject("Review key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.25f;light.transform.rotation=Quaternion.Euler(38,152,0);
            camera=new GameObject("Review camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.23f,.28f,.30f);camera.nearClipPlane=.025f;camera.farClipPlane=20;camera.fieldOfView=38;
            camera.transform.position=new Vector3(0,1,3.25f);camera.transform.LookAt(new Vector3(0,.95f,0));
            male=Avatar("Male review",new Vector3(.48f,0,0),false);female=Avatar("Female review",new Vector3(-.48f,0,0),true);
            result.maleTriangles=male.TriangleCount;result.femaleTriangles=female.TriangleCount;result.bones=male.BoneCount;result.clips=male.ClipCount;
            Check(male.AnimationReady&&female.AnimationReady,"Both animation graphs initialize");
            Check(male.ClipCount==8&&female.ClipCount==8,"Both FBX imports contain all eight clips");
            Check(male.BoneCount==40&&female.BoneCount==40,"Both imports retain all 40 weighted bones");
            Check(male.TriangleCount<=5000&&female.TriangleCount<=5000,"Both full models remain within 5000 triangles");
            male.SamplePose("Idle",0);female.SamplePose("Idle",0);yield return Capture("01-front");
            male.transform.rotation=female.transform.rotation=Quaternion.Euler(0,180,0);yield return Capture("02-back");
            male.transform.rotation=Quaternion.Euler(0,-20,0);female.transform.rotation=Quaternion.Euler(0,-20,0);
            camera.transform.position=new Vector3(0,1.59f,1.9f);camera.transform.LookAt(new Vector3(0,1.56f,0));yield return Capture("03-face");
            camera.transform.position=new Vector3(0,1,3.25f);camera.transform.LookAt(new Vector3(0,.95f,0));
            male.SamplePose("Walk",.25f);female.SamplePose("Walk",.75f);yield return Capture("04-walk");
            male.SamplePose("Run",.2f);female.SamplePose("Run",.6f);yield return Capture("05-run");
            male.SamplePose("Mine",.14f);female.SamplePose("Mine",.14f);yield return Capture("06-mine");
            male.SamplePose("Airborne",0);female.SamplePose("Airborne",0);yield return Capture("07-airborne");
            male.Build(false,1);female.Build(true,1);yield return Capture("08-alternate");
            // Exercise the actual graph transitions, including a masked punch during walking.
            Vector3 foot=male.BonePosition("FootL");
            Vector3 idleHand=male.BonePosition("HandR");float handTravel=0;
            for(int frame=0;frame<30;frame++){male.Animate(1,frame>8,frame*.16f);handTravel=Mathf.Max(handTravel,Vector3.Distance(idleHand,male.BonePosition("HandR")));yield return null;}
            Check(Vector3.Distance(foot,male.BonePosition("FootL"))>.025f,"Live locomotion moves the foot while mining layer is active");
            Check(handTravel>.20f,"Live upper-body mining layer raises and extends the striking hand");
            male.gameObject.SetActive(false);female.gameObject.SetActive(false);
            camera.transform.position=Vector3.zero;camera.transform.rotation=Quaternion.identity;camera.fieldOfView=78;
            var o=new GameObject("First person review");o.transform.SetParent(camera.transform,false);o.transform.localPosition=new Vector3(0,-1.5f,.02f);hands=o.AddComponent<AvatarView>();hands.FirstPersonArms=true;
            foreach(bool isFemale in new[]{false,true})foreach(int skin in new[]{0,1})
            {
                hands.Build(isFemale,skin);hands.SamplePose("FP_Idle",0);result.armsTriangles=hands.TriangleCount;
                Check(hands.TriangleCount<=1500,"Articulated arm pair remains below the initial 1500 triangle limit");
                result.leftWristViewport=camera.WorldToViewportPoint(hands.BonePosition("HandL"));result.rightWristViewport=camera.WorldToViewportPoint(hands.BonePosition("HandR"));
                foreach(var wrist in new[]{result.leftWristViewport,result.rightWristViewport})
                    Check(wrist.z>.2f&&wrist.y>0&&wrist.y<.38f&&wrist.x>.12f&&wrist.x<.88f,"Resting wrists sit inside the lower camera frame");
                string label=(isFemale?"female":"male")+"-skin"+skin;
                yield return Capture("09-hands-"+label);
                Vector3 rest=hands.BonePosition("HandR");hands.SamplePose("FP_Mine",.14f);
                Check(Vector3.Distance(rest,hands.BonePosition("HandR"))>.10f,"Authored mining clip extends the striking hand");
                yield return Capture("10-strike-"+label);
            }
            hands.Build(false,0);hands.SamplePose("FP_Idle",0);
            Vector3 referenceWrist=camera.WorldToViewportPoint(hands.BonePosition("HandR"));
            foreach(float fov in new[]{60f,100f})
            {
                camera.fieldOfView=fov;hands.FitFirstPersonFov(fov);
                Check(Vector3.Distance(referenceWrist,camera.WorldToViewportPoint(hands.BonePosition("HandR")))<.001f,"Hand framing remains stable when world field of view changes");
                yield return Capture("11-fov-"+fov);
            }
            camera.fieldOfView=78;hands.FitFirstPersonFov(78);
            // Captured sequence samples the graph that the live player uses, not just imported clips.
            for(int frame=0;frame<48;frame++)
            {
                hands.Animate(frame<12?0:1,frame>=20&&frame<42,frame*.14f);
                if(frame%4==0)yield return Capture("motion-"+frame.ToString("D2"));else yield return null;
            }
            Check(errors.Count==0,"No Unity errors during appearance rebuilds and animation transitions");
        }
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
