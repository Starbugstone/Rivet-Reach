using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RivetReach
{
    public sealed class CratePresentation : MonoBehaviour
    {
        sealed class View {public GameObject root;public StationState state;public RawImage icon;public Text text;public long revision=-1;}
        Expedition game;VoxelWorld world;float nextRefresh;
        readonly Dictionary<BlockPos,View> views=new Dictionary<BlockPos,View>();
        readonly HashSet<BlockPos> near=new HashSet<BlockPos>();readonly List<BlockPos> remove=new List<BlockPos>();
        public GameObject ViewAt(BlockPos p)=>views.TryGetValue(p,out var view)?view.root:null;
        public void Initialize(Expedition game){this.game=game;world=game.World;world.OriginShifted+=Shift;world.BlockChanged+=Changed;}
        void Changed(BlockPos p){if(CrateId.Part(world.Get(p))||views.ContainsKey(p))nextRefresh=0;}
        void Shift(Vector3 delta){foreach(var v in views.Values)v.root.transform.position-=delta;nextRefresh=0;}
        View Create(StationState state)
        {
            var root=Instantiate(Resources.Load<GameObject>("Industry/Runtime/"+(state.Block==CrateId.Crate?"bulk_crate":"crate_controller")),transform,false);
            var canvas=new GameObject("Contents label",typeof(RectTransform),typeof(Canvas));canvas.transform.SetParent(root.transform,false);
            var rect=(RectTransform)canvas.transform;rect.sizeDelta=new Vector2(256,192);rect.localPosition=new Vector3(.5f,.56f,.001f);rect.localScale=Vector3.one*.0018f;
            canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var icon=new GameObject("Stored item",typeof(RectTransform),typeof(RawImage));icon.transform.SetParent(rect,false);
            var ir=(RectTransform)icon.transform;ir.sizeDelta=new Vector2(112,112);ir.anchoredPosition=new Vector2(0,23);var image=icon.GetComponent<RawImage>();image.raycastTarget=false;
            var label=new GameObject("Count and lock",typeof(RectTransform),typeof(Text));label.transform.SetParent(rect,false);
            var tr=(RectTransform)label.transform;tr.sizeDelta=new Vector2(250,60);tr.anchoredPosition=new Vector2(0,-61);
            var text=label.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=27;text.alignment=TextAnchor.MiddleCenter;text.color=state.Block==CrateId.Crate?new Color(.07f,.09f,.08f):new Color(1,.84f,.43f);text.raycastTarget=false;
            return new View{root=root,state=state,icon=image,text=text};
        }
        void Update()
        {
            if(game==null||Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.25f;near.Clear();
            foreach(var p in game.Crates.Nearby(world.Address(game.Player.transform.position)))
            {
                if(!world.Ready(p)||(world.Local(p)-game.Player.transform.position).sqrMagnitude>64*64)continue;
                var state=game.Survival.At(p);if(state==null)continue;near.Add(p);
                if(views.TryGetValue(p,out var old)&&old.state!=state){Destroy(old.root);views.Remove(p);}
                if(!views.TryGetValue(p,out var view))views[p]=view=Create(state);
                var rotation=Quaternion.Euler(0,state.Rotation*90,0);view.root.transform.SetPositionAndRotation(world.Local(p)+Vector3.one*.5f-rotation*(Vector3.one*.5f),rotation);
                var endpoint=game.Crates.At(p);long revision=endpoint.Revision;if(view.revision==revision)continue;view.revision=revision;
                if(state.Crate is CrateStorage crate)
                {
                    view.icon.enabled=crate.Item!=0;if(crate.Item!=0)view.icon.texture=game.UI.ItemIcon(crate.Item);
                    view.text.text=crate.Item==0?"EMPTY":crate.Count.ToString("N0")+(crate.Locked?"  LOCK":"");
                }
                else
                {
                    view.icon.enabled=false;view.text.rectTransform.anchoredPosition=Vector2.zero;view.text.rectTransform.sizeDelta=new Vector2(250,170);
                    view.text.text="WAREHOUSE\n"+endpoint.Members.Count+" CRATES\n"+(endpoint.Status=="Ready"?"CONNECTED":"CHECK BANK");
                }
            }
            remove.Clear();foreach(var p in views.Keys)if(!near.Contains(p))remove.Add(p);foreach(var p in remove){Destroy(views[p].root);views.Remove(p);}
        }
        void OnDestroy(){if(world!=null){world.OriginShifted-=Shift;world.BlockChanged-=Changed;}}
    }
}
