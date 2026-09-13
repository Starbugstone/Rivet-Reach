using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace RivetReach
{
    // Explicit schema; never serialize Unity objects, delegates, caches or arbitrary CLR types.
    public sealed class SaveWriter : BinaryWriter
    {
        public readonly int Format;
        public SaveWriter(Stream stream,int format=SaveStore.Format) : base(stream,Encoding.UTF8,true) { Format=format; }
        public void Pos(BlockPos p){Write(p.X);Write(p.Y);Write(p.Z);}
        public void Vector(Vector3 v){Write(v.x);Write(v.y);Write(v.z);}
        public void Point(WorldPoint p){Pos(p.Cell);Vector(p.Fraction);}
        public void Stack(ItemStack s){Write(s.Id);Write(s.Count);if(Format<8){if(s.HasContents)throw new InvalidDataException("Legacy stacks cannot hold contents.");return;}Write(s.Energy);Write(s.FluidAmount);Write(s.FluidCapacity);Write(s.FluidId==0?"":Fluids.Registry.Get(s.FluidId).StableId);}
        public void Slots(IReadOnlyList<ItemStack> slots){Write(slots.Count);foreach(var s in slots)Stack(s);}
        public void Positions(IEnumerable<BlockPos> positions){var list=positions.ToList();Write(list.Count);foreach(var p in list)Pos(p);}
    }
    public sealed class SaveReader : BinaryReader
    {
        public readonly ItemRegistry Registry;
        public readonly int Format;
        public SaveReader(Stream stream,ItemRegistry registry,int format=SaveStore.Format) : base(stream,Encoding.UTF8,true){Registry=registry;Format=format;}
        public static void Require(bool ok,string message){if(!ok)throw new InvalidDataException(message);}
        public int Count(int max=2000000){int n=ReadInt32();Require(n>=0&&n<=max,"Invalid save collection size.");return n;}
        public int Int(int min,int max){int n=ReadInt32();Require(n>=min&&n<=max,"Invalid saved integer.");return n;}
        public long Long(long min=0,long max=long.MaxValue-1000000){long n=ReadInt64();Require(n>=min&&n<=max,"Invalid saved counter.");return n;}
        public double Number(double min=0,double max=1e12){double n=ReadDouble();Require(!double.IsNaN(n)&&!double.IsInfinity(n)&&n>=min&&n<=max,"Invalid saved number.");return n;}
        public float Float(float min=-1e12f,float max=1e12f){float n=ReadSingle();Require(!float.IsNaN(n)&&!float.IsInfinity(n)&&n>=min&&n<=max,"Invalid saved number.");return n;}
        public BlockPos Pos()
        {
            var p=new BlockPos(ReadInt64(),ReadInt32(),ReadInt64());
            Require(p.X>=-TerrainGenerator.HorizontalLimit-64&&p.X<=TerrainGenerator.HorizontalLimit+64&&p.Z>=-TerrainGenerator.HorizontalLimit-64&&p.Z<=TerrainGenerator.HorizontalLimit+64&&p.Y>=TerrainGenerator.MinY-64&&p.Y<=TerrainGenerator.MaxY+256,"Saved position is outside supported world bounds.");return p;
        }
        public Vector3 Vector(float min=-1e12f,float max=1e12f)=>new Vector3(Float(min,max),Float(min,max),Float(min,max));
        public WorldPoint Point()=>new WorldPoint(Pos(),Vector(0,.99999999f));
        public ItemStack Stack()
        {byte id=ReadByte();int count=ReadInt32();Require(id==0?count==0:id!=IndustryId.DoorUpper&&count>0&&count<=Registry.Get(id).stackLimit,"Invalid saved item stack.");var stack=new ItemStack(id,count);
            if(Format>=8){stack.Energy=Long();stack.FluidAmount=Long();stack.FluidCapacity=Long();string fluid=Text();var definition=fluid==""?null:Fluids.Registry.ByStableId(fluid);Require(fluid==""||definition!=null,"Unknown carried liquid.");stack.FluidId=definition?.Source??0;}
            Require(stack.ValidContents,"Invalid carried storage contents.");return stack;}
        public void Slots(ItemContainer container)
        {Require(Count(256)==container.Count,"Saved container size differs.");for(int i=0;i<container.Count;i++){var s=Stack();if(!s.Empty)Require(container.Add(s,i,i+1)==0,"Saved container overflow.");}}
        public void PlayerInventory(Inventory inventory)
        {
            // Schemas 1–5 stored 12 hotbar slots followed by 48 backpack slots.
            int savedHotbar=Format<6?12:Inventory.HotbarCount;
            int savedCount=Format<6?60:Inventory.SlotCount;
            Require(Count(256)==savedCount,"Saved player inventory size differs.");
            for(int i=0;i<savedCount;i++)
            {
                var stack=Stack();int destination=i<savedHotbar?i:Inventory.HotbarCount+i-savedHotbar;
                if(!stack.Empty)Require(inventory.Add(stack,destination,destination+1)==0,"Saved inventory overflow.");
            }
        }
        public List<BlockPos> Positions(){int n=Count();var list=new List<BlockPos>(n);for(int i=0;i<n;i++)list.Add(Pos());return list;}
        public string Text(int max=256){string s=ReadString();Require(s.Length<=max,"Saved text is too long.");return s;}
    }
    public sealed class SaveEntry
    {
        public string Id,Name,WorldId,Path;
        public long UtcTicks;
        public int Seed;
        public string GeneratorVersion=TerrainGenerator.Version;
        public bool Backup;
    }
    public sealed class SaveStore
    {
        public const int Format=8;
        const int MaxBytes=256*1024*1024;
        public string DirectoryPath {get;}
        readonly ItemRegistry registry;
        readonly string content;
        readonly HashSet<string> legacyContent=new HashSet<string>(),currentContent=new HashSet<string>(),modernContent=new HashSet<string>();
        public string ScanWarning {get;private set;}
        public SaveStore(string path,ItemRegistry registry)
        {
            DirectoryPath=path;this.registry=registry;
            // Schema 1/2 predate orchard state; doors and crank are independent additive content.
            // Every definition present before each accepted extension must still match.
            var processing=ProcessingCatalogAsset.Load();
            string Fingerprint(int legacy,bool orchard=false,bool wrench=false,bool electric=false,bool lava=false,bool floater=false)
            {
                string definitions=string.Join("\n",registry.items.Where(i=>(floater||i.stableId!="rivet:floater_rock")&&(lava||i.stableId!="rivet:lava_bucket")&&(electric||i.stableId!="rivet:electric_furnace")&&(wrench||i.stableId!="rivet:wrench")&&(orchard||i.stableId!="rivet:sapling"&&i.stableId!="rivet:apple")&&((legacy&2)==0||i.stableId!="rivet:hand_crank")&&((legacy&1)==0||i.stableId!="rivet:wooden_door")).OrderBy(i=>i.runtimeId).Select(i=>JsonUtility.ToJson(i)))
                    +string.Join("\n",processing.recipes.OrderBy(i=>i.stableId,StringComparer.Ordinal).Select(i=>JsonUtility.ToJson(i)))
                    +string.Join("\n",processing.fuels.OrderBy(i=>i.itemId,StringComparer.Ordinal).Select(i=>JsonUtility.ToJson(i)))
                    +string.Join("\n",RecipeCatalogAsset.Load().recipes.Where(i=>(electric||i.stableId!="rivet:industry_174")&&(wrench||i.stableId!="rivet:wrench")&&((legacy&2)==0||i.stableId!="rivet:industry_170")&&((legacy&1)==0||i.stableId!="rivet:wooden_door")).OrderBy(i=>i.stableId,StringComparer.Ordinal).Select(i=>JsonUtility.ToJson(i)))
                    +string.Join("\n",Resources.LoadAll<MobDefinition>("Mobs/Definitions").Where(i=>floater||i.stableId!="rivet:floater").OrderBy(i=>i.stableId,StringComparer.Ordinal).Select(i=>MobFingerprint(i,floater)));
                return Convert.ToBase64String(Hash(Encoding.UTF8.GetBytes(definitions)));
            }
            content=Fingerprint(0,true,true,true,true,true);modernContent.Add(content);modernContent.Add(Fingerprint(0,true,true,true,true));modernContent.Add(Fingerprint(0,true,true,false,true));modernContent.Add(Fingerprint(0,true,true,true));modernContent.Add(Fingerprint(0,true,true));for(int legacy=0;legacy<4;legacy++){legacyContent.Add(Fingerprint(legacy));currentContent.Add(Fingerprint(legacy,true));}
        }
        // Older saves hashed the exact original fields. Strip only the appended extension;
        // keep every original mob/item/recipe field in compatibility checks.
        static string MobFingerprint(MobDefinition definition,bool floater)
        {
            string json=JsonUtility.ToJson(definition);
            if(floater)return json;
            int suffix=json.LastIndexOf(",\"hoverHeight\":",StringComparison.Ordinal);
            if(suffix<0)throw new InvalidOperationException("Missing mob compatibility projection.");
            return json.Substring(0,suffix)+"}";
        }
        static byte[] Hash(byte[] bytes){using var sha=SHA256.Create();return sha.ComputeHash(bytes);}
        string SlotPath(string id){SaveReader.Require(Guid.TryParseExact(id,"N",out _),"Invalid save slot.");return System.IO.Path.Combine(DirectoryPath,id+".rrsave");}
        public byte[] Encode(SaveEntry entry,Action<SaveWriter> capture)
        {
            using var payload=new MemoryStream();
            using(var w=new SaveWriter(payload))
            {w.Write(entry.Id);w.Write(entry.Name);w.Write(entry.WorldId);w.Write(entry.UtcTicks);w.Write(entry.Seed);w.Write(entry.GeneratorVersion);w.Write(content);capture(w);}
            var data=payload.ToArray();SaveReader.Require(data.Length<=MaxBytes,"This alpha supports saves up to 256 MiB.");
            using var file=new MemoryStream();using(var w=new SaveWriter(file)){w.Write("RIVET REACH SAVE");w.Write(Format);w.Write(data.Length);w.Write(data);w.Write(Hash(data));}return file.ToArray();
        }
        public SaveReader Open(byte[] bytes,out SaveEntry entry)
        {
            using var envelope=new BinaryReader(new MemoryStream(bytes));
            SaveReader.Require(bytes.Length<=MaxBytes+128&&envelope.ReadString()=="RIVET REACH SAVE","Not a Rivet Reach save.");
            int format=envelope.ReadInt32();
            SaveReader.Require(format>=1&&format<=Format,"This save requires a different save-format version.");
            int length=envelope.ReadInt32();SaveReader.Require(length>0&&length<=MaxBytes&&length==envelope.BaseStream.Length-envelope.BaseStream.Position-32,"Save is incomplete or truncated.");
            byte[] data=envelope.ReadBytes(length),hash=envelope.ReadBytes(32);
            SaveReader.Require(hash.SequenceEqual(Hash(data)),"Save checksum failed; the file is damaged.");
            var r=new SaveReader(new MemoryStream(data),registry,format);
            try
            {
                entry=new SaveEntry{Id=r.Text(32),Name=r.Text(48),WorldId=r.Text(32),UtcTicks=r.Long(1,DateTime.MaxValue.Ticks),Seed=r.ReadInt32()};
                SaveReader.Require(Guid.TryParseExact(entry.Id,"N",out _)&&Guid.TryParseExact(entry.WorldId,"N",out _)&&!string.IsNullOrWhiteSpace(entry.Name),"Invalid save identity.");
                entry.GeneratorVersion=r.Text();
                SaveReader.Require(entry.GeneratorVersion==TerrainGenerator.Version||entry.GeneratorVersion==TerrainGenerator.LegacyVersion,"This save requires a different terrain generator.");
                string savedContent=r.Text();
                SaveReader.Require(format>=4?modernContent.Contains(savedContent):format==3?currentContent.Contains(savedContent):legacyContent.Contains(savedContent),"This save requires different content definitions; migration is not available.");
                return r;
            }
            catch{r.Dispose();throw;}
        }
        public byte[] Read(SaveEntry entry)
        {
            string path=SlotPath(entry.Id)+(entry.Backup?".bak":"");
            var info=new FileInfo(path);SaveReader.Require(info.Length<=MaxBytes+128,"Save exceeds the supported size.");return File.ReadAllBytes(path);
        }
        public List<SaveEntry> List()
        {
            var entries=new List<SaveEntry>();ScanWarning=null;
            try
            {
                if(!Directory.Exists(DirectoryPath))return entries;
                foreach(string path in Directory.EnumerateFiles(DirectoryPath,"*.rrsave*"))
                {
                    bool backup=path.EndsWith(".rrsave.bak",StringComparison.Ordinal);if(!backup&&!path.EndsWith(".rrsave",StringComparison.Ordinal))continue;
                    try
                    {
                        SaveReader.Require(new FileInfo(path).Length<=MaxBytes+128,"Save exceeds the supported size.");
                        using var r=Open(File.ReadAllBytes(path),out var entry);
                        SaveReader.Require(path==SlotPath(entry.Id)+(backup?".bak":""),"Save filename does not match its identity.");
                        entry.Path=path;entry.Backup=backup;entries.Add(entry);
                    }
                    catch(Exception ex) when(IsSaveError(ex)){ScanWarning="Some saves are unavailable: "+ex.Message;}
                }
            }
            catch(Exception ex) when(IsSaveError(ex)){ScanWarning="Cannot list saves: "+ex.Message;}
            return entries.OrderByDescending(e=>e.UtcTicks).ThenBy(e=>e.Backup).ThenBy(e=>e.Id,StringComparer.Ordinal).ToList();
        }
        public void Write(SaveEntry entry,byte[] bytes)
        {
            // Validate the complete envelope before touching an existing checkpoint.
            using(var check=Open(bytes,out var actual))SaveReader.Require(actual.Id==entry.Id,"Save identity mismatch.");
            Directory.CreateDirectory(DirectoryPath);string target=SlotPath(entry.Id),temp=target+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(target))
                {
                    bool valid=true;try{using var check=Open(File.ReadAllBytes(target),out _);}catch(Exception ex) when(IsSaveError(ex)){valid=false;}
                    // Never replace a good recovery backup with a corrupt primary.
                    File.Replace(temp,target,valid?target+".bak":null);
                }
                else File.Move(temp,target);
            }
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
        public static bool IsSaveError(Exception ex)=>ex is InvalidDataException||ex is IOException||ex is UnauthorizedAccessException||ex is ArgumentException||ex is InvalidOperationException||ex is OverflowException||ex is System.Security.SecurityException;
    }
}
