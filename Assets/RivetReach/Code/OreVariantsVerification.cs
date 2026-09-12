using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewOreDrops()
        {
            var player=game.Player;var world=game.World;game.SetCreative(true);game.Mobs.enabled=false;game.Diagnostics=false;
            var mesh=OreVisuals.Prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
            var lamp=new GameObject("Ore drop review light");var light=lamp.AddComponent<Light>();light.range=18;light.intensity=5;
            foreach(byte ore in new[]{BlockId.CopperOre,BlockId.IronOre})
            {
                var band=OreGenerator.Bands.Single(b=>b.Block==ore);
                var target=OreGenerator.Veins(game.Seed,new BlockPos(-64,band.PeakY-16,-64),new BlockPos(64,band.PeakY+16,64))
                    .Where(v=>v.Band.Block==ore&&world.Generator.At(v.Centre)==ore).Select(v=>v.Centre).First();
                player.enabled=false;player.ResetMotion();player.transform.position=world.Local(target)+new Vector3(.5f,-1,-3);
                yield return null;yield return Settle();
                for(int z=-4;z<=0;z++)for(int y=-1;y<=3;y++)for(int x=-1;x<=1;x++)
                {
                    var p=target.Offset(x,y,z);if(p.Equals(target))continue;
                    byte old=world.Get(p);if(old!=0)world.Remove(p,old);
                }
                for(int z=-4;z<=0;z++)for(int x=-1;x<=1;x++)world.Place(target.Offset(x,-2,z),BlockId.Stone);
                lamp.transform.position=world.Local(target)+new Vector3(.5f,1,-2);
                byte raw=game.Registry.FistDrop(ore);int spawned=game.Items.TotalSpawned;
                Check(raw==(ore==BlockId.CopperOre?BlockId.RawCopper:BlockId.RawIron),"Mined metal retains its processing and save identity: "+ore);
                Check(!world.Mine(target,ore,ToolCapability.Pickaxe,ToolTier.Wood),"Under-tier mining preserves metal: "+ore);
                Check(world.Mine(target,ore,ToolCapability.Pickaxe,ToolTier.Stone)&&game.Items.TotalSpawned==spawned+1,"Natural metal mining creates exactly one item: "+ore);
                Check(!world.Mine(target,ore,ToolCapability.Pickaxe,ToolTier.Stone)&&game.Items.TotalSpawned==spawned+1,"Repeated mining cannot duplicate the drop: "+ore);
                var pile=game.Items.Piles.Last(p=>p.Stack.Id==raw);pile.Delay=30;
                yield return new WaitForSecondsRealtime(.5f);
                Check(pile.View!=null&&pile.View.GetComponentsInChildren<MeshFilter>().Single().sharedMesh==mesh,"Actual mined drop uses the stylised ore mesh: "+raw);
                Check(pile.View.GetComponentInChildren<Renderer>().sharedMaterial==OreVisuals.Material(ore),"Actual mined drop shares the correct ore material: "+raw);
                player.Camera.transform.position=pile.View.transform.position+new Vector3(.6f,.55f,-.75f);player.Camera.transform.LookAt(pile.View.transform.position);
                player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
                yield return Capture("ore-drop-"+raw);
                int before=game.Inventory.Total(raw);pile.Delay=0;player.transform.position=pile.Position.Local(world.Origin);game.Items.Step(0);
                Check(game.Inventory.Total(raw)==before+1&&!game.Items.Piles.Contains(pile),"Pickup conserves the mined processing item: "+raw);
                player.transform.position=world.Local(target)+new Vector3(.5f,-1,-3);player.Yaw=0;player.Pitch=12;player.enabled=true;
                player.Arms.gameObject.SetActive(true);player.Body.gameObject.SetActive(true);
                game.Selected=game.Inventory.FindSlot(s=>s.Id==raw);yield return new WaitForSecondsRealtime(.7f);
                Check(player.HeldBlock.ItemId==raw&&player.HeldBlock.Visible,"Collected metal is displayed in hand: "+raw);
                var held=player.HeldBlock.Socket.GetComponentsInChildren<MeshFilter>().Single(m=>m.sharedMesh==mesh);
                Check(held.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap")==OreVisuals.Palette(ore),"Collected metal uses its ore palette in hand: "+raw);
                yield return Capture("ore-drop-held-"+raw);
                var recipe=game.Processing.Find(raw);var furnace=new FurnaceState(game.Processing,id=>game.Registry.Get(id).stackLimit);
                var input=new ItemStack(raw,1);furnace.Click(0,ref input,false);var fuel=new ItemStack(BlockId.Coal,1);furnace.Click(1,ref fuel,false);furnace.Advance(200);
                Check(input.Empty&&furnace.Slots[0].Empty&&furnace.Slots[2].Id==(ore==BlockId.CopperOre?BlockId.CopperIngot:BlockId.IronIngot)&&furnace.Slots[2].Count==1&&recipe.Ticks==200,"Stylised drop still smelts one ingot in 200 ticks: "+raw);
                Check(MachineState.CrusherOutput(raw).Id==(ore==BlockId.CopperOre?IndustryId.CrushedCopper:IndustryId.CrushedIron)&&MachineState.CrusherOutput(raw).Count==2,"Stylised drop retains its crusher input and yield: "+raw);
                game.SetMode(ScreenMode.Inventory);yield return new WaitForSecondsRealtime(.2f);
                Check(game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.RawImage>().Any(i=>i.texture==Resources.Load<Texture2D>("Industry/Icons/"+ore)),"Inventory shows the authored ore icon: "+raw);
                yield return Capture("ore-drop-inventory-"+raw);game.SetMode(ScreenMode.Play);
            }
            Check(game.Registry.FistDrop(BlockId.CoalOre)==BlockId.Coal&&!OreVisuals.UsesModel(BlockId.Coal),"Coal keeps its ordinary resource drop and art");
            Check(!OreVisuals.UsesModel(BlockId.RawGold)&&!OreVisuals.UsesModel(BlockId.Diamond),"Other mined resource appearances remain unchanged");
            Destroy(lamp);
        }
        IEnumerator ReviewOreVariants()
        {
            byte[] ids={IndustryId.AzureOre,BlockId.IronOre,BlockId.CopperOre,BlockId.CoalOre,BlockId.GoldOre,BlockId.DiamondOre};
            var player=game.Player;var world=game.World;game.SetCreative(true);game.Mobs.enabled=false;game.Diagnostics=false;
            var mesh=OreVisuals.Prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
            Check(mesh.triangles.Length/3==4129,"All ore variants retain the approved 4129-triangle source mesh");
            foreach(byte id in ids)
            {
                Check(OreVisuals.Material(id)!=null&&OreVisuals.Palette(id)!=null,"Authored ore material and palette load: "+id);
                Check(OreVisuals.Material(id)==OreVisuals.Material(id)&&OreVisuals.Palette(id)==OreVisuals.Palette(id),"Repeated lookups return shared assets: "+id);
                var cells=new byte[34*34*34];
                for(int z=0;z<32;z++)for(int y=0;y<32;y++)for(int x=0;x<32;x++)cells[ChunkMesher.Index(x,y,z)]=id;
                var chunk=ChunkMesher.Build(default,0,cells);
                Check(chunk.Triangles.Length/3==12,"32768 uniform ore cells mesh to twelve exterior triangles: "+id);
                for(int i=0;i<cells.Length;i++)cells[i]=BlockId.Stone;
                for(int z=0;z<32;z++)for(int y=0;y<32;y++)for(int x=0;x<32;x++)cells[ChunkMesher.Index(x,y,z)]=id;
                Check(ChunkMesher.Build(default,0,cells).Triangles.Length==0,"Buried ore adds no terrain triangles: "+id);
            }
            Check(game.Registry.FistDrop(BlockId.IronOre)==BlockId.RawIron&&game.Registry.FistDrop(BlockId.CopperOre)==BlockId.RawCopper&&game.Registry.FistDrop(BlockId.CoalOre)==BlockId.Coal&&game.Registry.FistDrop(BlockId.GoldOre)==BlockId.RawGold&&game.Registry.FistDrop(BlockId.DiamondOre)==BlockId.Diamond,"Material revisions preserve distinct mined resources");
            var fixture=new GameObject("Ore variant review fixture");fixture.transform.position=player.transform.position+Vector3.up*8;
            player.enabled=false;player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
            var tempMeshes=new List<Mesh>();
            for(int i=0;i<ids.Length;i++)
            {
                var model=OreVisuals.Create(ids[i],fixture.transform);model.transform.localPosition=new Vector3((i%3)*1.6f,0,(i/3)*2.7f);
                Check(model.GetComponentInChildren<MeshFilter>().sharedMesh==mesh,"Variant instance shares the same mesh reference: "+ids[i]);
                var cells=new byte[34*34*34];cells[ChunkMesher.Index(0,0,0)]=ids[i];var data=ChunkMesher.Build(default,0,cells);
                var tileMesh=new Mesh{vertices=data.Vertices,normals=data.Normals,uv=data.UV,uv2=data.Tiles,triangles=data.Triangles};tempMeshes.Add(tileMesh);
                var cube=new GameObject("Terrain ore "+ids[i]);cube.transform.SetParent(fixture.transform,false);cube.transform.localPosition=model.transform.localPosition+new Vector3(0,0,1.15f);
                cube.AddComponent<MeshFilter>().sharedMesh=tileMesh;cube.AddComponent<MeshRenderer>().sharedMaterial=world.TerrainMaterial;
            }
            float savedFov=player.Camera.fieldOfView;player.Camera.fieldOfView=35;
            player.Camera.transform.position=fixture.transform.position+new Vector3(7,7,-6);player.Camera.transform.LookAt(fixture.transform.position+new Vector3(2,0,2.1f));
            yield return new WaitForSecondsRealtime(.5f);yield return Capture("ore-variants-unity");
            fixture.SetActive(false);int savedCap=Application.targetFrameRate;Application.targetFrameRate=-1;var samples=new List<float>();
            IEnumerator Sample(){samples.Clear();float until=Time.realtimeSinceStartup+3;while(Time.realtimeSinceStartup<until){yield return null;samples.Add(Time.unscaledDeltaTime*1000);}samples.Sort();}
            yield return Sample();float baseline=samples[samples.Count/2],baseline95=samples[(int)(samples.Count*.95f)];
            foreach(Transform child in fixture.transform)Destroy(child.gameObject);yield return null;
            for(int i=0;i<256;i++)
            {
                byte id=ids[i%ids.Length];var model=OreVisuals.Create(id,fixture.transform);model.transform.localPosition=new Vector3((i%16)*.30f,0,(i/16)*.30f);model.transform.localScale=Vector3.one*.25f;
                Check(model.GetComponentInChildren<MeshFilter>().sharedMesh==mesh&&model.GetComponentInChildren<Renderer>().sharedMaterial==OreVisuals.Material(id),"Stress instance shares geometry and material: "+i);
            }
            fixture.SetActive(true);yield return new WaitForSecondsRealtime(.5f);yield return Sample();
            File.WriteAllText(Path.Combine(output,"ore-sharing.txt"),$"Shared mesh: {Profiler.GetRuntimeMemorySizeLong(mesh)} runtime bytes, {mesh.vertexCount} vertices, 4129 triangles\n256 objects; one mesh reference; six shared world materials; maximum visible geometry 1057024 triangles before culling, excluding shadow passes\nBaseline median/p95 ms: {baseline:F3}/{baseline95:F3}\n256-prop median/p95 ms: {samples[samples.Count/2]:F3}/{samples[(int)(samples.Count*.95f)]:F3}\nEach sample 3 seconds; frame cap {Application.targetFrameRate}; vSync {QualitySettings.vSyncCount}; uncapped whole-frame timing, not isolated GPU time or a large-factory benchmark.\n");
            yield return Capture("ore-sharing-256");
            if(System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-rr-ore-refinement-review")>=0)
                yield return CompareOreMeshOrder(fixture,mesh);
            Application.targetFrameRate=savedCap;player.Camera.fieldOfView=savedFov;Destroy(fixture);foreach(var m in tempMeshes)Destroy(m);
            player.enabled=true;player.Arms.gameObject.SetActive(true);player.Body.gameObject.SetActive(true);player.Pitch=12;
            MeshFilter heldInstance=null;
            for(int i=0;i<ids.Length;i++)
            {
                game.Inventory.Add(ids[i],1,i,i+1);game.Selected=i;yield return new WaitForSecondsRealtime(.6f);
                Check(player.HeldBlock.Visible&&player.HeldBlock.ItemId==ids[i],"Held ore selection settles: "+ids[i]);
                var held=player.HeldBlock.Socket.GetComponentsInChildren<MeshFilter>().Single(m=>m.sharedMesh==mesh);
                if(i==0)heldInstance=held;else Check(held==heldInstance,"Switching ores reuses the existing held object: "+ids[i]);
                Check(held.GetComponent<Renderer>().sharedMaterial.GetTexture("_BaseMap")==OreVisuals.Palette(ids[i]),"Held ore uses the correct palette: "+ids[i]);
                yield return Capture("ore-held-"+ids[i]);
            }
            game.SetMode(ScreenMode.Inventory);yield return Capture("ore-variants-inventory");
        }
        IEnumerator CompareOreMeshOrder(GameObject fixture,Mesh original)
        {
            // Diagnostic only: never mutate the shared authored resource.
            var optimized=Instantiate(original);optimized.name="Ore mesh-order experiment";optimized.Optimize();
            var filters=fixture.GetComponentsInChildren<MeshFilter>();
            Check(optimized.vertexCount==original.vertexCount&&optimized.triangles.Length==original.triangles.Length&&optimized.bounds==original.bounds,"Mesh ordering preserves counts and bounds");
            var report=new System.Text.StringBuilder("Same player/process/camera; 256 props; original versus Mesh.Optimize clone; 1 second settle then 5 seconds uncapped per sample. ABBAAB order. Whole-frame times, not isolated GPU measurements.\n");
            foreach(bool useOptimized in new[]{false,true,true,false,false,true})
            {
                foreach(var filter in filters)filter.sharedMesh=useOptimized?optimized:original;
                yield return new WaitForSecondsRealtime(1);
                var times=new List<float>();float until=Time.realtimeSinceStartup+5;
                while(Time.realtimeSinceStartup<until){yield return null;times.Add(Time.unscaledDeltaTime*1000);}
                times.Sort();report.AppendLine($"{(useOptimized?"optimized":"original")}: median {times[times.Count/2]:F3} ms; p95 {times[(int)(times.Count*.95f)]:F3} ms; frames {times.Count}");
            }
            File.WriteAllText(Path.Combine(output,"ore-mesh-order.txt"),report.ToString());
            yield return Capture("ore-mesh-order-optimized");
            foreach(var filter in filters)filter.sharedMesh=original;
            yield return Capture("ore-mesh-order-original");Destroy(optimized);
        }
    }
}
