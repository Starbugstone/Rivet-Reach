using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RivetReach.Editor
{
    public static class TorchAssets
    {
        // Creates the initial authored light prefab only; subsequent tuning is preserved.
        public static void Prepare()
        {
            const string path="Assets/RivetReach/Resources/TorchLight.prefab";
            if(File.Exists(path))return;
            var source=new GameObject("Torch light");
            try
            {
                var light=source.AddComponent<Light>();light.type=LightType.Point;light.range=TorchPresentation.LightRange;
                light.color=new Color(1,.57f,.22f);light.intensity=3;light.shadows=LightShadows.Hard;
                light.shadowBias=.025f;light.shadowNormalBias=.12f;light.shadowNearPlane=.05f;light.enabled=false;
                var data=light.GetUniversalAdditionalLightData();data.usePipelineSettings=false;
                var serialized=new SerializedObject(data);
                serialized.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(source,path);
            }
            finally{Object.DestroyImmediate(source);}
        }
    }
}
