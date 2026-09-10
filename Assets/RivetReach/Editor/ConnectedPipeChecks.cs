using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class ConnectedPipeChecks
    {
        sealed class World : IIndustryWorld
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public bool Ready(BlockPos p)=>true;
            public byte Get(BlockPos p)=>Cells.TryGetValue(p,out var b)?b:(byte)0;
            public bool Remove(BlockPos p,byte expected){if(Get(p)!=expected)return false;Cells.Remove(p);return true;}
            public ItemContainer Storage(BlockPos p)=>null;
            public byte Drop(byte b)=>b;
            public bool PlayerInside(BlockPos p)=>false;
        }
        public static void Run()
        {
            var log=new StringBuilder();int checks=0;
            void Check(bool ok,string message){if(!ok)throw new Exception("Connected pipes: "+message);checks++;log.AppendLine("PASS "+message);}
            foreach(string key in new[]{"item_pipe","fluid_pipe","power_cable","signal_conduit","pipe_signal_addition","pipe_power_addition"})
            for(int mask=0;mask<64;mask++)
            {
                var mesh=ConnectedPipeVisuals.Shape(key,mask);var vertices=mesh.vertices;var normals=mesh.normals;
                Check(vertices.Length>0&&mesh.triangles.Length>0&&mesh.uv.Length==vertices.Length&&mesh.subMeshCount==1,key+" mask "+mask+" imports with geometry and one atlas material");
                Check(normals.All(n=>!float.IsNaN(n.x)&&!float.IsNaN(n.y)&&!float.IsNaN(n.z)&&n.sqrMagnitude>.8f),key+" mask "+mask+" has finite nonzero normals");
                Check(vertices.All(v=>v.x>=-.001f&&v.x<=1.001f&&v.y>=-.001f&&v.y<=1.001f&&v.z>=-.001f&&v.z<=1.001f),key+" mask "+mask+" stays inside its placed cell");
                for(int face=0;face<6;face++)if((mask&(1<<face))!=0)
                {
                    int axis=face/2;float edge=(face&1)==0?1:0;
                    var end=vertices.Where(v=>Mathf.Abs(v[axis]-edge)<.0001f).ToArray();
                    Check(vertices.Select((v,i)=>(v,i)).Where(pair=>Mathf.Abs(pair.v[axis]-edge)<.0001f).All(pair=>Mathf.Abs(normals[pair.i][axis])<.002f),key+" mask "+mask+" preserves tangent normals at connected face "+face);
                    Check(end.Length>=8,key+" mask "+mask+" reaches connected face "+face);
                    // Adjacent opposite faces must align even when a neighboring cell has another junction shape.
                    var reference=ConnectedPipeVisuals.Shape(key,1<<(face^1)).vertices.Where(v=>Mathf.Abs(v[axis]-(1-edge))<.0001f).ToArray();
                    // Importers duplicate seam/cap vertices. Bounds measure the actual ring centre
                    // without bias from those duplicated samples.
                    Vector3 Centre(Vector3[] points)
                    {var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);return bounds.center;}
                    var a=Centre(end);var b=Centre(reference);a[axis]=b[axis]=0;
                    Check((a-b).sqrMagnitude<.000001f,key+" mask "+mask+" matches the opposite boundary anchor "+face);
                }
            }
            foreach(byte id in new[]{IndustryId.ItemPipe,IndustryId.FluidPipe})
            for(int mask=0;mask<64;mask++)
            {
                var world=new World();var sim=new IndustrySimulation(world,_=>64);var p=new BlockPos(0,20,0);
                MachineState Place(BlockPos at){world.Cells[at]=id;var m=sim.Add(at,id);m.Additions=PipeAddition.Signal|PipeAddition.Power;sim.Rotate(m);return m;}
                Place(p);for(int f=0;f<6;f++)if((mask&(1<<f))!=0)Place(IndustryDefinition.Neighbor(p,f));
                for(int tick=0;tick<8;tick++)sim.Step();
                var transport=id==IndustryId.ItemPipe?sim.ItemNetwork:sim.FluidNetwork;
                int Mask(NetworkTopology network)=>network.Connections.TryGetValue(p,out var value)?value:0;
                Check(Mask(transport)==mask&&Mask(sim.Signals.Topology)==mask&&Mask(sim.Power.Topology)==mask,"Rotated "+id+" discovers the real six-face transport and independent channel mask "+mask);
            }
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/connected-pipe-checks.txt",$"PASS {checks} assertions\n"+log);
        }
    }
}
