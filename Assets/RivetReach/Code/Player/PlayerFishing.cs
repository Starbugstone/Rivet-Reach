using System;
using UnityEngine;

namespace RivetReach
{
    public sealed class PlayerFishing
    {
        public const float CastReach=8,TetherReach=12;
        public readonly FishingCast Cast=new FishingCast();
        readonly Expedition game;
        readonly Func<BlockPos,bool> resident;
        readonly Func<BlockPos,byte> get;
        int slot;
        public PlayerFishing(Expedition game)
        {this.game=game;resident=game.World.Ready;get=game.World.Get;}
        bool Selected=>game.Mode==ScreenMode.Play&&!game.Health.Dead&&!game.Player.Inspecting&&game.Inventory.Slots[game.Selected].Id==FishId.Rod;
        public Vector3 FloatPosition=>game.World.Local(Cast.Target)+new Vector3(.5f,Fluids.Water.Height(Fluids.Water.Source),.5f);
        public bool SiteValid()=>FishingWater.Suitable(Cast.Target,resident,get);
        bool TetherValid()
        {
            var eye=game.Player.Camera.transform.position;var direction=FloatPosition-eye;float length=direction.magnitude;
            return resident(game.World.Address(eye))&&!Fluids.IsFluid(get(game.World.Address(eye)))&&length<=TetherReach&&length>.05f&&game.World.Raycast(eye,direction/length,length+.15f,out var hit,out _,out _,true)&&hit.Equals(Cast.Target);
        }
        public bool Use()
        {
            if(!Selected)return false;
            if(Cast.Active)
            {
                bool valid=game.Selected==slot&&TetherValid()&&SiteValid();
                bool caught=Cast.Reel()&&valid;
                if(caught)
                {
                    int remainder=game.Inventory.Add(FishId.Raw,1);
                    if(remainder>0)game.Drop(new ItemStack(FishId.Raw,remainder));
                    game.Sound.Pickup();game.Notify(remainder>0?"Caught a fish — backpack full; pick it up nearby":"Caught a Raw Fish",3);game.WearSelectedTool();
                }
                else game.Notify("Line reeled in — no catch",2);
                return true;
            }
            var eye=game.Player.Camera.transform;
            if(!resident(game.World.Address(eye.position))||Fluids.IsFluid(get(game.World.Address(eye.position)))||
               !game.World.Raycast(eye.position,eye.forward,CastReach,out var hit,out byte id,out _,true)||id!=Fluids.Water.Source||!FishingWater.Suitable(hit,resident,get))
            {game.Notify("Aim at open water: at least 7×7 source blocks, two blocks deep",3);return false;}
            slot=game.Selected;Cast.Start(hit,UnityEngine.Random.Range(FishingCast.MinimumWait,FishingCast.MaximumWait+1));
            game.Notify("Line cast — wait for the float to dip, then Use again",4);return true;
        }
        public void Advance(int ticks)
        {
            if(!Cast.Active)return;
            if(!Selected||game.Selected!=slot||!TetherValid()||!SiteValid())
            {Cast.Cancel();game.Notify("Fishing cancelled",2);return;}
            var previous=Cast.Phase;Cast.Advance(ticks);
            if(Cast.Phase==FishingPhase.Bite&&previous!=FishingPhase.Bite){game.Sound.Pickup();game.Notify("Bite! Use now to reel in",3);}
            else if(!Cast.Active)game.Notify("The fish escaped — cast again",2);
        }
    }
    // Resolve the line after the held item has followed its hand socket this frame.
    [DefaultExecutionOrder(200)]
    public sealed class FishingPresentation : MonoBehaviour
    {
        Expedition game;GameObject bobber;LineRenderer line;Material material;
        public void Initialize(Expedition owner)
        {
            game=owner;bobber=Instantiate(Resources.Load<GameObject>("Fishing/Bobber"),transform,false);
            foreach(var r in bobber.GetComponentsInChildren<Renderer>())r.sharedMaterial=Resources.Load<Material>("Fishing/Fishing");
            var obj=new GameObject("Fishing line");obj.transform.SetParent(transform,false);line=obj.AddComponent<LineRenderer>();
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color(.9f,.82f,.6f));line.sharedMaterial=material;
            line.positionCount=6;line.startWidth=.012f;line.endWidth=.006f;line.useWorldSpace=true;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            bobber.SetActive(false);line.enabled=false;
        }
        void LateUpdate()
        {
            bool active=game.Fishing.Cast.Active&&game.Mode==ScreenMode.Play;
            bobber.SetActive(active);line.enabled=active;if(!active)return;
            bool bite=game.Fishing.Cast.Phase==FishingPhase.Bite;
            Vector3 end=game.Fishing.FloatPosition+Vector3.up*(bite?-.025f+Mathf.Sin(Time.time*22)*.015f:Mathf.Sin(Time.time*3)*.025f);
            bobber.transform.position=end;
            Vector3 start=game.Player.HeldBlock.FishingTip;
            for(int i=0;i<6;i++){float t=i/5f;line.SetPosition(i,Vector3.Lerp(start,end,t)-Vector3.up*(Mathf.Sin(t*Mathf.PI)*(bite?.03f:.18f)));}
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
