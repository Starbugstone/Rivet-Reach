using System;
using UnityEngine;

namespace RivetReach
{
    public sealed class PlayerNavigation
    {
        public BlockPos? LastDeath {get;private set;}
        public void RecordDeath(BlockPos position)=>LastDeath=position;
        public void ClearDeath()=>LastDeath=null;
        public void WriteSave(SaveWriter writer)
        {if(writer.Format<19)return;writer.Write(LastDeath.HasValue);if(LastDeath.HasValue)writer.Pos(LastDeath.Value);}
        public void ReadSave(SaveReader reader)
        {LastDeath=null;if(reader.Format>=19&&reader.ReadBoolean())LastDeath=reader.Pos();}
        public static string Bearing(BlockPos from,BlockPos to,float yaw)
        {
            // Subtract integer addresses before converting. Floating-origin shifts
            // and distant homes cannot move the marker or lose world precision.
            double dx=(double)(to.X-from.X),dz=(double)(to.Z-from.Z);
            double distance=Math.Sqrt(dx*dx+dz*dz);int dy=to.Y-from.Y;
            if(distance<2&&Math.Abs(dy)<3)return "Here";
            float angle=Mathf.DeltaAngle(yaw,(float)(Math.Atan2(dx,dz)*180/Math.PI));
            string direction=Math.Abs(angle)<=22.5?"Ahead":angle>22.5&&angle<157.5?"Right":angle< -22.5&&angle> -157.5?"Left":"Behind";
            string range=distance>=1000?(distance/1000).ToString("0.0")+" km":Math.Round(distance)+" m";
            return direction+" · "+range+(Math.Abs(dy)>=3?" · "+Math.Abs(dy)+" m "+(dy>0?"above":"below"):"");
        }
    }
}
