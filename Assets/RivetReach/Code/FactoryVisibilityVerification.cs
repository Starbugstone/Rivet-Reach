using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        IEnumerator ReviewFactoryVisibility(Func<string,int,IEnumerator> sample,BlockPos origin,MachineState tank)
        {
            var player=game.Player;var original=player.transform.position;var rotation=player.Camera.transform.rotation;
            var far=FindAnyObjectByType<FactoryDistancePresentation>();var sim=game.Industry.Simulation;
            var positions=new[]{new Vector3(20,12,-28),new Vector3(20,24,-72),new Vector3(20,40,-152),new Vector3(20,128,34)};
            try
            {
                for(int i=0;i<positions.Length;i++)
                {
                    player.transform.position=game.World.Local(origin)+positions[i];player.Camera.transform.LookAt(game.World.Local(origin)+new Vector3(16,1,38));
                    yield return Settle(150);float deadline=Time.realtimeSinceStartup+60;
                    while(far.PendingCount>0&&Time.realtimeSinceStartup<deadline)yield return null;
                    Check(far.PendingCount==0,"Distant mesh queue drains at viewpoint "+i);
                    Check(far.InstanceCount>0&&far.DrawCount>0,"Distant factory geometry is submitted at viewpoint "+i);
                    Check(FindAnyObjectByType<MultiblockPresentation>().LiquidAt(tank.Structure)!=null,"Tank liquid remains rendered beyond the former controller cutoff at viewpoint "+i);
                    yield return sample("visibility-distant-"+i,600);
                    long tick=sim.Tick;long contents=tank.Structure.Fluid.Amount;
                    FactoryVisibility.LegacyReview=true;yield return new WaitForSecondsRealtime(.5f);
                    yield return sample("visibility-legacy-"+i,600);
                    Check(far.DrawCount==0,"Historical 64-block visibility control disables distant submissions");
                    Check(sim.Tick>tick&&tank.Structure.Fluid.Amount==contents,"Visibility control leaves live simulation and stored tank contents intact");
                    FactoryVisibility.LegacyReview=false;
                    if(i==1)
                    {
                        FactoryVisibility.FullDetailReview=true;yield return new WaitForSecondsRealtime(.5f);
                        yield return sample("visibility-full-detail-reference",600);FactoryVisibility.FullDetailReview=false;
                    }
                    yield return new WaitForSecondsRealtime(.5f);
                    var detail=FindAnyObjectByType<IndustryPresentation>();float retired=Time.realtimeSinceStartup+30;
                    while(detail.PendingDestroyCount>0&&Time.realtimeSinceStartup<retired)yield return null;
                    yield return sample("visibility-restored-"+i,600);
                }
                game.SetMode(ScreenMode.Pause);
                byte[] State(){using var stream=new MemoryStream();using(var writer=new SaveWriter(stream))sim.WriteSave(writer);return stream.ToArray();}
                byte[] before=State();FactoryVisibility.LegacyReview=true;yield return Settle(30);FactoryVisibility.LegacyReview=false;yield return Settle(30);
                Check(before.SequenceEqual(State()),"Presentation transitions preserve the entire paused industrial save payload byte-for-byte");
                game.SetMode(ScreenMode.Play);
                var probe=origin.Offset(0,3,-5);player.transform.position=game.World.Local(origin)+new Vector3(20,24,-72);
                Check(game.World.Get(probe)==0&&game.World.Place(probe,IndustryId.PowerCable),"Place distant mutation probe");
                yield return Settle(180);float prepared=Time.realtimeSinceStartup+30;while((far.PendingCount>0||!far.Represents(probe))&&Time.realtimeSinceStartup<prepared)yield return null;
                Check(far.Represents(probe),"New distant connector acquires its representation");
                Check(game.World.Remove(probe,IndustryId.PowerCable),"Remove distant mutation probe");yield return Settle(30);
                Check(!far.Represents(probe),"Removed distant connector leaves no phantom geometry");
                // Constant-speed travel through the transition in both directions.
                IEnumerator Travel()
                {
                    float start=Time.realtimeSinceStartup;
                    while(Time.realtimeSinceStartup-start<30)
                    {
                        float t=(Time.realtimeSinceStartup-start)/30;float distance=24+144*Mathf.Sin(t*Mathf.PI);
                        player.transform.position=game.World.Local(origin)+new Vector3(20,16,-distance);
                        player.Camera.transform.LookAt(game.World.Local(origin)+new Vector3(16,1,35));yield return null;
                    }
                }
                var travel=StartCoroutine(Travel());yield return sample("visibility-moving-transition",-30);yield return travel;
                Check(far.ReducedTriangles<far.SourceTriangles,"Derived distant geometry reduces total source triangles");
                Check(tank.Structure.Formed&&tank.Structure.Fluid.Amount>0,"Distant tank remains formed with exact authoritative storage");
                File.WriteAllText(Path.Combine(output,"visibility-geometry.txt"),$"Distant cached source triangles {far.SourceTriangles}; reduced triangles {far.ReducedTriangles}; peak cooperative mesh-build work {far.PeakBuildMilliseconds:F3} ms.\nDither transition {FactoryVisibility.FadeStart}–{FactoryVisibility.FadeEnd} blocks, detailed roots retained to {FactoryVisibility.DetailRange}, distant range follows terrain fog.\nComparisons are live factory runs with matched cameras, old cutoff controls and restoration; sequential thermal/production drift remains possible.\n");
            }
            finally{FactoryVisibility.LegacyReview=false;FactoryVisibility.FullDetailReview=false;player.transform.position=original;player.Camera.transform.rotation=rotation;}
        }
        IEnumerator CaptureFactoryVisibility(BlockPos origin)
        {
            var player=game.Player;var original=player.transform.position;var rotation=player.Camera.transform.rotation;
            var far=FindAnyObjectByType<FactoryDistancePresentation>();
            // Supplied capture-world liquid makes the level readable through the glass.
            // This separate visual run is excluded from conservation/timing comparisons.
            var captureTank=game.Industry.Simulation.At(origin.Offset(1,1,76)).Structure;
            long topUp=Math.Max(0,captureTank.Fluid.Capacity/2-captureTank.Fluid.Amount);
            Check(topUp==0||captureTank.Fluid.Deposit(Fluids.Water,topUp),"Prepare half-full visual demonstration tank");
            File.WriteAllText(Path.Combine(output,"visibility-capture-counts.csv"),"view,distant_instances,distant_draw_submissions,pending_meshes,cached_source_triangles,cached_distant_triangles,peak_mesh_build_ms\n");
            var shots=new[]{("visibility-close",new Vector3(28,10,24),new Vector3(12,0,35)),("visibility-midrange",new Vector3(20,24,-72),new Vector3(16,0,38)),("visibility-longrange",new Vector3(20,40,-152),new Vector3(16,0,38)),("visibility-elevated-tank",new Vector3(36,56,128),new Vector3(2,2,78)),("visibility-tank-detail",new Vector3(10,7,66),new Vector3(2,2,78))};
            foreach(var shot in shots)
            {
                player.transform.position=game.World.Local(origin)+shot.Item2;player.Camera.transform.LookAt(game.World.Local(origin)+shot.Item3);
                yield return Settle(150);while(far.PendingCount>0)yield return null;yield return Capture(shot.Item1);
                File.AppendAllText(Path.Combine(output,"visibility-capture-counts.csv"),FormattableString.Invariant($"{shot.Item1},{far.InstanceCount},{far.DrawCount},{far.PendingCount},{far.SourceTriangles},{far.ReducedTriangles},{far.PeakBuildMilliseconds:F3}\n"));
            }
            string frames=Path.Combine(output,"visibility-video-frames");Directory.CreateDirectory(frames);
            try
            {
                Time.captureFramerate=30;
                for(int frame=0;frame<720;frame++)
                {
                    float t=frame/719f;Vector3 eye,target;
                    if(t<.7f){float u=Mathf.SmoothStep(0,1,t/.7f);eye=Vector3.Lerp(new Vector3(28,10,24),new Vector3(20,40,-152),u);target=new Vector3(16,1,35);}
                    else{float u=Mathf.SmoothStep(0,1,(t-.7f)/.3f);eye=Vector3.Lerp(new Vector3(20,40,-152),new Vector3(36,56,128),u);target=Vector3.Lerp(new Vector3(16,1,35),new Vector3(2,2,78),u);}
                    player.transform.position=game.World.Local(origin)+eye;player.Camera.transform.LookAt(game.World.Local(origin)+target);
                    yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(frames,"factory-"+frame.ToString("D4")+".png"));
                }
            }
            finally{Time.captureFramerate=0;player.transform.position=original;player.Camera.transform.rotation=rotation;}
            File.WriteAllText(Path.Combine(output,"visibility-video.txt"),"720 actual Unity frames at fixed 30 Hz capture; 24 seconds. Frame capture is excluded from timing samples and is not a real-time FPS claim.\n");
        }
    }
}
