using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RivetReach.Editor
{
    public static class ResidencyInvalidationChecks
    {
        sealed class World : IIndustryWorld
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public readonly Dictionary<BlockPos,ItemContainer> Chests=new Dictionary<BlockPos,ItemContainer>();
            public readonly HashSet<ChunkPos> Sleeping=new HashSet<ChunkPos>();
            public bool Ready(BlockPos p)=>!Sleeping.Contains(p.Chunk);
            public byte Get(BlockPos p)=>Cells.TryGetValue(p,out byte id)?id:(byte)0;
            public bool Remove(BlockPos p,byte expected){if(Get(p)!=expected)return false;Cells.Remove(p);return true;}
            public ItemContainer Storage(BlockPos p)=>Chests.TryGetValue(p,out var chest)?chest:null;
            public byte Drop(byte id)=>id;
            public bool PlayerInside(BlockPos p)=>false;
        }
        sealed class Fixture
        {
            public readonly World World=new World();
            public readonly IndustrySimulation Sim;
            public Fixture(){Sim=new IndustrySimulation(World,_=>64);}
            public MachineState Add(BlockPos p,byte id){World.Cells[p]=id;return Sim.Add(p,id);}
            public void Settle()
            {
                for(int i=0;i<100;i++){Sim.Step();if(!Sim.Rebuilding&&Sim.Multiblocks.PendingCount==0)return;}
                throw new Exception("Residency topology failed to settle");
            }
            public void Sleep(ChunkPos chunk,bool sleeping)
            {if(sleeping)World.Sleeping.Add(chunk);else World.Sleeping.Remove(chunk);Sim.ResidencyChanged(chunk);}
            public MachineState Tank(BlockPos min)
            {
                var bounds=new StructureBounds(min,min.Offset(4,2,2));
                for(int x=0;x<5;x++)for(int y=0;y<3;y++)for(int z=0;z<3;z++)
                {
                    var p=min.Offset(x,y,z);int axes=bounds.BoundaryAxes(p);if(axes==0)continue;
                    byte id=axes>1?IndustryId.TankFrame:IndustryId.TankWall;
                    if(x==1&&y==1&&z==0)id=IndustryId.TankController;
                    Add(p,id);
                }
                return Sim.At(min.Offset(1,1,0));
            }
        }
        public static void Run()
        {
            var log=new StringBuilder();int assertions=0;
            void Check(bool valid,string message)
            {if(!valid)throw new Exception("Residency: "+message);assertions++;log.AppendLine("PASS "+message);}
            {
                var index=new ResidencyDependencies();var a=new BlockPos(-1,31,31);var b=a.Offset(-1,0,0);
                index.Add(a);index.Add(b);index.Remove(a);
                Check(index.Contains(b.Chunk)&&index.Contains(b.Offset(0,1,0).Chunk),"Removing one owner preserves shared anchor and upper-page dependencies");
                Check(!index.Contains(new ChunkPos(0,0,0)),"Removing the last boundary owner releases its unique neighbouring page");
                index.Remove(b);
                Check(!index.Contains(b.Chunk)&&!index.Contains(b.Offset(0,1,0).Chunk),"Removing all owners releases every page reference");
                index.Add(a);
                Check(index.Contains(new ChunkPos(0,0,0))&&index.Contains(a.Chunk),"Re-registering restored negative-coordinate owners restores boundary coverage");
            }
            {
                var f=new Fixture();var battery=f.Add(new BlockPos(30,20,20),IndustryId.Battery);
                f.Add(new BlockPos(31,20,20),IndustryId.PowerCable);var crusher=f.Add(new BlockPos(32,20,20),IndustryId.Crusher);
                battery.EnergyCells[0].Charge(1000000);crusher.Items.Add(BlockId.RawIron,4,0,1);f.Settle();
                int rebuilds=f.Sim.TopologyRebuilds;var groups=f.Sim.Power.Topology.Groups;long revision=f.Sim.Revision;
                for(int i=0;i<10000;i++)f.Sim.ResidencyChanged(new ChunkPos(100+i,0,100));
                Check(!f.Sim.Rebuilding&&f.Sim.Revision==revision,"Ten thousand unrelated terrain pages do not invalidate industry");
                f.Sim.Step();
                Check(f.Sim.TopologyRebuilds==rebuilds&&ReferenceEquals(groups,f.Sim.Power.Topology.Groups)&&crusher.ReceivedWatts==160,"Unrelated streaming preserves operating power graph and factory progress");
                f.Sleep(crusher.Position.Chunk,true);f.Settle();long energy=battery.EnergyCells[0].Amount;double work=crusher.Work;
                for(int i=0;i<20;i++)f.Sim.Step();
                Check(!crusher.Eligible&&crusher.Status==MachineStatus.Dormant&&crusher.ReceivedWatts==0&&crusher.Work==work&&battery.EnergyCells[0].Amount==energy,"Boundary unload pauses production and conserves dormant work and stored energy");
                f.Sleep(crusher.Position.Chunk,false);f.Settle();
                Check(crusher.Eligible&&crusher.ReceivedWatts==160&&crusher.Work>work,"Boundary reload resumes the same machine through rebuilt power connections");
            }
            {
                var f=new Fixture();var p=new BlockPos(31,20,20);var pipe=f.Add(p,IndustryId.ItemPipe);var chest=p.Offset(1,0,0);
                f.World.Cells[chest]=BlockId.Chest;f.World.Chests[chest]=new ItemContainer(27,_=>64);f.Settle();
                Check(f.Sim.ItemNetwork.Connections.TryGetValue(p,out int mask)&&(mask&1)!=0,"A chest across a page boundary registers as a pipe endpoint");
                f.Sleep(chest.Chunk,true);f.Settle();
                Check(!f.Sim.ItemNetwork.Connections.TryGetValue(p,out mask)||(mask&1)==0,"External chest unload removes its endpoint despite no machine anchor in that page");
                f.Sleep(chest.Chunk,false);f.Settle();
                Check(f.Sim.ItemNetwork.Connections.TryGetValue(p,out mask)&&(mask&1)!=0,"External chest reload restores its original pipe end");
                f.Sim.Remove(p);f.World.Cells.Remove(p);f.Settle();long revision=f.Sim.Revision;
                f.Sim.ResidencyChanged(chest.Chunk);
                Check(!f.Sim.Rebuilding&&f.Sim.Revision==revision,"Removing the last pipe releases its external endpoint residency subscription");
            }
            {
                var f=new Fixture();var door=f.Add(new BlockPos(20,31,20),IndustryId.WoodenDoor);f.Settle();
                var upper=door.Position.Offset(0,1,0).Chunk;f.Sleep(upper,true);f.Settle();
                Check(!door.Eligible&&door.Status==MachineStatus.Dormant,"A door sleeps when only its upper-cell page unloads");
                f.Sleep(upper,false);f.Settle();Check(door.Eligible,"Door upper-cell reload restores eligibility");
            }
            {
                var f=new Fixture();var a=f.Tank(new BlockPos(29,20,20));var b=f.Tank(new BlockPos(128,20,20));f.Settle();
                Check(a.Structure.Formed&&b.Structure.Formed,"Independent tanks form before residency stress");
                a.Structure.Fluid.Deposit(Fluids.Water,200000);long bRevision=b.Structure.Revision;
                f.Sim.ResidencyChanged(new ChunkPos(100,0,100));
                Check(a.Structure.Formed&&b.Structure.Formed&&f.Sim.Multiblocks.PendingCount==0,"Unrelated streaming queues neither controller");
                var edge=new BlockPos(32,21,21).Chunk;f.Sleep(edge,true);
                Check(!a.Structure.Formed&&b.Structure.Formed&&b.Structure.Revision==bRevision,"Boundary unload invalidates only the affected potential footprint immediately");
                f.Settle();
                Check(a.Structure.State==MultiblockState.Waiting&&a.Structure.Fluid.Amount==200000&&b.Structure.Revision==bRevision,"Unavailable shell/interior retains exact contents while the distant tank remains formed");
                f.Sleep(edge,false);f.Settle();
                Check(a.Structure.Formed&&a.Structure.Fluid.Amount==200000&&b.Structure.Revision==bRevision,"Reload restores the affected shared store without revalidating the distant tank");
            }
            {
                var center=new BlockPos(10,20,10);var battery=new MachineState(center,IndustryId.Battery,_=>64);
                var machines=new List<MachineState>{battery};
                foreach(var offset in new[]{(-1,0),(-1,1),(0,1),(1,1),(1,0)})
                    machines.Add(new MachineState(center.Offset(offset.Item1,0,offset.Item2),IndustryId.PowerCable,_=>64));
                var graph=new NetworkTopology(NetworkKind.Power);foreach(var _ in graph.Rebuild(machines)){}
                NetworkTopology.Group connected=null;foreach(var group in graph.Groups)if(group.Ports.Count>1)connected=group;
                int batteryEntries=0,faces=0;foreach(var port in connected.Ports)if(port.Machine==battery){batteryEntries++;faces=port.Faces;}
                Check(connected.Ports.Count==6&&batteryEntries==1&&faces==1,"Power traversal preserves the first device-face occurrence and exactly one shared budget per connected grid");
                Check(graph.Connections.TryGetValue(center,out int mask)&&mask==19,"Deduplication retains all three connected device faces for geometry and connectivity");
                Check(graph.Groups.Count==4,"Disconnected device faces remain separate terminal groups");
            }
            {
                const int count=4096;var machines=new List<MachineState>();
                for(int i=0;i<count;i++)machines.Add(new MachineState(new BlockPos(i,20,20),IndustryId.PowerCable,_=>64));
                var graph=new NetworkTopology(NetworkKind.Power);var published=graph.Groups;
                using(var pending=graph.Rebuild(machines).GetEnumerator())
                {
                    bool yielded=true;for(int i=0;i<count*2+1;i++)if(!pending.MoveNext()){yielded=false;break;}
                    Check(yielded&&ReferenceEquals(graph.Groups,published),"Scanning already visited vertices still yields after a large BFS without publishing a partial graph");
                }
                Check(ReferenceEquals(graph.Groups,published),"Cancelling during the visited-node scan preserves the completed graph");
                int steps=0;foreach(var _ in graph.Rebuild(machines))steps++;
                Check(steps>=count*2+(count-1)/64&&graph.Groups.Count==1&&graph.Groups[0].Ports.Count==count,"A restarted bounded traversal publishes every cable once without changing connectivity");
            }
            {
                var f=new Fixture();var origin=new BlockPos(10,20,10);
                var a=f.Add(origin,IndustryId.Battery);f.Add(origin.Offset(1,0,0),IndustryId.PowerCable);var crusherA=f.Add(origin.Offset(2,0,0),IndustryId.Crusher);
                var b=f.Add(origin.Offset(100,0,0),IndustryId.Battery);f.Add(origin.Offset(101,0,0),IndustryId.PowerCable);var crusherB=f.Add(origin.Offset(102,0,0),IndustryId.Crusher);
                for(int i=1;i<=256;i++)f.Add(origin.Offset(0,0,i),IndustryId.SignalConduit);
                a.EnergyCells[0].Charge(4000000);b.EnergyCells[0].Charge(4000000);crusherA.Items.Add(BlockId.RawIron,64,0,1);crusherB.Items.Add(BlockId.RawIron,64,0,1);f.Settle();
                NetworkTopology.Group stable=null,affected=null;
                foreach(var group in f.Sim.Power.Topology.Groups)foreach(var port in group.Ports)
                {if(port.Machine==crusherB&&group.Ports.Count>1)stable=group;if(port.Machine==crusherA&&group.Ports.Count>1)affected=group;}
                double aWork=crusherA.Work,bWork=crusherB.Work;long aEnergy=a.EnergyCells[0].Amount,bEnergy=b.EnergyCells[0].Amount;
                long powerRevision=f.Sim.Power.Topology.Revision;
                f.Sleep(a.Position.Chunk,true);
                Check(!affected.Active&&stable.Active,"Unloading one factory immediately suspends only its shared automation component");
                f.Sim.BeginFrame(4,double.PositiveInfinity);for(int i=0;i<20;i++)f.Sim.Step();f.Sim.EndFrame();
                Check(f.Sim.ReconstructionStepsThisFrame==4&&f.Sim.Rebuilding&&f.Sim.Power.Topology.Revision==powerRevision,"Twenty fixed ticks share one four-operation frame budget and cannot publish an incomplete replacement");
                Check(crusherA.Work==aWork&&a.EnergyCells[0].Amount==aEnergy&&crusherA.ReceivedWatts==0,"Suspended factory retains exact work and storage without stale power delivery");
                Check(crusherB.Work==bWork+20&&b.EnergyCells[0].Amount==bEnergy-160000&&crusherB.ReceivedWatts==160,"Independent factory completes every fixed tick and pays exact energy during another component's reconstruction");
                f.Sleep(a.Position.Chunk,false);f.Settle();
                Check(f.Sim.Power.Topology.Groups.Contains(stable)&&stable.Active&&crusherA.ReceivedWatts==160,"Cancelled unload reconstruction reconnects atomically while preserving unaffected group identity");
                Check(crusherA.Work==aWork+1&&a.EnergyCells[0].Amount==aEnergy-8000,"Reconnected machinery advances once without offline or reconstruction catch-up");
            }
            {
                var f=new Fixture();var start=new BlockPos(10,20,10);
                var source=new ItemContainer(27,_=>64);var middle=new ItemContainer(27,_=>64);var destination=new ItemContainer(27,_=>64);
                foreach(var entry in new[]{(0,source),(2,middle),(4,destination)})
                {var position=start.Offset(entry.Item1,0,0);f.World.Cells[position]=BlockId.Chest;f.World.Chests.Add(position,entry.Item2);}
                source.Add(BlockId.Stone,32);var first=f.Add(start.Offset(1,0,0),IndustryId.ItemPipe);var second=f.Add(start.Offset(3,0,0),IndustryId.ItemPipe);
                f.Sim.TogglePipeEnd(first,1);f.Sim.TogglePipeEnd(second,1);f.Settle();
                f.Sim.Invalidate(first.Position);bool suspended=true;foreach(var group in f.Sim.ItemNetwork.Groups)suspended&=!group.Active;
                Check(suspended,"Independent pipe graphs sharing a physical chest suspend together as one state dependency component");
                int before=destination.Total(BlockId.Stone);f.Sim.BeginFrame(1,double.PositiveInfinity);for(int i=0;i<20;i++)f.Sim.Step();f.Sim.EndFrame();
                Check(destination.Total(BlockId.Stone)==before&&source.Total(BlockId.Stone)+middle.Total(BlockId.Stone)+destination.Total(BlockId.Stone)==32,"Shared chest cannot forward during partial reconstruction and retains every item");
                f.Settle();for(int i=0;i<20;i++)f.Sim.Step();
                Check(destination.Total(BlockId.Stone)>before&&source.Total(BlockId.Stone)+middle.Total(BlockId.Stone)+destination.Total(BlockId.Stone)==32,"Reconnected shared-inventory graphs resume exact transfers");
            }
            {
                var f=new Fixture();var start=new BlockPos(10,20,10);var battery=f.Add(start,IndustryId.Battery);
                f.Add(start.Offset(1,0,0),IndustryId.PowerCable);var lamp=f.Add(start.Offset(2,0,0),IndustryId.Lamp);
                battery.EnergyCells[0].Charge(1000000);int lightChanges=0;f.Sim.LightChanged+=_=>lightChanges++;f.Settle();
                Check(lamp.Running&&f.Sim.IsSimulating(lamp)&&lightChanges==1,"Operational powered lamp publishes its initial light source");
                f.Sim.Invalidate(lamp.Position);f.Sim.BeginFrame(1,double.PositiveInfinity);for(int i=0;i<3;i++)f.Sim.Step();f.Sim.EndFrame();
                Check(!f.Sim.IsSimulating(lamp)&&lamp.ReceivedWatts==0&&lightChanges==2,"Suspended lamp invalidates cached lighting exactly once despite retaining its status snapshot");
                f.Settle();Check(f.Sim.IsSimulating(lamp)&&lamp.ReceivedWatts==20&&lightChanges==3,"Reconnected lamp republishes light only when real powered ticking resumes");
            }
            {
                var f=new Fixture();var start=new BlockPos(10,20,10);
                var battery=f.Add(start,IndustryId.Battery);f.Add(start.Offset(1,0,0),IndustryId.PowerCable);var crusher=f.Add(start.Offset(2,0,0),IndustryId.Crusher);
                for(int i=1;i<=64;i++)f.Add(start.Offset(0,0,i),IndustryId.SignalConduit);
                var distant=f.Add(start.Offset(256,0,0),IndustryId.PowerCable);f.Settle();
                battery.EnergyCells[0].Charge(1000000);crusher.Items.Add(BlockId.RawIron,64,0,1);
                long energy=battery.EnergyCells[0].Amount;double work=crusher.Work;
                f.Sim.Invalidate(battery.Position);
                f.Sim.BeginFrame(8,double.PositiveInfinity);f.Sim.Step();f.Sim.EndFrame();
                int started=f.Sim.TopologyRebuilds,frames=0;
                while(!f.Sim.IsSimulating(crusher)&&frames++<1000)
                {
                    // Alternate real residency changes and ordinary configuration
                    // invalidation in a disjoint component throughout the rebuild.
                    f.Sleep(distant.Position.Chunk,(frames&1)==0);f.Sim.Invalidate(distant.Position);
                    f.Sim.BeginFrame(8,double.PositiveInfinity);f.Sim.Step();f.Sim.EndFrame();
                    Check(f.Sim.ReconstructionStepsThisFrame<=8,"Disjoint churn respects the shared reconstruction budget");
                }
                Check(frames<1000&&f.Sim.TopologyRebuilds==started,"Disjoint residency/configuration churn does not restart the in-flight factory reconstruction");
                Check(crusher.Work==work+1&&battery.EnergyCells[0].Amount==energy-8000,"First publication resumes exactly one production tick without catch-up or lost energy");
                Check(f.Sim.Rebuilding&&!f.Sim.IsSimulating(distant),"Disjoint changes remain queued and suspended after the first atomic publication");
                f.Sleep(distant.Position.Chunk,false);f.Settle();
                Check(f.Sim.IsSimulating(distant)&&!f.Sim.Rebuilding,"Queued disjoint changes subsequently reconnect after churn ends");
                f.Sim.Invalidate(battery.Position);f.Sim.BeginFrame(8,double.PositiveInfinity);f.Sim.Step();f.Sim.EndFrame();
                int before=f.Sim.TopologyRebuilds;double frozenWork=crusher.Work;long frozenEnergy=battery.EnergyCells[0].Amount;
                f.Sleep(battery.Position.Chunk,true);f.Sim.BeginFrame(8,double.PositiveInfinity);f.Sim.Step();f.Sim.EndFrame();
                Check(f.Sim.TopologyRebuilds==before+1&&!f.Sim.IsSimulating(crusher)&&crusher.Work==frozenWork&&battery.EnergyCells[0].Amount==frozenEnergy,
                    "Intersecting residency changes cancel stale reconstruction and preserve frozen work/storage");
                f.Sleep(battery.Position.Chunk,false);f.Settle();
                Check(f.Sim.IsSimulating(crusher),"An intersecting cancelled reconstruction safely reconnects after reload");
            }
            Directory.CreateDirectory("Logs/ReleaseReview");
            File.WriteAllText("Logs/ReleaseReview/residency-checks.txt",$"PASS {assertions} assertions\n"+log);
        }
    }
}
