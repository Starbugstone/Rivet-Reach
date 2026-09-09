using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RivetReach
{
    public sealed class FirstPersonPlayer : MonoBehaviour
    {
        public Expedition Game;
        public Camera Camera;
        public AvatarView Body,Arms;
        public HeldBlockView HeldBlock {get;private set;}
        public bool Inspecting {get;private set;}
        public bool Grounded {get;private set;}
        public bool Sprinting {get;private set;}
        public bool HasTarget {get;private set;}
        public BlockPos Target {get;private set;}
        public byte TargetId {get;private set;}
        public float MiningProgress {get;private set;}
        public float Yaw=25,Pitch=10;
        public bool Female;
        public int Skin;
        public float Height=1.8f;
        float vertical,phase;
        byte previousHeld;float previousSwingPhase;bool hitSoundPlayed;
        float eyeHeight=1.64f,eyeVelocity;
        Vector2 handSway,handSwayVelocity;
        public float VisualEyeHeight=>eyeHeight;
        GameObject selection;
        public bool SelectionVisible=>selection!=null&&selection.activeInHierarchy;
        float nextPlace;
        float eating,fallDistance;
        byte eatingItem;
        public float EatingProgress=>eating/1.2f;
        byte miningItem;
        float lastForwardPress=float.NegativeInfinity;
        bool doubleTapSprint;
        Material lineMaterial;
        Mesh lineMesh;
        public Vector2? VerificationMovement;
        public bool VerificationMining;
        public bool? VerificationCrouching;
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
            HeldBlock=gameObject.AddComponent<HeldBlockView>();HeldBlock.Player=this;
            game.World.OriginShifted+=shift=>transform.position-=shift;
            CreateSelection();
        }
        public void RefreshAppearance()
        {
            if(HeldBlock!=null)HeldBlock.Detach();
            Body.HideHeadAndArms=!Inspecting;Body.Build(Female,Skin);Arms.Build(Female,Skin);
            PlayerPrefs.SetInt("female",Female?1:0);PlayerPrefs.SetInt("skin",Skin);PlayerPrefs.Save();
        }
        void Update()
        {
            if(Game==null)return;

            bool control=Game.Started&&!Game.Paused&&!Game.InventoryOpen;
            if(!control){eating=0;eatingItem=0;}
            bool audibleSwing=false;
            if(!Game.Started)
            {
                ResetSprint();
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
            UpdateSprintGesture(control&&!Inspecting);
            transform.rotation=Quaternion.Euler(0,Yaw,0);
            if(!Game.Paused && Game.World.Ready(Game.World.Address(transform.position)))
            {
                bool crouch=control&&(VerificationCrouching??Game.Input.Held("Crouch"));
                float desired=crouch?1.25f:1.8f;
                if(desired<Height||!Game.World.Overlaps(transform.position,.6f,desired))Height=desired;
                Vector2 input=VerificationMovement??(control?Game.Input.Move:Vector2.zero);
                Sprinting=control&&Game.Hunger.CanSprint&&Height>=1.5f&&input.sqrMagnitude>0&&(Game.Input.Held("Sprint")||doubleTapSprint);
                float speed=Height<1.5f?2.2f:Sprinting?6.5f:4.5f;
                input=Vector2.ClampMagnitude(input,1);
                Vector3 move=transform.TransformDirection(new Vector3(input.x,0,input.y))*speed;
                bool jump=control&&Game.Input.Pressed("Jump")&&Grounded;
                // The requested 1.6-block apex leaves clearance for future half blocks.
                if(jump)vertical=8f;
                float dt=Mathf.Min(Time.deltaTime,.05f);
                float nextVertical=Mathf.Max(-35,vertical-20*dt);
                float verticalTravel=(vertical+nextVertical)*.5f*dt;vertical=nextVertical;
                if(crouch&&Grounded&&!jump&&!Game.World.Overlaps(transform.position+move*dt-Vector3.up*.12f,.6f,.12f))move=Vector3.zero;
                Vector3 previous=transform.position;
                transform.position=Game.World.Move(transform.position,move*dt+Vector3.up*verticalTravel,.6f,Height,out bool ground);
                if(Game.Mobs!=null)transform.position=Game.Mobs.ConstrainPlayer(previous,transform.position);
                float impact=!Grounded&&ground?Mathf.Max(0,-vertical):0;
                Grounded=ground;if(ground)vertical=-1;
                Vector3 travelled=transform.position-previous;travelled.y=0;
                float distance=travelled.magnitude,motion=Mathf.Clamp01(distance/Mathf.Max(.001f,speed*dt));
                if(control)Game.Hunger.Exert(distance*(Sprinting?.1:.01)+(jump?.2:0));
                if(!ground&&transform.position.y<previous.y)fallDistance+=previous.y-transform.position.y;
                if(ground){if(fallDistance>3)Game.TakeDamage(Mathf.Floor(fallDistance-3),DamageKind.Fall);fallDistance=0;}
                // Footfalls follow the same distance-driven phase as the authored feet.
                int priorContact=Mathf.FloorToInt(phase/Mathf.PI);
                if(ground)phase+=distance*(Height<1.5f?3.8f:speed>5?2.2f:2.4f)*(input.y<-.1f?-1:1);
                if(ground&&distance>.001f&&Mathf.FloorToInt(phase/Mathf.PI)!=priorContact)
                    Game.Sound.Step(Game.World.Get(Game.World.Address(transform.position-Vector3.up*.08f)),Height<1.5f?.42f:Sprinting?1.15f:1);
                if(impact>2)Game.Sound.Land(impact);
                bool swing=control&&(Game.Input.Mine||VerificationMining)&&!Game.Input.Place;
                var held=Game.Inventory.Slots[Game.Selected];byte heldId=held.Empty?(byte)0:held.Id;
                if(heldId!=previousHeld){Game.Sound.Equip();previousHeld=heldId;}
                var grip=HeldBlock.DesiredGrip;Body.SetGrip(grip);Arms.SetGrip(grip);
                Body.Animate(motion,swing,phase,Grounded,speed>5,Height<1.5f,input,impact);
                Arms.Animate(motion,swing,phase,Grounded,speed>5,Height<1.5f,input,impact);
                audibleSwing=swing;
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
            Arms.FitFirstPersonFov(Camera.fieldOfView);HeldBlock.FrameFirstPerson();
            Vector2 swayTarget=control?Vector2.ClampMagnitude(Game.Input.Look,8):Vector2.zero;
            handSway=Vector2.SmoothDamp(handSway,swayTarget,ref handSwayVelocity,.075f,100,Time.deltaTime);
            // Camera aim is direct; only the held hands lag slightly behind a turn.
            Arms.transform.localPosition+=new Vector3(-handSway.x*.0012f,-handSway.y*.001f,0);
            if(control&&!Inspecting)TargetAndMine();else{HasTarget=false;MiningProgress=0;nextPlace=0;}
            UpdateSwingSound(audibleSwing);
            selection.SetActive(HasTarget);
            if(HasTarget)selection.transform.position=Game.World.Local(Target)-Vector3.one*.002f;
        }
        void UpdateSwingSound(bool swing)
        {
            // Targeting and camera updates have completed for this frame before contact audio.
            if(swing)
            {
                if(Arms.SwingPhase<previousSwingPhase||previousSwingPhase==0){Game.Sound.Swing();hitSoundPlayed=false;}
                if(!hitSoundPlayed&&Arms.SwingPhase>=.43f){if(HasTarget)Game.Sound.Hit(TargetId,Game.World.Local(Target)+Vector3.one*.5f);hitSoundPlayed=true;}
                previousSwingPhase=Arms.SwingPhase;
            }
            else{previousSwingPhase=0;hitSoundPlayed=false;}
        }
        void ResetSprint(){doubleTapSprint=false;lastForwardPress=float.NegativeInfinity;Sprinting=false;}
        public void ResetMotion(){vertical=0;fallDistance=0;eating=0;MiningProgress=0;VerificationMovement=null;ResetSprint();}
        void UpdateSprintGesture(bool control)
        {
            if(!control||Game.Input.Rebinding!=null||(VerificationCrouching??Game.Input.Held("Crouch"))){ResetSprint();return;}
            bool forward=Game.Input.Held("Forward")&&!Game.Input.Held("Back");
            if(!forward)doubleTapSprint=false;
            if(!forward||!Game.Input.Pressed("Forward"))return;
            if(Time.unscaledTime-lastForwardPress<=.3f){doubleTapSprint=true;lastForwardPress=float.NegativeInfinity;}
            else lastForwardPress=Time.unscaledTime;
        }
        void OnDisable(){ResetSprint();}
        void TargetAndMine()
        {
            if(Game.Mode!=ScreenMode.Play||Game.Health.Dead)return;
            var selected=Game.Inventory.Slots[Game.Selected];byte heldId=selected.Empty?(byte)0:selected.Id;
            if(heldId!=miningItem){MiningProgress=0;miningItem=heldId;}
            ToolCapability tool=Game.Registry.Capabilities(selected);
            if(!Game.Input.Place&&Game.Mobs!=null&&Game.Mobs.HandlePlayerTarget(Game.Input.Mine||VerificationMining))
            {HasTarget=false;MiningProgress=0;eating=0;eatingItem=0;return;}
            bool found=Game.World.Raycast(Camera.transform.position,Camera.transform.forward,5,out var pos,out byte id);
            if(!found||!HasTarget||!pos.Equals(Target)||id!=TargetId)MiningProgress=0;
            HasTarget=found;Target=pos;TargetId=id;
            if(Game.Input.Place)
            {
                MiningProgress=0;
                if(found&&BlockId.Station(id)&&!Game.Input.Held("Crouch"))
                {if(Game.Input.PlacePressed)Game.TryOpenStation(pos);return;}
                if(found&&(tool&ToolCapability.Hoe)!=0&&(id==BlockId.Grass||id==BlockId.Dirt))
                {
                    if(Time.time>=nextPlace&&Game.World.Till(pos)){nextPlace=Time.time+.22f;Arms.TriggerSwing();Game.Hunger.Exert(.05);}
                    return;
                }
                if(found&&id==BlockId.Farmland&&selected.Id==BlockId.Potato)
                {if(Game.World.Plant(pos.Offset(0,1,0))){Game.Inventory.Take(Game.Selected,1);Arms.TriggerSwing();}return;}
                int food=selected.Empty?0:Game.Registry.Get(selected.Id).foodPoints;
                if(food>0)
                {
                    if(eatingItem!=selected.Id){eating=0;eatingItem=selected.Id;}
                    if(Game.Hunger.Food<HungerState.Maximum)eating+=Time.deltaTime;
                    if(eating>=1.2f){if(Game.Hunger.TryEat(Game.Inventory,Game.Selected,food)){Game.Sound.Pickup();Game.Notify("Ate "+Game.Registry.Get(selected.Id).displayName,1);}eating=0;}
                    return;
                }
                eating=0;
                // Failed attempts must not consume the repeat interval: underfoot space
                // may become clear for only a few frames near the apex of a jump.
                if(Time.time>=nextPlace&&Game.TryPlaceSelected()){Arms.TriggerSwing();Body.TriggerSwing();nextPlace=Time.time+.22f;}
                return;
            }
            nextPlace=0;eating=0;eatingItem=0;
            if(!found||!(Game.Input.Mine||VerificationMining)){MiningProgress=0;return;}
            if(!BlockId.Mineable(id,tool,Game.Registry.Tier(selected)))
            {MiningProgress=0;Game.Notify(BlockId.MiningHint(id,tool,Game.Registry.Tier(selected)),1);return;}
            MiningProgress+=Time.deltaTime/Game.Registry.MiningSeconds(id,selected);
            if(MiningProgress<1)return;
            MiningProgress=0;
            if(Game.World.Mine(pos,id,tool,Game.Registry.Tier(selected)))
            {
                Game.Hunger.Exert(.05);
                byte drop=Game.Registry.FistDrop(id);
                Game.Sound.Mine(id,Game.World.Local(pos)+Vector3.one*.5f);Game.Notify("Gathered "+Game.Registry.Get(drop).displayName+" — walk close to collect",1);
            }
        }
        void CreateSelection()
        {
            selection=new GameObject("Voxel selection");selection.transform.localScale=Vector3.one*1.004f;
            selection.transform.SetParent(Game.transform,false);
            var vertices=new Vector3[8];for(int i=0;i<8;i++)vertices[i]=new Vector3(i&1,(i>>1)&1,(i>>2)&1);
            int[] edges={0,1,0,2,0,4,1,3,1,5,2,3,2,6,3,7,4,5,4,6,5,7,6,7};
            var positions=new List<Vector3>();var other=new List<Vector3>();var sides=new List<Vector2>();var triangles=new List<int>();
            for(int i=0;i<edges.Length;i+=2)
            {
                int first=positions.Count;Vector3 a=vertices[edges[i]],b=vertices[edges[i+1]];
                positions.AddRange(new[]{a,a,b,b});other.AddRange(new[]{b,b,a,a});sides.AddRange(new[]{new Vector2(-1,0),new Vector2(1,0),new Vector2(1,0),new Vector2(-1,0)});
                triangles.AddRange(new[]{first,first+2,first+1,first+2,first+3,first+1});
            }
            lineMesh=new Mesh{name="Constant-width target outline"};lineMesh.SetVertices(positions);lineMesh.SetUVs(0,other);lineMesh.SetUVs(1,sides);lineMesh.SetTriangles(triangles,0);lineMesh.RecalculateBounds();
            selection.AddComponent<MeshFilter>().sharedMesh=lineMesh;
            lineMaterial=new Material(Shader.Find("RivetReach/BlockOutline"));var renderer=selection.AddComponent<MeshRenderer>();renderer.sharedMaterial=lineMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            selection.SetActive(false);
        }
        void OnDestroy(){if(lineMesh!=null)Destroy(lineMesh);if(lineMaterial!=null)Destroy(lineMaterial);if(selection!=null)Destroy(selection);}
    }
}
