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
            public int maleTriangles,femaleTriangles,armsTriangles,bones,clips,fullVertices,armsVertices;
            public float motion90Milliseconds,mining90Milliseconds,bodyAndArmsAnimationMeanMs,bodyAndArmsAnimationP95Ms;
            public string animationBenchmark="1000 paired body/arms Animate calls after warmup; main-thread graph evaluation only, excludes rendering and terrain";
            public Vector3 leftWristViewport,rightWristViewport,idleHand,liveHand;
            public float liveHandTravel;
            public Vector3 strafeStep;
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
            result.maleTriangles=male.TriangleCount;result.femaleTriangles=female.TriangleCount;result.bones=male.BoneCount;result.clips=male.ClipCount;result.fullVertices=male.VertexCount;
            Check(male.AnimationReady&&female.AnimationReady,"Both animation graphs initialize");
            Check(male.ClipCount==16&&female.ClipCount==16,"Both FBX imports contain all sixteen clips");
            Check(male.BoneCount==40&&female.BoneCount==40,"Both imports retain all 40 weighted bones");
            Check(male.TriangleCount<=35000&&female.TriangleCount<=35000,"Both full models remain within the 35000 triangle review budget");
            male.SamplePose("Idle",0);female.SamplePose("Idle",0);yield return Capture("01-front");
            male.transform.rotation=female.transform.rotation=Quaternion.Euler(0,180,0);yield return Capture("02-back");
            male.transform.rotation=Quaternion.Euler(0,-20,0);female.transform.rotation=Quaternion.Euler(0,-20,0);
            camera.transform.position=new Vector3(0,1.59f,1.9f);camera.transform.LookAt(new Vector3(0,1.56f,0));yield return Capture("03-face");
            camera.transform.position=new Vector3(0,1,3.25f);camera.transform.LookAt(new Vector3(0,.95f,0));
            male.SamplePose("Walk",.25f);female.SamplePose("Walk",.75f);yield return Capture("04-walk");
            male.SamplePose("Run",.2f);female.SamplePose("Run",.6f);yield return Capture("05-run");
            male.SamplePose("Mine",.22f);female.SamplePose("Mine",.22f);yield return Capture("06-mine");
            male.SamplePose("Airborne",0);female.SamplePose("Airborne",0);yield return Capture("07-airborne");
            male.SamplePose("Idle",0);float standingHead=male.BonePosition("Head").y;
            male.SamplePose("CrouchIdle",0);female.SamplePose("CrouchWalk",.3f);yield return Capture("07b-crouch");
            Check(standingHead-male.BonePosition("Head").y>.25f,"Crouch bends the skeleton and lowers the head without scaling the model");
            Check(Mathf.Abs(male.BonePosition("FootL").y-.155f)<.025f,"Crouch retains grounded ankle contact");
            male.SamplePose("Land",.2f);female.SamplePose("Land",.2f);yield return Capture("07c-landing");
            Check(standingHead-male.BonePosition("Head").y>.04f,"Landing clip absorbs impact through the legs and hips");
            male.Build(false,1);female.Build(true,1);yield return Capture("08-alternate");
            // Exercise the actual graph transitions, including a masked punch during walking.
            Vector3 foot=male.BonePosition("FootL");
            Vector3 idleHand=male.BonePosition("HandR");float handTravel=0;result.idleHand=idleHand;
            for(int frame=0;frame<30;frame++){male.Animate(1,frame>8,frame*.16f,deltaTime:1f/60);handTravel=Mathf.Max(handTravel,Vector3.Distance(idleHand,male.BonePosition("HandR")));yield return null;}
            Check(Vector3.Distance(foot,male.BonePosition("FootL"))>.025f,"Live locomotion moves the foot while mining layer is active");
            Check(male.BonePosition("HandR").y>idleHand.y+.25f,"Masked mining raises the hand above its walking swing");
            result.liveHandTravel=handTravel;result.liveHand=male.BonePosition("HandR");
            Check(handTravel>.20f,"Live upper-body mining layer raises and extends the striking hand");
            for(int frame=0;frame<60;frame++)male.Animate(1,false,1,direction:Vector2.right,deltaTime:1f/60);
            Vector3 strafeFoot=male.BonePosition("FootL");
            male.Animate(1,false,2.5f,direction:Vector2.right,deltaTime:1f/60);
            Vector3 strafeStep=male.BonePosition("FootL")-strafeFoot;
            result.strafeStep=strafeStep;yield return Capture("07d-strafe");
            Check(Mathf.Abs(strafeStep.x)>.02f,"Strafing turns the foot path laterally while preserving forward upper-body presentation");
            male.gameObject.SetActive(false);female.gameObject.SetActive(false);
            camera.transform.position=Vector3.zero;camera.transform.rotation=Quaternion.identity;camera.fieldOfView=78;
            var o=new GameObject("First person review");o.transform.SetParent(camera.transform,false);o.transform.localPosition=new Vector3(0,-1.5f,.02f);hands=o.AddComponent<AvatarView>();hands.FirstPersonArms=true;
            foreach(bool isFemale in new[]{false,true})foreach(int skin in new[]{0,1})
            {
                hands.Build(isFemale,skin);hands.SamplePose("FP_Idle",0);result.armsTriangles=hands.TriangleCount;result.armsVertices=hands.VertexCount;
                Check(hands.TriangleCount<=12000,"Articulated arm pair remains below the 12000 triangle review budget");
                result.leftWristViewport=camera.WorldToViewportPoint(hands.BonePosition("HandL"));result.rightWristViewport=camera.WorldToViewportPoint(hands.BonePosition("HandR"));
                foreach(var wrist in new[]{result.leftWristViewport,result.rightWristViewport})
                    Check(wrist.z>.2f&&wrist.y>0&&wrist.y<.38f&&wrist.x>.12f&&wrist.x<.88f,"Resting wrists sit inside the lower camera frame");
                string label=(isFemale?"female":"male")+"-skin"+skin;
                yield return Capture("09-hands-"+label);
                Vector3 rest=hands.BonePosition("HandR");hands.SamplePose("FP_Mine",.22f);
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
            Check(hands.VertexCount<result.fullVertices*.55f,"First-person mesh excludes hidden body vertices from skinning");
            // Time-based state tests catch sluggish input response and frame-rate-dependent blends.
            hands.Build(false,0);float elapsed=0;
            while(hands.MotionWeight<.9f&&elapsed<1){hands.Animate(1,false,elapsed*8,deltaTime:1f/120);elapsed+=1f/120;}
            result.motion90Milliseconds=elapsed*1000;
            Check(elapsed<=.11f,"Locomotion reaches 90 percent presentation weight within 110 ms");
            elapsed=0;
            while(hands.MiningWeight<.9f&&elapsed<1){hands.Animate(0,true,0,deltaTime:1f/120);elapsed+=1f/120;}
            result.mining90Milliseconds=elapsed*1000;
            Check(elapsed<=.065f,"Mining reaches 90 percent presentation weight within 65 ms");
            Vector3[] poses=new Vector3[2];int[] rates={30,144};
            for(int r=0;r<2;r++)
            {
                hands.Build(false,0);
                for(int frame=1;frame<=rates[r]/2;frame++)hands.Animate(1,false,frame/(float)rates[r]*6,deltaTime:1f/rates[r]);
                poses[r]=hands.BonePosition("HandR");
            }
            Check(Vector3.Distance(poses[0],poses[1])<.001f,"30 and 144 Hz animation updates converge to the same hand pose");
            for(int frame=0;frame<36;frame++)hands.Animate(0,false,3,deltaTime:1f/120);
            Check(hands.MotionWeight<.015f,"Stopping settles the hands to idle within 300 ms");
            hands.Animate(0,false,0,landingImpact:10,deltaTime:.016f);
            for(int frame=0;frame<12;frame++)hands.Animate(0,false,0,deltaTime:1f/120);
            Check(hands.LandingWeight>.2f,"Landing impact activates the presentation layer");
            for(int frame=0;frame<60;frame++)hands.Animate(0,false,0,deltaTime:1f/120);
            Check(hands.LandingWeight<.001f,"Landing presentation settles without a persistent offset");
            male.gameObject.SetActive(true);
            var durations=new double[1000];
            for(int frame=-100;frame<durations.Length;frame++)
            {
                long start=System.Diagnostics.Stopwatch.GetTimestamp();
                male.Animate(1,true,frame*.1f,deltaTime:1f/60);hands.Animate(1,true,frame*.1f,deltaTime:1f/60);
                if(frame>=0)durations[frame]=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            }
            male.gameObject.SetActive(false);result.bodyAndArmsAnimationMeanMs=(float)durations.Average();Array.Sort(durations);result.bodyAndArmsAnimationP95Ms=(float)durations[949];
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
