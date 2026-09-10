using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewSurvival()
        {
            Check(game.Inventory.Slots.All(s=>s.Empty),"New expedition starts without supplied progression tools");
            yield return ReviewCrafting();
            var world=game.World;var player=game.Player;var original=WorldPoint.FromLocal(player.transform.position,world.Origin);
            // Small supported open-air fixture, within resident terrain. Materials used below are explicit test fixtures.
            var baseCell=world.Address(player.transform.position).Offset(0,4,0);
            for(int z=-2;z<=4;z++)for(int x=-3;x<=4;x++)
            {
                var floor=baseCell.Offset(x,-1,z);if(world.Get(floor)!=0)world.Remove(floor,world.Get(floor));world.Place(floor,BlockId.Dirt);
                for(int y=0;y<=3;y++){var p=baseCell.Offset(x,y,z);if(world.Get(p)!=0)world.Remove(p,world.Get(p));}
            }
            player.ResetMotion();player.transform.position=world.Local(baseCell)+new Vector3(.5f,.02f,.5f);player.Yaw=0;player.Pitch=0;yield return null;
            var bench=baseCell.Offset(0,0,2);world.Place(bench,BlockId.Workbench);
            void Aim(BlockPos cell)
            {
                Vector3 direction=(world.Local(cell)+Vector3.one*.5f-player.Camera.transform.position).normalized;
                player.Yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(direction.y)*Mathf.Rad2Deg;
            }
            IEnumerator Use()
            {
                InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
                InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;yield return null;
            }
            Aim(bench);yield return null;yield return Use();
            Check(game.OpenStation?.Block==BlockId.Workbench&&game.Crafting.Grid.Size==3,"Use on a placed workbench opens its real 3x3 grid");
            var pickRecipe=game.Recipes.Recipes.Single(r=>r.Output.Id==BlockId.WoodPickaxe);
            for(int i=0;i<pickRecipe.Ingredients.Count;i++)if(!pickRecipe.Ingredients[i].Empty)game.Crafting.Grid.Add(pickRecipe.Ingredients[i].Id,1,i,i+1);
            yield return Capture("workbench-pickaxe-ready");yield return ClickCraftUI(CraftResultSlot,false,true);
            Check(game.Inventory.Total(BlockId.WoodPickaxe)==1&&game.Crafting.Grid.Slots.All(s=>s.Empty),"Workbench pointer result crafts a wooden pickaxe");
            game.UI.InspectBrowserItem(BlockId.WoodPickaxe,false);yield return Capture("workbench-recipes");game.UI.CloseBrowserRecipe();
            game.SetMode(ScreenMode.Play);yield return null;
            var ore=baseCell.Offset(1,0,2);world.Place(ore,BlockId.Stone);
            Check(!world.Mine(ore,BlockId.Stone,ToolCapability.None,ToolTier.None)&&world.Get(ore)==BlockId.Stone,"Fists cannot bypass the stone extraction tier");
            Check(world.Mine(ore,BlockId.Stone,ToolCapability.Pickaxe,ToolTier.Wood),"Wooden pickaxe capability extracts stone");
            Check(game.Items.Total(BlockId.Cobblestone)>=1,"Stone supplies cobblestone for the next tool and furnace");
            var furnacePos=baseCell.Offset(2,0,2);world.Place(furnacePos,BlockId.Furnace);Aim(furnacePos);yield return null;yield return Use();
            Check(game.OpenStation?.Furnace!=null,"Use opens the world furnace");
            var furnace=game.OpenStation.Furnace;
            game.Inventory.Add(BlockId.RawIron,2,12,13);game.Inventory.Add(BlockId.Coal,1,13,14);
            yield return null;yield return DragCraftUI(12,100);yield return DragCraftUI(13,101);
            Check(furnace.Slots[0].Id==BlockId.RawIron&&game.Survival.ActiveFurnaces==1,"Input and fuel gestures wake the furnace");
            yield return new WaitForSeconds(1);Check(furnace.ProgressTicks>0&&furnace.Slots[2].Empty,"Furnace progress advances with its interface open");
            yield return Capture("furnace-smelting");game.SetMode(ScreenMode.Play);yield return null;
            game.Survival.AdvanceTicks(400);Check(furnace.Slots[2].Count==2&&furnace.Slots[0].Empty,"Closed furnace produces two conserved ingots");
            Check(game.TryOpenStation(furnacePos),"Same furnace reopens by its world address");yield return null;yield return null;
            var held=new ItemStack(BlockId.Dirt,1);furnace.Click(2,ref held,false);Check(held.Count==1&&furnace.Slots[2].Count==2,"Output rejects insertion at the station authority");
            yield return ClickCraftUI(102,false,true);Check(game.Inventory.Total(BlockId.IronIngot)==2&&furnace.Slots[2].Empty,"Shift output transfers ingots to inventory");
            game.Inventory.Add(BlockId.Potato,1,14,15);yield return null;yield return DragCraftUI(14,100);game.Survival.Wake(furnacePos);game.Survival.AdvanceTicks(200);
            Check(furnace.Slots[2].Id==BlockId.BakedPotato,"The same furnace bakes a potato using remaining fuel");
            yield return ClickCraftUI(102,false,true);game.SetMode(ScreenMode.Play);yield return null;
            var soil=baseCell.Offset(-2,-1,1);game.Inventory.Add(BlockId.WoodHoe,1,5,6);game.Selected=5;Aim(soil);yield return null;yield return Use();
            Check(world.Get(soil)==BlockId.Farmland,"Using a hoe tills dirt");
            game.Inventory.Add(BlockId.Potato,2,6,7);game.Selected=6;Aim(soil);yield return null;yield return Use();
            var crop=soil.Offset(0,1,0);Check(world.Get(crop)==BlockId.PotatoPlant&&game.Inventory.Slots[6].Count==1,"Planting consumes one potato");
            Check(!world.Solid(crop)&&game.Survival.ScheduledCrops==1,"Crops are non-solid scheduled world state");
            for(int stage=0;stage<3;stage++)game.Survival.AdvanceTicks(WorldSurvival.CropStageTicks);
            Check(world.Get(crop)==BlockId.MaturePotatoPlant,"An exposed planted potato grows through all stages");
            Aim(crop);yield return null;yield return Capture("potato-farm");
            int potatoes=game.Items.Total(BlockId.Potato);Check(world.Mine(crop,BlockId.MaturePotatoPlant,ToolCapability.None,ToolTier.None),"Ripe potatoes can be harvested by hand");
            Check(game.Items.Total(BlockId.Potato)>=potatoes+2&&game.Survival.ScheduledCrops==0,"Harvest yields potatoes and removes the growth schedule");
            Check(world.Plant(crop),"Farmland accepts replanting");game.Inventory.Take(6,1);world.Remove(soil,BlockId.Farmland);
            Check(world.Get(crop)==0&&game.Survival.ScheduledCrops==0,"Removing supporting soil uproots the crop");
            // Inspect hunger/health/equipment through the same game-facing authority used by input and attacks.
            game.Hunger.Exert(60);Check(game.Hunger.Food<=5&&!game.Hunger.CanSprint,"Exertion makes the player hungry and disables sprinting");
            game.Inventory.Add(BlockId.BakedPotato,2,7,8);game.Selected=7;player.Pitch=-60;yield return null;
            int foodBefore=game.Hunger.Food;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSeconds(1.4f);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(game.Hunger.Food==foodBefore+5,"Holding Use eats exactly one baked potato");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSeconds(.3f);
            Check(player.EatingProgress>0,"A second bite starts while Use remains held");
            game.SetMode(ScreenMode.Inventory);yield return null;yield return null;
            Check(player.EatingProgress==0&&game.Hunger.Food==foodBefore+5,"Opening inventory cancels an unfinished bite without consuming food");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            for(int i=0;i<4;i++){game.Inventory.Add((byte)(78+i),1,20+i,21+i);yield return null;yield return DragCraftUI(20+i,200+i);}
            Check(game.Equipment.Protection==20,"Four equipment slots activate a complete diamond set");yield return Capture("armor-equipped");game.SetMode(ScreenMode.Play);yield return null;
            float hp=game.Health.Hearts;Check(Math.Abs(game.TakeDamage(10)-2)<.001f&&Math.Abs(game.Health.Hearts-(hp-2))<.001f,"Game damage applies the equipped armor defense");
            yield return new WaitForSeconds(.5f);yield return Capture("hearts-hunger-armor");
            // Cross a real origin shift and unload, preserving the same block-entity authorities.
            var chestPos=baseCell.Offset(-1,0,2);world.Place(chestPos,BlockId.Chest);var chest=game.Survival.At(chestPos);
            Check(game.TryOpenStation(chestPos),"Placed chest opens its world storage interface");yield return null;yield return null;
            game.Inventory.Add(BlockId.Planks,11,25,26);yield return null;yield return DragCraftUI(25,100);
            Check(chest.Storage.Total(BlockId.Planks)==11&&game.Inventory.Slots[25].Empty,"Chest drag transfers items through its visible storage slots");
            yield return Capture("chest-storage");game.SetMode(ScreenMode.Play);yield return null;
            var benchState=game.Survival.At(bench);benchState.Crafting.Grid.Add(BlockId.Log,3);
            var streamInput=new ItemStack(BlockId.RawCopper,32);furnace.Click(0,ref streamInput,false);game.Survival.Wake(furnacePos);
            Check(streamInput.Empty,"Furnace streaming fixture enters through its ingredient authority");
            long pausedTick=game.Survival.Tick;int pausedBurn=furnace.BurnTicks;
            game.SetMode(ScreenMode.Pause);yield return new WaitForSecondsRealtime(.3f);
            Check(game.Survival.Tick==pausedTick&&furnace.BurnTicks==pausedBurn,"Pausing suspends the survival clock and furnace fuel");game.SetMode(ScreenMode.Play);
            var far=new BlockPos(800,world.Generator.Height(800,0)+1,0);
            player.ResetMotion();player.transform.position=world.Local(far)+new Vector3(.5f,.01f,.5f);yield return null;yield return Settle(90);
            Check(!world.Ready(furnacePos)&&world.Origin.X!=0,"Station review unloads the fixture and shifts rendering origin");
            var parked=furnace.Slots.ToArray();int burn=furnace.BurnTicks,progress=furnace.ProgressTicks;
            game.Survival.AdvanceTicks(400);
            Check(furnace.Slots.SequenceEqual(parked)&&furnace.BurnTicks==burn&&furnace.ProgressTicks==progress,"Unloaded furnace preserves input, output, paid fuel and progress without ticking");
            player.ResetMotion();player.transform.position=world.Local(baseCell)+new Vector3(.5f,.02f,.5f);yield return null;yield return Settle(90);
            Check(ReferenceEquals(game.Survival.At(furnacePos)?.Furnace,furnace)&&furnace.Slots[0].Count+furnace.Slots[2].Count==32,"Reload keeps the same furnace and conserves its ingredient/output total");
            Check(ReferenceEquals(game.Survival.At(chestPos),chest)&&chest.Storage.Total(BlockId.Planks)==11&&ReferenceEquals(game.Survival.At(bench),benchState)&&benchState.Crafting.Grid.Total(BlockId.Log)==3,"Chest and workbench contents survive unload and origin shifts");
            furnace.Take(0,int.MaxValue);furnace.Take(2,int.MaxValue);benchState.Crafting.Grid.Take(0,int.MaxValue);
            // Dismantling drains contents exactly once after their streamed presentation returns.
            int planks=game.Items.Total(BlockId.Planks);world.Mine(chestPos,BlockId.Chest,ToolCapability.None,ToolTier.None);
            Check(game.Survival.At(chestPos)==null&&game.Items.Total(BlockId.Planks)==planks+11,"Dismantling a chest conserves its contents exactly once");
            var raw=new ItemStack(BlockId.RawCopper,3);furnace.Click(0,ref raw,false);int rawBefore=game.Items.Total(BlockId.RawCopper);
            world.Mine(furnacePos,BlockId.Furnace,ToolCapability.Pickaxe,ToolTier.Wood);
            Check(game.Survival.At(furnacePos)==null&&game.Items.Total(BlockId.RawCopper)==rawBefore+3,"Dismantling a furnace drops its unprocessed input");
            game.TakeDamage(100,DamageKind.Fall);Check(game.Health.Dead&&game.Mode==ScreenMode.Death,"Lethal damage enters the death screen");
            Check(game.Inventory.Slots.All(s=>s.Empty)&&game.Equipment.Slots.All(s=>s.Empty),"Death releases inventory and equipped armor once");
            yield return Capture("death-and-respawn");game.Respawn();Check(!game.Health.Dead&&game.Health.Hearts==20&&game.Hunger.Food==20,"Respawn restores survival state");
            Check(game.TakeDamage(20)==0,"Respawn immunity rejects immediate damage");
            Check(world.Get(bench)==BlockId.Workbench,"Respawn retains world construction");
            player.transform.position=original.Local(world.Origin);player.ResetMotion();yield return null;
        }
    }
}
