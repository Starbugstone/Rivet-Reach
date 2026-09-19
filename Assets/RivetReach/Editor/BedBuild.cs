using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class BedBuild
    {
        public static void Prepare()
        {
            var items=ItemRegistry.Load();
            if(!items.items.Any(i=>i.runtimeId==BedId.Bed))
            {items.items=items.items.Append(new ItemDefinition{runtimeId=BedId.Bed,stableId="rivet:bed",displayName="Bed",colour=new Color(.2f,.55f,.6f),fistSeconds=1.2f,stackLimit=1}).ToArray();items.InvalidateIndex();EditorUtility.SetDirty(items);}
            if(items.Get(BedId.Bed).stableId!="rivet:bed")throw new Exception("Bed ID occupied");
            var catalog=RecipeCatalogAsset.Load();const string path="Assets/RivetReach/Resources/Definitions/Recipes/Bed.asset";
            var recipe=AssetDatabase.LoadAssetAtPath<RecipeAsset>(path);
            if(recipe==null)
            {
                recipe=ScriptableObject.CreateInstance<RecipeAsset>();recipe.stableId="rivet:bed";recipe.kind=RecipeKind.Shaped;recipe.minimumGridSize=3;recipe.width=3;recipe.height=2;
                recipe.ingredients=Enumerable.Range(0,6).Select(i=>new RecipeCellData{itemId=i<3?"rivet:cloth":"rivet:planks",count=1}).ToArray();recipe.output=new RecipeCellData{itemId="rivet:bed",count=1};AssetDatabase.CreateAsset(recipe,path);
            }
            if(!catalog.recipes.Contains(recipe)){catalog.recipes=catalog.recipes.Append(recipe).ToArray();EditorUtility.SetDirty(catalog);}
            const string fbx="Assets/RivetReach/Resources/Industry/bed.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);var filter=source.GetComponentInChildren<MeshFilter>();
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/RivetReach/Resources/Industry/Runtime/bed_0.asset");
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,"Assets/RivetReach/Resources/Industry/Runtime/bed_0.asset");}else mesh.Clear();
            var vertices=filter.sharedMesh.vertices;var normals=filter.sharedMesh.normals;var indices=filter.sharedMesh.triangles;
            for(int i=0;i<vertices.Length;i++){var v=filter.transform.TransformPoint(vertices[i]);vertices[i]=new Vector3(1+v.x,v.y,-v.z);var n=filter.transform.TransformDirection(normals[i]);normals[i]=new Vector3(n.x,n.y,-n.z).normalized;}
            for(int i=0;i<indices.Length;i+=3){int swap=indices[i+1];indices[i+1]=indices[i+2];indices[i+2]=swap;}
            mesh.vertices=vertices;mesh.normals=normals;mesh.uv=filter.sharedMesh.uv;mesh.triangles=indices;mesh.RecalculateBounds();mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
            const string materialPath="Assets/RivetReach/Resources/Industry/Bed.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(Shader.Find("RivetReach/WorldLit"));AssetDatabase.CreateAsset(material,materialPath);}
            material.SetTexture("_BaseMap",Resources.Load<Texture2D>("Industry/Atlas"));material.SetFloat("_Smoothness",.08f);EditorUtility.SetDirty(material);
            var root=new GameObject("bed");root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=material;
            try{PrefabUtility.SaveAsPrefabAsset(root,"Assets/RivetReach/Resources/Industry/Runtime/bed.prefab");}finally{UnityEngine.Object.DestroyImmediate(root);}
            var icon=(TextureImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/Icons/254.png");icon.alphaIsTransparency=true;icon.mipmapEnabled=false;icon.textureCompression=TextureImporterCompression.Uncompressed;icon.SaveAndReimport();
            AssetDatabase.SaveAssets();catalog.Compile(items);
            var b=mesh.bounds;if(b.min.x<-.001||b.min.y<-.001||b.min.z<-.001||b.max.x>1.001||b.max.y>1.001||b.max.z>2.001||mesh.triangles.Length==0)throw new Exception("Bed outside 1x1x2 footprint: "+b);
            Directory.CreateDirectory("Logs/Beds");File.WriteAllText("Logs/Beds/imports.txt",$"{indices.Length/3} triangles; 1 renderer; 1 shared material; bounds {b.min} to {b.max}; UV/normals {mesh.uv.Length}/{mesh.normals.Length}.\n");
        }
    }
}
