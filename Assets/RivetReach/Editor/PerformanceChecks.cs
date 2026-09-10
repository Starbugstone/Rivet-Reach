using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class PerformanceChecks
    {
        public static void Run()
        {
            var results=new List<string>();
            var generator=new TerrainGenerator(246813);
            var cases=new List<byte[]>{new byte[34*34*34],Enumerable.Repeat(BlockId.Stone,34*34*34).ToArray()};
            foreach(var p in new[]{new ChunkPos(0,0,0),new ChunkPos(0,-2,0),new ChunkPos(-2,1,3)})cases.Add(generator.Generate(p));
            var mixed=new byte[34*34*34];
            for(int i=1;i<256;i++)mixed[ChunkMesher.Index(i%16*2,8,i/16*2)]=(byte)i;
            cases.Add(mixed);
            var random=new System.Random(17);var noise=new byte[34*34*34];
            byte[] ids={0,0,BlockId.Stone,BlockId.Grass,BlockId.Log,BlockId.Leaves,BlockId.PotatoPlant,BlockId.Torch,IndustryId.Bench,Fluids.Water.Source,Fluids.Water.Flow(4)};
            for(int i=0;i<noise.Length;i++)noise[i]=ids[random.Next(ids.Length)];cases.Add(noise);
            // Golden geometry captured from the original mesher for terrain-6-azure.
            // Includes positions, winding, UVs, tile IDs, fluid colours and scheduled cells.
            string[] expected={"6DB65FD59FD356F6729140571B5BCD6BB3B83492A16E1BF0A3884442FC3C8A0E","6DB65FD59FD356F6729140571B5BCD6BB3B83492A16E1BF0A3884442FC3C8A0E","CA552034F0C5500E012157A273D2483B4988577B6418C6F6E120FBF444D3C9A5","5A25408993FBF5E00B0073CD8B7EA7B1907CE444B698A3B50226E16A0350AAD3","8A5CE840F13B5762E3E605732727627DAEFEF90271480D98BEC76F055BC335FF","39A693073E56F449759C39EA861BE0DD9E5F0FADBB70EE82FB6C92E10AE5C36E","67FDDE1B0826A9F9D04881FDE7A07E8F2E17743282430CFB0B7618C2FAFBF229"};
            for(int i=0;i<cases.Count;i++)
            {
                var current=ChunkMesher.Build(default,3,cases[i]);string hash=Digest(current);
                if(hash!=expected[i])throw new Exception("Changed mesh output, fixture "+i);
                results.Add("PASS fixture "+i+": "+hash);
            }
            var fluid=ChunkMesher.Build(default,0,noise).FluidMesh;var mesh=fluid.ToMesh();
            try{if(!ReferenceEquals(mesh,fluid.ToMesh(mesh))||mesh.vertexCount!=fluid.Vertices.Length)throw new Exception("Fluid mesh reuse differs");}
            finally{UnityEngine.Object.DestroyImmediate(mesh);}
            Directory.CreateDirectory("Logs/Performance");File.WriteAllLines("Logs/Performance/mesh-regression.txt",results);UnityEngine.Debug.Log("PASS performance mesh equivalence and reuse");
        }
        static string Digest(ChunkBuild mesh)
        {
            using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))
            {
                void V3(Vector3[] values){writer.Write(values.Length);foreach(var v in values){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}}
                void V2(Vector2[] values){writer.Write(values.Length);foreach(var v in values){writer.Write(v.x);writer.Write(v.y);}}
                void Ints(int[] values){writer.Write(values.Length);foreach(var v in values)writer.Write(v);}
                V3(mesh.Vertices);V3(mesh.Normals);V2(mesh.UV);V2(mesh.Tiles);Ints(mesh.Triangles);
                V3(mesh.FluidMesh.Vertices);V3(mesh.FluidMesh.Normals);Ints(mesh.FluidMesh.Indices);Ints(mesh.FluidMesh.ActiveCells);
                foreach(var c in mesh.FluidMesh.Colours){writer.Write(c.r);writer.Write(c.g);writer.Write(c.b);writer.Write(c.a);}
                writer.Flush();using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-","");
            }
        }
    }
}
