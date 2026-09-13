using System;
using System.Collections.Generic;
using System.Linq;

namespace RivetReach
{
    // A bounded, immutable matcher for small station inventories. Exact IDs and tags
    // follow the same transaction rules. Supplied work buffers avoid per-tick garbage.
    public sealed class IngredientMatcher
    {
        readonly ItemSelector[] units;
        readonly int slots,fullMask;
        readonly int[] limits=new int[256];
        readonly byte[] representatives;
        public IngredientMatcher(IEnumerable<RecipeIngredient> ingredients,ItemRegistry items,int slots=3)
        {
            if(ingredients==null||items==null)throw new ArgumentNullException();
            if(slots<1||slots>9)throw new ArgumentOutOfRangeException(nameof(slots));this.slots=slots;
            var list=new List<ItemSelector>();
            foreach(var input in ingredients)
            {
                if(input.Count<1||input.Count>9||list.Count+input.Count>9)throw new ArgumentException("Ingredient matcher supports one to nine units.");
                var selector=items.Select(input.ItemId);
                for(int n=0;n<input.Count;n++)list.Add(selector);
            }
            if(list.Count==0)throw new ArgumentException("Recipe needs ingredients.");units=list.ToArray();fullMask=(1<<units.Length)-1;
            var profiles=new HashSet<(int mask,int capacity)>();var examples=new List<byte>();
            foreach(var item in items.items)
            {
                byte id=item.runtimeId;limits[id]=item.stackLimit;int mask=0;
                for(int n=0;n<units.Length;n++)if(units[n].Matches(id))mask|=1<<n;
                if(mask!=0&&profiles.Add((mask,Math.Min(units.Length,limits[id]))))examples.Add(id);
            }
            representatives=examples.ToArray();
            Span<byte> ids=stackalloc byte[slots];Span<int> counts=stackalloc int[slots];ids.Clear();counts.Clear();
            if(!Complete(0,ids,counts))throw new ArgumentException("Ingredients cannot fit this station's input slots.");
        }
        public bool Accepts(byte id)
        {foreach(var unit in units)if(unit.Matches(id))return true;return false;}
        public bool TryPlan(IReadOnlyList<ItemStack> inventory,int[] used)
        {
            if(inventory.Count<slots||used.Length!=slots)throw new ArgumentException("Invalid ingredient work buffer.");
            Array.Clear(used,0,used.Length);return Match(0,inventory,used);
        }
        bool Match(int unit,IReadOnlyList<ItemStack> inventory,int[] used)
        {
            if(unit==units.Length)return true;
            for(int slot=0;slot<slots;slot++)
            {
                var stack=inventory[slot];
                if(stack.HasContents||stack.Count<=used[slot]||!units[unit].Matches(stack.Id))continue;
                used[slot]++;if(Match(unit+1,inventory,used))return true;used[slot]--;
            }
            return false;
        }
        // Accept a piped unit only if it satisfies an additional requirement and
        // the remaining ingredients can still fit. Manual bulk stacks may leave surplus. This handles overlapping
        // tags and mixtures that would otherwise occupy every slot prematurely.
        public bool CanStage(IReadOnlyList<ItemStack> inventory,byte incoming)
        {
            if(!Accepts(incoming)||inventory.Count<slots)return false;
            Span<byte> ids=stackalloc byte[slots];Span<int> counts=stackalloc int[slots];int target=-1,empty=-1;
            for(int slot=0;slot<slots;slot++)
            {
                var s=inventory[slot];if(s.HasContents)return false;
                ids[slot]=s.Empty?(byte)0:s.Id;counts[slot]=s.Empty?0:s.Count;
                if(s.Empty&&empty<0)empty=slot;
                if(s.Id==incoming&&s.Count<limits[incoming]&&target<0)target=slot;
            }
            int rank=SuppliedRank(ids,counts);if(rank==units.Length)return false;
            if(target<0)target=empty;if(target<0)return false;ids[target]=incoming;counts[target]++;
            if(SuppliedRank(ids,counts)<=rank)return false;
            Span<int> used=stackalloc int[slots];used.Clear();
            return Extend(0,0,0,rank,ids,counts,used);
        }
        int SuppliedRank(Span<byte> ids,Span<int> counts)
        {
            // Bipartite maximum matching, at most nine requirement units and 81
            // available unit tokens. A slot's useful capacity is bounded by recipe size.
            Span<int> owners=stackalloc int[slots*units.Length];owners.Fill(-1);
            Span<bool> seen=stackalloc bool[owners.Length];int rank=0;
            for(int unit=0;unit<units.Length;unit++){seen.Clear();if(Augment(unit,ids,counts,owners,seen))rank++;}
            return rank;
        }
        bool Augment(int unit,Span<byte> ids,Span<int> counts,Span<int> owners,Span<bool> seen)
        {
            for(int slot=0;slot<slots;slot++)
            {
                if(!units[unit].Matches(ids[slot]))continue;
                for(int n=0;n<Math.Min(counts[slot],units.Length);n++)
                {
                    int token=slot*units.Length+n;if(seen[token])continue;seen[token]=true;
                    if(owners[token]<0||Augment(owners[token],ids,counts,owners,seen)){owners[token]=unit;return true;}
                }
            }
            return false;
        }
        bool Extend(int unit,int matched,int mask,int previousRank,Span<byte> ids,Span<int> counts,Span<int> used)
        {
            if(matched+units.Length-unit<=previousRank)return false;
            if(unit==units.Length)return Complete(mask,ids,counts);
            for(int slot=0;slot<slots;slot++)if(used[slot]<counts[slot]&&units[unit].Matches(ids[slot]))
            {
                used[slot]++;bool fits=Extend(unit+1,matched+1,mask|(1<<unit),previousRank,ids,counts,used);used[slot]--;if(fits)return true;
            }
            return Extend(unit+1,matched,mask,previousRank,ids,counts,used);
        }
        bool Complete(int mask,Span<byte> ids,Span<int> counts)
        {
            if(mask==fullMask)return true;int unit=0;while((mask&(1<<unit))!=0)unit++;
            int empty=-1;
            for(int slot=0;slot<slots;slot++)
            {
                byte id=ids[slot];if(id==0){if(empty<0)empty=slot;continue;}
                if(counts[slot]>=limits[id]||!units[unit].Matches(id))continue;
                counts[slot]++;bool fits=Complete(mask|(1<<unit),ids,counts);counts[slot]--;if(fits)return true;
            }
            if(empty>=0)foreach(byte candidate in representatives)
            {
                if(!units[unit].Matches(candidate))continue;
                ids[empty]=candidate;counts[empty]=1;bool fits=Complete(mask|(1<<unit),ids,counts);ids[empty]=0;counts[empty]=0;if(fits)return true;
            }
            return false;
        }
    }
}
