using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewChickens()
        {
            FreezeSaveFixture();game.Animals.enabled=false;game.enabled=false;yield return Settle();
            var world=game.World;var player=game.Player;var animals=game.Animals;animals.Clear();animals.NaturalSpawning=false;
            var p=world.Address(player.transform.position).Offset(0,3,5);
            void Put(BlockPos cell,byte id)
            {byte old=world.Get(cell);if(old==id)return;if(old!=0)Check(world.Remove(cell,old),"Clear chicken fixture");if(id!=0)Check(world.Place(cell,id),"Place chicken fixture "+id);}
            for(int x=-6;x<=6;x++)for(int z=-6;z<=6;z++)
            {
                Put(p.Offset(x,0,z),BlockId.Grass);
                for(int y=1;y<=3;y++)Put(p.Offset(x,y,z),0);
                if(Math.Abs(x)==6||Math.Abs(z)==6)Put(p.Offset(x,1,z),BlockId.Planks);
            }
            game.Sky.Clock.SetTime(.4);game.Sky.Apply();game.SetCreative(false);game.SetMode(ScreenMode.Play);
            for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,64);
            game.Inventory.Add(FarmId.WheatSeed,12);game.Selected=0;
            Vector3 Feet(int x,int z)=>world.Local(p.Offset(x,1,z))+new Vector3(.5f,.006f,.5f);
            void Aim(Vector3 at,Vector3 from){player.transform.position=from;player.ResetMotion();player.Camera.transform.position=from+Vector3.up*1.5f;player.Camera.transform.LookAt(at);var d=player.Camera.transform.forward;player.Yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;player.Pitch=-Mathf.Asin(d.y)*Mathf.Rad2Deg;}
            // Resident shared eligibility, including daylight and support restrictions.
            Aim(Feet(0,-40),Feet(0,-28));
            float lightDeadline=Time.realtimeSinceStartup+60;byte level=0;
            while(!world.TryGetSpawnLight(p.Offset(0,1,0),false,out level)&&Time.realtimeSinceStartup<lightDeadline)yield return null;
            Check(level>=9&&animals.CanSpawnNatural(Feet(0,0)),"Daylight resident grass outside view permits natural chickens");
            game.Sky.Clock.SetTime(.9);Check(!animals.CanSpawnNatural(Feet(0,0)),"Night prevents new natural chickens without removing existing records");game.Sky.Clock.SetTime(.4);
            Put(p,BlockId.Dirt);Check(!animals.CanSpawnNatural(Feet(0,0)),"Dirt is excluded from the explicit natural grass whitelist");Put(p,BlockId.Grass);
            lightDeadline=Time.realtimeSinceStartup+60;while(!world.TryGetSpawnLight(p.Offset(0,1,0),false,out level)&&Time.realtimeSinceStartup<lightDeadline)yield return null;
            Aim(Feet(0,0)+Vector3.up*.6f,Feet(0,-28));
            var sight=Feet(0,0)+Vector3.up*.6f-player.Camera.transform.position;for(int n=0;n<64&&world.Raycast(player.Camera.transform.position,sight.normalized,sight.magnitude,out var blocked,out _);n++)Put(blocked,0);
            Check(!animals.CanSpawnNatural(Feet(0,0)),"Visible unobstructed sites do not pop in chickens");
            Aim(Feet(0,0)+Vector3.up*.6f,Feet(0,-3));
            var adult=animals.Spawn(Feet(0,0));var partner=animals.Spawn(Feet(2,0));var chick=animals.Spawn(Feet(-2,0),true);
            Check(adult!=null&&partner!=null&&chick!=null,"Adults and chick spawn on clear supported ground");animals.Step();
            foreach(var c in animals.Animals)c.View.Present(c,.1f,world.Origin);
            Check(adult.View.Triangles==3154&&chick.View.Triangles==2586,"Actual Unity adult/chick triangle counts match original Blender export");
            Check(game.Mobs.Mobs.Count==0&&animals.Animals.Count==3,"Passive animals never enter hostile lifecycle collection");
            yield return Capture("chickens-adult-and-chick");
            var origin=player.Camera.transform.position;var direction=(adult.Position.Local(world.Origin)+Vector3.up*.6f-origin).normalized;
            Check(game.SelectInteraction(origin,direction,4,out var target)&&ReferenceEquals(target.Source,animals)&&target.Entity.Target==adult,"Shared nearest-target selector finds chicken");
            var blocker=world.Address(origin+direction*1.4f);Put(blocker,BlockId.Stone);
            Check(game.SelectInteraction(origin,direction,4,out target)&&target.Kind==InteractionTargetKind.Block,"Solid obstruction wins selection over chicken");Put(blocker,0);
            game.Inventory.Add(BlockId.Stone,1);game.Selected=game.Inventory.FindSlot(s=>s.Id==BlockId.Stone);
            Check(!game.CanPlace(world.Address(adult.Position.Local(world.Origin)),out _),"Building cannot overlap a persistent chicken");game.Selected=0;
            // Deferred growth preserves a chick under a one-block-high ceiling.
            chick.Path.Clear();chick.ThinkTicks=1000;chick.GrowthTicks=1;var roof=chick.Position.Cell.Offset(0,1,0);Put(roof,BlockId.Stone);animals.Step();Check(!chick.Adult&&chick.GrowthTicks==1,"Low ceiling prevents adult growth without losing the chick");
            Put(roof,0);animals.Step();Check(chick.Adult&&chick.Health==animals.Catalog.health,"Removing ceiling permits one adult growth transition");
            // Actual bound input; held use cannot repeatedly consume feed.
            Aim(adult.Position.Local(world.Origin)+Vector3.up*.55f,Feet(0,-2));player.enabled=true;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(MouseButton.Right));yield return new WaitForSecondsRealtime(.2f);
            Check(adult.LoveTicks>0&&game.Inventory.Total(FarmId.WheatSeed)==11,"Bound Use feeds selected adult exactly one seed");
            yield return new WaitForSecondsRealtime(.2f);Check(game.Inventory.Total(FarmId.WheatSeed)==11,"Held Use consumes no extra seeds");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;player.enabled=false;adult.View.Present(adult,.1f,world.Origin);yield return Capture("chickens-feeding");
            Aim(partner.Position.Local(world.Origin)+Vector3.up*.5f,Feet(2,-2));Check(animals.TryFeed(partner),"Second nearby adult accepts grain/seed readiness");
            animals.Step();Check(animals.TotalBorn==1&&animals.Animals.Count==4&&!adult.ReadyToBreed&&!partner.ReadyToBreed,"Two fed adults produce exactly one real chick and consume both readiness states");
            int count=animals.Animals.Count;for(int i=0;i<20;i++)animals.Step();Check(animals.Animals.Count==count&&!animals.TryFeed(partner)&&game.Inventory.Total(FarmId.WheatSeed)==10,"Cooldown prevents duplicate offspring and feed consumption");
            foreach(var c in animals.Animals)if(c.View!=null)c.View.Present(c,.1f,world.Origin);
            Aim(Feet(0,0)+Vector3.up*.4f,Feet(0,-4));yield return Capture("chickens-breeding");
            // Lure uses real navigation while the selected feed remains in hand.
            var before=adult.Position.Local(world.Origin);Aim(before,Feet(-3,-3));float distance=Vector3.Distance(before,player.transform.position);
            adult.ThinkTicks=0;adult.Path.Clear();for(int i=0;i<40;i++)animals.Step();
            Check(Vector3.Distance(adult.Position.Local(world.Origin),player.transform.position)<distance-.2f,"Selected seeds lure chickens through shared voxel navigation");
            Check(animals.LastSearches<=2,"Passive fixed-step path search budget remains bounded");
            var savedPlayer=player.transform.position;int timer=adult.EggTicks;int growth=animals.Animals.Single(c=>!c.Adult).GrowthTicks;
            player.transform.position+=Vector3.right*200;for(int i=0;i<30;i++)animals.Step();
            Check(animals.Animals.Count==4&&adult.EggTicks==timer&&animals.Animals.Single(c=>!c.Adult).GrowthTicks==growth,"Distant passive records persist with frozen egg and growth timers");
            player.transform.position=savedPlayer;for(int i=0;i<21;i++)animals.Step();
            int eggs=game.Items.Total(ChickenId.Egg);adult.EggTicks=1;animals.Step();Check(game.Items.Total(ChickenId.Egg)==eggs+1&&adult.EggTicks>=animals.Catalog.eggMinimumTicks,"Egg deadline emits exactly one collectible ordinary item");
            animals.Step();Check(game.Items.Total(ChickenId.Egg)==eggs+1,"Next tick cannot duplicate the egg");
            Aim(adult.Position.Local(world.Origin)+Vector3.up*.18f,adult.Position.Local(world.Origin)+new Vector3(2.5f,0,-1));
            float originalFov=player.Camera.fieldOfView;player.Camera.fieldOfView=35;game.Items.enabled=true;yield return new WaitForSecondsRealtime(.8f);game.Items.enabled=false;
            Check(game.Items.Piles.Any(pile=>pile.Stack.Id==ChickenId.Egg&&pile.View!=null&&pile.View.GetComponentsInChildren<Renderer>().Length>0),"Laid egg has an actual rendered world-item view");yield return Capture("chickens-egg");player.Camera.fieldOfView=originalFov;
            int raw=game.Items.Total(ChickenId.Raw),feathers=game.Items.Total(ChickenId.Feather);Check(animals.Damage(chick,100,Vector3.forward),"Adult takes authoritative damage");
            Check(game.Items.Total(ChickenId.Raw)==raw+1&&game.Items.Total(ChickenId.Feather)>=feathers+1&&game.Items.Total(ChickenId.Feather)<=feathers+2,"Defeated adult awards one raw chicken and one or two feathers");
            int loot=game.Items.Total(ChickenId.Raw)+game.Items.Total(ChickenId.Feather);Check(!animals.Damage(chick,100,Vector3.forward)&&game.Items.Total(ChickenId.Raw)+game.Items.Total(ChickenId.Feather)==loot,"Duplicate damage cannot award loot twice");
            var baby=animals.Animals.Single(c=>!c.Adult);Check(animals.Damage(baby,100,Vector3.forward)&&game.Items.Total(ChickenId.Raw)+game.Items.Total(ChickenId.Feather)==loot,"Chicks award no meat or feather farming shortcut");
            Aim(chick.Position.Local(world.Origin)+Vector3.up*.16f,chick.Position.Local(world.Origin)+new Vector3(2.5f,0,-1));player.Camera.fieldOfView=35;
            game.Items.enabled=true;yield return new WaitForSecondsRealtime(1.3f);game.Items.enabled=false;
            Check(game.Items.Piles.Any(pile=>pile.Stack.Id==ChickenId.Raw&&pile.View!=null)&&game.Items.Piles.Any(pile=>pile.Stack.Id==ChickenId.Feather&&pile.View!=null),"Adult loot has actual rendered meat and feather views");yield return Capture("chickens-drops");player.Camera.fieldOfView=originalFov;
            // Native cooking and recipe-browser snapshots use existing station transactions.
            var cookPos=p.Offset(4,1,-3);Put(cookPos,FarmId.Cooker);var cooker=game.Industry.Simulation.At(cookPos);
            foreach(var recipe in new[]{("rivet:cook_chicken",ChickenId.Raw,ChickenId.Cooked),("rivet:cook_egg",ChickenId.Egg,ChickenId.CookedEgg),("rivet:cook_chicken_stew",ChickenId.Raw,ChickenId.Stew)})
            {
                for(int slot=0;slot<5;slot++)cooker.Items.Take(slot,64);cooker.SelectCooking(recipe.Item1);cooker.Items.Add(recipe.Item2,1,0,1);
                if(recipe.Item3==ChickenId.Stew)cooker.Items.Add(FarmId.Carrot,2,1,2);cooker.Items.Add(BlockId.Coal,1,3,4);
                for(int i=0;i<200;i++)game.Industry.Simulation.Step();
                Check(cooker.Items.Slots[4].Id==recipe.Item3&&cooker.Items.Slots[4].Count==1&&cooker.Items.Slots.Take(3).All(s=>s.Empty),"Placed cooker conserves ingredients for "+recipe.Item1);
                game.Inventory.Add(recipe.Item3,1);
            }
            Aim(world.Local(cookPos)+Vector3.one*.5f,world.Local(cookPos)+new Vector3(.5f,.006f,-2));yield return null;
            Check(game.TryInteractTarget(),"Open reachable cooker");yield return Capture("chickens-cooking");
            foreach(byte id in new[]{ChickenId.Cooked,ChickenId.CookedEgg,ChickenId.Stew}){game.UI.InspectBrowserItem(id,false);yield return Capture("chickens-recipe-"+id);game.UI.CloseBrowserRecipe();}
            game.SetMode(ScreenMode.Play);game.Inventory.Add(ChickenId.Egg,2);game.Inventory.Add(ChickenId.Feather,2);int savedEggs=game.Inventory.Total(ChickenId.Egg),savedFeathers=game.Inventory.Total(ChickenId.Feather);
            // Exact record/timer/random sequence and drops survive a real save replacement.
            game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"Saves"));
            double tickMs=animals.MaximumTickMs;int pathSearches=animals.PathSearches;
            var snapshot=animals.Animals.Select(c=>(c.Id,c.Position,c.Health,c.GrowthTicks,c.EggTicks,c.CooldownTicks,c.LoveTicks,c.RandomState)).ToArray();
            Check(game.SaveGame("Chicken checkpoint"),"Save persistent passive population: "+game.SaveStatus);var entry=game.Saves.List().First(e=>!e.Backup);
            Check(game.LoadGame(entry),"Load passive checkpoint: "+game.SaveStatus);FreezeSaveFixture();game.Animals.enabled=false;game.enabled=false;world=game.World;player=game.Player;animals=game.Animals;
            yield return Settle(120);game.enabled=true;yield return null;game.enabled=false;
            Check(animals.Animals.Count==snapshot.Length&&ReferenceEquals(game.PassiveTargets,animals),"Restore separate passive collection and current interaction source");
            foreach(var old in snapshot){var c=animals.Animals.Single(a=>a.Id==old.Id);Check(c.Position.Equals(old.Position)&&c.Health==old.Health&&c.GrowthTicks==old.GrowthTicks&&c.EggTicks==old.EggTicks&&c.CooldownTicks==old.CooldownTicks&&c.LoveTicks==old.LoveTicks&&c.RandomState==old.RandomState,"Restore exact passive identity position health timers and random state");}
            Check(game.Inventory.Total(ChickenId.Egg)==savedEggs&&game.Inventory.Total(ChickenId.Feather)==savedFeathers&&game.Inventory.Total(ChickenId.Stew)==1&&game.Industry.Simulation.At(cookPos).Items.Slots[4].Id==ChickenId.Stew,"Chicken items and cooker output persist with animals");
            byte[] original=File.ReadAllBytes(entry.Path),body;using(var reader=game.Saves.Open(original,out _))body=reader.ReadBytes((int)(reader.BaseStream.Length-reader.BaseStream.Position));
            var intactWorld=world;var intactAnimals=animals;File.WriteAllBytes(entry.Path,game.Saves.Encode(entry,w=>w.Write(body,0,body.Length-1)));
            Check(!game.LoadGame(entry)&&game.World==intactWorld&&game.Animals==intactAnimals&&ReferenceEquals(game.PassiveTargets,intactAnimals)&&game.Animals.Animals.Count==snapshot.Length,"Late passive-section failure rolls back the exact live animal collection and interaction source");File.WriteAllBytes(entry.Path,original);
            File.WriteAllText(Path.Combine(output,"passive-metrics.txt"),"Peak fixed tick ms (small functional fixture, not population benchmark): "+tickMs+"; path searches: "+pathSearches+"\n");
        }
    }
}
