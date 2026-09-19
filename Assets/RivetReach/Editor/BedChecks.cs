using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class BedChecks
    {
        public static void Run()
        {
            var lines=new StringBuilder();int count=0;
            void Check(bool ok,string message){if(!ok)throw new Exception("Beds: "+message);count++;lines.AppendLine("PASS "+message);}
            var policy=new SleepPolicy();
            foreach(var sample in new[]{(1,1),(2,1),(3,2),(4,2),(5,3),(100,50)})
            {Check(policy.Required(sample.Item1)==sample.Item2,"Default half-player threshold rounds up for "+sample.Item1);Check(!policy.Met(sample.Item2-1,sample.Item1)&&policy.Met(sample.Item2,sample.Item1),"Only enough sleepers pass for "+sample.Item1);}
            Check(!policy.Met(0,0),"Empty sessions never trigger sleep");
            Check(new SleepPolicy(75,1).Required(4)==3&&new SleepPolicy(10,3).Required(10)==3&&new SleepPolicy(10,3).Required(1)==1,"Percentage/minimum configuration and small-session clamp");
            foreach(double day in new[]{0d,1d,23d,1000d})foreach(double hour in new[]{0d,3d,5.99,18d,23.99})
            {double morning=SleepPolicy.Morning(day+hour/24);Check(Math.Abs(morning-(day+(hour<6?.25:1.25)))<1e-9&&morning>day+hour/24,"Next morning boundary at day "+day+" hour "+hour);}
            var items=ItemRegistry.Load();var catalog=RecipeCatalogAsset.Load();var recipes=catalog.Compile(items);
            Check(items.items.All(i=>i.runtimeId!=BedId.Head)&&!BlockId.Placeable(BedId.Head)&&items.FistDrop(BedId.Head)==BedId.Bed,"Head is internal; recovery resolves to one Bed");
            Check(!BlockId.Opaque(BedId.Bed)&&!BlockId.Opaque(BedId.Head)&&BlockId.Solid(BedId.Bed)&&BlockId.Solid(BedId.Head),"Paired model reserves collision cells without opaque terrain faces");
            Check((BlockDefinitions.Get(BedId.Bed).Traits&BlockTraits.HasCustomSelectionShape)!=0&&(BlockDefinitions.Get(BedId.Head).Traits&BlockTraits.HasCustomSelectionShape)!=0,"All cached bed selection boxes initialize within their cells");
            var recipe=recipes.Recipes.Single(r=>r.Id=="rivet:bed");Check(recipe.MinimumGridSize==3&&recipe.Width==3&&recipe.Height==2&&recipe.Output.Count==1,"One bed uses the workbench 3x2 layout");
            for(int size=3;size<=4;size++)for(int oy=0;oy<=size-2;oy++)for(int ox=0;ox<=size-3;ox++)
            {
                var craft=new CraftingSession(recipes,size,id=>items.Get(id).stackLimit);
                for(int y=0;y<2;y++)for(int x=0;x<3;x++){int slot=(y+oy)*size+x+ox;craft.Grid.Add(y==0?FarmId.Cloth:BlockId.Planks,1,slot,slot+1);}
                ItemStack held=default;Check(craft.CraftToCursor(ref held).Succeeded&&held.Id==BedId.Bed&&held.Count==1&&craft.Grid.Slots.All(s=>s.Empty),"Exact six-input craft in translated "+size+" grid at "+ox+","+oy);
            }
            var p=new BlockPos(-1,30,31);var heads=new[]{p.Offset(0,0,1),p.Offset(1,0,0),p.Offset(0,0,-1),p.Offset(-1,0,0)};
            for(int r=0;r<4;r++)Check(BedId.HeadAt(p,r).Equals(heads[r]),"Cardinal footprint "+r);
            foreach(byte b in new[]{BedId.Bed,BedId.Head,BlockId.MobSpawner,BlockId.Torch,IndustryId.WoodenDoor,Fluids.Water.Source})Check(!BedId.Floor(b),"Invalid floor "+b);
            // Remove only this additive feature; every unrelated previous definition remains checked.
            var oldItems=ScriptableObject.CreateInstance<ItemRegistry>();var originals=catalog.recipes;
            try
            {
                var current=new SaveStore("unused",items);
                oldItems.items=items.items.Where(i=>i.runtimeId!=BedId.Bed).Select(i=>JsonUtility.FromJson<ItemDefinition>(JsonUtility.ToJson(i))).ToArray();
                catalog.recipes=originals.Where(r=>r.stableId!="rivet:bed").ToArray();
                var prior=new SaveStore("unused",oldItems);var entry=new SaveEntry{Id=new string('a',32),WorldId=new string('b',32),Name="pre-bed",Seed=1,UtcTicks=DateTime.UtcNow.Ticks,GeneratorVersion=TerrainGenerator.Version};
                using(var reader=current.Open(prior.Encode(entry,w=>w.Write(314)),out _))Check(reader.ReadInt32()==314,"Pre-bed content fingerprint accepted");
                oldItems.Get(BlockId.Stone).fistSeconds+=.125f;oldItems.InvalidateIndex();bool rejected=false;
                try{using var reader=current.Open(new SaveStore("unused",oldItems).Encode(entry,w=>w.Write(314)),out _);}catch(InvalidDataException){rejected=true;}
                Check(rejected,"Unrelated prior content change remains rejected");
            }
            finally{catalog.recipes=originals;UnityEngine.Object.DestroyImmediate(oldItems);}
            Directory.CreateDirectory("Logs/Beds");lines.AppendLine(count+" checks passed");File.WriteAllText("Logs/Beds/editor-checks.txt",lines.ToString());
        }
    }
}
