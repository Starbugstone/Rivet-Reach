using UnityEngine;

namespace RivetReach
{
    public sealed class WorldIndustry : IIndustryWorld
    {
        readonly Expedition game;
        public IndustrySimulation Simulation {get;}
        public WorldIndustry(Expedition game)
        {
            this.game=game;Simulation=new IndustrySimulation(this,id=>game.Registry.Get(id).stackLimit);
            game.World.BlockChanged+=Changed;game.World.ResidencyChanged+=Simulation.Invalidate;
            game.World.IsOpenMachine=p=>Simulation.At(p)?.Definition.Id==IndustryId.Door&&Simulation.At(p).Running;
        }
        public bool Ready(BlockPos p)=>game.World.Ready(p);
        public byte Get(BlockPos p)=>game.World.Get(p);
        public bool Remove(BlockPos p,byte expected)=>game.World.Remove(p,expected);
        public ItemContainer Storage(BlockPos p)=>game.Survival.At(p)?.Storage;
        public byte Drop(byte block)=>game.Registry.FistDrop(block);
        public bool PlayerInside(BlockPos p)=>game.World.OccupiesCell(game.Player.transform.position,.6f,game.Player.Height,p)||game.Mobs!=null&&game.Mobs.Occupies(p);
        void Changed(BlockPos p)
        {
            var old=Simulation.At(p);byte id=Get(p);
            if(old!=null&&old.Definition.Id!=id)
            {
                if(game.OpenMachine==old)game.SetMode(ScreenMode.Play);
                Simulation.Remove(p);
                for(int i=0;i<old.Items.Count;i++){var stack=old.Items.Take(i,int.MaxValue);if(!stack.Empty)game.Items.Spawn(stack,game.World.Local(p)+Vector3.one*.5f,Vector3.up);}
            }
            if(IndustryId.Placed(id)&&Simulation.At(p)==null)Simulation.Add(p,id);
            var above=p.Offset(0,1,0);
            if(Get(above)==IndustryId.SignalWire&&!BlockId.Solid(id))game.World.Mine(above,IndustryId.SignalWire,ToolCapability.None);
        }
        public void Advance(int ticks){for(int i=0;i<ticks;i++)Simulation.Step();}
        public bool Bucket(MachineState machine,bool fill)
        {
            if(game.OpenMachine!=machine||!Ready(machine.Position)||machine.Definition.WaterCapacity==0)return false;
            byte from=fill?Fluids.WaterBucket:Fluids.EmptyBucket,to=fill?Fluids.EmptyBucket:Fluids.WaterBucket;
            int slot=game.Inventory.FindSlot(s=>s.Id==from&&s.Count==1);
            if(slot<0||fill&&machine.WaterMl>machine.Definition.WaterCapacity-10000||!fill&&machine.WaterMl<10000)return false;
            if(!game.Inventory.ReplaceSingle(slot,from,to))return false;
            machine.WaterMl+=fill?10000:-10000;return true;
        }
    }
    public sealed partial class Expedition
    {
        public WorldIndustry Industry {get;private set;}
        public MachineState OpenMachine {get;private set;}
        public bool TryOpenMachine(BlockPos position)
        {
            if(Mode!=ScreenMode.Play||!World.Ready(position)||!World.Raycast(Player.Camera.transform.position,Player.Camera.transform.forward,5,out var visible,out _)||!visible.Equals(position))return false;
            var machine=Industry.Simulation.At(position);if(machine==null)return false;
            if(machine.Definition.Id==IndustryId.Lever||machine.Definition.Id==IndustryId.Button)
            {Industry.Simulation.Activate(machine);Sound.Place(machine.Definition.Id,World.Local(position));return true;}
            if(machine.Definition.Id==IndustryId.Bench)return TryOpenStation(position);
            OpenMachine=machine;StationPosition=position;SetMode(ScreenMode.Inventory);return true;
        }
    }
}
