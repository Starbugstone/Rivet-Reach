using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewScreenReuse()
        {
            game.StartSession(246813);game.World.ViewDistance=4;game.Mobs.NaturalSpawning=false;yield return Settle();
            game.SetMode(ScreenMode.Inventory);yield return null;
            var inventoryRoot=game.UI.VisibleRoot;
            var backpack=CraftView(12);var sidebar=game.UI.VisibleRoot.GetComponentsInChildren<Transform>().Single(t=>t.name=="Item browser");
            var search=sidebar.GetComponentInChildren<InputField>();search.text="workbench";
            int created=game.UI.CreatedWidgets;
            for(int i=0;i<100;i++)
            {
                game.SetMode(ScreenMode.Play);game.SetMode(ScreenMode.Inventory);
                Check(CraftView(12)==backpack && game.UI.HasInventoryBinding,"Retained backpack rebound on open "+i);
                Check(game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Count(v=>v.CatalogSource)==1,"Search retains one visible matching item on open "+i);
            }
            Check(game.UI.CreatedWidgets==created,"100 warmed opens create zero UI widgets");
            search.text="log";
            var oldBrowserItem=game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().First(v=>v.CatalogSource);
            var oldBrowserPress=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            oldBrowserItem.OnPointerDown(oldBrowserPress);
            search.text="workbench";oldBrowserItem.OnPointerClick(oldBrowserPress);
            Check(!game.UI.VisibleRoot.GetComponentsInChildren<Transform>().Any(t=>t.name=="Recipe detail"),
                "Search rebinding cancels the old icon press instead of opening a phantom recipe");
            search.text="";
            Check(game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Where(v=>v.CatalogSource).All(v=>v.Item!=0&&v.Icon.enabled&&v.Icon.texture!=null),
                "Clearing search restores every visible sidebar icon after empty cells were cleared");
            game.SetMode(ScreenMode.Play);
            var hits=new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=new Vector2(300,300)},hits);
            Check(inventoryRoot.GetComponentsInChildren<Canvas>(true).All(v=>!v.enabled) &&
                inventoryRoot.GetComponentsInChildren<GraphicRaycaster>(true).All(v=>!v.enabled) &&
                inventoryRoot.GetComponentsInChildren<Selectable>().All(v=>!v.enabled) && hits.All(h=>!h.gameObject.transform.IsChildOf(inventoryRoot)),
                "Hidden inventory has no rendering, raycasting or keyboard-selectable controls");
            game.SetMode(ScreenMode.Inventory);
            Check(inventoryRoot.GetComponentsInChildren<Canvas>(true).All(v=>v.enabled) &&
                inventoryRoot.GetComponentsInChildren<GraphicRaycaster>(true).All(v=>v.enabled),
                "Reopening restores all inventory canvas regions and their pointer input");
            // Coroutine resumes after Update; a transaction here must reach the cursor
            // before rendering, even when EventSystem ran after the UI's Update.
            yield return null;
            game.Inventory.Add(BlockId.Log,3,12,13);game.UI.ClickSlot(12,false,false);
            yield return new WaitForEndOfFrame();
            var held=inventoryRoot.GetComponentsInChildren<RectTransform>().Single(t=>t.name=="Held stack");
            Check(held.gameObject.activeInHierarchy&&held.GetComponentInChildren<Text>().text=="3"&&held.GetComponent<RawImage>().texture!=null,
                "A stack picked up after Update is visible with its count in the same rendered frame");
            RectTransformUtility.ScreenPointToLocalPointInRectangle(inventoryRoot,Mouse.current.position.ReadValue(),null,out var cursorPoint);
            Check(Vector2.Distance((Vector2)held.localPosition,cursorPoint+new Vector2(10,-10))<.01f,
                "Held stack uses the current pointer position without interpolation");
            yield return null;game.UI.ClickSlot(12,false,false);
            yield return new WaitForEndOfFrame();
            Check(!held.gameObject.activeSelf,"Depositing a stack hides its cursor image in the same rendered frame");
            game.Inventory.Take(12,3);
            yield return null;
            // Complete a native mouse press only after the same slot has been rebound.
            var point=CraftPoint(12);game.Inventory.Add(BlockId.Log,3,12,13);yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point}.WithButton(MouseButton.Left));yield return null;yield return null;
            game.SetMode(ScreenMode.Play);game.SetMode(ScreenMode.Inventory);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return null;yield return null;
            Check(game.UI.HeldStack.Empty && game.Inventory.Total(BlockId.Log)==3,"Native release after rebinding cannot pick up or deposit a stale slot");
            // Begin a real drag so closing must return the cursor exactly once; its old
            // release then lands over a valid slot in the reused inventory.
            point=CraftPoint(12);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point}.WithButton(MouseButton.Left));yield return null;yield return null;
            var destination=CraftPoint(13);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=destination}.WithButton(MouseButton.Left));yield return null;yield return null;
            Check(game.UI.HeldStack.Id==BlockId.Log && game.UI.HeldStack.Count==3,"Native left drag acquires the actual stack before cancellation");
            game.SetMode(ScreenMode.Play);game.SetMode(ScreenMode.Inventory);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=destination});yield return null;yield return null;
            Check(game.UI.HeldStack.Empty && game.Inventory.Total(BlockId.Log)==3 && game.Inventory.Slots[13].Empty,
                "Canceled drag returns items once and its release cannot deposit into the rebound view");
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            game.UI.HeldStack=new ItemStack(BlockId.Planks,3);point=CraftPoint(CraftCell);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point}.WithButton(MouseButton.Right));yield return null;yield return null;
            Check(game.Crafting.Grid.Total(BlockId.Planks)==1&&game.UI.HeldStack.Count==2,"Right-paint fixture has both a held and placed ingredient");
            game.SetMode(ScreenMode.Play);game.SetMode(ScreenMode.Inventory);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=CraftPoint(CraftCell+1)}.WithButton(MouseButton.Right));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=CraftPoint(CraftCell+1)});yield return null;yield return null;
            Check(game.UI.HeldStack.Empty&&game.Crafting.Grid.Slots.All(v=>v.Empty)&&game.Inventory.Total(BlockId.Planks)==3,
                "Canceled right-paint cannot continue placing or duplicate its returned cursor/grid items");
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            foreach(byte item in new[]{BlockId.Workbench,BlockId.Dirt,BlockId.IronIngot,IndustryId.Crusher}){game.UI.InspectBrowserItem(item,false);game.UI.CloseBrowserRecipe();}
            created=game.UI.CreatedWidgets;
            for(int i=0;i<30;i++)
            {
                game.UI.InspectBrowserItem(i%2==0?IndustryId.Crusher:BlockId.Dirt,false);
                if(i%2!=0)Check(!game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Any(v=>v.RecipeId!=null),"Empty recipe view clears every old result identity "+i);
                game.UI.CloseBrowserRecipe();
            }
            Check(game.UI.CreatedWidgets==created,"Recipe navigation reuses a bounded set of widgets");
            game.SetMode(ScreenMode.Play);var saved=game.Player.transform.position;game.Player.enabled=false;
            var fixture=game.World.Address(saved).Offset(0,12,0);game.Player.transform.position=game.World.Local(fixture)+new Vector3(.5f,0,.5f);
            game.Player.Camera.transform.position=game.Player.transform.position+Vector3.up*1.64f;yield return Settle();
            var a=fixture.Offset(-1,0,2);var b=fixture.Offset(1,0,2);
            foreach(var pos in new[]{a,b}){byte old=game.World.Get(pos);if(old!=0)game.World.Remove(pos,old);Check(game.World.Place(pos,BlockId.Workbench),"Place retained bench fixture "+pos);}
            var benchA=game.Survival.At(a);var benchB=game.Survival.At(b);
            benchA.Crafting.Grid.Add(BlockId.Log,1);benchB.Crafting.Grid.Add(BlockId.Cobblestone,1);
            Check(benchA.Crafting.Grid.Revision==benchB.Crafting.Grid.Revision,"Distinct benches deliberately share revision numbers");
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Add(BlockId.Dirt,64,i,i+1);
            void Open(BlockPos pos,bool machine=false)
            {
                game.Player.Camera.transform.LookAt(game.World.Local(pos)+Vector3.one*.5f);
                Check(machine?game.TryOpenMachine(pos):game.TryOpenStation(pos),"Open current fixture authority "+pos);
            }
            Open(a);yield return null;var oldOutput=CraftView(CraftResultSlot);var oldCell=CraftView(CraftCell);
            Check(oldOutput.Icon.enabled && oldCell.ShownId==BlockId.Log,"Bench A exposes its actual log recipe");
            var stalePress=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};oldOutput.OnPointerDown(stalePress);
            game.SetMode(ScreenMode.Play);Open(b);yield return null;
            Check(CraftView(CraftCell)==oldCell && oldCell.ShownId==BlockId.Cobblestone && !CraftView(CraftResultSlot).Icon.enabled,"Same widgets show bench B despite equal revisions, with no phantom result");
            oldOutput.OnPointerClick(stalePress);
            Check(game.UI.HeldStack.Empty && benchA.Crafting.Grid.Total(BlockId.Log)==1 && benchB.Crafting.Grid.Total(BlockId.Cobblestone)==1,"Old bench result gesture cannot craft on either authority");
            yield return Capture("rebound-workbench-no-phantom");
            int warmed=game.UI.CreatedWidgets;
            for(int i=0;i<30;i++)
            {
                game.SetMode(ScreenMode.Play);Open(i%2==0?a:b);
                byte expected=i%2==0?BlockId.Log:BlockId.Cobblestone;
                Check(CraftView(CraftCell).ShownId==expected,"Station switch publishes correct ingredient before the next frame "+i);
            }
            Check(game.UI.CreatedWidgets==warmed && benchA.Crafting.Grid.Total(BlockId.Log)==1 && benchB.Crafting.Grid.Total(BlockId.Cobblestone)==1,"Repeated full-inventory bench switching neither creates widgets nor duplicates ingredients");
            game.SetMode(ScreenMode.Play);
            foreach(var pos in new[]{a,b}){game.World.Remove(pos,BlockId.Workbench);Check(game.World.Place(pos,IndustryId.Crusher),"Place same-type machine fixture");}
            var ma=game.Industry.Simulation.At(a);var mb=game.Industry.Simulation.At(b);ma.Priority=0;mb.Priority=2;
            Open(a,true);yield return null;
            BoundUIButton Priority()=>game.UI.VisibleRoot.GetComponentsInChildren<BoundUIButton>().Single(v=>v.GetComponentInChildren<Text>().text.StartsWith("PRIORITY:"));
            var button=Priority();button.OnPointerDown(stalePress);
            game.SetMode(ScreenMode.Play);Open(b,true);yield return null;
            Check(Priority()==button && button.GetComponentInChildren<Text>().text=="PRIORITY: LOW","Cached machine control labels rebind to the new machine");
            button.OnPointerClick(stalePress);
            Check(ma.Priority==0&&mb.Priority==2,"Stale machine button release cannot modify the previous or current machine");
            yield return BrowserPointer(button);
            Check(ma.Priority==0&&mb.Priority==0,"New pointer action affects only the currently bound machine exactly once");
            game.SetMode(ScreenMode.Play);game.World.Remove(a,IndustryId.Crusher);game.World.Remove(b,IndustryId.Crusher);
            game.Player.transform.position=saved;game.Player.ResetMotion();game.Player.enabled=true;
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            game.SetMode(ScreenMode.Inventory);game.Inventory.Add(BlockId.Diamond,7,12,13);yield return null;
            game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"screen-reuse-saves"));
            Check(game.SaveGame("Retained screen checkpoint",true),"Write isolated UI binding checkpoint");
            var entry=game.Saves.List().First(e=>e.Id==game.SaveId&&!e.Backup);
            game.SetMode(ScreenMode.Inventory);game.Inventory.Take(12,int.MaxValue);game.Inventory.Add(BlockId.Log,11,12,13);yield return null;
            var beforeLoad=game.Inventory;var previousSlot=CraftView(12);previousSlot.OnPointerDown(stalePress);
            game.SetMode(ScreenMode.Pause);Check(game.LoadGame(entry),"Load checkpoint with retained views");yield return Settle();
            game.Mobs.NaturalSpawning=false;game.SetMode(ScreenMode.Inventory);yield return null;
            Check(!ReferenceEquals(beforeLoad,game.Inventory)&&CraftView(12)==previousSlot&&previousSlot.ShownId==BlockId.Diamond&&previousSlot.ShownCount==7,"Save load rebinds the same view to new authoritative containers");
            previousSlot.OnPointerClick(stalePress);
            Check(game.UI.HeldStack.Empty&&game.Inventory.Total(BlockId.Diamond)==7&&beforeLoad.Total(BlockId.Log)==11,"Pre-load input cannot transfer items between sessions");
            game.SetMode(ScreenMode.Pause);game.SetCreative(true);game.SetMode(ScreenMode.Inventory);yield return null;
            Check(game.UI.VisibleRoot.GetComponentsInChildren<Button>().Any(v=>v.name.StartsWith("Creative item")),"Creative catalog is shown after cached survival inventory");
            game.SetMode(ScreenMode.Pause);game.SetCreative(false);game.SetMode(ScreenMode.Inventory);yield return null;
            Check(!game.UI.VisibleRoot.GetComponentsInChildren<Button>().Any(v=>v.name.StartsWith("Creative item"))&&game.Crafting.Grid.Size==2,"Survival hides all cached Creative grants and restores personal crafting");
            bool female=game.Player.Female;int skin=game.Player.Skin;
            game.SetMode(ScreenMode.Appearance);game.SetAppearance(!female,1-skin);game.SetMode(ScreenMode.Inventory);yield return null;
            Check(CraftView(12)==previousSlot&&game.Player.Female!=female,"Appearance changes keep the inventory view bound to current items");
            game.SetAppearance(female,skin);
            game.UI.HeldStack=new ItemStack(BlockId.Log,13);game.StartSession(246813);game.World.ViewDistance=4;game.Mobs.NaturalSpawning=false;yield return Settle();
            game.SetMode(ScreenMode.Inventory);yield return null;
            Check(game.UI.HeldStack.Empty&&game.Inventory.Slots.All(s=>s.Empty)&&game.Crafting.Preview==null,"New session cannot inherit cached cursor items, ingredients or recipe previews");
            yield return Capture("new-session-clean-inventory");game.SetMode(ScreenMode.Play);
        }
    }
}
