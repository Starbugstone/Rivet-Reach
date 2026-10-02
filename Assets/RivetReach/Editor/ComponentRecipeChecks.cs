using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    // Independent issue #1 component expectations and exact historical-content checks.
    public static class ComponentRecipeChecks
    {
        public static void VerifyAndBuild()
        {
            Directory.CreateDirectory("Logs");
            Run();DomainChecks.Run();SurvivalChecks.Run();SaveCompatibilityChecks.Run();
            WikiExport.Export();RecipeBrowserBuild.Run();
        }
        public static void Run()
        {
            var lines=new List<string>();
            void Check(bool ok,string message){if(!ok)throw new Exception("Component recipes: "+message);lines.Add("PASS "+message);}
            var items=ItemRegistry.Load();var catalog=RecipeCatalogAsset.Load();var registry=catalog.Compile(items);
            int Limit(byte id)=>items.Get(id).stackLimit;
            foreach(var expected in new[]{(input:BlockId.IronIngot,output:IndustryId.Cog,count:1),(input:IndustryId.IronPlate,output:IndustryId.Rivets,count:4)})
            {
                foreach(int size in new[]{2,3,4})for(int slot=0;slot<size*size;slot++)
                {
                    var session=new CraftingSession(registry,size,Limit);session.Grid.Add(expected.input,1,slot,slot+1);
                    if(size<4){Check(session.Preview==null,"Component needs the Machinist's Bench, grid "+size+" slot "+slot);continue;}
                    Check(session.Preview?.Output.Id==expected.output&&session.Preview.Output.Count==expected.count,"Independent component output at slot "+slot);
                    ItemStack cursor=default;
                    Check(session.CraftToCursor(ref cursor).Succeeded&&cursor.Id==expected.output&&cursor.Count==expected.count&&session.Grid.Slots.All(s=>s.Empty),"Exact component consumption at slot "+slot);
                    Check(!session.CraftToCursor(ref cursor).Succeeded&&cursor.Count==expected.count,"No second output after consumption");
                }
                var full=new CraftingSession(registry,4,Limit);full.Grid.Add(expected.input,1,0,1);var blocked=new ItemStack(expected.output,Limit(expected.output));
                Check(!full.CraftToCursor(ref blocked).Succeeded&&full.Grid.Slots[0].Count==1,"Full cursor preserves ingredient");
            }
            var obsolete=new CraftingSession(registry,4,Limit);obsolete.Grid.Add(BlockId.IronIngot,1,0,1);obsolete.Grid.Add(BlockId.IronIngot,1,1,2);
            Check(obsolete.Preview==null,"Old two-ingot Cog layout is no longer registered");
            var glass=ProcessingCatalogAsset.Load().Compile(items).Recipes.Single(r=>r.Input.Id==BlockId.Sand);
            Check(glass.Input.Count==1&&glass.Output.Id==IndustryId.Glass&&glass.Output.Count==1,"One Sand smelts into one Glass component");
            Check(!BlockId.Placeable(IndustryId.Glass)&&BlockId.Placeable(IndustryId.TankGlass),"Glass remains a component; reinforced tank glass supplies placed industrial windows");
            var furnace=new FurnaceState(ProcessingCatalogAsset.Load().Compile(items),Limit);
            var sand=new ItemStack(BlockId.Sand,1);furnace.Click(0,ref sand,false);var coal=new ItemStack(BlockId.Coal,1);furnace.Click(1,ref coal,false);
            furnace.Advance(glass.Ticks);Check(sand.Empty&&coal.Empty&&furnace.Slots[0].Empty&&furnace.Slots[2].Id==IndustryId.Glass&&furnace.Slots[2].Count==1,"Real furnace transaction produces Glass");

            var cog=catalog.recipes.Single(r=>r.stableId=="rivet:industry_125");var rivets=catalog.recipes.Single(r=>r.stableId=="rivet:industry_126");
            var planks=catalog.recipes.Single(r=>r.stableId=="rivet:craft_planks");
            string cogJson=JsonUtility.ToJson(cog),rivetJson=JsonUtility.ToJson(rivets),plankJson=JsonUtility.ToJson(planks);
            var identity=new SaveEntry{Id=Guid.NewGuid().ToString("N"),WorldId=Guid.NewGuid().ToString("N"),Name="Component contract",Seed=18,UtcTicks=DateTime.UtcNow.Ticks,GeneratorVersion=TerrainGenerator.Version};
            var current=new SaveStore("unused",items);byte[] oldBytes;
            void Payload(SaveWriter writer){writer.Stack(new ItemStack(IndustryId.Cog,17));writer.Stack(new ItemStack(IndustryId.Rivets,31));}
            void ReadExact(SaveStore store,byte[] bytes)
            {
                using var reader=store.Open(bytes,out _);var a=reader.Stack();var b=reader.Stack();
                Check(reader.Format==18&&a.Id==IndustryId.Cog&&a.Count==17&&b.Id==IndustryId.Rivets&&b.Count==31,"Schema 18 retains exact existing component stacks");
            }
            try
            {
                ReadExact(current,current.Encode(identity,Payload));
                // Recreate the exact former recipe definitions, not a bypassed hash.
                // This is a synthetic schema-18 envelope; pinned schemas 1–17 are
                // independently exercised by SaveCompatibilityChecks.
                cog.kind=RecipeKind.Shaped;cog.width=2;cog.ingredients=new[]{new RecipeCellData{itemId="rivet:iron_ingot",count=1},new RecipeCellData{itemId="rivet:iron_ingot",count=1}};
                rivets.ingredients=new[]{new RecipeCellData{itemId="rivet:iron_ingot",count=1}};rivets.output.count=8;
                oldBytes=new SaveStore("unused",items).Encode(identity,Payload);
                JsonUtility.FromJsonOverwrite(cogJson,cog);JsonUtility.FromJsonOverwrite(rivetJson,rivets);
                ReadExact(current,oldBytes);
                void Reject(Action mutate,RecipeAsset recipe,string original,string label)
                {
                    try
                    {
                        mutate();bool rejected=false;
                        try{using var reader=new SaveStore("unused",items).Open(oldBytes,out _);}
                        catch(InvalidDataException){rejected=true;}
                        Check(rejected,"Historical component compatibility rejects "+label);
                    }
                    finally{JsonUtility.FromJsonOverwrite(original,recipe);}
                }
                Reject(()=>cog.minimumGridSize=3,cog,cogJson,"changed station gate");
                Reject(()=>cog.output.count=2,cog,cogJson,"changed Cog yield");
                Reject(()=>cog.mirror=true,cog,cogJson,"changed mirror flag");
                Reject(()=>cog.ingredients[0].count=2,cog,cogJson,"changed Cog input quantity");
                Reject(()=>rivets.ingredients[0].itemId="rivet:copper_plate",rivets,rivetJson,"changed Rivet material");
                Reject(()=>rivets.output.count=5,rivets,rivetJson,"changed Rivet yield");
                Reject(()=>planks.output.count=5,planks,plankJson,"unrelated starter recipe change");
            }
            finally{JsonUtility.FromJsonOverwrite(cogJson,cog);JsonUtility.FromJsonOverwrite(rivetJson,rivets);JsonUtility.FromJsonOverwrite(plankJson,planks);}
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/component-recipe-checks.txt",lines);
            Debug.Log("PASS "+lines.Count+" component recipe and compatibility assertions");
        }
    }
}
