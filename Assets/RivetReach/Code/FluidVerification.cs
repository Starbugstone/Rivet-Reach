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
        IEnumerator ReviewFluids()
        {
            var world=game.World;var player=game.Player;var water=Fluids.Water;
            void HideSurveyAvatar(){player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);}
            game.Diagnostics=false;game.Mobs.enabled=false;player.enabled=false;
            player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);
            Check(game.Inventory.Slots.All(s=>s.Empty),"Ordinary session starts without buckets or water");
            foreach(var biome in new[]{BiomeId.Sea,BiomeId.River})
            {
                var site=TerrainReviewSites.Biome(world.Generator,biome);var riverDirection=Vector3.forward;
                if(biome==BiomeId.River)
                {
                    double nearest=double.MaxValue;
                    for(int z=-600;z<=600;z+=8)for(int x=-600;x<=600;x+=8)
                    {
                        var c=world.Generator.Column(x,z);if(c.Biome!=BiomeId.River)continue;
                        bool Dry(int dx,int dz)=>world.Generator.Column(x+dx,z+dz).WaterLevel==int.MinValue;
                        bool acrossX=Dry(-8,0)&&Dry(8,0),acrossZ=Dry(0,-8)&&Dry(0,8);
                        double score=x*x+z*z;if((!acrossX&&!acrossZ)||score>=nearest)continue;
                        nearest=score;site=new BlockPos(x,c.Height,z);riverDirection=acrossX?Vector3.forward:Vector3.right;
                    }
                }
                player.transform.position=world.Local(new BlockPos(site.X,TerrainProfile.SeaLevel+12,site.Z));
                yield return null;yield return Settle(120);
                var surface=new BlockPos(site.X,TerrainProfile.SeaLevel,site.Z);
                Check(world.Ready(surface)&&world.Get(surface)==water.Source,"Resident "+biome+" contains generated water sources");
                Check(!world.Solid(surface),"Generated "+biome+" is passable fluid");
                player.Camera.transform.position=world.Local(surface)+new Vector3(12,18,-24);
                player.Camera.transform.LookAt(world.Local(surface)+new Vector3(0,0,20));
                if(biome==BiomeId.River)
                {
                    player.Camera.transform.position=world.Local(surface)-riverDirection*18+Vector3.up*26;
                    player.Camera.transform.LookAt(world.Local(surface)+riverDirection*14);
                }
                sampling=true;yield return new WaitForSecondsRealtime(2);sampling=false;
                HideSurveyAvatar();yield return Capture("fluid-"+biome.ToString().ToLowerInvariant());
                if(biome==BiomeId.Sea)
                {
                    player.transform.position=world.Local(surface)+new Vector3(.5f,-1,.5f);player.ResetMotion();player.enabled=true;
                    float startY=player.transform.position.y;
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(game.Input.Keys["Jump"]));yield return new WaitForSeconds(.4f);
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;player.enabled=false;
                    Check(player.transform.position.y>startY+.3f,"Jump control swims upward through source water");
                    player.transform.position=world.Local(surface)+new Vector3(.5f,-1,.5f);player.ResetMotion();player.enabled=true;startY=player.transform.position.y;
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(game.Input.Keys["Crouch"]));yield return new WaitForSeconds(.4f);
                    InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());yield return null;player.enabled=false;
                    Check(player.transform.position.y<startY-.3f,"Crouch control swims downward through source water");
                    game.Items.enabled=false;
                    var floatItem=game.Registry.Get(BlockId.Stick);bool previousBuoyant=floatItem.buoyant;floatItem.buoyant=true;
                    var floatStart=world.Local(surface)+new Vector3(.5f,-.4f,.5f);
                    game.Items.Spawn(new ItemStack(BlockId.Stick,1),floatStart,Vector3.zero,30);var floating=game.Items.Piles.Last();
                    for(int step=0;step<100;step++)game.Items.Step(.05f);
                    floatItem.buoyant=previousBuoyant;game.Items.enabled=true;
                    Check(floating.Position.Local(world.Origin).y>floatStart.y+.5f&&floating.Sleeping&&floating.Stack.Count==1,"Buoyant item rises to the surface and sleeps without bouncing or losing quantity");
                }
            }
            var cell=new BlockPos(31,80,-4);
            player.transform.position=world.Local(cell)+new Vector3(.5f,2,-4);
            yield return null;yield return Settle(120);
            bool floor=true;
            for(int z=-9;z<=9;z++)for(int x=-9;x<=9;x++)floor&=world.Place(cell.Offset(x,-1,z),BlockId.Stone);
            Check(floor,"Ready elevated fluid test floor");
            Check(world.ChangeFluid(cell,0,water.Source),"Place source through fluid authority");
            yield return new WaitForSeconds(4);
            File.WriteAllText(Path.Combine(output,"flow-debug.txt"),"Pending="+world.FluidSimulation.Pending+" tickMs="+world.LastFluidTickMs+" row="+string.Join(",",Enumerable.Range(0,10).Select(x=>world.Get(cell.Offset(x,0,0)))));
            Check(world.Get(cell.Offset(7,0,0))==water.Flow(7)&&world.Get(cell.Offset(8,0,0))==0,"Runtime water crosses chunk seam and stops after seven cells");
            yield return Settle(90);
            player.Camera.transform.position=world.Local(cell)+new Vector3(10,10,-13);player.Camera.transform.LookAt(world.Local(cell));
            HideSurveyAvatar();yield return Capture("fluid-seven-cell-flow");
            game.Items.enabled=false;
            var driftStart=world.Local(cell)+new Vector3(2.5f,.12f,.5f);
            game.Items.Spawn(new ItemStack(BlockId.Cobblestone,1),driftStart,Vector3.zero,30);
            var drifting=game.Items.Piles.Last();
            for(int step=0;step<30;step++)game.Items.Step(.05f);
            Check(drifting.Position.Local(world.Origin).x>driftStart.x+.15f&&drifting.Stack.Count==1,"Current carries a sinking item along the bottom without losing quantity");
            game.Items.enabled=true;
            for(int slot=0;slot<game.Inventory.Count;slot++)game.Inventory.Add(BlockId.Stone,64,slot,slot+1);
            game.Inventory.Take(0,64);game.Inventory.Add(Fluids.EmptyBucket,1,0,1);game.Selected=0;
            player.transform.position=world.Local(cell)+new Vector3(.5f,1,-2);
            player.Camera.transform.position=world.Local(cell)+new Vector3(.5f,2,-2);
            player.Camera.transform.LookAt(world.Local(cell)+new Vector3(.5f,.7f,.5f));
            Check(world.Raycast(player.Camera.transform.position,player.Camera.transform.forward,5,out var hit,out byte id,out _,true)&&hit.Equals(cell)&&id==water.Source,"Bucket ray selects source through flowing water");
            Check(game.TryUseBucket()&&game.Inventory.Slots[0].Id==Fluids.WaterBucket&&world.Get(cell)==0,"Aimed bucket command collects source with full inventory");
            yield return new WaitForSeconds(5);yield return Settle(90);
            Check(world.Get(cell.Offset(6,0,0))==0,"Runtime source removal drains dependent flow");
            player.transform.position=world.Local(cell)+new Vector3(.5f,0,-2.5f);player.Yaw=0;player.Pitch=35;player.ResetMotion();player.enabled=true;
            yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState().WithButton(PlayerPrefs.GetInt("mineButton",0)==0?MouseButton.Right:MouseButton.Left));yield return null;yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState());yield return null;player.enabled=false;
            Check(game.Inventory.Slots[0].Id==Fluids.EmptyBucket,"Mouse Use empties a water bucket and returns its container");
            var source=cell.Offset(-5,0,4);
            bool rim=true;
            for(int z=-1;z<=2;z++)for(int x=-1;x<=2;x++)
                if(x<0||x>1||z<0||z>1)rim&=world.Place(source.Offset(x,0,z),BlockId.Stone);
            Check(rim,"Terrain placement displaces flowing water to enclose the renewal pool");
            for(int z=0;z<2;z++)for(int x=0;x<2;x++)
            {var p=source.Offset(x,0,z);byte old=world.Get(p);if(old!=0)world.ChangeFluid(p,old,0);}
            Check(world.ChangeFluid(source,0,water.Source)&&world.ChangeFluid(source.Offset(1,0,1),0,water.Source),"Place two diagonal pool sources");
            yield return new WaitForSeconds(2);
            Check(world.Get(source.Offset(1,0,0))==water.Source&&world.Get(source.Offset(0,0,1))==water.Source,"Two-source pool renews in the running player");
            player.Camera.transform.position=world.Local(source)+new Vector3(3,4,-4);player.Camera.transform.LookAt(world.Local(source)+Vector3.one*.5f);
            yield return Settle(90);HideSurveyAvatar();yield return Capture("fluid-renewable-pool");
            var point=world.Local(source)+new Vector3(.5f,.3f,.5f);
            Check(world.Submerged(point,out var definition,out _)&&definition==water,"Entity immersion queries fluid surface height");
            Check(!world.Submerged(world.Local(source)+new Vector3(.5f,.98f,.5f),out _,out _),"Air above a lowered source surface is dry");
            var preserved=source;var far=preserved.Offset(1600,0,1600);
            player.transform.position=world.Local(far);yield return null;yield return Settle(120);
            Check(!world.Ready(preserved)&&world.Get(preserved)==water.Source,"Source edits survive chunk unload");
            player.transform.position=world.Local(preserved)+Vector3.up*4;yield return null;yield return Settle(120);
            Check(world.Ready(preserved)&&world.Get(preserved)==water.Source,"Source returns after origin shift and reload");
            Check(string.IsNullOrEmpty(world.Error),"Fluid worker meshes publish without exceptions");
            File.WriteAllText(Path.Combine(output,"fluid-state.txt"),"Generator: "+TerrainGenerator.Version+"\nPending fluid cells: "+world.FluidSimulation.Pending+"\nLast fluid update ms: "+world.LastFluidTickMs+"\nWorld edits: "+world.EditCount+"\n");
        }
    }
}
