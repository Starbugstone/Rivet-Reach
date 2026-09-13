using System;
using System.Collections.Generic;
using System.Linq;

namespace RivetReach
{
    // Immutable selector shared by recipe compilers; quantities belong to their recipe.
    public sealed class ItemSelector
    {
        readonly bool[] members=new bool[256];
        public IReadOnlyList<byte> Choices {get;}
        internal ItemSelector(IEnumerable<byte> choices)
        {var ids=choices.OrderBy(id=>id).ToArray();foreach(byte id in ids)members[id]=true;Choices=Array.AsReadOnly(ids);}
        public bool Matches(byte id)=>members[id];
    }

    public static class ItemTags
    {
        public const string Edible="edible",Burnable="burnable",BoilerFuel="boiler_fuel";
        // Frozen additions for this release's additive save projection, also used by
        // the Editor authoring pass. Future items author their tags in the registry.
        public static string MaterialAddition(byte id)=>id switch
        {
            BlockId.Log=>"log",BlockId.Planks=>"planks",
            BlockId.RawIron or BlockId.RawCopper or BlockId.RawGold=>"raw_ore",
            BlockId.CopperIngot or BlockId.IronIngot or BlockId.GoldIngot=>"ingot",_=>null
        };
        public static bool Valid(string tag)=>!string.IsNullOrEmpty(tag)&&tag.Length<=64&&tag[0]>='a'&&tag[0]<='z'&&tag.All(c=>c>='a'&&c<='z'||c>='0'&&c<='9'||c=='_');
    }
}
