using System;
using System.Collections.Generic;

namespace RivetReach
{
    public enum MultiblockRole { None, Frame, Wall, Glass, Controller, FluidPort, Hatch, Valve, Sensor, Battery }
    public enum MultiblockState { Pending, Formed, Invalid, Waiting }
    public readonly struct StructureBounds
    {
        public readonly BlockPos Min,Max;
        public StructureBounds(BlockPos min,BlockPos max){Min=min;Max=max;}
        public int Width=>checked((int)(Max.X-Min.X+1));
        public int Height=>checked(Max.Y-Min.Y+1);
        public int Depth=>checked((int)(Max.Z-Min.Z+1));
        public int InteriorCells=>checked((Width-2)*(Height-2)*(Depth-2));
        public bool Contains(BlockPos p)=>p.X>=Min.X&&p.X<=Max.X&&p.Y>=Min.Y&&p.Y<=Max.Y&&p.Z>=Min.Z&&p.Z<=Max.Z;
        public int BoundaryAxes(BlockPos p)=>(p.X==Min.X||p.X==Max.X?1:0)+(p.Y==Min.Y||p.Y==Max.Y?1:0)+(p.Z==Min.Z||p.Z==Max.Z?1:0);
        public int Face(BlockPos p)=>p.X==Max.X?0:p.X==Min.X?1:p.Y==Max.Y?2:p.Y==Min.Y?3:p.Z==Max.Z?4:5;
        public override string ToString()=>$"{Width} × {Height} × {Depth}";
    }
    public sealed class MultiblockValidation
    {
        public bool Valid;public bool Waiting;public string Message;public BlockPos? Fault;
        public StructureBounds Bounds;
        public readonly Dictionary<BlockPos,MultiblockRole> Members=new Dictionary<BlockPos,MultiblockRole>();
        public int Reads;
        public MultiblockValidation Fail(string message,BlockPos? p=null,bool waiting=false){Valid=false;Message=message;Fault=p;Waiting=waiting;return this;}
    }
    public interface IMultiblockValidator
    {
        MultiblockValidation Validate(MultiblockDefinition definition,MachineState controller,IIndustryWorld world,Func<BlockPos,MachineState> machine,Func<BlockPos,bool> claimed);
    }
    public sealed class MultiblockDefinition
    {
        public readonly string StableId;
        public readonly int MaxDimension,CellBudget;
        public readonly IReadOnlyDictionary<byte,MultiblockRole> Parts;
        public readonly IMultiblockValidator Validator;
        public readonly Func<IMultiblockMachineData> CreateMachine;
        public MultiblockDefinition(string id,int maxDimension,int cellBudget,IReadOnlyDictionary<byte,MultiblockRole> parts,IMultiblockValidator validator,Func<IMultiblockMachineData> createMachine)
        {
            if(string.IsNullOrWhiteSpace(id)||maxDimension<1||maxDimension>32||cellBudget<1||cellBudget>65536||parts==null||validator==null||createMachine==null)throw new ArgumentException("Invalid bounded multiblock definition");
            StableId=id;MaxDimension=maxDimension;CellBudget=cellBudget;Parts=parts;Validator=validator;CreateMachine=createMachine;
        }
        public MultiblockRole Role(byte id)=>Parts.TryGetValue(id,out var role)?role:MultiblockRole.None;
        public static readonly MultiblockDefinition BatteryBank=new MultiblockDefinition("rivet:battery_bank",5,1024,new Dictionary<byte,MultiblockRole>{{IndustryId.Battery,MultiblockRole.Battery},{IndustryId.BatteryController,MultiblockRole.Controller}},new BatteryBankValidator(),()=>new BatteryBankData());
        public static readonly MultiblockDefinition Tank=new MultiblockDefinition("rivet:rectangular_tank",9,4096,new Dictionary<byte,MultiblockRole>{
            {IndustryId.TankFrame,MultiblockRole.Frame},{IndustryId.TankWall,MultiblockRole.Wall},{IndustryId.TankGlass,MultiblockRole.Glass},
            {IndustryId.TankController,MultiblockRole.Controller},{IndustryId.TankPort,MultiblockRole.FluidPort},{IndustryId.TankHatch,MultiblockRole.Hatch},
            {IndustryId.TankValve,MultiblockRole.Valve},{IndustryId.TankSensor,MultiblockRole.Sensor}},new CuboidTankValidator(),()=>new TankMachineData(250000));
    }
    public interface IMultiblockMachineData
    {
        bool CanDismantle {get;}
        string TryForm(MultiblockValidation validation);
    }
    public sealed class TankMachineData : IMultiblockMachineData
    {
        readonly long perCell;
        public readonly FluidStorage Storage=new FluidStorage(0);
        public TankMachineData(long millilitresPerCell){if(millilitresPerCell<1)throw new ArgumentOutOfRangeException(nameof(millilitresPerCell));perCell=millilitresPerCell;}
        public bool CanDismantle=>Storage.Amount==0;
        public string TryForm(MultiblockValidation validation)
        {
            long capacity=checked((long)validation.Bounds.InteriorCells*perCell);
            return Storage.Resize(capacity)?null:$"Stored amount exceeds new capacity by {(Storage.Amount-capacity)/1000.0:0.###} L";
        }
    }
    public sealed class MultiblockInstance
    {
        public Guid StructureId {get;internal set;}=Guid.NewGuid();
        public readonly Guid WorldId;
        public readonly MultiblockDefinition Definition;
        public readonly MachineState Controller;
        public readonly IMultiblockMachineData MachineData;
        public FluidStorage Fluid=>(MachineData as TankMachineData)?.Storage;
        public long Revision {get;internal set;}
        public MultiblockState State {get;internal set;}=MultiblockState.Pending;
        public MultiblockValidation Validation {get;internal set;}=new MultiblockValidation{Message="Validate structure"};
        public MultiblockValidation LastFormed {get;internal set;}
        public StructureBounds Bounds=>Validation.Bounds;
        public bool Formed=>State==MultiblockState.Formed;
        public MultiblockInstance(Guid worldId,MultiblockDefinition definition,MachineState controller){WorldId=worldId;Definition=definition;Controller=controller;MachineData=definition.CreateMachine();}
    }
}
