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
                yield return p;
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
        public static bool Accepts(MachineState m,FluidDefinition fluid)=>IndustryId.TankPart(m.Definition.Id)||fluid?.StableId==Fluids.Water.StableId;
    }
}
