using System.Collections.Generic;
using UnityEngine;

namespace RivetReach
{
    public sealed class BedPresentation : MonoBehaviour
    {
        Expedition game;VoxelWorld world;float nextRefresh;
        readonly Dictionary<BlockPos,GameObject> views=new Dictionary<BlockPos,GameObject>();
        readonly HashSet<BlockPos> nearby=new HashSet<BlockPos>();
        readonly List<BlockPos> remove=new List<BlockPos>();
        public void Initialize(Expedition game){this.game=game;world=game.World;world.OriginShifted+=Shift;world.BlockChanged+=Changed;}
        void Shift(Vector3 delta){foreach(var view in views.Values)view.transform.position-=delta;nextRefresh=0;}
        void Changed(BlockPos cell){nextRefresh=0;}
        public GameObject ViewAt(BlockPos foot)=>views.TryGetValue(foot,out var view)?view:null;
        void Update()
        {
            if(game==null||Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.25f;nearby.Clear();
            foreach(var bed in world.NearbyBeds(world.Address(game.Player.transform.position)))
            {
                if(!world.Ready(bed.Foot)||!world.Ready(bed.Head)||(world.Local(bed.Foot)-game.Player.transform.position).sqrMagnitude>64*64)continue;
                nearby.Add(bed.Foot);
                if(!views.TryGetValue(bed.Foot,out var view)){view=Instantiate(Resources.Load<GameObject>("Industry/Runtime/bed"),transform,false);view.name="Bed";views.Add(bed.Foot,view);}
                var rotation=Quaternion.Euler(0,bed.Rotation*90,0);
                view.transform.SetPositionAndRotation(world.Local(bed.Foot)+new Vector3(.5f,0,.5f)-rotation*new Vector3(.5f,0,.5f),rotation);
            }
            remove.Clear();foreach(var pair in views)if(!nearby.Contains(pair.Key))remove.Add(pair.Key);
            foreach(var p in remove){Destroy(views[p]);views.Remove(p);}
        }
        void OnDestroy(){if(world!=null){world.OriginShifted-=Shift;world.BlockChanged-=Changed;}}
    }
}
