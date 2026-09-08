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
            if(tiles==null||tiles.width!=size)
            {
                tiles=new Texture2DArray(size,size,4,TextureFormat.RGBA32,true,false);
                // CreateAsset replaces the serialized content and preserves the existing .meta GUID.
                AssetDatabase.CreateAsset(tiles,path);
            }
            tiles.name="Terrain tiles v3";tiles.filterMode=FilterMode.Bilinear;tiles.anisoLevel=8;tiles.wrapMode=TextureWrapMode.Repeat;
            for(int layer=0;layer<4;layer++)
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
                    if(layer==3)
                    {
                        // Irregular mineral planes, without horizontal masonry courses.
                        float mineral=Mineral(x,y);
                        c=Color.Lerp(new Color(.43f,.46f,.47f),new Color(.56f,.57f,.55f),mineral*.65f+broad*.35f);
                        c*=.96f+small*.075f;
                        if(grain>.98f)c=Color.Lerp(c,new Color(.60f,.53f,.40f),.24f);
                    }
                    c*=.988f+grain*.024f;c.a=1;pixels[x+y*size]=c;
                }
                tiles.SetPixels(pixels,layer);
            }
            tiles.Apply(true,false);EditorUtility.SetDirty(tiles);return tiles;
        }
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
