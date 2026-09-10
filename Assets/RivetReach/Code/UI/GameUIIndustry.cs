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
            Label(parent,"MACHINE CONTROL",821,115,315,32,23);
            Label(parent,m.Definition.Help,821,157,307,62,15,gold);
            var status=Panel(parent,821,228,310,55,slate);machineStatus=Label(status.transform,"",12,10,290,40,19,gold);
            if(m.Definition.Id==IndustryId.Crusher||m.Definition.Id==IndustryId.Boiler)
            {Label(parent,m.Definition.Id==IndustryId.Boiler?"FUEL":"RAW ORE",821,299,150,22,13,gold);Slot(parent,MachineSlotStart,821,325,55);}
            if(m.Definition.Id==IndustryId.Crusher||m.Definition.Id==IndustryId.Drill)
            {Label(parent,"OUTPUT",1053,299,90,22,13,gold);Slot(parent,MachineSlotStart+2,1069,325,55);}
            var track=Panel(parent,892,346,155,9,slate);machineProgress=Panel(track.transform,0,0,0,9,gold);
            machineDetail=Label(parent,"",821,391,311,62,14);
            if(m.Definition.WaterCapacity>0)
            {Button(parent,"ADD 10 L",821,455,146,34,()=>{if(!game.Industry.Bucket(m,true))game.Notify("Need a water bucket and 10 L of free space",3);});Button(parent,"TAKE 10 L",977,455,147,34,()=>{if(!game.Industry.Bucket(m,false))game.Notify("Need an empty bucket and 10 L of water",3);});}
            else if(m.Definition.Watts>0)
            {Button(parent,"PRIORITY: "+new[]{"HIGH","NORMAL","LOW"}[m.Priority],821,455,303,34,()=>{m.Priority=(m.Priority+1)%3;Rebuild();});}
            Button(parent,"ROTATE PORTS 90°",821,495,303,34,()=>{game.Industry.Simulation.Rotate(m);Rebuild();});
            nextMachineRefresh=0;
        }
        static string MachinePortSummary(MachineState m)
        {
            string ports="";
            foreach(var p in m.Definition.Ports)
            {for(int f=0;f<6;f++)if((p.Faces&(1<<f))!=0)ports+=(ports.Length==0?"":" · ")+p.Kind+" "+p.Role+" "+new[]{"R","L","top","base","rear","front"}[f];}
            return "Ports (relative to front): "+ports;
        }
        void RefreshMachine()
        {
            if(machineStatus==null||game.OpenMachine==null||Time.unscaledTime<nextMachineRefresh)return;
            nextMachineRefresh=Time.unscaledTime+.1f;var m=game.OpenMachine;
            machineStatus.text=game.Industry.Simulation.Rebuilding?"Connecting networks…":m.Status==MachineStatus.NoInput&&m.Definition.Id==IndustryId.Extractor?"Chest empty or missing":m.Status==MachineStatus.NoInput&&m.Definition.Id==IndustryId.Drill?"Cutting path obstructed":StatusName(m.Status);
            machineStatus.color=m.Running?new Color(.35f,.90f,.83f):gold;
            bool signal=m.Definition.Ports.Any(p=>p.Kind==NetworkKind.Signal);
            string detail=signal?"Signal: "+(m.SignalAttached||IndustryId.Route(m.Definition.Id)?(m.Signal?"ON":"OFF"):m.Definition.Ports.Any(p=>p.Kind==NetworkKind.Signal&&p.Role==PortRole.Output)?(m.Source?"ON":"OFF"):"not connected"):"";
            if(m.Definition.Watts>0)detail+=$"\nPower: {m.ReceivedWatts} / {m.RequestedWatts} W";
            if(m.Definition.Id==IndustryId.Alternator)detail+=$"\nElectrical output: {m.SupplyWatts} W";
            if(m.Definition.WaterCapacity>0)detail+=$"\nWater: {m.WaterMl/1000f:0.0} / {m.Definition.WaterCapacity/1000} L";
            if(m.Definition.Id==IndustryId.Boiler)detail+=$"\nFuel remaining: {m.BurnTicks/20f:0.0} s";
            if(m.Definition.Id==IndustryId.Drill)detail+=$"\nCutting depth: {m.DrillDepth} blocks";
            machineDetail.text=detail;
            int ticks=m.Definition.Id==IndustryId.Crusher?100:m.Definition.Id==IndustryId.Pump?40:120;
            float fraction=(float)m.Work/ticks;
            if(m.Definition.Id==IndustryId.Boiler)fraction=m.BurnTicks/1600f;
            if(m.Definition.Id==IndustryId.Tank)fraction=m.WaterMl/(float)m.Definition.WaterCapacity;
            if(m.Definition.Id==IndustryId.Alternator)fraction=m.SupplyWatts/400f;
            if(m.Definition.Id==IndustryId.Lamp)fraction=m.ReceivedWatts/20f;
            machineProgress.rectTransform.sizeDelta=new Vector2(155*Mathf.Clamp01(fraction),9);
        }
        public static string StatusName(MachineStatus state)=>state switch
        {
            MachineStatus.NoPower=>"No electrical power",MachineStatus.Underpowered=>"Underpowered · working slowly",MachineStatus.DisabledBySignal=>"Disabled by signal",
            MachineStatus.OutputFull=>"Output full",MachineStatus.NoInput=>"Waiting for ingredients",MachineStatus.NoFuel=>"Add coal or charcoal",MachineStatus.NoWater=>"Waiting for source water",
            MachineStatus.NoShaft=>"Connect a running boiler",MachineStatus.Dormant=>"Waiting for loaded terrain",MachineStatus.Depleted=>"Bedrock · column complete",_=>state.ToString()
        };
    }
}
