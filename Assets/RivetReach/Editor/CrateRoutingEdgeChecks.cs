using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RivetReach.Editor
{
    // Focused edge coverage for cached, numeric item-pipe routing. This intentionally
    // owns its own tiny world so it exercises the public industry endpoint boundary.
    public static class CrateRoutingEdgeChecks
    {
        sealed class World : IIndustryWorld,IIndustryItemEndpoints
        {
            public readonly Dictionary<BlockPos,byte> Blocks=new Dictionary<BlockPos,byte>();
            public readonly Dictionary<BlockPos,IItemPipeInventory> Endpoints=new Dictionary<BlockPos,IItemPipeInventory>();
            public readonly Dictionary<BlockPos,CrateStorage> Crates=new Dictionary<BlockPos,CrateStorage>();
            public readonly HashSet<BlockPos> Dormant=new HashSet<BlockPos>();
            public readonly CrateNetwork Network;
            public World(){Network=new CrateNetwork(Get,p=>Crates.TryGetValue(p,out var crate)?crate:null,Ready);}
            public void Crate(BlockPos p){Blocks[p]=CrateId.Crate;Crates[p]=new CrateStorage(_=>64);Network.Invalidate();}
            public bool Ready(BlockPos p)=>!Dormant.Contains(p);
            public byte Get(BlockPos p)=>Blocks.TryGetValue(p,out var id)?id:(byte)0;
            public bool Remove(BlockPos p,byte expected)=>Get(p)==expected&&Blocks.Remove(p);
            public ItemContainer Storage(BlockPos p)=>Endpoints.TryGetValue(p,out var endpoint)?endpoint as ItemContainer:null;
            public IItemPipeInventory ItemEndpoint(BlockPos p)=>CrateId.Part(Get(p))?Network.At(p):Endpoints.TryGetValue(p,out var endpoint)?endpoint:null;
            public int ItemEndpointRotation(BlockPos p)=>0;
            public void ItemEndpointChanged(BlockPos p){}
            public byte Drop(byte block)=>block;
            public bool PlayerInside(BlockPos p)=>false;
        }
        static void Settle(IndustrySimulation sim)
        {
            for(int i=0;i<400;i++){sim.Step();if(!sim.Rebuilding&&sim.Multiblocks.PendingCount==0)break;}
            while(sim.Tick%5!=0)sim.Step();
        }
        static void Phase(IndustrySimulation sim){for(int i=0;i<5;i++)sim.Step();}
        static MachineState Pipe(World world,IndustrySimulation sim,BlockPos p,int sourceFace,int sinkFace)
        {
            world.Blocks[p]=IndustryId.ItemPipe;var pipe=sim.Add(p,IndustryId.ItemPipe);
            pipe.PipeDirections=(2<<(sourceFace*2))|(1<<(sinkFace*2));return pipe;
        }
        static ItemContainer Chest(World world,BlockPos p)
        {
            world.Blocks[p]=BlockId.Chest;var chest=new ItemContainer(8,_=>64);world.Endpoints[p]=chest;return chest;
        }
        public static void Run()
        {
            Directory.CreateDirectory("Logs/Crates");var log=new StringBuilder();int assertions=0;
            void Check(bool value,string message)
            {if(!value)throw new Exception("Crate routing edge: "+message);assertions++;log.AppendLine("PASS "+message);}
            AliasAcrossSeparateGraphs(Check);
            EqualNumericPrioritiesAndCache(Check);
            MultipleFacesAreOneReceiver(Check);
            log.Insert(0,"PASS "+assertions+" assertions\n");File.WriteAllText("Logs/Crates/routing-edge-checks.txt",log.ToString());
        }
        static void AliasAcrossSeparateGraphs(Action<bool,string> check)
        {
            var world=new World();var sim=new IndustrySimulation(world,_=>64);
            var crate=new BlockPos(0,20,0);var controller=crate.Offset(0,1,0);world.Crate(crate);world.Blocks[controller]=CrateId.Controller;world.Network.Invalidate();
            // These pipes deliberately have no face-adjacent path to each other.
            var directPipe=crate.Offset(-1,0,0);var controllerPipe=controller.Offset(0,0,-1);
            Pipe(world,sim,directPipe,0,1);Pipe(world,sim,controllerPipe,4,5);
            var directSink=Chest(world,directPipe.Offset(-1,0,0));var controllerSink=Chest(world,controllerPipe.Offset(0,0,-1));
            Settle(sim);world.Crates[crate].Insert(new ItemStack(BlockId.Stone,8));Phase(sim);
            check(directSink.Total(BlockId.Stone)+controllerSink.Total(BlockId.Stone)==1&&world.Crates[crate].Count==7,
                "Direct and controller aliases on separate pipe graphs emit one physical crate item per phase");
            var direct=world.Network.At(crate);var aggregate=world.Network.At(controller);int before=world.Crates[crate].Count;
            check(!aggregate.TryInsertFrom(BlockId.Stone,direct.SourceIdentity(0),0)&&world.Crates[crate].Count==before,
                "Controller rejects delivery back into the physical crate that sourced it");
        }
        static void EqualNumericPrioritiesAndCache(Action<bool,string> check)
        {
            var world=new World();var sim=new IndustrySimulation(world,_=>64);var pipeAt=new BlockPos(0,20,0);
            Pipe(world,sim,pipeAt,0,1);var source=Chest(world,IndustryDefinition.Neighbor(pipeAt,0));var chest=Chest(world,IndustryDefinition.Neighbor(pipeAt,1));
            var crate=IndustryDefinition.Neighbor(pipeAt,2);world.Crate(crate);
            var controller=IndustryDefinition.Neighbor(pipeAt,3);world.Blocks[controller]=CrateId.Controller;world.Crate(controller.Offset(0,-1,0));world.Network.Invalidate();
            var pipe=sim.At(pipeAt);pipe.PipeDirections|=(1<<(2*2))|(1<<(3*2));
            var direct=world.Network.At(crate);var aggregate=world.Network.At(controller);
            chest.ItemInputPriority=55;direct.ItemInputPriority=55;aggregate.ItemInputPriority=55;
            Settle(sim);source.Add(BlockId.Stone,12);int topology=sim.TopologyRebuilds,cache=sim.ItemRouteCacheBuilds;
            for(int i=0;i<6;i++)Phase(sim);
            check(chest.Total(BlockId.Stone)==2&&world.Crates[crate].Count==2&&world.Crates[controller.Offset(0,-1,0)].Count==2,
                "Equal controller, direct crate and chest priorities round-robin compatible cargo");
            check(sim.TopologyRebuilds==topology&&sim.ItemRouteCacheBuilds==cache,
                "Stable numeric priorities do not rebuild topology or route cache");
            aggregate.ItemInputPriority=90;int aggregateBefore=world.Crates[controller.Offset(0,-1,0)].Count;Phase(sim);
            check(world.Crates[controller.Offset(0,-1,0)].Count==aggregateBefore+1&&sim.TopologyRebuilds==topology&&sim.ItemRouteCacheBuilds==cache,
                "Priority edit rebuckets the existing cache without rebuilding topology");
        }
        static void MultipleFacesAreOneReceiver(Action<bool,string> check)
        {
            var world=new World();var sim=new IndustrySimulation(world,_=>64);var receiverAt=new BlockPos(0,20,0);var receiver=Chest(world,receiverAt);
            BlockPos right=receiverAt.Offset(1,0,0),left=receiverAt.Offset(-1,0,0),northRight=right.Offset(0,0,1),north=receiverAt.Offset(0,0,1),northLeft=left.Offset(0,0,1);
            var source=Chest(world,northRight.Offset(1,0,0));var other=Chest(world,north.Offset(0,1,0));
            var a=Pipe(world,sim,right,0,1);var b=Pipe(world,sim,left,0,1);var feed=Pipe(world,sim,northRight,0,1);var middle=Pipe(world,sim,north,0,2);var tail=Pipe(world,sim,northLeft,0,1);
            // Join the two receiver faces around the north side; only feed has an output end.
            a.PipeDirections=1<<(1*2);b.PipeDirections=1<<(0*2);middle.PipeDirections=1<<(2*2);tail.PipeDirections=0;
            Settle(sim);source.Add(BlockId.Dirt,10);receiver.ItemInputPriority=44;other.ItemInputPriority=44;
            for(int i=0;i<6;i++)Phase(sim);
            check(receiver.Total(BlockId.Dirt)==3&&other.Total(BlockId.Dirt)==3,
                "Two pipe faces to one chest are one equal-priority receiver, not two shares");
        }
    }
}
