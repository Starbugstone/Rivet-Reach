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
        public int Total(byte id){int total=0;foreach(var p in Piles)if(p.Stack.Id==id)total+=p.Stack.Count;return total;}
        public void Initialize(Expedition game)
        {
            Game=game;World=game.World;
            World.BlockChanged+=pos=>{foreach(var p in Piles)if(Math.Abs(p.Position.Cell.X-pos.X)<=1&&Math.Abs(p.Position.Cell.Z-pos.Z)<=1&&Math.Abs(p.Position.Cell.Y-pos.Y)<=2)p.Sleeping=false;};
            World.OriginShifted+=shift=>{foreach(var p in Piles)if(p.View!=null)p.View.transform.position=p.Position.Local(World.Origin);};
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
                p.Age+=dt;p.Delay=Mathf.Max(0,p.Delay-dt);
                if(!p.Sleeping)
                {
                    p.Velocity.y=Mathf.Max(-18,p.Velocity.y-18*dt);
                    local=World.Move(local,p.Velocity*dt,.20f,.20f,out bool grounded);
                    if(grounded){p.Velocity=Vector3.Lerp(p.Velocity,Vector3.zero,.7f);p.Velocity.y=0;if(p.Velocity.sqrMagnitude<.02f)p.Sleeping=true;}
                    p.Position=WorldPoint.FromLocal(local,World.Origin);
                }
                if(p.Delay<=0&&Vector3.Distance(local,player+Vector3.up*.5f)<1.5f)
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
            // Spatial buckets avoid testing every pair in a dense field of distinct piles.
            var buckets=new Dictionary<(long,int,long),List<Pile>>();
            foreach(var p in Piles)
            {
                if(p.Delay>0||!World.Ready(p.Position.Cell)||Vector3.Distance(p.Position.Local(World.Origin),player)>64)continue;
                var c=p.Position.Cell;var key=(BlockPos.FloorDiv(c.X,2),(int)BlockPos.FloorDiv(c.Y,2),BlockPos.FloorDiv(c.Z,2));
                Pile survivor=null;
                for(int z=-1;z<=1;z++)for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
                {
                    if(!buckets.TryGetValue((key.Item1+x,key.Item2+y,key.Item3+z),out var nearby))continue;
                    foreach(var other in nearby)
                    {
                        if(other.Stack.Id!=p.Stack.Id||other.Stack.Count>=Game.Registry.Get(p.Stack.Id).stackLimit)continue;
                        if((other.Position.Local(World.Origin)-p.Position.Local(World.Origin)).sqrMagnitude>1)continue;
                        if(other.Sleeping!=p.Sleeping)continue;
                        Vector3 from=p.Position.Local(World.Origin)+Vector3.up*.1f,toward=other.Position.Local(World.Origin)+Vector3.up*.1f-from;
                        if(World.Raycast(from,toward.normalized,toward.magnitude,out _,out _))continue;
                        if(survivor==null||other.Id<survivor.Id)survivor=other;
                    }
                }
                if(survivor!=null)
                {
                    int n=Math.Min(p.Stack.Count,Game.Registry.Get(p.Stack.Id).stackLimit-survivor.Stack.Count);
                    survivor.Stack.Count+=n;p.Stack.Count-=n;survivor.Age=Math.Max(survivor.Age,p.Age);
                }
                if(!p.Stack.Empty){if(!buckets.TryGetValue(key,out var list)){list=new List<Pile>();buckets.Add(key,list);}list.Add(p);}
            }
            for(int i=Piles.Count-1;i>=0;i--)if(Piles[i].Stack.Empty)Delete(i);
        }
        void Delete(int i){if(Piles[i].View!=null)Destroy(Piles[i].View);Piles.RemoveAt(i);}
        void OnDestroy(){foreach(var m in materials.Values)Destroy(m);}
    }
}
