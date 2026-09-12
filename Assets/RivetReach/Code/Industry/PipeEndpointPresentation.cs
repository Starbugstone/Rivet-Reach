using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    // Shared code-native direction glyphs. They remain aligned with flow while
    // turning around the pipe axis to stay readable from above, below or either side.
    public sealed class PipeEndpointPresentation : MonoBehaviour
    {
        sealed class View { public Transform Root;public Renderer Arrow;public MachineState Pipe;public int Face; }
        readonly Dictionary<(BlockPos,int),View> views=new Dictionary<(BlockPos,int),View>();
        readonly HashSet<(BlockPos,int)> visible=new HashSet<(BlockPos,int)>();
        readonly List<(BlockPos,int)> remove=new List<(BlockPos,int)>();
        Expedition game;float nextRefresh;Mesh glyph;Material input,output,outline;
        public int ViewCount=>views.Count;
        public Transform ViewAt(BlockPos p,int face)=>views.TryGetValue((p,face),out var v)?v.Root:null;
        public void Initialize(Expedition owner)
        {
            game=owner;glyph=CreateGlyph();
            var source=Resources.Load<Material>("Industry/Status");
            input=new Material(source){name="Pipe input blue"};input.SetColor("_BaseColor",new Color(.12f,.57f,1));
            output=new Material(source){name="Pipe output red"};output.SetColor("_BaseColor",new Color(1,.13f,.10f));
            outline=new Material(source){name="Pipe arrow border"};outline.SetColor("_BaseColor",new Color(.012f,.018f,.025f));
        }
        static Mesh CreateGlyph()
        {
            // Arrow in XY, pointing +Y. Duplicate reversed triangles are intentional
            // so an end remains readable while the player crosses its facing plane.
            var mesh=new Mesh{name="Pipe direction arrow"};
            mesh.vertices=new[]{new Vector3(-.045f,-.14f,0),new Vector3(.045f,-.14f,0),new Vector3(.045f,.015f,0),new Vector3(-.045f,.015f,0),new Vector3(-.105f,.015f,0),new Vector3(.105f,.015f,0),new Vector3(0,.145f,0)};
            mesh.triangles=new[]{0,1,2,0,2,3,4,5,6,2,1,0,3,2,0,6,5,4};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        Renderer Surface(Transform parent,string name,Material material,float scale,float depth)
        {
            var root=new GameObject(name);root.transform.SetParent(parent,false);root.transform.localScale=Vector3.one*scale;root.transform.localPosition=Vector3.forward*depth;
            root.AddComponent<MeshFilter>().sharedMesh=glyph;var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return renderer;
        }
        void LateUpdate()
        {
            if(game==null||game.Player==null)return;
            var sim=game.Industry.Simulation;
            if(Time.unscaledTime>=nextRefresh)
            {
                nextRefresh=Time.unscaledTime+.2f;visible.Clear();
                foreach(var pipe in sim.EligibleMachines)
                {
                    if(!PipeConnections.IsTransport(pipe.Definition.Id)||(game.World.Local(pipe.Position)-game.Player.transform.position).sqrMagnitude>32*32)continue;
                    for(int face=0;face<6;face++)
                    {
                        if(!game.HoldingWrench||!sim.HasPipeEnd(pipe,face)||sim.PipeEndRole(pipe,face)==PortRole.Disabled)continue;var key=(pipe.Position,face);visible.Add(key);
                        // A replacement pipe can reuse the position before the next visual refresh.
                        if(views.TryGetValue(key,out var existing)){existing.Pipe=pipe;continue;}
                        var root=new GameObject("Pipe end "+face);root.transform.SetParent(transform,false);
                        Surface(root.transform,"Arrow border",outline,1.1f,0);
                        views.Add(key,new View{Root=root.transform,Arrow=Surface(root.transform,"Flow arrow",input,1,.004f),Pipe=pipe,Face=face});
                    }
                }
                remove.Clear();foreach(var pair in views)if(!visible.Contains(pair.Key))remove.Add(pair.Key);
                foreach(var key in remove){Destroy(views[key].Root.gameObject);views.Remove(key);}
            }
            foreach(var v in views.Values)
            {
                v.Root.gameObject.SetActive(game.HoldingWrench&&sim.HasPipeEnd(v.Pipe,v.Face)&&sim.PipeEndRole(v.Pipe,v.Face)!=PortRole.Disabled);
                var d=IndustryDefinition.Directions[v.Face];var axis=new Vector3(d.x,d.y,d.z);
                var center=game.World.Local(v.Pipe.Position)+Vector3.one*.5f+axis*.34f;
                var normal=Vector3.ProjectOnPlane(game.Player.Camera.transform.position-center,axis);
                if(normal.sqrMagnitude<.0001f)normal=Mathf.Abs(axis.y)>.5f?Vector3.forward:Vector3.up;
                normal.Normalize();bool entering=sim.PipeEndRole(v.Pipe,v.Face)==PortRole.Input;
                v.Root.position=center+normal*.21f;v.Root.rotation=Quaternion.LookRotation(normal,entering?axis:-axis);
                v.Arrow.sharedMaterial=entering?input:output;
            }
        }
        void OnDestroy(){if(glyph!=null)Destroy(glyph);if(input!=null)Destroy(input);if(output!=null)Destroy(output);if(outline!=null)Destroy(outline);}
    }
}
