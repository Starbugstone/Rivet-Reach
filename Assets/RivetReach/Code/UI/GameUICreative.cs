using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        bool creativeCrafting;
        string creativeSearch="";

        void BuildCreativeCatalog(Transform parent)
        {
            Label(parent,"ALL ITEMS",821,115,300,38,27);
            Label(parent,"Click an item to receive a full stack",821,157,315,24,14,gold);
            var searchPanel=Panel(parent,821,188,315,34,slate);
            var search=searchPanel.gameObject.AddComponent<InputField>();
            search.textComponent=Label(searchPanel.transform,"",9,5,296,25,16);
            search.placeholder=Label(searchPanel.transform,"Search items or #tag…",9,5,296,25,16,gold);
            search.text=creativeSearch;search.characterLimit=64;
            var viewport=Panel(parent,821,233,315,258,slate);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic=true;
            var content=Rect(viewport.transform,"Creative items",0,0,315,258);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport=viewport.rectTransform;scroll.content=content;scroll.horizontal=false;
            scroll.movementType=ScrollRect.MovementType.Clamped;
            var status=Label(parent,"Scroll for more items",821,497,315,35,14,gold);currentStation.creativeStatus=status;
            void Populate(string query)
            {
                creativeSearch=query;
                foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
                int row=0;var terms=query.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
                foreach(var item in game.Registry.items.OrderBy(i=>i.displayName,StringComparer.OrdinalIgnoreCase))
                {
                    if(!game.Registry.MatchesSearch(item.runtimeId,terms))continue;
                    var entry=Button(content,"",4,row++*52+4,307,48,()=>
                    {
                        status.text=game.TryGiveCreativeItem(item.runtimeId)?$"Added {item.stackLimit} × {item.displayName}":"Inventory full · Make room for a stack";
                        RefreshSlots();
                    });
                    entry.gameObject.name="Creative item "+item.stableId;
                    var icon=Rect(entry.transform,"Icon",5,6,36,36).gameObject.AddComponent<RawImage>();
                    icon.texture=icons[item.runtimeId];icon.raycastTarget=false;
                    Label(entry.transform,item.displayName,48,4,205,42,14).alignment=TextAnchor.MiddleLeft;
                    Label(entry.transform,"+"+item.stackLimit,253,12,48,26,13,gold).alignment=TextAnchor.MiddleRight;
                }
                content.sizeDelta=new Vector2(315,Mathf.Max(258,row*52+4));
                scroll.verticalNormalizedPosition=1;
                status.text=row==0?"No matching items":$"{row} items · Scroll for more";
            }
            search.onValueChanged.AddListener(Populate);Populate(creativeSearch);
        }
    }
}
