using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    [Serializable]
    public sealed class MobSpawnerDefinition
    {
        public string key="rivet:floater_spawner",species="rivet:floater";
        public float activationRadius=36,interval=10;
        public int activeCap=5,radius=4,attempts=12;
        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(key)||string.IsNullOrWhiteSpace(species)||activationRadius<1||activationRadius>64||interval<1||
                float.IsNaN(interval)||float.IsInfinity(interval)||float.IsNaN(activationRadius)||activeCap<1||activeCap>5||radius<1||radius>8||attempts<1||attempts>32)
                throw new InvalidOperationException("Invalid spawner configuration.");
        }
    }
    public sealed class MobSpawnerState
    {
        public long Id,Sequence;
        public BlockPos Position;
        public string Definition;
        public float Cooldown=4;
        public SpawnRejection LastRejection;
    }

    public sealed partial class MobSystem
    {
        public static readonly MobSpawnerDefinition[] SpawnerDefinitions={new MobSpawnerDefinition()};
        readonly Dictionary<BlockPos,MobSpawnerState> spawners=new Dictionary<BlockPos,MobSpawnerState>();
        readonly List<MobSpawnerState> residentSpawners=new List<MobSpawnerState>();
        long nextSpawnerId=1;
        public IEnumerable<MobSpawnerState> Spawners=>spawners.Values;
        public IReadOnlyList<MobSpawnerState> ResidentSpawners=>residentSpawners;
        public static MobSpawnerDefinition SpawnerDefinition(string key)
        {foreach(var definition in SpawnerDefinitions)if(definition.key==key)return definition;return null;}
        MobDefinition SpawnerSpecies(string key)
        {foreach(var definition in Definitions)if(definition.stableId==key)return definition;return null;}
        public MobSpawnerState SpawnerAt(BlockPos p)=>spawners.TryGetValue(p,out var state)?state:null;
        public int SpawnerPopulation(long id)
        {int count=0;foreach(var mob in Mobs)if(mob.Alive&&mob.SpawnerId==id)count++;return count;}
        void InitializeSpawners()
        {
            var keys=new HashSet<string>(StringComparer.Ordinal);
            foreach(var definition in SpawnerDefinitions)
            {
                definition.Validate();if(!keys.Add(definition.key))throw new InvalidOperationException("Duplicate spawner definition.");
                if(SpawnerSpecies(definition.species)==null)throw new InvalidOperationException("Unknown spawner species: "+definition.species);
            }
            world.ChunkReady+=ScanSpawners;world.BlockChanged+=SpawnerChanged;world.ResidencyChanged+=RefreshResidentSpawners;
            gameObject.AddComponent<MobSpawnerPresentation>().Initialize(this,game);
        }
        void ScanSpawners(ChunkPos chunk,byte[] cells)
        {
            var min=chunk.Min;
            for(int z=0;z<32;z++)for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                if(cells[ChunkMesher.Index(x,y,z)]==BlockId.MobSpawner)SpawnerChanged(min.Offset(x,y,z));
        }
        void SpawnerChanged(BlockPos p)
        {
            bool exists=spawners.TryGetValue(p,out var state);
            if(world.Get(p)==BlockId.MobSpawner)
            {
                if(exists)return;
                state=new MobSpawnerState{Id=nextSpawnerId++,Position=p,Definition=SpawnerDefinitions[0].key,Cooldown=Mathf.Min(4,SpawnerDefinitions[0].interval)};spawners.Add(p,state);
                if(world.Ready(p))residentSpawners.Add(state);
            }
            else if(exists){spawners.Remove(p);residentSpawners.Remove(state);}
        }
        void RefreshResidentSpawners()
        {
            residentSpawners.Clear();
            foreach(var state in spawners.Values)if(world.Ready(state.Position))residentSpawners.Add(state);
        }
        public bool TrySpawnerCycle(MobSpawnerState state)
        {
            if(state==null||!spawners.TryGetValue(state.Position,out var actual)||actual!=state||!game.ReadyToPlay||game.Health.Dead||
                !world.Ready(state.Position)||world.Get(state.Position)!=BlockId.MobSpawner)return false;
            var definition=SpawnerDefinition(state.Definition);
            if(definition==null)throw new InvalidOperationException("Unknown spawner definition.");
            Vector3 centre=world.Local(state.Position)+Vector3.one*.5f;
            if((centre-game.Player.transform.position).sqrMagnitude>definition.activationRadius*definition.activationRadius)return false;
            if(SpawnerPopulation(state.Id)>=definition.activeCap){state.LastRejection=SpawnRejection.PopulationCap;return false;}
            var mob=SpawnerSpecies(definition.species);
            if(mob.nocturnal&&!IsNight){state.LastRejection=SpawnRejection.Daytime;return false;}
            // The cage itself must be dark as well as the chosen floor. Lighting a cage
            // cannot merely push its attempts to an unlit corner at the edge of the room.
            if(!world.TryGetSpawnLight(state.Position.Offset(0,1,0),IsNight,out byte light))
            {state.LastRejection=SpawnRejection.UnknownLight;return false;}
            if(!mob.spawnRules.AllowsLight(light)){state.LastRejection=light>mob.spawnRules.maximumLight?SpawnRejection.TooBright:SpawnRejection.TooDark;return false;}
            for(int attempt=0;attempt<definition.attempts;attempt++)
            {
                uint random=TerrainGenerator.Hash(state.Position.X,state.Sequence++,state.Position.Z,game.Seed);
                int side=definition.radius*2+1;
                int dx=(int)(random%(uint)side)-definition.radius,dz=(int)((random>>9)%(uint)side)-definition.radius;
                int dy=(int)((random>>18)%3)-1;
                var support=state.Position.Offset(dx,dy-1,dz);
                Vector3 feet=world.Local(support)+new Vector3(.5f,1.006f+mob.hoverHeight,.5f);
                state.LastRejection=CheckSpawnSite(mob,feet);
                if(state.LastRejection!=SpawnRejection.None)continue;
                CreateMob(mob,feet,state.Id);return true;
            }
            return false;
        }
        void AdvanceSpawners(float dt)
        {
            if(!game.ReadyToPlay||game.Health.Dead)return;
            foreach(var state in residentSpawners)
            {
                var definition=SpawnerDefinition(state.Definition);
                if((world.Local(state.Position)+Vector3.one*.5f-game.Player.transform.position).sqrMagnitude>definition.activationRadius*definition.activationRadius)continue;
                state.Cooldown-=dt;if(state.Cooldown>0)continue;
                state.Cooldown=definition.interval;TrySpawnerCycle(state);
            }
        }
    }
}
