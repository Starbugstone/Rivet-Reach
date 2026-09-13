using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class RangedPumpChecks
    {
        sealed class World : IIndustryWorld
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public readonly HashSet<BlockPos> Sleeping=new HashSet<BlockPos>();
            public int Reads,Removals;public bool RejectRemoval;
            public bool Ready(BlockPos p)=>!Sleeping.Contains(p);
            public byte Get(BlockPos p){Reads++;return Cells.TryGetValue(p,out var b)?b:(byte)0;}
            public bool Remove(BlockPos p,byte expected){if(RejectRemoval||!Ready(p)||Get(p)!=expected)return false;Cells.Remove(p);Removals++;return true;}
            public ItemContainer Storage(BlockPos p)=>null;public byte Drop(byte b)=>b;public bool PlayerInside(BlockPos p)=>false;
        }
        public static void Run()
        {
            var lines=new List<string>();void Check(bool ok,string message){if(!ok)throw new Exception("Ranged pump: "+message);lines.Add("PASS "+message);}
            var items=ItemRegistry.Load();var origin=new BlockPos(0,0,0);
            void Steps(IndustrySimulation s,int count){for(int i=0;i<count;i++)s.Step();}
            var recipe=RecipeCatalogAsset.Load().Compile(items).Recipes.Single(r=>r.Output.Id==IndustryId.RangedPump);
            Check(recipe.MinimumGridSize==4&&recipe.Output.Count==1,"Registered Machinist bench recipe produces one pump");
            Check(recipe.Ingredients.Count==2&&recipe.Ingredients.Any(i=>i.Id==IndustryId.Pump&&i.Count==1)&&recipe.Ingredients.Any(i=>i.Id==BlockId.FloaterRock&&i.Count==1),"Upgrade consumes exactly one Pump and one Floater Rock");
            Check(RecipeTransferChecks.Run(items,RecipeCatalogAsset.Load().Compile(items))>0,"All crafting recipes transfer and conserve ingredients");
            foreach(var liquid in new[]{Fluids.Water,Fluids.Lava})
            foreach(var pos in new[]{new BlockPos(-8,-8,-8),new BlockPos(8,8,8),new BlockPos(-8,8,8),new BlockPos(8,-8,-8)})
            {
                var w=new World();w.Cells[pos]=liquid.Source;var sim=new IndustrySimulation(w,id=>64);var m=sim.Add(origin,IndustryId.RangedPump);
                for(int search=0;search<25&&m.Work==0;search++)sim.Step();Check(m.Work==1&&w.Removals==0,"Source acquired at inclusive corner "+pos+" / "+liquid.DisplayName);
                Steps(sim,38);Check(m.Work==39&&m.Fluid.Amount==0,"No early output at 39 eligible ticks");sim.Step();
                Check(m.Fluid.Fluid==liquid&&m.Fluid.Amount==10000&&w.Removals==1&&w.Get(pos)==0,"Exact 10 L source removal at 40 ticks");
                Check(m.RequestedWatts==0&&!PipeConnections.Supports(m,NetworkKind.Power),"No electrical demand or endpoint");
                Steps(sim,80);Check(w.Removals==1&&m.Fluid.Amount==10000,"Full buffer cannot remove more sources");
            }
            {
                var w=new World();var sim=new IndustrySimulation(w,id=>64);var m=sim.Add(origin,IndustryId.RangedPump);
                foreach(int f in Enumerable.Range(0,6)){var d=IndustryDefinition.Directions[f];w.Cells[origin.Offset(d.x*9,d.y*9,d.z*9)]=Fluids.Lava.Source;}
                w.Cells[origin.Offset(0,1,0)]=Fluids.Lava.Flow(1);w.Cells[origin.Offset(0,-1,0)]=Fluids.Lava.Falling;
                Steps(sim,120);Check(w.Removals==0&&m.Fluid.Amount==0,"All six radius-nine faces and flowing/falling liquids are excluded");
                int maxReads=0;for(int i=0;i<100;i++){w.Reads=0;sim.Step();maxReads=Math.Max(maxReads,w.Reads);}Check(maxReads<=IndustrySimulation.RangedPumpScanBudget,"Per-tick empty search is bounded to 256 cell reads");
            }
            {
                var w=new World();var p=origin.Offset(0,8,0);w.Cells[p]=Fluids.Lava.Source;
                var sim=new IndustrySimulation(w,id=>64);var m=sim.Add(origin,IndustryId.RangedPump);for(int search=0;search<25&&m.Work==0;search++)sim.Step();Steps(sim,12);double work=m.Work;
                var lever=sim.Add(origin.Offset(0,0,-1),IndustryId.Lever);Steps(sim,20);Check(m.Work==work&&m.Status==MachineStatus.DisabledBySignal,"OFF signal preserves work");
                sim.Activate(lever);sim.Step();Check(m.Work==work+1,"ON signal resumes work");
                w.Sleeping.Add(p);Steps(sim,20);Check(m.Work==work+1&&w.Removals==0&&m.Status==MachineStatus.Dormant,"Unloaded source pauses without collection");
                w.Sleeping.Clear();w.RejectRemoval=true;Steps(sim,50);Check(m.Fluid.Amount==0&&w.Removals==0,"Rejected world transaction never yields fluid");
                w.RejectRemoval=false;sim.Step();Check(w.Removals==1&&m.Fluid.Amount==10000,"Successful retry yields one source only");
                m.Fluid.Withdraw(10000);w.Cells[p]=Fluids.Water.Source;Steps(sim,100);Check(m.Fluid.Fluid==Fluids.Water,"Emptied buffer can switch liquid type");
                m.Fluid.Withdraw(1);w.Cells[p]=Fluids.Lava.Source;Steps(sim,100);Check(w.Get(p)==Fluids.Lava.Source&&m.Fluid.Fluid==Fluids.Water,"Retained liquid blocks mixing and partial-capacity extraction");
            }
            {
                var w=new World();var p=origin.Offset(0,8,0);w.Cells[p]=Fluids.Lava.Source;var sim=new IndustrySimulation(w,id=>64);
                var a=sim.Add(origin,IndustryId.RangedPump);var b=sim.Add(origin.Offset(1,0,0),IndustryId.RangedPump);Steps(sim,180);
                Check(w.Removals==1&&a.Fluid.Amount+b.Fluid.Amount==10000,"Overlapping pumps cannot duplicate one finite source");
            }
            for(int face=0;face<6;face++)
            {
                var w=new World();var sim=new IndustrySimulation(w,id=>64);var m=sim.Add(origin,IndustryId.RangedPump);m.Fluid.Deposit(Fluids.Lava,10000);
                var pipe=IndustryDefinition.Neighbor(origin,face);var tank=sim.Add(IndustryDefinition.Neighbor(pipe,face),IndustryId.Tank);var conduit=sim.Add(pipe,IndustryId.FluidPipe);sim.Step();
                for(int turn=0;turn<3&&sim.PipeEndRole(conduit,face)!=PortRole.Input;turn++)sim.TogglePipeEnd(conduit,face);Steps(sim,100);
                Check(m.Fluid.Amount==0&&tank.Fluid.Amount==10000&&tank.Fluid.Fluid==Fluids.Lava,"All-face lava export conserves liquid on face "+face);
            }
            // Test the actual machine serializer rather than mirroring its record layout.
            {
                var w=new World();w.Cells[origin]=IndustryId.RangedPump;w.Cells[origin.Offset(0,8,0)]=Fluids.Lava.Source;
                var sim=new IndustrySimulation(w,id=>64);var m=sim.Add(origin,IndustryId.RangedPump);for(int search=0;search<25&&m.Work==0;search++)sim.Step();Steps(sim,16);
                m.Fluid.Deposit(Fluids.Lava,1234);double work=m.Work;
                using var bytes=new MemoryStream();using(var writer=new SaveWriter(bytes))typeof(IndustrySimulation).GetMethod("WriteSave",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(sim,new object[]{writer});
                bytes.Position=0;var restored=new IndustrySimulation(w,id=>64);using(var reader=new SaveReader(bytes,items))typeof(IndustrySimulation).GetMethod("ReadSave",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(restored,new object[]{reader});
                var saved=restored.At(origin);Check(saved.Work==work&&saved.Fluid.Amount==1234&&saved.Fluid.Fluid==Fluids.Lava,"Machine save round trip retains exact lava and partial work");
            }
            Compatibility(items,Check);
            var model=Resources.Load<GameObject>("Industry/Runtime/ranged_liquid_pump");Check(model!=null&&model.GetComponentsInChildren<MeshFilter>().Length==3,"Imported cradle, suspended core and status lamp");
            var filters=model.GetComponentsInChildren<MeshFilter>();
            Check(filters.All(f=>f.sharedMesh.vertices.All(v=>{var p=f.transform.TransformPoint(v);return p.x>=-.001f&&p.y>=-.001f&&p.z>=-.001f&&p.x<=1.001f&&p.y<=1.001f&&p.z<=1.001f;})),"Imported geometry remains inside one cell");
            lines.Add("Imported triangles: "+filters.Sum(f=>f.sharedMesh.triangles.Length/3)+"; mesh parts: "+filters.Length);
            lines.Insert(0,"PASS "+lines.Count(line=>line.StartsWith("PASS "))+" assertions");File.WriteAllLines("Logs/ranged-pump-checks.txt",lines);
        }
        static void Compatibility(ItemRegistry items,Action<bool,string> check)
        {
            var store=new SaveStore("unused",items);var old=ScriptableObject.CreateInstance<ItemRegistry>();var catalog=RecipeCatalogAsset.Load();var original=catalog.recipes;
            try
            {
                old.items=items.items.Where(i=>i.runtimeId!=IndustryId.RangedPump).Select(i=>JsonUtility.FromJson<ItemDefinition>(JsonUtility.ToJson(i))).ToArray();catalog.recipes=original.Where(r=>r.stableId!="rivet:industry_180").ToArray();
                var entry=new SaveEntry{Id=Guid.NewGuid().ToString("N"),WorldId=Guid.NewGuid().ToString("N"),Name="Pre-ranged content",UtcTicks=DateTime.UtcNow.Ticks};
                var bytes=new SaveStore("unused",old).Encode(entry,w=>w.Write(512));using(var r=store.Open(bytes,out _))check(r.ReadInt32()==512,"Pre-ranged current-schema content remains compatible");
                old.items[0].attackDamage++;bytes=new SaveStore("unused",old).Encode(entry,w=>w.Write(512));bool rejected=false;try{using var r=store.Open(bytes,out _);}catch(InvalidDataException){rejected=true;}check(rejected,"Unrelated earlier definition changes still reject");
            }
            finally{catalog.recipes=original;UnityEngine.Object.DestroyImmediate(old);}
        }
    }
}
