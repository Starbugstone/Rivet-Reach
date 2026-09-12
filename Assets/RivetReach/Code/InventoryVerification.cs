using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewInventoryExpansion()
        {
            game.SetCreative(true);game.Diagnostics=false;
            Check(game.Inventory.Count==71&&game.Inventory.Slots.All(s=>s.Empty),"New session has 71 empty slots");
            game.Selected=0;
            yield return CreativeHold(.06f,game.Input.Keys["Previous slot"]);
            Check(game.Selected==14,"Previous wraps from slot 1 to slot 15");
            yield return CreativeHold(.06f,game.Input.Keys["Next slot"]);
            Check(game.Selected==0,"Next wraps from slot 15 to slot 1");
            for(int i=1;i<=15;i++)
            {
                yield return CreativeHold(.04f,game.Input.Keys["Next slot"]);
                Check(game.Selected==i%15,"Next action reaches hotbar slot "+(game.Selected+1));
            }
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{scroll=new Vector2(0,120)});yield return null;yield return null;
            Check(game.Selected==14,"Mouse wheel wraps through slot 15");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            yield return CreativeHold(.06f,Key.Digit0);Check(game.Selected==9,"Zero still selects slot 10");
            yield return CreativeHold(.06f,Key.Digit1);Check(game.Selected==0,"One still selects slot 1");
            game.SetMode(ScreenMode.Inventory);yield return null;
            var views=game.UI.VisibleRoot.GetComponentsInChildren<SlotView>().Where(v=>v.Index<Inventory.SlotCount).ToArray();
            Check(views.Length==71&&views.Select(v=>v.Index).Distinct().Count()==71,"Inventory renders all 71 slots exactly once");
            foreach(var view in views)
            {
                var rect=(RectTransform)view.transform;var corners=new Vector3[4];rect.GetWorldCorners(corners);
                Check(corners.All(p=>p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height),"Slot on screen: "+view.Index);
            }
            yield return CreativeDragTo(BlockId.Planks,CraftPoint(70));
            Check(game.Inventory.Slots[70].Count==64,"Catalog pointer drag fills the last slot of row seven");
            yield return DragCraftUI(70,14);
            Check(game.Inventory.Slots[70].Empty&&game.Inventory.Slots[14].Count==64,"Pointer drag moves row-seven stack to hotbar slot 15");
            for(int i=15;i<70;i++)game.Inventory.Add(BlockId.Dirt,64,i,i+1);
            yield return ClickCraftUI(14,false,true);
            Check(game.Inventory.Slots[14].Empty&&game.Inventory.Slots[70].Count==64,"Shift-click reaches the final backpack slot when other slots are full");
            game.UI.ClickSlot(70,false,true);
            Check(game.Inventory.Slots[0].Id==BlockId.Planks&&game.Inventory.Slots[70].Empty,"Backpack quick transfer returns to hotbar");
            for(int i=0;i<71;i++){game.Inventory.Take(i,int.MaxValue);game.Inventory.Add(BlockId.Planks,1+i%64,i,i+1);}
            yield return null;yield return Capture("inventory-seven-rows");
            var expected=game.Inventory.Slots.ToArray();game.Selected=14;game.SetMode(ScreenMode.Pause);
            game.InitializeSaves(Path.Combine(output,"Saves"));
            Check(game.SaveGame("Expanded inventory",true),"Save expanded inventory with slot 15 selected: "+game.SaveStatus);
            var entry=game.Saves.List().First();game.Inventory.Take(70,int.MaxValue);
            Check(game.LoadGame(entry),"Load expanded inventory: "+game.SaveStatus);
            game.SetMode(ScreenMode.Pause);yield return Settle();
            Check(game.Selected==14&&game.Inventory.Slots.Select((s,i)=>s.Id==expected[i].Id&&s.Count==expected[i].Count).All(ok=>ok),"All 71 exact stacks and selected slot 15 survive load");
            game.SetMode(ScreenMode.Play);yield return null;
            Check(game.UI.VisibleRoot.GetComponentsInChildren<SlotView>().Count(v=>v.Index<Inventory.HotbarCount)==15,"HUD renders all 15 hotbar slots");
            yield return Capture("hotbar-fifteen-slots");
            game.SetMode(ScreenMode.Inventory);yield return Capture("inventory-restored");
        }
    }
}
