using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewWorkshopFollowup()
        {
            var world=game.World;var player=game.Player;var sim=game.Industry.Simulation;
            game.SetCreative(true);game.Mobs.enabled=false;game.Items.enabled=false;game.Diagnostics=false;
            var origin=world.Address(player.transform.position).Offset(-4,3,5);
            player.enabled=false;player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);player.HeldBlock.enabled=false;
            for(int x=-3;x<13;x++)for(int z=-4;z<8;z++)for(int y=-2;y<4;y++)
            {var p=origin.Offset(x,y,z);byte old=world.Get(p);if(old!=0)world.Remove(p,old);if(y==-2)world.Place(p,BlockId.Stone);}
            MachineState Place(BlockPos p,byte id){Check(PlaceWorkshopItem(p,id),"Creative placement: "+game.Registry.Get(id).displayName);return sim.At(p);}
            // Correct placement points the actual front towards a builder in every compass direction.
            for(int rotation=0;rotation<4;rotation++)
            {
                var p=origin.Offset(rotation*2,0,5);world.Place(p.Offset(0,-1,0),BlockId.Stone);
                game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(IndustryId.TankController,1,0,1);game.Selected=0;
                player.transform.position=world.Local(p)+new Vector3(2,0,0);player.transform.rotation=Quaternion.Euler(0,rotation*90,0);
                player.Camera.transform.position=world.Local(p)+new Vector3(.5f,.8f,.5f);player.Camera.transform.rotation=Quaternion.LookRotation(Vector3.down,Vector3.forward);
                Check(game.TryPlaceSelected()&&sim.At(p).Rotation==rotation,"Controller front faces builder at yaw "+rotation*90);
                world.Remove(p,IndustryId.TankController);
            }
            // 2×2 water cells sit BELOW the pump, over a solid floor.
            var source=origin.Offset(1,-1,0);
            for(int x=0;x<2;x++)for(int z=0;z<2;z++)
            {
                var p=source.Offset(x,0,z);for(int face=0;face<6;face++)
                {var n=IndustryDefinition.Neighbor(p,face);if(n.Y==p.Y&&(n.X<source.X||n.X>source.X+1||n.Z<source.Z||n.Z>source.Z+1)&&world.Get(n)==0)world.Place(n,BlockId.Sandstone);}
            }
            Check(world.ChangeFluid(source,0,Fluids.Water.Source)&&world.ChangeFluid(source.Offset(1,0,1),0,Fluids.Water.Source),"Fill opposite corners of renewal pool");
            yield return new WaitForSecondsRealtime(2);
            Check(world.Get(source.Offset(1,0,0))==Fluids.Water.Source&&world.Get(source.Offset(0,0,1))==Fluids.Water.Source,"Natural renewal fills all four pool cells without electricity");
            var pump=Place(source.Offset(0,1,0),IndustryId.Pump);
            player.transform.position=world.Local(origin)+new Vector3(6,1,-3);player.Camera.transform.position=world.Local(origin)+new Vector3(4,3,-3);player.Camera.transform.LookAt(world.Local(source)+Vector3.one*.5f);
            yield return new WaitForSecondsRealtime(3);
            Check(pump.WaterMl==10000&&pump.Work==0&&pump.RequestedWatts==0&&pump.ReceivedWatts==0&&world.Get(source)==Fluids.Water.Source,"Pump extracts 10 L without electricity and the pool renews");
            yield return Capture("pump-no-electricity-renewal-pool");
            // Source removal by normal fluid authority renews even while the pump buffer is full.
            Check(world.ChangeFluid(source,Fluids.Water.Source,0),"Remove intake source to exercise natural renewal");yield return new WaitForSecondsRealtime(2);
            Check(world.Get(source)==Fluids.Water.Source&&pump.WaterMl==10000,"Renewal under a full pump is natural fluid behaviour");
            var battery=Place(pump.Position.Offset(0,0,1),IndustryId.Battery);Check(battery.EnergyCells[0].Charge(1000000),"Seed a bounded 1 kJ test charge");
            pump.WaterMl=0;player.transform.position=world.Local(origin)+new Vector3(6,1,-3);yield return new WaitForSecondsRealtime(3);
            Check(pump.WaterMl==10000&&battery.EnergyCells[0].Amount==1000000&&world.Get(source)==Fluids.Water.Source,"Pump uses no adjacent battery charge for 10 L; pool renews");
            long retained=battery.EnergyCells[0].Amount;yield return new WaitForSecondsRealtime(.5f);Check(retained==battery.EnergyCells[0].Amount,"Full pump requests no more stored electricity");
            player.Camera.transform.position=world.Local(pump.Position)+new Vector3(.5f,3,.5f);player.Camera.transform.LookAt(world.Local(pump.Position)+Vector3.one*.5f);Check(game.TryOpenMachine(pump.Position),"Open pump control");yield return Capture("pump-intake-status");game.SetMode(ScreenMode.Play);
            // A pump at water level replaces that water cell and cannot renew sandstone below.
            var low=source.Offset(1,0,1);world.ChangeFluid(low,world.Get(low),0);var lowPump=Place(low,IndustryId.Pump);
            yield return new WaitForSecondsRealtime(1);Check(world.Get(low.Offset(0,-1,0))==BlockId.Stone&&lowPump.Status==MachineStatus.NoWater&&lowPump.WaterMl==0,"Pump placed inside basin has solid intake; no water is created through solid ground");
            // Build a 2×1×2 bank using the same creative placement path.
            var pack=origin.Offset(6,0,0);var controller=Place(pack,IndustryId.BatteryController);
            for(int x=0;x<2;x++)for(int z=0;z<2;z++)if(x!=0||z!=0)Place(pack.Offset(x,0,z),IndustryId.Battery);
            player.transform.position=world.Local(origin)+new Vector3(4,1,-3);yield return new WaitForSecondsRealtime(1);
            Check(controller.Structure.Formed&&BatteryPower.Cells(controller).Count==3,"Player-built battery bank forms with three cells");
            var engine=Place(pack.Offset(4,0,0),IndustryId.Boiler);var alternator=Place(pack.Offset(5,0,0),IndustryId.Alternator);
            engine.WaterMl=10000;engine.Items.Add(BlockId.Coal,1);
            for(int x=0;x<=5;x++)Place(pack.Offset(x,0,-1),IndustryId.PowerCable);
            Place(pack.Offset(5,0,1),IndustryId.PowerCable);Place(pack.Offset(6,0,1),IndustryId.PowerCable);Place(pack.Offset(6,0,0),IndustryId.PowerCable);Place(pack.Offset(6,0,-1),IndustryId.PowerCable);
            player.transform.position=world.Local(origin)+new Vector3(4,1,-3);yield return new WaitForSecondsRealtime(1);
            Check(BatteryPower.Amount(controller)>0&&controller.BatteryWatts==400,"Actual alternator charges formed bank through cables");
            engine.WaterMl=0;var lamp=Place(pack.Offset(-1,0,-1),IndustryId.Lamp);sim.Rotate(lamp);
            player.transform.position=world.Local(origin)+new Vector3(4,1,-3);yield return new WaitForSecondsRealtime(.6f);
            Check(lamp.ReceivedWatts==20&&controller.BatteryWatts==-20,"Bank supplies lamp after generation stops");
            player.Camera.transform.position=world.Local(pack)+new Vector3(-3,2.5f,-4);player.Camera.transform.LookAt(world.Local(pack)+new Vector3(.6f,.6f,.5f));yield return Capture("battery-bank-powered-lamp");
            player.Camera.transform.position=world.Local(pack)+new Vector3(.5f,3,.5f);player.Camera.transform.LookAt(world.Local(pack)+Vector3.one*.5f);Check(game.TryOpenMachine(controller.Position),"Open battery bank control");yield return Capture("battery-bank-control");game.SetMode(ScreenMode.Play);
            // Restore ordinary live first-person control after inventory and pause transitions.
            player.transform.position=world.Local(origin)+new Vector3(0,0,-3);player.Yaw=0;player.Pitch=0;player.ResetMotion();player.HeldBlock.enabled=true;player.enabled=true;
            game.Sky.Clock.SetTime(.4);game.Sky.Apply();yield return new WaitForSecondsRealtime(1);
            yield return ReviewHeldWorkshopItems();
        }
        IEnumerator ReviewHeldWorkshopItems()
        {
            var player=game.Player;var held=player.HeldBlock;bool originalFemale=player.Female;
            var ids=game.Registry.items.Where(d=>d.runtimeId!=0).Select(d=>d.runtimeId).ToArray();
            foreach(byte id in ids)
            {
                game.SetMode(ScreenMode.Inventory);game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(id,1,0,1);game.Selected=0;yield return null;game.SetMode(ScreenMode.Play);
                yield return new WaitForSecondsRealtime(.4f);
                Check(held.Visible&&held.ItemId==id,"Live held selection after inventory: "+game.Registry.Get(id).displayName);
                var root=held.Socket.GetComponentsInChildren<Transform>().First(t=>t.name=="Selected item in hand");
                var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                Check(renderers.Length>0&&renderers.Any(r=>r.GetComponent<MeshFilter>()?.sharedMesh?.vertexCount>0),"Selected item has renderable geometry: "+id);
                // Freeze the pose, then compare actual framebuffer with/without ONLY the item.
                player.enabled=false;held.enabled=false;
                yield return new WaitForEndOfFrame();var visible=ScreenCapture.CaptureScreenshotAsTexture();
                foreach(var r in renderers)r.forceRenderingOff=true;
                yield return new WaitForEndOfFrame();var hidden=ScreenCapture.CaptureScreenshotAsTexture();
                foreach(var r in renderers)r.forceRenderingOff=false;
                var a=visible.GetPixels32();var b=hidden.GetPixels32();int pixels=0;
                for(int y=Screen.height/5;y<Screen.height*4/5;y++)for(int x=Screen.width/3;x<Screen.width;x++)
                {int i=y*Screen.width+x;if(Math.Abs(a[i].r-b[i].r)+Math.Abs(a[i].g-b[i].g)+Math.Abs(a[i].b-b[i].b)>90)pixels++;}
                Check(pixels>35,"Selected item contributes visible screen pixels: "+id+" / "+pixels);
                if(id==BlockId.Stone||id==BlockId.Sandstone||id==IndustryId.TankController||id==IndustryId.Battery||id==IndustryId.PowerCable||id==BlockId.WoodPickaxe||id==BlockId.WoodSword)File.WriteAllBytes(Path.Combine(output,"held-"+id+".png"),visible.EncodeToPNG());
                Destroy(visible);Destroy(hidden);held.enabled=true;player.enabled=true;
            }
            for(int variant=0;variant<2;variant++)
            {
                player.Female=variant==1;player.RefreshAppearance();game.SetMode(ScreenMode.Pause);yield return null;game.SetMode(ScreenMode.Play);yield return new WaitForSecondsRealtime(.5f);
                Check(held.Visible&&player.Arms.AnimationReady,"Held item returns after appearance rebuild and pause: "+variant);
            }
            player.Female=originalFemale;player.RefreshAppearance();yield return new WaitForSecondsRealtime(.4f);
            yield return Capture("held-item-after-session-transitions");
        }
    }
}
