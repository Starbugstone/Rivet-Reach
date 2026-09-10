using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        Text machineStatus,machineDetail;Image machineProgress;float nextMachineRefresh;
        const int MachineSlotStart=300;
        void BuildMachine(Transform parent)
        {
            var m=game.OpenMachine;
            if(IndustryId.TankPart(m.Definition.Id)){BuildMultiblockMachine(parent,m);return;}
            Label(parent,"MACHINE CONTROL",821,115,315,32,23);
            Label(parent,m.Definition.Help,821,157,307,62,15,gold);
            var status=Panel(parent,821,228,310,55,slate);machineStatus=Label(status.transform,"",12,10,290,40,19,gold);
            if(m.Definition.Id==IndustryId.Crusher||m.Definition.Id==IndustryId.Boiler)
            {Label(parent,m.Definition.Id==IndustryId.Boiler?"FUEL":"RAW ORE",821,299,150,22,13,gold);Slot(parent,MachineSlotStart,821,325,55);}
            if(m.Definition.Id==IndustryId.Crusher||m.Definition.Id==IndustryId.Drill)
            {Label(parent,"OUTPUT",1053,299,90,22,13,gold);Slot(parent,MachineSlotStart+2,1069,325,55);}
            var track=Panel(parent,892,346,155,9,slate);machineProgress=Panel(track.transform,0,0,0,9,gold);
            machineDetail=Label(parent,"",821,391,311,62,14);
            if(m.Definition.WaterCapacity>0||m.Definition.Id==IndustryId.TankController||m.Definition.Id==IndustryId.TankHatch)
            {Button(parent,"ADD 10 L",821,455,146,34,()=>{if(!game.Industry.Bucket(m,true))game.Notify("Need a water bucket and 10 L of free space",3);});Button(parent,"TAKE 10 L",977,455,147,34,()=>{if(!game.Industry.Bucket(m,false))game.Notify("Need an empty bucket and 10 L of water",3);});}
            else if(m.Definition.Watts>0)
            {Button(parent,"PRIORITY: "+new[]{"HIGH","NORMAL","LOW"}[m.Priority],821,455,303,34,()=>{m.Priority=(m.Priority+1)%3;Rebuild();});}
            Button(parent,"ROTATE PORTS 90°",821,495,303,34,()=>{game.Industry.Simulation.Rotate(m);Rebuild();});
            if(PipeConnections.IsTransport(m.Definition.Id))
            {
                Button(parent,(m.Additions&PipeAddition.Signal)!=0?"WITH SIGNAL":"FIT SIGNAL",821,455,146,34,()=>{if(!game.Industry.AddPipeChannel(m,PipeAddition.Signal))game.Notify("Requires one Signal Conduit",3);Rebuild();});
                Button(parent,(m.Additions&PipeAddition.Power)!=0?"WITH POWER":"FIT POWER",977,455,147,34,()=>{if(!game.Industry.AddPipeChannel(m,PipeAddition.Power))game.Notify("Requires one Power Cable",3);Rebuild();});
            }
            nextMachineRefresh=0;
        }
        void BuildMultiblockMachine(Transform parent,MachineState m)
        {
            Label(parent,"TANK CONTROL",821,115,315,32,23);
            Label(parent,m.Definition.Help,821,157,307,62,15,gold);
            var status=Panel(parent,821,228,310,55,slate);machineStatus=Label(status.transform,"",12,10,290,40,18,gold);
            machineDetail=Label(parent,"",821,299,307,82,14);
            var track=Panel(parent,821,388,303,7,slate);machineProgress=Panel(track.transform,0,0,0,7,gold);
            if(m.Definition.Id==IndustryId.TankController||m.Definition.Id==IndustryId.TankHatch)
            {
                Button(parent,"ADD 10 L",821,415,146,34,()=>{if(!game.Industry.Bucket(m,true))game.Notify("Need a formed tank, compatible bucket and 10 L free capacity",3);});
                Button(parent,"TAKE 10 L",977,415,147,34,()=>{if(!game.Industry.Bucket(m,false))game.Notify("Need an empty bucket and 10 L stored fluid",3);});
            }
            if(m.Definition.Id==IndustryId.TankPort||m.Definition.Id==IndustryId.TankValve)
                Button(parent,"PORT: "+m.PortMode,821,415,303,34,()=>{m.PortMode=(FluidPortMode)(((int)m.PortMode+1)%3);game.Industry.Simulation.Invalidate();Rebuild();});
            if(m.Definition.Id==IndustryId.TankSensor)
                Button(parent,"ON AT: "+m.LevelThreshold+"%",821,415,303,34,()=>{m.LevelThreshold=m.LevelThreshold==100?10:m.LevelThreshold+10;Rebuild();});
            Button(parent,"ROTATE 90°",821,456,146,34,()=>{game.Industry.Simulation.Rotate(m);Rebuild();});
            Button(parent,"RE-SCAN",977,456,147,34,()=>{var c=m.Structure??game.Industry.Simulation.Multiblocks.At(m.Position);if(c!=null)game.Industry.Simulation.Multiblocks.Request(c);else game.Notify("Build a sealed shell with one outward-facing controller",3);});
            if(m.Definition.Id==IndustryId.TankController)
                Button(parent,"RECOVERY OUT: "+(m.RecoveryOutput?"ON":"OFF"),821,497,303,30,()=>{m.RecoveryOutput=!m.RecoveryOutput;game.Industry.Simulation.Invalidate();Rebuild();});
            nextMachineRefresh=0;
        }
        static string MachinePortSummary(MachineState m)
        {
            if(PipeConnections.IsTransport(m.Definition.Id))
                return "Connections on all six faces: "+string.Join(" · ",PipeConnections.Ports(m).Select(p=>p.Kind==NetworkKind.Item?"Items":p.Kind==NetworkKind.Fluid?"Fluids":p.Kind==NetworkKind.Signal?"Blue Signal":"Electricity"));
            if(IndustryId.TankPart(m.Definition.Id))return "Shell blocks share one tank · front faces outward";
            string ports="";
            foreach(var p in PipeConnections.Ports(m))
            {for(int f=0;f<6;f++)if((p.Faces&(1<<f))!=0)ports+=(ports.Length==0?"":" · ")+p.Kind+" "+p.Role+" "+new[]{"R","L","top","base","rear","front"}[f];}
            return "Ports (relative to front): "+ports;
        }
        void RefreshMachine()
        {
            if(machineStatus==null||game.OpenMachine==null||Time.unscaledTime<nextMachineRefresh)return;
            nextMachineRefresh=Time.unscaledTime+.1f;var m=game.OpenMachine;
            machineStatus.text=m.FluidConflict?"Fluid conflict · drain vessels":game.Industry.Simulation.Rebuilding?"Connecting networks…":m.Status==MachineStatus.NoInput&&m.Definition.Id==IndustryId.Extractor?"Chest empty or missing":m.Status==MachineStatus.NoInput&&m.Definition.Id==IndustryId.Drill?"Cutting path obstructed":StatusName(m.Status);
            machineStatus.color=m.Running?new Color(.35f,.90f,.83f):gold;
            bool signal=PipeConnections.Ports(m).Any(p=>p.Kind==NetworkKind.Signal);
            string detail=signal?"Signal: "+(m.SignalAttached||IndustryId.Route(m.Definition.Id)?(m.Signal?"ON":"OFF"):m.Definition.Ports.Any(p=>p.Kind==NetworkKind.Signal&&p.Role==PortRole.Output)?(m.Source?"ON":"OFF"):"not connected"):"";
            if(m.Definition.Watts>0)detail+=$"\nPower: {m.ReceivedWatts} / {m.RequestedWatts} W";
            if(m.Definition.Id==IndustryId.Alternator)detail+=$"\nElectrical output: {m.SupplyWatts} W";
            if(m.Definition.WaterCapacity>0)detail+=$"\nWater: {m.WaterMl/1000f:0.0} / {m.Definition.WaterCapacity/1000} L";
            if(m.Definition.Id==IndustryId.Boiler)detail+=$"\nFuel remaining: {m.BurnTicks/20f:0.0} s";
            if(m.Definition.Id==IndustryId.Drill)detail+=$"\nCutting depth: {m.DrillDepth} blocks";
            if(IndustryId.TankPart(m.Definition.Id))
            {
                var c=m.Structure??game.Industry.Simulation.Multiblocks.At(m.Position);
                machineStatus.text=m.FluidConflict?"Fluid conflict · drain vessels":c==null?"Awaiting completed shell":c.State==MultiblockState.Pending?"Validating structure…":c.Formed?"FORMED · "+c.Bounds:c.State==MultiblockState.Waiting?"Waiting for terrain":"STRUCTURE INVALID";
                detail=c==null?"Build hollow · frame edges · solid roof/floor":$"{c.Fluid.Fluid?.DisplayName??"Empty"}: {c.Fluid.Amount/1000.0:0.###} / {c.Fluid.Capacity/1000.0:0.###} L\nController: {c.Controller.Position}";
                if(c!=null&&!c.Formed)detail+="\n"+c.Validation.Message+(c.Validation.Fault.HasValue?" @ "+c.Validation.Fault:"");
                machineDetail.fontSize=c!=null&&!c.Formed?12:14;
            }
            if(PipeConnections.IsTransport(m.Definition.Id))detail+="\nAdditional channels: "+m.Additions;
            machineDetail.text=detail;
            int ticks=m.Definition.Id==IndustryId.Crusher?100:m.Definition.Id==IndustryId.Pump?40:120;
            float fraction=(float)m.Work/ticks;
            if(m.Definition.Id==IndustryId.Boiler)fraction=m.BurnTicks/1600f;
            if(m.Definition.Id==IndustryId.Tank)fraction=m.WaterMl/(float)m.Definition.WaterCapacity;
            if(m.Definition.Id==IndustryId.Alternator)fraction=m.SupplyWatts/400f;
            if(m.Definition.Id==IndustryId.Lamp)fraction=m.ReceivedWatts/20f;
            if(IndustryId.TankPart(m.Definition.Id))fraction=m.Structure?.Fluid.Capacity>0?(float)(m.Structure.Fluid.Amount/(double)m.Structure.Fluid.Capacity):0;
            machineProgress.rectTransform.sizeDelta=new Vector2((IndustryId.TankPart(m.Definition.Id)?303:155)*Mathf.Clamp01(fraction),IndustryId.TankPart(m.Definition.Id)?7:9);
        }
        public static string StatusName(MachineStatus state)=>state switch
        {
            MachineStatus.NoPower=>"No electrical power",MachineStatus.Underpowered=>"Underpowered · working slowly",MachineStatus.DisabledBySignal=>"Disabled by signal",
            MachineStatus.OutputFull=>"Output full",MachineStatus.NoInput=>"Waiting for ingredients",MachineStatus.NoFuel=>"Add coal or charcoal",MachineStatus.NoWater=>"Waiting for source water",
            MachineStatus.NoShaft=>"Connect a running boiler",MachineStatus.Dormant=>"Waiting for loaded terrain",MachineStatus.Depleted=>"Bedrock · column complete",_=>state.ToString()
        };
    }
}
