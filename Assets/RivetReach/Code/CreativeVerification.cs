using System;
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
        IEnumerator CreativeClick(Button button)
        {
            var rect=(RectTransform)button.transform;
            Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return null;yield return null;
        }
        IEnumerator CreativeHold(float seconds,params Key[] keys)
        {
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));yield return new WaitForSecondsRealtime(seconds);
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;yield return null;
        }
        IEnumerator ReviewCreative()
        {
            var player=game.Player;var world=game.World;
            game.Mobs.enabled=false;game.Diagnostics=false;
            Button Named(string text)=>game.UI.GetComponentsInChildren<Button>().Single(b=>b.GetComponentsInChildren<Text>().Any(t=>t.text==text));
            Check(!game.Creative&&game.Inventory.Slots.All(s=>s.Empty),"Ordinary session starts in Survival with an empty inventory");
            game.Inventory.Add(BlockId.Dirt,3);
            game.Hunger.Exert(80);game.TakeDamage(4);
            float health=game.Health.Hearts;
            yield return CreativeHold(.06f,game.Input.Keys["Pause"]);
            Check(game.Mode==ScreenMode.Pause&&Time.timeScale==0,"Escape opens the paused menu");
            yield return CreativeClick(Named("CREATIVE MODE: OFF"));
            Check(game.Creative&&game.Mode==ScreenMode.Pause&&Time.timeScale==0,"Pointer toggle enables Creative while retaining pause");
            Check(game.Inventory.Total(BlockId.Dirt)==3&&game.Health.Hearts==health&&game.Hunger.Food==0,"Enabling Creative preserves inventory, health and hunger");
            yield return Capture("creative-menu");
            yield return CreativeHold(.06f,game.Input.Keys["Pause"]);
            foreach(DamageKind kind in Enum.GetValues(typeof(DamageKind)))Check(game.TakeDamage(100,kind)==0&&!game.Health.Dead,"Creative rejects "+kind+" damage");
            yield return new WaitForSeconds(4.2f);
            Check(game.Health.Hearts==health&&game.Hunger.Food==0,"Automatic starvation cannot damage Creative players");
            Check(world.Grass.Tick>0&&game.Sky.Clock.TotalDays>0,"World simulation continues in Creative");
            yield return CreativeHold(.06f,game.Input.Keys["Inventory"]);
            var entries=game.UI.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Creative item ")).ToArray();
            Check(entries.Length==game.Registry.items.Length,"Catalog contains every registered current item");
            yield return CreativeClick(entries[0]);
            Check(game.Inventory.Slots.Sum(s=>s.Count)>3,"Pointer click on catalog supplies an item stack");
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            foreach(var item in game.Registry.items)
            {
                Check(game.TryGiveCreativeItem(item.runtimeId)&&game.Inventory.Total(item.runtimeId)==item.stackLimit,"Catalog grants legal full stack of "+item.displayName);
                for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            }
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Add(BlockId.Dirt,64,i,i+1);
            long revision=game.Inventory.Revision;
            Check(!game.TryGiveCreativeItem(BlockId.Log)&&game.Inventory.Revision==revision,"Full inventory rejects grants without changing existing stacks");
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            var search=game.UI.GetComponentInChildren<InputField>();search.text="Diamond pickaxe";yield return null;
            Check(game.UI.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Creative item "))==1,"Search filters items by name");
            search.text="no such item";yield return null;
            Check(!game.UI.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("Creative item ")),"Empty search result is safe");
            search.text="";yield return null;
            game.TryGiveCreativeItem(BlockId.Planks);game.TryGiveCreativeItem(BlockId.DiamondPickaxe);game.TryGiveCreativeItem(BlockId.Potato);
            yield return Capture("creative-catalog");
            yield return CreativeClick(Named("CRAFTING"));
            Check(game.UI.GetComponentsInChildren<SlotView>().Count(v=>v.Index>=60&&v.Index<64)==4,"Creative retains personal crafting access");
            yield return CreativeClick(Named("ALL ITEMS"));
            yield return CreativeHold(.06f,game.Input.Keys["Inventory"]);
            Check(!game.TryGiveCreativeItem(BlockId.Log),"Catalog grant requires an inventory screen");

            // Ready elevated platform isolates flight and landing from generated terrain.
            player.enabled=false;
            var cell=new BlockPos(8,96,8);player.transform.position=world.Local(cell)+new Vector3(.5f,.02f,.5f);
            yield return null;yield return Settle(90);
            bool floor=true;for(int x=-4;x<=4;x++)for(int z=-4;z<=4;z++)floor&=world.Place(cell.Offset(x,-1,z),BlockId.Stone);
            Check(floor,"Place flight fixture floor");
            player.ResetMotion();player.Yaw=0;player.Pitch=25;player.enabled=true;yield return null;
            Vector3 start=player.transform.position;
            yield return CreativeHold(.5f,game.Input.Keys["Jump"]);
            Check(player.transform.position.y>start.y+2,"Jump rises in Creative with zero food");
            float hover=player.transform.position.y;yield return new WaitForSeconds(.4f);
            Check(Mathf.Abs(player.transform.position.y-hover)<.01f,"Creative hovers when controls are released");
            yield return CreativeHold(.25f,game.Input.Keys["Crouch"]);
            Check(player.transform.position.y<hover-.8f&&player.Height>1.5f,"Crouch descends without shrinking the player");
            start=player.transform.position;
            yield return CreativeHold(.25f,game.Input.Keys["Forward"],game.Input.Keys["Sprint"]);
            Check((player.transform.position-start).magnitude>2&&game.Hunger.Food==0,"Sprint accelerates flight even with zero food");
            yield return CreativeHold(.7f,game.Input.Keys["Crouch"]);
            Check(!world.Overlaps(player.transform.position,.6f,player.Height)&&player.transform.position.y>=world.Local(cell).y-.002f,"Descending flight collides with the floor");
            var oldJump=game.Input.Keys["Jump"];game.Input.Keys["Jump"]=Key.F8;
            start=player.transform.position;yield return CreativeHold(.25f,Key.F8);game.Input.Keys["Jump"]=oldJump;
            Check(player.transform.position.y>start.y+1,"Flight respects rebound Jump control");
            start=player.transform.position;
            yield return CreativeHold(.06f,game.Input.Keys["Inventory"]);
            yield return CreativeHold(.2f,game.Input.Keys["Jump"]);
            Check((player.transform.position-start).magnitude<.01f,"Inventory suppresses flight movement and preserves hover");
            yield return CreativeHold(.06f,game.Input.Keys["Inventory"]);
            yield return Capture("creative-flight");

            player.enabled=false;player.transform.position=world.Local(cell)+new Vector3(.5f,.02f,.5f);
            player.Camera.transform.position=player.transform.position+Vector3.up*1.64f;
            var support=cell.Offset(0,-1,2);player.Camera.transform.LookAt(world.Local(support)+new Vector3(.5f,1,.5f));
            game.Selected=0;int count=game.Inventory.Slots[0].Count;
            Check(game.TryPlaceSelected()&&game.Inventory.Slots[0].Count==count,"Creative placement builds without consuming the selected stack");
            world.Remove(support.Offset(0,1,0),BlockId.Planks);
            game.Selected=0;game.SetMode(ScreenMode.Pause);yield return null;
            yield return CreativeClick(Named("CREATIVE MODE: ON"));
            Check(!game.Creative&&game.Inventory.Slots[0].Count==count,"Pointer toggle restores Survival and retains items");
            game.SetMode(ScreenMode.Inventory);
            Check(!game.TryGiveCreativeItem(BlockId.Log)&&!game.UI.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("Creative item ")),"Survival hides the catalog and rejects item grants");
            game.SetMode(ScreenMode.Play);
            Check(game.TryPlaceSelected()&&game.Inventory.Slots[0].Count==count-1,"Survival placement consumes items again");
            Check(game.TakeDamage(2)==2,"Survival damage resumes after disabling Creative");
            player.transform.position=world.Local(cell)+new Vector3(.5f,2,.5f);player.ResetMotion();player.enabled=true;
            float y=player.transform.position.y;yield return new WaitForSeconds(.2f);
            Check(player.transform.position.y<y-.1f,"Gravity resumes after Creative is disabled");
            game.SetCreative(true);game.StartSession(game.Seed+1);
            Check(!game.Creative&&game.Inventory.Slots.All(s=>s.Empty),"A replacement session resets Creative and starts empty-handed");
        }
    }
}
