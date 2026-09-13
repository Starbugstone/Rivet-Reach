using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class CompostChecks
    {
        sealed class World:IIndustryWorld
        {
            public bool Sleeping;
            public readonly Dictionary<BlockPos,byte> Blocks=new Dictionary<BlockPos,byte>();
            public bool Ready(BlockPos p)=>!Sleeping;
            public byte Get(BlockPos p)=>Blocks.TryGetValue(p,out var id)?id:(byte)0;
            public bool Remove(BlockPos p,byte expected)=>false;
            public ItemContainer Storage(BlockPos p)=>null;
            public byte Drop(byte id)=>id;
            public bool PlayerInside(BlockPos p)=>false;
        }
        public static void Run()
        {
            Directory.CreateDirectory("Logs/Compost");var lines=new List<string>();
            void Check(bool ok,string text){if(!ok)throw new Exception("Compost: "+text);lines.Add("PASS "+text);}
            var registry=ItemRegistry.Load();var processing=ProcessingCatalogAsset.Load().Compile(registry);var catalog=CompostCatalog.Current;
            var crafting=RecipeCatalogAsset.Load().Compile(registry);var session=new CraftingSession(crafting,3,i=>registry.Get(i).stackLimit);
            foreach(int slot in new[]{0,2,3,5,6,7,8})session.Grid.Add(BlockId.Planks,1,slot,slot+1);
            Check(session.Preview?.Output.Id==CompostId.Bin&&session.Preview.Output.Count==1,"Seven-plank U layout produces one bin at workbench");
            session.Grid.Take(8,1);Check(session.Preview==null,"Incomplete bin recipe cannot craft");
            foreach(var input in catalog.inputs)
            {
                byte id=registry.ResolveId(input.item);int count=catalog.BatchCount(id);
                Check(registry.HasTag(id,"compostable")&&count*catalog.Points(id)==catalog.pointsPerCompost,"Exact configured input value: "+input.item);
                var world=new World();var sim=new IndustrySimulation(world,i=>registry.Get(i).stackLimit,processing);var bin=sim.Add(new BlockPos(0,0,0),CompostId.Bin);var port=(IItemPipeInventory)bin;
                for(int i=0;i<count-1;i++)Check(port.TryInsert(id,i%6),"All-face insertion "+input.item+" #"+i);
                for(int i=0;i<250;i++)sim.Step();Check(bin.Work==0&&bin.Items.Slots[2].Empty&&bin.Items.Slots[0].Count==count-1,"Incomplete batch retains all inputs: "+input.item);
                Check(port.TryInsert(id,5),"Finish organic batch");for(int i=0;i<catalog.ticks-1;i++)sim.Step();
                Check(bin.Work==catalog.ticks-1&&bin.Items.Slots[0].Count==count&&bin.Items.Slots[2].Empty,"Unpaid partial work retains exact recoverable ingredients");
                sim.Step();Check(bin.Items.Slots[0].Empty&&bin.Items.Slots[2].Id==CompostId.Compost&&bin.Items.Slots[2].Count==1&&bin.Work==0,"Exact completed compost transaction");
                Check(bin.RequestedWatts==0&&bin.ReceivedWatts==0&&bin.BurnTicks==0&&!PipeConnections.Supports(bin,NetworkKind.Power),"No fuel or electrical endpoint");
                Check(port.Extract(0,1).Empty&&port.Extract(1,1).Empty&&port.Extract(2,1).Id==CompostId.Compost,"Pipes extract only finished compost");
            }
            {
                var world=new World();var sim=new IndustrySimulation(world,i=>registry.Get(i).stackLimit,processing);var pos=new BlockPos(0,0,0);world.Blocks[pos]=CompostId.Bin;var bin=sim.Add(pos,CompostId.Bin);var port=(IItemPipeInventory)bin;
                Check(!port.TryInsert(CompostId.Compost)&&!port.TryInsert(BlockId.Stone)&&!port.TryInsert(BlockId.Coal),"Compost, inert matter and fuel cannot enter");
                bin.Items.Add(BlockId.Potato,24,0,1);for(int i=0;i<70;i++)sim.Step();Check(bin.Work==70,"Partial work started");
                bin.Items.Add(CompostId.Compost,64,2,3);for(int i=0;i<200;i++)sim.Step();Check(bin.Work==70&&bin.Items.Slots[0].Count==24&&bin.Status==MachineStatus.OutputFull,"Full output pauses without consumption");
                bin.Items.Take(2,64);world.Sleeping=true;sim.Invalidate();for(int i=0;i<200;i++)sim.Step();Check(bin.Work==70&&bin.Status==MachineStatus.Dormant,"Dormancy preserves partial work");world.Sleeping=false;sim.Invalidate();
                var leverPos=pos.Offset(0,0,-1);world.Blocks[leverPos]=IndustryId.Lever;var lever=sim.Add(leverPos,IndustryId.Lever);for(int i=0;i<20;i++)sim.Step();Check(bin.SignalAttached&&!bin.Enabled&&bin.Work==70,"Optional OFF signal pauses work");sim.Activate(lever);for(int i=0;i<10;i++)sim.Step();Check(bin.Work==80,"ON resumes exact work");
                byte[] Save(){using var stream=new MemoryStream();using(var writer=new SaveWriter(stream))typeof(IndustrySimulation).GetMethod("WriteSave",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(sim,new object[]{writer});return stream.ToArray();}
                IndustrySimulation Load(byte[] bytes){var loaded=new IndustrySimulation(world,i=>registry.Get(i).stackLimit,processing);using var stream=new MemoryStream(bytes);using var reader=new SaveReader(stream,registry);typeof(IndustrySimulation).GetMethod("ReadSave",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(loaded,new object[]{reader});return loaded;}
                var restored=Load(Save());var b=restored.At(pos);Check(b.Work==80&&b.Items.Slots[0].Count==24,"Save preserves ingredients and exact partial progress");for(int i=0;i<120;i++)restored.Step();Check(b.Items.Slots[2].Count==1&&b.Items.Slots[0].Count==12,"Restored batch completes exactly once");
                void Reject(string label,Action mutate,Action reset){mutate();bool rejected=false;try{Load(Save());}catch(System.Reflection.TargetInvocationException e) when(e.InnerException is InvalidDataException){rejected=true;}finally{reset();}Check(rejected,label);}
                Reject("Reject fractional compost work",()=>bin.Work=.5,()=>bin.Work=80);
                Reject("Reject completed uncommitted work",()=>bin.Work=catalog.ticks,()=>bin.Work=80);
                Reject("Reject fuel in compost state",()=>bin.BurnTicks=1,()=>bin.BurnTicks=0);
                Reject("Reject hidden-slot contents",()=>bin.Items.Add(BlockId.Stone,1,1,2),()=>bin.Items.Take(1,64));
                bin.Items.Take(0,64);bin.Items.Add(FarmId.WheatSeed,24,0,1);sim.Step();Check(bin.Work==1&&bin.WorkInput==FarmId.WheatSeed,"Changing input identity resets unpaid work");
            }
            foreach(var crop in CropRules.Definitions.Where(c=>c.stages>1))
            {
                int points=CropRules.Harvest(crop.Mature,uint.MaxValue).Sum(s=>s.Count*catalog.Points(s.Id));
                int maximum=crop.maxYield*catalog.Points(crop.produce)+crop.seedYield*catalog.Points(crop.planting);
                Check(points<=maximum&&maximum<catalog.pointsPerCompost*(crop.stages-1),"Harvest cannot fund repeated instant growth: "+crop.key);
            }
            var copy=JsonUtility.FromJson<CompostCatalog>(Resources.Load<TextAsset>("Definitions/Compost").text);copy.Validate(registry);int before=copy.Points(BlockId.Potato);copy.inputs[0].points=5;bool invalid=false;try{copy.Validate(registry);}catch(ArgumentException){invalid=true;}Check(invalid&&copy.Points(BlockId.Potato)==before,"Catalog rejection is atomic and disallows fractional batches");
            var browser=new RecipeBrowserIndex(registry,RecipeCatalogAsset.Load().Compile(registry),processing);Check(browser.Find(CompostId.Compost,false).Count==catalog.inputs.Length,"Browser and wiki expose every exact compost conversion");
            File.WriteAllLines("Logs/Compost/checks.txt",lines);Debug.Log("Compost checks: "+lines.Count+" assertions passed");
        }
    }
}
