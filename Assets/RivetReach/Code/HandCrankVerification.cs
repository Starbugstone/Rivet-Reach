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
        IEnumerator ReviewHandCrank()
        {
            var world=game.World;var player=game.Player;var sim=game.Industry.Simulation;
            game.SetCreative(true);game.Mobs.enabled=false;game.Items.enabled=false;game.Diagnostics=false;
            player.enabled=false;player.ResetMotion();var origin=world.Address(player.transform.position).Offset(0,2,4);
            for(int x=-4;x<=4;x++)for(int z=-5;z<=4;z++)for(int y=-1;y<=4;y++)
            {var p=origin.Offset(x,y,z);byte old=world.Get(p);if(old!=0)world.Remove(p,old);if(y==-1)world.Place(p,BlockId.Stone);}
            var batteryPos=origin.Offset(0,0,1);Check(world.Place(batteryPos,IndustryId.Battery),"Place empty battery fixture");
            var battery=sim.At(batteryPos);battery.BatteryMode=BatteryMode.ChargeOnly;
            // Real placement transaction aligns the rear socket on each horizontal battery side.
            foreach(int face in new[]{0,1,4,5})
            {
                var pos=IndustryDefinition.Neighbor(batteryPos,face);var direction=world.Local(pos)-world.Local(batteryPos);
                player.transform.position=world.Local(pos)+direction*2;
                player.Camera.transform.position=world.Local(batteryPos)+Vector3.one*.5f+direction*3;
                player.Camera.transform.LookAt(world.Local(batteryPos)+Vector3.one*.5f);
                game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(IndustryId.HandCrank,1,0,1);game.Selected=0;
                Check(game.TryPlaceSelected()&&IndustrySimulation.Neighbor(sim.At(pos),4).Equals(batteryPos),"Placement aligns crank socket to battery side "+face);
                Check(world.Remove(pos,IndustryId.HandCrank),"Recover empty crank fixture "+face);
            }
            player.transform.position=world.Local(origin)+new Vector3(.5f,0,-2.5f);player.Yaw=0;player.Pitch=20;player.enabled=true;player.ResetMotion();
            game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(IndustryId.HandCrank,2,0,1);game.Selected=0;
            yield return new WaitForSecondsRealtime(.4f);
            // Aim at the battery's front; right-click must place rather than opening its inventory.
            player.Pitch=15;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForSecondsRealtime(.7f);
            var crank=sim.At(origin);
            Check(crank?.Definition.Id==IndustryId.HandCrank&&game.Mode==ScreenMode.Play,"Right-click attaches selected crank directly to battery without opening UI");
            Check(IndustrySimulation.Neighbor(crank,4).Equals(batteryPos),"Pointer placement connects the rear electrical socket");
            game.Inventory.Take(0,int.MaxValue);player.Pitch=20;yield return new WaitForSecondsRealtime(.3f);
            Check(player.HasTarget&&player.Target.Equals(origin),"Crosshair targets the placed crank");
            long before=BatteryPower.Amount(battery);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForSecondsRealtime(.7f);
            Check(BatteryPower.Amount(battery)-before==50000&&game.Mode==ScreenMode.Play,"One actual right-click charges exactly 50 J and keeps gameplay open");
            before=BatteryPower.Amount(battery);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSecondsRealtime(1.65f);
            var view=UnityEngine.Object.FindAnyObjectByType<IndustryPresentation>().ViewAt(origin);
            Check(view!=null&&view.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)==3044,"Unity renders the original two-part 3044-triangle crank");
            var pivot=view.GetComponentsInChildren<Transform>().Single(t=>t.name=="MotionSpinCrank");var rotation=pivot.localRotation;yield return new WaitForSecondsRealtime(.12f);
            Check(Quaternion.Angle(rotation,pivot.localRotation)>10,"Paid manual strokes animate the imported crank pivot");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForSecondsRealtime(.7f);
            long produced=BatteryPower.Amount(battery)-before;
            Check(produced>=150000&&produced<=250000&&produced%50000==0,"Holding right-click repeats bounded complete 50 J turns: "+produced+" mJ");
            before=BatteryPower.Amount(battery);yield return new WaitForSecondsRealtime(.7f);
            Check(before==BatteryPower.Amount(battery)&&crank.SupplyWatts==0,"Release stops repeated production");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSecondsRealtime(.15f);
            game.SetMode(ScreenMode.Pause);before=BatteryPower.Amount(battery);int pending=crank.PulseTicks;yield return new WaitForSecondsRealtime(.6f);
            Check(before==BatteryPower.Amount(battery)&&pending==crank.PulseTicks,"Pause freezes paid stroke and prevents repeated generation");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;game.SetMode(ScreenMode.Play);yield return new WaitForSecondsRealtime(.7f);
            before=BatteryPower.Amount(battery);player.Yaw=90;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSecondsRealtime(.6f);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(before==BatteryPower.Amount(battery),"Holding Use while looking away cannot remotely crank");
            player.enabled=false;player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);player.HeldBlock.enabled=false;
            player.Camera.transform.position=world.Local(origin)+new Vector3(2.8f,2.1f,-2.5f);player.Camera.transform.LookAt(world.Local(origin)+new Vector3(.5f,.5f,.85f));
            game.Sky.Clock.SetTime(.35);game.Sky.Apply();yield return Capture("hand-crank-battery");
            player.Camera.transform.position=world.Local(batteryPos)+new Vector3(2.5f,1.3f,.5f);player.Camera.transform.LookAt(world.Local(batteryPos)+Vector3.one*.5f);
            Check(game.TryOpenMachine(batteryPos),"Battery charge remains inspectable through its free side");yield return Capture("hand-crank-stored-power");game.SetMode(ScreenMode.Play);
            // Disk save/load preserves both exact energy and a partly completed paid turn.
            game.InitializeSaves(Path.Combine(output,"Saves"));game.SetMode(ScreenMode.Pause);
            Check(sim.TryCrank(crank),"Start paid stroke before checkpoint");
            before=BatteryPower.Amount(battery);pending=crank.PulseTicks;
            Check(game.SaveGame("Hand crank verification"),"Save crank and battery checkpoint");
            var entry=game.Saves.List().First(e=>!e.Backup);Check(game.LoadGame(entry),"Load crank and battery checkpoint");game.SetMode(ScreenMode.Pause);
            Check(BatteryPower.Amount(game.Industry.Simulation.At(batteryPos))==before&&game.Industry.Simulation.At(origin).PulseTicks==pending,"Save/load conserves exact battery charge and remaining paid stroke");
        }
    }
}
