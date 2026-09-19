using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    public static class ChickenId
    {
        public const byte Egg=248,Raw=249,Cooked=250,Feather=251,CookedEgg=252,Stew=253;
        public static bool Added(byte id)=>id>=Egg&&id<=Stew;
    }
    [Serializable] public sealed class ChickenCatalog
    {
        public int health=6,chickHealth=3,growthTicks=12000,breedCooldownTicks=6000,loveTicks=600,eggMinimumTicks=6000,eggMaximumTicks=12000;
        public float width=.62f,height=1.12f,chickWidth=.36f,chickHeight=.64f,speed=1.8f,followRange=10;
        public string[] feed={"#seed","#grain"};
        public MobSpawnRules spawnRules=new MobSpawnRules{habitat=MobHabitat.Surface,supportBlocks=new[]{"rivet:grass"},minimumLight=9,maximumLight=15};
        public static ChickenCatalog Load()
        {
            var asset=Resources.Load<TextAsset>("Definitions/Chickens");
            if(asset==null)throw new InvalidOperationException("Missing chicken catalog.");
            var result=JsonUtility.FromJson<ChickenCatalog>(asset.text);result.Validate(ItemRegistry.Load());return result;
        }
        public void Validate(ItemRegistry items)
        {
            foreach(float value in new[]{width,height,chickWidth,chickHeight,speed,followRange})if(float.IsNaN(value)||float.IsInfinity(value))throw new InvalidOperationException("Non-finite chicken dimensions or movement.");
            if(spawnRules==null)throw new InvalidOperationException("Missing chicken spawn profile.");
            if(health<1||chickHealth<1||chickHealth>health||growthTicks<20||breedCooldownTicks<20||loveTicks<20||eggMinimumTicks<20||eggMaximumTicks<eggMinimumTicks||eggMaximumTicks>120000||growthTicks>120000||breedCooldownTicks>120000||loveTicks>12000||width<.2f||width>1||height<.2f||height>2||chickWidth<.2f||chickWidth>width||chickHeight<.2f||chickHeight>height||speed<=0||speed>8||followRange<1||followRange>32||feed==null||feed.Length==0)throw new InvalidOperationException("Invalid chicken lifecycle definition.");
            foreach(var selector in feed)if(items.Select(selector).Choices.Count==0)throw new InvalidOperationException("Empty chicken feed selector.");
            spawnRules.Validate(items);
        }
    }
    // Persistent values only. Views, navigation paths and nearby-target caches are derived.
    public sealed class ChickenState
    {
        public long Id;
        public WorldPoint Position,Home;
        public int Health,GrowthTicks,EggTicks,CooldownTicks,LoveTicks;
        public uint RandomState;
        public float Yaw,Vertical;
        public bool Grounded;
        internal int PanicTicks,PeckTicks,ThinkTicks,PathIndex;
        internal Vector3 Flee;
        internal readonly List<BlockPos> Path=new List<BlockPos>();
        internal ChickenView View;
        public bool Adult=>GrowthTicks==0;
        public bool Alive=>Health>0;
        public bool ReadyToBreed=>Alive&&Adult&&CooldownTicks==0&&LoveTicks>0;
        public uint NextRandom(){uint x=RandomState;x^=x<<13;x^=x>>17;x^=x<<5;return RandomState=x==0?1:x;}
        public void ScheduleEgg(ChickenCatalog c)=>EggTicks=c.eggMinimumTicks+(int)(NextRandom()%(uint)(c.eggMaximumTicks-c.eggMinimumTicks+1));
        public bool Feed(ChickenCatalog c)
        {
            if(!Alive||!Adult||CooldownTicks>0||LoveTicks>0)return false;
            LoveTicks=c.loveTicks;PeckTicks=30;return true;
        }
        public void Advance(int ticks,bool adultFits,ChickenCatalog c)
        {
            if(ticks<0)throw new ArgumentOutOfRangeException(nameof(ticks));if(!Alive)return;
            LoveTicks=Math.Max(0,LoveTicks-ticks);CooldownTicks=Math.Max(0,CooldownTicks-ticks);
            if(!Adult)
            {
                int before=GrowthTicks;GrowthTicks=Math.Max(0,GrowthTicks-ticks);
                if(GrowthTicks==0&&!adultFits){GrowthTicks=1;return;}
                if(GrowthTicks==0){Health=c.health;EggTicks=Math.Max(0,EggTicks-Math.Max(0,ticks-before));}
            }
            else EggTicks=Math.Max(0,EggTicks-ticks);
        }
        public void Bred(ChickenCatalog c){LoveTicks=0;CooldownTicks=c.breedCooldownTicks;}
    }
}
