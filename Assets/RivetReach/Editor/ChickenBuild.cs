using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class ChickenBuild
    {
        public static void Prepare()
        {
            var items=ItemRegistry.Load();
            void Item(byte id,string key,string name,int food,params string[] tags)
            {
                if(items.items.Any(i=>i.runtimeId==id)){if(items.Get(id).stableId!="rivet:"+key)throw new Exception("Chicken item ID occupied: "+id);return;}
                items.items=items.items.Append(new ItemDefinition{runtimeId=id,stableId="rivet:"+key,displayName=name,stackLimit=64,fistSeconds=.15f,foodPoints=food,tags=tags,colour=new Color(.85f,.66f,.36f)}).ToArray();
            }
            Item(ChickenId.Egg,"egg","Egg",0,"egg","raw_egg");Item(ChickenId.Raw,"raw_chicken","Raw Chicken",2,"edible","meat","raw_meat");
            Item(ChickenId.Cooked,"cooked_chicken","Cooked Chicken",6,"edible","meat","prepared_food");Item(ChickenId.Feather,"feather","Feather",0,"feather");
            Item(ChickenId.CookedEgg,"cooked_egg","Cooked Egg",4,"edible","egg","prepared_food");Item(ChickenId.Stew,"chicken_stew","Chicken Stew",12,"edible","prepared_food");
            items.InvalidateIndex();EditorUtility.SetDirty(items);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            const string folder="Assets/RivetReach/Resources/Chickens/";var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"Chicken.mat");
            if(material==null){material=new Material(Shader.Find("RivetReach/WorldLit"));AssetDatabase.CreateAsset(material,folder+"Chicken.mat");}
            material.SetTexture("_BaseMap",Resources.Load<Texture2D>("Chickens/Palette"));material.SetFloat("_Smoothness",.12f);EditorUtility.SetDirty(material);
            foreach(string path in Directory.GetFiles(folder,"*.fbx"))
            {
                bool creature=Path.GetFileNameWithoutExtension(path)=="Chicken"||Path.GetFileNameWithoutExtension(path)=="Chick";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.isReadable=true;importer.useFileScale=true;
                importer.importAnimation=creature;importer.animationType=creature?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
                if(creature){importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.optimizeGameObjects=false;importer.animationCompression=ModelImporterAnimationCompression.Off;}
                importer.SaveAndReimport();
                if(!creature)continue;
                bool chick=Path.GetFileNameWithoutExtension(path)=="Chick";
                var clips=new System.Collections.Generic.List<ModelImporterClipAnimation>();
                foreach(string action in new[]{"Idle","Walk","Peck","Death"})
                {
                    string wanted=action+(chick?".001":"");
                    var clip=importer.defaultClipAnimations.FirstOrDefault(c=>c.takeName.Substring(c.takeName.LastIndexOf('|')+1)==wanted);
                    if(clip==null)throw new Exception("Missing chicken take "+wanted+": "+string.Join(",",importer.defaultClipAnimations.Select(c=>c.takeName)));
                    clip.name=action;clip.loopTime=action!="Death";clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;
                    clip.keepOriginalOrientation=true;clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;clip.wrapMode=action=="Death"?WrapMode.ClampForever:WrapMode.Loop;clips.Add(clip);
                }
                importer.clipAnimations=clips.ToArray();importer.SaveAndReimport();
            }
            foreach(string path in Directory.GetFiles(folder,"*.png"))
            {var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.alphaIsTransparency=!path.EndsWith("Palette.png");importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
            AssetDatabase.SaveAssets();ChickenCatalog.Load();CookingCatalog.Current.Validate(items);
            Directory.CreateDirectory("Logs/Chickens");
            File.WriteAllLines("Logs/Chickens/imports.txt",Directory.GetFiles(folder,"*.fbx").Select(path=>
            {
                var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>();var filters=root.GetComponentsInChildren<MeshFilter>();
                int triangles=skins.Sum(r=>(int)r.sharedMesh.GetIndexCount(0)/3)+filters.Sum(f=>f.sharedMesh.triangles.Length/3);
                int bones=skins.Sum(r=>r.bones.Length);int renderers=root.GetComponentsInChildren<Renderer>().Length;
                if(triangles==0||renderers!=1)throw new Exception("Invalid chicken mesh import: "+path);
                return Path.GetFileName(path)+" | triangles="+triangles+" | renderers="+renderers+" | bones="+bones+" | clips="+string.Join(",",Resources.LoadAll<AnimationClip>("Chickens/"+Path.GetFileNameWithoutExtension(path)).Where(c=>!c.name.StartsWith("__")).Select(c=>c.name));
            }));
        }
    }
}
