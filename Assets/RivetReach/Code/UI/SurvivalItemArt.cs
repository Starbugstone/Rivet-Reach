using UnityEngine;

namespace RivetReach
{
    // Small original silhouettes, generated once per UI. Gameplay identity comes from definitions.
    public static class SurvivalItemArt
    {
        public static Texture2D Icon(ItemDefinition item)
        {
            const int size=48;var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float px=x-24,py=y-24;bool body=false,handle=false;Color color=item.colour;
                var tool=item.toolCapabilities;
                if(tool!=ToolCapability.None)
                {
                    handle=Mathf.Abs(px)<2.4f&&py>-20&&py<11;
                    if((tool&ToolCapability.Pickaxe)!=0)body=Mathf.Abs(px)<20&&Mathf.Abs(py-(13-Mathf.Abs(px)*.25f))<3.5f;
                    else if((tool&ToolCapability.Axe)!=0)body=px>-15&&px<7&&py>6&&py<20&&px+py>0;
                    else if((tool&ToolCapability.Shovel)!=0)body=Mathf.Abs(px)<9&&py>3&&py<20&&Mathf.Abs(px)+py<26;
                    else if((tool&ToolCapability.Hoe)!=0)body=px>-15&&px<4&&py>12&&py<19||px<-10&&px>-16&&py>5&&py<14;
                    else body=Mathf.Abs(px)<(py>13?(22-py)*.65f:5)&&py>-1&&py<22||Mathf.Abs(px)<11&&py>-4&&py<0;
                }
                else if(item.armorSlot!=ArmorSlot.None)
                {
                    if(item.armorSlot==ArmorSlot.Head)body=Mathf.Abs(px)<16&&py>-9&&py<16&&!(Mathf.Abs(px)<10&&py<6);
                    if(item.armorSlot==ArmorSlot.Chest)body=Mathf.Abs(px)<(py>5?20:13)&&py>-18&&py<18&&!(Mathf.Abs(px)<6&&py>10);
                    if(item.armorSlot==ArmorSlot.Legs)body=Mathf.Abs(px)<14&&py>-20&&py<19&&!(Mathf.Abs(px)<4&&py<9);
                    if(item.armorSlot==ArmorSlot.Feet)body=Mathf.Abs(px)>3&&Mathf.Abs(px)<(py<0?20:15)&&py>-13&&py<13;
                }
                else if(Fluids.IsBucket(item.runtimeId))
                {
                    body=py>-17&&py<9&&Mathf.Abs(px)<12+(py+17)*.16f;
                    handle=Mathf.Abs(px*px+(py-10)*(py-10)-180)<36&&py>8;
                    if(body&&py>3)color=item.runtimeId==Fluids.EmptyBucket?new Color(.14f,.2f,.24f):new Color(.1f,.6f,.85f);
                }
                else if(item.runtimeId==BlockId.Torch)
                {
                    handle=Mathf.Abs(px)<3&&py>-20&&py<7;
                    body=py>3&&py<22&&Mathf.Abs(px)<(22-py)*.43f;
                    color=Mathf.Abs(px)<3&&py<13?new Color(1,1,.66f):new Color(1,.48f,.08f);
                }
                else if(item.runtimeId==BlockId.Stick)body=Mathf.Abs(px-py*.4f)<3&&py>-20&&py<20;
                else if(item.foodPoints>0)
                {body=px*px/330+py*py/210<1;color*=((x*7+y*13)%29<3?.72f:1);if(item.runtimeId==BlockId.BakedPotato&&Mathf.Abs(py)<3&&Mathf.Abs(px)<13)color=new Color(1,.84f,.44f);}
                else if(item.runtimeId==BlockId.Charcoal)body=Mathf.Abs(px)<17&&Mathf.Abs(py)<13&&Mathf.Abs(px)+Mathf.Abs(py)<24;
                else body=Mathf.Abs(px)<19&&py>-11&&py<12&&px+py<25&&px-py>-25;
                if(body||handle)pixels[x+y*size]=(body?color:new Color(.42f,.26f,.11f))*(py>px*.3f?1.12f:.76f);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=item.displayName+" icon",filterMode=FilterMode.Point};texture.SetPixels(pixels);texture.Apply();return texture;
        }
    }
}
