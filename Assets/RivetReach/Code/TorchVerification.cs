using System.Collections;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewTorches()
        {
            var world=game.World;var player=game.Player;
            game.Mobs.enabled=false;game.Diagnostics=false;player.enabled=false;game.Items.enabled=false;
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
            Check(game.Inventory.Slots.All(s=>s.Empty),"Survival starts without free torches");
            foreach(byte fuel in new[]{BlockId.Coal,BlockId.Charcoal})
            {
                var craft=game.PersonalCrafting;craft.Grid.Add(fuel,1,0,1);craft.Grid.Add(BlockId.Stick,1,2,3);
                ItemStack cursor=default;
                Check(craft.CraftToCursor(ref cursor).Succeeded&&cursor.Id==BlockId.Torch&&cursor.Count==4&&craft.Grid.Slots.All(s=>s.Empty),"Personal grid consumes one "+game.Registry.Get(fuel).displayName+" above one stick for four torches");
                game.Inventory.Add(cursor.Id,cursor.Count);
                craft.Grid.Add(BlockId.Stick,1,0,1);craft.Grid.Add(fuel,1,2,3);
                Check(craft.Preview==null,"Reversed torch ingredients reject without consumption");
                craft.Grid.Take(0,1);craft.Grid.Take(2,1);
            }
            game.Selected=game.Inventory.FindSlot(s=>s.Id==BlockId.Torch);
            // A roofed room isolates artificial light from the moving night sky.
            var origin=world.Address(player.transform.position).Offset(-3,0,0);
            for(int z=0;z<=8;z++)for(int y=-1;y<=4;y++)for(int x=0;x<=6;x++)
            {
                var cell=origin.Offset(x,y,z);byte previous=world.Get(cell);
                if(previous!=0)world.Remove(cell,previous);
                if(y==-1||y==4||x==0||x==6||z==8)world.Place(cell,BlockId.Stone);
            }
            game.Sky.Clock.SetTime(4.95);game.Sky.Apply();
            player.transform.position=world.Local(origin)+new Vector3(3.5f,.02f,1.5f);
            player.Camera.transform.position=player.transform.position+Vector3.up*1.64f;
            var floor=origin.Offset(3,-1,4);var torch=floor.Offset(0,1,0);
            player.Camera.transform.LookAt(world.Local(floor)+new Vector3(.5f,1,.5f));
            Check(game.TryPlaceSelected()&&world.Get(torch)==BlockId.Torch&&game.Inventory.Total(BlockId.Torch)==7,"Floor placement consumes exactly one crafted torch");
            Check(!world.Solid(torch)&&!BlockId.Opaque(BlockId.Torch),"Torch does not block movement or skylight");
            Check(world.Raycast(world.Local(torch)+new Vector3(.5f,.5f,-1),Vector3.forward,3,out var target,out byte hit)&&target.Equals(torch)&&hit==BlockId.Torch,"Placed torch remains targetable for mining");
            var ceiling=origin.Offset(3,4,4);
            Check(!world.PlaceTorch(ceiling.Offset(0,-1,0),ceiling),"Ceiling attachment is rejected");
            var wet=origin.Offset(2,0,4);world.ChangeFluid(wet,0,Fluids.Water.Source);
            Check(!world.PlaceTorch(wet,wet.Offset(0,-1,0)),"Submerged placement is rejected");world.ChangeFluid(wet,Fluids.Water.Source,0);
            var support=origin.Offset(6,1,5);var wallTorch=support.Offset(-1,0,0);
            player.Camera.transform.LookAt(world.Local(support)+new Vector3(0,.5f,.5f));
            Check(game.TryPlaceSelected()&&world.Get(wallTorch)==BlockId.Torch&&game.Inventory.Total(BlockId.Torch)==6,"Wall placement consumes one torch and retains its attachment");
            player.Camera.transform.LookAt(world.Local(origin)+new Vector3(3.5f,1.25f,7.5f));
            yield return null;world.TorchView.Refresh();
            Check(world.TorchView.ActiveLightCount==2&&world.TorchView.Lights.Where(l=>l.enabled).All(l=>l.type==LightType.Point&&l.shadows!=LightShadows.None),"Floor and wall torches activate real shadow-casting point lights");
            // Compare actual rendered pixels with identical geometry/camera and disabled lights.
            world.TorchView.enabled=false;
            foreach(var light in world.TorchView.Lights)light.enabled=false;
            yield return Capture("torches-unlit");yield return new WaitForEndOfFrame();
            var dark=ScreenCapture.CaptureScreenshotAsTexture();
            world.TorchView.Refresh();yield return Capture("torches-lit");yield return new WaitForEndOfFrame();
            var lit=ScreenCapture.CaptureScreenshotAsTexture();
            double darkSum=0,litSum=0;int samples=0;
            for(int y=lit.height/3;y<lit.height*2/3;y++)for(int x=lit.width/4;x<lit.width*3/4;x++)
            {darkSum+=dark.GetPixel(x,y).grayscale;litSum+=lit.GetPixel(x,y).grayscale;samples++;}
            Destroy(dark);Destroy(lit);
            Check(litSum/samples>darkSum/samples+.025,"Rendered room brightens with point lights: mean luminance "+(darkSum/samples).ToString("F4")+" → "+(litSum/samples).ToString("F4"));
            world.TorchView.enabled=true;
            int dropped=game.Items.Total(BlockId.Torch);
            Check(world.Mine(torch,BlockId.Torch,ToolCapability.None,ToolTier.None),"A torch can be mined by hand");
            Check(game.Items.Total(BlockId.Torch)==dropped+1&&world.TorchView.ActiveLightCount==1&&!world.TorchSupport(torch,out _),"Mining returns one torch and removes its light and attachment");
            world.Remove(support,BlockId.Stone);
            Check(world.Get(wallTorch)==0&&game.Items.Total(BlockId.Torch)==dropped+2&&world.TorchView.ActiveLightCount==0,"Breaking a wall support drops one attached torch and removes its light");
            Check(world.PlaceTorch(torch,floor),"Torch can be replaced for fluid interaction");
            var above=torch.Offset(0,1,0);world.ChangeFluid(above,0,Fluids.Water.Source);
            for(int step=0;step<8;step++)world.FluidSimulation.Step(world);
            Check(Fluids.IsFluid(world.Get(torch))&&game.Items.Total(BlockId.Torch)==dropped+3&&!world.TorchSupport(torch,out _),"Flowing water washes away one torch and returns exactly one item");
            world.ChangeFluid(above,world.Get(above),0);world.ChangeFluid(torch,world.Get(torch),0);
            Check(world.PlaceTorch(torch,floor),"Torch can be replaced for streaming check");
            // Pause fluid/grass time during the focused residency round trip.
            game.SetMode(ScreenMode.Pause);
            var saved=WorldPoint.FromLocal(player.transform.position,world.Origin);
            player.transform.position+=Vector3.right*640;yield return null;yield return Settle(120);
            world.TorchView.Refresh();
            Check(!world.Ready(torch)&&world.Get(torch)==BlockId.Torch&&world.TorchSupport(torch,out var savedSupport)&&savedSupport.Equals(floor)&&world.TorchView.ActiveLightCount==0,"Unloading retains the torch and attachment without a distant light");
            player.transform.position=saved.Local(world.Origin);yield return null;yield return Settle(120);
            world.TorchView.Refresh();
            Check(world.Get(torch)==BlockId.Torch&&world.TorchView.ActiveLightCount==1&&Vector3.Distance(world.TorchView.Lights.First(l=>l.enabled).transform.position,world.TorchView.FlamePosition(torch,floor))<.001f,"Returning restores the placed torch and correctly positioned light after origin shifts");
            for(int z=2;z<7;z++)for(int x=1;x<6;x++)
            {var cell=origin.Offset(x,0,z);if(world.Get(cell)==0)world.PlaceTorch(cell,cell.Offset(0,-1,0));}
            world.TorchView.Refresh();
            Check(world.TorchView.ActiveLightCount==TorchPresentation.LightLimit&&world.TorchView.Lights.Count()==TorchPresentation.LightLimit,"Dense torch placement reuses the fixed eight-light pool");
            Check(errors.Count==0,"Torch scenario completes without Unity errors");
        }
    }
}
