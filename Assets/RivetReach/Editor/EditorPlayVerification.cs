using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RivetReach.Editor
{
    // Explicit Editor-only smoke test: exercise ordinary Bootstrap, including domain reloads.
    // Does not prepare assets, replace scenes or change appearance preferences.
    [InitializeOnLoad]
    public static class EditorPlayVerification
    {
        const string Key="RivetReach.EditorPlayVerification.";
        const string Output="Logs/EditorPlayVerification";
        static bool Active=>SessionState.GetBool(Key+"active",false);
        static string Errors=>SessionState.GetString(Key+"errors","");
        static int Cycle=>SessionState.GetInt(Key+"cycle",0);
        static IEnumerator run;
        static object waiting;
        static int lastFrame;
        static double deadline;
        static double restartAt;
        static EditorPlayVerification()
        {
            EditorApplication.playModeStateChanged+=StateChanged;
            Application.logMessageReceived+=Log;
            EditorApplication.update+=Tick;
        }
        [MenuItem("Rivet Reach/Verify Editor Play startup")]
        public static void Begin()
        {
            if(Active||EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop the current Play session before running the Editor startup check.");
            var scene=SceneManager.GetActiveScene();
            if(SceneManager.sceneCount!=1||scene.path!="Assets/RivetReach/Scenes/Main.unity"||scene.isDirty)
                throw new InvalidOperationException("Open the saved Main scene first; verification preserves unsaved scenes.");
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output+"/checks.txt","");
            File.WriteAllText(Output+"/result.txt","RUNNING\n");
            SessionState.SetString(Key+"errors","");SessionState.SetInt(Key+"cycle",0);
            SessionState.SetBool(Key+"complete",false);SessionState.SetBool(Key+"active",true);
            EditorApplication.EnterPlaymode();
        }
        static void Log(string message,string stack,LogType type)
        {
            if(Active&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))
                SessionState.SetString(Key+"errors",Errors+message+"\n"+stack+"\n");
        }
        static void StateChanged(PlayModeStateChange state)
        {
            if(!Active)return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {run=Run();waiting=null;lastFrame=-1;deadline=EditorApplication.timeSinceStartup+100;}
            if(state!=PlayModeStateChange.EnteredEditMode)return;
            if(!SessionState.GetBool(Key+"complete",false)&&Errors.Length==0)
                SessionState.SetString(Key+"errors","Verification was interrupted before completing a Play cycle.");
            if(Errors.Length==0&&Cycle<2)
            {
                // Re-enter after the exit transition has settled; a same-update delay can be lost.
                restartAt=EditorApplication.timeSinceStartup+.5;
                return;
            }
            string result=Errors.Length==0?"PASS":"FAIL";
            File.WriteAllText(Output+"/result.txt",result+"\nUnity "+Application.unityVersion+"\nCompleted Play cycles: "+Cycle+"\n"+Errors);
            SessionState.SetBool(Key+"active",false);
            Debug.Log("Editor Play startup verification: "+result+". See "+Output+".");
        }
        static void Check(bool passed,string description)
        {
            if(!passed)throw new InvalidOperationException(description);
            File.AppendAllText(Output+"/checks.txt",$"Cycle {Cycle+1}: {description}\n");
        }
        static void Tick()
        {
            if(Active&&restartAt>0&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&EditorApplication.timeSinceStartup>=restartAt)
            {
                restartAt=0;SessionState.SetBool(Key+"complete",false);
                EditorApplication.EnterPlaymode();return;
            }
            if(!Active||!EditorApplication.isPlaying||run==null)return;
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Editor Play verification timed out.");
                if(lastFrame==Time.frameCount||waiting is CustomYieldInstruction wait&&wait.keepWaiting)return;
                if(run.MoveNext()){waiting=run.Current;lastFrame=Time.frameCount;return;}
                SessionState.SetInt(Key+"cycle",Cycle+1);SessionState.SetBool(Key+"complete",true);
            }
            catch(Exception ex){SessionState.SetString(Key+"errors",Errors+ex+"\n");}
            (run as IDisposable)?.Dispose();run=null;
            EditorApplication.ExitPlaymode();
        }
        static IEnumerator Run()
        {
            yield return null;
            Check(Errors.Length==0,"Bootstrap logged no errors or exceptions");
            foreach(var type in new[]{typeof(Light),typeof(AudioSource)})
                Check(GizmoUtility.TryGetGizmoInfo(type,out var icon)&&!icon.iconEnabled,type.Name+" Editor icons stay hidden after entering Play");
            var game=Expedition.Instance;
            Check(game!=null&&game.World!=null&&game.Player!=null&&game.Items!=null&&game.UI!=null,"Ordinary Bootstrap completed the whole session");
            Check(game.Mode==ScreenMode.Title&&!game.Started,"Play opens on the title screen");
            Check(game.UI.VisibleRoot.GetComponentInChildren<Canvas>().isActiveAndEnabled,"Title canvas is active");
            var start=game.UI.VisibleRoot.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="START EXPEDITION");
            Check(start.isActiveAndEnabled&&start.interactable,"Start Expedition button is available");
            Check(game.Player.Body.AnimationReady&&game.Player.Arms.AnimationReady,"Player body and hand animation graphs are ready");
            var preview=UnityEngine.Object.FindObjectsByType<AvatarView>(FindObjectsInactive.Include).Single(v=>v.Preview);
            Check(preview.AnimationReady,"Appearance portrait animation graph is ready");
            // Build and rebuild the real models on an isolated view; no saved selection changes.
            var fixture=new GameObject("Avatar initialization regression");fixture.transform.position=Vector3.down*2000;
            var view=fixture.AddComponent<AvatarView>();
            foreach(bool female in new[]{false,true})foreach(int skin in new[]{0,1})foreach(bool arms in new[]{false,true})
            {
                view.FirstPersonArms=arms;view.Build(female,skin);
                var animator=view.GetComponentsInChildren<Animator>().Single();
                Check(view.AnimationReady&&view.ClipCount==50&&animator.isActiveAndEnabled,$"{(female?"Female":"Male")} skin {skin} {(arms?"hands":"body")} creates its Animator");
                var before=view.BonePosition("HandR");
                view.SamplePose(arms?"FP_Mine":"Mine",.216f);
                Check(Vector3.Distance(before,view.BonePosition("HandR"))>.03f,"Authored mining clip moves the initialized hand");
                yield return null;
            }
            UnityEngine.Object.Destroy(fixture);
            foreach(var prop in new[]{("GripSword",3366),("GripPickaxe",2876)})
            {
                var asset=Resources.Load<GameObject>("Characters/"+prop.Item1);
                int triangles=asset.GetComponentsInChildren<MeshFilter>().Sum(f=>(int)f.sharedMesh.GetIndexCount(0)/3);
                Check(triangles==prop.Item2&&asset.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.Length==1),
                    prop.Item1+" imports its source triangle count and one material");
            }
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Output+$"/cycle-{Cycle+1}-title.png");
            yield return new WaitForSecondsRealtime(.5f);
            start.onClick.Invoke();
            Check(game.Started&&game.Mode==ScreenMode.Play&&Time.timeScale==1,"Title button enters gameplay");
            float deadline=Time.realtimeSinceStartup+60;
            while(!game.ReadyToPlay&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(game.ReadyToPlay,"Spawn terrain becomes ready");
            yield return new WaitForSecondsRealtime(1);
            Check(game.Player.Camera.isActiveAndEnabled&&game.Player.Camera.targetTexture==null,"Expedition camera renders to the Game view");
            Check(game.Player.Camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing,"Editor gameplay camera enables colour grading");
            Check(game.Player.Arms.gameObject.activeInHierarchy&&game.Player.Body.gameObject.activeInHierarchy,"Body and hands are visible during gameplay");
            Vector3 position=game.Player.transform.position;
            game.Player.VerificationMovement=Vector2.up;game.Player.VerificationMining=true;
            yield return new WaitForSecondsRealtime(.4f);
            Vector3 travelled=game.Player.transform.position-position;travelled.y=0;
            Check(travelled.magnitude>.1f&&game.Player.Arms.MotionWeight>.1f,"Player moves and blends its locomotion animation");
            Check(game.Player.Arms.MiningWeight>.9f,"Mining animation responds in Editor Play");
            game.Player.VerificationMovement=null;game.Player.VerificationMining=false;
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Output+$"/cycle-{Cycle+1}-world.png");
            yield return new WaitForSecondsRealtime(.5f);
            game.Player.Pitch=85;game.Player.Camera.fieldOfView=100;
            yield return new WaitForSecondsRealtime(.3f);
            Check(game.Player.Camera.WorldToViewportPoint(game.Player.Body.BonePosition("Neck")).y<0,"Editor look-down keeps the neck opening below the lens at 100-degree FOV");
            ScreenCapture.CaptureScreenshot(Output+$"/cycle-{Cycle+1}-lookdown.png");
            yield return new WaitForSecondsRealtime(.5f);
            float crouchDeadline=Time.realtimeSinceStartup+3;
            // Exercise the real crouch/animation path independently of Game-view keyboard focus.
            game.Player.VerificationCrouching=true;
            while((game.Player.Height>1.5f||game.Player.Body.CrouchWeight<.995f)&&Time.realtimeSinceStartup<crouchDeadline)
            {
                yield return null;
            }
            Check(game.Player.Height<1.5f&&game.Player.Camera.WorldToViewportPoint(game.Player.Body.BonePosition("Neck")).y<0,
                "Editor crouching look-down keeps the neck opening below the lens at 100-degree FOV; height="+game.Player.Height+", blend="+game.Player.Body.CrouchWeight+", neck="+game.Player.Camera.WorldToViewportPoint(game.Player.Body.BonePosition("Neck")));
            ScreenCapture.CaptureScreenshot(Output+$"/cycle-{Cycle+1}-crouch-lookdown.png");
            yield return new WaitForSecondsRealtime(.5f);
            game.Player.VerificationCrouching=null;
            game.Player.Pitch=10;game.Selected=11;game.Inventory.Take(11,int.MaxValue);game.Inventory.Add(2,2,11,12);
            yield return new WaitForSecondsRealtime(.4f);
            var held=game.Player.HeldBlock;
            Check(held.Visible&&held.ItemId==2&&held.Hand==game.Player.Arms.Bone("HandR"),"Selected dirt appears on the animated right hand in Editor Play");
            var centre=game.Player.Camera.WorldToViewportPoint(held.Centre);
            Check(centre.x>.5f&&centre.x<1&&centre.y>0&&centre.y<.5f,"Held block stays in the lower-right view at 100-degree FOV");
            ScreenCapture.CaptureScreenshot(Output+$"/cycle-{Cycle+1}-held-block.png");
            yield return new WaitForSecondsRealtime(.4f);
            game.Inventory.Take(11,2);yield return new WaitForSecondsRealtime(.1f);
            Check(!held.Visible,"Emptying the selected stack restores the bare fist in Editor Play");
            Check(game.World.Grass.Tick>0&&game.Registry.FistDrop(1)==2,"Grass ticks and grass-to-dirt drops are enabled in ordinary Editor Play");
            Check(Errors.Length==0,"Completed Play cycle without logged errors or exceptions");
        }
    }
}
