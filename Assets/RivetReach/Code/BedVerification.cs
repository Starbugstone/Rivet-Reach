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
        IEnumerator ReviewBedResume()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rr-save-directory");Check(index>=0,"Fresh bed resume uses isolated checkpoint directory");
            game.InitializeSaves(args[index+1]);Check(game.ContinueLatestSave(),"Continue bed checkpoint in fresh process: "+game.SaveStatus);FreezeSaveFixture();game.Animals.enabled=false;game.enabled=false;
            yield return Settle(120);game.enabled=true;yield return null;game.enabled=false;
            var home=game.Beds.Home;Check(home.HasValue&&game.World.BedAt(home.Value)?.Identity==game.Beds.HomeIdentity,"Fresh process retains bed/home identity");
            Check(game.Beds.Policy.Percentage==75&&game.Beds.Policy.MinimumCount==2&&game.Beds.TryRespawn(out _),"Fresh process retains policy and safe arrival");
            Check(!game.Creative&&game.World.Get(home.Value)==BedId.Bed&&game.World.Get(game.World.BedAt(home.Value).Head)==BedId.Head,"Fresh process retains both cells and normal Survival mode");
            yield return Capture("bed-continued");
        }
        IEnumerator ReviewBeds()
        {
            FreezeSaveFixture();game.Animals.enabled=false;game.Animals.Clear();game.enabled=false;game.SetCreative(false);yield return Settle();
            var world=game.World;var player=game.Player;var start=world.Address(player.transform.position);
            var p=new BlockPos(start.Chunk.Min.X+31,start.Y+3,start.Z+5);
            void Put(BlockPos cell,byte id)
            {byte old=world.Get(cell);if(old==id)return;if(old!=0)Check(world.Remove(cell,old),"Clear bed fixture");if(id!=0)Check(Fluids.IsFluid(id)?world.ChangeFluid(cell,0,id):world.Place(cell,id),"Place bed fixture "+id);}
            for(int x=-4;x<=4;x++)for(int z=-3;z<=3;z++)
            {Put(p.Offset(x,-1,z),BlockId.Planks);for(int y=0;y<3;y++)Put(p.Offset(x,y,z),0);}
            void Aim(Vector3 at,Vector3 from)
            {player.transform.position=from;player.ResetMotion();player.Camera.transform.position=from+Vector3.up*1.5f;player.Camera.transform.LookAt(at);var d=player.Camera.transform.forward;player.Yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(d.y)*Mathf.Rad2Deg;}
            void AimBed()=>Aim(world.Local(p)+new Vector3(.5f,.65f,.5f),world.Local(p)+new Vector3(-2,.02f,-.5f));
            var bench=p.Offset(-3,0,2);Put(bench,BlockId.Workbench);Put(p.Offset(-2,0,2),BlockId.Torch);game.Sky.Clock.SetTime(.4);game.Sky.Apply();
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,64);game.Inventory.Add(FarmId.Cloth,3);game.Inventory.Add(BlockId.Planks,3);
            game.SetMode(ScreenMode.Play);Aim(world.Local(bench)+Vector3.one*.5f,world.Local(bench)+new Vector3(.5f,.02f,-2));yield return null;
            Check(game.TryInteractTarget(),"Open real workbench for bed crafting");game.UI.InspectBrowserItem(BedId.Bed,false);yield return Capture("bed-recipe");game.UI.CloseBrowserRecipe();
            Check(game.Crafting.FillRecipe("rivet:bed",game.Inventory)==RecipeFillStatus.Filled,"Fill bed recipe from ordinary inventory");yield return Capture("bed-crafting");
            Check(game.Crafting.CraftToInventory(game.Inventory,1).Succeeded&&game.Inventory.Total(BedId.Bed)==1&&game.Inventory.Total(FarmId.Cloth)==0&&game.Inventory.Total(BlockId.Planks)==0&&game.Crafting.Grid.Slots.All(s=>s.Empty),"Craft one Bed from exactly three Cloth and three Planks");yield return Capture("bed-crafted");
            game.SetMode(ScreenMode.Play);game.Selected=game.Inventory.FindSlot(s=>s.Id==BedId.Bed);
            Aim(world.Local(p)+new Vector3(.5f,-.01f,.5f),world.Local(p)+new Vector3(-2,.02f,.5f));yield return null;
            Check(game.PlacementPreview(out var preview,out _)&&preview.Equals(p),"Real placement ray selects supported bed foot");
            Put(p.Offset(1,0,0),BlockId.Stone);Check(!game.TryPlaceSelected()&&game.Inventory.Total(BedId.Bed)==1&&world.Get(p)==0,"Blocked second half rejects whole transaction without consuming item");Put(p.Offset(1,0,0),0);
            Check(game.TryPlaceSelected()&&game.Inventory.Total(BedId.Bed)==0,"Actual bed placement consumes exactly one crafted item");
            var bed=world.BedAt(p);Check(bed!=null&&bed.Rotation==1&&bed.Head.Equals(p.Offset(1,0,0))&&!bed.Foot.Chunk.Equals(bed.Head.Chunk),"Bed pair spans a real chunk boundary with correct facing");
            Check(world.Get(p)==BedId.Bed&&world.Get(bed.Head)==BedId.Head,"Both cells publish as one bed");
            Check(!world.Select(world.Local(p)+new Vector3(-1,.15f,.5f),Vector3.right,4,out var under)||!BedId.Part(under.Block),"Selection rays pass through the authored gap beneath the bed");AimBed();yield return new WaitForSecondsRealtime(.4f);
            Check(world.GetComponent<BedPresentation>().ViewAt(p)!=null,"Resident paired bed has original rendered model");yield return Capture("bed-placed");
            player.enabled=true;yield return null;InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSecondsRealtime(.2f);
            Check(game.Beds.Home.HasValue&&game.Beds.Home.Value.Equals(p)&&game.Beds.HomeIdentity==bed.Identity&&Math.Abs(game.Sky.Clock.TotalDays-.4)<1e-10,"Bound daytime Use sets home without changing time");
            yield return Capture("bed-home-set");InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;player.enabled=false;
            var spare=p.Offset(0,0,-2);
            for(int rotation=0;rotation<4;rotation++)
            {
                int recovered=game.Items.Total(BedId.Bed);Check(world.PlaceBed(spare,rotation),"Place bed cardinal rotation "+rotation);
                var placed=world.BedAt(spare);Check(placed.Rotation==rotation&&ReferenceEquals(world.BedAt(placed.Head),placed),"Both halves resolve the same rotated state "+rotation);
                Check(world.Mine(spare,BedId.Bed,ToolCapability.None)&&game.Items.Total(BedId.Bed)==recovered+1,"Foot mining recovers exactly one rotated bed "+rotation);
            }
            Put(spare.Offset(0,-1,0),0);Check(!world.PlaceBed(spare,0)&&world.Get(spare)==0&&world.Get(spare.Offset(0,0,1))==0,"Missing foot support rejects both cells");Put(spare.Offset(0,-1,0),BlockId.Planks);
            Put(spare,Fluids.Water.Source);Check(!world.PlaceBed(spare,0)&&world.Get(spare)==Fluids.Water.Source,"Water cannot be overwritten by a bed");Put(spare,0);
            var pick=new Inventory(id=>game.Registry.Get(id).stackLimit);InventoryActions.Pick(pick,game.Registry,BedId.Head,0,true);Check(pick.Slots[0].Id==BedId.Bed&&pick.Slots[0].Count==1,"Creative Pick Block on head produces public Bed item");
            // Existing production states must not earn a skipped night.
            var chicken=game.Animals.Spawn(world.Local(p.Offset(3,0,2))+new Vector3(.5f,.006f,.5f));Check(chicken!=null,"Create ordinary passive timer state beside the bed");
            var cookerPos=p.Offset(-3,0,-2);Put(cookerPos,FarmId.Cooker);var cooker=game.Industry.Simulation.At(cookerPos);cooker.Items.Add(BlockId.Potato,2,0,1);cooker.Items.Add(BlockId.Coal,1,3,4);
            byte[] State(Action<SaveWriter> write){using var buffer=new MemoryStream();using(var writer=new SaveWriter(buffer))write(writer);return buffer.ToArray();}
            byte[] Production()=>State(w=>{world.WriteSave(w);game.Survival.WriteSave(w);game.Industry.Simulation.WriteSave(w);game.Animals.WriteSave(w);game.Hunger.WriteSave(w);});
            AimBed();game.Sky.Clock.SetTime(.875);game.Sky.Apply();yield return Capture("bed-night");var before=Production();
            Check(game.TryInteractTarget()&&game.Sky.Clock.TotalDays==1.25,"Night bed use advances exactly to next 06:00");Check(before.SequenceEqual(Production()),"Sleep leaves terrain growth, fluids, animals, machinery and hunger byte-for-byte unchanged");yield return Capture("bed-morning");
            Aim(world.Local(bed.Head)+new Vector3(.5f,.8f,.5f),world.Local(bed.Head)+new Vector3(2,.02f,.5f));game.Sky.Clock.SetTime(2.125);Check(game.TryInteractTarget()&&game.Sky.Clock.TotalDays==2.25,"Head-half use after midnight reaches this morning, not another full day");
            // Actual held use cannot advance another day, and the bound Interact shares the route.
            player.enabled=true;yield return null;InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSecondsRealtime(.4f);Check(game.Sky.Clock.TotalDays==2.25,"Holding Use at morning cannot repeatedly advance days");InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;player.enabled=false;
            Check(game.Beds.TryRespawn(out var arrival),"Bed has a supported clear respawn position");
            game.Health.Damage(100,DamageKind.Impact,0);game.SetMode(ScreenMode.Death);game.Respawn();Check(!game.Health.Dead&&world.Address(player.transform.position).Equals(arrival),"Actual death Respawn resolves to the saved home");player.enabled=true;yield return new WaitForSecondsRealtime(.15f);player.enabled=false;Aim(world.Local(p)+new Vector3(.8f,.65f,.5f),player.transform.position);player.enabled=true;yield return new WaitForSecondsRealtime(.15f);player.enabled=false;yield return Capture("bed-respawn");
            // Block every neighboring arrival; a failed use must not overwrite home.
            for(int x=-1;x<=2;x++)for(int z=-1;z<=1;z++){var q=p.Offset(x,0,z);if(world.BedAt(q)==null)Put(q,BlockId.Stone);}
            Check(!game.Beds.TryRespawn(out _),"Fully obstructed arrival ring rejects bed respawn");
            for(int x=-1;x<=2;x++)for(int z=-1;z<=1;z++){var q=p.Offset(x,0,z);if(world.BedAt(q)==null)Put(q,0);}
            Check(game.Beds.TryRespawn(out _),"Clearing arrival ring restores safe bed access");
            long identity=bed.Identity;int drops=game.Items.Piles.Sum(d=>d.Stack.Id==BedId.Bed?d.Stack.Count:0);
            Check(world.Mine(bed.Head,BedId.Head,ToolCapability.None),"Mine the head through ordinary mining");
            Check(world.Get(p)==0&&world.Get(bed.Head)==0&&world.Beds.Count==0&&game.Items.Piles.Sum(d=>d.Stack.Id==BedId.Bed?d.Stack.Count:0)==drops+1,"Either half recovers exactly one item and removes the whole pair");
            Check(world.PlaceBed(p,1)&&world.BedAt(p).Identity!=identity&&!game.Beds.TryRespawn(out _),"Replacement at identical coordinates cannot revive destroyed home identity");
            game.Health.Damage(100,DamageKind.Impact,0);game.SetMode(ScreenMode.Death);game.Respawn();Check(!game.Health.Dead&&(game.Message?.Contains("unavailable")??false),"Destroyed home reports normal world-spawn fallback");
            AimBed();yield return Settle();game.enabled=true;yield return null;game.enabled=false;game.SetMode(ScreenMode.Play);yield return null;Check(game.TryInteractTarget()&&game.Beds.TryRespawn(out _),"Using replacement deliberately assigns its new identity");
            bed=world.BedAt(p);game.Beds.ConfigureSleepPolicy(75,2);
            game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"Saves"));Check(game.SaveGame("Bed checkpoint"),"Save bed pair/home: "+game.SaveStatus);var entry=game.Saves.List().First(e=>!e.Backup);
            Check(game.LoadGame(entry),"Load bed checkpoint: "+game.SaveStatus);FreezeSaveFixture();game.Animals.enabled=false;game.enabled=false;world=game.World;player=game.Player;yield return Settle(120);game.enabled=true;yield return null;game.enabled=false;
            Check(world.BedAt(p)?.Identity==bed.Identity&&game.Beds.HomeIdentity==bed.Identity&&game.Beds.TryRespawn(out _)&&game.Beds.Policy.Percentage==75&&game.Beds.Policy.MinimumCount==2,"Restore exact bed identity, home and configured sleep threshold");
            byte[] original=File.ReadAllBytes(entry.Path),body;using(var reader=game.Saves.Open(original,out _))body=reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position));
            var intactWorld=world;var intactBeds=game.Beds;File.WriteAllBytes(entry.Path,game.Saves.Encode(entry,w=>w.Write(body,0,body.Length-1)));
            Check(!game.LoadGame(entry)&&game.World==intactWorld&&game.Beds==intactBeds&&world.BedAt(p)?.Identity==bed.Identity,"Late bed-section failure rolls back original world and home binding");File.WriteAllBytes(entry.Path,original);
            // Travel far enough to unload the home; authority and saved footprint remain.
            game.SetMode(ScreenMode.Pause);player.transform.position+=Vector3.right*704;yield return null;yield return Settle(120);
            Check(!world.Ready(p)&&game.Beds.TryRespawn(out arrival),"Distant unloaded bed validates through authoritative world state");
            game.Health.Damage(100,DamageKind.Impact,0);game.SetMode(ScreenMode.Death);game.Respawn();Check(world.Address(player.transform.position).Equals(arrival)&&game.WaitingForRespawn,"Distant home respawn waits for destination terrain");
            yield return Settle(120);game.enabled=true;yield return null;game.enabled=false;Check(!game.WaitingForRespawn,"Destination residency releases respawn wait");
            bed=world.BedAt(p);drops=game.Items.Piles.Sum(d=>d.Stack.Id==BedId.Bed?d.Stack.Count:0);Check(world.Mine(bed.Head.Offset(0,-1,0),BlockId.Planks,ToolCapability.None),"Mine second supporting floor");
            Check(world.BedAt(p)==null&&world.Get(p)==0&&world.Get(bed.Head)==0&&game.Items.Piles.Sum(d=>d.Stack.Id==BedId.Bed?d.Stack.Count:0)==drops+1,"Lost support across boundary recovers one whole bed");
        }
    }
}
