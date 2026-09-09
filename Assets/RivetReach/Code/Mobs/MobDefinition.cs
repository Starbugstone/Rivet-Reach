using UnityEngine;

namespace RivetReach
{
    [CreateAssetMenu(menuName="Rivet Reach/Mob definition")]
    public sealed class MobDefinition : ScriptableObject
    {
        public string stableId, displayName, model;
        public bool nocturnal, territorial;
        [Min(1)] public int health=30, damage=8, population=6;
        [Min(.2f)] public float width=1.1f, height=1.6f, speed=3.2f;
        [Min(.1f)] public float strideLength=1.1f;
        [Min(1)] public float noticeRange=15, attackRange=2.1f, leashRange=26;
        [Min(.1f)] public float windup=.65f, recovery=1.1f;
        public void Validate()
        {
            if(string.IsNullOrWhiteSpace(stableId)||string.IsNullOrWhiteSpace(model)||health<1||damage<1||population<1||
               width<.2f||height<.2f||speed<=0||strideLength<.1f||noticeRange<attackRange||attackRange<1||leashRange<noticeRange||windup<.1f||recovery<.1f)
                throw new System.InvalidOperationException("Invalid mob definition: "+name);
        }
    }

    public enum MobIntent { Idle, Wander, Warning, Chase, Windup, Recovery, Return, Dead }

    // Session entity identity and position survive presentation changes and origin shifts.
    public sealed class MobState
    {
        public long Id;
        public MobDefinition Definition;
        public WorldPoint Position, Home;
        public int Health;
        public MobIntent Intent;
        public float Timer, Anger, Yaw, Vertical, PathAt, Unseen;
        public bool Grounded;
        public Vector3 Knockback;
        internal readonly System.Collections.Generic.List<BlockPos> Path=new System.Collections.Generic.List<BlockPos>();
        internal int PathIndex;
        internal MobView View;
        public bool Alive=>Health>0;
    }
}
