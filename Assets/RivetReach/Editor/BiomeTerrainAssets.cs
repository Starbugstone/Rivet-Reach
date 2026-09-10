using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class BiomeTerrainAssets
    {
        public const int TileCount=45;
        static float Noise(int x,int y)
        {
            int gx=x/16,gy=y/16;
            double u=WorldNoise.Smooth(x%16/16.0),v=WorldNoise.Smooth(y%16/16.0);
            double H(int a,int b)=>TerrainGenerator.Hash(a%4,733,b%4,927)/(double)uint.MaxValue;
            return (float)WorldNoise.Blend(WorldNoise.Blend(H(gx,gy),H(gx+1,gy),u),WorldNoise.Blend(H(gx,gy+1),H(gx+1,gy+1),u),v);
        }
        public static void Items(ItemRegistry registry)
        {
            foreach(var item in new[]{
                new ItemDefinition{runtimeId=BlockId.Sand,stableId="rivet:sand",displayName="Sand",fistSeconds=.5f,colour=new Color(.77f,.66f,.39f)},
                new ItemDefinition{runtimeId=BlockId.Sandstone,stableId="rivet:sandstone",displayName="Sandstone",fistSeconds=.95f,colour=new Color(.65f,.49f,.29f)},
                new ItemDefinition{runtimeId=BlockId.Snow,stableId="rivet:snow",displayName="Snow",fistSeconds=.4f,colour=new Color(.85f,.90f,.94f)},
                new ItemDefinition{runtimeId=BlockId.RedClay,stableId="rivet:red_clay",displayName="Red clay",fistSeconds=.8f,colour=new Color(.64f,.31f,.19f)}})
                if(!registry.items.Any(i=>i.runtimeId==item.runtimeId))registry.items=registry.items.Append(item).ToArray();
        }
        public static void Tiles(Texture2DArray tiles)
        {
            int size=tiles.width;
            for(int layer=40;layer<=43;layer++)
            {
                var pixels=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float broad=Noise(x,y),grain=TerrainGenerator.Hash(x,927,y,331)%10000/9999f;
                    Color c;
                    if(layer==40)c=Color.Lerp(new Color(.69f,.56f,.31f),new Color(.86f,.75f,.49f),broad)*(.97f+grain*.06f);
                    else if(layer==41)
                    {
                        float strata=(Mathf.Sin(y*Mathf.PI/16+broad*.9f)+1)*.5f;
                        c=Color.Lerp(new Color(.54f,.39f,.22f),new Color(.76f,.60f,.37f),broad*.55f+strata*.45f);
                    }
                    else if(layer==42)c=Color.Lerp(new Color(.73f,.82f,.88f),new Color(.94f,.97f,.98f),broad*.7f+grain*.3f);
                    else
                    {
                        float stripe=(Mathf.Sin(y*Mathf.PI/16)+1)*.5f;
                        c=Color.Lerp(new Color(.49f,.22f,.14f),new Color(.72f,.38f,.22f),broad*.65f+stripe*.35f);
                    }
                    c.a=1;pixels[x+y*size]=c;
                }
                tiles.SetPixels(pixels,layer);
            }
        }
    }
}
