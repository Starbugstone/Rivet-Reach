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
        IEnumerator ReviewOrchardLegacy()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rr-legacy-directory");
            Check(index>=0,"Explicit legacy fixture directory supplied");game.InitializeSaves(args[index+1]);
            var old=game.Saves.List().Where(e=>!e.Backup).ToArray();Check(old.Length>=2,"Discover pre-orchard and pre-crank checkpoints");
            foreach(var entry in old)
            {
                game.InitializeSaves(args[index+1]);byte[] bytes=game.Saves.Read(entry);
                using(var reader=game.Saves.Open(bytes,out _))Check(reader.Format<=2,"Fixture is an actual earlier schema: "+entry.Name);
                var changed=UnityEngine.Object.Instantiate(game.Registry);changed.Get(BlockId.IronIngot).stackLimit++;
                bool rejected=false;try{using var reader=new SaveStore(output,changed).Open(bytes,out _);}catch(InvalidDataException){rejected=true;}
                UnityEngine.Object.Destroy(changed);Check(rejected,"Legacy compatibility still rejects changed pre-existing content");
                Check(game.LoadGame(entry),"Load earlier checkpoint with full state: "+game.SaveStatus);FreezeSaveFixture();yield return Settle(120);
                game.InitializeSaves(Path.Combine(output,"Migrated"));Check(game.SaveGame(entry.Name,true),"Migrate loaded state to a new schema-3 checkpoint");
                var migrated=game.Saves.List().First(e=>e.Id==game.SaveId&&!e.Backup);byte[] fresh=game.Saves.Read(migrated);
                Check(game.LoadGame(migrated),"Reload migrated checkpoint: "+game.SaveStatus);FreezeSaveFixture();
                Check(game.CaptureSave(migrated).SequenceEqual(fresh),"Migrated checkpoint round-trips every serialized field exactly");
            }
        }
        IEnumerator ReviewOrchard()
        {
            FreezeSaveFixture();var world=game.World;var player=game.Player;
            var root=world.Address(player.transform.position).Offset(8,2,5);
            void Clear(BlockPos p){byte id=world.Get(p);if(id!=0)Check(world.Remove(p,id),"Clear orchard fixture "+p);}
            for(int z=-3;z<=3;z++)for(int x=-3;x<=9;x++)for(int y=-1;y<=8;y++)
            {var p=root.Offset(x,y,z);Clear(p);if(y==-1)Check(world.Place(p,BlockId.Dirt),"Orchard soil "+p);}
            var second=root.Offset(6,0,0);
            Check(!world.Place(root.Offset(0,1,0),BlockId.Sapling),"Unsupported sapling placement rejects");
            // Real selected-stack placement consumes exactly one; Creative preserves its stack.
            game.SetMode(ScreenMode.Play);game.SetCreative(false);
            player.Camera.transform.position=world.Local(root)+new Vector3(.5f,2,-2);player.Camera.transform.LookAt(world.Local(root)+new Vector3(.5f,-.1f,.5f));
            game.Inventory.Add(BlockId.Sapling,4,0,1);game.Selected=0;
            Check(game.TryPlaceSelected()&&world.Get(root)==BlockId.Sapling&&game.Inventory.Total(BlockId.Sapling)==3,"Use plants one sapling on dirt through inventory placement");
            Check(!game.CanPlace(root,out _)&&!world.Place(root,BlockId.Sapling)&&game.Inventory.Total(BlockId.Sapling)==3,"Repeated occupied planting consumes nothing");
            game.SetCreative(true);player.Camera.transform.position=world.Local(second)+new Vector3(.5f,2,-2);player.Camera.transform.LookAt(world.Local(second)+new Vector3(.5f,-.1f,.5f));
            Check(game.TryPlaceSelected()&&world.Get(second)==BlockId.Sapling&&game.Inventory.Total(BlockId.Sapling)==3,"Creative plants without spending a sapling");
            FreezeSaveFixture();game.SetCreative(false);
            game.enabled=false;game.SetMode(ScreenMode.Play);
            player.Camera.transform.position=world.Local(root)+new Vector3(1.7f,1.5f,-2.5f);player.Camera.transform.LookAt(world.Local(root)+new Vector3(.5f,.4f,.5f));
            yield return Capture("saplings-planted");game.SetMode(ScreenMode.Pause);game.enabled=true;
            game.Survival.AdvanceTicks(WorldSurvival.SaplingGrowthTicks-1);
            Check(world.Get(root)==BlockId.Sapling&&world.Get(second)==BlockId.Sapling,"Saplings do not grow before their scheduled time");
            // Roof, canopy obstruction and player all defer the complete tree transaction.
            var roof=root.Offset(0,7,0);Check(world.Place(roof,BlockId.Stone),"Place growth roof");
            player.transform.position=world.Local(second)+new Vector3(.5f,0,.5f);
            game.Survival.AdvanceTicks(1);
            Check(world.Get(root)==BlockId.Sapling&&world.Get(second)==BlockId.Sapling,"Darkness and player occupancy postpone growth");
            Clear(roof);var obstruction=root.Offset(1,4,0);Check(world.Place(obstruction,BlockId.Planks),"Place canopy obstruction");
            Check(!world.GrowSapling(root)&&world.Get(root)==BlockId.Sapling&&world.Get(obstruction)==BlockId.Planks,"Growth never overwrites construction or partly creates a trunk");Clear(obstruction);
            player.transform.position=world.Local(root.Offset(-3,0,-3));
            game.Survival.AdvanceTicks(100);
            Check(world.NaturalLog(root)&&world.NaturalLog(second),"Clear scheduled saplings become axe-fellable trees");
            Check(world.Get(root.Offset(0,-1,0))==BlockId.Dirt,"Growing a tree preserves its soil");
            // Planted progress, grown provenance and leaf roll sequence survive a real checkpoint.
            var seedling=root.Offset(3,0,3);Check(world.Place(seedling,BlockId.Sapling),"Plant saved seedling");game.Survival.AdvanceTicks(1800);
            game.InitializeSaves(Path.Combine(output,"Saves"));Check(game.SaveGame("Orchard checkpoint",true),"Save orchard checkpoint: "+game.SaveStatus);
            var entry=game.Saves.List().First(e=>!e.Backup);byte[] saved=game.Saves.Read(entry);
            Check(game.LoadGame(entry),"Load orchard checkpoint: "+game.SaveStatus);FreezeSaveFixture();world=game.World;player=game.Player;
            Check(game.CaptureSave(entry).SequenceEqual(saved),"Complete checkpoint including tree provenance and growth deadlines round-trips exactly");
            Check(world.NaturalLog(root)&&world.NaturalLog(second)&&world.Get(seedling)==BlockId.Sapling,"Restored grown trees and seedling retain their distinct states");
            yield return Settle(120);
            game.Survival.AdvanceTicks(1799);Check(world.Get(seedling)==BlockId.Sapling,"Loading grants no offline growth");
            // This seedling touches existing canopies; it remains safely blocked at its due time.
            game.Survival.AdvanceTicks(1);Check(world.Get(seedling)==BlockId.Sapling,"Canopy competition leaves the saved seedling intact");
            var beforeManual=game.Items.TotalSpawned;var crown=Enumerable.Range(4,3).Select(y=>root.Offset(0,y,0)).First(p=>world.NaturalLeaf(p));
            Check(world.Mine(crown,BlockId.Leaves,ToolCapability.None)&&game.Items.TotalSpawned>=beforeManual+1,"Hand-mined natural leaves retain their leaf block and use the bonus-drop path");
            int before=game.Items.TotalSpawned;
            for(int i=0;i<30;i++)
            {Check(world.Place(crown,BlockId.Leaves)&&world.Mine(crown,BlockId.Leaves,ToolCapability.None),"Placed leaf recovery "+i);}
            Check(game.Items.TotalSpawned==before+30,"Repeatedly placed leaves never farm bonus saplings or apples");
            int manualSaplings=game.Items.Piles.Where(p=>p.Stack.Id==BlockId.Sapling).Sum(p=>p.Stack.Count),manualApples=game.Items.Piles.Where(p=>p.Stack.Id==BlockId.Apple).Sum(p=>p.Stack.Count),manualLeaves=0;
            foreach(var tree in world.Generator.Trees(-55,-55,55,55))
            {
                if(manualLeaves>=300)break;
                for(int y=2;y<=tree.Logs&&manualLeaves<300;y++)for(int z=-2;z<=2&&manualLeaves<300;z++)for(int x=-2;x<=2&&manualLeaves<300;x++)
                {var leaf=tree.Root.Offset(x,y,z);if(world.Ready(leaf)&&world.NaturalLeaf(leaf)&&world.Mine(leaf,BlockId.Leaves,ToolCapability.None))manualLeaves++;}
                if(game.Items.Piles.Where(p=>p.Stack.Id==BlockId.Sapling).Sum(p=>p.Stack.Count)>manualSaplings&&game.Items.Piles.Where(p=>p.Stack.Id==BlockId.Apple).Sum(p=>p.Stack.Count)>manualApples)break;
            }
            Check(game.Items.Piles.Where(p=>p.Stack.Id==BlockId.Sapling).Sum(p=>p.Stack.Count)>manualSaplings&&game.Items.Piles.Where(p=>p.Stack.Id==BlockId.Apple).Sum(p=>p.Stack.Count)>manualApples,"Manually destroyed natural leaves produce both occasional item types ("+manualLeaves+" leaves)");
            // Chopping real generated trees exercises the complete automatic decay/drop path.
            int saplings=game.Items.Piles.Where(p=>p.Stack.Id==BlockId.Sapling).Sum(p=>p.Stack.Count);
            int apples=game.Items.Piles.Where(p=>p.Stack.Id==BlockId.Apple).Sum(p=>p.Stack.Count);
            int cut=0;
            foreach(var tree in world.Generator.Trees(-55,-55,55,55))
            {
                if(cut>=20)break;if(!world.Ready(tree.Root)||!world.NaturalLog(tree.Root))continue;
                Check(world.Mine(tree.Root,BlockId.Log,ToolCapability.Axe),"Chop natural tree "+cut);cut++;
            }
            for(int i=0;i<1000;i++)world.Trees.Step(world);
            int newSaplings=game.Items.Piles.Where(p=>p.Stack.Id==BlockId.Sapling).Sum(p=>p.Stack.Count)-saplings;
            int newApples=game.Items.Piles.Where(p=>p.Stack.Id==BlockId.Apple).Sum(p=>p.Stack.Count)-apples;
            Check(cut>=10&&newSaplings>0&&newApples>0,"Trunk chopping and leaf decay create occasional saplings and apples: "+cut+" trees, "+newSaplings+" saplings, "+newApples+" apples");
            Check(game.Items.Piles.Where(p=>p.Stack.Id==BlockId.Sapling||p.Stack.Id==BlockId.Apple).All(p=>p.ActionCreated),"Leaf drops use ordinary physical action-pickup piles");
            before=game.Items.TotalSpawned;Check(!world.Mine(crown,BlockId.Leaves,ToolCapability.None)&&game.Items.TotalSpawned==before,"Already removed leaves cannot duplicate drops");
            // A replacement player log must still stop an in-flight grown-tree felling job.
            Check(world.Mine(root,BlockId.Log,ToolCapability.Axe),"Axe queues grown tree felling");var replacement=root.Offset(0,1,0);
            Clear(replacement);Check(world.Place(replacement,BlockId.Log),"Replace queued grown log with construction");world.Trees.Step(world);
            Check(!world.NaturalLog(replacement)&&world.Get(replacement)==BlockId.Log,"Grown-tree felling protects replacement player wood");
            Check(world.Mine(second,BlockId.Log,ToolCapability.Axe),"Chop restored grown tree");for(int i=0;i<150;i++)world.Trees.Step(world);
            Check(world.Get(second.Offset(0,2,0))==0,"Restored grown trunk fells normally");
            // Recovery from mined soil and water displacement each produces one seedling.
            before=game.Items.TotalSpawned;Clear(seedling.Offset(0,-1,0));
            Check(world.Get(seedling)==0&&game.Items.TotalSpawned==before+1,"Removing soil uproots and returns one sapling");
            Check(world.Place(seedling.Offset(0,-1,0),BlockId.Dirt)&&world.Place(seedling,BlockId.Sapling),"Replant recovered sapling");before=game.Items.TotalSpawned;
            Check(world.ChangeFluid(seedling,BlockId.Sapling,Fluids.Water.Source)&&game.Items.TotalSpawned==before+1,"Water displaces a sapling and returns it exactly once");
            // Restore a tidy pre-felling scene for the visual/eating review.
            Check(game.LoadGame(entry),"Restore planted orchard for visuals");FreezeSaveFixture();world=game.World;player=game.Player;yield return Settle(120);
            player.transform.position=world.Local(root)+new Vector3(-1.5f,0,-2.5f);player.Yaw=0;player.Pitch=0;player.enabled=true;
            game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(BlockId.Apple,3,0,1);game.Inventory.Add(BlockId.Sapling,3,1,2);game.Selected=0;
            game.SetMode(ScreenMode.Play);game.SetCreative(true);yield return new WaitForSeconds(.5f);
            Check(player.HeldBlock.Visible&&player.HeldBlock.Socket.GetComponentsInChildren<MeshFilter>().Any(f=>f.sharedMesh!=null&&f.gameObject.activeInHierarchy&&f.sharedMesh.name=="Apple"),"Held apple renders its imported mesh");
            Check(game.UI.ItemIcon(BlockId.Apple)==FoodVisuals.Icon(BlockId.Apple),"Inventory uses the rendered apple icon");yield return Capture("apple-held-orchard");
            game.Selected=1;yield return new WaitForSeconds(.4f);yield return Capture("sapling-held-orchard");
            game.Items.enabled=true;
            game.Items.Spawn(new ItemStack(BlockId.Apple,2),player.transform.position+player.transform.forward*1.3f+Vector3.left*.4f+Vector3.up*.6f,Vector3.zero,120);
            game.Items.Spawn(new ItemStack(BlockId.Sapling,2),player.transform.position+player.transform.forward*1.3f+Vector3.right*.4f+Vector3.up*.6f,Vector3.zero,120);
            yield return new WaitForSeconds(.7f);player.Pitch=35;
            Check(game.Items.Piles.Any(p=>p.Stack.Id==BlockId.Apple&&p.View!=null&&p.View.GetComponentsInChildren<MeshFilter>().Any(f=>f.sharedMesh!=null&&f.sharedMesh.name=="Apple")),"Dropped apples render the authored food mesh");
            yield return Capture("orchard-drops");
            game.SetMode(ScreenMode.Inventory);yield return Capture("orchard-inventory");game.SetMode(ScreenMode.Play);game.SetCreative(false);
            game.Selected=0;player.Pitch=-30;game.Hunger.Exert(32);yield return new WaitForSeconds(.4f);
            int food=game.Hunger.Food,fruit=game.Inventory.Total(BlockId.Apple);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSeconds(.4f);yield return Capture("apple-eating");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            // Capture waits include 0.4 seconds; release before 1.2 seconds must still cancel.
            Check(game.Inventory.Total(BlockId.Apple)==fruit,"Releasing an unfinished apple bite consumes nothing");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSeconds(1.3f);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(game.Inventory.Total(BlockId.Apple)==fruit-1&&game.Hunger.Food==food+4,"Holding Use eats one apple and restores four food points");
        }
    }
}
