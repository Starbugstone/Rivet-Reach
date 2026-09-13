using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewElectricFurnace()
        {
            FreezeSaveFixture();game.SetCreative(true);game.enabled=false;
            var world=game.World;var player=game.Player;var sim=game.Industry.Simulation;
            var p=world.Address(player.transform.position).Offset(0,2,4);
            void Put(BlockPos pos,byte id)
            {byte old=world.Get(pos);if(old!=0)Check(world.Remove(pos,old),"Clear fixture cell");if(id!=0)Check(world.Place(pos,id),"Place fixture "+id);}
            for(int x=-4;x<=4;x++)for(int z=-3;z<=3;z++)for(int y=-1;y<=3;y++)Put(p.Offset(x,y,z),y==-1?BlockId.Stone:(byte)0);
            Put(p,IndustryId.ElectricFurnace);Put(p.Offset(0,0,1),IndustryId.PowerCable);Put(p.Offset(0,0,2),IndustryId.Battery);
            var m=sim.At(p);var battery=sim.At(p.Offset(0,0,2));Check(battery.EnergyCells[0].Charge(100000000),"Charge isolated review fixture");
            var held=new ItemStack(BlockId.RawIron,2);m.Click(0,ref held,false);Check(held.Empty,"Manual insertion uses filtered machine input");
            var fuel=new ItemStack(BlockId.Coal,1);m.Click(1,ref fuel,false);Check(fuel.Count==1&&m.Items.Slots[1].Empty,"Fuel rejected without consuming held item");
            for(int i=0;i<151;i++)sim.Step();Check(m.Work==151&&m.Items.Slots[2].Empty&&m.ReceivedWatts==200,"Actual furnace reaches saved progress beyond old 120-tick limit");
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);player.HeldBlock.enabled=false;
            player.transform.position=world.Local(p)+new Vector3(.5f,0,-2);
            player.Camera.transform.position=world.Local(p)+new Vector3(2.8f,2.4f,-3.7f);player.Camera.transform.LookAt(world.Local(p)+new Vector3(0,.55f,.65f));
            game.SetMode(ScreenMode.Play);game.Sky.Clock.SetTime(.35);game.Sky.Apply();yield return new WaitForSecondsRealtime(.5f);
            var presentation=Object.FindAnyObjectByType<IndustryPresentation>();var view=presentation.ViewAt(p);
            Check(view!=null&&view.GetComponentsInChildren<MeshFilter>().Length==2,"Actual imported furnace cabinet and status lamp render");
            var renderers=view.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            Check(bounds.size.x<=1.01f&&bounds.size.y<=1.01f&&bounds.size.z<=1.01f,"Imported model remains within one cell");
            File.WriteAllText(Path.Combine(output,"model.txt"),"Imported triangles: "+view.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)+"; renderers: "+renderers.Length+"; bounds: "+bounds.size);
            yield return Capture("electric-furnace-powered");
            Check(game.TryOpenMachine(p),"Open furnace machine interface");yield return new WaitForSecondsRealtime(.2f);
            Check(game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("200 / 200 W")),"Interface reports allocated electrical power");
            Check(!game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text=="FUEL"),"Interface has no fuel slot label");
            yield return Capture("electric-furnace-interface");
            game.SetMode(ScreenMode.Pause);Put(p.Offset(0,0,1),0);sim.Step();
            Check(m.Status==MachineStatus.NoPower&&m.Work==151,"Unpowered furnace retains partial work before save");
            game.SetMode(ScreenMode.Play);Check(game.TryOpenMachine(p),"Open disconnected furnace before save");
            yield return new WaitForSecondsRealtime(.4f);
            Check(game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("No electrical power")),"Visible panel reports no electrical power");
            yield return Capture("electric-furnace-no-power");game.SetMode(ScreenMode.Pause);Put(p.Offset(0,0,1),IndustryId.PowerCable);
            game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"Saves"));
            long energy=battery.EnergyCells[0].Amount;Check(game.SaveGame("Electric furnace partial work"),"Save actual furnace checkpoint");
            Check(game.LoadGame(game.Saves.List().First(e=>!e.Backup)),"Reload furnace checkpoint");FreezeSaveFixture();game.enabled=false;
            world=game.World;sim=game.Industry.Simulation;yield return Settle(120);m=sim.At(p);battery=sim.At(p.Offset(0,0,2));
            Check(m.Work==151&&m.Items.Slots[0].Count==2&&battery.EnergyCells[0].Amount==energy,"Save preserves exact long partial work, input and battery energy");
            for(int i=0;i<49;i++)sim.Step();Check(m.Work==0&&m.Items.Slots[2].Count==1&&m.Items.Slots[0].Count==1,"Loaded process completes at exactly 200 powered ticks");
            Put(p.Offset(0,0,1),0);sim.Step();double work=m.Work;for(int i=0;i<10;i++)sim.Step();
            Check(m.Work==work&&m.Status==MachineStatus.NoPower,"Actual cable removal stops processing");
            game.SetMode(ScreenMode.Pause);Put(p.Offset(0,0,1),IndustryId.PowerCable);
            Put(p.Offset(-1,0,0),IndustryId.ItemPipe);Put(p.Offset(-2,0,0),BlockId.Chest);
            Put(p.Offset(1,0,0),IndustryId.ItemPipe);Put(p.Offset(2,0,0),BlockId.Chest);
            var source=game.Survival.At(p.Offset(-2,0,0)).Storage;source.Add(BlockId.Coal,3);source.Add(IndustryId.CrushedIron,2);
            var input=sim.At(p.Offset(-1,0,0));var outputPipe=sim.At(p.Offset(1,0,0));
            void Mode(MachineState pipe,int face,PortRole role)
            {for(int i=0;i<3&&sim.PipeEndRole(pipe,face)!=role;i++)Check(sim.TogglePipeEnd(pipe,face),"Set pipe endpoint");}
            Mode(input,1,PortRole.Output);Mode(input,0,PortRole.Input);Mode(outputPipe,1,PortRole.Output);Mode(outputPipe,0,PortRole.Input);
            for(int i=0;i<630;i++)sim.Step();
            Check(game.Survival.At(p.Offset(2,0,0)).Storage.Total(BlockId.IronIngot)==4&&source.Total(BlockId.Coal)==3&&source.Total(IndustryId.CrushedIron)==0,"Actual pipes skip coal, feed crushed ore and export four exact ingots");
            Check(m.Items.Slots.All(s=>s.Empty)&&m.BurnTicks==0&&m.RequestedWatts==0,"Completed automated furnace is empty, fuel-free and draws no idle power");
            game.SetCreative(true);game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(IndustryId.Wrench,1,0,1);game.Selected=0;
            game.SetMode(ScreenMode.Play);game.Player.Arms.gameObject.SetActive(false);game.Player.Body.gameObject.SetActive(false);game.Player.HeldBlock.enabled=false;
            game.Player.Camera.transform.position=world.Local(p)+new Vector3(3.9f,4.2f,-5.7f);game.Player.Camera.transform.LookAt(world.Local(p)+new Vector3(0,.4f,.6f));
            yield return new WaitForSecondsRealtime(.5f);yield return Capture("electric-furnace-pipes");
            Check(world.Remove(p,IndustryId.ElectricFurnace)&&sim.At(p)==null,"Mining removes electric furnace authority");
            Check(game.Registry.FistDrop(IndustryId.ElectricFurnace)==IndustryId.ElectricFurnace,"Mining recovers the electric furnace item");
            game.enabled=true;
        }
    }
}
