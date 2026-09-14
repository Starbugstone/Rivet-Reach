using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    public static class CompostId
    {
        public const byte Compost=236,Bin=242,Auto=243;
        public static bool Added(byte id)=>id==Compost||id==Bin||id==Auto;
        // Frozen additive tag projection for pre-compost checkpoints.
        public static bool OriginalInput(byte id)=>id==BlockId.Leaves||id==BlockId.Sapling||id==BlockId.Apple||id==BlockId.Potato||id==BlockId.BakedPotato||id==FarmId.WheatSeed||id==FarmId.Grain||id==FarmId.FlaxSeed||id==FarmId.Fibre||id==FarmId.CarrotSeed||id==FarmId.Carrot||id==FarmId.BerrySeed||id==FarmId.Berries||id==FarmId.Mushroom||id==FarmId.Bread||id==FarmId.RoastCarrot||id==FarmId.Stew||id==FarmId.CookedMushroom||id==FarmId.Porridge;
    }
    [Serializable] public sealed class CompostInput { public string item;public int points; }
    [Serializable] public sealed class CompostCatalog
    {
        public int pointsPerCompost=24,ticks=200;
        public CompostInput[] inputs;
        int[] contributions;
        static CompostCatalog current;
        public static CompostCatalog Current
        {
            get
            {
                if(current==null){var asset=Resources.Load<TextAsset>("Definitions/Compost");if(asset==null)throw new InvalidOperationException("Missing compost catalog.");var candidate=JsonUtility.FromJson<CompostCatalog>(asset.text);candidate.Validate(ItemRegistry.Load());current=candidate;}
                return current;
            }
        }
        public void Validate(ItemRegistry registry)
        {
            if(pointsPerCompost<1||pointsPerCompost>64||ticks<1||ticks>72000||inputs==null||inputs.Length==0)throw new ArgumentException("Invalid compost catalog.");
            var compiled=new int[256];var seen=new HashSet<string>(StringComparer.Ordinal);
            foreach(var input in inputs)
            {
                if(input==null||!seen.Add(input.item))throw new ArgumentException("Duplicate compost input.");
                var item=registry.Get(registry.ResolveId(input.item));
                if(item==null||item.runtimeId==CompostId.Compost||item.runtimeId==CompostId.Bin||item.runtimeId==CompostId.Auto||!registry.HasTag(item.runtimeId,"compostable")||input.points<1||input.points>pointsPerCompost)throw new ArgumentException("Invalid compost contribution: "+input.item);
                compiled[item.runtimeId]=input.points;
            }
            contributions=compiled;
        }
        public int Points(byte id)=>contributions[id];
        public int BatchCount(byte id)=>Points(id)>0?(pointsPerCompost+Points(id)-1)/Points(id):0;
    }
}
