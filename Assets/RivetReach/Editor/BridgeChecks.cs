using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class BridgeChecks
    {
        sealed class World:IIndustryWorld
        {
            public readonly Dictionary<BlockPos,ItemContainer> Chests=new Dictionary<BlockPos,ItemContainer>();
            public readonly HashSet<ChunkPos> Sleeping=new HashSet<ChunkPos>();
            public bool Ready(BlockPos p)=>!Sleeping.Contains(p.Chunk);
            public byte Get(BlockPos p)=>0;public bool Remove(BlockPos p,byte id)=>false;
            public ItemContainer Storage(BlockPos p)=>Chests.TryGetValue(p,out var c)?c:null;
            public byte Drop(byte id)=>id;public bool PlayerInside(BlockPos p)=>false;
        }
        public static void Run()
        {
            var lines=new List<string>();void Check(bool ok,string message){if(!ok)throw new Exception("Bridge: "+message);lines.Add("PASS "+message);}
            var registry=ItemRegistry.Load();var recipes=RecipeCatalogAsset.Load().Compile(registry);
            TierCompatibility(registry,Check);
            foreach(byte id in new[]{IndustryId.ItemBridge,IndustryId.FluidBridge,IndustryId.PowerBridge,IndustryId.ChunkLoader})
            {
                Check(registry.Get(id).runtimeId==id&&IndustryId.Placed(id),"Registered placeable item "+id);
                Check(recipes.Recipes.Single(r=>r.Output.Id==id).MinimumGridSize==4,"Machinist bench recipe "+id);
                if(id!=IndustryId.ChunkLoader)
                {
                    var recipe=recipes.Recipes.Single(r=>r.Output.Id==id);
                    Check(recipe.Output.Count==2&&recipe.Ingredients.Count==5&&recipe.Ingredients.Any(i=>i.Id==BlockId.GoldIngot&&i.Count==2)&&recipe.Ingredients.Any(i=>i.Id==BlockId.Diamond&&i.Count==2),"Bridge pair requires two gold ingots and two diamonds: "+id);
                }
                var model=Resources.Load<GameObject>("Industry/Runtime/"+IndustryDefinition.All[id].Key);
                Check(model!=null&&model.GetComponentsInChildren<MeshFilter>().Length>0,"Imported original model "+id);
                int triangles=model.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);
                lines.Add("MEASURE imported "+id+": "+triangles+" triangles");
            }
            {
                var world=new World();var sim=new IndustrySimulation(world,id=>64);var a=sim.Add(new BlockPos(0,0,0),IndustryId.PowerBridge);var b=sim.Add(new BlockPos(1000,0,0),IndustryId.PowerBridge);
                bool Link(MachineState m,string name)=>sim.ConfigureBridge(m,sim.LocalOwnerId,name,out _);
                Check(Link(a,"Quarry")&&Link(b,"quarry"),"Case-insensitive owner-scoped pair configured");
                var battery=sim.Add(a.Position.Offset(-2,0,0),IndustryId.Battery);sim.Add(a.Position.Offset(-1,0,0),IndustryId.PowerCable);battery.EnergyCells[0].Charge(1000000);
                sim.Add(b.Position.Offset(1,0,0),IndustryId.PowerCable);var lamp=sim.Add(b.Position.Offset(2,0,0),IndustryId.Lamp);sim.Step();
                Check(sim.BridgePartner(a)==b&&lamp.ReceivedWatts==20&&battery.EnergyCells[0].Amount==999000,"Remote power uses exactly 1 J for one 20 W tick");
                var third=sim.Add(new BlockPos(2000,0,0),IndustryId.PowerBridge);Check(!Link(third,"Quarry")&&third.LinkName=="","Third endpoint rejected atomically");
                Check(!sim.ConfigureBridge(a,Guid.NewGuid().ToString("N"),"Other",out _)&&a.LinkName=="Quarry","Wrong owner cannot rename bridge");
                Check(!Link(a,"<b>bad</b>")&&!Link(a,new string('x',33))&&a.LinkName=="Quarry","Invalid name leaves existing pair intact");
                string owner=Guid.NewGuid().ToString("N");var foreign=sim.Add(new BlockPos(3000,0,0),IndustryId.PowerBridge);foreign.OwnerId=owner;
                Check(sim.ConfigureBridge(foreign,owner,"Quarry",out _),"Different player can reuse the same name");sim.Step();Check(sim.BridgePartner(foreign)==null&&sim.BridgePartner(a)==b,"Names never cross owner boundaries");
                var liquid=sim.Add(new BlockPos(4000,0,0),IndustryId.FluidBridge);Check(Link(liquid,"Quarry"),"Different channels may reuse a name");sim.Step();Check(sim.BridgePartner(liquid)==null,"Channels remain independent");
                world.Sleeping.Add(b.Position.Chunk);sim.Invalidate();sim.Step();long held=battery.EnergyCells[0].Amount;sim.Step();
                Check(lamp.ReceivedWatts==0&&battery.EnergyCells[0].Amount==held&&sim.BridgePartner(a)==null,"Dormant partner disconnects without energy loss");
                world.Sleeping.Clear();sim.Invalidate();sim.Step();Check(lamp.ReceivedWatts==20,"Residency automatically restores remote power");
                Check(Link(b,"Elsewhere"),"Reassign pair name");sim.Step();Check(lamp.ReceivedWatts==0,"Renaming disconnects previous remote grid");
                Check(Link(b,"Quarry"),"Rejoin pair");sim.Step();sim.Remove(a.Position);sim.Step();Check(lamp.ReceivedWatts==0,"Mining endpoint disconnects graph");
            }
            {
                var world=new World();var sim=new IndustrySimulation(world,id=>64);var a=sim.Add(new BlockPos(0,0,0),IndustryId.ItemBridge);var b=sim.Add(new BlockPos(1000,0,0),IndustryId.ItemBridge);
                sim.ConfigureBridge(a,sim.LocalOwnerId,"Cargo",out _);sim.ConfigureBridge(b,sim.LocalOwnerId,"Cargo",out _);
                var pa=sim.Add(a.Position.Offset(-1,0,0),IndustryId.ItemPipe);var pb=sim.Add(b.Position.Offset(1,0,0),IndustryId.ItemPipe);
                var source=new ItemContainer(3,id=>64);var sink=new ItemContainer(3,id=>64);world.Chests[a.Position.Offset(-2,0,0)]=source;world.Chests[b.Position.Offset(2,0,0)]=sink;
                source.Add(BlockId.FloaterRock,12);Check(sim.TogglePipeEnd(pa,1),"Configure distant cargo source");sim.Invalidate();for(int i=0;i<20;i++)sim.Step();
                Check(source.Total(BlockId.FloaterRock)==8&&sink.Total(BlockId.FloaterRock)==4,"Linked item graph preserves four items/second source budget and identity");
                sim.ConfigureBridge(b,sim.LocalOwnerId,"",out _);for(int i=0;i<20;i++)sim.Step();Check(sink.Total(BlockId.FloaterRock)==4,"Unlinked item bridge transfers nothing");
            }
            foreach(var fluid in new[]{Fluids.Water,Fluids.Lava})
            {
                var sim=new IndustrySimulation(new World(),id=>64);var a=sim.Add(new BlockPos(0,0,0),IndustryId.FluidBridge);var b=sim.Add(new BlockPos(1000,0,0),IndustryId.FluidBridge);
                sim.ConfigureBridge(a,sim.LocalOwnerId,"Liquid",out _);sim.ConfigureBridge(b,sim.LocalOwnerId,"Liquid",out _);
                var source=sim.Add(a.Position.Offset(-2,0,0),IndustryId.Tank);var sink=sim.Add(b.Position.Offset(2,0,0),IndustryId.Tank);
                sim.Add(a.Position.Offset(-1,0,0),IndustryId.FluidPipe);sim.Add(b.Position.Offset(1,0,0),IndustryId.FluidPipe);source.Fluid.Deposit(fluid,10000);
                for(int i=0;i<20;i++)sim.Step();Check(source.Fluid.Amount==8000&&sink.Fluid.Amount==2000&&sink.Fluid.Fluid==fluid,"Exact remote liquid budget and identity: "+fluid.StableId);
                sink.Fluid.Withdraw(2000);sink.Fluid.Deposit(fluid==Fluids.Water?Fluids.Lava:Fluids.Water,1000);sim.Step();Check(source.Fluid.Amount==8000&&sink.Fluid.Amount==1000,"Mixed-liquid connected graph rejects transfer");
            }
            {
                var sim=new IndustrySimulation(new World(),id=>64);var a=sim.Add(new BlockPos(-1,0,-1),IndustryId.ChunkLoader);var b=sim.Add(new BlockPos(-2,0,-2),IndustryId.ChunkLoader);
                Check(sim.LoaderChunks().Count()==1&&sim.LoaderChunks().Single().Equals(a.Position.Chunk),"Overlapping loaders share one negative-coordinate chunk ticket");
                Check(sim.ConfigureLoader(a,sim.LocalOwnerId,false)&&sim.LoaderChunks().Count()==1,"Disabling one loader retains the other's ticket");
                sim.Remove(b.Position);Check(!sim.LoaderChunks().Any(),"Removing final loader releases chunk ticket");
            }
            File.WriteAllLines("Logs/bridge-checks.txt",lines);Debug.Log("Bridge checks PASS "+lines.Count);
        }
        static void TierCompatibility(ItemRegistry registry,Action<bool,string> check)
        {
            var catalog=RecipeCatalogAsset.Load();
            var changed=catalog.recipes.Where(r=>new[]{"rivet:industry_180","rivet:industry_190","rivet:industry_191","rivet:industry_192"}.Contains(r.stableId)).ToArray();
            var original=changed.Select(r=>r.ingredients).ToArray();
            var store=new SaveStore("unused",registry);
            var entry=new SaveEntry{Id=Guid.NewGuid().ToString("N"),WorldId=Guid.NewGuid().ToString("N"),Name="Recipe tier compatibility",UtcTicks=DateTime.UtcNow.Ticks};
            byte[] Encode()=>new SaveStore("unused",registry).Encode(entry,w=>w.Write(731));
            void Accept(byte[] bytes,string name){using var reader=store.Open(bytes,out _);check(reader.ReadInt32()==731,name);}
            void Reject(string name){bool rejected=false;try{using var reader=store.Open(Encode(),out _);}catch(InvalidDataException){rejected=true;}check(rejected,name);}
            Accept(Encode(),"Current tiered recipe checkpoint opens");
            try
            {
                for(int i=0;i<changed.Length;i++)changed[i].ingredients=original[i].Take(changed[i].stableId=="rivet:industry_180"?2:3).ToArray();
                Accept(Encode(),"Pre-tiering bridge and ranged pump checkpoint opens without changing payload");
                changed[0].ingredients[0].count++;
                Reject("Changed original ingredient quantity still rejects in pre-tiering checkpoint");
                changed[0].ingredients[0].count--;
                var other=catalog.recipes.First(r=>r.stableId=="rivet:industry_193");var savedOutput=other.output;
                try{other.output.count++;Reject("Unrelated chunk loader recipe change still rejects");}
                finally{other.output=savedOutput;}
            }
            finally{for(int i=0;i<changed.Length;i++)changed[i].ingredients=original[i];}
            try
            {
                changed[0].ingredients=(RecipeCellData[])original[0].Clone();
                int gold=Array.FindIndex(changed[0].ingredients,c=>c.itemId=="rivet:gold_ingot");changed[0].ingredients[gold].count++;
                Reject("Unknown future gold quantity is not accepted as the historical recipe");
            }
            finally{for(int i=0;i<changed.Length;i++)changed[i].ingredients=original[i];}
        }
    }
}
