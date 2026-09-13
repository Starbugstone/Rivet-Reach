using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class DoorChecks
    {
        sealed class World:IIndustryWorld
        {
            public readonly Dictionary<BlockPos,byte> Blocks=new Dictionary<BlockPos,byte>();
            public readonly HashSet<BlockPos> Occupied=new HashSet<BlockPos>(),Sleeping=new HashSet<BlockPos>();
            public bool Ready(BlockPos p)=>!Sleeping.Contains(p);
            public byte Get(BlockPos p)=>Blocks.TryGetValue(p,out var id)?id:(byte)0;
            public bool Remove(BlockPos p,byte expected)=>Blocks.Remove(p);
            public ItemContainer Storage(BlockPos p)=>null;
            public byte Drop(byte b)=>b;
            public bool PlayerInside(BlockPos p)=>Occupied.Contains(p);
        }
        public static void Run()
        {
            var lines=new StringBuilder();int count=0;
            void Check(bool ok,string msg){if(!ok)throw new Exception("Door: "+msg);count++;lines.AppendLine("PASS "+msg);}
            var world=new World();var sim=new IndustrySimulation(world,id=>64);
            MachineState Add(BlockPos p,byte id){world.Blocks[p]=id;return sim.Add(p,id);}
            void Settle(){for(int i=0;i<100;i++){sim.Step();if(!sim.Rebuilding)return;}throw new Exception("Topology stalled");}
            var p=new BlockPos(0,0,0);var door=Add(p,IndustryId.WoodenDoor);world.Blocks[p.Offset(0,1,0)]=IndustryId.DoorUpper;Settle();
            Check(door.WorkInput==0,"Door starts closed");sim.ToggleDoor(door);Check(door.WorkInput==1,"Manual use opens immediately");
            world.Occupied.Add(p.Offset(0,1,0));sim.ToggleDoor(door);Check(!door.Source&&door.WorkInput==1,"Upper-half occupant defers manual close");
            world.Occupied.Clear();sim.Step();Check(door.WorkInput==0,"Pending close completes when clear");
            var conduit=Add(p.Offset(1,0,0),IndustryId.SignalConduit);var lever=Add(p.Offset(2,0,0),IndustryId.Lever);Settle();
            Check(door.SignalAttached&&!door.Signal&&door.WorkInput==0,"OFF Blue cable connects without electricity");
            sim.Activate(lever);sim.Step();Check(door.WorkInput==1&&door.Signal,"Rising signal opens door");
            sim.ToggleDoor(door);sim.Step();Check(door.WorkInput==0,"Right-click overrides steady signal");
            sim.Activate(lever);sim.Step();sim.Activate(lever);sim.Step();Check(door.WorkInput==1,"Next rising edge opens after manual override");
            world.Occupied.Add(p);sim.Activate(lever);sim.Step();Check(!door.Source&&door.WorkInput==1,"Signal close waits for lower-half occupant");
            world.Occupied.Clear();sim.Step();Check(door.WorkInput==0,"Signal close retries once doorway clears");
            sim.Activate(lever);sim.Step();sim.Remove(conduit.Position);world.Blocks.Remove(conduit.Position);Settle();Check(!door.Signal&&door.WorkInput==0,"Disconnecting live cable closes door");
            sim.ToggleDoor(door);world.Sleeping.Add(p.Offset(0,1,0));sim.Invalidate();Settle();Check(!door.Eligible&&door.WorkInput==1,"Dormant upper cell preserves open state");
            sim.ToggleDoor(door);Check(door.WorkInput==1,"Partially dormant door rejects interaction");world.Sleeping.Clear();sim.Invalidate();Settle();Check(door.WorkInput==1,"Wake retains manual open state");
            var items=ItemRegistry.Load();var catalog=RecipeCatalogAsset.Load();var registry=catalog.Compile(items);
            Check(items.items.All(i=>i.runtimeId!=IndustryId.DoorUpper)&&!BlockId.Placeable(IndustryId.DoorUpper),"Upper part is neither a catalog item nor independently placeable");
            Check(items.FistDrop(IndustryId.DoorUpper)==IndustryId.WoodenDoor,"Upper mining resolves to the same door item");
            var recipe=registry.Recipes.Single(r=>r.Id=="rivet:wooden_door");
            Check(recipe.MinimumGridSize==3&&recipe.Width==2&&recipe.Height==3&&recipe.Kind==RecipeKind.Shaped&&recipe.Output.Count==3,"Workbench recipe is two columns of three planks, producing three doors");
            for(int size=3;size<=4;size++)for(int ox=0;ox<=size-2;ox++)for(int oy=0;oy<=size-3;oy++)
            {
                var craft=new CraftingSession(registry,size,id=>items.Get(id).stackLimit);
                for(int y=0;y<3;y++)for(int x=0;x<2;x++){int slot=(y+oy)*size+x+ox;craft.Grid.Add(BlockId.Planks,1,slot,slot+1);}
                ItemStack held=default;Check(craft.CraftToCursor(ref held).Succeeded&&held.Id==IndustryId.WoodenDoor&&held.Count==3&&craft.Grid.Slots.All(s=>s.Empty),"Exact plank/output conservation in translated "+size+" grid");
            }
            // Fingerprints retain every older definition; removing only the additive content is allowed.
            var store=new SaveStore("unused",items);var oldItems=ScriptableObject.CreateInstance<ItemRegistry>();var originals=catalog.recipes;
            try
            {
                foreach(bool crank in new[]{true,false})
                {
                    oldItems.items=items.items.Where(i=>i.runtimeId!=Fluids.LavaBucket&&i.runtimeId!=IndustryId.ElectricFurnace&&i.runtimeId!=IndustryId.Wrench&&i.runtimeId!=IndustryId.WoodenDoor&&(crank||i.runtimeId!=IndustryId.HandCrank)).Select(i=>JsonUtility.FromJson<ItemDefinition>(JsonUtility.ToJson(i))).ToArray();
                    catalog.recipes=originals.Where(r=>r.stableId!="rivet:industry_174"&&r.stableId!="rivet:wrench"&&r.stableId!="rivet:wooden_door"&&(crank||r.stableId!="rivet:industry_170")).ToArray();
                    var entry=new SaveEntry{Id=Guid.NewGuid().ToString("N"),WorldId=Guid.NewGuid().ToString("N"),Name="Door compatibility",UtcTicks=DateTime.UtcNow.Ticks};
                    var bytes=SaveFixtureEnvelope.Schema3(new SaveStore("unused",oldItems).Encode(entry,w=>w.Write(314)));using(var reader=store.Open(bytes,out _))Check(reader.ReadInt32()==314,"Pre-door content loads, crank present="+crank);
                    oldItems.items[0].attackDamage++;bytes=SaveFixtureEnvelope.Schema3(new SaveStore("unused",oldItems).Encode(entry,w=>w.Write(314)));bool rejected=false;
                    try{using var reader=store.Open(bytes,out _);}catch(InvalidDataException){rejected=true;}Check(rejected,"Unrelated content changes rejected, crank present="+crank);
                }
            }
            finally{catalog.recipes=originals;UnityEngine.Object.DestroyImmediate(oldItems);}
            lines.AppendLine(count+" checks passed");File.WriteAllText("Logs/door-checks.txt",lines.ToString());
        }
    }
}
