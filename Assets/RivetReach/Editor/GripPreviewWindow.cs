using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public sealed class GripPreviewWindow : EditorWindow
    {
        [MenuItem("Rivet Reach/Review hand grips")]
        static void Open()=>GetWindow<GripPreviewWindow>("Hand grips");
        void OnGUI()
        {
            EditorGUILayout.LabelField("Hand and item contact",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Enter Play and start an expedition. These cosmetic props preview the authored grips; they do not add tool or combat gameplay.",MessageType.Info);
            var player=Object.FindAnyObjectByType<FirstPersonPlayer>();
            using(new EditorGUI.DisabledScope(!EditorApplication.isPlaying||player==null||player.HeldBlock==null))
            {
                if(GUILayout.Button("Selected inventory block / bare hand"))player.HeldBlock.SetPreview(null);
                if(GUILayout.Button("Sword · one hand"))player.HeldBlock.SetPreview(GripPose.Tool);
                if(GUILayout.Button("Pickaxe · two hands"))player.HeldBlock.SetPreview(GripPose.TwoHandTool);
            }
        }
        void OnDisable(){var player=Object.FindAnyObjectByType<FirstPersonPlayer>();if(player!=null&&player.HeldBlock!=null)player.HeldBlock.SetPreview(null);}
    }
}
