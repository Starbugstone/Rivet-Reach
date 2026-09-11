using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        const int CraftCell=Inventory.SlotCount, CraftResultSlot=Inventory.SlotCount+16;
        SlotView CraftView(int index)=>game.UI.VisibleRoot.GetComponentsInChildren<SlotView>().Single(v=>v.Index==index);
        Vector2 CraftPoint(int index)
        {
            var rect=CraftView(index).GetComponent<RectTransform>();
            return RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        }
        IEnumerator ClickCraftUI(int index,bool right=false,bool shift=false)
        {
            var point=CraftPoint(index);
            InputSystem.QueueStateEvent(Keyboard.current,shift?new KeyboardState(Key.LeftShift):new KeyboardState());
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return null;yield return null;
            var raycast=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},raycast);
            Check(raycast.Count>0&&raycast[0].gameObject.GetComponentInParent<SlotView>()==CraftView(index),"Pointer ray reaches slot "+index);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point}.WithButton(right?MouseButton.Right:MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return null;yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
        }
        IEnumerator DragCraftUI(int source,int destination)
        {
            var from=CraftPoint(source);var to=CraftPoint(destination);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=from});yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=from}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=Vector2.Lerp(from,to,.5f)}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=to}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=to});yield return null;yield return null;
        }
        IEnumerator ReviewCrafting()
        {
            var original=game.Inventory.Slots.ToArray();
            game.SetMode(ScreenMode.Inventory);yield return null;yield return null;
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            game.Inventory.Add(BlockId.Log,8,12,13);
            Check(game.Crafting.Grid.Size==2&&game.Recipes.Recipes.Count>=52,"Personal grid loads the modular progression catalog");
            Check(!CraftView(CraftResultSlot).Icon.enabled,"Empty grid has no claimable result");
            yield return ClickCraftUI(12);yield return ClickCraftUI(CraftCell+3,true);yield return ClickCraftUI(12);
            Check(game.Crafting.Preview?.Output.Id==BlockId.Planks,"A log in an offset personal cell produces planks");
            yield return DragCraftUI(CraftResultSlot,13);
            Check(game.Inventory.Slots[13].Id==BlockId.Planks&&game.Inventory.Slots[13].Count==4&&game.UI.HeldStack.Empty,"Dragging a result crafts one complete bundle");
            yield return ClickCraftUI(13);
            for(int i=0;i<4;i++)yield return ClickCraftUI(CraftCell+i,true);
            Check(game.Crafting.Preview?.Output.Id==BlockId.Workbench,"Four planks preview a workbench");
            yield return Capture("personal-workbench-ready");
            game.UI.InspectBrowserItem(BlockId.Workbench,false);yield return null;
            Check(game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text=="Personal crafting · 2 × 2"),"Recipe browser shows the personal workbench layout");
            yield return Capture("personal-recipe-browser");game.UI.CloseBrowserRecipe();yield return null;
            yield return ClickCraftUI(CraftResultSlot);yield return ClickCraftUI(14);
            Check(game.Inventory.Total(BlockId.Workbench)==1&&game.Crafting.Preview==null,"One workbench consumes four planks");
            yield return DragCraftUI(12,CraftCell);yield return ClickCraftUI(14);yield return ClickCraftUI(CraftResultSlot,false,true);
            Check(game.Inventory.Total(BlockId.Planks)==28&&game.Crafting.Grid.Total(BlockId.Log)==0,"Shift result batches all seven remaining logs");
            Check(game.UI.HeldStack.Id==BlockId.Workbench&&game.UI.HeldStack.Count==1,"Personal Shift crafting leaves an unrelated cursor item unchanged");
            yield return ClickCraftUI(14);
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            game.Inventory.Add(BlockId.Planks,game.Registry.Get(BlockId.Planks).stackLimit,12,13);
            yield return ClickCraftUI(12);
            game.Crafting.Grid.Add(BlockId.Log,1);
            yield return ClickCraftUI(CraftResultSlot,false,true);
            Check(game.Inventory.Total(BlockId.Planks)==4&&game.Crafting.Grid.Total(BlockId.Log)==0&&game.UI.HeldStack.Id==BlockId.Planks&&game.UI.HeldStack.Count==game.Registry.Get(BlockId.Planks).stackLimit,"Shift crafting sends output to inventory even when the matching cursor stack is full");
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            game.Inventory.Add(BlockId.Dirt,64*60);game.Crafting.Grid.Add(BlockId.Log,1,3,4);
            yield return null;yield return ClickCraftUI(CraftResultSlot,false,true);
            Check(game.Crafting.Grid.Total(BlockId.Log)==1&&game.Inventory.Total(BlockId.Planks)==0,"Full inventory rejects crafting without consumption");
            Check(game.UI.HeldStack.Id==BlockId.Planks&&game.UI.HeldStack.Count==game.Registry.Get(BlockId.Planks).stackLimit,"Blocked Shift crafting preserves the held stack");
            game.Inventory.Take(0,int.MaxValue);yield return ClickCraftUI(0);
            game.SetMode(ScreenMode.Play);yield return null;game.SetMode(ScreenMode.Inventory);yield return null;yield return null;
            Check(game.Crafting.Grid.Total(BlockId.Log)==1&&game.Crafting.Preview!=null,"Close/reopen retains ingredients when storage is full");
            game.UI.Rebuild();yield return null;yield return null;
            Check(game.Crafting.Preview?.Output.Id==BlockId.Planks,"Rebuilding the interface preserves the recipe authority");
            yield return Capture("personal-full-inventory");
            game.Inventory.Take(0,int.MaxValue);game.SetMode(ScreenMode.Play);yield return null;
            Check(game.Crafting.Grid.Total(BlockId.Log)==0&&game.Inventory.Total(BlockId.Log)==1,"Closing returns ingredients exactly once when space exists");
            game.Crafting.Grid.Add(BlockId.Log,1);game.UI.ClickSlot(CraftResultSlot,false,false);
            Check(game.UI.HeldStack.Empty&&game.Crafting.Grid.Total(BlockId.Log)==1,"Stale UI handlers cannot craft during gameplay");
            for(int i=0;i<game.Crafting.Grid.Count;i++)game.Crafting.Grid.Take(i,int.MaxValue);
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            for(int i=0;i<original.Length;i++)if(!original[i].Empty)game.Inventory.Add(original[i].Id,original[i].Count,i,i+1);
            Check(game.Inventory.Slots.SequenceEqual(original),"Crafting verification restores the original inventory");
        }
    }
}
