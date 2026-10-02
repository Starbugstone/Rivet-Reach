using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class AlphaGuidanceChecks
    {
        public static void Run()
        {
            var lines=new List<string>();
            void Check(bool ok,string label){if(!ok)throw new Exception("Guidance/building: "+label);lines.Add("PASS "+label);}
            var items=ItemRegistry.Load();var recipes=RecipeCatalogAsset.Load().Compile(items);
            foreach(var slab in new[]{BuildingBlocks.Wood,BuildingBlocks.Stone})
            {
                Check(ReferenceEquals(items.Capability<IHalfBlock>(slab.Lower),slab),"Compiled half-block capability "+slab.Lower);
                Check(items.Get(slab.Upper)==items.Get(slab.Lower)&&items.Get(slab.Double)==items.Get(slab.Lower),"World variants resolve canonical item");
                byte material=slab==BuildingBlocks.Wood?BlockId.Planks:BlockId.Stone;
                var session=new CraftingSession(recipes,3,id=>items.Get(id).stackLimit);
                for(int i=3;i<6;i++)session.Grid.Add(material,1,i,i+1);
                ItemStack cursor=default;
                Check(session.Preview?.Output.Id==slab.Lower&&session.CraftToCursor(ref cursor).Succeeded&&cursor.Count==6&&session.Grid.Slots.All(s=>s.Empty),"Three full blocks make exactly six slabs");
                foreach(byte id in new[]{slab.Lower,slab.Upper})
                {
                    var cells=new byte[34*34*34];cells[ChunkMesher.Index(1,1,1)]=id;
                    var built=ChunkMesher.Build(default,0,cells);
                    Check(built.Triangles.Length==36&&Mathf.Approximately(built.Bounds.size.y,.5f),"Six faces with true half-height mesh "+id);
                    var shape=BlockDefinitions.Get(id).Selection;float occupied=id==slab.Lower?.25f:.75f,empty=1-occupied;
                    Check(shape.Intersect(new Vector3(-1,occupied,.5f),Vector3.right,4,out _,out _)&&!shape.Intersect(new Vector3(-1,empty,.5f),Vector3.right,4,out _,out _),"Target passes through empty half "+id);
                    Check(BlockId.Opaque(id),"Solid slab roof blocks voxel sunlight "+id);
                    Check(BlockDefinitions.Get(id).Collision.Bounds==BuildingBlocks.Bounds(id),"Collision and selection use same half "+id);
                }
                var hit=new BlockSelectionHit(new BlockPos(31,10,0),slab.Lower,new Vector3(31.5f,10.5f,.5f),Vector3Int.up,2);
                BuildingBlocks.Placement(slab,hit,new Vector3(.5f,.5f,.5f),out var p,out byte result,out byte expected);
                Check(p.Equals(hit.Position)&&result==slab.Double&&expected==slab.Lower,"Exposed lower face combines in its own cell");
                hit=new BlockSelectionHit(hit.Position,slab.Upper,Vector3.zero,Vector3Int.down,2);
                BuildingBlocks.Placement(slab,hit,new Vector3(.5f,.5f,.5f),out p,out result,out expected);
                Check(p.Equals(hit.Position)&&result==slab.Double&&expected==slab.Upper,"Exposed upper face combines in its own cell");
                hit=new BlockSelectionHit(hit.Position,BlockId.Stone,Vector3.zero,Vector3Int.right,2);
                BuildingBlocks.Placement(slab,hit,new Vector3(1,.9f,.5f),out p,out result,out expected);
                Check(p.X==32&&result==slab.Upper,"Upper side placement crosses chunk boundary");
                Check(!BuildingBlocks.FullTop(slab.Lower)&&BuildingBlocks.FullTop(slab.Upper)&&BuildingBlocks.FullTop(slab.Double),"Support tracks full top face");
                Check(!VoxelWorld.TorchSupported(slab.Lower,1)&&VoxelWorld.TorchSupported(slab.Upper,1)&&!VoxelWorld.TorchSupported(slab.Upper,0),"Torch support rejects floating attachments");
                Check(items.FistDrop(slab.Upper)==slab.Lower&&items.FistDrop(slab.Double)==slab.Lower,"Mining variants returns public slab");
                var inventory=new Inventory(id=>items.Get(id).stackLimit);InventoryActions.Pick(inventory,items,slab.Double,0,true);
                Check(inventory.Slots[0].Id==slab.Lower,"Pick Block never creates internal variant");
            }
            var glassCells=new byte[34*34*34];glassCells[ChunkMesher.Index(1,1,1)]=IndustryId.Glass;
            var single=ChunkMesher.Build(default,0,glassCells);
            Check(single.Triangles.Length==0&&single.GlassMesh.Indices.Length==36,"Glass uses separate transparent chunk mesh");
            glassCells[ChunkMesher.Index(2,1,1)]=IndustryId.Glass;
            var pair=ChunkMesher.Build(default,0,glassCells);
            Check(pair.GlassMesh.Indices.Length==60,"Adjacent glass removes both internal faces");
            int mask=BuildingMesher.Connections(glassCells,ChunkMesher.Index(1,1,1),2,-1);
            Check((mask&2)!=0&&(mask&1)==0,"Exterior face joins right edge only");
            glassCells[ChunkMesher.Index(2,2,1)]=IndustryId.Glass;
            mask=BuildingMesher.Connections(glassCells,ChunkMesher.Index(1,1,1),2,-1);
            Check((mask&128)!=0,"Diagonal connection preserves concave-corner information");
            var boundary=new byte[34*34*34];boundary[ChunkMesher.Index(31,1,1)]=IndustryId.Glass;boundary[ChunkMesher.Index(32,1,1)]=IndustryId.Glass;
            var edge=ChunkMesher.Build(default,0,boundary);
            Check(edge.GlassMesh.Indices.Length==30&&(BuildingMesher.Connections(boundary,ChunkMesher.Index(31,1,1),2,-1)&2)!=0,"Halo joins glass across chunk boundary");
            boundary[ChunkMesher.Index(32,1,1)]=0;
            Check(ChunkMesher.Build(default,0,boundary).GlassMesh.Indices.Length==36&&(BuildingMesher.Connections(boundary,ChunkMesher.Index(31,1,1),2,-1)&2)==0,"Removing neighbor restores exposed face and rim");
            Check(BlockId.Solid(IndustryId.Glass)&&!BlockId.Opaque(IndustryId.Glass)&&BlockId.Placeable(IndustryId.Glass),"Existing Glass is solid, transparent and placeable");
            foreach(var band in OreGenerator.Bands)
            {string source=ResourceGuidance.Source(band.Block,items);Check(source.Contains("Y "+band.MinY)&&source.Contains("Y "+band.PeakY)&&source.Contains(BlockId.RequiredTier(band.Block).ToString().ToLowerInvariant()),"Depth/tool cues match generator authority "+band.Block);}
            Check(ResourceGuidance.ToolHint(BlockId.DiamondOre,ToolCapability.Pickaxe,ToolTier.Stone).StartsWith("Requires"),"Insufficient pickaxe shows requirement");
            Check(ResourceGuidance.ToolHint(BlockId.DiamondOre,ToolCapability.Pickaxe,ToolTier.Iron).Contains("ready"),"Sufficient pickaxe shows readiness");
            var near=new BlockPos(100000000,0,-100000000);
            Check(PlayerNavigation.Bearing(near,near.Offset(0,10,100),0)=="Ahead · 100 m · 10 m above","Navigation retains precision at remote coordinates");
            Check(PlayerNavigation.Bearing(near,near.Offset(0,0,100),180).StartsWith("Behind"),"Navigation follows player heading");
            var navigation=new PlayerNavigation();navigation.RecordDeath(new BlockPos(31,-64,-33));
            using(var stream=new MemoryStream())
            {
                using(var writer=new SaveWriter(stream))navigation.WriteSave(writer);stream.Position=0;
                var loaded=new PlayerNavigation();using(var reader=new SaveReader(stream,items))loaded.ReadSave(reader);
                Check(loaded.LastDeath.Equals(navigation.LastDeath),"Schema 19 restores death address exactly");loaded.ClearDeath();Check(!loaded.LastDeath.HasValue,"Death marker can be dismissed");
            }
            using(var reader=new SaveReader(new MemoryStream(),items,18)){var old=new PlayerNavigation();old.ReadSave(reader);Check(!old.LastDeath.HasValue,"Legacy saves require no added marker bytes");}
            Directory.CreateDirectory("Logs/AlphaGuidance");File.WriteAllLines("Logs/AlphaGuidance/domain-checks.txt",lines);
            Debug.Log("PASS "+lines.Count+" alpha guidance/building checks");
        }
    }
}
