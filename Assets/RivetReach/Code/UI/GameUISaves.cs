using System;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        int savePage;
        string saveNameText;
        void BuildSaveMenu()
        {
            Panel(root,0,0,1280,720,new Color(0,0,0,.4f));var panel=Panel(root,160,40,960,640,ink);var p=panel.transform;
            bool saving=game.Mode==ScreenMode.Save;
            Label(p,saving?"SAVE EXPEDITION":"LOAD EXPEDITION",32,24,700,48,32);
            Button(p,"BACK",802,24,126,40,()=>game.SetMode(game.Started?ScreenMode.Pause:ScreenMode.Title));
            if(saving)
            {
                Label(p,"SAVE NAME",32,110,600,28,16,gold);
                var background=Panel(p,32,151,690,48,slate);var field=background.gameObject.AddComponent<InputField>();
                field.textComponent=Label(background.transform,"",12,9,664,32,21);field.characterLimit=48;field.text=saveNameText??game.SaveName;
                field.onValueChanged.AddListener(value=>saveNameText=value);
                Button(p,game.SaveId==null?"SAVE GAME":"UPDATE THIS SAVE",32,226,432,54,()=>{game.SaveGame(field.text);Rebuild();},true);
                Button(p,"SAVE AS NEW SLOT",484,226,444,54,()=>{game.SaveGame(field.text,true);Rebuild();});
                Label(p,"Updating keeps the previous checkpoint as a recovery backup.\nSave As creates a separate slot. Creative mode and flight reset on load.",32,320,870,78,18);
                Label(p,game.SaveStatus??"Choose a name and save your expedition.",32,438,890,112,18,gold);
                Label(p,"Saves are stored on this computer. Quit from the pause menu to save first.",32,578,890,42,15);
                return;
            }
            var saves=game.Saves.List();const int perPage=6;int pages=Math.Max(1,(saves.Count+perPage-1)/perPage);savePage=Math.Clamp(savePage,0,pages-1);
            Label(p,game.Started?"Loading replaces your unsaved progress. Save first to keep it.":"Choose a checkpoint to resume your expedition.",32,87,880,35,17,gold);
            for(int i=0;i<perPage&&savePage*perPage+i<saves.Count;i++)
            {
                var entry=saves[savePage*perPage+i];float y=132+i*60;
                string date=new DateTime(entry.UtcTicks,DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                Label(p,entry.Name+(entry.Backup?" · PREVIOUS BACKUP":""),32,y,650,28,20);
                Label(p,$"{date} · Seed {entry.Seed}",32,y+28,650,24,14);
                Button(p,entry.Backup?"RECOVER":"LOAD",744,y,184,46,()=>{if(!game.LoadGame(entry))Rebuild();},!entry.Backup);
            }
            if(saves.Count==0)Label(p,"No compatible saved expeditions yet.",32,164,820,60,22);
            Button(p,"PREVIOUS",32,508,172,38,()=>{savePage--;Rebuild();}).interactable=savePage>0;
            Label(p,$"Page {savePage+1} / {pages}",224,514,420,30,16);
            Button(p,"NEXT",756,508,172,38,()=>{savePage++;Rebuild();}).interactable=savePage+1<pages;
            Label(p,game.SaveStatus??game.Saves.ScanWarning??"Previous backups can recover an earlier checkpoint.",32,565,892,65,16,gold);
        }
    }
}
