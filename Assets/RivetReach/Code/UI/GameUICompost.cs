using UnityEngine;
namespace RivetReach
{
    public sealed partial class GameUI
    {
        void BuildCompost(Transform parent,MachineState m)
        {
            Label(parent,"COMPOST BIN",821,115,315,32,23);
            Label(parent,"Collect organic material, then compost it.\nNo fuel or electricity required.",821,157,307,62,15,gold);
            var status=Panel(parent,821,228,310,55,slate);machineStatus=Label(status.transform,"",12,10,290,40,18,gold);
            Label(parent,"ORGANIC INPUT",821,299,180,22,13,gold);Slot(parent,MachineSlotStart,821,325,55);
            Label(parent,"COMPOST",1040,299,100,22,13,gold);Slot(parent,MachineSlotStart+2,1069,325,55);
            var track=Panel(parent,892,346,155,9,slate);machineProgress=Panel(track.transform,0,0,0,9,gold);
            machineDetail=Label(parent,"",821,390,311,125,14);
            nextMachineRefresh=0;
        }
        void RefreshCompost(MachineState m)
        {
            machineStatus.text=m.Status==MachineStatus.Running?"Composting":StatusName(m.Status);
            var catalog=CompostCatalog.Current;var input=m.Items.Slots[0];int batch=m.CompostBatchCount;
            machineDetail.text=(batch==0?"Add #compostable items.\nOne ingredient type at a time.":$"{input.Count} / {batch} {game.Registry.Get(input.Id).displayName} per Compost\n{catalog.Points(input.Id)} organic points each · {catalog.pointsPerCompost} per batch")+$"\nProcessing: {m.Work/20:0.#} / {catalog.ticks/20f:0.#} s\nItem pipes: input / output on any face";
            machineProgress.GetComponent<RectTransform>().sizeDelta=new Vector2(155*Mathf.Clamp01((float)(m.Work/m.ProcessingTicks)),9);
        }
    }
}
