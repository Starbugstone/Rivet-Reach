using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        sealed class StressLine
        {
            public MachineState Crusher,Furnace,Boiler,Alternator,Cooker,ElectricCooker,Composter,Pump,Ranged,Drill;
            public CrateStorage Input,Output;
            public long IronUnits=>2L*Input.Count+2L*Crusher.Items.Total(BlockId.RawIron)+Crusher.Items.Total(IndustryId.CrushedIron)
                +Furnace.Items.Total(IndustryId.CrushedIron)+Furnace.Items.Total(BlockId.IronIngot)+Output.Count;
        }

        IEnumerator ReviewStressFactory(Func<string,int,IEnumerator> sample)
        {
            // Build once, then test increasing simultaneous work without reducing view
            // distance or changing any production rates. Fixture construction is excluded.
            game.StartSession(246813);game.SetCreative(true);game.World.ViewDistance=10;
            game.Mobs.NaturalSpawning=false;game.Animals.NaturalSpawning=false;
            var world=game.World;var player=game.Player;var sim=game.Industry.Simulation;
            player.enabled=false;player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
            var origin=new BlockPos(0,80,0);player.transform.position=world.Local(origin)+new Vector3(20,12,24);
            game.SetMode(ScreenMode.Pause);yield return Settle(180);
            int placed=0;
            void Place(BlockPos p,byte id)
            {
                if(!world.Ready(p))throw new InvalidOperationException("Stress fixture requires resident cell "+p);
                byte old=world.Get(p);if(old==id)return;
                if(old!=0&&!world.Remove(p,old))throw new InvalidOperationException("Cannot clear stress cell "+p);
                if(!world.Place(p,id))throw new InvalidOperationException("Cannot place stress block "+id+" at "+p);placed++;
            }
            MachineState Machine(BlockPos p,byte id){Place(p,id);return sim.At(p);}
            MachineState Pipe(BlockPos p,params (int face,PortRole role)[] ends)
            {
                var m=Machine(p,IndustryId.ItemPipe);
                foreach(var e in ends)m.PipeDirections=(m.PipeDirections&~(3<<(2*e.face)))|((e.role==PortRole.Input?1:2)<<(2*e.face));
                return m;
            }
            // A raised, supported floor keeps the generator and ore fixtures independent.
            for(int z=-2;z<74;z++)
            {
                for(int x=-2;x<34;x++)Place(origin.Offset(x,-1,z),BlockId.Stone);
                if(z%2==0)yield return null;
            }
            var lines=new List<StressLine>();var generators=new List<MachineState>();var batteries=new List<MachineState>();
            for(int n=0;n<32;n++)
            {
                var p=origin.Offset(n%4*8,0,n/4*9);var line=new StressLine();lines.Add(line);
                for(int x=-1;x<8;x++)Machine(p.Offset(x,0,-1),IndustryId.PowerCable);
                for(int z=0;z<8;z++){Machine(p.Offset(-1,0,z),IndustryId.PowerCable);Machine(p.Offset(7,0,z),IndustryId.PowerCable);}
                line.Boiler=Machine(p,IndustryId.Boiler);line.Boiler.Items.Add(BlockId.Charcoal,64,0,1);line.Boiler.WaterMl=100000;
                line.Alternator=Machine(p.Offset(1,0,0),IndustryId.Alternator);
                line.Crusher=Machine(p.Offset(3,0,0),IndustryId.Crusher);line.Furnace=Machine(p.Offset(5,0,0),IndustryId.ElectricFurnace);
                Machine(p.Offset(6,0,0),IndustryId.Lamp);
                Pipe(p.Offset(3,0,1),(4,PortRole.Output),(5,PortRole.Input));Place(p.Offset(3,0,2),CrateId.Crate);
                line.Input=game.Survival.At(p.Offset(3,0,2)).Crate;line.Input.Insert(new ItemStack(BlockId.RawIron,8192));
                Pipe(p.Offset(4,0,0),(1,PortRole.Output),(0,PortRole.Input));
                Pipe(p.Offset(5,0,1),(5,PortRole.Output),(4,PortRole.Input));Place(p.Offset(5,0,2),CrateId.Crate);
                line.Output=game.Survival.At(p.Offset(5,0,2)).Crate;line.Output.Lock(BlockId.IronIngot);
                Place(p.Offset(6,0,2),CrateId.Controller);
                line.Pump=Machine(p.Offset(0,0,4),IndustryId.Pump);line.Ranged=Machine(p.Offset(0,0,6),IndustryId.RangedPump);
                foreach(int z in new[]{4,5})
                {
                    Place(p.Offset(0,-2,z),BlockId.Stone);world.Remove(p.Offset(0,-1,z),BlockId.Stone);
                    Check(world.ChangeFluid(p.Offset(0,-1,z),0,Fluids.Water.Source),"Supported stress water source");
                }
                foreach(int z in new[]{4,6}){Machine(p.Offset(1,0,z),IndustryId.FluidPipe);Machine(p.Offset(2,0,z),IndustryId.Tank);}
                line.Cooker=Machine(p.Offset(4,0,5),FarmId.Cooker);line.Cooker.SelectCooking("rivet:cook_baked_potato");line.Cooker.Items.Add(BlockId.Potato,64,0,1);line.Cooker.Items.Add(BlockId.Coal,64,3,4);
                line.ElectricCooker=Machine(p.Offset(6,0,5),FarmId.ElectricCooker);line.ElectricCooker.SelectCooking("rivet:cook_baked_potato");line.ElectricCooker.Items.Add(BlockId.Potato,64,0,1);
                foreach(int x in new[]{4,6}){Pipe(p.Offset(x,0,6),(5,PortRole.Output),(4,PortRole.Input));Place(p.Offset(x,0,7),CrateId.Crate);}
                line.Composter=Machine(p.Offset(4,0,3),CompostId.Auto);Machine(p.Offset(4,0,4),IndustryId.PowerCable);
                Machine(p.Offset(5,0,4),IndustryId.PowerCable);var battery=Machine(p.Offset(6,0,4),IndustryId.Battery);batteries.Add(battery);
                Pipe(p.Offset(3,0,3),(5,PortRole.Output),(0,PortRole.Input)); // Organic supply is separate from the ore line.
                Place(p.Offset(2,0,3),BlockId.Chest);game.Survival.At(p.Offset(2,0,3)).Storage.Add(BlockId.Leaves,27*64);
                var organicPipe=sim.At(p.Offset(3,0,3));organicPipe.PipeDirections=2<<(2*1)|1;
                line.Drill=Machine(p.Offset(0,0,7),IndustryId.Drill);
                Place(p.Offset(2,0,7),BlockId.Furnace);var furnace=game.Survival.At(p.Offset(2,0,7)).Furnace;
                var cargo=new ItemStack(BlockId.RawCopper,64);furnace.Click(0,ref cargo,false);cargo=new ItemStack(BlockId.Coal,64);furnace.Click(1,ref cargo,false);game.Survival.Wake(p.Offset(2,0,7));
                generators.Add(Machine(p.Offset(2,0,0),n%2==0?IndustryId.SolarPanel:IndustryId.WindTurbine));
                File.WriteAllText(Path.Combine(output,"stress-progress.txt"),$"Constructed {n+1}/32 production lines; {placed} placed blocks.");
                yield return null;
            }
            // Shared-storage/control annex: formed shells, bank, manual generator,
            // signal consumers and named remote routes all use their live authorities.
            var annex=origin.Offset(0,0,76);
            for(int z=0;z<8;z++){for(int x=0;x<32;x++)Place(annex.Offset(x,-1,z),BlockId.Stone);yield return null;}
            var bounds=new StructureBounds(annex,annex.Offset(4,4,4));
            for(int x=0;x<5;x++)for(int y=0;y<5;y++)for(int z=0;z<5;z++)
            {int axes=bounds.BoundaryAxes(annex.Offset(x,y,z));if(axes>0)Machine(annex.Offset(x,y,z),axes>=2?IndustryId.TankFrame:y==0||y==4?IndustryId.TankWall:IndustryId.TankGlass);}
            var tankController=Machine(annex.Offset(1,1,0),IndustryId.TankController);
            Machine(annex.Offset(2,1,0),IndustryId.TankHatch);Machine(annex.Offset(3,1,0),IndustryId.TankValve);
            var levelSensor=Machine(annex.Offset(1,2,0),IndustryId.TankSensor);Machine(annex.Offset(2,2,0),IndustryId.TankPort);
            Machine(annex.Offset(1,2,-1),IndustryId.Indicator);
            var bank=Machine(annex.Offset(7,0,0),IndustryId.BatteryController);
            var bankCell=Machine(annex.Offset(7,0,1),IndustryId.Battery);batteries.Add(bankCell);
            var crank=Machine(annex.Offset(8,0,1),IndustryId.HandCrank);
            for(int z=-3;z<0;z++)Machine(annex.Offset(7,0,z),IndustryId.PowerCable);
            var lever=Machine(annex.Offset(10,0,0),IndustryId.Lever);sim.Activate(lever);
            Machine(annex.Offset(11,0,0),IndustryId.SignalWire);Machine(annex.Offset(12,0,0),IndustryId.SignalConduit);
            Machine(annex.Offset(13,0,0),IndustryId.Relay);Machine(annex.Offset(14,0,0),IndustryId.Door);
            Machine(annex.Offset(10,0,2),IndustryId.Button);Machine(annex.Offset(12,0,2),IndustryId.Sensor);
            Machine(annex.Offset(15,0,0),IndustryId.WoodenDoor);
            Place(annex.Offset(16,0,0),BlockId.Workbench);Machine(annex.Offset(17,0,0),IndustryId.Bench);
            var bin=Machine(annex.Offset(18,0,0),CompostId.Bin);bin.AddCompost(BlockId.Leaves,64);
            Place(annex.Offset(20,0,0),BlockId.Chest);game.Survival.At(annex.Offset(20,0,0)).Storage.Add(BlockId.Stone,64);
            Machine(annex.Offset(21,0,0),IndustryId.Extractor);Pipe(annex.Offset(22,0,0));Place(annex.Offset(23,0,0),BlockId.Chest);
            foreach(byte id in new[]{IndustryId.ItemBridge,IndustryId.FluidBridge,IndustryId.PowerBridge})
            {
                int x=24+(id-IndustryId.ItemBridge)*2,z=id==IndustryId.ItemBridge?2:0;var first=Machine(annex.Offset(x,0,z),id);var second=Machine(annex.Offset(x,0,z+4),id);
                Check(sim.ConfigureBridge(first,first.OwnerId,"stress-"+id,out _)&&sim.ConfigureBridge(second,second.OwnerId,"stress-"+id,out _),"Pair stress bridge channel");
            }
            Machine(annex.Offset(30,0,0),IndustryId.ChunkLoader);
            // Exercise each remote channel with actual supply and demand.
            for(int z=-3;z<0;z++)for(int x=24;x<=28;x++)Place(annex.Offset(x,-1,z),BlockId.Stone);
            Place(annex.Offset(24,0,0),BlockId.Chest);var bridgeSupply=game.Survival.At(annex.Offset(24,0,0)).Storage;bridgeSupply.Add(BlockId.Stone,256);
            Pipe(annex.Offset(24,0,1),(5,PortRole.Output));Pipe(annex.Offset(24,0,7),(4,PortRole.Input));
            Place(annex.Offset(24,-1,8),BlockId.Stone);Place(annex.Offset(24,0,8),BlockId.Chest);var bridgeOutput=game.Survival.At(annex.Offset(24,0,8)).Storage;
            var liquidSupply=Machine(annex.Offset(26,0,-2),IndustryId.Tank);liquidSupply.Fluid.Deposit(Fluids.Water,100000);
            var liquidPipe=Machine(annex.Offset(26,0,-1),IndustryId.FluidPipe);liquidPipe.PipeDirections=2<<(5*2);
            Machine(annex.Offset(26,0,5),IndustryId.FluidPipe);var liquidOutput=Machine(annex.Offset(26,0,6),IndustryId.Tank);
            Machine(annex.Offset(28,0,-2),IndustryId.WindTurbine);Machine(annex.Offset(28,0,-1),IndustryId.PowerCable);
            Machine(annex.Offset(28,0,5),IndustryId.PowerCable);var remoteLamp=Machine(annex.Offset(28,0,6),IndustryId.Lamp);
            // Farm: 512 plants using every cultivable crop, with mixed initial stages.
            var crops=CropRules.Definitions.Where(c=>c.planting!=0).ToArray();int cropCount=0;var growing=new List<BlockPos>();
            for(int z=0;z<32;z++)
            {
                for(int x=38;x<54;x++)
                {
                    var p=origin.Offset(x,0,z);Place(p.Offset(0,-1,0),BlockId.Dirt);Check(world.Till(p.Offset(0,-1,0)),"Till stress farm");
                    var crop=crops[(x+z)%crops.Length];Check(world.Plant(p,crop.planting),"Plant stress farm");
                    if((x+z)%2==0)while(world.Get(p)<crop.Mature)Check(world.Grow(p,world.Get(p)),"Mature half of stress crops");else growing.Add(p);cropCount++;
                }
                yield return null;
            }
            for(int z=36;z<54;z++){for(int x=38;x<56;x++)Place(origin.Offset(x,-1,z),BlockId.Grass);yield return null;}
            long immediateBeforeGrowth=world.ImmediateMeshBuilds;
            foreach(var p in growing.Take(16)){byte stage=world.Get(p);Check(world.Grow(p,stage)&&world.Get(p)==stage+1,"Crop growth publishes authoritative stage immediately");}
            Check(world.ImmediateMeshBuilds==immediateBeforeGrowth&&world.PendingCount>0,"Mass crop growth coalesces queued meshes without any synchronous terrain rebuild");
            player.transform.position=world.Local(origin)+new Vector3(27,8,32);player.Camera.transform.LookAt(world.Local(origin)+new Vector3(20,0,40));
            game.Sky.Clock.SetTime(.9);game.Sky.Apply();game.SetMode(ScreenMode.Play);player.Arms.gameObject.SetActive(false);
            sim.Invalidate();yield return Settle(180);
            float deadline=Time.realtimeSinceStartup+180;
            while((sim.Rebuilding||world.PendingLightChunks>0)&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(!sim.Rebuilding,"Large factory topology completes");
            Check(tankController.Structure?.Formed==true&&bank.Structure?.Formed==true,"Stress tank and battery bank form through shared validation");
            Check(tankController.Structure.Fluid.Deposit(Fluids.Water,100000),"Stress tank stores shared liquid");
            Check(sim.TryCrank(crank),"Hand crank starts a paid generation stroke in the stress bank");
            for(int i=0;i<64;i++)Check(game.Animals.Spawn(world.Local(origin)+new Vector3(38.5f+i%8*2,.02f,36.5f+i/8*2),i%4==0)!=null,"Spawn persistent stress chicken");
            var surfaceMobs=game.Mobs.Definitions.Where(d=>d.spawnRules.habitat!=MobHabitat.Underground).ToArray();int mobCount=0;
            for(int z=57;z<70;z++){for(int x=38;x<62;x++)Place(origin.Offset(x,-1,z),BlockId.Grass);yield return null;}
            deadline=Time.realtimeSinceStartup+90;while(world.PendingLightChunks>0&&Time.realtimeSinceStartup<deadline)yield return null;
            foreach(var definition in surfaceMobs)
            for(int i=0;i<definition.population&&mobCount<MobSystem.MaximumPopulation;i++)
            {
                var position=world.Local(origin)+new Vector3(39.5f+mobCount%7*3.2f,.02f,59.5f+mobCount/7*4);
                Check(game.Mobs.Spawn(definition,position)!=null,"Place separated stress mob: "+game.Mobs.LastSpawnRejection);mobCount++;
            }
            Check(mobCount==MobSystem.MaximumPopulation,"Stress encounter reaches the ordinary ambient hostile cap");
            game.Sky.Clock.SetTime(.5);game.Sky.Apply();
            long[] iron=lines.Select(l=>l.IronUnits).ToArray();long outputs=lines.Sum(l=>(long)l.Output.Count);
            Check(lines.All(l=>l.Alternator.SupplyWatts==800),"Every stress boiler/alternator produces its authored 800 W");
            yield return sample("stress-factory-farm-mobs-clear",1800);
            var materialViews=FindAnyObjectByType<IndustryPresentation>();
            Check(materialViews.MaterialVariantCount==7&&sim.Machines.Values.Select(m=>materialViews.ViewAt(m.Position)).Where(root=>root!=null)
                .SelectMany(root=>root.GetComponentsInChildren<Renderer>(true)).All(renderer=>!renderer.HasPropertyBlock()),
                "Real machine views and fittings share seven session variants without per-renderer material overrides");
            Check(lines.Sum(l=>(long)l.Output.Count)>outputs,"Stress factory delivers actual refined ingots into physical crates");
            for(int i=0;i<lines.Count;i++)Check(lines[i].IronUnits==iron[i],"Large factory conserves exact ore-equivalent units on line "+i);
            Check(bridgeOutput.Total(BlockId.Stone)>0&&bridgeOutput.Total(BlockId.Stone)+bridgeSupply.Total(BlockId.Stone)==256,"Stress item bridges transfer with exact conservation; source="+bridgeSupply.Total(BlockId.Stone)+", destination="+bridgeOutput.Total(BlockId.Stone));
            Check(liquidOutput.Fluid.Amount>0&&liquidOutput.Fluid.Amount+liquidSupply.Fluid.Amount==100000,"Stress liquid bridges transfer with exact conservation");
            Check(remoteLamp.ReceivedWatts>0,"Stress electrical bridges supply a remote load");
            game.Diagnostics=true;yield return sample("stress-diagnostics",600);game.Diagnostics=false;
            bool isolation=Environment.GetCommandLineArgs().Contains("-rr-factory-isolation");
            if(isolation){yield return sample("isolation-live-settled",(Environment.GetCommandLineArgs().Contains("-rr-factory-cpu-only")||Environment.GetCommandLineArgs().Contains("-rr-factory-shader"))?-45:600);yield return ReviewFactoryIsolation(sample);}
            else yield return sample("stress-five-minute-soak",-300);
            Check(game.World.Error==null,isolation?"Isolation scenario has no worker failure":"Five-minute combined simulation has no worker failure");
            for(int i=0;i<lines.Count;i++)Check(lines[i].IronUnits==iron[i],"Soaked factory conserves line "+i);
            yield return StressBlogCapture("blog-01-factory-overview",origin,new Vector3(42,26,35),new Vector3(16,0,35),.5,WeatherKind.Clear);
            yield return StressBlogCapture("blog-02-dusk-production",origin,new Vector3(-5,10,4),new Vector3(15,0,30),.76,WeatherKind.Clear);
            yield return StressBlogCapture("blog-03-storm-industry",origin,new Vector3(38,12,75),new Vector3(13,1,25),.72,WeatherKind.Storm);
            yield return StressBlogCapture("blog-04-farm-and-power",origin,new Vector3(63,15,4),new Vector3(28,0,28),.5,WeatherKind.Clear);
            player.transform.position=world.Local(origin)+new Vector3(27,8,32);player.Camera.transform.LookAt(world.Local(origin)+new Vector3(20,0,40));
            game.Sky.Clock.SetTime(.5);game.Weather.SetWeather(WeatherKind.Storm,true);yield return sample("stress-factory-farm-mobs-storm",1800);
            game.SetMode(ScreenMode.Inventory);yield return sample("stress-inventory",600);game.SetMode(ScreenMode.Play);
            var factoryEye=player.transform.position;
            var machineViews=FindAnyObjectByType<IndustryPresentation>();var crateViews=FindAnyObjectByType<CratePresentation>();
            long machinesReused=machineViews.ReusedViews,cratesReused=crateViews.ReusedViews;
            // Cross the presentation boundary briefly before the longer residency trip.
            player.transform.position=factoryEye+Vector3.right*68;yield return new WaitForSecondsRealtime(.6f);
            player.transform.position=factoryEye;yield return new WaitForSecondsRealtime(.6f);
            Check(machineViews.ReusedViews>machinesReused&&crateViews.ReusedViews>cratesReused,"Returning across view range reuses exact machine and crate views");
            float travelStart=0;bool observedDormant=false,checkedDormant=false,loaderPoweredWhileAway=false;long dormantIron=0;double dormantWork=0;
            IEnumerator TravelFactory()
            {
                travelStart=Time.realtimeSinceStartup;
                while(Time.realtimeSinceStartup-travelStart<60)
                {
                    float elapsed=Time.realtimeSinceStartup-travelStart;
                    float distance=elapsed<14?elapsed*30:elapsed<22?420:elapsed<36?420-(elapsed-22)*30:0;
                    player.transform.position=factoryEye+Vector3.right*distance;
                    if(!lines[0].Crusher.Eligible)
                    {
                        if(remoteLamp.Eligible&&remoteLamp.ReceivedWatts>0)loaderPoweredWhileAway=true;
                        if(!observedDormant){observedDormant=true;dormantIron=lines[0].IronUnits;dormantWork=lines[0].Crusher.Work;}
                        else {if(lines[0].IronUnits!=dormantIron||lines[0].Crusher.Work!=dormantWork)throw new Exception("Dormant factory advanced or lost cargo");checkedDormant=true;}
                    }
                    yield return null;
                }
                player.transform.position=factoryEye;
            }
            var journey=StartCoroutine(TravelFactory());yield return sample("active-factory-unload-return",-60);yield return journey;
            yield return Settle(180);Check(observedDormant&&checkedDormant&&lines[0].Crusher.Eligible,"Working factory unloads and resumes after return");
            Check(lines[0].IronUnits==iron[0],"Streaming factory preserves exact production resources");
            Check(loaderPoweredWhileAway,"Explicit loader keeps remote renewable generation and bridge load operating while unticketed factory sleeps");
            Check(machineViews.PeakCreatedPerFrame<=IndustryPresentation.CreateBudgetPerFrame&&crateViews.PeakCreatedPerFrame<=CratePresentation.CreateBudgetPerFrame,"Cold view creation obeys per-frame operation caps");
            Check(machineViews.CachedViewCount<=IndustryPresentation.InactiveViewCapacity&&crateViews.CachedViewCount<=CratePresentation.InactiveViewCapacity,"Inactive presentation caches stay within their caps");
            float cleanupDeadline=Time.realtimeSinceStartup+30;
            while((machineViews.PendingDestroyCount>0||crateViews.PendingDestroyCount>0)&&Time.realtimeSinceStartup<cleanupDeadline)yield return null;
            Check(machineViews.PendingDestroyCount==0&&crateViews.PendingDestroyCount==0,"Deferred presentation teardown drains after the player returns");
            File.WriteAllText(Path.Combine(output,"view-reuse.txt"),$"Machines: created {machineViews.CreatedViews}, reused {machineViews.ReusedViews}, destroyed {machineViews.DestroyedViews}, inactive {machineViews.CachedViewCount}, peak retirement {machineViews.PeakPendingDestroyCount}.\nCrates: created {crateViews.CreatedViews}, reused {crateViews.ReusedViews}, destroyed {crateViews.DestroyedViews}, inactive {crateViews.CachedViewCount}, peak retirement {crateViews.PeakPendingDestroyCount}.\n");
            // Fixed workload deliberately blocks processing, then removes production.
            foreach(var line in lines){line.Crusher.Items.Add(IndustryId.CrushedIron,64,2,3);line.Furnace.Items.Add(BlockId.IronIngot,64,2,3);line.Output.Insert(new ItemStack(BlockId.IronIngot,CrateStorage.Capacity));}
            yield return sample("stress-full-output",600);
            foreach(var line in lines){line.Boiler.WaterMl=0;line.Boiler.Items.Take(0,int.MaxValue);}
            foreach(var m in generators){m.SignalAttached=true;m.Signal=false;}
            foreach(var m in batteries)m.EnergyCells[0].Discharge(m.EnergyCells[0].Amount);
            yield return sample("stress-power-shortage",600);
            File.WriteAllText(Path.Combine(output,"stress-workload.txt"),$"Seed 246813; view radius 10; 32 production lines; {placed} placed blocks; {sim.Machines.Count} industrial assemblies; {game.Survival.StationCount} stations/crates; {cropCount} crop cells; {game.Animals.Animals.Count} persistent chickens; {mobCount} explicitly placed hostile mobs.\nAll setup resources are fixtures. Ordinary simulation, storage, renderers and navigation run during samples.\n");
            game.Weather.SetWeather(WeatherKind.Clear,true);
        }
        IEnumerator StressBlogCapture(string name,BlockPos origin,Vector3 eye,Vector3 target,double hour,WeatherKind weather)
        {
            game.Player.transform.position=game.World.Local(origin)+eye;
            game.Player.Camera.transform.LookAt(game.World.Local(origin)+target);
            game.Sky.Clock.SetTime(hour);game.Sky.Apply();game.Weather.SetWeather(weather,true);
            yield return new WaitForSecondsRealtime(2);
            var machines=FindAnyObjectByType<IndustryPresentation>();var crates=FindAnyObjectByType<CratePresentation>();
            float deadline=Time.realtimeSinceStartup+30;
            while((machines.PendingCreateCount>0||crates.PendingCreateCount>0)&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(machines.PendingCreateCount==0&&crates.PendingCreateCount==0,"Blog camera finishes loading visible assemblies: "+name);
            yield return AlphaSceneCapture(name);
        }
    }
}
