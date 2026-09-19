using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RivetReach.Editor
{
    // Bake the actual terrain plant geometry, using one shared framing for all growth stages.
    public static class CropIconBuild
    {
        public static void Bake()
        {
            var atlas=Resources.Load<Texture2DArray>("Materials/BlockTiles");
            for(byte id=BlockId.PotatoPlant;id<=BlockId.MaturePotatoPlant;id++)
            {
                var cells=new byte[34*34*34];cells[ChunkMesher.Index(0,0,0)]=id;
                var built=ChunkMesher.Build(default,0,cells);var mesh=new Mesh();
                mesh.vertices=built.Vertices;mesh.normals=built.Normals;mesh.uv=built.UV;mesh.triangles=built.Triangles;mesh.RecalculateBounds();
                var texture=new Texture2D(atlas.width,atlas.height,TextureFormat.RGBA32,false);texture.SetPixels(atlas.GetPixels(BlockId.Tile(id,1,1)));texture.Apply();
                var material=new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/RivetReach/Editor/ItemIcon.shader"));material.SetTexture("_BaseMap",texture);
                var preview=new PreviewRenderUtility();
                try
                {
                    var root=new GameObject("Potato growth "+(id-BlockId.PotatoPlant));root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=material;preview.AddSingleGO(root);
                    var camera=preview.camera;camera.orthographic=true;camera.orthographicSize=.58f;camera.nearClipPlane=.01f;camera.farClipPlane=20;
                    var target=new Vector3(.5f,.35f,.5f);camera.transform.position=target+new Vector3(1,.8f,-2)*2;camera.transform.LookAt(target);
                    preview.BeginPreview(new Rect(0,0,192,192),GUIStyle.none);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;preview.Render(true,false);
                    var rendered=(RenderTexture)preview.EndPreview();var active=RenderTexture.active;RenderTexture.active=rendered;
                    var icon=new Texture2D(192,192,TextureFormat.RGBA32,false);icon.ReadPixels(new Rect(0,0,192,192),0,0);icon.Apply();RenderTexture.active=active;
                    if(QualitySettings.activeColorSpace==ColorSpace.Linear&&!rendered.sRGB){var colors=icon.GetPixels();for(int n=0;n<colors.Length;n++)colors[n]=colors[n].gamma;icon.SetPixels(colors);icon.Apply();}
                    string path="Assets/RivetReach/Resources/Farming/"+id+"Icon.png";File.WriteAllBytes(path,icon.EncodeToPNG());Object.DestroyImmediate(icon);AssetDatabase.ImportAsset(path);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                }
                finally{preview.Cleanup();Object.DestroyImmediate(mesh);Object.DestroyImmediate(material);Object.DestroyImmediate(texture);}
            }
        }
    }
}
