using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace RivetReach.Editor
{
    public static class CrateBuild
    {
        public static void Prepare()
        {
            var items=ItemRegistry.Load();var catalog=RecipeCatalogAsset.Load();
            foreach(var entry in new[]{(CrateId.Crate,"bulk_crate","Bulk Crate"),(CrateId.Controller,"crate_controller","Crate Controller")})
            {
                var (id,key,name)=entry;
                if(!items.items.Any(i=>i.runtimeId==id))items.items=items.items.Append(new ItemDefinition{runtimeId=id,stableId="rivet:"+key,displayName=name,stackLimit=64,fistSeconds=1.5f,colour=new Color(.48f,.32f,.14f)}).ToArray();
                items.InvalidateIndex();if(items.Get(id).stableId!="rivet:"+key)throw new Exception("Crate ID occupied");EditorUtility.SetDirty(items);
                string recipePath="Assets/RivetReach/Resources/Definitions/Recipes/"+name.Replace(" ","")+".asset";
                var recipe=AssetDatabase.LoadAssetAtPath<RecipeAsset>(recipePath);
                if(recipe==null)
                {
                    recipe=ScriptableObject.CreateInstance<RecipeAsset>();recipe.stableId="rivet:"+key;recipe.output=new RecipeCellData{itemId="rivet:"+key,count=1};
                    if(id==CrateId.Crate)
                    {
                        recipe.kind=RecipeKind.Shaped;recipe.minimumGridSize=3;recipe.width=recipe.height=3;
                        recipe.ingredients=Enumerable.Range(0,9).Select(i=>new RecipeCellData{itemId=i==4?"rivet:chest":"rivet:planks",count=1}).ToArray();
                    }
                    else
                    {
                        recipe.kind=RecipeKind.Shapeless;recipe.minimumGridSize=4;recipe.width=4;recipe.height=2;
                        recipe.ingredients=new[]{Cell("bulk_crate",1),Cell("machine_casing",1),Cell("gold_ingot",2),Cell("iron_ingot",2),Cell("item_pipe",2),Cell("cog",1)};
                    }
                    AssetDatabase.CreateAsset(recipe,recipePath);
                }
                if(!catalog.recipes.Contains(recipe)){catalog.recipes=catalog.recipes.Append(recipe).ToArray();EditorUtility.SetDirty(catalog);}
                Import(key,id);
            }
            AssetDatabase.SaveAssets();catalog.Compile(items);
        }
        static RecipeCellData Cell(string key,int count)=>new RecipeCellData{itemId="rivet:"+key,count=count};
        static void Import(string key,byte id)
        {
            string fbx="Assets/RivetReach/Resources/Industry/"+key+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);var filter=source.GetComponentInChildren<MeshFilter>();
            string meshPath="Assets/RivetReach/Resources/Industry/Runtime/"+key+"_0.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}else mesh.Clear();
            var vertices=filter.sharedMesh.vertices;var normals=filter.sharedMesh.normals;var indices=filter.sharedMesh.triangles;
            for(int i=0;i<vertices.Length;i++){var v=filter.transform.TransformPoint(vertices[i]);vertices[i]=new Vector3(1+v.x,v.y,-v.z);var n=filter.transform.TransformDirection(normals[i]);normals[i]=new Vector3(n.x,n.y,-n.z).normalized;}
            for(int i=0;i<indices.Length;i+=3){int swap=indices[i+1];indices[i+1]=indices[i+2];indices[i+2]=swap;}
            mesh.vertices=vertices;mesh.normals=normals;mesh.uv=filter.sharedMesh.uv;mesh.triangles=indices;mesh.RecalculateBounds();mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
            const string materialPath="Assets/RivetReach/Resources/Industry/Crates.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("RivetReach/WorldLit"));AssetDatabase.CreateAsset(material,materialPath);}
            material.SetTexture("_BaseMap",Resources.Load<Texture2D>("Industry/Atlas"));material.SetFloat("_Smoothness",.1f);EditorUtility.SetDirty(material);
            var root=new GameObject(key);root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=material;
            try{PrefabUtility.SaveAsPrefabAsset(root,"Assets/RivetReach/Resources/Industry/Runtime/"+key+".prefab");}finally{Object.DestroyImmediate(root);}
            var icon=(TextureImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/Icons/"+id+".png");icon.alphaIsTransparency=true;icon.mipmapEnabled=false;icon.textureCompression=TextureImporterCompression.Uncompressed;icon.SaveAndReimport();
            var b=mesh.bounds;if(b.min.x<-.001||b.min.y<-.001||b.min.z<-.001||b.max.x>1.001||b.max.y>1.001||b.max.z>1.001||indices.Length==0)throw new Exception("Crate outside placement cell: "+b);
            Directory.CreateDirectory("Logs/Crates");File.WriteAllText("Logs/Crates/"+key+"-imports.txt",$"{indices.Length/3} triangles; 1 renderer; 1 material; bounds {b.min} to {b.max}; UV/normals {mesh.uv.Length}/{mesh.normals.Length}.\n");
        }
    }
}
