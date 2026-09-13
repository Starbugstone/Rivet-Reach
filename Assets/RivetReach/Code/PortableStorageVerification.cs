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
        IEnumerator ReviewPortableStorageLegacy()
        {
            var args=System.Environment.GetCommandLineArgs();int index=System.Array.IndexOf(args,"-rr-legacy-directory");
            Check(index>=0,"Legacy fixture directory supplied");string directory=args[index+1];game.InitializeSaves(directory);
            var entries=game.Saves.List().Where(e=>!e.Backup).ToArray();Check(entries.Length>=3,"Discover historical full-world fixtures");
            foreach(var entry in entries)
            {
                game.InitializeSaves(directory);var bytes=game.Saves.Read(entry);
                using(var reader=game.Saves.Open(bytes,out _))Check(reader.Format<8,"Fixture predates portable storage: schema "+reader.Format);
                var changed=Object.Instantiate(game.Registry);changed.Get(BlockId.IronIngot).stackLimit++;
                bool rejected=false;try{using var reader=new SaveStore(output,changed).Open(bytes,out _);}catch(InvalidDataException){rejected=true;}
                Destroy(changed);Check(rejected,"Old save still rejects unrelated content changes");
                Check(game.LoadGame(entry),"Load actual historical checkpoint: "+game.SaveStatus);FreezeSaveFixture();
                Check(game.Inventory.Slots.All(s=>!s.HasContents),"Legacy carried storage initializes empty");
                game.InitializeSaves(Path.Combine(output,"Migrated"));Check(game.SaveGame(entry.Name,true),"Write migrated schema 8 checkpoint");
                var migrated=game.Saves.List().First(e=>e.Id==game.SaveId&&!e.Backup);var fresh=game.Saves.Read(migrated);
                Check(game.LoadGame(migrated),"Reload migrated checkpoint: "+game.SaveStatus);FreezeSaveFixture();
                Check(game.CaptureSave(migrated).SequenceEqual(fresh),"Every migrated state field round-trips exactly");yield return null;
            }
        }
        IEnumerator ReviewPortableStorage()
        {
            FreezeSaveFixture();game.enabled=false;yield return Settle();
            var world=game.World;var sim=game.Industry.Simulation;var player=game.Player;
            var origin=world.Address(player.transform.position).Offset(0,2,5);
            void Put(BlockPos p,byte id)
            {byte old=world.Get(p);if(old!=0)Check(world.Remove(p,old),"Clear fixture cell");if(id!=0)Check(world.Place(p,id),"Place fixture "+id);}
            for(int x=-3;x<12;x++)for(int z=-2;z<5;z++)for(int y=-1;y<4;y++)Put(origin.Offset(x,y,z),y==-1?BlockId.Stone:(byte)0);
            void Select(ItemStack stack){game.Inventory.Take(0,int.MaxValue);Check(game.Inventory.Add(stack,0,1)==0,"Select carried storage");game.Selected=0;}
            void Aim(BlockPos p)
            {player.transform.position=world.Local(p)+new Vector3(.5f,.02f,-2.3f);player.transform.rotation=Quaternion.identity;player.Camera.transform.localPosition=Vector3.up*1.62f;player.Camera.transform.LookAt(world.Local(p)+Vector3.one*.5f);}
            foreach(byte id in new[]{IndustryId.Battery,IndustryId.Tank})
            {
                Put(origin,id);var m=sim.At(origin);
                if(id==IndustryId.Battery)m.EnergyCells[0].Charge(1234567);else m.Fluid.Deposit(Fluids.Lava,12345);
                var expected=PortableStorage.Capture(m);Check(!world.Remove(origin,id),"Direct removal cannot silently erase stored resource");
                int before=game.Items.Piles.Count;Check(world.Mine(origin,id,ToolCapability.Pickaxe),"Mine nonempty storage");
                Check(sim.At(origin)==null&&game.Items.Piles.Count==before+1&&game.Items.Piles.Last().Stack.Equals(expected),"Mining returns exactly one content-bearing item");
                var drop=game.Items.Piles.Last();Select(expected);game.Items.Piles.Remove(drop);
                // Actual player placement, including Creative: contents are consumed once.
                game.SetCreative(true);game.SetMode(ScreenMode.Play);player.transform.position=world.Local(origin)+new Vector3(.5f,.02f,-2.3f);player.Camera.transform.position=player.transform.position+Vector3.up*1.62f;player.Camera.transform.LookAt(world.Local(origin)+new Vector3(.5f,-.01f,.5f));
                Check(game.TryPlaceSelected(),"Place recovered item through player authority");
                Check(game.Inventory.Slots[0].Empty&&PortableStorage.Capture(sim.At(origin)).Equals(expected),"Creative placement cannot duplicate stored contents");
                sim.Step();Aim(origin);Check(game.TryOpenMachine(origin),"Open restored storage interface");
                yield return new WaitForSecondsRealtime(.4f);yield return Capture(id==IndustryId.Battery?"storage-restored-battery":"storage-restored-lava-tank");
                game.SetMode(ScreenMode.Pause);Check(world.Mine(origin,id,ToolCapability.Pickaxe),"Recover placed storage again");
                Check(game.Items.Piles.Last().Stack.Equals(expected),"Repeated mining preserves exact contents");
            }
            // Bank charge is owned by each cell; removing one returns only its share.
            var bank=origin.Offset(5,0,0);Put(bank,IndustryId.BatteryController);Put(bank.Offset(1,0,0),IndustryId.Battery);Put(bank.Offset(2,0,0),IndustryId.Battery);
            sim.At(bank.Offset(1,0,0)).EnergyCells[0].Charge(1111111);sim.At(bank.Offset(2,0,0)).EnergyCells[0].Charge(2222222);
            for(int i=0;i<5;i++)sim.Step();Check(sim.At(bank).Structure.Formed,"Bank fixture formed");
            Check(world.Mine(bank.Offset(1,0,0),IndustryId.Battery,ToolCapability.Pickaxe),"Mine charged bank member");
            Check(game.Items.Piles.Last().Stack.Energy==1111111&&sim.At(bank.Offset(2,0,0)).EnergyCells[0].Amount==2222222,"Only removed cell energy travels with its item");
            for(int i=0;i<3;i++)sim.Step();Check(!sim.At(bank).Structure.Formed,"Missing cell invalidates bank");
            // An invalid controller also recovers the exact shared liquid, including residue.
            var controller=origin.Offset(8,0,0);Put(controller,IndustryId.TankController);var tank=sim.At(controller).Structure;
            tank.Fluid.Resize(500000);tank.Fluid.Deposit(Fluids.Lava,250001);for(int i=0;i<3;i++)sim.Step();
            Check(!tank.Formed&&world.Mine(controller,IndustryId.TankController,ToolCapability.Pickaxe),"Recover nonempty invalid tank controller");
            var parcel=game.Items.Piles.Last().Stack;Check(parcel.FluidAmount==250001&&parcel.FluidCapacity==500000,"Controller carries entire shared storage exactly once");
            Put(controller,IndustryId.TankController);PortableStorage.Restore(sim.At(controller),parcel);game.Items.Piles.RemoveAt(game.Items.Piles.Count-1);
            Check(sim.At(controller).Structure.Fluid.Amount==250001,"Controller placement restores retained capacity and liquid");
            // Bucket UI and actual pipes accept lava; water cannot be mixed into it.
            Put(origin,IndustryId.Tank);var vessel=sim.At(origin);game.SetMode(ScreenMode.Play);Aim(origin);Check(game.TryOpenMachine(origin),"Open small tank");
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            game.Inventory.Add(Fluids.LavaBucket,1);Check(game.Industry.Bucket(vessel,true)&&vessel.Fluid.Fluid==Fluids.Lava&&vessel.Fluid.Amount==10000,"Lava bucket fills small tank");
            game.Inventory.Add(Fluids.WaterBucket,1);Check(!game.Industry.Bucket(vessel,true)&&vessel.Fluid.Amount==10000,"Water bucket rejected by lava tank without consumption");
            Check(game.Industry.Bucket(vessel,false)&&vessel.Fluid.Amount==0&&game.Inventory.Total(Fluids.LavaBucket)==1,"Take bucket returns lava, never water");
            Check(game.Industry.Bucket(vessel,true),"Empty tank accepts registered liquid again");
            game.SetMode(ScreenMode.Pause);vessel.Fluid.Withdraw(vessel.Fluid.Amount);vessel.Fluid.Deposit(Fluids.Lava,54321);
            Put(origin.Offset(1,0,0),IndustryId.FluidPipe);Put(origin.Offset(2,0,0),IndustryId.Tank);
            var pipe=sim.At(origin.Offset(1,0,0));
            void Role(MachineState p,int face,PortRole role){for(int i=0;i<3&&sim.PipeEndRole(p,face)!=role;i++)Check(sim.TogglePipeEnd(p,face),"Configure pipe end");}
            Role(pipe,1,PortRole.Output);Role(pipe,0,PortRole.Input);for(int i=0;i<10;i++)sim.Step();
            var receiver=sim.At(origin.Offset(2,0,0));Check(receiver.Fluid.Fluid==Fluids.Lava&&receiver.Fluid.Amount>0&&vessel.Fluid.Amount+receiver.Fluid.Amount==54321,"Lava pipe transfer conserves exact amount");
            // Filled items also cross an item pipe between chests.
            var sourcePos=origin.Offset(0,0,3);Put(sourcePos,BlockId.Chest);Put(sourcePos.Offset(1,0,0),IndustryId.ItemPipe);Put(sourcePos.Offset(2,0,0),BlockId.Chest);
            var source=game.Survival.At(sourcePos).Storage;var destination=game.Survival.At(sourcePos.Offset(2,0,0)).Storage;
            var charged=new ItemStack(IndustryId.Battery,1){Energy=9876543};source.Add(charged);destination.Add(IndustryId.Battery,2);
            pipe=sim.At(sourcePos.Offset(1,0,0));Role(pipe,1,PortRole.Output);Role(pipe,0,PortRole.Input);for(int i=0;i<15;i++)sim.Step();
            Check(source.Slots.All(s=>s.Empty)&&destination.Slots.Any(s=>s.Equals(charged))&&destination.Slots[0].Count==2,"Item pipes preserve charge and separate it from empty stacks");
            // Save payloads in every movable location and generic placed liquid.
            Select(charged);game.Inventory.Add(new ItemStack(IndustryId.Tank,1){FluidAmount=111,FluidCapacity=100000,FluidId=Fluids.Water.Source});
            game.UI.HeldStack=new ItemStack(IndustryId.Tank,1){FluidAmount=222,FluidCapacity=100000,FluidId=Fluids.Lava.Source};game.UI.ReturnHeld();
            long carriedEnergy=game.Inventory.Slots.Sum(s=>s.Energy),dropEnergy=game.Items.Piles.Sum(p=>p.Stack.Energy),dropLiquid=game.Items.Piles.Sum(p=>p.Stack.FluidAmount);
            long placedLiquid=vessel.Fluid.Amount+receiver.Fluid.Amount;
            game.InitializeSaves(Path.Combine(output,"PortableSaves"));Check(game.SaveGame("Portable storage"),"Save all content-bearing locations");
            Check(game.LoadGame(game.Saves.List().First(e=>!e.Backup)),"Load schema 8 portable storage");FreezeSaveFixture();game.enabled=false;world=game.World;sim=game.Industry.Simulation;player=game.Player;yield return Settle(120);
            Check(game.Inventory.Slots.Sum(s=>s.Energy)==carriedEnergy&&game.Inventory.Slots.Sum(s=>s.FluidAmount)==333,"Save restores inventory and returned cursor contents");
            Check(game.Items.Piles.Sum(p=>p.Stack.Energy)==dropEnergy&&game.Items.Piles.Sum(p=>p.Stack.FluidAmount)==dropLiquid,"Save restores dropped contents");
            Check(game.Survival.At(sourcePos.Offset(2,0,0)).Storage.Slots.Any(s=>s.Equals(charged)),"Save restores filled chest item");
            Check(sim.At(origin).Fluid.Fluid==Fluids.Lava&&sim.At(origin).Fluid.Amount+sim.At(origin.Offset(2,0,0)).Fluid.Amount==placedLiquid,"Save restores placed lava identity and quantity");
            Check(sim.At(controller).Structure.Fluid.Amount==250001,"Save restores recovered controller residue");
            // Native input: Shift-left-click consumes neither held item nor aimed world block.
            Select(charged);game.enabled=true;yield return null;game.enabled=false;game.SetMode(ScreenMode.Play);Aim(origin);player.enabled=true;
            yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.LeftShift));InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Left));yield return null;yield return null;
            Check(!game.Inventory.Slots[0].HasContents&&game.Inventory.Slots[0].Count==1&&world.Get(origin)==IndustryId.Tank,"Shift-left-click empties selected battery without mining target: charge="+game.Inventory.Slots[0].Energy+", paused="+game.Paused+", loading="+game.LoadingSave);
            yield return new WaitForSecondsRealtime(.2f);Check(world.Get(origin)==IndustryId.Tank&&player.MiningProgress==0,"Held gesture cannot fall through into mining");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;player.enabled=false;
            Select(new ItemStack(IndustryId.Tank,1){FluidAmount=12345,FluidCapacity=100000,FluidId=Fluids.Lava.Source});game.SetMode(ScreenMode.Inventory);game.UI.HoverSlot(0);
            yield return new WaitForSecondsRealtime(.4f);yield return Capture("storage-carried-contents");
            game.UI.ClickSlot(0,false,false);Check(game.UI.HeldStack.FluidAmount==12345,"Pick up filled tank on cursor");game.UI.ClickSlot(1,false,true);
            Check(game.UI.HeldStack.Id==IndustryId.Tank&&!game.UI.HeldStack.HasContents,"Shift-left-click empties cursor tank");game.UI.ReturnHeld();
            game.SetMode(ScreenMode.Play);Aim(origin);Check(game.TryOpenMachine(origin),"Open saved lava vessel");yield return new WaitForSecondsRealtime(.4f);yield return Capture("storage-lava-pipes");
            game.SetMode(ScreenMode.Play);game.Player.Arms.gameObject.SetActive(false);game.Player.Body.gameObject.SetActive(false);game.Player.HeldBlock.enabled=false;
            Select(new ItemStack(IndustryId.Wrench,1));
            game.Player.Camera.transform.position=world.Local(origin)+new Vector3(3.5f,3.2f,-4.2f);game.Player.Camera.transform.LookAt(world.Local(origin)+new Vector3(1.3f,.45f,.5f));
            for(int i=0;i<3;i++)sim.Step();yield return new WaitForSecondsRealtime(.5f);yield return Capture("storage-lava-connection");
            var pickup=new ItemStack(IndustryId.Battery,1){Energy=7654321};game.Items.enabled=true;
            game.Items.Spawn(pickup,player.transform.position+Vector3.up*.2f,Vector3.zero);
            yield return new WaitForSecondsRealtime(.3f);
            Check(game.Inventory.Slots.Any(s=>s.Equals(pickup)),"Physical dropped-item pickup retains exact charge");
            game.Items.enabled=false;game.enabled=true;
        }
    }
}
