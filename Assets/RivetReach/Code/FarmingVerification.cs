using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewFarmingLegacy()
        {
            FreezeSaveFixture();game.enabled=false;
            var args=System.Environment.GetCommandLineArgs();int at=System.Array.IndexOf(args,"-rr-save-directory");
            Check(at>=0,"Legacy verification has an isolated save directory");game.InitializeSaves(args[at+1]);
            var entry=game.Saves.List().FirstOrDefault(e=>!e.Backup);Check(entry!=null,"Historical pre-farming content is accepted: "+game.Saves.ScanWarning);
            string legacy=entry.GeneratorVersion;Check(game.LoadGame(entry),"Load historical whole-world snapshot: "+game.SaveStatus);FreezeSaveFixture();game.enabled=false;
            var world=game.World;var oldPlayer=world.Address(game.Player.transform.position);
            Check(world.Generator.GenerationVersion==TerrainGenerator.Version&&world.GenerationAt(oldPlayer.Chunk)==legacy,"Legacy activity stays old while exploration uses the newest generator");
            var saved=world.SavedBlocks().ToArray();Check(saved.All(p=>world.GenerationAt(p.Key.Chunk)==legacy&&world.Get(p.Key)==p.Value),"Every saved edit retains old generation and exact contents");
            var remote=new BlockPos(20000,world.Generator.Height(20000,20000)+1,20000);
            Check(world.GenerationAt(remote.Chunk)==TerrainGenerator.Version,"New remote terrain adopts farming generation");
            yield return Settle(120);byte before=world.Get(oldPlayer.Offset(0,-1,0));
            game.Player.transform.position=world.Local(remote);yield return Settle(120);
            Check(world.Ready(remote)&&world.RecordedChunks>0&&world.GenerationAt(remote.Chunk)==TerrainGenerator.Version,"Exploration records newly generated chunks");
            game.SetMode(ScreenMode.Pause);Check(game.SaveGame("Legacy world with new exploration",true),"Save upgraded generation history without terrain retrofitting");
            var updated=game.Saves.List().First(e=>!e.Backup);Check(game.LoadGame(updated),"Reload mixed-generation checkpoint");FreezeSaveFixture();game.enabled=false;world=game.World;
            Check(world.GenerationAt(oldPlayer.Chunk)==legacy&&world.GenerationAt(remote.Chunk)==TerrainGenerator.Version&&world.Get(oldPlayer.Offset(0,-1,0))==before,"Old and new regions retain their independent generation after reload");
            Check(saved.All(p=>world.Get(p.Key)==p.Value),"Mixed-generation save preserves every original terrain edit");
        }
        IEnumerator ReviewFarming()
        {
            FreezeSaveFixture();game.SetCreative(true);game.enabled=false;
            game.SetMode(ScreenMode.Inventory);yield return null;
            var search=game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.InputField>().Single(f=>f.name=="Item browser search");
            search.text="#edible";yield return null;
            var foodViews=game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Where(v=>v.CatalogSource).ToArray();
            Check(foodViews.Length==11&&foodViews.All(v=>game.Registry.HasTag(v.Item,ItemTags.Edible)),"Actual item browser filters all eleven edible foods");
            yield return Capture("edible-tag-search");search.text="#ingot iron";yield return null;
            var ingotViews=game.UI.VisibleRoot.GetComponentsInChildren<BrowserItemView>().Where(v=>v.CatalogSource).ToArray();
            Check(ingotViews.Length==1&&ingotViews[0].Item==BlockId.IronIngot,"Tag and name filtering intersect in the real item browser");
            search.text="";game.SetMode(ScreenMode.Play);
            var world=game.World;var sim=game.Industry.Simulation;var player=game.Player;
            var p=world.Address(player.transform.position).Offset(0,2,4);
            void Put(BlockPos pos,byte id)
            {byte old=world.Get(pos);if(old==id)return;if(old!=0)Check(world.Remove(pos,old),"Clear farm fixture cell");if(id!=0)Check(world.Place(pos,id),"Place farm fixture "+id);}
            for(int x=-5;x<=5;x++)for(int z=-2;z<=4;z++)
            {Put(p.Offset(x,-1,z),BlockId.Dirt);for(int y=0;y<=2;y++)Put(p.Offset(x,y,z),0);}
            for(int row=0;row<5;row++)
            {
                var crop=CropRules.Definitions[row];
                for(int stage=0;stage<4;stage++)
                {
                    var soil=p.Offset(stage-4,-1,row);Check(world.Till(soil),"Till soil for "+crop.key);Check(world.Plant(soil.Offset(0,1,0),crop.planting),"Plant "+crop.key);
                    for(int n=0;n<stage;n++)Check(world.Grow(soil.Offset(0,1,0),(byte)(crop.first+n)),"Grow "+crop.key+" visible stage");
                }
            }
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);player.HeldBlock.enabled=false;
            player.transform.position=world.Local(p)+new Vector3(.5f,0,-1.6f);
            player.Camera.transform.position=world.Local(p)+new Vector3(-5.5f,4,-3.5f);player.Camera.transform.LookAt(world.Local(p)+new Vector3(-2,.1f,2));
            game.SetMode(ScreenMode.Play);game.Sky.Clock.SetTime(.35);game.Sky.Apply();yield return new WaitForSecondsRealtime(.6f);yield return Capture("crop-stages");
            // Real generated immature plants register through ChunkReady, without a block edit.
            BlockPos? wild=null;
            for(int z=-60;z<=60&&!wild.HasValue;z++)for(int x=-60;x<=60;x++)
            {
                int height=world.Generator.Height(x,z);var cell=new BlockPos(x,height+1,z);byte id=world.Get(cell);var crop=CropRules.For(id);
                if(world.Ready(cell)&&crop!=null&&id<crop.Mature&&world.Get(cell.Offset(0,-1,0))==BlockId.Grass){wild=cell;break;}
            }
            Check(wild.HasValue,"Find an actual generated immature wild plant in resident terrain");
            var wildCell=wild.Value;byte initial=world.Get(wildCell);var wildCrop=CropRules.For(initial);
            long tick=game.Survival.Tick;
            for(int i=0;i<600&&world.Get(wildCell)==initial;i++)game.Survival.AdvanceTicks(20);
            Check(world.Get(wildCell)>initial&&CropRules.For(world.Get(wildCell))==wildCrop,"Wild plant grows on natural grass through the real scheduler");
            player.Camera.transform.position=world.Local(wildCell)+new Vector3(1.2f,1.5f,-1.8f);player.Camera.transform.LookAt(world.Local(wildCell)+new Vector3(.5f,.3f,.5f));
            yield return new WaitForSecondsRealtime(.4f);yield return Capture("wild-growth");
            var immature=p.Offset(-4,0,0);int before=game.Items.Total(BlockId.Potato);Check(world.Mine(immature,world.Get(immature),ToolCapability.None),"Harvest planted crop through world mining");
            Check(game.Items.Total(BlockId.Potato)==before+1,"Immature potato harvest returns exactly one edible/replantable potato");
            // Restore one immature plot for the durable-growth check.
            Check(world.Plant(immature),"Replant saved growing crop");
            var cooker=p.Offset(2,0,0);var electric=p.Offset(4,0,0);Put(cooker,FarmId.Cooker);Put(electric,FarmId.ElectricCooker);
            Put(electric.Offset(0,0,1),IndustryId.PowerCable);Put(electric.Offset(0,0,2),IndustryId.Battery);
            var basic=sim.At(cooker);var powered=sim.At(electric);var battery=sim.At(electric.Offset(0,0,2));battery.EnergyCells[0].Charge(BatteryStorage.CellCapacity);
            foreach(var m in new[]{basic,powered})
            {
                m.SelectCooking("rivet:cook_vegetable_stew");var a=new ItemStack(BlockId.Potato,1);var b=new ItemStack(FarmId.Carrot,1);var c=new ItemStack(FarmId.Mushroom,1);
                m.Click(0,ref a,false);m.Click(1,ref b,false);m.Click(2,ref c,false);Check(a.Empty&&b.Empty&&c.Empty,"Manual tagged ingredients accepted");
            }
            var fuel=new ItemStack(BlockId.Log,1);basic.Click(3,ref fuel,false);Check(fuel.Empty,"Basic cooker accepts tagged wood fuel");
            for(int i=0;i<80;i++)sim.Step();Check(basic.Work==80&&powered.Work==80&&powered.ReceivedWatts==200,"Coal and electric cookers advance the shared recipe");
            player.Camera.transform.position=world.Local(p)+new Vector3(7,3.5f,-4);player.Camera.transform.LookAt(world.Local(p)+new Vector3(3,.5f,1));
            yield return new WaitForSecondsRealtime(.5f);yield return Capture("cookers-running");
            player.Camera.transform.position=world.Local(cooker)+new Vector3(.5f,1.4f,-2.2f);player.Camera.transform.LookAt(world.Local(cooker)+Vector3.one*.5f);
            Check(game.TryOpenMachine(cooker),"Open basic cooker with normal station action");yield return new WaitForSecondsRealtime(.3f);
            Check(game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text=="FUEL"),"Basic cooker panel shows separate fuel");yield return Capture("cooker-interface");
            game.SetMode(ScreenMode.Play);player.Camera.transform.position=world.Local(electric)+new Vector3(.5f,1.4f,-2.2f);player.Camera.transform.LookAt(world.Local(electric)+Vector3.one*.5f);Check(game.TryOpenMachine(electric),"Open electric cooker");yield return new WaitForSecondsRealtime(.3f);
            Check(game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("200 / 200 W")),"Electric cooker panel shows actual allocated power");yield return Capture("electric-cooker-interface");
            game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"Saves"));long energy=battery.EnergyCells[0].Amount;int heat=basic.BurnTicks;int recorded=world.RecordedChunks;
            Check(game.SaveGame("Farming and cookers"),"Save crops, cooker selections, heat, power and generated chunk history: "+game.SaveStatus);
            Check(game.LoadGame(game.Saves.List().First(e=>!e.Backup)),"Load complete farming checkpoint: "+game.SaveStatus);FreezeSaveFixture();game.enabled=false;
            world=game.World;sim=game.Industry.Simulation;yield return Settle(120);basic=sim.At(cooker);powered=sim.At(electric);battery=sim.At(electric.Offset(0,0,2));
            Check(basic.Work==80&&powered.Work==80&&basic.BurnTicks==heat&&battery.EnergyCells[0].Amount==energy,"Load preserves exact partial work and energy");
            Check(world.Get(immature)==BlockId.PotatoPlant&&world.RecordedChunks>=recorded,"Load retains immature growth and generated chunk history");
            for(int i=0;i<120;i++)sim.Step();Check(basic.Items.Slots[4].Id==FarmId.Stew&&powered.Items.Slots[4].Id==FarmId.Stew&&basic.Items.Slots.Take(3).All(s=>s.Empty),"Loaded cookers complete exactly once");
            // Exercise shared item-pipe transport, mixed ingredients, rear fuel and output extraction.
            Put(cooker.Offset(-1,0,0),IndustryId.ItemPipe);Put(cooker.Offset(-2,0,0),BlockId.Chest);Put(cooker.Offset(0,0,1),IndustryId.ItemPipe);Put(cooker.Offset(0,0,2),BlockId.Chest);
            Put(cooker.Offset(0,0,-1),IndustryId.ItemPipe);Put(cooker.Offset(0,0,-2),BlockId.Chest);
            var inputs=game.Survival.At(cooker.Offset(-2,0,0)).Storage;inputs.Add(BlockId.Potato,8);inputs.Add(FarmId.Mushroom,4);
            var fuels=game.Survival.At(cooker.Offset(0,0,2)).Storage;fuels.Add(BlockId.Coal,2);
            void Mode(MachineState pipe,int face,PortRole role){for(int i=0;i<3&&sim.PipeEndRole(pipe,face)!=role;i++)Check(sim.TogglePipeEnd(pipe,face),"Configure cooker pipe end");}
            var ip=sim.At(cooker.Offset(-1,0,0));Mode(ip,1,PortRole.Output);Mode(ip,0,PortRole.Input);
            var fp=sim.At(cooker.Offset(0,0,1));Mode(fp,4,PortRole.Output);Mode(fp,5,PortRole.Input);
            var op=sim.At(cooker.Offset(0,0,-1));Mode(op,4,PortRole.Output);Mode(op,5,PortRole.Input);
            for(int i=0;i<1200;i++)sim.Step();var outputChest=game.Survival.At(cooker.Offset(0,0,-2)).Storage;
            Check(outputChest.Total(FarmId.Stew)==5&&inputs.Total(BlockId.Potato)==0&&inputs.Total(FarmId.Mushroom)==0,"Pipes deliver four complete tagged batches and extract five meals without ingredient starvation");
            game.SetMode(ScreenMode.Play);game.Player.Arms.gameObject.SetActive(false);game.Player.Body.gameObject.SetActive(false);game.Player.HeldBlock.enabled=false;
            game.Player.Camera.transform.position=world.Local(cooker)+new Vector3(4.5f,4.8f,-5.7f);game.Player.Camera.transform.LookAt(world.Local(cooker)+new Vector3(-.2f,.3f,0));
            yield return new WaitForSecondsRealtime(.5f);yield return Capture("cooker-pipes");
            var hunger=new HungerState();hunger.Exert(60);var inventory=new Inventory(id=>game.Registry.Get(id).stackLimit);inventory.Add(FarmId.Stew,1,0,1);
            Check(hunger.TryEat(inventory,0,game.Registry.FoodPoints(FarmId.Stew))&&hunger.Food==17,"Prepared stew restores twelve points with one item");
        }
    }
}
