using System;
using UnityEngine;

namespace RivetReach
{
    // Ranks work already admitted by world residency. Camera direction never
    // changes demand, simulation eligibility, loader tickets or draw distance.
    public readonly struct ChunkWorkPriority
    {
        public readonly struct Candidate
        {
            public readonly ChunkPos Position;
            public readonly bool Safety;
            public readonly double Distance,Score;
            internal Candidate(ChunkPos position,bool safety,double distance,double score)
            {Position=position;Safety=safety;Distance=distance;Score=score;}
        }
        readonly ChunkPos center;
        readonly double forwardX,forwardY,forwardZ;
        public ChunkWorkPriority(ChunkPos center,Vector3 forward)
        {
            this.center=center;
            double length=(double)forward.x*forward.x+(double)forward.y*forward.y+(double)forward.z*forward.z;
            if(length>0&&!double.IsInfinity(length)&&!double.IsNaN(length))
            {
                // Normalize once for the entire selection pass, never per page.
                double inverse=1/Math.Sqrt(length);
                forwardX=forward.x*inverse;forwardY=forward.y*inverse;forwardZ=forward.z*inverse;
            }
            else forwardX=forwardY=forwardZ=0;
        }
        public Candidate Rank(ChunkPos position)
        {
            double x=position.X-center.X,y=(long)position.Y-center.Y,z=position.Z-center.Z;
            double distance=x*x+z*z+y*y*.5;
            bool safety=x>=-1&&x<=1&&y>=-1&&y<=1&&z>=-1&&z<=1;
            double dot=x*forwardX+y*forwardY+z*forwardZ,score=distance;
            if(dot!=0)
            {
                // Squared alignment avoids per-candidate normalization/sqrt.
                // Distance still dominates: a far front page cannot outrank all
                // nearby side/rear pages merely because the camera faces it.
                double alignment=Math.Min(1,dot*dot/(x*x+y*y+z*z));
                score*=dot>0?1-.6*alignment:1+.25*alignment;
            }
            return new Candidate(position,safety,distance,score);
        }
        // Three safety/direction choices, then one oldest pending job. Age must
        // describe the current enqueue, not the resident object's creation.
        public static bool IsOldestSlot(long dispatchCount)=>(dispatchCount&3)==3;
        public static int Compare(Candidate a,long aSequence,Candidate b,long bSequence,bool oldestSlot)
        {
            int result;
            if(oldestSlot&&(result=aSequence.CompareTo(bSequence))!=0)return result;
            if(a.Safety!=b.Safety)return a.Safety?-1:1;
            if((result=a.Score.CompareTo(b.Score))!=0)return result;
            if((result=a.Distance.CompareTo(b.Distance))!=0)return result;
            if((result=aSequence.CompareTo(bSequence))!=0)return result;
            if((result=a.Position.X.CompareTo(b.Position.X))!=0)return result;
            if((result=a.Position.Y.CompareTo(b.Position.Y))!=0)return result;
            return a.Position.Z.CompareTo(b.Position.Z);
        }
    }
}
