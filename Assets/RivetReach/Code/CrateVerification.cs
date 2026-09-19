using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewCrateResume()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rr-save-directory");Check(index>=0,"Fresh crate resume uses isolated checkpoint directory");
            game.InitializeSaves(args[index+1]);Check(game.ContinueLatestSave(),"Continue crate checkpoint: "+game.SaveStatus);FreezeSaveFixture();game.Animals.enabled=false;game.enabled=false;
            yield return Settle(120);game.enabled=true;yield return null;game.enabled=false;
            var crates=game.Survival.Stations.Select(s=>s.Value).Where(s=>s.Crate!=null).ToArray();
            Check(crates.Length==3&&crates.Sum(s=>s.Crate.Count)==1536,"Fresh process retains exact physical crate stock");
            Check(crates.Count(s=>s.Crate.Locked)==2&&crates.Any(s=>s.Crate.Locked&&s.Crate.Count==0&&s.Crate.Item==BlockId.Dirt),"Fresh process retains occupied and empty type locks");
            Check(crates.Any(s=>s.ItemInputPriority==77)&&game.Survival.Stations.Any(s=>s.Value.Block==CrateId.Controller&&s.Value.ItemInputPriority==65),"Fresh process retains custom receiver priorities");
            Check(game.Survival.Stations.Count(s=>s.Value.Block==CrateId.Controller)==1&&!game.Creative,"Fresh process retains controller and Survival mode");yield return Capture("crate-continued");
        }
        IEnumerator ReviewCrates()
        {
            FreezeSaveFixture();game.Animals.enabled=false;game.Animals.Clear();game.enabled=false;game.SetCreative(false);yield return Settle();
            var world=game.World;var player=game.Player;var start=world.Address(player.transform.position);var p=start.Offset(2,3,4);
            void Put(BlockPos cell,byte id){byte old=world.Get(cell);if(old==id)return;if(old!=0)Check(world.Remove(cell,old),"Clear crate fixture");if(id!=0)Check(world.Place(cell,id),"Place crate fixture "+id);}
            void Aim(Vector3 at,Vector3 from){player.transform.position=from;player.ResetMotion();player.Camera.transform.position=from+Vector3.up*1.5f;player.Camera.transform.LookAt(at);var d=player.Camera.transform.forward;player.Yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(d.y)*Mathf.Rad2Deg;}
            void AimAt(BlockPos cell)=>Aim(world.Local(cell)+Vector3.one*.5f,world.Local(cell)+new Vector3(.5f,.02f,-2));
            void ClearInventory(){for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,64);}
            for(int x=-4;x<=5;x++)for(int z=-3;z<=3;z++){Put(p.Offset(x,-1,z),BlockId.Planks);for(int y=0;y<3;y++)Put(p.Offset(x,y,z),0);}
            var bench=p.Offset(-3,0,2);var machinist=p.Offset(-2,0,2);Put(bench,BlockId.Workbench);Put(machinist,IndustryId.Bench);game.Sky.Clock.SetTime(.4);game.Sky.Apply();
            ClearInventory();game.Inventory.Add(BlockId.Planks,8);game.Inventory.Add(BlockId.Chest,1);game.SetMode(ScreenMode.Play);AimAt(bench);yield return null;
            Check(game.TryInteractTarget(),"Open actual workbench for crate crafting");game.UI.InspectBrowserItem(CrateId.Crate,false);yield return Capture("crate-recipe");game.UI.CloseBrowserRecipe();
            Check(game.Crafting.FillRecipe("rivet:bulk_crate",game.Inventory)==RecipeFillStatus.Filled,"Fill crate recipe from eight Planks and one Chest");yield return Capture("crate-crafting");
            Check(game.Crafting.CraftToInventory(game.Inventory,1).Succeeded&&game.Inventory.Total(CrateId.Crate)==1&&game.Crafting.Grid.Slots.All(s=>s.Empty),"Craft exactly one crate through ordinary transactions");yield return Capture("crate-crafted");
            game.SetMode(ScreenMode.Play);game.Selected=game.Inventory.FindSlot(s=>s.Id==CrateId.Crate);Aim(world.Local(p)+new Vector3(.5f,-.01f,.5f),world.Local(p)+new Vector3(.5f,.02f,-2));yield return null;
            Check(game.TryPlaceSelected()&&world.Get(p)==CrateId.Crate&&game.Inventory.Total(CrateId.Crate)==0,"Place crafted crate with real placement ray and consume exactly one item");
            var crate=game.Survival.At(p).Crate;AimAt(p);yield return new WaitForSecondsRealtime(.4f);Check(world.GetComponent<CratePresentation>().ViewAt(p)!=null,"Resident crate renders original imported model");yield return Capture("crate-placed");
            Check(game.TryInteractTarget()&&game.OpenStation?.Crate==crate,"Bound station interaction opens crate inventory");
            var priorityField=game.UI.VisibleRoot.GetComponentsInChildren<InputField>().Single(f=>f.name=="Item pipe priority");Check(priorityField.text=="30","Crate defaults to item priority 30");priorityField.text="77";priorityField.onEndEdit.Invoke("77");Check(game.Survival.At(p).ItemInputPriority==77,"Actual numeric priority field changes crate receiver");yield return Capture("crate-item-priority");game.Inventory.Add(BlockId.Stone,64);int slot=game.Inventory.FindSlot(s=>s.Id==BlockId.Stone);game.UI.ClickSlot(slot,false,true);
            Check(crate.Count==64&&game.Inventory.Total(BlockId.Stone)==0,"Actual Shift-click transfers inventory stack into crate");
            game.UI.VisibleRoot.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>()?.text=="LOCK TO ITEM").onClick.Invoke();Check(crate.Locked,"Actual Lock button retains assigned type");yield return Capture("crate-locked-interface");
            game.UI.ClickSlot(100,false,true);Check(crate.Count==0&&crate.Locked&&crate.Item==BlockId.Stone&&game.Inventory.Total(BlockId.Stone)==64,"Shift withdrawal retains empty lock and exact inventory stock");
            game.Inventory.Add(BlockId.Dirt,1);slot=game.Inventory.FindSlot(s=>s.Id==BlockId.Dirt);game.UI.ClickSlot(slot,false,true);Check(crate.Count==0&&game.Inventory.Total(BlockId.Dirt)==1,"Locked crate rejects incompatible shift deposit without consuming it");
            game.UI.VisibleRoot.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>()?.text=="UNLOCK").onClick.Invoke();Check(!crate.Locked&&crate.Item==0,"Actual Unlock button releases empty assignment");
            game.UI.ClickSlot(slot,false,true);Check(crate.Count==1&&crate.Item==BlockId.Dirt,"Unlocked empty crate automatically accepts new type");game.UI.ClickSlot(100,false,true);game.SetMode(ScreenMode.Play);
            crate.Insert(new ItemStack(BlockId.Stone,1024));crate.Lock(BlockId.Stone);AimAt(p);yield return new WaitForSecondsRealtime(.4f);yield return Capture("crate-front-stock");
            Check(!world.Mine(p,CrateId.Crate,ToolCapability.None)&&world.Get(p)==CrateId.Crate&&crate.Count==1024,"Mining occupied crate is blocked before contents or terrain change");
            // Author a second recipe through the real 4x4 station.
            ClearInventory();foreach(var cell in RecipeCatalogAsset.Load().recipes.Single(r=>r.stableId=="rivet:crate_controller").ingredients)game.Inventory.Add(game.Registry.ResolveId(cell.itemId),cell.count);
            AimAt(machinist);yield return null;Check(game.TryInteractTarget(),"Open actual Machinist's Bench");game.UI.InspectBrowserItem(CrateId.Controller,false);yield return Capture("controller-recipe");game.UI.CloseBrowserRecipe();
            Check(game.Crafting.FillRecipe("rivet:crate_controller",game.Inventory)==RecipeFillStatus.Filled,"Fill controller upgrade recipe with gold and workshop components");yield return Capture("controller-crafting");Check(game.Crafting.CraftToInventory(game.Inventory,1).Succeeded&&game.Inventory.Total(CrateId.Controller)==1,"Craft controller through normal 4x4 transaction");
            game.SetMode(ScreenMode.Play);var controller=p.Offset(1,0,0);game.Selected=game.Inventory.FindSlot(s=>s.Id==CrateId.Controller);Aim(world.Local(controller)+new Vector3(.5f,-.01f,.5f),world.Local(controller)+new Vector3(.5f,.02f,-2));yield return null;Check(game.TryPlaceSelected(),"Place crafted controller adjoining physical crate");
            var second=p.Offset(2,0,0);var third=second.Offset(0,1,0);Put(second,CrateId.Crate);Put(third,CrateId.Crate);game.Survival.At(second).Crate.Insert(new ItemStack(BlockId.RawIron,512));game.Survival.At(third).Crate.Lock(BlockId.Dirt);
            var endpoint=game.Crates.At(controller);Check(endpoint.Members.Count==3&&endpoint.Members.Sum(m=>m.store.Count)==1536,"Controller aggregates three face-connected physical inventories");
            AimAt(controller);yield return new WaitForSecondsRealtime(.4f);Check(game.TryInteractTarget(),"Interact opens connected controller inventory");priorityField=game.UI.VisibleRoot.GetComponentsInChildren<InputField>().Single(f=>f.name=="Item pipe priority");Check(priorityField.text=="40","Controller defaults to item priority 40");priorityField.text="65";priorityField.onEndEdit.Invoke("65");yield return Capture("controller-interface");game.SetMode(ScreenMode.Play);
            Aim(world.Local(controller)+new Vector3(.4f,.7f,.5f),world.Local(controller)+new Vector3(3,.1f,-4));yield return new WaitForSecondsRealtime(.4f);yield return Capture("crate-warehouse");
            Check(world.Mine(controller,CrateId.Controller,ToolCapability.None)&&crate.Count==1024&&game.Survival.At(second).Crate.Count==512,"Controller mining preserves all physical stock");Put(controller,CrateId.Controller);game.Survival.At(controller).ItemInputPriority=65;Check(game.Crates.At(controller).Members.Count==3,"Replacement controller reconnects existing inventory");
            var empty=p.Offset(4,0,0);Put(empty,CrateId.Crate);int before=game.Items.Total(CrateId.Crate);Check(world.Mine(empty,CrateId.Crate,ToolCapability.None)&&game.Items.Total(CrateId.Crate)==before+1,"Empty crate mining recovers exactly one item");
            // One actual pipe run feeds machine, warehouse, crate and chest endpoints.
            var pipeStart=p.Offset(0,0,-2);for(int i=0;i<5;i++)Put(pipeStart.Offset(i,0,0),IndustryId.ItemPipe);
            Put(controller.Offset(0,0,-1),IndustryId.ItemPipe);var feed=pipeStart.Offset(-1,0,0);var furnacePos=pipeStart.Offset(0,0,1);var singlePos=pipeStart.Offset(3,0,1);var chestPos=pipeStart.Offset(4,0,1);
            Put(feed,BlockId.Chest);Put(furnacePos,BlockId.Furnace);Put(singlePos,CrateId.Crate);Put(chestPos,BlockId.Chest);
            var simulation=game.Industry.Simulation;simulation.At(pipeStart).PipeDirections=2<<(1*2);simulation.Invalidate();
            void Phase(){do{simulation.Step();}while(simulation.Rebuilding||simulation.Tick%5!=0);}
            Phase();var supply=game.Survival.At(feed).Storage;var receiver=game.Survival.At(chestPos).Storage;var furnace=game.Survival.At(furnacePos).Furnace;
            supply.Add(BlockId.RawIron,32);game.Survival.At(controller).ItemInputPriority=40;game.Survival.At(p).ItemInputPriority=30;Phase();Check(furnace.Slots[0].Count==1&&receiver.Total(BlockId.RawIron)==0,"Real network feeds default machine priority 50 before storage");
            receiver.ItemInputPriority=90;Phase();Check(receiver.Total(BlockId.RawIron)==1,"Real network custom chest priority 90 overrides machine default");receiver.ItemInputPriority=50;
            int machineBefore=furnace.Slots[0].Count,chestBefore=receiver.Total(BlockId.RawIron);for(int i=0;i<6;i++)Phase();Check(furnace.Slots[0].Count==machineBefore+3&&receiver.Total(BlockId.RawIron)==chestBefore+3,"Real equal-priority furnace and chest round robin across six items");
            game.Survival.At(singlePos).Crate.Lock(BlockId.Cobblestone);game.Survival.At(singlePos).ItemInputPriority=100;Phase();Check(game.Survival.At(singlePos).Crate.Count==0,"Priority 100 cobblestone crate never receives incompatible iron");
            Aim(world.Local(chestPos)+new Vector3(.5f,.9f,.5f),world.Local(chestPos)+new Vector3(.5f,.5f,-2));yield return null;Check(game.TryInteractTarget()&&game.OpenStation?.Block==BlockId.Chest,"Open chest priority control");yield return Capture("chest-item-priority");game.SetMode(ScreenMode.Play);Aim(world.Local(furnacePos)+new Vector3(.5f,.9f,.5f),world.Local(furnacePos)+new Vector3(.5f,.5f,-2));yield return null;Check(game.TryInteractTarget()&&game.OpenStation?.Block==BlockId.Furnace,"Open furnace priority control");yield return Capture("machine-item-priority");game.SetMode(ScreenMode.Play);
            game.Inventory.Add(IndustryId.Wrench,1);game.Selected=game.Inventory.FindSlot(s=>s.Id==IndustryId.Wrench);Aim(world.Local(pipeStart)+new Vector3(2,.4f,.7f),world.Local(pipeStart)+new Vector3(5,1,-4));yield return new WaitForSecondsRealtime(.5f);yield return Capture("item-priority-network");
            Check(world.Mine(singlePos,CrateId.Crate,ToolCapability.None),"Empty filter-test crate recovers without affecting warehouse");game.Survival.At(p).ItemInputPriority=77;game.Survival.At(controller).ItemInputPriority=65;
            var electricPos=p.Offset(-4,0,1);Put(electricPos,IndustryId.ElectricFurnace);AimAt(electricPos);yield return null;Check(game.TryInteractTarget()&&game.OpenMachine?.Definition.Id==IndustryId.ElectricFurnace,"Open electrical machine receiver controls");priorityField=game.UI.VisibleRoot.GetComponentsInChildren<InputField>().Single(f=>f.name=="Item pipe priority");Check(priorityField.text=="50","Industrial machine defaults to item priority 50");priorityField.text="88";priorityField.onEndEdit.Invoke("88");Check(game.OpenMachine.ItemInputPriority==88&&game.OpenMachine.Priority==1,"Numeric item priority remains independent of electrical priority");yield return Capture("electric-machine-item-priority");
            game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"Saves"));Check(game.SaveGame("Crate warehouse"),"Save crate counts, locks and controller: "+game.SaveStatus);var entry=game.Saves.List().First(e=>!e.Backup);
            Check(game.LoadGame(entry),"Load crate checkpoint: "+game.SaveStatus);FreezeSaveFixture();game.Animals.enabled=false;game.enabled=false;world=game.World;player=game.Player;yield return Settle(120);game.enabled=true;yield return null;game.enabled=false;
            Check(game.Survival.At(p).Crate.Count==1024&&game.Survival.At(p).Crate.Locked&&game.Survival.At(second).Crate.Count==512&&game.Survival.At(third).Crate.Locked&&game.Survival.At(third).Crate.Item==BlockId.Dirt,"Save roundtrip retains exact counts and empty/occupied locks");Check(game.Crates.At(controller).Members.Count==3,"Loaded controller resolves new physical state instances");Check(game.Survival.At(p).ItemInputPriority==77&&game.Survival.At(controller).ItemInputPriority==65&&game.Survival.At(chestPos).ItemInputPriority==50,"Save roundtrip retains per-receiver custom priorities");Check(game.Industry.Simulation.At(electricPos).ItemInputPriority==88&&game.Industry.Simulation.At(electricPos).Priority==1,"Save roundtrip preserves independent machine item/electrical priorities");
            byte[] original=File.ReadAllBytes(entry.Path),body;using(var reader=game.Saves.Open(original,out _))body=reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position));var intactWorld=world;var intactCrates=game.Crates;
            File.WriteAllBytes(entry.Path,game.Saves.Encode(entry,w=>w.Write(body,0,body.Length-1)));Check(!game.LoadGame(entry)&&game.World==intactWorld&&game.Crates==intactCrates&&game.Survival.At(p).Crate.Count==1024,"Late load failure restores original world and crate authority without stock loss");File.WriteAllBytes(entry.Path,original);
            game.SetMode(ScreenMode.Pause);player.transform.position+=Vector3.right*704;yield return null;yield return Settle(120);Check(!world.Ready(p)&&game.Crates.At(controller).Insert(new ItemStack(BlockId.Stone,1))==0&&game.Survival.At(p).Crate.Count==1024,"Dormant controller refuses transfer and preserves stock without loading terrain");
        }
    }
}
