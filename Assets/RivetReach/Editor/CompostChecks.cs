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
            MachineState New(byte id,out IndustrySimulation sim,out World world)
            {world=new World();sim=new IndustrySimulation(world,i=>registry.Get(i).stackLimit,processing);var pos=new BlockPos(2,3,4);world.Blocks[pos]=id;var m=sim.Add(pos,id);sim.Step();if(m.IsAutoComposter)m.CompostCharge=MachineState.CompostChargeCapacity;return m;}
            foreach(var input in catalog.inputs)
            {
                byte id=registry.ResolveId(input.item);var bin=New(CompostId.Bin,out var sim,out var world);var pipe=(IItemPipeInventory)bin;
                int value=catalog.Points(id);
                Check(registry.HasTag(id,"compostable")&&value>0,"Authored compostable value: "+input.item);
                var held=new ItemStack(id,3);bin.Click(0,ref held,true);
                Check(held.Count==2&&bin.CompostPoints==value&&bin.Items.Slots[0].Empty,"One-item deposit is consumed immediately: "+input.item);
                bin.Click(0,ref held,false);Check(held.Empty&&bin.CompostPoints==3*value,"Stack deposit retains mixed progress: "+input.item);
                for(int face=0;face<6;face++)Check(!pipe.TryInsert(id,face),"Manual bin rejects pipe input face "+face);
                Check(!PipeConnections.Supports(bin,NetworkKind.Item)&&!PipeConnections.Supports(bin,NetworkKind.Power)&&!PipeConnections.Supports(bin,NetworkKind.Signal),"Basic bin exposes no automation endpoints");
                var auto=New(CompostId.Auto,out sim,out world);pipe=(IItemPipeInventory)auto;
                for(int face=0;face<6;face++)Check(pipe.TryInsert(id,face),"Auto accepts ingredient on face "+face);
                Check(auto.Items.Slots[0].Empty&&auto.CompostPoints==6*value%catalog.pointsPerCompost,"Pipe inputs immediately join shared progress");
                Check(pipe.Extract(0,10).Empty&&pipe.Extract(1,10).Empty,"Pipes cannot extract consumed input");
            }
            {
                var bin=New(CompostId.Bin,out var sim,out var world);
                Check(bin.AddCompost(FarmId.WheatSeed,3)==3&&bin.AddCompost(BlockId.Potato,4)==4&&bin.AddCompost(FarmId.Bread,3)==3&&bin.CompostPoints==23,"Mixed identities accumulate 23 points without requiring matching stacks");
                int roll=bin.NextCompostYield;Check(bin.AddCompost(BlockId.Potato,1)==1&&bin.CompostPoints==1&&bin.Items.Slots[2].Count==roll&&roll>=1&&roll<=4,"Completing item rolls 1–4 once and carries excess");
                var seen=new HashSet<int>();
                for(int i=0;i<100;i++){bin.Items.Take(2,64);roll=bin.NextCompostYield;seen.Add(roll);bin.AddCompost(FarmId.WheatSeed,24);Check(bin.Items.Slots[2].Count==roll&&bin.CompostPoints==1,"Stable batch yield and conserved remainder #"+i);}
                Check(seen.Count==4,"All four output quantities occur in deterministic batch sample");
                var auto=New(CompostId.Auto,out sim,out world);auto.AddCompost(FarmId.WheatSeed,23);auto.Items.Add(CompostId.Compost,64,2,3);roll=auto.NextCompostYield;long batches=auto.CompostBatches;
                var held=new ItemStack(BlockId.Potato,7);auto.Click(0,ref held,false);Check(held.Count==7&&auto.CompostPoints==23&&auto.CompostBatches==batches&&auto.NextCompostYield==roll,"Full output consumes nothing and does not reroll");
                auto.Items.Take(2,64);world.Sleeping=true;sim.Invalidate();sim.Step();Check(!((IItemPipeInventory)auto).TryInsert(BlockId.Potato),"Dormant autocomposter rejects resource transfer");world.Sleeping=false;sim.Invalidate();sim.Step();
                auto.SignalAttached=true;auto.Signal=false;Check(!((IItemPipeInventory)auto).TryInsert(BlockId.Potato),"OFF signal rejects automatic deposit");auto.Signal=true;
                Check(((IItemPipeInventory)auto).TryInsert(BlockId.Potato)&&auto.Items.Slots[2].Count==roll&&auto.CompostPoints==1,"Unblocked machine commits saved next yield");
                byte[] Save(int format=SaveStore.Format){using var stream=new MemoryStream();using(var writer=new SaveWriter(stream,format))typeof(IndustrySimulation).GetMethod("WriteSave",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(sim,new object[]{writer});return stream.ToArray();}
                IndustrySimulation Load(byte[] bytes,int format=SaveStore.Format){var loaded=new IndustrySimulation(world,i=>registry.Get(i).stackLimit,processing);using var stream=new MemoryStream(bytes);using var reader=new SaveReader(stream,registry,format);typeof(IndustrySimulation).GetMethod("ReadSave",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(loaded,new object[]{reader});return loaded;}
                var restored=Load(Save()).At(auto.Position);Check(restored.CompostPoints==auto.CompostPoints&&restored.CompostBatches==auto.CompostBatches&&restored.NextCompostYield==auto.NextCompostYield&&restored.Items.Slots[2].Equals(auto.Items.Slots[2]),"Schema 11 preserves points, output and random sequence exactly");
                void Reject(string label,Action mutate,Action reset){mutate();bool rejected=false;try{Load(Save());}catch(System.Reflection.TargetInvocationException e) when(e.InnerException is InvalidDataException){rejected=true;}finally{reset();}Check(rejected,label);}
                Reject("Reject negative level",()=>auto.CompostPoints=-1,()=>auto.CompostPoints=1);
                Reject("Reject uncommitted full level",()=>auto.CompostPoints=24,()=>auto.CompostPoints=1);
                Reject("Reject negative batch sequence",()=>auto.CompostBatches=-1,()=>auto.CompostBatches=1);
                Reject("Reject stale processing time",()=>auto.Work=.5,()=>auto.Work=0);
                Reject("Reject hidden-slot contents",()=>auto.Items.Add(BlockId.Stone,1,1,2),()=>auto.Items.Take(1,64));
                Check(auto.AddCompost(CompostId.Compost,64)==0&&auto.AddCompost(BlockId.Stone,64)==0,"Compost and inert materials cannot fund progress");
                var legacy=New(CompostId.Bin,out sim,out world);legacy.Items.Add(BlockId.Potato,24,0,1);legacy.Items.Add(CompostId.Compost,2,2,3);legacy.Work=80;legacy.WorkInput=BlockId.Potato;
                var migration=Load(Save(10),10);var migrated=migration.At(legacy.Position);
                Check(migrated.Work==0&&migrated.WorkInput==0&&migrated.CompostPoints==0&&migrated.Items.Slots[0].Count==24&&migrated.Items.Slots[2].Count==2,"Schema-10 migration preserves every queued input and finished output while discarding unpaid time");
                sim=migration;var reloaded=Load(Save()).At(legacy.Position);
                Check(reloaded.Items.Slots[0].Equals(migrated.Items.Slots[0])&&reloaded.Items.Slots[2].Equals(migrated.Items.Slots[2]),"Migrated legacy contents survive a new-schema save before interaction");
            }
            foreach(var crop in CropRules.Definitions.Where(c=>c.stages>1))
            {
                int maximum=crop.maxYield*catalog.Points(crop.produce)+crop.seedYield*catalog.Points(crop.planting);
                Check(4*maximum<catalog.pointsPerCompost*(crop.stages-1),"Even maximum random output cannot fund an instant crop loop: "+crop.key);
            }
            var copy=JsonUtility.FromJson<CompostCatalog>(Resources.Load<TextAsset>("Definitions/Compost").text);copy.Validate(registry);copy.inputs[0].points=5;copy.Validate(registry);Check(copy.Points(registry.ResolveId(copy.inputs[0].item))==5,"Non-dividing item values are supported by carry-over");
            copy.inputs[0].points=0;bool invalid=false;try{copy.Validate(registry);}catch(ArgumentException){invalid=true;}Check(invalid&&copy.Points(registry.ResolveId(copy.inputs[0].item))==5,"Invalid catalog cannot partly publish");
            var browser=new RecipeBrowserIndex(registry,crafting,processing);Check(browser.Find(CompostId.Compost,false).Count==catalog.inputs.Length*2,"Both composters list all contribution choices");
            Check(browser.Find(CompostId.Auto,false).Single().Ingredients.Any(i=>i.Id==BlockId.GoldIngot),"Automation upgrade consumes a high-tier material");
            File.WriteAllLines("Logs/Compost/checks.txt",lines);Debug.Log("Compost checks: "+lines.Count+" assertions passed");
        }
    }
}
