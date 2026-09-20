using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach.Editor
{
    public static class IndustryMaterialVariantChecks
    {
        public static void Run()
        {
            var lines=new List<string>();
            void Check(bool value,string message){if(!value)throw new Exception("Industry materials: "+message);lines.Add("PASS "+message);}
            var workshop=Resources.Load<Material>("Industry/Workshop");var indicator=Resources.Load<Material>("Industry/Status");
            var originalEmission=workshop.GetColor("_EmissionColor");var originalColour=indicator.GetColor("_BaseColor");
            // Reflection is confined to Editor verification of the private player helper.
            var type=typeof(IndustryPresentation).Assembly.GetType("RivetReach.IndustryMaterialVariants",true);
            var first=Activator.CreateInstance(type,new object[]{workshop,indicator});var second=Activator.CreateInstance(type,new object[]{workshop,indicator});
            Material Body(object owner,bool active)=>(Material)type.GetMethod("Body").Invoke(owner,new object[]{active});
            Material Status(object owner,MachineStatus state,bool running)=>(Material)type.GetMethod("Status").Invoke(owner,new object[]{state,running});
            int Count(object owner)=>(int)type.GetProperty("Count").GetValue(owner);
            var variants=new HashSet<Material>();var root=new GameObject("Shared material verification");
            try
            {
                Check(Resources.LoadAll<GameObject>("Industry/Runtime").SelectMany(prefab=>prefab.GetComponentsInChildren<Renderer>(true)).All(renderer=>renderer.sharedMaterials.Length==1),"Authored industrial renderers use one material slot, preserving whole-renderer override semantics");
                var idle=Body(first,false);var active=Body(first,true);variants.Add(idle);variants.Add(active);
                Check(idle.GetColor("_EmissionColor")==new Color(.12f,.12f,.12f)&&active.GetColor("_EmissionColor")==new Color(1.5f,1.5f,1.5f),"Body and pipe-fitting emission exactly preserve idle and active values");
                Check(Equivalent(workshop,idle,"_EmissionColor")&&Equivalent(workshop,active,"_EmissionColor"),"Body variants retain all other shader properties, textures, keywords and render settings");
                bool mapping=true,properties=true,reused=true;
                foreach(MachineStatus state in Enum.GetValues(typeof(MachineStatus)))foreach(bool running in new[]{false,true})
                {
                    var material=Status(first,state,running);variants.Add(material);
                    var expected=running?(state==MachineStatus.Underpowered?new Color(1,.68f,.12f):new Color(.24f,1,.61f)):state==MachineStatus.DisabledBySignal?new Color(.2f,.25f,.28f):state==MachineStatus.Ready?new Color(.30f,.52f,.62f):new Color(1,.31f,.08f);
                    mapping&=material.GetColor("_BaseColor")==expected;properties&=Equivalent(indicator,material,"_BaseColor");reused&=ReferenceEquals(material,Status(first,state,running));
                }
                Check(mapping,"Every status/running combination retains its original indicator colour");
                Check(properties,"Indicator variants retain all other shader properties, textures, keywords and render settings");
                Check(variants.Count==7&&Count(first)==7&&reused&&ReferenceEquals(idle,Body(first,false))&&ReferenceEquals(active,Body(first,true)),"All views and repeated state transitions share exactly seven stable session materials");
                var a=root.AddComponent<MeshRenderer>();var child=new GameObject("Second view");child.transform.SetParent(root.transform,false);var b=child.AddComponent<MeshRenderer>();
                var block=new MaterialPropertyBlock();block.SetColor("_EmissionColor",Color.magenta);a.SetPropertyBlock(block);b.SetPropertyBlock(block);
                var apply=type.GetMethod("Apply",BindingFlags.Public|BindingFlags.Static);apply.Invoke(null,new object[]{a,active});apply.Invoke(null,new object[]{b,active});
                Check(ReferenceEquals(a.sharedMaterial,b.sharedMaterial)&&ReferenceEquals(a.sharedMaterial,active)&&!a.HasPropertyBlock()&&!b.HasPropertyBlock(),"Renderer binding shares identity and clears the old per-renderer overrides");
                Check(!ReferenceEquals(idle,Body(second,false))&&Count(second)==7,"Independent sessions own separate finite palettes");
                Check(workshop.GetColor("_EmissionColor")==originalEmission&&indicator.GetColor("_BaseColor")==originalColour&&variants.All(m=>!ReferenceEquals(m,workshop)&&!ReferenceEquals(m,indicator)),"Resource material assets remain unchanged and are never used as mutable variants");
                UnityEngine.Object.DestroyImmediate(root);root=null;((IDisposable)first).Dispose();((IDisposable)first).Dispose();
                Check(Count(first)==0&&variants.All(m=>m==null)&&Body(second,false)!=null,"Disposal is idempotent, releases every owned material and preserves another session");
            }
            finally
            {
                if(root!=null)UnityEngine.Object.DestroyImmediate(root);((IDisposable)first).Dispose();((IDisposable)second).Dispose();
            }
            Directory.CreateDirectory("Logs/ReleaseReview");File.WriteAllLines("Logs/ReleaseReview/material-variant-checks.txt",new[]{"PASS "+lines.Count+" assertions"});File.AppendAllLines("Logs/ReleaseReview/material-variant-checks.txt",lines);
        }
        static bool Equivalent(Material source,Material variant,string changed)
        {
            if(source.shader!=variant.shader||source.renderQueue!=variant.renderQueue||source.enableInstancing!=variant.enableInstancing||source.doubleSidedGI!=variant.doubleSidedGI||source.globalIlluminationFlags!=variant.globalIlluminationFlags||!new HashSet<string>(source.shaderKeywords).SetEquals(variant.shaderKeywords))return false;
            for(int pass=0;pass<source.passCount;pass++)if(source.GetShaderPassEnabled(source.GetPassName(pass))!=variant.GetShaderPassEnabled(source.GetPassName(pass)))return false;
            var shader=source.shader;
            for(int i=0;i<shader.GetPropertyCount();i++)
            {
                string property=shader.GetPropertyName(i);if(property==changed)continue;
                switch(shader.GetPropertyType(i))
                {
                    case ShaderPropertyType.Color:if(source.GetColor(property)!=variant.GetColor(property))return false;break;
                    case ShaderPropertyType.Vector:if(source.GetVector(property)!=variant.GetVector(property))return false;break;
                    case ShaderPropertyType.Float:case ShaderPropertyType.Range:if(source.GetFloat(property)!=variant.GetFloat(property))return false;break;
                    case ShaderPropertyType.Int:if(source.GetInteger(property)!=variant.GetInteger(property))return false;break;
                    case ShaderPropertyType.Texture:if(source.GetTexture(property)!=variant.GetTexture(property)||source.GetTextureOffset(property)!=variant.GetTextureOffset(property)||source.GetTextureScale(property)!=variant.GetTextureScale(property))return false;break;
                    default:return false;
                }
            }
            return true;
        }
    }
}
