using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class IndustryAssets
    {
        public static void Prepare()
        {
            var items=ItemRegistry.Load();
            void Item(byte id,string key,string name,Color color)
            {if(items.items.Any(i=>i.runtimeId==id))return;items.items=items.items.Append(new ItemDefinition{runtimeId=id,stableId="rivet:"+key,displayName=name,colour=color,fistSeconds=1.2f}).ToArray();}
            Color copper=new Color(.75f,.44f,.22f),azure=new Color(.08f,.65f,.95f);
            Item(IndustryId.AzureOre,"azure_ore","Azure Ore",azure);Item(IndustryId.AzureCrystal,"azure_crystal","Azure Crystal",azure);
            Item(IndustryId.CopperWire,"copper_wire","Copper Wire",copper);Item(IndustryId.CopperPlate,"copper_plate","Copper Plate",copper);
            Item(IndustryId.IronPlate,"iron_plate","Iron Plate",Color.gray);Item(IndustryId.Cog,"cog","Iron Cog",Color.gray);Item(IndustryId.Rivets,"rivets","Rivets",copper);Item(IndustryId.Casing,"machine_casing","Machine Casing",Color.gray);Item(IndustryId.Glass,"glass","Glass",Color.white);
            Item(IndustryId.CrushedCopper,"crushed_copper","Crushed Copper",copper);Item(IndustryId.CrushedIron,"crushed_iron","Crushed Iron",Color.gray);Item(IndustryId.CrushedGold,"crushed_gold","Crushed Gold",Color.yellow);
            foreach(var d in IndustryDefinition.All.Values)Item(d.Id,d.Key,d.Name,copper);
            EditorUtility.SetDirty(items);AssetDatabase.SaveAssets();
            // Invalidate the registry's compiled identity map after extending authoring data.
            items.InvalidateIndex();
            var catalog=RecipeCatalogAsset.Load();
            string Stable(byte id)=>items.items.First(i=>i.runtimeId==id).stableId;
            RecipeCellData Cell(byte id,int count=1)=>new RecipeCellData{itemId=Stable(id),count=count};
            void Recipe(byte output,int count,int size,params (byte id,int count)[] input)
            {
                string key="rivet:industry_"+output;string path="Assets/RivetReach/Resources/Definitions/Recipes/Industry"+output+".asset";
                var recipe=AssetDatabase.LoadAssetAtPath<RecipeAsset>(path);
                if(recipe==null){recipe=ScriptableObject.CreateInstance<RecipeAsset>();recipe.stableId=key;recipe.kind=RecipeKind.Shapeless;recipe.minimumGridSize=size;recipe.width=recipe.height=1;recipe.ingredients=input.Select(i=>Cell(i.id,i.count)).ToArray();recipe.output=Cell(output,count);if(output==IndustryId.CopperPlate||output==IndustryId.IronPlate||output==IndustryId.Cog){int cells=output==IndustryId.Cog?2:3;recipe.kind=RecipeKind.Shaped;recipe.width=cells;recipe.ingredients=Enumerable.Repeat(Cell(input[0].id),cells).ToArray();}AssetDatabase.CreateAsset(recipe,path);}
                if(IndustryId.TankPart(output)){recipe.ingredients=input.Select(i=>Cell(i.id,i.count)).ToArray();EditorUtility.SetDirty(recipe);}
                if(!catalog.recipes.Contains(recipe)){catalog.recipes=catalog.recipes.Append(recipe).ToArray();EditorUtility.SetDirty(catalog);}
            }
            Recipe(IndustryId.Bench,1,3,(BlockId.Workbench,1),(BlockId.IronIngot,4),(BlockId.CopperIngot,2));
            Recipe(IndustryId.CopperWire,4,4,(BlockId.CopperIngot,1));Recipe(IndustryId.CopperPlate,3,4,(BlockId.CopperIngot,3));Recipe(IndustryId.IronPlate,3,4,(BlockId.IronIngot,3));Recipe(IndustryId.Cog,1,4,(BlockId.IronIngot,2));Recipe(IndustryId.Rivets,8,4,(BlockId.IronIngot,1));
            Recipe(IndustryId.Casing,1,4,(IndustryId.IronPlate,4),(IndustryId.Rivets,4));
            Recipe(IndustryId.SignalWire,4,4,(IndustryId.CopperWire,1),(IndustryId.AzureCrystal,1));
            Recipe(IndustryId.SignalConduit,2,4,(IndustryId.CopperPlate,2),(IndustryId.SignalWire,1));
            Recipe(IndustryId.Lever,1,3,(BlockId.Stick,1),(BlockId.Cobblestone,1),(IndustryId.AzureCrystal,1));Recipe(IndustryId.Button,1,3,(BlockId.Stone,1),(IndustryId.AzureCrystal,1));
            Recipe(IndustryId.Indicator,1,4,(IndustryId.Glass,1),(IndustryId.AzureCrystal,1),(IndustryId.CopperWire,1));
            Recipe(IndustryId.Relay,1,4,(IndustryId.SignalConduit,2),(IndustryId.AzureCrystal,1),(IndustryId.IronPlate,1));
            Recipe(IndustryId.Door,1,4,(IndustryId.IronPlate,4),(IndustryId.Cog,1),(IndustryId.SignalWire,1));
            Recipe(IndustryId.PowerCable,4,4,(IndustryId.CopperWire,4),(BlockId.Planks,1));
            Recipe(IndustryId.Boiler,1,4,(IndustryId.Casing,1),(IndustryId.CopperPlate,4),(IndustryId.Cog,2));
            Recipe(IndustryId.Alternator,1,4,(IndustryId.Casing,1),(IndustryId.Cog,2),(IndustryId.CopperWire,8));
            Recipe(IndustryId.Crusher,1,4,(IndustryId.Casing,1),(IndustryId.Cog,2),(IndustryId.IronPlate,4));
            Recipe(IndustryId.Lamp,1,4,(IndustryId.Glass,2),(IndustryId.CopperWire,2),(IndustryId.IronPlate,1));
            Recipe(IndustryId.Pump,1,4,(IndustryId.Casing,1),(IndustryId.Cog,1),(IndustryId.CopperWire,4),(IndustryId.CopperPlate,2));
            Recipe(IndustryId.Drill,1,4,(IndustryId.Casing,1),(IndustryId.Cog,2),(IndustryId.IronPlate,4),(IndustryId.CopperWire,4));
            Recipe(IndustryId.Tank,1,4,(IndustryId.CopperPlate,4),(IndustryId.IronPlate,2));Recipe(IndustryId.FluidPipe,4,4,(IndustryId.CopperPlate,2));
            Recipe(IndustryId.ItemPipe,4,4,(IndustryId.IronPlate,2),(IndustryId.CopperWire,2));Recipe(IndustryId.Extractor,1,4,(IndustryId.Cog,1),(IndustryId.CopperWire,2));Recipe(IndustryId.Sensor,1,4,(IndustryId.SignalWire,2),(IndustryId.CopperPlate,1),(IndustryId.Glass,1));
            Recipe(IndustryId.TankFrame,4,4,(IndustryId.IronPlate,3),(IndustryId.Rivets,4),(IndustryId.CopperPlate,1));
            Recipe(IndustryId.TankWall,4,4,(IndustryId.Casing,1),(IndustryId.IronPlate,4));
            Recipe(IndustryId.TankGlass,4,4,(IndustryId.Glass,4),(IndustryId.Rivets,4));
            Recipe(IndustryId.TankController,1,4,(IndustryId.TankWall,1),(IndustryId.Cog,1),(IndustryId.AzureCrystal,1));
            Recipe(IndustryId.TankPort,1,4,(IndustryId.TankWall,1),(IndustryId.FluidPipe,1));
            Recipe(IndustryId.TankHatch,1,4,(IndustryId.TankWall,1),(IndustryId.CopperPlate,2));
            Recipe(IndustryId.TankValve,1,4,(IndustryId.TankPort,1),(IndustryId.SignalConduit,1));
            Recipe(IndustryId.TankSensor,1,4,(IndustryId.TankWall,1),(IndustryId.SignalWire,1),(IndustryId.Glass,1));
            var processing=ProcessingCatalogAsset.Load();
            foreach(var pair in new[]{(IndustryId.AzureOre,IndustryId.AzureCrystal),(BlockId.Sand,IndustryId.Glass),(IndustryId.CrushedCopper,BlockId.CopperIngot),(IndustryId.CrushedIron,BlockId.IronIngot),(IndustryId.CrushedGold,BlockId.GoldIngot)})
            {
                string path="Assets/RivetReach/Resources/Definitions/Recipes/Process"+pair.Item1+".asset";
                var recipe=AssetDatabase.LoadAssetAtPath<ProcessingRecipeAsset>(path);
                if(recipe==null){recipe=ScriptableObject.CreateInstance<ProcessingRecipeAsset>();recipe.stableId="rivet:smelt_"+pair.Item1;recipe.input=Cell(pair.Item1);recipe.output=Cell(pair.Item2);recipe.ticks=200;AssetDatabase.CreateAsset(recipe,path);}
                if(!processing.recipes.Contains(recipe)){processing.recipes=processing.recipes.Append(recipe).ToArray();EditorUtility.SetDirty(processing);}
            }
            foreach(string path in Directory.GetFiles("Assets/RivetReach/Resources/Industry","*.fbx"))
            {
                var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(importer==null)throw new Exception("Missing Blender export "+path);
                if(importer.materialImportMode!=ModelImporterMaterialImportMode.ImportStandard||importer.importAnimation||!importer.isReadable){importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.importAnimation=false;importer.isReadable=true;importer.SaveAndReimport();}
            }
            foreach(string path in Directory.GetFiles("Assets/RivetReach/Resources/Industry","*.png",SearchOption.AllDirectories))
            {var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaIsTransparency=path.Contains("Icons");importer.sRGBTexture=!path.EndsWith("Surface.png");importer.mipmapEnabled=!path.Contains("Icons");importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();}
            const string materialPath="Assets/RivetReach/Resources/Industry/Workshop.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}
            material.SetTexture("_BaseMap",Resources.Load<Texture2D>("Industry/Atlas"));material.SetTexture("_MetallicGlossMap",Resources.Load<Texture2D>("Industry/Surface"));material.EnableKeyword("_METALLICSPECGLOSSMAP");material.SetFloat("_Smoothness",.65f);
            material.SetTexture("_EmissionMap",Resources.Load<Texture2D>("Industry/Emission"));material.SetColor("_EmissionColor",Color.white);material.EnableKeyword("_EMISSION");material.enableInstancing=true;EditorUtility.SetDirty(material);
            foreach(string path in Directory.GetFiles("Assets/RivetReach/Resources/Industry","*.fbx"))
            {var importer=(ModelImporter)AssetImporter.GetAtPath(path);var identifier=new AssetImporter.SourceAssetIdentifier(typeof(Material),"WorkshopAtlas");if(!importer.GetExternalObjectMap().TryGetValue(identifier,out var mapped)||mapped!=material){importer.AddRemap(identifier,material);importer.SaveAndReimport();}}
            const string statusPath="Assets/RivetReach/Resources/Industry/Status.mat";
            var status=AssetDatabase.LoadAssetAtPath<Material>(statusPath);if(status==null){status=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(status,statusPath);}status.SetColor("_BaseColor",new Color(.3f,.5f,.6f));status.enableInstancing=true;EditorUtility.SetDirty(status);
            Transparent("TankGlass",new Color(.37f,.68f,.76f,.13f),.86f);
            Transparent("TankWater",new Color(.08f,.48f,.66f,.88f),.8f);
            NormalizeModels(material);
            var tiles=TerrainTiles.Build();
            ArcadeTerrainArt.Apply(tiles,Resources.Load<Material>("Materials/Terrain"),AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/RivetReach/Settings/SampleSceneProfile.asset"));
            AssetDatabase.SaveAssets();
            catalog.Compile(items);processing.Compile(items);
        }
        static void Transparent(string key,Color color,float smoothness)
        {
            string path="Assets/RivetReach/Resources/Industry/"+key+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",10);m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);m.SetFloat("_Smoothness",smoothness);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=key=="TankGlass"?3010:3000;m.SetOverrideTag("RenderType","Transparent");m.enableInstancing=true;EditorUtility.SetDirty(m);
        }
        public static void PrepareConnectedPipes()
        {
            var keys=new[]{"item_pipe","fluid_pipe","power_cable","signal_conduit","pipe_signal_addition","pipe_power_addition"};
            AssetDatabase.Refresh();var material=Resources.Load<Material>("Industry/Workshop");
            foreach(string key in keys)
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/"+key+".fbx");
                importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.importAnimation=false;importer.isReadable=true;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"WorkshopAtlas"),material);importer.SaveAndReimport();
            }
            NormalizeModels(material,keys);
        }
        static void NormalizeModels(Material material,string[] keys=null)
        {
            const string directory="Assets/RivetReach/Resources/Industry/Runtime";
            Directory.CreateDirectory(directory);AssetDatabase.Refresh();
            Vector3 Convert(Vector3 v)=>new Vector3(-v.x,v.y,v.z+1);
            foreach(string path in Directory.GetFiles("Assets/RivetReach/Resources/Industry","*.fbx"))
            {
                string key=Path.GetFileNameWithoutExtension(path);if(keys!=null&&!keys.Contains(key))continue;var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var root=new GameObject(key);int index=0;
                try
                {
                    foreach(var filter in source.GetComponentsInChildren<MeshFilter>())
                    {
                        var sourceMesh=filter.sharedMesh;var pivot=filter.name.StartsWith("Mask")?Vector3.zero:Convert(filter.transform.position);
                        var vertices=sourceMesh.vertices;var normals=sourceMesh.normals;
                        for(int i=0;i<vertices.Length;i++)
                        {vertices[i]=Convert(filter.transform.TransformPoint(vertices[i]))-pivot;var n=filter.transform.TransformDirection(normals[i]);normals[i]=new Vector3(-n.x,n.y,n.z).normalized;}
                        var indices=sourceMesh.triangles;for(int i=0;i<indices.Length;i+=3){int temp=indices[i+1];indices[i+1]=indices[i+2];indices[i+2]=temp;}
                        string meshPath=directory+"/"+key+"_"+(index++)+".asset";
                        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}else mesh.Clear();
                        mesh.name=Path.GetFileNameWithoutExtension(meshPath);mesh.vertices=vertices;mesh.normals=normals;mesh.uv=sourceMesh.uv;mesh.triangles=indices;mesh.RecalculateBounds();mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
                        var part=new GameObject(filter.name.Split('.')[0]);part.transform.SetParent(root.transform,false);part.transform.localPosition=pivot;
                        part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=part.name=="StatusLight"?Resources.Load<Material>("Industry/Status"):part.name.StartsWith("Glass")?Resources.Load<Material>("Industry/TankGlass"):material;
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,directory+"/"+key+".prefab");
                    // Remove only superseded generated meshes for this export; preserve retained GUIDs.
                    foreach(string old in Directory.GetFiles(directory,key+"_*.asset"))
                        if(int.TryParse(Path.GetFileNameWithoutExtension(old).Substring(key.Length+1),out int oldIndex)&&oldIndex>=index)AssetDatabase.DeleteAsset(old);
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            AssetDatabase.SaveAssets();
        }
    }
}
