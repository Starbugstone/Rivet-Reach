using System.Collections.Generic;

namespace RivetReach
{
    public sealed partial class PassiveSystem
    {
        internal void WriteSave(SaveWriter w)
        {
            w.Write(nextId);w.Write(spawnRandom);w.Write(accumulator);w.Write(spawnTicks);w.Write(Animals.Count);
            foreach(var c in Animals)
            {
                w.Write(c.Id);w.Write("rivet:chicken");w.Point(c.Position);w.Point(c.Home);w.Write(c.Health);w.Write(c.GrowthTicks);w.Write(c.EggTicks);
                w.Write(c.CooldownTicks);w.Write(c.LoveTicks);w.Write(c.RandomState);w.Write(c.Yaw);w.Write(c.Vertical);w.Write(c.Grounded);
            }
        }
        internal void ReadSave(SaveReader r)
        {
            if(r.Format<12)return;
            nextId=r.Long(1);spawnRandom=r.ReadUInt32();SaveReader.Require(spawnRandom!=0,"Invalid passive spawn sequence.");
            accumulator=r.Float(0,.25f);spawnTicks=r.Int(0,200);int n=r.Count(MaximumPopulation);var ids=new HashSet<long>();
            for(int i=0;i<n;i++)
            {
                long id=r.Long(1,nextId-1);SaveReader.Require(ids.Add(id)&&r.Text()=="rivet:chicken","Unknown passive species or duplicate identity.");
                var c=new ChickenState{Id=id,Position=r.Point(),Home=r.Point(),Health=r.Int(1,Catalog.health),GrowthTicks=r.Int(0,Catalog.growthTicks),EggTicks=r.Int(0,Catalog.eggMaximumTicks),CooldownTicks=r.Int(0,Catalog.breedCooldownTicks),LoveTicks=r.Int(0,Catalog.loveTicks),RandomState=r.ReadUInt32(),Yaw=r.Float(-360,360),Vertical=r.Float(-25,8),Grounded=r.ReadBoolean()};
                SaveReader.Require(c.RandomState!=0&&(c.Adult||c.Health<=Catalog.chickHealth&&c.CooldownTicks==0&&c.LoveTicks==0)&&(c.CooldownTicks==0||c.LoveTicks==0),"Invalid passive lifecycle state.");
                Animals.Add(c);Register(c);
            }
            refreshTicks=0;
        }
    }
}
