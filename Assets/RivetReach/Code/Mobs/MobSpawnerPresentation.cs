using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RivetReach
{
    // Only resident nearby cages have views. The miniature is plain render geometry,
    // never a MobState/MobView, collider, or an entry in an entity population.
    public sealed class MobSpawnerPresentation : MonoBehaviour
    {
        sealed class View {public GameObject Root;public Transform Display;}
        readonly Dictionary<long,View> views=new Dictionary<long,View>();
        readonly HashSet<long> wanted=new HashSet<long>();
        readonly List<long> remove=new List<long>();
        MobSystem mobs;Expedition game;float nextRefresh;
        public void Initialize(MobSystem owner,Expedition expedition){mobs=owner;game=expedition;game.World.OriginShifted+=Shift;}
        void Shift(Vector3 offset){foreach(var view in views.Values)view.Root.transform.position-=offset;}
        void LateUpdate()
        {
            if(game==null||mobs==null)return;
            if(Time.unscaledTime>=nextRefresh)
            {
                nextRefresh=Time.unscaledTime+.25f;wanted.Clear();
                foreach(var state in mobs.ResidentSpawners)
                {
                    if(!game.World.Ready(state.Position)||(game.World.Local(state.Position)-game.Player.transform.position).sqrMagnitude>96*96)continue;
                    wanted.Add(state.Id);
                    if(!views.TryGetValue(state.Id,out var view))
                    {
                        var definition=MobSystem.SpawnerDefinition(state.Definition);
                        var species=System.Array.Find(mobs.Definitions,d=>d.stableId==definition.species);
                        var root=SpawnerVisuals.Create(transform,species);
                        var display=root.transform.Find("Species display (visual only)");
                        views.Add(state.Id,view=new View{Root=root,Display=display});
                    }
                    view.Root.transform.position=game.World.Local(state.Position)+new Vector3(.5f,0,.5f);
                }
                remove.Clear();foreach(var entry in views)if(!wanted.Contains(entry.Key))remove.Add(entry.Key);
                foreach(long id in remove){Destroy(views[id].Root);views.Remove(id);}
            }
            if(!game.Paused)foreach(var view in views.Values)view.Display.Rotate(0,Time.deltaTime*35,0);
        }
        void OnDestroy(){if(game!=null&&game.World!=null)game.World.OriginShifted-=Shift;foreach(var view in views.Values)if(view.Root!=null)Destroy(view.Root);views.Clear();}
    }
}
