using System;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public enum MobHabitat { Surface, Underground, Both }
    public enum SpawnRejection { None, Unloaded, Blocked, Fluid, UnsupportedFloor, WrongHabitat, UnknownLight, TooBright, TooDark, Occupied, TooClose, TooFar, SpawnProtection, Daytime, Visible, PopulationCap, SpeciesCap }

    // Shared site eligibility only: hostile and passive populations own their separate lifecycles.
    [Serializable]
    public sealed class MobSpawnRules
    {
        static readonly string[] originalSupport={"rivet:grass","rivet:dirt","rivet:stone","rivet:sand","rivet:sandstone","rivet:snow","rivet:red_clay"};
        public MobHabitat habitat=MobHabitat.Surface;
        public string[] supportBlocks=(string[])originalSupport.Clone();
        [Range(0,15)] public int minimumLight=0;
        [Range(0,15)] public int maximumLight=15;
        public bool Surface=>habitat==MobHabitat.Surface||habitat==MobHabitat.Both;
        public bool Underground=>habitat==MobHabitat.Underground||habitat==MobHabitat.Both;
        internal bool AlphaSupport=>supportBlocks!=null&&supportBlocks.SequenceEqual(originalSupport.Concat(new[]{"rivet:cobblestone","rivet:planks"}));
        internal bool OriginalSupport=>supportBlocks!=null&&supportBlocks.SequenceEqual(originalSupport);

        public void Validate(ItemRegistry registry=null)
        {
            if(!Enum.IsDefined(typeof(MobHabitat),habitat)||supportBlocks==null||supportBlocks.Length==0||
               supportBlocks.Any(string.IsNullOrWhiteSpace)||supportBlocks.Distinct().Count()!=supportBlocks.Length)
                throw new InvalidOperationException("Invalid mob habitat or spawn-support whitelist.");
            if(minimumLight<0||maximumLight>15||minimumLight>maximumLight)
                throw new InvalidOperationException("Mob spawn light must be an inclusive range within 0..15.");
            if(registry!=null)foreach(string id in supportBlocks)
                if(!BlockId.Solid(registry.ResolveId(id)))throw new InvalidOperationException("Mob spawn support must be a registered solid block: "+id);
        }
        public bool AllowsSupport(ItemRegistry registry,byte block)
            =>BlockId.Solid(block)&&Array.IndexOf(supportBlocks,registry.Get(block).stableId)>=0;
        public bool AllowsLight(int level)=>level>=minimumLight&&level<=maximumLight;

        // Works with body dimensions supplied by either mob system, without a hostile state/view.
        public bool AllowsSite(VoxelWorld world,ItemRegistry registry,Vector3 feet,float width,float height,float hoverHeight=0,bool isNight=false)
            =>CheckSite(world,registry,feet,width,height,hoverHeight,isNight)==SpawnRejection.None;
        public SpawnRejection CheckSite(VoxelWorld world,ItemRegistry registry,Vector3 feet,float width,float height,float hoverHeight=0,bool isNight=false)
        {
            var min=world.Address(feet+new Vector3(-width*.5f+.001f,-hoverHeight-.03f,-width*.5f+.001f));
            var max=world.Address(feet+new Vector3(width*.5f-.001f,height,width*.5f-.001f));
            if(hoverHeight>0&&Mathf.Abs(feet.y-(world.Local(min).y+1+hoverHeight))>.04f)return SpawnRejection.UnsupportedFloor;
            for(long z=min.Z;z<=max.Z;z++)for(long x=min.X;x<=max.X;x++)
            {
                var support=new BlockPos(x,min.Y,z);
                if(!world.Ready(support))return SpawnRejection.Unloaded;
                if(!world.Solid(support)||!AllowsSupport(registry,world.Get(support)))return SpawnRejection.UnsupportedFloor;
                for(int y=min.Y+1;y<=max.Y;y++)
                {
                    var p=new BlockPos(x,y,z);
                    if(!world.Ready(p))return SpawnRejection.Unloaded;
                    if(Fluids.IsFluid(world.Get(p)))return SpawnRejection.Fluid;
                    if(world.Solid(p))return SpawnRejection.Blocked;
                }
                // Sample above the floor even for hovering bodies. Unrestricted profiles
                // need no lighting solve; restricted profiles defer unknown/stale results.
                if(minimumLight>0||maximumLight<15)
                {
                    if(!world.TryGetSpawnLight(support.Offset(0,1,0),isNight,out byte light))return SpawnRejection.UnknownLight;
                    if(light>maximumLight)return SpawnRejection.TooBright;
                    if(light<minimumLight)return SpawnRejection.TooDark;
                }
                var head=new BlockPos(x,max.Y,z);
                if(world.SkyLight(head)>0)
                {if(!Surface)return SpawnRejection.WrongHabitat;}
                else
                {
                    // Roofed surface buildings and open shafts are not underground caves.
                    if(!Underground||head.Y>=world.SurfaceHeightAt(head))return SpawnRejection.WrongHabitat;
                }
            }
            return SpawnRejection.None;
        }

        // Search bounds depend on habitat, not hostility. Full 3D range is checked by the caller.
        public void SearchHeights(VoxelWorld world,BlockPos observerColumn,int verticalRange,out int low,out int high)
        {
            int surface=world.SurfaceHeightAt(observerColumn);
            high=Underground?Math.Min(surface+(Surface?5:-1),observerColumn.Y+verticalRange):surface+5;
            low=Underground?Math.Max(TerrainGenerator.MinY+1,observerColumn.Y-verticalRange):surface-7;
        }
    }
}
