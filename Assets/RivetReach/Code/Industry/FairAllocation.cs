using System;
using System.Collections.Generic;

namespace RivetReach
{
    // A phase-local, identity-keyed budget. Channels supply their own units and
    // commit rules; allocation never knows about items, fluid types or energy.
    public sealed class FairAllocation<T>
    {
        sealed class Share
        {
            public T Target;public long Remaining,Granted;
        }
        readonly List<Share> shares=new List<Share>(),lowest=new List<Share>();
        readonly Dictionary<T,Share> identities=new Dictionary<T,Share>();
        public void Clear(){shares.Clear();identities.Clear();}
        public void Add(T target,long capacity)
        {
            if(capacity<=0||identities.ContainsKey(target))return;
            var share=new Share{Target=target,Remaining=capacity};shares.Add(share);identities.Add(target,share);
        }
        // Redistribute capped/rejected shares. Retain grants across calls in the
        // same phase so several sources cannot all favour the first destination.
        public long Distribute(long budget,long tick,Func<T,long,long> transfer,Predicate<T> eligible=null)
        {
            long used=0;
            while(budget>0)
            {
                long level=long.MaxValue,next=long.MaxValue;lowest.Clear();
                foreach(var s in shares)
                {
                    if(s.Remaining==0||eligible!=null&&!eligible(s.Target))continue;
                    if(s.Granted<level){next=level;level=s.Granted;lowest.Clear();lowest.Add(s);}
                    else if(s.Granted==level)lowest.Add(s);
                    else next=Math.Min(next,s.Granted);
                }
                if(lowest.Count==0)break;
                long portion=Math.Max(1,budget/lowest.Count);
                if(next!=long.MaxValue)portion=Math.Min(portion,next-level);
                int start=(int)((tick%lowest.Count+lowest.Count)%lowest.Count);
                for(int n=0;n<lowest.Count&&budget>0;n++)
                {
                    var s=lowest[(start+n)%lowest.Count];long offer=Math.Min(budget,Math.Min(portion,s.Remaining));
                    long accepted=transfer(s.Target,offer);
                    if(accepted<0||accepted>offer)throw new InvalidOperationException("Invalid grid transfer");
                    s.Granted+=accepted;s.Remaining-=accepted;budget-=accepted;used+=accepted;
                    // Failed capacity/compatibility probes cannot spin forever.
                    if(accepted<offer)s.Remaining=0;
                }
            }
            return used;
        }
    }
}
