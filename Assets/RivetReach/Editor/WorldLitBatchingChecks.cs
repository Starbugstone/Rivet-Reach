using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach.Editor
{
    // Editor metadata only. Compatible buffers do not establish faster frames or
    // visual equivalence; native captures and timing remain separate acceptance.
    public static class WorldLitBatchingChecks
    {
        public static void Run()
        {
            var lines=new List<string>{"Unity "+Application.unityVersion+"; API "+SystemInfo.graphicsDeviceType+"; SRP Batcher enabled="+GraphicsSettings.useScriptableRenderPipelineBatching};
            var owned=new List<Material>();
            void Check(bool value,string message){lines.Add((value?"PASS ":"FAIL ")+message);if(!value)throw new InvalidOperationException("WorldLit batching: "+message);}
            try
            {
                var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RivetReach/Resources/Materials/WorldLit.shader");
                var source=Resources.Load<Material>("Industry/Workshop");
                Check(shader!=null&&source!=null&&source.shader==shader,"Actual Workshop references the project WorldLit shader");
                // These are the same native metadata APIs used by this Editor's
                // shader inspector. Reflection is confined to this explicit check.
                const BindingFlags flags=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
                var codeMethod=typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",flags,null,new[]{typeof(Shader),typeof(int)},null);
                var reasonMethod=typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason",flags,null,new[]{typeof(Shader),typeof(int),typeof(int)},null);
                Check(codeMethod!=null&&reasonMethod!=null,"Pinned Editor exposes SRP compatibility metadata; unavailable API means UNVERIFIED, never compatible");
                foreach(bool normalAndCutout in new[]{false,true})
                {
                    var material=new Material(source){name="WorldLit compatibility verification",hideFlags=HideFlags.HideAndDontSave,enableInstancing=false};owned.Add(material);
                    if(normalAndCutout){material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_ALPHATEST_ON");}
                    string variant=normalAndCutout?"authored material plus normal/cutout keywords":"authored Workshop keywords";
                    // A cold metadata lookup can be stale. Bind all four real passes
                    // first, as Unity's inspector binds before querying compatibility.
                    foreach(string name in new[]{"ForwardLit","ShadowCaster","DepthOnly","DepthNormals"})
                    {
                        int pass=material.FindPass(name);Check(pass>=0,"Required pass is retained: "+name+" / "+variant);
                        Check(material.SetPass(pass),"Pass compiles and binds: "+name+" / "+variant+" (no graphics device means UNVERIFIED)");
                    }
                    foreach(var message in ShaderUtil.GetShaderMessages(shader))lines.Add("Compiler "+message.severity+": "+message.message+" / "+message.file+":"+message.line);
                    Check(!ShaderUtil.ShaderHasError(shader),"Selected WorldLit passes have no reported compiler errors: "+variant);
                    int subshader=ShaderUtil.GetShaderData(shader).ActiveSubshaderIndex;
                    Check(subshader>=0,"Compiled shader has an active subshader: "+variant);
                    int code=(int)codeMethod.Invoke(null,new object[]{shader,subshader});
                    string reason=code==0?"compatible":(string)reasonMethod.Invoke(null,new object[]{shader,subshader,code});
                    Check(code==0,"Native SRP compatibility metadata: subshader="+subshader+", code="+code+", reason="+reason+" / "+variant);
                }
                lines.Add("PASS metadata checks only. Native renderer participation, visual equivalence and frame-time benefit remain unverified.");
            }
            catch(Exception error){lines.Add("FAIL exception: "+error);throw;}
            finally
            {
                foreach(var material in owned)if(material!=null)UnityEngine.Object.DestroyImmediate(material);
                Directory.CreateDirectory("Logs/ReleaseReview");File.WriteAllLines("Logs/ReleaseReview/worldlit-batching-checks.txt",lines);
            }
        }
    }
}
