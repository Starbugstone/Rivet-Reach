using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Stopwatch=System.Diagnostics.Stopwatch;

namespace RivetReach
{
    public sealed class CratePresentation : MonoBehaviour
    {
        public const int InactiveViewCapacity=256,DestroyBudgetPerFrame=2,CreateBudgetPerFrame=4;
        public const double CreateBudgetMilliseconds=.5;
        sealed class View {public GameObject root;public StationState state;public RawImage icon;public Text text;public long revision=-1;public int rotation=-1;}
        struct Candidate {public BlockPos Position;public StationState State;public float Distance;}
        static readonly Comparison<Candidate> nearest=(a,b)=>a.Distance.CompareTo(b.Distance);
        Expedition game;VoxelWorld world;float nextRefresh;int pendingIndex;
        GameObject cratePrefab,controllerPrefab;Font labelFont;
        readonly PresentationViewCache<StationState,View> cache=new PresentationViewCache<StationState,View>(InactiveViewCapacity);
        readonly Dictionary<BlockPos,View> views=new Dictionary<BlockPos,View>();
        readonly HashSet<BlockPos> near=new HashSet<BlockPos>();readonly List<BlockPos> remove=new List<BlockPos>();
        readonly List<Candidate> pending=new List<Candidate>();
        public int ViewCount=>views.Count;
        public int CachedViewCount=>cache.CachedCount;
        public int PendingDestroyCount=>cache.PendingDestroyCount;
        public int PeakPendingDestroyCount=>cache.PeakPendingDestroyCount;
        public int PendingCreateCount=>pending.Count-pendingIndex;
        public int CreatedThisFrame {get;private set;}
        public int PeakCreatedPerFrame {get;private set;}
        public double LastCreateMilliseconds {get;private set;}
        public long CreatedViews=>cache.Created;
        public long ReusedViews=>cache.Reused;
        public long DestroyedViews=>cache.Destroyed;
        public GameObject ViewAt(BlockPos p)=>views.TryGetValue(p,out var view)?view.root:null;
        public void Initialize(Expedition game)
        {
            this.game=game;world=game.World;world.OriginShifted+=Shift;world.BlockChanged+=Changed;
            cratePrefab=Resources.Load<GameObject>("Industry/Runtime/bulk_crate");controllerPrefab=Resources.Load<GameObject>("Industry/Runtime/crate_controller");labelFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        void Changed(BlockPos p){if(CrateId.Part(world.Get(p))||views.ContainsKey(p))nextRefresh=0;}
        void Shift(Vector3 delta){foreach(var view in views.Values)view.root.transform.position-=delta;nextRefresh=0;}
        View Create(StationState state)
        {
            var root=Instantiate(state.Block==CrateId.Crate?cratePrefab:controllerPrefab,transform,false);
            var canvas=new GameObject("Contents label",typeof(RectTransform),typeof(Canvas));canvas.transform.SetParent(root.transform,false);
            var rect=(RectTransform)canvas.transform;rect.sizeDelta=new Vector2(256,192);rect.localPosition=new Vector3(.5f,.56f,.001f);rect.localScale=Vector3.one*.0018f;
            canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
            var icon=new GameObject("Stored item",typeof(RectTransform),typeof(RawImage));icon.transform.SetParent(rect,false);
            var ir=(RectTransform)icon.transform;ir.sizeDelta=new Vector2(112,112);ir.anchoredPosition=new Vector2(0,23);var image=icon.GetComponent<RawImage>();image.raycastTarget=false;
            var label=new GameObject("Count and lock",typeof(RectTransform),typeof(Text));label.transform.SetParent(rect,false);
            var tr=(RectTransform)label.transform;tr.sizeDelta=new Vector2(250,60);tr.anchoredPosition=new Vector2(0,-61);
            var text=label.GetComponent<Text>();text.font=labelFont;text.fontSize=27;text.alignment=TextAnchor.MiddleCenter;text.color=state.Block==CrateId.Crate?new Color(.07f,.09f,.08f):new Color(1,.84f,.43f);text.raycastTarget=false;
            return new View{root=root,state=state,icon=image,text=text};
        }
        void RefreshView(View view,BlockPos position)
        {
            var state=view.state;
            if(view.rotation!=state.Rotation)
            {
                var rotation=Quaternion.Euler(0,state.Rotation*90,0);view.root.transform.SetPositionAndRotation(world.Local(position)+Vector3.one*.5f-rotation*(Vector3.one*.5f),rotation);view.rotation=state.Rotation;
            }
            var endpoint=game.Crates.At(position);long revision=endpoint.Revision;if(view.revision==revision)return;view.revision=revision;
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
        void RefreshVisible()
        {
            nextRefresh=Time.unscaledTime+.25f;near.Clear();pending.Clear();pendingIndex=0;var player=game.Player.transform.position;
            foreach(var position in game.Crates.Nearby(world.Address(player)))
            {
                if(!world.Ready(position))continue;float distance=(world.Local(position)-player).sqrMagnitude;if(distance>64*64)continue;
                var state=game.Survival.At(position);if(state==null)continue;near.Add(position);
                if(views.TryGetValue(position,out var old)&&!ReferenceEquals(old.state,state)){cache.Hide(old.state);views.Remove(position);}
                if(!views.TryGetValue(position,out var view))
                {
                    if(cache.TryActivate(state,out view)){view.revision=-1;view.rotation=-1;views.Add(position,view);}
                    else{pending.Add(new Candidate{Position=position,State=state,Distance=distance});continue;}
                }
                RefreshView(view,position);
            }
            remove.Clear();foreach(var position in views.Keys)if(!near.Contains(position))remove.Add(position);
            foreach(var position in remove){cache.Hide(views[position].state);views.Remove(position);}
            if(pending.Count>1)pending.Sort(nearest);
        }
        void BuildPending()
        {
            CreatedThisFrame=0;LastCreateMilliseconds=0;if(pendingIndex>=pending.Count)return;long started=Stopwatch.GetTimestamp();int attempted=0;
            while(pendingIndex<pending.Count&&attempted<CreateBudgetPerFrame&&(attempted==0||(Stopwatch.GetTimestamp()-started)*1000.0/Stopwatch.Frequency<CreateBudgetMilliseconds))
            {
                var candidate=pending[pendingIndex++];attempted++;
                if(!world.Ready(candidate.Position)||!ReferenceEquals(game.Survival.At(candidate.Position),candidate.State)||(world.Local(candidate.Position)-game.Player.transform.position).sqrMagnitude>64*64)continue;
                var view=Create(candidate.State);cache.Register(candidate.State,view,view.root);views.Add(candidate.Position,view);RefreshView(view,candidate.Position);CreatedThisFrame++;
            }
            LastCreateMilliseconds=(Stopwatch.GetTimestamp()-started)*1000.0/Stopwatch.Frequency;if(CreatedThisFrame>PeakCreatedPerFrame)PeakCreatedPerFrame=CreatedThisFrame;
        }
        void Update()
        {
            cache.DestroyPending(DestroyBudgetPerFrame);if(game==null)return;
            if(Time.unscaledTime>=nextRefresh)RefreshVisible();BuildPending();
        }
        void OnDestroy(){if(world!=null){world.OriginShifted-=Shift;world.BlockChanged-=Changed;}cache.Clear();views.Clear();near.Clear();pending.Clear();remove.Clear();game=null;world=null;}
    }
}
