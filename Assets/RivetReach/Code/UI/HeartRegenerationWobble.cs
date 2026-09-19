using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    // Fixed-size presentation state shared by the pixel food meter and text hearts.
    // Hidden screens and replaced player state establish a baseline, never a pulse.
    internal sealed class SurvivalWobble
    {
        const float Duration=.45f,Amplitude=3;
        readonly float[] began=new float[10];
        HungerState owner;
        float previous=float.NaN;
        bool active;
        public SurvivalWobble(){for(int i=0;i<began.Length;i++)began[i]=float.NegativeInfinity;}
        public bool Observe(HungerState current,float value,bool decrease,bool visible,float now)
        {
            bool wasActive=active;
            if(owner!=current||!visible||float.IsNaN(previous))
            {
                owner=current;previous=value;active=false;
                for(int i=0;i<began.Length;i++)began[i]=float.NegativeInfinity;
                return wasActive;
            }
            if(decrease?value<previous:value>previous)
            {
                float lower=Mathf.Min(previous,value),upper=Mathf.Max(previous,value);
                for(int i=0;i<began.Length;i++)if(upper>i*2&&lower<(i+1)*2)began[i]=now;
            }
            previous=value;active=false;
            for(int i=0;i<began.Length;i++)active|=now-began[i]<Duration;
            return active||wasActive;
        }
        public float Offset(int icon,float now)
        {
            float progress=(now-began[icon])/Duration;
            return progress>=0&&progress<1?Amplitude*Mathf.Sin(progress*Mathf.PI*2)*(1-progress):0;
        }
    }

    // Preserve the existing heart font, colours and layout; move only changed glyphs.
    public sealed class HeartRegenerationWobble : BaseMeshEffect
    {
        readonly SurvivalWobble wobble=new SurvivalWobble();
        Expedition game;
        public void Initialize(Expedition owner){game=owner;}
        void LateUpdate()
        {
            if(game?.Hunger==null||game.Health==null)return;
            bool visible=game.Mode==ScreenMode.Play&&!game.LoadingSave&&!game.WaitingForRespawn&&!game.Health.Dead&&graphic.canvas!=null&&graphic.canvas.isActiveAndEnabled;
            if(wobble.Observe(game.Hunger,game.Health.Hearts,false,visible,Time.unscaledTime))graphic.SetVerticesDirty();
        }
        public override void ModifyMesh(VertexHelper mesh)
        {
            if(!IsActive())return;
            for(int icon=0;icon<10&&icon*4+3<mesh.currentVertCount;icon++)
            {
                float offset=wobble.Offset(icon,Time.unscaledTime);if(offset==0)continue;
                for(int v=icon*4;v<icon*4+4;v++){var vertex=new UIVertex();mesh.PopulateUIVertex(ref vertex,v);vertex.position.y+=offset;mesh.SetUIVertex(vertex,v);}
            }
        }
    }
}
