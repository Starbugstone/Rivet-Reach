using UnityEngine;

namespace RivetReach.Editor
{
    // Original code-authored swatches. Reserved survival layers18..33; other kits retain theirs.
    public static class SurvivalTerrainArt
    {
        public static void Apply(Texture2DArray tiles)
        {
            int size=tiles.width;
            for(int layer=18;layer<=33;layer++)
            {
                var pixels=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float grain=.91f+(TerrainGenerator.Hash(x,layer,y,789)%100)/650f;
                    int board=y/16;bool joint=y%16<2||(x+(board%2)*32)%64<2;
                    Color wood=new Color(.64f,.42f,.21f)*grain*(joint?.53f:1);
                    int row=y/16,edge=(x+(row%2)*16)%32;
                    Color stone=new Color(.43f,.49f,.54f)*grain*(y%16<2||edge<2?.58f:1);
                    Color colour=wood;
                    if(layer==19)colour=stone;
                    if(layer==20)
                    {colour=wood;if(x<5||x>58||y<5||y>58)colour*=.58f;else if(x%18<2||y%18<2)colour*=.65f;}
                    if(layer==21)
                    {colour=wood;if(x<7||x>56)colour*=.53f;if(y>49)colour*=.65f;if(y>18&&y<41&&x>17&&x<45)colour=new Color(.22f,.15f,.10f);}
                    if(layer==22)
                    {colour=stone;if(x>10&&x<53&&y>10&&y<40)colour=new Color(.075f,.08f,.085f);if(y>44&&y<51&&x>9&&x<54)colour*=.55f;}
                    if(layer==23)
                    {colour=wood;if(y<5||y>58||x<5||x>58||y>37&&y<42)colour*=.40f;if(x>27&&x<36&&y>26&&y<46)colour=new Color(.85f,.68f,.27f);}
                    if(layer==24)colour=new Color(.32f,.215f,.115f)*grain*(x%12<3?.66f:1);
                    if(layer>=25&&layer<=28)
                    {colour=new Color(.23f,.48f,.14f)*grain;if((x+y)%13<2)colour*=1.3f;if(layer==28&&x%21<5&&y%21<5)colour=new Color(.87f,.78f,.70f);}
                    if(layer>=29)
                    {
                        Color[] metals={new Color(.12f,.15f,.18f),new Color(.70f,.76f,.80f),new Color(.82f,.42f,.21f),new Color(.93f,.69f,.16f),new Color(.29f,.77f,.79f)};
                        colour=metals[layer-29]*grain*(x<3||y<3?.65f:x>60||y>60?1.15f:1);
                    }
                    colour.a=1;pixels[x+y*size]=colour;
                }
                tiles.SetPixels(pixels,layer);
            }
        }
    }
}
