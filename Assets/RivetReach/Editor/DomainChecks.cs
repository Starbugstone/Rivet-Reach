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
            var inventory=new Inventory(_=>500);Check(inventory.Add(1,30500)==500,"Overflow preserves remainder");Check(inventory.Total(1)==30000,"All sixty slots used");
            var held=default(ItemStack);inventory.Click(0,ref held,true);Check(held.Count==250&&inventory.Slots[0].Count==250,"Right split");
            inventory.Click(0,ref held,true);Check(held.Count==249&&inventory.Slots[0].Count==251,"Place one");
            inventory.Click(0,ref held,false);Check(held.Empty&&inventory.Total(1)==30000,"Merge conservation");
            inventory.Take(12,500);inventory.QuickTransfer(0);Check(inventory.Slots[0].Empty&&inventory.Slots[12].Count==500,"Hotbar transfer");
            var random=new System.Random(91);int expected=inventory.Total(1);
            for(int i=0;i<10000;i++)
            {
                inventory.Click(random.Next(60),ref held,random.Next(2)==0);
                Check(inventory.Total(1)+held.Count==expected,"Random inventory operation conservation");
                Check(inventory.Slots.All(s=>s.Count>=0&&s.Count<=500)&&held.Count<=500,"Stack bounds");
            }
            string report="PASS: "+checks+" assertions. Signed coordinates, precision, deterministic terrain/halos, mesh winding/greedy occlusion, inventory overflow/split/transfer and 10,000 randomized stack operations.";
            File.WriteAllText("Logs/domain-checks.txt",report);Debug.Log(report);
        }
    }
}
