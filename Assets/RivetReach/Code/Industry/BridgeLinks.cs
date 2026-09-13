using System;
using System.Collections.Generic;
using System.Linq;

namespace RivetReach
{
    public sealed partial class IndustrySimulation
    {
        // Authority is an identity, never a display name. Future server commands supply
        // their authenticated actor here; names are scoped to owner, world and channel.
        public string LocalOwnerId {get;internal set;}=Guid.NewGuid().ToString("N");
        readonly Dictionary<MachineState,MachineState> bridgePartners=new Dictionary<MachineState,MachineState>();
        public static bool ValidLinkName(string name)=>name!=null&&name.Length<=32&&name==name.Trim()&&!name.Any(char.IsControl)&&!name.Contains('<')&&!name.Contains('>');
        public bool ConfigureBridge(MachineState m,string actor,string name,out string error)
        {
            error="";name=name?.Trim();
            if(m==null||At(m.Position)!=m||!IndustryId.Bridge(m.Definition.Id)||!world.Ready(m.Position)||m.OwnerId!=actor)
            {error="Only the owner can configure this bridge.";return false;}
            if(!ValidLinkName(name)){error="Use up to 32 characters, without markup or control characters.";return false;}
            if(name.Length>0&&machines.Values.Count(other=>other!=m&&other.OwnerId==actor&&other.Definition.Id==m.Definition.Id&&string.Equals(other.LinkName,name,StringComparison.OrdinalIgnoreCase))>=2)
            {error="That network already has two bridges. Choose another name.";return false;}
            m.LinkName=name;Invalidate();return true;
        }
        public MachineState BridgePartner(MachineState m)=>m!=null&&bridgePartners.TryGetValue(m,out var partner)?partner:null;
        public string BridgeStatus(MachineState m)
        {
            if(m.LinkName.Length==0)return "Unlinked · enter a network name";
            var partner=machines.Values.FirstOrDefault(other=>other!=m&&other.OwnerId==m.OwnerId&&other.Definition.Id==m.Definition.Id&&string.Equals(other.LinkName,m.LinkName,StringComparison.OrdinalIgnoreCase));
            if(partner==null)return "Waiting for second bridge";
            return !world.Ready(m.Position)||!world.Ready(partner.Position)?"Partner asleep · load both chunks":Rebuilding?"Connecting networks…":"Linked · "+partner.Position;
        }
        void RebuildBridgeLinks()
        {
            bridgePartners.Clear();
            foreach(var group in machines.Values.Where(m=>IndustryId.Bridge(m.Definition.Id)&&m.OwnerId.Length>0&&m.LinkName.Length>0)
                .GroupBy(m=>(m.OwnerId,m.Definition.Id,m.LinkName.ToUpperInvariant())))
            {
                var pair=group.Take(3).ToArray();
                if(pair.Length!=2||!pair[0].Eligible||!pair[1].Eligible)continue;
                bridgePartners[pair[0]]=pair[1];bridgePartners[pair[1]]=pair[0];
            }
        }
        public bool ConfigureLoader(MachineState m,string actor,bool enabled)
        {
            if(m==null||At(m.Position)!=m||m.Definition.Id!=IndustryId.ChunkLoader||m.OwnerId!=actor||!world.Ready(m.Position))return false;
            m.LoaderEnabled=enabled;Invalidate();return true;
        }
        public IEnumerable<ChunkPos> LoaderChunks()=>machines.Values.Where(m=>m.Definition.Id==IndustryId.ChunkLoader&&m.LoaderEnabled).Select(m=>m.Position.Chunk).Distinct();
    }
}
