// Review-only probe. Copy into an isolated checkout's Assets/RivetReach/Code.
// Never included in an ordinary shipped player; see README.md beside this file.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed class ReadinessSurvey : MonoBehaviour
    {
        [Serializable] public sealed class Survey { public string timestamp; public List<Sample> samples=new List<Sample>(); public List<string> errors=new List<string>(); }
        [Serializable] public sealed class Sample
        {
            public int seed,spawned,peak,animals,attackHits;public string phase,position;public float seconds;
            public List<string> species=new List<string>();public long[] rejections;
        }
        Expedition game;string output;Survey report=new Survey();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {if(Environment.GetCommandLineArgs().Contains("-rr-readiness-survey"))new GameObject("Readiness observer").AddComponent<ReadinessSurvey>();}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-rr-output");
            output=i>=0?args[i+1]:Path.Combine(Application.persistentDataPath,"ReadinessSurvey");Directory.CreateDirectory(output);
            Application.logMessageReceived+=OnLog;
            Application.runInBackground=true;yield return null;game=Expedition.Instance;
            yield return VerificationCoroutines.Run(Run(),e=>report.errors.Add(e.ToString()));
            report.timestamp=DateTime.UtcNow.ToString("O");File.WriteAllText(Path.Combine(output,"natural-spawns.json"),JsonUtility.ToJson(report,true));
            Application.Quit(report.errors.Count==0?0:1);
        }
        void OnLog(string message,string stack,LogType kind)
        {if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)report.errors.Add(message+"\n"+stack);}
        IEnumerator Settle()
        {
            float end=Time.realtimeSinceStartup+120;
            while((!game.ReadyToPlay||game.World.PendingCount>0)&&Time.realtimeSinceStartup<end)yield return null;
            if(!game.ReadyToPlay||game.World.PendingCount>0)throw new Exception("Survey streaming did not settle");
            yield return new WaitForSecondsRealtime(3);
        }
        IEnumerator Capture(string name)
        {yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));yield return new WaitForSecondsRealtime(.5f);}
        IEnumerator Run()
        {
            foreach(int seed in new[]{246813,777})foreach(string phase in new[]{"surface-day","surface-night","deep-cave-day"})
            {
                game.StartSession(seed);game.World.ViewDistance=4;game.SetCreative(true);game.Player.enabled=false;
                game.Player.Camera.transform.localPosition=Vector3.up*1.64f;game.Player.Body.transform.localPosition=new Vector3(0,0,-.34f);
                game.Player.Arms.FitFirstPersonFov(game.Player.Camera.fieldOfView);
                Debug.Log("Readiness phase "+seed+" "+phase);
                game.Mobs.NaturalSpawning=false;game.Animals.NaturalSpawning=false;
                var g=game.World.Generator;
                BlockPos site=phase.StartsWith("deep")?TerrainReviewSites.Cave(g):new BlockPos(96,g.Height(96,0)+1,0);
                // Find a dry, unobstructed surface observer outside the spawn sanctuary.
                if(!phase.StartsWith("deep"))
                {
                    bool found=false;
                    for(int z=-64;z<=64&&!found;z+=4)for(int x=64;x<=160&&!found;x+=4)
                    {int h=g.Height(x,z);var p=new BlockPos(x,h+1,z);if(g.At(p)==0&&g.At(p.Offset(0,1,0))==0&&g.At(p.Offset(0,-1,0))==BlockId.Grass){site=p;found=true;}}
                    if(!found)throw new Exception("No dry surface observer");
                }
                game.Player.transform.position=game.World.Local(site)+new Vector3(.5f,.01f,.5f);
                game.Player.transform.rotation=Quaternion.Euler(0,30,0);game.Player.Camera.transform.localRotation=Quaternion.Euler(8,0,0);
                game.Sky.Clock.SetTime(phase=="surface-night"?.9:.4);game.Sky.Apply();
                yield return Settle();game.Mobs.Clear();game.Animals.Clear();
                Array.Clear(game.Mobs.SpawnRejections,0,game.Mobs.SpawnRejections.Length);
                int before=game.Mobs.TotalSpawned,hits=game.Mobs.AttackHits;var seen=new Dictionary<long,string>();
                var sample=new Sample{seed=seed,phase=phase,position=site.ToString()};report.samples.Add(sample);
                var csv=new List<string>{"seconds,beetles,prowlers,floaters,cage_origin,chickens"};
                game.Mobs.NaturalSpawning=true;game.Animals.NaturalSpawning=true;
                float began=Time.realtimeSinceStartup,next=began;
                while(Time.realtimeSinceStartup-began<120)
                {
                    foreach(var mob in game.Mobs.Mobs)seen[mob.Id]=mob.Definition.stableId;
                    sample.peak=Math.Max(sample.peak,game.Mobs.Mobs.Count(m=>m.Alive));
                    if(Time.realtimeSinceStartup>=next)
                    {
                        next+=1;var alive=game.Mobs.Mobs.Where(m=>m.Alive).ToArray();
                        csv.Add((Time.realtimeSinceStartup-began).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+
                            string.Join(",",new[]{"rivet:rustback_beetle","rivet:dusk_prowler","rivet:floater"}.Select(id=>alive.Count(m=>m.Definition.stableId==id)))+","+alive.Count(m=>m.SpawnerId!=0)+","+game.Animals.Animals.Count);
                    }
                    yield return null;
                }
                sample.seconds=Time.realtimeSinceStartup-began;sample.spawned=game.Mobs.TotalSpawned-before;sample.animals=game.Animals.Animals.Count;
                sample.attackHits=game.Mobs.AttackHits-hits;sample.rejections=(long[])game.Mobs.SpawnRejections.Clone();
                sample.species=seen.Values.GroupBy(x=>x).Select(x=>x.Key+":"+x.Count()).ToList();
                File.WriteAllLines(Path.Combine(output,seed+"-"+phase+".csv"),csv);
                File.WriteAllText(Path.Combine(output,"natural-spawns.json"),JsonUtility.ToJson(report,true));
                yield return Capture(seed+"-"+phase);
            }
            game.Mobs.NaturalSpawning=false;game.Animals.NaturalSpawning=false;
            foreach(var biome in new[]{BiomeId.Forest,BiomeId.Desert,BiomeId.Badlands,BiomeId.Alpine,BiomeId.Sea})
            {
                var site=TerrainReviewSites.Biome(game.World.Generator,biome);
                game.Player.transform.position=game.World.Local(site)+new Vector3(.5f,biome==BiomeId.Sea?TerrainProfile.SeaLevel-site.Y+4:2,.5f);
                game.Player.transform.rotation=Quaternion.Euler(0,40,0);game.Player.Camera.transform.localRotation=Quaternion.Euler(12,0,0);
                game.Sky.Clock.SetTime(.4);game.Sky.Apply();yield return Settle();yield return Capture("scenery-777-"+biome);
            }
        }
    }
}

#if UNITY_EDITOR
namespace RivetReach.Editor
{
    [UnityEditor.InitializeOnLoad] public static class ReadinessSurveyEditor
    {
        [Serializable] public sealed class WorldSurvey {public List<WorldSample> worlds=new List<WorldSample>();}
        [Serializable] public sealed class WorldSample
        {
            public int seed,spawnY,trees,edibleHarvest,plants,maturePlants,rooms,buriedRooms;
            public float nearestTree;public List<string> food=new List<string>(),ores=new List<string>();public int[] biomes;
        }
        static ReadinessSurveyEditor(){UnityEditor.EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Logs/readiness-survey-request.txt";
            if(!File.Exists(request)||UnityEditor.EditorApplication.isCompiling||UnityEditor.EditorApplication.isUpdating)return;
            File.Delete(request);
            try
            {
                Scan();
                Type.GetType("RivetReach.Editor.ProjectBuild, Assembly-CSharp-Editor",true).GetMethod("Build",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)
                    .Invoke(null,new object[]{"ReadinessSurvey",UnityEditor.BuildOptions.None});
                File.WriteAllText("Logs/readiness-survey-result.txt","SUCCESS "+DateTime.UtcNow.ToString("O"));
            }
            catch(Exception e){File.WriteAllText("Logs/readiness-survey-result.txt",e.ToString());Debug.LogException(e);}
        }
        public static void Scan()
        {
            var report=new WorldSurvey();var registry=ItemRegistry.Load();
            foreach(int seed in new[]{246813,777,-917,int.MinValue,1,2,3,42,12345,65536,20261002,int.MaxValue})
            {
                var g=new TerrainGenerator(seed);var s=new WorldSample{seed=seed,spawnY=g.Height(0,0),biomes=new int[Enum.GetValues(typeof(BiomeId)).Length]};report.worlds.Add(s);
                var trees=g.Trees(-64,-64,64,64).Where(t=>Math.Abs(t.Root.X)<=64&&Math.Abs(t.Root.Z)<=64).ToArray();s.trees=trees.Length;
                s.nearestTree=trees.Length==0?-1:(float)trees.Min(t=>Math.Sqrt(t.Root.X*t.Root.X+t.Root.Z*t.Root.Z));
                var foods=new Dictionary<string,int>();
                for(int z=-64;z<=64;z++)for(int x=-64;x<=64;x++)
                {
                    byte b=g.At(new BlockPos(x,g.Height(x,z)+1,z));var crop=CropRules.For(b);if(crop==null)continue;
                    s.plants++;if(b==crop.Mature)s.maturePlants++;
                    foreach(var stack in CropRules.Harvest(b,TerrainGenerator.Hash(x,g.Height(x,z)+1,z,seed)))
                    {var item=registry.Get(stack.Id);if(item.foodPoints<=0)continue;s.edibleHarvest+=stack.Count;foods.TryGetValue(item.stableId,out int n);foods[item.stableId]=n+stack.Count;}
                }
                s.food=foods.OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value).ToList();
                foreach(var band in OreGenerator.Bands)
                {
                    int centres=0,exposed=0;double closest=double.MaxValue;BlockPos nearest=default;var surviving=new List<OreGenerator.Vein>();
                    foreach(var v in OreGenerator.Veins(seed,new BlockPos(-128,band.MinY,-128),new BlockPos(128,band.MaxY,128)))
                    {
                        var p=v.Centre;if(v.Band.Block!=band.Block||Math.Abs(p.X)>128||Math.Abs(p.Z)>128||g.At(p)!=band.Block)continue;
                        centres++;surviving.Add(v);double distance=Math.Sqrt(p.X*p.X+p.Z*p.Z+(double)(p.Y-s.spawnY)*(p.Y-s.spawnY));
                        if(distance<closest){closest=distance;nearest=p;}
                    }
                    var tested=surviving.OrderBy(v=>v.Centre.X*v.Centre.X+v.Centre.Z*v.Centre.Z+(double)(v.Centre.Y-s.spawnY)*(v.Centre.Y-s.spawnY)).Take(16).ToArray();
                    foreach(var v in tested)
                    {
                        var p=v.Centre;bool visible=false;
                        for(int z=-v.ZRadius;z<=v.ZRadius&&!visible;z++)for(int y=-v.YRadius;y<=v.YRadius&&!visible;y++)for(int x=-v.XRadius;x<=v.XRadius&&!visible;x++)
                        {var q=p.Offset(x,y,z);if(!v.Contains(q)||g.At(q)!=band.Block)continue;foreach(var d in new[]{(1,0,0),(-1,0,0),(0,1,0),(0,-1,0),(0,0,1),(0,0,-1)})if(g.At(q.Offset(d.Item1,d.Item2,d.Item3))==0){visible=true;break;}}
                        if(visible)exposed++;
                    }
                    s.ores.Add(registry.Get(band.Block).stableId+" centres="+centres+" exposed_of_nearest_"+tested.Length+"="+exposed+" nearest_centre="+nearest+" distance="+closest.ToString("F1",System.Globalization.CultureInfo.InvariantCulture));
                }
                for(int z=-768;z<=768;z+=16)for(int x=-768;x<=768;x+=16)s.biomes[(int)g.Biome(x,z)]++;
                for(int z=-2;z<=1;z++)for(int x=-2;x<=1;x++)if(SpawnerRooms.TryRoom(g,x,z,out var room)){s.rooms++;if(room.Buried)s.buriedRooms++;}
                File.WriteAllText("Logs/readiness-worlds.json",JsonUtility.ToJson(report,true));
            }
        }
    }
}
#endif
