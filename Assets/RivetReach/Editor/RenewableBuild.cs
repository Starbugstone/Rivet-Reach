using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class RenewableBuild
    {
        public static void Prepare()
        {
            var items=ItemRegistry.Load();var catalog=RecipeCatalogAsset.Load();
            foreach(byte id in new[]{IndustryId.SolarPanel,IndustryId.WindTurbine})
            {
                var definition=IndustryDefinition.All[id];string key=definition.Key;
                if(!items.items.Any(i=>i.runtimeId==id))items.items=items.items.Append(new ItemDefinition{runtimeId=id,stableId="rivet:"+key,displayName=definition.Name,stackLimit=64,fistSeconds=2,colour=new Color(.2f,.45f,.6f)}).ToArray();
                items.InvalidateIndex();if(items.Get(id).stableId!="rivet:"+key)throw new Exception("Renewable ID occupied");EditorUtility.SetDirty(items);
                string path="Assets/RivetReach/Resources/Definitions/Recipes/Industry"+id+".asset";
                var recipe=AssetDatabase.LoadAssetAtPath<RecipeAsset>(path);
                if(recipe==null){recipe=ScriptableObject.CreateInstance<RecipeAsset>();AssetDatabase.CreateAsset(recipe,path);}
                recipe.stableId="rivet:industry_"+id;recipe.kind=RecipeKind.Shapeless;recipe.minimumGridSize=4;recipe.width=4;recipe.height=4;
                recipe.output=Cell(key,1);
                recipe.ingredients=id==IndustryId.SolarPanel?new[]{Cell("machine_casing",1),Cell("glass",4),Cell("copper_wire",4),Cell("gold_ingot",2),Cell("diamond",1)}:new[]{Cell("alternator",1),Cell("iron_plate",4),Cell("cog",2),Cell("copper_wire",4),Cell("gold_ingot",2),Cell("diamond",1)};
                EditorUtility.SetDirty(recipe);if(!catalog.recipes.Contains(recipe)){catalog.recipes=catalog.recipes.Append(recipe).ToArray();EditorUtility.SetDirty(catalog);}
                var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/"+key+".fbx");
                importer.importAnimation=false;importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
                var icon=(TextureImporter)AssetImporter.GetAtPath("Assets/RivetReach/Resources/Industry/Icons/"+id+".png");icon.alphaIsTransparency=true;icon.mipmapEnabled=false;icon.textureCompression=TextureImporterCompression.Uncompressed;icon.SaveAndReimport();
            }
            IndustryAssets.NormalizeModels(Resources.Load<Material>("Industry/Workshop"),new[]{"solar_panel","wind_turbine"});
            AssetDatabase.SaveAssets();catalog.Compile(items);
            Directory.CreateDirectory("Logs/Renewables");
            foreach(string key in new[]{"solar_panel","wind_turbine"})
            {
                var prefab=Resources.Load<GameObject>("Industry/Runtime/"+key);int triangles=0;var bounds=new Bounds();bool first=true;
                foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh=filter.sharedMesh;triangles+=mesh.triangles.Length/3;
                    foreach(var vertex in mesh.vertices){var v=filter.transform.TransformPoint(vertex);if(first){bounds=new Bounds(v,Vector3.zero);first=false;}else bounds.Encapsulate(v);}
                    if(mesh.uv.Length!=mesh.vertexCount||mesh.normals.Length!=mesh.vertexCount)throw new Exception("Missing renewable UVs/normals");
                }
                if(bounds.min.x<-.001||bounds.min.y<-.001||bounds.min.z<-.001||bounds.max.x>1.001||bounds.max.y>1.001||bounds.max.z>1.001)throw new Exception(key+" outside one-cell footprint: "+bounds);
                File.WriteAllText("Logs/Renewables/"+key+"-import.txt",$"{triangles} triangles; {prefab.GetComponentsInChildren<MeshRenderer>().Length} renderers; bounds {bounds.min} to {bounds.max}\n");
            }
        }
        static RecipeCellData Cell(string key,int count)=>new RecipeCellData{itemId="rivet:"+key,count=count};
    }
}
