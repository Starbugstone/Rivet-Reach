using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class FloaterChecks
    {
        public static void Run()
        {
            var checks=new System.Collections.Generic.List<string>();
            void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);checks.Add(message);}
            var registry=ItemRegistry.Load();var d=Resources.Load<MobDefinition>("Mobs/Definitions/Floater");d.Validate();
            Check(registry.ResolveId(d.deathDropId)==BlockId.FloaterRock,"Defeat drop is a registered item");
            Check(!BlockId.Placeable(BlockId.FloaterRock)&&!Fluids.IsFluid(BlockId.FloaterRock)&&registry.Get(BlockId.FloaterRock).foodPoints==0,"Floater Rock is a stackable non-food resource, without a terrain placement or fluid ID collision");
            var model=Resources.Load<GameObject>("MobLoot/FloaterRock");
            Check(model!=null&&MobLootVisuals.Icon!=null,"Rock model and actual inventory icon import");
            var mesh=model.GetComponentInChildren<MeshFilter>().sharedMesh;
            Check(mesh.subMeshCount==1&&mesh.triangles.Length/3==116,"Collectible rock imports its 116 source triangles and one material slot");
            // When the retained pre-Floater checkpoint is available, verify the historical hash
            // against real prior-build bytes, then ensure older content remains protected.
            const string prior="Logs/Lava-Saves-Final/Saves";
            if(Directory.Exists(prior))
            {
                var store=new SaveStore(prior,registry);var entries=store.List();
                Check(entries.Count>0,"Real pre-Floater schema-7 checkpoint remains readable");
                var bytes=File.ReadAllBytes(entries.First().Path);
                var beetle=Resources.Load<MobDefinition>("Mobs/Definitions/RustbackBeetle");int health=beetle.health;
                try
                {
                    beetle.health++;
                    bool rejected=false;
                    try{using var reader=new SaveStore(prior,registry).Open(bytes,out _);}catch(InvalidDataException){rejected=true;}
                    Check(rejected,"Legacy compatibility still rejects changed pre-existing mob health");
                }
                finally{beetle.health=health;}
                var item=registry.Get(BlockId.Stone);int limit=item.stackLimit;
                try
                {
                    item.stackLimit++;
                    bool rejected=false;
                    try{using var reader=new SaveStore(prior,registry).Open(bytes,out _);}catch(InvalidDataException){rejected=true;}
                    Check(rejected,"Legacy compatibility still rejects changed pre-existing item statistics");
                }
                finally{item.stackLimit=limit;}
                checks.Add("Prior checkpoint: "+entries.First().Path);
            }
            File.WriteAllLines("Logs/floater-checks.txt",checks);
        }
    }
}
