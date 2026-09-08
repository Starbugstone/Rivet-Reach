using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewInteractions()
        {
            var player=game.Player;var world=game.World;
            var saved=WorldPoint.FromLocal(player.transform.position,world.Origin);var carried=game.Inventory.Slots.ToArray();
            bool female=player.Female;int skin=player.Skin,selected=game.Selected;
            game.SetMode(ScreenMode.Play);game.Diagnostics=false;
            player.transform.position=world.Local(new BlockPos(8,world.Generator.Height(8,-8)+1,-8))+new Vector3(.5f,.01f,.5f);
            player.Yaw=25;player.Pitch=8;yield return null;yield return Settle();
            for(int slot=0;slot<60;slot++)game.Inventory.Take(slot,int.MaxValue);
            game.Selected=0;game.SetAppearance(false,0);yield return null;yield return null;
            Check(!player.HeldBlock.Visible&&player.HeldBlock.ItemId==0,"An empty hotbar slot shows the bare hand");
            Check(player.Arms.TriangleCount<=6000,"Only the dominant first-person arm is rendered");
            report.armsTriangles=player.Arms.TriangleCount;report.bodyTriangles=player.Body.TriangleCount;
            yield return Capture("interaction-01-bare-hand");
            player.Pitch=-65;player.VerificationMining=true;var rest=player.Arms.BonePosition("HandR");float travel=0;
            for(int frame=0;frame<30;frame++){yield return null;travel=Mathf.Max(travel,Vector3.Distance(rest,player.Arms.BonePosition("HandR")));}
            Check(travel>.15f,"Live fist swing travels through a broad arc even when aiming at air");
            player.VerificationMining=false;yield return new WaitForSecondsRealtime(.5f);
            Check(player.Arms.MiningWeight<.02f,"Releasing mining completes the swing and settles to idle");
            player.Pitch=8;yield return null;player.enabled=false;
            for(int frame=0;frame<5;frame++)
            {player.Arms.SamplePose("FP_Mine",frame*.06f);yield return Capture("interaction-swing-"+frame);}
            player.enabled=true;
            for(byte id=1;id<=3;id++)
            {
                game.Inventory.Add(id,4,id-1,id);game.Selected=id-1;yield return null;yield return null;yield return new WaitForSecondsRealtime(.2f);
                Check(player.HeldBlock.Visible&&player.HeldBlock.ItemId==id&&player.HeldBlock.Hand==player.Arms.Bone("HandR"),"Selected terrain item follows the right hand: "+id);
                var position=player.Camera.WorldToViewportPoint(player.HeldBlock.Centre);
                yield return Capture("interaction-held-"+id);
                Check(position.z>.05f&&position.x>.5f&&position.x<1&&position.y>0&&position.y<.5f,"Held block is framed in the lower-right camera: "+id);
            }
            float originalFov=player.Camera.fieldOfView;
            foreach(float fov in new[]{60f,100f})
            {
                player.Camera.fieldOfView=fov;yield return null;yield return null;
                var point=player.Camera.WorldToViewportPoint(player.HeldBlock.Centre);
                Check(point.x>.5f&&point.x<1&&point.y>0&&point.y<.5f,"Held block stays framed at "+fov+"-degree FOV");
                yield return Capture("interaction-held-fov-"+fov);
            }
            player.Camera.fieldOfView=originalFov;yield return null;
            player.enabled=false;
            for(int frame=0;frame<5;frame++)
            {
                player.Arms.SamplePose("FP_MineBlock",frame*.06f);yield return Capture("interaction-held-swing-"+frame);
                Check(player.HeldBlock.Visible&&Vector3.Distance(player.HeldBlock.Centre,player.Arms.BonePosition("HandR"))<.15f,"Held item remains attached throughout the swing");
            }
            player.enabled=true;
            foreach(var grip in new[]{GripPose.Tool,GripPose.TwoHandTool})
            {
                player.HeldBlock.SetPreview(grip);yield return new WaitForSecondsRealtime(.35f);
                Check(player.HeldBlock.Visible&&player.HeldBlock.Socket==player.Arms.Bone("ToolSocket"),"Handle preview attaches to authored grip: "+grip);
                Check((player.Arms.TriangleCount>6000)==(grip==GripPose.TwoHandTool),"Support hand renders only for the two-hand grip: "+grip);
                yield return Capture("interaction-grip-"+grip);
                player.enabled=false;
                foreach(float time in new[]{0f,.15f,.3f,.45f,.6f})
                {
                    player.Arms.SamplePose("FP_Mine"+grip,time);yield return Capture("interaction-grip-"+grip+"-swing-"+Mathf.RoundToInt(time*100));
                    Check(Vector3.Distance(player.HeldBlock.Centre,player.Arms.BonePosition("ToolSocket"))<.0001f,"Tool remains fixed in the grip throughout the strike");
                }
                player.enabled=true;
            }
            player.HeldBlock.SetPreview(null);yield return new WaitForSecondsRealtime(.35f);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState(game.Input.Keys["Inspect"]));yield return null;yield return null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState());yield return null;yield return null;
            Check(player.Inspecting&&player.HeldBlock.Visible&&player.HeldBlock.Hand==player.Body.Bone("HandR"),"Inspection attaches the selected block to the full-body hand");
            yield return Capture("interaction-inspect-held");
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState(game.Input.Keys["Inspect"]));yield return null;yield return null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,new UnityEngine.InputSystem.LowLevel.KeyboardState());yield return null;
            foreach(bool variant in new[]{false,true})foreach(int palette in new[]{0,1})
            {
                game.SetMode(ScreenMode.Appearance);game.SetAppearance(variant,palette);yield return null;yield return null;
                Check(!player.HeldBlock.Visible,"Appearance menu hides the held block");
                game.SetMode(ScreenMode.Play);yield return null;yield return null;
                Check(player.HeldBlock.Visible&&player.HeldBlock.Hand==player.Arms.Bone("HandR"),"Held block reattaches after model/skin changes");
                yield return Capture("interaction-held-"+(variant?"female":"male")+"-"+palette);
            }
            game.SetAppearance(false,0);game.Inventory.Take(game.Selected,int.MaxValue);yield return null;yield return null;
            Check(!player.HeldBlock.Visible,"Consuming the selected stack removes the held item");
            game.Selected=11;yield return null;yield return null;
            player.Pitch=65;yield return null;yield return null;
            Check(player.SelectionVisible&&player.HasTarget,"A single outline identifies the aimed terrain block");
            Check(GameObject.Find("Placement preview")==null,"No second coloured destination box is drawn");
            yield return Capture("interaction-target-outline");
            game.SetMode(ScreenMode.Inventory);yield return null;yield return null;
            Check(!player.SelectionVisible&&!player.HeldBlock.Visible,"Inventory hides interaction views");
            game.SetMode(ScreenMode.Play);

            // Explicit fixture above the generated surface, spanning X=-1/0 chunk boundaries.
            int y=0;for(int z=8;z<=12;z++)for(int x=-2;x<=2;x++)y=Math.Max(y,world.Generator.Height(x,z)+3);
            var target=new BlockPos(0,y,10);var dark=new BlockPos(1,y,11);var coveredGrass=new BlockPos(2,y,12);
            for(int z=8;z<=12;z++)for(int x=-2;x<=2;x++)
            {
                var p=new BlockPos(x,y,z);Check(world.Place(p,(byte)(x==-2||x==2||z==8||z==12?1:2)),"Grass fixture cell has ready empty terrain");
            }
            Check(world.Place(dark.Offset(0,3,0),3)&&world.Place(coveredGrass.Offset(0,1,0),3),"Grass fixture has both an overhead roof and direct cover");
            Check(world.SkyLight(target.Offset(0,1,0))==15&&world.SkyLight(dark.Offset(0,1,0))==0,"Sky exposure sees roofs above the immediate neighbour");
            int spawned=game.Items.TotalSpawned;player.enabled=false;player.transform.position=world.Local(target)+new Vector3(.5f,4,-4);
            player.Camera.transform.localPosition=Vector3.up*1.64f;player.transform.rotation=Quaternion.identity;player.Camera.transform.localRotation=Quaternion.Euler(52,0,0);
            yield return Capture("interaction-grass-before");
            var eligible=new[]{new ChunkPos(-1,y/32,0),new ChunkPos(0,y/32,0)};
            int ticks=0;
            while((world.Get(target)!=1||world.Get(coveredGrass)!=2)&&ticks<24000)
            {
                for(int i=0;i<100;i++){world.Grass.Step(world,eligible);ticks++;}
                report.grassTickMaxMs=Math.Max(report.grassTickMaxMs,world.LastGrassTickMs);yield return null;
            }
            Check(world.Get(target)==1,"Random ticks spread grass across a real resident chunk boundary");
            Check(world.Get(dark)==2,"Dirt under an overhead roof stays dirt");
            Check(world.Get(coveredGrass)==2,"A covered grass block decays to dirt");
            Check(game.Items.TotalSpawned==spawned,"Grass state changes create no item drops");
            yield return Settle();yield return Capture("interaction-grass-after");
            game.SetMode(ScreenMode.Pause);long tick=world.Grass.Tick;yield return new WaitForSecondsRealtime(.25f);
            Check(world.Grass.Tick==tick,"Pausing freezes grass simulation time");game.SetMode(ScreenMode.Play);
            long before=world.Grass.Tick;yield return new WaitForSecondsRealtime(.3f);
            Check(world.Grass.Tick>before,"Grass ticks resume during ordinary Play");
            Check(world.Remove(dark.Offset(0,3,0),3)&&world.SkyLight(dark.Offset(0,1,0))==15,"Removing a roof immediately invalidates cached sky exposure");
            report.grassChanges=world.Grass.Changes;
            player.enabled=true;player.transform.position=world.Local(new BlockPos(700,world.Generator.Height(700,0)+1,0))+new Vector3(.5f,.01f,.5f);yield return null;yield return Settle();
            Check(!world.Ready(target),"Grass fixture unloads during long traversal");
            player.transform.position=world.Local(new BlockPos(0,world.Generator.Height(0,0)+1,0))+new Vector3(.5f,.01f,.5f);yield return null;yield return Settle();
            Check(world.Ready(target)&&world.Get(target)==1&&world.Get(coveredGrass)==2,"Grass changes survive chunk reload and origin shifts");
            for(int slot=0;slot<60;slot++)game.Inventory.Take(slot,int.MaxValue);
            foreach(var stack in carried)if(!stack.Empty)game.Inventory.Add(stack.Id,stack.Count);
            game.Selected=selected;game.SetAppearance(female,skin);player.transform.position=saved.Local(world.Origin);player.Pitch=10;
        }
    }
}
