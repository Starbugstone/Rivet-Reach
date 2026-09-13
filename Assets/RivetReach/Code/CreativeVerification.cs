using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewCreativeMining()
        {
            var player=game.Player;var world=game.World;
            game.Mobs.enabled=false;player.enabled=false;game.SetCreative(true);
            var cell=new BlockPos(8,96,8);player.transform.position=world.Local(cell);
            world.ViewDistance=4;yield return null;yield return Settle(90);
            for(int x=-3;x<=3;x++)for(int z=-4;z<=3;z++)Check(world.Place(cell.Offset(x,-1,z),BlockId.Stone),"Place mining fixture floor");
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            game.Selected=0;
            var mine=typeof(FirstPersonPlayer).GetMethod("TargetAndMine",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            void Select(byte id)
            {game.Inventory.Take(0,int.MaxValue);if(id!=0)game.Inventory.Add(id,1,0,1);}
            void Aim()
            {
                player.transform.position=world.Local(cell)+new Vector3(.5f,0,-3);
                player.transform.rotation=Quaternion.identity;
                player.Camera.transform.position=player.transform.position+Vector3.up*1.62f;
                player.Camera.transform.LookAt(world.Local(cell)+Vector3.one*.5f);
            }
            void TickMine()
            {Aim();player.VerificationMining=true;try{mine.Invoke(player,null);}finally{player.VerificationMining=false;}}
            foreach(byte held in new byte[]{0,BlockId.Dirt,BlockId.WoodPickaxe,BlockId.WoodAxe,IndustryId.Wrench})
            {
                Select(held);Check(world.Place(cell,BlockId.DiamondBlock),"Place high-tier block fixture");
                int before=game.Items.TotalSpawned;TickMine();
                Check(world.Get(cell)==0&&player.MiningProgress==0,"Creative removes high-tier block in one mining update with held item "+held);
                int expected=held==0||held==BlockId.Dirt?0:1;
                Check(game.Items.TotalSpawned==before+expected,"Only tools produce Creative mining drops: "+held);
                if(expected>0)Check(game.Items.Piles.Last().Stack.Id==BlockId.DiamondBlock,"Tool keeps ordinary block drop identity");
                if(held==0||held==BlockId.WoodPickaxe)
                {
                    player.Pitch=18;player.Yaw=0;player.enabled=true;
                    game.Notify(held==0?"Creative fists: instant removal, no item drop":"Creative tool: instant removal, diamond block dropped",5);
                    yield return Capture(held==0?"creative-fist-mining":"creative-tool-mining");
                    Check(!game.UI.VisibleRoot.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Requires iron pickaxe")),"Creative HUD does not show Survival tier requirements");
                    player.enabled=false;
                }
            }
            Select(0);
            foreach(byte id in new[]{BlockId.Chest,IndustryId.ItemPipe,IndustryId.Battery,IndustryId.Tank,BlockId.MaturePotatoPlant})
            {
                if(BlockId.Crop(id))
                {
                    var soil=cell.Offset(0,-1,0);world.Remove(soil,BlockId.Stone);world.Place(soil,BlockId.Dirt);
                    Check(world.Till(soil)&&world.Plant(cell),"Plant crop fixture");
                    while(world.Get(cell)!=id)Check(world.Grow(cell,world.Get(cell)),"Mature crop fixture");
                }
                else Check(world.Place(cell,id),"Place contents fixture "+id);
                game.Survival.At(cell)?.Storage?.Add(BlockId.Diamond,3);
                var machine=game.Industry.Simulation.At(cell);
                if(machine!=null)
                {
                    machine.Items.Add(BlockId.Diamond,3);
                    if(id==IndustryId.ItemPipe)machine.Additions=PipeAddition.Signal|PipeAddition.Power;
                    if(id==IndustryId.Battery)machine.EnergyCells[0].Charge(1234567);
                    if(id==IndustryId.Tank)machine.Fluid.Deposit(Fluids.Lava,12345);
                }
                int before=game.Items.TotalSpawned;TickMine();
                Check(world.Get(cell)==0&&game.Items.TotalSpawned==before,"Creative fists erase block, contents and fittings without drops: "+id);
                Check(!world.SuppressMiningDrops&&!world.RecoveringMachine,"Mining context restores after removal");
            }
            Check(!world.Mine(new BlockPos(0,TerrainGenerator.MinY,0),BlockId.Bedrock,ToolCapability.None,creative:true,drop:false),"Creative preserves protected bedrock");
            Check(!world.Mine(cell,BlockId.Stone,ToolCapability.None,creative:true,drop:false)&&!world.SuppressMiningDrops,"Stale removal restores drop context");
            Check(world.Place(cell,IndustryId.Tank),"Place tool recovery tank");
            var tank=game.Industry.Simulation.At(cell);tank.Fluid.Deposit(Fluids.Lava,12345);
            var contents=PortableStorage.Capture(tank);Select(BlockId.WoodPickaxe);TickMine();
            Check(world.Get(cell)==0&&game.Items.Piles.Last().Stack.Equals(contents),"Creative tool preserves exact portable contents after no-drop removals");
            game.SetCreative(false);Select(0);Check(world.Place(cell,BlockId.Stone),"Place Survival stone");TickMine();
            Check(world.Get(cell)==BlockId.Stone&&player.MiningProgress==0,"Survival fists still cannot mine stone");
            world.Remove(cell,BlockId.Stone);Check(world.Place(cell,BlockId.Log),"Place Survival log");TickMine();
            Check(world.Get(cell)==BlockId.Log&&player.MiningProgress>0&&player.MiningProgress<1,"Survival fist mining still uses timed progress");
            int drops=game.Items.TotalSpawned;
            Check(world.Mine(cell,BlockId.Log,ToolCapability.None)&&game.Items.TotalSpawned==drops+1,"Survival fists still drop logs");
            Check(world.Place(cell,BlockId.DiamondBlock),"Place Survival high-tier block");Select(BlockId.WoodPickaxe);TickMine();
            Check(world.Get(cell)==BlockId.DiamondBlock&&player.MiningProgress==0,"Survival still enforces pickaxe tiers");
            Select(BlockId.IronPickaxe);TickMine();
            Check(world.Get(cell)==BlockId.DiamondBlock&&player.MiningProgress>0&&player.MiningProgress<1,"Survival suitable tools still use timed mining");
        }
        IEnumerator CreativeClick(Button button)
        {
            var rect=(RectTransform)button.transform;
            Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point}.WithButton(MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=point});yield return null;yield return null;
        }
        IEnumerator CreativeHold(float seconds,params Key[] keys)
        {
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));yield return new WaitForSecondsRealtime(seconds);
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;yield return null;
        }
        IEnumerator CreativeDragTo(byte id,Vector2 destination,bool right=false)
        {
            var search=game.UI.VisibleRoot.GetComponentsInChildren<InputField>().Single(f=>f.name=="Item browser search");
            search.text=game.Registry.Get(id).displayName;yield return null;yield return null;
            var view=game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Single(v=>v.CatalogSource&&v.Item==id);
            var rect=(RectTransform)view.transform;
            Vector2 from=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var button=right?MouseButton.Right:MouseButton.Left;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=from});yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=from}.WithButton(button));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=Vector2.Lerp(from,destination,.5f)}.WithButton(button));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=destination}.WithButton(button));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState{position=destination});yield return null;yield return null;
        }
        bool CreativeWorkshop=>Array.Exists(Environment.GetCommandLineArgs(),arg=>arg=="-rr-creative-workshop");
        bool PlaceWorkshopItem(BlockPos cell,byte id)
        {
            if(!CreativeWorkshop)return game.World.Place(cell,id);
            var world=game.World;var player=game.Player;
            int slot=game.Inventory.FindSlot(s=>s.Id==id&&!s.Empty);
            ItemStack displaced=default;
            if(slot<0)
            {
                slot=game.Inventory.FindSlot(s=>s.Empty);
                if(slot<0){slot=0;displaced=game.Inventory.Take(slot,int.MaxValue);}
                game.SetMode(ScreenMode.Inventory);
                Check(game.TryGiveCreativeItemToSlot(id,slot),"Creative catalog supplies placement item "+game.Registry.Get(id).displayName);
                game.SetMode(ScreenMode.Play);
            }
            if(slot>=Inventory.HotbarCount)
            {
                ItemStack transfer=default;game.Inventory.Click(slot,ref transfer,false);
                game.Inventory.Click(0,ref transfer,false);game.Inventory.Click(slot,ref transfer,false);slot=0;
            }
            game.Selected=slot;
            var support=cell.Offset(0,-1,0);
            // A ray passes through water to the basin floor. Use an overhead support
            // when testing placement ABOVE water, preserving the intake source.
            if(Fluids.IsFluid(world.Get(support)))support=cell.Offset(0,1,0);
            bool scaffold=world.Get(support)==0;
            if(scaffold)Check(world.Place(support,BlockId.Stone),"Temporary building support");
            player.transform.position=world.Local(cell)+new Vector3(2.5f,0,.5f);
            player.transform.rotation=Quaternion.identity;
            player.Camera.transform.position=world.Local(cell)+new Vector3(.5f,.8f,.5f);
            player.Camera.transform.LookAt(world.Local(support)+new Vector3(.5f,.5f,.5f));
            int count=game.Inventory.Slots[slot].Count;
            bool placed=game.TryPlaceSelected();
            Check(placed&&world.Get(cell)==id&&game.Inventory.Slots[slot].Count==count,"Creative aimed placement retains stack: "+game.Registry.Get(id).displayName+" ("+game.PlacementDiagnostic+")");
            if(!displaced.Empty){game.Inventory.Take(slot,int.MaxValue);game.Inventory.Add(displaced.Id,displaced.Count,slot,slot+1);}
            if(scaffold)world.Remove(support,BlockId.Stone);
            return placed;
        }
        IEnumerator CreativeDoubleJump()
        {
            yield return CreativeHold(.04f,game.Input.Keys["Jump"]);
            yield return CreativeHold(.04f,game.Input.Keys["Jump"]);
        }
        void CheckMovementBindingMigration()
        {
            var names=new[]{"binding.Sprint","binding.Crouch","binding.movementVersion"};
            var saved=names.Select(n=>(name:n,exists:PlayerPrefs.HasKey(n),value:PlayerPrefs.GetInt(n))).ToArray();
            try
            {
                foreach(int partial in new[]{0,1,2})
                {
                    foreach(string name in names)PlayerPrefs.DeleteKey(name);
                    if(partial==1)PlayerPrefs.SetInt("binding.Sprint",(int)Key.LeftShift);
                    if(partial==2)PlayerPrefs.SetInt("binding.Crouch",(int)Key.LeftCtrl);
                    var input=new PlayerInput();
                    Check(input.Keys["Sprint"]==Key.LeftCtrl&&input.Keys["Crouch"]==Key.LeftShift,"Ctrl/Shift defaults migrate safely with legacy partial preferences "+partial);
                }
                PlayerPrefs.DeleteKey("binding.movementVersion");PlayerPrefs.SetInt("binding.Sprint",(int)Key.F9);PlayerPrefs.SetInt("binding.Crouch",(int)Key.F10);
                var custom=new PlayerInput();Check(custom.Keys["Sprint"]==Key.F9&&custom.Keys["Crouch"]==Key.F10,"Customized movement bindings survive migration");
                PlayerPrefs.SetInt("binding.Sprint",(int)Key.LeftShift);PlayerPrefs.SetInt("binding.Crouch",(int)Key.LeftCtrl);
                custom=new PlayerInput();Check(custom.Keys["Sprint"]==Key.LeftShift&&custom.Keys["Crouch"]==Key.LeftCtrl,"Later deliberate rebinding is not migrated again");
            }
            finally
            {
                foreach(var entry in saved)if(entry.exists)PlayerPrefs.SetInt(entry.name,entry.value);else PlayerPrefs.DeleteKey(entry.name);
                PlayerPrefs.Save();
            }
        }
        IEnumerator ReviewCreative()
        {
            CheckMovementBindingMigration();
            var player=game.Player;var world=game.World;
            game.Mobs.enabled=false;game.Diagnostics=false;
            Button Named(string text)=>game.UI.VisibleRoot.GetComponentsInChildren<Button>().Single(b=>b.GetComponentsInChildren<Text>().Any(t=>t.text==text));
            Check(!game.Creative&&game.Inventory.Slots.All(s=>s.Empty),"Ordinary session starts in Survival with an empty inventory");
            game.Inventory.Add(BlockId.Dirt,3);
            game.Hunger.Exert(80);game.TakeDamage(4);
            float health=game.Health.Hearts;
            yield return CreativeHold(.06f,game.Input.Keys["Pause"]);
            Check(game.Mode==ScreenMode.Pause&&Time.timeScale==0,"Escape opens the paused menu");
            yield return CreativeClick(Named("CREATIVE MODE: OFF"));
            Check(game.Creative&&game.Mode==ScreenMode.Pause&&Time.timeScale==0,"Pointer toggle enables Creative while retaining pause");
            Check(game.Inventory.Total(BlockId.Dirt)==3&&game.Health.Hearts==health&&game.Hunger.Food==0,"Enabling Creative preserves inventory, health and hunger");
            yield return Capture("creative-menu");
            yield return CreativeHold(.06f,game.Input.Keys["Pause"]);
            foreach(DamageKind kind in Enum.GetValues(typeof(DamageKind)))Check(game.TakeDamage(100,kind)==0&&!game.Health.Dead,"Creative rejects "+kind+" damage");
            yield return new WaitForSeconds(4.2f);
            Check(game.Health.Hearts==health&&game.Hunger.Food==0,"Automatic starvation cannot damage Creative players");
            Check(world.Grass.Tick>0&&game.Sky.Clock.TotalDays>0,"World simulation continues in Creative");
            yield return CreativeHold(.06f,game.Input.Keys["Inventory"]);
            var entries=game.UI.VisibleRoot.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Creative item ")).ToArray();
            Check(entries.Length==game.Registry.items.Length,"Catalog contains every registered current item");
            yield return CreativeClick(entries[0]);
            Check(game.Inventory.Slots.Sum(s=>s.Count)>3,"Pointer click on catalog supplies an item stack");
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            foreach(var item in game.Registry.items)
            {
                Check(game.TryGiveCreativeItem(item.runtimeId)&&game.Inventory.Total(item.runtimeId)==item.stackLimit,"Catalog grants legal full stack of "+item.displayName);
                for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            }
            foreach(var item in game.Registry.items)
            {
                yield return CreativeDragTo(item.runtimeId,CraftPoint(Inventory.HotbarCount));
                Check(game.Inventory.Slots[Inventory.HotbarCount].Id==item.runtimeId&&game.Inventory.Slots[Inventory.HotbarCount].Count==item.stackLimit&&game.UI.HeldStack.Empty,"Sidebar pointer drag grants legal stack into selected backpack slot: "+item.displayName);
                game.Inventory.Take(Inventory.HotbarCount,int.MaxValue);
            }
            yield return CreativeDragTo(BlockId.Planks,CraftPoint(0));
            Check(game.Inventory.Slots[0].Id==BlockId.Planks,"Sidebar drag targets hotbar slots");
            game.Inventory.Take(0,5);
            yield return CreativeDragTo(BlockId.Planks,CraftPoint(0));
            Check(game.Inventory.Slots[0].Count==game.Registry.Get(BlockId.Planks).stackLimit,"Matching target tops up to its legal limit");
            long dragRevision=game.Inventory.Revision;
            yield return CreativeDragTo(BlockId.Dirt,CraftPoint(0));
            Check(game.UI.VisibleRoot.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("Drop into an empty inventory slot")),"Rejected drop shows feedback inside the visible sidebar");
            yield return CreativeDragTo(BlockId.Planks,CraftPoint(0));
            yield return CreativeDragTo(BlockId.Dirt,new Vector2(5,5));
            yield return CreativeDragTo(BlockId.Dirt,CraftPoint(13),true);
            Check(game.Inventory.Revision==dragRevision&&game.UI.HeldStack.Empty,"Occupied, full, canceled and right-button drags preserve inventory and cursor");
            game.Inventory.Add(BlockId.Log,3,14,15);yield return ClickCraftUI(14);
            dragRevision=game.Inventory.Revision;
            yield return CreativeDragTo(BlockId.Dirt,CraftPoint(13));
            Check(game.Inventory.Revision==dragRevision&&game.UI.HeldStack.Id==BlockId.Log&&game.UI.HeldStack.Count==3,"Sidebar drag preserves an existing cursor stack");
            yield return ClickCraftUI(14);
            game.UI.VisibleRoot.GetComponentsInChildren<InputField>().Single(f=>f.name=="Item browser search").text="";
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Add(BlockId.Dirt,64,i,i+1);
            long revision=game.Inventory.Revision;
            Check(!game.TryGiveCreativeItem(BlockId.Log)&&game.Inventory.Revision==revision,"Full inventory rejects grants without changing existing stacks");
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            var search=game.UI.VisibleRoot.GetComponentInChildren<InputField>();search.text="Diamond pickaxe";yield return null;
            Check(game.UI.VisibleRoot.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Creative item "))==1,"Search filters items by name");
            search.text="no such item";yield return null;
            Check(!game.UI.VisibleRoot.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("Creative item ")),"Empty search result is safe");
            search.text="";yield return null;
            game.TryGiveCreativeItem(BlockId.Planks);game.TryGiveCreativeItem(BlockId.DiamondPickaxe);game.TryGiveCreativeItem(BlockId.Potato);
            yield return Capture("creative-catalog");
            yield return CreativeClick(Named("CRAFTING"));
            Check(game.UI.VisibleRoot.GetComponentsInChildren<SlotView>().Count(v=>v.Index>=CraftCell&&v.Index<CraftCell+4)==4,"Creative retains personal crafting access");
            yield return CreativeClick(Named("ALL ITEMS"));
            yield return CreativeHold(.06f,game.Input.Keys["Inventory"]);
            Check(!game.TryGiveCreativeItem(BlockId.Log),"Catalog grant requires an inventory screen");

            // Ready elevated platform isolates flight and landing from generated terrain.
            player.enabled=false;
            var cell=new BlockPos(8,96,8);player.transform.position=world.Local(cell)+new Vector3(.5f,.02f,.5f);
            yield return null;yield return Settle(90);
            bool floor=true;for(int x=-4;x<=4;x++)for(int z=-4;z<=4;z++)floor&=world.Place(cell.Offset(x,-1,z),BlockId.Stone);
            Check(floor,"Place flight fixture floor");
            player.ResetMotion();player.Yaw=0;player.Pitch=25;player.enabled=true;yield return null;
            Check(!player.Flying,"Creative starts walking with flight off");
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(game.Input.Keys["Crouch"]));yield return new WaitForSecondsRealtime(.15f);
            Check(player.Height<1.5f,"Creative walking supports crouch");
            yield return CreativeHold(.05f);
            Vector3 start=player.transform.position;
            yield return CreativeHold(.1f,game.Input.Keys["Jump"]);
            Check(!player.Flying&&player.transform.position.y>start.y,"One Jump press performs a normal jump without enabling flight");
            yield return new WaitForSeconds(.9f);
            yield return CreativeDoubleJump();Check(player.Flying,"Double-tap Jump enables flight");
            yield return new WaitForSeconds(.35f);
            start=player.transform.position;
            yield return CreativeHold(.5f,game.Input.Keys["Jump"]);
            Check(player.transform.position.y>start.y+2,"Jump rises in Creative with zero food");
            float hover=player.transform.position.y;yield return new WaitForSeconds(.4f);
            Check(Mathf.Abs(player.transform.position.y-hover)<.01f,"Creative hovers when controls are released");
            yield return CreativeHold(.25f,game.Input.Keys["Crouch"]);
            Check(player.transform.position.y<hover-.8f&&player.Height>1.5f,"Crouch descends without shrinking the player");
            start=player.transform.position;
            yield return CreativeHold(.25f,game.Input.Keys["Forward"],game.Input.Keys["Sprint"]);
            Check((player.transform.position-start).magnitude>2&&game.Hunger.Food==0,"Sprint accelerates flight even with zero food");
            yield return CreativeHold(.7f,game.Input.Keys["Crouch"]);
            Check(!world.Overlaps(player.transform.position,.6f,player.Height)&&player.transform.position.y>=world.Local(cell).y-.002f,"Descending flight collides with the floor");
            var oldJump=game.Input.Keys["Jump"];game.Input.Keys["Jump"]=Key.F8;
            start=player.transform.position;yield return CreativeHold(.25f,Key.F8);game.Input.Keys["Jump"]=oldJump;
            Check(player.transform.position.y>start.y+1,"Flight respects rebound Jump control");
            start=player.transform.position;
            yield return CreativeHold(.06f,game.Input.Keys["Inventory"]);
            yield return CreativeHold(.2f,game.Input.Keys["Jump"]);
            Check((player.transform.position-start).magnitude<.01f,"Inventory suppresses flight movement and preserves hover");
            yield return CreativeHold(.06f,game.Input.Keys["Inventory"]);
            yield return Capture("creative-flight");
            yield return new WaitForSeconds(.35f);
            yield return CreativeDoubleJump();Check(!player.Flying,"Double-tap Jump disables flight while retaining Creative");
            float falling=player.transform.position.y;yield return new WaitForSeconds(.2f);
            Check(game.Creative&&player.transform.position.y<falling&&game.TakeDamage(100)==0,"Flight off restores gravity while retaining invincibility");
            yield return new WaitForSeconds(.35f);
            yield return CreativeHold(.04f,game.Input.Keys["Jump"]);
            game.SetMode(ScreenMode.Inventory);yield return null;yield return null;
            yield return CreativeDoubleJump();Check(!player.Flying,"Inventory suppresses flight toggles");
            game.SetMode(ScreenMode.Play);yield return CreativeHold(.04f,game.Input.Keys["Jump"]);
            Check(!player.Flying,"Closing inventory clears a pending flight double tap");
            yield return new WaitForSeconds(.35f);
            var reboundJump=game.Input.Keys["Jump"];game.Input.Keys["Jump"]=Key.F8;
            yield return CreativeDoubleJump();Check(player.Flying,"Flight toggle follows rebound Jump");game.Input.Keys["Jump"]=reboundJump;

            player.enabled=false;player.transform.position=world.Local(cell)+new Vector3(.5f,.02f,.5f);
            player.Camera.transform.position=player.transform.position+Vector3.up*1.64f;
            var support=cell.Offset(0,-1,2);player.Camera.transform.LookAt(world.Local(support)+new Vector3(.5f,1,.5f));
            game.Selected=0;int count=game.Inventory.Slots[0].Count;
            Check(game.TryPlaceSelected()&&game.Inventory.Slots[0].Count==count,"Creative placement builds without consuming the selected stack");
            world.Remove(support.Offset(0,1,0),BlockId.Planks);
            game.Selected=0;game.SetMode(ScreenMode.Pause);yield return null;
            yield return CreativeClick(Named("CREATIVE MODE: ON"));
            Check(!game.Creative&&game.Inventory.Slots[0].Count==count,"Pointer toggle restores Survival and retains items");
            game.SetMode(ScreenMode.Inventory);
            Check(!game.TryGiveCreativeItem(BlockId.Log)&&!game.UI.VisibleRoot.GetComponentsInChildren<Button>().Any(b=>b.name.StartsWith("Creative item ")),"Survival hides the catalog and rejects item grants");
            yield return CreativeDragTo(BlockId.Dirt,CraftPoint(13));
            Check(game.Inventory.Slots[13].Empty,"Survival sidebar drag cannot grant items");
            game.SetMode(ScreenMode.Play);
            Check(game.TryPlaceSelected()&&game.Inventory.Slots[0].Count==count-1,"Survival placement consumes items again");
            Check(game.TakeDamage(2)==2,"Survival damage resumes after disabling Creative");
            player.transform.position=world.Local(cell)+new Vector3(.5f,2,.5f);player.ResetMotion();player.enabled=true;
            float y=player.transform.position.y;yield return new WaitForSeconds(.2f);
            Check(player.transform.position.y<y-.1f,"Gravity resumes after Creative is disabled");
            game.SetCreative(true);game.StartSession(game.Seed+1);
            Check(!game.Creative&&game.Inventory.Slots.All(s=>s.Empty),"A replacement session resets Creative and starts empty-handed");
        }
    }
}
