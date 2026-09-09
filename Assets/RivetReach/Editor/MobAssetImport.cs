using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public sealed class MobAssetImport : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.StartsWith("Assets/RivetReach/Resources/Mobs/",StringComparison.Ordinal))return;
            var importer=(ModelImporter)assetImporter;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.isReadable=true;importer.useFileScale=true;
            importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=true;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression=ModelImporterAnimationCompression.Off;importer.optimizeGameObjects=false;
        }
        void OnPreprocessTexture()
        {
            if(!assetPath.EndsWith("Mobs/CreaturePalette.png",StringComparison.Ordinal))return;
            var importer=(TextureImporter)assetImporter;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
        }
        [MenuItem("Rivet Reach/Mobs/Prepare and inspect assets")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/RivetReach/Resources/Mobs/Definitions");
            foreach(string model in new[]{"RustbackBeetle","DuskProwler"})
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Mobs/"+model+".fbx");
                if(importer==null)throw new InvalidOperationException("Missing mob export: "+model);
                if(importer.animationType!=ModelImporterAnimationType.Generic||importer.avatarSetup!=ModelImporterAvatarSetup.CreateFromThisModel||importer.clipAnimations.Any(c=>c.name.Contains('|'))||importer.clipAnimations.Length!=4)
                    importer.SaveAndReimport();
                // Takes are available after FBX import; querying them in OnPreprocessModel can
                // produce an empty array and erase the clip mapping on every reimport.
                var clips=importer.defaultClipAnimations;
                foreach(var clip in clips)
                {
                    clip.name=clip.takeName.Substring(clip.takeName.LastIndexOf('|')+1);
                    clip.loopTime=clip.name=="Idle"||clip.name=="Walk";
                    clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;
                    clip.keepOriginalOrientation=true;clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;
                    clip.wrapMode=clip.name=="Death"?WrapMode.ClampForever:clip.loopTime?WrapMode.Loop:WrapMode.Once;
                }
                if(clips.Length!=4)throw new InvalidOperationException(model+" exported takes: "+string.Join(", ",clips.Select(c=>c.takeName)));
                if(importer.clipAnimations.Length!=clips.Length||importer.clipAnimations.Where((c,i)=>c.name!=clips[i].name||!c.lockRootRotation||!c.lockRootHeightY||!c.lockRootPositionXZ).Any())
                {importer.clipAnimations=clips;importer.SaveAndReimport();}
                string path="Assets/RivetReach/Resources/Mobs/Definitions/"+model+".asset";
                if(AssetDatabase.LoadAssetAtPath<MobDefinition>(path)!=null)continue;
                var d=ScriptableObject.CreateInstance<MobDefinition>();bool beetle=model=="RustbackBeetle";
                d.stableId=beetle?"rivet:rustback_beetle":"rivet:dusk_prowler";d.displayName=beetle?"Rustback beetle":"Dusk prowler";
                d.model=model;d.territorial=beetle;d.nocturnal=!beetle;d.climbsWalls=beetle;d.health=beetle?12:18;d.damage=beetle?2:3;
                // Body colliders fit a voxel lane; legs, ears and tail are presentation.
                d.population=beetle?8:6;d.width=beetle?.9f:.85f;d.height=beetle?.92f:1.7f;d.speed=beetle?2.6f:3.7f;
                d.strideLength=beetle?.55f:1.1f;
                d.noticeRange=beetle?8:16;d.leashRange=beetle?20:30;d.attackRange=beetle?1.8f:2.05f;d.windup=beetle?.8f:.65f;d.recovery=beetle?1.2f:1.1f;
                d.Validate();AssetDatabase.CreateAsset(d,path);
            }
            const string materialPath="Assets/RivetReach/Resources/Mobs/CreatureMaterial.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}
            material.SetTexture("_BaseMap",Resources.Load<Texture2D>("Mobs/CreaturePalette"));material.SetFloat("_Smoothness",.18f);material.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            var report=new System.Text.StringBuilder();
            foreach(string model in new[]{"RustbackBeetle","DuskProwler"})
            {
                var prefab=Resources.Load<GameObject>("Mobs/"+model);var animation=prefab.GetComponent<Animator>();
                var actions=Resources.LoadAll<AnimationClip>("Mobs/"+model);
                if(animation==null)throw new InvalidOperationException("No generic animator: "+model);
                foreach(string clip in new[]{"Idle","Walk","Attack","Death"})if(!actions.Any(c=>c.name==clip))throw new InvalidOperationException("Missing "+model+" "+clip);
                foreach(var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh=renderer.sharedMesh;
                    if(mesh.subMeshCount!=1||mesh.triangles.Length/3>10000||renderer.bones.Length>24)throw new InvalidOperationException("Creature geometry contract failed: "+model);
                    if(renderer.bounds.size.y<.5f||renderer.bounds.size.y>3||renderer.bounds.size.z>4)
                        throw new InvalidOperationException("Creature import must remain in metres: "+model+" "+renderer.bounds);
                    if(mesh.boneWeights.Any(w=>w.weight0+w.weight1+w.weight2+w.weight3<.99f))throw new InvalidOperationException("Unweighted creature vertices: "+model);
                    report.AppendLine(model+": "+mesh.triangles.Length/3+" triangles; "+renderer.bones.Length+" bones; "+mesh.subMeshCount+" material; bounds "+renderer.bounds+"; clips Idle, Walk, Attack, Death");
                }
            }
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/mob-import-report.txt",report.ToString());
        }
    }
}
