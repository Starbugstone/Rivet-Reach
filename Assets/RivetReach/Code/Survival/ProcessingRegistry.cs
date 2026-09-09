using System;
using System.Collections.Generic;

namespace RivetReach
{
    public sealed class ProcessingSpec
    {
        public string Id,Input,Output;
        public int InputCount=1,OutputCount=1,Ticks=200;
    }
    public readonly struct FuelSpec
    {
        public readonly string Item;
        public readonly int Ticks;
        public FuelSpec(string item,int ticks){Item=item;Ticks=ticks;}
    }
    public sealed class ProcessingRecipe
    {
        public string Id {get;}
        public ItemStack Input {get;}
        public ItemStack Output {get;}
        public int Ticks {get;}
        internal ProcessingRecipe(ProcessingSpec spec,byte input,byte output)
        {Id=spec.Id;Input=new ItemStack(input,spec.InputCount);Output=new ItemStack(output,spec.OutputCount);Ticks=spec.Ticks;}
    }
    public sealed class ProcessingRegistry
    {
        readonly ProcessingRecipe[] inputs=new ProcessingRecipe[256];
        readonly int[] fuels=new int[256];
        public IReadOnlyList<ProcessingRecipe> Recipes {get;private set;}
        public ProcessingRecipe Find(byte input)=>inputs[input];
        public int FuelTicks(byte id)=>fuels[id];
        public static ProcessingRegistry Compile(IEnumerable<ProcessingSpec> specs,IEnumerable<FuelSpec> fuelSpecs,Func<string,byte> resolve,Func<byte,int> limit)
        {
            if(specs==null||fuelSpecs==null||resolve==null||limit==null)throw new ArgumentNullException();
            var registry=new ProcessingRegistry();var ids=new HashSet<string>(StringComparer.Ordinal);var list=new List<ProcessingRecipe>();
            foreach(var spec in specs)
            {
                if(spec==null||string.IsNullOrWhiteSpace(spec.Id)||!ids.Add(spec.Id)||spec.Ticks<=0)throw new ArgumentException("Invalid/duplicate furnace recipe identity or duration.");
                byte input=resolve(spec.Input),output=resolve(spec.Output);
                if(input==0||output==0||spec.InputCount<=0||spec.InputCount>limit(input)||spec.OutputCount<=0||spec.OutputCount>limit(output)||registry.inputs[input]!=null)
                    throw new ArgumentException("Invalid or ambiguous furnace recipe: "+spec.Id);
                var recipe=new ProcessingRecipe(spec,input,output);registry.inputs[input]=recipe;list.Add(recipe);
            }
            foreach(var fuel in fuelSpecs)
            {
                byte id=resolve(fuel.Item);
                if(id==0||fuel.Ticks<=0||registry.fuels[id]!=0)throw new ArgumentException("Invalid/duplicate furnace fuel: "+fuel.Item);
                registry.fuels[id]=fuel.Ticks;
            }
            registry.Recipes=list.AsReadOnly();return registry;
        }
    }
}
