using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace RivetReach
{
    public sealed class MobView : MonoBehaviour
    {
        public MobState State {get;private set;}
        static readonly string[] ClipNames={"Idle","Walk","Attack","Death"};
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        readonly AnimationClipPlayable[] clips=new AnimationClipPlayable[4];
        readonly float[] lengths=new float[4];
        Renderer[] renderers;
        MaterialPropertyBlock properties;
        float flash,clipTime,blend;
        int current=-1,previous=-1;
        public int Triangles {get;private set;}
        public void Initialize(MobState state,Material material)
        {
            State=state;
            var prefab=Resources.Load<GameObject>("Mobs/"+state.Definition.model);
            if(prefab==null)throw new InvalidOperationException("Missing creature model: "+state.Definition.model);
            var facing=new GameObject("Authored forward axis");facing.transform.SetParent(transform,false);
            var model=Instantiate(prefab,facing.transform);model.name=state.Definition.displayName;
            // Named bones define facing while preserving FBX's axis/unit conversion. The
            // independent parent cannot be overwritten by an imported root animation curve.
            Transform head=null,body=null;
            foreach(var bone in model.GetComponentsInChildren<Transform>())
            {if(bone.name=="Head")head=bone;if(bone.name=="Body")body=bone;}
            if(head==null||body==null)throw new InvalidOperationException("Creature facing bones missing.");
            var animator=model.GetComponent<Animator>();
            if(animator==null)throw new InvalidOperationException("Missing creature animator: "+state.Definition.model);
            animator.applyRootMotion=false;
            graph=PlayableGraph.Create(state.Definition.displayName);graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer=AnimationMixerPlayable.Create(graph,4);
            var output=AnimationPlayableOutput.Create(graph,"Creature pose",animator);output.SetSourcePlayable(mixer);
            var imported=Resources.LoadAll<AnimationClip>("Mobs/"+state.Definition.model);
            for(int i=0;i<ClipNames.Length;i++)
            {
                var clip=Array.Find(imported,c=>c.name==ClipNames[i]);
                if(clip==null)throw new InvalidOperationException("Missing creature action: "+ClipNames[i]);
                lengths[i]=clip.length;clips[i]=AnimationClipPlayable.Create(graph,clip);clips[i].SetApplyFootIK(false);clips[i].SetSpeed(0);
                graph.Connect(clips[i],0,mixer,i);
            }
            graph.Play();mixer.SetInputWeight(0,1);graph.Evaluate(0);
            // Generic import applies its root conversion when the graph first evaluates.
            // Calibrate from that evaluated pose so movement and the visible head agree.
            var forward=Vector3.ProjectOnPlane(head.position-body.position,Vector3.up).normalized;
            facing.transform.rotation=Quaternion.FromToRotation(forward,transform.forward)*facing.transform.rotation;
            renderers=model.GetComponentsInChildren<Renderer>();properties=new MaterialPropertyBlock();
            foreach(var renderer in renderers)
            {
                renderer.sharedMaterial=material;
                if(renderer is SkinnedMeshRenderer skin){skin.updateWhenOffscreen=false;Triangles+=(int)skin.sharedMesh.GetIndexCount(0)/3;}
            }
        }
        public void Hit(){flash=.15f;}
        public void Present(Vector3 position,float dt)
        {
            transform.position=Vector3.Lerp(transform.position,position,1-Mathf.Exp(-20*dt));
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,State.Yaw,0),1-Mathf.Exp(-12*dt));
            int clip=State.Intent==MobIntent.Dead?3:State.Intent==MobIntent.Windup?2:
                State.Intent==MobIntent.Chase||State.Intent==MobIntent.Return||State.Intent==MobIntent.Wander?1:0;
            if(clip!=current){previous=current;current=clip;clipTime=0;blend=previous<0?1:0;}
            float speed=clip==1?State.Definition.speed*(State.Intent==MobIntent.Chase?1:.45f)/State.Definition.strideLength:
                clip==2?lengths[clip]/State.Definition.windup:clip==3?lengths[clip]/1.4f:1;
            clipTime+=dt*speed;blend=Mathf.Min(1,blend+dt/.12f);
            clips[current].SetTime(clip<2?clipTime%lengths[clip]:Mathf.Min(clipTime,lengths[clip]-.001f));
            for(int i=0;i<4;i++)mixer.SetInputWeight(i,i==current?blend:i==previous?1-blend:0);
            graph.Evaluate(0);
            flash=Mathf.Max(0,flash-dt);
            var colour=flash>0?new Color(1.8f,.58f,.35f):State.Intent==MobIntent.Windup?new Color(1.35f,.85f,.65f):Color.white;
            properties.SetColor("_BaseColor",colour);
            foreach(var renderer in renderers)renderer.SetPropertyBlock(properties);
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
