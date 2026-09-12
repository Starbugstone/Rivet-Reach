using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class DomainChecks
    {
        static int checks;
        static void Check(bool value,string message){checks++;if(!value)throw new Exception("Domain check failed: "+message);}
        public static void Run()
        {
            checks=0;
            foreach(long x in new long[]{-1000000000,-65,-33,-32,-31,-1,0,1,31,32,33,1000000000})
            {
                var p=new BlockPos(x,-1,-x);var min=p.Chunk.Min;
                Check(p.Index>=0&&p.Index<32768,"Index bounds");
                Check(min.Offset(p.Index%32,p.Index/32%32,p.Index/1024).Equals(p),"Coordinate round trip "+p);
                var point=new WorldPoint(p,new Vector3(.25f,.5f,.75f));var round=WorldPoint.FromLocal(point.Local(p.Chunk.Min),p.Chunk.Min);
                Check(round.Cell.Equals(p)&&round.Fraction==point.Fraction,"Floating origin round trip");
            }
            var generator=new TerrainGenerator(246813);var adjacent=new[]{new ChunkPos(-1,1,0),new ChunkPos(0,1,0)};
            byte[] a=generator.Generate(adjacent[0]),b=generator.Generate(adjacent[1]);
            Check(a.SequenceEqual(new TerrainGenerator(246813).Generate(adjacent[0])),"Repeat seed");
            for(int z=-1;z<=32;z++)for(int y=-1;y<=32;y++)
            {Check(a[ChunkMesher.Index(32,y,z)]==b[ChunkMesher.Index(0,y,z)],"Neighbour halo seam");Check(a[ChunkMesher.Index(31,y,z)]==b[ChunkMesher.Index(-1,y,z)],"Reverse neighbour seam");}
            Check(!a.SequenceEqual(new TerrainGenerator(777).Generate(adjacent[0])),"Different seed changes terrain");
            var cells=new byte[34*34*34];cells[ChunkMesher.Index(7,8,9)]=3;
            var mesh=ChunkMesher.Build(default,0,cells);Check(mesh.Triangles.Length==36,"Single cube six faces");
            for(int i=0;i<mesh.Triangles.Length;i+=3)
            {
                int p=mesh.Triangles[i],q=mesh.Triangles[i+1],r=mesh.Triangles[i+2];
                Check(Vector3.Dot(Vector3.Cross(mesh.Vertices[q]-mesh.Vertices[p],mesh.Vertices[r]-mesh.Vertices[p]),mesh.Normals[p])>0,"Face winding");
            }
            for(int z=0;z<32;z++)for(int y=0;y<32;y++)for(int x=0;x<32;x++)cells[ChunkMesher.Index(x,y,z)]=3;
            Check(ChunkMesher.Build(default,0,cells).Triangles.Length==36,"Greedy solid chunk is six quads");
            Array.Fill(cells,(byte)3);Check(ChunkMesher.Build(default,0,cells).Triangles.Length==0,"Occluded chunk emits no faces");
            var inventory=new Inventory(_=>500);Check(inventory.Add(1,500*(Inventory.SlotCount+1))==500,"Overflow preserves remainder");Check(inventory.Total(1)==500*Inventory.SlotCount,"All player inventory slots used");
            var held=default(ItemStack);inventory.Click(0,ref held,true);Check(held.Count==250&&inventory.Slots[0].Count==250,"Right split");
            inventory.Click(0,ref held,true);Check(held.Count==249&&inventory.Slots[0].Count==251,"Place one");
            inventory.Click(0,ref held,false);Check(held.Empty&&inventory.Total(1)==500*Inventory.SlotCount,"Merge conservation");
            inventory.Take(Inventory.HotbarCount,500);inventory.QuickTransfer(0);Check(inventory.Slots[0].Empty&&inventory.Slots[Inventory.HotbarCount].Count==500,"Hotbar transfer");
            var random=new System.Random(91);int expected=inventory.Total(1);
            for(int i=0;i<10000;i++)
            {
                inventory.Click(random.Next(Inventory.SlotCount),ref held,random.Next(2)==0);
                Check(inventory.Total(1)+held.Count==expected,"Random inventory operation conservation");
                Check(inventory.Slots.All(s=>s.Count>=0&&s.Count<=500)&&held.Count<=500,"Stack bounds");
            }
            Check(ItemRegistry.Load().FistDrop(1)==2&&ItemRegistry.Load().FistDrop(2)==2&&ItemRegistry.Load().FistDrop(3)==BlockId.Cobblestone,"Grass drops dirt; mined stone supplies cobblestone");
            GrassChecks();TreeChecks.Run(Check);OreChecks.Run(Check);TerrainGenerationChecks.Run(Check);CraftingChecks.Run();SurvivalChecks.Run();
            string report="PASS: "+checks+" assertions. Signed coordinates, precision, deterministic terrain/halos, mesh winding/greedy occlusion, inventory overflow/split/transfer, 10,000 randomized stack operations, grass drops and deterministic light-gated random ticks; deterministic trees, axe capabilities, upward felling and leaf decay; ore bands, hosts, worker determinism, signed/distant halos and bedrock.";
            File.WriteAllText("Logs/domain-checks.txt",report);Debug.Log(report);
        }
        sealed class GrassFixture : IGrassWorld
        {
            public readonly System.Collections.Generic.Dictionary<BlockPos,byte> cells=new System.Collections.Generic.Dictionary<BlockPos,byte>();
            public bool ready=true;
            public int changes;
            public GrassFixture()
            {
                for(int z=0;z<32;z++)for(int x=-4;x<4;x++)cells[new BlockPos(x,0,z)]=(byte)(x<0?1:2);
                for(int z=8;z<16;z++)for(int x=0;x<4;x++)cells[new BlockPos(x,1,z)]=3;
                for(int z=24;z<32;z++)for(int x=-4;x<0;x++)cells[new BlockPos(x,1,z)]=3;
            }
            public bool TryRead(BlockPos p,out byte id){cells.TryGetValue(p,out id);return ready;}
            public byte SkyLight(BlockPos p)=>p.X>=0&&p.Z>=16&&p.Z<24?(byte)0:(byte)15;
            public bool ChangeGrass(BlockPos p,byte expected,byte replacement)
            {if(!TryRead(p,out byte id)||id!=expected)return false;cells[p]=replacement;changes++;return true;}
        }
        static void GrassChecks()
        {
            var a=new GrassFixture();var b=new GrassFixture();var first=new GrassSimulation(471);var second=new GrassSimulation(471);
            var chunks=new[]{new ChunkPos(-1,0,0),new ChunkPos(0,0,0)};
            first.Step(a,chunks);Check(a.changes==0,"Grass does not flood adjacent dirt in its first tick");
            second.Step(b,chunks);
            for(int tick=1;tick<16000;tick++){first.Step(a,chunks);second.Step(b,chunks);}
            Check(a.cells.All(p=>b.cells.TryGetValue(p.Key,out byte id)&&id==p.Value),"Same seed, active chunks and tick order reproduce grass changes");
            Check(a.cells.Any(p=>p.Key.X>=0&&p.Key.Y==0&&p.Value==1),"Random ticks spread grass across the negative/positive chunk seam");
            Check(a.cells.Where(p=>p.Key.X>=0&&p.Key.Y==0&&p.Key.Z>=8&&p.Key.Z<24).All(p=>p.Value==2),"Covered and unlit dirt do not grow grass");
            Check(a.cells.Any(p=>p.Key.X<0&&p.Key.Y==0&&p.Key.Z>=24&&p.Value==2),"Covered grass becomes dirt on random ticks");
            Check(a.cells.Where(p=>p.Key.Y==1).All(p=>p.Value==3),"Grass ticks never replace stone");
            int before=a.changes;a.ready=false;
            for(int tick=0;tick<100;tick++)first.Step(a,chunks);
            Check(a.changes==before,"Unready chunks cannot receive grass mutations");
            first.Step(a,Array.Empty<ChunkPos>());Check(first.LastSamples==0,"Dormant chunks require no voxel sampling");
        }
    }
}
