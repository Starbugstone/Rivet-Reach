using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RivetReach.Editor
{
    // Original arcade palette/detail pass. Preserve additional resource layers supplied by other kits.
    public static class ArcadeTerrainArt
    {
        static float Ramp(float a,float b,float value)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,value));
        static float Noise(float x,float y,float scale,int seed)
        {
            float px=x/scale,py=y/scale;int ix=Mathf.FloorToInt(px),iy=Mathf.FloorToInt(py),period=Mathf.RoundToInt(64/scale);
            float u=Mathf.SmoothStep(0,1,px-ix),v=Mathf.SmoothStep(0,1,py-iy);
            float H(int a,int b)=>TerrainGenerator.Hash((a%period+period)%period,seed,(b%period+period)%period,831)%10000/9999f;
            return Mathf.Lerp(Mathf.Lerp(H(ix,iy),H(ix+1,iy),u),Mathf.Lerp(H(ix,iy+1),H(ix+1,iy+1),u),v);
        }
        static Color Swatch(int layer,int x,int y,out float height)
        {
            float broad=Noise(x,y,16,3),small=Noise(x,y,4,18),grain=Noise(x,y,2,44);
            Color soil=Color.Lerp(new Color(.38f,.255f,.15f),new Color(.50f,.35f,.23f),broad);
            float pebble=Ramp(.66f,.78f,small)*Ramp(.38f,.63f,broad);
            soil=Color.Lerp(soil,new Color(.67f,.48f,.29f),pebble*.20f);soil*=.98f+grain*.04f;
            Color grass=Color.Lerp(new Color(.16f,.38f,.17f),new Color(.33f,.52f,.24f),broad);
            float blade=Mathf.Pow(Mathf.Max(0,Mathf.Sin(x*.55f+y*.21f+small*5)),12)*.045f;
            grass*=.97f+small*.07f+blade;height=.35f+small*.12f+pebble*.20f;
            if(layer==0){height=small*.07f+blade;return grass;}
            if(layer==1)
            {
                float edge=49+Noise(x,0,8,26)*7;
                float fringe=Ramp(edge-1,edge+1,y);
                height=Mathf.Lerp(height,.55f+small*.08f,fringe);
                return Color.Lerp(soil*(1-.22f*Mathf.Exp(-Mathf.Pow((y-edge+2)/2,2))),grass,fringe);
            }
            if(layer==2)return soil;
            if(layer==3)
            {
                float planes=Mathf.Floor(Noise(x+y*.18f,y,8,64)*5)/5;
                height=.2f+planes*.25f+grain*.025f;
                return Color.Lerp(new Color(.32f,.40f,.48f),new Color(.57f,.63f,.64f),planes*.65f+broad*.35f);
            }
            if(layer==4)
            {
                float groove=Mathf.Pow((Mathf.Sin(x*Mathf.PI/8+Noise(x,y,16,48)*2)+1)*.5f,6);
                height=.35f-groove*.22f+small*.04f;
                return Color.Lerp(new Color(.30f,.145f,.065f),new Color(.58f,.335f,.13f),Noise(x,0,8,21))*(1-groove*.35f);
            }
            if(layer==5)
            {
                float r=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f));
                float ring=(Mathf.Sin(r*.83f+small*.9f)+1)*.5f;height=ring*.05f;
                return Mathf.Max(Mathf.Abs(x-31.5f),Mathf.Abs(y-31.5f))>28?new Color(.30f,.16f,.065f):Color.Lerp(new Color(.57f,.35f,.15f),new Color(.78f,.58f,.29f),ring*.35f+broad*.65f);
            }
            float cluster=Mathf.Floor(Noise(x,y,8,79)*6)/6;
            height=cluster*.18f+small*.04f;
            return Color.Lerp(new Color(.10f,.31f,.14f),new Color(.34f,.59f,.20f),cluster*.7f+broad*.3f);
        }
        public static void Apply(Texture2DArray tiles,Material material,VolumeProfile profile)
        {
            const int size=64;const string detailPath="Assets/RivetReach/Resources/Materials/BlockDetail.asset";
            var detail=AssetDatabase.LoadAssetAtPath<Texture2DArray>(detailPath);
            if(detail==null||detail.depth!=tiles.depth)
            {detail=new Texture2DArray(size,size,tiles.depth,TextureFormat.RGBA32,true,true);AssetDatabase.CreateAsset(detail,detailPath);}
            detail.name="Arcade terrain normal and roughness";detail.filterMode=FilterMode.Trilinear;detail.wrapMode=TextureWrapMode.Repeat;detail.anisoLevel=8;
            for(int layer=0;layer<tiles.depth;layer++)
            {
                var colours=tiles.GetPixels(layer);var heights=new float[size*size];var normals=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    int k=x+y*size;
                    if(layer<7)colours[k]=Swatch(layer,x,y,out heights[k]);
                    else heights[k]=colours[k].grayscale*.06f;
                }
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float H(int a,int b)=>heights[(a+size)%size+(b+size)%size*size];
                    var normal=new Vector3((H(x-1,y)-H(x+1,y))*1.1f,(H(x,y-1)-H(x,y+1))*1.1f,1).normalized;
                    normals[x+y*size]=new Color(normal.x*.5f+.5f,normal.y*.5f+.5f,normal.z*.5f+.5f,layer==3?.73f:.88f);
                }
                if(layer<7)tiles.SetPixels(colours,layer);detail.SetPixels(normals,layer);
            }
            tiles.name="Arcade terrain tiles";tiles.Apply(true,false);detail.Apply(true,false);
            material.SetTexture("_DetailTiles",detail);EditorUtility.SetDirty(tiles);EditorUtility.SetDirty(detail);EditorUtility.SetDirty(material);
            if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.28f);bloom.threshold.Override(1.05f);bloom.scatter.Override(.62f);EditorUtility.SetDirty(bloom);}
            if(profile.TryGet<ColorAdjustments>(out var colour)){colour.postExposure.Override(.18f);colour.contrast.Override(10);colour.saturation.Override(8);EditorUtility.SetDirty(colour);}
            if(profile.TryGet<Vignette>(out var vignette)){vignette.intensity.Override(.12f);vignette.smoothness.Override(.55f);EditorUtility.SetDirty(vignette);}
            EditorUtility.SetDirty(profile);
            var importer=AssetImporter.GetAtPath("Assets/RivetReach/Resources/Effects/ArcadeChips.fbx") as ModelImporter;
            if(importer!=null&&(importer.materialImportMode!=ModelImporterMaterialImportMode.None||importer.importAnimation||!importer.isReadable))
            {importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=false;importer.isReadable=true;importer.SaveAndReimport();}
        }
    }
}
