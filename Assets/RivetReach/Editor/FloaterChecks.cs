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
            d.spawnRules.Validate(registry);
            var unrestricted=new MobSpawnRules();unrestricted.Validate(registry);
            Check(unrestricted.AllowsLight(0)&&unrestricted.AllowsLight(15),"Shared passive-ready profiles can accept the full light range");
            var bright=new MobSpawnRules{minimumLight=8};bright.Validate(registry);
            Check(!bright.AllowsLight(7)&&bright.AllowsLight(8)&&bright.AllowsLight(15),"Shared bright-ground profiles enforce an inclusive minimum light independently of hostility");
            foreach(var mob in Resources.LoadAll<MobDefinition>("Mobs/Definitions"))
            {
                mob.spawnRules.Validate(registry);
                Check(mob.spawnRules.minimumLight==0&&mob.spawnRules.maximumLight==7&&mob.spawnRules.AllowsLight(7)&&!mob.spawnRules.AllowsLight(8),mob.displayName+" permits darkness through level 7 and rejects level 8 or brighter");
            }
            foreach(var range in new[]{(-1,7),(0,16),(8,7)})
            {
                bool rejected=false;
                try{new MobSpawnRules{minimumLight=range.Item1,maximumLight=range.Item2}.Validate();}catch(InvalidOperationException){rejected=true;}
                Check(rejected,"Invalid spawn-light range is rejected: "+range);
            }
            const string beforeLight="Logs/CaveFullFinal/FloaterSaves";
            if(Directory.Exists(beforeLight))
            {
                var entries=new SaveStore(beforeLight,registry).List();
                Check(entries.Count>0,"Real pre-light cave-profile checkpoint remains readable");
                byte[] bytes=File.ReadAllBytes(entries.First().Path);
                bool Rejects(){try{using var reader=new SaveStore(beforeLight,registry).Open(bytes,out _);return false;}catch(InvalidDataException){return true;}}
                int maximum=d.spawnRules.maximumLight;var blocks=d.spawnRules.supportBlocks;
                try
                {
                    d.spawnRules.maximumLight=8;
                    Check(Rejects(),"Pre-light migration rejects unrecognized spawn-light tuning");d.spawnRules.maximumLight=maximum;
                    d.spawnRules.supportBlocks=new[]{"rivet:grass"};
                    Check(Rejects(),"Pre-light migration preserves existing support-block checks");
                }
                finally{d.spawnRules.maximumLight=maximum;d.spawnRules.supportBlocks=blocks;}
            }
            Check(d.spawnRules.habitat==MobHabitat.Underground&&!d.nocturnal,"Floater authors underground-only habitat with independent all-day timing");
            const string beforeHabitats="Logs/Compost/ReleaseReview/Saves";
            if(Directory.Exists(beforeHabitats))
            {
                var store=new SaveStore(beforeHabitats,registry);var entries=store.List();
                Check(entries.Count>0,"Real pre-habitat schema-10 checkpoint remains readable with original content checks");
                byte[] bytes=File.ReadAllBytes(entries.First().Path);
                bool Rejects(){try{using var reader=new SaveStore(beforeHabitats,registry).Open(bytes,out _);return false;}catch(InvalidDataException){return true;}}
                var habitat=d.spawnRules.habitat;var blocks=d.spawnRules.supportBlocks;int health=d.health;bool night=d.nocturnal;
                try
                {
                    d.spawnRules.habitat=MobHabitat.Both;
                    Check(Rejects(),"Legacy habitat migration rejects unrelated habitat changes");d.spawnRules.habitat=habitat;
                    d.spawnRules.supportBlocks=new[]{"rivet:grass"};
                    Check(Rejects(),"Legacy habitat migration rejects changed support-block requirements");d.spawnRules.supportBlocks=blocks;
                    d.health++;
                    Check(Rejects(),"Legacy habitat migration retains Floater combat-stat checks");d.health=health;
                    d.nocturnal=true;
                    Check(Rejects(),"Legacy habitat migration accepts only the known Floater timing change");
                }
                finally{d.spawnRules.habitat=habitat;d.spawnRules.supportBlocks=blocks;d.health=health;d.nocturnal=night;}
            }
            File.WriteAllLines("Logs/floater-checks.txt",checks);
        }
    }
}
