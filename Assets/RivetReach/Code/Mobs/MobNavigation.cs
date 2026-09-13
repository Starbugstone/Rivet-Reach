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
        public static Vector3 Feet(VoxelWorld world,BlockPos cell,MobDefinition definition=null)=>world.Local(cell)+new Vector3(.5f,.006f+(definition==null?0:definition.hoverHeight),.5f);
        public static bool Standable(VoxelWorld world,Vector3 feet,MobDefinition definition)
        {
            if(world.Overlaps(feet,definition.width,definition.height))return false;
            // Full support across the collider prevents teetering across narrow cave shafts.
            float r=definition.width*.5f-.015f;
            foreach(var corner in supportCorners)
            {
                var cell=world.Address(feet+corner*r-Vector3.up*(definition.hoverHeight+.03f));
                if(!world.Ready(cell)||!world.Solid(cell))return false;
                if(definition.hoverHeight>0&&Mathf.Abs(feet.y-(world.Local(cell).y+1+definition.hoverHeight))>.04f)return false;
            }
            return true;
        }
        public static Vector3 WallNormal(VoxelWorld world,Vector3 feet,MobDefinition definition,Vector3 preferred=default,bool lip=false)
        {
            if(!definition.climbsWalls)return Vector3.zero;
            bool Contact(Vector3 normal)
            {
                Vector3 tangent=Vector3.Cross(normal,Vector3.up)*definition.width*.32f;
                // Two grip points need real, loaded solid voxels. A lip probe is used only
                // during a checked transition onto the supporting top of this same wall.
                Vector3 probe=feet-normal*(definition.width*.5f+.10f)+Vector3.up*(lip?-.08f:.08f);
                for(int side=-1;side<=1;side+=2)
                {
                    var cell=world.Address(probe+tangent*side);
                    if(!world.Ready(cell)||!world.Solid(cell))return false;
                }
                return true;
            }
            if(preferred.sqrMagnitude>.5f&&Contact(preferred))return preferred;
            if(lip)return Vector3.zero;
            foreach(var direction in directions)if(Contact(direction))return direction;
            return Vector3.zero;
        }
        static bool Supported(VoxelWorld world,BlockPos cell,MobDefinition definition)
        {
            var feet=Feet(world,cell,definition);
            return Standable(world,feet,definition)||definition.climbsWalls&&
                !world.Overlaps(feet,definition.width,definition.height)&&WallNormal(world,feet,definition)!=Vector3.zero;
        }
        static bool Edge(VoxelWorld world,BlockPos from,BlockPos to,MobDefinition definition)
        {
            Vector3 a=Feet(world,from,definition),b=Feet(world,to,definition);
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
            // Voxel collision can rest feet a fraction below the integer top face. Address
            // the free cell above that tolerance, otherwise a return route starts in stone.
            BlockPos first=world.Address(start+Vector3.up*(.01f-definition.hoverHeight)),goal=world.Address(destination+Vector3.up*.01f);
            float Distance(BlockPos c)=>(float)(System.Math.Abs(c.X-goal.X)+System.Math.Abs(c.Z-goal.Z)) + System.Math.Abs(c.Y-goal.Y);
            void Add(BlockPos cell,int parent,float cost)
            {
                if(indices.TryGetValue(cell,out int index))
                {
                    var existing=nodes[index];
                    if(!existing.Closed&&cost<existing.Cost){existing.Cost=cost;existing.Parent=parent;existing.Score=cost+Distance(cell);nodes[index]=existing;}
                }
                else if(nodes.Count<256)
                {indices[cell]=nodes.Count;nodes.Add(new Node{Cell=cell,Parent=parent,Cost=cost,Score=cost+Distance(cell)});}
            }
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
                        if(!Supported(world,cell,definition)||!Edge(world,node.Cell,cell,definition))continue;
                        Add(cell,best,node.Cost+1+Mathf.Abs(rise)*.4f);
                        break;
                    }
                }
                if(definition.climbsWalls&&WallNormal(world,Feet(world,node.Cell,definition),definition)!=Vector3.zero)
                {
                    for(int rise=-1;rise<=1;rise+=2)
                    {
                        var cell=node.Cell.Offset(0,rise,0);bool clear=true;
                        // A wall gap, ceiling or unloaded border must not become a flying edge.
                        for(int sample=1;sample<=4;sample++)
                        {
                            var feet=Vector3.Lerp(Feet(world,node.Cell,definition),Feet(world,cell,definition),sample*.25f);
                            if(world.Overlaps(feet,definition.width,definition.height)||WallNormal(world,feet,definition)==Vector3.zero){clear=false;break;}
                        }
                        if(clear)Add(cell,best,node.Cost+1.15f);
                    }
                }
            }
            for(int index=closest;index>0;index=nodes[index].Parent)path.Add(nodes[index].Cell);
            path.Reverse();
        }
    }
}
