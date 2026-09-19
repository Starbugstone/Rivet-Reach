using System;
using System.Collections.Generic;
using System.Linq;

namespace RivetReach
{
    // Optional endpoint policy. A controller's virtual slot identifies a real physical inventory.
    public interface IItemPipeRoutingPolicy
    {
        object SourceIdentity(int slot);
        bool TryInsertFrom(byte id,object source,int localFace);
        bool SharesStorage(IItemPipeInventory other);
    }
    public static class ItemPipeRouting
    {
        public static int Priority(IItemPipeInventory inventory)=>Math.Clamp(inventory.ItemInputPriority,0,100);
        public static object SourceIdentity(IItemPipeInventory inventory,int slot)=>inventory is IItemPipeRoutingPolicy p?p.SourceIdentity(slot):inventory;
    }
    public sealed class CrateNetwork
    {
        public const int MaximumCrates=64;
        readonly Func<BlockPos,byte> block;readonly Func<BlockPos,CrateStorage> storage;readonly Func<BlockPos,bool> ready;
        readonly Dictionary<BlockPos,CrateEndpoint> endpoints=new Dictionary<BlockPos,CrateEndpoint>();
        internal Func<BlockPos,int> GetPriority;internal Action<BlockPos,int> SetPriority;
        public long Revision {get;private set;}
        public CrateNetwork(Func<BlockPos,byte> block,Func<BlockPos,CrateStorage> storage,Func<BlockPos,bool> ready)
        {this.block=block;this.storage=storage;this.ready=ready;}
        public void Invalidate()=>Revision++;
        public CrateEndpoint At(BlockPos p)
        {
            if(!CrateId.Part(block(p)))return null;
            if(!endpoints.TryGetValue(p,out var e))endpoints[p]=e=new CrateEndpoint(this,p,block(p)==CrateId.Controller);return e;
        }
        public void Removed(BlockPos p){endpoints.Remove(p);Invalidate();}
        public bool Known(BlockPos p)=>endpoints.ContainsKey(p);
        internal bool Ready(BlockPos p)=>ready(p);
        internal void Resolve(BlockPos start,List<(BlockPos pos,CrateStorage store)> members,out string status)
        {
            members.Clear();status="Ready";if(!ready(start)){status="Waiting for terrain";return;}
            byte id=block(start);if(id==CrateId.Crate){var one=storage(start);if(one!=null)members.Add((start,one));return;}
            if(id!=CrateId.Controller){status="Removed";return;}
            var seen=new HashSet<BlockPos>{start};var pending=new Queue<BlockPos>();pending.Enqueue(start);int controllers=0;
            while(pending.Count>0)
            {
                var p=pending.Dequeue();if(block(p)==CrateId.Controller)controllers++;else if(storage(p) is CrateStorage crate)members.Add((p,crate));
                if(controllers>1){status="Only one controller per connected bank";members.Clear();return;}
                if(members.Count>MaximumCrates){status="Maximum 64 crates per bank";members.Clear();return;}
                for(int f=0;f<6;f++)
                {
                    var q=IndustryDefinition.Neighbor(p,f);
                    if(ready(q)&&CrateId.Part(block(q))&&seen.Add(q))pending.Enqueue(q);
                }
            }
            members.Sort((a,b)=>{int c=a.pos.X.CompareTo(b.pos.X);if(c!=0)return c;c=a.pos.Y.CompareTo(b.pos.Y);return c!=0?c:a.pos.Z.CompareTo(b.pos.Z);});
            if(members.Count==0)status="Connect crates to this controller";
        }
    }
    public sealed class CrateEndpoint : IItemPipeInventory,IItemPipeRoutingPolicy
    {
        readonly CrateNetwork network;readonly BlockPos position;readonly bool controller;
        readonly List<(BlockPos pos,CrateStorage store)> members=new List<(BlockPos,CrateStorage)>();
        readonly HashSet<CrateStorage> identities=new HashSet<CrateStorage>();
        readonly List<ItemStack> stacks=new List<ItemStack>();long topology=-1;string status;
        public CrateEndpoint(CrateNetwork network,BlockPos p,bool controller){this.network=network;position=p;this.controller=controller;}
        void Resolve(){if(topology==network.Revision)return;network.Resolve(position,members,out status);identities.Clear();foreach(var m in members)identities.Add(m.store);topology=network.Revision;}
        public IReadOnlyList<(BlockPos pos,CrateStorage store)> Members {get{Resolve();return members;}}
        public string Status {get{Resolve();return status;}}
        int priority=-1;
        public int ItemInputPriority {get=>network.GetPriority?.Invoke(position)??(priority<0?(controller?40:30):priority);set{if(network.SetPriority!=null)network.SetPriority(position,value);else priority=value;}}
        public long Revision {get{Resolve();long n=network.Revision;foreach(var m in members)n+=m.store.Revision;return n;}}
        public IReadOnlyList<ItemStack> Slots {get{Resolve();stacks.Clear();foreach(var m in members)stacks.Add(network.Ready(m.pos)?m.store.Stack:default);return stacks;}}
        public CrateStorage Store(int slot){Resolve();return slot>=0&&slot<members.Count&&network.Ready(position)&&network.Ready(members[slot].pos)?members[slot].store:null;}
        public bool SharesStorage(IItemPipeInventory other)
        {if(other is not CrateEndpoint endpoint)return false;Resolve();foreach(var member in endpoint.Members)if(identities.Contains(member.store))return true;return false;}
        public bool CanExtract(int slot)=>Store(slot)?.Count>0;
        public object SourceIdentity(int slot)=>Store(slot);
        public bool Prefers(byte id,int localFace=-1)=>false;
        public bool TryInsert(byte id,int localFace=-1)=>TryInsertFrom(id,null,localFace);
        public bool TryInsertFrom(byte id,object source,int localFace=-1)=>Insert(new ItemStack(id,1),source)==1;
        public int Insert(ItemStack stack,object source=null)
        {
            Resolve();if(!network.Ready(position)||stack.HasContents)return 0;int remaining=stack.Count;
            for(int pass=0;pass<2&&remaining>0;pass++)foreach(var member in members)
            {
                var target=member.store;if(!network.Ready(member.pos)||ReferenceEquals(target,source)||(pass==0?target.Item!=stack.Id:target.Item!=0))continue;
                remaining-=target.Insert(stack.WithCount(remaining));
            }
            return stack.Count-remaining;
        }
        public ItemStack Extract(int slot,int count)=>Store(slot)?.Take(count)??default;
    }
}
