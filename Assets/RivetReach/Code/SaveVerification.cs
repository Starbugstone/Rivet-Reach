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
        Button SaveButton(string label)=>game.UI.VisibleRoot.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text==label);
        void FreezeSaveFixture(){game.SetMode(ScreenMode.Pause);game.Player.enabled=false;game.Items.enabled=false;game.Mobs.enabled=false;game.World.ViewDistance=4;game.Diagnostics=false;}
        IEnumerator ReviewSaveGame()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rr-save-directory");
            string directory=index>=0?args[index+1]:Path.Combine(output,"Saves");game.InitializeSaves(directory);
            if(args.Contains("-rr-save-resume-review"))
            {
                game.SetMode(ScreenMode.Title);yield return null;
                Check(!game.Started&&SaveButton("CONTINUE LATEST SAVE").interactable,"Fresh process discovers the previous process's checkpoint");
                SaveButton("CONTINUE LATEST SAVE").onClick.Invoke();FreezeSaveFixture();
                Check(game.Started&&game.Inventory.Total(BlockId.Diamond)==37&&game.SaveName=="Cross-process expedition","Continue latest restores disk progress after restarting the executable: "+game.SaveStatus);
                Check(!game.Creative&&game.Selected==9&&game.Player.Female&&game.Player.Skin==1,"Appearance, hotbar and session-only Creative contract survive process restart");
                Check(game.Sky.Clock.TotalDays==12.875&&game.World.EditCount>0&&game.Survival.StationCount==4,"World edits, containers and lunar time survive process restart");
                yield return Settle(120);yield return Capture("continued-expedition");yield break;
            }
            FreezeSaveFixture();yield return Settle();
            Check(game.Saves.List().Count==0&&!game.ContinueLatestSave(),"Empty save directory has no Continue candidate");
            var world=game.World;var sim=game.Industry.Simulation;var start=world.Address(game.Player.transform.position);var origin=start.Offset(6,2,6);
            void Put(BlockPos p,byte id)
            {byte before=world.Get(p);if(before!=0)Check(world.Remove(p,before),"Clear save fixture "+p);if(id!=0)Check(world.Place(p,id),"Place saved block "+id+" at "+p);}
            for(int x=0;x<14;x++)for(int z=0;z<6;z++)for(int y=-1;y<4;y++)Put(origin.Offset(x,y,z),y==-1?BlockId.Stone:(byte)0);
            var chest=origin;Put(chest,BlockId.Chest);game.Survival.At(chest).Storage.Add(BlockId.IronIngot,23,7,8);
            var bench=origin.Offset(1,0,0);Put(bench,BlockId.Workbench);game.Survival.At(bench).Crafting.Grid.Add(BlockId.Planks,8,4,5);
            var machinist=origin.Offset(2,0,0);Put(machinist,IndustryId.Bench);game.Survival.At(machinist).Crafting.Grid.Add(IndustryId.Cog,3,15,16);
            var furnace=origin.Offset(3,0,0);Put(furnace,BlockId.Furnace);var f=game.Survival.At(furnace).Furnace;
            var held=new ItemStack(BlockId.RawIron,4);f.Click(0,ref held,false);held=new ItemStack(BlockId.Coal,2);f.Click(1,ref held,false);f.Advance(73);game.Survival.Wake(furnace);
            var crop=origin.Offset(4,0,0);Put(crop,BlockId.Dirt);Check(world.Till(crop)&&world.Plant(crop.Offset(0,1,0)),"Plant scheduled crop for save");game.Survival.AdvanceTicks(41);
            var torch=origin.Offset(0,1,0);Check(world.PlaceTorch(torch,chest),"Save torch support relationship");
            var water=origin.Offset(5,0,0);Check(world.ChangeFluid(water,0,Fluids.Water.Source),"Save pending source propagation");
            // Compact formed tank; storage must also survive a breached shell.
            var tankOrigin=origin.Offset(7,0,0);var bounds=new StructureBounds(tankOrigin,tankOrigin.Offset(2,2,2));
            for(int x=0;x<3;x++)for(int y=0;y<3;y++)for(int z=0;z<3;z++){var p=tankOrigin.Offset(x,y,z);int axes=bounds.BoundaryAxes(p);if(axes>0)Put(p,axes>=2?IndustryId.TankFrame:y==0||y==2?IndustryId.TankWall:IndustryId.TankGlass);}
            var controller=tankOrigin.Offset(1,1,0);Put(controller,IndustryId.TankController);
            var battery=origin.Offset(11,0,0);Put(battery,IndustryId.Battery);sim.At(battery).EnergyCells[0].Charge(1234567);sim.At(battery).BatteryMode=BatteryMode.Isolated;
            var bank=origin.Offset(12,0,0);Put(bank,IndustryId.BatteryController);
            var pipe=origin.Offset(11,0,3);Put(pipe,IndustryId.FluidPipe);sim.At(pipe).Additions=PipeAddition.Signal|PipeAddition.Power;
            var machine=origin.Offset(12,0,3);Put(machine,IndustryId.Crusher);sim.At(machine).Items.Add(BlockId.RawCopper,6,0,1);sim.At(machine).Items.Add(IndustryId.CrushedCopper,7,2,3);sim.At(machine).Work=39.5;sim.At(machine).WorkInput=BlockId.RawCopper;sim.At(machine).Priority=2;
            for(int i=0;i<10;i++)sim.Step();
            var tank=sim.At(controller).Structure;Check(tank.Formed&&tank.Fluid.Deposit(Fluids.Water,123457),"Form tank and store exact non-bucket quantity: "+tank.Validation.Message);var identity=tank.StructureId;
            Check(world.Remove(tankOrigin.Offset(0,1,1),IndustryId.TankGlass),"Breach filled tank before saving");sim.Multiblocks.Step();
            Check(!tank.Formed&&tank.Fluid.Amount==123457,"Breached tank retains recovery contents");
            Check(sim.At(bank).Structure.Formed,"Battery bank fixture formed");
            game.Inventory.Add(BlockId.Diamond,37,9,10);game.PersonalCrafting.Grid.Add(BlockId.Log,5,3,4);game.UI.HeldStack=new ItemStack(BlockId.Stick,11);
            game.Hunger.Exert(9.25);game.Health.Damage(5,DamageKind.Impact,0);game.Health.Advance(23,game.Hunger);
            var armor=game.Registry.items.First(d=>d.armorSlot==ArmorSlot.Head);held=new ItemStack(armor.runtimeId,1);game.Equipment.Click(0,ref held,false);
            game.Selected=9;game.Player.Female=true;game.Player.Skin=1;game.Player.Yaw=123;game.Player.Pitch=-23;game.Player.RefreshAppearance();game.SetCreative(true);
            game.Items.Spawn(new ItemStack(BlockId.RawGold,17),world.Local(origin.Offset(3,2,4)),Vector3.right,3,true);game.Items.Piles.Last().Age=127.5f;
            var mob=game.Mobs.Spawn(game.Mobs.Definitions[0],world.Local(origin.Offset(4,0,4))+new Vector3(.5f,.006f,.5f));
            Check(mob!=null,"Create saved creature on supported terrain");mob.Health-=3;mob.Anger=4.75f;mob.Intent=MobIntent.Chase;
            // Tree work is scheduled without advancing it; its frontier must survive a checkpoint.
            BlockPos? natural=null;
            for(int x=-24;x<=24&&!natural.HasValue;x++)for(int z=-24;z<=24&&!natural.HasValue;z++)
            {int h=world.Generator.Height(x,z);var p=new BlockPos(x,h+1,z);if(world.NaturalLog(p))natural=p;}
            if(natural.HasValue)world.Trees.FellAbove(world,natural.Value);
            for(int slot=1;slot<game.Inventory.Count;slot++)if(game.Inventory.Slots[slot].Empty)game.Inventory.Add(BlockId.Stone,64,slot,slot+1);
            game.Sky.Clock.SetTime(12.875);game.Sky.Apply();game.SetMode(ScreenMode.Save);yield return null;
            var name=game.UI.VisibleRoot.GetComponentInChildren<InputField>();name.text="Cross-process expedition";SaveButton("SAVE GAME").onClick.Invoke();
            Check(game.SaveId!=null&&game.Inventory.Total(BlockId.Stick)==11&&game.UI.HeldStack.Empty,"Save button commits named slot and cursor items: "+game.SaveStatus);File.WriteAllText(Path.Combine(output,"save-cost.txt"),$"Fixture save: {game.LastSaveBytes} bytes; {game.LastSaveMilliseconds:0.###} ms synchronous capture, checksum, flush and publish. Single bounded workshop on this workstation; not a large-world guarantee.\n");yield return Capture("save-game");
            var entry=game.Saves.List().First(e=>!e.Backup);byte[] original=game.Saves.Read(entry);int edits=world.EditCount,pending=world.FluidSimulation.Pending,leaves=world.Trees.PendingLeaves,fells=world.Trees.PendingFells;long cropTick=game.Survival.Tick;
            int furnaceProgress=f.ProgressTicks,burn=f.BurnTicks,scheduledCrops=game.Survival.ScheduledCrops;
            game.Inventory.Take(9,37);game.Survival.At(chest).Storage.Take(7,23);sim.At(battery).EnergyCells[0].Discharge(1234567);
            Check(game.LoadGame(entry),"Load complete checkpoint: "+game.SaveStatus);FreezeSaveFixture();
            Check(game.CaptureSave(entry).SequenceEqual(original),"Every serialized field round-trips byte-for-byte before simulation resumes");
            Check(game.World.EditCount==edits&&game.World.FluidSimulation.Pending==pending&&game.World.Trees.PendingLeaves==leaves&&game.World.Trees.PendingFells==fells,"Terrain overlay and all pending world queues restored");
            Check(game.PersonalCrafting.Grid.Slots[3].Count==5,"Full inventory retains personal crafting ingredients across save/load");
            Check(game.Survival.Tick==cropTick&&game.Survival.ScheduledCrops==scheduledCrops&&game.Survival.At(furnace).Furnace.ProgressTicks==furnaceProgress&&game.Survival.At(furnace).Furnace.BurnTicks==burn,"Crop clock and furnace work/fuel restored exactly");
            Check(game.Industry.Simulation.At(controller).Structure.StructureId==identity&&game.Industry.Simulation.At(controller).Structure.Fluid.Amount==123457,"Breached tank identity and exact recovery contents restored");
            Check(game.Industry.Simulation.At(battery).EnergyCells[0].Amount==1234567&&game.Industry.Simulation.At(pipe).Additions==(PipeAddition.Signal|PipeAddition.Power),"Per-cell energy and independent pipe channels restored");
            Check(game.World.TorchSupport(torch,out var support)&&support.Equals(chest),"Torch attachment restored");
            Check(game.Items.Piles.Any(p=>p.Stack.Id==BlockId.RawGold&&p.Stack.Count==17&&p.Age==127.5f&&p.ActionCreated),"Dropped stack lifetime and pickup provenance restored");
            Check(!game.Creative&&!game.Player.Flying,"Creative permission and flight reset on load");
            Check(game.Mobs.Mobs.Any(m=>m.Id==mob.Id&&m.Health==mob.Health&&m.Anger==4.75f),"Creature identity, damage and intent restored");
            yield return Settle(120);
            for(int i=0;i<10;i++)game.Industry.Simulation.Multiblocks.Step();
            var restoredTank=game.Industry.Simulation.At(controller).Structure;
            Check(!restoredTank.Formed&&restoredTank.Fluid.Amount==123457,"Breached tank remains closed after residency validation");
            Check(game.World.Place(tankOrigin.Offset(0,1,1),IndustryId.TankGlass),"Repair loaded tank shell");for(int i=0;i<10;i++)game.Industry.Simulation.Multiblocks.Step();
            Check(restoredTank.Formed&&restoredTank.Fluid.Amount==123457&&restoredTank.StructureId==identity,"Repair loaded tank without fluid loss or new identity");
            Check(game.Industry.Simulation.At(bank).Structure.Formed&&BatteryPower.Amount(game.Industry.Simulation.At(bank))==1234567,"Bank reforms with exact original cell energy");
            // Restart from the disk checkpoint so subsequent corruption tests have a known state.
            Check(game.LoadGame(entry),"Repeated load starts from disk checkpoint");FreezeSaveFixture();
            var intactWorld=game.World;byte[] broken=(byte[])original.Clone();broken[broken.Length-1]^=1;File.WriteAllBytes(entry.Path,broken);
            Check(!game.LoadGame(entry)&&game.World==intactWorld&&game.Inventory.Total(BlockId.Diamond)==37,"Checksum failure preserves current world and inventory");
            File.WriteAllBytes(entry.Path,original.Take(original.Length/2).ToArray());Check(!game.LoadGame(entry)&&game.World==intactWorld,"Truncated file preserves active session");
            var future=(byte[])original.Clone();using(var stream=new MemoryStream(future)){using var reader=new BinaryReader(stream);reader.ReadString();long offset=stream.Position;using var writer=new BinaryWriter(stream);stream.Position=offset;writer.Write(999);}
            File.WriteAllBytes(entry.Path,future);Check(!game.LoadGame(entry)&&game.World==intactWorld,"Future schema version rejects without changing the session");
            // A valid envelope with bad section data exercises staged restore rollback, not only hashing.
            byte[] semantic=game.Saves.Encode(entry,w=>{w.Write("rivet:surface");w.Point(WorldPoint.FromLocal(game.Player.transform.position,game.World.Origin));w.Write(1200.0);w.Write(12.875);w.Write(9);w.Write(0f);w.Write(-1);});
            File.WriteAllBytes(entry.Path,semantic);Check(!game.LoadGame(entry)&&game.World==intactWorld&&intactWorld.gameObject.activeSelf,"Invalid body rolls back prepared session and reactivates original");
            byte[] body;using(var reader=game.Saves.Open(original,out _)){body=reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position));}
            File.WriteAllBytes(entry.Path,game.Saves.Encode(entry,w=>w.Write(body,0,body.Length-1)));
            Check(!game.LoadGame(entry)&&game.World==intactWorld&&game.Selected==9&&game.SaveId==entry.Id,"Late entity-section failure preserves world, selected slot and save identity");
            File.WriteAllBytes(entry.Path,original);
            using(var locked=new FileStream(entry.Path,FileMode.Open,FileAccess.Read,FileShare.None))Check(!game.SaveGame(),"Locked save file reports write failure and keeps the expedition open");
            Check(File.ReadAllBytes(entry.Path).SequenceEqual(original),"Failed replacement leaves the previous checkpoint unchanged");
            Check(game.SaveGame(),"Overwrite slot atomically: "+game.SaveStatus);Check(File.Exists(entry.Path+".bak"),"Previous checkpoint backup exists");
            File.WriteAllBytes(entry.Path,broken);var backup=game.Saves.List().Single(e=>e.Backup);Check(game.ContinueLatestSave(),"Continue falls back to valid backup after primary corruption");FreezeSaveFixture();
            Check(game.Inventory.Total(BlockId.Diamond)==37&&game.SaveGame(),"Recovered checkpoint can be saved again");Check(File.ReadAllBytes(entry.Path+".bak").SequenceEqual(original),"Recovery preserves good backup instead of copying corrupt primary over it");
            File.WriteAllText(Path.Combine(directory,"ignored.rrsave.tmp"),"interrupted");Check(game.Saves.List().Count==2,"Interrupted temporary file is not a Continue candidate");
            string blocked=Path.Combine(output,"not-a-directory");File.WriteAllText(blocked,"fixture");game.InitializeSaves(blocked);Check(!game.SaveGame(),"Disk path failure is reported without discarding progress");game.InitializeSaves(directory);
            Check(game.SaveGame("Second slot",true),"Save As creates another named slot");string secondId=game.SaveId;Check(secondId!=entry.Id,"Save As uses a distinct slot identity");
            game.SetMode(ScreenMode.Load);yield return null;Check(game.UI.VisibleRoot.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Cross-process expedition")),"Load menu lists named checkpoints");yield return Capture("load-game");
            Check(game.LoadGame(entry),"Choose older named save independently of latest");FreezeSaveFixture();
            // Preserve edits and stations while the player is beyond the original chunk residency.
            game.Player.transform.position+=Vector3.right*704;yield return null;yield return Settle(120);var remote=WorldPoint.FromLocal(game.Player.transform.position,game.World.Origin);
            Check(!game.World.Ready(chest)&&game.SaveGame("Distant checkpoint",true),"Save includes unloaded station and world state");var distant=game.Saves.List().First(e=>e.Id==game.SaveId&&!e.Backup);
            Check(game.LoadGame(distant),"Load distant checkpoint: "+game.SaveStatus);FreezeSaveFixture();
            var actual=WorldPoint.FromLocal(game.Player.transform.position,game.World.Origin);Check(actual.Cell.Equals(remote.Cell)&&actual.Fraction==remote.Fraction&&game.Survival.At(chest).Storage.Slots[7].Count==23,"Floating-origin position and unloaded container restored exactly");
            Check(game.LoadGame(entry),"Return to original named checkpoint");FreezeSaveFixture();
            yield return Settle(120);game.SetMode(ScreenMode.Play);game.TakeDamage(100,DamageKind.Impact);
            Check(game.Health.Dead&&game.Mode==ScreenMode.Death&&game.SaveGame("Death checkpoint",true),"Death can save its dropped possessions and death screen");
            var dead=game.Saves.List().First(e=>e.Id==game.SaveId&&!e.Backup);
            Check(game.LoadGame(dead)&&game.Health.Dead&&game.Mode==ScreenMode.Death,"Loading death checkpoint cannot bypass death/respawn rules");
            Check(game.LoadGame(entry),"Load living checkpoint from death screen");FreezeSaveFixture();
            Check(game.SaveGame("Cross-process expedition"),"Publish final checkpoint for fresh-process Continue");
            game.SaveAndTitle();yield return null;Check(game.Mode==ScreenMode.Title&&!game.Started,"Save and Title returns to title");yield return Capture("continue-latest-title");
            SaveButton("CONTINUE LATEST SAVE").onClick.Invoke();FreezeSaveFixture();Check(game.Started&&game.SaveName=="Cross-process expedition","Title Continue button loads the most recently written save");
        }
    }
}
