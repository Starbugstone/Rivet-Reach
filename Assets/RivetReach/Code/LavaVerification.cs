using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewLava()
        {
            var world=game.World;var player=game.Player;player.enabled=false;game.SetCreative(true);world.ViewDistance=4;game.Diagnostics=false;game.Mobs.enabled=false;
            void HideSurveyAvatar(){player.Arms.gameObject.SetActive(false);player.Body.gameObject.SetActive(false);}
            var g=world.Generator;BlockPos lake=default;bool found=false;int best=-1;
            for(int z=-192;z<=192&&best<25;z+=4)for(int x=-192;x<=192&&best<25;x+=4)
            {
                var p=new BlockPos(x,TerrainGenerator.LavaLevel,z);
                if(g.At(p)!=Fluids.Lava.Source||g.At(p.Offset(0,3,0))!=0)continue;
                int score=0;
                for(int dz=-4;dz<=4;dz+=2)for(int dx=-4;dx<=4;dx+=2)
                    if(g.At(p.Offset(dx,0,dz))==Fluids.Lava.Source&&g.At(p.Offset(dx,2,dz))==0)score++;
                if(score<=best)continue;best=score;lake=p;found=true;
            }
            Check(found,"Natural near-bedrock lava lake found with cave headroom");
            player.transform.position=world.Local(lake)+new Vector3(.5f,2,.5f);yield return null;yield return Settle(120);
            Check(world.Ready(lake)&&world.Get(lake)==Fluids.Lava.Source,"Native player renders generated lava lake");
            player.Camera.transform.position=world.Local(lake)+new Vector3(.5f,2.6f,.5f);player.Camera.transform.rotation=Quaternion.Euler(40,0,0);game.Notify("",0);
            HideSurveyAvatar();yield return Capture("lava-natural-lake");
            // A small isolated deck exposes bucket commands, surface contact and rendering clearly.
            var origin=new BlockPos(16,100,16);player.transform.position=world.Local(origin)+new Vector3(.5f,1,-3);yield return null;yield return Settle(120);
            bool deck=true;for(int z=-7;z<=7;z++)for(int x=-7;x<=7;x++)deck&=world.Place(origin.Offset(x,-1,z),BlockId.Stone);Check(deck,"Place hazard review deck");
            Check(world.ChangeFluid(origin,0,Fluids.Lava.Source),"Place lava through fluid authority");
            yield return new WaitForSeconds(3.6f);yield return Settle(90);
            Check(world.Get(origin.Offset(3,0,0))==Fluids.Lava.Flow(3)&&world.Get(origin.Offset(4,0,0))==0,"Runtime lava stops at three cells");
            player.Camera.transform.position=world.Local(origin)+new Vector3(5,5,-6);player.Camera.transform.LookAt(world.Local(origin)+Vector3.one*.5f);
            HideSurveyAvatar();yield return Capture("lava-flow");
            game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(Fluids.EmptyBucket,1,0,1);game.Selected=0;
            player.transform.position=world.Local(origin)+new Vector3(.5f,1,-2);player.Camera.transform.position=world.Local(origin)+new Vector3(.5f,2,-2);
            player.Camera.transform.LookAt(world.Local(origin)+new Vector3(.5f,.5f,.5f));
            Check(game.TryUseBucket()&&game.Inventory.Slots[0].Id==Fluids.LavaBucket&&world.Get(origin)==0,"Aimed bucket command collects lava source");
            player.Camera.transform.LookAt(world.Local(origin)+new Vector3(.5f,-.1f,.5f));
            Check(game.TryUseBucket()&&game.Inventory.Slots[0].Id==Fluids.EmptyBucket&&world.Get(origin)==Fluids.Lava.Source,"Aimed filled bucket places lava and returns empty bucket");
            game.Items.enabled=false;
            game.Items.Spawn(new ItemStack(BlockId.Diamond,3),world.Local(origin)+new Vector3(.5f,.05f,.5f),Vector3.zero,30);
            var doomed=game.Items.Piles.Last();doomed.Sleeping=true;int burned=game.Items.TotalBurned;game.Items.Step(.05f);
            Check(!game.Items.Piles.Contains(doomed)&&game.Items.TotalBurned==burned+3,"Lava destroys complete sleeping dropped stack before pickup, ignoring pickup delay");
            game.Items.Spawn(new ItemStack(BlockId.Stone,2),world.Local(origin)+new Vector3(.5f,.95f,.5f),Vector3.zero,30);
            var dry=game.Items.Piles.Last();dry.Sleeping=true;game.Items.Step(.05f);Check(game.Items.Piles.Contains(dry),"Item above lowered lava surface is not destroyed");
            Check(world.TouchesFluid(world.Local(origin)+new Vector3(1.25f,.05f,.5f),.6f,1.8f,Fluids.Lava),"Shallow edge contact is hazardous");
            player.transform.position=world.Local(origin)+new Vector3(.5f,.05f,.5f);game.SetCreative(false);player.enabled=false;
            float initial=game.Health.Hearts;game.AdvanceLava(1);
            Check(game.Health.Burning&&game.Health.Hearts==initial-4,"Lava sets actual player on fire and immediately removes two hearts");
            player.Camera.transform.position=player.transform.position+Vector3.up*1.6f;player.Camera.transform.rotation=Quaternion.Euler(35,0,0);
            yield return Capture("lava-burning");
            player.transform.position=world.Local(origin)+new Vector3(5.5f,.05f,.5f);game.AdvanceLava(20);
            Check(game.Health.Burning&&game.Health.Hearts<initial-4,"Actual player continues burning after leaving lava");
            var water=origin.Offset(5,0,0);Check(world.ChangeFluid(water,0,Fluids.Water.Source),"Place extinguishing water");game.AdvanceLava(1);
            Check(!game.Health.Burning,"Stepping into water extinguishes player");
            player.transform.position=world.Local(origin)+new Vector3(.5f,.05f,.5f);game.SetCreative(true);game.AdvanceLava(20);
            Check(!game.Health.Burning,"Creative player remains immune and unlit");
            game.SetCreative(false);game.Health.Respawn();game.AdvanceLava(1);
            game.Inventory.Take(0,int.MaxValue);game.Inventory.Add(Fluids.LavaBucket,1,0,1);
            game.SetMode(ScreenMode.Pause);game.InitializeSaves(Path.Combine(output,"saves"));
            Check(game.SaveGame("Lava verification",true),"Save lava world, bucket and burning player");
            int fire=game.Health.BurnTicks;float hearts=game.Health.Hearts;var entry=game.Saves.List().First(e=>e.Id==game.SaveId&&!e.Backup);
            Check(game.LoadGame(entry),"Reload lava checkpoint");game.Player.enabled=false;
            Check(game.Health.BurnTicks==fire&&game.Health.Hearts==hearts&&game.Inventory.Slots[0].Id==Fluids.LavaBucket&&game.World.Get(origin)==Fluids.Lava.Source,"Lava cells, bucket, hearts and remaining burn survive reload");
            Check(game.World.Generator.GenerationVersion==TerrainGenerator.Version,"Lava generator identity survives reload");
            game.SetCreative(true);yield return null;yield return Settle(120);game.Player.enabled=true;
            game.Player.transform.position=game.World.Local(origin)+new Vector3(.5f,1,-3);game.Player.Yaw=0;game.Player.Pitch=35;game.Player.ResetMotion();
            yield return new WaitForSecondsRealtime(.5f);yield return Capture("lava-bucket-held");game.Player.enabled=false;
            game.SetMode(ScreenMode.Inventory);yield return Capture("lava-bucket-inventory");game.SetMode(ScreenMode.Play);
            Check(string.IsNullOrEmpty(game.World.Error),"No terrain worker error during lava review");
            File.WriteAllText(Path.Combine(output,"lava-site.txt"),"Seed "+game.Seed+"; lake "+lake+"; generator "+TerrainGenerator.Version);
        }
    }
}
