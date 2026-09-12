using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewBatteryFill()
        {
            var world=game.World;var player=game.Player;var sim=game.Industry.Simulation;
            game.SetCreative(true);game.Mobs.enabled=false;game.Items.enabled=false;game.Diagnostics=false;
            player.enabled=false;player.ResetMotion();
            var origin=world.Address(player.transform.position).Offset(0,2,4);
            for(int x=-4;x<=5;x++)for(int z=-4;z<=4;z++)for(int y=-1;y<=4;y++)
            {var p=origin.Offset(x,y,z);byte old=world.Get(p);if(old!=0)world.Remove(p,old);if(y==-1)world.Place(p,BlockId.Stone);}
            Check(world.Place(origin,IndustryId.Battery),"Place an initially empty battery");
            var battery=sim.At(origin);var cell=battery.EnergyCells[0];battery.BatteryMode=BatteryMode.Isolated;
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);player.HeldBlock.enabled=false;
            player.transform.position=world.Local(origin)+new Vector3(.5f,0,-2);
            player.Camera.transform.position=world.Local(origin)+new Vector3(1.8f,1.25f,-1.7f);
            player.Camera.transform.LookAt(world.Local(origin)+Vector3.one*.5f);
            game.Sky.Clock.SetTime(.35);game.Sky.Apply();yield return new WaitForSecondsRealtime(.5f);
            var presentation=Object.FindAnyObjectByType<IndustryPresentation>();
            var root=presentation.ViewAt(origin);var fill=root.transform.Find("Stored energy fill");
            Check(fill!=null&&!fill.gameObject.activeSelf,"Empty battery has no visible charge liquid");
            Check(fill.GetComponent<Collider>()==null,"Charge display adds no collision or fluid authority");
            Check(fill.GetComponent<MeshFilter>().sharedMesh.triangles.Length==36,"Charge display adds one 12-triangle mesh");
            var material=fill.GetComponent<Renderer>().sharedMaterial;
            Check(material!=null&&material.IsKeywordEnabled("_EMISSION"),"Shared charge material has readable emission");
            yield return Capture("battery-empty");
            foreach(long amount in new[]{11800000L,50000000L,100000000L})
            {
                Check(cell.Charge(amount-cell.Amount),"Set charge fixture to "+amount+" mJ");yield return null;yield return null;
                float height=.72f*(float)(amount/(double)cell.Capacity);
                Check(fill.gameObject.activeSelf&&Mathf.Abs(fill.localScale.y-height)<.00001f&&Mathf.Abs(fill.localPosition.y-fill.localScale.y*.5f-.14f)<.00001f,"Fill rises from its fixed base to the actual stored fraction: "+amount);
                Check(fill.GetComponent<Renderer>().sharedMaterial==material,"Changing charge retains shared material");
                yield return Capture("battery-"+(amount==11800000?"11-8-percent":amount==50000000?"half":"full"));
            }
            Check(cell.Discharge(75000000),"Discharge fixture to one quarter");yield return null;yield return null;
            Check(Mathf.Abs(fill.localScale.y-.18f)<.00001f,"Live discharge lowers the existing fill without a topology rebuild");
            battery.Rotation=1;sim.Invalidate();yield return new WaitForSecondsRealtime(.3f);
            Check(Quaternion.Angle(root.transform.rotation,Quaternion.Euler(0,90,0))<.1f&&fill.parent==root.transform,"Charge fill follows battery rotation");
            Check(world.Place(origin.Offset(1,0,0),IndustryId.BatteryController),"Place adjacent bank controller");
            yield return new WaitForSecondsRealtime(.6f);
            Check(battery.Structure?.Formed==true&&BatteryPower.Cells(battery).Count==0,"Bank owns the cell endpoint");
            Check(fill.gameObject.activeSelf&&Mathf.Abs(fill.localScale.y-.18f)<.00001f,"Claimed bank cell still shows its own quarter charge");
            Check(world.Place(origin.Offset(-1,0,0),IndustryId.Battery),"Extend bank with empty cell");
            yield return new WaitForSecondsRealtime(.6f);
            var second=presentation.ViewAt(origin.Offset(-1,0,0)).transform.Find("Stored energy fill");
            Check(battery.Structure?.Formed==true&&!second.gameObject.activeSelf&&fill.gameObject.activeSelf,"Unequal bank cells show their own levels, not a duplicated aggregate");
            Check(second.GetComponent<Renderer>().sharedMaterial==material,"Bank cells share the charge material");
            game.InitializeSaves(Path.Combine(output,"Saves"));game.SetMode(ScreenMode.Pause);
            Check(game.SaveGame("Battery fill verification"),"Save charged bank checkpoint");
            Check(game.LoadGame(game.Saves.List().First(e=>!e.Backup)),"Reload charged bank checkpoint");
            game.SetMode(ScreenMode.Play);yield return Settle();yield return new WaitForSecondsRealtime(.5f);
            battery=game.Industry.Simulation.At(origin);cell=battery.EnergyCells[0];
            presentation=Object.FindAnyObjectByType<IndustryPresentation>();fill=presentation.ViewAt(origin).transform.Find("Stored energy fill");
            Check(cell.Amount==25000000&&fill.gameObject.activeSelf&&Mathf.Abs(fill.localScale.y-.18f)<.00001f,"Reconstructed view derives its fill from exact saved charge");
            Check(cell.Discharge(cell.Amount),"Exhaust remaining fixture charge");yield return null;yield return null;
            Check(!fill.gameObject.activeSelf,"Fully discharged battery hides its fill again");
        }
    }
}
