using System;
using System.Collections.Generic;
using System.Linq;

namespace RivetReach
{
    public sealed partial class MobSystem
    {
        internal void WriteSpawnerSave(SaveWriter w)
        {
            if(w.Format<13)
            {
                SaveReader.Require(spawners.Count==0&&Mobs.All(m=>m.SpawnerId==0),"Legacy saves cannot represent spawner state.");return;
            }
            w.Write(nextSpawnerId);w.Write(spawners.Count);
            foreach(var state in spawners.Values.OrderBy(s=>s.Id))
            {w.Write(state.Id);w.Pos(state.Position);w.Write(state.Definition);w.Write(state.Sequence);w.Write(state.Cooldown);}
        }
        internal void ReadSpawnerSave(SaveReader r)
        {
            if(r.Format<13)return;
            nextSpawnerId=r.Long(1);int count=r.Count(100000);var ids=new HashSet<long>();
            for(int i=0;i<count;i++)
            {
                long id=r.Long(1,nextSpawnerId-1);var position=r.Pos();string key=r.Text();
                var definition=SpawnerDefinition(key);
                SaveReader.Require(definition!=null&&ids.Add(id)&&!spawners.ContainsKey(position)&&world.Get(position)==BlockId.MobSpawner,"Invalid saved mob spawner.");
                var state=new MobSpawnerState{Id=id,Position=position,Definition=key,Sequence=r.Long(),Cooldown=r.Float(0,definition.interval)};
                spawners.Add(position,state);
            }
            int natural=0;var origins=new Dictionary<long,int>();
            foreach(var mob in Mobs)
            {
                SaveReader.Require(mob.SpawnerId>=0&&mob.SpawnerId<nextSpawnerId,"Invalid mob spawner origin.");
                if(!mob.Alive)continue;
                if(mob.SpawnerId==0){natural++;continue;}
                // A broken cage can leave ordinary living mobs behind. The identity is
                // retained until death/despawn, but cannot be reused by a new cage.
                origins.TryGetValue(mob.SpawnerId,out int active);origins[mob.SpawnerId]=active+1;
            }
            SaveReader.Require(natural<=MaximumPopulation,"Invalid natural mob population.");
            foreach(var origin in origins)
            {
                var state=spawners.Values.FirstOrDefault(s=>s.Id==origin.Key);
                int cap=state==null?SpawnerDefinitions.Max(d=>d.activeCap):SpawnerDefinition(state.Definition).activeCap;
                SaveReader.Require(origin.Value<=cap,"Invalid per-spawner mob population.");
                if(state!=null)
                {
                    string species=SpawnerDefinition(state.Definition).species;
                    SaveReader.Require(Mobs.Where(m=>m.SpawnerId==origin.Key).All(m=>m.Definition.stableId==species),"Saved origin and species disagree.");
                }
            }
            RefreshResidentSpawners();
        }
    }
}
