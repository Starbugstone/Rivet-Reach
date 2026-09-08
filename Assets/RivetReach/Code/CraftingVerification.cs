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
        SlotView CraftView(int index)=>game.UI.GetComponentsInChildren<SlotView>().Single(v=>v.Index==index);
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
            game.Inventory.Add(BlockId.Stone,8,12,13);game.Inventory.Add(BlockId.Log,8,13,14);
            Check(game.Crafting.Grid.Size==2&&game.Recipes.Recipes.Count==3,"Personal 2x2 grid loads three authored starter recipes");
            Check(CraftView(CraftResultSlot).Icon.enabled==false,"Empty grid has no claimable result");
            yield return ClickCraftUI(12);
            yield return ClickCraftUI(CraftCell,true);yield return ClickCraftUI(CraftCell,true);
            yield return ClickCraftUI(CraftCell+1,true);yield return ClickCraftUI(CraftCell+1,true);
            yield return ClickCraftUI(12);
            yield return ClickCraftUI(13);yield return ClickCraftUI(CraftCell+3,true);yield return ClickCraftUI(CraftCell+3,true);yield return ClickCraftUI(13);
            Check(game.UI.HeldStack.Empty&&game.Crafting.Preview?.Output.Id==BlockId.StarterAxe&&game.Crafting.MaximumCrafts==2,"Right-place ingredients updates the visible axe result");
            Check(CraftView(CraftResultSlot).Icon.enabled&&CraftView(CraftResultSlot).Count.text=="1","Result icon and output quantity are visible");
            yield return Capture("crafting-axe-ready");
            var guideButton=game.UI.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.GetComponentInChildren<UnityEngine.UI.Text>().text=="RECIPES");
            guideButton.onClick.Invoke();yield return null;
            Check(game.UI.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text=="PERSONAL RECIPES"),"Recipe guide opens from the compiled catalog");
            yield return Capture("crafting-recipe-guide");guideButton.onClick.Invoke();yield return null;
            yield return ClickCraftUI(CraftResultSlot);
            Check(game.UI.HeldStack.Id==BlockId.StarterAxe&&game.UI.HeldStack.Count==1&&game.Crafting.Grid.Total(BlockId.Stone)==2,"Click result consumes one complete recipe into cursor");
            yield return ClickCraftUI(CraftResultSlot);
            Check(game.Crafting.Grid.Total(BlockId.Stone)==2&&game.UI.HeldStack.Count==1,"Full tool cursor cannot duplicate or consume another craft");
            yield return ClickCraftUI(14);yield return ClickCraftUI(CraftResultSlot,false,true);
            Check(game.Inventory.Total(BlockId.StarterAxe)==2&&game.Crafting.Preview==null&&game.Crafting.Grid.Total(BlockId.Log)==0,"Shift-result crafts remaining recipe to inventory and clears preview");
            yield return Capture("crafting-axe-complete");

            yield return ClickCraftUI(12);yield return ClickCraftUI(CraftCell,true);yield return ClickCraftUI(12);
            yield return ClickCraftUI(13);yield return ClickCraftUI(CraftCell+2,true);yield return ClickCraftUI(13);
            Check(game.Crafting.Preview?.Output.Id==BlockId.StarterDagger,"Shapeless dagger accepts separated vertical cells");
            yield return DragCraftUI(CraftResultSlot,16);
            Check(game.UI.HeldStack.Empty&&game.Inventory.Slots[16].Id==BlockId.StarterDagger&&game.Crafting.Preview==null,"Dragging the result crafts exactly once and delivers it to the target slot");
            yield return DragCraftUI(12,CraftCell+1);
            Check(game.Inventory.Slots[12].Empty&&game.Crafting.Grid.Slots[1].Id==BlockId.Stone,"Dragging inventory ingredients into grid uses real slots");
            yield return ClickCraftUI(CraftCell+1,false,true);
            Check(game.Crafting.Grid.Slots[1].Empty&&game.Inventory.Total(BlockId.Stone)==3,"Shift ingredient returns its whole stack to inventory");

            // Full destination: opening/closing and rebuilding UI must retain every ingredient.
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            game.Inventory.Add(BlockId.Dirt,30000);
            game.Crafting.Grid.Add(BlockId.Stone,1,0,1);game.Crafting.Grid.Add(BlockId.Log,1,3,4);
            yield return null;yield return ClickCraftUI(CraftResultSlot,false,true);
            Check(game.Crafting.Grid.Total(BlockId.Stone)==1&&game.Crafting.Grid.Total(BlockId.Log)==1&&game.Inventory.Total(BlockId.StarterDagger)==0,"Full inventory rejects output without consuming inputs");
            yield return Capture("crafting-inventory-full");
            game.SetMode(ScreenMode.Play);yield return null;game.SetMode(ScreenMode.Inventory);yield return null;yield return null;
            Check(game.Crafting.Grid.Total(BlockId.Stone)==1&&game.Crafting.Grid.Total(BlockId.Log)==1&&game.Crafting.Preview!=null,"Close/reopen keeps ingredients when return storage is full");
            game.UI.Rebuild();yield return null;yield return null;
            Check(game.Crafting.Preview?.Output.Id==BlockId.StarterDagger,"UI rebuilding leaves authority state intact");
            game.Inventory.Take(0,int.MaxValue);game.Inventory.Take(1,int.MaxValue);
            game.SetMode(ScreenMode.Play);yield return null;
            Check(game.Crafting.Grid.Total(BlockId.Stone)==0&&game.Crafting.Grid.Total(BlockId.Log)==0&&game.Inventory.Total(BlockId.Stone)==1&&game.Inventory.Total(BlockId.Log)==1,"Closing returns ingredients once space exists");

            // Output input is rejected outside inventory even if an old slot handler is invoked.
            game.Crafting.Grid.Add(BlockId.Stone,1,0,1);game.Crafting.Grid.Add(BlockId.Log,1,3,4);
            game.UI.ClickSlot(CraftResultSlot,false,false);
            Check(game.UI.HeldStack.Empty&&game.Crafting.Grid.Total(BlockId.Stone)==1,"Crafting UI cannot execute while playing");
            game.Crafting.Grid.Take(0,int.MaxValue);game.Crafting.Grid.Take(3,int.MaxValue);
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            for(int i=0;i<original.Length;i++)if(!original[i].Empty)game.Inventory.Add(original[i].Id,original[i].Count,i,i+1);
            Check(game.Inventory.Slots.SequenceEqual(original),"Runtime verification restores original inventory");
        }
    }
}
