using System;
using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    [Serializable] public sealed class FoodReserve { public string item;public int saturation; }
    [Serializable] public sealed class FoodBalanceCatalog
    {
        public FoodReserve[] foods;
        Dictionary<byte,int> reserves;
        static FoodBalanceCatalog cached;
        public static FoodBalanceCatalog Current=>cached??=Load();
        static FoodBalanceCatalog Load()
        {
            var asset=Resources.Load<TextAsset>("Definitions/FoodBalance");
            if(asset==null)throw new InvalidOperationException("Missing food balance configuration.");
            var catalog=JsonUtility.FromJson<FoodBalanceCatalog>(asset.text)??throw new ArgumentException("Invalid food balance configuration.");
            catalog.Validate(ItemRegistry.Load());return catalog;
        }
        public void Validate(ItemRegistry items)
        {
            if(foods==null||foods.Length>255)throw new ArgumentException("Invalid food reserve list.");
            var compiled=new Dictionary<byte,int>();
            foreach(var food in foods)
            {
                if(food==null||string.IsNullOrWhiteSpace(food.item)||food.saturation<0||food.saturation>HungerState.Maximum)throw new ArgumentException("Invalid food reserve.");
                byte id=items.ResolveId(food.item);
                if(items.FoodPoints(id)<=0||!compiled.TryAdd(id,food.saturation))throw new ArgumentException("Food reserve requires a unique edible item.");
            }
            reserves=compiled;
        }
        public int Saturation(byte item)=>reserves.TryGetValue(item,out int value)?value:0;
    }
}
