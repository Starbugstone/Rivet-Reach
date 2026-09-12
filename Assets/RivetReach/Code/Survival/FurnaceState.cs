using System;
using System.Collections.Generic;

namespace RivetReach
{
    // Input/fuel/output authority: callers can never insert into the result slot.
    public sealed partial class FurnaceState : IItemPipeInventory
    {
        bool IItemPipeInventory.CanExtract(int slot)=>slot==2;
        int PipeInputSlot(byte id,int localFace)=>localFace<0?(Accepts(0,id)?0:Accepts(1,id)?1:-1):localFace==4?1:0;
        bool IItemPipeInventory.Prefers(byte id,int localFace)
        {
            int slot=PipeInputSlot(id,localFace);
            return slot>=0&&Accepts(slot,id)&&(Slots[slot].Id==id||slot==0&&!Slots[2].Empty&&registry.Find(id).Output.Id==Slots[2].Id);
        }
        bool IItemPipeInventory.TryInsert(byte id,int localFace)
        {
            // Rear pipes supply fuel; all other faces supply recipe ingredients.
            int slot=PipeInputSlot(id,localFace);
            if(slot<0||!Accepts(slot,id)||contents.Capacity(id,slot,slot+1)==0)return false;
            contents.Add(id,1,slot,slot+1);InputChanged();return true;
        }
        ItemStack IItemPipeInventory.Extract(int slot,int count)=>slot==2?Take(slot,count):default;
        readonly ProcessingRegistry registry;
        readonly ItemContainer contents;
        readonly Func<byte,int> limit;
        byte workingInput;
        public IReadOnlyList<ItemStack> Slots=>contents.Slots;
        public long Revision=>contents.Revision;
        public int BurnTicks {get;private set;}
        public int FuelDuration {get;private set;}
        public int ProgressTicks {get;private set;}
        public ProcessingRecipe Recipe=>registry.Find(Slots[0].Id);
        public bool CanWork=>Recipe!=null&&Slots[0].Count>=Recipe.Input.Count&&
            (Slots[2].Empty||Slots[2].Id==Recipe.Output.Id)&&Slots[2].Count<=limit(Recipe.Output.Id)-Recipe.Output.Count;
        public bool NeedsTick=>BurnTicks>0||CanWork&&registry.FuelTicks(Slots[1].Id)>0;
        public string Status=>Recipe==null?"Add an ingredient":Slots[0].Count<Recipe.Input.Count?"More ingredients needed":!CanWork?"Output full":BurnTicks==0&&registry.FuelTicks(Slots[1].Id)==0?"Add fuel":"Cooking / smelting";
        public FurnaceState(ProcessingRegistry registry,Func<byte,int> limit)
        {this.registry=registry??throw new ArgumentNullException(nameof(registry));this.limit=limit??throw new ArgumentNullException(nameof(limit));contents=new ItemContainer(3,limit);}
        public bool Accepts(int slot,byte id)=>slot==0?registry.Find(id)!=null:slot==1&&registry.FuelTicks(id)>0;
        void InputChanged(){if(workingInput!=Slots[0].Id){ProgressTicks=0;workingInput=Slots[0].Id;}}
        public void Click(int slot,ref ItemStack held,bool right)
        {
            if(slot<0||slot>2)throw new ArgumentOutOfRangeException(nameof(slot));
            if(slot==2)
            {
                var output=Slots[2];if(output.Empty||!held.Empty&&held.Id!=output.Id)return;
                int amount=Math.Min(right?(output.Count+1)/2:output.Count,limit(output.Id)-(held.Empty?0:held.Count));
                if(amount<=0)return;contents.Take(2,amount);held=new ItemStack(output.Id,held.Count+amount);
            }
            else if(held.Empty||Accepts(slot,held.Id))contents.Click(slot,ref held,right);
            InputChanged();
        }
        public void TransferOut(int slot,ItemContainer destination){contents.TransferTo(slot,destination);InputChanged();}
        public void TransferIn(ItemContainer source,int sourceSlot)
        {
            var stack=source.Slots[sourceSlot];if(stack.Empty)return;
            // Logs are both ingredients and fuel; prefer input when shift-transferring.
            int slot=Accepts(0,stack.Id)?0:Accepts(1,stack.Id)?1:-1;
            if(slot>=0)source.TransferTo(sourceSlot,contents,slot,slot+1);InputChanged();
        }
        public ItemStack Take(int slot,int count){var result=contents.Take(slot,count);InputChanged();return result;}
        public void Advance(int ticks)
        {
            if(ticks<0)throw new ArgumentOutOfRangeException(nameof(ticks));
            InputChanged();
            while(ticks>0)
            {
                bool work=CanWork;
                if(BurnTicks==0)
                {
                    int fuel=registry.FuelTicks(Slots[1].Id);
                    if(!work||fuel==0)return;
                    contents.Take(1,1);FuelDuration=BurnTicks=fuel;
                }
                int step=Math.Min(ticks,BurnTicks);
                if(work)step=Math.Min(step,Recipe.Ticks-ProgressTicks);
                BurnTicks-=step;ticks-=step;
                if(!work)continue; // Already lit fuel burns even while the output is blocked.
                ProgressTicks+=step;
                if(ProgressTicks<Recipe.Ticks)continue;
                var recipe=Recipe;
                // Capacity and inputs were checked in this synchronous authority turn.
                contents.Add(recipe.Output.Id,recipe.Output.Count,2,3);contents.Take(0,recipe.Input.Count);
                ProgressTicks=0;InputChanged();
            }
        }
    }
}
