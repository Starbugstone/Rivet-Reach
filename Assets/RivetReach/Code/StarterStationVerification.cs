using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewStarterStationGraphics()
        {
            report.workload="Starter station Blender imports, actual station input, burning embers, held/dropped models, view residency and saved state";
            var world=game.World;var player=game.Player;game.Mobs.enabled=false;game.Diagnostics=false;
            var origin=world.Address(player.transform.position).Offset(0,2,4);
            for(int x=-5;x<=9;x++)for(int z=-6;z<=5;z++)for(int y=-1;y<=6;y++)
            {
                var p=origin.Offset(x,y,z);byte before=world.Get(p);if(before!=0)world.Remove(p,before);
                if(y==-1)world.Place(p,BlockId.Stone);
            }
            var ids=new[]{BlockId.Workbench,BlockId.Chest,BlockId.Furnace,IndustryId.Bench};
            var positions=Enumerable.Range(0,4).Select(i=>origin.Offset(i*2,0,0)).ToArray();
            for(int i=0;i<ids.Length;i++)Check(world.Place(positions[i],ids[i]),"Place review station "+ids[i]);
            player.ResetMotion();player.transform.position=world.Local(origin)+new Vector3(.5f,.01f,-2);player.Yaw=0;
            yield return Settle();yield return new WaitForSecondsRealtime(.4f);
            var presentation=world.GetComponent<StarterStationPresentation>();
            for(int i=0;i<3;i++)
            {
                var pos=positions[i];var view=presentation.ViewAt(pos);Check(view!=null,"Placed authored model exists "+ids[i]);
                Check(view.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)>100,"Runtime uses detailed mesh "+ids[i]);
                player.transform.position=world.Local(pos)+new Vector3(.5f,.01f,-2);player.ResetMotion();yield return null;
                var direction=(world.Local(pos)+Vector3.one*.5f-player.Camera.transform.position).normalized;
                player.Yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(direction.y)*Mathf.Rad2Deg;
                yield return null;yield return StarterKey(game.Input.Keys["Interact"]);
                Check(game.OpenStation==game.Survival.At(pos),"Interact opens real station "+ids[i]);
                if(i==0)Check(game.Crafting.Grid.Size==3,"Workbench retains 3x3 crafting");
                if(i==1)Check(game.OpenStation.Storage.Count==27,"Chest retains 27 storage slots");
                game.SetMode(ScreenMode.Play);yield return null;yield return StarterUse();
                Check(game.OpenStation==game.Survival.At(pos),"Mouse Use opens real station "+ids[i]);
                yield return Capture("station-ui-"+StarterStationVisuals.Key(ids[i]));game.SetMode(ScreenMode.Play);
            }
            // Hold real stacks through the production grip path and create real world piles.
            for(int i=0;i<3;i++)
            {
                game.Inventory.Take(i,int.MaxValue);game.Inventory.Add(ids[i],1,i,i+1);game.Selected=i;
                yield return new WaitForSecondsRealtime(1);
                Check(player.GetComponentsInChildren<MeshFilter>().Any(f=>f.sharedMesh!=null&&f.sharedMesh.name.StartsWith(StarterStationVisuals.Key(ids[i])+"_")),"Held model uses authored geometry "+ids[i]);
                yield return Capture("station-held-"+StarterStationVisuals.Key(ids[i]));
                game.Items.Spawn(new ItemStack(ids[i],1),world.Local(origin.Offset(i*2,0,-1))+new Vector3(.5f,.4f,.5f),Vector3.zero,30);
            }
            yield return new WaitForSecondsRealtime(.5f);
            foreach(byte id in ids.Take(3))Check(game.Items.Piles.Any(p=>p.Stack.Id==id&&p.View!=null&&p.View.GetComponentsInChildren<MeshFilter>().Any(f=>f.sharedMesh.name.StartsWith(StarterStationVisuals.Key(id)+"_"))),"Dropped model uses authored geometry "+id);
            var chest=game.Survival.At(positions[1]);chest.Storage.Add(BlockId.IronIngot,17);
            var furnace=game.Survival.At(positions[2]).Furnace;
            var input=new ItemStack(BlockId.RawIron,2);furnace.Click(0,ref input,false);var fuel=new ItemStack(BlockId.Coal,1);furnace.Click(1,ref fuel,false);
            game.Survival.Wake(positions[2]);game.Survival.AdvanceTicks(1);
            game.Items.enabled=false;game.SetMode(ScreenMode.Pause);yield return null;player.enabled=false;
            var ember=presentation.ViewAt(positions[2]).GetComponentsInChildren<Renderer>().Single(r=>r.name=="Embers");
            var properties=new MaterialPropertyBlock();ember.GetPropertyBlock(properties);
            Check(properties.GetColor("_EmissionColor").r>1&&furnace.BurnTicks>0,"Firebox emission follows actual burning fuel");
            var camera=player.Camera;player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
            void HideUI(){foreach(var canvas in game.UI.VisibleRoot.GetComponentsInChildren<Canvas>())canvas.enabled=false;}
            HideUI();game.Sky.Clock.SetTime(.39);game.Sky.Apply();Debug.developerConsoleVisible=false;
            IEnumerator Shot(string name,Vector3 eye,Vector3 target,float fov=45)
            {
                camera.fieldOfView=fov;camera.transform.position=world.Local(origin)+eye;camera.transform.LookAt(world.Local(origin)+target);
                yield return new WaitForSecondsRealtime(.5f);yield return Capture(name);
            }
            yield return Shot("station-workshop",new Vector3(-2.6f,3.6f,-6.8f),new Vector3(3,.45f,.4f),52);
            for(int i=0;i<3;i++)yield return Shot("station-"+StarterStationVisuals.Key(ids[i]),new Vector3(i*2-1.1f,1.5f,-1.8f),new Vector3(i*2+.5f,.47f,.5f),43);
            yield return Shot("station-back",new Vector3(7,3.1f,4.3f),new Vector3(2.5f,.4f,.5f),53);
            game.Survival.AdvanceTicks(400);Check(furnace.Slots[2].Id==BlockId.IronIngot&&furnace.Slots[2].Count==2,"Revised furnace still smelts two iron inputs");
            game.Survival.AdvanceTicks(1600);yield return null;ember.GetPropertyBlock(properties);
            Check(furnace.BurnTicks==0&&properties.GetColor("_EmissionColor")==Color.black,"Firebox cools when actual fuel expires");
            yield return Shot("station-furnace-cold",new Vector3(2.9f,1.5f,-1.8f),new Vector3(4.5f,.47f,.5f),43);
            var home=player.transform.position;player.transform.position+=Vector3.right*80;yield return new WaitForSecondsRealtime(.4f);
            Check(presentation.ViewAt(positions[1])==null,"Distant presentation is released");
            player.transform.position=home;yield return Settle();yield return new WaitForSecondsRealtime(.4f);
            Check(presentation.ViewAt(positions[1])!=null&&game.Survival.At(positions[1])==chest&&chest.Storage.Total(BlockId.IronIngot)==17,"Returning restores view and preserves storage authority");
            game.InitializeSaves(System.IO.Path.Combine(output,"Saves"));Check(game.SaveGame("Station art review",true),"Save revised stations");
            var entry=game.Saves.List().First(e=>!e.Backup);Check(game.LoadGame(entry),"Load revised stations");
            game.SetMode(ScreenMode.Pause);game.Player.enabled=false;game.Mobs.enabled=false;yield return Settle();yield return new WaitForSecondsRealtime(.4f);
            presentation=game.World.GetComponent<StarterStationPresentation>();
            foreach(var pos in positions.Take(3))Check(presentation.ViewAt(pos)!=null,"Saved station rebuilds its model "+pos);
            Check(game.Survival.At(positions[1]).Storage.Total(BlockId.IronIngot)==17,"Saved chest retains all stored iron");
            Check(game.Survival.At(positions[2]).Furnace.Slots[2].Count==2,"Saved furnace retains smelted output");
            Check(game.World.Remove(positions[1],BlockId.Chest),"Mine revised chest");yield return new WaitForSecondsRealtime(.4f);
            Check(presentation.ViewAt(positions[1])==null&&game.Survival.At(positions[1])==null,"Mining removes station view and authority");
            Check(game.Items.Piles.Where(p=>p.Stack.Id==BlockId.IronIngot).Sum(p=>p.Stack.Count)==17,"Mining drops exactly the chest contents");
            Check(errors.Count==0,"Station graphics review has no Unity errors");
        }
    }
}
