using System;
using UnityEngine;

namespace RivetReach
{
    // Presentation preference only. Verification keeps its explicit frame policy.
    public static class FramePacing
    {
        public enum Mode { Legacy90, DisplaySync, Cap60, Cap120 }
        const string Key = "display.framePacing";
        public static Mode Current => (Mode)Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, 3);
        public static Mode Next(Mode mode) => (Mode)(((int)mode + 1) % 4);

        public static void Apply(Mode mode, bool persist = false)
        {
            if (!Enum.IsDefined(typeof(Mode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            if (persist)
            {
                PlayerPrefs.SetInt(Key, (int)mode);
                PlayerPrefs.Save();
            }
            QualitySettings.vSyncCount = mode == Mode.DisplaySync ? 1 : 0;
            Application.targetFrameRate = mode switch { Mode.DisplaySync => -1, Mode.Cap60 => 60, Mode.Cap120 => 120, _ => 90 };
            var arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-rr-frame-limit");
            if (index >= 0 && index + 1 < arguments.Length && int.TryParse(arguments[index + 1], out int limit)
                && (limit == -1 || limit >= 60 && limit <= 360))
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = limit;
            }
        }

        public static string Label(Mode mode) => mode switch
        {
            Mode.DisplaySync => "FRAME PACING: DISPLAY SYNC",
            Mode.Cap60 => "FRAME PACING: 60 FPS CAP",
            Mode.Cap120 => "FRAME PACING: 120 FPS CAP",
            _ => "FRAME PACING: 90 FPS CAP"
        };
    }
}
