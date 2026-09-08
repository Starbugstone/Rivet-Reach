using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewTrees()
        {
            var world=game.World;var player=game.Player;sampling=true;
            Check(game.Inventory.Slots[11].Id==BlockId.StarterAxe&&game.Inventory.Slots[11].Count==1,"New session supplies one starter axe in hotbar slot 12");
            foreach(var tool in new[]{(slot:9,id:BlockId.StarterDagger,grip:GripPose.Tool),(slot:10,id:BlockId.StarterPickaxe,grip:GripPose.TwoHandTool)})
            {
                var stack=game.Inventory.Slots[tool.slot];var definition=game.Registry.Get(tool.id);
                Check(stack.Id==tool.id&&stack.Count==1&&definition.stackLimit==1,"New session supplies a single-stack "+definition.displayName);
                Check((game.Registry.Capabilities(stack)&ToolCapability.Axe)==0&&!BlockId.Placeable(tool.id),"Pickaxe/dagger cannot acquire axe felling or become terrain voxels");
                game.Selected=tool.slot;yield return new WaitForSecondsRealtime(.5f);
                Check(player.HeldBlock.Visible&&player.HeldBlock.ItemId==tool.id&&player.HeldBlock.DesiredGrip==tool.grip,"Hotbar selection displays the existing "+definition.displayName+" with its authored grip");
                yield return Capture("hotbar-"+tool.id);
                player.Pitch=-65;player.VerificationMining=true;yield return new WaitForSecondsRealtime(.7f);
                player.VerificationMining=false;player.Pitch=10;yield return new WaitForSecondsRealtime(.4f);
                Check(player.Arms.MiningWeight<.02f,"New hotbar tool completes its swing and returns to idle");
            }
            Check(TerrainGenerator.Version=="terrain-2-trees","Tree generation version is recorded");
            var natural=world.Generator.Trees(-35,-35,35,35).First(t=>t.Root.X>7&&t.Root.Z>7);
            Check(world.Get(natural.Root)==BlockId.Log&&world.Get(natural.Root.Offset(0,natural.Logs,0))==BlockId.Leaves,"Streamed natural tree contains logs and leaves");
            // Measure recognition separately from rendering/remeshing. Consume results so
            // the benchmark measures real queries, not an optimized-away expression.
            double Recognition(BlockPos p,int count,bool expected)
            {
                for(int i=0;i<100;i++)world.NaturalLog(p);
                int hits=0;var watch=System.Diagnostics.Stopwatch.StartNew();
                for(int i=0;i<count;i++)if(world.NaturalLog(p))hits++;
                watch.Stop();Check(hits==(expected?count:0),"Recognition benchmark preserves generated/placed identity");
                return watch.Elapsed.TotalMilliseconds*1000000/count;
            }
            report.generatedLogRecognitionNs=Recognition(natural.Root,100000,true);
            var placedProbe=new BlockPos(0,world.Generator.Height(0,0)+5,0);world.Place(placedProbe,BlockId.Log);
            report.placedLogRecognitionNs=Recognition(placedProbe,100000,false);world.Remove(placedProbe,BlockId.Log);
            var unloadedProbe=world.Generator.Trees(10000,10000,10040,10040).First().Root;
            Check(!world.Ready(unloadedProbe),"Recognition benchmark includes a nonresident generated log");
            report.unloadedLogRecognitionUs=Recognition(unloadedProbe,1000,true)/1000;

            player.enabled=false;player.transform.position=world.Local(natural.Root)+new Vector3(-5,1,-7);
            player.Camera.transform.position=player.transform.position+Vector3.up*1.6f;player.Camera.transform.LookAt(world.Local(natural.Root)+Vector3.up*2);
            game.Selected=11;game.Diagnostics=false;
            player.Arms.SetGrip(GripPose.Tool);player.Body.SetGrip(GripPose.Tool);
            for(int i=0;i<100;i++){player.Arms.Animate(0,false,0,true,false,false,Vector2.zero,0);player.Arms.FitFirstPersonFov(player.Camera.fieldOfView);player.HeldBlock.FrameFirstPerson();yield return null;}
            Check(player.HeldBlock.DesiredGrip==GripPose.Tool,"Selected gameplay axe uses the authored tool grip");
            yield return Capture("trees-before-cut");
            int before=game.Items.TotalSpawned;
            var cutClock=System.Diagnostics.Stopwatch.StartNew();
            Check(world.Mine(natural.Root,BlockId.Log,ToolCapability.Axe),"Axe can cut generated tree at its base");
            report.treeCutMs=cutClock.Elapsed.TotalMilliseconds;
            yield return new WaitForSecondsRealtime(4);
            Check(Enumerable.Range(0,natural.Logs).All(y=>world.Get(natural.Root.Offset(0,y,0))==0),"Axe removes every generated trunk log");
            Check(game.Items.TotalSpawned==before+natural.Logs,"Generated tree creates exactly one physical item per log");
            Check(world.Get(natural.Root.Offset(0,natural.Logs,0))==0,"Unsupported natural crown decays after felling");
            yield return Capture("trees-after-cut");
            // High fixture crosses both horizontal and vertical chunk seams.
            var cut=new BlockPos(31,95,31);player.transform.position=world.Local(cut)+new Vector3(-3,0,-3);
            yield return Settle();
            foreach(var tool in new[]{ToolCapability.None,ToolCapability.Pickaxe,ToolCapability.Blade,ToolCapability.Axe,ToolCapability.Axe|ToolCapability.Pickaxe})
            {
                for(int y=-1;y<=3;y++){var p=cut.Offset(0,y,0);if(world.Get(p)!=0)world.Remove(p,world.Get(p));Check(world.Place(p,BlockId.Log),"Fixture log placed across seam");}
                var branch=cut.Offset(1,2,0);if(world.Get(branch)!=0)world.Remove(branch,world.Get(branch));Check(world.Place(branch,BlockId.Log),"Connected branch crosses x seam");
                var separated=cut.Offset(3,2,0);if(world.Get(separated)!=0)world.Remove(separated,world.Get(separated));world.Place(separated,BlockId.Log);
                var bridge=cut.Offset(2,2,0);if(world.Get(bridge)!=0)world.Remove(bridge,world.Get(bridge));world.Place(bridge,BlockId.Leaves);
                before=game.Items.TotalSpawned;Check(world.Mine(cut,BlockId.Log,tool),"Mine command accepts "+tool);
                yield return new WaitForSecondsRealtime(.5f);
                bool axe=false; // Every fixture log here was placed by the player.
                Check(world.Get(cut.Offset(0,1,0))==(axe?0:BlockId.Log)&&world.Get(branch)==(axe?0:BlockId.Log),"Player-placed upper/branch logs survive every tool type: "+tool);
                Check(world.Get(cut.Offset(0,-1,0))==BlockId.Log&&world.Get(separated)==BlockId.Log,"Stump and leaf-connected neighbouring logs survive: "+tool);
                Check(game.Items.TotalSpawned==before+(axe?5:1),"No lost or duplicated log drops: "+tool);
                Check(!world.Mine(cut,BlockId.Log,tool),"Repeated stale mining command produces no drops");
            }
            game.Selected=11;int axeCount=game.Inventory.Total(BlockId.StarterAxe);
            Check(!game.CanPlace(cut,out _)&&!world.Place(cut,BlockId.StarterAxe)&&game.Inventory.Total(BlockId.StarterAxe)==axeCount,"Axe cannot become a placed voxel or be consumed by failed placement");
            game.Inventory.Add(BlockId.Log,2);game.Inventory.Add(BlockId.Leaves,2);
            game.Selected=Array.FindIndex(game.Inventory.Slots,s=>s.Id==BlockId.Log);yield return new WaitForSecondsRealtime(.1f);
            Check(player.HeldBlock.DesiredGrip==GripPose.Block,"Collected logs use the held-block presentation");
            // Exercise the player hold-to-mine path with the selected tool definition.
            for(int y=0;y<=3;y++)if(world.Get(cut.Offset(0,y,0))==0)world.Place(cut.Offset(0,y,0),BlockId.Log);
            var support=cut.Offset(-3,-2,0);world.Place(support,BlockId.Stone);
            player.transform.position=world.Local(support)+new Vector3(.5f,1.001f,.5f);
            player.Yaw=90;player.Pitch=3;player.enabled=true;game.Selected=11;
            yield return new WaitForSecondsRealtime(.4f);
            Check(player.HasTarget&&player.Target.Equals(cut),"Player aims at the fixture's middle log");
            before=game.Items.TotalSpawned;player.VerificationMining=true;
            float until=Time.realtimeSinceStartup+4;
            while(world.Get(cut)!=0&&Time.realtimeSinceStartup<until)yield return null;
            player.VerificationMining=false;yield return new WaitForSecondsRealtime(.7f);
            Check(world.Get(cut)==0&&world.Get(cut.Offset(0,3,0))==BlockId.Log&&game.Items.TotalSpawned==before+1,"Holding Mine with the selected axe removes only the targeted player-placed base block");
            Check(player.HeldBlock.Visible&&player.HeldBlock.ItemId==BlockId.StarterAxe,"Axe is visibly held after grip transition");
            yield return Capture("axe-held-after-mining");
            float originalFov=player.Camera.fieldOfView;bool originalFemale=player.Female;int originalSkin=player.Skin;
            foreach(bool female in new[]{false,true})foreach(float fov in new[]{60f,78f,100f})
            {
                game.SetAppearance(female,0);player.Camera.fieldOfView=fov;yield return new WaitForSecondsRealtime(.7f);
                var renderer=player.HeldBlock.Socket.GetComponentsInChildren<MeshRenderer>().First(r=>r.gameObject.activeInHierarchy);
                var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                Vector3 min=Vector3.one*float.MaxValue,max=Vector3.one*float.MinValue;
                foreach(var vertex in mesh.vertices)
                {var screen=player.Camera.WorldToViewportPoint(renderer.transform.TransformPoint(vertex));min=Vector3.Min(min,screen);max=Vector3.Max(max,screen);}
                Check(mesh.triangles.Length/3==752&&mesh.subMeshCount==1,"Imported original axe has 752 triangles and one material");
                Check(min.z>player.Camera.nearClipPlane&&min.x>.60f&&max.y<.78f&&max.y>.25f,"Axe stays right of aim with lower screen framing at "+fov+" FOV, "+(female?"female":"male")+": "+min+" to "+max);
                Check(Vector3.Dot(player.HeldBlock.AxeBladeForward,player.Camera.transform.forward)>.7f,"Axe cutting edge faces the chopping direction at rest");
                yield return Capture("axe-"+(female?"female":"male")+"-fov"+fov);
            }
            game.SetAppearance(originalFemale,originalSkin);player.Camera.fieldOfView=originalFov;
            // The held assembly must remain attached throughout the existing mining sweep.
            player.VerificationMining=true;
            for(int frame=0;frame<32;frame++)
            {
                yield return null;
                Check(Vector3.Dot(player.HeldBlock.AxeBladeForward,player.Camera.transform.forward)>.25f,"Swing keeps the axe cutting edge facing away from the player");
                if(frame==9)yield return Capture("axe-swing");
            }
            player.VerificationMining=false;
            yield return new WaitForSecondsRealtime(.5f);

            player.enabled=false;
            var placedLeaf=cut.Offset(0,4,0);if(world.Get(placedLeaf)!=0)world.Remove(placedLeaf,world.Get(placedLeaf));world.Place(placedLeaf,BlockId.Leaves);
            world.Place(cut.Offset(0,3,0),BlockId.Log);world.Mine(cut.Offset(0,3,0),BlockId.Log,ToolCapability.Axe);
            yield return new WaitForSecondsRealtime(3);
            Check(world.Get(placedLeaf)==BlockId.Leaves,"Player-placed leaves do not decay after their supporting log is removed");
            var naturalTests=world.Generator.Trees(-40,-40,40,40).Where(t=>world.NaturalLog(t.Root)&&t.Root.X<0&&t.Root.Z<0).Take(7).ToArray();
            Check(naturalTests.Length==7,"Enough untouched generated trees are available for provenance tests");
            int treeIndex=0;
            foreach(var tool in new[]{ToolCapability.None,ToolCapability.Pickaxe,ToolCapability.Blade,ToolCapability.Axe,ToolCapability.Axe|ToolCapability.Pickaxe})
            {
                var tree=naturalTests[treeIndex++];var middle=tree.Root.Offset(0,1,0);var baseLog=middle.Offset(1,0,0);
                if(world.Get(baseLog)!=0)world.Remove(baseLog,world.Get(baseLog));world.Place(baseLog,BlockId.Log);
                before=game.Items.TotalSpawned;Check(world.Mine(middle,BlockId.Log,tool),"Middle cut of a generated tree accepts "+tool);
                yield return new WaitForSecondsRealtime(.5f);bool axeTool=(tool&ToolCapability.Axe)!=0;
                Check(world.Get(tree.Root)==BlockId.Log&&world.Get(baseLog)==BlockId.Log,"Generated stump and adjoining player construction remain intact: "+tool);
                Check(world.Get(tree.Root.Offset(0,tree.Logs-1,0))==(axeTool?0:BlockId.Log)&&game.Items.TotalSpawned==before+(axeTool?tree.Logs-1:1),"Only axe capability fells generated logs above the cut: "+tool);
            }
            var replacedTree=naturalTests[5];before=game.Items.TotalSpawned;
            Check(world.Mine(replacedTree.Root,BlockId.Log,ToolCapability.Axe),"Generated cut queues upper logs");
            var replacedLog=replacedTree.Root.Offset(0,1,0);world.Remove(replacedLog,BlockId.Log);world.Place(replacedLog,BlockId.Log);
            yield return new WaitForSecondsRealtime(.5f);
            Check(world.Get(replacedLog)==BlockId.Log&&world.Get(replacedLog.Offset(0,1,0))==BlockId.Log&&game.Items.TotalSpawned==before+1,"Queued felling rechecks origin and stops at a replaced player log");
            var unloadedTree=naturalTests[6];game.SetMode(ScreenMode.Pause);before=game.Items.TotalSpawned;
            Check(world.Mine(unloadedTree.Root,BlockId.Log,ToolCapability.Axe),"Generated felling operation is queued before unloading");
            var returnPoint=WorldPoint.FromLocal(player.transform.position,world.Origin);
            player.transform.position+=new Vector3(1024,0,0);yield return Settle();
            Check(!world.Ready(unloadedTree.Root)&&world.Trees.PendingFells>0,"Generated log queue persists while its chunks are unloaded and paused");
            game.SetMode(ScreenMode.Play);yield return new WaitForSecondsRealtime(.7f);
            Check(world.Get(unloadedTree.Root.Offset(0,unloadedTree.Logs-1,0))==0&&game.Items.TotalSpawned==before+unloadedTree.Logs,"Generated upper logs fell once even after their chunks unload");
            player.transform.position=returnPoint.Local(world.Origin);yield return Settle();
            Check(world.Get(unloadedTree.Root)==0&&world.Get(unloadedTree.Root.Offset(0,unloadedTree.Logs-1,0))==0,"Generated felling edits survive reloading the tree chunks");
            var saved=WorldPoint.FromLocal(player.transform.position,world.Origin);
            player.transform.position+=new Vector3(1024,0,0);yield return Settle();
            Check(!world.Ready(cut),"Tree test chunks unload after distant travel");
            player.transform.position=saved.Local(world.Origin);yield return Settle();
            Check(world.Get(cut)==0&&world.Get(cut.Offset(0,1,0))==BlockId.Log&&world.Get(placedLeaf)==BlockId.Leaves,"Felling edits and placed leaves survive unload/reload and origin shifts");
            Check(world.Get(natural.Root)==0&&world.Get(natural.Root.Offset(0,natural.Logs,0))==0,"Generated tree and decayed crown stay removed after streaming reload");
            report.terrainTilePixels=Resources.Load<Texture2DArray>("Materials/BlockTiles").width;report.triangles=world.MeshTriangles;
            report.treeTickMaxMs=world.MaxTreeTickMs;sampling=false;
        }
    }
}
