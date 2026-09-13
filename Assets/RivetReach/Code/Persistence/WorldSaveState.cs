using System;
using System.Collections.Generic;
using System.Linq;

namespace RivetReach
{
    public sealed partial class VoxelWorld
    {
        internal void WriteSave(SaveWriter w)
        {
            w.Write(edits.Count);
            foreach(var page in edits.OrderBy(p=>p.Key.X).ThenBy(p=>p.Key.Y).ThenBy(p=>p.Key.Z))
            {w.Pos(page.Key.Min);w.Write(page.Value.Count);foreach(var e in page.Value.OrderBy(e=>e.Key)){w.Write(e.Key);w.Write(e.Value);}}
            w.Write(torchSupports.Values.Sum(p=>p.Count));foreach(var page in torchSupports.Values)foreach(var p in page){w.Pos(p.Key);w.Pos(p.Value);}
            w.Write(grassAccumulator);w.Write(treeAccumulator);w.Write(fluidAccumulator);
            Grass.WriteSave(w);Trees.WriteSave(w);FluidSimulation.WriteSave(w);
            w.Write(leafHarvestSequence);w.Positions(grownTreeCells.OrderBy(p=>p.X).ThenBy(p=>p.Y).ThenBy(p=>p.Z));
        }
        internal void ReadSave(SaveReader r,BlockPos origin)
        {
            SaveReader.Require(chunks.Count==0,"Restore requires a fresh world.");Origin=origin;
            int count=r.Count(100000);
            for(int i=0;i<count;i++)
            {
                var min=r.Pos();SaveReader.Require(min.Equals(min.Chunk.Min),"Invalid saved chunk address.");var page=new Dictionary<int,byte>();edits.Add(min.Chunk,page);
                int n=r.Count(32768);
                for(int j=0;j<n;j++)
                {
                    int index=r.Int(0,32767);byte id=r.ReadByte();var p=min.Offset(index%32,index/32%32,index/1024);
                    SaveReader.Require(p.Y>TerrainGenerator.MinY&&p.Y<=TerrainGenerator.MaxY&&p.X>=-TerrainGenerator.HorizontalLimit&&p.X<=TerrainGenerator.HorizontalLimit&&p.Z>=-TerrainGenerator.HorizontalLimit&&p.Z<=TerrainGenerator.HorizontalLimit,"Invalid edited terrain position.");
                    SaveReader.Require(id==0||Fluids.IsFluid(id)||BlockId.Placeable(id)||BlockId.Crop(id)||id==IndustryId.DoorUpper||id==BlockId.Farmland,"Invalid saved terrain cell.");page.Add(index,id);
                    if(BlockId.Opaque(id)){var key=(p.X,p.Z);if(!editedColumns.TryGetValue(key,out var column))editedColumns[key]=column=new SortedSet<int>();column.Add(p.Y);}
                }
            }
            count=r.Count();
            for(int i=0;i<count;i++)
            {
                var p=r.Pos();var support=r.Pos();
                SaveReader.Require(Get(p)==BlockId.Torch&&BlockId.Solid(Get(support))&&p.Y>=support.Y&&Math.Abs(p.X-support.X)+Math.Abs(p.Y-support.Y)+Math.Abs(p.Z-support.Z)==1,"Invalid torch attachment.");
                if(!torchSupports.TryGetValue(p.Chunk,out var page))torchSupports[p.Chunk]=page=new Dictionary<BlockPos,BlockPos>();page.Add(p,support);
            }
            foreach(var page in edits)foreach(var e in page.Value)if(e.Value==BlockId.Torch)
            {var p=page.Key.Min.Offset(e.Key%32,e.Key/32%32,e.Key/1024);SaveReader.Require(TorchSupport(p,out _),"Missing saved torch attachment.");}
            grassAccumulator=r.Float(0);treeAccumulator=r.Float(0);fluidAccumulator=r.Float(0);
            ValidateDoors();
            Grass.ReadSave(r);Trees.ReadSave(r);FluidSimulation.ReadSave(r);
            if(r.Format>=3)
            {
                leafHarvestSequence=r.Long();
                foreach(var p in r.Positions())
                    SaveReader.Require((Get(p)==BlockId.Log||Get(p)==BlockId.Leaves)&&edits.TryGetValue(p.Chunk,out var page)&&page.ContainsKey(p.Index)&&grownTreeCells.Add(p),"Invalid grown tree provenance.");
            }
        }
        internal void ValidateDoors()
        {
            foreach(var p in SavedBlocks())
            {
                if(p.Value==IndustryId.WoodenDoor)SaveReader.Require(Get(p.Key.Offset(0,1,0))==IndustryId.DoorUpper&&DoorFloor(Get(p.Key.Offset(0,-1,0))),"Invalid door footprint or support.");
                if(p.Value==IndustryId.DoorUpper)SaveReader.Require(Get(p.Key.Offset(0,-1,0))==IndustryId.WoodenDoor,"Orphaned upper door cell.");
            }
        }
        internal IEnumerable<KeyValuePair<BlockPos,byte>> SavedBlocks()
        {foreach(var page in edits)foreach(var e in page.Value)yield return new KeyValuePair<BlockPos,byte>(page.Key.Min.Offset(e.Key%32,e.Key/32%32,e.Key/1024),e.Value);}
    }
    public sealed partial class GrassSimulation
    {
        internal void WriteSave(SaveWriter w)=>w.Write(Tick);
        internal void ReadSave(SaveReader r)=>Tick=r.Long();
    }
    public sealed partial class TreeSimulation
    {
        internal void WriteSave(SaveWriter w)
        {
            w.Write(tick);w.Write(felling.Count);
            foreach(var f in felling){w.Write(f.CutY);w.Positions(f.Pending);w.Positions(f.Seen);}
            w.Write(leaves.Count);foreach(var leaf in leaves){w.Pos(leaf.p);w.Write(leaf.due);}
        }
        internal void ReadSave(SaveReader r)
        {
            tick=r.Long();int n=r.Count();
            for(int i=0;i<n;i++){var f=new Fell{CutY=r.Int(TerrainGenerator.MinY,TerrainGenerator.MaxY)};foreach(var p in r.Positions())f.Pending.Enqueue(p);foreach(var p in r.Positions())f.Seen.Add(p);SaveReader.Require(f.Pending.Count>0,"Empty saved tree job.");felling.Enqueue(f);}
            n=r.Count();for(int i=0;i<n;i++){var p=r.Pos();long due=r.Long();SaveReader.Require(scheduledLeaves.Add(p),"Duplicate leaf job.");leaves.Enqueue((p,due));}
        }
    }
    public sealed partial class FluidSimulation
    {
        internal void WriteSave(SaveWriter w)
        {
            w.Write(tick);w.Write(due.Count);foreach(var job in due){w.Write(job.Key);w.Positions(job.Value);}
            w.Write(sleeping.Count);foreach(var page in sleeping){w.Pos(page.Key.Min);w.Positions(page.Value);}
        }
        internal void ReadSave(SaveReader r)
        {
            tick=r.Long();int n=r.Count();
            for(int i=0;i<n;i++){long when=r.Long();var positions=r.Positions();due.Add(when,new Queue<BlockPos>(positions));foreach(var p in positions)SaveReader.Require(scheduled.Add(p),"Duplicate fluid job.");}
            n=r.Count();for(int i=0;i<n;i++){var p=r.Pos();SaveReader.Require(p.Equals(p.Chunk.Min),"Invalid fluid frontier.");sleeping.Add(p.Chunk,new HashSet<BlockPos>(r.Positions()));}
        }
    }
    public sealed partial class WorldSurvival
    {
        internal void WriteSave(SaveWriter w)
        {
            w.Write(Tick);w.Write(fraction);w.Write(stations.Count);
            foreach(var entry in stations)
            {
                w.Pos(entry.Key);var s=entry.Value;w.Write(s.Block);w.Write(s.Rotation);
                if(s.Crafting!=null)w.Slots(s.Crafting.Grid.Slots);if(s.Storage!=null)w.Slots(s.Storage.Slots);s.Furnace?.WriteSave(w);
            }
            w.Write(crops.Count);foreach(var crop in crops){w.Pos(crop.Position);w.Write(crop.Due);}
        }
        internal void ReadSave(SaveReader r)
        {
            Tick=r.Long();fraction=r.Number();int n=r.Count();
            for(int i=0;i<n;i++)
            {
                var p=r.Pos();byte id=r.ReadByte();SaveReader.Require(BlockId.Station(id)&&game.World.Get(p)==id,"Saved station does not match terrain.");
                var s=new StationState(id,game.Recipes,game.Processing,item=>game.Registry.Get(item).stackLimit);stations.Add(p,s);
                s.Rotation=r.Format>=5?r.Int(0,3):0;
                if(s.Crafting!=null)r.Slots(s.Crafting.Grid);if(s.Storage!=null)r.Slots(s.Storage);s.Furnace?.ReadSave(r);Wake(p);
            }
            n=r.Count();for(int i=0;i<n;i++){var p=r.Pos();long due=r.Long();byte id=game.World.Get(p);SaveReader.Require(BlockId.GrowingPlant(id)&&!scheduled.ContainsKey(p),"Invalid crop schedule.");Schedule(p,due);}
            foreach(var p in game.World.SavedBlocks())
            {if(BlockId.Station(p.Value))SaveReader.Require(stations.ContainsKey(p.Key),"Missing saved station.");if(BlockId.GrowingPlant(p.Value))SaveReader.Require(scheduled.ContainsKey(p.Key),"Missing saved crop schedule.");}
        }
    }
    public sealed partial class FurnaceState
    {
        internal void WriteSave(SaveWriter w){w.Slots(Slots);w.Write(BurnTicks);w.Write(FuelDuration);w.Write(ProgressTicks);w.Write(workingInput);}
        internal void ReadSave(SaveReader r)
        {
            r.Slots(contents);FuelDuration=0;BurnTicks=r.Int(0,int.MaxValue);FuelDuration=r.Int(BurnTicks,int.MaxValue);ProgressTicks=r.Int(0,int.MaxValue);workingInput=r.ReadByte();
            SaveReader.Require(workingInput==Slots[0].Id&&(Recipe==null?ProgressTicks==0:ProgressTicks<Recipe.Ticks),"Invalid furnace work state.");
            SaveReader.Require((Slots[0].Empty||Accepts(0,Slots[0].Id))&&(Slots[1].Empty||Accepts(1,Slots[1].Id)),"Invalid furnace contents.");
        }
    }
    public sealed partial class IndustrySimulation
    {
        internal void WriteSave(SaveWriter w)
        {
            w.Write(Tick);w.Write(Multiblocks.WorldId.ToString("N"));w.Write(machines.Count);
            foreach(var m in machines.Values)
            {
                w.Pos(m.Position);w.Write(m.Definition.Id);w.Slots(m.Items.Slots);w.Write(m.Rotation);w.Write(m.Source);w.Write(m.NextSource);w.Write(m.PulseTicks);w.Write(m.BurnTicks);
                w.Write(m.WaterMl);w.Write(m.DrillDepth);w.Write(m.Work);w.Write(m.WorkInput);w.Write(m.Priority);w.Write((int)m.Additions);w.Write((int)m.BatteryMode);
                w.Write(m.RecoveryOutput);w.Write((int)m.PortMode);w.Write(m.LevelThreshold);w.Write((int)m.Status);w.Write(m.PipeDirections);
                if(m.EnergyCells.Length==1)w.Write(m.EnergyCells[0].Amount);
                if(m.Definition.Id==IndustryId.TankController||m.Definition.Id==IndustryId.BatteryController)
                {
                    w.Write(m.Structure.StructureId.ToString("N"));
                    if(m.Structure.Fluid!=null){var f=m.Structure.Fluid;w.Write(f.Capacity);w.Write(f.Amount);w.Write(f.Fluid?.StableId??"");}
                }
            }
        }
        internal void ReadSave(SaveReader r)
        {
            Tick=r.Long();SaveReader.Require(Guid.TryParseExact(r.Text(32),"N",out var worldId),"Invalid multiblock world identity.");Multiblocks.WorldId=worldId;
            int n=r.Count();var ids=new HashSet<Guid>();
            for(int i=0;i<n;i++)
            {
                var p=r.Pos();byte id=r.ReadByte();SaveReader.Require(IndustryId.Placed(id)&&world.Get(p)==id,"Saved machine does not match terrain.");var m=Add(p,id);
                r.Slots(m.Items);m.Rotation=r.Int(0,3);m.Source=r.ReadBoolean();m.NextSource=r.ReadBoolean();m.PulseTicks=r.Int(0,id==IndustryId.HandCrank?IndustrySimulation.CrankTicks:20);m.BurnTicks=r.Int(0,1600);
                m.WaterMl=r.Int(0,m.Definition.WaterCapacity);m.DrillDepth=r.Int(1,TerrainGenerator.MaxY-TerrainGenerator.MinY+2);m.Work=r.Number(0,m.Definition.Id==IndustryId.ElectricFurnace?processing.Recipes.Max(recipe=>recipe.Ticks):120);m.WorkInput=r.ReadByte();m.Priority=r.Int(0,2);
                m.Additions=(PipeAddition)r.Int(0,3);SaveReader.Require(m.Additions==0||PipeConnections.IsTransport(id),"Invalid pipe fittings.");m.BatteryMode=(BatteryMode)r.Int(0,3);
                m.RecoveryOutput=r.ReadBoolean();m.PortMode=(FluidPortMode)r.Int(0,2);m.LevelThreshold=r.Int(0,100);m.Status=(MachineStatus)r.Int(0,Enum.GetValues(typeof(MachineStatus)).Length-1);
                if(r.Format>=4){m.PipeDirections=r.Int(0,4095);SaveReader.Require(PipeConnections.ValidDirections(m.PipeDirections)&&(m.PipeDirections==0||PipeConnections.IsTransport(id)),"Invalid pipe end directions.");}
                if(m.EnergyCells.Length==1)SaveReader.Require(m.EnergyCells[0].Charge(r.Long(0,BatteryStorage.CellCapacity)),"Invalid battery energy.");
                if(id==IndustryId.TankController||id==IndustryId.BatteryController)
                {
                    SaveReader.Require(Guid.TryParseExact(r.Text(32),"N",out var structureId)&&ids.Add(structureId),"Invalid structure identity.");m.Structure.StructureId=structureId;
                    if(m.Structure.Fluid!=null)
                    {
                        long capacity=r.Long(0,85750000),amount=r.Long(0,capacity);string fluid=r.Text();var f=m.Structure.Fluid;
                        SaveReader.Require(f.Resize(capacity)&&(amount==0?fluid=="":fluid==Fluids.Water.StableId&&f.Deposit(Fluids.Water,amount)),"Invalid saved tank contents.");
                    }
                }
            }
            foreach(var m in machines.Values)if(m.Definition.Id==IndustryId.WoodenDoor)
                SaveReader.Require(world.Get(m.Position.Offset(0,1,0))==IndustryId.DoorUpper&&m.WorkInput<=1&&m.Work==0&&m.PulseTicks==0&&m.BurnTicks==0&&m.Items.Slots.All(s=>s.Empty),"Invalid saved door.");
            // Graphs and shared cell membership are rebuilt from restored terrain when it is resident.
            Invalidate();
        }
    }
}
