using UnityEngine;
namespace RivetReach
{
    public sealed partial class GameUI
    {
        void BuildCompost(Transform parent,MachineState m)
        {
            Label(parent,m.IsAutoComposter?"AUTOCOMPOSTER":"COMPOST BIN",821,115,315,32,23);
            Label(parent,"Mix any compostable organics.\nItems are consumed as you add them.",821,157,307,62,15,gold);
            var status=Panel(parent,821,228,310,55,slate);machineStatus=Label(status.transform,"",12,10,290,40,20,gold);
            var track=Panel(parent,821,295,310,18,slate);machineProgress=Panel(track.transform,0,0,0,18,gold);
            Label(parent,"ADD ORGANICS",821,329,170,22,13,gold);Slot(parent,MachineSlotStart,821,357,55);
            if(m.IsAutoComposter){Label(parent,"COMPOST",1030,329,110,22,13,gold);Slot(parent,MachineSlotStart+2,1069,357,55);}
            machineDetail=Label(parent,"",821,417,311,170,13);
            nextMachineRefresh=0;
        }
        void RefreshCompost(MachineState m)
        {
            float fraction=(float)m.CompostPoints/CompostCatalog.Current.pointsPerCompost;
            machineStatus.text=$"Compost level: {fraction*100:0.#}%";
            machineDetail.text="Left-click: add held stack\nRight-click: add one · Shift-click: add stack\nMix inputs in any order. Excess carries over.\n100% produces 1–4 Compost.\n"+(m.IsAutoComposter?"Mining loses unfinished progress and charge.\n":"Mining loses unfinished progress.\n")+
                (m.IsAutoComposter?$"160 W · 8 J/item · charge {m.CompostCharge/20f:0.#}/512 J\nOutput stored for collection or pipes.\n"+StatusName(m.Status):"Finished Compost pops out above the bin.\nManual only · no pipe connections.");
            machineProgress.GetComponent<RectTransform>().sizeDelta=new Vector2(310*Mathf.Clamp01(fraction),18);
        }
    }
}
