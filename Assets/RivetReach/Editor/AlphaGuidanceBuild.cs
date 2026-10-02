using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RivetReach.Editor
{
    public static class AlphaGuidanceBuild
    {
        public static void Prepare()
        {
            var items=ItemRegistry.Load();
            foreach(var slab in new[]{(id:BuildingBlocks.WoodenSlab,key:"wooden_slab",name:"Wooden Slab",material:"planks",color:new Color(.6f,.38f,.2f)),
                (id:BuildingBlocks.StoneSlab,key:"stone_slab",name:"Stone Slab",material:"stone",color:new Color(.52f,.55f,.56f))})
            {
                var present=items.items.FirstOrDefault(i=>i.runtimeId==slab.id);
                if(present!=null&&present.stableId!="rivet:"+slab.key)throw new Exception("Slab ID is already occupied.");
                if(present==null)
                {items.items=items.items.Append(new ItemDefinition{runtimeId=slab.id,stableId="rivet:"+slab.key,displayName=slab.name,colour=slab.color,fistSeconds=slab.id==BuildingBlocks.WoodenSlab?1:1.5f,tags=new[]{"half_block"}}).ToArray();items.InvalidateIndex();EditorUtility.SetDirty(items);}
                string path="Assets/RivetReach/Resources/Definitions/Recipes/"+slab.key+".asset";
                var recipe=AssetDatabase.LoadAssetAtPath<RecipeAsset>(path);
                if(recipe==null)
                {
                    recipe=ScriptableObject.CreateInstance<RecipeAsset>();recipe.stableId="rivet:"+slab.key;recipe.kind=RecipeKind.Shaped;recipe.minimumGridSize=3;recipe.width=3;recipe.height=1;
                    recipe.ingredients=Enumerable.Range(0,3).Select(_=>new RecipeCellData{itemId="rivet:"+slab.material,count=1}).ToArray();recipe.output=new RecipeCellData{itemId="rivet:"+slab.key,count=6};AssetDatabase.CreateAsset(recipe,path);
                }
                var catalog=RecipeCatalogAsset.Load();
                if(!catalog.recipes.Contains(recipe)){catalog.recipes=catalog.recipes.Append(recipe).ToArray();EditorUtility.SetDirty(catalog);}
            }
            const string matPath="Assets/RivetReach/Resources/Materials/ConnectedGlass.mat";
            var glass=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(glass==null){glass=new Material(Shader.Find("RivetReach/ConnectedGlass"));AssetDatabase.CreateAsset(glass,matPath);}
            AssetDatabase.SaveAssets();RecipeCatalogAsset.Load().Compile(items);
            BakeSlabIcons();
        }
        static void BakeSlabIcons()
        {
            var atlas=Resources.Load<Texture2DArray>("Materials/BlockTiles");
            foreach(byte id in new[]{BuildingBlocks.WoodenSlab,BuildingBlocks.StoneSlab})
            {
                var cells=new byte[34*34*34];cells[ChunkMesher.Index(0,0,0)]=id;
                var mesh=ChunkMesher.Build(default,0,cells).ToMesh();
                var texture=new Texture2D(atlas.width,atlas.height,TextureFormat.RGBA32,false);texture.SetPixels(atlas.GetPixels(BlockId.Tile(id,0,1)));texture.Apply();
                var material=new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/RivetReach/Editor/ItemIcon.shader"));material.SetTexture("_BaseMap",texture);
                var preview=new PreviewRenderUtility();
                try
                {
                    var root=new GameObject("Slab icon");root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=material;preview.AddSingleGO(root);
                    var camera=preview.camera;camera.orthographic=true;camera.orthographicSize=.8f;camera.nearClipPlane=.01f;camera.farClipPlane=20;
                    var target=new Vector3(.5f,.25f,.5f);camera.transform.position=target+new Vector3(1.4f,1.2f,-2)*2;camera.transform.LookAt(target);
                    preview.BeginPreview(new Rect(0,0,192,192),GUIStyle.none);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;preview.Render(true,false);
                    var rendered=(RenderTexture)preview.EndPreview();var active=RenderTexture.active;RenderTexture.active=rendered;
                    var icon=new Texture2D(192,192,TextureFormat.RGBA32,false);icon.ReadPixels(new Rect(0,0,192,192),0,0);icon.Apply();RenderTexture.active=active;
                    if(QualitySettings.activeColorSpace==ColorSpace.Linear&&!rendered.sRGB){var colors=icon.GetPixels();for(int i=0;i<colors.Length;i++)colors[i]=colors[i].gamma;icon.SetPixels(colors);icon.Apply();}
                    string path="Assets/RivetReach/Resources/Industry/Icons/"+id+".png";File.WriteAllBytes(path,icon.EncodeToPNG());Object.DestroyImmediate(icon);AssetDatabase.ImportAsset(path);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                }
                finally{preview.Cleanup();Object.DestroyImmediate(mesh);Object.DestroyImmediate(material);Object.DestroyImmediate(texture);}
            }
        }
        public static void Build()
        {
            Directory.CreateDirectory("Logs/AlphaGuidance");Prepare();
            AlphaGuidanceChecks.Run();StarterRecipeChecks.Run(ItemRegistry.Load(),RecipeCatalogAsset.Load().Compile(ItemRegistry.Load()));
            ComponentRecipeChecks.Run();SaveCompatibilityChecks.Run();
            WikiExport.Export();
            typeof(ProjectBuild).GetMethod("Build",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"AlphaGuidance",BuildOptions.None});
        }
    }
}
