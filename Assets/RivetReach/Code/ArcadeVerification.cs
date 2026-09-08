using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewArcade()
        {
            var world=game.World;var player=game.Player;var fx=ArcadePresentation.Active;
            Check(fx!=null,"Arcade presentation initializes in an ordinary expedition");
            var pools=fx.GetComponentsInChildren<ParticleSystem>();
            Check(pools.Length==6&&pools.Sum(p=>p.main.maxParticles)==ArcadePresentation.ParticleLimit,"Six fixed particle systems bound action effects to 728 particles");
            Check(pools.All(p=>p.main.simulationSpace==ParticleSystemSimulationSpace.World&&!p.collision.enabled),"Cosmetic particles use world space and no physics collision");
            var source=Resources.Load<GameObject>("Effects/ArcadeChips");
            Check(source!=null&&source.GetComponentsInChildren<MeshFilter>().Length==3,"The three original Blender debris meshes import");
            foreach(var mesh in source.GetComponentsInChildren<MeshFilter>())Check(mesh.sharedMesh.isReadable&&mesh.sharedMesh.triangles.Length/3<=128,"Debris is readable and below 128 triangles: "+mesh.name);
            var tiles=Resources.Load<Texture2DArray>("Materials/BlockTiles");var detail=Resources.Load<Texture2DArray>("Materials/BlockDetail");
            Check(detail!=null&&detail.width==64&&detail.depth==tiles.depth,"Terrain detail maps cover the complete current tile registry");
            foreach(string shader in new[]{"RivetReach/VoxelTerrain","RivetReach/ExpeditionSky","RivetReach/ArcadeParticle","RivetReach/ArcadeChip","RivetReach/ArcadeCracks","RivetReach/ArcadeGrass"})Check(Shader.Find(shader)!=null&&Shader.Find(shader).isSupported,"Shader imports with a supported pass: "+shader);
            var grass=fx.GetComponent<ArcadeGrass>();Check(grass!=null&&grass.TuftCount>0&&grass.TuftCount<=289,"Wind grass is bounded and rooted on exposed nearby terrain");
            report.arcadeGrassTufts=grass.TuftCount;report.arcadeGrassTriangles=grass.TuftCount*12;
            game.Diagnostics=false;player.Pitch=8;yield return Capture("arcade-landscape");
            player.Pitch=-28;yield return Capture("arcade-sky");
            int y=world.Generator.Height(8,-8)+1;
            void Put(BlockPos cell,byte id){byte was=world.Get(cell);if(was!=0)world.Remove(cell,was);if(id!=0)world.Place(cell,id);}
            for(int x=6;x<=10;x++)for(int z=-9;z<=-2;z++)
            {Put(new BlockPos(x,y-1,z),BlockId.Grass);for(int h=0;h<4;h++)Put(new BlockPos(x,y+h,z),0);}
            var target=new BlockPos(8,y+1,-5);Put(target,BlockId.Dirt);
            player.transform.position=world.Local(new BlockPos(8,y,-9))+new Vector3(.5f,.01f,.5f);player.Yaw=0;player.Pitch=2;
            game.Selected=0;game.Inventory.Take(0,int.MaxValue);game.SetAppearance(false,0);fx.SetIntensity(1);
            yield return new WaitForSecondsRealtime(.35f);
            Check(player.HasTarget&&player.Target.Equals(target),"The effects review aims at a real mineable voxel");
            yield return Capture("arcade-before-action");
            int spawned=game.Items.TotalSpawned,bursts=fx.BurstCount;
            bool sawCracks=false,sawParticles=false,placed=false,pickedUp=false;
            int priorCapture=Time.captureFramerate;Time.captureFramerate=60;
            try
            {
                for(int frame=0;frame<150;frame++)
                {
                    player.VerificationMining=frame<58||frame>=112;
                    if(frame==66)
                    {
                        Put(new BlockPos(8,y+1,-4),BlockId.Stone);game.Inventory.Add(BlockId.Dirt,3,0,1);
                        placed=game.TryPlaceSelected();
                    }
                    if(frame==88)
                    {
                        int beforePickup=game.Inventory.Total(BlockId.Log);
                        game.Items.Spawn(new ItemStack(BlockId.Log,1),player.transform.position+Vector3.forward+Vector3.up*.5f,Vector3.zero);
                        game.Items.Step(.05f);pickedUp=game.Inventory.Total(BlockId.Log)==beforePickup+1;
                    }
                    if(frame==102)game.Selected=10;
                    yield return null;yield return new WaitForEndOfFrame();
                    sawCracks|=fx.CracksVisible;sawParticles|=fx.ParticleCount>0;
                    report.arcadeParticlePeak=Math.Max(report.arcadeParticlePeak,fx.ParticleCount);
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"arcade-motion-"+frame.ToString("000")+".png"));
                }
            }
            finally{Time.captureFramerate=priorCapture;player.VerificationMining=false;}
            Check(sawCracks&&sawParticles,"Live mining produces surface cracks, shaped debris and action particles");
            Check(game.Items.TotalSpawned>spawned&&fx.BurstCount>bursts,"A committed block break emits debris and an authoritative item drop");
            Check(placed,"The placement pulse follows an accepted inventory transaction");
            Check(pickedUp,"Pickup glints follow actual collection into inventory");
            int edits=world.EditCount,items=game.Items.TotalSpawned;int objects=fx.GetComponentsInChildren<Transform>(true).Length;
            Vector3 effectAt=player.Camera.transform.position+player.Camera.transform.forward*2;
            for(int i=0;i<100;i++)fx.Impact(BlockId.Stone,effectAt,Vector3.back);
            report.arcadeParticlePeak=Math.Max(report.arcadeParticlePeak,fx.ParticleCount);
            // Check synchronously: the ordinary grass simulation may legitimately edit terrain next frame.
            Check(world.EditCount==edits&&game.Items.TotalSpawned==items,"Cosmetic impact bursts cannot edit terrain or mint item quantities");
            yield return null;
            Check(fx.ParticleCount<=ArcadePresentation.ParticleLimit&&fx.GetComponentsInChildren<Transform>(true).Length==objects,"Burst overload retains its particle and object bounds");
            yield return Capture("arcade-impact");
            fx.SetIntensity(0);int muted=fx.BurstCount;fx.Impact(BlockId.Stone,effectAt,Vector3.back);yield return null;
            Check(fx.ParticleCount==0&&fx.BurstCount==muted,"Zero effect intensity clears and suppresses optional action particles");
            fx.SetIntensity(1);game.SetMode(ScreenMode.Pause);float clock=fx.PresentationTime;yield return new WaitForSecondsRealtime(.2f);
            Check(Mathf.Approximately(clock,fx.PresentationTime),"Presentation animation clock freezes while paused");
            game.SetMode(ScreenMode.Settings);yield return Capture("arcade-settings");game.SetMode(ScreenMode.Play);
            // Particle cost sample excludes screenshot encoding and terrain fixture construction.
            sampling=true;float until=Time.realtimeSinceStartup+4,next=0;
            while(Time.realtimeSinceStartup<until)
            {
                if(Time.realtimeSinceStartup>next){next=Time.realtimeSinceStartup+.11f;fx.Impact(BlockId.Stone,effectAt,Vector3.back);}
                report.arcadeParticlePeak=Math.Max(report.arcadeParticlePeak,fx.ParticleCount);
                yield return null;
            }
            sampling=false;
            Check(fx.ParticleCount<=ArcadePresentation.ParticleLimit,"Sustained contact effects remain bounded during the measured workload");
            // Exercise the real floating-origin event while an identifiable particle is alive.
            fx.SetIntensity(0);fx.SetIntensity(1);fx.Impact(BlockId.Stone,effectAt,Vector3.back);yield return null;
            var pool=pools.Single(p=>p.name=="Block chips");var particles=new ParticleSystem.Particle[192];int count=pool.GetParticles(particles);
            Check(count>0,"Origin-shift fixture has active debris");var before=particles[0];var origin=world.Origin;
            player.enabled=false;player.transform.position+=Vector3.right*640;yield return null;yield return null;
            pool.GetParticles(particles);Vector3 shiftedBy=new Vector3(world.Origin.X-origin.X,world.Origin.Y-origin.Y,world.Origin.Z-origin.Z);
            Check(shiftedBy.sqrMagnitude>0&&(particles[0].position+shiftedBy-before.position).magnitude<.4f,"Active debris follows the real origin shift without a long spatial jump");
            player.enabled=true;
            Check(errors.Count==0,"Arcade review finishes without Unity errors");
        }
    }
}
