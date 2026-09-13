using System;

namespace RivetReach
{
    public sealed partial class MachineState
    {
        public bool IsCooker=>FarmId.CookerBlock(Definition.Id);
        public string CookingId="",CookingSignature="";
        internal readonly int[] CookingPlan;
        uint signatureItems=uint.MaxValue;string signatureRecipe,foodSignature;

        public CookingRecipe FoodRecipe=>IsCooker?CookingCatalog.Current.Find(CookingId):null;
        public int OutputSlot=>IsCooker?4:2;
        public bool CookerAccepts(int slot,byte id)
        {
            if(slot==3)return Definition.Id==FarmId.Cooker&&FuelTicks(id)>0;
            return slot>=0&&slot<3&&FoodRecipe!=null&&FoodRecipe.Matcher.Accepts(id);
        }
        bool InsertCooker(byte id,int face)
        {
            if(Definition.Id==FarmId.Cooker&&face==4)return CookerAccepts(3,id)&&Items.Add(id,1,3,4)==0;
            if(face<0&&CookerAccepts(3,id))return Items.Add(id,1,3,4)==0;
            if(!CookerAccepts(0,id))return false;
            return FoodRecipe.Matcher.CanStage(Items.Slots,id)&&Items.Add(id,1,0,3)==0;
        }
        bool PrefersCooking(byte id,int face)
        {
            if(!CookerAccepts(face==4&&Definition.Id==FarmId.Cooker?3:0,id))return false;
            for(int i=0;i<3;i++)if(Items.Slots[i].Id==id)return true;return false;
        }
        public void SelectCooking(string id)
        {
            if(!IsCooker||CookingCatalog.Current.Find(id)==null)throw new ArgumentException("Unknown food recipe.");
            if(CookingId==id)return;CookingId=id;Work=0;CookingSignature="";
        }
        public string FoodSignature
        {
            get
            {
                uint current=(uint)(Items.Slots[0].Id|Items.Slots[1].Id<<8|Items.Slots[2].Id<<16);
                if(current!=signatureItems||CookingId!=signatureRecipe)
                {signatureItems=current;signatureRecipe=CookingId;foodSignature=CookingId+":"+Items.Slots[0].Id+","+Items.Slots[1].Id+","+Items.Slots[2].Id;}
                return foodSignature;
            }
        }
    }
}
