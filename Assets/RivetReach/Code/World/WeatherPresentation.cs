using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    // One bounded listener-local rain mesh and two audio sources. No particle objects,
    // physics collisions, terrain generation, gameplay lighting or weather damage.
    public sealed class WeatherPresentation : MonoBehaviour
    {
        public const int Diameter=22,MaximumStreaks=1024;
        // Stratified cylindrical sampling avoids the sparse screen edges of a rotated square.
        readonly Vector2[] offsets=new Vector2[MaximumStreaks];
        readonly float[] phases=new float[MaximumStreaks],densities=new float[MaximumStreaks];
        readonly int[] columns=new int[MaximumStreaks];
        readonly Vector3[] vertices=new Vector3[MaximumStreaks*4];
        readonly Color[] colours=new Color[MaximumStreaks*4];
        readonly float[] floors=new float[Diameter*Diameter];
        readonly bool[] known=new bool[Diameter*Diameter];
        Expedition game;VoxelWorld world;Mesh mesh;Material material;MeshRenderer rainRenderer;
        AudioSource rain,thunder;AudioLowPassFilter rainFilter;AudioClip rainClip,thunderClip;
        float time,nextColumns,nextThunder=18,thunderDelay=-1,flash,exposure;
        BlockPos anchor;bool anchored,suspended,columnsDirty=true,meshShown;
        int columnRevision,shownColumnRevision;
        float shownTime,shownStrength,shownDaylight;
        Vector3 shownEye,shownRight;BlockPos shownOrigin;
        static readonly int FlashProperty=Shader.PropertyToID("_RRWeatherFlash");
        public long TotalRoofQueries {get;private set;}
        public int MeshUploads {get;private set;}
        public float Flash=>flash;
        public float Exposure=>exposure;
        public int VisibleStreaks {get;private set;}
        public double LastMeshMilliseconds {get;private set;}
        public int RoofQueries {get;private set;}
        public int KnownColumns {get;private set;}
        internal void CountScreenCoverage(int[] bins)
        {
            var camera=game.Player.Camera;
            for(int i=0;i<MaximumStreaks;i++)
            {
                if(colours[i*4].a<=.001f)continue;
                var p=camera.WorldToViewportPoint(vertices[i*4]);
                if(p.z>0&&p.x>=0&&p.x<1&&p.y>=0&&p.y<1)bins[Mathf.Min(bins.Length-1,(int)(p.x*bins.Length))]++;
            }
        }
        public bool AudioReady=>rainClip!=null&&thunderClip!=null;
        public float RainVolume=>rain.volume;
        public int ThunderEvents {get;private set;}
        public void Initialize(Expedition owner)
        {
            game=owner;world=owner.World;
            world.BlockChanged+=Changed;world.ChunkResidencyChanged+=ResidencyChanged;world.OriginShifted+=Shifted;
            for(int i=0;i<MaximumStreaks;i++)
            {
                int stratum=i/4,ring=stratum/16,sector=stratum%16;
                float radius=Mathf.Sqrt((ring+Sample(stratum,1))/16)*9.5f;
                float angle=(sector+Sample(i,2))*Mathf.PI/8;
                Vector2 raw=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius+Vector2.one*.5f;
                int x=Mathf.FloorToInt(raw.x),z=Mathf.FloorToInt(raw.y);
                offsets[i]=new Vector2(x+Mathf.Clamp(raw.x-x,.15f,.85f),z+Mathf.Clamp(raw.y-z,.15f,.85f));
                columns[i]=x+Diameter/2+(z+Diameter/2)*Diameter;
                phases[i]=(i%4+Sample(stratum,3))/4;densities[i]=Sample(i,4);
            }
            mesh=new Mesh{name="Local rain streaks"};mesh.MarkDynamic();
            int[] triangles=new int[MaximumStreaks*6];
            for(int i=0;i<MaximumStreaks;i++){int v=i*4,t=i*6;triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3;}
            var uv=new Vector2[MaximumStreaks*4];for(int i=0;i<MaximumStreaks;i++){uv[i*4]=new Vector2(0,0);uv[i*4+1]=new Vector2(1,0);uv[i*4+2]=new Vector2(1,1);uv[i*4+3]=new Vector2(0,1);}
            mesh.vertices=vertices;mesh.colors=colours;mesh.uv=uv;mesh.triangles=triangles;mesh.bounds=new Bounds(Vector3.zero,Vector3.one*64);
            var child=new GameObject("Local rain");child.transform.SetParent(transform,false);
            child.AddComponent<MeshFilter>().sharedMesh=mesh;rainRenderer=child.AddComponent<MeshRenderer>();
            material=new Material(Resources.Load<Shader>("Materials/WeatherRain"));rainRenderer.sharedMaterial=material;
            rainRenderer.shadowCastingMode=ShadowCastingMode.Off;rainRenderer.receiveShadows=false;rainRenderer.enabled=false;
            rain=AudioSource("Rain",true);thunder=AudioSource("Distant thunder",false);
            rainFilter=rain.gameObject.AddComponent<AudioLowPassFilter>();
            rainClip=MakeAudio(false);thunderClip=MakeAudio(true);rain.clip=rainClip;thunder.clip=thunderClip;rain.Play();
        }
        static float Sample(int index,uint salt)
        {
            uint value=(uint)index*747796405u+salt*2891336453u;
            value=((value>>((int)(value>>28)+4))^value)*277803737u;value=(value>>22)^value;
            return (value&0xffffff)/16777216f;
        }
        AudioSource AudioSource(string label,bool loop)
        {
            var child=new GameObject(label);child.transform.SetParent(transform,false);var source=child.AddComponent<AudioSource>();
            source.playOnAwake=false;source.loop=loop;source.spatialBlend=0;source.dopplerLevel=0;source.volume=0;return source;
        }
        // Original deterministic synthesis; no third-party recordings or runtime file I/O.
        static AudioClip MakeAudio(bool storm)
        {
            const int rate=22050,seconds=8;var samples=new float[rate*seconds];uint random=storm?971u:139u;float low=0,slow=0;
            for(int i=0;i<samples.Length;i++)
            {
                random^=random<<13;random^=random>>17;random^=random<<5;float noise=(random/4294967295f)*2-1;
                low+=.035f*(noise-low);slow+=.002f*(noise-slow);float t=i/(float)rate;
                float envelope=storm?Mathf.Min(1,t*5)*Mathf.Exp(-t*.55f)*(1+.25f*Mathf.Sin(t*5)) : .82f+.12f*Mathf.Sin(t*Mathf.PI/4);
                samples[i]=storm?Mathf.Clamp((low*3+slow*6)*envelope,-1,1):(noise*.30f+low*.5f)*envelope;
            }
            // Rain loop joins smoothly; thunder fades fully at its tail.
            for(int i=0;i<256;i++){float gain=i/256f;samples[i]*=gain;samples[samples.Length-1-i]*=gain;}
            var clip=AudioClip.Create(storm?"Original rolling thunder":"Original rainfall",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void LateUpdate()
        {
            using var cost=RuntimeCosts.Weather.Auto();
            if(game==null||game.World.gameObject!=gameObject)return;
            bool pause=game.Paused||!game.Started;
            if(pause!=suspended){suspended=pause;if(pause){rain.Pause();thunder.Pause();}else{rain.UnPause();thunder.UnPause();}}
            float dt=pause?0:Time.deltaTime;time+=dt;
            var eye=game.Player.Camera.transform.position;var cell=world.Address(eye);
            float strength=game.Weather.RainStrength;
            bool needsShelter=game.Started&&(strength>.001f||rain.volume>.001f||thunder.isPlaying||thunderDelay>=0);
            bool moved=!anchored||!cell.Equals(anchor);
            bool distant=!anchored||Math.Abs(cell.X-anchor.X)>8||Math.Abs(cell.Y-anchor.Y)>8||Math.Abs(cell.Z-anchor.Z)>8;
            if(needsShelter&&(distant||time>=nextColumns&&(moved||columnsDirty||KnownColumns<known.Length)))
            {anchor=cell;anchored=true;nextColumns=time+.25f;RefreshColumns();}
            if(!needsShelter)columnsDirty=true;
            float target=game.Started?strength*Mathf.Lerp(.045f,.48f,exposure)*game.Sound.Master:0;
            if(!pause)rain.volume=Mathf.MoveTowards(rain.volume,target,dt*.5f);
            if(game.Sound.Master==0)rain.volume=0;
            rainFilter.cutoffFrequency=Mathf.Lerp(1000,15000,exposure);
            if(!pause||game.Sound.Master==0)thunder.volume=game.Sound.Master*Mathf.Lerp(.16f,.65f,exposure);
            flash=Mathf.Max(0,flash-dt*1.7f);
            if(dt>0&&game.Weather.Kind==WeatherKind.Storm&&strength>.8f)
            {
                nextThunder-=dt;
                if(nextThunder<=0){flash=.6f;thunderDelay=1.8f;nextThunder=23+Mathf.Repeat(time*7,19);}
            }
            if(thunderDelay>=0&&dt>0){thunderDelay-=dt;if(thunderDelay<0){thunder.Play();ThunderEvents++;}}
            Shader.SetGlobalFloat(FlashProperty,flash);
            rainRenderer.enabled=game.Started&&strength>.001f;
            if(rainRenderer.enabled)
            {
                var right=game.Player.Camera.transform.right;
                if(!meshShown||shownTime!=time||shownStrength!=strength||shownDaylight!=game.Sky.Daylight||shownEye!=eye||shownRight!=right||shownColumnRevision!=columnRevision||!shownOrigin.Equals(world.Origin))
                {
                    RenderRain(eye,strength);meshShown=true;shownTime=time;shownStrength=strength;shownDaylight=game.Sky.Daylight;
                    shownEye=eye;shownRight=right;shownColumnRevision=columnRevision;shownOrigin=world.Origin;
                }
                else LastMeshMilliseconds=0;
            }
            else{VisibleStreaks=0;meshShown=false;LastMeshMilliseconds=0;}
        }
        void Changed(BlockPos position)
        {
            // Roofs may be arbitrarily far above the listener. Horizontal bounds
            // cover every sampled column; liquid edits share this invalidation.
            if(!anchored||Math.Abs(position.X-anchor.X)<=Diameter/2&&Math.Abs(position.Z-anchor.Z)<=Diameter/2)columnsDirty=true;
        }
        void ResidencyChanged(ChunkPos chunk)
        {
            if(!anchored){columnsDirty=true;return;}
            var min=anchor.Offset(-Diameter/2,0,-Diameter/2).Chunk;var max=anchor.Offset(Diameter/2,0,Diameter/2).Chunk;
            if(chunk.X>=min.X&&chunk.X<=max.X&&chunk.Z>=min.Z&&chunk.Z<=max.Z)columnsDirty=true;
        }
        void Shifted(Vector3 delta){columnsDirty=true;meshShown=false;}
        void RefreshColumns()
        {
            columnsDirty=false;columnRevision++;RoofQueries=0;KnownColumns=0;
            for(int z=0;z<Diameter;z++)for(int x=0;x<Diameter;x++)
            {
                int i=x+z*Diameter;var cell=anchor.Offset(x-Diameter/2,0,z-Diameter/2);RoofQueries++;TotalRoofQueries++;
                known[i]=world.TryPrecipitationHeight(cell,out int height);floors[i]=height+1;if(known[i])KnownColumns++;
                // Clip rain against visible source/flowing liquids as well as solid roofs.
                if(known[i])for(int y=anchor.Y+8;y>Math.Max(height,anchor.Y-10);y--)
                {var p=new BlockPos(cell.X,y,cell.Z);if(world.TryRead(p,out byte id)&&Fluids.IsFluid(id)){floors[i]=y+1;break;}}
            }
            int centre=Diameter/2+Diameter/2*Diameter;
            exposure=known[centre]&&anchor.Y>=floors[centre]?1:0;
        }
        void RenderRain(Vector3 eye,float strength)
        {
            long began=System.Diagnostics.Stopwatch.GetTimestamp();VisibleStreaks=0;
            var world=game.World;var origin=world.Local(anchor);var right=game.Player.Camera.transform.right*.010f;
            float speed=Mathf.Lerp(10,17,strength),length=Mathf.Lerp(.32f,.65f,strength);
            Color colour=Color.Lerp(new Color(.28f,.38f,.53f,.22f),new Color(.65f,.78f,.89f,.38f),game.Sky.Daylight);
            for(int i=0;i<MaximumStreaks;i++)
            {
                int column=columns[i],v=i*4;
                float y=eye.y+8-Mathf.Repeat(time*speed+phases[i]*18,18);
                bool visible=known[column]&&y-length>=floors[column]&&densities[i]<strength;
                Vector3 bottom=origin+new Vector3(offsets[i].x,0,offsets[i].y);bottom.y=y;
                Vector3 top=bottom+new Vector3(-.12f*strength,length,.025f);
                vertices[v]=bottom-right;vertices[v+1]=bottom+right;vertices[v+2]=top+right;vertices[v+3]=top-right;
                var tint=visible?colour:Color.clear;tint.a*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1,3,Vector3.Distance(bottom,eye)));for(int k=0;k<4;k++)colours[v+k]=tint;
                if(visible)VisibleStreaks++;
            }
            mesh.vertices=vertices;mesh.colors=colours;mesh.bounds=new Bounds(eye,Vector3.one*48);MeshUploads++;
            LastMeshMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-began)*1000.0/System.Diagnostics.Stopwatch.Frequency;
        }
        void OnDisable(){if(rain!=null)rain.Pause();if(thunder!=null)thunder.Pause();suspended=true;}
        void OnDestroy(){if(world!=null){world.BlockChanged-=Changed;world.ChunkResidencyChanged-=ResidencyChanged;world.OriginShifted-=Shifted;}if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);if(rainClip!=null)Destroy(rainClip);if(thunderClip!=null)Destroy(thunderClip);}
    }
}
