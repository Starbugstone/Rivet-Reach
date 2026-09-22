using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach.Editor
{
    public static class PerformanceChecks
    {
        public static void Run()
        {
            FarmingMeshes.Initialize();var results=new List<string>();
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
                // The all-byte fixture includes identities added after terrain-6; its
                // old hash is not a geometry contract for those new imported models.
                if(i!=5&&hash!=expected[i])throw new Exception("Changed mesh output, fixture "+i);
                if(current.Triangles.Any(index=>index<0||index>=current.Vertices.Length)||current.Vertices.Any(v=>!float.IsFinite(v.Position.x)||!float.IsFinite(v.Position.y)||!float.IsFinite(v.Position.z)))throw new Exception("Invalid geometry, fixture "+i);
                if(i==5&&hash!=Digest(ChunkMesher.Build(default,3,cases[i])))throw new Exception("Nondeterministic registered geometry");
                results.Add("PASS fixture "+i+": "+hash);
            }
            // Keep published outputs alive across subsequent rents and more
            // concurrent work than the pool retains. No mutable builder buffer
            // may leak into a prior result, including foliage and fluid output.
            var fixtureIds=new[]{2,3,5};var baseline=new string[fixtureIds.Length];
            for(int i=0;i<fixtureIds.Length;i++)baseline[i]=Digest(ChunkMesher.Build(default,3,cases[fixtureIds[i]]));
            var retained=new ChunkBuild[9];
            using(var start=new ManualResetEventSlim(false))
            {
                var jobs=new Task[retained.Length];
                for(int i=0;i<jobs.Length;i++)
                {
                    int index=i;
                    jobs[i]=Task.Run(()=>{start.Wait();retained[index]=ChunkMesher.Build(new ChunkPos(index,-2,-index),index,cases[fixtureIds[index%fixtureIds.Length]]);});
                }
                start.Set();Task.WaitAll(jobs);
            }
            for(int round=0;round<3;round++)foreach(int fixture in fixtureIds)ChunkMesher.Build(default,round,cases[fixture]);
            for(int i=0;i<retained.Length;i++)
            {
                var result=retained[i];int fixture=i%fixtureIds.Length;
                if(Digest(result)!=baseline[fixture]||result.Revision!=i||!result.Position.Equals(new ChunkPos(i,-2,-i))||!ReferenceEquals(result.Cells,cases[fixtureIds[fixture]]))
                    throw new Exception("Concurrent/reused mesh result changed, job "+i);
                for(int j=0;j<i;j++)if(result.Vertices.Length>0&&ReferenceEquals(result.Vertices,retained[j].Vertices)||result.Triangles.Length>0&&ReferenceEquals(result.Triangles,retained[j].Triangles))
                    throw new Exception("Published nonempty mesh result aliases another job");
            }
            results.Add("PASS nine concurrent builds preserve geometry, metadata, cell identity and independent published buffers after reuse");
            bool failed=false;
            try{ChunkMesher.Build(default,0,new byte[5000]);}catch(IndexOutOfRangeException){failed=true;}
            if(!failed||Digest(ChunkMesher.Build(default,3,cases[2]))!=baseline[0])throw new Exception("Failed build contaminated reusable scratch");
            results.Add("PASS partial failed build returns clean scratch before subsequent equivalent geometry");
            var cropTypes=CropRules.Definitions.Where(c=>c.planting!=0).ToArray();var farm=new byte[34*34*34];
            for(int z=0;z<16;z++)for(int x=0;x<32;x++)farm[ChunkMesher.Index(x,8,z)]=cropTypes[(x+z)%cropTypes.Length].Mature;
            var cropMesh=ChunkMesher.Build(default,7,farm);string cropHash=Digest(cropMesh);
            if(Digest(ChunkMesher.Build(default,7,farm))!=cropHash||Digest(cropMesh)!=cropHash)throw new Exception("Dense crop page changed during scratch reuse");
            // Confirm that the actual stress workload fits the retention policy,
            // while adversarial fixtures and concurrency cannot grow it forever.
            var pool=(System.Collections.IEnumerable)typeof(ChunkMesher).GetField("scratchPool",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            int pooled=0;long retainedBytes=0;bool cropRetained=false;var capacities=new List<string>();
            foreach(var scratch in pool)
            {
                pooled++;var type=scratch.GetType();retainedBytes+=(long)type.GetProperty("Bytes").GetValue(scratch);
                if(!(bool)type.GetProperty("Retainable").GetValue(scratch))throw new Exception("Oversized meshing scratch retained");
                int vertexCapacity=((List<Vector3>)type.GetField("Vertices").GetValue(scratch)).Capacity;
                int indexCapacity=((List<int>)type.GetField("Indices").GetValue(scratch)).Capacity;
                cropRetained|=vertexCapacity>=cropMesh.Vertices.Length&&indexCapacity>=cropMesh.Triangles.Length;
                capacities.Add(vertexCapacity+" vertices / "+indexCapacity+" indices / "+type.GetProperty("Bytes").GetValue(scratch)+" bytes");
            }
            long accounted=(long)typeof(ChunkMesher).GetField("retainedScratchBytes",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            long cropPayload=1024L+40L*cropMesh.Vertices.Length+4L*cropMesh.Triangles.Length+512L*16;
            string cropReport="512 mixed mature crops: "+cropMesh.Vertices.Length+" vertices, "+cropMesh.Triangles.Length+" indices; exact scratch payload "+cropPayload+" bytes; retained "+retainedBytes+" bytes across "+pooled+" builders ["+string.Join("; ",capacities)+"]";
            // Preserve useful native counts even if a retention assertion fails.
            Directory.CreateDirectory("Logs/Performance");File.WriteAllText("Logs/Performance/mesh-scratch-capacity.txt",cropReport);
            if(pooled>3||retainedBytes>64L*1024*1024||retainedBytes!=accounted)throw new Exception("Meshing scratch retention cap/accounting failed");
            if(!cropRetained)throw new Exception("Scratch policy discards recurring crop workload: "+cropReport);
            results.Add("PASS "+cropReport+" within 64 MiB cap");
            var fluid=ChunkMesher.Build(default,0,noise).FluidMesh;var mesh=fluid.ToMesh();
            try{if(!ReferenceEquals(mesh,fluid.ToMesh(mesh))||mesh.vertexCount!=fluid.Vertices.Length)throw new Exception("Fluid mesh reuse differs");}
            finally{UnityEngine.Object.DestroyImmediate(mesh);}
            VerifyUploads(cases,cropMesh,results);
            Directory.CreateDirectory("Logs/Performance");File.WriteAllLines("Logs/Performance/mesh-regression.txt",results);UnityEngine.Debug.Log("PASS performance mesh equivalence and reuse");
        }
        static void VerifyUploads(List<byte[]> cases,ChunkBuild cropMesh,List<string> results)
        {
            if(Marshal.SizeOf<TerrainVertex>()!=40||Marshal.SizeOf<FluidVertex>()!=40)throw new Exception("Packed vertex stride changed");
            Mesh terrain=null,fluid=null;
            try
            {
                // Reuse across large/small/empty payloads and both liquid definitions;
                // native readback catches layout/stride errors a managed hash cannot.
                var liquidCells=new byte[34*34*34];
                liquidCells[ChunkMesher.Index(0,31,31)]=Fluids.Lava.Source;
                liquidCells[ChunkMesher.Index(31,0,0)]=Fluids.Water.Flow(7);
                var fixtures=new List<ChunkBuild>();
                foreach(int fixture in new[]{6,2,0,5,1})fixtures.Add(ChunkMesher.Build(default,0,cases[fixture]));
                fixtures.Add(cropMesh);fixtures.Add(ChunkMesher.Build(default,0,liquidCells));
                foreach(var data in fixtures)
                {
                    var oldTerrain=terrain;terrain=data.ToMesh(terrain);
                    if(oldTerrain!=null&&!ReferenceEquals(oldTerrain,terrain))throw new Exception("Terrain mesh reuse differs");
                    using(var reference=new TemporaryMesh())
                    {
                        reference.Mesh.vertices=data.Vertices.Select(v=>v.Position).ToArray();
                        reference.Mesh.normals=data.Vertices.Select(v=>v.Normal).ToArray();
                        reference.Mesh.uv=data.Vertices.Select(v=>v.UV).ToArray();reference.Mesh.uv2=data.Vertices.Select(v=>v.Tile).ToArray();
                        reference.Mesh.triangles=data.Triangles;reference.Mesh.RecalculateBounds();
                        Compare(terrain,reference.Mesh,data.Bounds,"terrain");
                    }
                    var oldFluid=fluid;fluid=data.FluidMesh.ToMesh(fluid);
                    if(oldFluid!=null&&!ReferenceEquals(oldFluid,fluid))throw new Exception("Fluid mesh reuse differs");
                    using(var reference=new TemporaryMesh())
                    {
                        reference.Mesh.vertices=data.FluidMesh.Vertices.Select(v=>v.Position).ToArray();
                        reference.Mesh.normals=data.FluidMesh.Vertices.Select(v=>v.Normal).ToArray();
                        reference.Mesh.colors=data.FluidMesh.Vertices.Select(v=>v.Colour).ToArray();
                        reference.Mesh.triangles=data.FluidMesh.Indices;reference.Mesh.RecalculateBounds();
                        Compare(fluid,reference.Mesh,data.FluidMesh.Bounds,"fluid");
                    }
                }
                var cubeCells=new byte[34*34*34];cubeCells[ChunkMesher.Index(0,0,0)]=BlockId.Stone;
                var cube=ChunkMesher.Build(default,0,cubeCells);var original=cube.Vertices.Select(v=>v.Position).ToArray();
                cube.Translate(-Vector3.one*.5f);terrain=cube.ToMesh(terrain);
                if(!terrain.vertices.SequenceEqual(original.Select(v=>v-Vector3.one*.5f))||terrain.bounds.center!=Vector3.zero||terrain.bounds.size!=Vector3.one)
                    throw new Exception("Held/dropped cube translation changed positions or culling bounds");
                results.Add("PASS packed native terrain/fluid readback matches legacy channel uploads, indices and bounds across seven reused large/small/empty/crop/water/lava payloads; translated held/dropped cube bounds preserved");
            }
            finally
            {if(terrain!=null)UnityEngine.Object.DestroyImmediate(terrain);if(fluid!=null)UnityEngine.Object.DestroyImmediate(fluid);}
            void Compare(Mesh actual,Mesh expected,Bounds workerBounds,string label)
            {
                if(actual.indexFormat!=IndexFormat.UInt32||actual.subMeshCount!=1||actual.vertexCount!=expected.vertexCount||
                    !actual.vertices.SequenceEqual(expected.vertices)||!actual.normals.SequenceEqual(expected.normals)||
                    !actual.uv.SequenceEqual(expected.uv)||!actual.uv2.SequenceEqual(expected.uv2)||
                    !actual.colors.SequenceEqual(expected.colors)||!actual.triangles.SequenceEqual(expected.triangles))
                    throw new Exception("Packed "+label+" native attributes or winding differ from legacy mesh");
                var sub=actual.GetSubMesh(0);
                if(sub.firstVertex!=0||sub.vertexCount!=actual.vertexCount||sub.indexStart!=0||sub.indexCount!=(int)actual.GetIndexCount(0)||sub.topology!=MeshTopology.Triangles||
                    (workerBounds.center-expected.bounds.center).sqrMagnitude>1e-10f||(workerBounds.extents-expected.bounds.extents).sqrMagnitude>1e-10f||
                    actual.bounds!=workerBounds||sub.bounds!=workerBounds)
                    throw new Exception("Packed "+label+" worker/native/submesh bounds differ from legacy culling bounds");
                if(actual.vertexCount>0&&(actual.vertexBufferCount!=1||actual.GetVertexBufferStride(0)!=40))throw new Exception("Packed "+label+" native vertex layout differs");
            }
        }
        sealed class TemporaryMesh:IDisposable
        {
            public readonly Mesh Mesh=new Mesh{indexFormat=IndexFormat.UInt32};
            public void Dispose()=>UnityEngine.Object.DestroyImmediate(Mesh);
        }
        static string Digest(ChunkBuild mesh)
        {
            using(var stream=new MemoryStream())using(var writer=new BinaryWriter(stream))
            {
                void V3(Vector3[] values){writer.Write(values.Length);foreach(var v in values){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}}
                void V2(Vector2[] values){writer.Write(values.Length);foreach(var v in values){writer.Write(v.x);writer.Write(v.y);}}
                void Ints(int[] values){writer.Write(values.Length);foreach(var v in values)writer.Write(v);}
                V3(mesh.Vertices.Select(v=>v.Position).ToArray());V3(mesh.Vertices.Select(v=>v.Normal).ToArray());V2(mesh.Vertices.Select(v=>v.UV).ToArray());V2(mesh.Vertices.Select(v=>v.Tile).ToArray());Ints(mesh.Triangles);
                V3(mesh.FluidMesh.Vertices.Select(v=>v.Position).ToArray());V3(mesh.FluidMesh.Vertices.Select(v=>v.Normal).ToArray());Ints(mesh.FluidMesh.Indices);Ints(mesh.FluidMesh.ActiveCells);
                foreach(var c in mesh.FluidMesh.Vertices.Select(v=>v.Colour)){writer.Write(c.r);writer.Write(c.g);writer.Write(c.b);writer.Write(c.a);}
                writer.Flush();using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-","");
            }
        }
    }
}
