using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    public static class StarterStationVisuals
    {
        public static bool UsesModel(byte id)=>id==BlockId.Workbench||id==BlockId.Furnace||id==BlockId.Chest;
        public static string Key(byte id)=>id==BlockId.Workbench?"workbench":id==BlockId.Furnace?"furnace":id==BlockId.Chest?"chest":null;
        public static GameObject Prefab(byte id)=>Resources.Load<GameObject>("Industry/Runtime/"+Key(id));
    }

    // Visuals follow existing station authority, including saved/reloaded state. They own no inventory or fuel.
    public sealed class StarterStationPresentation : MonoBehaviour
    {
        sealed class View { public GameObject Root; public StationState State; public Renderer Embers; public bool Burning; }
        Expedition game;float nextRefresh;
        readonly Dictionary<BlockPos,View> views=new Dictionary<BlockPos,View>();
        readonly HashSet<BlockPos> nearby=new HashSet<BlockPos>();
        readonly List<BlockPos> remove=new List<BlockPos>();
        MaterialPropertyBlock glow;
        public GameObject ViewAt(BlockPos position)=>views.TryGetValue(position,out var view)?view.Root:null;
        public void Initialize(Expedition expedition)
        {
            game=expedition;glow=new MaterialPropertyBlock();
            game.World.OriginShifted+=Shift;game.World.BlockChanged+=Changed;
        }
        void Changed(BlockPos position){nextRefresh=0;}
        void Shift(Vector3 delta){foreach(var view in views.Values)view.Root.transform.position-=delta;nextRefresh=0;}
        void Update()
        {
            if(game==null)return;
            if(Time.unscaledTime>=nextRefresh)
            {
                nextRefresh=Time.unscaledTime+.25f;nearby.Clear();
                foreach(var pair in game.Survival.Stations)
                {
                    if(!StarterStationVisuals.UsesModel(pair.Value.Block)||!game.World.Ready(pair.Key)||
                        (game.World.Local(pair.Key)-game.Player.transform.position).sqrMagnitude>64*64)continue;
                    nearby.Add(pair.Key);
                    if(views.TryGetValue(pair.Key,out var old)&&old.State!=pair.Value)
                    {Destroy(old.Root);views.Remove(pair.Key);}
                    if(!views.TryGetValue(pair.Key,out var view))
                    {
                        var prefab=StarterStationVisuals.Prefab(pair.Value.Block);if(prefab==null)continue;
                        var root=Instantiate(prefab,transform,false);root.name=game.Registry.Get(pair.Value.Block).displayName;
                        view=new View{Root=root,State=pair.Value};
                        foreach(var renderer in root.GetComponentsInChildren<Renderer>())if(renderer.name=="Embers")view.Embers=renderer;
                        views.Add(pair.Key,view);
                    }
                    view.Root.transform.position=game.World.Local(pair.Key);
                }
                remove.Clear();foreach(var pair in views)if(!nearby.Contains(pair.Key))remove.Add(pair.Key);
                foreach(var position in remove){Destroy(views[position].Root);views.Remove(position);}
            }
            foreach(var view in views.Values)
            {
                bool burning=view.State.Furnace!=null&&view.State.Furnace.BurnTicks>0;
                if(view.Embers==null||view.Burning==burning)continue;
                view.Burning=burning;glow.SetColor("_EmissionColor",burning?new Color(3.5f,.55f,.035f):Color.black);
                view.Embers.SetPropertyBlock(glow);
            }
        }
        void OnDestroy()
        {
            if(game!=null&&game.World!=null){game.World.OriginShifted-=Shift;game.World.BlockChanged-=Changed;}
        }
    }
}
