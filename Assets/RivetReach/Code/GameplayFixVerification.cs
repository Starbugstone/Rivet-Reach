using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewGameplayFixes()
        {
            game.InitializeSaves(Path.Combine(output,"Saves"));
            var world=game.World;var player=game.Player;
            var origin=new BlockPos(0,world.Generator.Height(0,0)+1,0);
            Check(world.ChangeFluid(origin,BlockId.Air,Fluids.Water.Source),"Flood original spawn feet");
            var far=new BlockPos(800,world.Generator.Height(800,0)+1,0);
            player.ResetMotion();player.transform.position=world.Local(far)+new Vector3(.5f,.02f,.5f);
            yield return null;yield return Settle(90);
            Check(world.Origin.X!=0&&!world.Ready(origin),"Death location shifts render origin and unloads original spawn");
            Check(game.TakeDamage(1000)>0&&game.Health.Dead,"Remote lethal damage enters death screen");
            game.Respawn();var cell=world.Address(player.transform.position);
            Check(!game.Health.Dead&&(cell.X+.5)*(cell.X+.5)+(cell.Z+.5)*(cell.Z+.5)<=10000,"Remote death respawns within 100 blocks of world 0:0");
            Check((cell.X!=origin.X||cell.Z!=origin.Z)&&!Fluids.IsFluid(world.Get(cell))&&BlockId.Solid(world.Get(cell.Offset(0,-1,0))),"Respawn chooses a different surface column from saved water and has solid support");
            Check(game.WaitingForRespawn&&game.Paused&&Time.timeScale==0,"Respawn freezes simulation while destination chunks load");
            float until=Time.realtimeSinceStartup+90;
            while(game.WaitingForRespawn&&Time.realtimeSinceStartup<until)yield return null;
            Check(!game.WaitingForRespawn&&!game.Paused&&Time.timeScale==1,"Respawn resumes once destination terrain is ready");
            Check(game.TakeDamage(5)==0,"Respawn immunity remains after terrain preparation");
            yield return Settle(90);
            cell=world.Address(player.transform.position);
            Check(player.transform.position.y+world.Origin.Y>=world.Generator.Height(cell.X,cell.Z)+.98f,"Player remains on the surface after respawn terrain settles");
            foreach(var tier in new[]{ToolTier.Wood,ToolTier.Stone,ToolTier.Copper,ToolTier.Iron,ToolTier.Diamond})
            {
                var pick=new ItemStack((byte)(BlockId.WoodPickaxe+((int)tier-1)*5),1);
                float stone=game.Registry.MiningSeconds(BlockId.Stone,pick);
                foreach(var ore in game.Registry.items.Where(i=>BlockId.Ore(i.runtimeId)&&tier>=BlockId.RequiredTier(i.runtimeId)))
                    Check(game.Registry.MiningSeconds(ore.runtimeId,pick)>=stone*1.25f-.0001f,tier+" mines "+ore.displayName+" at least 25% slower than stone");
            }
            game.SetMode(ScreenMode.Pause);Check(game.SaveGame("Standalone quit regression",true),"Create isolated standalone checkpoint");
            var entry=game.Saves.List().Single();File.Copy(entry.Path,Path.Combine(output,"checkpoint-before.rrsave"));
            game.Inventory.Add(BlockId.Diamond,9);
            Check(game.UI.VisibleRoot.GetComponentsInChildren<Button>().Any(b=>b.interactable&&b.GetComponentInChildren<Text>().text=="QUIT WITHOUT SAVING"),"Standalone pause menu exposes Quit Without Saving");
            yield return Capture("respawn-and-quit");
            // Start writes the report, then invokes the actual quit button. The launcher
            // verifies process exit and compares the checkpoint with the copy above.
        }
    }
}
