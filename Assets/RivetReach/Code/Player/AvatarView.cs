using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

namespace RivetReach
{
    // The Blender library owns poses and joint motion. Gameplay supplies state only.
    public sealed class AvatarView : MonoBehaviour
    {
        readonly Dictionary<string,Transform> bones=new Dictionary<string,Transform>();
        readonly List<Mesh> derivedMeshes=new List<Mesh>();
        GameObject model;
        Material material;
        AvatarMask miningMask;
        PlayableGraph graph;
        AnimationMixerPlayable locomotion;
        AnimationLayerMixerPlayable layers;
        AnimationClipPlayable[] movement;
        AnimationClipPlayable strike;
        float motionBlend,runBlend,airBlend,miningBlend,strikeTime,strikeLength;
        bool wasMining;
        public bool FirstPersonArms;
        public bool HideHeadAndArms;
        public bool Preview;
        public int TriangleCount {get;private set;}
        public int BoneCount {get;private set;}
        public int ClipCount {get;private set;}
        public bool AnimationReady => graph.IsValid();

        public void Build(bool female,int skin)
        {
            Release();bones.Clear();TriangleCount=0;
            string path="Characters/"+(female?"ExplorerFemale":"ExplorerMale");
            var asset=Resources.Load<GameObject>(path);
            if(asset==null){Debug.LogError("Missing player model");return;}
            model=Instantiate(asset,transform);model.name=female?"Female":"Male";
            model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
            foreach(var t in model.GetComponentsInChildren<Transform>())bones[t.name]=t;
            material=new Material(Resources.Load<Material>("Materials/Player"));
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
                    var source=r.sharedMesh;var derived=Instantiate(source);derivedMeshes.Add(derived);
                    var weights=source.boneWeights;var sourceIndices=source.triangles;var indices=new List<int>();
                    bool Visible(int vertex)
                    {
                        string bone=r.bones[weights[vertex].boneIndex0].name;
                        bool arm=IsArm(bone);
                        return FirstPersonArms?arm:!arm&&bone!="Head"&&bone!="Neck"&&bone!="Chest"&&bone!="Spine";
                    }
                    for(int i=0;i<sourceIndices.Length;i+=3)
                    {
                        if(!Visible(sourceIndices[i])||!Visible(sourceIndices[i+1])||!Visible(sourceIndices[i+2]))continue;
                        indices.Add(sourceIndices[i]);indices.Add(sourceIndices[i+1]);indices.Add(sourceIndices[i+2]);
                    }
                    derived.triangles=indices.ToArray();r.sharedMesh=derived;
                }
                TriangleCount+=r.sharedMesh.triangles.Length/3;
            }
            CreateAnimation(path);
        }
        static bool IsArm(string name)=>name.Contains("Arm")||name.Contains("Forearm")||name.Contains("Hand")||name.Contains("Finger")||name.Contains("Thumb");
        void CreateAnimation(string path)
        {
            var clips=Resources.LoadAll<AnimationClip>(path).Where(c=>!c.name.StartsWith("__preview__")).ToArray();ClipCount=clips.Length;
            AnimationClip Find(string name)=>clips.FirstOrDefault(c=>c.name==name||c.name.EndsWith("|"+name));
            string[] names=FirstPersonArms?new[]{"FP_Idle","FP_Walk"}:new[]{"Idle","Walk","Run","Airborne"};
            string mine=FirstPersonArms?"FP_Mine":"Mine";
            if(names.Any(n=>Find(n)==null)||Find(mine)==null){Debug.LogError("Incomplete explorer animation library: "+path);return;}
            var animator=model.GetComponent<Animator>()??model.AddComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            graph=PlayableGraph.Create(name+" Explorer animation");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            locomotion=AnimationMixerPlayable.Create(graph,names.Length);
            movement=new AnimationClipPlayable[names.Length];
            for(int i=0;i<names.Length;i++)
            {movement[i]=AnimationClipPlayable.Create(graph,Find(names[i]));graph.Connect(movement[i],0,locomotion,i);}
            locomotion.SetInputWeight(0,1);
            strike=AnimationClipPlayable.Create(graph,Find(mine));strikeLength=Find(mine).length;
            layers=AnimationLayerMixerPlayable.Create(graph,2);graph.Connect(locomotion,0,layers,0);graph.Connect(strike,0,layers,1);layers.SetInputWeight(0,1);
            if(!FirstPersonArms)
            {
                miningMask=new AvatarMask();miningMask.AddTransformPath(model.transform,true);
                for(int i=0;i<miningMask.transformCount;i++)
                {
                    string p=miningMask.GetTransformPath(i);
                    miningMask.SetTransformActive(i,p.Contains("/Chest"));
                }
                layers.SetLayerMaskFromAvatarMask(1,miningMask);
            }
            var output=AnimationPlayableOutput.Create(graph,"Explorer",animator);output.SetSourcePlayable(layers);
            motionBlend=runBlend=airBlend=miningBlend=strikeTime=0;wasMining=false;graph.Play();graph.Evaluate(0);
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
        public void Animate(float motion,bool mining,float phase,bool grounded=true,bool sprinting=false)
        {
            if(!graph.IsValid())return;
            float dt=Preview?Time.unscaledDeltaTime:Time.deltaTime;
            // Preview always breathes at rest. Movement phase follows distance travelled.
            float target=Preview?0:Mathf.Clamp01(motion);
            motionBlend=Mathf.MoveTowards(motionBlend,target,dt*8);
            runBlend=Mathf.MoveTowards(runBlend,sprinting?1:0,dt*6);
            airBlend=Mathf.MoveTowards(airBlend,grounded?0:1,dt*10);
            locomotion.SetInputWeight(0,(1-motionBlend)*(FirstPersonArms?1:1-airBlend));
            locomotion.SetInputWeight(1,motionBlend*(FirstPersonArms?1:(1-runBlend)*(1-airBlend)));
            movement[0].SetTime((movement[0].GetTime()+dt)%movement[0].GetAnimationClip().length);
            double gait=phase/(Mathf.PI*2);
            movement[1].SetTime(gait%1*movement[1].GetAnimationClip().length);
            if(!FirstPersonArms)
            {
                locomotion.SetInputWeight(2,motionBlend*runBlend*(1-airBlend));locomotion.SetInputWeight(3,airBlend);
                movement[2].SetTime(gait%1*movement[2].GetAnimationClip().length);movement[3].SetTime(0);
            }
            if(mining&&!wasMining)strikeTime=0;
            if(mining)strikeTime=(strikeTime+dt)%Mathf.Max(.01f,strikeLength);
            else strikeTime=Mathf.Min(strikeTime+dt,strikeLength);
            wasMining=mining;miningBlend=Mathf.MoveTowards(miningBlend,mining?1:0,dt*(mining?16:9));
            strike.SetTime(strikeTime);layers.SetInputWeight(1,miningBlend);graph.Evaluate(0);
        }
        // Explicit deterministic pose sampling for source/import comparison and regression evidence.
        public void SamplePose(string clip,float seconds)
        {
            if(model==null)return;
            var c=Resources.LoadAll<AnimationClip>("Characters/"+(model.name=="Female"?"ExplorerFemale":"ExplorerMale")).FirstOrDefault(a=>a.name==clip||a.name.EndsWith("|"+clip));
            if(c==null)throw new InvalidOperationException("Missing pose "+clip);
            c.SampleAnimation(model,seconds);
        }
        public Vector3 BonePosition(string name)=>bones[name].position;
        public Transform Bone(string name)=>bones[name];
        void Release()
        {
            if(graph.IsValid())graph.Destroy();
            if(model!=null){model.SetActive(false);Destroy(model);}if(material!=null)Destroy(material);if(miningMask!=null)Destroy(miningMask);
            foreach(var mesh in derivedMeshes)if(mesh!=null)Destroy(mesh);derivedMeshes.Clear();
        }
        void OnDestroy()=>Release();
    }
}
