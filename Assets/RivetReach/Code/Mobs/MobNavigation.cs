using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    // A bounded local voxel search. Every edge checks today's terrain, including headroom,
    // one-block steps, safe drops and unknown chunk borders. No baked navigation mesh.
    public sealed class MobNavigation
    {
        struct Node { public BlockPos Cell; public int Parent; public float Cost, Score; public bool Closed; }
        readonly List<Node> nodes=new List<Node>(128);
        readonly Dictionary<BlockPos,int> indices=new Dictionary<BlockPos,int>(128);
        static readonly Vector3Int[] directions={Vector3Int.left,Vector3Int.right,Vector3Int.forward,Vector3Int.back};
        static readonly Vector3[] supportCorners={new Vector3(-1,0,-1),new Vector3(1,0,-1),new Vector3(-1,0,1),new Vector3(1,0,1)};
        public int LastExpanded {get;private set;}
        public static Vector3 Feet(VoxelWorld world,BlockPos cell)=>world.Local(cell)+new Vector3(.5f,.006f,.5f);
        public static bool Standable(VoxelWorld world,Vector3 feet,MobDefinition definition)
        {
            if(world.Overlaps(feet,definition.width,definition.height))return false;
            // Full support across the collider prevents teetering across narrow cave shafts.
            float r=definition.width*.5f-.015f;
            foreach(var corner in supportCorners)
            {
                var cell=world.Address(feet+corner*r-Vector3.up*.03f);
                if(!world.Ready(cell)||!BlockId.Solid(world.Get(cell)))return false;
            }
            return true;
        }
        static bool Edge(VoxelWorld world,BlockPos from,BlockPos to,MobDefinition definition)
        {
            Vector3 a=Feet(world,from),b=Feet(world,to);
            float top=Mathf.Max(a.y,b.y);
            if(world.Overlaps(new Vector3(a.x,top,a.z),definition.width,definition.height))return false;
            for(int i=1;i<=4;i++)
            {
                var sample=Vector3.Lerp(a,b,i*.25f);sample.y=top;
                if(world.Overlaps(sample,definition.width,definition.height))return false;
            }
            return true;
        }
        public void Find(VoxelWorld world,Vector3 start,Vector3 destination,MobDefinition definition,List<BlockPos> path)
        {
            path.Clear();nodes.Clear();indices.Clear();LastExpanded=0;
            BlockPos first=world.Address(start),goal=world.Address(destination);
            float Distance(BlockPos c)=>(float)(System.Math.Abs(c.X-goal.X)+System.Math.Abs(c.Z-goal.Z)) + System.Math.Abs(c.Y-goal.Y)*.5f;
            nodes.Add(new Node{Cell=first,Parent=-1,Score=Distance(first)});indices[first]=0;
            int closest=0;
            while(LastExpanded<96)
            {
                int best=-1;float score=float.MaxValue;
                for(int i=0;i<nodes.Count;i++)if(!nodes[i].Closed&&nodes[i].Score<score){score=nodes[i].Score;best=i;}
                if(best<0)break;
                var node=nodes[best];node.Closed=true;nodes[best]=node;LastExpanded++;
                if(Distance(node.Cell)<Distance(nodes[closest].Cell))closest=best;
                if(Distance(node.Cell)<1.5f){closest=best;break;}
                foreach(var direction in directions)
                {
                    for(int dy=0;dy<=2;dy++)
                    {
                        int rise=dy==0?0:dy==1?1:-1;
                        var cell=node.Cell.Offset(direction.x,rise,direction.z);
                        if(!Standable(world,Feet(world,cell),definition)||!Edge(world,node.Cell,cell,definition))continue;
                        float cost=node.Cost+1+Mathf.Abs(rise)*.4f;
                        if(indices.TryGetValue(cell,out int index))
                        {
                            var existing=nodes[index];
                            if(!existing.Closed&&cost<existing.Cost){existing.Cost=cost;existing.Parent=best;existing.Score=cost+Distance(cell);nodes[index]=existing;}
                        }
                        else if(nodes.Count<256)
                        {indices[cell]=nodes.Count;nodes.Add(new Node{Cell=cell,Parent=best,Cost=cost,Score=cost+Distance(cell)});}
                        break;
                    }
                }
            }
            for(int index=closest;index>0;index=nodes[index].Parent)path.Add(nodes[index].Cell);
            path.Reverse();
        }
    }
}
