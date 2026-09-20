using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator PaintInventorySlots(params int[] indices)
        {
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=CraftPoint(indices[0])});yield return null;yield return null;
            foreach(int index in indices)
            {
                InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=CraftPoint(index)}.WithButton(MouseButton.Right));
                yield return null;yield return null;
            }
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=CraftPoint(indices[indices.Length-1])});yield return null;yield return null;
        }
        // Independent native pointer regression; this intentionally starts a fresh test session.
        IEnumerator ReviewInventoryGestures()
        {
            game.StartSession(246813);game.World.ViewDistance=4;game.Mobs.NaturalSpawning=false;game.SetCreative(true);yield return Settle();
            game.SetMode(ScreenMode.Inventory);yield return null;
            byte helmet=game.Registry.items.First(item=>item.armorSlot==ArmorSlot.Head).runtimeId;
            game.Inventory.Add(helmet,1,0,1);yield return ClickCraftUI(0,false,true);
            Check(game.Equipment.Slots[0].Id==helmet&&game.Inventory.Slots[0].Empty,"Shift-click equips armor into an empty matching equipment slot");
            game.Inventory.Add(helmet,1,0,1);yield return ClickCraftUI(0,false,true);
            Check(game.Equipment.Slots[0].Id==helmet&&game.Inventory.Slots[0].Empty&&game.Inventory.Slots[Inventory.HotbarCount].Id==helmet,
                "Spare armor Shift-click transfers from hotbar to backpack when its equipment slot is occupied");
            yield return ClickCraftUI(Inventory.HotbarCount,false,true);
            Check(game.Inventory.Slots[0].Id==helmet&&game.Inventory.Slots[Inventory.HotbarCount].Empty&&game.Equipment.Slots[0].Id==helmet,
                "Spare armor Shift-click returns from backpack to hotbar without replacing equipped armor");
            for(int i=Inventory.HotbarCount;i<Inventory.SlotCount;i++)game.Inventory.Add(BlockId.Dirt,64,i,i+1);
            long revision=game.Inventory.Revision;yield return ClickCraftUI(0,false,true);
            Check(game.Inventory.Revision==revision&&game.Inventory.Slots[0].Id==helmet,"Blocked spare-armor transfer changes neither inventory nor equipment");
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            game.SetMode(ScreenMode.Play);var original=game.Player.transform.position;game.Player.enabled=false;
            var origin=game.World.Address(original).Offset(0,12,0);game.Player.transform.position=game.World.Local(origin)+new Vector3(.5f,0,.5f);
            game.Player.Camera.transform.position=game.Player.transform.position+Vector3.up*1.64f;yield return Settle();
            var chestPos=origin.Offset(-1,0,2);var furnacePos=origin.Offset(1,0,2);var cookerPos=origin.Offset(2,0,1);
            foreach(var fixture in new[]{(chestPos,BlockId.Chest),(furnacePos,BlockId.Furnace),(cookerPos,FarmId.ElectricCooker)})
            {
                byte existing=game.World.Get(fixture.Item1);if(existing!=0)game.World.Remove(fixture.Item1,existing);
                Check(game.World.Place(fixture.Item1,fixture.Item2),"Place gesture fixture "+fixture.Item2);
            }
            game.Player.Camera.transform.LookAt(game.World.Local(chestPos)+Vector3.one*.5f);
            Check(game.TryOpenStation(chestPos),"Open real chest for right-drag regression");yield return null;
            var chest=game.OpenStation.Storage;chest.Add(BlockId.Cobblestone,2,1,2);chest.Add(BlockId.Planks,64,2,3);
            game.UI.HeldStack=new ItemStack(BlockId.Planks,4);yield return PaintInventorySlots(100,101,102,103,100);
            Check(chest.Slots[0].Count==1&&chest.Slots[3].Count==1&&chest.Slots[1].Id==BlockId.Cobblestone&&chest.Slots[1].Count==2&&chest.Slots[2].Count==64&&game.UI.HeldStack.Count==2,
                "Native chest right-drag places one per visited compatible slot, skips full/foreign stacks and never repeats a revisited slot");
            game.UI.ReturnHeld();chest.Add(BlockId.Stone,5,4,5);yield return ClickCraftUI(104,true);
            Check(chest.Slots[4].Count==2&&game.UI.HeldStack.Id==BlockId.Stone&&game.UI.HeldStack.Count==3,"Chest right-click still takes the rounded-up half exactly once");
            game.SetMode(ScreenMode.Play);game.Player.Camera.transform.LookAt(game.World.Local(furnacePos)+Vector3.one*.5f);
            Check(game.TryOpenStation(furnacePos),"Open real furnace for guarded right-drag regression");yield return null;
            var furnace=game.OpenStation.Furnace;game.UI.HeldStack=new ItemStack(BlockId.RawIron,3);yield return PaintInventorySlots(100,101,102);
            Check(furnace.Slots[0].Id==BlockId.RawIron&&furnace.Slots[0].Count==1&&furnace.Slots[1].Empty&&furnace.Slots[2].Empty&&game.UI.HeldStack.Count==2,
                "Furnace right-drag uses ingredient validation and cannot place ore in fuel or output slots");
            game.SetMode(ScreenMode.Play);game.Player.Camera.transform.LookAt(game.World.Local(cookerPos)+Vector3.one*.5f);
            Check(game.TryOpenMachine(cookerPos),"Open real cooker for input/output right-drag regression");yield return null;
            var cooker=game.OpenMachine;cooker.SelectCooking("rivet:cook_bread");
            game.UI.HeldStack=new ItemStack(FarmId.Grain,5);yield return PaintInventorySlots(300,301,302,304);
            Check(cooker.Items.Slots.Take(3).All(stack=>stack.Id==FarmId.Grain&&stack.Count==1)&&cooker.Items.Slots[4].Empty&&game.UI.HeldStack.Count==2,
                "Cooker right-drag distributes ingredients across all three inputs without touching its output");
            game.UI.ReturnHeld();game.Inventory.Take(0,int.MaxValue);cooker.Items.Add(FarmId.Bread,2,4,5);
            game.UI.HeldStack=new ItemStack(FarmId.Bread,4);yield return PaintInventorySlots(0,304);
            Check(game.Inventory.Slots[0].Id==FarmId.Bread&&game.Inventory.Slots[0].Count==1&&cooker.Items.Slots[4].Count==2&&game.UI.HeldStack.Count==3,
                "Painting across a matching machine output never withdraws extra finished items");
            yield return Capture("inventory-gesture-cooker");
            game.SetMode(ScreenMode.Play);game.Player.transform.position=original;game.Player.ResetMotion();game.Player.enabled=true;
            yield return ReviewInventoryPresetControls();
        }
        IEnumerator ReviewInventoryPresetControls()
        {
            var originalBindings=new Dictionary<string,Key>(game.Input.Keys);
            var originalPrefs=originalBindings.Keys.ToDictionary(action=>"binding."+action,
                key=>(exists:PlayerPrefs.HasKey(key),value:PlayerPrefs.GetInt(key)));
            bool hadMousePreference=PlayerPrefs.HasKey("mineButton");int mousePreference=PlayerPrefs.GetInt("mineButton",0);
            string pendingRebind=game.Input.Rebinding;
            try
            {
                // Only this disposable fixture changes the in-memory starting pair.
                // Temporarily move unrelated E/F owners to unused keys so the real
                // preset button's successful path is testable with any user profile.
                foreach(var pair in originalBindings)
                    if(pair.Key!="Inventory"&&pair.Key!="Interact"&&(pair.Value==Key.E||pair.Value==Key.F))
                        game.Input.Keys[pair.Key]=new[]{Key.F1,Key.F2,Key.F3,Key.F4,Key.F6,Key.F7,Key.F8,Key.F9,Key.F10,Key.F11,Key.Numpad0,Key.Numpad1,Key.Numpad2,Key.Numpad3,Key.Numpad4,Key.Numpad5,Key.Numpad6,Key.Numpad7}.First(key=>!game.Input.Keys.Values.Contains(key));
                game.Input.Keys["Inventory"]=Key.Tab;game.Input.Keys["Interact"]=Key.E;
                var beforePreset=new Dictionary<string,Key>(game.Input.Keys);
                game.SetMode(ScreenMode.Controls);yield return null;yield return null;Canvas.ForceUpdateCanvases();
                var buttons=game.UI.VisibleRoot.GetComponentsInChildren<Button>();
                var preset=buttons.Single(button=>button.GetComponentInChildren<Text>().text=="E INVENTORY PRESET");
                Rect Bounds(Component component)
                {
                    var corners=new Vector3[4];((RectTransform)component.transform).GetWorldCorners(corners);
                    return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
                }
                var bounds=Bounds(preset);
                Check(bounds.xMin>=0&&bounds.yMin>=0&&bounds.xMax<=Screen.width&&bounds.yMax<=Screen.height&&
                    buttons.Where(button=>button!=preset).All(button=>!bounds.Overlaps(Bounds(button))),
                    "E inventory preset is fully on screen and overlaps no other Controls button");
                yield return BrowserPointer(preset);
                Check(game.Input.Keys["Inventory"]==Key.E&&game.Input.Keys["Interact"]==Key.F&&
                    beforePreset.All(pair=>pair.Key=="Inventory"||pair.Key=="Interact"||game.Input.Keys[pair.Key]==pair.Value),
                    "Actual Controls preset button applies E/F and leaves every unrelated test binding unchanged");
                Check(PlayerPrefs.GetInt("binding.Inventory")== (int)Key.E&&PlayerPrefs.GetInt("binding.Interact")== (int)Key.F&&
                    PlayerPrefs.HasKey("mineButton")==hadMousePreference&&PlayerPrefs.GetInt("mineButton",0)==mousePreference,
                    "Preset button persists its chosen pair without changing mouse Use preferences");
                yield return Capture("controls-e-inventory-preset");
            }
            finally
            {
                foreach(var pair in originalBindings)game.Input.Keys[pair.Key]=pair.Value;
                foreach(var pair in originalPrefs)if(pair.Value.exists)PlayerPrefs.SetInt(pair.Key,pair.Value.value);else PlayerPrefs.DeleteKey(pair.Key);
                if(hadMousePreference)PlayerPrefs.SetInt("mineButton",mousePreference);else PlayerPrefs.DeleteKey("mineButton");
                PlayerPrefs.Save();if(pendingRebind!=null)game.Input.BeginRebind(pendingRebind);
                game.SetMode(ScreenMode.Play);
            }
            Check(originalBindings.All(pair=>game.Input.Keys[pair.Key]==pair.Value)&&originalPrefs.All(pair=>
                    PlayerPrefs.HasKey(pair.Key)==pair.Value.exists&&(!pair.Value.exists||PlayerPrefs.GetInt(pair.Key)==pair.Value.value)),
                "Controls verification restores every original live and persisted player binding");
        }
    }
}
