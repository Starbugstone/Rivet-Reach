using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator StarterKey(Key key)
        {
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key));yield return null;yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;yield return null;
        }
        IEnumerator StarterUse(bool crouch=false)
        {
            InputSystem.QueueStateEvent(Keyboard.current,crouch?new KeyboardState(game.Input.Keys["Crouch"]):new KeyboardState());
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(PlayerPrefs.GetInt("mineButton",0)==0?MouseButton.Right:MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;yield return null;
        }
        IEnumerator ReviewStarterCrafting()
        {
            var original=game.Inventory.Slots.ToArray();var world=game.World;var player=game.Player;game.Diagnostics=false;
            var baseCell=world.Address(player.transform.position).Offset(0,4,0);
            // Only the supported floor and three input logs are fixtures. Every recipe,
            // result transfer, workbench placement and interaction below uses player input.
            for(int z=-2;z<=8;z++)for(int x=-3;x<=3;x++)
            {
                var floor=baseCell.Offset(x,-1,z);if(world.Get(floor)!=0)world.Remove(floor,world.Get(floor));world.Place(floor,BlockId.Dirt);
                for(int y=0;y<=3;y++){var cell=baseCell.Offset(x,y,z);if(world.Get(cell)!=0)world.Remove(cell,world.Get(cell));}
            }
            player.ResetMotion();player.transform.position=world.Local(baseCell)+new Vector3(.5f,.02f,.5f);player.Yaw=0;player.Pitch=0;yield return null;
            void Aim(BlockPos cell)
            {
                Vector3 direction=(world.Local(cell)+Vector3.one*.5f-player.Camera.transform.position).normalized;
                player.Yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(direction.y)*Mathf.Rad2Deg;
            }
            bool HasText(string text)=>game.UI.GetComponentsInChildren<Text>().Any(t=>t.text.Contains(text));
            int Slot(byte id)=>Enumerable.Range(0,game.Inventory.Count).First(i=>game.Inventory.Slots[i].Id==id);
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            game.Inventory.Add(BlockId.Log,3,12,13);
            yield return StarterKey(game.Input.Keys["Inventory"]);Check(game.Crafting.Grid.Size==2,"Inventory key opens the personal 2x2 grid");
            yield return DragCraftUI(12,CraftCell);yield return Capture("starter-planks");yield return ClickCraftUI(CraftResultSlot,false,true);
            Check(game.Inventory.Total(BlockId.Planks)==12&&game.Inventory.Total(BlockId.Log)==0,"Three logs craft exactly twelve planks");
            yield return ClickCraftUI(Slot(BlockId.Planks));for(int i=0;i<4;i++)yield return ClickCraftUI(CraftCell+i,true);yield return ClickCraftUI(0);
            yield return Capture("starter-workbench");yield return ClickCraftUI(CraftResultSlot);yield return ClickCraftUI(1);
            Check(game.Inventory.Total(BlockId.Workbench)==1&&game.Inventory.Total(BlockId.Planks)==8,"A 2x2 square consumes four planks for one workbench");
            yield return ClickCraftUI(Slot(BlockId.Planks));yield return ClickCraftUI(CraftCell,true);yield return ClickCraftUI(CraftCell+2,true);yield return ClickCraftUI(0);
            yield return Capture("starter-sticks");yield return ClickCraftUI(CraftResultSlot,false,true);
            Check(game.Inventory.Total(BlockId.Stick)==4&&game.Inventory.Total(BlockId.Planks)==6,"Two vertical planks craft exactly four sticks");
            yield return StarterKey(game.Input.Keys["Inventory"]);game.Selected=Slot(BlockId.Workbench);
            var bench=baseCell.Offset(0,0,2);Aim(bench.Offset(0,-1,0));yield return null;yield return StarterUse();
            Check(world.Get(bench)==BlockId.Workbench&&game.Inventory.Total(BlockId.Workbench)==0&&game.Mode==ScreenMode.Play,"Use places the crafted workbench and consumes one item");
            Aim(bench);yield return new WaitForSeconds(.7f);yield return Capture("starter-interact");
            Check(HasText(game.Input.Keys["Interact"]+" / "+game.Input.UseButtonName)&&HasText("3 × 3 crafting"),"Target prompt names the actual interaction controls and crafting size");
            yield return StarterKey(game.Input.Keys["Interact"]);
            Check(game.OpenStation?.Block==BlockId.Workbench&&game.Crafting.Grid.Size==3,"Interact with an empty hand opens the placed workbench's 3x3 interface");
            Check(game.UI.GetComponentsInChildren<SlotView>().Count(v=>v.Index>=CraftCell&&v.Index<CraftCell+16)==9,"Workbench displays all nine input slots");
            yield return ClickCraftUI(Slot(BlockId.Planks));for(int i=0;i<3;i++)yield return ClickCraftUI(CraftCell+i,true);yield return ClickCraftUI(0);
            int sticks=Slot(BlockId.Stick);yield return ClickCraftUI(sticks);yield return ClickCraftUI(CraftCell+4,true);yield return ClickCraftUI(CraftCell+7,true);
            yield return Capture("starter-pickaxe");yield return ClickCraftUI(CraftResultSlot,false,true);
            Check(game.Inventory.Total(BlockId.WoodPickaxe)==1&&game.UI.HeldStack.Id==BlockId.Stick&&game.UI.HeldStack.Count==2,"Workbench Shift crafting sends the tool to inventory while preserving sticks on the cursor");
            yield return ClickCraftUI(sticks);
            Check(game.Inventory.Total(BlockId.WoodPickaxe)==1&&game.Inventory.Total(BlockId.Planks)==3&&game.Inventory.Total(BlockId.Stick)==2&&game.Crafting.Grid.Slots.All(s=>s.Empty),"3 planks across the top and 2 centered sticks craft exactly one wooden pickaxe");
            yield return StarterKey(game.Input.Keys["Inventory"]);game.Selected=Slot(BlockId.Planks);Aim(bench);yield return null;yield return StarterUse();
            Check(game.OpenStation?.Block==BlockId.Workbench&&game.Inventory.Total(BlockId.Planks)==3,"Mouse Use opens a station before placing the held block");
            yield return StarterKey(game.Input.Keys["Inventory"]);Aim(bench);yield return null;yield return StarterUse(true);
            Check(game.Mode==ScreenMode.Play&&game.Inventory.Total(BlockId.Planks)==2,"Crouch plus Use places against a station instead of opening it");
            var placed=bench.Offset(0,0,-1);Check(world.Get(placed)==BlockId.Planks,"Station-adjacent placement uses the aimed face");
            Aim(bench);yield return null;yield return StarterKey(game.Input.Keys["Interact"]);
            Check(game.Mode==ScreenMode.Play&&!game.TryOpenStation(bench),"An occluding block prevents both target interaction and direct station opening");
            var topDirection=(world.Local(bench)+new Vector3(.5f,.99f,.5f)-player.Camera.transform.position).normalized;
            player.Yaw=Mathf.Atan2(topDirection.x,topDirection.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(topDirection.y)*Mathf.Rad2Deg;yield return null;
            yield return StarterKey(game.Input.Keys["Interact"]);
            Check(game.OpenStation?.Block==BlockId.Workbench,"An exposed workbench top remains interactable when its center is occluded");
            game.SetMode(ScreenMode.Play);world.Remove(placed,BlockId.Planks);yield return null;
            var near=player.transform.position;player.ResetMotion();player.transform.position=world.Local(baseCell.Offset(0,0,8))+new Vector3(.5f,.02f,.5f);Aim(bench);yield return null;
            yield return StarterKey(game.Input.Keys["Interact"]);Check(game.Mode==ScreenMode.Play,"Station outside five-block targeting reach cannot open");
            player.ResetMotion();player.transform.position=near;Aim(bench);yield return null;
            var old=game.Input.Keys["Interact"];game.Input.Keys["Interact"]=Key.F8;
            yield return StarterKey(Key.F8);Check(game.OpenStation?.Block==BlockId.Workbench,"Interaction follows its rebound key");game.Input.Keys["Interact"]=old;
            game.SetMode(ScreenMode.Play);yield return null;
            game.SetMode(ScreenMode.Controls);yield return Capture("starter-controls");game.SetMode(ScreenMode.Play);yield return null;
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            for(int i=0;i<original.Length;i++)if(!original[i].Empty)game.Inventory.Add(original[i].Id,original[i].Count,i,i+1);
            game.Selected=0;
        }
    }
}
