using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        const int StationSlotStart=100,ArmorSlotStart=200;
        long StationRevision=>game.OpenMachine?.Items.Revision??game.OpenStation?.Furnace?.Revision??game.OpenStation?.Storage?.Revision??0;
        ItemStack StationStack(int slot)
        {
            if(game.OpenStation.Furnace!=null)return slot>=0&&slot<3?game.OpenStation.Furnace.Slots[slot]:default;
            var storage=game.OpenStation.Storage;return storage!=null&&slot>=0&&slot<storage.Count?storage.Slots[slot]:default;
        }
        void ResetSurvivalUI(){healthText=hungerText=armorText=furnaceText=null;cookBar=burnBar=null;shownHealth=float.NaN;shownFood=shownArmor=shownProgress=shownBurn=-1;shownFurnaceRevision=-1;}
        void BuildEquipment(Transform parent)
        {
            Label(parent,"ARMOR",28,392,200,24,14,gold);
            string[] labels={"HEAD","BODY","LEGS","FEET"};
            for(int i=0;i<4;i++){Slot(parent,ArmorSlotStart+i,28+i*53,423,46);Label(parent,labels[i],28+i*53,476,52,18,10,gold);}
            BuildSurvivalMeter(parent,SurvivalMeter.Kind.Armor,28,500,210,22);
        }
        void BuildSurvivalMeter(Transform parent,SurvivalMeter.Kind kind,float x,float y,float width,float height)
        {Rect(parent,kind+" icons",x,y,width,height).gameObject.AddComponent<SurvivalMeter>().Initialize(game,kind);}
        void BuildSurvivalHUD()
        {
            Panel(root,296,558,688,53,new Color(.045f,.08f,.085f,.70f));
            healthText=Label(root,"",304,580,335,30,24,new Color(.95f,.27f,.29f));
            BuildSurvivalMeter(root,SurvivalMeter.Kind.Food,655,583,322,26);
            BuildSurvivalMeter(root,SurvivalMeter.Kind.Armor,304,562,260,18);
            hungerText=Label(root,"",655,563,322,18,12,gold);hungerText.alignment=TextAnchor.UpperRight;
        }
        void BuildDeath()
        {
            Panel(root,0,0,1280,720,new Color(.17f,.025f,.03f,.70f));
            Label(root,"YOU DIED",450,220,400,55,42).alignment=TextAnchor.MiddleCenter;
            Label(root,"Your items remain where you fell.",390,290,520,40,20).alignment=TextAnchor.MiddleCenter;
            Button(root,"RESPAWN",490,366,300,55,game.Respawn,true);
            Button(root,"LOAD GAME",490,436,300,48,()=>game.SetMode(ScreenMode.Load));
            Button(root,"SAVE GAME",490,499,300,48,()=>game.SetMode(ScreenMode.Save));
            message=Label(root,"",310,575,660,70,18,gold);message.alignment=TextAnchor.MiddleCenter;
        }
        void BuildFurnace(Transform parent)
        {
            Label(parent,"FURNACE",821,115,300,42,27);
            Button(parent,"RECIPES & FUEL",821,164,282,32,()=>InspectBrowserItem(BlockId.Furnace,true));
            Label(parent,"INGREDIENT",821,213,120,24,13,gold);Slot(parent,StationSlotStart,837,242,60);
            Label(parent,"FUEL",821,356,100,24,13,gold);Slot(parent,StationSlotStart+1,837,384,60);
            Label(parent,"RESULT",1010,261,120,24,13,gold);Slot(parent,StationSlotStart+2,1021,290,72);
            var cookTrack=Panel(parent,925,314,72,10,slate);cookBar=Panel(cookTrack.transform,0,0,0,10,gold);
            var burnTrack=Panel(parent,837,327,60,8,slate);burnBar=Panel(burnTrack.transform,0,0,0,8,new Color(1,.40f,.13f));
            furnaceText=Label(parent,"",821,469,300,64,15,gold);
        }
        void BuildChest(Transform parent)
        {
            Label(parent,"CHEST",821,115,300,42,27);Label(parent,"27 slots · Shift-click to transfer",821,164,300,30,15,gold);
            for(int i=0;i<27;i++)Slot(parent,StationSlotStart+i,821+i%5*57,213+i/5*51,45);
        }
        bool ClickStationSlot(int index,bool right,bool shift)
        {
            if(index>=MachineSlotStart&&index<MachineSlotStart+3&&game.OpenMachine!=null){int slot=index-MachineSlotStart;if(shift&&HeldStack.Empty)game.OpenMachine.Items.TransferTo(slot,game.Inventory);else game.OpenMachine.Click(slot,ref HeldStack,right);return true;}
            if(index>=ArmorSlotStart&&index<ArmorSlotStart+4)
            {
                int slot=index-ArmorSlotStart;
                if(shift&&HeldStack.Empty)game.Equipment.TransferOut(slot,game.Inventory);else game.Equipment.Click(slot,ref HeldStack,right);
                return true;
            }
            if(index<StationSlotStart||game.OpenStation==null)return false;
            int cell=index-StationSlotStart;var station=game.OpenStation;
            if(station.Furnace!=null&&cell<3)
            {
                if(shift&&HeldStack.Empty)station.Furnace.TransferOut(cell,game.Inventory);else station.Furnace.Click(cell,ref HeldStack,right);
                game.Survival.Wake(game.StationPosition);return true;
            }
            if(station.Storage!=null&&cell<station.Storage.Count)
            {
                if(shift&&HeldStack.Empty)station.Storage.TransferTo(cell,game.Inventory);else station.Storage.Click(cell,ref HeldStack,right);
                return true;
            }
            return false;
        }
        void QuickTransferInventory(int index)
        {
            var stack=game.Inventory.Slots[index];if(stack.Empty)return;
            if(game.OpenMachine!=null)game.OpenMachine.TransferIn(game.Inventory,index);
            else if(game.OpenStation?.Furnace!=null){game.OpenStation.Furnace.TransferIn(game.Inventory,index);game.Survival.Wake(game.StationPosition);}
            else if(game.OpenStation?.Storage!=null)game.Inventory.TransferTo(index,game.OpenStation.Storage);
            else if(game.Registry.Get(stack.Id).armorSlot!=ArmorSlot.None)game.Equipment.TransferIn(game.Inventory,index);
            else game.Inventory.QuickTransfer(index);
        }
        void RefreshSurvival()
        {
            if(healthText!=null&&shownHealth!=game.Health.Hearts)
            {
                shownHealth=game.Health.Hearts;
                string text="";for(int i=0;i<10;i++)text+=(game.Health.Hearts>=i*2+2?"<color=#ef5359>♥</color>":game.Health.Hearts>i*2?"<color=#f49b9e>♥</color>":"<color=#523c45>♥</color>");
                healthText.text=text;
            }
            bool eating=game.Player.EatingProgress>0;
            if(hungerText!=null&&(shownFood!=game.Hunger.Food||shownEating!=eating))
            {shownFood=game.Hunger.Food;shownEating=eating;hungerText.text=eating?"Eating…":game.Creative?"Frozen":game.Hunger.CanSprint?"":"Eat to sprint";}
            var furnace=game.OpenStation?.Furnace;
            if(furnaceText!=null&&furnace!=null&&(shownProgress!=furnace.ProgressTicks||shownBurn!=furnace.BurnTicks||shownFurnaceRevision!=furnace.Revision))
            {
                shownProgress=furnace.ProgressTicks;shownBurn=furnace.BurnTicks;shownFurnaceRevision=furnace.Revision;
                furnaceText.text=furnace.Status+"\n"+(furnace.ProgressTicks/20f).ToString("0.0")+" s / "+((furnace.Recipe?.Ticks??200)/20f).ToString("0.0")+" s";
                cookBar.rectTransform.sizeDelta=new Vector2(72*furnace.ProgressTicks/(furnace.Recipe?.Ticks??200),10);
                burnBar.rectTransform.sizeDelta=new Vector2(furnace.FuelDuration==0?0:60f*furnace.BurnTicks/furnace.FuelDuration,8);
            }
        }
    }
}
