using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        int cratePage;
        CrateEndpoint OpenCrates=>game.OpenStation!=null&&CrateId.Part(game.OpenStation.Block)?game.Crates.At(game.StationPosition):null;
        void BuildCrate(Transform parent,bool controller)
        {
            Label(parent,controller?"CRATE CONTROLLER":"BULK CRATE",821,115,300,42,24);
            currentStation.crateStatus=Label(parent,"",821,164,292,70,14,gold);
            if(controller)
            {
                for(int i=0;i<16;i++)Slot(parent,StationSlotStart+i,821+i%4*62,244+i/4*58,50);
                Button(parent,"‹",821,487,50,30,()=>{if(!HasInventoryBinding)return;cratePage=Mathf.Max(0,cratePage-1);RefreshSlots();RefreshCrates();});
                Button(parent,"›",1045,487,50,30,()=>{if(!HasInventoryBinding)return;cratePage=Mathf.Min(3,cratePage+1);RefreshSlots();RefreshCrates();});
                currentStation.cratePage=Label(parent,"",878,490,160,25,14,gold);
            }
            else
            {
                Slot(parent,StationSlotStart,915,250,72);
                Button(parent,"LOCK TO ITEM",821,351,274,38,()=>
                {
                    if(!HasInventoryBinding)return;var store=game.OpenStation?.Crate;if(store==null)return;
                    byte item=store.Item!=0?store.Item:!HeldStack.Empty?HeldStack.Id:game.Inventory.Slots[game.Selected].Id;
                    if(!store.Lock(item))game.Notify("Hold an item on the cursor or select it in your hotbar to set the lock.",5);
                    RefreshSlots();RefreshCrates();
                });
                Button(parent,"UNLOCK",821,402,274,38,()=>{if(!HasInventoryBinding)return;game.OpenStation?.Crate?.Unlock();RefreshSlots();RefreshCrates();});
            }
            Label(parent,"Click: move a stack · Right-click: half / one\nShift-click: transfer · Empty before mining\nFilled batteries/tanks belong in a chest.",821,532,300,54,12,gold);
        }
        void RefreshCrates()
        {
            var endpoint=OpenCrates;if(endpoint==null||currentStation.crateStatus==null)return;
            if(game.OpenStation.Crate is CrateStorage store)
                currentStation.crateStatus.text=(store.Item==0?"Unassigned":game.Registry.Get(store.Item).displayName)+$"\n{store.Count:N0} / {CrateStorage.Capacity:N0}"+(store.Locked?" · Locked":" · Unlocked");
            else
            {
                long count=0;foreach(var member in endpoint.Members)count+=member.store.Count;
                currentStation.crateStatus.text=$"{endpoint.Members.Count} / 64 connected crates\n{count:N0} / {(long)endpoint.Members.Count*CrateStorage.Capacity:N0} items\n"+endpoint.Status;
                currentStation.cratePage.text=$"Page {cratePage+1} / 4";
            }
        }
        ItemStack CrateStack(int slot)
        {
            var endpoint=OpenCrates;if(endpoint==null)return default;
            var store=endpoint.Store((game.OpenStation.Block==CrateId.Controller?cratePage*16:0)+slot);return store?.Stack??default;
        }
        void ClickCrate(int slot,bool right,bool shift)
        {
            var endpoint=OpenCrates;if(endpoint==null)return;
            int index=(game.OpenStation.Block==CrateId.Controller?cratePage*16:0)+slot;var store=endpoint.Store(index);
            if(!HeldStack.Empty)
            {
                if(HeldStack.HasContents){game.Notify("Use a chest for batteries or tanks with stored contents.",4);return;}
                int amount=right?1:HeldStack.Count;int moved=endpoint.Insert(HeldStack.WithCount(amount));HeldStack=HeldStack.WithCount(HeldStack.Count-moved);
            }
            else if(store!=null&&store.Count>0)
            {
                int amount=shift?Mathf.Min(store.Count,game.Inventory.Capacity(store.Item)):right?(Mathf.Min(store.Count,game.Registry.Get(store.Item).stackLimit)+1)/2:game.Registry.Get(store.Item).stackLimit;
                if(shift)
                {
                    // Capacity was measured synchronously; publish only the accepted quantity.
                    int left=game.Inventory.Add(store.Item,amount);store.Take(amount-left);
                }
                else HeldStack=store.TakeStack(amount);
            }
            RefreshCrates();
        }
        void TransferIntoCrate(int index)
        {
            var stack=game.Inventory.Slots[index];if(stack.HasContents){game.Notify("Use a chest for batteries or tanks with stored contents.",4);return;}
            int amount=OpenCrates?.Insert(stack)??0;if(amount>0)game.Inventory.Take(index,amount);RefreshCrates();
        }
    }
}
