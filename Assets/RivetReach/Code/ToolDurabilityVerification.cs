using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewToolsLegacy()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-rr-save-directory");Check(at>=0,"Historical fixture directory provided");game.InitializeSaves(args[at+1]);
            var entry=game.Saves.List().First(e=>!e.Backup);var bytes=game.Saves.Read(entry);
            using(var reader=game.Saves.Open(bytes,out _))Check(reader.Format<18,"Actual historical checkpoint predates tool wear: schema "+reader.Format);
            var changed=UnityEngine.Object.Instantiate(game.Registry);changed.Get(BlockId.IronIngot).stackLimit++;bool rejected=false;
            try{using var reader=new SaveStore(output,changed).Open(bytes,out _);}catch(InvalidDataException){rejected=true;}
            Destroy(changed);Check(rejected,"Historical saves still reject unrelated item changes");
            Check(game.LoadGame(entry),"Load historical full world: "+game.SaveStatus);FreezeSaveFixture();game.enabled=false;game.Animals.enabled=false;
            Check(game.Inventory.Slots.All(s=>s.Wear==0),"Old inventory tools begin pristine");
            int food=game.Hunger.Food,reserve=game.Hunger.Saturation;double exhaustion=game.Hunger.Exhaustion;
            game.InitializeSaves(Path.Combine(output,"Migrated"));Check(game.SaveGame("Migrated tools",true),"Write migrated schema18 checkpoint");entry=game.Saves.List().First(e=>!e.Backup);bytes=game.Saves.Read(entry);
            Check(game.LoadGame(entry),"Reload migrated world");FreezeSaveFixture();game.enabled=false;game.Animals.enabled=false;
            Check(game.CaptureSave(entry).SequenceEqual(bytes)&&game.Hunger.Food==food&&game.Hunger.Saturation==reserve&&game.Hunger.Exhaustion==exhaustion,"Migration preserves complete state and exact food reserve");yield return Settle(120);
        }
        IEnumerator ReviewToolsResume()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-rr-save-directory");
            Check(at>=0,"Isolated tools checkpoint directory provided");game.InitializeSaves(args[at+1]);
            var entry=game.Saves.List().First(e=>!e.Backup);byte[] before=game.Saves.Read(entry);
            Check(game.LoadGame(entry),"Fresh process loads tools checkpoint: "+game.SaveStatus);FreezeSaveFixture();game.enabled=false;game.Animals.enabled=false;
            Check(game.Inventory.Slots.Any(s=>s.Wear>0),"Fresh process restores worn tools");
            Check(game.CaptureSave(entry).SequenceEqual(before),"Fresh process preserves every saved field byte-for-byte, including food and tool wear");
            yield return Settle(120);
        }
        IEnumerator ReviewTools()
        {
            if(!Environment.GetCommandLineArgs().Contains("-rr-tools-focused"))yield return ReviewAlphaSurvival();
            FreezeSaveFixture();game.enabled=false;game.Animals.enabled=false;game.SetCreative(false);yield return Settle();
            var world=game.World;var player=game.Player;var p=world.Address(player.transform.position).Offset(0,3,0);
            void Put(BlockPos cell,byte id){byte old=world.Get(cell);if(old!=0)Check(world.Remove(cell,old),"Clear tool fixture");if(id!=0)Check(world.Place(cell,id),"Place tool fixture");}
            for(int x=-4;x<=8;x++)for(int z=-4;z<=7;z++)for(int y=-1;y<4;y++)Put(p.Offset(x,y,z),y==-1?BlockId.Planks:(byte)0);
            yield return Settle();
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);
            void Select(byte id,int wear=0){game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(new ItemStack(id,1){Wear=wear},0,1);game.Selected=0;}
            void Aim(BlockPos cell)
            {
                player.transform.position=world.Local(p)+new Vector3(.5f,.02f,-2.5f);player.ResetMotion();
                Vector3 delta=(world.Local(cell)+Vector3.one*.5f-(player.transform.position+Vector3.up*1.62f)).normalized;
                player.Yaw=Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(delta.y)*Mathf.Rad2Deg;
            }
            void Mouse(bool down)=>InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,down?new MouseState().WithButton(MouseButton.Left):new MouseState());
            game.SetMode(ScreenMode.Play);game.enabled=true;player.enabled=true;game.Sky.Clock.SetTime(.35);game.Sky.Apply();yield return null;game.enabled=false;
            var csv=new StringBuilder("item,block,expected_seconds,observed_seconds,wear\n");
            foreach(byte block in new[]{BlockId.Stone,BlockId.Log})
            {
                float previous=100;
                for(int tier=0;tier<5;tier++)
                {
                    byte id=(byte)((block==BlockId.Stone?BlockId.WoodPickaxe:BlockId.WoodAxe)+tier*5);Select(id);Put(p,block);Aim(p);yield return null;yield return null;
                    Check(player.HasTarget&&player.Target.Equals(p),"Actual selection targets tier timing block");
                    float elapsed=0,expected=game.Registry.MiningSeconds(block,game.Inventory.Slots[0]);Mouse(true);
                    while(world.Get(p)==block&&elapsed<5){yield return null;elapsed+=Time.deltaTime;}
                    Mouse(false);yield return null;
                    Check(world.Get(p)==0&&game.Inventory.Slots[0].Wear==1,"Actual mining removes block and wears tool exactly once: "+id);
                    Check(Mathf.Abs(elapsed-expected)<.16f&&elapsed<previous,"Higher tier mines faster through player input: "+id);previous=elapsed;
                    csv.AppendLine(FormattableString.Invariant($"{game.Registry.Get(id).stableId},{block},{expected},{elapsed},{game.Inventory.Slots[0].Wear}"));
                    if(tier==4&&block==BlockId.Stone){Put(p,BlockId.Stone);Aim(p);yield return null;yield return Capture("diamond-pickaxe-used");Put(p,0);}
                }
            }
            File.WriteAllText(Path.Combine(output,"mining-times.csv"),csv.ToString());
            Select(BlockId.WoodPickaxe);Put(p,BlockId.Stone);Aim(p);yield return null;Mouse(true);yield return new WaitForSecondsRealtime(.2f);Mouse(false);yield return null;
            Check(world.Get(p)==BlockId.Stone&&game.Inventory.Slots[0].Wear==0,"Cancelled mining spends no durability");
            game.Inventory.Add(BlockId.WoodPickaxe,1,1,2);Mouse(true);yield return new WaitForSecondsRealtime(.3f);game.Selected=1;yield return null;
            Check(player.MiningProgress<.1f,"Switching to another same-kind tool resets unfinished progress");Mouse(false);yield return null;
            Select(BlockId.WoodPickaxe);Put(p,BlockId.GoldBlock);Aim(p);yield return null;Mouse(true);yield return new WaitForSecondsRealtime(.3f);Mouse(false);yield return null;
            Check(world.Get(p)==BlockId.GoldBlock&&game.Inventory.Slots[0].Wear==0,"Ineligible extraction grade spends no durability");
            int beforeFinalDrops=game.Items.Piles.Where(pile=>pile.Stack.Id==BlockId.Cobblestone).Sum(pile=>pile.Stack.Count);
            Put(p,BlockId.Stone);Select(BlockId.WoodPickaxe,63);Aim(p);yield return null;Mouse(true);float deadline=Time.realtimeSinceStartup+4;
            while(world.Get(p)==BlockId.Stone&&Time.realtimeSinceStartup<deadline)yield return null;Mouse(false);yield return null;
            Check(world.Get(p)==0&&game.Inventory.Slots[0].Empty&&game.Items.Piles.Where(pile=>pile.Stack.Id==BlockId.Cobblestone).Sum(pile=>pile.Stack.Count)==beforeFinalDrops+1,"Final use pays its drop exactly once and breaks the exhausted pickaxe");
            Put(p,BlockId.Stone);Select(BlockId.DiamondPickaxe,100);game.SetCreative(true);Aim(p);yield return null;Mouse(true);yield return null;yield return null;Mouse(false);yield return null;
            Check(world.Get(p)==0&&game.Inventory.Slots[0].Wear==100,"Creative mining preserves tool durability");game.SetCreative(false);
            Put(p,BlockId.Dirt);Select(BlockId.WoodHoe);Aim(p);yield return null;
            InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return null;yield return null;Mouse(false);yield return null;
            Check(world.Get(p)==BlockId.Farmland&&game.Inventory.Slots[0].Wear==1,"Actual hoe Use tills once and spends one durability");
            // Shared combat entry points pay wear only for accepted damage.
            Put(p,0);Select(BlockId.WoodSword);Aim(p);player.transform.position+=Vector3.forward*.6f;yield return null;
            game.Mobs.Clear();var definition=Resources.LoadAll<MobDefinition>("Mobs/Definitions").First(d=>d.stableId=="rivet:rustback_beetle");
            var mob=game.Mobs.Spawn(definition,world.Local(p)+new Vector3(.5f,0,.5f));
            Vector3 look=(mob.Position.Local(world.Origin)+Vector3.up*definition.height*.5f-player.Camera.transform.position).normalized;
            player.Yaw=Mathf.Atan2(look.x,look.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(look.y)*Mathf.Rad2Deg;yield return null;
            int health=mob.Health;Check(game.Mobs.HandlePlayerTarget(true)&&mob.Health<health&&game.Inventory.Slots[0].Wear==1,"Landed hostile strike spends exactly one sword use");
            game.Mobs.Clear();game.Animals.Clear();Select(BlockId.WoodAxe);
            var chicken=game.Animals.Spawn(world.Local(p)+new Vector3(.5f,0,.5f));Check(chicken!=null,"Spawn passive target on fixture floor");
            game.Animals.Interact(chicken,true,false,false);Check(game.Inventory.Slots[0].Wear==1,"Landed passive strike spends exactly one axe use");game.Animals.Clear();
            var tree=world.Generator.Trees(p.X-40,p.Z-40,p.X+40,p.Z+40).First(t=>world.Ready(t.Root)&&world.Get(t.Root)==BlockId.Log&&world.NaturalLog(t.Root)&&world.Get(t.Root.Offset(0,1,0))==BlockId.Log);
            var feet=tree.Root.Offset(0,0,-2);
            // Natural roots can sit beside a slope; prepare a clear approach without touching the tree.
            for(int z=-3;z<0;z++){Put(tree.Root.Offset(0,-1,z),BlockId.Planks);for(int y=0;y<3;y++)Put(tree.Root.Offset(0,y,z),0);}
            player.transform.position=world.Local(feet)+new Vector3(.5f,.02f,.5f);player.ResetMotion();Select(BlockId.WoodAxe,63);
            look=(world.Local(tree.Root)+Vector3.one*.5f-(player.transform.position+Vector3.up*1.62f)).normalized;
            player.Yaw=Mathf.Atan2(look.x,look.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(look.y)*Mathf.Rad2Deg;yield return null;yield return null;
            Check(player.HasTarget&&player.Target.Equals(tree.Root),"Select natural trunk for final-use felling: "+player.Target+" / "+player.TargetId+" expected "+tree.Root);Mouse(true);deadline=Time.realtimeSinceStartup+3;
            while(world.Get(tree.Root)==BlockId.Log&&Time.realtimeSinceStartup<deadline)yield return null;Mouse(false);yield return null;
            for(int step=0;step<40;step++)world.AdvanceTrees(.1f);
            Check(game.Inventory.Slots[0].Empty&&world.Get(tree.Root.Offset(0,1,0))==0,"Final axe use breaks tool once and queued natural-tree felling completes");
            Aim(p);yield return null;
            player.enabled=false;game.SetMode(ScreenMode.Pause);
            // Transfer a worn tool through an actual pipe without reducing it to an item ID.
            var sourcePos=p.Offset(3,0,2);Put(sourcePos,BlockId.Chest);Put(sourcePos.Offset(1,0,0),IndustryId.ItemPipe);Put(sourcePos.Offset(2,0,0),BlockId.Chest);
            var sim=game.Industry.Simulation;var source=game.Survival.At(sourcePos).Storage;var destination=game.Survival.At(sourcePos.Offset(2,0,0)).Storage;
            var worn=new ItemStack(BlockId.IronAxe,1){Wear=311};source.Add(worn);var pipe=sim.At(sourcePos.Offset(1,0,0));
            void Role(int face,PortRole role){for(int i=0;i<3&&sim.PipeEndRole(pipe,face)!=role;i++)Check(sim.TogglePipeEnd(pipe,face),"Configure tools pipe");}
            Role(1,PortRole.Output);Role(0,PortRole.Input);for(int i=0;i<20;i++)sim.Step();
            Check(source.Total(worn.Id)==0&&destination.Slots.Any(s=>s.Equals(worn)),"Actual item pipes preserve exact worn-tool condition");
            game.Items.Piles.Clear();game.Items.Spawn(worn,world.Local(p)+Vector3.up*2,Vector3.zero);game.Items.Spawn(worn,world.Local(p)+Vector3.up*2,Vector3.zero);
            for(int step=0;step<20;step++)game.Items.Step(.05f);
            Check(game.Items.Piles.Count==2&&game.Items.Piles.All(pile=>pile.Stack.Equals(worn)),"Identically worn dropped tools remain separate");
            player.transform.position=game.Items.Piles[0].Position.Local(world.Origin);game.Items.Step(.05f);
            Check(game.Items.Piles.Count==0&&game.Inventory.Slots.Count(s=>s.Equals(worn))==2,"Pickup preserves two separate identically worn tools");
            int recovered=game.Inventory.FindSlot(s=>s.Equals(worn));game.Drop(game.Inventory.Take(recovered,1));
            Check(game.Items.Piles.Count==1&&game.Items.Piles[0].Stack.Equals(worn),"Manual re-drop preserves exact condition");
            Select(BlockId.WoodPickaxe,40);game.Inventory.Add(new ItemStack(BlockId.WoodPickaxe,1){Wear=20});game.Inventory.Add(new ItemStack(BlockId.StonePickaxe,1){Wear=64});game.Inventory.Add(new ItemStack(BlockId.CopperPickaxe,1){Wear=64});game.Inventory.Add(new ItemStack(BlockId.IronPickaxe,1){Wear=128});game.Inventory.Add(new ItemStack(BlockId.DiamondPickaxe,1){Wear=256});game.Inventory.Add(worn);
            player.enabled=true;player.ResetMotion();game.Notify("",0);game.SetMode(ScreenMode.Inventory);game.UI.HoverSlot(0);yield return null;yield return Capture("tool-durability-inventory");
            Check(game.UI.VisibleRoot.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("Durability 24 / 64")),"Inventory displays exact remaining durability");
            game.SetMode(ScreenMode.Play);Aim(p);yield return null;yield return Capture("tool-durability-hotbar");
            player.enabled=false;game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"Saves"));Check(game.SaveGame("Tool wear"),"Save worn tools, chest and drops: "+game.SaveStatus);
            var entry=game.Saves.List().First(e=>!e.Backup);byte[] before=game.Saves.Read(entry);
            Check(game.LoadGame(entry),"Restore schema18 tools checkpoint: "+game.SaveStatus);FreezeSaveFixture();game.enabled=false;game.Animals.enabled=false;
            Check(game.CaptureSave(entry).SequenceEqual(before),"Complete tool checkpoint reloads byte-for-byte without repairing tools");
            yield return Settle(120);
        }
    }
}
