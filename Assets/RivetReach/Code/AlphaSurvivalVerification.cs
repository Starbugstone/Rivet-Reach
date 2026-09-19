using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        // Run this route before the focused alpha fixtures. It uses an ordinary
        // empty-handed Survival world, real movement/mining/placement, and recipes
        // paid from gathered inventory. It never supplies items or teleports.
        IEnumerator ReviewAlphaSurvival()
        {
            game.SetCreative(false);game.SetMode(ScreenMode.Play);game.Mobs.NaturalSpawning=true;
            var player=game.Player;var world=game.World;float began=Time.realtimeSinceStartup;
            var observations=new List<string>();
            void Observe(string text)=>observations.Add((Time.realtimeSinceStartup-began).ToString("F1")+"s: "+text+"; food="+game.Hunger.Food+" health="+game.Health.Hearts+" position="+world.Address(player.transform.position));
            void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));
            void Aim(Vector3 target)
            {Vector3 direction=(target-player.Camera.transform.position).normalized;player.Yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(direction.y)*Mathf.Rad2Deg;}
            IEnumerator Walk(Vector3 destination,float stop=2.4f,float timeout=25)
            {
                float until=Time.realtimeSinceStartup+timeout;Vector3 last=player.transform.position;float stuck=0;
                while(Time.realtimeSinceStartup<until&&!game.Health.Dead)
                {
                    Vector3 delta=destination-player.transform.position;delta.y=0;if(delta.magnitude<stop)break;
                    player.Yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;player.Pitch=12;
                    stuck=(player.transform.position-last).sqrMagnitude<.00005f?stuck+Time.deltaTime:0;
                    Keys(stuck>.3f?new[]{Key.W,Key.Space}:new[]{Key.W});last=player.transform.position;yield return null;
                }
                Keys();yield return new WaitForSecondsRealtime(.2f);
            }
            IEnumerator Mine(BlockPos target,float seconds=2.5f)
            {
                Aim(world.Local(target)+Vector3.one*.5f);yield return null;
                InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Left));
                float until=Time.realtimeSinceStartup+seconds;byte original=world.Get(target);
                while(Time.realtimeSinceStartup<until&&world.Get(target)==original&&!game.Health.Dead)yield return null;
                InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForSecondsRealtime(.2f);
            }
            Check(game.Inventory.Slots.All(s=>s.Empty)&&!game.Creative,"Survival route starts empty-handed with normal survival authority");
            Observe("Begin gathering, workshop and exploration route");yield return Capture("survival-start");
            var origin=world.Address(player.transform.position);var visited=new HashSet<BlockPos>();
            for(int treeNumber=0;treeNumber<8&&game.Inventory.Total(BlockId.Log)<4&&!game.Health.Dead;treeNumber++)
            {
                var trees=world.Generator.Trees(origin.X-40,origin.Z-40,origin.X+40,origin.Z+40)
                    .Where(t=>!visited.Contains(t.Root)&&world.Ready(t.Root)&&world.Get(t.Root)==BlockId.Log)
                    .OrderBy(t=>(world.Local(t.Root)-player.transform.position).sqrMagnitude).ToArray();
                if(trees.Length==0)break;var tree=trees[0];visited.Add(tree.Root);
                yield return Walk(world.Local(tree.Root)+Vector3.one*.5f);
                yield return Mine(tree.Root);yield return Mine(tree.Root.Offset(0,1,0));
                yield return Walk(world.Local(tree.Root)+Vector3.one*.5f,.9f,8);yield return new WaitForSecondsRealtime(1);
                Observe("Gathered logs="+game.Inventory.Total(BlockId.Log));
            }
            yield return Capture("survival-gathered");
            bool Craft(string id)
            {
                if(game.Crafting.FillRecipe(id,game.Inventory)!=RecipeFillStatus.Filled)return false;
                return game.Crafting.CraftToInventory(game.Inventory,1).Succeeded;
            }
            game.SetMode(ScreenMode.Inventory);
            int logs=game.Inventory.Total(BlockId.Log);for(int i=0;i<logs;i++)if(!Craft("rivet:craft_planks"))break;
            bool benchCrafted=Craft("rivet:craft_workbench");Observe("Crafted workbench="+benchCrafted);
            yield return Capture("survival-personal-crafting");game.SetMode(ScreenMode.Play);
            BlockPos bench=default;bool placed=false;
            if(benchCrafted)
            {
                game.Selected=InventoryActions.Pick(game.Inventory,game.Registry,BlockId.Workbench,0,false);
                player.Pitch=48;yield return null;yield return null;
                InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
                InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForSecondsRealtime(.5f);
                var feet=world.Address(player.transform.position);
                for(int z=-4;z<=4;z++)for(int y=-2;y<=2;y++)for(int x=-4;x<=4;x++)if(world.Get(feet.Offset(x,y,z))==BlockId.Workbench){bench=feet.Offset(x,y,z);placed=true;}
                if(placed)
                {
                    Aim(world.Local(bench)+Vector3.one*.5f);yield return null;Keys(Key.E);yield return null;yield return null;Keys();yield return null;
                    if(game.InventoryOpen&&game.OpenStation!=null)
                    {Craft("rivet:craft_sticks");bool pick=Craft("rivet:craft_wood_pickaxe");Observe("Placed workshop and crafted wooden pickaxe="+pick);yield return Capture("survival-workbench");}
                }
            }
            game.SetMode(ScreenMode.Play);
            // Explore ordinary nearby terrain for the rest of the first five minutes.
            int route=0;
            while(Time.realtimeSinceStartup-began<300&&!game.Health.Dead)
            {
                var centre=world.Local(origin);float angle=route++*1.4f;
                yield return Walk(centre+new Vector3(Mathf.Cos(angle)*18,0,Mathf.Sin(angle)*18),3,12);
                Observe("Exploration leg "+route);
            }
            if(game.Inventory.Total(BlockId.WoodPickaxe)>0)
            {
                game.Selected=InventoryActions.Pick(game.Inventory,game.Registry,BlockId.WoodPickaxe,0,false);
                // Dig a shallow working face beside the workshop with the actual pick.
                var start=world.Address(player.transform.position);var dig=start.Offset(2,-1,0);
                for(int i=0;i<4&&!game.Health.Dead;i++)yield return Mine(dig.Offset(0,-i,0),4);
                Observe("Worked nearby ground with crafted tool");yield return Capture("survival-first-mining");
            }
            Keys();InputSystem.QueueStateEvent(Mouse.current,new MouseState());
            Observe("Route finished; logs="+logs+" workbench="+placed+" pickaxe="+game.Inventory.Total(BlockId.WoodPickaxe));
            yield return Capture("survival-five-minutes");
            File.WriteAllLines(Path.Combine(output,"survival-observations.txt"),observations);
            Check(!game.Creative&&!game.Health.Dead&&game.Hunger.Food>0,"Five-minute scripted Survival route remains alive without supplied resources or Creative");
            Check(placed&&game.Inventory.Total(BlockId.WoodPickaxe)>0,"Gathered resources establish a placed workshop and crafted mining tool");
        }
    }
}
