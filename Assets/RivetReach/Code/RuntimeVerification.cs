using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Profiling;

namespace RivetReach
{
    // Explicit command-line test mode. Ordinary new games never receive fixtures or automation.
    public sealed partial class RuntimeVerification : MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public string timestamp,unity,cpu,gpu,result,workload;
            public int bodyShadowTriangles,terrainTilePixels;
            public int memoryMB,width,height,assertions,residentPeak,triangles,maleTriangles,femaleTriangles,armsTriangles,bodyTriangles,drawCallsPeak;
            public long allocatedMemoryBytes;
            public float frameMedianMs,frameP95Ms,frameMaxMs,firstReadySeconds,miningFrameMaxMs;
            public double miningMeshMs,placementMeshMs,grassTickMaxMs;
            public int grassChanges;
            public int viewRadius;public float fogStart,fogEnd;
            public string[] checks,errors;
        }
        Expedition game;string output;
        readonly List<string> checks=new List<string>(),errors=new List<string>();
        readonly List<float> frames=new List<float>();
        Report report=new Report();bool sampling,miningSample;ProfilerRecorder drawCalls;
        void Awake(){Application.logMessageReceived+=Log;drawCalls=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");}
        void Log(string condition,string stack,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)errors.Add(condition+"\n"+stack);}
        IEnumerator Start()
        {
            game=Expedition.Instance;var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-rr-output");
            output=index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.persistentDataPath,"Verification");Directory.CreateDirectory(output);
            var routines=new Stack<IEnumerator>();routines.Push(Run());
            while(routines.Count>0)
            {
                bool more;object current=null;
                try{more=routines.Peek().MoveNext();if(more)current=routines.Peek().Current;}
                catch(Exception ex){errors.Add(ex.ToString());break;}
                if(!more){routines.Pop();continue;}
                if(current is IEnumerator nested){routines.Push(nested);continue;}
                yield return current;
            }
            sampling=false;report.timestamp=DateTime.UtcNow.ToString("O");report.unity=Application.unityVersion;report.cpu=SystemInfo.processorType;report.gpu=SystemInfo.graphicsDeviceName;
            report.memoryMB=SystemInfo.systemMemorySize;report.width=Screen.width;report.height=Screen.height;report.assertions=checks.Count;
            report.drawCallsPeak=report.drawCallsPeak>0?report.drawCallsPeak:-1;
            report.checks=checks.ToArray();report.errors=errors.ToArray();report.result=errors.Count==0?"PASS":"FAIL";
            if(frames.Count>0){frames.Sort();report.frameMedianMs=frames[frames.Count/2];report.frameP95Ms=frames[(int)((frames.Count-1)*.95f)];report.frameMaxMs=frames[frames.Count-1];}
            File.WriteAllText(Path.Combine(output,"runtime-report.json"),JsonUtility.ToJson(report,true));
            Application.Quit(errors.Count==0?0:1);
        }
        void Update(){if(miningSample)report.miningFrameMaxMs=Math.Max(report.miningFrameMaxMs,Time.unscaledDeltaTime*1000);if(sampling){frames.Add(Time.unscaledDeltaTime*1000);if(drawCalls.Valid)report.drawCallsPeak=Math.Max(report.drawCallsPeak,(int)drawCalls.LastValue);report.allocatedMemoryBytes=Math.Max(report.allocatedMemoryBytes,UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());}if(game!=null)report.residentPeak=Math.Max(report.residentPeak,game.World.ResidentCount);}
        void Check(bool value,string message){if(!value)throw new Exception("Runtime check failed: "+message);checks.Add(message);}
        IEnumerator Settle(float timeout=60)
        {
            float until=Time.realtimeSinceStartup+timeout;
            while((!game.ReadyToPlay||game.World.PendingCount>0)&&Time.realtimeSinceStartup<until)yield return null;
            Check(game.ReadyToPlay&&game.World.PendingCount==0,"Terrain demand drained with safe player residency");
            yield return new WaitForSecondsRealtime(.3f);
        }
        IEnumerator Capture(string name)
        {
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.4f);
        }
        IEnumerator Run()
        {
            // Nested enumerators are driven by Unity. Check failures are also captured by Log().
            float began=Time.realtimeSinceStartup;game.World.ViewDistance=10;game.StartSession(246813);game.Diagnostics=true;
            yield return Settle();report.firstReadySeconds=Time.realtimeSinceStartup-began;
            var player=game.Player;var world=game.World;report.viewRadius=world.ViewDistance;report.fogStart=world.FogStart;report.fogEnd=world.FogEnd;var start=world.Address(player.transform.position);var saved=WorldPoint.FromLocal(player.transform.position,world.Origin);
            if(Array.Exists(Environment.GetCommandLineArgs(),arg=>arg=="-rr-interaction-review"))
            {report.workload="Grass and hand interaction review";yield return ReviewInteractions();yield break;}
            bool visualOnly=Array.Exists(Environment.GetCommandLineArgs(),arg=>arg=="-rr-visual-review");
            report.workload=visualOnly?"Visual review and first-person body regression":"Full terrain/inventory and visual regression";
            yield return ReviewVisuals();
            if(visualOnly)yield break;
            Check(!world.Overlaps(player.transform.position,.6f,1.8f),"Spawn has headroom and no solid overlap");
            Check(world.Raycast(player.transform.position+Vector3.up,Vector3.down,4,out var ground,out byte id),"Axis-aligned ray hits ground");
            Check(id!=0,"Spawn is supported by terrain");
            // Test both unready horizontal frontiers above the terrain, avoiding unrelated walls.
            var centre=world.Address(player.transform.position).Chunk;
            foreach(int sign in new[]{-1,1})
            {
                long edge=(centre.X+(sign>0?world.ViewDistance+1:-world.ViewDistance))*32;
                int y=(centre.Y+1)*32+2;
                var inside=new BlockPos(sign>0?edge-1:edge,y,centre.Z*32+16);
                var outside=new BlockPos(sign>0?edge:edge-1,y,centre.Z*32+16);
                Check(world.Ready(inside)&&!world.Ready(outside),"Frontier fixture has ready and unready neighbours");
                Vector3 from=world.Local(inside)+Vector3.one*.5f;
                Vector3 stopped=world.Move(from,Vector3.right*sign*2,.6f,1.8f,out _);
                Check(Mathf.Abs(stopped.x-from.x)<.3f,"Swept collision holds against an unready frontier");
            }
            yield return Capture("01-world");
            // Queue real Input System keyboard states so action mapping and movement run normally.
            float baselineY=player.transform.position.y;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(game.Input.Keys["Jump"]));yield return null;yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSecondsRealtime(.2f);
            Check(player.transform.position.y>baselineY+.5f,"Mapped jump raises the grounded player");
            yield return new WaitForSecondsRealtime(.7f);
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(game.Input.Keys["Crouch"]));yield return null;yield return null;
            Check(player.Height<1.5f,"Mapped crouch changes presentation and collision height");
            Check(player.VisualEyeHeight>1.10f&&player.VisualEyeHeight<1.64f,"Crouch camera moves continuously while collision height responds immediately");
            yield return new WaitForSecondsRealtime(.3f);
            Check(Mathf.Abs(player.VisualEyeHeight-1.09f)<.012f,"Crouch eye transition reaches its target within 300 ms");
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;yield return null;
            Check(player.Height==1.8f,"Releasing crouch restores height with headroom");
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(game.Input.Keys["Inventory"]));yield return null;yield return null;
            Check(game.InventoryOpen&&Cursor.lockState==CursorLockMode.None,"Inventory key releases pointer");
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(game.Input.Keys["Inventory"]));yield return null;yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
            Check(game.Mode==ScreenMode.Play,"Inventory key resumes exploration");
            // Exercise the real hold-to-mine path with camera facing the ground.
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());player.Pitch=80;yield return null;miningSample=true;player.VerificationMining=true;
            float until=Time.realtimeSinceStartup+3;
            while(game.Items.TotalSpawned==0&&Time.realtimeSinceStartup<until)yield return null;
            player.VerificationMining=false;InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;miningSample=false;
            Check(game.Items.TotalSpawned==1,"Fist hold removes exactly one addressed block");report.miningMeshMs=world.LastEditMeshMs;
            var removed=player.Target; // The frame after removal can target deeper terrain; retain ground below feet as fallback.
            if(world.Get(removed)!=0)removed=ground;
            yield return new WaitForSecondsRealtime(1);
            Check(game.Items.TotalSpawned==1,"Mining creates one item (actual "+game.Items.TotalSpawned+")");
            Check(game.Inventory.Total(1)+game.Items.Total(1)==0&&game.Inventory.Total(2)+game.Items.Total(2)==1,"Fist-mined surface grass yields one dirt and no grass item");
            Check(game.Inventory.Total(1)+game.Inventory.Total(2)+game.Inventory.Total(3)+game.Items.Piles.Sum(p=>p.Stack.Count)==1,"Mining/pickup conserves quantity");
            player.Pitch=20;yield return Capture("02-mining");
            // Exercise face targeting and the real opposite-button placement action.
            game.Inventory.Add(3,4);game.Selected=Array.FindIndex(game.Inventory.Slots,stack=>stack.Id==3);
            player.transform.position=saved.Local(world.Origin);player.Yaw=90;player.Pitch=48;yield return null;
            Check(game.PlacementPreview(out var placed,out _),"Aimed block face offers a valid adjacent placement cell");
            int stoneBefore=game.Inventory.Total(3);
            var placementButton=PlayerPrefs.GetInt("mineButton",0)==0?MouseButton.Right:MouseButton.Left;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(placementButton));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(world.Get(placed)==3&&game.Inventory.Total(3)==stoneBefore-1,"Mapped placement adds one voxel and consumes exactly one selected item");
            report.placementMeshMs=world.LastEditMeshMs;
            Check(world.Overlaps(world.Local(placed)+new Vector3(.5f,.01f,.5f),.6f,1.8f),"Placed block participates in collision immediately");
            Check(!world.Place(placed,3)&&!game.CanPlace(placed,out _),"An occupied cell rejects placement");
            int quantity=game.Inventory.Total(3);Check(!game.CanPlace(world.Address(player.transform.position),out _)&&game.Inventory.Total(3)==quantity,"Player overlap rejects placement without consuming inventory");
            game.SetMode(ScreenMode.Inventory);Check(!game.TryPlaceSelected(),"Inventory mode suppresses placement");game.SetMode(ScreenMode.Play);
            int placementSlot=game.Selected;game.Selected=11;Check(!game.TryPlaceSelected(),"An empty selected slot cannot create blocks");game.Selected=placementSlot;
            Check(!world.Place(new BlockPos(99999,90,99999),3),"Unready terrain rejects placement");
            player.transform.position=saved.Local(world.Origin)+Vector3.left*2;player.Pitch=20;yield return new WaitForSecondsRealtime(1.1f);yield return Capture("02b-placement");
            // Two neighbouring edited seam blocks must survive unload and a floating-origin shift.
            var seam=new BlockPos(31,world.Generator.Height(31,0),0);var seam2=new BlockPos(32,world.Generator.Height(32,0),0);
            Check(world.Remove(seam,world.Get(seam))&&world.Remove(seam2,world.Get(seam2)),"Edits on both sides of a chunk seam commit");
            world.Remove(seam,1);Check(world.Get(seam)==0&&world.Get(seam2)==0,"Repeated stale mining does not restore blocks");
            // Item fixture is deliberately outside pickup range and kept across unload.
            var pilePos=new BlockPos(20,world.Generator.Height(20,0)+1,0);
            game.Items.Spawn(new ItemStack(3,450),world.Local(pilePos)+new Vector3(.5f,.01f,.5f),Vector3.zero);
            game.Items.Spawn(new ItemStack(3,100),world.Local(pilePos)+new Vector3(.7f,.01f,.5f),Vector3.zero);
            yield return new WaitForSecondsRealtime(1);
            Check(game.Items.Total(3)>=550,"Physical stack merge preserves overflow");
            Check(game.Items.Piles.All(p=>p.Stack.Count<=500),"World pile stack limits hold");
            Check(game.Items.Piles.Any(p=>p.Stack.Id==3&&p.Stack.Count==500)&&game.Items.Piles.Any(p=>p.Stack.Id==3&&p.Stack.Count==50),"Two settled piles merge once with exact overflow");
            float oldest=game.Items.Piles.Max(p=>p.Age);
            var far=new BlockPos(640,world.Generator.Height(640,0)+2,0);
            player.transform.position=world.Local(far)+new Vector3(.5f,0,.5f);yield return null;yield return Settle();
            Check(world.Origin.X!=0,"Long traversal shifts the rendering origin");Check(!world.Ready(seam),"Old chunks unload");
            Check(game.Items.Piles.Max(p=>p.Age)<=oldest+.15f,"Unloaded item lifetime is suspended");
            sampling=true;
            for(int i=0;i<180;i++)
            {
                // A controlled flight path exercises continuous streaming independent of slopes.
                var p=new BlockPos(640+i,world.Generator.Height(640+i,0)+3,0);player.transform.position=world.Local(p)+new Vector3(.5f,0,.5f);
                yield return new WaitForSecondsRealtime(.025f);
            }
            yield return Settle();sampling=false;report.triangles=world.MeshTriangles;
            player.transform.position=saved.Local(world.Origin)+Vector3.up;yield return null;yield return Settle();
            Check(world.Get(seam)==0&&world.Get(seam2)==0,"Mined seam remains empty after unload/reload/origin shift");
            Check(game.Items.Total(3)>=550,"Dropped quantities survive chunk unloading");
            Check(world.Get(placed)==3,"Placed voxel survives chunk unload, reload and origin shifts");
            // Remine the placed cell through the same fist path, proving one recoverable item.
            Vector3 aim=(world.Local(placed)+Vector3.one*.5f)-(player.transform.position+Vector3.up*1.64f);
            player.Yaw=Mathf.Atan2(aim.x,aim.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Atan2(aim.y,new Vector2(aim.x,aim.z).magnitude)*Mathf.Rad2Deg;
            yield return null;Check(player.HasTarget&&player.Target.Equals(placed),"Placed block is targetable for fist mining");
            int remineBefore=game.Items.TotalSpawned;player.VerificationMining=true;until=Time.realtimeSinceStartup+3;
            while(world.Get(placed)!=0&&Time.realtimeSinceStartup<until)yield return null;
            player.VerificationMining=false;yield return null;
            Check(world.Get(placed)==0&&game.Items.TotalSpawned==remineBefore+1,"Fist mining a placed block creates exactly one recoverable item");
            // Fill every slot, leave exactly five spaces, then exercise real partial pickup.
            var carried=game.Inventory.Slots.ToArray();for(int slot=0;slot<60;slot++)game.Inventory.Take(slot,int.MaxValue);
            game.Inventory.Add(3,29995);
            // Isolate this quantity fixture from the just-mined drop: it may still be in
            // pickup/merge range depending on frame timing. Ordinary item rules are unchanged.
            var priorDelays=game.Items.Piles.Select(p=>(pile:p,delay:p.Delay)).ToArray();
            foreach(var savedDelay in priorDelays)savedDelay.pile.Delay=Math.Max(1,savedDelay.delay);
            game.Items.Spawn(new ItemStack(3,10),player.transform.position+Vector3.up*.35f,Vector3.zero);
            var partial=game.Items.Piles[game.Items.Piles.Count-1];game.Items.Step(.05f);
            Check(game.Inventory.Total(3)==30000&&partial.Stack.Count==5,"Partial pickup fills five spaces and leaves five in world");
            game.Items.Step(.05f);Check(partial.Stack.Count==5,"Full inventory does not consume remaining world items");
            foreach(var savedDelay in priorDelays)savedDelay.pile.Delay=savedDelay.delay;
            game.Items.Piles.Remove(partial);if(partial.View!=null)Destroy(partial.View);
            for(int slot=0;slot<60;slot++)game.Inventory.Take(slot,int.MaxValue);
            foreach(var stack in carried)if(!stack.Empty)game.Inventory.Add(stack.Id,stack.Count);
            // Walk across a prepared, naturally flat local strip with mapped input.
            BlockPos walk=default;bool strip=false;
            for(int z=-12;z<12&&!strip;z++)for(int x=-12;x<12&&!strip;x++)
            {
                int h=world.Generator.Height(x,z);
                if(Enumerable.Range(0,5).All(dx=>world.Get(new BlockPos(x+dx,h,z))!=0&&world.Get(new BlockPos(x+dx,h+1,z))==0&&world.Get(new BlockPos(x+dx,h+2,z))==0))
                {walk=new BlockPos(x,h+1,z);strip=true;}
            }
            Check(strip,"A natural walking test strip exists");player.transform.position=world.Local(walk)+new Vector3(.5f,.01f,.5f);player.Yaw=90;
            yield return new WaitForSecondsRealtime(.3f);float walkStart=player.transform.position.x;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(game.Input.Keys["Forward"]));yield return new WaitForSecondsRealtime(.4f);
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;
            Check(player.transform.position.x-walkStart>1,"Mapped forward movement traverses solid terrain");
            player.transform.position=saved.Local(world.Origin)+Vector3.up;player.Yaw=25;yield return new WaitForSecondsRealtime(.3f);
            // Real inventory mutations, then UI representations and held-stack close.
            game.Inventory.Add(1,28);game.Inventory.Add(2,142);game.Inventory.Add(3,64);
            game.SetMode(ScreenMode.Inventory);game.UI.ClickSlot(0,true,false);
            byte heldId=game.UI.HeldStack.Id;int before=game.Inventory.Total(heldId)+game.UI.HeldStack.Count;game.SetMode(ScreenMode.Play);
            Check(game.UI.HeldStack.Empty&&game.Inventory.Total(heldId)==before,"Closing inventory returns held stack without loss");
            game.SetMode(ScreenMode.Inventory);yield return Capture("03-inventory");
            game.SetMode(ScreenMode.Appearance);game.SetAppearance(false,0);yield return null;
            report.armsTriangles=player.Arms.TriangleCount;report.bodyTriangles=player.Body.TriangleCount;
            report.maleTriangles=Resources.Load<GameObject>("Characters/ExplorerMale").GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.triangles.Length/3;
            yield return Capture("04-male");game.SetAppearance(false,1);game.UI.Rebuild();yield return Capture("04b-male-alternate");game.SetAppearance(true,0);game.UI.Rebuild();yield return Capture("05-female");
            report.femaleTriangles=Resources.Load<GameObject>("Characters/ExplorerFemale").GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.triangles.Length/3;
            game.SetAppearance(true,1);game.UI.Rebuild();yield return Capture("06-alternate-skin");
            Check(Mathf.Approximately(player.Height,1.8f),"Appearance changes preserve gameplay height");
            Check(report.maleTriangles<=35000&&report.femaleTriangles<=35000,"Both imported player meshes meet revised triangle review budget");
            Check(report.armsTriangles<=12000,"Derived first-person hands and arms meet revised triangle review budget");
            game.SetAppearance(false,0);game.SetMode(ScreenMode.Settings);yield return Capture("07-settings");
            game.SetMode(ScreenMode.Controls);yield return Capture("08-controls");
            // Inspect an actually generated underground cavity with a supported two-cell opening.
            bool caveFound=false;BlockPos cave=default;
            for(int z=-32;z<=32&&!caveFound;z+=2)for(int x=-32;x<=32&&!caveFound;x+=2)for(int y=15;y<60&&!caveFound;y++)
            {
                var p=new BlockPos(x,y,z);
                if(world.Generator.At(p)==0&&world.Generator.At(p.Offset(0,1,0))==0&&world.Generator.At(p.Offset(0,-1,0))!=0&&y<world.Generator.Height(x,z)-5)
                {cave=p;caveFound=true;}
            }
            Check(caveFound,"Generator contains supported underground caves");game.SetMode(ScreenMode.Play);
            player.transform.position=world.Local(cave)+new Vector3(.5f,.01f,.5f);player.Pitch=0;player.Yaw=55;yield return null;yield return Settle();
            Check(!world.Overlaps(player.transform.position,.6f,1.8f),"Cave collision permits a supported player");yield return Capture("09-cave");
            game.SetMode(ScreenMode.Title);yield return Capture("10-title");
            yield return ReviewInteractions();
            Check(world.Error==null,"No terrain worker errors");
        }
        IEnumerator ReviewVisuals()
        {
            var player=game.Player;bool female=player.Female;int skin=player.Skin;
            float fov=player.Camera.fieldOfView,pitch=player.Pitch;
            var tiles=Resources.Load<Texture2DArray>("Materials/BlockTiles");report.terrainTilePixels=tiles.width;
            Check(tiles.width==64&&tiles.height==64&&tiles.depth==4,"Four 64-texel terrain tiles are imported");
            Check(game.World.TerrainMaterial.shader.isSupported,"Terrain shader has a supported rendering pass");
            Check(player.Camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing,"Gameplay camera enables the scene colour grade");
            game.Diagnostics=false;
            player.Pitch=10;yield return Capture("visual-01-landscape");
            player.Pitch=-25;yield return Capture("visual-02-sky");
            foreach(bool variant in new[]{false,true})foreach(int variantSkin in new[]{0,1})
            {
                game.SetAppearance(variant,variantSkin);yield return null;
                var body=player.Body.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly);
                var weights=body.sharedMesh.boneWeights;
                Check(weights.Any(w=>body.bones[w.boneIndex0].name=="Chest")&&weights.Any(w=>body.bones[w.boneIndex0].name=="Spine"),"First-person jacket and waist survive the visibility filter");
                int full=Resources.Load<GameObject>("Characters/"+(variant?"ExplorerFemale":"ExplorerMale")).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.triangles.Length/3;
                if(variant)report.femaleTriangles=full;else report.maleTriangles=full;
                Check(player.Body.ShadowTriangleCount==full,"First-person player casts the complete model silhouette");
                report.bodyTriangles=player.Body.TriangleCount;report.armsTriangles=player.Arms.TriangleCount;report.bodyShadowTriangles=player.Body.ShadowTriangleCount;
                string label=(variant?"female":"male")+"-skin"+variantSkin;
                foreach(float angle in new[]{60f,85f})
                {
                    player.Pitch=angle;player.Camera.fieldOfView=78;
                    yield return new WaitForSecondsRealtime(.25f);yield return Capture("visual-lookdown-"+label+"-"+angle);
                    Check(player.Camera.WorldToViewportPoint(player.Body.BonePosition("Neck")).y<0,"Standing look-down keeps the neck opening below the lens");
                }
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(game.Input.Keys["Crouch"]));
                yield return new WaitForSecondsRealtime(.4f);
                Check(player.Height<1.5f&&player.Body.CrouchWeight>.9f,"Look-down capture uses the real crouching pose");
                Check(player.Camera.WorldToViewportPoint(player.Body.BonePosition("Neck")).y<0,"Crouching look-down keeps the neck opening below the lens");
                yield return Capture("visual-crouch-"+label);
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSecondsRealtime(.4f);
            }
            game.SetAppearance(false,0);
            foreach(float lens in new[]{60f,100f})
            {
                player.Camera.fieldOfView=lens;player.Pitch=85;yield return new WaitForSecondsRealtime(.25f);
                Check(player.Camera.WorldToViewportPoint(player.Body.BonePosition("Neck")).y<0,"Neck opening stays outside the 60/100-degree camera range");
                yield return Capture("visual-lookdown-fov"+lens);
            }
            // Warm steady scene sample; separate from the streaming/mining timings below.
            player.Camera.fieldOfView=fov;player.Pitch=10;sampling=true;
            yield return new WaitForSecondsRealtime(3);sampling=false;
            game.Inventory.Add(1,24);game.Inventory.Add(2,48);game.Inventory.Add(3,12);
            yield return Capture("visual-03-hotbar");
            game.Inventory.Take(0,int.MaxValue);game.Inventory.Take(1,int.MaxValue);game.Inventory.Take(2,int.MaxValue);
            game.SetAppearance(female,skin);player.Pitch=pitch;game.Diagnostics=true;
            Check(errors.Count==0,"Body views and scene polish complete without Unity errors");
        }
        void OnDestroy(){Application.logMessageReceived-=Log;drawCalls.Dispose();}
    }
}
