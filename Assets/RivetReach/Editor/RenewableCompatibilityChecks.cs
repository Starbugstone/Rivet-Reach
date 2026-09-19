using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class RenewableCompatibilityChecks
    {
        public static void Run()
        {
            int assertions=0;
            void Check(bool ok,string message){if(!ok)throw new Exception("Renewables compatibility: "+message);assertions++;}
            var items=ItemRegistry.Load();
            var catalog=RecipeCatalogAsset.Load();
            var originalRecipes=catalog.recipes;
            var legacy=ScriptableObject.CreateInstance<ItemRegistry>();
            try
            {
                legacy.items=items.items.Where(item=>item.runtimeId!=178&&item.runtimeId!=179)
                    .Select(item=>JsonUtility.FromJson<ItemDefinition>(JsonUtility.ToJson(item))).ToArray();
                var entry=new SaveEntry{Id=Guid.NewGuid().ToString("N"),WorldId=Guid.NewGuid().ToString("N"),Name="Pre-renewables content",Seed=1,UtcTicks=DateTime.UtcNow.Ticks,GeneratorVersion=TerrainGenerator.Version};
                byte[] EncodeLegacy(int marker)
                {
                    catalog.recipes=originalRecipes.Where(recipe=>recipe.stableId!="rivet:industry_178"&&recipe.stableId!="rivet:industry_179").ToArray();
                    try{return new SaveStore("unused",legacy).Encode(entry,writer=>writer.Write(marker));}
                    finally{catalog.recipes=originalRecipes;}
                }
                byte[] bytes=EncodeLegacy(178);
                using(var reader=new SaveStore("unused",items).Open(bytes,out _))
                {
                    Check(reader.Format==16&&reader.ReadInt32()==178,"Exact pre-renewable schema-16 content remains compatible");
                }
                legacy.items[0].attackDamage++;
                bytes=EncodeLegacy(179);
                bool rejected=false;
                try{using var reader=new SaveStore("unused",items).Open(bytes,out _);}
                catch(System.IO.InvalidDataException){rejected=true;}
                Check(rejected,"Unrelated legacy item mutation remains rejected");
            }
            finally
            {
                catalog.recipes=originalRecipes;
                UnityEngine.Object.DestroyImmediate(legacy);
            }
            Directory.CreateDirectory("Logs/Renewables");
            File.WriteAllText("Logs/Renewables/compatibility-checks.txt","PASS "+assertions+" assertions\n");
        }
    }
}
