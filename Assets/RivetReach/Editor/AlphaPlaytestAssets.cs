using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class AlphaPlaytestAssets
    {
        public static void Prepare()
        {
            var registry=ItemRegistry.Load();var items=registry.items.ToList();
            void Add(byte id,string key,string name,float seconds,Color colour)
            {
                var old=items.Find(i=>i.runtimeId==id);
                if(old!=null&&old.stableId!=key)throw new InvalidOperationException("Occupied alpha item ID "+id);
                if(old==null){old=new ItemDefinition{runtimeId=id,stableId=key};items.Add(old);}
                old.displayName=name;old.stackLimit=64;old.fistSeconds=seconds;old.colour=colour;
            }
            Add(BlockId.LavaRock,"rivet:lava_rock","Lava Rock",30,new Color(.15f,.12f,.22f));
            Add(BlockId.MobSpawner,"rivet:mob_spawner","Mob Spawner",4,new Color(.2f,.23f,.24f));
            registry.items=items.ToArray();registry.InvalidateIndex();EditorUtility.SetDirty(registry);
            var tiles=Resources.Load<Texture2DArray>("Materials/BlockTiles");LavaRockTile(tiles);
            var detail=Resources.Load<Texture2DArray>("Materials/BlockDetail");
            var pixels=tiles.GetPixels(45);var normals=new Color[pixels.Length];int size=tiles.width;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float H(int a,int b)=>pixels[(a+size)%size+(b+size)%size*size].grayscale*.2f;
                var n=new Vector3(H(x-1,y)-H(x+1,y),H(x,y-1)-H(x,y+1),1).normalized;
                normals[x+y*size]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,.38f);
            }
            detail.SetPixels(normals,45);detail.Apply(true,false);EditorUtility.SetDirty(detail);
            const string folder="Assets/RivetReach/Resources/Spawners/";
            foreach(string name in new[]{"Palette","38"})
            {
                var texture=(TextureImporter)AssetImporter.GetAtPath(folder+name+".png");
                texture.alphaIsTransparency=name=="38";texture.mipmapEnabled=name=="Palette";
                texture.filterMode=name=="Palette"?FilterMode.Point:FilterMode.Bilinear;
                texture.textureCompression=TextureImporterCompression.Uncompressed;texture.SaveAndReimport();
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"Cage.mat");
            if(material==null){material=new Material(Shader.Find("RivetReach/WorldLit"));AssetDatabase.CreateAsset(material,folder+"Cage.mat");}
            material.SetTexture("_BaseMap",Resources.Load<Texture2D>("Spawners/Palette"));material.SetFloat("_Smoothness",.28f);material.SetFloat("_Metallic",.65f);EditorUtility.SetDirty(material);
            var importer=(ModelImporter)AssetImporter.GetAtPath(folder+"Cage.fbx");
            importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"SpawnerPalette"),material);importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
        }
        // Original deterministic chipped volcanic glass, tileable at every edge.
        // Only the reserved layer is modified; existing terrain artwork is preserved.
        public static void LavaRockTile(Texture2DArray tiles)
        {
            int size=tiles.width;var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float best=float.MaxValue,second=float.MaxValue,shade=0;
                for(int cy=-1;cy<=4;cy++)for(int cx=-1;cx<=4;cx++)
                {
                    uint hash=TerrainGenerator.Hash((cx+4)%4,29,(cy+4)%4,419);
                    float px=(cx+(hash%1000)/1000f)*size/4f,py=(cy+((hash>>10)%1000)/1000f)*size/4f;
                    float distance=(x-px)*(x-px)+(y-py)*(y-py);
                    if(distance<best){second=best;best=distance;shade=((hash>>20)%1000)/1000f;}
                    else if(distance<second)second=distance;
                }
                float edge=Mathf.Clamp01((Mathf.Sqrt(second)-Mathf.Sqrt(best))*2);
                float grain=(TerrainGenerator.Hash(x,73,y,419)%1000)/1000f;
                pixels[x+y*size]=Color.Lerp(new Color(.065f,.055f,.09f),new Color(.22f,.17f,.29f),(.2f+shade*.55f+grain*.1f)*edge);
            }
            tiles.SetPixels(pixels,45);tiles.Apply(true,false);EditorUtility.SetDirty(tiles);
        }
    }
}
