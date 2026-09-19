using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class CrateChecks
    {
        sealed class World : IIndustryWorld,IIndustryItemEndpoints
        {
            public readonly Dictionary<BlockPos,byte> Blocks=new Dictionary<BlockPos,byte>();
            public readonly Dictionary<BlockPos,CrateStorage> Crates=new Dictionary<BlockPos,CrateStorage>();
            public readonly Dictionary<BlockPos,IItemPipeInventory> Endpoints=new Dictionary<BlockPos,IItemPipeInventory>();
            public readonly HashSet<BlockPos> Dormant=new HashSet<BlockPos>();public readonly CrateNetwork Network;
            public World(){Network=new CrateNetwork(Get,p=>Crates.TryGetValue(p,out var c)?c:null,Ready);}
            public void Crate(BlockPos p){Blocks[p]=CrateId.Crate;Crates[p]=new CrateStorage(_=>64);Network.Invalidate();}
            public bool Ready(BlockPos p)=>!Dormant.Contains(p);
            public byte Get(BlockPos p)=>Blocks.TryGetValue(p,out var b)?b:(byte)0;
            public bool Remove(BlockPos p,byte expected)=>Blocks.Remove(p);
            public ItemContainer Storage(BlockPos p)=>Endpoints.TryGetValue(p,out var e)?e as ItemContainer:null;
            public IItemPipeInventory ItemEndpoint(BlockPos p)=>CrateId.Part(Get(p))?Network.At(p):Endpoints.TryGetValue(p,out var e)?e:null;
            public int ItemEndpointRotation(BlockPos p)=>0;
            public void ItemEndpointChanged(BlockPos p){}
            public byte Drop(byte b)=>b;public bool PlayerInside(BlockPos p)=>false;
        }
        public static void Run()
        {
            var log=new StringBuilder();int count=0;
            void Check(bool ok,string text){if(!ok)throw new Exception("Crates: "+text);log.AppendLine("PASS "+text);count++;}
            var c=new CrateStorage(_=>64);
            Check(c.Insert(new ItemStack(BlockId.Stone,20000))==16384&&c.Count==16384,"Exact capacity limits oversized deposit");
            Check(c.Insert(new ItemStack(BlockId.Stone,1))==0&&c.Insert(new ItemStack(BlockId.Dirt,1))==0,"Full or incompatible deposits do not mutate storage");
            Check(c.TakeStack(1000).Count==64&&c.Count==16320,"Cursor withdrawals obey ordinary stack limit");c.Take(int.MaxValue);
            Check(c.Item==0&&!c.Locked,"Unassigned again when an unlocked crate empties");
            Check(c.Lock(BlockId.Dirt)&&c.Count==0&&c.Item==BlockId.Dirt&&c.Insert(new ItemStack(BlockId.Stone,1))==0,"Empty manual lock reserves item type without material consumption");
            c.Insert(new ItemStack(BlockId.Dirt,10));c.Take(10);Check(c.Item==BlockId.Dirt&&c.Locked,"Empty lock survives withdrawals");c.Unlock();Check(c.Item==0,"Unlock empty crate clears assignment");
            var charged=new ItemStack(IndustryId.Battery,1){Energy=123};Check(c.Insert(charged)==0&&charged.Energy==123,"Metadata-bearing storage is rejected without changing its contents");
            var single=new CrateStorage(_=>1);single.Insert(new ItemStack(BlockId.IronAxe,100));Check(single.TakeStack(50).Count==1&&single.Count==99,"Nonstackable equipment still withdraws singly");
            var w=new World();var p=new BlockPos(0,20,0);w.Blocks[p]=CrateId.Controller;
            for(int i=1;i<=64;i++)w.Crate(p.Offset(i,0,0));var bank=w.Network.At(p);
            Check(bank.Members.Count==64&&bank.ItemInputPriority==40,"Controller discovers exactly 64 face-connected crates");
            var first=w.Crates[p.Offset(1,0,0)];var second=w.Crates[p.Offset(2,0,0)];second.Insert(new ItemStack(BlockId.Stone,10));
            Check(bank.Insert(new ItemStack(BlockId.Stone,5))==5&&first.Count==0&&second.Count==15,"Existing assigned crate receives before earlier empty crate");
            Check(ReferenceEquals(bank.SourceIdentity(1),second)&&bank.SharesStorage(w.Network.At(p.Offset(2,0,0))),"Controller and direct pipe share physical source identity");
            w.Crate(p.Offset(65,0,0));Check(bank.Members.Count==0&&bank.Status.Contains("64")&&bank.Insert(new ItemStack(BlockId.Stone,1))==0,"65th connected crate invalidates aggregate rather than silently losing capacity");
            w.Blocks.Remove(p.Offset(65,0,0));w.Crates.Remove(p.Offset(65,0,0));w.Network.Invalidate();Check(bank.Members.Count==64&&second.Count==15,"Removing excess crate restores aggregate with retained stock");
            w.Dormant.Add(p.Offset(1,0,0));w.Network.Invalidate();Check(bank.Members.Count==0&&second.Count==15,"Unloaded crate cannot bridge a controller to resident stock");w.Dormant.Clear();w.Network.Invalidate();
            w.Blocks[p.Offset(-1,0,0)]=CrateId.Controller;w.Network.Invalidate();Check(bank.Members.Count==0&&bank.Status.Contains("one controller"),"Multiple controllers invalidate a connected bank");w.Blocks.Remove(p.Offset(-1,0,0));w.Network.Invalidate();
            w.Blocks.Remove(p);w.Network.Removed(p);Check(second.Count==15&&w.Network.At(p.Offset(2,0,0)).Insert(new ItemStack(BlockId.Stone,1))==1,"Controller removal preserves individually usable crate contents");
            Pipes(Check);Aliases(Check);Sensors(Check);Priorities(Check);RoutingCost(Check);Congestion(Check);RecipesAndCompatibility(Check);
            Directory.CreateDirectory("Logs/Crates");log.AppendLine(count+" checks passed");File.WriteAllText("Logs/Crates/editor-checks.txt",log.ToString());
        }
        static void Settle(IndustrySimulation sim){for(int i=0;i<400;i++){sim.Step();if(!sim.Rebuilding&&sim.Multiblocks.PendingCount==0)break;}while(sim.Tick%5!=0)sim.Step();}
        static void Phase(IndustrySimulation sim){for(int i=0;i<5;i++)sim.Step();}
        static void Pipes(Action<bool,string> check)
        {
            var w=new World();var sim=new IndustrySimulation(w,_=>64);var p=new BlockPos(0,20,0);w.Blocks[p]=IndustryId.ItemPipe;var pipe=sim.Add(p,IndustryId.ItemPipe);pipe.PipeDirections=2;
            var source=new ItemContainer(1,_=>64);var chest=new ItemContainer(1,_=>64);var processing=ProcessingCatalogAsset.Load().Compile(ItemRegistry.Load());var furnace=new FurnaceState(processing,_=>64);
            var src=IndustryDefinition.Neighbor(p,0);var machine=IndustryDefinition.Neighbor(p,1);var controller=IndustryDefinition.Neighbor(p,2);var crate=IndustryDefinition.Neighbor(p,3);var target=IndustryDefinition.Neighbor(p,4);
            w.Blocks[src]=BlockId.Chest;w.Endpoints[src]=source;w.Blocks[machine]=BlockId.Furnace;w.Endpoints[machine]=furnace;
            w.Blocks[controller]=CrateId.Controller;w.Crate(controller.Offset(1,0,0));var warehouse=w.Crates[controller.Offset(1,0,0)];w.Crate(crate);var standalone=w.Crates[crate];w.Blocks[target]=BlockId.Chest;w.Endpoints[target]=chest;Settle(sim);
            source.Add(BlockId.RawIron,10);Phase(sim);check(furnace.Slots[0].Count==1&&warehouse.Count==0&&standalone.Count==0&&chest.Total(BlockId.RawIron)==0,"Machine input takes precedence over all storage tiers");
            for(int i=1;i<64;i++)((IItemPipeInventory)furnace).TryInsert(BlockId.RawIron,0);Phase(sim);
            check(warehouse.Count==1&&standalone.Count==0&&chest.Total(BlockId.RawIron)==0,"Full machine falls through to controller before single crate/chest");
            warehouse.Insert(new ItemStack(BlockId.RawIron,16384));Phase(sim);check(standalone.Count==1&&chest.Total(BlockId.RawIron)==0,"Full controller falls through to individual crate before chest");
            standalone.Insert(new ItemStack(BlockId.RawIron,16384));Phase(sim);check(chest.Total(BlockId.RawIron)==1&&source.Total(BlockId.RawIron)==6,"Full crates fall through to chest with exact source conservation");
            chest.Add(BlockId.RawIron,64);int before=source.Total(BlockId.RawIron);Phase(sim);check(source.Total(BlockId.RawIron)==before,"All full destinations retain source stock");
            furnace.Take(0,64);warehouse.Take(int.MaxValue);standalone.Take(int.MaxValue);chest.Take(0,64);source.Take(0,64);warehouse.Lock(BlockId.Dirt);standalone.Lock(BlockId.Dirt);source.Add(FarmId.FlaxSeed,1);Phase(sim);check(chest.Total(FarmId.FlaxSeed)==1&&source.Total(FarmId.FlaxSeed)==0,"Incompatible machine and locked warehouses fall through to chest");
        }
        static void Aliases(Action<bool,string> check)
        {
            var w=new World();var sim=new IndustrySimulation(w,_=>64);var a=new BlockPos(0,20,0);w.Crate(a);w.Blocks[a.Offset(1,0,0)]=CrateId.Controller;var chest=new ItemContainer(1,_=>64);w.Blocks[a.Offset(3,0,0)]=BlockId.Chest;w.Endpoints[a.Offset(3,0,0)]=chest;
            var pipes=new MachineState[4];for(int i=0;i<4;i++){var q=a.Offset(i,1,0);w.Blocks[q]=IndustryId.ItemPipe;pipes[i]=sim.Add(q,IndustryId.ItemPipe);}
            pipes[0].PipeDirections=pipes[1].PipeDirections=2<<(3*2);Settle(sim);w.Crates[a].Insert(new ItemStack(BlockId.Stone,10));Phase(sim);
            check(w.Crates[a].Count==9&&chest.Total(BlockId.Stone)==1,"Controller/direct output aliases emit once per physical crate per phase");
            pipes[0].PipeDirections=1<<(3*2);sim.Invalidate();Settle(sim);int before=chest.Total(BlockId.Stone);Phase(sim);
            check(chest.Total(BlockId.Stone)==before+1,"Controller output skips its own member input and reaches external chest");
            pipes[0].PipeDirections=2<<(3*2);pipes[1].PipeDirections=1<<(3*2);sim.Invalidate();Settle(sim);before=chest.Total(BlockId.Stone);Phase(sim);
            check(chest.Total(BlockId.Stone)==before+1,"Direct crate output skips controller alias instead of self-circulating");
        }
        static void Priorities(Action<bool,string> check)
        {
            var w=new World();var sim=new IndustrySimulation(w,_=>64);var p=new BlockPos(0,20,0);w.Blocks[p]=IndustryId.ItemPipe;var pipe=sim.Add(p,IndustryId.ItemPipe);pipe.PipeDirections=2;
            var source=new ItemContainer(4,_=>64);var left=new ItemContainer(4,_=>64);var right=new ItemContainer(4,_=>64);
            foreach(var pair in new[]{(0,source),(1,left),(2,right)}){var q=IndustryDefinition.Neighbor(p,pair.Item1);w.Blocks[q]=BlockId.Chest;w.Endpoints[q]=pair.Item2;}Settle(sim);
            source.Add(BlockId.Stone,32);left.ItemInputPriority=80;right.ItemInputPriority=20;int rebuilds=sim.TopologyRebuilds;Phase(sim);check(left.Total(BlockId.Stone)==1&&right.Total(BlockId.Stone)==0,"Custom higher numeric priority overrides default ordering");
            right.ItemInputPriority=80;for(int i=0;i<6;i++)Phase(sim);check(left.Total(BlockId.Stone)==4&&right.Total(BlockId.Stone)==3,"Equal custom priorities round robin one source across six deliveries");check(sim.TopologyRebuilds==rebuilds,"Priority edits rebucket endpoints without rebuilding pipe topology");
            var qcrate=IndustryDefinition.Neighbor(p,4);w.Crate(qcrate);w.Crates[qcrate].Lock(BlockId.Dirt);w.Network.At(qcrate).ItemInputPriority=100;sim.Invalidate();Settle(sim);int before=left.Total(BlockId.Stone)+right.Total(BlockId.Stone);Phase(sim);check(w.Crates[qcrate].Count==0&&left.Total(BlockId.Stone)+right.Total(BlockId.Stone)==before+1,"Highest-priority incompatible locked crate is never a valid destination");
            left.ItemInputPriority=0;right.ItemInputPriority=100;before=right.Total(BlockId.Stone);Phase(sim);check(right.Total(BlockId.Stone)==before+1,"Priority zero remains a valid overflow tier and 100 is served first");
        }
        static void RoutingCost(Action<bool,string> check)
        {
            var report=new StringBuilder();
            foreach(int length in new[]{16,1024})
            {
                var w=new World();var sim=new IndustrySimulation(w,_=>64);var p=new BlockPos(0,20,0);
                for(int x=0;x<length;x++){var q=p.Offset(x,0,0);w.Blocks[q]=IndustryId.ItemPipe;sim.Add(q,IndustryId.ItemPipe);}
                var source=new ItemContainer(4,_=>64);var sink=new ItemContainer(4,_=>64);var a=p.Offset(-1,0,0);var b=p.Offset(length,0,0);w.Blocks[a]=w.Blocks[b]=BlockId.Chest;w.Endpoints[a]=source;w.Endpoints[b]=sink;
                sim.At(p).PipeDirections=2<<(1*2);Settle(sim);var samples=new double[200];long allocated=0;int builds=sim.ItemRouteCacheBuilds;long probes=sim.ItemReceiverProbes;
                for(int i=0;i<200;i++){source.Add(BlockId.Stone,1);Phase(sim);sink.Take(0,64);samples[i]=sim.LastItemRoutingMs;allocated+=sim.LastItemRoutingAllocatedBytes;}
                Array.Sort(samples);check(sim.ItemRouteCacheBuilds==builds&&sim.ItemReceiverProbes-probes==200,"Stable "+length+"-pipe route reuses topology and probes one receiver per delivered item");
                check(allocated<65536,"Stable "+length+"-pipe routing uses bounded low managed allocations over 200 phases");report.AppendLine($"{length} pipes, 1 source / 1 receiver, 200 active phases: routing median {samples[100]:F6} ms, p95 {samples[190]:F6} ms, allocated {allocated} bytes, route cache rebuilds {sim.ItemRouteCacheBuilds-builds}, receiver probes {sim.ItemReceiverProbes-probes}.");
            }
            File.WriteAllText("Logs/Crates/routing-benchmark.txt",report.ToString());
        }
        static void Congestion(Action<bool,string> check)
        {
            var w=new World();var sim=new IndustrySimulation(w,_=>64);var p=new BlockPos(0,20,0);var sources=new List<ItemContainer>();var sinks=new List<ItemContainer>();
            for(int i=0;i<128;i++)
            {
                var q=p.Offset(i,0,0);w.Blocks[q]=IndustryId.ItemPipe;var pipe=sim.Add(q,IndustryId.ItemPipe);var storage=new ItemContainer(1,_=>64);var end=q.Offset(0,1,0);w.Blocks[end]=BlockId.Chest;w.Endpoints[end]=storage;
                if(i<64){sources.Add(storage);pipe.PipeDirections=2<<(2*2);}else sinks.Add(storage);
            }
            Settle(sim);foreach(var sink in sinks)sink.Add(BlockId.Stone,64);foreach(var source in sources)source.Add(BlockId.Stone,64);Phase(sim);
            long probes=sim.ItemReceiverProbes,allocated=0;var samples=new double[200];
            for(int i=0;i<200;i++){Phase(sim);allocated+=sim.LastItemRoutingAllocatedBytes;samples[i]=sim.LastItemRoutingMs;}
            Array.Sort(samples);check(sim.ItemReceiverProbes-probes==64*200,"64 congested sources probe each full receiver once per phase, not 4096 times");check(sources.All(s=>s.Total(BlockId.Stone)==64)&&sinks.All(s=>s.Total(BlockId.Stone)==64),"Congestion preserves every source and destination item");check(allocated==0,"Warmed congested routing allocates zero managed bytes across 200 phases");
            foreach(var sink in sinks)sink.Take(0,64);Phase(sim);check(sinks.All(s=>s.Total(BlockId.Stone)==1)&&sources.All(s=>s.Total(BlockId.Stone)==63),"Opening all 64 receivers recovers next phase with equal round-robin shares");
            File.AppendAllText("Logs/Crates/routing-benchmark.txt",$"128 pipes, 64 sources / 64 full receivers, 200 phases: routing median {samples[100]:F6} ms, p95 {samples[190]:F6} ms, allocated {allocated} bytes, probes {sim.ItemReceiverProbes-probes-64}.\n");
        }
        static void Sensors(Action<bool,string> check)
        {
            var w=new World();var sim=new IndustrySimulation(w,_=>64);var p=new BlockPos(0,20,0);w.Blocks[p]=IndustryId.Sensor;var sensor=sim.Add(p,IndustryId.Sensor);var q=IndustryDefinition.Neighbor(p,4);w.Crate(q);Settle(sim);
            w.Crates[q].Insert(new ItemStack(BlockId.Stone,31));Phase(sim);check(!sensor.Source,"Inventory sensor remains off below 32 crate items");w.Crates[q].Insert(new ItemStack(BlockId.Stone,1));Phase(sim);check(sensor.Source,"Inventory sensor counts physical crate stock");
            w.Blocks[q]=CrateId.Controller;w.Network.Removed(q);w.Crates.Remove(q);w.Crate(q.Offset(1,0,0));w.Crate(q.Offset(2,0,0));w.Crates[q.Offset(1,0,0)].Insert(new ItemStack(BlockId.Stone,16));w.Crates[q.Offset(2,0,0)].Insert(new ItemStack(BlockId.Dirt,16));sim.Invalidate();Settle(sim);check(sensor.Source,"Inventory sensor sums connected controller stock");w.Dormant.Add(q);w.Network.Invalidate();Phase(sim);check(!sensor.Source,"Inventory sensor ignores dormant controller");
        }
        static void RecipesAndCompatibility(Action<bool,string> check)
        {
            var items=ItemRegistry.Load();var catalog=RecipeCatalogAsset.Load();var compiled=catalog.Compile(items);
            foreach(var key in new[]{"rivet:bulk_crate","rivet:crate_controller"})
            {
                var recipe=compiled.Recipes.Single(r=>r.Id==key);var craft=new CraftingSession(compiled,recipe.MinimumGridSize,id=>items.Get(id).stackLimit);var inventory=new Inventory(id=>items.Get(id).stackLimit);
                foreach(var cell in catalog.recipes.Single(r=>r.stableId==key).ingredients)inventory.Add(items.ResolveId(cell.itemId),cell.count);
                check(craft.FillRecipe(key,inventory)==RecipeFillStatus.Filled,"Fill "+key+" from exact construction ingredients");
                ItemStack cursor=default;check(craft.CraftToCursor(ref cursor).Succeeded&&cursor.Id==recipe.Output.Id&&cursor.Count==1&&craft.Grid.Slots.All(s=>s.Empty)&&inventory.Slots.All(s=>s.Empty),"Conserve every input crafting "+key);
            }
            var old=ScriptableObject.CreateInstance<ItemRegistry>();var originals=catalog.recipes;
            try
            {
                var current=new SaveStore("unused",items);old.items=items.items.Where(i=>!CrateId.Part(i.runtimeId)).Select(i=>JsonUtility.FromJson<ItemDefinition>(JsonUtility.ToJson(i))).ToArray();catalog.recipes=originals.Where(r=>r.stableId!="rivet:bulk_crate"&&r.stableId!="rivet:crate_controller").ToArray();
                var previous=new SaveStore("unused",old);var entry=new SaveEntry{Id=new string('a',32),WorldId=new string('b',32),Name="pre-crates",Seed=1,UtcTicks=DateTime.UtcNow.Ticks,GeneratorVersion=TerrainGenerator.Version};
                using(var r=current.Open(previous.Encode(entry,w=>w.Write(314)),out _))check(r.ReadInt32()==314,"Exact pre-crate definitions remain compatible");
                old.Get(BlockId.Stone).fistSeconds+=.1f;old.InvalidateIndex();bool rejected=false;try{using var r=current.Open(new SaveStore("unused",old).Encode(entry,w=>w.Write(314)),out _);}catch(InvalidDataException){rejected=true;}check(rejected,"Unrelated definition changes remain rejected");
            }
            finally{catalog.recipes=originals;UnityEngine.Object.DestroyImmediate(old);}
        }
    }
}
