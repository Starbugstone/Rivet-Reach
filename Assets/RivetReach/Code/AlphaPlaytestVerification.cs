using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        void AlphaPut(BlockPos p,byte id)
        {
            var world=game.World;byte old=world.Get(p);if(old==id)return;
            if(old!=0&&!world.Remove(p,old))throw new Exception("Cannot clear alpha fixture "+p);
            if(id==BlockId.Farmland)
            {if(!world.Place(p,BlockId.Dirt)||!world.Till(p))throw new Exception("Cannot till alpha fixture "+p);return;}
            if(CropRules.For(id) is CropDefinition crop)
            {if(!world.Plant(p,crop.planting))throw new Exception("Cannot plant alpha fixture");while(world.Get(p)<id)if(!world.Grow(p,world.Get(p)))throw new Exception("Cannot grow alpha fixture");return;}
            if(id!=0&&!(Fluids.IsFluid(id)?world.ChangeFluid(p,0,id):world.Place(p,id)))throw new Exception("Cannot place alpha fixture "+p+" id "+id);
        }
        void AlphaAim(BlockPos p)
        {
            var player=game.Player;Vector3 direction=(game.World.Local(p)+Vector3.one*.5f-player.Camera.transform.position).normalized;
            player.Yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(direction.y)*Mathf.Rad2Deg;
        }
        IEnumerator AlphaSceneCapture(string name)
        {
            // A detached fixture camera records the world without a stale first-person
            // body/hand or target label from the paused input component.
            var player=game.Player;var canvas=game.UI.GetComponentInChildren<Canvas>();
            bool body=player.Body.gameObject.activeSelf,arms=player.Arms.gameObject.activeSelf,ui=canvas.enabled;
            player.Body.gameObject.SetActive(false);player.Arms.gameObject.SetActive(false);canvas.enabled=false;
            try{yield return Capture(name);}
            finally{player.Body.gameObject.SetActive(body);player.Arms.gameObject.SetActive(arms);canvas.enabled=ui;}
        }
        IEnumerator ReviewAlphaPlaytest()
        {
            FreezeSaveFixture();game.enabled=false;game.Animals.enabled=false;game.SetCreative(false);yield return Settle();
            var world=game.World;var player=game.Player;var floor=world.Address(player.transform.position).Offset(0,2,0);
            for(int z=-5;z<=5;z++)for(int y=0;y<=5;y++)for(int x=-5;x<=5;x++)AlphaPut(floor.Offset(x,y,z),y==0?BlockId.Stone:(byte)0);
            player.transform.position=world.Local(floor)+new Vector3(.5f,1.01f,.5f);player.ResetMotion();game.SetMode(ScreenMode.Play);player.enabled=true;
            yield return new WaitForSecondsRealtime(.4f);
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Space));
            int rises=0;bool wasGrounded=player.Grounded;float previousY=player.transform.position.y,maxY=previousY;float until=Time.realtimeSinceStartup+3.5f;
            while(Time.realtimeSinceStartup<until)
            {
                yield return null;if(wasGrounded&&!player.Grounded&&player.transform.position.y>previousY)rises++;
                maxY=Mathf.Max(maxY,player.transform.position.y);wasGrounded=player.Grounded;previousY=player.transform.position.y;
            }
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return new WaitForSecondsRealtime(1);
            Check(rises>=3&&maxY-world.Local(floor).y<2.9f,"Held Space repeats grounded jumps without an air-jump height gain");
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            var target=floor.Offset(0,1,3);AlphaPut(target,BlockId.Cobblestone);game.Inventory.Add(BlockId.Cobblestone,11,Inventory.HotbarCount,Inventory.HotbarCount+1);
            game.Inventory.Add(BlockId.Dirt,7,0,1);game.Selected=0;AlphaAim(target);yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Middle));yield return null;yield return null;InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(game.Inventory.Slots[0].Id==BlockId.Cobblestone&&game.Inventory.Slots[0].Count==11&&game.Inventory.Total(BlockId.Dirt)==7,"Bound Pick Block swaps owned backpack stack without creation or loss");
            yield return Capture("pick-block-survival");
            game.SetCreative(true);AlphaPut(target,BlockId.LavaRock);AlphaAim(target);yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Middle));yield return null;yield return null;InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;
            Check(game.Inventory.Slots[game.Selected].Id==BlockId.LavaRock&&game.Inventory.Slots[game.Selected].Count==64,"Actual Creative middle-click provides usable targeted stack");game.SetCreative(false);
            game.Inventory.Add(BlockId.Dirt,35,16,17);game.Inventory.Add(BlockId.Dirt,40,22,23);int dirtTotal=game.Inventory.Total(BlockId.Dirt);
            game.SetMode(ScreenMode.Inventory);yield return null;yield return null;var point=CraftPoint(22);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point}.WithButton(MouseButton.Middle));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return null;yield return null;
            Check(game.Inventory.Slots[15].Id==BlockId.Dirt&&game.Inventory.Slots[15].Count==64&&game.Inventory.Total(BlockId.Dirt)==dirtTotal,"Actual inventory middle-click consolidates and conserves backpack");yield return Capture("inventory-organised");game.SetMode(ScreenMode.Play);
            var torch=floor.Offset(-2,1,2);AlphaPut(torch,BlockId.Torch);var behind=torch.Offset(0,0,1);AlphaPut(behind,BlockId.Stone);
            Check(world.Select(world.Local(torch)+new Vector3(.1f,.5f,-1),Vector3.forward,4,out var gap)&&gap.Position.Equals(behind),"World DDA looks through torch empty space to full block");
            Check(world.Select(world.Local(torch)+new Vector3(.5f,.5f,-1),Vector3.forward,4,out var direct)&&direct.Position.Equals(torch),"World DDA selects visible torch geometry");

            player.enabled=false;
            var crop=CropRules.Planting(BlockId.Potato);var plant=floor.Offset(2,1,2);AlphaPut(plant.Offset(0,-1,0),BlockId.Dirt);Check(world.Till(plant.Offset(0,-1,0)),"Till crop fixture");
            AlphaPut(plant,crop.Mature);game.Selected=0;game.Inventory.Take(0,int.MaxValue);AlphaAim(plant);yield return null;yield return null;
            int ordinaryBefore=game.Items.Total(BlockId.Potato);var normalHarvest=CropRules.Harvest(crop.Mature,TerrainGenerator.Hash(plant.X,plant.Y,plant.Z,game.Seed)).ToArray();
            player.enabled=true;InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;player.enabled=false;
            Check(world.Get(plant)==0&&game.Items.Total(BlockId.Potato)==ordinaryBefore+normalHarvest.Where(t=>t.Id==BlockId.Potato).Sum(t=>t.Count),"Actual ordinary right-click harvest uses normal farmland crop drops");
            AlphaPut(plant,crop.Mature);int before=game.Inventory.Total(BlockId.Potato);var harvest=CropRules.Harvest(crop.Mature,TerrainGenerator.Hash(plant.X,plant.Y,plant.Z,game.Seed)).ToArray();
            game.Inventory.Add(BlockId.WoodHoe,1,0,1);game.Selected=0;AlphaAim(plant);player.enabled=true;yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;player.enabled=false;
            Check(world.Get(plant)==crop.first,"Actual selected-hoe right-click harvest replants at initial stage");
            yield return new WaitForSecondsRealtime(.7f);yield return Capture("hoe-harvest-replanted");
            Check(game.Inventory.Total(BlockId.Potato)==before+harvest.Where(s=>s.Id==BlockId.Potato).Sum(s=>s.Count)-1,"Harvest consumes exactly one actual inserted seed");
            Check(!game.TryHarvestCrop(plant,crop.first,true),"Unripe crop rejects convenience harvest");
            BlockPos wild=default;CropDefinition wildCrop=null;
            for(int z=-90;z<=90&&wildCrop==null;z++)for(int x=-90;x<=90&&wildCrop==null;x++)
            {
                var site=new BlockPos(x,world.Generator.Height(x,z)+1,z);var candidate=CropRules.For(world.Get(site));
                if(candidate!=null&&world.Ready(site)&&world.Get(site.Offset(0,-1,0))==BlockId.Grass){wild=site;wildCrop=candidate;}
            }
            Check(wildCrop!=null,"Find actual naturally generated crop on wild soil");
            while(world.Get(wild)<wildCrop.Mature)Check(world.Grow(wild,world.Get(wild)),"Grow wild sample to ripe stage");
            Check(!game.TryHarvestCrop(wild,wildCrop.Mature,true)&&world.Get(wild)==wildCrop.Mature,"Actual ripe wild crop rejects hoe convenience harvest");
            AlphaPut(plant,0);AlphaPut(plant.Offset(0,-1,0),BlockId.Farmland);AlphaPut(plant,crop.Mature);
            for(int i=0;i<game.Inventory.Count;i++){game.Inventory.Take(i,int.MaxValue);game.Inventory.Add(BlockId.Stone,64,i,i+1);}
            int oldDrops=game.Items.Total(BlockId.Potato);Check(game.TryHarvestCrop(plant,crop.Mature,true)&&world.Get(plant)==0,"Full backpack cannot invent a replanting seed");
            Check(game.Items.Total(BlockId.Potato)==oldDrops+harvest.Where(s=>s.Id==BlockId.Potato).Sum(s=>s.Count),"Full backpack safely drops all overflow exactly once");
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            game.Inventory.Add(BlockId.Torch,1);game.Selected=0;player.enabled=true;player.Pitch=80;player.Yaw=0;
            foreach(bool female in new[]{false,true})foreach(int skin in new[]{0,1})
            {
                player.Female=female;player.Skin=skin;player.RefreshAppearance();yield return new WaitForSecondsRealtime(.5f);
                var renderers=player.Body.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled&&r.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly).ToArray();
                Check(renderers.Length>0&&renderers.All(r=>r.sharedMaterial.shader.name=="RivetReach/ExplorerSkin"&&r.sharedMaterial.GetTexture("_BaseMap")==Resources.Load<Texture2D>("Characters/"+(skin==0?"SkinField":"SkinOchre"))),"Both body variants retain actual registered skin texture");
                yield return Capture("body-looking-down-"+(female?"female":"male")+"-skin-"+skin);
            }
            player.enabled=false;
            yield return ReviewAlphaLavaRock();
            yield return ReviewAlphaSpawners();
            yield return ReviewGeneratedSpawnerRoom();
        }
        IEnumerator ReviewAlphaLavaRock()
        {
            var world=game.World;var player=game.Player;var p=world.Address(player.transform.position).Offset(5,0,0);
            for(int z=-2;z<=2;z++)for(int x=-3;x<=3;x++)for(int y=-1;y<=2;y++)AlphaPut(p.Offset(x,y,z),y==-1||Math.Abs(z)==2||Math.Abs(x)==3?BlockId.Stone:(byte)0);
            AlphaPut(p.Offset(-2,0,0),Fluids.Water.Source);
            for(int i=0;i<8;i++)world.AdvanceFluids(.25f);
            Check(Fluids.Registry.Get(world.Get(p.Offset(-1,0,0)))==Fluids.Water&&world.Get(p.Offset(-1,0,0))!=Fluids.Water.Source,"Actual resident world establishes flowing water for source reaction");
            AlphaPut(p,Fluids.Lava.Source);for(int i=0;i<8&&world.Get(p)!=BlockId.LavaRock;i++)world.AdvanceFluids(.25f);
            Check(world.Get(p)==BlockId.LavaRock,"Resident fluid simulation turns contacted lava source into stable Lava Rock");yield return SettleLighting();
            player.Camera.transform.position=world.Local(p)+new Vector3(1,7,-1);player.Camera.transform.LookAt(world.Local(p)+Vector3.one*.4f);yield return AlphaSceneCapture("lava-rock-reaction");
            int before=game.Items.Total(BlockId.LavaRock);Check(!world.Mine(p,BlockId.LavaRock,ToolCapability.Pickaxe,ToolTier.Iron)&&world.Get(p)==BlockId.LavaRock,"Iron pick cannot harvest Lava Rock");
            Check(world.Mine(p,BlockId.LavaRock,ToolCapability.Pickaxe,ToolTier.Diamond)&&game.Items.Total(BlockId.LavaRock)==before+1,"Diamond pick harvests exactly one Lava Rock");
        }
        IEnumerator ReviewGeneratedSpawnerRoom()
        {
            var world=game.World;var player=game.Player;
            Check(SpawnerRooms.TryRoom(world.Generator,0,-8,out var room)&&!room.Buried,"Known seeded room chooses connected placement");
            player.transform.position=world.Local(room.FloorCentre)+new Vector3(.5f,1.01f,-2.5f);player.ResetMotion();yield return Settle(120);yield return SettleLighting();
            Check(world.Get(room.Spawner)==BlockId.MobSpawner&&game.Mobs.SpawnerAt(room.Spawner)!=null,"Generated room discovers a functional generic cage through ordinary chunk integration");
            int cobble=0;for(int x=-4;x<=4;x++)for(int z=-4;z<=4;z++)if(world.Get(room.FloorCentre.Offset(x,0,z))==BlockId.Cobblestone)cobble++;
            Check(cobble==81,"Actual generated dungeon floor uses cobblestone throughout");
            // Held torch only aids the capture; it does not affect spawn-light authority.
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);game.Inventory.Add(BlockId.Torch,1);game.Selected=0;player.enabled=true;AlphaAim(room.Spawner);yield return new WaitForSecondsRealtime(.6f);player.enabled=false;
            player.Camera.transform.position=world.Local(room.Spawner)+new Vector3(.5f,1.15f,-1.3f);player.Camera.transform.LookAt(world.Local(room.Spawner)+new Vector3(.5f,.5f,.5f));
            yield return AlphaSceneCapture("generated-floater-room");
            var miniature=UnityEngine.Object.FindObjectsByType<FrozenSpawnerDisplay>(FindObjectsSortMode.None).OrderBy(v=>(v.transform.position-world.Local(room.Spawner)).sqrMagnitude).First();
            File.WriteAllText(Path.Combine(output,"miniature-renderers.txt"),string.Join("\n",miniature.GetComponentsInChildren<MeshRenderer>().Select(r=>r.name+" enabled="+r.enabled+" bounds="+r.bounds+" local="+r.localBounds+" scale="+r.transform.lossyScale+" vertices="+r.GetComponent<MeshFilter>().sharedMesh.vertexCount+" material="+r.sharedMaterial.name+" shader="+r.sharedMaterial.shader.name+" viewport="+player.Camera.WorldToViewportPoint(r.bounds.center))));
            var visible=miniature.GetComponentsInChildren<MeshRenderer>();
            Check(visible.Length>0&&visible.All(r=>r.enabled&&r.GetComponent<MeshFilter>().sharedMesh.vertexCount>0&&r.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0)>0),"Native cage miniature has enabled indexed render geometry after animation cleanup");
            var miniatureBounds=visible[0].bounds;foreach(var renderer in visible)miniatureBounds.Encapsulate(renderer.bounds);
            Check(miniatureBounds.size.magnitude>.1f&&Mathf.Max(miniatureBounds.size.x,Mathf.Max(miniatureBounds.size.y,miniatureBounds.size.z))<.85f&&(miniatureBounds.center-(world.Local(room.Spawner)+Vector3.one*.5f)).magnitude<.04f,"Native rotating miniature stays inside its cage after deferred component destruction");
            Check(miniature.GetComponentsInChildren<Animator>().Length==0&&miniature.GetComponentsInChildren<SkinnedMeshRenderer>().Length==0&&miniature.GetComponentsInChildren<Collider>().Length==0&&miniature.GetComponentsInChildren<MobView>().Length==0,"Native miniature is render-only with no animation, collider or AI entity");
            var state=game.Mobs.SpawnerAt(room.Spawner);for(int i=0;i<20&&game.Mobs.SpawnerPopulation(state.Id)==0;i++)game.Mobs.TrySpawnerCycle(state);
            Check(game.Mobs.SpawnerPopulation(state.Id)>0,"Unedited generated cage creates Floater through shared room and spawn rules");
        }
        IEnumerator ReviewAlphaSpawners()
        {
            var world=game.World;var player=game.Player;var mobs=game.Mobs;
            var floor=new BlockPos(144,-64,16);player.transform.position=world.Local(floor)+new Vector3(.5f,1.01f,.5f);player.ResetMotion();yield return Settle(120);
            for(int z=-9;z<=9;z++)for(int y=0;y<=6;y++)for(int x=-20;x<=20;x++)
                AlphaPut(floor.Offset(x,y,z),y==0||y==6||Math.Abs(x)==20||Math.Abs(z)==9?BlockId.Cobblestone:(byte)0);
            game.SetMode(ScreenMode.Play);game.Sky.Clock.SetTime(.5);game.Sky.Apply();yield return SettleLighting();
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            yield return new WaitForSecondsRealtime(.4f);
            player.Camera.transform.position=world.Local(floor)+new Vector3(.5f,2.65f,-5);player.Camera.transform.LookAt(world.Local(floor)+new Vector3(.5f,1.5f,4));
            var darkCell=floor.Offset(0,1,3);AlphaPut(darkCell,BlockId.Stone);yield return SettleLighting();
            Check(world.TryGetSpawnLight(darkCell.Offset(0,1,0),false,out byte dark)&&dark==0,"Ordinary block fixture is genuinely unlit");
            int ActiveLights()=>UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l=>l.enabled&&l.gameObject.activeInHierarchy&&l.type==LightType.Point&&l.intensity>0);
            int lights=ActiveLights();yield return AlphaSceneCapture("ordinary-block-dark-before");
            ArcadePresentation.Active.Impact(BlockId.Stone,world.Local(darkCell)+new Vector3(.5f,.5f,0),Vector3.back);yield return AlphaSceneCapture("ordinary-block-dark-hit");
            Check(ActiveLights()==lights,"Ordinary block hit creates no point-light pulse");
            Check(world.Mine(darkCell,BlockId.Stone,ToolCapability.Pickaxe,ToolTier.Diamond),"Break ordinary stone fixture");yield return AlphaSceneCapture("ordinary-block-dark-broken");
            AlphaPut(darkCell,BlockId.Stone);ArcadePresentation.Active.Place(world.Local(darkCell)+Vector3.one*.5f,BlockId.Stone);yield return AlphaSceneCapture("ordinary-block-dark-placed");yield return SettleLighting();
            Check(ActiveLights()==lights&&world.TryGetSpawnLight(darkCell.Offset(0,1,0),false,out dark)&&dark==0,"Ordinary mining and placement add neither transient light nor a cached source");
            AlphaPut(darkCell,0);AlphaPut(darkCell,BlockId.Torch);yield return SettleLighting();
            Check(world.TryGetSpawnLight(darkCell.Offset(0,1,0),false,out byte lit)&&lit>=8,"Real torch still illuminates after flash removal");yield return AlphaSceneCapture("ordinary-block-real-torch");
            AlphaPut(darkCell,0);yield return SettleLighting();Check(world.TryGetSpawnLight(darkCell,false,out dark)&&dark==0,"Removing real torch restores darkness");
            var floater=mobs.Definitions.Single(d=>d.stableId=="rivet:floater");var at=floor.Offset(6,1,0);
            Vector3 feet=world.Local(at)+new Vector3(.5f,.006f+floater.hoverHeight,.5f);
            Check(mobs.CheckSpawnSite(floater,feet)==SpawnRejection.None,"Cobblestone dark cave meets shared Floater eligibility");
            var cageA=floor.Offset(-6,1,0);var cageB=floor.Offset(6,1,0);AlphaPut(cageA,BlockId.MobSpawner);AlphaPut(cageB,BlockId.MobSpawner);yield return SettleLighting();
            var a=mobs.SpawnerAt(cageA);var b=mobs.SpawnerAt(cageB);Check(a!=null&&b!=null&&a.Id!=b.Id,"Each cage receives its own durable identity");
            player.transform.position=world.Local(floor)+new Vector3(.5f,1.01f,-6);
            a.Cooldown=1;b.Cooldown=10;mobs.NaturalSpawning=false;mobs.enabled=true;
            yield return new WaitForSecondsRealtime(.4f);Check(mobs.SpawnerPopulation(a.Id)==0,"Active cage waits for its remaining cooldown");
            yield return new WaitForSecondsRealtime(1.2f);mobs.enabled=false;
            Check(mobs.SpawnerPopulation(a.Id)==1&&a.Sequence<=12,"Normal tick creates at most one mob with bounded candidates");long scheduled=a.Sequence;
            mobs.enabled=true;yield return new WaitForSecondsRealtime(.5f);mobs.enabled=false;Check(a.Sequence==scheduled&&mobs.SpawnerPopulation(a.Id)==1,"Cooldown prevents rapid successive active cycles");
            for(int i=0;i<100;i++){mobs.TrySpawnerCycle(a);mobs.TrySpawnerCycle(b);}
            Check(mobs.SpawnerPopulation(a.Id)==5&&mobs.SpawnerPopulation(b.Id)==5,"Two nearby spawners each maintain exactly five own live mobs");
            Check(mobs.Mobs.Count(m=>m.Alive&&m.SpawnerId==0)==0,"Spawner mobs never consume natural population count");
            foreach(var mob in mobs.Mobs.Where(m=>m.Alive))Check(mobs.CheckSpawnSite(mob.Definition,mob.Position.Local(world.Origin))==SpawnRejection.Occupied,"Existing mob body rejects occupied spawn volume");
            game.Inventory.Add(BlockId.Torch,1,0,1);game.Selected=0;player.HeldBlock.PrepareFrame(1);
            player.transform.position=world.Local(cageA)+new Vector3(.5f,.01f,-4.5f);
            player.Camera.transform.position=world.Local(cageA)+new Vector3(0,1.2f,-2);player.Camera.transform.LookAt(world.Local(cageA)+Vector3.one*.5f);yield return AlphaSceneCapture("floater-spawner-active");
            Check(world.TryGetSpawnLight(cageA.Offset(0,1,0),false,out byte heldLight)&&heldLight==0,"Held torch aids view but leaves cage spawn light dark");
            game.Inventory.Take(0,int.MaxValue);player.HeldBlock.PrepareFrame(0);
            // Fill the ordinary cap outside the two cage candidate areas.
            foreach(var species in mobs.Definitions.Where(d=>d.stableId!="rivet:floater"))
                for(int x=-18;x<=18&&mobs.Mobs.Count(m=>m.Alive&&m.SpawnerId==0&&m.Definition==species)<species.population;x+=3)
                    for(int z=-7;z<=7&&mobs.Mobs.Count(m=>m.Alive&&m.SpawnerId==0&&m.Definition==species)<species.population;z+=14)
                        mobs.Spawn(species,world.Local(floor.Offset(x,1,z))+new Vector3(.5f,.006f+species.hoverHeight,.5f));
            Check(mobs.Mobs.Count(m=>m.Alive&&m.SpawnerId==0)==MobSystem.MaximumPopulation,"Fill ordinary natural population cap independently of ten spawner mobs");
            var victim=mobs.Mobs.First(m=>m.SpawnerId==a.Id&&m.Alive);Check(mobs.Damage(victim,1000,Vector3.zero),"Defeat one spawner mob normally");
            Check(mobs.SpawnerPopulation(a.Id)==4&&mobs.SpawnerPopulation(b.Id)==5,"Death frees only the originating cage slot");
            for(int i=0;i<100&&mobs.SpawnerPopulation(a.Id)<5;i++)mobs.TrySpawnerCycle(a);
            Check(mobs.SpawnerPopulation(a.Id)==5,"A freed spawner slot can be replaced even while the natural population cap is full");
            mobs.Clear();AlphaPut(cageA.Offset(0,0,1),BlockId.Torch);yield return SettleLighting();
            Check(!mobs.TrySpawnerCycle(a)&&a.LastRejection==SpawnRejection.TooBright,"Torch beside cage prevents attempts through shared light limits");
            yield return AlphaSceneCapture("floater-spawner-torch-disabled");AlphaPut(cageA.Offset(0,0,1),0);yield return SettleLighting();
            Check(mobs.TrySpawnerCycle(a),"Removing torch makes cage eligible again");
            player.transform.position+=Vector3.right*60;int prior=mobs.SpawnerPopulation(a.Id);Check(!mobs.TrySpawnerCycle(a)&&mobs.SpawnerPopulation(a.Id)==prior,"Outside activation radius never creates a mob");player.transform.position-=Vector3.right*60;
            game.InitializeSaves(Path.Combine(output,"Saves"));game.SetMode(ScreenMode.Pause);Check(game.SaveGame("Alpha spawner checkpoint"),"Save spawner identities, origins and cooldowns: "+game.SaveStatus);
            long aId=a.Id,bId=b.Id,sequence=a.Sequence;float cooldown=a.Cooldown;var entry=game.Saves.List().First(e=>!e.Backup);
            Check(game.LoadGame(entry),"Load schema-13 spawner checkpoint: "+game.SaveStatus);FreezeSaveFixture();game.enabled=false;game.Animals.enabled=false;yield return Settle(120);
            world=game.World;player=game.Player;mobs=game.Mobs;a=mobs.SpawnerAt(cageA);b=mobs.SpawnerAt(cageB);
            Check(a!=null&&b!=null&&a.Id==aId&&b.Id==bId&&a.Sequence==sequence&&a.Cooldown==cooldown&&mobs.SpawnerPopulation(aId)==prior,"Reload preserves origin population, stable IDs, sequence and timer");
            int collectible=game.Items.Total(BlockId.MobSpawner);Check(world.Mine(cageA,BlockId.MobSpawner,ToolCapability.Pickaxe,ToolTier.Diamond),"Mine cage through ordinary world authority");
            Check(mobs.SpawnerAt(cageA)==null&&game.Items.Total(BlockId.MobSpawner)==collectible,"Broken cage has no spawner drop");
            Check(game.SaveGame("Alpha broken cage"),"Save living mobs whose origin cage was broken");
            entry=game.Saves.List().First(e=>!e.Backup);Check(game.LoadGame(entry),"Reload orphan origin without resurrecting cage");FreezeSaveFixture();game.enabled=false;game.Animals.enabled=false;yield return Settle(120);
            Check(game.Mobs.SpawnerAt(cageA)==null&&game.Mobs.SpawnerPopulation(aId)==prior,"Broken origin identity remains only on its surviving ordinary mobs");
            world=game.World;player=game.Player;mobs=game.Mobs;mobs.Clear();AlphaPut(cageB,0);
            player.transform.position=world.Local(floor)+new Vector3(-17,1.01f,.5f);player.Camera.transform.position=player.transform.position+Vector3.up*1.64f;player.Camera.transform.rotation=Quaternion.LookRotation(Vector3.left);
            // Let ordinary load completion release its pause before observing normal
            // Survival spawning; keeping Expedition disabled would freeze that gate.
            game.enabled=true;yield return null;game.SetMode(ScreenMode.Play);mobs.NaturalSpawning=true;mobs.enabled=true;
            Check(!game.LoadingSave&&!game.Paused,"Loaded Survival simulation actually resumes before natural spawn observation");
            bool NaturalFloater(MobState m)=>m.Alive&&m.SpawnerId==0&&m.Definition.stableId=="rivet:floater";
            float deadline=Time.realtimeSinceStartup+180;
            while(!mobs.Mobs.Any(NaturalFloater)&&Time.realtimeSinceStartup<deadline&&!game.Health.Dead)yield return null;
            mobs.enabled=false;mobs.NaturalSpawning=false;game.enabled=false;
            File.WriteAllText(Path.Combine(output,"natural-spawn-diagnostics.txt"),string.Join("\n",Enum.GetValues(typeof(SpawnRejection)).Cast<SpawnRejection>().Select(reason=>reason+"="+mobs.SpawnRejections[(int)reason])));
            Check(mobs.Mobs.Any(NaturalFloater),"Normal two-second natural spawn cadence eventually produces a Floater in a valid dark cave");
            var natural=mobs.Mobs.First(NaturalFloater);
            var naturalGround=natural.Home.Cell.Offset(0,-1,0);
            Check(world.TryGetSpawnLight(natural.Home.Cell,false,out byte naturalLight)&&naturalLight<=7,"Natural Floater origin obeys the authoritative dark-site light rule");
            File.AppendAllText(Path.Combine(output,"natural-spawn-diagnostics.txt"),"\nNatural origin="+natural.Home.Cell+"; support="+world.Get(naturalGround)+"; light="+naturalLight+"; constructed-floor eligibility checked separately. Nearby caves compete for the same four-Floater cap.");
            var roomPlayer=player.transform.position;player.transform.position=natural.Position.Local(world.Origin)+new Vector3(0,0,-4);
            player.Camera.transform.position=player.transform.position+Vector3.up*1.5f;player.Camera.transform.LookAt(natural.Position.Local(world.Origin)+Vector3.up*.5f);
            game.Inventory.Add(BlockId.Torch,1,0,1);game.Selected=0;player.HeldBlock.PrepareFrame(1);yield return AlphaSceneCapture("natural-floater-dark-room");
            game.Inventory.Take(0,int.MaxValue);player.HeldBlock.PrepareFrame(0);player.transform.position=roomPlayer;
            // Track a natural mob in the constructed room for the bounded unload check;
            // its eligibility was already verified independently of random site selection.
            mobs.Clear();natural=mobs.Spawn(floater,world.Local(floor.Offset(12,1,0))+new Vector3(.5f,.006f+floater.hoverHeight,.5f));
            Check(natural!=null,"Create natural-lifecycle sample for controlled chunk unloading");
            // Resident streaming, not permanent origin references, owns both lifecycles.
            AlphaPut(cageB,BlockId.MobSpawner);yield return SettleLighting();b=mobs.SpawnerAt(cageB);
            for(int i=0;i<100&&mobs.SpawnerPopulation(b.Id)==0;i++)mobs.TrySpawnerCycle(b);
            Check(mobs.SpawnerPopulation(b.Id)>0,"Create an origin-owned mob alongside natural lifecycle sample");
            long streamId=b.Id;var tracked=mobs.Mobs.Where(m=>m.Alive).ToArray();
            world.ViewDistance=1;player.transform.position=world.Local(floor)+new Vector3(80,1.01f,.5f);player.ResetMotion();yield return Settle(120);
            Check(tracked.All(m=>Vector3.Distance(m.Position.Local(world.Origin),player.transform.position)<MobSystem.DespawnDistance),"Unload fixture stays within ordinary distance-despawn threshold");
            Check(tracked.All(m=>!world.Ready(m.Position.Cell)),"Both mob samples actually leave resident chunks");
            mobs.enabled=true;yield return new WaitForSecondsRealtime(.15f);mobs.enabled=false;
            Check(tracked.All(m=>!mobs.Mobs.Contains(m))&&mobs.SpawnerPopulation(streamId)==0,"Chunk unloading removes natural and spawner mobs alike and releases origin slots");
            world.ViewDistance=4;

        }
    }
}
