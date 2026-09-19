using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        bool HasItemPriority=>game.OpenMachine is MachineState m?m.Definition.Ports.Any(p=>p.Kind==NetworkKind.Item&&p.Role==PortRole.Input):game.OpenStation?.Furnace!=null||game.OpenStation?.Storage!=null||CrateId.Part(game.OpenStation?.Block??0);
        int ItemPriority=>game.OpenMachine?.ItemInputPriority??game.OpenStation?.ItemInputPriority??0;
        void SetItemPriority(int value)
        {
            if(!HasInventoryBinding||!HasItemPriority)return;value=Mathf.Clamp(value,0,100);
            if(game.OpenMachine!=null)game.OpenMachine.ItemInputPriority=value;else game.OpenStation.ItemInputPriority=value;
            currentStation.itemPriorityField.SetTextWithoutNotify(value.ToString());
        }
        void BuildItemPriority(Transform parent)
        {
            if(!HasItemPriority)return;
            Label(parent,"ITEM PRIORITY",821,595,140,30,14,gold);
            Button(parent,"−",962,592,34,30,()=>SetItemPriority(ItemPriority-1));
            var background=Panel(parent,1000,592,80,30,slate);var field=background.gameObject.AddComponent<InputField>();currentStation.itemPriorityField=field;field.name="Item pipe priority";
            field.textComponent=Label(background.transform,"",8,2,64,27,18);field.textComponent.alignment=TextAnchor.MiddleCenter;field.contentType=InputField.ContentType.IntegerNumber;field.characterLimit=3;
            field.onEndEdit.AddListener(value=>SetItemPriority(int.TryParse(value,out int number)?number:ItemPriority));
            Button(parent,"+",1084,592,34,30,()=>SetItemPriority(ItemPriority+1));
        }
    }
}
