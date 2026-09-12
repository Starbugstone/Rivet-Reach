using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewFurnacePipes(BlockPos origin)
        {
            FreezeSaveFixture();game.SetCreative(true);game.enabled=false;
            var world=game.World;var sim=game.Industry.Simulation;var p=origin.Offset(0,0,6);
            void Put(BlockPos pos,byte id)
            {byte old=world.Get(pos);if(old!=0)Check(world.Remove(pos,old),"Clear furnace pipe fixture");if(id!=0)Check(world.Place(pos,id),"Place furnace pipe fixture "+id);}
            for(int x=-2;x<=5;x++)for(int y=0;y<=3;y++)Put(p.Offset(x,y,0),0);
            Put(p,IndustryId.Crusher);Put(p.Offset(1,0,0),IndustryId.ItemPipe);
            // Place the furnace after its pipe: station creation must invalidate topology.
            var fp=p.Offset(2,0,0);Put(fp,BlockId.Furnace);
            game.Survival.At(fp).Rotation=2;
            var input=sim.At(p.Offset(1,0,0));var crusher=sim.At(p);crusher.Items.Add(IndustryId.CrushedIron,4,2,3);
            void Mode(MachineState pipe,int face,PortRole role)
            {for(int i=0;i<3&&sim.PipeEndRole(pipe,face)!=role;i++)Check(sim.TogglePipeEnd(pipe,face),"Set furnace fixture pipe direction");}
            Mode(input,1,PortRole.Output);
            var fuelPipePos=fp.Offset(0,0,-1);Put(fuelPipePos,IndustryId.ItemPipe);Put(fp.Offset(0,0,-2),BlockId.Chest);
            var supply=game.Survival.At(fp.Offset(0,0,-2)).Storage;supply.Add(BlockId.Dirt,7);supply.Add(BlockId.Coal,2);
            Mode(sim.At(fuelPipePos),5,PortRole.Output);
            Put(fp.Offset(1,0,0),IndustryId.ItemPipe);Put(fp.Offset(2,0,0),BlockId.Chest);
            Mode(sim.At(fp.Offset(1,0,0)),1,PortRole.Output);
            for(int i=0;i<45;i++)sim.Step();
            var furnace=game.Survival.At(fp).Furnace;
            Check(furnace.Slots[0].Id==IndustryId.CrushedIron&&furnace.Slots[0].Count==4&&crusher.Items.Slots[2].Empty,"Actual crusher transfers all crushed iron into newly placed furnace");
            Check(furnace.Slots[1].Id==BlockId.Coal&&furnace.Slots[1].Count==2&&supply.Total(BlockId.Dirt)==7,"Actual fuel pipe skips dirt and fills only furnace fuel");
            Check(game.Survival.ActiveFurnaces>0,"Network insertion wakes the actual survival furnace scheduler");
            game.SetMode(ScreenMode.Play);game.Sky.Clock.SetTime(.35);game.Sky.Apply();
            game.Player.Camera.transform.position=world.Local(fp)+new Vector3(3.5f,3.5f,-5);
            game.Player.Camera.transform.LookAt(world.Local(fp)+Vector3.up*.6f);
            yield return new WaitForSecondsRealtime(.4f);
            var arrows=world.GetComponent<PipeEndpointPresentation>();
            Check(arrows.ViewAt(input.Position,0)!=null&&arrows.ViewAt(input.Position,1)!=null,"Actual crusher and furnace ends both render direction arrows");
            yield return Capture("crusher-furnace-pipes");
            game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"FurnacePipes"));Check(game.SaveGame("Furnace pipes"),"Save automated furnace with input and fuel");
            var save=game.Saves.List().First(e=>!e.Backup);Check(game.LoadGame(save),"Reload automated furnace");FreezeSaveFixture();game.enabled=false;
            world=game.World;sim=game.Industry.Simulation;furnace=game.Survival.At(fp).Furnace;
            Check(game.Survival.At(fp).Rotation==2&&game.Survival.ActiveFurnaces>0&&furnace.Slots[0].Count==4&&furnace.Slots[1].Count==2,"Loaded furnace preserves items and resumes its active scheduler");
            yield return Settle(120);
            Check(game.World.Ready(fp),"Loaded furnace chunk is resident before advancing work");
            game.Survival.AdvanceTicks(800);for(int i=0;i<30;i++)sim.Step();
            Check(game.Survival.At(fp.Offset(2,0,0)).Storage.Total(BlockId.IronIngot)==4&&furnace.Slots[0].Empty&&furnace.Slots[2].Empty,"Loaded crusher → furnace → chest chain produces exactly four ingots");
            Check(furnace.Slots[1].Count==1&&game.Survival.At(fp.Offset(0,0,-2)).Storage.Total(BlockId.Dirt)==7,"Output extraction leaves unburned fuel and rejected source items intact");
            // A cold, blocked furnace must wake when a pipe makes output space.
            var port=(IItemPipeInventory)furnace;
            for(int i=0;i<64;i++)Check(port.TryInsert(BlockId.RawIron),"Fill blocked-furnace input fixture");
            var fuel=new ItemStack(BlockId.Coal,8);furnace.Click(1,ref fuel,false);game.Survival.Wake(fp);game.Survival.AdvanceTicks(14400);
            Check(furnace.Slots[2].Count==64&&furnace.BurnTicks==0,"Furnace reaches full output with no burning fuel");
            Check(port.TryInsert(BlockId.RawIron),"Retain next ingredient behind blocked output");fuel=new ItemStack(BlockId.Coal,1);furnace.Click(1,ref fuel,false);game.Survival.Wake(fp);
            Check(!furnace.NeedsTick,"Full result prevents fuel ignition");
            for(int i=0;i<5;i++)sim.Step();
            Check(furnace.NeedsTick&&game.Survival.ActiveFurnaces>0,"Pipe result extraction wakes a cold output-blocked furnace");
            game.Survival.AdvanceTicks(200);Check(furnace.Slots[0].Empty,"Unblocked furnace resumes through the survival scheduler");
            Put(p,BlockId.Chest);Mode(sim.At(p.Offset(1,0,0)),1,PortRole.Output);
            var mixed=game.Survival.At(p).Storage;mixed.Add(BlockId.RawCopper,4);mixed.Add(BlockId.RawIron,4);
            for(int i=0;i<20;i++)sim.Step();
            Check(furnace.Slots[0].Id==BlockId.RawIron&&furnace.Slots[0].Count>0&&mixed.Total(BlockId.RawCopper)==4,"Actual receiver prefers iron ingredients matching its retained iron output");
            for(int slot=0;slot<3;slot++)furnace.Take(slot,int.MaxValue);
            for(int slot=0;slot<mixed.Count;slot++)mixed.Take(slot,int.MaxValue);
            var rearSupply=game.Survival.At(fp.Offset(0,0,-2)).Storage;
            for(int slot=0;slot<rearSupply.Count;slot++)rearSupply.Take(slot,int.MaxValue);
            mixed.Add(BlockId.Log,2);rearSupply.Add(BlockId.Log,2);
            for(int i=0;i<20;i++)sim.Step();
            Check(furnace.Slots[0].Id==BlockId.Log&&furnace.Slots[0].Count==2&&furnace.Slots[1].Id==BlockId.Log&&furnace.Slots[1].Count==2,"Loaded rotated furnace routes logs independently to ingredient and rear fuel slots");
            // Exhaust any previously ignited coal before measuring log-fueled work.
            furnace.Take(0,2);game.Survival.AdvanceTicks(2000);
            var ingredients=new ItemStack(BlockId.Log,2);furnace.Click(0,ref ingredients,false);game.Survival.Wake(fp);game.Survival.AdvanceTicks(200);
            Check(furnace.Slots[2].Id==BlockId.Charcoal&&furnace.Slots[1].Count==1,"Actual rear-fed log burns to make charcoal from the other log input");
            game.enabled=true;
        }
    }
}
