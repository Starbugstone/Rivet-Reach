using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach.Editor
{
    [InitializeOnLoad]
    public static class GameplayFixChecks
    {
        const string Key="RivetReach.GameplayFixChecks";
        const string Output="Logs/GameplayFixChecks";
        static double deadline;
        static GameplayFixChecks(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
        static void Check(bool value,string message)
        {
            if(!value)throw new Exception(message);
            File.AppendAllText(Output+"/checks.txt",message+"\n");
        }
        public static void RunBatch()
        {
            if(!Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run in an isolated batch Editor.");
            Directory.CreateDirectory(Output);File.WriteAllText(Output+"/checks.txt","");
            try
            {
                PlacementChecks();ProjectBuild.BuildAlpha();
                EditorSceneManager.OpenScene("Assets/RivetReach/Scenes/Main.unity");
                SessionState.SetInt(Key,1);EditorApplication.EnterPlaymode();
            }
            catch(Exception ex){Fail(ex);}
        }
        static void PlacementChecks()
        {
            foreach(int seed in new[]{0,1,246813,777,int.MaxValue})
            {
                var generator=new TerrainGenerator(seed);
                Check(RespawnPlacement.TryFind(generator.At,out var p)&&Inside(p),"Safe origin spawn for seed "+seed);
                Check(BlockId.Solid(generator.At(p.Offset(0,-1,0)))&&!BlockId.Solid(generator.At(p))&&!Fluids.IsFluid(generator.At(p)),"Seed has dry supported feet");
            }
            byte Flooded(BlockPos p)=>p.X==0&&p.Z==0?(p.Y<=20?BlockId.Stone:p.Y<=30?Fluids.Water.Source:BlockId.Air):p.Y<=20?BlockId.Stone:BlockId.Air;
            Check(RespawnPlacement.TryFind(Flooded,out var dry)&&(dry.X!=0||dry.Z!=0),"Flooded origin selects another dry column");
            byte FloodedCave(BlockPos p)=>p.Y==0?BlockId.Bedrock:p.Y>=20?Flooded(p):BlockId.Air;
            Check(RespawnPlacement.TryFind(FloodedCave,out var surface)&&surface.Y==21&&(surface.X!=0||surface.Z!=0),"Flooded surface cannot redirect respawn into a dry cave underneath");
            byte Roof(BlockPos p)=>p.Y==500?BlockId.Stone:BlockId.Air;
            Check(RespawnPlacement.TryFind(Roof,out var roof)&&roof.Y==501,"Tall player construction provides a supported roof spawn");
            byte Excavated(BlockPos p)=>p.Y==TerrainGenerator.MinY?BlockId.Bedrock:BlockId.Air;
            Check(RespawnPlacement.TryFind(Excavated,out var pit)&&pit.Y==TerrainGenerator.MinY+1,"Deep excavation can respawn safely above protected bedrock");
            byte Edge(BlockPos p)=>p.X==99&&p.Z==0&&p.Y==20?BlockId.Stone:BlockId.Air;
            Check(RespawnPlacement.TryFind(Edge,out var edge)&&edge.X==99&&Inside(edge),"Search reaches dry ground near the 100-block boundary");
            bool outside=false;
            byte Blocked(BlockPos p){outside|=!Inside(p);return BlockId.Stone;}
            Check(!RespawnPlacement.TryFind(Blocked,out _)&&!outside,"Fully blocked area fails without searching outside the circle");
            byte LowCeiling(BlockPos p)=>p.Y==TerrainGenerator.MaxY-2||p.Y==TerrainGenerator.MaxY?BlockId.Stone:BlockId.Air;
            Check(!RespawnPlacement.TryFind(LowCeiling,out _),"One-block headroom cannot be a respawn destination");
        }
        static bool Inside(BlockPos p)=>(p.X+.5)*(p.X+.5)+(p.Z+.5)*(p.Z+.5)<=10000;
        static void Changed(PlayModeStateChange state)
        {
            if(SessionState.GetInt(Key,0)==0)return;
            if(state==PlayModeStateChange.EnteredPlayMode)deadline=EditorApplication.timeSinceStartup+90;
            if(state!=PlayModeStateChange.EnteredEditMode)return;
            if(SessionState.GetInt(Key,0)!=3){Fail(new Exception("Play ended before Quit Without Saving was invoked."));return;}
            try
            {
                string path=SessionState.GetString(Key+"save","");
                Check(File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes(Output+"/checkpoint-before.rrsave")),"Editor Quit preserves checkpoint bytes after unsaved inventory edits");
                Check(Directory.GetFiles(Output+"/Saves").Length==1,"Editor Quit creates no new checkpoint or backup");
                Check(!EditorApplication.isPlaying,"Quit Without Saving exits Editor Play mode");
                SessionState.SetInt(Key,0);File.WriteAllText(Output+"/result.txt","PASS\n");EditorApplication.Exit(0);
            }
            catch(Exception ex){Fail(ex);}
        }
        static void Tick()
        {
            int phase=SessionState.GetInt(Key,0);
            if(phase==0||!EditorApplication.isPlaying)return;
            try
            {
                if(deadline>0&&EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Editor quit test timed out.");
                var game=Expedition.Instance;if(game==null)return;
                if(phase==1)
                {
                    game.InitializeSaves(Path.GetFullPath(Output+"/Saves"));game.StartSession(246813);game.World.ViewDistance=4;SessionState.SetInt(Key,2);return;
                }
                if(phase!=2||!game.ReadyToPlay)return;
                game.SetMode(ScreenMode.Pause);Check(game.SaveGame("Quit regression",true),"Create isolated Editor checkpoint");
                var entry=game.Saves.List().Single();SessionState.SetString(Key+"save",entry.Path);
                File.Copy(entry.Path,Output+"/checkpoint-before.rrsave",true);
                game.Inventory.Add(BlockId.Diamond,7);
                var button=game.UI.VisibleRoot.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="QUIT WITHOUT SAVING");
                Check(button.isActiveAndEnabled&&button.interactable,"Real pause-menu quit button is available");
                SessionState.SetInt(Key,3);button.onClick.Invoke();
            }
            catch(Exception ex){Fail(ex);}
        }
        static void Fail(Exception ex)
        {
            SessionState.SetInt(Key,0);Directory.CreateDirectory(Output);File.WriteAllText(Output+"/result.txt","FAIL\n"+ex);Debug.LogException(ex);EditorApplication.Exit(1);
        }
    }
}
