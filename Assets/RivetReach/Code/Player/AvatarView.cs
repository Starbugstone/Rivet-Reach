using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace RivetReach
{
    public enum GripPose { Empty, Block, Tool, TwoHandTool }

    // The Blender library owns poses and joint motion. Gameplay supplies state only.
    public sealed class AvatarView : MonoBehaviour
    {
        const float SwingSpeed=2.5f;
        readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
        readonly List<Mesh> derivedMeshes=new List<Mesh>();
        GameObject model;
        Material material;
        AvatarMask miningMask,gripMask;
        SkinnedMeshRenderer supportHand;
        int supportTriangles,supportVertices;
        readonly float[] gripWeights={1,0,0,0};
        public GripPose Grip {get;private set;}
        public float GripWeight=>gripWeights[(int)Grip];
        public void SetGrip(GripPose grip)=>Grip=grip;
        PlayableGraph graph;
        AnimationMixerPlayable locomotion,holding,strikes;
        AnimationLayerMixerPlayable layers;
        AnimationClipPlayable[] movement;
        AnimationClipPlayable[] holds,strikeClips;
        float motionBlend,runBlend,airBlend,crouchBlend,miningBlend,strikeTime,strikeLength;
        float landingTime=1,landingStrength,landingBlend;
        Vector2 directionBlend;
        Quaternion animatedHips,animatedChest;
        bool presentationApplied;
        public float MotionWeight=>motionBlend;
        public float CrouchWeight=>crouchBlend;
        public float MiningWeight=>miningBlend;
        public float LandingWeight=>landingBlend;
        bool wasMining;
        bool swingRequested,swingActive;
        public float SwingPhase=>strikeLength>0?Mathf.Clamp01(strikeTime/strikeLength):0;
        public void TriggerSwing()=>swingRequested=true;
        public bool FirstPersonArms;
        public bool HideHeadAndArms;
        public bool Preview;
        int triangles,vertices;
        public int TriangleCount=>triangles+(supportHand!=null&&supportHand.enabled?supportTriangles:0);
        public bool SupportHandVisible=>supportHand!=null&&supportHand.enabled;
        public int VertexCount=>vertices+(supportHand!=null&&supportHand.enabled?supportVertices:0);
        public int ShadowTriangleCount {get;private set;}
        public int BoneCount {get;private set;}
        public int ClipCount {get;private set;}
        public bool AnimationReady => graph.IsValid();

        public void Build(bool female,int skin)
        {
            Release();bones.Clear();triangles=vertices=ShadowTriangleCount=supportTriangles=supportVertices=0;presentationApplied=false;
            string path="Characters/"+(female?"ExplorerFemale":"ExplorerMale");
            var asset=Resources.Load<GameObject>(path);
            if(asset==null){Debug.LogError("Missing player model");return;}
            model=Instantiate(asset,transform);model.name=female?"Female":"Male";
            model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
            foreach(var t in model.GetComponentsInChildren<Transform>())bones[t.name]=t;
            material=new Material(Resources.Load<Material>("Materials/Player"));
            material.SetFloat("_FirstPerson",FirstPersonArms?1:0);
            material.SetTexture("_BaseMap",Resources.Load<Texture2D>("Characters/"+(skin==0?"SkinField":"SkinOchre")));
            foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                BoneCount=r.bones.Length;r.updateWhenOffscreen=false;
                // Authored clips are bounded in-place, including the first-person strike.
                var bounds=r.sharedMesh.bounds;bounds.Expand(1.4f);r.localBounds=bounds;
                if(FirstPersonArms)r.shadowCastingMode=ShadowCastingMode.Off;
                if(FirstPersonArms||HideHeadAndArms)
                {
                    var source=r.sharedMesh;
                    bool Visible(string bone)=>FirstPersonArms?IsArm(bone)&&bone.EndsWith("R"):!IsArm(bone)&&bone!="Head"&&bone!="Neck";
                    r.sharedMesh=ExtractMesh(source,r.bones,Visible);
                    if(FirstPersonArms)
                    {
                        var supportObject=new GameObject("Support hand");supportObject.transform.SetParent(r.transform,false);
                        supportHand=supportObject.AddComponent<SkinnedMeshRenderer>();
                        supportHand.sharedMesh=ExtractMesh(source,r.bones,bone=>IsArm(bone)&&bone.EndsWith("L"));
                        supportHand.bones=r.bones;supportHand.rootBone=r.rootBone;supportHand.sharedMaterial=material;supportHand.localBounds=bounds;
                        supportHand.shadowCastingMode=ShadowCastingMode.Off;supportHand.enabled=false;
                        supportTriangles=supportHand.sharedMesh.triangles.Length/3;supportVertices=supportHand.sharedMesh.vertexCount;
                    }
                    if(HideHeadAndArms)
                    {
                        // The visible first-person mesh omits the head and duplicate arms,
                        // while its shadow keeps the full silhouette on the same animated bones.
                        var shadowObject=new GameObject("Full player shadow");shadowObject.transform.SetParent(r.transform,false);
                        var shadow=shadowObject.AddComponent<SkinnedMeshRenderer>();shadow.sharedMesh=source;
                        shadow.bones=r.bones;shadow.rootBone=r.rootBone;shadow.sharedMaterial=material;shadow.localBounds=bounds;
                        shadow.shadowCastingMode=ShadowCastingMode.ShadowsOnly;shadow.receiveShadows=false;
                        r.shadowCastingMode=ShadowCastingMode.Off;ShadowTriangleCount+=source.triangles.Length/3;
                    }
                }
                triangles+=r.sharedMesh.triangles.Length/3;
                vertices+=r.sharedMesh.vertexCount;
            }
            CreateAnimation(path);
        }
        Mesh ExtractMesh(Mesh source,Transform[] skeleton,Func<string,bool> visible)
        {
            var weights=source.boneWeights;var sourceIndices=source.triangles;var indices=new List<int>();
            for(int i=0;i<sourceIndices.Length;i+=3)
            {
                if(!visible(skeleton[weights[sourceIndices[i]].boneIndex0].name)||!visible(skeleton[weights[sourceIndices[i+1]].boneIndex0].name)||!visible(skeleton[weights[sourceIndices[i+2]].boneIndex0].name))continue;
                indices.Add(sourceIndices[i]);indices.Add(sourceIndices[i+1]);indices.Add(sourceIndices[i+2]);
            }
            var used=indices.Distinct().ToArray();var remap=new int[source.vertexCount];
            for(int i=0;i<used.Length;i++)remap[used[i]]=i;
            var positions=source.vertices;var normals=source.normals;var uv=source.uv;var tangents=source.tangents;
            var mesh=new Mesh{name=source.name+" Visible region",indexFormat=source.indexFormat};
            mesh.vertices=used.Select(i=>positions[i]).ToArray();mesh.normals=used.Select(i=>normals[i]).ToArray();mesh.uv=used.Select(i=>uv[i]).ToArray();
            if(tangents.Length==source.vertexCount)mesh.tangents=used.Select(i=>tangents[i]).ToArray();
            mesh.boneWeights=used.Select(i=>weights[i]).ToArray();mesh.bindposes=source.bindposes;mesh.triangles=indices.Select(i=>remap[i]).ToArray();mesh.RecalculateBounds();derivedMeshes.Add(mesh);return mesh;
        }
        static bool IsArm(string name)=>name.Contains("Arm")||name.Contains("Forearm")||name.Contains("Hand")||name.Contains("Finger")||name.Contains("Thumb");
        void CreateAnimation(string path)
        {
            var clips=Resources.LoadAll<AnimationClip>(path).Where(c=>!c.name.StartsWith("__preview__")).ToArray();ClipCount=clips.Length;
            AnimationClip Find(string name)=>clips.FirstOrDefault(c=>c.name==name||c.name.EndsWith("|"+name));
            string[] names=FirstPersonArms?new[]{"FP_Idle","FP_Walk","FP_Run","FP_Airborne","FP_Crouch","FP_CrouchWalk","FP_Land"}:new[]{"Idle","Walk","Run","Airborne","CrouchIdle","CrouchWalk","Land"};
            string mine=FirstPersonArms?"FP_Mine":"Mine";
            if(names.Any(n=>Find(n)==null)||Find(mine)==null){Debug.LogError("Incomplete explorer animation library: "+path);return;}
            // Unity's missing-component sentinel in the Editor is not a CLR null.
            // Use the component API so the runtime Animator is created in Play mode too.
            if(!model.TryGetComponent<Animator>(out var animator))animator=model.AddComponent<Animator>();
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            graph=PlayableGraph.Create(name+" Explorer animation");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            locomotion=AnimationMixerPlayable.Create(graph,names.Length);
            movement=new AnimationClipPlayable[names.Length];
            for(int i=0;i<names.Length;i++)
            {movement[i]=AnimationClipPlayable.Create(graph,Find(names[i]));graph.Connect(movement[i],0,locomotion,i);}
            locomotion.SetInputWeight(0,1);
            holding=AnimationMixerPlayable.Create(graph,3);strikes=AnimationMixerPlayable.Create(graph,4);
            holds=new AnimationClipPlayable[3];strikeClips=new AnimationClipPlayable[4];
            string prefix=FirstPersonArms?"FP_":"";string[] suffixes={"","Block","Tool","TwoHandTool"};
            for(int i=0;i<4;i++)
            {
                strikeClips[i]=AnimationClipPlayable.Create(graph,Find(prefix+"Mine"+suffixes[i]));graph.Connect(strikeClips[i],0,strikes,i);
                if(i>0){holds[i-1]=AnimationClipPlayable.Create(graph,Find(prefix+"Hold"+suffixes[i]));graph.Connect(holds[i-1],0,holding,i-1);}
            }
            strikeLength=strikeClips[(int)Grip].GetAnimationClip().length;
            layers=AnimationLayerMixerPlayable.Create(graph,3);graph.Connect(locomotion,0,layers,0);graph.Connect(holding,0,layers,1);graph.Connect(strikes,0,layers,2);layers.SetInputWeight(0,1);
            var hierarchy=model.GetComponentsInChildren<Transform>();
            gripMask=new AvatarMask{transformCount=hierarchy.Length};miningMask=new AvatarMask{transformCount=hierarchy.Length};
            for(int i=0;i<hierarchy.Length;i++)
            {
                var t=hierarchy[i];string bonePath="";
                for(var current=t;current!=model.transform;current=current.parent)bonePath=current.name+(bonePath.Length==0?"":"/"+bonePath);
                bool arm=t==bones["ClavicleR"]||t.IsChildOf(bones["ClavicleR"])||t==bones["ClavicleL"]||t.IsChildOf(bones["ClavicleL"]);
                gripMask.SetTransformPath(i,bonePath);gripMask.SetTransformActive(i,arm);
                miningMask.SetTransformPath(i,bonePath);miningMask.SetTransformActive(i,FirstPersonArms?arm:t==bones["Chest"]||t.IsChildOf(bones["Chest"]));
            }
            layers.SetLayerMaskFromAvatarMask(1,gripMask);layers.SetLayerMaskFromAvatarMask(2,miningMask);
            for(int i=0;i<4;i++)gripWeights[i]=i==(int)Grip?1:0;
            UpdateGrip(0);
            var output=AnimationPlayableOutput.Create(graph,"Explorer",animator);output.SetSourcePlayable(layers);
            motionBlend=runBlend=airBlend=crouchBlend=miningBlend=strikeTime=landingBlend=0;landingTime=1;directionBlend=Vector2.zero;wasMining=swingRequested=swingActive=false;graph.Play();graph.Evaluate(0);
        }
        public void FitFirstPersonFov(float fieldOfView)
        {
            if(!FirstPersonArms)return;
            // Compensate camera projection around its origin: fixed 78-degree hand framing
            // while world FOV changes, without another camera or changes to gameplay reach.
            float scale=Mathf.Tan(fieldOfView*Mathf.Deg2Rad*.5f)/Mathf.Tan(78*Mathf.Deg2Rad*.5f);
            transform.localScale=new Vector3(scale,scale,1);
            transform.localPosition=new Vector3(0,-1.5f*scale,.02f);
        }
        static float Follow(float current,float target,float halfLife,float dt)
            =>Mathf.Lerp(current,target,1-Mathf.Exp(-.69314718f*dt/halfLife));
        public void Animate(float motion,bool mining,float phase,bool grounded=true,bool sprinting=false,
            bool crouching=false,Vector2 direction=default,float landingImpact=0,float deltaTime=-1)
        {
            if(!graph.IsValid())return;
            RestoreAnimatedPose();
            float dt=deltaTime>=0?deltaTime:Preview?Time.unscaledDeltaTime:Time.deltaTime;
            dt=Mathf.Clamp(dt,0,.05f);
            float target=Preview?0:Mathf.Clamp01(motion);
            // Short attack, softer settling, and exponential weights avoid frame-rate-dependent ramps.
            motionBlend=Follow(motionBlend,target,target>motionBlend?.025f:.045f,dt);
            runBlend=Follow(runBlend,sprinting?1:0,.05f,dt);
            airBlend=Follow(airBlend,grounded?0:1,.025f,dt);
            crouchBlend=Follow(crouchBlend,crouching?1:0,.045f,dt);
            directionBlend=Vector2.Lerp(directionBlend,direction,1-Mathf.Exp(-dt*18));
            if(landingImpact>2){landingTime=0;landingStrength=Mathf.Clamp01((landingImpact-2)/8);}
            float landLength=movement[6].GetAnimationClip().length;
            landingTime=Mathf.Min(landingTime+dt,landLength);
            landingBlend=landingStrength*Mathf.Sin(Mathf.PI*landingTime/landLength)*(1-airBlend)*(FirstPersonArms?1:1-crouchBlend);
            float ground=(1-airBlend)*(1-landingBlend),standing=ground*(1-crouchBlend),crouched=ground*crouchBlend;
            locomotion.SetInputWeight(0,(1-motionBlend)*standing);
            locomotion.SetInputWeight(1,motionBlend*(1-runBlend)*standing);
            locomotion.SetInputWeight(2,motionBlend*runBlend*standing);
            locomotion.SetInputWeight(3,airBlend*(1-landingBlend));
            locomotion.SetInputWeight(4,(1-motionBlend)*crouched);
            locomotion.SetInputWeight(5,motionBlend*crouched);
            locomotion.SetInputWeight(6,landingBlend);
            double gait=phase/(Mathf.PI*2);
            // The player supplies a continuous signed distance phase, including backpedalling.
            gait-=Math.Floor(gait);
            for(int i=0;i<movement.Length;i++)
            {
                float length=movement[i].GetAnimationClip().length;
                if(i==1||i==2||i==5)movement[i].SetTime(gait*length);
                else if(i==6)movement[i].SetTime(landingTime);
                else movement[i].SetTime((movement[i].GetTime()+dt)%length);
            }
            if(!swingActive)strikeLength=strikeClips[(int)Grip].GetAnimationClip().length;
            if(swingRequested||mining&&(!wasMining||!swingActive)){strikeTime=0;swingActive=true;}
            swingRequested=false;
            if(swingActive)
            {
                strikeTime+=dt*SwingSpeed;
                if(strikeTime>=strikeLength){if(mining)strikeTime%=Mathf.Max(.01f,strikeLength);else{strikeTime=strikeLength;swingActive=false;}}
            }
            wasMining=mining;miningBlend=Follow(miningBlend,swingActive?1:0,swingActive?.014f:.035f,dt*SwingSpeed);
            UpdateGrip(dt);
            for(int i=0;i<4;i++)strikeClips[i].SetTime(SwingPhase*strikeClips[i].GetAnimationClip().length);
            layers.SetInputWeight(2,miningBlend);graph.Evaluate(0);
            if(FirstPersonArms)
            {
                // Move the entire held assembly, preserving every palm/handle contact.
                model.transform.localPosition=new Vector3(.12f*gripWeights[3]+Mathf.Cos(phase)*.0035f*motionBlend,Mathf.Sin(phase*2)*.005f*motionBlend-.016f*crouchBlend-landingBlend*.025f,0);
                // Bring the support arm in from below the frame before exposing the prop.
                // This moves the whole limb, leaving its authored wrist/finger pose intact.
                float supportDrop=.45f*Mathf.Clamp01((.985f-gripWeights[3])/.985f);
                bones["ClavicleL"].position-=transform.TransformVector(Vector3.up*supportDrop);
            }
            if(!Preview&&!FirstPersonArms)
            {
                // Turn the stepping stance into a strafe while keeping the chest facing the view.
                // The direction filter affects bones only; input and collision remain immediate.
                var chest=bones["Chest"];var upperRotation=chest.rotation;var hips=bones["Hips"];
                animatedHips=hips.localRotation;animatedChest=chest.localRotation;presentationApplied=true;
                float heading=Mathf.Atan2(directionBlend.x,Mathf.Max(.02f,Mathf.Abs(directionBlend.y)))*Mathf.Rad2Deg;
                heading=Mathf.Clamp(heading*(directionBlend.y<-.1f?-1:1),-65,65)*motionBlend;
                hips.rotation=Quaternion.AngleAxis(heading,transform.up)*hips.rotation;
                chest.rotation=upperRotation*Quaternion.Euler(directionBlend.y*motionBlend*2,0,-directionBlend.x*motionBlend*5);
            }
        }
        void UpdateGrip(float dt)
        {
            for(int i=0;i<4;i++)gripWeights[i]=Follow(gripWeights[i],i==(int)Grip?1:0,.025f,dt);
            float held=1-gripWeights[0];layers.SetInputWeight(1,held);
            for(int i=0;i<4;i++)
            {
                strikes.SetInputWeight(i,gripWeights[i]);
                if(i>0){holding.SetInputWeight(i-1,held>.00001f?gripWeights[i]/held:0);holds[i-1].SetTime((holds[i-1].GetTime()+dt)%holds[i-1].GetAnimationClip().length);}
            }
            if(supportHand!=null)supportHand.enabled=gripWeights[3]>.005f;
        }
        // Explicit deterministic pose sampling for source/import comparison and regression evidence.
        void RestoreAnimatedPose()
        {
            if(!presentationApplied)return;
            bones["Hips"].localRotation=animatedHips;bones["Chest"].localRotation=animatedChest;presentationApplied=false;
        }
        public void SamplePose(string clip,float seconds)
        {
            if(model==null)return;
            RestoreAnimatedPose();
            var c=Resources.LoadAll<AnimationClip>("Characters/"+(model.name=="Female"?"ExplorerFemale":"ExplorerMale")).FirstOrDefault(a=>a.name==clip||a.name.EndsWith("|"+clip));
            if(c==null)throw new InvalidOperationException("Missing pose "+clip);
            c.SampleAnimation(model,seconds);
            model.transform.localPosition=FirstPersonArms&&clip.Contains("TwoHandTool")?Vector3.right*.12f:Vector3.zero;
            if(supportHand!=null)supportHand.enabled=clip.Contains("TwoHandTool");
        }
        public Vector3 BonePosition(string name)=>bones[name].position;
        public Transform Bone(string name)=>bones[name];
        void Release()
        {
            if(graph.IsValid())graph.Destroy();
            if(model!=null){model.SetActive(false);Destroy(model);}if(material!=null)Destroy(material);if(miningMask!=null)Destroy(miningMask);if(gripMask!=null)Destroy(gripMask);supportHand=null;
            foreach(var mesh in derivedMeshes)if(mesh!=null)Destroy(mesh);derivedMeshes.Clear();
        }
        void OnDestroy()=>Release();
    }
}
