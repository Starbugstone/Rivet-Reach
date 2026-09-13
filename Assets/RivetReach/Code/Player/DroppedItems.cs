using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class DroppedItems : MonoBehaviour
    {
        public sealed class Pile
        {
            public long Id;
            public ItemStack Stack;
            public WorldPoint Position;
            public Vector3 Velocity;
            public float Age,Delay;
            public bool ActionCreated {get;internal set;}
            public float CollectionRadius=>ActionCreated?ActionPickupRadius:PickupRadius;
            internal float EscapeRetry;
            public bool Sleeping;
            public GameObject View;
            internal int ViewCopies;
        }
        public readonly List<Pile> Piles=new List<Pile>();
        public VoxelWorld World;
        public Expedition Game;
        public int TotalSpawned {get;private set;}
        public int TotalExpired {get;private set;}
        public int TotalBurned {get;private set;}
        long nextId=1;
        float accumulator,mergeAt;
        readonly Dictionary<byte,Material> materials=new Dictionary<byte,Material>();
        Texture2D torchIcon;
        readonly Dictionary<byte,Mesh> meshes=new Dictionary<byte,Mesh>();
        readonly Dictionary<(byte,long,int,long),List<Pile>> mergeBuckets=new Dictionary<(byte,long,int,long),List<Pile>>();
        readonly Stack<List<Pile>> spareBuckets=new Stack<List<Pile>>();
        // Enclose the rotated 23 cm display cube as well as its physical height.
        public const float CollisionWidth=.33f,CollisionHeight=.23f,PickupRadius=1.7f*1.2f,ActionPickupRadius=PickupRadius*1.2f;
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
        public void Spawn(ItemStack stack,Vector3 local,Vector3 velocity,float delay=0,bool actionCreated=false)
        {
            if(stack.Empty)return;int max=stack.Limit(Game.Registry.Get(stack.Id).stackLimit);
            while(stack.Count>0)
            {
                int n=Math.Min(stack.Count,max);stack.Count-=n;
                Piles.Add(new Pile{Id=nextId++,Stack=stack.WithCount(n),Position=WorldPoint.FromLocal(local,World.Origin),Velocity=velocity,Delay=delay,ActionCreated=actionCreated});
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
                int copies=Math.Min(p.Stack.Count,3);
                if(p.View==null||p.ViewCopies!=copies)
                {
                    if(p.View!=null){p.View.SetActive(false);Destroy(p.View);}
                    p.View=CreatePileView(p.Stack.Id,copies);p.ViewCopies=copies;
                    p.View.name="Item stack "+p.Id;p.View.transform.SetParent(transform,false);p.View.transform.localScale=Vector3.one*.23f;
                }
                p.View.transform.position=p.Position.Local(World.Origin)+Vector3.up*.115f;
                p.View.transform.rotation=Quaternion.Euler(0,(p.Id*37)%360+Time.time*18,0);
            }
        }
        public void Step(float dt)
        {
            Vector3 player=Game.Player.transform.position;
            for(int i=0;i<Piles.Count;i++)
            {
                var p=Piles[i];var local=p.Position.Local(World.Origin);
                if(!World.Ready(p.Position.Cell)||Vector3.Distance(local,player)>64)continue;
                if(World.TouchesFluid(local,CollisionWidth,CollisionHeight,Fluids.Lava)){TotalBurned+=p.Stack.Count;Delete(i--);continue;}
                p.Age+=dt;p.Delay=Mathf.Max(0,p.Delay-dt);p.EscapeRetry=Mathf.Max(0,p.EscapeRetry-dt);
                bool clear=true;
                if(!p.Sleeping)
                {
                    if(World.Overlaps(local,CollisionWidth,CollisionHeight))
                    {clear=p.EscapeRetry<=0&&Escape(p,World.Address(local+Vector3.up*(CollisionHeight*.5f)));local=p.Position.Local(World.Origin);}
                    if(clear)
                    {
                        bool wet=World.Submerged(local+Vector3.up*.1f,out var fluid,out _);
                        if(wet)
                        {
                            var current=World.Current(World.Address(local+Vector3.up*.1f));
                            float rise=Game.Registry.Get(p.Stack.Id).buoyant?.7f:-1.2f;
                            p.Velocity=Vector3.Lerp(p.Velocity,current+Vector3.up*rise,Mathf.Clamp01(fluid.Drag*dt));
                        }
                        else p.Velocity.y=Mathf.Max(-18,p.Velocity.y-18*dt);
                        var wetCell=World.Address(local+Vector3.up*.1f);
                        local=World.Move(local,p.Velocity*dt,CollisionWidth,CollisionHeight,out bool grounded);
                        if(wet&&Game.Registry.Get(p.Stack.Id).buoyant&&Fluids.Registry.Get(World.Get(wetCell.Offset(0,1,0)))!=fluid)
                        {
                            float surface=World.Local(wetCell).y+fluid.Height(World.Get(wetCell))-.101f;
                            if(local.y>=surface)
                            {
                                local.y=surface;p.Velocity.y=0;
                                if(p.Velocity.sqrMagnitude<.02f&&World.Current(wetCell).sqrMagnitude<.01f)p.Sleeping=true;
                            }
                        }
                        if(grounded){p.Velocity=Vector3.Lerp(p.Velocity,Vector3.zero,.7f);p.Velocity.y=0;if(p.Velocity.sqrMagnitude<.02f&&(!wet||World.Current(World.Address(local+Vector3.up*.1f)).sqrMagnitude<.01f))p.Sleeping=true;}
                        p.Position=WorldPoint.FromLocal(local,World.Origin);
                    }
                }
                if(World.TouchesFluid(local,CollisionWidth,CollisionHeight,Fluids.Lava)){TotalBurned+=p.Stack.Count;Delete(i--);continue;}
                if(clear&&p.Delay<=0&&Vector3.Distance(local,player+Vector3.up*.5f)<=p.CollectionRadius)
                {
                    Vector3 start=player+Vector3.up*.9f,delta=local+Vector3.up*.1f-start;
                    // Collection follows physical passage, including open doors and passable pipes.
                    if(!World.RaycastSolid(start,delta.normalized,delta.magnitude-.05f,out _,out _))
                    {
                        int left=Game.Inventory.Add(p.Stack);
                        if(left<p.Stack.Count){Game.Sound.Pickup();ArcadePresentation.Active?.Pickup(local,p.Stack.Id);}
                        else Game.Notify("Inventory full — make room to collect this stack",2);
                        p.Stack=p.Stack.WithCount(left);
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
                            // Keep the action bonus on its own drops, even beside the same item type.
                            if(!other.Stack.CanStack(p.Stack)||other.Stack.Count>=limit||other.Sleeping!=p.Sleeping||other.ActionCreated!=p.ActionCreated)continue;
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
        GameObject CreatePileView(byte id,int copies)
        {
            if(copies==1)return CreateView(id);
            var view=new GameObject("Stacked world item display");
            // A compact stair-step silhouette distinguishes quantity without changing
            // collision/pickup bounds. Cap geometry at three copies even for full stacks.
            for(int i=0;i<copies;i++)
            {
                var item=CreateView(id);item.transform.SetParent(view.transform,false);
                float offset=copies==2?(i==0?-.14f:.14f):(i-1)*.16f;
                item.transform.localPosition=new Vector3(offset,offset,offset*.5f);
                item.transform.localScale=Vector3.one*.64f;
            }
            return view;
        }
        GameObject CreateView(byte id)
        {
            var view=new GameObject("World item display");
            var capability=Game.Registry.Capabilities(new ItemStack(id,1));
            if(id==IndustryId.Wrench)
            {
                var model=Instantiate(Resources.Load<GameObject>("Tools/Wrench"),view.transform,false);
                var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                float scale=1/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);model.transform.localPosition=-bounds.center*scale;model.transform.localScale*=scale;
                foreach(var r in renderers)r.sharedMaterial=Resources.Load<Material>("Industry/Workshop");
            }
            else if(EquipmentVisuals.UsesModel(id))EquipmentVisuals.Create(id,view.transform);
            else if(FoodVisuals.UsesModel(id))FoodVisuals.Create(id,view.transform);
            else if(OreVisuals.UsesModel(id))
            {
                var model=OreVisuals.Create(id,view.transform);model.transform.localPosition=-Vector3.one*.5f;
            }
            else if(StarterStationVisuals.UsesModel(id))
            {
                var model=Instantiate(StarterStationVisuals.Prefab(id),view.transform,false);model.transform.localPosition=-Vector3.one*.5f;
            }
            else if(id>=IndustryId.AzureOre&&id<=IndustryId.CrushedGold)
            {
                var prefab=Resources.Load<GameObject>("Industry/Runtime/"+Game.Registry.Get(id).stableId.Substring(6));
                if(prefab!=null){var model=Instantiate(prefab,view.transform,false);model.transform.localPosition=-Vector3.one*.5f;foreach(var renderer in model.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=Resources.Load<Material>("Industry/Workshop");}
            }
            else if(id==BlockId.Torch)
            {
                if(!materials.TryGetValue(id,out var material))
                {
                    torchIcon=SurvivalItemArt.Icon(Game.Registry.Get(id));
                    material=new Material(Shader.Find("RivetReach/HeldTool"));material.SetTexture("_BaseMap",torchIcon);
                    material.SetFloat("_Cutoff",.1f);material.SetFloat("_Cull",0);material.SetFloat("_FirstPerson",0);materials.Add(id,material);
                }
                var card=GameObject.CreatePrimitive(PrimitiveType.Quad);Destroy(card.GetComponent<Collider>());
                card.transform.SetParent(view.transform,false);card.GetComponent<MeshRenderer>().sharedMaterial=material;
            }
            else if(Fluids.IsBucket(id))
            {
                var model=Instantiate(Resources.Load<GameObject>("Characters/PalmBucket"),view.transform,false);
                var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                if(!materials.TryGetValue(Fluids.EmptyBucket,out var material))
                {material=new Material(Shader.Find("RivetReach/HeldTool"));material.SetTexture("_BaseMap",Resources.Load<Texture2D>("Characters/SkinField"));material.SetFloat("_FirstPerson",0);materials.Add(Fluids.EmptyBucket,material);}
                foreach(var r in renderers)r.sharedMaterial=material;
                if(Fluids.Registry.FromBucket(id) is FluidDefinition liquid)
                {
                    var surface=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(surface.GetComponent<Collider>());surface.transform.SetParent(model.transform,false);
                    surface.transform.localPosition=new Vector3(0,.32f,0);surface.transform.localScale=new Vector3(.80f,.008f,.80f);
                    if(!materials.TryGetValue(id,out var fill))
                    {fill=new Material(Shader.Find("RivetReach/HeldTool"));fill.SetColor("_BaseColor",new Color(liquid.Red,liquid.Green,liquid.Blue));fill.SetFloat("_FirstPerson",0);materials.Add(id,fill);}
                    surface.GetComponent<Renderer>().sharedMaterial=fill;
                }
                float scale=1/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);model.transform.localPosition=-bounds.center*scale;model.transform.localScale*=scale;
            }
            else if(capability!=ToolCapability.None)
            {
                bool axe=(capability&ToolCapability.Axe)!=0;
                string path=axe?"Tools/StarterAxe":(capability&ToolCapability.Pickaxe)!=0?"Characters/GripPickaxe":"Characters/GripSword";
                var model=Instantiate(Resources.Load<GameObject>(path),view.transform,false);
                var renderers=model.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                float scale=1/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);model.transform.localPosition=-bounds.center*scale;model.transform.localScale*=scale;
                if(!materials.TryGetValue(id,out var material))
                {material=new Material(Shader.Find("RivetReach/HeldTool"));material.SetFloat("_FirstPerson",0);material.SetFloat("_AxePalette",axe?1:0);material.SetTexture("_BaseMap",Resources.Load<Texture2D>(axe?"Tools/StarterAxe":"Characters/SkinField"));materials.Add(id,material);}
                foreach(var renderer in renderers)renderer.sharedMaterial=material;
            }
            else
            {
                if(!meshes.TryGetValue(id,out var mesh))
                {
                    var cells=new byte[34*34*34];cells[ChunkMesher.Index(0,0,0)]=id;var cube=ChunkMesher.Build(default,0,cells);
                    for(int i=0;i<cube.Vertices.Length;i++)cube.Vertices[i]-=Vector3.one*.5f;
                    mesh=new Mesh{name="World item voxel "+id};mesh.vertices=cube.Vertices;mesh.normals=cube.Normals;mesh.uv=cube.UV;mesh.uv2=cube.Tiles;mesh.triangles=cube.Triangles;mesh.RecalculateBounds();meshes.Add(id,mesh);
                }
                view.AddComponent<MeshFilter>().sharedMesh=mesh;view.AddComponent<MeshRenderer>().sharedMaterial=World.TerrainMaterial;
            }
            return view;
        }
        void OnDestroy(){if(torchIcon!=null)Destroy(torchIcon);foreach(var m in materials.Values)Destroy(m);foreach(var mesh in meshes.Values)Destroy(mesh);}
    }
}
