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
        IEnumerator ReviewDoors()
        {
            var world=game.World;var player=game.Player;var sim=game.Industry.Simulation;
            game.SetCreative(true);game.Mobs.enabled=false;game.Items.enabled=false;game.Diagnostics=false;
            player.enabled=false;player.ResetMotion();var origin=world.Address(player.transform.position).Offset(0,2,4);
            for(int x=-3;x<=3;x++)for(int z=-3;z<=3;z++)for(int y=-1;y<=3;y++)
            {var p=origin.Offset(x,y,z);byte old=world.Get(p);if(old!=0)world.Remove(p,old);if(y==-1)world.Place(p,BlockId.Stone);}
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            game.Selected=0;game.Inventory.Add(IndustryId.WoodenDoor,3,0,1);
            player.transform.position=world.Local(origin)+new Vector3(.5f,0,-2);
            player.Camera.transform.position=world.Local(origin)+new Vector3(.5f,1.6f,-2);
            player.Camera.transform.LookAt(world.Local(origin)+new Vector3(.5f,-.02f,.5f));
            Check(world.Place(origin.Offset(0,1,0),BlockId.Planks),"Block upper placement cell");
            Check(!game.TryPlaceSelected()&&game.Inventory.Slots[0].Count==3&&world.Get(origin)==0,"Obstructed two-cell placement consumes nothing");
            world.Remove(origin.Offset(0,1,0),BlockId.Planks);
            game.SetCreative(false);Check(game.TryPlaceSelected(),"Player placement command accepts clear supported doorway");
            Check(game.Inventory.Slots[0].Count==2&&world.Get(origin)==IndustryId.WoodenDoor&&world.Get(origin.Offset(0,1,0))==IndustryId.DoorUpper,"Placement consumes one door for both cells");game.SetCreative(true);
            var door=sim.At(origin);Check(door!=null&&sim.At(origin.Offset(0,1,0))==null,"Both halves share one machine authority");
            game.Inventory.Take(0,int.MaxValue);player.Yaw=0;player.Pitch=0;player.enabled=true;yield return new WaitForSecondsRealtime(.5f);
            Check(player.HasTarget&&player.Target.Equals(origin.Offset(0,1,0)),"Standing crosshair targets upper door half");
            Check(world.Solid(origin)&&world.Solid(origin.Offset(0,1,0)),"Closed door blocks both cells");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForSecondsRealtime(.2f);
            Check(door.WorkInput==1&&game.Mode==ScreenMode.Play,"Actual right-click opens upper half without opening inventory");
            Check(!world.Solid(origin)&&!world.Solid(origin.Offset(0,1,0)),"Open door releases both collision cells");
            var mob=Resources.LoadAll<MobDefinition>("Mobs/Definitions").First(m=>m.width<1);
            Check(!MobNavigation.Standable(world,world.Local(origin)+new Vector3(.5f,2.006f,.5f),mob),"Open door cannot provide invisible support for creatures");
            yield return Capture("door-manual-open");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSecondsRealtime(.7f);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(door.WorkInput==0,"Held right-click closes exactly once");
            player.enabled=false;sim.ToggleDoor(door);player.transform.position=world.Local(origin)+new Vector3(.5f,.1f,.5f);sim.ToggleDoor(door);
            Check(door.WorkInput==1&&!door.Source,"Door cannot close through the player");
            player.transform.position=world.Local(origin)+new Vector3(.5f,0,-2);yield return new WaitForSecondsRealtime(.2f);Check(door.WorkInput==0,"Pending close completes after player exits");
            var cable=origin.Offset(1,0,0);var leverPos=origin.Offset(2,0,0);
            Check(world.Place(cable,IndustryId.SignalConduit)&&world.Place(leverPos,IndustryId.Lever),"Connect ordinary blue cable and lever at door base");yield return new WaitForSecondsRealtime(.3f);
            var lever=sim.At(leverPos);sim.Activate(lever);yield return new WaitForSecondsRealtime(.2f);
            Check(door.Signal&&door.WorkInput==1&&door.ReceivedWatts==0,"Live Blue Signal opens door without electrical power");
            var view=UnityEngine.Object.FindAnyObjectByType<IndustryPresentation>().ViewAt(origin);
            Check(view!=null&&view.GetComponentsInChildren<Renderer>().Length==2,"Unity instantiates original frame and hinged timber leaf");
            var pivot=view.GetComponentsInChildren<Transform>().Single(t=>t.name=="MotionDoor");
            Check(Quaternion.Angle(pivot.localRotation,Quaternion.Euler(0,-90,0))<.1f,"Imported door hinge follows open authority");
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);player.HeldBlock.enabled=false;
            player.Camera.transform.position=world.Local(origin)+new Vector3(1.7f,1.9f,-3.6f);player.Camera.transform.LookAt(world.Local(origin)+new Vector3(.9f,.9f,.5f));
            game.Sky.Clock.SetTime(.35);game.Sky.Apply();yield return Capture("door-signal-open");
            sim.Activate(lever);yield return new WaitForSecondsRealtime(.2f);Check(door.WorkInput==0&&!door.Signal,"OFF signal closes door");yield return Capture("door-signal-closed");
            int drops=0;world.BlockMined+=(p,id)=>{if(IndustryId.DoorPart(id))drops++;};
            Check(world.Mine(origin.Offset(0,1,0),IndustryId.DoorUpper,ToolCapability.None),"Mine upper half");
            Check(drops==1&&world.Get(origin)==0&&world.Get(origin.Offset(0,1,0))==0&&sim.At(origin)==null,"Upper mining removes whole door and emits exactly one recovery");
            Check(world.Place(origin,IndustryId.WoodenDoor),"Replace door for support removal");
            world.Remove(origin.Offset(0,-1,0),BlockId.Stone);
            Check(drops==2&&world.Get(origin)==0&&world.Get(origin.Offset(0,1,0))==0,"Removing floor recovers one whole door");
            Check(!world.Place(origin,IndustryId.WoodenDoor),"Unsupported door placement fails");
            world.Place(origin.Offset(0,-1,0),BlockId.Stone);world.Place(origin,IndustryId.WoodenDoor);yield return new WaitForSecondsRealtime(.2f);
            door=sim.At(origin);door.Rotation=3;sim.Invalidate();yield return new WaitForSecondsRealtime(.2f);sim.ToggleDoor(door);
            game.InitializeSaves(Path.Combine(output,"Saves"));game.SetMode(ScreenMode.Pause);
            Check(game.SaveGame("Doors verification"),"Save open rotated door with signal connection");var entry=game.Saves.List().First(e=>!e.Backup);
            Check(game.LoadGame(entry),"Load door checkpoint");game.SetMode(ScreenMode.Pause);
            door=game.Industry.Simulation.At(origin);
            Check(door!=null&&door.Rotation==3&&door.WorkInput==1&&door.Source&&game.World.Get(origin.Offset(0,1,0))==IndustryId.DoorUpper,"Save/load preserves footprint, rotation, requested and physical open state");
            game.SetMode(ScreenMode.Play);yield return Settle();yield return new WaitForSecondsRealtime(.3f);
            Check(!game.World.Solid(origin)&&!game.World.Solid(origin.Offset(0,1,0)),"Restored door remains passable after residency and topology rebuild");
        }
    }
}
