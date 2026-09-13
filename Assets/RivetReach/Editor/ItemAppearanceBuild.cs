using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RivetReach.Editor
{
    public static class ItemAppearanceBuild
    {
        [MenuItem("Rivet Reach/Bake held item icons")]
        public static void Bake()=>BakeOnly(0);
        public static void BakeOnly(byte only)
        {
            const string folder="Assets/RivetReach/Resources/ItemIcons";
            Directory.CreateDirectory(folder);
            foreach(var item in ItemRegistry.Load().items.Where(item=>ItemAppearance.BakedIcon(item)&&(only==0||item.runtimeId==only)))
            {
                var preview=new PreviewRenderUtility();Material material=null,waterMaterial=null;
                try
                {
                    bool axe=(item.toolCapabilities&ToolCapability.Axe)!=0,torch=item.runtimeId==BlockId.Torch,bucket=Fluids.IsBucket(item.runtimeId);
                    string path=bucket?"Characters/PalmBucket":torch?"Characters/GripTorch":ItemAppearance.ToolPath(item.toolCapabilities);
                    var model=Object.Instantiate(Resources.Load<GameObject>(path));preview.AddSingleGO(model);
                    material=new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/RivetReach/Editor/ItemIcon.shader"));
                    material.SetFloat("_Torch",torch?1:0);
                    material.SetTexture("_BaseMap",Resources.Load<Texture2D>(axe?"Tools/StarterAxe":"Characters/SkinField"));
                    material.SetColor("_BaseColor",ItemAppearance.ToolTint(item));
                    foreach(var renderer in model.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=material;
                    if(bucket&&item.runtimeId!=Fluids.EmptyBucket)
                    {
                        var water=GameObject.CreatePrimitive(PrimitiveType.Cylinder);preview.AddSingleGO(water);
                        water.transform.localPosition=new Vector3(0,.32f,0);water.transform.localScale=new Vector3(.80f,.008f,.80f);
                        waterMaterial=new Material(material);waterMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);
                        var liquid=Fluids.Registry.FromBucket(item.runtimeId);
                        waterMaterial.SetColor("_BaseColor",new Color(liquid.Red,liquid.Green,liquid.Blue));water.GetComponent<Renderer>().sharedMaterial=waterMaterial;
                    }
                    var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
                    foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                    var camera=preview.camera;camera.orthographic=true;camera.nearClipPlane=.001f;camera.farClipPlane=100;
                    camera.transform.position=bounds.center+(bucket?new Vector3(1.3f,1.1f,-2):new Vector3(.25f,.16f,-2)).normalized*bounds.size.magnitude*3;
                    camera.transform.LookAt(bounds.center);
                    float span=0;
                    foreach(var renderer in renderers)
                    {
                        var b=renderer.bounds;
                        for(int i=0;i<8;i++)
                        {
                            var p=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                            p=camera.transform.InverseTransformPoint(p);span=Mathf.Max(span,Mathf.Abs(p.x),Mathf.Abs(p.y));
                        }
                    }
                    camera.orthographicSize=span*1.13f;
                    preview.ambientColor=new Color(.42f,.42f,.42f);
                    preview.lights[0].intensity=1.6f;preview.lights[0].transform.rotation=Quaternion.Euler(35,-30,0);
                    preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(320,135,0);
                    preview.BeginPreview(new Rect(0,0,128,128),GUIStyle.none);
                    camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
                    preview.Render(true,false);var rendered=(RenderTexture)preview.EndPreview();
                    var previous=RenderTexture.active;RenderTexture.active=rendered;
                    var icon=new Texture2D(rendered.width,rendered.height,TextureFormat.RGBA32,false);
                    icon.ReadPixels(new Rect(0,0,rendered.width,rendered.height),0,0);icon.Apply();RenderTexture.active=previous;
                    // PreviewRenderUtility uses a linear HDR target; PNG consumers expect sRGB.
                    if(QualitySettings.activeColorSpace==ColorSpace.Linear&&!rendered.sRGB)
                    {var colours=icon.GetPixels();for(int i=0;i<colours.Length;i++)colours[i]=colours[i].gamma;icon.SetPixels(colours);icon.Apply();}
                    var pixels=icon.GetPixels32();
                    if(pixels.Count(c=>c.a>127)<50)throw new Exception("Empty model icon: "+item.displayName);
                    if(pixels.Count(c=>c.a>127&&Math.Max(c.r,Math.Max(c.g,c.b))>=64)<50)
                        throw new Exception("Unreadably dark model icon: "+item.displayName);
                    if(pixels[0].a!=0||pixels[rendered.width-1].a!=0||pixels[pixels.Length-1].a!=0||pixels[pixels.Length-rendered.width].a!=0)
                        throw new Exception("Model icon needs transparent margins: "+item.displayName);
                    File.WriteAllBytes(folder+"/"+item.runtimeId+".png",icon.EncodeToPNG());Object.DestroyImmediate(icon);
                }
                finally{preview.Cleanup();if(material!=null)Object.DestroyImmediate(material);if(waterMaterial!=null)Object.DestroyImmediate(waterMaterial);}
            }
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles(folder,"*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
        }
        public static void Run()
        {
            Bake();WikiExport.Export();Directory.CreateDirectory("Logs/ItemAppearance");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                locationPathName="Builds/ItemAppearance/RivetReach.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development
            });
            File.WriteAllText("Logs/ItemAppearance/build.txt",$"{result.summary.result}; errors {result.summary.totalErrors}; warnings {result.summary.totalWarnings}\n");
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Item appearance build failed");
        }
    }
}
