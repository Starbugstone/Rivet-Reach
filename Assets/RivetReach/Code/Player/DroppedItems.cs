using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    public sealed class DroppedItems : MonoBehaviour
    {
        public sealed class Pile
        {
            public long Id;
            public ItemStack Stack;
            public WorldPoint Position;
            public Vector3 Velocity;
            public float Age,Delay;
            internal float EscapeRetry;
            public bool Sleeping;
            public GameObject View;
        }
        public readonly List<Pile> Piles=new List<Pile>();
        public VoxelWorld World;
        public Expedition Game;
        public int TotalSpawned {get;private set;}
        public int TotalExpired {get;private set;}
        long nextId=1;
        float accumulator,mergeAt;
        readonly Dictionary<byte,Material> materials=new Dictionary<byte,Material>();
        readonly Dictionary<(byte,long,int,long),List<Pile>> mergeBuckets=new Dictionary<(byte,long,int,long),List<Pile>>();
        readonly Stack<List<Pile>> spareBuckets=new Stack<List<Pile>>();
        // Enclose the rotated 23 cm display cube as well as its physical height.
        public const float CollisionWidth=.33f,CollisionHeight=.23f,PickupRadius=1.7f;
        public int Total(byte id){int total=0;foreach(var p in Piles)if(p.Stack.Id==id)total+=p.Stack.Count;return total;}
        public void Initialize(Expedition game)
        {
            Game=game;World=game.World;
            World.BlockChanged+=BlockChanged;
            World.OriginShifted+=shift=>{foreach(var p in Piles)if(p.View!=null)p.View.transform.position=p.Position.Local(World.Origin);};
        }
        void BlockChanged(BlockPos cell)
        {
            foreach(var p in Piles)
            {
                if(Math.Abs(p.Position.Cell.X-cell.X)>1||Math.Abs(p.Position.Cell.Z-cell.Z)>1||Math.Abs(p.Position.Cell.Y-cell.Y)>2)continue;
                p.Sleeping=false;p.EscapeRetry=0;
                var local=p.Position.Local(World.Origin);
                if(World.Overlaps(local,CollisionWidth,CollisionHeight))
                    Escape(p,World.Get(cell)!=0?cell:World.Address(local+Vector3.up*(CollisionHeight*.5f)));
            }
        }
        bool Escape(Pile pile,BlockPos obstacle)
        {
            var local=pile.Position.Local(World.Origin);var min=World.Local(obstacle);
            // Prefer the top of the new block, then the closest clear side. Check complete
            // item bounds against authoritative voxels, including unloaded frontiers.
            var sides=new[]{Vector3.left,Vector3.right,Vector3.back,Vector3.forward};
            Vector3 Candidate(Vector3 side,int distance)
            {
                var next=local;
                if(side.x!=0)next.x=min.x+(side.x>0?1+CollisionWidth*.5f+distance+.003f:-CollisionWidth*.5f-distance-.003f);
                else next.z=min.z+(side.z>0?1+CollisionWidth*.5f+distance+.003f:-CollisionWidth*.5f-distance-.003f);
                return next;
            }
            Array.Sort(sides,(a,b)=>(Candidate(a,0)-local).sqrMagnitude.CompareTo((Candidate(b,0)-local).sqrMagnitude));
            bool MoveTo(Vector3 next)
            {
                if(World.Overlaps(next,CollisionWidth,CollisionHeight))return false;
                var direction=next-local;var horizontal=new Vector3(direction.x,0,direction.z).normalized;
                pile.Position=WorldPoint.FromLocal(next,World.Origin);
                pile.Velocity=horizontal*1.6f+Vector3.up*(direction.y>0?2.25f:direction.y<0?-.8f:.8f);
                pile.Sleeping=false;pile.EscapeRetry=0;
                if(pile.View!=null)pile.View.transform.position=next+Vector3.up*.115f;
                return true;
            }
            for(int distance=0;distance<8;distance++)
            {
                if(MoveTo(new Vector3(local.x,min.y+1+distance+.003f,local.z)))return true;
                foreach(var side in sides)if(MoveTo(Candidate(side,distance)))return true;
                if(MoveTo(new Vector3(local.x,min.y-CollisionHeight-distance-.003f,local.z)))return true;
            }
            // A completely sealed/unloaded region must not reject the placement or erase
            // the pile. Retain it and retry when terrain changes or space becomes available.
            pile.Velocity=Vector3.zero;pile.EscapeRetry=.25f;return false;
        }
        public void Spawn(ItemStack stack,Vector3 local,Vector3 velocity,float delay=0)
        {
            if(stack.Empty)return;int max=Game.Registry.Get(stack.Id).stackLimit;
            while(stack.Count>0)
            {
                int n=Math.Min(stack.Count,max);stack.Count-=n;
                Piles.Add(new Pile{Id=nextId++,Stack=new ItemStack(stack.Id,n),Position=WorldPoint.FromLocal(local,World.Origin),Velocity=velocity,Delay=delay});
                TotalSpawned+=n;
            }
        }
        void Update()
        {
            if(Game==null||!Game.Started||Game.Paused)return;
            accumulator+=Time.deltaTime;
            int steps=0;
            while(accumulator>=.05f&&steps++<4){Step(.05f);accumulator-=.05f;}
            // Backlog is retained, not converted into lost item time.
            foreach(var p in Piles)
            {
                bool visible=World.Ready(p.Position.Cell)&&Vector3.Distance(p.Position.Local(World.Origin),Game.Player.transform.position)<96;
                if(!visible){if(p.View!=null){Destroy(p.View);p.View=null;}continue;}
                if(p.View==null)
                {
                    p.View=GameObject.CreatePrimitive(PrimitiveType.Cube);p.View.name="Item stack "+p.Id;p.View.transform.SetParent(transform,false);
                    Destroy(p.View.GetComponent<Collider>());p.View.transform.localScale=Vector3.one*.23f;
                    if(!materials.TryGetValue(p.Stack.Id,out var m))
                    {
                        m=new Material(Resources.Load<Material>("Materials/Player"));m.SetTexture("_BaseMap",Texture2D.whiteTexture);m.SetColor("_BaseColor",Game.Registry.Get(p.Stack.Id).colour);materials.Add(p.Stack.Id,m);
                    }
                    p.View.GetComponent<Renderer>().sharedMaterial=m;
                }
                p.View.transform.position=p.Position.Local(World.Origin)+Vector3.up*.115f;
                p.View.transform.rotation=Quaternion.Euler(0,(p.Id*37)%360,0);
            }
        }
        public void Step(float dt)
        {
            Vector3 player=Game.Player.transform.position;
            for(int i=0;i<Piles.Count;i++)
            {
                var p=Piles[i];var local=p.Position.Local(World.Origin);
                if(!World.Ready(p.Position.Cell)||Vector3.Distance(local,player)>64)continue;
                p.Age+=dt;p.Delay=Mathf.Max(0,p.Delay-dt);p.EscapeRetry=Mathf.Max(0,p.EscapeRetry-dt);
                bool clear=true;
                if(!p.Sleeping)
                {
                    if(World.Overlaps(local,CollisionWidth,CollisionHeight))
                    {clear=p.EscapeRetry<=0&&Escape(p,World.Address(local+Vector3.up*(CollisionHeight*.5f)));local=p.Position.Local(World.Origin);}
                    if(clear)
                    {
                        p.Velocity.y=Mathf.Max(-18,p.Velocity.y-18*dt);
                        local=World.Move(local,p.Velocity*dt,CollisionWidth,CollisionHeight,out bool grounded);
                        if(grounded){p.Velocity=Vector3.Lerp(p.Velocity,Vector3.zero,.7f);p.Velocity.y=0;if(p.Velocity.sqrMagnitude<.02f)p.Sleeping=true;}
                        p.Position=WorldPoint.FromLocal(local,World.Origin);
                    }
                }
                if(clear&&p.Delay<=0&&Vector3.Distance(local,player+Vector3.up*.5f)<=PickupRadius)
                {
                    Vector3 start=player+Vector3.up*.9f,delta=local+Vector3.up*.1f-start;
                    if(!World.Raycast(start,delta.normalized,delta.magnitude-.05f,out _,out _))
                    {
                        int left=Game.Inventory.Add(p.Stack.Id,p.Stack.Count);
                        if(left<p.Stack.Count)Game.Sound.Pickup();
                        else Game.Notify("Inventory full — make room to collect this stack",2);
                        p.Stack=new ItemStack(p.Stack.Id,left);
                    }
                }
                if(p.Stack.Empty||p.Age>=1200)
                {if(p.Age>=1200)TotalExpired+=p.Stack.Count;Delete(i--);}
            }
            mergeAt+=dt;if(mergeAt<.5f)return;mergeAt=0;
            // Item identity is part of the spatial key: different items can occupy the
            // same cell without competing for space or scanning each other's piles.
            // Reuse scratch storage and release pile references after every merge pass.
            foreach(var p in Piles)
            {
                if(p.Delay>0||!World.Ready(p.Position.Cell)||Vector3.Distance(p.Position.Local(World.Origin),player)>64)continue;
                var local=p.Position.Local(World.Origin);
                if(World.Overlaps(local,CollisionWidth,CollisionHeight))continue;
                int limit=Game.Registry.Get(p.Stack.Id).stackLimit;
                var c=p.Position.Cell;var key=(p.Stack.Id,BlockPos.FloorDiv(c.X,2),(int)BlockPos.FloorDiv(c.Y,2),BlockPos.FloorDiv(c.Z,2));
                while(!p.Stack.Empty)
                {
                    Pile survivor=null;
                    for(int z=-1;z<=1;z++)for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
                    {
                        if(!mergeBuckets.TryGetValue((key.Item1,key.Item2+x,key.Item3+y,key.Item4+z),out var nearby))continue;
                        foreach(var other in nearby)
                        {
                            if(other.Stack.Count>=limit||other.Sleeping!=p.Sleeping)continue;
                            Vector3 toward=other.Position.Local(World.Origin)-local;
                            if(toward.sqrMagnitude>1)continue;
                            if(World.Raycast(local+Vector3.up*.1f,toward.normalized,toward.magnitude,out _,out _))continue;
                            if(survivor==null||other.Id<survivor.Id)survivor=other;
                        }
                    }
                    if(survivor==null)break;
                    int n=Math.Min(p.Stack.Count,limit-survivor.Stack.Count);
                    survivor.Stack.Count+=n;p.Stack.Count-=n;survivor.Age=Math.Max(survivor.Age,p.Age);
                }
                if(!p.Stack.Empty&&p.Stack.Count<limit)
                {
                    if(!mergeBuckets.TryGetValue(key,out var list))
                    {list=spareBuckets.Count>0?spareBuckets.Pop():new List<Pile>();mergeBuckets.Add(key,list);}
                    list.Add(p);
                }
            }
            foreach(var list in mergeBuckets.Values){list.Clear();spareBuckets.Push(list);}
            mergeBuckets.Clear();
            for(int i=Piles.Count-1;i>=0;i--)if(Piles[i].Stack.Empty)Delete(i);
        }
        void Delete(int i){if(Piles[i].View!=null)Destroy(Piles[i].View);Piles.RemoveAt(i);}
        void OnDestroy(){foreach(var m in materials.Values)Destroy(m);}
    }
}
