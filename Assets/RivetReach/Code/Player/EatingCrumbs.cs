using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    // A small cosmetic pool. Crumbs never become inventory drops or world entities.
    public sealed class EatingCrumbs : MonoBehaviour
    {
        public const int Limit=32;
        public ParticleSystem Particles {get;private set;}
        public int Emitted {get;private set;}
        FirstPersonPlayer player;
        VoxelWorld world;
        Material material;
        Mesh mesh;
        readonly System.Random random=new System.Random(68031);
        readonly ParticleSystem.Particle[] shifted=new ParticleSystem.Particle[Limit];
        int lastBite=-1;
        float previousTime;

        public void Initialize(FirstPersonPlayer owner)
        {
            player=owner;world=owner.Game.World;world.OriginShifted+=Shift;
            var source=Resources.Load<GameObject>("Effects/ArcadeChips");
            foreach(var filter in source.GetComponentsInChildren<MeshFilter>())if(filter.name=="StoneChip")
            {
                mesh=Instantiate(filter.sharedMesh);mesh.name="Food crumb";
                var vertices=mesh.vertices;var bounds=mesh.bounds;float scale=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
                for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-bounds.center)/scale;
                mesh.vertices=vertices;mesh.RecalculateBounds();break;
            }
            var root=new GameObject("Falling food crumbs");root.transform.SetParent(transform,false);
            Particles=root.AddComponent<ParticleSystem>();Particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=Particles.main;main.loop=true;main.playOnAwake=false;main.maxParticles=Limit;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;main.gravityModifier=.35f;main.startRotation3D=true;
            var emission=Particles.emission;emission.enabled=false;var shape=Particles.shape;shape.enabled=false;
            var size=Particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,.3f));
            var colour=Particles.colorOverLifetime;colour.enabled=true;var fade=new Gradient();
            fade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,.65f),new GradientAlphaKey(0,1)});colour.color=fade;
            var rotation=Particles.rotationOverLifetime;rotation.enabled=true;rotation.separateAxes=true;
            rotation.x=new ParticleSystem.MinMaxCurve(-5,5);rotation.z=new ParticleSystem.MinMaxCurve(-7,7);
            material=new Material(Shader.Find("RivetReach/ArcadeChip"));material.SetFloat("_FirstPerson",1);
            var renderer=Particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
            renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=mesh;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            Particles.Play();
        }
        float Rand(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
        public void ResetBite(){lastBite=-1;previousTime=0;}
        public void Bite(byte item,float seconds,Vector3 food)
        {
            if(seconds<previousTime)lastBite=-1;previousTime=seconds;
            if(seconds<.22f)return;
            int bite=Mathf.FloorToInt((seconds-.22f)/.2f);if(bite==lastBite)return;lastBite=bite;
            float intensity=ArcadePresentation.Active!=null?ArcadePresentation.Active.Intensity:1;
            if(intensity<=0)return;
            var camera=player.Camera.transform;float scale=player.Arms.transform.localScale.x;
            var skin=player.Game.Registry.Get(item).colour;var flesh=Color.Lerp(skin,new Color(.96f,.83f,.56f),.7f);
            // Small bursts from the food's near face, with downward velocity and
            // world gravity. Released crumbs stay behind when the player turns.
            for(int i=0;i<Mathf.CeilToInt(5*intensity);i++)
            {
                var tint=i%3==0?skin:flesh;tint.a=intensity;
                Particles.Emit(new ParticleSystem.EmitParams{
                    position=food+camera.TransformVector(new Vector3(Rand(-.025f,.025f)*scale,-.018f*scale,-.075f)),
                    velocity=camera.TransformVector(new Vector3(Rand(-.12f,.12f)*scale,Rand(-.12f,-.035f)*scale,Rand(-.015f,.06f))),
                    startSize=Rand(.008f,.016f)*scale,startLifetime=Rand(.28f,.42f),startColor=tint,
                    rotation3D=new Vector3(Rand(0,360),Rand(0,360),Rand(0,360)),randomSeed=(uint)++Emitted},1);
            }
        }
        void LateUpdate()
        {
            if(player==null)return;
            if(player.Game.Mode!=ScreenMode.Play||player.Game.Creative||player.Inspecting||ArcadePresentation.Active!=null&&ArcadePresentation.Active.Intensity<=0)
            {Particles.Clear();ResetBite();}
        }
        void Shift(Vector3 offset)
        {
            if(Particles==null)return;int count=Particles.GetParticles(shifted);
            for(int i=0;i<count;i++)shifted[i].position-=offset;Particles.SetParticles(shifted,count);
        }
        void OnDisable(){if(Particles!=null)Particles.Clear();ResetBite();}
        void OnDestroy(){if(world!=null)world.OriginShifted-=Shift;if(material!=null)Destroy(material);if(mesh!=null)Destroy(mesh);}
    }
}
