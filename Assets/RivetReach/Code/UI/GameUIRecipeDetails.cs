using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        sealed class RecipeWidgets
        {
            public Button back,previous,next,recipes,uses,fill;
            public Text title,page,station,role,output,method,materials,footer,empty,transfer;
            public BrowserItemView stationIcon,result,emptyIcon;
            public RectTransform content,viewport,arrow;
            public ScrollRect scroll;
            public readonly List<BrowserItemView> cells=new List<BrowserItemView>(),totals=new List<BrowserItemView>(),fuels=new List<BrowserItemView>();
            public readonly List<Text> totalNames=new List<Text>();
        }
        RecipeWidgets recipeWidgets;
        BrowserRecipe shownRecipe;
        void AddMissingIngredientBorder(BrowserItemView view)
        {
            float size=((RectTransform)view.transform).sizeDelta.x;
            var border=Rect(view.transform,"Missing ingredient border",0,0,size,size);
            var color=new Color(1f,.32f,.25f,1f);
            Panel(border,0,0,size,3,color).raycastTarget=false;
            Panel(border,0,size-3,size,3,color).raycastTarget=false;
            Panel(border,0,3,3,size-6,color).raycastTarget=false;
            Panel(border,size-3,3,3,size-6,color).raycastTarget=false;
            view.MissingBorder=border.gameObject;view.MissingBorder.SetActive(false);
        }
        void RefreshMissingIngredients()
        {
            if(!RecipeVisible||shownRecipe==null)return;
            if(shownRecipe.FoodRecipe!=null)
            {
                foreach(var v in recipeWidgets.cells)v.MissingBorder.SetActive(false);
                foreach(var v in recipeWidgets.totals)v.MissingBorder.SetActive(false);
                recipeWidgets.transfer.text="Pictured ingredients are examples; choose any listed tag member.";return;
            }
            // The fill action uses the backpack and existing grid, never the held cursor.
            // Aggregate repeated ingredients so a partial supply cannot satisfy every cell.
            var missing=new HashSet<byte>();var w=recipeWidgets;
            foreach(var group in shownRecipe.Ingredients.Where(s=>!s.Empty).GroupBy(s=>s.Id))
            {
                int required=group.Sum(s=>s.Count);
                int available=game.Inventory.Total(group.Key)+game.Crafting.Grid.Total(group.Key);
                bool shortfall=available<required;
                if(shortfall)missing.Add(group.Key);
                int index=w.totals.FindIndex(v=>v.gameObject.activeSelf&&v.Item==group.Key);
                if(index>=0)w.totalNames[index].text=required+" × "+game.Registry.Get(group.Key).displayName+
                    (shortfall?" · missing "+(required-available):"");
            }
            foreach(var cell in w.cells)cell.MissingBorder.SetActive(missing.Contains(cell.Item));
            foreach(var total in w.totals)total.MissingBorder.SetActive(missing.Contains(total.Item));
            w.transfer.text=missing.Count>0?"Missing ingredients outlined in red.":shownRecipe.GridRecipe!=null?"Ingredients available · Fill grid to continue.":"";
        }
        void BuildRecipeDetails()
        {
            var panel=Panel(browserHost,246,104,688,418,new Color(ink.r,ink.g,ink.b,1));recipePanel=panel.gameObject;recipePanel.name="Recipe detail";recipePanel.SetActive(false);
            var w=recipeWidgets=new RecipeWidgets();
            Panel(panel.transform,0,0,688,3,gold);
            w.back=Button(panel.transform,"‹ BACK",12,14,98,32,()=>
            {if(browserHistory.Count==0)return;var prior=browserHistory.Pop();browserItem=prior.item;browserUses=prior.uses;recipePage=prior.page;DrawBrowserRecipe();});
            w.title=Label(panel.transform,"",124,15,424,30,22);
            Button(panel.transform,"DONE",578,14,98,32,CloseBrowserRecipe);
            w.recipes=Button(panel.transform,"RECIPES",12,58,130,34,()=>{browserUses=false;recipePage=0;DrawBrowserRecipe();});
            w.uses=Button(panel.transform,"USES",150,58,130,34,()=>{browserUses=true;recipePage=0;DrawBrowserRecipe();});
            w.previous=Button(panel.transform,"‹",514,58,36,34,()=>{recipePage--;DrawBrowserRecipe();});
            w.next=Button(panel.transform,"›",640,58,36,34,()=>{recipePage++;DrawBrowserRecipe();});
            w.page=Label(panel.transform,"",555,65,82,26,15,gold);w.page.alignment=TextAnchor.UpperCenter;
            w.fill=Button(panel.transform,"FILL GRID",294,58,204,34,()=>{if(shownRecipe!=null)FillBrowserRecipe(shownRecipe.Output.Id,shownRecipe.Id,Keyboard.current?.shiftKey.isPressed==true);});
            w.transfer=recipeTransferStatus=Label(panel.transform,"",16,140,658,17,12,gold);
            w.stationIcon=BrowserIcon(panel.transform,default,16,106,34);
            w.station=Label(panel.transform,"",16,111,445,26,18,gold);
            w.role=Label(panel.transform,"",410,116,260,26,13,gold);
            for(int i=0;i<16;i++){var cell=BrowserIcon(panel.transform,default,24+i%4*42,158+i/4*42,38);AddMissingIngredientBorder(cell);w.cells.Add(cell);}
            w.arrow=Label(panel.transform,"→",207,156,45,45,32,gold).rectTransform;
            w.result=BrowserIcon(panel.transform,default,261,156,60);
            w.output=Label(panel.transform,"",232,156,130,50,16,gold);w.output.alignment=TextAnchor.UpperCenter;
            w.method=Label(panel.transform,"",24,334,640,24,14,gold);
            w.materials=Label(panel.transform,"MATERIALS / CRAFT",386,155,276,24,13,gold);
            var viewport=Panel(panel.transform,382,185,286,138,ink);w.viewport=viewport.rectTransform;viewport.gameObject.AddComponent<RectMask2D>();
            w.content=Rect(viewport.transform,"Material totals",0,0,282,138);
            w.scroll=viewport.gameObject.AddComponent<ScrollRect>();w.scroll.viewport=w.viewport;w.scroll.content=w.content;w.scroll.horizontal=false;w.scroll.movementType=ScrollRect.MovementType.Clamped;w.scroll.scrollSensitivity=25;
            // Capacity follows the immutable catalog, never the number of navigation visits.
            int totalCapacity=browserIndex.Recipes.Max(r=>r.Ingredients.Where(s=>!s.Empty).Select(s=>s.Id).Distinct().Count());
            for(int i=0;i<totalCapacity;i++)
            {var total=BrowserIcon(w.content,default,2,i*38,32);AddMissingIngredientBorder(total);w.totals.Add(total);w.totalNames.Add(Label(w.content,"",42,i*38+6,234,30,14));}
            int fuelCapacity=browserIndex.Recipes.Max(r=>r.Fuels.Count);
            for(int i=0;i<fuelCapacity;i++)w.fuels.Add(BrowserIcon(panel.transform,default,190+i*36,369,32));
            w.footer=Label(panel.transform,"",24,380,640,24,13,gold);
            w.emptyIcon=BrowserIcon(panel.transform,default,30,130,64);
            w.empty=Label(panel.transform,"",116,134,532,110,19);
        }
        void BindBrowserIcon(BrowserItemView view,ItemStack stack)
        {
            if(view.MissingBorder!=null)view.MissingBorder.SetActive(false);
            view.Item=stack.Id;view.RecipeId=null;view.Icon.enabled=!stack.Empty;
            view.Icon.texture=stack.Empty?null:BrowserTexture(stack.Id);
            view.CountLabel.text=stack.Count>1?stack.Count.ToString():"";
            view.gameObject.name=stack.Empty?"Empty ingredient":"Inspect "+game.Registry.Get(stack.Id).stableId;
        }
        void BindRecipeDetails()
        {
            // Retained widgets can be rebound during a pointer gesture. Invalidate that gesture.
            if(!constructing){BindingVersion++;EndRightPaint();creativeDrag=default;}
            if(recipeWidgets==null)BuildRecipeDetails();
            recipePanel.SetActive(false);var w=recipeWidgets;
            HoverBrowserItem(0);hoveredSlot=-1;RefreshTooltip();
            var matches=browserIndex.Find(browserItem,browserUses);recipePage=Mathf.Clamp(recipePage,0,Math.Max(0,matches.Count-1));
            shownRecipe=matches.Count==0?null:matches[recipePage];
            w.title.text=game.Registry.Get(browserItem).displayName;
            w.back.interactable=browserHistory.Count>0;w.previous.interactable=recipePage>0;w.next.interactable=recipePage+1<matches.Count;
            w.page.text=matches.Count==0?"0 / 0":$"{recipePage+1} / {matches.Count}";
            w.recipes.targetGraphic.color=!browserUses?new Color(.29f,.45f,.43f):slate;
            w.uses.targetGraphic.color=browserUses?new Color(.29f,.45f,.43f):slate;
            recipeTransferStatus=w.transfer;
            recipeTransferStatus.text="";
            bool any=shownRecipe!=null;
            w.fill.gameObject.SetActive(shownRecipe?.GridRecipe!=null);
            w.station.gameObject.SetActive(any);w.role.gameObject.SetActive(any);w.arrow.gameObject.SetActive(any);w.output.gameObject.SetActive(any);
            w.method.gameObject.SetActive(any);w.materials.gameObject.SetActive(any);w.viewport.gameObject.SetActive(any);
            w.result.gameObject.SetActive(any);w.empty.gameObject.SetActive(!any);w.emptyIcon.gameObject.SetActive(!any);
            BindBrowserIcon(w.stationIcon,any?new ItemStack(shownRecipe.Station,1):default);w.stationIcon.gameObject.SetActive(any&&shownRecipe.Station!=0);
            BindBrowserIcon(w.result,any?shownRecipe.Output:default);
            BindBrowserIcon(w.emptyIcon,new ItemStack(browserItem,1));
            for(int i=0;i<w.cells.Count;i++){BindBrowserIcon(w.cells[i],default);w.cells[i].gameObject.SetActive(false);}
            foreach(var icon in w.fuels){BindBrowserIcon(icon,default);icon.gameObject.SetActive(false);}
            for(int i=0;i<w.totals.Count;i++){BindBrowserIcon(w.totals[i],default);w.totals[i].gameObject.SetActive(false);w.totalNames[i].text="";w.totalNames[i].gameObject.SetActive(false);}
            w.scroll.StopMovement();w.content.anchoredPosition=Vector2.zero;
            if(!any)
            {
                w.empty.text=browserUses?"No registered recipe uses this item.":"No crafting or processing recipe.\nFind this item through exploration or other world interactions.";
                w.footer.text="Browse another item, or select the other tab.";recipePanel.SetActive(true);return;
            }
            var recipe=shownRecipe;var grid=recipe.GridRecipe;
            w.station.text=recipe.StationName;w.station.rectTransform.anchoredPosition=new Vector2(recipe.Station==0?16:60,-111);
            w.role.text=recipe.Station==browserItem&&browserUses?"Used here as the station":recipe.Fuels.Contains(browserItem)&&browserUses?"Used here as fuel":"";
            int width=grid==null?1:grid.MinimumGridSize;
            int cellCount=grid==null?recipe.Ingredients.Count:width*width;
            for(int i=0;i<cellCount;i++)
            {
                int col=i%width,row=i/width;ItemStack ingredient=default;
                if(grid==null||grid.Kind==RecipeKind.Shapeless){if(i<recipe.Ingredients.Count)ingredient=recipe.Ingredients[i];}
                else if(col<grid.Width&&row<grid.Height)ingredient=recipe.Ingredients[row*grid.Width+col];
                var cell=w.cells[i];BindBrowserIcon(cell,ingredient);((RectTransform)cell.transform).anchoredPosition=new Vector2(24+col*42,-158-row*42);cell.gameObject.SetActive(true);
            }
            float centre=156+width*21;w.arrow.anchoredPosition=new Vector2(207,-centre+22);
            ((RectTransform)w.result.transform).anchoredPosition=new Vector2(261,-centre+30);w.result.RecipeId=recipe.Id;
            w.output.rectTransform.anchoredPosition=new Vector2(232,-centre-38);w.output.text=game.Registry.Get(recipe.Output.Id).displayName+" × "+recipe.Output.Count;
            w.method.text=grid!=null?grid.Kind==RecipeKind.Shapeless?"Any arrangement":grid.AllowsMirroring?"Shown layout or mirror":"Shown layout":
                (recipe.Ticks/20f).ToString("0.#")+" seconds"+(recipe.Watts>0?" · "+recipe.Watts+" W at full power":" · Requires fuel");
            var totals=recipe.Ingredients.Where(s=>!s.Empty).GroupBy(s=>s.Id).Select(g=>new ItemStack(g.Key,g.Sum(s=>s.Count))).ToArray();
            w.content.sizeDelta=new Vector2(282,Math.Max(138,totals.Length*38));
            for(int i=0;i<totals.Length;i++)
            {BindBrowserIcon(w.totals[i],totals[i]);w.totals[i].gameObject.SetActive(true);w.totalNames[i].text=totals[i].Count+" × "+game.Registry.Get(totals[i].Id).displayName;w.totalNames[i].gameObject.SetActive(true);}
            if(recipe.FoodRecipe!=null)
            {
                for(int i=0;i<recipe.FoodRecipe.ingredients.Length;i++)
                {
                    var input=recipe.FoodRecipe.ingredients[i];
                    w.totalNames[i].text=input.count+" × "+(input.selector.StartsWith("#")?input.selector+": "+string.Join(" / ",CookingCatalog.Current.Choices(input.selector).Select(id=>game.Registry.Get(id).displayName)):game.Registry.Get(game.Registry.ResolveId(input.selector)).displayName);
                }
            }
            w.footer.text=recipe.Fuels.Count>0?"FUEL · choose one":"Shift-click Fill grid: max · Output: Shift one / Ctrl+Shift max";
            for(int i=0;i<recipe.Fuels.Count;i++)
            {BindBrowserIcon(w.fuels[i],new ItemStack(recipe.Fuels[i],(recipe.Ticks+game.Processing.FuelTicks(recipe.Fuels[i])-1)/game.Processing.FuelTicks(recipe.Fuels[i])));w.fuels[i].gameObject.SetActive(true);}
            recipePanel.SetActive(true);RefreshMissingIngredients();
        }
    }
}
