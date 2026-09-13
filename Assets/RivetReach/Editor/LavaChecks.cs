using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class LavaChecks
    {
        sealed class World:IFluidWorld
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public readonly FluidSimulation Solver=new FluidSimulation(Fluids.Registry);
            public byte Get(BlockPos p)=>Cells.TryGetValue(p,out var id)?id:p.Y<=0?BlockId.Stone:(byte)0;
            public bool TryRead(BlockPos p,out byte id){id=Get(p);return true;}
            public bool ChangeFluid(BlockPos p,byte expected,byte next){if(Get(p)!=expected)return false;Cells[p]=next;Solver.Changed(this,p);return true;}
            public void Put(BlockPos p,byte id){Cells[p]=id;Solver.Changed(this,p);}
            public void Run(int ticks){for(int i=0;i<ticks;i++)Solver.Step(this);}
        }
        public static void Run()
        {
            var lines=new List<string>();
            void Check(bool ok,string message){if(!ok)throw new Exception("Lava check: "+message);lines.Add("PASS "+message);}
            var lava=Fluids.Lava;var p=new BlockPos(0,1,0);var w=new World();w.Put(p,lava.Source);w.Run(19);
            Check(w.Get(p.Offset(1,0,0))==0,"Lava waits twenty fixed ticks before spreading");
            w.Run(1);Check(w.Get(p.Offset(1,0,0))==lava.Flow(1),"Lava spreads at one second intervals");
            w.Run(500);Check(w.Get(p.Offset(3,0,0))==lava.Flow(3)&&w.Get(p.Offset(4,0,0))==0,"Lava spreads three horizontal cells and sleeps");
            Check(w.Solver.Pending==0,"Settled lava requires no recurring volume scan");
            w.Put(p.Offset(2,0,0),lava.Source);w.Run(100);
            Check(w.Get(p.Offset(1,0,0))==lava.Flow(1),"Two lava sources never renew a source");
            var bag=new ItemContainer(1,id=>1);bag.Add(Fluids.EmptyBucket,1);
            Check(BucketTransfer.TryUse(w,bag,0,p,Fluids.Registry)&&bag.Slots[0].Id==Fluids.LavaBucket&&w.Get(p)==0,"Full inventory collects lava into its existing bucket slot");
            Check(BucketTransfer.TryUse(w,bag,0,p,Fluids.Registry)&&bag.Slots[0].Id==Fluids.EmptyBucket&&w.Get(p)==lava.Source,"Placing lava returns exactly one empty bucket");
            Check(!BucketTransfer.TryUse(w,bag,0,p.Offset(1,0,0),Fluids.Registry),"Flowing lava cannot be collected");
            w.Put(p.Offset(2,0,0),0);w.Put(p,0);w.Run(500);Check(w.Cells.Values.All(id=>!Fluids.IsFluid(id)),"Dependent lava drains after source removal");
            w=new World();w.Put(p,lava.Source);w.Put(p.Offset(1,0,0),Fluids.Water.Source);w.Run(200);
            Check(w.Get(p)==lava.Source&&w.Get(p.Offset(1,0,0))==Fluids.Water.Source,"Water and lava preserve distinct sources at contact");
            var health=new HealthState();void Damage(float amount,DamageKind kind)=>health.Damage(amount,kind,20);
            health.AdvanceHeat(1,true,false,Damage);Check(health.Hearts==16&&health.Burning,"Lava immediately ignites and deals four heat damage regardless of armor");
            health.AdvanceHeat(10,true,false,Damage);Check(health.Hearts==12,"Lava repeats four damage every half second");
            health.AdvanceHeat(20,false,false,Damage);Check(health.Hearts<12&&health.Burning,"Burn damage continues on dry land");
            health.AdvanceHeat(1,false,true,Damage);Check(!health.Burning,"Water extinguishes burning");
            health.AdvanceHeat(1,true,false,Damage);health.Respawn();Check(!health.Burning&&health.Hearts==20,"Respawn resets fire and restores health");
            var single=new HealthState();var batched=new HealthState();single.AdvanceHeat(1,true,false,(a,k)=>single.Damage(a,k,0));batched.AdvanceHeat(1,true,false,(a,k)=>batched.Damage(a,k,0));
            for(int i=0;i<80;i++)single.AdvanceHeat(1,false,false,(a,k)=>single.Damage(a,k,0));
            batched.AdvanceHeat(80,false,false,(a,k)=>batched.Damage(a,k,0));
            Check(single.Hearts==batched.Hearts&&!single.Burning&&!batched.Burning,"Burn expiration and damage are invariant to fixed-tick batching");
            foreach(int seed in new[]{246813,17,-99})
            {
                var generator=new TerrainGenerator(seed);var old=new TerrainGenerator(seed,TerrainGenerator.LegacyVersion);BlockPos site=default;bool found=false;
                for(int z=-128;z<=128&&!found;z+=4)for(int x=-128;x<=128&&!found;x+=4)
                {var cell=new BlockPos(x,TerrainGenerator.LavaLevel,z);if(generator.At(cell)==lava.Source){site=cell;found=true;}}
                Check(found,"Deep lava lake found for seed "+seed);
                var cells=generator.Generate(site.Chunk);var legacy=old.Generate(site.Chunk);int lavaCells=0;
                for(int z=-1;z<=32;z++)for(int y=-1;y<=32;y++)for(int x=-1;x<=32;x++)
                {
                    var cell=site.Chunk.Min.Offset(x,y,z);int i=ChunkMesher.Index(x,y,z);
                    CheckCell(cells[i]==generator.At(cell),"Point/page mismatch",cell);
                    CheckCell(legacy[i]==old.At(cell),"Legacy point/page mismatch",cell);
                    if(cells[i]==lava.Source){lavaCells++;CheckCell(legacy[i]==0&&cell.Y>TerrainGenerator.MinY+3&&cell.Y<=TerrainGenerator.LavaLevel,"Lava replaced protected or non-cave terrain",cell);}
                    else CheckCell(cells[i]==legacy[i],"Lava changed unrelated terrain",cell);
                }
                Check(lavaCells>0,"Chunk/halo reads agree; legacy caves, ores and protected bedrock preserved, seed "+seed+" ("+lavaCells+" lava cells)");
                Check(generator.At(new BlockPos(0,generator.Height(0,0)+1,0))==0,"Dry spawn retained, seed "+seed);
            }
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/lava-checks.txt",lines);Debug.Log(string.Join("\n",lines));
        }
        static void CheckCell(bool ok,string message,BlockPos p){if(!ok)throw new Exception(message+" "+p);}
    }
}
