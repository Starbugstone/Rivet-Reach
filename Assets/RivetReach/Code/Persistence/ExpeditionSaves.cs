using System;
using System.IO;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class Expedition
    {
        public bool LoadingSave {get;private set;}
        public SaveStore Saves {get;private set;}
        public string SaveId {get;private set;}
        public string SaveName {get;private set;}="Expedition";
        public string SaveStatus {get;private set;}
        public int LastSaveBytes {get;private set;}
        public double LastSaveMilliseconds {get;private set;}
        public string WorldId {get;private set;}
        public void InitializeSaves(string directory=null)=>Saves=new SaveStore(directory??Path.Combine(Application.persistentDataPath,"Saves"),Registry);
        internal byte[] CaptureSave(SaveEntry entry)
        {
            return Saves.Encode(entry,w=>
            {
                w.Write("rivet:surface");w.Point(WorldPoint.FromLocal(Player.transform.position,World.Origin));
                w.Write(Sky.Clock.DaySeconds);w.Write(Sky.Clock.TotalDays);w.Write(Selected);w.Write(Math.Max(0,invulnerableUntil-Time.time));
                World.WriteSave(w);Player.WriteSave(w);w.Slots(Inventory.Slots);w.Slots(PersonalCrafting.Grid.Slots);Hunger.WriteSave(w);Health.WriteSave(w);Equipment.WriteSave(w);
                Survival.WriteSave(w);Industry.Simulation.WriteSave(w);Items.WriteSave(w);Mobs.WriteSave(w);
            });
        }
        public bool SaveGame(string name=null,bool newSlot=false)
        {
            if(!Started){SaveStatus="Start an expedition before saving.";return false;}
            if(!Paused){SaveStatus="Pause the expedition before saving.";return false;}
            try
            {
                string label=(name??SaveName).Trim();if(label.Length==0)label="Expedition";
                SaveReader.Require(label.Length<=48&&!Array.Exists(label.ToCharArray(),char.IsControl),"Use a save name of at most 48 characters.");
                // Cursor contents already return through SetMode; keep the capture boundary explicit.
                UI.ReturnHeld();
                var entry=new SaveEntry{Id=newSlot||SaveId==null?Guid.NewGuid().ToString("N"):SaveId,Name=label,WorldId=WorldId,Seed=Seed,UtcTicks=DateTime.UtcNow.Ticks};
                var timer=System.Diagnostics.Stopwatch.StartNew();byte[] bytes=CaptureSave(entry);Saves.Write(entry,bytes);
                SaveId=entry.Id;SaveName=entry.Name;LastSaveBytes=bytes.Length;LastSaveMilliseconds=timer.Elapsed.TotalMilliseconds;SaveStatus="Saved "+SaveName+".";return true;
            }
            catch(Exception ex) when(SaveStore.IsSaveError(ex)){SaveStatus="Save failed: "+ex.Message;return false;}
        }
        public bool LoadGame(SaveEntry entry)
        {
            try
            {
                byte[] bytes=Saves.Read(entry);using var r=Saves.Open(bytes,out var metadata);
                SaveReader.Require(metadata.Id==entry.Id,"Save slot identity changed.");
                RestoreSave(r,metadata);SaveStatus="Loaded "+metadata.Name+(entry.Backup?" (previous backup)":"")+".";Notify(SaveStatus,5);return true;
            }
            catch(Exception ex) when(SaveStore.IsSaveError(ex)){SaveStatus="Load failed: "+ex.Message;return false;}
        }
        public bool ContinueLatestSave()
        {
            foreach(var entry in Saves.List())if(LoadGame(entry))return true;
            SaveStatus=Saves.ScanWarning??"No saved expedition could be loaded.";return false;
        }
        void RestoreSave(SaveReader r,SaveEntry entry)
        {
            SaveReader.Require(r.Text()=="rivet:surface","Unsupported saved world definition.");var point=r.Point();
            double daySeconds=r.Number(1,1e9),days=r.Number(0,1e9);int selected=r.Int(0,r.Format<6?11:Inventory.HotbarCount-1);float immunity=r.Float(0,2);
            // Keep the original session alive until every section has been restored and validated.
            var oldWorld=World;var oldPlayer=Player;var oldItems=Items;var oldMobs=Mobs;var oldInventory=Inventory;var oldCrafting=PersonalCrafting;
            var oldHunger=Hunger;var oldHealth=Health;var oldEquipment=Equipment;var oldSurvival=Survival;var oldIndustry=Industry;
            string oldSaveId=SaveId,oldSaveName=SaveName;int oldSelected=Selected;bool oldLoading=LoadingSave,oldWaiting=WaitingForRespawn;
            var oldStation=OpenStation;var oldMachine=OpenMachine;int oldSeed=Seed;bool oldCreative=Creative;
            float oldImmunity=invulnerableUntil,oldDayLength=Sky.DayLengthMinutes;double oldDays=Sky.Clock.TotalDays;string oldWorldId=WorldId;
            oldWorld.gameObject.SetActive(false);oldPlayer.gameObject.SetActive(false);oldItems.gameObject.SetActive(false);
            try
            {
                CreateSession(entry.Seed);WorldId=entry.WorldId;
                // An origin near the player retains integer precision at remote saved coordinates.
                World.ReadSave(r,new BlockPos(point.Cell.Chunk.Min.X,0,point.Cell.Chunk.Min.Z));Player.ReadSave(r,point);
                r.PlayerInventory(Inventory);r.Slots(PersonalCrafting.Grid);Hunger.ReadSave(r);Health.ReadSave(r);Equipment.ReadSave(r);
                Survival.ReadSave(r);Industry.Simulation.ReadSave(r);
                SaveReader.Require(Industry.Simulation.Multiblocks.WorldId.ToString("N")==WorldId,"Saved world identities differ.");
                foreach(var p in World.SavedBlocks())if(IndustryId.Placed(p.Value))SaveReader.Require(Industry.Simulation.At(p.Key)!=null,"Missing saved machine.");
                Items.ReadSave(r);Mobs.ReadSave(r);SaveReader.Require(r.BaseStream.Position==r.BaseStream.Length,"Unexpected trailing save data.");
                Sky.DayLengthMinutes=(float)(daySeconds/60);Sky.ResetClock();Sky.Clock.SetTime(days);Sky.Apply();
            }
            catch
            {
                if(World!=oldWorld){World.Stop();World.gameObject.SetActive(false);Destroy(World.gameObject);Player.gameObject.SetActive(false);Destroy(Player.gameObject);Items.gameObject.SetActive(false);Destroy(Items.gameObject);}
                World=oldWorld;Player=oldPlayer;Items=oldItems;Mobs=oldMobs;Inventory=oldInventory;PersonalCrafting=oldCrafting;Hunger=oldHunger;Health=oldHealth;Equipment=oldEquipment;Survival=oldSurvival;Industry=oldIndustry;OpenStation=oldStation;OpenMachine=oldMachine;Seed=oldSeed;Creative=oldCreative;invulnerableUntil=oldImmunity;WorldId=oldWorldId;SaveId=oldSaveId;SaveName=oldSaveName;Selected=oldSelected;LoadingSave=oldLoading;
                WaitingForRespawn=oldWaiting;Sky.DayLengthMinutes=oldDayLength;Sky.ResetClock();Sky.Clock.SetTime(oldDays);Sky.Apply();RenderSettings.fogStartDistance=World.FogStart;RenderSettings.fogEndDistance=World.FogEnd;
                oldWorld.gameObject.SetActive(true);oldPlayer.gameObject.SetActive(true);oldItems.gameObject.SetActive(true);throw;
            }
            oldWorld.Stop();Destroy(oldWorld.gameObject);Destroy(oldPlayer.gameObject);Destroy(oldItems.gameObject);
            Player.RefreshAppearance();Selected=selected;invulnerableUntil=Time.time+immunity;SaveId=entry.Id;SaveName=entry.Name;Started=true;
            LoadingSave=true;UI.HeldStack=default;UI.RefreshPreview();SetMode(ScreenMode.Play);
        }
        public void SaveAndQuit(){if(!Started||SaveGame())Quit();else UI.Rebuild();}
        public void SaveAndTitle(){if(!SaveGame()){UI.Rebuild();return;}Started=false;SetMode(ScreenMode.Title);}
    }
}
