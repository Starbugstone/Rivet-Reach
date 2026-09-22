using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RivetReach
{
    public sealed partial class RuntimeVerification
    {
        // Diagnostic controls only: freeze authoritative work and transforms for a
        // matched camera, then restore every setting before ordinary live tests.
        IEnumerator ReviewFactoryIsolation(Func<string,int,IEnumerator> sample)
        {
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Check(pipeline!=null,"Graphics isolation uses the active URP asset");
            var machines=FindAnyObjectByType<IndustryPresentation>();
            var crates=FindAnyObjectByType<CratePresentation>();
            var behaviours=new Behaviour[]{game,game.World,game.Mobs,game.Animals,game.UI,machines,crates,
                game.World.GetComponent<WeatherPresentation>(),game.World.GetComponent<TorchPresentation>()};
            var enabled=Array.ConvertAll(behaviours,b=>b!=null&&b.enabled);
            int msaa=pipeline.msaaSampleCount;float scale=pipeline.renderScale;
            var sun=game.Sky.MainLight;var shadows=sun.shadows;
            var machineRenderers=new HashSet<Renderer>();var creatureRenderers=new HashSet<Renderer>();
            void Add(GameObject root,HashSet<Renderer> destination)
            {if(root!=null)foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))destination.Add(renderer);}
            foreach(var m in game.Industry.Simulation.Machines.Values)Add(machines.ViewAt(m.Position),machineRenderers);
            foreach(var m in game.Mobs.Mobs)if(m.View!=null)Add(m.View.gameObject,creatureRenderers);
            foreach(var a in game.Animals.Animals)if(a.View!=null)Add(a.View.gameObject,creatureRenderers);
            var original=machineRenderers.Concat(creatureRenderers).Distinct().ToDictionary(r=>r,r=>r.enabled);
            void Restore()
            {
                pipeline.msaaSampleCount=msaa;pipeline.renderScale=scale;sun.shadows=shadows;
                foreach(var pair in original)if(pair.Key!=null)pair.Key.enabled=pair.Value;
            }
            void Control(int index)
            {
                if(index==0)sun.shadows=LightShadows.None;
                if(index==1)pipeline.msaaSampleCount=1;
                if(index==2)pipeline.renderScale=.7f;
                if(index==3)foreach(var r in machineRenderers)r.enabled=false;
                if(index==4)foreach(var r in creatureRenderers)r.enabled=false;
            }
            var names=new[]{"sun-shadows-off","msaa-off","scale-70","machine-renderers-off","creature-renderers-off"};
            long tick=game.Industry.Simulation.Tick;double day=game.Sky.Clock.TotalDays;
            File.WriteAllText(Path.Combine(output,"graphics-isolation.txt"),
                "Diagnostic frozen-scene experiment, not ordinary gameplay FPS. Same camera/factory/world/clock; principal simulation and presentation updates suspended, while ambient effects and dropped-item updates remain active. " +
                "Each control has 300-frame baseline/control/baseline samples, with normal 90-frame warmups. Two rounds; second reverses control order. " +
                "Settings and enabled states restored before live unloading/travel. GPU timings remain delayed; inspect thermal telemetry. " +
                "Machine control disables current IndustryPresentation renderers including pipe views/shadows, but leaves crates, lights and terrain intact. " +
                "Creature control disables mesh renderers, not a gameplay population change. No quality setting is shipped differently.\n"+
                $"Original MSAA {msaa}; render scale {scale}; sun shadows {shadows}; machine renderers {machineRenderers.Count}; creature renderers {creatureRenderers.Count}; " +
                $"active shadowed non-directional lights {FindObjectsByType<Light>().Count(l=>l.enabled&&l.gameObject.activeInHierarchy&&l.type!=LightType.Directional&&l.shadows!=LightShadows.None)}.\n");
            var groups=game.Industry.Simulation.Power.Topology.Groups;
            File.AppendAllText(Path.Combine(output,"graphics-isolation.txt"),
                $"Power topology at freeze: {groups.Count} groups, {groups.Count(g=>g.Active)} active; " +
                $"{groups.Sum(g=>g.Devices.Count)} device endpoints; " +
                $"{groups.Sum(g=>g.Devices.Count(p=>p.Port.Role==PortRole.Input))} input, " +
                $"{groups.Sum(g=>g.Devices.Count(p=>p.Port.Role==PortRole.Output))} output, " +
                $"{groups.Sum(g=>g.Devices.Count(p=>p.Port.Role==PortRole.Storage))} storage endpoints.\n");
            int RoleMask(NetworkTopology.Group group)
            {
                int mask=0;foreach(var endpoint in group.Devices)
                    mask|=endpoint.Port.Role==PortRole.Input?1:endpoint.Port.Role==PortRole.Output?2:endpoint.Port.Role==PortRole.Storage?4:0;
                return mask;
            }
            File.AppendAllText(Path.Combine(output,"graphics-isolation.txt"),
                "Power group role masks (1=input, 2=output, 4=storage): " + string.Join(", ",groups.GroupBy(RoleMask).OrderBy(g=>g.Key).Select(g=>g.Key+":"+g.Count()))+".\n"+
                $"Groups with no possible source-to-sink role pairing: {groups.Count(g=>{int mask=RoleMask(g);return mask!=3&&mask!=5&&mask!=6&&mask!=7;})}.\n");
            if(Environment.GetCommandLineArgs().Contains("-rr-factory-cpu-only"))
            {
                File.AppendAllText(Path.Combine(output,"graphics-isolation.txt"),"CPU-only scenario: preceding description records available controls; no settings/renderers/components were changed and no graphics comparison was run. Live sample is 45 seconds.\n");
                yield break;
            }
            try
            {
                foreach(var b in behaviours)if(b!=null)b.enabled=false;
                yield return Capture("isolation-restored-before");
                for(int round=0;round<2;round++)
                for(int j=0;j<names.Length;j++)
                {
                    int index=round==0?j:names.Length-1-j;string prefix="gpu-r"+(round+1)+"-"+names[index];
                    Restore();yield return sample(prefix+"-before",300);
                    Control(index);yield return sample(prefix+"-control",300);
                    Restore();yield return sample(prefix+"-after",300);
                    Check(game.Industry.Simulation.Tick==tick&&game.Sky.Clock.TotalDays==day,"Frozen graphics comparison does not advance factory or clock: "+prefix);
                }
                yield return Capture("isolation-restored-after");
            }
            finally
            {
                Restore();for(int i=0;i<behaviours.Length;i++)if(behaviours[i]!=null)behaviours[i].enabled=enabled[i];
            }
            Check(pipeline.msaaSampleCount==msaa&&pipeline.renderScale==scale&&sun.shadows==shadows&&
                original.All(p=>p.Key!=null&&p.Key.enabled==p.Value),"Graphics controls restore exact quality and renderer state");
        }
    }
}
