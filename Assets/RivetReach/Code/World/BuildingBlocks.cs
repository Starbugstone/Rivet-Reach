using System;
using UnityEngine;

namespace RivetReach
{
    public interface IHalfBlock : IItemCapability
    {
        byte Lower {get;}
        byte Upper {get;}
        byte Double {get;}
    }

    // Cell variants are world geometry, never additional inventory items.
    public static class BuildingBlocks
    {
        public const byte WoodenSlab=82,WoodenUpper=83,WoodenDouble=84;
        public const byte StoneSlab=85,StoneUpper=86,StoneDouble=87;
        sealed class Slab : IHalfBlock
        {
            public byte Lower {get;}
            public byte Upper=>(byte)(Lower+1);
            public byte Double=>(byte)(Lower+2);
            public Slab(byte lower){Lower=lower;}
        }
        public static readonly IHalfBlock Wood=new Slab(WoodenSlab),Stone=new Slab(StoneSlab);
        public static IHalfBlock SlabFor(byte id)=>id>=WoodenSlab&&id<=WoodenDouble?Wood:id>=StoneSlab&&id<=StoneDouble?Stone:null;
        public static bool AddedItem(byte id)=>id==WoodenSlab||id==StoneSlab;
        public static bool Internal(byte id)=>id==WoodenUpper||id==WoodenDouble||id==StoneUpper||id==StoneDouble;
        public static bool Half(byte id)=>id==WoodenSlab||id==WoodenUpper||id==StoneSlab||id==StoneUpper;
        public static bool Doubled(byte id)=>id==WoodenDouble||id==StoneDouble;
        public static int DropCount(byte id)=>Doubled(id)?2:1;
        public static byte Item(byte id)=>SlabFor(id)?.Lower??id;
        public static Bounds Bounds(byte id)=>new Bounds(new Vector3(.5f,Upper(id)?.75f:.25f,.5f),new Vector3(1,.5f,1));
        public static bool Upper(byte id)=>id==WoodenUpper||id==StoneUpper;
        public static bool FullTop(byte id)=>BlockId.Solid(id)&&(!Half(id)||Upper(id));
        public static bool Recipe(string id)=>id=="rivet:wooden_slab"||id=="rivet:stone_slab";

        // Decide against the precise hit, so clicking an exposed half fills that
        // cell while a side hit places in the corresponding adjacent half.
        public static void Placement(IHalfBlock slab,BlockSelectionHit hit,Vector3 hitInCell,out BlockPos cell,out byte result,out byte expected)
        {
            bool hitLower=hit.Block==slab.Lower,hitUpper=hit.Block==slab.Upper;
            if(hitLower&&hit.Face.y>0||hitUpper&&hit.Face.y<0)
            {cell=hit.Position;result=slab.Double;expected=hit.Block;return;}
            cell=hit.Position.Offset(hit.Face.x,hit.Face.y,hit.Face.z);expected=0;
            result=hit.Face.y<0||hit.Face.y==0&&hitInCell.y>=.5f?slab.Upper:slab.Lower;
        }
    }
}
