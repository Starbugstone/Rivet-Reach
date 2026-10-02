using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    // Import contract for the second-generation explorers (Tools/create_explorer_v2.py).
    // The first-generation assets keep their own settings in ProjectBuild.Prepare.
    public static class ExplorerV2Assets
    {
        const string Folder="Assets/RivetReach/Resources/Characters/V2/";

        public static void Prepare()
        {
            foreach(string name in new[]{"ExplorerMale","ExplorerFemale"})ConfigureExplorer(Folder+name+".fbx");
            Texture("SkinBase",TextureImporterType.Default,true);
            Texture("SkinSurface",TextureImporterType.Default,false);
            Texture("SkinNormal",TextureImporterType.NormalMap,false);
        }

        // Shared rig/clip contract: readable for first-person extraction, Generic animation,
        // uncompressed curves and loop flags derived from the authored clip names.
        public static void ConfigureExplorer(string path)
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            if(importer==null)throw new System.InvalidOperationException("Missing explorer model "+path);
            bool changed=!importer.isReadable||importer.materialImportMode!=ModelImporterMaterialImportMode.None||
                importer.animationType!=ModelImporterAnimationType.Generic||importer.optimizeGameObjects||
                !importer.importAnimation||importer.animationCompression!=ModelImporterAnimationCompression.Off;
            importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.animationType=ModelImporterAnimationType.Generic;importer.optimizeGameObjects=false;
            importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
            var clips=importer.defaultClipAnimations;
            foreach(var clip in clips)
            {
                clip.name=clip.takeName.Substring(clip.takeName.LastIndexOf('|')+1);
                clip.loopTime=!clip.name.Contains("Mine")&&clip.name!="Land"&&clip.name!="FP_Land";
                clip.lockRootRotation=clip.lockRootHeightY=clip.lockRootPositionXZ=true;
                clip.keepOriginalOrientation=clip.keepOriginalPositionY=clip.keepOriginalPositionXZ=true;
            }
            var current=importer.clipAnimations;
            if(current.Length!=clips.Length||current.Where((c,i)=>c.name!=clips[i].name||c.takeName!=clips[i].takeName||c.loopTime!=clips[i].loopTime||
                c.firstFrame!=clips[i].firstFrame||c.lastFrame!=clips[i].lastFrame||!c.lockRootHeightY||!c.lockRootPositionXZ||!c.lockRootRotation).Any())
            {importer.clipAnimations=clips;changed=true;}
            if(changed)importer.SaveAndReimport();
        }

        static void Texture(string name,TextureImporterType type,bool srgb)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+name+".png");
            if(importer==null)throw new System.InvalidOperationException("Missing explorer texture "+name);
            if(importer.textureType==type&&importer.sRGBTexture==srgb&&importer.textureCompression==TextureImporterCompression.CompressedHQ&&
               importer.mipmapEnabled&&importer.wrapMode==TextureWrapMode.Clamp&&importer.filterMode==FilterMode.Trilinear&&importer.maxTextureSize==2048)return;
            importer.textureType=type;importer.sRGBTexture=srgb;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;
            importer.maxTextureSize=2048;importer.alphaSource=TextureImporterAlphaSource.None;importer.SaveAndReimport();
        }
    }
}
