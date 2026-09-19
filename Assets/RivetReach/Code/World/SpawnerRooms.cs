using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RivetReach
{
    // Versioned settings: future shipped tuning must receive a new generator identity,
    // so already generated chunks keep their exact room layout.
    public sealed class SpawnerRoomProfile
    {
        public readonly int RegionSize,ChancePercent,BuriedPercent,CandidateAttempts;
        public SpawnerRoomProfile(int regionSize=128,int chancePercent=45,int buriedPercent=1,int candidateAttempts=96)
        {
            if(regionSize<64||regionSize%32!=0||chancePercent<0||chancePercent>100||buriedPercent<0||buriedPercent>100||candidateAttempts<1||candidateAttempts>256)
                throw new ArgumentException("Invalid spawner room profile.");
            RegionSize=regionSize;ChancePercent=chancePercent;BuriedPercent=buriedPercent;CandidateAttempts=candidateAttempts;
        }
        public static readonly SpawnerRoomProfile Alpha=new SpawnerRoomProfile();
    }
    public readonly struct SpawnerRoom
    {
        public readonly BlockPos FloorCentre;
        public readonly bool Buried;
        public readonly int EntranceFace,CorridorLength;
        public BlockPos Spawner=>FloorCentre.Offset(0,1,0);
        public SpawnerRoom(BlockPos floor,bool buried,int face,int length)
        {FloorCentre=floor;Buried=buried;EntranceFace=face;CorridorLength=length;}
        public bool TryCell(BlockPos p,out byte block)
        {
            block=0;long dx=p.X-FloorCentre.X,dz=p.Z-FloorCentre.Z;int dy=p.Y-FloorCentre.Y;
            if(dy<0||dy>5)return false;
            bool room=Math.Abs(dx)<=4&&Math.Abs(dz)<=4;
            bool corridor=false;long along=0,across=0;
            if(!Buried)
            {
                along=EntranceFace==0?dx:EntranceFace==1?-dx:EntranceFace==2?dz:-dz;
                across=EntranceFace<2?dz:dx;
                corridor=along>=4&&along<=4+CorridorLength&&Math.Abs(across)<=2&&dy<=4;
            }
            if(!room&&!corridor)return false;
            if(p.Equals(Spawner)){block=BlockId.MobSpawner;return true;}
            if(corridor&&Math.Abs(across)<2&&dy>=1&&dy<=3){block=0;return true;}
            if(room)block=dy==0||dy==5||Math.Abs(dx)==4||Math.Abs(dz)==4?BlockId.Cobblestone:(byte)0;
            else block=BlockId.Cobblestone;
            return true;
        }
    }
    public static class SpawnerRooms
    {
        readonly struct RoomResult
        {public readonly bool Present;public readonly SpawnerRoom Room;public RoomResult(bool present,SpawnerRoom room){Present=present;Room=room;}}
        static readonly ConcurrentDictionary<(int seed,string version,long x,long z,SpawnerRoomProfile profile),Lazy<RoomResult>> cache=new ConcurrentDictionary<(int,string,long,long,SpawnerRoomProfile),Lazy<RoomResult>>();
        static readonly ConcurrentQueue<(int seed,string version,long x,long z,SpawnerRoomProfile profile)> order=new ConcurrentQueue<(int,string,long,long,SpawnerRoomProfile)>();
        const int CacheLimit=2048;
        public static bool TryRoom(TerrainGenerator generator,long regionX,long regionZ,out SpawnerRoom room,SpawnerRoomProfile profile=null)
        {
            profile=profile??SpawnerRoomProfile.Alpha;var key=(generator.Seed,generator.GenerationVersion,regionX,regionZ,profile);
            if(!cache.TryGetValue(key,out var pending))
            {
                var settings=profile;
                var candidate=new Lazy<RoomResult>(()=>{bool present=Find(generator,regionX,regionZ,settings,out var found);return new RoomResult(present,found);},LazyThreadSafetyMode.ExecutionAndPublication);
                pending=cache.GetOrAdd(key,candidate);
                if(ReferenceEquals(pending,candidate))
                {
                    order.Enqueue(key);
                    while(cache.Count>CacheLimit&&order.TryDequeue(out var old))cache.TryRemove(old,out _);
                }
            }
            var result=pending.Value;room=result.Room;return result.Present;
        }
        static bool Find(TerrainGenerator g,long rx,long rz,SpawnerRoomProfile profile,out SpawnerRoom room)
        {
            room=default;uint choice=TerrainGenerator.Hash(rx,9707,rz,g.Seed);
            if(choice%100>=profile.ChancePercent)return false;
            bool buried=TerrainGenerator.Hash(rx,9719,rz,g.Seed)%100<profile.BuriedPercent;
            int chunks=profile.RegionSize/32;
            bool hasHidden=false,hasConnected=false;SpawnerRoom hidden=default,connected=default;
            for(int attempt=0;attempt<profile.CandidateAttempts;attempt++)
            {
                uint hash=TerrainGenerator.Hash(rx,9800+attempt,rz,g.Seed);
                long x=rx*profile.RegionSize+(hash%(uint)chunks)*32+16;
                long z=rz*profile.RegionSize+((hash>>8)%(uint)chunks)*32+16;
                if(Math.Abs(x)<64&&Math.Abs(z)<64||Math.Abs(x)>TerrainGenerator.HorizontalLimit-32||Math.Abs(z)>TerrainGenerator.HorizontalLimit-32)continue;
                // One chunk owns the entire template and its entrance. No structures
                // can straddle a preserved older chunk and retrofit its boundary.
                int ceiling=Math.Min(-16,g.Height(x,z)-40);
                int highestChunk=(int)BlockPos.FloorDiv(ceiling-13,32);
                int lowestChunk=(int)BlockPos.FloorDiv(TerrainGenerator.LavaLevel+24,32)+1;
                if(highestChunk<lowestChunk)continue;
                int y=(lowestChunk+(int)((hash>>16)%(uint)(highestChunk-lowestChunk+1)))*32+8;
                var floor=new BlockPos(x,y,z);
                // Establish both viable modes before applying the 99/1 draw. Different
                // placement rejection rates must not silently bias that split.
                if(!hasHidden&&Fits(g,floor,true)){hidden=new SpawnerRoom(floor,true,0,0);hasHidden=true;}
                if(hasConnected){if(hasHidden){room=buried?hidden:connected;return true;}continue;}
                if(!Fits(g,floor,false))continue;
                for(int f=0;f<4&&!hasConnected;f++)
                {
                    int face=(f+(int)(hash>>28))%4,dx=face==0?1:face==1?-1:0,dz=face==2?1:face==3?-1:0;
                    for(int length=1;length<=6;length++)
                    {
                        var doorway=floor.Offset(dx*(5+length),1,dz*(5+length));
                        if(g.GroundAt(doorway)!=0||g.GroundAt(doorway.Offset(0,1,0))!=0||g.GroundAt(doorway.Offset(0,2,0))!=0)continue;
                        if(!BlockId.Solid(g.GroundAt(doorway.Offset(0,-1,0))))continue;
                        connected=new SpawnerRoom(floor,false,face,length);hasConnected=true;
                        if(hasHidden){room=buried?hidden:connected;return true;}break;
                    }
                }
            }
            return false;
        }
        static bool Fits(TerrainGenerator g,BlockPos floor,bool buried)
        {
            // Keep roof/floor and all shell corners in dry original rock, preserving
            // caves as explicit entrances rather than cutting an exposed box in a void.
            for(int z=-5;z<=5;z++)for(int x=-5;x<=5;x++)
            {
                if(floor.Y+6>=g.Height(floor.X+x,floor.Z+z)-20)return false;
                for(int y=0;y<=6;y++)
                {
                    // At this deliberately dry depth, only the shell needs terrain
                    // probes; an existing empty interior will be carved either way.
                    if(!buried&&y>0&&y<5&&(Math.Abs(x)<4||Math.Abs(z)<4))continue;
                    byte id=g.GroundAt(floor.Offset(x,y,z));
                    if(!BlockId.Solid(id)||Fluids.IsFluid(id)||id==BlockId.Bedrock)return false;
                }
            }
            return true;
        }
        public static bool TryCell(TerrainGenerator g,BlockPos p,out byte block)
        {
            block=0;if(p.Y<=TerrainGenerator.LavaLevel+24||p.Y>-16)return false;
            var profile=SpawnerRoomProfile.Alpha;
            return TryRoom(g,BlockPos.FloorDiv(p.X,profile.RegionSize),BlockPos.FloorDiv(p.Z,profile.RegionSize),out var room)&&room.TryCell(p,out block);
        }
        public static void Stamp(TerrainGenerator g,ChunkPos chunk,byte[] cells)
        {
            var min=chunk.Min;if(min.Y>-16||min.Y+32<=TerrainGenerator.LavaLevel+24)return;
            var profile=SpawnerRoomProfile.Alpha;
            // A room is wholly inside one chunk; halo requests use the same cached definition.
            for(long rz=BlockPos.FloorDiv(min.Z-1,profile.RegionSize);rz<=BlockPos.FloorDiv(min.Z+32,profile.RegionSize);rz++)
            for(long rx=BlockPos.FloorDiv(min.X-1,profile.RegionSize);rx<=BlockPos.FloorDiv(min.X+32,profile.RegionSize);rx++)
            {
                if(!TryRoom(g,rx,rz,out var room)||Math.Abs(room.FloorCentre.X-min.X)>44||Math.Abs(room.FloorCentre.Z-min.Z)>44||room.FloorCentre.Y>min.Y+32||room.FloorCentre.Y+5<min.Y-1)continue;
                for(int z=-1;z<=32;z++)for(int y=-1;y<=32;y++)for(int x=-1;x<=32;x++)
                    if(room.TryCell(min.Offset(x,y,z),out byte block))cells[ChunkMesher.Index(x,y,z)]=block;
            }
        }
    }
}
