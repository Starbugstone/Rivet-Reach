using System.Collections;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewMultiblocks()
        {
            var world=game.World;var player=game.Player;var sim=game.Industry.Simulation;
            game.Mobs.enabled=false;player.enabled=false;game.Items.enabled=false;game.Diagnostics=false;
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
            Check(game.Inventory.Slots.All(s=>s.Empty),"Ordinary session starts empty handed");
            var start=world.Address(player.transform.position);
            var origin=new BlockPos(start.Chunk.X*32+29,start.Y,start.Z+5);
            // Deliberately straddle an X chunk boundary with actual editable world cells.
            for(int x=-3;x<11;x++)for(int z=-5;z<8;z++)for(int y=-1;y<7;y++)
            {var p=origin.Offset(x,y,z);byte b=world.Get(p);if(b!=0)world.Remove(p,b);if(y==-1)world.Place(p,BlockId.Stone);}
            MachineState Place(int x,int y,int z,byte id,int rotation=0)
            {var p=origin.Offset(x,y,z);byte b=world.Get(p);if(b!=0)world.Remove(p,b);Check(world.Place(p,id),"Place "+game.Registry.Get(id).displayName);var m=sim.At(p);for(int i=0;i<rotation;i++)sim.Rotate(m);return m;}
            const int width=6,height=4,depth=5;
            var bounds=new StructureBounds(origin,origin.Offset(width-1,height-1,depth-1));
            for(int x=0;x<width;x++)for(int y=0;y<height;y++)for(int z=0;z<depth;z++)
            {var p=origin.Offset(x,y,z);int axes=bounds.BoundaryAxes(p);if(axes==0)continue;Place(x,y,z,axes>=2?IndustryId.TankFrame:y==0||y==height-1?IndustryId.TankWall:IndustryId.TankGlass);}
            var controller=Place(1,1,0,IndustryId.TankController);var hatch=Place(2,1,0,IndustryId.TankHatch);
            var valve=Place(3,1,0,IndustryId.TankValve);var outputPort=Place(5,1,2,IndustryId.TankPort,3);outputPort.PortMode=FluidPortMode.Output;
            var sensor=Place(1,2,0,IndustryId.TankSensor);sensor.LevelThreshold=50;
            var feed=Place(3,1,-2,IndustryId.Tank,3);feed.WaterMl=100000;
            var wired=Place(3,1,-1,IndustryId.FluidPipe);wired.Additions=PipeAddition.Signal|PipeAddition.Power;
            var lever=Place(2,1,-1,IndustryId.Lever);Place(3,0,-1,IndustryId.PowerCable);
            var sink=Place(7,1,2,IndustryId.Tank);Place(6,1,2,IndustryId.FluidPipe);
            var indicator=Place(1,2,-1,IndustryId.Indicator,2);
            player.transform.position=world.Local(origin)+new Vector3(3,1,-4);
            player.Camera.transform.position=world.Local(origin)+new Vector3(8,5,-6);player.Camera.transform.LookAt(world.Local(origin)+new Vector3(2.8f,1.5f,2));
            game.Sky.Clock.SetTime(.4);game.Sky.Apply();yield return new WaitForSecondsRealtime(1);
            var tank=controller.Structure;
            Check(tank.Formed&&tank.Bounds.Width==6&&tank.Fluid.Capacity==6000000,"6×4×5 tank forms across chunk boundary at 6,000 L");
            Check(valve.Structure==tank&&hatch.Structure==tank&&outputPort.Structure==tank,"All shell interfaces share controller storage");
            Check(feed.WaterMl==100000&&tank.Fluid.Amount==0,"OFF wired signal closes tank valve");
            sim.Activate(lever);yield return new WaitForSecondsRealtime(.5f);
            Check(feed.WaterMl<100000&&feed.WaterMl+tank.Fluid.Amount+sink.WaterMl==100000,"Wired pipe opens valve and conserves fluid through shared ports");
            tank.Fluid.Deposit(Fluids.Water,3600000);yield return new WaitForSecondsRealtime(.25f);
            Check(sensor.Source&&indicator.Signal,"Actual tank level sensor emits binary signal above configured threshold");
            yield return Capture("multiblock-formed-connected-tank");
            var presentation=world.GetComponent<MultiblockPresentation>();Check(presentation.ViewCount==tank.Validation.Members.Count,"Every placed shell cell has a live view");
            var tankRoots=presentation.RenderedRoots.ToArray();
            var rendered=tankRoots.SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>()).Where(r=>r.gameObject.activeInHierarchy&&r.enabled).ToArray();
            int glassRenderers=rendered.Count(r=>r.name=="Connected glass");
            int tankTriangles=tankRoots.SelectMany(r=>r.GetComponentsInChildren<MeshFilter>()).Where(f=>f.gameObject.activeInHierarchy).Sum(f=>f.sharedMesh.triangles.Length/3);
            int tankRenderers=rendered.Length;
            Check(tankRenderers<=presentation.ViewCount*3+1,"Connected tank views use at most three material groups per cell plus one liquid volume");
            report.triangles=tankTriangles;
            System.IO.File.WriteAllText(System.IO.Path.Combine(output,"multiblock-render-cost.txt"),$"6×4×5 formed tank: {tankTriangles} active triangles, {tankRenderers} mesh renderers, {glassRenderers} glass renderers, {presentation.ViewCount} shell views. Includes one 12-triangle liquid volume; excludes pipes and the surrounding scene. Combined surface meshes are shared by configuration; renderer count is not a measured draw-call count.");
            Check(glassRenderers>0&&world.Get(origin.Offset(1,1,1))==BlockId.Air,"Visible contained liquid leaves authoritative interior air cells untouched");
            player.Camera.transform.position=world.Local(origin)+new Vector3(3.0f,2.8f,-5.0f);player.Camera.transform.LookAt(world.Local(origin)+new Vector3(2.5f,1.8f,1));
            yield return Capture("multiblock-controls-and-pipe-additions");
            void Aim(MachineState m)
            {player.transform.position=world.Local(m.Position)+new Vector3(.5f,0,-2.2f);player.Camera.transform.position=world.Local(m.Position)+new Vector3(.5f,.6f,-1.3f);player.Camera.transform.LookAt(world.Local(m.Position)+Vector3.one*.5f);}
            var upgrade=Place(8,1,-2,IndustryId.ItemPipe);
            Aim(upgrade);Check(game.TryOpenMachine(upgrade.Position),"Open ordinary item pipe through normal targeting");
            game.Inventory.Add(IndustryId.SignalConduit,1);game.Inventory.Add(IndustryId.PowerCable,1);
            Check(game.Industry.AddPipeChannel(upgrade,PipeAddition.Signal)&&game.Industry.AddPipeChannel(upgrade,PipeAddition.Power)&&upgrade.Additions==(PipeAddition.Signal|PipeAddition.Power),"Install both independent channels through the real pipe interface authority");
            Check(game.Inventory.Total(IndustryId.SignalConduit)==0&&game.Inventory.Total(IndustryId.PowerCable)==0&&!game.Industry.AddPipeChannel(upgrade,PipeAddition.Signal),"Pipe fittings consume components once and reject duplicate installation");
            game.SetMode(ScreenMode.Play);Aim(upgrade);Check(game.TryOpenMachine(upgrade.Position),"Reopen fitted pipe controls");yield return Capture("multiblock-pipe-channel-ui");game.SetMode(ScreenMode.Play);
            // Stop transport for exact bucket/full-inventory and breach checks.
            sim.Activate(lever);outputPort.PortMode=FluidPortMode.Disabled;sim.Invalidate();yield return new WaitForSecondsRealtime(.2f);
            Aim(controller);Check(game.TryOpenMachine(controller.Position),"Controller opens through ordinary target interaction");
            game.Inventory.Add(Fluids.WaterBucket,1);
            for(int i=0;i<game.Inventory.Count;i++)if(game.Inventory.Slots[i].Empty)game.Inventory.Add(BlockId.Stone,64,i,i+1);
            long before=tank.Fluid.Amount;
            Check(game.Industry.Bucket(controller,true)&&tank.Fluid.Amount==before+10000,"Full inventory swaps filled bucket transactionally");
            Check(game.Industry.Bucket(controller,false)&&tank.Fluid.Amount==before,"Full inventory withdrawal returns exact 10 L bucket");
            yield return Capture("multiblock-controller-ui");game.SetMode(ScreenMode.Play);
            var breach=origin.Offset(0,2,2);long held=tank.Fluid.Amount;var identity=tank.StructureId;
            Check(world.Mine(breach,IndustryId.TankGlass,ToolCapability.Pickaxe),"Mine an individual glass member through ordinary mining authority");
            Check(!tank.Formed&&tank.Fluid.Amount==held,"Mining breaches immediately and preserves stored amount");yield return new WaitForSecondsRealtime(.2f);
            Check(!tank.Formed&&tank.Fluid.Amount==held&&!world.Remove(controller.Position,IndustryId.TankController),"Invalid tank pauses ports and rejects nonempty controller dismantle");
            Aim(controller);Check(game.TryOpenMachine(controller.Position),"Breached controller remains usable for emergency recovery");yield return Capture("multiblock-breach-diagnostics");game.SetMode(ScreenMode.Play);
            Check(world.Place(breach,IndustryId.TankGlass),"Replace mined glass member");yield return new WaitForSecondsRealtime(.25f);
            Check(tank.Formed&&tank.StructureId==identity&&tank.Fluid.Amount==held,"Repair restores same logical tank and liquid");
            sampling=true;yield return new WaitForSecondsRealtime(6);sampling=false;
            game.SetMode(ScreenMode.Pause);var saved=WorldPoint.FromLocal(player.transform.position,world.Origin);
            player.transform.position+=Vector3.right*640;yield return null;yield return Settle(120);sim.Step();
            Check(!world.Ready(controller.Position)&&!tank.Formed&&tank.Fluid.Amount==held,"Unloaded cross-chunk tank retains exact contents and pauses");
            player.transform.position=saved.Local(world.Origin);yield return null;yield return Settle(120);
            for(int i=0;i<20;i++)sim.Step();
            Check(tank.Formed&&tank.StructureId==identity&&tank.Fluid.Amount==held,"Chunk reload and floating-origin round trip restore one tank");
            game.SetMode(ScreenMode.Play);
            player.Camera.transform.position=world.Local(origin)+new Vector3(-7,5,-7);player.Camera.transform.LookAt(world.Local(origin)+new Vector3(2,1.5f,2));
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
            yield return Capture("multiblock-repaired-side-window");
            Check(errors.Count==0,"Multiblock review completes without Unity errors");
        }
    }
}
