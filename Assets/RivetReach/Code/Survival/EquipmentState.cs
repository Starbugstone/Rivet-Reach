using System;
using System.Collections.Generic;

namespace RivetReach
{
    public sealed class EquipmentState
    {
        readonly ItemContainer contents;
        readonly Func<byte,ItemDefinition> definition;
        public IReadOnlyList<ItemStack> Slots=>contents.Slots;
        public long Revision=>contents.Revision;
        public int Protection
        {
            get {int total=0;foreach(var stack in Slots)if(!stack.Empty)total+=definition(stack.Id).armorPoints;return Math.Min(20,total);}
        }
        public EquipmentState(Func<byte,ItemDefinition> definition)
        {this.definition=definition;contents=new ItemContainer(4,id=>1);}
        public void Click(int slot,ref ItemStack held,bool right)
        {if(held.Empty||(int)definition(held.Id).armorSlot==slot+1)contents.Click(slot,ref held,right);}
        public void TransferIn(ItemContainer source,int index)
        {
            var stack=source.Slots[index];if(stack.Empty)return;int slot=(int)definition(stack.Id).armorSlot-1;
            if(slot>=0&&slot<4)source.TransferTo(index,contents,slot,slot+1);
        }
        public void TransferOut(int slot,ItemContainer destination)=>contents.TransferTo(slot,destination);
        public ItemStack Take(int slot)=>contents.Take(slot,1);
    }
    public enum DamageKind { Impact, Fall, Starvation }
    public sealed class HealthState
    {
        public const float Maximum=20;
        public float Hearts {get;private set;}=Maximum;
        public bool Dead=>Hearts<=0;
        int foodTimer;
        public float Damage(float amount,DamageKind kind,int protection)
        {
            if(float.IsNaN(amount)||float.IsInfinity(amount)||amount<0)throw new ArgumentOutOfRangeException(nameof(amount));
            if(kind==DamageKind.Impact)amount*=1-Math.Clamp(protection,0,20)*.04f;
            float floor=kind==DamageKind.Starvation?1:0;
            float taken=Math.Min(amount,Math.Max(0,Hearts-floor));Hearts-=taken;return taken;
        }
        public void Advance(int ticks,HungerState hunger)
        {
            if(ticks<0)throw new ArgumentOutOfRangeException(nameof(ticks));
            if(Dead||hunger.Food>0&&(hunger.Food<18||Hearts>=Maximum)){foodTimer=0;return;}
            foodTimer+=ticks;
            while(foodTimer>=80&&!Dead)
            {
                foodTimer-=80;
                if(hunger.Food==0)Damage(1,DamageKind.Starvation,0);
                else if(hunger.Food>=18&&Hearts<Maximum){Hearts=Math.Min(Maximum,Hearts+1);hunger.Exert(6);}
            }
        }
        public void Respawn(){Hearts=Maximum;foodTimer=0;}
    }
}
