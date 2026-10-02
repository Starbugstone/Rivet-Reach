using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    // Bake only material variant references so Unity retains the runtime-created
    // instancing/LOD variants in players. Models, maps and item icons stay original.
    public static class FactoryLodAssets
    {
        public static void Prepare()
        {
            const string directory="Assets/RivetReach/Resources/Materials/FactoryLod";Directory.CreateDirectory(directory);AssetDatabase.Refresh();
            var sources=new HashSet<Material>();
            foreach(var prefab in Resources.LoadAll<GameObject>("Industry/Runtime"))foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)
                if(material!=null&&material.shader.name=="RivetReach/WorldLit")sources.Add(material);
            foreach(string name in new[]{"Workshop","Status","TankGlass","BatteryCharge"})sources.Add(Resources.Load<Material>("Industry/"+name));
            sources.RemoveWhere(m=>m.shader.name!="RivetReach/WorldLit");
            foreach(var source in sources)foreach(bool far in new[]{false,true})
            {
                string path=directory+"/"+(far?"Far-":"Near-")+source.name+".mat";var target=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(target==null){target=new Material(source);AssetDatabase.CreateAsset(target,path);}else target.CopyPropertiesFromMaterial(source);
                target.shader=Resources.Load<Shader>("Materials/MachineLit");target.shaderKeywords=source.shaderKeywords;target.renderQueue=source.renderQueue;
                target.EnableKeyword(far?"RR_MACHINE_FAR":"RR_MACHINE_NEAR");target.enableInstancing=far;
                if(far){target.DisableKeyword("_NORMALMAP");target.DisableKeyword("_METALLICSPECGLOSSMAP");}
                EditorUtility.SetDirty(target);
            }
            AssetDatabase.SaveAssets();Debug.Log("Prepared factory distance material variants for "+sources.Count+" original materials");
        }
    }
}
