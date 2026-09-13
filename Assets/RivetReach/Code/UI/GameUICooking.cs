using System;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        void BuildCooker(Transform parent,MachineState m)
        {
            Label(parent,m.Definition.Name.ToUpperInvariant(),821,115,310,32,23);
            Label(parent,m.Definition.Id==FarmId.Cooker?"Burnable fuel · rear input\nIngredient pipes on every other face":"Electric heat · power and ingredients\non all six faces",821,151,310,50,15,gold);
            MachineButton(parent,live=>"RECIPE: "+live.FoodRecipe.name,821,208,303,38,live=>
            {
                var recipes=CookingCatalog.Current.recipes;int next=(Array.IndexOf(recipes,live.FoodRecipe)+1)%recipes.Length;
                live.SelectCooking(recipes[next].id);nextMachineRefresh=0;
            });
            machineStatus=Label(parent,"",821,251,307,44,14,gold);
            Label(parent,"INGREDIENTS",821,300,205,22,13,gold);Label(parent,"RESULT",1064,300,70,22,13,gold);
            for(int i=0;i<3;i++)Slot(parent,MachineSlotStart+i,821+i*60,327,53);
            Slot(parent,MachineSlotStart+4,1069,327,55);
            var track=Panel(parent,821,397,303,8,slate);machineProgress=Panel(track.transform,0,0,0,8,gold);
            if(m.Definition.Id==FarmId.Cooker){Label(parent,"FUEL",821,426,60,22,13,gold);Slot(parent,MachineSlotStart+3,821,451,53);}
            machineDetail=Label(parent,"",m.Definition.Id==FarmId.Cooker?892:821,426,m.Definition.Id==FarmId.Cooker?232:303,88,14);
            MachineButton(parent,live=>"ROTATE 90°",821,526,146,32,live=>game.Industry.Simulation.Rotate(live));
            if(m.Definition.Id==FarmId.ElectricCooker)MachineButton(parent,live=>"PRIORITY: "+new[]{"HIGH","NORMAL","LOW"}[live.Priority],977,526,147,32,live=>live.Priority=(live.Priority+1)%3);
            nextMachineRefresh=0;
        }
        void RefreshCooker(MachineState m)
        {
            var recipe=m.FoodRecipe;
            string Ingredients()=>string.Join(" + ",recipe.ingredients.Select(i=>i.count+" "+(i.selector.StartsWith("#")?i.selector:game.Registry.Get(game.Registry.ResolveId(i.selector)).displayName)));
            machineStatus.text=Ingredients()+"\n"+StatusName(m.Status);
            machineProgress.rectTransform.sizeDelta=new Vector2(303*(float)(m.Work/recipe.ticks),8);
            machineDetail.text=(m.Definition.Id==FarmId.Cooker?$"Stored heat: {m.BurnTicks/20f:0.#} s":$"Power: {m.ReceivedWatts} / {m.RequestedWatts} W")+$"\nCook time: {recipe.ticks/20f:0.#} s\n"+(m.SignalAttached?"Signal: "+(m.Signal?"ON":"OFF"):"Optional Blue Signal");
        }
    }
}
