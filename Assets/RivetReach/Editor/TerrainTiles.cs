using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    // Original, repeatable 64-texel terrain swatches. No runtime texture generation.
    public static class TerrainTiles
    {
        public static Texture2DArray Build()
        {
            const string path="Assets/RivetReach/Resources/Materials/BlockTiles.asset";
            const int size=64;
            var tiles=AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
            const int layers=BiomeTerrainAssets.TileCount;
            if(tiles==null||tiles.width!=size||tiles.depth!=layers)
            {
                tiles=new Texture2DArray(size,size,layers,TextureFormat.RGBA32,true,false);
                // CreateAsset replaces the serialized content and preserves the existing .meta GUID.
                AssetDatabase.CreateAsset(tiles,path);
            }
            tiles.name="Terrain tiles v5 minerals";tiles.filterMode=FilterMode.Bilinear;tiles.anisoLevel=8;tiles.wrapMode=TextureWrapMode.Repeat;
            for(int layer=0;layer<layers;layer++)
            {
                var pixels=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float broad=Patch(x,y,8,layer),small=Patch(x,y,4,layer+8),grain=Hash(x,y,layer+17);
                    Color dirt=Color.Lerp(new Color(.34f,.245f,.16f),new Color(.49f,.365f,.245f),broad);
                    dirt*=.94f+small*.12f;
                    if(grain>.973f)dirt=Color.Lerp(dirt,new Color(.55f,.49f,.38f),.45f);
                    Color grass=Color.Lerp(new Color(.34f,.445f,.21f),new Color(.43f,.535f,.25f),broad);
                    grass*=.95f+small*.10f;
                    // Short clustered blade highlights, rather than uniformly noisy pixels.
                    if(Hash(x/2,y/5,31)>.72f&&x%2==0)grass*=1.045f;
                    Color c=dirt;
                    if(layer==0)c=grass;
                    if(layer==1)
                    {
                        int fringe=50+(int)(Hash(x/4,0,24)*7);
                        if(y>=fringe)c=grass*(.94f+.06f*(y-fringe)/14f);
                        else if(y>=fringe-2)c=dirt*.81f;
                    }
                    if(layer==3||layer>=7&&layer<=11)
                    {
                        // Irregular mineral planes, without horizontal masonry courses.
                        float mineral=Mineral(x,y);
                        c=Color.Lerp(new Color(.43f,.46f,.47f),new Color(.56f,.57f,.55f),mineral*.65f+broad*.35f);
                        c*=.96f+small*.075f;
                        if(grain>.98f)c=Color.Lerp(c,new Color(.60f,.53f,.40f),.24f);
                    }
                    if(layer==4)
                    {
                        float ridge=Patch(x,0,4,61),groove=Mathf.Pow((Mathf.Sin(x*.70f+small*2)+1)*.5f,8);
                        c=Color.Lerp(new Color(.24f,.145f,.075f),new Color(.46f,.32f,.17f),ridge)*(.86f+small*.24f);
                        c*=1-groove*.24f;
                    }
                    if(layer==5)
                    {
                        float dx=x-31.5f,dy=y-31.5f,r=Mathf.Sqrt(dx*dx+dy*dy);
                        float ring=(Mathf.Sin(r*.95f+small*1.4f)+1)*.5f;
                        c=Color.Lerp(new Color(.49f,.33f,.17f),new Color(.69f,.51f,.29f),ring*.6f+broad*.4f);
                        if(Mathf.Max(Mathf.Abs(dx),Mathf.Abs(dy))>28)c=new Color(.30f,.19f,.095f)*(.9f+small*.2f);
                    }
                    if(layer==6)
                    {
                        float cluster=Hash(x/8,y/8,71),vein=(x+y)%8==0?.84f:1;
                        c=Color.Lerp(new Color(.18f,.30f,.105f),new Color(.36f,.49f,.19f),cluster*.5f+small*.5f)*vein;
                    }
                    if(layer>=7&&layer<=11)
                    {
                        // Original angular inclusions embedded in the existing stone palette.
                        int cx=x/16,cy=y/16;float sx=cx*16+4+Hash(cx,cy,110+layer)*8,sy=cy*16+4+Hash(cx,cy,130+layer)*8;
                        float dx=x-sx,dy=y-sy,edge=Mathf.Abs(dx)+Mathf.Abs(dy)*1.15f;
                        float radius=4+Hash(cx,cy,150+layer)*3;
                        if(edge<radius+1)c*=.67f;
                        if(edge<radius)c=MineralColour(layer-7)*(.72f+small*.20f+(dx-dy>0?.27f:0));
                    }
                    if(layer==12)
                    {
                        float plane=Mineral(x,y),fracture=Patch(x,y,4,183);
                        c=Color.Lerp(new Color(.105f,.12f,.14f),new Color(.29f,.31f,.34f),plane)*(.9f+small*.2f);
                        if(fracture<.25f)c*=.60f;
                    }
                    if(layer>=13)c=MineralColour(layer-13)*(.70f+Mineral(x,y)*.32f+small*.16f);
                    c*=.988f+grain*.024f;c.a=1;pixels[x+y*size]=c;
                }
                tiles.SetPixels(pixels,layer);
            }
            SurvivalTerrainArt.Apply(tiles);
            BiomeTerrainAssets.Tiles(tiles);
            // Azure belongs to every build path, alongside the other finite ore swatches.
            var azure=tiles.GetPixels(3);
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                if((x*3+y*5)%23<3||Mathf.Abs((x%21)-(y%21))<2)azure[x+y*size]=new Color(.035f,.58f+(x%4)*.08f,.96f);
            tiles.SetPixels(azure,44);
            tiles.Apply(true,false);EditorUtility.SetDirty(tiles);return tiles;
        }
        static Color MineralColour(int index)=>index==0?new Color(.66f,.39f,.28f):index==1?new Color(.87f,.49f,.22f):index==2?new Color(.12f,.15f,.19f):index==3?new Color(.94f,.73f,.20f):new Color(.43f,.86f,.91f);
        static float Hash(int x,int y,int salt)=>TerrainGenerator.Hash(x,salt,y,953)%10000/9999f;
        static float Mineral(int x,int y)
        {
            float nearest=float.MaxValue,shade=0;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
            {
                int cx=x/16+dx,cy=y/16+dy,wrapX=(cx+4)%4,wrapY=(cy+4)%4;
                float sx=cx*16+4+Hash(wrapX,wrapY,41)*8,sy=cy*16+4+Hash(wrapX,wrapY,47)*8;
                float distance=(x-sx)*(x-sx)+(y-sy)*(y-sy);
                if(distance<nearest){nearest=distance;shade=Hash(wrapX,wrapY,53);}
            }
            return shade;
        }
        static float Patch(int x,int y,int cell,int salt)
        {
            int ix=x/cell,iy=y/cell,period=64/cell;
            float u=(x%cell)/(float)cell,v=(y%cell)/(float)cell;
            u=u*u*(3-2*u);v=v*v*(3-2*v);
            float Sample(int a,int b)=>Hash(a%period,b%period,salt);
            return Mathf.Lerp(Mathf.Lerp(Sample(ix,iy),Sample(ix+1,iy),u),Mathf.Lerp(Sample(ix,iy+1),Sample(ix+1,iy+1),u),v);
        }
    }
}
