using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewMovementInteractions()
        {
            var world=game.World;var player=game.Player;
            var saved=WorldPoint.FromLocal(player.transform.position,world.Origin);var carried=game.Inventory.Slots.ToArray();int selected=game.Selected;
            int frameRate=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;
            var changes=new Dictionary<BlockPos,byte>();
            void Set(BlockPos p,byte id)
            {
                if(!changes.ContainsKey(p))changes[p]=world.Get(p);
                byte old=world.Get(p);if(old!=0&&old!=id)world.Remove(p,old);if(id!=0&&old!=id&&!world.Place(p,id))throw new Exception("Movement fixture terrain unavailable");
            }
            int y=0;for(int x=-2;x<=2;x++)for(int z=-2;z<=28;z++)y=Math.Max(y,world.Generator.Height(x,z)+3);
            var cell=new BlockPos(0,y,0);var at=world.Local(cell);
            game.SetMode(ScreenMode.Play);game.Diagnostics=false;player.VerificationMovement=null;player.VerificationMining=false;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.QueueStateEvent(Mouse.current,new MouseState());
            player.enabled=false;
            for(int z=-2;z<=28;z++)for(int x=-2;x<=2;x++)Set(cell.Offset(x,-1,z),3);
            for(int slot=0;slot<60;slot++)game.Inventory.Take(slot,int.MaxValue);
            game.Inventory.Add(2,30);game.Selected=0;
            // Feet touching a block face share movement's 1 mm skin. The slight overlap
            // accepted by collision resolution must not make adjacent floor placement fail.
            Set(cell.Offset(1,-1,0),0);player.transform.position=at+new Vector3(.95f,-.0008f,.5f);
            Check(!world.Overlaps(player.transform.position,.6f,player.Height)&&game.CanPlace(cell.Offset(1,-1,0),out _),"A block below touching feet is legal at the same collision skin used by movement");
            Check(!game.CanPlace(cell,out _),"A block intersecting the player's legs still rejects placement");
            Set(cell.Offset(1,-1,0),3);
            var placeButton=PlayerPrefs.GetInt("mineButton",0)==0?MouseButton.Right:MouseButton.Left;
            QualitySettings.vSyncCount=0;
            foreach(int fps in new[]{20,60,120})
            {
                Application.targetFrameRate=fps;Set(cell,0);Set(cell.Offset(0,1,0),0);
                player.enabled=false;player.transform.position=at+new Vector3(.5f,.003f,.5f);player.Yaw=0;player.Pitch=85;player.enabled=true;
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.QueueStateEvent(Mouse.current,new MouseState());
                yield return new WaitForSecondsRealtime(.3f);
                Check(player.Grounded,"Pillar fixture starts grounded at target "+fps+" FPS");
                int before=game.Inventory.Total(2);
                InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(placeButton));
                yield return new WaitForSecondsRealtime(.08f);
                Check(world.Get(cell)==0&&game.Inventory.Total(2)==before,"Holding placement cannot fill occupied leg space at target "+fps+" FPS");
                Check(game.PlacementDiagnostic=="Cannot place inside the player"&&game.Message!="Cannot place inside the player","Body-overlap feedback is recorded for debugging without a normal HUD notification");
                if(fps==20)
                {
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(game.Input.Keys["Diagnostics"]));yield return null;yield return null;
                    Check(game.Diagnostics&&game.UI.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("Placement: Cannot place inside the player")),"The debug action opens the panel with the last placement rejection");
                    yield return Capture("f12-placement-debug");
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;yield return null;
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(game.Input.Keys["Diagnostics"]));yield return null;yield return null;
                    Check(!game.Diagnostics&&!game.UI.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("Placement: Cannot place inside the player")),"Toggling debug off hides the panel and its placement message");
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;yield return null;
                }
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(game.Input.Keys["Jump"]));
                float peak=0,until=Time.realtimeSinceStartup+1.2f;int framesSeen=0;
                yield return null;yield return null;
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                while(Time.realtimeSinceStartup<until){peak=Math.Max(peak,player.transform.position.y-at.y);framesSeen++;yield return null;}
                InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
                Check(world.Get(cell)==2&&game.Inventory.Total(2)==before-1,"Jump with held placement builds exactly one block underneath at target "+fps+" FPS (peak "+peak.ToString("F3")+" m, "+framesSeen+" frames)");
                Check(peak>=1.55f&&peak<=1.65f&&world.Get(cell.Offset(0,1,0))==0,"Jump reaches 1.6 blocks without allowing a second underfoot block at target "+fps+" FPS");
                Check(player.Grounded&&Math.Abs(player.transform.position.y-(at.y+1))<.02f&&!world.Overlaps(player.transform.position,.6f,player.Height),"Player lands on the new pillar block without penetration at target "+fps+" FPS");
                if(fps==60)yield return Capture("underfoot-jump-placement");
            }
            Set(cell,0);Application.targetFrameRate=60;
            player.enabled=false;player.transform.position=at+new Vector3(.5f,.003f,.5f);player.Yaw=0;player.Pitch=10;player.enabled=true;
            yield return new WaitForSecondsRealtime(.2f);
            IEnumerator Keys(params Key[] keys)
            {InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));yield return null;yield return null;}
            var forward=game.Input.Keys["Forward"];var crouch=game.Input.Keys["Crouch"];var sprint=game.Input.Keys["Sprint"];
            yield return Keys(forward);yield return new WaitForSecondsRealtime(.35f);
            Check(!player.Sprinting,"A single held forward press keeps walking");
            yield return Keys();yield return Keys(forward);
            Check(!player.Sprinting,"Forward presses outside the double-tap window keep walking");
            yield return Keys();yield return Keys(forward);
            Check(player.Sprinting,"A rapid second forward press starts sprinting");
            Vector3 runStart=player.transform.position;float runBegan=Time.time;
            yield return new WaitForSecondsRealtime(.25f);
            float measuredSpeed=(player.transform.position-runStart).magnitude/(Time.time-runBegan);
            Check(player.Sprinting&&measuredSpeed>6.1f&&measuredSpeed<6.9f,"Holding forward sustains double-tap sprint at "+measuredSpeed.ToString("F2")+" m/s");
            yield return Keys();Check(!player.Sprinting,"Releasing forward ends double-tap sprint");
            yield return Keys(forward);yield return Keys();yield return Keys(forward,crouch);
            Check(!player.Sprinting&&player.Height<1.5f,"Crouch cancels the sprint gesture");
            yield return Keys(forward);Check(!player.Sprinting,"Standing back up does not resume the cancelled sprint");
            yield return Keys(forward,sprint);Check(player.Sprinting,"The dedicated sprint key continues to work");
            yield return Keys();yield return Keys(forward);yield return Keys();yield return Keys(forward);
            Check(player.Sprinting,"Another double tap can restart sprinting");
            game.SetMode(ScreenMode.Inventory);yield return null;yield return null;
            Check(!player.Sprinting,"Opening inventory clears sprint state");
            game.SetMode(ScreenMode.Play);yield return null;yield return null;
            Check(!player.Sprinting,"Closing inventory while forward is held does not resume sprinting");
            yield return Keys();
            // Change only the in-memory mapping; user preferences must not be rewritten.
            game.Input.Keys["Forward"]=Key.UpArrow;
            yield return Keys(Key.UpArrow);yield return Keys();yield return Keys(Key.UpArrow);
            Check(player.Sprinting,"Double-tap sprint follows the rebound Forward action");
            yield return Keys();game.Input.Keys["Forward"]=forward;
            foreach(var pair in changes){byte current=world.Get(pair.Key);if(current!=0&&current!=pair.Value)world.Remove(pair.Key,current);if(pair.Value!=0&&world.Get(pair.Key)==0)world.Place(pair.Key,pair.Value);}
            for(int slot=0;slot<60;slot++){game.Inventory.Take(slot,int.MaxValue);if(!carried[slot].Empty)game.Inventory.Add(carried[slot].Id,carried[slot].Count,slot,slot+1);}
            game.Selected=selected;player.transform.position=saved.Local(world.Origin);player.Pitch=10;
            Application.targetFrameRate=frameRate;QualitySettings.vSyncCount=vsync;yield return null;
        }
    }
}
