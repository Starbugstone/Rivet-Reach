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
            var clone=ScriptableObject.CreateInstance<ItemRegistry>();
            try
            {
                clone.items=items.items.Select(item=>JsonUtility.FromJson<ItemDefinition>(JsonUtility.ToJson(item))).ToArray();
                var entry=new SaveEntry{Id=Guid.NewGuid().ToString("N"),WorldId=Guid.NewGuid().ToString("N"),Name="Current renewable content",Seed=1,UtcTicks=DateTime.UtcNow.Ticks,GeneratorVersion=TerrainGenerator.Version};
                // This synthetic current-schema catalog check is not a schema-16
                // migration fixture; the native fixture covers that historical path.
                byte[] EncodeCurrent(int marker)
                {
                    return new SaveStore("unused",clone).Encode(entry,writer=>writer.Write(marker));
                }
                byte[] bytes=EncodeCurrent(178);
                using(var reader=new SaveStore("unused",items).Open(bytes,out _))
                {
                    Check(reader.Format==17&&reader.ReadInt32()==178,"Current full-renewable schema-17 content round-trips");
                }
                clone.items[0].attackDamage++;
                bytes=EncodeCurrent(179);
                bool rejected=false;
                try{using var reader=new SaveStore("unused",items).Open(bytes,out _);}
                catch(System.IO.InvalidDataException){rejected=true;}
                Check(rejected,"Unrelated legacy item mutation remains rejected");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }
            Directory.CreateDirectory("Logs/Renewables");
            File.WriteAllText("Logs/Renewables/compatibility-checks.txt","PASS "+assertions+" assertions\n");
        }
    }
}
