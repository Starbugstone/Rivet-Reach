using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    // Original code-authored silhouettes: one mesh per meter, no font/emoji dependency.
    public sealed class SurvivalMeter : MaskableGraphic
    {
        public enum Kind { Food, Armor }
        public const int IconCount=10;
        public Kind MeterKind {get;private set;}
        public int Points {get;private set;}=-1;
        Expedition game;
        static readonly string[] Food={
            "............",".....oooo...","....ohhooo..","...ohhoooo..",
            "...ooooooo..","...ooooooo..","....ooooo...","...bbooo....",
            "..bbb.......",".bbbb.......","..bb........","............"};
        static readonly string[] Armor={
            "............",".ssssssssss.",".shsssssshs.",".shsssssshs.",
            ".shsssssshs.",".shsssssshs.","..shsssshs..","..shsssshs..",
            "...shsshs...","....shhs....",".....ss.....","............"};
        static readonly Color Empty=new Color(.20f,.24f,.26f),Outline=new Color(.055f,.075f,.085f);
        public void Initialize(Expedition owner,Kind kind)
        {game=owner;MeterKind=kind;raycastTarget=false;Refresh();}
        protected override void OnEnable(){base.OnEnable();Points=-1;Refresh();}
        void LateUpdate()=>Refresh();
        void Refresh()
        {
            if(game==null||game.Hunger==null||game.Equipment==null)return;
            int points=Mathf.Clamp(MeterKind==Kind.Food?game.Hunger.Food:game.Equipment.Protection,0,20);
            if(Points==points)return;
            Points=points;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rows=MeterKind==Kind.Food?Food:Armor;
            var rect=rectTransform.rect;float step=rect.width/IconCount,size=Mathf.Min(step-2,rect.height),pixel=size/12;
            for(int icon=0;icon<IconCount;icon++)for(int y=0;y<12;y++)for(int x=0;x<12;x++)
            {
                char cell=rows[y][x];Color tint;
                if(cell=='.')
                {
                    bool edge=x>0&&rows[y][x-1]!='.'||x<11&&rows[y][x+1]!='.'||y>0&&rows[y-1][x]!='.'||y<11&&rows[y+1][x]!='.';
                    if(!edge)continue;tint=Outline;
                }
                else if(Points<=icon*2||(Points==icon*2+1&&x>=6))tint=Empty;
                else if(cell=='b')tint=new Color(.95f,.88f,.70f);
                else if(MeterKind==Kind.Armor)tint=cell=='h'?new Color(.84f,.96f,1):new Color(.48f,.72f,.84f);
                else tint=cell=='h'?new Color(1,.80f,.40f):new Color(.86f,.47f,.16f);
                float left=rect.xMin+icon*step+(step-size)*.5f+x*pixel,top=rect.yMax-(rect.height-size)*.5f-y*pixel;
                int first=mesh.currentVertCount;
                mesh.AddVert(new Vector3(left,top-pixel),tint,Vector2.zero);
                mesh.AddVert(new Vector3(left,top),tint,Vector2.zero);
                mesh.AddVert(new Vector3(left+pixel,top),tint,Vector2.zero);
                mesh.AddVert(new Vector3(left+pixel,top-pixel),tint,Vector2.zero);
                mesh.AddTriangle(first,first+1,first+2);mesh.AddTriangle(first+2,first+3,first);
            }
        }
    }
}
