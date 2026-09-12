using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    // Presentation follows inventory capabilities; cosmetic previews remain separate.
    public sealed class HeldBlockView : MonoBehaviour
    {
        public FirstPersonPlayer Player;
        public byte ItemId {get;private set;}
        public GripPose? PreviewGrip {get;private set;}
        public GripPose DesiredGrip
        {
            get
            {
                if(PreviewGrip.HasValue)return PreviewGrip.Value;
                var stack=displayed;
                if(stack.Empty)return GripPose.Empty;
                if(stack.Id==BlockId.Torch)return GripPose.Tool;
                var tool=Player.Game.Registry.Capabilities(stack);
                if((tool&ToolCapability.Axe)!=0)return GripPose.Axe;
                if((tool&ToolCapability.Shovel)!=0)return GripPose.Shovel;
                if((tool&ToolCapability.Hoe)!=0)return GripPose.Hoe;
                if((tool&ToolCapability.Pickaxe)!=0)return GripPose.TwoHandTool;
                return tool!=ToolCapability.None?GripPose.Tool:GripPose.Block;
            }
        }
        public bool Visible=>view!=null&&view.activeInHierarchy;
        public Transform Hand {get;private set;}
        public Transform Socket {get;private set;}
        public Vector3 Centre=>view.transform.position;
        public void SetPreview(GripPose? grip){PreviewGrip=grip;}
        ItemStack displayed;
        int displayedSlot=-1;
        float equipLowering;
        bool equipping;
        public float EquipLowering=>equipLowering;
        // Cache presentation only. The selected inventory stack still owns every action.
        public void PrepareFrame(float dt)
        {
            var selected=Player.Game.Inventory.Slots[Player.Game.Selected];
            byte next=selected.Empty?(byte)0:selected.Id;
            byte current=displayed.Empty?(byte)0:displayed.Id;
            // A depleted source slot cannot leave a ghost item during a selection change.
            if(current!=0&&displayedSlot>=0&&Player.Game.Inventory.Slots[displayedSlot].Empty)
            {displayed=default;equipLowering=0;equipping=false;return;}
            equipping=next!=current;
            equipLowering=Mathf.MoveTowards(equipLowering,equipping?1:0,dt/(equipping?.09f:.14f));
            if(equipLowering>=1||next==current){displayed=selected;displayedSlot=Player.Game.Selected;}
        }
        GameObject industryItem;
        GameObject foodItem;
        Material foodMaterial;
        GameObject view,block,sword,pickaxe,axe,shovel,hoe,card,torch,bucket,bucketWater;
        Material cardMaterial,torchMaterial,waterMaterial;
        MeshFilter filter;
        Material material,toolMaterial,axeMaterial,industryMaterial,industryGlass;
        Vector3 bladeAxis,handleAxis;
        public Vector3 AxeBladeForward=>axe==null?Vector3.zero:axe.transform.TransformVector(bladeAxis).normalized;
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
            axeMaterial=new Material(Shader.Find("RivetReach/HeldTool"));axeMaterial.SetTexture("_BaseMap",Resources.Load<Texture2D>("Tools/StarterAxe"));
            axeMaterial.SetFloat("_AxePalette",1);
            industryMaterial=new Material(Shader.Find("RivetReach/HeldTool"));industryMaterial.SetTexture("_BaseMap",Resources.Load<Texture2D>("Industry/Atlas"));
            industryGlass=new Material(Shader.Find("RivetReach/HeldGlass"));industryGlass.SetColor("_BaseColor",new Color(.37f,.68f,.76f,.22f));
            view.SetActive(false);
        }
        GameObject Tool(string path)
        {
            var tool=Instantiate(Resources.Load<GameObject>(path),view.transform,false);
            foreach(var renderer in tool.GetComponentsInChildren<Renderer>()){renderer.sharedMaterial=toolMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;}
            return tool;
        }
        void LateUpdate()
        {
            if(Player==null||Player.Game==null)return;
            var game=Player.Game;var selected=displayed;byte id=selected.Empty?(byte)0:selected.Id;
            if(id!=ItemId)
            {
                bool reuseOre=industryItem!=null&&OreVisuals.UsesModel(ItemId)&&OreVisuals.UsesModel(id);
                ItemId=id;
                if(foodItem!=null){Destroy(foodItem);foodItem=null;}
                if(FoodVisuals.UsesModel(id))
                {
                    if(foodMaterial==null)
                    {foodMaterial=new Material(Shader.Find("RivetReach/HeldTool"));foodMaterial.SetTexture("_BaseMap",FoodVisuals.Palette);}
                    foodMaterial.SetTexture("_BaseMap",FoodVisuals.PaletteFor(id));
                    foodItem=FoodVisuals.Create(id,block.transform,foodMaterial);
                    // Long axis crosses the supporting palm; the baked opening faces up.
                    foodItem.transform.localRotation=Quaternion.Euler(0,20,0)*foodItem.transform.localRotation;
                    foodItem.transform.localPosition=new Vector3(0,-.22f,0);
                }
                if(industryItem!=null&&!reuseOre){Destroy(industryItem);industryItem=null;}
                industryMaterial.SetTexture("_BaseMap",OreVisuals.UsesModel(id)?OreVisuals.Palette(id):Resources.Load<Texture2D>("Industry/Atlas"));
                if(ItemAppearance.TryIndustryKey(id,out var assemblyKey)||OreVisuals.UsesModel(id)||StarterStationVisuals.UsesModel(id))
                {
                    var prefab=StarterStationVisuals.UsesModel(id)?StarterStationVisuals.Prefab(id):OreVisuals.UsesModel(id)?OreVisuals.Prefab:Resources.Load<GameObject>("Industry/Runtime/"+assemblyKey);
                    if(prefab!=null&&!reuseOre)
                    {
                        industryItem=ConnectedPipeVisuals.UsesConnectedMesh(id)?ConnectedPipeVisuals.Create(assemblyKey,block.transform):Instantiate(prefab,block.transform,false);
                        industryItem.transform.localRotation=Quaternion.Euler(0,180,0);
                        industryItem.transform.localPosition=-(industryItem.transform.localRotation*(Vector3.one*.5f));
                        foreach(var r in industryItem.GetComponentsInChildren<Renderer>())
                        {if(!ItemAppearance.VisibleIndustryPart(id,r.name))r.enabled=false;r.sharedMaterial=r.name.StartsWith("Glass")?industryGlass:industryMaterial;r.shadowCastingMode=ShadowCastingMode.Off;}
                    }
                }
                if(industryItem==null&&id!=BlockId.Torch&&(BlockId.Placeable(id)||BlockId.RawMaterial(id)))
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
                if(id!=0)
                {
                    var definition=game.Registry.Get(id);var tint=ItemAppearance.ToolTint(definition);
                    toolMaterial.SetColor("_BaseColor",tint);axeMaterial.SetColor("_BaseColor",tint);
                    if(foodItem==null&&industryItem==null&&(id==BlockId.Torch||!BlockId.Placeable(id)&&!BlockId.RawMaterial(id))&&definition.toolCapabilities==ToolCapability.None)
                    {
                        if(card==null)
                        {
                            card=GameObject.CreatePrimitive(PrimitiveType.Quad);Destroy(card.GetComponent<Collider>());card.transform.SetParent(block.transform,false);
                            card.transform.localScale=Vector3.one*1.5f;card.transform.localPosition=new Vector3(0,0,-.55f);
                            cardMaterial=new Material(Shader.Find("RivetReach/HeldTool"));cardMaterial.SetFloat("_Cutoff",.1f);cardMaterial.SetFloat("_Cull",0);
                            var cardRenderer=card.GetComponent<Renderer>();cardRenderer.sharedMaterial=cardMaterial;cardRenderer.shadowCastingMode=ShadowCastingMode.Off;
                        }
                        cardMaterial.SetTexture("_BaseMap",game.UI.ItemIcon(id));
                    }
                }
            }
            var grip=DesiredGrip;var rig=Player.Inspecting?Player.Body:Player.Arms;
            // Selection changes below the frame; the visible object stays attached throughout recovery.
            bool show=grip!=GripPose.Empty&&game.Started&&!game.Paused&&!game.InventoryOpen&&rig.Grip==grip;
            view.SetActive(show);if(!show)return;
            Hand=rig.Bone("HandR");Socket=rig.Bone(grip==GripPose.Block?"BlockSocket":"ToolSocket");
            if(view.transform.parent!=Socket)view.transform.SetParent(Socket,false);
            float boneUnits=rig.transform.InverseTransformVector(Socket.TransformVector(Vector3.up)).magnitude;
            view.transform.localPosition=Vector3.zero;view.transform.localRotation=Quaternion.identity;view.transform.localScale=Vector3.one/boneUnits;
            block.SetActive(grip==GripPose.Block);
            bool isBucket=Fluids.IsBucket(id),isTorch=id==BlockId.Torch;
            bool showCard=foodItem==null&&industryItem==null&&!isBucket&&!isTorch&&id!=0&&(id==BlockId.Torch||!BlockId.Placeable(id)&&!BlockId.RawMaterial(id))&&game.Registry.Get(id).toolCapabilities==ToolCapability.None;
            filter.GetComponent<Renderer>().enabled=!showCard&&!isBucket&&industryItem==null&&foodItem==null;
            if(isBucket&&bucket==null)
            {
                bucket=Tool("Characters/PalmBucket");bucket.transform.SetParent(block.transform,false);
                bucketWater=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(bucketWater.GetComponent<Collider>());bucketWater.transform.SetParent(block.transform,false);
                bucketWater.transform.localPosition=new Vector3(0,.32f,0);bucketWater.transform.localScale=new Vector3(.80f,.008f,.80f);
                waterMaterial=new Material(Shader.Find("RivetReach/HeldTool"));waterMaterial.SetColor("_BaseColor",new Color(.08f,.42f,.58f));
                var waterRenderer=bucketWater.GetComponent<Renderer>();waterRenderer.sharedMaterial=waterMaterial;waterRenderer.shadowCastingMode=ShadowCastingMode.Off;
            }
            if(bucketWater!=null)bucketWater.SetActive(isBucket&&id!=Fluids.EmptyBucket);
            if(bucket!=null)bucket.SetActive(isBucket);
            if(isTorch&&torch==null)
            {
                torch=Tool("Characters/GripTorch");torchMaterial=new Material(toolMaterial);torchMaterial.SetFloat("_Torch",1);torchMaterial.SetColor("_BaseColor",Color.white);
                foreach(var r in torch.GetComponentsInChildren<Renderer>())r.sharedMaterial=torchMaterial;
            }
            if(torch!=null)torch.SetActive(isTorch);if(card!=null)card.SetActive(showCard&&industryItem==null);
            if(showCard)card.transform.rotation=Player.Camera.transform.rotation;
            bool useAxe=!PreviewGrip.HasValue&&(game.Registry.Capabilities(selected)&ToolCapability.Axe)!=0;
            if(useAxe&&axe==null)
            {
                axe=Instantiate(Resources.Load<GameObject>(ItemAppearance.ToolPath(ToolCapability.Axe)),view.transform,false);
                foreach(var marker in axe.GetComponentsInChildren<Transform>())
                {
                    if(marker.name=="BladeForward")bladeAxis=axe.transform.InverseTransformPoint(marker.position).normalized;
                    if(marker.name=="HandleUp")handleAxis=axe.transform.InverseTransformPoint(marker.position).normalized;
                }
                foreach(var renderer in axe.GetComponentsInChildren<Renderer>()){renderer.sharedMaterial=axeMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;}
            }
            if(axe!=null)axe.SetActive(useAxe);
            if(useAxe)
            {
                // Keep the cutting edge fixed to the grip through the complete arc.
                // Re-aiming at the camera direction flips the head as the shaft passes it.
                var shaft=axe.transform.localRotation*handleAxis;
                var blade=axe.transform.localRotation*bladeAxis;
                var desired=Vector3.ProjectOnPlane(axe.transform.parent.InverseTransformDirection(Hand.up),shaft);
                if(desired.sqrMagnitude>.0001f)
                    axe.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(blade,desired,shaft),shaft)*axe.transform.localRotation;
            }
            var capability=game.Registry.Capabilities(selected);bool useShovel=!PreviewGrip.HasValue&&(capability&ToolCapability.Shovel)!=0,useHoe=!PreviewGrip.HasValue&&(capability&ToolCapability.Hoe)!=0;
            if(useShovel&&shovel==null)shovel=Tool(ItemAppearance.ToolPath(ToolCapability.Shovel));
            if(useHoe&&hoe==null)hoe=Tool(ItemAppearance.ToolPath(ToolCapability.Hoe));
            if(shovel!=null)shovel.SetActive(useShovel);if(hoe!=null)hoe.SetActive(useHoe);
            if(grip==GripPose.Tool&&!isTorch&&!useAxe&&!useShovel&&!useHoe&&sword==null)sword=Tool(ItemAppearance.ToolPath(ToolCapability.Blade));
            if(grip==GripPose.TwoHandTool&&pickaxe==null)pickaxe=Tool(ItemAppearance.ToolPath(ToolCapability.Pickaxe));
            if(sword!=null)sword.SetActive(grip==GripPose.Tool&&!isTorch&&!useAxe&&!useShovel&&!useHoe);if(pickaxe!=null)pickaxe.SetActive(grip==GripPose.TwoHandTool);
            float firstPerson=Player.Inspecting?0:1;
            material.SetFloat("_FirstPerson",firstPerson);toolMaterial.SetFloat("_FirstPerson",firstPerson);axeMaterial.SetFloat("_FirstPerson",firstPerson);
            industryMaterial.SetFloat("_FirstPerson",firstPerson);industryGlass.SetFloat("_FirstPerson",firstPerson);
            if(waterMaterial!=null)waterMaterial.SetFloat("_FirstPerson",firstPerson);
            if(torchMaterial!=null)torchMaterial.SetFloat("_FirstPerson",firstPerson);
            if(cardMaterial!=null)cardMaterial.SetFloat("_FirstPerson",firstPerson);
            if(foodMaterial!=null)foodMaterial.SetFloat("_FirstPerson",firstPerson);
        }
        public void FrameFirstPerson()
        {
            var arm=Player.Arms.transform;
            float amount=equipLowering*equipLowering*(3-2*equipLowering);
            arm.localRotation=Quaternion.Euler(12*amount,0,-6*amount);
            arm.localPosition+=new Vector3(.025f,-.52f,.08f)*amount*arm.localScale.x;
        }
        void OnDestroy(){if(foodMaterial!=null)Destroy(foodMaterial);if(industryMaterial!=null)Destroy(industryMaterial);if(industryGlass!=null)Destroy(industryGlass);if(view!=null)Destroy(view);if(material!=null)Destroy(material);if(toolMaterial!=null)Destroy(toolMaterial);if(axeMaterial!=null)Destroy(axeMaterial);if(torchMaterial!=null)Destroy(torchMaterial);if(waterMaterial!=null)Destroy(waterMaterial);if(cardMaterial!=null)Destroy(cardMaterial);foreach(var mesh in meshes.Values)Destroy(mesh);}
    }
}
