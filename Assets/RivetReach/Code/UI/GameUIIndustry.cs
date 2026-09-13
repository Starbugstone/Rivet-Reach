using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        const int MachineSlotStart=300;
        void BuildMachine(Transform parent)
        {
            var m=game.OpenMachine;
            if(IndustryId.BatteryPart(m.Definition.Id)){BuildBatteryMachine(parent,m);return;}
            if(IndustryId.TankPart(m.Definition.Id)){BuildMultiblockMachine(parent,m);return;}
            Label(parent,"MACHINE CONTROL",821,115,315,32,23);
            Label(parent,PipeConnections.IsTransport(m.Definition.Id)?"Wrench + right-click the pipe side: Input → Output → No connection. Click the missing end to reconnect.":m.Definition.Help+(m.Definition.Watts>0?"\nPower connects on all six faces." : ""),821,157,307,62,15,gold);
            var status=Panel(parent,821,228,310,55,slate);machineStatus=Label(status.transform,"",12,10,290,40,19,gold);
            if(m.Definition.Id==IndustryId.Crusher||m.Definition.Id==IndustryId.ElectricFurnace||m.Definition.Id==IndustryId.Boiler)
            {Label(parent,m.Definition.Id==IndustryId.Boiler?"FUEL":"INPUT",821,299,150,22,13,gold);Slot(parent,MachineSlotStart,821,325,55);}
            if(m.Definition.Id==IndustryId.Crusher||m.Definition.Id==IndustryId.ElectricFurnace||m.Definition.Id==IndustryId.Drill)
            {Label(parent,"OUTPUT",1053,299,90,22,13,gold);Slot(parent,MachineSlotStart+2,1069,325,55);}
            var track=Panel(parent,892,346,155,9,slate);machineProgress=Panel(track.transform,0,0,0,9,gold);
            machineDetail=Label(parent,"",821,385,311,68,m.Definition.Id==IndustryId.Pump?12:14);
            if(m.Definition.WaterCapacity>0||m.Definition.Id==IndustryId.TankController||m.Definition.Id==IndustryId.TankHatch)
            {MachineButton(parent,live=>"ADD 10 L",821,455,146,34,live=>{if(!game.Industry.Bucket(live,true))game.Notify("Need a compatible liquid bucket and 10 L of free space",3);});MachineButton(parent,live=>"TAKE 10 L",977,455,147,34,live=>{if(!game.Industry.Bucket(live,false))game.Notify("Need an empty bucket and 10 L of stored liquid",3);});}
            else if(m.Definition.Watts>0)
            {MachineButton(parent,live=>"PRIORITY: "+new[]{"HIGH","NORMAL","LOW"}[live.Priority],821,455,303,34,live=>{live.Priority=(live.Priority+1)%3;});}
            MachineButton(parent,live=>"ROTATE PORTS 90°",821,495,303,34,live=>{game.Industry.Simulation.Rotate(live);});
            if(PipeConnections.IsTransport(m.Definition.Id))
            {
                MachineButton(parent,live=>(live.Additions&PipeAddition.Signal)!=0?"WITH SIGNAL":"FIT SIGNAL",821,455,146,34,live=>{if(!game.Industry.AddPipeChannel(live,PipeAddition.Signal))game.Notify("Requires one Signal Conduit",3);});
                MachineButton(parent,live=>(live.Additions&PipeAddition.Power)!=0?"WITH POWER":"FIT POWER",977,455,147,34,live=>{if(!game.Industry.AddPipeChannel(live,PipeAddition.Power))game.Notify("Requires one Power Cable",3);});
            }
            nextMachineRefresh=0;
        }
        void BuildBatteryMachine(Transform parent,MachineState m)
        {
            Label(parent,m.Definition.Id==IndustryId.Battery?"BATTERY":"BATTERY BANK",821,115,315,32,23);
            Label(parent,m.Definition.Help,821,157,307,62,15,gold);
            var status=Panel(parent,821,228,310,55,slate);machineStatus=Label(status.transform,"",12,10,290,40,18,gold);
            machineDetail=Label(parent,"",821,299,307,112,14);
            var track=Panel(parent,821,415,303,7,slate);machineProgress=Panel(track.transform,0,0,0,7,gold);
            MachineButton(parent,live=>"MODE: "+live.BatteryMode,821,437,303,34,live=>{live.BatteryMode=(BatteryMode)(((int)live.BatteryMode+1)%4);});
            MachineButton(parent,live=>"ROTATE 90°",821,478,146,34,live=>{game.Industry.Simulation.Rotate(live);});
            if(m.Definition.Id==IndustryId.BatteryController)MachineButton(parent,live=>"RE-SCAN",977,478,147,34,live=>game.Industry.Simulation.Multiblocks.Request(live.Structure));
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
                MachineButton(parent,live=>"ADD 10 L",821,415,146,34,live=>{if(!game.Industry.Bucket(live,true))game.Notify("Need a formed tank, compatible bucket and 10 L free capacity",3);});
                MachineButton(parent,live=>"TAKE 10 L",977,415,147,34,live=>{if(!game.Industry.Bucket(live,false))game.Notify("Need an empty bucket and 10 L stored fluid",3);});
            }
            if(m.Definition.Id==IndustryId.TankPort||m.Definition.Id==IndustryId.TankValve)
                MachineButton(parent,live=>live.PortMode==FluidPortMode.Disabled?"PORT: DISABLED":"PORT: ENABLED",821,415,303,34,live=>{live.PortMode=live.PortMode==FluidPortMode.Disabled?FluidPortMode.Input:FluidPortMode.Disabled;game.Industry.Simulation.Invalidate();});
            if(m.Definition.Id==IndustryId.TankSensor)
                MachineButton(parent,live=>"ON AT: "+live.LevelThreshold+"%",821,415,303,34,live=>{live.LevelThreshold=live.LevelThreshold==100?10:live.LevelThreshold+10;});
            MachineButton(parent,live=>"ROTATE 90°",821,456,146,34,live=>{game.Industry.Simulation.Rotate(live);});
            MachineButton(parent,live=>"RE-SCAN",977,456,147,34,live=>{var c=live.Structure??game.Industry.Simulation.Multiblocks.At(live.Position);if(c!=null)game.Industry.Simulation.Multiblocks.Request(c);else game.Notify("Build a sealed shell with one outward-facing controller",3);});
            if(m.Definition.Id==IndustryId.TankController)
                MachineButton(parent,live=>"RECOVERY OUT: "+(live.RecoveryOutput?"ON":"OFF"),821,497,303,30,live=>{live.RecoveryOutput=!live.RecoveryOutput;game.Industry.Simulation.Invalidate();});
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
            machineStatus.text=m.FluidConflict?"Fluid conflict · drain vessels":m.Status==MachineStatus.NoInput&&m.Definition.Id==IndustryId.Extractor?"Chest empty or missing":m.Status==MachineStatus.NoInput&&m.Definition.Id==IndustryId.Drill?"Cutting path obstructed":StatusName(m.Status);
            machineStatus.color=m.Running?new Color(.35f,.90f,.83f):gold;
            bool signal=PipeConnections.Ports(m).Any(p=>p.Kind==NetworkKind.Signal);
            string detail=signal?"Signal: "+(m.SignalAttached||IndustryId.Route(m.Definition.Id)?(m.Signal?"ON":"OFF"):m.Definition.Ports.Any(p=>p.Kind==NetworkKind.Signal&&p.Role==PortRole.Output)?(m.Source?"ON":"OFF"):"not connected"):"";
            string powerConnection=PipeConnections.Ports(m).Any(p=>p.Kind==NetworkKind.Power)?"Power network: "+(game.Industry.Simulation.Power.Topology.Connected(m)?"connected":"not connected"):"";
            if(powerConnection.Length>0)detail+=(detail.Length>0?"\n":"")+powerConnection;
            if(m.Definition.Watts>0)detail+=$"\nPower: {m.ReceivedWatts} / {m.RequestedWatts} W";
            if(m.Definition.Id==IndustryId.Alternator)detail+=$"\nElectrical output: {m.SupplyWatts} W";
            if(m.Definition.WaterCapacity>0)detail+=$"\n{(m.Definition.Id==IndustryId.Tank?m.Fluid.Fluid?.DisplayName??"Empty":"Water")}: {m.Fluid.Amount/1000.0:0.###} / {m.Fluid.Capacity/1000} L";
            if(m.Definition.Id==IndustryId.Pump){byte intake=game.World.Get(IndustrySimulation.Neighbor(m,3));detail+="\nBelow: "+(intake==Fluids.Water.Source?"water source":intake==0?"air — needs source":"blocked / not a source");}
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
            if(IndustryId.BatteryPart(m.Definition.Id))
            {
                var c=m.Structure;bool member=m.Definition.Id==IndustryId.Battery&&c!=null;
                long amount=member?m.EnergyCells[0].Amount:BatteryPower.Amount(m),capacity=member?m.EnergyCells[0].Capacity:BatteryPower.Capacity(m);
                machineStatus.text=member?"Controlled by battery bank":m.Definition.Id==IndustryId.BatteryController&&c?.Formed!=true?"BANK INCOMPLETE":m.BatteryInputWatts>0&&m.BatteryOutputWatts>0?"Charging and supplying":m.BatteryWatts>0?"Charging":m.BatteryWatts<0?"Delivering electricity":m.BatteryMode==BatteryMode.Isolated?"Isolated":amount==0?"Empty · connect generation":amount==capacity?"Fully charged":"Ready · no power transfer";
                detail=$"Stored: {amount/1000000.0:0.###} / {capacity/1000000.0:0.###} kJ\nCharge: {m.BatteryInputWatts} W · Output: {m.BatteryOutputWatts} W";
                detail+=member?"\nBank mode overrides cell mode":"\nStores surplus · supplies connected loads";
                if(powerConnection.Length>0)detail+="\n"+powerConnection;
                if(c!=null)detail+=c.Formed?"\nFORMED · "+c.Bounds:"\n"+c.Validation.Message;
                machineProgress.rectTransform.sizeDelta=new Vector2(capacity>0?303*(float)(amount/(double)capacity):0,7);
                machineDetail.text=detail;return;
            }
            if(m.Definition.Id==IndustryId.PowerCable)
            {
                var grid=game.Industry.Simulation.Power.Topology.Groups.FirstOrDefault(g=>g.Ports.Any(p=>p.Machine==m));
                if(grid!=null)
                {
                    long stored=0;foreach(var p in grid.Ports)if(p.Port.Role==PortRole.Storage)stored+=BatteryPower.Amount(p.Machine);
                    detail+=$"\nGrid input: {grid.Supply} W · Required: {grid.Demand} W\nConnected storage: {stored/1000000.0:0.###} kJ";
                }
            }
            if(PipeConnections.IsTransport(m.Definition.Id))detail+="\nAdditional channels: "+m.Additions;
            machineDetail.text=detail;
            int ticks=m.ProcessingTicks;
            float fraction=(float)m.Work/ticks;
            if(m.Definition.Id==IndustryId.Boiler)fraction=m.BurnTicks/1600f;
            if(m.Definition.Id==IndustryId.Tank)fraction=m.WaterMl/(float)m.Definition.WaterCapacity;
            if(m.Definition.Id==IndustryId.Alternator)fraction=m.SupplyWatts/(float)IndustrySimulation.AlternatorWatts;
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
