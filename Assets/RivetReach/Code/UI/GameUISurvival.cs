using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        const int StationSlotStart=100,ArmorSlotStart=200;
        long lastStationRevision=-1,lastEquipmentRevision=-1;
        float shownHealth=float.NaN;
        int shownFood=-1,shownArmor=-1,shownProgress=-1,shownBurn=-1;
        long shownFurnaceRevision=-1;
        bool shownEating;
        Text healthText,hungerText,armorText,furnaceText;
        Image cookBar,burnBar;
        long StationRevision=>game.OpenStation?.Furnace?.Revision??game.OpenStation?.Storage?.Revision??0;
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
            armorText=Label(parent,"",28,500,210,24,13,gold);
        }
        void BuildSurvivalHUD()
        {
            Panel(root,296,558,688,53,new Color(.045f,.08f,.085f,.70f));
            healthText=Label(root,"",304,580,335,30,24,new Color(.95f,.27f,.29f));
            hungerText=Label(root,"",655,586,322,26,16,gold);hungerText.alignment=TextAnchor.UpperRight;
            armorText=Label(root,"",304,563,350,22,13,new Color(.66f,.80f,.87f));
        }
        void BuildDeath()
        {
            Panel(root,0,0,1280,720,new Color(.17f,.025f,.03f,.70f));
            Label(root,"YOU DIED",450,220,400,55,42).alignment=TextAnchor.MiddleCenter;
            Label(root,"Your items remain where you fell.",390,290,520,40,20).alignment=TextAnchor.MiddleCenter;
            Button(root,"RESPAWN",490,366,300,55,game.Respawn,true);
            message=Label(root,"",310,450,660,70,18,gold);message.alignment=TextAnchor.MiddleCenter;
        }
        void BuildFurnace(Transform parent)
        {
            Label(parent,"FURNACE",821,115,300,42,27);
            GameObject guide=null;
            Button(parent,"RECIPES & FUEL",821,164,282,32,()=>guide.SetActive(!guide.activeSelf));
            Label(parent,"INGREDIENT",821,213,120,24,13,gold);Slot(parent,StationSlotStart,837,242,60);
            Label(parent,"FUEL",821,356,100,24,13,gold);Slot(parent,StationSlotStart+1,837,384,60);
            Label(parent,"RESULT",1010,261,120,24,13,gold);Slot(parent,StationSlotStart+2,1021,290,72);
            var cookTrack=Panel(parent,925,314,72,10,slate);cookBar=Panel(cookTrack.transform,0,0,0,10,gold);
            var burnTrack=Panel(parent,837,327,60,8,slate);burnBar=Panel(burnTrack.transform,0,0,0,8,new Color(1,.40f,.13f));
            furnaceText=Label(parent,"",821,469,300,64,15,gold);
            guide=Panel(parent,816,204,329,330,ink).gameObject;
            Label(guide.transform,"10 seconds per item",12,10,305,24,17,gold);
            int row=0;
            foreach(var recipe in game.Processing.Recipes)
                Label(guide.transform,game.Registry.Get(recipe.Input.Id).displayName+" → "+game.Registry.Get(recipe.Output.Id).displayName,12,46+row++*28,305,26,14);
            Label(guide.transform,"Coal / charcoal: 8 items\nLog / plank: 1½ items · Stick: ½ item\nBurning fuel runs down while idle.\nDrag logs to FUEL to burn them.",12,226,305,94,14,gold);
            guide.SetActive(false);
        }
        void BuildChest(Transform parent)
        {
            Label(parent,"CHEST",821,115,300,42,27);Label(parent,"27 slots · Shift-click to transfer",821,164,300,30,15,gold);
            for(int i=0;i<27;i++)Slot(parent,StationSlotStart+i,821+i%5*57,213+i/5*51,45);
        }
        bool ClickStationSlot(int index,bool right,bool shift)
        {
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
            if(game.OpenStation?.Furnace!=null){game.OpenStation.Furnace.TransferIn(game.Inventory,index);game.Survival.Wake(game.StationPosition);}
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
            {shownFood=game.Hunger.Food;shownEating=eating;hungerText.text="FOOD "+shownFood+" / 20"+(eating?" · Eating…":game.Creative?" · Frozen":game.Hunger.CanSprint?"":" · Eat to sprint");}
            if(armorText!=null&&shownArmor!=game.Equipment.Protection){shownArmor=game.Equipment.Protection;armorText.text="ARMOR "+shownArmor+" / 20";}
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
