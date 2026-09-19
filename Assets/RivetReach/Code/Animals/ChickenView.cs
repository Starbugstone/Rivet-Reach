using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace RivetReach
{
    // Presentation follows passive state and never awards eggs, offspring or loot.
    public sealed class ChickenView : MonoBehaviour
    {
        PlayableGraph graph;AnimationMixerPlayable mixer;readonly AnimationClipPlayable[] clips=new AnimationClipPlayable[4];
        readonly float[] lengths=new float[4];Renderer[] renderers;MaterialPropertyBlock properties;
        float clock,flash,deathTime;int current=-1;bool dead;
        public int Triangles {get;private set;}
        public void Initialize(ChickenState c)
        {
            string path="Chickens/"+(c.Adult?"Chicken":"Chick");var facing=new GameObject("Authored facing").transform;facing.SetParent(transform,false);
            var model=Instantiate(Resources.Load<GameObject>(path),facing,false);var animator=model.GetComponent<Animator>();
            if(animator==null)throw new InvalidOperationException("Chicken animator missing.");animator.applyRootMotion=false;
            graph=PlayableGraph.Create("Passive chicken");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);mixer=AnimationMixerPlayable.Create(graph,4);
            AnimationPlayableOutput.Create(graph,"Chicken pose",animator).SetSourcePlayable(mixer);
            var imported=Resources.LoadAll<AnimationClip>(path);string[] names={"Idle","Walk","Peck","Death"};
            for(int i=0;i<4;i++)
            {var clip=Array.Find(imported,a=>a.name==names[i]);if(clip==null)throw new InvalidOperationException("Chicken action missing: "+names[i]);lengths[i]=clip.length;clips[i]=AnimationClipPlayable.Create(graph,clip);clips[i].SetSpeed(0);graph.Connect(clips[i],0,mixer,i);}
            graph.Play();mixer.SetInputWeight(0,1);graph.Evaluate(0);
            Transform head=null,body=null;foreach(var t in model.GetComponentsInChildren<Transform>()){if(t.name=="Head")head=t;if(t.name=="Body")body=t;}
            if(head==null||body==null)throw new InvalidOperationException("Chicken facing bones missing.");
            facing.rotation=Quaternion.FromToRotation(Vector3.ProjectOnPlane(head.position-body.position,Vector3.up).normalized,transform.forward)*facing.rotation;
            renderers=model.GetComponentsInChildren<Renderer>();properties=new MaterialPropertyBlock();var material=Resources.Load<Material>("Chickens/Chicken");
            foreach(var r in renderers){r.sharedMaterial=material;if(r is SkinnedMeshRenderer skin){skin.updateWhenOffscreen=false;Triangles+=(int)skin.sharedMesh.GetIndexCount(0)/3;}}
        }
        public void Hit()=>flash=.16f;
        public void Die(){dead=true;deathTime=0;Play(3,0);}
        void Update(){if(!dead)return;deathTime+=Time.deltaTime;Play(3,Time.deltaTime);if(deathTime>=1.2f)Destroy(gameObject);}
        void Play(int action,float dt)
        {
            if(current!=action){current=action;clock=0;for(int i=0;i<4;i++)mixer.SetInputWeight(i,i==action?1:0);}
            clock+=dt;clips[action].SetTime(action==3?Mathf.Min(clock,lengths[action]-.001f):clock%lengths[action]);graph.Evaluate(0);
        }
        public void Present(ChickenState c,float dt,BlockPos origin)
        {
            transform.position=Vector3.Lerp(transform.position,c.Position.Local(origin),1-Mathf.Exp(-20*dt));
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,c.Yaw,0),1-Mathf.Exp(-12*dt));
            Play(c.PeckTicks>0?2:c.PathIndex<c.Path.Count?1:0,dt);flash=Mathf.Max(0,flash-dt);
            properties.SetColor("_BaseColor",flash>0?new Color(1.6f,.65f,.45f):c.LoveTicks>0?new Color(1.15f,.85f,.9f):Color.white);
            foreach(var r in renderers)r.SetPropertyBlock(properties);
        }
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}
