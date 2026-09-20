using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        // Rebuild text only when its inputs change. Include the retained widget
        // identity so new screens/sessions cannot inherit stale presentation.
        (Text,bool,byte,ToolCapability,ToolTier,MachineState,int,PortRole,bool,Key,string) shownTarget;
        (Text,long,int,int,WeatherKind) shownClock;
        Text shownDiagnostics;bool shownDiagnosticsEnabled;float nextDiagnosticsRefresh;
        void RefreshDiagnostics()
        {
            if(diagnostics==null)return;
            if(!game.Diagnostics)
            {
                if(shownDiagnosticsEnabled||shownDiagnostics!=diagnostics)diagnostics.text="";
                shownDiagnostics=diagnostics;shownDiagnosticsEnabled=false;return;
            }
            // Counts traverse resident meshes. Debug text is legible at four
            // updates per second and must not repeat those scans every frame.
            if(shownDiagnostics==diagnostics&&shownDiagnosticsEnabled&&UnityEngine.Time.unscaledTime<nextDiagnosticsRefresh)return;
            shownDiagnostics=diagnostics;shownDiagnosticsEnabled=true;nextDiagnosticsRefresh=UnityEngine.Time.unscaledTime+.25f;
            diagnostics.text=game.Diagnostics?$"{1/Mathf.Max(.001f,frameAverage):0} fps · {frameAverage*1000:0.0} ms\nWorld: {TerrainGenerator.WorldId} · seed {game.Seed} · {game.World.Address(game.Player.transform.position)}\nChunks {game.World.ReadyCount}/{game.World.ResidentCount} · queue {game.World.PendingCount}\nGeneration + mesh {game.World.LastBuildMs:0.0} ms · edit mesh {game.World.LastEditMeshMs:0.0} ms\nTriangles {game.World.MeshTriangles:N0} · changes {game.World.EditCount} · piles {game.Items.Piles.Count}\nStale jobs rejected {game.World.RejectedJobs} · origin {game.World.Origin}\nPlacement: {game.PlacementDiagnostic??"No attempt yet"}\nIndustry: {game.Industry.Simulation.Machines.Count} assemblies · tick {game.Industry.Simulation.LastStepMs:0.00} ms · {(game.Industry.Simulation.Rebuilding?"Connecting":"Ready")}":"";
        }
        void RefreshTargetLabel()
        {
            if(targetLabel!=null)
            {
                bool hasPipe=game.TryGetPipeEndTarget(out var pipe,out int pipeFace);
                var role=hasPipe?game.Industry.Simulation.PipeEndRole(pipe,pipeFace):PortRole.Disabled;
                var selected=game.Inventory.Slots[game.Selected];
                var capability=game.Creative?ToolCapability.Pickaxe:game.Registry.Capabilities(selected);
                var tier=game.Creative?ToolTier.Diamond:game.Registry.Tier(selected);
                var key=(targetLabel,game.Player.HasTarget,game.Player.TargetId,capability,tier,pipe,pipeFace,role,game.HoldingWrench,game.Input.Keys["Interact"],game.Input.UseButtonName);
                if(key.Equals(shownTarget))return;
                shownTarget=key;
                string hint=BlockId.MiningHint(game.Player.TargetId,capability,tier);
                targetLabel.text=game.Player.HasTarget?(Fluids.Registry.Get(game.Player.TargetId) is FluidDefinition targetFluid?targetFluid.DisplayName+" source · Use bucket":game.Registry.Get(game.Player.TargetId).displayName)+((BlockId.Station(game.Player.TargetId)||IndustryId.Placed(game.Player.TargetId))?$"\n{game.Input.Keys["Interact"]} / {game.Input.UseButtonName} · Open"+(game.Player.TargetId==BlockId.Workbench?" 3 × 3 crafting":""):hint.Length>0?" · "+hint:""):"";
                if(game.Player.HasTarget&&IndustryId.DoorPart(game.Player.TargetId))
                    targetLabel.text=$"Wooden Door\n{game.Input.Keys["Interact"]} / {game.Input.UseButtonName}: open / close · Blue Signal at base";
                if(game.Player.HasTarget&&game.Player.TargetId==IndustryId.HandCrank)
                    targetLabel.text=$"Hand Crank\n{game.Input.Keys["Interact"]} / {game.Input.UseButtonName}: turn · hold {game.Input.UseButtonName} to repeat\n50 J per turn · 100 W while cranking";
                if(hasPipe)
                    targetLabel.text=(pipe.Definition.Id==IndustryId.ItemPipe?"Item":"Fluid")+" connection · "+(role==PortRole.Disabled?"NO CONNECTION":role==PortRole.Input?"<color=#3399ff>INPUT into machine</color>":"<color=#ff4433>OUTPUT from machine</color>")+(game.HoldingWrench?$"\n{game.Input.UseButtonName} with wrench: Input → Output → No connection":"\nHold a Wrench to change direction");
            }
        }
        void RefreshWorldTime()
        {
            if(worldTime==null)return;
            var clock=game.Sky.Clock;int minute=(int)(clock.Hour*60);
            var key=(worldTime,clock.DayNumber,minute,clock.MoonPhase,game.Weather.Kind);
            if(key.Equals(shownClock))return;
            shownClock=key;
            worldTime.text=$"Day {clock.DayNumber} · {minute/60:00}:{minute%60:00}\n{clock.MoonPhaseName} · {game.Weather.Kind}";
        }
    }
}
