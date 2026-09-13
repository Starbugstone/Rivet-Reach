using UnityEngine;

namespace RivetReach
{
    public sealed partial class MobSystem
    {
        // Low flight follows loaded surfaces through the same bounded voxel path graph.
        // The body position itself hovers, so hits, placement and saves agree with the art.
        Vector3 HoverMove(MobState mob,Vector3 local,float dt)
        {
            var d=mob.Definition;mob.Climbing=false;mob.WallNormal=Vector3.zero;
            Vector3 horizontal=Vector3.zero;float desired=float.NegativeInfinity;
            float radius=d.width*.5f-.015f;
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            {
                var probe=local+new Vector3(x*radius,.02f,z*radius);
                if(world.Raycast(probe,Vector3.down,3,out var ground,out _)&&world.Ready(ground)&&world.Solid(ground))
                    desired=Mathf.Max(desired,world.Local(ground).y+1.006f+d.hoverHeight);
            }
            if((mob.Intent==MobIntent.Chase||mob.Intent==MobIntent.Wander||mob.Intent==MobIntent.Return)&&mob.PathIndex<mob.Path.Count)
            {
                Vector3 next=MobNavigation.Feet(world,mob.Path[mob.PathIndex],d),toward=next-local;
                if(!MobNavigation.Standable(world,next,d))
                {mob.Path.Clear();mob.PathAt=elapsed;}
                else if(toward.magnitude<.14f)mob.PathIndex++;
                else
                {
                    Face(mob,toward);
                    // Rise before crossing a step; World.Move checks the full swept body.
                    if(next.y>local.y+.08f)desired=Mathf.Max(desired,next.y);
                    else
                    {
                        toward.y=0;
                        horizontal=toward.normalized*Mathf.Min(d.speed*(mob.Intent==MobIntent.Chase?1:.45f)*dt,toward.magnitude);
                    }
                }
            }
            if(float.IsNegativeInfinity(desired))mob.Vertical=Mathf.Max(-25,mob.Vertical-20*dt);
            else mob.Vertical=Mathf.Clamp((desired-local.y)/dt,-3,3);
            return horizontal+Vector3.up*mob.Vertical*dt;
        }
    }
}
