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
                SessionState.SetBool(Key+"complete",false);
                EditorApplication.delayCall+=EditorApplication.EnterPlaymode;
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
            var game=Expedition.Instance;
            Check(game!=null&&game.World!=null&&game.Player!=null&&game.Items!=null&&game.UI!=null,"Ordinary Bootstrap completed the whole session");
            Check(game.Mode==ScreenMode.Title&&!game.Started,"Play opens on the title screen");
            Check(game.UI.GetComponentInChildren<Canvas>().isActiveAndEnabled,"Title canvas is active");
            var start=game.UI.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="START EXPEDITION");
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
                Check(view.AnimationReady&&view.ClipCount==16&&animator.isActiveAndEnabled,$"{(female?"Female":"Male")} skin {skin} {(arms?"hands":"body")} creates its Animator");
                var before=view.BonePosition("HandR");
                view.SamplePose(arms?"FP_Mine":"Mine",.216f);
                Check(Vector3.Distance(before,view.BonePosition("HandR"))>.03f,"Authored mining clip moves the initialized hand");
                yield return null;
            }
            UnityEngine.Object.Destroy(fixture);
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
            Check(Errors.Length==0,"Completed Play cycle without logged errors or exceptions");
        }
    }
}
