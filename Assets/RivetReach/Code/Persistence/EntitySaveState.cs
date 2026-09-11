using System;
using System.Linq;
using UnityEngine;

namespace RivetReach
{
    public sealed partial class HungerState
    {
        internal void WriteSave(SaveWriter w){w.Write(Food);w.Write(Exhaustion);}
        internal void ReadSave(SaveReader r){Food=r.Int(0,Maximum);Exhaustion=r.Number(0,4);}
    }
    public sealed partial class HealthState
    {
        internal void WriteSave(SaveWriter w){w.Write(Hearts);w.Write(foodTimer);w.Write(Regenerating);}
        internal void ReadSave(SaveReader r)
        {
            Hearts=r.Float(0,Maximum);foodTimer=r.Int(0,79);
            // Schema 1 predates the 60%/50% healing band; decide from food on its next tick.
            Regenerating=r.Format>=2&&r.ReadBoolean();
        }
    }
    public sealed partial class EquipmentState
    {
        internal void WriteSave(SaveWriter w)=>w.Slots(Slots);
        internal void ReadSave(SaveReader r)
        {r.Slots(contents);for(int i=0;i<4;i++)SaveReader.Require(Slots[i].Empty||Slots[i].Count==1&&(int)definition(Slots[i].Id).armorSlot==i+1,"Invalid saved armor slot.");}
    }
    public sealed partial class FirstPersonPlayer
    {
        internal void WriteSave(SaveWriter w)
        {
            w.Write(Yaw);w.Write(Pitch);w.Write(Female);w.Write(Skin);w.Write(Height);w.Write(vertical);w.Write(fallDistance);w.Write(Grounded);
        }
        internal void ReadSave(SaveReader r,WorldPoint position)
        {
            ResetMotion();transform.position=position.Local(Game.World.Origin);Yaw=r.Float();Pitch=r.Float(-85,85);Female=r.ReadBoolean();Skin=r.Int(0,1);
            Height=r.Float(1.25f,1.8f);vertical=r.Float();fallDistance=r.Float(0);Grounded=r.ReadBoolean();eyeHeight=Height-.16f;
            transform.rotation=Quaternion.Euler(0,Yaw,0);Camera.transform.localPosition=Vector3.up*eyeHeight;Camera.transform.localRotation=Quaternion.Euler(Pitch,0,0);
        }
    }
    public sealed partial class DroppedItems
    {
        internal void WriteSave(SaveWriter w)
        {
            w.Write(nextId);w.Write(accumulator);w.Write(mergeAt);w.Write(Piles.Count);
            foreach(var p in Piles){w.Write(p.Id);w.Stack(p.Stack);w.Point(p.Position);w.Vector(p.Velocity);w.Write(p.Age);w.Write(p.Delay);w.Write(p.ActionCreated);w.Write(p.EscapeRetry);w.Write(p.Sleeping);}
        }
        internal void ReadSave(SaveReader r)
        {
            nextId=r.Long(1);accumulator=r.Float(0);mergeAt=r.Float();int n=r.Count();var ids=new System.Collections.Generic.HashSet<long>();
            for(int i=0;i<n;i++)
            {
                var p=new Pile{Id=r.Long(1,nextId-1),Stack=r.Stack(),Position=r.Point(),Velocity=r.Vector(),Age=r.Float(0),Delay=r.Float(),ActionCreated=r.ReadBoolean(),EscapeRetry=r.Float(),Sleeping=r.ReadBoolean()};
                SaveReader.Require(!p.Stack.Empty&&ids.Add(p.Id),"Invalid dropped stack identity.");Piles.Add(p);
            }
        }
    }
    public sealed partial class MobSystem
    {
        internal void WriteSave(SaveWriter w)
        {
            w.Write(nextId);w.Write(accumulator);w.Write(spawnAt);w.Write(elapsed);w.Write(nextStrike);w.Write(nextPlayerHit);w.Write(graceUntil);w.Write(Mobs.Count);
            foreach(var m in Mobs)
            {
                w.Write(m.Id);w.Write(m.Definition.stableId);w.Point(m.Position);w.Point(m.Home);w.Write(m.Health);w.Write((int)m.Intent);
                w.Write(m.Timer);w.Write(m.Anger);w.Write(m.Yaw);w.Write(m.Vertical);w.Write(m.Unseen);w.Write(m.Grounded);w.Write(m.Climbing);w.Vector(m.WallNormal);w.Vector(m.ClimbDirection);w.Vector(m.Knockback);
            }
        }
        internal void ReadSave(SaveReader r)
        {
            nextId=r.Long(1);accumulator=r.Float(0);spawnAt=r.Float();elapsed=r.Float(0);nextStrike=r.Float();nextPlayerHit=r.Float();graceUntil=r.Float();
            int n=r.Count(MaximumPopulation);var ids=new System.Collections.Generic.HashSet<long>();
            for(int i=0;i<n;i++)
            {
                long id=r.Long(1,nextId-1);string stable=r.Text();var definition=Definitions.SingleOrDefault(d=>d.stableId==stable);
                SaveReader.Require(definition!=null&&ids.Add(id),"Unknown creature or duplicate identity.");
                var m=new MobState{Id=id,Definition=definition,Position=r.Point(),Home=r.Point(),Health=r.Int(0,definition.health),Intent=(MobIntent)r.Int(0,7),Timer=r.Float(),Anger=r.Float(),Yaw=r.Float(),Vertical=r.Float(),Unseen=r.Float(),Grounded=r.ReadBoolean(),Climbing=r.ReadBoolean(),WallNormal=r.Vector(),ClimbDirection=r.Vector(),Knockback=r.Vector(),PathAt=elapsed};
                SaveReader.Require(m.Alive==(m.Intent!=MobIntent.Dead),"Invalid saved creature health.");
                var root=new GameObject(definition.displayName+" #"+m.Id);root.transform.SetParent(transform,false);root.transform.position=m.Position.Local(game.World.Origin);
                m.View=root.AddComponent<MobView>();m.View.Initialize(m,material);Mobs.Add(m);
            }
            previousPlayer=game.Player.transform.position;
        }
    }
}
