using System;
using System.Collections.Generic;

namespace RivetReach
{
    public enum NetworkKind { Signal, Power, Item, Fluid }
    public enum PortRole { Route, Input, Output, Storage, Disabled }
    public enum MachineStatus { Ready, Running, NoPower, Underpowered, DisabledBySignal, OutputFull, NoInput, NoFuel, NoWater, NoShaft, Dormant, Depleted, StructureInvalid }
    public readonly struct MachinePort
    {
        public readonly NetworkKind Kind; public readonly PortRole Role; public readonly int Faces;
        public MachinePort(NetworkKind kind,PortRole role,int faces){Kind=kind;Role=role;Faces=faces;}
    }
    public static class IndustryId
    {
        public const byte AzureOre=120,AzureCrystal=121,CopperWire=122,CopperPlate=123,IronPlate=124,Cog=125,Rivets=126,Casing=127,Glass=128;
        public const byte Bench=130,SignalWire=131,SignalConduit=132,Relay=133,Lever=134,Button=135,Indicator=136,Door=137,PowerCable=138,Lamp=139,Boiler=140,Alternator=141,Crusher=142,Pump=143,Drill=144,Tank=145,ItemPipe=146,FluidPipe=147,Extractor=148,Sensor=149;
        public const byte CrushedCopper=150,CrushedIron=151,CrushedGold=152;
        public const byte TankFrame=160,TankWall=161,TankGlass=162,TankController=163,TankPort=164,TankHatch=165,TankValve=166,TankSensor=167;
        public const byte Battery=168,BatteryController=169,HandCrank=170;
        public const byte WoodenDoor=171,DoorUpper=172;
        public const byte Wrench=173,ElectricFurnace=174;
        public static bool DoorPart(byte id)=>id==WoodenDoor||id==DoorUpper;
        public static bool BatteryPart(byte id)=>id==Battery||id==BatteryController;
        public static bool TankPart(byte id)=>id>=TankFrame&&id<=TankSensor;
        public static bool Placed(byte id)=>id>=Bench&&id<=Sensor||TankPart(id)||BatteryPart(id)||id==HandCrank||id==WoodenDoor||id==ElectricFurnace;
        public static bool Route(byte id)=>id==SignalWire||id==SignalConduit||id==PowerCable||id==ItemPipe||id==FluidPipe;
        public static bool Thin(byte id)=>Route(id)||id==Lever||id==Button||id==Indicator||id==Relay||id==Sensor;
    }
    public sealed class IndustryDefinition
    {
        public readonly byte Id; public readonly string Name,Key,Help;
        public readonly int Watts,WaterCapacity;
        public bool RequiresItemFuel=>Id==IndustryId.Boiler;
        public readonly MachinePort[] Ports;
        // Lower-corner anchor; the wooden door also reserves its upper cell. Four horizontal rotations. Faces: right,left,top,bottom,back,front.
        public static readonly (int x,int y,int z)[] Directions={(1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1)};
        public static readonly IReadOnlyDictionary<byte,IndustryDefinition> All=Build();
        IndustryDefinition(byte id,string key,string name,string help,int watts,int water,params MachinePort[] ports)
        {Id=id;Key=key;Name=name;Help=help;Watts=watts;WaterCapacity=water;Ports=ports;}
        static Dictionary<byte,IndustryDefinition> Build()
        {
            var d=new Dictionary<byte,IndustryDefinition>();
            MachinePort P(NetworkKind k,PortRole r,int f)=>new MachinePort(k,r,f);
            void Add(byte id,string key,string name,string help,int watts=0,int water=0,params MachinePort[] ports)=>d.Add(id,new IndustryDefinition(id,key,name,help,watts,water,ports));
            var si=P(NetworkKind.Signal,PortRole.Input,32);var pi=P(NetworkKind.Power,PortRole.Input,16);
            var ii=P(NetworkKind.Item,PortRole.Input,2);var io=P(NetworkKind.Item,PortRole.Output,1);
            Add(IndustryId.Bench,"machinist_bench","Machinist's Bench","4 × 4 assemblies · manual component shaping");
            Add(IndustryId.SignalWire,"signal_wire","Signal Wire","Floor wire · horizontal connections only",0,0,P(NetworkKind.Signal,PortRole.Route,51));
            Add(IndustryId.SignalConduit,"signal_conduit","Signal Conduit","Enclosed signal · routes in all six directions",0,0,P(NetworkKind.Signal,PortRole.Route,63));
            Add(IndustryId.PowerCable,"power_cable","Power Cable","Electrical energy · separate from blue control",0,0,P(NetworkKind.Power,PortRole.Route,63));
            Add(IndustryId.ItemPipe,"item_pipe","Item Pipe","Items · wrench sets machine-facing input / output",0,0,P(NetworkKind.Item,PortRole.Route,63));
            Add(IndustryId.FluidPipe,"fluid_pipe","Fluid Pipe","Fluids · wrench sets machine-facing input / output",0,0,P(NetworkKind.Fluid,PortRole.Route,63));
            Add(IndustryId.Lever,"lever","Lever","Use to toggle a binary blue signal",0,0,P(NetworkKind.Signal,PortRole.Output,51));
            Add(IndustryId.Button,"button","Button","Use for a one-second pulse",0,0,P(NetworkKind.Signal,PortRole.Output,51));
            Add(IndustryId.Relay,"signal_relay","Signal Relay","Rear input → front output · one tick delay",0,0,P(NetworkKind.Signal,PortRole.Input,16),P(NetworkKind.Signal,PortRole.Output,32));
            Add(IndustryId.Indicator,"signal_indicator","Signal Indicator","Tiny signal pilot · requires no electricity",0,0,si);
            Add(IndustryId.Door,"workshop_hatch","Workshop Hatch","Signal opens the hatch · no electricity needed",0,0,si);
            Add(IndustryId.Lamp,"workshop_lamp","Workshop Lamp","Power on any face · optional signal at front",20,0,pi,si);
            Add(IndustryId.Boiler,"boiler_engine","Boiler Engine","Coal / charcoal + water → right-hand shaft",0,100000,P(NetworkKind.Fluid,PortRole.Input,16),ii);
            Add(IndustryId.Alternator,"alternator","Alternator","Left shaft couples to Boiler · 800 W output",0,0,P(NetworkKind.Power,PortRole.Output,16));
            Add(IndustryId.Crusher,"crusher","Crusher","1 raw ore → 2 crushed ore\n1 stone / cobblestone → 1 sand",160,0,pi,si,ii,io);
            Add(IndustryId.ElectricFurnace,"electric_furnace","Electric Furnace","Furnace recipes · electricity instead of fuel\n200 W · same full-power processing time",200,0,pi,si,ii,io);
            Add(IndustryId.Pump,"pump","Pump","Source below → 10 L in 2 seconds\nNo electricity required",0,10000,si,P(NetworkKind.Fluid,PortRole.Output,1));
            Add(IndustryId.Drill,"drill","Drill","Mines a finite column below · stops at bedrock",240,0,pi,si,io);
            Add(IndustryId.Tank,"water_tank","Water Tank","100 L · any one liquid · mining retains contents",0,100000,P(NetworkKind.Fluid,PortRole.Input,2),P(NetworkKind.Fluid,PortRole.Output,1));
            Add(IndustryId.Extractor,"extractor","Extractor","Chest on left → pipe on right · 4 items / sec",0,0,si,io);
            Add(IndustryId.Sensor,"inventory_sensor","Inventory Sensor","Chest behind · ON at 32 items",0,0,P(NetworkKind.Signal,PortRole.Output,32));
            Add(IndustryId.TankFrame,"tank_frame","Reinforced Tank Frame","Use on every edge and corner · outer size 3–9 per axis");
            Add(IndustryId.TankWall,"tank_wall","Tank Wall","Solid panel · required floor and roof");
            Add(IndustryId.TankGlass,"tank_glass","Reinforced Tank Glass","Side windows connect when the hollow tank forms");
            Add(IndustryId.TankController,"tank_controller","Tank Controller","Exactly one · face outward · mining retains contents",0,0,P(NetworkKind.Fluid,PortRole.Output,32));
            Add(IndustryId.TankPort,"tank_port","Tank Fluid Port","Face outward · wrench sets each pipe end",0,0,P(NetworkKind.Fluid,PortRole.Input,32));
            Add(IndustryId.TankHatch,"tank_hatch","Tank Access Hatch","10 L bucket transfers · one shared tank inventory");
            Add(IndustryId.TankValve,"tank_valve","Signal Valve Port","Front fluid nozzle + keyed signal · ON opens valve",0,0,P(NetworkKind.Fluid,PortRole.Input,32),P(NetworkKind.Signal,PortRole.Input,32));
            Add(IndustryId.TankSensor,"tank_sensor","Tank Level Sensor","Front Blue Signal output · threshold adjustable",0,0,P(NetworkKind.Signal,PortRole.Output,32));
            Add(IndustryId.Battery,"battery_block","Battery Block","100 kJ · stores grid surplus · separate connections on all faces",0,0,P(NetworkKind.Power,PortRole.Storage,63));
            Add(IndustryId.BatteryController,"battery_controller","Battery Bank Controller","Solid pack of batteries · one controller facing out",0,0,P(NetworkKind.Power,PortRole.Storage,32));
            Add(IndustryId.HandCrank,"hand_crank","Hand Crank","Use / hold Use: 50 J per turn · power on all faces",0,0,P(NetworkKind.Power,PortRole.Output,16));
            Add(IndustryId.WoodenDoor,"wooden_door","Wooden Door","Use to open / close · Blue Signal connects at the base",0,0,P(NetworkKind.Signal,PortRole.Input,63));
            return d;
        }
        public static int RotateFace(int face,int turns)
        {for(int i=0;i<turns;i++)face=face==0?5:face==5?1:face==1?4:face==4?0:face;return face;}
        public static BlockPos Neighbor(BlockPos p,int face,int turns=0)
        {var d=Directions[RotateFace(face,turns)];return p.Offset(d.x,d.y,d.z);}
    }
    public sealed class MachineState : IItemPipeInventory
    {
        IReadOnlyList<ItemStack> IItemPipeInventory.Slots=>Items.Slots;
        bool IItemPipeInventory.CanExtract(int slot)=>slot==2;
        bool IItemPipeInventory.Prefers(byte id,int localFace)=>AcceptsPipeInput(id,localFace)&&(Items.Slots[0].Id==id||
            !Items.Slots[2].Empty&&ProcessingOutput(id).Id==Items.Slots[2].Id);
        bool IItemPipeInventory.TryInsert(byte id,int localFace)=>AcceptsPipeInput(id,localFace)&&Items.Capacity(id,0,1)>0&&Items.Add(id,1,0,1)==0;
        ItemStack IItemPipeInventory.Extract(int slot,int count)=>slot==2?Items.Take(slot,count):default;
        public readonly BlockPos Position; public readonly IndustryDefinition Definition;
        public readonly ItemContainer Items;
        readonly ProcessingRegistry processing;
        public ProcessingRecipe FurnaceRecipe=>Definition.Id==IndustryId.ElectricFurnace?processing?.Find(Items.Slots[0].Id):null;
        public ItemStack ProcessingOutput(byte id)=>Definition.Id==IndustryId.Crusher?CrusherOutput(id):Definition.Id==IndustryId.ElectricFurnace?processing?.Find(id)?.Output??default:default;
        public int ProcessingTicks=>Definition.Id==IndustryId.Crusher?CrusherTicks:Definition.Id==IndustryId.ElectricFurnace?FurnaceRecipe?.Ticks??200:Definition.Id==IndustryId.Pump?40:120;
        public int Rotation {get;internal set;}
        public MachineStatus Status {get;internal set;}
        public bool Signal,SignalAttached,Source,NextSource,Eligible,FluidConflict;
        public int PulseTicks,BurnTicks,RequestedWatts,ReceivedWatts,SupplyWatts,DrillDepth=1;
        public readonly FluidStorage Fluid;
        public int WaterMl {get=>(int)Fluid.Amount;set=>Fluid.SetWater(value);}
        public MultiblockInstance Structure;
        public PipeAddition Additions;
        // Two bits per world-facing pipe end: 0 = default, 1 = into machine, 2 = out.
        // These belong to the pipe, so rotating a machine never moves a configured end.
        public int PipeDirections;
        public readonly BatteryStorage[] EnergyCells;
        public BatteryMode BatteryMode;
        public int BatteryWatts,BatteryInputWatts,BatteryOutputWatts;
        public bool RecoveryOutput;
        public FluidPortMode PortMode;
        public int LevelThreshold=80;
        public double Work;
        public byte WorkInput;
        public int Priority=1;
        public MachineState(BlockPos p,byte id,Func<byte,int> limit,ProcessingRegistry processing=null)
        {this.processing=processing;Position=p;Definition=IndustryDefinition.All[id];EnergyCells=id==IndustryId.Battery?new[]{new BatteryStorage()}:Array.Empty<BatteryStorage>();Fluid=new FluidStorage(Definition.WaterCapacity);Items=new ItemContainer(3,limit);}
        public bool Enabled=>!SignalAttached||Signal;
        public bool Running=>Status==MachineStatus.Running||Status==MachineStatus.Underpowered;
        bool AcceptsPipeInput(byte id,int localFace)=>Accepts(0,id)&&(!Definition.RequiresItemFuel||localFace<0||localFace==4);
        public bool Accepts(int slot,byte id)=>slot==0&&(Definition.Id==IndustryId.Boiler?(id==BlockId.Coal||id==BlockId.Charcoal):!ProcessingOutput(id).Empty);
        public const int CrusherTicks = 100;
        public static ItemStack CrusherOutput(byte id)=>id switch
        {
            BlockId.RawCopper=>new ItemStack(IndustryId.CrushedCopper,2),
            BlockId.RawIron=>new ItemStack(IndustryId.CrushedIron,2),
            BlockId.RawGold=>new ItemStack(IndustryId.CrushedGold,2),
            BlockId.Stone or BlockId.Cobblestone=>new ItemStack(BlockId.Sand,1),
            _=>default
        };
        public void Click(int slot,ref ItemStack held,bool right)
        {if(slot<0||slot>=3)return;if(!held.Empty&&!Accepts(slot,held.Id))return;Items.Click(slot,ref held,right);}
        public void TransferIn(ItemContainer from,int slot)
        {var s=from.Slots[slot];if(!s.Empty&&Accepts(0,s.Id))from.TransferTo(slot,Items,0,1);}
    }
}
