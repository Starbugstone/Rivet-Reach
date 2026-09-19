using System;

namespace RivetReach
{
    public static class CrateId
    {
        public const byte Crate=176,Controller=177;
        public static bool Part(byte id)=>id==Crate||id==Controller;
    }
    // Owns physical capacity independently of controllers, topology and loaded presentation.
    public sealed class CrateStorage
    {
        public const int Capacity=16384;
        public byte Item {get;private set;}
        public int Count {get;private set;}
        public bool Locked {get;private set;}
        public long Revision {get;private set;}
        readonly Func<byte,int> limit;
        public CrateStorage(Func<byte,int> limit){this.limit=limit;}
        public ItemStack Stack=>new ItemStack(Item,Count);
        public bool Accepts(byte id)=>id!=0&&Count<Capacity&&(Item==0||Item==id);
        public int Insert(ItemStack stack)
        {
            if(stack.Empty||stack.HasInstanceState||!stack.ValidContents||!Accepts(stack.Id))return 0;
            int n=Math.Min(stack.Count,Capacity-Count);Item=stack.Id;Count+=n;Revision++;return n;
        }
        public ItemStack Take(int count)
        {
            int n=Math.Min(Math.Max(0,count),Count);if(n==0)return default;var result=new ItemStack(Item,n);Count-=n;
            if(Count==0&&!Locked)Item=0;Revision++;return result;
        }
        public ItemStack TakeStack(int count)=>Take(Math.Min(count,Item==0?0:limit(Item)));
        public bool Lock(byte id)
        {if(id==0||Count>0&&Item!=id)return false;Item=id;Locked=true;Revision++;return true;}
        public void Unlock(){Locked=false;if(Count==0)Item=0;Revision++;}
        internal void WriteSave(SaveWriter w){w.Write(Item);w.Write(Count);w.Write(Locked);}
        internal void ReadSave(SaveReader r)
        {
            byte id=r.ReadByte();int count=r.Int(0,Capacity);bool locked=r.ReadBoolean();
            SaveReader.Require(id==0?count==0&&!locked:r.Registry.Get(id).runtimeId==id&&id!=IndustryId.DoorUpper&&id!=BedId.Head&&(count>0||locked),"Invalid bulk crate contents or lock.");
            Item=id;Count=count;Locked=locked;Revision++;
        }
    }
}
