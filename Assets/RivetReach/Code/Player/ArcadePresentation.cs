using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RivetReach
{
    // Cosmetic consumers of committed world events. Particles never own blocks, items or collision.
    [DefaultExecutionOrder(100)]
    public sealed class ArcadePresentation : MonoBehaviour
    {
        public static ArcadePresentation Active {get;private set;}
        public const int ParticleLimit=728;
        public float Intensity {get;private set;}
        public int BurstCount {get;private set;}
        public int ParticleCount {get {int count=0;foreach(var system in systems)count+=system.particleCount;return count;}}
        public bool CracksVisible=>cracks!=null&&cracks.activeSelf;
        public float PresentationTime=>clock;
        Expedition game;VoxelWorld world;
        ParticleSystem chips,wood,leaves,dust,sparks,rings;
        readonly List<ParticleSystem> systems=new List<ParticleSystem>();
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly ParticleSystem.Particle[] shifted=new ParticleSystem.Particle[256];
        readonly System.Random random=new System.Random(9471);
        GameObject cracks;Material crackMaterial;
        float clock,lastPhase,pulse,nextPollen,lastY,lastFall;bool struck,wasGrounded;
        int burstFrame=-1,frameBursts;Vector3 flashPosition;
        Color flashColour;
        void Awake()
        {
            Active=this;game=GetComponentInParent<Expedition>();Intensity=Mathf.Clamp01(PlayerPrefs.GetFloat("visual.effects",1));
            chips=Pool("Block chips",192,MeshMaterial(),FindMesh("StoneChip"));
            wood=Pool("Wood splinters",96,MeshMaterial(),FindMesh("WoodSliver"));
            leaves=Pool("Leaf fragments",96,MeshMaterial(),FindMesh("LeafChip"));
            dust=Pool("Soft dust and pollen",192,SpriteMaterial(0));
            sparks=Pool("Contact and pickup glints",128,SpriteMaterial(2));
            rings=Pool("Action rings",24,SpriteMaterial(1));
            var ringSize=rings.sizeOverLifetime;ringSize.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.3f,1,1.3f));
            var dustSize=dust.sizeOverLifetime;dustSize.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.5f,1,1.5f));
            cracks=GameObject.CreatePrimitive(PrimitiveType.Cube);cracks.name="Mining surface feedback";Destroy(cracks.GetComponent<Collider>());
            cracks.transform.SetParent(transform,false);cracks.transform.localScale=Vector3.one*1.003f;
            crackMaterial=new Material(Shader.Find("RivetReach/ArcadeCracks"));materials.Add(crackMaterial);
            var renderer=cracks.GetComponent<MeshRenderer>();renderer.sharedMaterial=crackMaterial;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;cracks.SetActive(false);
            Bind();
            gameObject.AddComponent<ArcadeGrass>();
        }
        Mesh FindMesh(string name)
        {
            var source=Resources.Load<GameObject>("Effects/ArcadeChips");
            if(source!=null)foreach(var filter in source.GetComponentsInChildren<MeshFilter>())if(filter.name==name)
            {
                var mesh=Instantiate(filter.sharedMesh);mesh.name=name+" particles";
                var vertices=mesh.vertices;var bounds=mesh.bounds;float scale=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
                for(int i=0;i<vertices.Length;i++)vertices[i]=(vertices[i]-bounds.center)/scale;
                mesh.vertices=vertices;mesh.RecalculateBounds();meshes.Add(mesh);return mesh;
            }
            Debug.LogError("Missing Blender effect mesh: "+name);return null;
        }
        Material MeshMaterial(){var m=new Material(Shader.Find("RivetReach/ArcadeChip"));materials.Add(m);return m;}
        Material SpriteMaterial(int mode,bool firstPerson=false)
        {var m=new Material(Shader.Find("RivetReach/ArcadeParticle"));m.SetFloat("_Mode",mode);m.SetFloat("_FirstPerson",firstPerson?1:0);materials.Add(m);return m;}
        ParticleSystem Pool(string name,int limit,Material material,Mesh mesh=null)
        {
            var o=new GameObject(name);o.transform.SetParent(transform,false);var system=o.AddComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=system.main;main.loop=true;main.playOnAwake=false;main.maxParticles=limit;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startLifetime=.5f;main.startSpeed=0;main.startSize=.1f;main.gravityModifier=mesh!=null?1.0f:.05f;main.startRotation3D=mesh!=null;
            var emission=system.emission;emission.enabled=false;var shape=system.shape;shape.enabled=false;
            var size=system.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,1,1,0));
            var colour=system.colorOverLifetime;colour.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.8f,.45f),new GradientAlphaKey(0,1)});colour.color=gradient;
            var rotation=system.rotationOverLifetime;rotation.enabled=mesh!=null;rotation.separateAxes=true;
            rotation.x=new ParticleSystem.MinMaxCurve(-4,4);rotation.z=new ParticleSystem.MinMaxCurve(-6,6);
            var renderer=system.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            if(mesh!=null){renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=mesh;}
            renderer.sortMode=ParticleSystemSortMode.Distance;system.Play();systems.Add(system);return system;
        }
        void Bind()
        {
            if(world!=null){world.BlockMined-=Break;world.OriginShifted-=Shift;}
            world=game.World;world.BlockMined+=Break;world.OriginShifted+=Shift;
            foreach(var system in systems)system.Clear();cracks.SetActive(false);pulse=0;lastPhase=0;struck=false;
            lastY=game.Player.transform.position.y;wasGrounded=game.Player.Grounded;
            game.Player.Camera.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }
        public void SetIntensity(float value)
        {
            Intensity=Mathf.Clamp01(value);PlayerPrefs.SetFloat("visual.effects",Intensity);
            if(Intensity==0){foreach(var system in systems)system.Clear();pulse=0;}
        }
        float Rand(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
        Vector3 Spread()=>new Vector3(Rand(-1,1),Rand(-.4f,1),Rand(-1,1)).normalized;
        Color Tint(byte id)=>id==BlockId.Grass?new Color(.47f,.65f,.20f):id==BlockId.Dirt?new Color(.58f,.36f,.19f):id==BlockId.Stone?new Color(.55f,.66f,.75f):id==BlockId.Log?new Color(.64f,.40f,.19f):id==BlockId.Leaves?new Color(.37f,.67f,.24f):game.Registry.Get(id).colour;
        bool Allow(Vector3 at)
        {
            if(!game.Started||game.Paused||Intensity<=0||(at-game.Player.transform.position).sqrMagnitude>24*24)return false;
            if(burstFrame!=Time.frameCount){burstFrame=Time.frameCount;frameBursts=0;}
            if(frameBursts++>=8)return false;BurstCount++;return true;
        }
        void Emit(ParticleSystem system,Vector3 at,Vector3 velocity,Color colour,float size,float life)
        {
            colour.a*=Intensity;
            var emit=new ParticleSystem.EmitParams{position=at,velocity=velocity,startColor=colour,startSize=size,startLifetime=life};
            if(system==chips||system==wood||system==leaves)emit.rotation3D=new Vector3(Rand(0,360),Rand(0,360),Rand(0,360));
            else emit.rotation=Rand(0,360);
            system.Emit(emit,1);
        }
        public void Impact(byte id,Vector3 at,Vector3 normal)
        {
            if(!Allow(at))return;var colour=Tint(id);pulse=1;flashPosition=at;flashColour=Color.Lerp(colour,new Color(1,.72f,.29f),.6f);
            int count=Mathf.CeilToInt(9*Intensity);
            for(int i=0;i<count;i++)Emit(id==BlockId.Leaves?leaves:id==BlockId.Log?wood:chips,at+normal*.035f,normal*Rand(.6f,2.2f)+Spread()*1.1f,colour,Rand(.04f,.085f),Rand(.18f,.40f));
            for(int i=0;i<3*Intensity;i++)Emit(dust,at+normal*.04f,normal*.3f+Spread()*.3f,new Color(colour.r,colour.g,colour.b,.28f),Rand(.22f,.42f),.34f);
            if(id==BlockId.Stone||id>BlockId.Leaves)for(int i=0;i<3*Intensity;i++)Emit(sparks,at+normal*.07f,normal*1.2f+Spread()*2,new Color(2.2f,1.3f,.40f,.9f),.095f,.16f);
        }
        void Break(BlockPos cell,byte id)
        {
            Vector3 at=world.Local(cell)+Vector3.one*.5f;if(!Allow(at))return;var colour=Tint(id);
            for(int i=0;i<28*Intensity;i++)Emit(id==BlockId.Leaves?leaves:id==BlockId.Log?wood:chips,at+Spread()*.3f,Spread()*Rand(1,3.1f)+Vector3.up*1.3f,colour,Rand(.045f,.14f),Rand(.35f,.68f));
            for(int i=0;i<7*Intensity;i++)Emit(dust,at+Spread()*.26f,Spread()*.55f,new Color(colour.r,colour.g,colour.b,.24f),Rand(.35f,.65f),.5f);
            Emit(rings,at,Vector3.zero,new Color(1.7f,1.05f,.35f,.38f),1.30f,.22f);pulse=1;flashPosition=at;flashColour=new Color(1,.7f,.28f);
        }
        public void Place(Vector3 centre,byte id)
        {
            if(!Allow(centre))return;
            Vector3 facing=(game.Player.Camera.transform.position-centre).normalized;
            Vector3 front=centre+facing*(.66f/Mathf.Max(.01f,Mathf.Abs(facing.x),Mathf.Abs(facing.y),Mathf.Abs(facing.z)));
            Emit(rings,front,Vector3.zero,new Color(.55f,1.7f,1.8f,.4f),1.35f,.30f);
            for(int i=0;i<12*Intensity;i++)Emit(sparks,centre+Spread()*.55f,Vector3.up*.65f+Spread()*.4f,new Color(.65f,1.6f,1.75f,.75f),.08f,.32f);
        }
        public void Pickup(Vector3 at,byte id)
        {
            if(!Allow(at))return;
            Vector3 toPlayer=(game.Player.Camera.transform.position-at).normalized;
            for(int i=0;i<8*Intensity;i++)Emit(sparks,at+Spread()*.12f,toPlayer*2+Spread()*.45f,new Color(1.8f,1.5f,.60f,.8f),Rand(.06f,.13f),.32f);
        }
        void Shift(Vector3 offset)
        {
            foreach(var system in systems){int count=system.GetParticles(shifted);for(int i=0;i<count;i++)shifted[i].position-=offset;system.SetParticles(shifted,count);}
            flashPosition-=offset;lastY-=offset.y;
        }
        void LateUpdate()
        {
            if(game==null||game.Player==null)return;if(world!=game.World)Bind();
            float dt=game.Paused?0:Time.deltaTime;clock+=dt;Shader.SetGlobalFloat("_RRPresentationTime",clock);
            foreach(var system in systems)
            {if(game.Paused){if(!system.isPaused)system.Pause();}else if(system.isPaused)system.Play();}
            var p=game.Player;bool playing=game.Started&&!game.Paused&&!game.InventoryOpen&&!p.Inspecting;
            pulse=Mathf.MoveTowards(pulse,0,dt*8);
            Shader.SetGlobalVector("_RRImpactLight",new Vector4(flashPosition.x,flashPosition.y,flashPosition.z,pulse*Intensity));Shader.SetGlobalColor("_RRImpactColour",flashColour);
            bool swing=playing&&p.Arms.MiningWeight>.15f;
            float phase=p.Arms.SwingPhase;if(phase<lastPhase)struck=false;
            if(swing&&!struck&&phase>=.43f)
            {
                struck=true;
                if(p.HasTarget&&world.Raycast(p.Camera.transform.position,p.Camera.transform.forward,5,out var cell,out byte id,out var face))
                {
                    Vector3 normal=(Vector3)face,centre=world.Local(cell)+Vector3.one*.5f;
                    float denominator=Vector3.Dot(p.Camera.transform.forward,normal);
                    if(Mathf.Abs(denominator)>.0001f){float distance=Vector3.Dot(centre+normal*.502f-p.Camera.transform.position,normal)/denominator;Impact(id,p.Camera.transform.position+p.Camera.transform.forward*distance,normal);}
                }
            }
            if(!swing)struck=false;lastPhase=phase;
            cracks.SetActive(playing&&p.HasTarget&&(p.MiningProgress>.015f||pulse>.1f));
            if(cracks.activeSelf){cracks.transform.position=world.Local(p.Target)+Vector3.one*.5f;crackMaterial.SetFloat("_Progress",p.MiningProgress);crackMaterial.SetFloat("_Pulse",pulse*Intensity);}
            if(playing&&!wasGrounded&&p.Grounded&&lastFall<-3&&Allow(p.transform.position))
                for(int i=0;i<8*Intensity;i++)Emit(dust,p.transform.position+Spread()*.2f,Spread()*.8f,new Color(.70f,.65f,.48f,.23f),.30f,.35f);
            if(Time.deltaTime>0)lastFall=(p.transform.position.y-lastY)/Time.deltaTime;lastY=p.transform.position.y;wasGrounded=p.Grounded;
            if(playing&&Intensity>0&&clock>nextPollen)
            {
                nextPollen=clock+.35f;
                if(!world.Raycast(p.Camera.transform.position,Vector3.up,20,out _,out _))
                {Vector3 at=p.Camera.transform.position+p.Camera.transform.forward*Rand(2,7)+new Vector3(Rand(-4,4),Rand(-1,3),Rand(-4,4));Emit(dust,at,new Vector3(.18f,.06f,.09f),new Color(1.15f,1.1f,.65f,.3f),Rand(.015f,.035f),Rand(2,4));}
            }
        }
        void OnDestroy()
        {
            if(world!=null){world.BlockMined-=Break;world.OriginShifted-=Shift;}
            foreach(var material in materials)Destroy(material);foreach(var mesh in meshes)Destroy(mesh);
            if(Active==this)Active=null;Shader.SetGlobalVector("_RRImpactLight",Vector4.zero);
        }
    }
}
