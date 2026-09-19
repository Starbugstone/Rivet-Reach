using System;
using UnityEngine;

namespace RivetReach
{
    // The current one-player session uses this same policy as a future authoritative roster.
    // It does not create a network session or claim multiplayer support.
    public sealed class SleepPolicy
    {
        public int Percentage {get;}
        public int MinimumCount {get;}
        public SleepPolicy(int percentage=50,int minimumCount=1)
        {if(percentage<1||percentage>100||minimumCount<1||minimumCount>1000000)throw new ArgumentOutOfRangeException();Percentage=percentage;MinimumCount=minimumCount;}
        public int Required(int activePlayers)=>activePlayers<=0?0:Math.Min(activePlayers,Math.Max(MinimumCount,(int)(((long)activePlayers*Percentage+99)/100)));
        public bool Met(int sleeping,int activePlayers)=>activePlayers>0&&sleeping>=Required(activePlayers);
        public static double Morning(double days)
        {double today=Math.Floor(days),hour=(days-today)*24;return today+(hour<6?.25:1.25);}
    }
    public sealed class PlayerBeds
    {
        readonly Expedition game;
        public SleepPolicy Policy {get;private set;}=new SleepPolicy();
        public BlockPos? Home {get;private set;}
        public long HomeIdentity {get;private set;}
        public PlayerBeds(Expedition game){this.game=game;}
        public void ConfigureSleepPolicy(int percentage,int minimumCount)=>Policy=new SleepPolicy(percentage,minimumCount);
        public bool Use(BlockPos cell)
        {
            var w=game.World;var bed=w.BedAt(cell);
            if(game.Mode!=ScreenMode.Play||game.Health.Dead||bed==null||!w.Ready(bed.Foot)||!w.Ready(bed.Head))return false;
            if(!game.SelectInteraction(game.Player.Camera.transform.position,game.Player.Camera.transform.forward,5,out var hit)||hit.Kind!=InteractionTargetKind.Block||w.BedAt(hit.Block.Position)!=bed)return false;
            if(!TryArrival(bed,out _)){game.Notify("Bed unavailable: leave clear, safe standing space beside it.",4);return true;}
            Home=bed.Foot;HomeIdentity=bed.Identity;
            if(!game.Sky.Clock.IsNight){game.Notify("Home spawn set. You can sleep here at night.",4);return true;}
            if(Policy.Met(1,1))
            {
                double morning=SleepPolicy.Morning(game.Sky.Clock.TotalDays);
                if(morning>1e9){game.Notify("Home spawn set. This world has reached its calendar limit.",4);return true;}
                game.Fishing.Cast.Cancel();game.Sky.Clock.SetTime(morning);game.Sky.Apply();
                game.Notify("Home spawn set. You slept until morning.",4);
            }
            return true;
        }
        public bool TryRespawn(out BlockPos cell)
        {
            cell=default;if(!Home.HasValue)return false;var bed=game.World.BedAt(Home.Value);
            return bed!=null&&bed.Identity==HomeIdentity&&TryArrival(bed,out cell);
        }
        public bool TryArrival(BedState bed,out BlockPos spawn)
        {
            spawn=default;var w=game.World;
            if(w.Get(bed.Foot)!=BedId.Bed||w.Get(bed.Head)!=BedId.Head||!BedId.Floor(w.Get(bed.Foot.Offset(0,-1,0)))||!BedId.Floor(w.Get(bed.Head.Offset(0,-1,0))))return false;
            // Deterministic bounded ring, tested against authoritative terrain even if unloaded.
            foreach(var anchor in new[]{bed.Foot,bed.Head})
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
            {
                if(dx==0&&dz==0)continue;var p=anchor.Offset(dx,0,dz);
                if(p.Equals(bed.Foot)||p.Equals(bed.Head)||p.Y+1>TerrainGenerator.MaxY||Math.Abs(p.X)>TerrainGenerator.HorizontalLimit||Math.Abs(p.Z)>TerrainGenerator.HorizontalLimit)continue;
                if(!BedId.Floor(w.Get(p.Offset(0,-1,0)))||w.Get(p)!=0||w.Get(p.Offset(0,1,0))!=0)continue;
                bool hazard=false;
                for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)for(int y=0;y<=1;y++)if(Fluids.IsFluid(w.Get(p.Offset(x,y,z))))hazard=true;
                if(hazard||(game.Mobs?.Occupies(p)??false)||(game.Mobs?.Occupies(p.Offset(0,1,0))??false)||(game.Animals?.Occupies(p)??false)||(game.Animals?.Occupies(p.Offset(0,1,0))??false))continue;
                spawn=p;return true;
            }
            return false;
        }
        internal void WriteSave(SaveWriter w)
        {
            game.World.WriteBeds(w);w.Write(Home.HasValue);if(Home.HasValue){w.Pos(Home.Value);w.Write(HomeIdentity);}
            w.Write(Policy.Percentage);w.Write(Policy.MinimumCount);
        }
        internal void ReadSave(SaveReader r)
        {
            SaveReader.Require(!Home.HasValue&&HomeIdentity==0,"Home restore requires a fresh session.");
            Home=null;HomeIdentity=0;if(r.Format<14)return;game.World.ReadBeds(r);
            if(r.ReadBoolean()){Home=r.Pos();HomeIdentity=r.Long(1);SaveReader.Require(game.World.IssuedBedIdentity(HomeIdentity),"Unknown saved home-bed identity.");}
            Policy=new SleepPolicy(r.Int(1,100),r.Int(1,1000000));
        }
    }
}
