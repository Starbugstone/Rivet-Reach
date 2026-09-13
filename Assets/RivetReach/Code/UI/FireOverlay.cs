using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    // Lightweight original screen-space flames; no texture downloads or per-frame objects.
    public sealed class FireOverlay : MaskableGraphic
    {
        Expedition game;
        bool visible;
        public void Initialize(Expedition owner){game=owner;raycastTarget=false;}
        void LateUpdate()
        {
            bool next=game!=null&&game.Health.Burning&&!game.Health.Dead&&!game.Creative;
            if(next||visible!=next){visible=next;SetVerticesDirty();}
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(!visible)return;
            var r=rectTransform.rect;
            for(int i=0;i<24;i++)
            {
                float x=r.xMin+r.width*i/23f,w=r.width/20;
                float h=r.height*(.08f+.06f*(1+Mathf.Sin(i*2.31f+Time.time*5)));
                int n=mesh.currentVertCount;
                mesh.AddVert(new Vector3(x-w,r.yMin),new Color(1,.16f,.01f,.68f),Vector2.zero);
                mesh.AddVert(new Vector3(x+w*.3f*Mathf.Sin(Time.time*3+i),r.yMin+h),new Color(1,.65f,.08f,.08f),Vector2.zero);
                mesh.AddVert(new Vector3(x+w,r.yMin),new Color(1,.36f,.015f,.58f),Vector2.zero);
                mesh.AddTriangle(n,n+1,n+2);
            }
        }
    }
}
