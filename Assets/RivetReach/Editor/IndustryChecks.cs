using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class IndustryChecks
    {
        sealed class World : IIndustryWorld
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public readonly Dictionary<BlockPos,ItemContainer> Chests=new Dictionary<BlockPos,ItemContainer>();
            public readonly HashSet<BlockPos> Sleeping=new HashSet<BlockPos>();
            public bool Ready(BlockPos p)=>!Sleeping.Contains(p);
            public byte Get(BlockPos p)=>Cells.TryGetValue(p,out byte b)?b:(byte)0;
            public bool Remove(BlockPos p,byte expected){if(!Ready(p)||Get(p)!=expected)return false;Cells.Remove(p);return true;}
            public ItemContainer Storage(BlockPos p)=>Chests.TryGetValue(p,out var c)?c:null;
            public byte Drop(byte block)=>ItemRegistry.Load().FistDrop(block);
            public bool PlayerInside(BlockPos p)=>false;
        }
        public static void Run()
        {
            var report=new StringBuilder();int assertions=0;
            void Check(bool ok,string message){if(!ok)throw new Exception("Industry: "+message);assertions++;report.AppendLine("PASS "+message);}
            CrusherRecipes(Check);
            BlockPos P(int x,int y=0,int z=0)=>new BlockPos(x,y,z);
            var world=new World();var sim=new IndustrySimulation(world,id=>64);
            void Settle(){for(int n=0;n<10000;n++){sim.Step();if(!sim.Rebuilding)return;}throw new Exception("Topology did not settle");}
            var lever=sim.Add(P(0),IndustryId.Lever);var wire=sim.Add(P(1),IndustryId.SignalWire);var indicator=sim.Add(P(2),IndustryId.Indicator);sim.Rotate(indicator);
            sim.Activate(lever);Settle();Check(indicator.Signal,"Lever powers an explicitly connected indicator without electricity");
            sim.Remove(P(1));Settle();Check(!indicator.Signal,"Removing wire splits signal graph");
            sim.Add(P(1),IndustryId.SignalConduit);Settle();Check(indicator.Signal,"Conduit reconnects the graph");
            sim.Remove(P(1));sim.Add(P(1),IndustryId.ItemPipe);Settle();Check(!indicator.Signal,"Ordinary item pipes do not transmit signal");
            sim.Remove(P(1));sim.Add(P(1),IndustryId.FluidPipe);Settle();Check(!indicator.Signal,"Ordinary fluid pipes do not transmit signal");
            sim.Remove(P(1));sim.Add(P(1),IndustryId.SignalConduit);world.Sleeping.Add(P(1));sim.Invalidate();Settle();Check(!indicator.Signal,"Dormant intermediate segments break connectivity");
            world.Sleeping.Clear();sim.Invalidate();Settle();Check(indicator.Signal,"Residency restore reconnects without offline credit");
            sim.Activate(lever);sim.Step();Check(!indicator.Signal,"Switch OFF propagates deterministically");
            var relay=sim.Add(P(10),IndustryId.Relay);var button=sim.Add(P(10,0,1),IndustryId.Button);var pilot=sim.Add(P(10,0,-1),IndustryId.Indicator);sim.Rotate(pilot);sim.Rotate(pilot);
            Settle();sim.Activate(button);sim.Step();Check(relay.Signal&&!pilot.Signal,"Relay samples input without zero-delay output");sim.Step();Check(pilot.Signal,"Relay publishes on the following tick");
            for(int i=0;i<22;i++)sim.Step();Check(!pilot.Signal,"Button expires after one-second pulse plus relay delay");
            var engine=sim.Add(P(20),IndustryId.Boiler);engine.WaterMl=100000;engine.Items.Add(BlockId.Charcoal,2,0,1);
            var alt=sim.Add(P(21),IndustryId.Alternator);
            for(int x=21;x<=24;x++)sim.Add(P(x,0,1),IndustryId.PowerCable);
            var crusher=sim.Add(P(24),IndustryId.Crusher);crusher.Items.Add(BlockId.RawIron,5,0,1);Settle();
            Check(alt.SupplyWatts==400&&crusher.ReceivedWatts==160,"Adjacent mechanical shaft generates electricity for crusher");
            for(int i=0;i<100;i++)sim.Step();Check(crusher.Items.Total(IndustryId.CrushedIron)==2&&crusher.Items.Total(BlockId.RawIron)==4,"Crusher conserves one raw input into two authored outputs");
            var control=sim.Add(P(24,0,-1),IndustryId.Lever);Settle();double work=crusher.Work;
            Check(crusher.SignalAttached&&!crusher.Signal&&crusher.RequestedWatts==0,"Attached OFF signal requests zero processing watts");
            for(int i=0;i<15;i++)sim.Step();Check(crusher.Work==work,"Signal-disabled machine retains partial work");
            sim.Activate(control);sim.Step();Check(crusher.RequestedWatts==160,"Re-enabled machine requests normal power");
            crusher.Items.Add(IndustryId.CrushedIron,64,2,3);sim.Step();Check(crusher.Status==MachineStatus.OutputFull&&crusher.RequestedWatts==0,"Full output stops work and new power demand");
            crusher.Items.Take(2,64);sim.Remove(P(22,0,1));Settle();Check(crusher.ReceivedWatts==0&&crusher.Status==MachineStatus.NoPower,"Cable split removes power without consuming input");
            sim.Add(P(22,0,1),IndustryId.PowerCable);
            var c2=sim.Add(P(23,0,2),IndustryId.Crusher);sim.Rotate(c2);sim.Rotate(c2);c2.Items.Add(BlockId.RawIron,5,0,1);
            var c3=sim.Add(P(22,0,2),IndustryId.Crusher);sim.Rotate(c3);sim.Rotate(c3);c3.Items.Add(BlockId.RawIron,5,0,1);Settle();
            Check(crusher.ReceivedWatts+c2.ReceivedWatts+c3.ReceivedWatts==400,"Allocator distributes exactly the available supply");
            Check(Math.Abs(crusher.ReceivedWatts-c2.ReceivedWatts)<=1&&c3.Status==MachineStatus.Underpowered,"Equal priorities share shortages proportionally");
            c3.Priority=0;sim.Step();Check(c3.ReceivedWatts==160&&crusher.ReceivedWatts+c2.ReceivedWatts==240,"Priority loads are served before proportional lower-priority loads");
            var source=sim.Add(P(40),IndustryId.Tank);var pipe=sim.Add(P(41),IndustryId.FluidPipe);var dest=sim.Add(P(42),IndustryId.Tank);source.WaterMl=10000;Settle();
            Check(source.WaterMl+dest.WaterMl==10000&&dest.WaterMl>0,"Fluid transfers conserve exact millilitres");
            world.Sleeping.Add(P(41));sim.Invalidate();Settle();int water=dest.WaterMl;sim.Step();Check(dest.WaterMl==water,"Dormant fluid segment blocks transfer");
            var outChest=new ItemContainer(27,id=>64);world.Chests[P(24,0,3)]=outChest;
            sim.Add(P(25),IndustryId.ItemPipe);sim.Add(P(25,0,1),IndustryId.ItemPipe);sim.Add(P(25,0,2),IndustryId.ItemPipe);sim.Add(P(25,0,3),IndustryId.ItemPipe);
            crusher.Items.Add(IndustryId.CrushedIron,2,2,3);Settle();int total=crusher.Items.Total(IndustryId.CrushedIron)+outChest.Total(IndustryId.CrushedIron);for(int i=0;i<5;i++)sim.Step();
            Check(outChest.Total(IndustryId.CrushedIron)>0&&crusher.Items.Total(IndustryId.CrushedIron)+outChest.Total(IndustryId.CrushedIron)==total,"Item pipes move existing output to chest without duplication");
            // Large static graph and a ring: bounded topology and no per-tick rebuild.
            var scaleWorld=new World();var scale=new IndustrySimulation(scaleWorld,id=>64);
            for(int i=0;i<10000;i++)scale.Add(P(i,20),IndustryId.SignalConduit);
            var clock=Stopwatch.StartNew();int steps=0;do{scale.Step();steps++;}while(scale.Rebuilding&&steps<100);
            Check(!scale.Rebuilding&&steps>1,"10,000-node topology rebuild is spread over bounded steps");
            int rebuilds=scale.TopologyRebuilds;double[] times=new double[200];
            for(int i=0;i<times.Length;i++){scale.Step();times[i]=scale.LastStepMs;}
            Check(scale.TopologyRebuilds==rebuilds,"Stable topology is cached across simulation ticks");Array.Sort(times);
            report.AppendLine($"10,000 eligible signal nodes: rebuild steps {steps}; steady p50 {times[100]:F3} ms; p95 {times[190]:F3} ms; max {times[199]:F3} ms. Editor CPU: {SystemInfo.processorType}");
            // Active mixed machine workload, separate from the cached-wire benchmark.
            var activeWorld=new World();var factory=new IndustrySimulation(activeWorld,id=>64);
            for(int i=0;i<1000;i++)
            {
                int x=i*8;var boiler=factory.Add(P(x),IndustryId.Boiler);boiler.WaterMl=100000;boiler.Items.Add(BlockId.Charcoal,1,0,1);
                factory.Add(P(x+1),IndustryId.Alternator);var processor=factory.Add(P(x+3),IndustryId.Crusher);processor.Items.Add(BlockId.RawIron,64,0,1);
                for(int wireX=x+1;wireX<=x+3;wireX++)factory.Add(P(wireX,0,1),IndustryId.PowerCable);
            }
            do{factory.Step();}while(factory.Rebuilding);
            double[] activeTimes=new double[200];for(int i=0;i<activeTimes.Length;i++){factory.Step();activeTimes[i]=factory.LastStepMs;}Array.Sort(activeTimes);
            Check(factory.Machines.Values.Where(m=>m.Definition.Id==IndustryId.Crusher).All(m=>m.Items.Total(IndustryId.CrushedIron)==4&&m.Items.Total(BlockId.RawIron)==62),"1,000 concurrently powered crushers preserve independent resource accounting");
            report.AppendLine($"1,000 active boiler/alternator/crusher chains + 3,000 cable nodes: p50 {activeTimes[100]:F3} ms; p95 {activeTimes[190]:F3} ms; max {activeTimes[199]:F3} ms. Domain simulation only, no rendering.");
            // Finite extraction and complete-output backpressure.
            var miningWorld=new World();var mining=new IndustrySimulation(miningWorld,id=>64);
            var mb=mining.Add(P(0),IndustryId.Boiler);mb.WaterMl=100000;mb.Items.Add(BlockId.Charcoal,1,0,1);mining.Add(P(1),IndustryId.Alternator);
            for(int x=1;x<=5;x++)mining.Add(P(x,0,1),IndustryId.PowerCable);
            var md=mining.Add(P(3),IndustryId.Drill);var mp=mining.Add(P(5),IndustryId.Pump);
            miningWorld.Cells[P(3,-1)]=BlockId.Stone;miningWorld.Cells[P(3,-2)]=BlockId.Bedrock;miningWorld.Cells[P(5,-1)]=Fluids.Water.Source;
            for(int i=0;i<150;i++)mining.Step();
            Check(md.Items.Total(BlockId.Cobblestone)==1&&miningWorld.Get(P(3,-1))==0&&miningWorld.Get(P(3,-2))==BlockId.Bedrock&&md.Status==MachineStatus.Depleted,"Drill removes real finite terrain once and preserves bedrock");
            Check(mp.WaterMl==10000&&miningWorld.Get(P(5,-1))==0&&mp.RequestedWatts==0,"Pump consumes one real source for 10 L and stops when its output is full");
            mp.WaterMl=0;miningWorld.Cells[P(5,-1)]=Fluids.Water.Flow(1);mining.Step();Check(mp.Status==MachineStatus.NoWater&&mp.RequestedWatts==0,"Flowing water cannot create free pump source volume");
            var registry=ItemRegistry.Load();Check(!BlockId.Mineable(IndustryId.AzureOre,ToolCapability.Pickaxe,ToolTier.Stone)&&BlockId.Mineable(IndustryId.AzureOre,ToolCapability.Pickaxe,ToolTier.Copper),"Azure mining unlocks specifically at Copper");
            var band=OreGenerator.Bands.Single(b=>b.Block==IndustryId.AzureOre);Check(band.MinY==-96&&band.MaxY==8&&band.PeakY==-40,"Azure uses the shared finite ore band pipeline");
            var generator=new TerrainGenerator(246813);int azure=0;
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            {
                var pos=new ChunkPos(x,-2,z);var a=generator.Generate(pos);var b=new TerrainGenerator(246813).Generate(pos);
                Check(a.SequenceEqual(b),"Azure generation repeats at "+pos);
                for(int y=0;y<32;y++)for(int zz=0;zz<32;zz++)for(int xx=0;xx<32;xx++)if(a[ChunkMesher.Index(xx,y,zz)]==IndustryId.AzureOre){azure++;Check(generator.At(pos.Min.Offset(xx,y,zz))==IndustryId.AzureOre,"Azure stamped and point lookup agree");}
            }
            Check(azure>0,"Azure is present in generated normal stone");
            var imported=new StringBuilder();
            foreach(var d in IndustryDefinition.All.Values)
            {
                var prefab=Resources.Load<GameObject>("Industry/Runtime/"+d.Key);Check(prefab!=null,"Imported model: "+d.Name);
                int triangles=0;var bounds=new Bounds();bool first=true;
                foreach(var mesh in prefab.GetComponentsInChildren<MeshFilter>())
                {triangles+=mesh.sharedMesh.triangles.Length/3;foreach(var v in mesh.sharedMesh.vertices){var p=mesh.transform.TransformPoint(v);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}}
                Check(triangles>0&&bounds.size.x>.1f&&bounds.size.y>.02f&&bounds.min.x>-.08f&&bounds.min.z>-.08f&&bounds.max.x<1.08f&&bounds.max.z<1.08f&&bounds.size.x<1.15f&&bounds.size.y<(d.Id==IndustryId.WoodenDoor?2.15f:1.15f)&&bounds.size.z<1.15f,"Imported geometry fits declared footprint: "+d.Name);
                imported.AppendLine($"{d.Name}: {triangles} triangles; {prefab.GetComponentsInChildren<Renderer>().Length} renderers; bounds {bounds}; root scale {prefab.transform.localScale}, rotation {prefab.transform.localEulerAngles}; first child {prefab.transform.GetChild(0).localPosition}, scale {prefab.transform.GetChild(0).localScale}");
            }
            File.WriteAllText("Logs/industry-models.txt",imported.ToString());report.AppendLine("Assertions: "+assertions);File.WriteAllText("Logs/industry-checks.txt",report.ToString());
        }
        static void CrusherRecipes(Action<bool,string> check)
        {
            var registry=ItemRegistry.Load();
            var index=new RecipeBrowserIndex(registry,RecipeCatalogAsset.Load().Compile(registry),ProcessingCatalogAsset.Load().Compile(registry));
            foreach(var recipe in new[]{(BlockId.Stone,BlockId.Sand,1),(BlockId.Cobblestone,BlockId.Sand,1),
                (BlockId.RawCopper,IndustryId.CrushedCopper,2),(BlockId.RawIron,IndustryId.CrushedIron,2),(BlockId.RawGold,IndustryId.CrushedGold,2)})
            {
                var (input,output,count)=recipe;string label=registry.Get(input).displayName;
                var world=new World();var sim=new IndustrySimulation(world,id=>64);
                var crusher=sim.Add(new BlockPos(0,20,0),IndustryId.Crusher);
                var battery=sim.Add(new BlockPos(0,21,0),IndustryId.Battery);
                while(sim.Rebuilding)sim.Step();sim.Step();
                var held=new ItemStack(input,2);crusher.Click(0,ref held,false);
                check(held.Empty&&crusher.Items.Total(input)==2,"Crusher accepts manual input: "+label);
                sim.Step();check(crusher.Status==MachineStatus.NoPower&&crusher.Work==0&&crusher.Items.Total(input)==2,"Blackout retains input without progress: "+label);
                battery.EnergyCells[0].Charge(10000000);
                crusher.Items.Add(output,64-count,2,3);
                for(int i=0;i<99;i++)sim.Step();
                check(crusher.Items.Total(input)==2&&crusher.Items.Slots[2].Count==64-count,"No completion before five powered seconds: "+label);
                sim.Step();
                check(crusher.Items.Total(input)==1&&crusher.Items.Slots[2].Id==output&&crusher.Items.Slots[2].Count==64,"Exact output fits remaining capacity: "+label);
                check(battery.EnergyCells[0].Amount==9200000,"Five-second cycle consumes exactly 800 J: "+label);
                sim.Step();check(crusher.Status==MachineStatus.OutputFull&&crusher.RequestedWatts==0&&crusher.Items.Total(input)==1,"Full output blocks input consumption and power demand: "+label);
                crusher.Items.Take(2,64);crusher.Items.Add(BlockId.Dirt,1,2,3);sim.Step();
                check(crusher.Status==MachineStatus.OutputFull&&crusher.Items.Total(input)==1,"Incompatible output blocks processing: "+label);
                var shown=index.Find(output,false).Single(r=>r.Station==IndustryId.Crusher&&r.Ingredients[0].Id==input);
                check(shown.Output.Count==count&&shown.Ingredients[0].Count==1&&shown.Ticks==100&&shown.Watts==160&&index.Find(input,true).Contains(shown),"Recipe and uses browser show actual quantity, time and power: "+label);
                check(!crusher.Accepts(0,BlockId.Sand)&&!crusher.Accepts(0,BlockId.Dirt),"Sand and dirt are not crusher inputs: "+label);
            }
            // Both inputs make sand, but changing their identity must still discard paid work.
            var switching=new IndustrySimulation(new World(),id=>64);
            var machine=switching.Add(new BlockPos(0,20,0),IndustryId.Crusher);
            var source=switching.Add(new BlockPos(0,21,0),IndustryId.Battery);
            switching.Step();source.EnergyCells[0].Charge(10000000);machine.Items.Add(BlockId.Stone,1,0,1);
            for(int i=0;i<50;i++)switching.Step();
            check(machine.Work==50,"Stone accumulates half a powered cycle");
            machine.Items.Take(0,1);var inventory=new ItemContainer(1,id=>64);inventory.Add(BlockId.Cobblestone,1);
            machine.TransferIn(inventory,0);switching.Step();
            check(inventory.Total(BlockId.Cobblestone)==0&&machine.Work==1&&machine.Items.Total(BlockId.Sand)==0,"Inventory transfer accepts cobblestone and input swap resets paid progress");
        }
    }
}
