using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    [Serializable] public sealed class FoodIngredient { public string selector;public int count=1; }
    [Serializable] public sealed class CookingRecipe
    {
        public string id,name;
        public FoodIngredient[] ingredients;
        public byte output;
        public int count=1,ticks=200;
    }
    [Serializable] public sealed class CookingCatalog
    {

        public CookingRecipe[] recipes;
        public int electricWatts=200;
        static CookingCatalog cached;
        public static CookingCatalog Current=>cached??=Load();
        static CookingCatalog Load()
        {
            var asset=Resources.Load<TextAsset>("Definitions/Cooking");
            if(asset==null)throw new InvalidOperationException("Missing food cooker catalog.");
            var result=JsonUtility.FromJson<CookingCatalog>(asset.text);result.Validate(ItemRegistry.Load());return result;
        }
        public void Validate(ItemRegistry items)
        {
            if(electricWatts<1||electricWatts>10000||recipes==null||recipes.Length==0||recipes.Length>32)throw new ArgumentException("Invalid cooker configuration.");
            var names=new HashSet<string>();
            names.Clear();
            foreach(var recipe in recipes)
            {
                if(string.IsNullOrWhiteSpace(recipe.id)||!names.Add(recipe.id)||recipe.ticks<1||recipe.ticks>12000||recipe.count<1||recipe.count>items.Get(recipe.output).stackLimit||items.FoodPoints(recipe.output)==0||recipe.ingredients==null||recipe.ingredients.Length==0||recipe.ingredients.Length>3||recipe.ingredients.Sum(i=>i.count)>9)throw new ArgumentException("Invalid food recipe.");
                foreach(var input in recipe.ingredients)if(input.count<1||Choices(input.selector).Length==0)throw new ArgumentException("Invalid food ingredient.");
            }
        }
        public byte[] Choices(string selector)
        {
            if(string.IsNullOrEmpty(selector))return Array.Empty<byte>();
            if(selector.StartsWith("#",StringComparison.Ordinal))return ItemRegistry.Load().items.Where(i=>ItemRegistry.Load().HasTag(i.runtimeId,selector.Substring(1))).Select(i=>i.runtimeId).ToArray();
            return new[]{ItemRegistry.Load().ResolveId(selector)};
        }
        public CookingRecipe Find(string id)=>recipes.FirstOrDefault(r=>r.id==id);
        // Bounded backtracking handles overlapping tags and split stacks without greedy allocation bugs.
        public int[] Plan(CookingRecipe recipe,IReadOnlyList<ItemStack> slots)
        {
            var needed=new List<byte[]>();foreach(var input in recipe.ingredients)for(int n=0;n<input.count;n++)needed.Add(Choices(input.selector));
            var used=new int[3];
            bool Match(int at)
            {
                if(at==needed.Count)return true;
                for(int slot=0;slot<3;slot++)if(!slots[slot].HasContents&&slots[slot].Count>used[slot]&&Array.IndexOf(needed[at],slots[slot].Id)>=0)
                {used[slot]++;if(Match(at+1))return true;used[slot]--;}
                return false;
            }
            return Match(0)?used:null;
        }
    }
    public sealed partial class MachineState
    {
        public bool IsCooker=>FarmId.CookerBlock(Definition.Id);
        public string CookingId="",CookingSignature="";
        public CookingRecipe FoodRecipe=>IsCooker?CookingCatalog.Current.Find(CookingId):null;
        public int OutputSlot=>IsCooker?4:2;
        public bool CookerAccepts(int slot,byte id)
        {
            if(slot==3)return Definition.Id==FarmId.Cooker&&FuelTicks(id)>0;
            return slot>=0&&slot<3&&FoodRecipe!=null&&FoodRecipe.ingredients.Any(i=>Array.IndexOf(CookingCatalog.Current.Choices(i.selector),id)>=0);
        }
        public int FuelTicks(byte id)=>id==0?0:processing?.FuelTicks(id)??ProcessingCatalogAsset.Load().fuels.FirstOrDefault(f=>f.itemId==ItemRegistry.Load().Get(id).stableId&&ItemRegistry.Load().HasTag(id,"burnable")).ticks;
        bool InsertCooker(byte id,int face)
        {
            if(Definition.Id==FarmId.Cooker&&face==4)return CookerAccepts(3,id)&&Items.Add(id,1,3,4)==0;
            if(face<0&&CookerAccepts(3,id))return Items.Add(id,1,3,4)==0;
            if(!CookerAccepts(0,id))return false;
            // Reserve room for the other ingredients: pipes stage one recipe batch at a time.
            int desired=FoodRecipe.ingredients.Where(i=>Array.IndexOf(CookingCatalog.Current.Choices(i.selector),id)>=0).Sum(i=>i.count);
            var related=FoodRecipe.ingredients.Where(i=>Array.IndexOf(CookingCatalog.Current.Choices(i.selector),id)>=0).SelectMany(i=>CookingCatalog.Current.Choices(i.selector)).Distinct().ToArray();
            int present=Items.Slots.Take(3).Where(s=>Array.IndexOf(related,s.Id)>=0).Sum(s=>s.Count);
            return present<desired&&Items.Add(id,1,0,3)==0;
        }
        public void SelectCooking(string id)
        {
            if(!IsCooker||CookingCatalog.Current.Find(id)==null)throw new ArgumentException("Unknown food recipe.");
            if(CookingId==id)return;CookingId=id;Work=0;CookingSignature="";
        }
        public string FoodSignature=>CookingId+":"+string.Join(",",Items.Slots.Take(3).Select(s=>s.Id));
    }
    public sealed partial class IndustrySimulation
    {
        void PrepareCooker(MachineState m)
        {
            var recipe=m.FoodRecipe;
            if(m.CookingSignature!=m.FoodSignature){m.Work=0;m.CookingSignature=m.FoodSignature;}
            if(!m.Enabled){m.Status=MachineStatus.DisabledBySignal;return;}
            if(recipe==null||CookingCatalog.Current.Plan(recipe,m.Items.Slots)==null){m.Status=MachineStatus.NoInput;return;}
            if(m.Items.Capacity(recipe.output,4,5)<recipe.count){m.Status=MachineStatus.OutputFull;return;}
            if(m.Definition.Id==FarmId.Cooker)
            {
                if(m.BurnTicks==0)
                {
                    byte fuel=m.Items.Slots[3].Id;
                    if(!m.CookerAccepts(3,fuel)){m.Status=MachineStatus.NoFuel;return;}
                    m.Items.Take(3,1);m.BurnTicks=m.FuelTicks(fuel);
                }
                m.Status=MachineStatus.Ready;
            }
            else {m.RequestedWatts=CookingCatalog.Current.electricWatts;m.Status=MachineStatus.NoPower;}
        }
        void AdvanceCooker(MachineState m)
        {
            bool electric=m.Definition.Id==FarmId.ElectricCooker;
            if(electric?(m.RequestedWatts==0||m.ReceivedWatts==0):m.Status!=MachineStatus.Ready)return;
            var recipe=m.FoodRecipe;var plan=CookingCatalog.Current.Plan(recipe,m.Items.Slots);
            if(plan==null||m.Items.Capacity(recipe.output,4,5)<recipe.count)return;
            // Pipes may have filled an empty ingredient slot since preparation.
            if(m.CookingSignature!=m.FoodSignature){m.Work=0;m.CookingSignature=m.FoodSignature;}
            if(!electric)m.BurnTicks--;
            m.Work+=electric?m.ReceivedWatts/(double)CookingCatalog.Current.electricWatts:1;
            m.Status=electric&&m.ReceivedWatts<m.RequestedWatts?MachineStatus.Underpowered:MachineStatus.Running;
            if(m.Work+1e-9<recipe.ticks)return;
            // One authority turn: validate all ingredients and output capacity before consuming anything.
            for(int slot=0;slot<3;slot++)if(plan[slot]>0)m.Items.Take(slot,plan[slot]);
            m.Items.Add(recipe.output,recipe.count,4,5);m.Work-=recipe.ticks;m.CookingSignature=m.FoodSignature;
        }
    }
}
