using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class FactoryVisibilityChecks
    {
        public static void Run()
        {
            var lines=new List<string>();void Check(bool value,string message){if(!value)throw new Exception(message);lines.Add("PASS "+message);}
            var tank=new Bounds(new Vector3(0,0,80),new Vector3(20,20,20));
            Check(FactoryVisibility.Visible(Vector3.zero,tank,72),"Whole tank bounds retain a near wall even when the controller/center is outside range");
            Check(!FactoryVisibility.Visible(Vector3.zero,tank,69),"Whole tank disappears only outside its nearest visible bound");
            Check(FactoryVisibility.Visible(Vector3.zero,new Bounds(new Vector3(48,48,0),Vector3.one),304),"Elevated machinery survives the former 64-block sphere");
            Check(FactoryVisibility.FadeStart<FactoryVisibility.FadeEnd&&FactoryVisibility.FadeEnd<FactoryVisibility.DetailRange,"Detailed roots overlap the complete fade band");
            long original=0,reduced=0;
            foreach(byte id in new[]{IndustryId.Boiler,IndustryId.Alternator,IndustryId.Crusher,IndustryId.Pump,IndustryId.WindTurbine,IndustryId.Tank})
            {
                var prefab=Resources.Load<GameObject>("Industry/Runtime/"+IndustryDefinition.All[id].Key);var mesh=DistantMesh.Combine(prefab);
                try
                {
                    long source=0;foreach(var f in prefab.GetComponentsInChildren<MeshFilter>(true))if(f.name!="StatusLight")source+=(long)f.sharedMesh.GetIndexCount(0)/3;
                    long result=(long)mesh.GetIndexCount(0)/3;original+=source;reduced+=result;
                    Check(result>0&&result<=source,IndustryDefinition.All[id].Key+": source "+source+" triangles, distant "+result);
                    foreach(var p in mesh.vertices)Check(float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z),"Finite derived vertex");
                }
                finally{UnityEngine.Object.DestroyImmediate(mesh);}
            }
            Check(reduced<original,"Measured representative distant geometry reduction");
            for(int mask=0;mask<64;mask++)for(int rotation=0;rotation<4;rotation++)for(int disconnected=0;disconnected<64;disconnected++)
            {
                var transform=ConnectedPipeVisuals.ShapeTransform(mask,rotation,disconnected);
                var center=transform.MultiplyPoint3x4(Vector3.one*.5f);
                Check(float.IsFinite(center.x)&&transform.determinant>0,"Pipe transform preserves finite positive scale");
            }
            var shader=Resources.Load<Shader>("Materials/MachineLit");Check(shader!=null,"Shared distance shader is included");
            Directory.CreateDirectory("Logs/ReleaseReview");File.WriteAllLines("Logs/ReleaseReview/factory-visibility-checks.txt",new[]{"PASS "+lines.Count+" assertions; representative triangles "+original+" -> "+reduced});
            File.AppendAllLines("Logs/ReleaseReview/factory-visibility-checks.txt",lines.FindAll(l=>!l.EndsWith("Finite derived vertex")&&!l.EndsWith("Pipe transform preserves finite positive scale")));
        }
    }
}
