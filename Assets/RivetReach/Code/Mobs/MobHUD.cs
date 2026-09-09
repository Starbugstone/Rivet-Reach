using UnityEngine;

namespace RivetReach
{
    public sealed class MobHUD : MonoBehaviour
    {
        MobSystem mobs;Expedition game;GUIStyle label;
        public void Initialize(MobSystem system,Expedition expedition){mobs=system;game=expedition;}
        void OnGUI()
        {
            if(game.Mode!=ScreenMode.Play||mobs.Target==null||!mobs.Target.Alive)return;
            if(label==null)label=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=18,fontStyle=FontStyle.Bold};
            var target=mobs.Target;
            float x=Screen.width*.5f-160,y=Screen.height*.5f+35;
            GUI.color=new Color(.04f,.07f,.08f,.92f);GUI.DrawTexture(new Rect(x,y,320,57),Texture2D.whiteTexture);
            GUI.color=Color.white;
            string action=target.Intent==MobIntent.Windup?" — incoming bite!":target.Intent==MobIntent.Warning?" — territorial":"";
            GUI.Label(new Rect(x,y+2,320,30),target.Definition.displayName+action,label);
            GUI.color=new Color(.28f,.14f,.1f);GUI.DrawTexture(new Rect(x+16,y+37,288,7),Texture2D.whiteTexture);
            GUI.color=new Color(.85f,.55f,.26f);GUI.DrawTexture(new Rect(x+16,y+37,288*target.Health/target.Definition.health,7),Texture2D.whiteTexture);
            GUI.color=Color.white;
        }
    }
}
