using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class GameUI
    {
        InputField bridgeNameField {get=>currentStation.bridgeNameField;set=>currentStation.bridgeNameField=value;}
        void BuildBridgeMachine(Transform parent,MachineState m)
        {
            bool loader=m.Definition.Id==IndustryId.ChunkLoader;
            Label(parent,loader?"CHUNK LOADER":"BRIDGE NETWORK",821,115,315,32,23);
            Label(parent,loader?"Keeps this chunk active at any distance.\nNo fuel or electricity needed.":"Two matching bridges · one private name\nConnect pipes or cables on any face.",821,157,307,62,15,gold);
            var status=Panel(parent,821,228,310,65,slate);machineStatus=Label(status.transform,"",12,8,288,53,16,gold);
            machineDetail=Label(parent,"",821,305,307,70,14);
            var track=Panel(parent,821,379,303,4,slate);machineProgress=Panel(track.transform,0,0,0,4,gold);
            if(loader)
            {
                MachineButton(parent,live=>live.LoaderEnabled?"ENABLED · CLICK TO DISABLE":"DISABLED · CLICK TO ENABLE",821,403,303,38,live=>
                {if(game.Industry.Simulation.ConfigureLoader(live,game.Industry.Simulation.LocalOwnerId,!live.LoaderEnabled))game.World.RefreshChunkTickets();});
                Label(parent,"Only this chunk is covered. Machines and pipes across a boundary need another loader. Pausing or closing the world stops production.",821,453,307,80,14);
            }
            else
            {
                Label(parent,"NETWORK NAME",821,390,307,24,13,gold);
                var background=Panel(parent,821,419,303,38,slate);bridgeNameField=background.gameObject.AddComponent<InputField>();
                bridgeNameField.textComponent=Label(background.transform,"",8,5,287,28,16);bridgeNameField.textComponent.supportRichText=false;
                bridgeNameField.characterLimit=32;bridgeNameField.text=m.LinkName;
                MachineButton(parent,live=>"APPLY NAME",821,468,146,34,live=>
                {if(!game.Industry.Simulation.ConfigureBridge(live,game.Industry.Simulation.LocalOwnerId,bridgeNameField.text,out var error))game.Notify(error,4);});
                MachineButton(parent,live=>"UNLINK",977,468,147,34,live=>
                {if(game.Industry.Simulation.ConfigureBridge(live,game.Industry.Simulation.LocalOwnerId,"",out var error))bridgeNameField.text="";else game.Notify(error,4);});
                Label(parent,"Names ignore case. Other players' names stay separate.",821,507,307,40,13);
            }
            nextMachineRefresh=0;
        }
        void RefreshBridgeMachine(MachineState m)
        {
            var sim=game.Industry.Simulation;bool loader=m.Definition.Id==IndustryId.ChunkLoader;
            machineStatus.text=loader?(m.LoaderEnabled?"Active · keeps this chunk loaded":"Disabled · normal chunk unloading"):sim.BridgeStatus(m);
            machineDetail.supportRichText=false;
            machineDetail.text=loader?$"Chunk: {m.Position.Chunk.Min}\nCoverage: 32 × 32 × 32 blocks":$"Network: {(m.LinkName.Length==0?"—":m.LinkName)}\nOwner: {(m.OwnerId==sim.LocalOwnerId?"You":m.OwnerId.Substring(0,8))}\nThis bridge: {m.Position}";
        }
    }
}
