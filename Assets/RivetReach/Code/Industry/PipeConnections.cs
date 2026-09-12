using System;
using System.Collections.Generic;

namespace RivetReach
{
    [Flags] public enum PipeAddition { None=0, Signal=1, Power=2 }
    public enum FluidPortMode { Input, Output, Disabled }
    public static class PipeConnections
    {
        public static bool IsTransport(byte id)=>id==IndustryId.ItemPipe||id==IndustryId.FluidPipe;
        // All channel geometry and graph discovery consume these exact same rotated faces.
        public static int WorldFaces(MachinePort port,int rotation)
        {int faces=0;for(int f=0;f<6;f++)if((port.Faces&(1<<f))!=0)faces|=1<<IndustryDefinition.RotateFace(f,rotation);return faces;}
        public static bool Matches(int a,int b,int face)=>(a&(1<<face))!=0&&(b&(1<<(face^1)))!=0;
        public static IEnumerable<MachinePort> Ports(MachineState m)
        {
            if(IndustryId.BatteryPart(m.Definition.Id)&&BatteryPower.Cells(m).Count==0)yield break;
            foreach(var p in m.Definition.Ports)
            {
                if(IndustryId.TankPart(m.Definition.Id))
                {
                    if(m.Definition.Id==IndustryId.TankController)
                    {if(RecoveryEnabled(m))yield return p;continue;}
                    if(m.Structure==null||!m.Structure.Formed)continue;
                    if(p.Kind==NetworkKind.Fluid){if(m.PortMode==FluidPortMode.Disabled)continue;yield return new MachinePort(p.Kind,m.PortMode==FluidPortMode.Input?PortRole.Input:PortRole.Output,p.Faces);continue;}
                }
                yield return p.Kind==NetworkKind.Power?new MachinePort(p.Kind,p.Role,63):p;
            }
            if(!IsTransport(m.Definition.Id))yield break;
            if((m.Additions&PipeAddition.Signal)!=0)yield return new MachinePort(NetworkKind.Signal,PortRole.Route,63);
            if((m.Additions&PipeAddition.Power)!=0)yield return new MachinePort(NetworkKind.Power,PortRole.Route,63);
        }
        static bool RecoveryEnabled(MachineState m)=>m.RecoveryOutput&&m.Structure!=null&&(m.Structure.Formed||m.Structure.State==MultiblockState.Invalid);
        public static bool FluidEnabled(MachineState m)
        {
            if(!IndustryId.TankPart(m.Definition.Id))return true;
            if(m.Definition.Id==IndustryId.TankController)return RecoveryEnabled(m);
            return m.Structure?.Formed==true&&m.PortMode!=FluidPortMode.Disabled&&(m.Definition.Id!=IndustryId.TankValve||m.SignalAttached&&m.Signal);
        }
        public static FluidStorage Storage(MachineState m)=>IndustryId.TankPart(m.Definition.Id)?m.Structure?.Fluid:m.Fluid;
        public static bool Accepts(MachineState m,FluidDefinition fluid)
        {
            if(m==null||fluid==null||!Supports(m,NetworkKind.Fluid))return false;
            if(IndustryId.TankPart(m.Definition.Id))return true;
            // Explicit machine capabilities: adding a fluid port to another device
            // must not silently make it accept water (or every future liquid).
            byte id=m.Definition.Id;
            return (id==IndustryId.Boiler||id==IndustryId.Pump||id==IndustryId.Tank)&&fluid.StableId==Fluids.Water.StableId;
        }
        public static NetworkKind TransportKind(MachineState pipe)=>pipe.Definition.Id==IndustryId.ItemPipe?NetworkKind.Item:NetworkKind.Fluid;
        public static bool Supports(MachineState m,NetworkKind kind)
        {
            if(m==null||IndustryId.Route(m.Definition.Id))return false;
            foreach(var p in m.Definition.Ports)if(p.Kind==kind)return true;
            return false;
        }
        public static PortRole DefaultRole(MachineState machine,NetworkKind kind,int worldFace)
        {
            // Keep the old inlet/outlet directions as defaults for existing constructions.
            // Newly available faces prefer input on machines that have an input buffer.
            PortRole fallback=PortRole.Output;
            foreach(var p in machine.Definition.Ports)
            {
                if(p.Kind!=kind)continue;
                var role=kind==NetworkKind.Fluid&&IndustryId.TankPart(machine.Definition.Id)&&machine.Definition.Id!=IndustryId.TankController
                    ?(machine.PortMode==FluidPortMode.Output?PortRole.Output:PortRole.Input):p.Role;
                if((WorldFaces(p,machine.Rotation)&(1<<worldFace))!=0)return role;
                if(role==PortRole.Input)fallback=PortRole.Input;
            }
            return fallback;
        }
        public static PortRole EndRole(MachineState pipe,int face,MachineState machine)
        {
            int configured=(pipe.PipeDirections>>(face*2))&3;
            return configured==3?PortRole.Disabled:configured==1?PortRole.Input:configured==2?PortRole.Output:machine==null?PortRole.Input:DefaultRole(machine,TransportKind(pipe),face^1);
        }
        public static bool ValidDirections(int directions)
        {
            // All four two-bit values are defined: default, input, output, disconnected.
            return directions>=0&&directions<=4095;
        }
    }
}
