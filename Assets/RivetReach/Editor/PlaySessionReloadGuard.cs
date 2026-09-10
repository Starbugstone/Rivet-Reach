using UnityEditor;

namespace RivetReach.Editor
{
    // Session authorities use non-serialized inventories, workers and event wiring.
    // A script reload cannot restore them. Keep playing the loaded code until Stop;
    // do not silently reset the world or suppress exceptions from a broken session.
    [InitializeOnLoad]
    public static class PlaySessionReloadGuard
    {
        static bool locked;
        public static bool Locked=>locked;
        static PlaySessionReloadGuard()
        {
            EditorApplication.playModeStateChanged+=Changed;
            EditorApplication.quitting+=Unlock;
        }
        static void Changed(PlayModeStateChange state)
        {
            // Enter/exit domain reloads stay enabled. Only the live Play interval is locked.
            if(state==PlayModeStateChange.EnteredPlayMode&&!locked)
            {EditorApplication.LockReloadAssemblies();locked=true;}
            else if(state==PlayModeStateChange.ExitingPlayMode||state==PlayModeStateChange.EnteredEditMode)Unlock();
        }
        static void Unlock(){if(!locked)return;locked=false;EditorApplication.UnlockReloadAssemblies();}
    }
}
