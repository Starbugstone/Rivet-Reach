using System;
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
        IEnumerator ReviewFishing()
        {
            FreezeSaveFixture();game.enabled=false;game.SetCreative(false);yield return Settle();
            var world=game.World;var player=game.Player;var p=world.Address(player.transform.position).Offset(0,3,5);
            void Put(BlockPos cell,byte id)
            {byte old=world.Get(cell);if(old==id)return;if(old!=0)Check(world.Remove(cell,old),"Clear fishing fixture");if(id!=0)Check(Fluids.IsFluid(id)?world.ChangeFluid(cell,0,id):world.Place(cell,id),"Place fishing fixture "+id+" at "+cell);}
            for(int x=-5;x<=5;x++)for(int z=-5;z<=5;z++)
            {
                Put(p.Offset(x,-2,z),BlockId.Stone);
                for(int y=-1;y<=3;y++)Put(p.Offset(x,y,z),y<=0?(Math.Abs(x)<=3&&Math.Abs(z)<=3?Fluids.Water.Source:BlockId.Dirt):BlockId.Air);
            }
            game.Sky.Clock.SetTime(.4);game.Sky.Apply();
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,64);
            var benchPos=p.Offset(5,1,-3);Put(benchPos,BlockId.Workbench);game.Inventory.Add(BlockId.Stick,3);game.Inventory.Add(FarmId.String,2);
            game.SetMode(ScreenMode.Play);player.transform.position=world.Local(benchPos)+new Vector3(.5f,.02f,-2);player.ResetMotion();player.Camera.transform.position=world.Local(benchPos)+new Vector3(.5f,1.2f,-2);player.Camera.transform.LookAt(world.Local(benchPos)+Vector3.one*.5f);yield return null;
            Check(game.TryInteractTarget(),"Open actual workbench for rod crafting");game.UI.InspectBrowserItem(FishId.Rod,false);yield return Capture("fishing-rod-recipe");Check(game.InventoryOpen&&game.OpenStation!=null,"Recipe capture remains at reachable workbench");game.UI.CloseBrowserRecipe();
            Check(game.Crafting.FillRecipe("rivet:fishing_rod",game.Inventory)==RecipeFillStatus.Filled,"Fill rod recipe from the ordinary inventory");yield return Capture("fishing-crafting");
            Check(game.Crafting.CraftToInventory(game.Inventory,1).Succeeded&&game.Inventory.Total(FishId.Rod)==1&&game.Inventory.Total(BlockId.Stick)==0&&game.Inventory.Total(FarmId.String)==0&&game.Crafting.Grid.Slots.All(s=>s.Empty),"Craft one rod with exactly three sticks and two string");yield return Capture("fishing-crafted");
            game.Selected=game.Inventory.FindSlot(s=>s.Id==FishId.Rod);
            void AimWater()
            {player.transform.position=world.Local(p)+new Vector3(.5f,1.02f,-4.5f);player.ResetMotion();player.Camera.transform.position=world.Local(p)+new Vector3(.5f,2.4f,-4.5f);player.Camera.transform.LookAt(world.Local(p)+new Vector3(.5f,.88f,.5f));}
            game.SetMode(ScreenMode.Play);AimWater();yield return Settle(40);AimWater();
            Check(game.Fishing.Use()&&game.Fishing.Cast.Phase==FishingPhase.Waiting,"Actual selected rod casts through source-water targeting");
            var target=game.Fishing.Cast.Target;Check(game.Fishing.SiteValid(),"Cast targets a resident open two-deep water patch");
            int wait=game.Fishing.Cast.Remaining;Check(wait>=160&&wait<=320,"Cast schedules an eight-to-sixteen second wait");
            // Exercise the actual held rod and line renderers, not only the state machine.
            player.enabled=true;player.Yaw=0;player.Pitch=20;yield return null;
            var direction=(world.Local(target)+new Vector3(.5f,.88f,.5f)-player.Camera.transform.position).normalized;
            player.Yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(direction.y)*Mathf.Rad2Deg;
            yield return new WaitForSecondsRealtime(.5f);player.enabled=false;
            Check(player.HeldBlock.Visible&&player.HeldBlock.ItemId==FishId.Rod,"Rod is visible in first-person hand");
            var tipOnScreen=player.Camera.WorldToViewportPoint(player.HeldBlock.FishingTip);Check(tipOnScreen.z>0&&tipOnScreen.z<3&&tipOnScreen.x>0&&tipOnScreen.x<1&&tipOnScreen.y>0&&tipOnScreen.y<1,"Held rod tip stays visibly in front of player");
            Check(world.GetComponentsInChildren<LineRenderer>().Any(line=>line.enabled&&line.positionCount==6),"Fishing line connects rod to visible float");
            yield return new WaitForEndOfFrame();
            var fishingLine=world.GetComponentsInChildren<LineRenderer>().Single(line=>line.enabled&&line.positionCount==6);
            Check(Vector3.Distance(fishingLine.GetPosition(0),player.HeldBlock.FishingTip)<.001f,"Fishing line follows the current rendered rod tip after held-item updates");
            File.WriteAllText(Path.Combine(output,"line-attachment.txt"),"Tip viewport: "+player.Camera.WorldToViewportPoint(player.HeldBlock.FishingTip)+"; line start: "+player.Camera.WorldToViewportPoint(fishingLine.GetPosition(0)));
            yield return Capture("fishing-cast");
            game.Fishing.Advance(wait-1);Check(game.Fishing.Cast.Phase==FishingPhase.Waiting,"No premature catch");
            game.Fishing.Advance(1);Check(game.Fishing.Cast.Phase==FishingPhase.Bite,"Bite starts on its scheduled tick");yield return Capture("fishing-bite");
            Check(game.Fishing.Use()&&game.Inventory.Total(FishId.Raw)==1&&!game.Fishing.Cast.Active,"Reeling a bite grants exactly one fish");
            Check(game.Inventory.Total(FishId.Rod)==1,"Casting never consumes the rod");yield return Capture("fishing-catch");
            AimWater();Check(game.Fishing.Use(),"Recast after catch");Check(game.Fishing.Use()&&game.Inventory.Total(FishId.Raw)==1,"Early reel gives no fish");
            Check(game.Fishing.Use(),"Cast for timeout");game.Fishing.Advance(game.Fishing.Cast.Remaining+60);Check(!game.Fishing.Cast.Active&&game.Inventory.Total(FishId.Raw)==1,"Missed bite gives no fish");
            foreach(var mode in new[]{ScreenMode.Inventory,ScreenMode.Pause})
            {AimWater();Check(game.Fishing.Use(),"Cast before menu");game.SetMode(mode);Check(!game.Fishing.Cast.Active,"Menu cancels active cast");game.SetMode(ScreenMode.Play);}
            AimWater();Check(game.Fishing.Use(),"Cast before selection change");game.Selected=1;game.Fishing.Advance(0);Check(!game.Fishing.Cast.Active,"Switching away cancels cast");game.Selected=0;
            AimWater();Check(game.Fishing.Use(),"Cast before water removal");target=game.Fishing.Cast.Target;Put(target.Offset(0,-1,0),0);game.Fishing.Advance(1);Check(!game.Fishing.Cast.Active,"Removing water cancels without a catch");Put(target.Offset(0,-1,0),Fluids.Water.Source);
            AimWater();Check(game.Fishing.Use(),"Cast before walking away");player.Camera.transform.position+=Vector3.right*20;game.Fishing.Advance(0);Check(!game.Fishing.Cast.Active,"Walking beyond tether cancels");AimWater();
            var blocker=world.Address(player.Camera.transform.position+player.Camera.transform.forward*1.2f);Put(blocker,BlockId.Stone);Check(!game.Fishing.Use(),"Cannot cast through a wall");Put(blocker,0);
            for(int i=1;i<game.Inventory.Count;i++){game.Inventory.Take(i,64);game.Inventory.Add(BlockId.Stone,64,i,i+1);}
            int dropped=game.Items.Total(FishId.Raw);AimWater();Check(game.Fishing.Use(),"Full backpack can still cast");game.Fishing.Advance(game.Fishing.Cast.Remaining);Check(game.Fishing.Use()&&game.Items.Total(FishId.Raw)==dropped+1&&game.Inventory.Total(FishId.Raw)==0,"Full inventory drops exactly one caught fish without loss");
            for(int i=1;i<game.Inventory.Count;i++)game.Inventory.Take(i,64);
            game.Inventory.Add(FishId.Raw,4);game.Inventory.Add(FishId.Cooked,2);game.Inventory.Add(FishId.Stew,1);
            game.Hunger.Exert((game.Hunger.Food-8)*4);Check(game.Hunger.TryEat(game.Inventory,game.Inventory.FindSlot(s=>s.Id==FishId.Cooked),game.Registry.FoodPoints(FishId.Cooked))&&game.Hunger.Food==14,"Cooked fish uses ordinary eating and food values");
            // Real input: holding Use through a bite does not automatically reel or catch.
            AimWater();player.enabled=true;player.Yaw=0;player.Pitch=20;yield return null;
            direction=(world.Local(p)+new Vector3(.5f,.88f,.5f)-player.Camera.transform.position).normalized;player.Yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(direction.y)*Mathf.Rad2Deg;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSecondsRealtime(.2f);
            Check(game.Fishing.Cast.Active,"Bound Use press casts fishing line");game.Fishing.Advance(game.Fishing.Cast.Remaining);int before=game.Inventory.Total(FishId.Raw);yield return new WaitForSecondsRealtime(.25f);
            Check(game.Fishing.Cast.Phase==FishingPhase.Bite&&game.Inventory.Total(FishId.Raw)==before,"Held Use does not auto-reel a bite");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;player.enabled=false;Check(!game.Fishing.Cast.Active&&game.Inventory.Total(FishId.Raw)==before+1,"A second bound Use press reels exactly once");
            var sim=game.Industry.Simulation;var cookPos=p.Offset(5,1,0);Put(cookPos,FarmId.Cooker);var cooker=sim.At(cookPos);cooker.SelectCooking("rivet:cook_fish");Check(cooker.CookingId=="rivet:cook_fish","Select fish cooking recipe");cooker.Items.Add(FishId.Raw,1,0,1);cooker.Items.Add(BlockId.Coal,1,3,4);
            for(int i=0;i<200;i++)sim.Step();Check(cooker.Items.Slots[4].Id==FishId.Cooked&&cooker.Items.Slots[4].Count==1&&cooker.Items.Slots[0].Empty,"Placed cooker consumes one raw fish and produces one cooked fish");
            player.transform.position=world.Local(cookPos)+new Vector3(.5f,.02f,-2);player.ResetMotion();player.Camera.transform.position=world.Local(cookPos)+new Vector3(.5f,1.2f,-2);player.Camera.transform.LookAt(world.Local(cookPos)+Vector3.one*.5f);yield return null;Check(game.TryInteractTarget(),"Open actual cooker interface");yield return Capture("fishing-cooking");Check(game.InventoryOpen&&game.OpenMachine==cooker,"Cooking capture shows reachable cooker and finished output");game.SetMode(ScreenMode.Play);
            AimWater();Check(game.Fishing.Use(),"Cast before save boundary");game.SetMode(ScreenMode.Pause);Check(!game.Fishing.Cast.Active,"Pause cancels transient cast before save");game.InitializeSaves(Path.Combine(output,"Saves"));Check(game.SaveGame("Fishing checkpoint"),"Save fish, rod and cooked output: "+game.SaveStatus);
            int raw=game.Inventory.Total(FishId.Raw),cooked=game.Inventory.Total(FishId.Cooked);var entry=game.Saves.List().First(e=>!e.Backup);
            Check(game.LoadGame(entry),"Load fishing checkpoint: "+game.SaveStatus);FreezeSaveFixture();game.enabled=false;world=game.World;player=game.Player;yield return Settle(120);game.enabled=true;yield return null;game.enabled=false;
            Check(!game.Fishing.Cast.Active&&game.Inventory.Total(FishId.Rod)==1&&game.Inventory.Total(FishId.Raw)==raw&&game.Inventory.Total(FishId.Cooked)==cooked&&game.Inventory.Total(FishId.Stew)==1,"Fish inventory survives reload; transient cast does not");
            Check(game.Industry.Simulation.At(cookPos).Items.Slots[4].Id==FishId.Cooked,"Fish recipe and finished cooker output survive reload");
            game.SetMode(ScreenMode.Inventory);game.UI.InspectBrowserItem(FishId.Cooked,false);yield return Capture("fishing-cooked-recipe");game.UI.CloseBrowserRecipe();game.UI.InspectBrowserItem(FishId.Stew,false);yield return Capture("fishing-meal-recipe");game.UI.CloseBrowserRecipe();
        }
    }
}
