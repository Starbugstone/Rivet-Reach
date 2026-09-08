using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class CraftingChecks
    {
        static int assertions;
        static readonly List<string> measurements=new List<string>();
        static void Check(bool value,string message)
        {assertions++;if(!value)throw new Exception("Crafting check failed: "+message);}
        static byte Resolve(string id)
        {
            if(!byte.TryParse(id,out byte result)||result==0)throw new ArgumentException("Unknown fixture item "+id);
            return result;
        }
        static int Limit(byte id)=>id>=240?1:500;
        static RecipeIngredient Ingredient(int id,int count=1)=>id==0?default:new RecipeIngredient(id.ToString(),count);
        static RecipeSpec Shape(string id,int width,int height,params int[] cells)
        {
            var inputs=new RecipeIngredient[cells.Length];for(int i=0;i<cells.Length;i++)inputs[i]=Ingredient(cells[i]);
            return new RecipeSpec{Id=id,Width=width,Height=height,Ingredients=inputs,Output=Ingredient(10,4)};
        }
        static RecipeRegistry Compile(params RecipeSpec[] specs)=>RecipeRegistry.Compile(specs,Resolve,Limit);
        static CraftingSession Session(RecipeSpec spec,int size=2)=>new CraftingSession(Compile(spec),size,Limit);
        static void Put(ItemContainer target,int index,int id,int count=1)
        {Check(target.Add((byte)id,count,index,index+1)==0,"Fixture stack fits");}
        static void Clear(ItemContainer target){for(int i=0;i<target.Count;i++)target.Take(i,int.MaxValue);}
        static void Reject(Action action,string message)
        {
            bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}
            Check(rejected,message);
        }

        [MenuItem("Rivet Reach/Validate crafting and benchmark")]
        public static void Run()
        {
            assertions=0;measurements.Clear();
            Layouts();Shapeless();Transactions();Validation();RandomizedConservation();Catalog();Benchmark();
            Directory.CreateDirectory("Logs");
            string report=$"PASS: {assertions} crafting assertions. UTC {DateTime.UtcNow:O}; Unity {Application.unityVersion}; {SystemInfo.processorType}\n"+string.Join("\n",measurements);
            File.WriteAllText("Logs/crafting-checks.txt",report);UnityEngine.Debug.Log(report);
        }

        static void Layouts()
        {
            var spec=Shape("elbow",2,2,1,2,0,3);spec.Mirror=true;
            var registry=Compile(spec);
            for(int size=2;size<=4;size++)
            for(int y=0;y<=size-2;y++)for(int x=0;x<=size-2;x++)for(int mirror=0;mirror<2;mirror++)
            {
                var session=new CraftingSession(registry,size,Limit);var grid=session.Grid;
                Put(grid,y*size+x,mirror==0?1:2,5);Put(grid,y*size+x+1,mirror==0?2:1,7);
                Put(grid,(y+1)*size+x+(mirror==0?1:0),3,3);
                Check(session.Preview?.Id=="elbow"&&session.MaximumCrafts==3,"Translated/mirrored shape matches in "+size);
                var held=default(ItemStack);Check(session.CraftToCursor(ref held).Succeeded&&held.Count==4,"Shape output");
                Check(grid.Total(1)==(mirror==0?4:6)&&grid.Total(2)==(mirror==0?6:4)&&grid.Total(3)==2,"Correct mirrored source cells consumed");
                Put(grid,(y+1)*size+x+(mirror==0?0:1),4);
                Check(session.Preview==null,"Inner hole must stay empty");
            }
            for(int size=3;size<=4;size++)
            {
                var grid=new CraftingGrid(size,Limit);Put(grid,0,1);Put(grid,1,2);Put(grid,size+1,3);Put(grid,size*size-1,4);
                Check(!registry.TryMatch(grid,new int[16],out _,out _),"Extra outside item rejects recipe");
            }
            var fixedShape=Shape("fixed",2,2,1,2,0,3);var fixedSession=Session(fixedShape);
            Put(fixedSession.Grid,0,2);Put(fixedSession.Grid,1,1);Put(fixedSession.Grid,2,3);
            Check(fixedSession.Preview==null,"Mirroring is opt-in");Clear(fixedSession.Grid);
            Put(fixedSession.Grid,1,1);Put(fixedSession.Grid,2,3);Put(fixedSession.Grid,3,2);
            Check(fixedSession.Preview==null,"Rotation is not implicit");
            var padded=Shape("padded",3,3,0,0,0,0,1,0,0,0,0);var trimmed=Session(padded);
            Put(trimmed.Grid,3,1);Check(trimmed.Preview?.Id=="padded","Blank authoring borders normalize");
            foreach(int requiredSize in new[]{3,4})
            {
                var station=Shape("gate",1,1,1);station.MinimumGridSize=requiredSize;
                for(int actual=2;actual<=4;actual++)
                {var session=Session(station,actual);Put(session.Grid,0,1);Check((session.Preview!=null)==(actual>=requiredSize),"Explicit grid gate");}
                var cells=new int[requiredSize*requiredSize];for(int i=0;i<cells.Length;i++)cells[i]=i+1;
                var large=Shape("large",requiredSize,requiredSize,cells);
                for(int actual=2;actual<=4;actual++)
                {
                    var session=Session(large,actual);
                    if(actual>=requiredSize)for(int y=0;y<requiredSize;y++)for(int x=0;x<requiredSize;x++)Put(session.Grid,y*actual+x,1+y*requiredSize+x);
                    else Put(session.Grid,0,1);
                    Check((session.Preview!=null)==(actual>=requiredSize),"3x3/4x4 dimensions cannot fit smaller grids");
                }
            }
            var snapshot=Shape("snapshot",1,1,1);var immutable=Session(snapshot);snapshot.Id="changed";snapshot.Ingredients[0]=Ingredient(2);snapshot.Output=Ingredient(3);
            Put(immutable.Grid,0,1);Check(immutable.Preview?.Id=="snapshot"&&immutable.Preview.Output.Id==10,"Compiled registry owns immutable copies");
        }

        static void Shapeless()
        {
            var spec=Shape("mixed",1,1,1,1,2);spec.Kind=RecipeKind.Shapeless;
            spec.Ingredients[0]=Ingredient(1,2);spec.Ingredients[1]=Ingredient(1,4);
            for(int size=2;size<=4;size++)
            for(int a=0;a<size*size;a++)for(int b=0;b<size*size;b++)
            {
                if(a==b)continue;int c=0;while(c==a||c==b)c++;
                var session=Session(spec,size);Put(session.Grid,a,1,12);Put(session.Grid,b,1,4);Put(session.Grid,c,2,9);
                Check(session.MaximumCrafts==2,"Shapeless counts pair deterministically with duplicate items");
                var destination=new Inventory(Limit);Check(session.CraftToInventory(destination).Crafts==2,"Shapeless maximum batch");
                Check(session.Grid.Total(1)==4&&session.Grid.Total(2)==7&&destination.Total(10)==8,"Shapeless quantity conservation");
            }
            var aggregated=Session(spec);Put(aggregated.Grid,0,1,6);Put(aggregated.Grid,1,2,1);
            Check(aggregated.Preview==null,"Repeated ingredient entries require separate cells");
            Put(aggregated.Grid,2,1,1);Check(aggregated.Preview==null,"Insufficient per-cell quantity rejects");
            var sixteen=Shape("sixteen",1,1,new int[16]);sixteen.Kind=RecipeKind.Shapeless;
            for(int i=0;i<16;i++)sixteen.Ingredients[i]=Ingredient(i+1);
            var full=Session(sixteen,4);for(int i=0;i<16;i++)Put(full.Grid,i,16-i);
            Check(full.Preview?.MinimumGridSize==4,"Shapeless sixteen-slot recipe matches full 4x4");
        }

        static void Transactions()
        {
            var spec=Shape("bundle",1,1,1);var session=Session(spec);Put(session.Grid,0,1,10);
            var cursor=new ItemStack(2,1);long revision=session.Grid.Revision;
            Check(session.CraftToCursor(ref cursor).Status==CraftStatus.CursorOccupied&&session.Grid.Revision==revision,"Incompatible cursor consumes nothing");
            cursor=new ItemStack(10,498);
            Check(session.CraftToCursor(ref cursor).Status==CraftStatus.OutputFull&&cursor.Count==498&&session.Grid.Revision==revision,"Partial output never consumed or emitted");
            cursor=new ItemStack(10,496);Check(session.CraftToCursor(ref cursor).Succeeded&&cursor.Count==500&&session.Grid.Total(1)==9,"Exact cursor fit");
            var inventory=new Inventory(Limit);inventory.Add(2,30000);revision=session.Grid.Revision;
            Check(session.CraftToInventory(inventory).Status==CraftStatus.OutputFull&&session.Grid.Revision==revision,"Full inventory consumes nothing");
            inventory.Take(0,500);Put(inventory,0,10,493);
            var result=session.CraftToInventory(inventory);
            Check(result.Crafts==1&&inventory.Total(10)==497&&session.Grid.Total(1)==8,"Batch leaves unusable partial-output capacity");
            inventory.Take(0,1);
            Check(session.CraftToInventory(inventory,1).Crafts==1&&inventory.Total(10)==500,"Requested batch count honored");
            Check(session.CraftToInventory(inventory,0).Status==CraftStatus.InvalidRequest,"Zero batch rejected");
            Check(session.Preview!=null,"Preview present before external change");session.Grid.Take(0,500);cursor=default;
            Check(session.CraftToCursor(ref cursor).Status==CraftStatus.NoRecipe&&cursor.Empty,"Stale preview cannot craft after removal");
            Put(session.Grid,0,2,5);Check(session.Preview==null,"Changing input identity invalidates preview");Clear(session.Grid);Put(session.Grid,0,1,3);
            session.ReturnIngredients(inventory);Check(session.Grid.Total(1)==3,"Closing into full inventory retains ingredients");
            inventory.Take(1,500);session.ReturnIngredients(inventory);Check(session.Grid.Total(1)==0&&inventory.Total(1)==3,"Retrying return transfers remaining ingredients exactly once");
            session.ReturnIngredients(inventory);Check(inventory.Total(1)==3,"Repeated return is idempotent");
            var tool=Shape("tool",1,1,1);tool.Output=Ingredient(240);var tools=Session(tool);Put(tools.Grid,0,1,100);
            var toolBag=new Inventory(Limit);toolBag.Add(2,29000);
            Check(tools.CraftToInventory(toolBag).Crafts==2&&toolBag.Total(240)==2&&tools.Grid.Total(1)==98,"Unstackable tools use one destination slot each");
            var large=Shape("largecounts",1,1,1);large.Ingredients[0]=Ingredient(1,500);large.Output=Ingredient(10,500);
            var largeSession=Session(large);Put(largeSession.Grid,0,1,500);cursor=default;
            Check(largeSession.CraftToCursor(ref cursor).Succeeded&&cursor.Count==500&&largeSession.Grid.Total(1)==0,"Stack-limit-sized input/output");
            // Inputs and outputs may share an identity without gaining extra output capacity from the grid.
            var same=Shape("same",1,1,1);same.Ingredients[0]=Ingredient(1,2);same.Output=Ingredient(1);
            var sameSession=Session(same);Put(sameSession.Grid,0,1,5);var sameBag=new Inventory(Limit);
            Check(sameSession.CraftToInventory(sameBag).Crafts==2&&sameSession.Grid.Total(1)==1&&sameBag.Total(1)==2,"Same-item input/output accounting");
        }

        static void Validation()
        {
            Reject(()=>Compile(Shape("bad",2,2,1)),"Malformed shaped dimensions");
            Reject(()=>Compile(Shape("empty",1,1,0)),"Free recipe rejected");
            var unknown=Shape("unknown",1,1,1);unknown.Ingredients[0]=new RecipeIngredient("missing");Reject(()=>Compile(unknown),"Unknown stable item ID");
            var zero=Shape("zero",1,1,1);zero.Ingredients[0]=Ingredient(1,0);Reject(()=>Compile(zero),"Zero ingredient count");
            var negative=Shape("negative",1,1,1);negative.Output=Ingredient(10,-1);Reject(()=>Compile(negative),"Negative output count");
            var tooMany=Shape("overstack",1,1,1);tooMany.Output=Ingredient(240,2);Reject(()=>Compile(tooMany),"Output exceeds item stack limit");
            var input=Shape("input",1,1,1);input.Ingredients[0]=Ingredient(1,501);Reject(()=>Compile(input),"Ingredient exceeds item stack limit");
            var badGate=Shape("gate",1,1,1);badGate.MinimumGridSize=5;Reject(()=>Compile(badGate),"Unsupported station size");
            var id=Shape("duplicate",1,1,1);Reject(()=>Compile(id,Shape("duplicate",1,1,2)),"Duplicate stable recipe ID");
            Reject(()=>Compile(id,Shape("overlap",2,1,0,1)),"Equivalent padded pattern rejected");
            var quantity=Shape("quantity",1,1,1);quantity.Ingredients[0]=Ingredient(1,2);Reject(()=>Compile(id,quantity),"Quantity variants cannot hide ambiguous shapes");
            var mirror=Shape("mirror",2,1,1,2);mirror.Mirror=true;Reject(()=>Compile(mirror,Shape("inverse",2,1,2,1)),"Mirrored collision rejected");
            var bag=Shape("bag",1,1,1,2);bag.Kind=RecipeKind.Shapeless;
            Reject(()=>Compile(mirror,bag),"Shaped/shapeless overlap rejected");Reject(()=>Compile(bag,mirror),"Ambiguity rejection independent of asset order");
            var bag2=Shape("bag2",1,1,2,1);bag2.Kind=RecipeKind.Shapeless;Reject(()=>Compile(bag,bag2),"Permuted shapeless duplicate rejected");
            var blank=Shape("blank",1,1,0);blank.Ingredients[0]=new RecipeIngredient("",1);Reject(()=>Compile(blank),"Malformed empty cell rejected");
            var symmetric=Shape("symmetric",2,1,1,1);symmetric.Mirror=true;symmetric.Ingredients[1]=Ingredient(1,2);Reject(()=>Compile(symmetric),"Ambiguous symmetric counts rejected");
            Reject(()=>new CraftingGrid(1,Limit),"Grid lower bound");Reject(()=>new CraftingGrid(5,Limit),"Grid upper bound");
            var inventory=new Inventory(Limit);Reject(()=>inventory.Add(1,-1),"Negative additions cannot corrupt storage");
            Reject(()=>inventory.Add(0,1),"Air is never stored as an item");Reject(()=>inventory.Add(1,1,0,61),"Invalid range rejected before writes");
            Check(inventory.Slots is not ItemStack[],"Backing array is not exposed");
            var held=new ItemStack(1,501);Reject(()=>inventory.Click(0,ref held,false),"Over-limit cursor rejected before writes");
            Check(inventory.Total(1)==0&&inventory.Revision==0,"Rejected mutations leave inventory unchanged");
            inventory.Add(2,30000);inventory.Take(0,500);Put(inventory,0,10,498);
            long revision=inventory.Revision;
            Check(!inventory.TryAddExact(10,4)&&inventory.Revision==revision&&inventory.Total(10)==498,"Exact insertion never partially writes");
        }

        static void RandomizedConservation()
        {
            var spec=Shape("conservation",1,1,1);spec.Kind=RecipeKind.Shapeless;
            var session=Session(spec,4);var inventory=new Inventory(Limit);inventory.Add(1,12000);
            var held=default(ItemStack);var random=new System.Random(83721);
            for(int step=0;step<15000;step++)
            {
                switch(random.Next(7))
                {
                    case 0:inventory.Click(random.Next(60),ref held,random.Next(2)==0);break;
                    case 1:session.Grid.Click(random.Next(16),ref held,random.Next(2)==0);break;
                    case 2:session.CraftToCursor(ref held);break;
                    case 3:session.CraftToInventory(inventory,random.Next(1,200));break;
                    case 4:session.Grid.TransferTo(random.Next(16),inventory);break;
                    case 5:session.ReturnIngredients(inventory);break;
                    case 6:inventory.QuickTransfer(random.Next(60));break;
                }
                int raw=inventory.Total(1)+session.Grid.Total(1)+(held.Id==1?held.Count:0);
                int output=inventory.Total(10)+session.Grid.Total(10)+(held.Id==10?held.Count:0);
                Check(raw*4+output==48000,"Random craft/cursor/transfer/close resource conservation");
                foreach(var stack in inventory.Slots)Check(stack.Count>=0&&stack.Count<=500,"Inventory remains bounded");
                foreach(var stack in session.Grid.Slots)Check(stack.Count>=0&&stack.Count<=500,"Grid remains bounded");
            }
        }

        static void Catalog()
        {
            var items=ItemRegistry.Load();var source=RecipeCatalogAsset.Load();var catalog=source.Compile(items);
            Check(catalog.Recipes.Count==source.recipes.Length,"Every registered asset compiles without a hard-coded recipe count");
            foreach(var recipe in catalog.Recipes)
            {
                var session=new CraftingSession(catalog,recipe.MinimumGridSize,id=>items.Get(id).stackLimit);
                for(int i=0;i<recipe.Ingredients.Count;i++)
                {
                    var ingredient=recipe.Ingredients[i];if(ingredient.Empty)continue;
                    int cell=recipe.Kind==RecipeKind.Shaped?i/recipe.Width*session.Grid.Size+i%recipe.Width:i;
                    Put(session.Grid,cell,ingredient.Id,ingredient.Count);
                }
                Check(session.Preview?.Id==recipe.Id,"Authored layout resolves to its own compiled recipe: "+recipe.Id);
                var inventory=new Inventory(id=>items.Get(id).stackLimit);
                Check(session.CraftToInventory(inventory,1).Crafts==1&&inventory.Total(recipe.Output.Id)==recipe.Output.Count,"Authored quantities execute: "+recipe.Id);
                foreach(var ingredient in session.Grid.Slots)Check(ingredient.Empty,"Authored single-craft ingredients consumed exactly");
            }
        }

        static void Benchmark()
        {
            foreach(int count in new[]{10,1000,10000})
            {
                var specs=new RecipeSpec[count];
                for(int i=0;i<count;i++)
                {
                    var cells=new int[16];cells[0]=1;cells[15]=2;
                    int code=i;for(int digit=0;digit<4;digit++){cells[digit+1]=3+code%100;code/=100;}
                    specs[i]=Shape("benchmark:"+i,4,4,cells);
                }
                var timer=Stopwatch.StartNew();var registry=Compile(specs);timer.Stop();double compileMs=timer.Elapsed.TotalMilliseconds;
                var grid=new CraftingGrid(4,Limit);
                for(int i=0;i<16;i++)if(specs[count-1].Ingredients[i].Count>0)grid.Add(Resolve(specs[count-1].Ingredients[i].ItemId),1,i,i+1);
                var consume=new int[16];
                for(int i=0;i<10000;i++)registry.TryMatch(grid,consume,out _,out _);
                const int iterations=100000;
                long before=GC.GetAllocatedBytesForCurrentThread();timer.Restart();int hits=0;
                for(int i=0;i<iterations;i++)if(registry.TryMatch(grid,consume,out _,out _))hits++;
                timer.Stop();long allocation=GC.GetAllocatedBytesForCurrentThread()-before;
                Check(hits==iterations&&allocation==0,"Matched 4x4 lookup allocates zero bytes for "+count+" recipes");
                double hitUs=timer.Elapsed.TotalMilliseconds*1000/iterations;
                grid.Take(15,1);grid.Add(200,1,15,16);
                for(int i=0;i<10000;i++)registry.TryMatch(grid,consume,out _,out _);
                before=GC.GetAllocatedBytesForCurrentThread();timer.Restart();hits=0;
                for(int i=0;i<iterations;i++)if(registry.TryMatch(grid,consume,out _,out _))hits++;
                timer.Stop();allocation=GC.GetAllocatedBytesForCurrentThread()-before;
                Check(hits==0&&allocation==0,"Missing 4x4 lookup allocates zero bytes for "+count+" recipes");
                measurements.Add($"{count} recipes; compile {compileMs:F3} ms; 4x4 hit {hitUs:F3} us, miss {timer.Elapsed.TotalMilliseconds*1000/iterations:F3} us; {iterations} iterations each; {allocation} bytes per loop after warmup.");
            }
            var session=Session(Shape("cache",1,1,1));Put(session.Grid,0,1,500);var expected=session.Preview;
            var watch=new Stopwatch();long start=GC.GetAllocatedBytesForCurrentThread();watch.Start();int matched=0;
            for(int i=0;i<1000000;i++)if(ReferenceEquals(session.Preview,expected))matched++;
            watch.Stop();long allocated=GC.GetAllocatedBytesForCurrentThread()-start;
            Check(matched==1000000&&allocated==0,"Unchanged previews allocate zero bytes");
            measurements.Add($"Cached preview: {watch.Elapsed.TotalMilliseconds:F3} ms / 1,000,000 reads, {allocated} bytes. Editor Mono, isolated synchronous workload; not whole-game frame timings.");
        }
    }
}
