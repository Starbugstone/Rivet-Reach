using System;
using System.Collections;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        // Opt-in photographic fixture: actual placed blocks, production renderers and simulation.
        // No scene changes are applied to ordinary sessions.
        IEnumerator CaptureMachineryShowcase()
        {
            report.workload="Staged in-game machinery photographs: real formed tank, working boiler/alternator, crusher, pipe fittings and powered lamps";
            var world=game.World;var player=game.Player;var sim=game.Industry.Simulation;
            game.Mobs.enabled=false;player.enabled=false;game.Items.enabled=false;game.Diagnostics=false;
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
            foreach(var canvas in game.UI.VisibleRoot.GetComponentsInChildren<Canvas>())canvas.enabled=false;
            Debug.developerConsoleVisible=false;
            var origin=world.Address(player.transform.position).Offset(-3,0,7);
            for(int x=-5;x<=10;x++)for(int z=-9;z<=7;z++)for(int y=-2;y<=8;y++)
            {
                var p=origin.Offset(x,y,z);byte b=world.Get(p);if(b!=0)world.Remove(p,b);
                if(y<0)world.Place(p,BlockId.Stone);
            }
            // Clear foreground vegetation along the photographic sightlines.
            for(int x=-12;x<=10;x++)for(int z=-17;z<=-8;z++)for(int y=0;y<=12;y++)
            {
                var p=origin.Offset(x,y,z);byte b=world.Get(p);
                if(b==BlockId.Log||b==BlockId.Leaves)world.Remove(p,b);
            }
            MachineState Place(int x,int y,int z,byte id,int rotation=0)
            {
                var p=origin.Offset(x,y,z);byte b=world.Get(p);if(b!=0)world.Remove(p,b);
                Check(world.Place(p,id),"Showcase placed "+game.Registry.Get(id).displayName);
                var m=sim.At(p);for(int i=0;i<rotation;i++)sim.Rotate(m);return m;
            }
            var bounds=new StructureBounds(origin,origin.Offset(5,4,4));
            for(int x=0;x<6;x++)for(int y=0;y<5;y++)for(int z=0;z<5;z++)
            {
                int axes=bounds.BoundaryAxes(origin.Offset(x,y,z));if(axes==0)continue;
                Place(x,y,z,axes>=2?IndustryId.TankFrame:y==0||y==4?IndustryId.TankWall:IndustryId.TankGlass);
            }
            var controller=Place(1,1,0,IndustryId.TankController);
            Place(2,1,0,IndustryId.TankHatch);Place(4,1,0,IndustryId.TankValve);
            var sensor=Place(1,2,0,IndustryId.TankSensor);sensor.LevelThreshold=50;
            Place(1,2,-1,IndustryId.Indicator,2);
            var outlet=Place(0,1,2,IndustryId.TankPort,1);outlet.PortMode=FluidPortMode.Output;
            Place(-1,1,2,IndustryId.FluidPipe);Place(-2,1,2,IndustryId.FluidPipe);
            for(int y=2;y<=3;y++)Place(-2,y,2,IndustryId.FluidPipe);
            for(int z=1;z>=-4;z--)Place(-2,3,z,IndustryId.FluidPipe);
            for(int y=0;y<3;y++)Place(-2,y,-4,IndustryId.FluidPipe);
            // An elevated cross feeds a second vessel, with a capped expansion branch.
            Place(-3,3,-1,IndustryId.FluidPipe);Place(-4,3,-1,IndustryId.FluidPipe);
            Place(-1,3,-1,IndustryId.FluidPipe);Place(0,3,-1,IndustryId.FluidPipe);
            Place(-2,4,0,IndustryId.FluidPipe);
            var elevatedTank=Place(-5,3,-1,IndustryId.Tank,2);
            for(int y=0;y<3;y++)Place(-5,y,-1,BlockId.Stone);
            var engine=Place(-2,0,-5,IndustryId.Boiler);var alternator=Place(-1,0,-5,IndustryId.Alternator);
            engine.Items.Add(BlockId.Charcoal,8,0,1);engine.WaterMl=10000;
            for(int x=-1;x<=7;x++)Place(x,0,-4,IndustryId.PowerCable);
            var crusher=Place(4,0,-5,IndustryId.Crusher);crusher.Items.Add(BlockId.RawIron,64,0,1);
            Place(1,0,-5,BlockId.Chest);Place(2,0,-5,IndustryId.Extractor);
            var item=Place(3,0,-5,IndustryId.ItemPipe);item.Additions=PipeAddition.Power|PipeAddition.Signal;
            var itemOutlet=Place(5,0,-5,IndustryId.ItemPipe);itemOutlet.Additions=PipeAddition.Power|PipeAddition.Signal;
            for(int y=0;y<=2;y++){var rise=Place(6,y,-5,IndustryId.ItemPipe);rise.Additions=PipeAddition.Power|PipeAddition.Signal;}
            Place(7,2,-5,BlockId.Chest);Place(7,0,-5,BlockId.Stone);Place(7,1,-5,BlockId.Stone);
            var lamps=new[]{Place(-1,0,-3,IndustryId.Lamp,2),Place(2,0,-3,IndustryId.Lamp,2),Place(7,0,-3,IndustryId.Lamp,2)};
            for(int x=4;x<=6;x++)
            {var pipe=Place(x,1,-2,IndustryId.FluidPipe);pipe.Additions=PipeAddition.Power|PipeAddition.Signal;}
            var neck=Place(4,1,-1,IndustryId.FluidPipe);neck.Additions=PipeAddition.Power|PipeAddition.Signal;
            var lever=Place(4,1,-3,IndustryId.Lever);sim.Activate(lever);
            Place(6,0,-2,IndustryId.PowerCable);Place(6,0,-3,IndustryId.PowerCable);
            var feed=Place(7,1,-2,IndustryId.Tank,2);feed.WaterMl=100000;
            // A low stone pedestal supports the raised feed vessel.
            Place(7,0,-2,BlockId.Stone);
            player.transform.position=world.Local(origin)+new Vector3(3,1,-7);
            yield return new WaitForSecondsRealtime(2);
            Check(controller.Structure.Formed,"Photographed 6 x 5 x 5 tank forms from real editable blocks");
            controller.Structure.Fluid.Deposit(Fluids.Water,6600000);
            yield return new WaitForSecondsRealtime(1);
            Check(alternator.SupplyWatts==400&&crusher.ReceivedWatts>0,"Photographed workshop is running on boiler power");
            foreach(var lamp in lamps)Check(lamp.Running,"Photographed workshop lamp has real electrical supply");
            Check(sensor.Source,"Photographed level sensor responds to shared tank contents");
            sim.Invalidate();yield return new WaitForSecondsRealtime(.5f);
            var presentation=world.GetComponent<IndustryPresentation>();var cross=origin.Offset(-2,3,-1);
            Check(sim.FluidNetwork.Connections[cross]==51,"Fluid header renders an actual four-way planar cross");
            Check(sim.FluidNetwork.Connections[origin.Offset(-2,3,0)]==52,"Fluid header includes an upward T junction");
            Check(sim.FluidNetwork.Connections[origin.Offset(-2,3,2)]==40,"Tank outlet turns from a vertical riser into the overhead header");
            Check(sim.ItemNetwork.Connections[origin.Offset(6,1,-5)]==12,"Item pipe connects vertically on both faces");
            Check(elevatedTank.WaterMl>0,"Overhead fluid route supplies the elevated vessel");
            var branch=origin.Offset(-1,3,-1);world.Remove(branch,IndustryId.FluidPipe);
            yield return new WaitForSecondsRealtime(.4f);
            Check(sim.FluidNetwork.Connections[cross]==50,"Removing a branch reduces the cross to a T");
            Check(presentation.ViewAt(cross).GetComponentInChildren<MeshFilter>().sharedMesh==ConnectedPipeVisuals.Shape("fluid_pipe",50),"Live renderer updates to the three-branch Blender shape");
            world.Place(branch,IndustryId.FluidPipe);yield return new WaitForSecondsRealtime(.4f);
            Check(sim.FluidNetwork.Connections[cross]==51&&presentation.ViewAt(cross).GetComponentInChildren<MeshFilter>().sharedMesh==ConnectedPipeVisuals.Shape("fluid_pipe",51),"Replacing the branch restores the real cross mesh");
            Check(presentation.ViewAt(origin.Offset(-2,3,-2)).GetComponentsInChildren<MeshRenderer>().Length==1,"Straight pipe has one continuous surface renderer");
            Check(presentation.ViewAt(origin.Offset(6,1,-5)).GetComponentsInChildren<MeshRenderer>().Length==3,"Vertical item pipe has one surface plus two independent fitted leads");
            // The session is paused between stills to hold fluid level, animated machinery and clock.
            game.SetMode(ScreenMode.Pause);
            foreach(var canvas in game.UI.VisibleRoot.GetComponentsInChildren<Canvas>())canvas.enabled=false;
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
            IEnumerator Shot(string name,Vector3 position,Vector3 target,float fov,double time)
            {
                game.Sky.Clock.SetTime(time);game.Sky.Apply();
                player.Camera.fieldOfView=fov;
                player.Camera.transform.position=world.Local(origin)+position;
                player.Camera.transform.LookAt(world.Local(origin)+target);
                yield return new WaitForSecondsRealtime(.6f);yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(output,name+".png"),2);
                yield return new WaitForSecondsRealtime(1);
            }
            yield return Shot("machinery-01-golden-workshop",new Vector3(-7,6,-10),new Vector3(2,1.7f,-.5f),48,.718);
            yield return Shot("machinery-02-tank-hero",new Vector3(-4.5f,1.8f,-5.5f),new Vector3(2.4f,2.4f,1.4f),57,.715);
            yield return Shot("machinery-03-connected-controls",new Vector3(6,2.8f,-5),new Vector3(2.6f,1.9f,.2f),47,.36);
            yield return Shot("machinery-04-blue-hour",new Vector3(-6,3.2f,-8),new Vector3(2,1.9f,-.2f),54,.7575);
            yield return Shot("machinery-05-steam-and-steel",new Vector3(-3.6f,1.0f,-6.8f),new Vector3(-.8f,.6f,-4.5f),55,.715);
            yield return Shot("machinery-06-vertical-junctions",new Vector3(-5,4.2f,-4),new Vector3(-2,2.6f,-.5f),57,.70);
            Check(errors.Count==0,"Showcase captures complete without Unity errors");
        }
    }
}
