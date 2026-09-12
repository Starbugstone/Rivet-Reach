using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewPickupRanges()
        {
            var world=game.World;var player=game.Player;var items=game.Items;
            var saved=WorldPoint.FromLocal(player.transform.position,world.Origin);var carried=game.Inventory.Slots.ToArray();
            var existing=items.Piles.ToArray();var cell=world.Address(player.transform.position).Offset(0,12,0);
            var at=world.Local(cell);byte previous=world.Get(cell);
            player.enabled=false;items.enabled=false;
            player.transform.position=at+new Vector3(.5f,-.2f,-1.7f);
            void ClearInventory(){for(int i=0;i<game.Inventory.Count;i++)game.Inventory.Take(i,int.MaxValue);}
            void ClearDrops(){foreach(var p in items.Piles.Except(existing).ToArray()){if(p.View!=null)Destroy(p.View);items.Piles.Remove(p);}}
            DroppedItems.Pile Spawn(float distance,bool action=false,int count=1,float delay=0)
            {
                items.Spawn(new ItemStack(BlockId.Dirt,count),player.transform.position+Vector3.up*.5f+Vector3.forward*distance,Vector3.zero,delay,action);
                var pile=items.Piles.Last();pile.Sleeping=true;return pile;
            }
            ClearInventory();
            var normal=Spawn(2.03f);var outside=Spawn(2.05f);
            items.Step(0);
            Check(!items.Piles.Contains(normal)&&items.Piles.Contains(outside)&&game.Inventory.Total(BlockId.Dirt)==1,"Ordinary pickup includes 2.03 m and excludes 2.05 m around the new 2.04 m radius");
            ClearDrops();ClearInventory();
            if(previous!=0)world.Remove(cell,previous);
            Check(world.Place(cell,BlockId.Dirt)&&world.Mine(cell,BlockId.Dirt,ToolCapability.None),"Real mining commits the pickup fixture and creates its drop");
            var mined=items.Piles.Except(existing).Single();mined.Sleeping=true;
            var unrelated=Spawn(2.2f);
            items.Step(0);
            Check(mined.ActionCreated&&!items.Piles.Contains(mined)&&items.Piles.Contains(unrelated)&&game.Inventory.Total(BlockId.Dirt)==1,"Only the exact mined pile is collected at 2.2 m; an identical existing item stays");
            ClearDrops();ClearInventory();
            var boosted=Spawn(2.44f,true);var beyond=Spawn(2.46f,true);
            items.Step(0);
            Check(!items.Piles.Contains(boosted)&&items.Piles.Contains(beyond),"Action pickup includes 2.44 m and excludes 2.46 m around the 2.448 m radius");
            ClearDrops();ClearInventory();
            int capacity=game.Inventory.Count*game.Registry.Get(BlockId.Dirt).stackLimit;
            game.Inventory.Add(BlockId.Dirt,capacity-1);
            var partial=Spawn(2.2f,true,3);items.Step(0);
            Check(partial.Stack.Count==2&&partial.ActionCreated&&game.Inventory.Total(BlockId.Dirt)==capacity,"Partial action pickup conserves the remainder and its own range");
            var ordinary=Spawn(2.2f);var matching=Spawn(2.2f,true,2);items.Step(.5f);
            Check(items.Piles.Contains(ordinary)&&ordinary.Stack.Count==1&&items.Piles.Contains(partial)&&partial.Stack.Count==4&&!items.Piles.Contains(matching),"Matching action piles merge but cannot transfer their bonus to ordinary piles of the same item");
            ClearDrops();ClearInventory();
            var delayed=Spawn(2.2f,true,1,1);items.Step(.5f);
            Check(items.Piles.Contains(delayed)&&game.Inventory.Total(BlockId.Dirt)==0,"Action pickup still honors a pickup delay");
            items.Step(.5f);Check(!items.Piles.Contains(delayed),"Eligible action pile is collected after its delay");
            ClearDrops();ClearInventory();
            var blocked=Spawn(2.2f,true);var barrier=cell.Offset(0,0,-1);
            byte oldBarrier=world.Get(barrier);if(oldBarrier!=0)world.Remove(barrier,oldBarrier);
            Check(world.Place(barrier,BlockId.Stone),"Pickup barrier fixture is resident");items.Step(0);
            Check(items.Piles.Contains(blocked)&&game.Inventory.Total(BlockId.Dirt)==0,"The action bonus cannot collect through a solid barrier");
            world.Remove(barrier,BlockId.Stone);items.Step(0);
            Check(!items.Piles.Contains(blocked),"Removing the barrier allows action pickup");
            ClearDrops();ClearInventory();
            foreach(byte passable in new[]{IndustryId.FluidPipe,IndustryId.ItemPipe,IndustryId.PowerCable,IndustryId.SignalConduit,IndustryId.SignalWire})
            {
                Check(world.Place(barrier,passable)&&!world.Solid(barrier),"Placed pickup fixture is passable: "+passable);
                Vector3 start=player.transform.position+Vector3.up*.9f;
                Check(world.Raycast(start,Vector3.forward,2,out var target,out _)&&target.Equals(barrier),"Passable fixture remains interaction-targetable: "+passable);
                var through=Spawn(1.9f);var actionThrough=Spawn(2.2f,true);
                items.Step(0);
                Check(!items.Piles.Contains(through)&&!items.Piles.Contains(actionThrough)&&game.Inventory.Total(BlockId.Dirt)==2,"Normal and action pickup pass through placed block: "+passable);
                ClearInventory();var underneath=Spawn(1.2f);
                Check(underneath.Position.Cell.Equals(barrier)&&!world.Overlaps(underneath.Position.Local(world.Origin),DroppedItems.CollisionWidth,DroppedItems.CollisionHeight),"Dropped item fits within passable block cell: "+passable);
                items.Step(0);
                Check(!items.Piles.Contains(underneath)&&game.Inventory.Total(BlockId.Dirt)==1,"Normal pickup collects an item inside the passable block cell: "+passable);
                world.Remove(barrier,passable);ClearDrops();ClearInventory();
            }
            Check(world.Place(barrier,IndustryId.Crusher),"Place solid machine pickup barrier");
            var behindMachine=Spawn(1.9f);items.Step(0);
            Check(items.Piles.Contains(behindMachine)&&game.Inventory.Total(BlockId.Dirt)==0,"Solid placed machines still block normal pickup");
            world.Remove(barrier,IndustryId.Crusher);ClearDrops();ClearInventory();
            var support=barrier.Offset(0,-1,0);byte oldSupport=world.Get(support);
            if(oldSupport!=0)world.Remove(support,oldSupport);
            Check(world.Place(support,BlockId.Stone)&&world.Place(barrier,IndustryId.WoodenDoor),"Place supported door pickup fixture");
            var behindDoor=Spawn(1.9f);items.Step(0);
            Check(items.Piles.Contains(behindDoor)&&game.Inventory.Total(BlockId.Dirt)==0,"Closed door blocks normal pickup");
            game.Industry.Simulation.ToggleDoor(game.Industry.Simulation.At(barrier));items.Step(0);
            Check(!world.Solid(barrier)&&!items.Piles.Contains(behindDoor)&&game.Inventory.Total(BlockId.Dirt)==1,"Opening the door immediately allows normal pickup");
            world.Remove(barrier,IndustryId.WoodenDoor);world.Remove(support,BlockId.Stone);
            if(oldSupport!=0)world.Place(support,oldSupport);
            if(oldBarrier!=0)world.Place(barrier,oldBarrier);
            ClearDrops();ClearInventory();
            if(previous!=0)world.Place(cell,previous);
            for(int i=0;i<carried.Length;i++)if(!carried[i].Empty)game.Inventory.Add(carried[i].Id,carried[i].Count,i,i+1);
            player.transform.position=saved.Local(world.Origin);player.enabled=true;items.enabled=true;
            yield return null;
        }
        IEnumerator ReviewPlacementItems()
        {
            var world=game.World;var player=game.Player;var items=game.Items;
            var saved=WorldPoint.FromLocal(player.transform.position,world.Origin);var carried=game.Inventory.Slots.ToArray();int selected=game.Selected;
            var existing=items.Piles.ToArray();var changes=new Dictionary<BlockPos,byte>();
            void Set(BlockPos p,byte id)
            {
                if(!changes.ContainsKey(p))changes[p]=world.Get(p);
                byte old=world.Get(p);if(old!=0&&old!=id)world.Remove(p,old);if(id!=0&&old!=id)Check(world.Place(p,id),"Placement-item fixture uses ready terrain");
            }
            int y=0;for(int x=28;x<=34;x++)for(int z=-7;z<=-1;z++)y=Math.Max(y,world.Generator.Height(x,z)+TerrainGenerator.MaxTreeHeight+3);
            var cell=new BlockPos(31,y,-4);Vector3 at=world.Local(cell);
            game.SetMode(ScreenMode.Play);game.Diagnostics=false;player.enabled=false;
            player.transform.position=at+new Vector3(.5f,0,-3);player.Camera.transform.position=at+new Vector3(.5f,.55f,-2);player.Camera.transform.LookAt(at+new Vector3(.5f,.55f,1.5f));
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
            for(int z=-2;z<=2;z++)for(int x=-2;x<=2;x++)Set(cell.Offset(x,-1,z),3);
            Set(cell.Offset(0,0,1),3);
            for(int slot=0;slot<60;slot++)game.Inventory.Take(slot,int.MaxValue);
            game.Inventory.Add(3,game.Inventory.Count*game.Registry.Get(3).stackLimit);game.Selected=0;
            items.Spawn(new ItemStack(2,37),at+new Vector3(.5f,.02f,.5f),Vector3.zero,5);
            items.Spawn(new ItemStack(2,11),at+new Vector3(.60f,.02f,.5f),Vector3.zero,5);
            items.Spawn(new ItemStack(1,5),at+new Vector3(-.08f,.02f,.5f),Vector3.zero,5);
            yield return null;yield return null;items.enabled=false;
            var piles=items.Piles.Except(existing).ToArray();
            for(int i=0;i<piles.Length;i++){piles[i].Sleeping=true;piles[i].Age=123+i;piles[i].Delay=5;}
            var records=piles.Select(p=>(pile:p,id:p.Id,stack:p.Stack,age:p.Age,delay:p.Delay,position:p.Position.Local(world.Origin))).ToArray();
            Check(game.Inventory.Slots.All(s=>!s.Empty),"Placement can start with a full inventory and uncollectable loose stacks");
            Check(game.PlacementPreview(out var aimed,out _)&&aimed.Equals(cell),"The real aimed face targets the cell containing dropped items");
            Check(game.CanPlace(cell,out _)&&records.All(r=>r.pile.Position.Local(world.Origin)==r.position),"Placement validation allows piles without moving them before commit");
            yield return Capture("placement-items-before");
            int before=game.Inventory.Total(3),spawned=items.TotalSpawned,expired=items.TotalExpired;
            Check(game.TryPlaceSelected()&&world.Get(cell)==3&&game.Inventory.Total(3)==before-1,"Placing through several loose stacks commits one block and consumes exactly one item");
            changes[cell]=0;
            Check(records.All(r=>r.pile.Id==r.id&&r.pile.Stack.Id==r.stack.Id&&r.pile.Stack.Count==r.stack.Count&&r.pile.Age==r.age&&r.pile.Delay==r.delay),"Displacement preserves pile identity, quantities, lifetime and pickup delay");
            Check(piles.All(p=>p.Position.Local(world.Origin).y>=at.y+1&&!world.Overlaps(p.Position.Local(world.Origin),DroppedItems.CollisionWidth,DroppedItems.CollisionHeight)),"Sleeping piles and a pile straddling the block edge move clear above the new block");
            Check(piles.All(p=>!p.Sleeping&&p.Velocity.y>0),"Displaced piles wake with an upward pop");
            Check(piles.All(p=>p.View!=null&&Vector3.Distance(p.View.transform.position,p.Position.Local(world.Origin)+Vector3.up*.115f)<.0001f),"Pile views leave the block in the same placement turn");
            yield return Capture("placement-items-pop-up");
            // Reset only the fixture's drops, then block the top and three sides. The free
            // right exit crosses X=31/32 into another resident chunk.
            Set(cell,0);Set(cell.Offset(0,1,0),3);Set(cell.Offset(-1,0,0),3);Set(cell.Offset(0,0,-1),3);
            foreach(var p in piles){p.Position=WorldPoint.FromLocal(at+new Vector3(.5f,.02f,.5f),world.Origin);p.Sleeping=true;}
            Check(world.Place(cell,3),"A block can also commit beneath a low ceiling with loose items present");
            Check(piles.All(p=>p.Position.Cell.X==32&&!world.Overlaps(p.Position.Local(world.Origin),DroppedItems.CollisionWidth,DroppedItems.CollisionHeight)&&p.Velocity.x>0),"A low ceiling shoves piles into the free side across a chunk seam");
            player.Camera.transform.position=at+new Vector3(3,1.4f,-2.5f);player.Camera.transform.LookAt(at+new Vector3(.5f,.4f,.5f));
            yield return Capture("placement-items-sideways");
            // Filling the last cell of a one-block shell still preserves the drops: use
            // the nearest clear exit beyond the shell instead of retaining a solid overlap.
            Set(cell,0);Set(cell.Offset(1,0,0),3);
            foreach(var p in piles){p.Position=WorldPoint.FromLocal(at+new Vector3(.5f,.02f,.5f),world.Origin);p.Sleeping=true;}
            Check(world.Place(cell,3),"Dropped items do not prevent closing a small enclosure");
            Check(piles.All(p=>!world.Overlaps(p.Position.Local(world.Origin),DroppedItems.CollisionWidth,DroppedItems.CollisionHeight)),"Enclosed piles escape beyond adjacent solid cells without clipping");
            Check(records.All(r=>r.pile.Stack.Id==r.stack.Id&&r.pile.Stack.Count==r.stack.Count&&r.pile.Age==r.age&&r.pile.Delay==r.delay)&&items.TotalSpawned==spawned&&items.TotalExpired==expired,"Repeated placement neither duplicates nor deletes drops or resets their timers");
            for(int step=0;step<30;step++)items.Step(.05f);
            Check(piles.All(p=>!world.Overlaps(p.Position.Local(world.Origin),DroppedItems.CollisionWidth,DroppedItems.CollisionHeight)),"Popped items remain outside solids as normal gravity resumes");
            Check(piles.Sum(p=>p.Stack.Count)==53&&piles.All(p=>p.Sleeping),"The original dropped quantities settle back to sleep intact");
            foreach(var p in piles){if(p.View!=null)Destroy(p.View);items.Piles.Remove(p);}
            // Dense mixed drops share one cell. Only matching identities consolidate,
            // and redundant scene objects must disappear as well as their records.
            Set(cell,0);Set(cell.Offset(0,1,0),0);Set(cell.Offset(-1,0,0),0);Set(cell.Offset(1,0,0),0);Set(cell.Offset(0,0,-1),0);Set(cell.Offset(0,0,1),0);
            for(int i=0;i<100;i++)
            {
                items.Spawn(new ItemStack(2,1),at+new Vector3(.32f,.003f,.5f),Vector3.zero,5);
                items.Spawn(new ItemStack(1,1),at+new Vector3(.68f,.003f,.5f),Vector3.zero,5);
            }
            items.enabled=true;yield return null;yield return null;items.enabled=false;
            var dense=items.Piles.Except(existing).ToArray();
            Check(dense.Length==200&&dense.All(p=>p.View!=null),"Dense fixture begins with 200 separate dropped-item records and views");
            foreach(var p in dense){p.Sleeping=true;p.Delay=0;p.Age=0;}
            dense[0].Age=200;
            int denseSpawned=items.TotalSpawned,denseExpired=items.TotalExpired;
            items.Step(.5f);yield return null;
            var merged=items.Piles.Except(existing).ToArray();
            Check(merged.Length==4&&merged.All(p=>p.Stack.Count<=64)&&merged.GroupBy(p=>p.Stack.Id).All(g=>g.Sum(p=>p.Stack.Count)==100),"Two hundred drops consolidate into capacity-limited stacks per matching item type");
            Check(merged.All(p=>world.Address(p.Position.Local(world.Origin)+Vector3.up*.115f).Equals(cell)),"Different item types coexist on the same block after merging");
            Check(dense.Count(p=>p.View!=null)==4,"Merging removes 196 redundant item GameObjects and leaves four views");
            Check(merged.First(p=>p.Stack.Id==2).Id==dense[0].Id&&Math.Abs(merged.First(p=>p.Stack.Id==2).Age-200.5f)<.001f,"Stable pile identity survives and merging retains the oldest elapsed lifetime");
            Check(items.TotalSpawned==denseSpawned&&items.TotalExpired==denseExpired,"Merging changes no spawned or expired item totals");
            player.Camera.transform.position=at+new Vector3(.5f,1.3f,-2.3f);player.Camera.transform.LookAt(at+new Vector3(.5f,.2f,.5f));
            yield return Capture("dropped-items-mixed-stacks");
            items.Spawn(new ItemStack(2,450),at+new Vector3(.5f,.003f,.5f),Vector3.zero);
            foreach(var pile in items.Piles.Except(existing))pile.Sleeping=true;items.Step(.5f);
            var overflow=items.Piles.Except(existing).ToArray();
            Check(overflow.Where(p=>p.Stack.Id==2).Select(p=>p.Stack.Count).OrderBy(n=>n).SequenceEqual(new[]{38,64,64,64,64,64,64,64,64})&&overflow.Where(p=>p.Stack.Id==1).Sum(p=>p.Stack.Count)==100,"Merging respects the 64-item limit, leaves the exact remainder and never mixes identities");
            items.Spawn(new ItemStack(2,1),at+new Vector3(.5f,.003f,.5f),Vector3.zero,2);
            var delayed=items.Piles[items.Piles.Count-1];delayed.Sleeping=true;items.Step(.5f);
            Check(items.Piles.Contains(delayed)&&delayed.Stack.Count==1&&Math.Abs(delayed.Delay-1.5f)<.001f,"A newly thrown item retains its pickup delay before becoming merge eligible");
            for(int step=0;step<30;step++)items.Step(.05f);
            items.Step(.5f);
            Check(!items.Piles.Contains(delayed)&&items.Piles.Except(existing).Where(p=>p.Stack.Id==2).Sum(p=>p.Stack.Count)==551,"The delayed matching item merges once eligible without losing quantity");
            foreach(var p in items.Piles.Except(existing).ToArray()){if(p.View!=null)Destroy(p.View);items.Piles.Remove(p);}
            // Both items are within merge range around a solid corner, but the direct
            // route crosses terrain. Neither pile may be pulled through that corner.
            Set(cell,3);
            items.Spawn(new ItemStack(2,7),at+new Vector3(.6f,.003f,-.2f),Vector3.zero);
            items.Spawn(new ItemStack(2,9),at+new Vector3(1.2f,.003f,.4f),Vector3.zero);
            var corner=items.Piles.Except(existing).ToArray();foreach(var p in corner)p.Sleeping=true;
            Check(Vector3.Distance(corner[0].Position.Local(world.Origin),corner[1].Position.Local(world.Origin))<1&&corner.All(p=>!world.Overlaps(p.Position.Local(world.Origin),DroppedItems.CollisionWidth,DroppedItems.CollisionHeight)),"Corner fixture has clear item bounds within merge distance");
            items.Step(.5f);
            Check(corner.All(p=>items.Piles.Contains(p))&&corner.Sum(p=>p.Stack.Count)==16,"Nearby matching drops remain separate across a solid corner");
            foreach(var p in items.Piles.Except(existing).ToArray()){if(p.View!=null)Destroy(p.View);items.Piles.Remove(p);}
            foreach(var pair in changes){byte current=world.Get(pair.Key);if(current!=0&&current!=pair.Value)world.Remove(pair.Key,current);if(pair.Value!=0&&world.Get(pair.Key)==0)world.Place(pair.Key,pair.Value);}
            for(int slot=0;slot<60;slot++){game.Inventory.Take(slot,int.MaxValue);if(!carried[slot].Empty)game.Inventory.Add(carried[slot].Id,carried[slot].Count,slot,slot+1);}
            items.enabled=true;game.Selected=selected;player.transform.position=saved.Local(world.Origin);player.enabled=true;player.Pitch=10;
            yield return null;
        }
    }
}
