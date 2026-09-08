using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RivetReach
{
    public sealed class FirstPersonPlayer : MonoBehaviour
    {
        public Expedition Game;
        public Camera Camera;
        public AvatarView Body,Arms;
        public bool Inspecting {get;private set;}
        public bool Grounded {get;private set;}
        public bool HasTarget {get;private set;}
        public BlockPos Target {get;private set;}
        public byte TargetId {get;private set;}
        public float MiningProgress {get;private set;}
        public float Yaw=25,Pitch=10;
        public bool Female;
        public int Skin;
        public float Height=1.8f;
        float vertical,phase,footstep;
        float eyeHeight=1.64f,eyeVelocity;
        Vector2 handSway,handSwayVelocity;
        public float VisualEyeHeight=>eyeHeight;
        GameObject selection,placementGhost;
        Material ghostMaterial;
        float nextPlace;
        Material lineMaterial;
        Mesh lineMesh;
        public Vector2? VerificationMovement;
        public bool VerificationMining;
        public void Initialize(Expedition game)
        {
            Game=game;
            var cameraObject=new GameObject("Expedition Camera");cameraObject.transform.SetParent(transform,false);
            Camera=cameraObject.AddComponent<Camera>();Camera.tag="MainCamera";Camera.nearClipPlane=.025f;Camera.farClipPlane=640;
            Camera.fieldOfView=PlayerPrefs.GetFloat("fov",78);Camera.backgroundColor=new Color(.52f,.66f,.76f);Camera.clearFlags=CameraClearFlags.Skybox;
            var cameraData=Camera.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;cameraData.dithering=true;
            cameraObject.AddComponent<AudioListener>();
            var body=new GameObject("Player appearance");body.transform.SetParent(transform,false);Body=body.AddComponent<AvatarView>();Body.HideHeadAndArms=true;
            var arms=new GameObject("First person hands");arms.transform.SetParent(Camera.transform,false);arms.transform.localPosition=new Vector3(0,-1.50f,.02f);Arms=arms.AddComponent<AvatarView>();Arms.FirstPersonArms=true;
            Female=PlayerPrefs.GetInt("female",0)==1;Skin=PlayerPrefs.GetInt("skin",0);RefreshAppearance();
            game.World.OriginShifted+=shift=>transform.position-=shift;
            CreateSelection();
        }
        public void RefreshAppearance()
        {
            Body.HideHeadAndArms=!Inspecting;Body.Build(Female,Skin);Arms.Build(Female,Skin);
            PlayerPrefs.SetInt("female",Female?1:0);PlayerPrefs.SetInt("skin",Skin);PlayerPrefs.Save();
        }
        void Update()
        {
            if(Game==null)return;

            bool control=Game.Started&&!Game.Paused&&!Game.InventoryOpen;
            if(!Game.Started)
            {
                transform.rotation=Quaternion.Euler(0,Yaw,0);
                Camera.transform.localPosition=Vector3.up*1.64f;
                Camera.transform.localRotation=Quaternion.Euler(Pitch,0,0);Arms.gameObject.SetActive(false);Body.gameObject.SetActive(false);return;
            }
            Body.gameObject.SetActive(true);Arms.gameObject.SetActive(!Inspecting&&!Game.InventoryOpen&&!Game.Paused);
            if(control)
            {
                var look=Game.Input.Look;Yaw+=look.x;Pitch=Mathf.Clamp(Pitch-look.y,-85,85);
                if(Game.Input.Pressed("Inspect")){Inspecting=!Inspecting;RefreshAppearance();}
            }
            transform.rotation=Quaternion.Euler(0,Yaw,0);
            if(!Game.Paused && Game.World.Ready(Game.World.Address(transform.position)))
            {
                bool crouch=control&&Game.Input.Held("Crouch");
                float desired=crouch?1.25f:1.8f;
                if(desired<Height||!Game.World.Overlaps(transform.position,.6f,desired))Height=desired;
                Vector2 input=VerificationMovement??(control?Game.Input.Move:Vector2.zero);
                float speed=Height<1.5f?2.2f:control&&Game.Input.Held("Sprint")?6.5f:4.5f;
                input=Vector2.ClampMagnitude(input,1);
                Vector3 move=transform.TransformDirection(new Vector3(input.x,0,input.y))*speed;
                bool jump=control&&Game.Input.Pressed("Jump")&&Grounded;
                if(jump)vertical=6.7f;
                vertical=Mathf.Max(-35,vertical-20*Mathf.Min(Time.deltaTime,.05f));
                float dt=Mathf.Min(Time.deltaTime,.05f);
                if(crouch&&Grounded&&!jump&&!Game.World.Overlaps(transform.position+move*dt-Vector3.up*.12f,.6f,.12f))move=Vector3.zero;
                Vector3 previous=transform.position;
                transform.position=Game.World.Move(transform.position,(move+Vector3.up*vertical)*dt,.6f,Height,out bool ground);
                float impact=!Grounded&&ground?Mathf.Max(0,-vertical):0;
                Grounded=ground;if(ground)vertical=-1;
                Vector3 travelled=transform.position-previous;travelled.y=0;
                float distance=travelled.magnitude,motion=Mathf.Clamp01(distance/Mathf.Max(.001f,speed*dt));
                // A gait cycle covers two steps; advance only when the feet can contact terrain.
                if(ground)phase+=distance*(Height<1.5f?3.8f:speed>5?2.2f:2.4f)*(input.y<-.1f?-1:1);
                if(distance>.001f&&ground){footstep+=dt;if(footstep>.42f){Game.Sound.Step();footstep=0;}}
                Body.Animate(motion,control&&(Game.Input.Mine||VerificationMining),phase,Grounded,speed>5,Height<1.5f,input,impact);
                Arms.Animate(motion,control&&(Game.Input.Mine||VerificationMining),phase,Grounded,speed>5,Height<1.5f,input,impact);
            }
            if(Inspecting)
            {
                Vector3 desired=transform.position-transform.forward*3+Vector3.up*1.3f;
                var direction=desired-(transform.position+Vector3.up*1.3f);
                if(Game.World.Raycast(transform.position+Vector3.up*1.3f,direction.normalized,3,out var obstruction,out _))
                    desired=transform.position+Vector3.up*1.3f+direction.normalized*Mathf.Max(.4f,Vector3.Distance(Game.World.Local(obstruction),transform.position)-.7f);
                Camera.transform.position=desired;Camera.transform.LookAt(transform.position+Vector3.up*1.05f);
            }
            else
            {
                // Smooth the eye transition independently of the immediate collision-height change.
                eyeHeight=Mathf.SmoothDamp(eyeHeight,Height-.16f,ref eyeVelocity,.105f,20,Time.deltaTime);
                if(Game.World.Raycast(transform.position+Vector3.up*.1f,Vector3.up,eyeHeight,out var ceiling,out _))
                    eyeHeight=Mathf.Min(eyeHeight,Mathf.Max(.25f,Game.World.Local(ceiling).y-transform.position.y-.035f));
                Camera.transform.localPosition=Vector3.up*eyeHeight;
                Camera.transform.localRotation=Quaternion.Euler(Pitch,0,0);
            }
            // Put the first-person torso behind the eye, including the forward bend in crouch.
            // This keeps the open neck/shoulder cuts outside the lens without moving the camera,
            // collision capsule or interaction rays. Follow the same smooth crouch blend as the rig.
            Body.transform.localPosition=Inspecting?Vector3.zero:new Vector3(0,0,Mathf.Lerp(-.34f,-.48f,Body.CrouchWeight));
            Body.transform.localScale=Vector3.one;
            Arms.FitFirstPersonFov(Camera.fieldOfView);
            Vector2 swayTarget=control?Vector2.ClampMagnitude(Game.Input.Look,8):Vector2.zero;
            handSway=Vector2.SmoothDamp(handSway,swayTarget,ref handSwayVelocity,.075f,100,Time.deltaTime);
            // Camera aim is direct; only the held hands lag slightly behind a turn.
            Arms.transform.localPosition+=new Vector3(-handSway.x*.0012f,-handSway.y*.001f,0);
            if(control&&!Inspecting)TargetAndMine();else{HasTarget=false;MiningProgress=0;nextPlace=0;}
            bool preview=control&&!Inspecting&&HasTarget&&!Game.Inventory.Slots[Game.Selected].Empty;
            placementGhost.SetActive(preview);
            if(preview)
            {
                bool valid=Game.PlacementPreview(out var cell,out _);placementGhost.transform.position=Game.World.Local(cell)-Vector3.one*.004f;
                ghostMaterial.SetColor("_BaseColor",valid?new Color(.3f,1,.65f):new Color(1,.3f,.23f));
            }
            selection.SetActive(HasTarget);
            if(HasTarget)selection.transform.position=Game.World.Local(Target)-Vector3.one*.002f;
        }
        void TargetAndMine()
        {
            bool found=Game.World.Raycast(Camera.transform.position,Camera.transform.forward,5,out var pos,out byte id);
            if(!found||!HasTarget||!pos.Equals(Target))MiningProgress=0;
            HasTarget=found;Target=pos;TargetId=id;
            if(Game.Input.Place)
            {
                MiningProgress=0;
                if(Time.time>=nextPlace){Game.TryPlaceSelected();nextPlace=Time.time+.22f;}
                return;
            }
            nextPlace=0;
            if(!found||!(Game.Input.Mine||VerificationMining)){MiningProgress=0;return;}
            MiningProgress+=Time.deltaTime/Game.Registry.Get(id).fistSeconds;
            if(MiningProgress<1)return;
            MiningProgress=0;
            if(Game.World.Remove(pos,id))
            {
                Game.Items.Spawn(new ItemStack(id,1),Game.World.Local(pos)+new Vector3(.5f,.3f,.5f),Vector3.up*1.6f);
                Game.Sound.Mine();Game.Notify("Gathered "+Game.Registry.Get(id).displayName+" — walk close to collect",1);
            }
        }
        void CreateSelection()
        {
            selection=new GameObject("Voxel selection");selection.transform.localScale=Vector3.one*1.004f;
            selection.transform.SetParent(Game.transform,false);
            var vertices=new Vector3[8];for(int i=0;i<8;i++)vertices[i]=new Vector3(i&1,(i>>1)&1,(i>>2)&1);
            int[] edges={0,1,0,2,0,4,1,3,1,5,2,3,2,6,3,7,4,5,4,6,5,7,6,7};
            lineMesh=new Mesh{name="Selection edges"};lineMesh.vertices=vertices;lineMesh.SetIndices(edges,MeshTopology.Lines,0);lineMesh.RecalculateBounds();
            selection.AddComponent<MeshFilter>().sharedMesh=lineMesh;
            placementGhost=new GameObject("Placement preview");placementGhost.transform.SetParent(Game.transform,false);placementGhost.transform.localScale=Vector3.one*1.008f;
            placementGhost.AddComponent<MeshFilter>().sharedMesh=lineMesh;ghostMaterial=new Material(Resources.Load<Material>("Materials/Selection"));placementGhost.AddComponent<MeshRenderer>().sharedMaterial=ghostMaterial;placementGhost.SetActive(false);
            lineMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));lineMaterial.SetColor("_BaseColor",new Color(1,.85f,.4f));selection.AddComponent<MeshRenderer>().sharedMaterial=lineMaterial;
        }
        void OnDestroy(){if(placementGhost!=null)Destroy(placementGhost);if(ghostMaterial!=null)Destroy(ghostMaterial);if(lineMesh!=null)Destroy(lineMesh);if(lineMaterial!=null)Destroy(lineMaterial);if(selection!=null)Destroy(selection);}
    }
}
