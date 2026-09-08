using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    // Authored foley variations, bounded voices and listener-relative ambience.
    public sealed class WorldSound : MonoBehaviour
    {
        readonly Dictionary<string,AudioClip[]> banks=new Dictionary<string,AudioClip[]>();
        readonly Dictionary<string,int> previous=new Dictionary<string,int>();
        readonly System.Random variation=new System.Random();
        readonly AudioSource[] voices=new AudioSource[12];
        readonly float[] voiceGains=new float[12];
        AudioSource wind;AudioLowPassFilter windFilter;
        int voice;float lastPickup,nextEnvironment;bool paused;
        public int LoadedClipCount {get;private set;}
        public int VoiceLimit=>voices.Length;
        public float Master {get;private set;}
        public void SetMaster(float value)
        {
            Master=Mathf.Clamp01(value);PlayerPrefs.SetFloat("audio.master",Master);
            for(int i=0;i<voices.Length;i++)if(voices[i]!=null)voices[i].volume=voiceGains[i]*Master;
            if(wind!=null&&Master==0)wind.volume=0;
        }
        void Awake()
        {
            Master=Mathf.Clamp01(PlayerPrefs.GetFloat("audio.master",.8f));
            foreach(string material in new[]{"Grass","Soil","Stone","Wood","Leaves"})
                foreach(string action in new[]{"Step","Hit","Break"})Load(action+material,action=="Step"?5:action=="Hit"?4:3);
            foreach(string action in new[]{"Swing","Equip","Pickup","Land"})Load(action,4);
            for(int i=0;i<voices.Length;i++)
            {
                var o=new GameObject("Foley voice "+i);o.transform.SetParent(transform,false);
                voices[i]=o.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].rolloffMode=AudioRolloffMode.Logarithmic;
                voices[i].minDistance=1.5f;voices[i].maxDistance=24;voices[i].dopplerLevel=0;
            }
            var ambience=new GameObject("Wind through canopy");ambience.transform.SetParent(transform,false);
            wind=ambience.AddComponent<AudioSource>();wind.playOnAwake=false;wind.loop=true;wind.spatialBlend=0;wind.volume=0;
            wind.clip=Resources.Load<AudioClip>("Audio/WindCanopy");windFilter=ambience.AddComponent<AudioLowPassFilter>();
            if(wind.clip!=null){LoadedClipCount++;wind.Play();}else Debug.LogError("Missing ambience: WindCanopy");
        }
        void Load(string bank,int count)
        {
            var clips=new AudioClip[count];
            for(int i=0;i<count;i++){clips[i]=Resources.Load<AudioClip>("Audio/"+bank+i);if(clips[i]!=null)LoadedClipCount++;else Debug.LogError("Missing foley: "+bank+i);}
            banks[bank]=clips;
        }
        static string Surface(byte id)=>id==BlockId.Grass?"Grass":id==BlockId.Stone?"Stone":id==BlockId.Log?"Wood":id==BlockId.Leaves?"Leaves":"Soil";
        void Play(string bank,float gain,Vector3? position=null,float pitch=1)
        {
            var clips=banks[bank];int index=variation.Next(clips.Length-1);
            if(previous.TryGetValue(bank,out int last)&&index>=last)index++;
            else if(!previous.ContainsKey(bank))index=variation.Next(clips.Length);
            previous[bank]=index;if(clips[index]==null||Master<=0)return;
            // Reuse an idle source first; overload steals only the oldest pooled voice.
            int chosen=voice;
            for(int i=0;i<voices.Length;i++){int candidate=(voice+i)%voices.Length;if(!voices[candidate].isPlaying){chosen=candidate;break;}}
            var source=voices[chosen];voice=(chosen+1)%voices.Length;source.Stop();
            source.transform.position=position??Vector3.zero;source.spatialBlend=position.HasValue?1:0;
            voiceGains[chosen]=gain;source.volume=gain*Master;source.pitch=pitch*(.965f+(float)variation.NextDouble()*.07f);source.clip=clips[index];source.Play();
        }
        public void Step(byte surface=BlockId.Stone,float intensity=1)=>Play("Step"+Surface(surface),.43f*intensity);
        public void Swing()=>Play("Swing",.26f);
        public void Hit(byte surface,Vector3 position)=>Play("Hit"+Surface(surface),.38f,position);
        public void Mine(byte surface=BlockId.Stone,Vector3? position=null)=>Play("Break"+Surface(surface),.64f,position);
        public void Place(byte surface,Vector3 position)=>Play("Hit"+Surface(surface),.5f,position,.87f);
        public void Equip()=>Play("Equip",.24f);
        public void ShiftOrigin(Vector3 offset){foreach(var source in voices)if(source.spatialBlend>0)source.transform.position-=offset;}
        public void Land(float impact)=>Play("Land",Mathf.Lerp(.15f,.55f,Mathf.InverseLerp(2,12,impact)));
        public void Pickup(){if(Time.unscaledTime-lastPickup<.08f)return;lastPickup=Time.unscaledTime;Play("Pickup",.34f);}
        float shelter;
        void Update()
        {
            var game=Expedition.Instance;if(game==null||game.Player==null)return;
            bool suspend=game.Paused||!game.Started;
            if(suspend!=paused)
            {
                paused=suspend;
                foreach(var source in voices){if(paused)source.Pause();else source.UnPause();}
            }
            if(Time.unscaledTime>=nextEnvironment)
            {
                nextEnvironment=Time.unscaledTime+.4f;
                shelter=game.World.Raycast(game.Player.Camera.transform.position,Vector3.up,24,out _,out _)?1:0;
            }
            float target=suspend?0:Mathf.Lerp(.24f,.025f,shelter)*Master;
            wind.volume=Mathf.MoveTowards(wind.volume,target,Time.unscaledDeltaTime*.12f);
            windFilter.cutoffFrequency=Mathf.Lerp(windFilter.cutoffFrequency,Mathf.Lerp(13000,1400,shelter),1-Mathf.Exp(-Time.unscaledDeltaTime*2));
        }
    }
}
