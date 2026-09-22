using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RivetReach.Editor
{
    // Explicit isolated-project check against shaders staged from Git by
    // Tools/prepare_shader_comparison.py. Reference assets are never shipped.
    [InitializeOnLoad]
    public static class ShaderEquivalenceChecks
    {
        static double nextPoll;
        static ShaderEquivalenceChecks(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Logs/shader-comparison-request.txt";
            if(EditorApplication.timeSinceStartup<nextPoll)return;
            nextPoll=EditorApplication.timeSinceStartup+2;
            if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||BuildPipeline.isBuildingPlayer)return;
            File.Delete(request);
            try{Run();File.WriteAllText("Logs/shader-comparison-result.txt","PASS");}
            catch(Exception e){File.WriteAllText("Logs/shader-comparison-result.txt","FAIL "+e);Debug.LogException(e);}
        }
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||Shader.GetGlobalFloat("_RRLightEnabled")!=0)
                throw new InvalidOperationException("Shader comparison requires an idle isolated project without a live light field.");
            var lines=new List<string>();Directory.CreateDirectory("Logs/ShaderComparison");
            var table=new ComputeBuffer(1,16);var cells=new ComputeBuffer(8192,4);
            var oldFog=Shader.GetGlobalVector("_RRFogRange");var oldColour=Shader.GetGlobalVector("_RRFogColour");var oldHeld=Shader.GetGlobalVector("_RRHeldTorchAmbient");
            int oldMask=Shader.GetGlobalInt("_RRLightMask");bool asyncCompilation=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            try
            {
                table.SetData(new[]{new Vector4Int(0,0,0,1)});
                Shader.SetGlobalBuffer("_RRLightTable",table);Shader.SetGlobalBuffer("_RRLightCells",cells);Shader.SetGlobalInt("_RRLightMask",0);
                Shader.SetGlobalVector("_RRFogColour",new Vector4(.36f,.48f,.62f,1));
                foreach(string name in new[]{"WorldLit","HeldTool","HeldBlock","ExplorerSkin"})
                {
                    var current=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RivetReach/Resources/Materials/"+name+".shader");
                    var reference=Resources.Load<Shader>("Verification/"+name);
                    if(current==null||reference==null)throw new InvalidOperationException("Missing current/reference shader: "+name);
                    var material=name=="WorldLit"?new Material(Resources.Load<Material>("Industry/Workshop")):new Material(current);
                    material.shader=current;if(material.HasProperty("_FirstPerson"))material.SetFloat("_FirstPerson",0);
                    if(material.HasProperty("_Tiles"))material.SetTexture("_Tiles",Resources.Load<Texture2DArray>("Materials/BlockTiles"));
                    var preview=new PreviewRenderUtility();
                    try
                    {
                        var root=GameObject.CreatePrimitive(PrimitiveType.Sphere);root.transform.position=new Vector3(16,16,16);root.transform.localScale=Vector3.one*2;
                        root.GetComponent<Renderer>().sharedMaterial=material;preview.AddSingleGO(root);
                        var camera=preview.camera;camera.transform.position=new Vector3(19,18,21);camera.transform.LookAt(root.transform.position);
                        camera.nearClipPlane=.1f;camera.farClipPlane=50;camera.fieldOfView=35;
                        preview.lights[0].transform.rotation=Quaternion.Euler(40,30,0);preview.lights[1].intensity=.2f;
                        for(int state=0;state<(name=="WorldLit"?10:8);state++)
                        {
                            // Outdoor/day, dark cave/night, mixed sky/block, carried fill,
                            // partial/full/no fog, plus the disabled preview-light path.
                            if(name=="WorldLit"&&state>=8){material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",10);material.SetFloat("_ZWrite",0);material.SetColor("_BaseColor",new Color(.4f,.7f,.8f,.35f));material.renderQueue=3000;}
                            var packed=new uint[8192];
                            for(int i=0;i<32768;i++)
                            {
                                int value=state==0?240:state==1?0:(((i%32+2*(i/32%32))%16)<<4)|((i/1024+i%32)%16);
                                packed[i/4]|=(uint)value<<((i%4)*8);
                            }
                            cells.SetData(packed);Shader.SetGlobalFloat("_RRLightEnabled",state==7?0:1);
                            Shader.SetGlobalVector("_RRHeldTorchAmbient",state==3?new Vector4(16,17,18,1):Vector4.zero);
                            Shader.SetGlobalVector("_RRFogRange",(state==4||state==8)?new Vector4(4,8,0,0):state==5?new Vector4(1,2,0,0):new Vector4(100,200,0,0));
                            preview.lights[0].intensity=state==1?.05f:2;
                            if(name=="WorldLit"&&state==6){material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(.3f,.05f,.02f));}
                            var variantKeywords=material.shaderKeywords;
                            Color32[] Render(Shader shader,string suffix)
                            {
                                material.shader=shader;material.shaderKeywords=variantKeywords;for(int pass=0;pass<material.passCount;pass++)material.SetPass(pass);preview.BeginPreview(new Rect(0,0,256,256),GUIStyle.none);
                                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.04f,.05f,1);preview.Render(true,false);
                                var target=(RenderTexture)preview.EndPreview();var old=RenderTexture.active;RenderTexture.active=target;
                                var image=new Texture2D(256,256,TextureFormat.RGBA32,false);
                                try{image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();var result=image.GetPixels32();if(name=="WorldLit")File.WriteAllBytes("Logs/ShaderComparison/"+name+"-"+state+"-"+suffix+".png",image.EncodeToPNG());return result;}
                                finally{RenderTexture.active=old;Object.DestroyImmediate(image);}
                            }
                            var a=Render(reference,"reference");var b=Render(current,"current");int max=0,changed=0;var colours=new HashSet<Color32>();
                            for(int i=0;i<a.Length;i++){int d=Math.Max(Math.Abs(a[i].r-b[i].r),Math.Max(Math.Abs(a[i].g-b[i].g),Math.Abs(a[i].b-b[i].b)));max=Math.Max(max,d);if(d>0)changed++;colours.Add(a[i]);}
                            bool ok=max<=1&&colours.Count>(state==0?32:1)&&!ShaderUtil.ShaderHasError(current)&&!ShaderUtil.ShaderHasError(reference);
                            lines.Add($"{(ok?"PASS":"FAIL")} {name} state {state}: max channel delta {max}/255; changed pixels {changed}/{a.Length}; reference colours {colours.Count}");
                            if(!ok)throw new InvalidOperationException(lines[lines.Count-1]);
                        }
                    }
                    finally{preview.Cleanup();Object.DestroyImmediate(material);}
                }
            }
            finally
            {
                Shader.SetGlobalFloat("_RRLightEnabled",0);Shader.SetGlobalInt("_RRLightMask",oldMask);Shader.SetGlobalVector("_RRFogRange",oldFog);Shader.SetGlobalVector("_RRFogColour",oldColour);Shader.SetGlobalVector("_RRHeldTorchAmbient",oldHeld);
                ShaderUtil.allowAsyncCompilation=asyncCompilation;table.Dispose();cells.Dispose();File.WriteAllLines("Logs/ShaderComparison/results.txt",lines);
            }
        }
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        struct Vector4Int
        {
            public int X,Y,Z,W;
            public Vector4Int(int x,int y,int z,int w){X=x;Y=y;Z=z;W=w;}
        }
    }
}
