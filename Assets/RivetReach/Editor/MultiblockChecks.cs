using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RivetReach.Editor
{
    public static class MultiblockChecks
    {
        sealed class World : IIndustryWorld
        {
            public readonly Dictionary<BlockPos,byte> Cells=new Dictionary<BlockPos,byte>();
            public readonly HashSet<BlockPos> Sleeping=new HashSet<BlockPos>();
            public bool Ready(BlockPos p)=>!Sleeping.Contains(p);
            public byte Get(BlockPos p)=>Cells.TryGetValue(p,out byte b)?b:(byte)0;
            public bool Remove(BlockPos p,byte expected){if(Get(p)!=expected)return false;Cells.Remove(p);return true;}
            public ItemContainer Storage(BlockPos p)=>null;public byte Drop(byte b)=>b;public bool PlayerInside(BlockPos p)=>false;
        }
        sealed class DryMachine : IMultiblockMachineData
        {
            public bool CanDismantle=>true;
            public string TryForm(MultiblockValidation validation)=>null;
        }
        sealed class FixedValidator : IMultiblockValidator
        {
            public MultiblockValidation Validate(MultiblockDefinition d,MachineState c,IIndustryWorld w,Func<BlockPos,MachineState> machine,Func<BlockPos,bool> claimed)
            {
                var result=new MultiblockValidation{Valid=true,Message="FORMED",Bounds=new StructureBounds(c.Position,c.Position)};
                result.Members.Add(c.Position,MultiblockRole.Controller);return result;
            }
        }
        public static void Run()
        {
            var log=new StringBuilder();int assertions=0;
            void Check(bool valid,string message){if(!valid)throw new Exception("Multiblock: "+message);assertions++;log.AppendLine("PASS "+message);}
            var world=new World();var sim=new IndustrySimulation(world,id=>64);
            void Settle(){for(int i=0;i<100;i++){sim.Step();if(!sim.Rebuilding&&sim.Multiblocks.PendingCount==0)return;}throw new Exception("Multiblock topology failed to settle");}
            void Set(BlockPos p,byte id,int rotation=0)
            {if(sim.At(p)!=null)sim.Remove(p);world.Cells[p]=id;if(IndustryId.Placed(id)){var m=sim.Add(p,id);for(int i=0;i<rotation;i++)sim.Rotate(m);}sim.Multiblocks.Changed(p);}
            MachineState Tank(BlockPos min,int w,int h,int depth)
            {
                var bounds=new StructureBounds(min,min.Offset(w-1,h-1,depth-1));
                for(int x=0;x<w;x++)for(int y=0;y<h;y++)for(int z=0;z<depth;z++)
                {var p=min.Offset(x,y,z);int axes=bounds.BoundaryAxes(p);if(axes==0)continue;Set(p,axes>1?IndustryId.TankFrame:y==0||y==h-1?IndustryId.TankWall:IndustryId.TankGlass);}
                var controller=min.Offset(1,1,0);Set(controller,IndustryId.TankController);return sim.At(controller);
            }
            var c=Tank(new BlockPos(0,0,0),3,3,3);Settle();var instance=c.Structure;
            Check(instance.Formed&&instance.Fluid.Capacity==250000&&instance.Validation.Members.Count==26,"3³ tank forms from 26 real shell members at 250 L");
            Check(instance.Validation.Reads<=4096,"Formation obeys independent hard cell budget");
            Check(instance.Fluid.Deposit(Fluids.Water,200000),"Deposit exact water quantity");
            Check(!sim.Multiblocks.CanRemove(c.Position)&&sim.Remove(c.Position)==null,"Nonempty controller dismantle requires draining");
            var broken=new BlockPos(0,2,0);Set(broken,0);
            Check(!instance.Formed&&instance.Fluid.Amount==200000,"Edit invalidates immediately, before the next transfer phase");Settle();
            Check(!instance.Formed&&instance.Fluid.Amount==200000,"Breach retains all emergency-contained contents");
            Set(broken,IndustryId.TankFrame);Settle();Check(instance.Formed&&instance.Fluid.Amount==200000,"Repair reforms same storage without duplicating contents");
            var interior=new BlockPos(1,1,1);Set(interior,BlockId.Stone);Settle();Check(!instance.Formed,"Interior obstruction invalidates a formed tank");Set(interior,Fluids.Water.Source);Settle();Check(!instance.Formed,"World source inside cannot be captured as stored fluid");Set(interior,0);Settle();Check(instance.Formed,"Removing interior obstruction restores tank");
            Set(new BlockPos(1,2,1),IndustryId.TankGlass);Settle();Check(!instance.Formed,"Glass roof rejected");Set(new BlockPos(1,2,1),IndustryId.TankWall);Settle();
            var otherController=new BlockPos(1,1,2);Set(otherController,IndustryId.TankController,2);Settle();Check(!instance.Formed&&!sim.At(otherController).Structure.Formed,"Two controllers cannot claim one cavity");Set(otherController,IndustryId.TankGlass);Settle();Check(instance.Formed,"Removing extra controller permits formation");
            sim.Rotate(c);Settle();Check(!instance.Formed,"Inward controller orientation rejected");for(int i=0;i<3;i++)sim.Rotate(c);Settle();Check(instance.Formed,"Correct orientation restores structure");
            world.Sleeping.Add(interior);sim.Multiblocks.ResidencyChanged();Check(!instance.Formed,"Residency invalidates immediately");Settle();Check(instance.State==MultiblockState.Waiting&&instance.Fluid.Amount==200000,"Unready interior sleeps without losing storage");world.Sleeping.Clear();sim.Multiblocks.ResidencyChanged();Settle();Check(instance.Formed&&c.Structure==instance,"Reload restores same structure identity");
            var rectangular=Tank(new BlockPos(20,0,0),5,4,8);Settle();Check(rectangular.Structure.Formed&&rectangular.Structure.Fluid.Capacity==9000000,"Independent dimensions 5×4×8 form with volume-based 9,000 L");
            var adjacent=Tank(new BlockPos(25,0,0),3,3,3);Settle();Check(adjacent.Structure.Formed&&rectangular.Structure.Formed&&adjacent.Structure!=rectangular.Structure,"Touching tanks remain separate instances");
            var overlap=MultiblockDefinition.Tank.Validator.Validate(MultiblockDefinition.Tank,rectangular,world,sim.At,p=>true);Check(!overlap.Valid&&overlap.Message.Contains("belongs"),"Validator rejects another owner's claim");
            var smallBudget=new MultiblockDefinition("test:bounded",9,3,MultiblockDefinition.Tank.Parts,new CuboidTankValidator(),()=>new TankMachineData(250000));
            var exhausted=smallBudget.Validator.Validate(smallBudget,c,world,sim.At,p=>false);Check(!exhausted.Valid&&exhausted.Reads==4&&exhausted.Message.Contains("budget"),"Hard scanner budget fails safely even for a legal shape");
            var max=Tank(new BlockPos(50,0,0),9,9,9);var watch=Stopwatch.StartNew();Settle();watch.Stop();Check(max.Structure.Formed&&max.Structure.Fluid.Capacity==85750000,"Configured 9³ maximum forms");log.AppendLine($"9³ formation plus topology: {watch.Elapsed.TotalMilliseconds:0.###} ms; reads {max.Structure.Validation.Reads}");
            var tooBig=Tank(new BlockPos(65,0,0),10,3,3);Settle();Check(!tooBig.Structure.Formed,"10-block dimension is bounded and rejected");
            // Resize a tank about a retained controller; old outer panels are removed explicitly.
            var resize=Tank(new BlockPos(90,0,0),4,4,4);Settle();var retained=resize.Structure;retained.Fluid.Deposit(Fluids.Water,500000);
            for(int x=0;x<4;x++)for(int y=0;y<4;y++)for(int z=0;z<4;z++){var p=new BlockPos(90+x,y,z);if(!p.Equals(resize.Position))Set(p,0);}
            var b=new StructureBounds(new BlockPos(90,0,0),new BlockPos(92,2,2));
            for(int x=0;x<3;x++)for(int y=0;y<3;y++)for(int z=0;z<3;z++){var p=b.Min.Offset(x,y,z);if(p.Equals(resize.Position))continue;int axes=b.BoundaryAxes(p);if(axes>0)Set(p,axes>1?IndustryId.TankFrame:y==0||y==2?IndustryId.TankWall:IndustryId.TankGlass);}
            Settle();Check(!retained.Formed&&retained.Validation.Message.Contains("exceeds")&&retained.Fluid.Amount==500000,"Undersized repair retains excess fluid and explains failure");
            retained.Fluid.Withdraw(250000);sim.Multiblocks.Request(retained);Settle();Check(retained.Formed&&retained.Fluid.Capacity==250000,"Draining excess allows smaller tank to form");
            retained.Fluid.Withdraw(250000);Check(sim.Multiblocks.CanRemove(resize.Position),"Empty controller can be dismantled");Set(resize.Position,0);Settle();Check(sim.Multiblocks.At(resize.Position)==null,"Dismantling releases membership and controller");
            // Two physically separate output ports drain one shared tank, including another graph.
            Set(new BlockPos(2,1,1),IndustryId.TankPort,3);Set(new BlockPos(1,1,2),IndustryId.TankPort,2);
            sim.At(new BlockPos(2,1,1)).PortMode=FluidPortMode.Output;sim.At(new BlockPos(1,1,2)).PortMode=FluidPortMode.Output;
            Set(new BlockPos(3,1,1),IndustryId.FluidPipe);Set(new BlockPos(4,1,1),IndustryId.Tank);
            Set(new BlockPos(1,1,3),IndustryId.FluidPipe);Set(new BlockPos(1,1,4),IndustryId.Tank,3);Settle();
            var a=sim.At(new BlockPos(4,1,1));var bTank=sim.At(new BlockPos(1,1,4));
            long total=instance.Fluid.Amount+a.WaterMl+bTank.WaterMl;for(int i=0;i<20;i++)sim.Step();
            Check(a.WaterMl>0&&bTank.WaterMl>0&&instance.Fluid.Amount+a.WaterMl+bTank.WaterMl==total,"Two ports in different graphs drain one authoritative tank conservatively");
            instance.Fluid.Withdraw(instance.Fluid.Amount);instance.Fluid.Deposit(Fluids.Water,100);a.WaterMl=bTank.WaterMl=0;sim.Step();Check(a.WaterMl+bTank.WaterMl==100&&instance.Fluid.Amount==0,"Two ports cannot overdraw final 100 mL");
            Set(new BlockPos(0,2,0),0);a.WaterMl=bTank.WaterMl=0;instance.Fluid.Deposit(Fluids.Water,1000);sim.Step();Check(a.WaterMl+bTank.WaterMl==0&&instance.Fluid.Amount==1000,"Breached storage cannot transfer through stale graphs");
            // A residual smaller than one bucket remains recoverable through the explicit controller outlet.
            instance.Fluid.Withdraw(instance.Fluid.Amount);instance.Fluid.Deposit(Fluids.Water,50);
            Set(new BlockPos(1,1,-1),IndustryId.FluidPipe);Set(new BlockPos(1,1,-2),IndustryId.Tank,1);
            c.RecoveryOutput=true;sim.Invalidate();Settle();sim.Step();
            Check(instance.Fluid.Amount==0&&sim.At(new BlockPos(1,1,-2)).WaterMl==50,"Explicit breached-controller outlet recovers sub-bucket remainder exactly");
            c.RecoveryOutput=false;Set(new BlockPos(0,2,0),IndustryId.TankFrame);
            sim.At(new BlockPos(2,1,1)).PortMode=FluidPortMode.Input;sim.At(new BlockPos(1,1,2)).PortMode=FluidPortMode.Input;
            sim.Rotate(a);sim.Rotate(a);sim.Rotate(bTank);sim.Rotate(bTank);
            a.WaterMl=bTank.WaterMl=1000;instance.Fluid.Deposit(Fluids.Water,249950);Settle();
            Check(instance.Formed&&instance.Fluid.Amount==250000&&a.WaterMl+bTank.WaterMl==1950,"Inputs in separate graphs reserve one shared final 50 mL of capacity");
            var growing=Tank(new BlockPos(110,0,0),3,3,3);Settle();var grown=growing.Structure;grown.Fluid.Deposit(Fluids.Water,50000);
            for(int x=0;x<3;x++)for(int y=0;y<3;y++)for(int z=0;z<3;z++){var p=new BlockPos(110+x,y,z);if(!p.Equals(growing.Position))Set(p,0);}
            var larger=new StructureBounds(new BlockPos(110,0,0),new BlockPos(114,3,4));
            for(int x=0;x<5;x++)for(int y=0;y<4;y++)for(int z=0;z<5;z++){var p=larger.Min.Offset(x,y,z);if(p.Equals(growing.Position))continue;int axes=larger.BoundaryAxes(p);if(axes>0)Set(p,axes>1?IndustryId.TankFrame:y==0||y==3?IndustryId.TankWall:IndustryId.TankGlass);}
            Settle();Check(grown.Formed&&growing.Structure==grown&&grown.Fluid.Capacity==4500000&&grown.Fluid.Amount==50000,"Expansion to 5×4×5 preserves controller identity and contents");
            var oil=new FluidDefinition("test:oil",200,199,7,5,false,3,1,.2f,.1f,.05f);var storage=new FluidStorage(20000);
            instance.Fluid.Withdraw(instance.Fluid.Amount);instance.Fluid.Deposit(oil,10000);a.WaterMl=1000;sim.Step();
            Check(instance.Fluid.Amount==10000&&a.WaterMl==1000&&a.FluidConflict,"Conflicting fluid identities in one pipe component stop before withdrawal and expose a fault");
            Check(storage.Deposit(oil,10000)&&!storage.Deposit(Fluids.Water,10000)&&storage.Amount==10000,"Generic storage rejects wrong identity before mutation");
            Check(storage.Withdraw(10000)&&storage.Fluid==null&&storage.Deposit(Fluids.Water,20000)&&!storage.Deposit(Fluids.Water,1),"Zero clears identity and full storage rejects overflow");
            Check(!storage.Resize(19999)&&storage.Capacity==20000&&!storage.Withdraw(20001),"Capacity and withdrawal cannot discard or create liquid");
            foreach(byte pipe in new[]{IndustryId.ItemPipe,IndustryId.FluidPipe})
            {
                var w=new World();var s=new IndustrySimulation(w,id=>64);var p=s.Add(new BlockPos(0,0,0),pipe);
                Check(PipeConnections.Ports(p).Count()==1,"Ordinary pipe has one channel: "+pipe);
                p.Additions=PipeAddition.Signal|PipeAddition.Power;var power=s.Add(new BlockPos(0,1,0),IndustryId.PowerCable);var lever=s.Add(new BlockPos(1,0,0),IndustryId.Lever);s.Activate(lever);
                for(int i=0;i<10;i++)s.Step();
                Check(p.Signal&&s.Power.Topology.Connections[p.Position]==4&&s.Signals.Topology.Connections[p.Position]==1,"Same pipe connection rules isolate vertical power and horizontal signal: "+pipe);
                var transport=pipe==IndustryId.ItemPipe?s.ItemNetwork:s.FluidNetwork;Check(!transport.Connections.ContainsKey(p.Position),"Control additions do not connect transport to cables: "+pipe);
                Check(PipeConnections.Ports(p).Select(v=>v.Kind).Distinct().Count()==3,"One physical pipe exposes three independent channel vertices: "+pipe);
            }
            var genericWorld=new World();var generic=new IndustrySimulation(genericWorld,id=>64);var anchor=generic.Add(new BlockPos(0,0,0),IndustryId.Bench);
            var definition=new MultiblockDefinition("test:dry_machine",1,16,new Dictionary<byte,MultiblockRole>{{IndustryId.Bench,MultiblockRole.Controller}},new FixedValidator(),()=>new DryMachine());
            generic.Multiblocks.Register(anchor,definition);generic.Step();
            Check(anchor.Structure.Formed&&anchor.Structure.MachineData is DryMachine&&anchor.Structure.Fluid==null,"Shared lifecycle accepts a different validator and non-fluid machine data");
            Check(generic.Remove(anchor.Position)==anchor,"Generic dismantle consults machine data without assuming a tank");
            log.AppendLine("Assertions: "+assertions);File.WriteAllText("Logs/multiblock-checks.txt",log.ToString());
        }
    }
}
