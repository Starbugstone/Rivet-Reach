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
        IEnumerator ReviewConnections()
        {
            FreezeSaveFixture();game.SetCreative(true);
            var world=game.World;var player=game.Player;var sim=game.Industry.Simulation;
            var origin=world.Address(player.transform.position).Offset(5,2,5);
            void Put(BlockPos p,byte id)
            {byte old=world.Get(p);if(old!=0)Check(world.Remove(p,old),"Clear connection fixture");if(id!=0)Check(world.Place(p,id),"Place connection fixture "+id);}
            for(int x=-3;x<=6;x++)for(int z=-3;z<=8;z++)for(int y=-1;y<=4;y++)Put(origin.Offset(x,y,z),y==-1?BlockId.Stone:(byte)0);
            MachineState Machine(int x,int y,int z,byte id){var p=origin.Offset(x,y,z);Put(p,id);return sim.At(p);}
            var crank=Machine(-1,0,0,IndustryId.HandCrank);var battery=Machine(0,0,0,IndustryId.Battery);
            Machine(1,0,0,IndustryId.PowerCable);var crusher=Machine(2,0,0,IndustryId.Crusher);
            for(int i=0;i<10;i++)sim.Step();Check(sim.TryCrank(crank),"Start crank attached at battery side");
            for(int i=0;i<10;i++)sim.Step();Check(battery.EnergyCells[0].Amount==50000,"Crank charges through a side independent of its facing");
            battery.EnergyCells[0].Charge(2100000);crusher.Items.Add(BlockId.RawIron,4,0,1);sim.Step();
            Check(crusher.ReceivedWatts==160&&battery.BatteryWatts==-160,"Screenshot layout: battery → side cable → crusher supplies 160 W");
            yield return ReviewSeparatePowerGrids(origin);
            yield return ReviewBalancedGrids(origin);
            yield return ReviewPowerConnectionStatus(battery,crusher);
            var itemMachine=Machine(0,0,3,IndustryId.Crusher);var itemPipe=Machine(1,0,3,IndustryId.ItemPipe);
            var chestPos=origin.Offset(2,0,3);Put(chestPos,BlockId.Chest);itemMachine.Items.Add(IndustryId.CrushedIron,64,2,3);
            var source=Machine(0,0,6,IndustryId.Tank);var fluidPipe=Machine(1,0,6,IndustryId.FluidPipe);var destination=Machine(2,0,6,IndustryId.Tank);source.WaterMl=100000;
            var middlePipe=Machine(5,0,6,IndustryId.FluidPipe);Machine(6,0,6,IndustryId.FluidPipe);
            // A vertical pair verifies that top/bottom endpoints display the same arrows.
            var upperPipe=Machine(4,1,3,IndustryId.FluidPipe);var lowerTank=Machine(4,0,3,IndustryId.Tank);var upperTank=Machine(4,2,3,IndustryId.Tank);lowerTank.WaterMl=10000;
            Check(sim.TogglePipeEnd(upperPipe,3),"Configure bottom end as output from lower tank");
            for(int i=0;i<10;i++)sim.Step();game.SetMode(ScreenMode.Play);game.Sky.Clock.SetTime(.35);game.Sky.Apply();
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);player.HeldBlock.enabled=false;
            player.transform.position=world.Local(origin)+new Vector3(4,0,-3);
            player.Camera.transform.position=world.Local(origin)+new Vector3(6.7f,5.6f,-5.8f);player.Camera.transform.LookAt(world.Local(origin)+new Vector3(1.3f,.75f,2.9f));
            yield return new WaitForSecondsRealtime(.5f);
            var arrows=world.GetComponent<PipeEndpointPresentation>();
            game.Inventory.Take(0,int.MaxValue);game.Selected=0;yield return new WaitForSecondsRealtime(.3f);
            Check(arrows.ViewCount==0,"Empty hands hide every pipe arrow");
            game.Inventory.Add(IndustryId.Wrench,1,0,1);yield return new WaitForSecondsRealtime(.3f);
            Check(arrows.ViewCount==6,"Six machine-facing item/fluid arrows render, including vertical ends");
            Check(Vector3.Dot(arrows.ViewAt(itemPipe.Position,1).up,Vector3.right)>.99f&&Vector3.Dot(arrows.ViewAt(itemPipe.Position,0).up,Vector3.right)>.99f,"Red output exits source and blue input enters destination along the actual flow");
            yield return Capture("connections-workshop");
            player.Camera.transform.position=world.Local(origin)+new Vector3(3.4f,2.2f,3.8f);player.Camera.transform.LookAt(world.Local(origin)+new Vector3(1.45f,.5f,6.2f));
            yield return Capture("fluid-end-arrows");

            // Aim the ordinary FPS camera at the pipe end and queue actual mouse events.
            game.Inventory.Take(0,int.MaxValue);game.Selected=0;player.enabled=true;player.ResetMotion();
            void AimItem(float x)
            {
                player.transform.position=world.Local(origin)+new Vector3(x,0,1);player.Yaw=0;
                player.Pitch=Mathf.Atan2(1.64f-.5f,2.29f)*Mathf.Rad2Deg;
            }
            AimItem(1.16f);yield return new WaitForSecondsRealtime(.3f);
            Check(game.TryGetPipeEndTarget(out var aimed,out int face)&&aimed==itemPipe&&face==1,"Crosshair selects machine-facing end instead of opening the whole pipe");
            Check(sim.PipeEndRole(itemPipe,1)==PortRole.Output,"Existing right-side outlet defaults to red output");
            var crafting=new CraftingSession(game.Recipes,3,id=>game.Registry.Get(id).stackLimit);
            foreach(int slot in new[]{0,1,4})crafting.Grid.Add(BlockId.IronIngot,1,slot,slot+1);
            ItemStack made=default;Check(crafting.CraftToCursor(ref made).Succeeded&&made.Id==IndustryId.Wrench&&made.Count==1&&crafting.Grid.Slots.All(s=>s.Empty),"Workbench recipe consumes three iron ingots for one wrench");
            game.Inventory.Add(made.Id,1,1,2);
            Check(!game.HoldingWrench&&!game.TryConfigurePipeEnd(),"A wrench elsewhere in inventory does not authorize empty-handed configuration");
            int unchanged=itemPipe.PipeDirections;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(itemPipe.PipeDirections==unchanged&&game.InventoryOpen,"Actual empty-handed right-click opens the pipe without changing direction");
            game.SetMode(ScreenMode.Play);game.Inventory.Add(BlockId.Stick,1,0,1);yield return null;
            Check(!game.TryConfigurePipeEnd(),"Holding a different item rejects direction changes");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(itemPipe.PipeDirections==unchanged&&game.InventoryOpen,"Actual right-click with another item cannot change direction");
            game.SetMode(ScreenMode.Play);game.Selected=1;player.HeldBlock.enabled=true;yield return new WaitForSecondsRealtime(.4f);
            Check(game.HoldingWrench&&player.HeldBlock.ItemId==IndustryId.Wrench&&player.HeldBlock.DesiredGrip==GripPose.Tool&&player.HeldBlock.Visible,"Crafted wrench uses the held tool grip and original mesh");
            yield return Capture("wrench-held");
            game.Items.enabled=true;game.Items.Spawn(new ItemStack(IndustryId.Wrench,1),world.Local(origin)+new Vector3(3.5f,.4f,1.5f),Vector3.zero,10,true);
            yield return new WaitForSecondsRealtime(.2f);
            var drop=game.Items.Piles.Last();
            Check(drop.View!=null&&drop.View.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)==Resources.Load<GameObject>("Tools/Wrench").GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3),"Dropped wrench renders the same original imported geometry");game.Items.enabled=false;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
            Check(game.Mode==ScreenMode.Play&&sim.PipeEndRole(itemPipe,1)==PortRole.Disabled,"Actual wrench right-click cycles Output to No connection without opening inventory");
            int disconnected=itemPipe.PipeDirections;yield return new WaitForSecondsRealtime(.35f);
            Check(itemPipe.PipeDirections==disconnected,"Held right-click does not cycle past disconnected");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(arrows.ViewAt(itemPipe.Position,1)==null&&game.TryGetPipeEndTarget(out aimed,out face)&&aimed==itemPipe&&face==1,"Disconnected end hides its arrow but remains aimable at the same side");
            var pipeView=world.GetComponent<IndustryPresentation>().ViewAt(itemPipe.Position);
            Check(pipeView.GetComponentInChildren<Renderer>().bounds.min.x>world.Local(itemPipe.Position).x+.2f,"Actual item-pipe rendering leaves a gap at the disconnected machine");
            yield return Capture("item-end-disconnected");
            game.Selected=0;yield return new WaitForSecondsRealtime(.3f);
            Check(arrows.ViewCount==0&&!game.TryConfigurePipeEnd(),"Another selected item hides arrows and cannot reconnect a missing arm");
            game.Selected=1;yield return new WaitForSecondsRealtime(.3f);
            Check(arrows.ViewAt(itemPipe.Position,0)!=null&&arrows.ViewAt(itemPipe.Position,1)==null,"Holding wrench again shows only connected-end arrows");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
            Check(game.Mode==ScreenMode.Play&&sim.PipeEndRole(itemPipe,1)==PortRole.Input,"Actual wrench click on the missing end restores blue Input");
            int setting=itemPipe.PipeDirections;yield return new WaitForSecondsRealtime(.35f);
            Check(itemPipe.PipeDirections==setting,"Holding right-click does not repeatedly flip an endpoint");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            yield return Capture("item-end-blue-input");
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(game.Input.Keys["Interact"]));yield return null;yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
            Check(sim.PipeEndRole(itemPipe,1)==PortRole.Input&&game.InventoryOpen,"Interact opens the pipe without changing direction even with the wrench selected");
            game.SetMode(ScreenMode.Play);yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(sim.PipeEndRole(itemPipe,1)==PortRole.Output&&game.Mode==ScreenMode.Play&&game.Inventory.Slots[1].Id==IndustryId.Wrench&&game.Inventory.Slots[1].Count==1,"Another wrench right-click restores red output without consuming the tool");
            yield return Capture("item-end-red-output");
            AimItem(1.5f);yield return new WaitForSecondsRealtime(.3f);
            Check(!game.TryGetPipeEndTarget(out _,out _),"Pipe centre remains a distinct target");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(game.OpenMachine==itemPipe&&game.InventoryOpen,"Right-clicking the pipe centre still opens its channel-fitting interface");
            yield return Capture("pipe-connection-help");game.SetMode(ScreenMode.Play);player.enabled=false;
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
            player.Camera.transform.position=world.Local(origin)+new Vector3(4.8f,3.1f,1.3f);player.Camera.transform.LookAt(world.Local(upperPipe.Position)+Vector3.one*.5f);
            yield return Capture("vertical-fluid-arrows");
            // Fluid end targeting goes through the same visibility/reach authority.
            player.Camera.transform.position=world.Local(fluidPipe.Position)+new Vector3(.16f,1.3f,-2);
            player.Camera.transform.LookAt(world.Local(fluidPipe.Position)+new Vector3(.16f,.5f,.29f));
            Check(game.TryGetPipeEndTarget(out aimed,out face)&&aimed==fluidPipe&&face==1&&game.TryConfigurePipeEnd(),"Selected wrench can disconnect a fluid end through the same targeting authority");
            Check(sim.PipeEndRole(fluidPipe,1)==PortRole.Disabled&&sim.PipeEndRole(fluidPipe,0)==PortRole.Input,"Changing one fluid end leaves the other end unchanged");
            yield return new WaitForSecondsRealtime(.3f);yield return Capture("fluid-end-disconnected");
            Check(game.TryConfigurePipeEnd()&&sim.PipeEndRole(fluidPipe,1)==PortRole.Input,"Wrench on missing fluid end restores Input");
            Check(sim.TogglePipeEnd(itemPipe,1)&&sim.PipeEndRole(itemPipe,1)==PortRole.Disabled,"Persist a disconnected item endpoint");
            Check(sim.TogglePipeEnd(fluidPipe,0)&&sim.TogglePipeEnd(fluidPipe,0)&&sim.PipeEndRole(fluidPipe,0)==PortRole.Disabled,"Persist a disconnected fluid endpoint");
            player.Camera.transform.position+=Vector3.back*5;
            Check(!game.TryConfigurePipeEnd(),"Out-of-reach pipe end cannot be configured");
            player.HeldBlock.enabled=false;
            player.Camera.transform.position=world.Local(origin)+new Vector3(1.5f,1.4f,-2);
            player.Camera.transform.LookAt(world.Local(origin)+new Vector3(1.5f,.5f,.5f));
            Check(!game.TryConfigurePipeEnd(),"Wrench cannot set direction on an electrical cable");
            player.Camera.transform.position=world.Local(middlePipe.Position)+new Vector3(.84f,1.3f,-2);
            player.Camera.transform.LookAt(world.Local(middlePipe.Position)+new Vector3(.84f,.5f,.29f));
            Check(!game.TryConfigurePipeEnd()&&middlePipe.PipeDirections==0,"Wrench cannot configure a pipe-to-pipe join or its loose ends");
            // Pause freezes transfers so the save comparison includes exact resources.
            FreezeSaveFixture();game.InitializeSaves(Path.Combine(output,"Saves"));
            int itemSettings=itemPipe.PipeDirections,fluidSettings=fluidPipe.PipeDirections,verticalSettings=upperPipe.PipeDirections;
            long energy=battery.EnergyCells[0].Amount;int water=source.WaterMl+destination.WaterMl+lowerTank.WaterMl+upperTank.WaterMl;
            int products=itemMachine.Items.Total(IndustryId.CrushedIron)+game.Survival.At(chestPos).Storage.Total(IndustryId.CrushedIron);
            Check(game.SaveGame("Configurable connections"),"Save pipe direction checkpoint: "+game.SaveStatus);
            var entry=game.Saves.List().First(e=>!e.Backup);byte[] saved=game.Saves.Read(entry);
            using(var reader=game.Saves.Open(saved,out _))Check(reader.Format==SaveStore.Format,"New checkpoint uses the current explicit schema");
            Check(game.LoadGame(entry),"Load configured pipe ends: "+game.SaveStatus);FreezeSaveFixture();
            Check(game.CaptureSave(entry).SequenceEqual(saved),"All serialized state round-trips byte-for-byte before simulation resumes");
            sim=game.Industry.Simulation;
            var intactWorld=game.World;var restoredPipe=sim.At(itemPipe.Position);int valid=restoredPipe.PipeDirections;
            restoredPipe.PipeDirections=4096;byte[] invalid=game.CaptureSave(entry);restoredPipe.PipeDirections=valid;File.WriteAllBytes(entry.Path,invalid);
            Check(!game.LoadGame(entry)&&game.World==intactWorld&&sim.At(itemPipe.Position).PipeDirections==valid,"Invalid saved end mode rejects and rolls back without changing the current world");File.WriteAllBytes(entry.Path,saved);
            Check(sim.At(itemPipe.Position).PipeDirections==itemSettings&&sim.At(fluidPipe.Position).PipeDirections==fluidSettings&&sim.At(upperPipe.Position).PipeDirections==verticalSettings,"Item, fluid and vertical end settings survive load");
            Check(sim.At(battery.Position).EnergyCells[0].Amount==energy&&sim.At(source.Position).WaterMl+sim.At(destination.Position).WaterMl+sim.At(lowerTank.Position).WaterMl+sim.At(upperTank.Position).WaterMl==water,"Save/load preserves exact battery energy and all fluid");
            Check(sim.At(itemMachine.Position).Items.Total(IndustryId.CrushedIron)+game.Survival.At(chestPos).Storage.Total(IndustryId.CrushedIron)==products,"Save/load preserves all machine and chest items");
            yield return Settle(120);for(int i=0;i<10;i++)sim.Step();
            game.Player.Camera.transform.position=game.World.Local(origin)+new Vector3(3.7f,2.1f,3.5f);game.Player.Camera.transform.LookAt(game.World.Local(fluidPipe.Position)+Vector3.one*.5f);
            yield return new WaitForSecondsRealtime(.3f);
            Check(game.World.GetComponent<PipeEndpointPresentation>().ViewAt(fluidPipe.Position,1)!=null,"Restored connected endpoints rebuild their wrench-visible arrows");
            Check(sim.PipeEndRole(sim.At(itemPipe.Position),1)==PortRole.Disabled&&sim.PipeEndRole(sim.At(fluidPipe.Position),0)==PortRole.Disabled&&game.World.GetComponent<PipeEndpointPresentation>().ViewAt(fluidPipe.Position,0)==null,"Saved disconnected item and fluid ends stay disabled after simulation resumes");
            yield return Capture("restored-connection-directions");
            Check(game.Inventory.Slots[1].Id==IndustryId.Wrench,"Wrench inventory identity survives save/load");
            yield return ReviewFurnacePipes(origin);
        }

        IEnumerator ReviewSeparatePowerGrids(BlockPos origin)
        {
            var sim=game.Industry.Simulation;var world=game.World;var player=game.Player;
            game.enabled=false;
            MachineState Put(int x,byte id)
            {var p=origin.Offset(x,0,-2);Check(world.Place(p,id),"Place separate power-grid fixture");return sim.At(p);}
            var boiler=Put(-3,IndustryId.Boiler);boiler.WaterMl=100000;boiler.Items.Add(BlockId.Coal,1);
            var alt=Put(-2,IndustryId.Alternator);var input=Put(-1,IndustryId.PowerCable);
            var cell=Put(0,IndustryId.Battery);Put(1,IndustryId.PowerCable);var load=Put(2,IndustryId.Crusher);load.Items.Add(BlockId.RawIron,64,0,1);
            for(int i=0;i<100;i++){sim.Step();if(!sim.Rebuilding&&sim.Multiblocks.PendingCount==0)break;}
            long before=BatteryPower.Amount(cell);sim.Step();
            Check(cell.BatteryInputWatts==800&&cell.BatteryOutputWatts==160&&load.ReceivedWatts==160&&BatteryPower.Amount(cell)-before==32000,"Native separate cable grids: 800 W generator charges battery while crusher receives 160 W");
            Check(!sim.Power.Topology.Groups.Any(g=>g.Ports.Any(p=>p.Machine==alt)&&g.Ports.Any(p=>p.Machine==load)),"Native battery does not connect separate cable grids");
            game.SetMode(ScreenMode.Play);player.transform.position=world.Local(cell.Position)+new Vector3(.5f,.1f,-2);
            player.Camera.transform.position=world.Local(cell.Position)+new Vector3(.5f,3,.5f);
            player.Camera.transform.LookAt(world.Local(cell.Position)+Vector3.one*.5f);
            Check(game.TryOpenMachine(cell.Position),"Open separate-grid battery interface");yield return new WaitForSecondsRealtime(.2f);
            UnityEngine.UI.Text Field(string name)=>(UnityEngine.UI.Text)typeof(GameUI).GetProperty(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(game.UI);
            Check(Field("machineStatus").text=="Charging and supplying"&&Field("machineDetail").text.Contains("Charge: 800 W · Output: 160 W"),"Battery UI reports simultaneous grid input and output, not just net charge");
            yield return Capture("battery-separate-grids");
            foreach(var cable in new[]{input,sim.At(origin.Offset(1,0,-2))})
            {
                game.SetMode(ScreenMode.Play);player.transform.position=world.Local(cable.Position)+new Vector3(.5f,.1f,-2);
                player.Camera.transform.position=world.Local(cable.Position)+new Vector3(.5f,3,.5f);player.Camera.transform.LookAt(world.Local(cable.Position)+Vector3.one*.5f);
                Check(game.TryOpenMachine(cable.Position),"Open actual cable-grid diagnostics");yield return new WaitForSecondsRealtime(.2f);
                string expected=cable==input?"Grid input: 800 W · Required: 0 W":"Grid input: 160 W · Required: 160 W";
                Check(Field("machineDetail").text.Contains(expected)&&Field("machineDetail").text.Contains("Connected storage:"),"Each cable reports only its own grid input and requirements plus shared storage");
                yield return Capture(cable==input?"power-input-grid":"power-output-grid");
            }
            game.SetMode(ScreenMode.Pause);
            Check(world.Remove(input.Position,IndustryId.PowerCable),"Break native generator cable");sim.Step();before=BatteryPower.Amount(cell);sim.Step();
            Check(load.ReceivedWatts==160&&cell.BatteryInputWatts==0&&before-BatteryPower.Amount(cell)==8000,"Cable break leaves independent crusher grid supplied from stored energy");
            Check(world.Place(input.Position,IndustryId.PowerCable),"Replace native generator cable");sim.Step();before=BatteryPower.Amount(cell);sim.Step();
            Check(load.ReceivedWatts==160&&BatteryPower.Amount(cell)-before==32000,"Native cable replacement restores charging automatically");
            // Remove only this disposable fixture so the existing UI/save scenario stays isolated.
            cell.EnergyCells[0].Discharge(BatteryPower.Amount(cell));
            for(int x=-3;x<=2;x++){var p=origin.Offset(x,0,-2);Check(world.Remove(p,world.Get(p)),"Remove separate-grid fixture");}
            sim.Step();game.enabled=true;
        }

        IEnumerator ReviewBalancedGrids(BlockPos origin)
        {
            var sim=game.Industry.Simulation;var world=game.World;var player=game.Player;game.enabled=false;
            var placed=new System.Collections.Generic.List<BlockPos>();
            MachineState Put(int x,int z,byte id)
            {var p=origin.Offset(x,0,z);Check(world.Place(p,id),"Place balanced grid fixture");placed.Add(p);return sim.At(p);}
            void Settle(){for(int i=0;i<100;i++){sim.Step();if(!sim.Rebuilding&&sim.Multiblocks.PendingCount==0)return;}Check(false,"Settle balanced grid");}
            void Clear()
            {
                game.SetMode(ScreenMode.Pause);
                foreach(var pos in placed)
                {
                    var m=sim.At(pos);if(m?.Definition.Id==IndustryId.Battery)m.EnergyCells[0].Discharge(BatteryPower.Amount(m));
                    Check(world.Remove(pos,world.Get(pos)),"Remove balanced grid fixture");
                }
                placed.Clear();Settle();
            }
            IEnumerator Inspect(MachineState m,string capture)
            {
                game.SetMode(ScreenMode.Play);player.transform.position=world.Local(m.Position)+new Vector3(.5f,.1f,-2);
                player.Camera.transform.position=world.Local(m.Position)+new Vector3(.5f,3,.5f);player.Camera.transform.LookAt(world.Local(m.Position)+Vector3.one*.5f);
                Check(game.TryOpenMachine(m.Position),"Open balanced grid interface");yield return new WaitForSecondsRealtime(.2f);yield return Capture(capture);
            }
            var boiler=Put(-3,-2,IndustryId.Boiler);boiler.WaterMl=100000;boiler.Items.Add(BlockId.Coal,1);
            Put(-2,-2,IndustryId.Alternator);for(int x=-1;x<=3;x++)Put(x,-2,IndustryId.PowerCable);
            var a=Put(0,-3,IndustryId.Battery);var b=Put(2,-3,IndustryId.Battery);var load=Put(3,-3,IndustryId.Crusher);load.Items.Add(BlockId.RawIron,64,0,1);Settle();sim.Step();
            Check(a.BatteryInputWatts==320&&b.BatteryInputWatts==320&&load.ReceivedWatts==160,"Native two batteries equally store the 640 W grid surplus");
            yield return Inspect(a,"battery-equal-charge");
            boiler.WaterMl=0;sim.Step();Check(a.BatteryOutputWatts==80&&b.BatteryOutputWatts==80&&load.ReceivedWatts==160,"Native two batteries equally cover the 160 W deficit");
            yield return Inspect(b,"battery-equal-discharge");Clear();
            foreach(bool fluid in new[]{false,true})
            {
                byte type=fluid?IndustryId.FluidPipe:IndustryId.ItemPipe;
                for(int x=-1;x<=3;x++)Put(x,-2,type);
                var positions=new[]{origin.Offset(-1,0,-3),origin.Offset(1,0,-3),origin.Offset(1,0,-1),origin.Offset(3,0,-1)};
                for(int i=0;i<4;i++)Put((int)(positions[i].X-origin.X),(int)(positions[i].Z-origin.Z),fluid?IndustryId.Tank:BlockId.Chest);
                for(int i=0;i<4;i++)
                {
                    var pipe=sim.At(positions[i].Offset(0,0,i<2?1:-1));int face=i<2?5:4;
                    var role=i<2?PortRole.Output:PortRole.Input;
                    for(int n=0;n<3&&sim.PipeEndRole(pipe,face)!=role;n++)Check(sim.TogglePipeEnd(pipe,face),"Configure balanced pipe end");
                }
                Settle();
                if(fluid){sim.At(positions[0]).WaterMl=sim.At(positions[1]).WaterMl=1000;sim.Step();}
                else
                {
                    game.Survival.At(positions[0]).Storage.Add(BlockId.Stone,10);game.Survival.At(positions[1]).Storage.Add(BlockId.Stone,10);
                    do{sim.Step();}while(sim.Tick%5!=0);
                }
                if(fluid)
                    Check(sim.At(positions[0]).WaterMl==900&&sim.At(positions[1]).WaterMl==900&&sim.At(positions[2]).WaterMl==100&&sim.At(positions[3]).WaterMl==100,"Native fluid inputs share 200 mL equally with exact source withdrawal");
                else
                    Check(game.Survival.At(positions[0]).Storage.Total(BlockId.Stone)==9&&game.Survival.At(positions[1]).Storage.Total(BlockId.Stone)==9&&game.Survival.At(positions[2]).Storage.Total(BlockId.Stone)==1&&game.Survival.At(positions[3]).Storage.Total(BlockId.Stone)==1,"Native item inputs share two whole items equally");
                game.SetMode(ScreenMode.Play);player.transform.position=world.Local(origin)+new Vector3(1,0,-3);
                player.Camera.transform.position=world.Local(origin)+new Vector3(5,5,-6);player.Camera.transform.LookAt(world.Local(origin)+new Vector3(1,.5f,-2));
                game.Inventory.Take(0,int.MaxValue);game.Selected=0;game.Inventory.Add(IndustryId.Wrench,1,0,1);
                yield return new WaitForSecondsRealtime(.3f);
                Check(world.GetComponent<PipeEndpointPresentation>().ViewCount==4,"Four direction arrows survive replacement with the other pipe channel");
                yield return Capture(fluid?"fluid-equal-sharing":"item-equal-sharing");Clear();
            }
            game.Inventory.Take(0,int.MaxValue);game.enabled=true;
        }

        IEnumerator ReviewPowerConnectionStatus(MachineState battery,MachineState crusher)
        {
            var sim=game.Industry.Simulation;var world=game.World;var player=game.Player;
            long energy=battery.EnergyCells[0].Amount;battery.EnergyCells[0].Discharge(energy);sim.Step();
            void Aim(MachineState machine)
            {
                game.SetMode(ScreenMode.Play);player.transform.position=world.Local(machine.Position)+new Vector3(.5f,.1f,-2);
                player.Camera.transform.position=world.Local(machine.Position)+new Vector3(.5f,3,.5f);
                player.Camera.transform.LookAt(world.Local(machine.Position)+Vector3.one*.5f);
                Check(game.TryOpenMachine(machine.Position),"Open actual machine interface for power-status review");
            }
            UnityEngine.UI.Text Field(string name)=>(UnityEngine.UI.Text)typeof(GameUI).GetProperty(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(game.UI);
            // Freeze only automatic ticks while the real retained UI refreshes from
            // explicitly advanced authoritative simulation states.
            game.enabled=false;Aim(crusher);yield return new WaitForSecondsRealtime(.2f);
            Check(Field("machineStatus").text=="No electrical power"&&Field("machineDetail").text.Contains("Power network: connected")&&Field("machineDetail").text.Contains("Power: 0 / 160 W"),"Live UI separates an already connected electrical network from zero available machine power");
            yield return Capture("power-connected-empty");
            sim.Invalidate();yield return new WaitForSecondsRealtime(.2f);
            Check(sim.Rebuilding&&Field("machineStatus").text=="No electrical power"&&Field("machineDetail").text.Contains("Power network: connected"),"Global topology work cannot replace the machine's blackout status with Connecting networks");
            battery.EnergyCells[0].Charge(8000);sim.Step();yield return new WaitForSecondsRealtime(.2f);
            Check(Field("machineStatus").text=="Running"&&Field("machineDetail").text.Contains("Power network: connected")&&Field("machineDetail").text.Contains("Power: 160 / 160 W"),"Receiving electricity changes the operating status while connectivity stays registered");
            sim.Step();yield return new WaitForSecondsRealtime(.2f);
            Check(Field("machineStatus").text=="No electrical power"&&Field("machineDetail").text.Contains("Power network: connected"),"Depletion returns to No electrical power without a connection message");
            var cable=crusher.Position.Offset(-1,0,0);Check(world.Remove(cable,IndustryId.PowerCable),"Remove actual cable for disconnected UI review");sim.Step();yield return new WaitForSecondsRealtime(.2f);
            Check(Field("machineStatus").text=="No electrical power"&&Field("machineDetail").text.Contains("Power network: not connected"),"Physical cable removal changes the separate connection label");
            yield return Capture("power-cable-disconnected");Check(world.Place(cable,IndustryId.PowerCable),"Restore electrical cable");sim.Step();
            var pumpPos=crusher.Position.Offset(3,0,0);Check(world.Place(pumpPos,IndustryId.Pump),"Place pump to review the five-line status layout");sim.Step();Aim(sim.At(pumpPos));yield return new WaitForSecondsRealtime(.2f);
            Check(!Field("machineDetail").text.Contains("Power network:")&&Field("machineDetail").text.Contains("Below:")&&Field("machineDetail").text.Contains("Water:"),"Pump shows water and intake diagnostics without an electrical connection requirement");yield return Capture("pump-network-status");
            game.SetMode(ScreenMode.Pause);world.Remove(pumpPos,IndustryId.Pump);battery.EnergyCells[0].Charge(energy);game.enabled=true;
        }

        IEnumerator ReviewConnectionLegacy()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rr-legacy-directory");
            Check(index>=0,"Explicit earlier checkpoint fixtures supplied");string directory=args[index+1];game.InitializeSaves(directory);
            var entries=game.Saves.List().Where(e=>!e.Backup).ToArray();Check(entries.Length>=2,"Discover earlier full-world checkpoints");
            var formats=new System.Collections.Generic.HashSet<int>();
            foreach(var entry in entries)
            {
                game.InitializeSaves(directory);var bytes=game.Saves.Read(entry);
                using(var reader=game.Saves.Open(bytes,out _)){Check(reader.Format>=1&&reader.Format<=3,"Fixture predates pipe direction schema: "+entry.Name);formats.Add(reader.Format);}
                var changed=UnityEngine.Object.Instantiate(game.Registry);changed.Get(BlockId.IronIngot).stackLimit++;
                bool rejected=false;try{using var reader=new SaveStore(output,changed).Open(bytes,out _);}catch(InvalidDataException){rejected=true;}
                UnityEngine.Object.Destroy(changed);Check(rejected,"Earlier checkpoint still rejects unrelated changed item definitions");
                Check(game.LoadGame(entry),"Load actual earlier checkpoint: "+game.SaveStatus);FreezeSaveFixture();
                Check(game.Industry.Simulation.Machines.Values.All(m=>m.PipeDirections==0),"Older machine records have no added direction bytes");
                yield return Settle(120);for(int i=0;i<10;i++)game.Industry.Simulation.Step();
                game.InitializeSaves(Path.Combine(output,"Migrated"));Check(game.SaveGame(entry.Name,true),"Save migrated checkpoint with the current schema");
                var migrated=game.Saves.List().First(e=>e.Id==game.SaveId&&!e.Backup);var fresh=game.Saves.Read(migrated);
                using(var reader=game.Saves.Open(fresh,out _))Check(reader.Format==SaveStore.Format,"Migration writes the current schema");
                Check(game.LoadGame(migrated),"Reload migrated checkpoint: "+game.SaveStatus);FreezeSaveFixture();
                Check(game.CaptureSave(migrated).SequenceEqual(fresh),"Every migrated serialized field round-trips exactly");
            }
            Check(formats.Contains(2)&&formats.Contains(3),"Actual schema-2 and schema-3 checkpoints both migrate");
        }
    }
}
