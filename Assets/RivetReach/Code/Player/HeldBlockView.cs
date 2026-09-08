using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    // Presentation follows inventory. Tool previews exercise grips without adding gameplay items.
    public sealed class HeldBlockView : MonoBehaviour
    {
        public FirstPersonPlayer Player;
        public byte ItemId {get;private set;}
        public GripPose? PreviewGrip {get;private set;}
        public GripPose DesiredGrip=>PreviewGrip??(Player.Game.Inventory.Slots[Player.Game.Selected].Empty?GripPose.Empty:GripPose.Block);
        public bool Visible=>view!=null&&view.activeInHierarchy;
        public Transform Hand {get;private set;}
        public Transform Socket {get;private set;}
        public Vector3 Centre=>view.transform.position;
        public void SetPreview(GripPose? grip){PreviewGrip=grip;}
        GameObject view,block,sword,pickaxe;
        MeshFilter filter;
        Material material,toolMaterial;
        readonly Dictionary<byte,Mesh> meshes=new Dictionary<byte,Mesh>();
        public void Detach(){if(view!=null)view.transform.SetParent(transform,false);Hand=Socket=null;}
        void Awake()
        {
            view=new GameObject("Selected item in hand");view.transform.SetParent(transform,false);
            block=new GameObject("Palm block");block.transform.SetParent(view.transform,false);block.transform.localScale=Vector3.one*.14f;
            filter=block.AddComponent<MeshFilter>();var renderer=block.AddComponent<MeshRenderer>();
            material=new Material(Shader.Find("RivetReach/HeldBlock"));material.SetTexture("_Tiles",Resources.Load<Texture2DArray>("Materials/BlockTiles"));
            renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            toolMaterial=new Material(Shader.Find("RivetReach/HeldTool"));toolMaterial.SetTexture("_BaseMap",Resources.Load<Texture2D>("Characters/SkinField"));
            view.SetActive(false);
        }
        GameObject Tool(string path)
        {
            var tool=Instantiate(Resources.Load<GameObject>("Characters/"+path),view.transform,false);
            foreach(var renderer in tool.GetComponentsInChildren<Renderer>()){renderer.sharedMaterial=toolMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;}
            return tool;
        }
        void LateUpdate()
        {
            if(Player==null||Player.Game==null)return;
            var game=Player.Game;var selected=game.Inventory.Slots[game.Selected];byte id=selected.Empty?(byte)0:selected.Id;
            if(id!=ItemId)
            {
                ItemId=id;
                if(id!=0)
                {
                    if(!meshes.TryGetValue(id,out var mesh))
                    {
                        var cells=new byte[34*34*34];cells[ChunkMesher.Index(0,0,0)]=id;
                        var cube=ChunkMesher.Build(default,0,cells);
                        for(int i=0;i<cube.Vertices.Length;i++)cube.Vertices[i]-=Vector3.one*.5f;
                        mesh=new Mesh{name="Held terrain block "+id};mesh.vertices=cube.Vertices;mesh.normals=cube.Normals;mesh.uv=cube.UV;mesh.uv2=cube.Tiles;mesh.triangles=cube.Triangles;mesh.RecalculateBounds();meshes.Add(id,mesh);
                    }
                    filter.sharedMesh=mesh;
                }
            }
            var grip=DesiredGrip;var rig=Player.Inspecting?Player.Body:Player.Arms;
            // The object appears once the fingers have reached their new contact pose.
            bool show=grip!=GripPose.Empty&&game.Started&&!game.Paused&&!game.InventoryOpen&&rig.Grip==grip&&rig.GripWeight>.985f;
            view.SetActive(show);if(!show)return;
            Hand=rig.Bone("HandR");Socket=rig.Bone(grip==GripPose.Block?"BlockSocket":"ToolSocket");
            if(view.transform.parent!=Socket)view.transform.SetParent(Socket,false);
            float boneUnits=rig.transform.InverseTransformVector(Socket.TransformVector(Vector3.up)).magnitude;
            view.transform.localPosition=Vector3.zero;view.transform.localRotation=Quaternion.identity;view.transform.localScale=Vector3.one/boneUnits;
            block.SetActive(grip==GripPose.Block);
            if(grip==GripPose.Tool&&sword==null)sword=Tool("GripSword");
            if(grip==GripPose.TwoHandTool&&pickaxe==null)pickaxe=Tool("GripPickaxe");
            if(sword!=null)sword.SetActive(grip==GripPose.Tool);if(pickaxe!=null)pickaxe.SetActive(grip==GripPose.TwoHandTool);
            float firstPerson=Player.Inspecting?0:1;
            material.SetFloat("_FirstPerson",firstPerson);toolMaterial.SetFloat("_FirstPerson",firstPerson);
        }
        void OnDestroy(){if(view!=null)Destroy(view);if(material!=null)Destroy(material);if(toolMaterial!=null)Destroy(toolMaterial);foreach(var mesh in meshes.Values)Destroy(mesh);}
    }
}
