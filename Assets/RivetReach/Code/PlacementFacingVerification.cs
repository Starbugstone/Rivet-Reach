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
        IEnumerator ReviewPlacementFacing()
        {
            FreezeSaveFixture();game.SetMode(ScreenMode.Play);
            var world=game.World;var player=game.Player;
            var origin=world.Address(player.transform.position).Offset(0,2,4);
            for(int x=-5;x<=13;x++)for(int z=-5;z<=5;z++)for(int y=-1;y<=5;y++)
            {var p=origin.Offset(x,y,z);byte old=world.Get(p);if(old!=0)world.Remove(p,old);if(y==-1)world.Place(p,BlockId.Stone);}
            player.transform.position=world.Local(origin)+new Vector3(.5f,.01f,-2.5f);yield return Settle();
            var fronts=new[]{Vector3.back,Vector3.left,Vector3.forward,Vector3.right};
            var ids=new[]{BlockId.Furnace,BlockId.Chest,BlockId.Workbench,IndustryId.Bench,IndustryId.Crusher,IndustryId.Boiler,IndustryId.Battery};
            GameObject View(BlockPos p)=>StarterStationVisuals.UsesModel(world.Get(p))?world.GetComponent<StarterStationPresentation>().ViewAt(p):world.GetComponent<IndustryPresentation>().ViewAt(p);
            foreach(byte id in ids)for(int rotation=0;rotation<4;rotation++)
            {
                var centre=world.Local(origin)+Vector3.one*.5f;
                player.transform.position=centre+fronts[rotation]*2.5f;player.transform.position=new Vector3(player.transform.position.x,world.Local(origin).y+.01f,player.transform.position.z);
                player.Camera.transform.position=player.transform.position+Vector3.up*1.64f;
                player.Camera.transform.LookAt(world.Local(origin)+new Vector3(.5f,-.01f,.5f));
                game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(id,2,0,1);game.Selected=0;
                Check(game.PlacementPreview(out var preview,out _)&&preview.Equals(origin),"Target floor for "+id+" facing "+rotation);
                Check(game.TryPlaceSelected()&&game.Inventory.Slots[0].Count==1,"Placement consumes exactly one "+id+" facing "+rotation);
                int actual=BlockId.Station(id)?game.Survival.At(origin).Rotation:game.Industry.Simulation.At(origin).Rotation;
                Check(actual==rotation,"Authority faces player for "+id+" side "+rotation);
                yield return new WaitForSecondsRealtime(.4f);
                var view=View(origin);Check(view!=null&&Vector3.Dot(view.transform.rotation*Vector3.back,fronts[rotation])>.999f,"Rendered front faces player for "+id+" side "+rotation);
                var bounds=new Bounds();bool first=true;
                foreach(var renderer in view.GetComponentsInChildren<Renderer>())
                {if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
                if(StarterStationVisuals.UsesModel(id))Check(bounds.min.x>=centre.x-.501f&&bounds.max.x<=centre.x+.501f&&bounds.min.z>=centre.z-.501f&&bounds.max.z<=centre.z+.501f,"Rotated model stays in its occupied cell "+id+" side "+rotation);
                if(id==BlockId.Furnace)
                {
                    player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);player.Camera.transform.LookAt(centre);
                    game.Sky.Clock.SetTime(.35);game.Sky.Apply();yield return Capture("furnace-facing-"+rotation);
                }
                Check(world.Remove(origin,id),"Remove facing fixture "+id);
            }
            // Near-vertical and off-centre targeting must retain a horizontal front.
            Check(PlacementFacing.TowardsPlayer(Vector3.zero,new Vector3(-2,8,-.25f),0)==1,"Off-centre elevated player selects nearest west front");
            Check(PlacementFacing.TowardsPlayer(Vector3.zero,Vector3.up*4,270)==3,"Directly overhead uses stable horizontal yaw");
            Check(PlacementFacing.TowardsPlayer(Vector3.zero,new Vector3(.25f,-8,2),0)==2,"Player below selects nearest north front");
            // Exercise the real Use input once, facing east (the old furnace always faced south).
            player.transform.position=world.Local(origin)+new Vector3(3,.01f,.5f);player.ResetMotion();player.Yaw=270;player.Pitch=Mathf.Atan2(1.65f,2.5f)*Mathf.Rad2Deg;player.enabled=true;
            game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(BlockId.Furnace,2,0,1);
            yield return new WaitForSecondsRealtime(.3f);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return new WaitForSecondsRealtime(.3f);
            Check(game.Survival.At(origin)?.Rotation==3&&game.Inventory.Slots[0].Count==1,"Actual right-click places east-facing furnace and consumes one");
            FreezeSaveFixture();
            var chest=origin.Offset(3,0,2);var bench=origin.Offset(6,0,2);
            Check(world.Place(chest,BlockId.Chest)&&world.Place(bench,BlockId.Workbench),"Place persistence companions");
            game.Survival.At(chest).Rotation=1;game.Survival.At(chest).Storage.Add(BlockId.IronIngot,17);game.Survival.At(bench).Rotation=2;
            var home=player.transform.position;player.transform.position+=Vector3.right*80;yield return new WaitForSecondsRealtime(.4f);
            Check(View(origin)==null,"Distant facing presentation released");player.transform.position=home;yield return Settle();yield return new WaitForSecondsRealtime(.4f);
            Check(Vector3.Dot(View(origin).transform.rotation*Vector3.back,Vector3.right)>.999f,"Facing retained when presentation recreated");
            game.InitializeSaves(Path.Combine(output,"Saves"));Check(game.SaveGame("Placement facing",true),"Save placed facing");
            var entry=game.Saves.List().First(e=>!e.Backup);var bytes=game.Saves.Read(entry);
            using(var reader=game.Saves.Open(bytes,out _))Check(reader.Format==5,"Station facing uses explicit schema 5");
            Check(game.LoadGame(entry),"Load facing checkpoint: "+game.SaveStatus);FreezeSaveFixture();
            Check(game.CaptureSave(entry).SequenceEqual(bytes),"Full checkpoint round-trips byte-exactly with station rotations");
            Check(game.Survival.At(origin).Rotation==3&&game.Survival.At(chest).Rotation==1&&game.Survival.At(bench).Rotation==2&&game.Survival.At(chest).Storage.Total(BlockId.IronIngot)==17,"All starter facings and contents survive load");
            yield return Settle();yield return new WaitForSecondsRealtime(.4f);world=game.World;
            Check(Vector3.Dot(View(origin).transform.rotation*Vector3.back,Vector3.right)>.999f,"Loaded furnace model faces east");
            var intact=world;game.Survival.At(chest).Rotation=4;
            Check(game.SaveGame("Invalid facing fixture",true),"Write checksum-valid invalid facing fixture");game.Survival.At(chest).Rotation=1;
            var invalid=game.Saves.List().First(e=>e.Id==game.SaveId&&!e.Backup);
            Check(!game.LoadGame(invalid)&&game.World==intact&&game.Survival.At(chest).Rotation==1&&game.Survival.At(chest).Storage.Total(BlockId.IronIngot)==17,"Invalid facing rejects with full session rollback");
            Check(errors.Count==0,"Facing review logs no Unity errors");
        }

        IEnumerator ReviewFacingLegacy()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rr-legacy-directory");Check(index>=0,"Earlier checkpoint directory supplied");
            string directory=args[index+1];game.InitializeSaves(directory);var entries=game.Saves.List().Where(e=>!e.Backup).ToArray();
            var formats=new System.Collections.Generic.HashSet<int>();Check(entries.Length>=3,"Discover schema 2, 3 and 4 fixtures");
            foreach(var entry in entries)
            {
                game.InitializeSaves(directory);var bytes=game.Saves.Read(entry);
                using(var reader=game.Saves.Open(bytes,out _)){Check(reader.Format>=1&&reader.Format<=4,"Fixture predates station facing");formats.Add(reader.Format);}
                var changed=UnityEngine.Object.Instantiate(game.Registry);changed.Get(BlockId.IronIngot).stackLimit++;
                bool rejected=false;try{using var reader=new SaveStore(output,changed).Open(bytes,out _);}catch(InvalidDataException){rejected=true;}
                Destroy(changed);Check(rejected,"Legacy compatibility still rejects unrelated definition changes");
                Check(game.LoadGame(entry),"Load previous checkpoint: "+game.SaveStatus);FreezeSaveFixture();
                Check(game.Survival.Stations.All(s=>s.Value.Rotation==0),"Older stations retain original world-facing direction");
                game.InitializeSaves(Path.Combine(output,"Migrated"));Check(game.SaveGame(entry.Name,true),"Save migrated station state");
                var migrated=game.Saves.List().First(e=>e.Id==game.SaveId&&!e.Backup);var fresh=game.Saves.Read(migrated);
                Check(game.LoadGame(migrated),"Reload migrated checkpoint: "+game.SaveStatus);FreezeSaveFixture();
                Check(game.CaptureSave(migrated).SequenceEqual(fresh),"All migrated resources, machine orientations and pipe directions round-trip exactly");yield return null;
            }
            Check(formats.Contains(2)&&formats.Contains(3)&&formats.Contains(4),"Actual schema 2, 3 and 4 checkpoints migrate");
        }
    }
}
