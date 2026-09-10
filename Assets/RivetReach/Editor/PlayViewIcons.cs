using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    // These are Editor annotations, not world objects or gameplay feedback.
    [InitializeOnLoad]
    public static class PlayViewIcons
    {
        static bool pending=true;
        static double nextAttempt;
        static PlayViewIcons()
        {
            EditorApplication.update+=HideWhenReady;
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.ExitingEditMode||state==PlayModeStateChange.EnteredPlayMode)
                {pending=true;nextAttempt=0;HideWhenReady();}
            };
        }
        static void HideWhenReady()
        {
            if(!pending||EditorApplication.timeSinceStartup<nextAttempt)return;
            nextAttempt=EditorApplication.timeSinceStartup+.25;
            pending=false;bool changed=false;
            // A fresh Editor registers annotations after its views initialize.
            // AudioListener has no icon; the speaker markers belong to AudioSource.
            foreach(var type in new[]{typeof(Light),typeof(AudioSource)})
            {
                if(!GizmoUtility.TryGetGizmoInfo(type,out var info)){pending=true;continue;}
                if(!info.iconEnabled)continue;
                GizmoUtility.SetIconEnabled(type,false);changed=true;
            }
            if(changed)SceneView.RepaintAll();
        }
    }
}
