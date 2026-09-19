using UnityEngine;

namespace RivetReach
{
    public readonly struct EntitySelectionHit
    {
        public readonly object Target;
        public readonly float Distance;
        public readonly Vector3 Point;
        public EntitySelectionHit(object target,float distance,Vector3 point){Target=target;Distance=distance;Point=point;}
    }
    public interface IInteractionTargetSource
    {
        bool TrySelect(Vector3 origin,Vector3 direction,float reach,out EntitySelectionHit hit);
        void Interact(object target,bool attack,bool usePressed,bool useHeld);
    }
    public enum InteractionTargetKind { None, Block, Entity }
    public readonly struct InteractionTargetHit
    {
        public readonly InteractionTargetKind Kind;
        public readonly BlockSelectionHit Block;
        public readonly EntitySelectionHit Entity;
        public readonly IInteractionTargetSource Source;
        public Vector3 Point=>Kind==InteractionTargetKind.Block?Block.Point:Entity.Point;
        public float Distance=>Kind==InteractionTargetKind.Block?Block.Distance:Entity.Distance;
        public InteractionTargetHit(BlockSelectionHit block){Kind=InteractionTargetKind.Block;Block=block;Entity=default;Source=null;}
        public InteractionTargetHit(EntitySelectionHit entity,IInteractionTargetSource source){Kind=InteractionTargetKind.Entity;Block=default;Entity=entity;Source=source;}
    }
    public sealed partial class Expedition
    {
        public IInteractionTargetSource PassiveTargets {get;set;}
        public bool SelectInteraction(Vector3 origin,Vector3 direction,float reach,out InteractionTargetHit hit,bool fluidSources=false)
        {
            hit=default;float nearest=reach;
            if(World.Select(origin,direction,reach,out var block,fluidSources))
            {hit=new InteractionTargetHit(block);nearest=block.Distance;}
            if(Mobs!=null&&Mobs.TrySelect(origin,direction,nearest,out var hostile)&&hostile.Distance<nearest)
            {hit=new InteractionTargetHit(hostile,Mobs);nearest=hostile.Distance;}
            if(PassiveTargets!=null&&PassiveTargets.TrySelect(origin,direction,nearest,out var passive)&&passive.Distance<nearest)
                hit=new InteractionTargetHit(passive,PassiveTargets);
            return hit.Kind!=InteractionTargetKind.None;
        }
    }
    public sealed partial class MobSystem : IInteractionTargetSource
    {
        public bool TrySelect(Vector3 origin,Vector3 direction,float reach,out EntitySelectionHit hit)
        {
            hit=default;var target=RayTarget(origin,direction,Mathf.Min(reach,3.2f));
            if(target==null)return false;
            var d=target.Definition;var ray=new Ray(origin,direction.normalized);
            var bounds=new Bounds(target.Position.Local(world.Origin)+Vector3.up*d.height*.5f,new Vector3(d.width,d.height,d.width));
            if(!bounds.IntersectRay(ray,out float distance))return false;
            hit=new EntitySelectionHit(target,distance,ray.GetPoint(distance));return true;
        }
        public void Interact(object target,bool attack,bool usePressed,bool useHeld)
        {
            Target=null;
            if(target is MobState mob&&mob.Alive&&Mobs.Contains(mob))HandlePlayerTarget(attack);
        }
    }
}
