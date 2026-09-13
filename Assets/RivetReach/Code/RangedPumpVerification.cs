using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewRangedPump()
        {
            FreezeSaveFixture();game.SetCreative(true);game.enabled=false;
            var world=game.World;var sim=game.Industry.Simulation;var player=game.Player;
            var p=new BlockPos(16,100,16);player.transform.position=world.Local(p)+new Vector3(.5f,1,-3);yield return Settle(150);
            void Put(BlockPos at,byte id)
            {byte old=world.Get(at);if(old!=0)Check(world.Remove(at,old),"Clear fixture cell");if(id!=0)Check(world.Place(at,id),"Place fixture "+id);}
            // Raised, contained source pool and dry machine deck expose the finite drain visibly.
            for(int x=-5;x<=3;x++)for(int z=-2;z<=4;z++)Put(p.Offset(x,-2,z),BlockId.Stone);
            for(int x=-5;x<=3;x++)for(int z=-2;z<=4;z++)if(x>=0||x==-5||z==-2||z==4)Put(p.Offset(x,-1,z),BlockId.Stone);
            Put(p,IndustryId.RangedPump);var m=sim.At(p);
            var sources=new[]{p.Offset(-2,-1,1),p.Offset(-3,-1,1),p.Offset(-2,-1,2),p.Offset(-3,-1,2)};
            foreach(var source in sources)Check(world.ChangeFluid(source,0,Fluids.Lava.Source),"Place real finite lava source");
            sim.Step(); // Publish resident machines to the presentation list.
            void Camera()
            {
                player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);player.HeldBlock.enabled=false;
                player.transform.position=world.Local(p)+new Vector3(.5f,0,-2);
                player.Camera.transform.position=world.Local(p)+new Vector3(2.8f,2.6f,-4.2f);
                player.Camera.transform.LookAt(world.Local(p)+new Vector3(-.5f,.1f,.6f));game.Sky.Clock.SetTime(.35);game.Sky.Apply();game.Notify("",0);
            }
            game.SetMode(ScreenMode.Play);Camera();yield return new WaitForSecondsRealtime(.5f);
            var view=Object.FindAnyObjectByType<IndustryPresentation>().ViewAt(p);
            Check(view!=null&&view.GetComponentsInChildren<MeshFilter>().Length==3,"Actual imported ranged pump is rendered");
            File.WriteAllText(Path.Combine(output,"model.txt"),"Imported triangles: "+view.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)+"; renderers: "+view.GetComponentsInChildren<Renderer>().Length);
            yield return Capture("ranged-pump-lava-pool");
            player.Camera.transform.position=world.Local(p)+new Vector3(1.8f,1.2f,-1.8f);player.Camera.transform.LookAt(world.Local(p)+new Vector3(.5f,.45f,.5f));yield return Capture("ranged-pump-model");
            for(int i=0;i<25&&m.Work==0;i++)sim.Step();Check(m.Work==1,"Bounded scan discovers a remote source");
            for(int i=0;i<16;i++)sim.Step();Check(m.Work==17&&m.Fluid.Amount==0,"Collection retains partial eligible work");
            game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"Saves"));Check(game.SaveGame("Ranged pump partial collection"),"Save partial collection");
            Check(game.LoadGame(game.Saves.List().First(e=>!e.Backup)),"Load actual world checkpoint");FreezeSaveFixture();game.enabled=false;
            world=game.World;sim=game.Industry.Simulation;player=game.Player;yield return Settle(120);game.enabled=true;yield return null;game.enabled=false;Check(!game.LoadingSave,"Finish loading in paused mode before UI interaction");m=sim.At(p);
            Check(m.Work==17&&sources.All(s=>world.Get(s)==Fluids.Lava.Source),"Reload preserves work and all undrained sources");
            for(int i=0;i<65&&m.Fluid.Amount==0;i++)sim.Step();
            Check(m.Fluid.Amount==10000&&m.Fluid.Fluid==Fluids.Lava&&sources.Count(s=>world.Get(s)==Fluids.Lava.Source)==3,"First extraction removes one source for exactly 10 L of lava");
            for(int i=0;i<80;i++)sim.Step();Check(m.Fluid.Amount==10000&&sources.Count(s=>world.Get(s)==Fluids.Lava.Source)==3,"Full pump stops consuming sources");
            game.SetCreative(true);game.SetMode(ScreenMode.Play);Camera();player.Camera.transform.position=world.Local(p)+new Vector3(2.4f,2.1f,-3.2f);player.Camera.transform.LookAt(world.Local(p)+Vector3.one*.5f);Check(game.TryOpenMachine(p),"Right-click/Interact machine target opens controls");yield return new WaitForSecondsRealtime(.4f);
            Check(game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("Lava: 10 / 10 L")),"Machine interface names lava and exact buffer volume");yield return Capture("ranged-pump-lava-buffer");
            game.SetMode(ScreenMode.Pause);Check(game.SaveGame("Ranged pump lava buffer",true),"Save typed lava buffer");
            Check(game.LoadGame(game.Saves.List().First(e=>!e.Backup)),"Reload typed lava checkpoint");FreezeSaveFixture();game.enabled=false;world=game.World;sim=game.Industry.Simulation;player=game.Player;yield return Settle(120);game.enabled=true;yield return null;game.enabled=false;Check(!game.LoadingSave,"Finish loading in paused mode before UI interaction");m=sim.At(p);
            Check(m.Fluid.Fluid==Fluids.Lava&&m.Fluid.Amount==10000,"Actual world load preserves exact lava buffer");
            Put(p.Offset(1,0,0),IndustryId.FluidPipe);Put(p.Offset(2,0,0),IndustryId.Tank);var tank=sim.At(p.Offset(2,0,0));
            for(int i=0;i<800&&tank.Fluid.Amount<40000;i++)sim.Step();
            Check(sources.All(s=>world.Get(s)==0)&&m.Fluid.Amount==0&&tank.Fluid.Fluid==Fluids.Lava&&tank.Fluid.Amount==40000,"Actual fluid pipes drain four finite sources into exactly 40 L of stored lava; tank="+tank.Fluid.Amount+", pump="+m.Fluid.Amount+", sources="+sources.Count(s=>world.Get(s)==Fluids.Lava.Source)+", status="+m.Status);
            game.SetCreative(true);game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(IndustryId.Wrench,1,0,1);game.Selected=0;
            game.SetMode(ScreenMode.Play);Camera();yield return new WaitForSecondsRealtime(.5f);yield return Capture("ranged-pump-drained-pool");
            player.Camera.transform.position=world.Local(p)+new Vector3(2.5f,1.8f,-2);player.Camera.transform.LookAt(world.Local(p.Offset(2,0,0))+Vector3.one*.5f);
            Check(game.TryOpenMachine(p.Offset(2,0,0)),"Open destination tank");yield return new WaitForSecondsRealtime(.4f);
            Check(game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("Lava: 40 / 100 L")),"Destination panel shows all collected lava");yield return Capture("ranged-pump-destination");
            game.SetMode(ScreenMode.Pause);Check(world.Mine(p,IndustryId.RangedPump,ToolCapability.Pickaxe),"Mine drained ranged pump");
            Check(game.Items.Piles.Any(d=>d.Stack.Id==IndustryId.RangedPump)&&sim.At(p)==null,"Mining recovers one ranged pump item and removes authority");game.enabled=true;
        }
    }
}
