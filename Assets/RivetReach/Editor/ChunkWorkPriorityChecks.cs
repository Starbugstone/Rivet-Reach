using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class ChunkWorkPriorityChecks
    {
        public static void Run()
        {
            var report=new StringBuilder();int assertions=0;
            void Check(bool valid,string message)
            {if(!valid)throw new Exception("Chunk work priority: "+message);assertions++;report.AppendLine("PASS "+message);}
            var center=new ChunkPos(0,0,0);var forward=new ChunkWorkPriority(center,Vector3.forward);
            int Compare(ChunkWorkPriority priority,ChunkPos a,ChunkPos b)=>ChunkWorkPriority.Compare(priority.Rank(a),1,priority.Rank(b),2,false);
            var front=new ChunkPos(0,0,4);var rear=new ChunkPos(0,0,-4);
            Check(Compare(forward,front,rear)<0,"Equidistant forward work wins over rear work");
            Check(Compare(new ChunkWorkPriority(center,Vector3.back),rear,front)<0,"Turning the camera reverses directional preference");
            Check(Compare(new ChunkWorkPriority(center,Vector3.down),new ChunkPos(0,-4,0),new ChunkPos(0,4,0))<0,"Looking down prioritizes below-player work");
            Check(Compare(new ChunkWorkPriority(center,Vector3.up),new ChunkPos(0,4,0),new ChunkPos(0,-4,0))<0,"Looking up prioritizes above-player work");
            Check(Compare(forward,center,new ChunkPos(0,-1,0))<0,"The player's own page is the first normal-slot priority");
            var safetyCorner=new ChunkPos(1,-1,-1);var justAhead=new ChunkPos(0,0,2);
            Check(forward.Rank(safetyCorner).Safety&&!forward.Rank(justAhead).Safety&&Compare(forward,safetyCorner,justAhead)<0,
                "Every immediate neighboring page, including rear floor corners, precedes direction-biased distant work");
            Check(Compare(forward,new ChunkPos(0,0,-2),new ChunkPos(0,0,8))<0,"Nearby rear terrain still precedes much farther forward terrain");
            var negativeCenter=new ChunkPos(-1234567,-4,-7654321);var shifted=new ChunkWorkPriority(negativeCenter,Vector3.forward);
            Check(shifted.Rank(negativeCenter.Offset(0,0,4)).Score==forward.Rank(front).Score&&
                shifted.Rank(negativeCenter.Offset(1,-1,-1)).Safety,"Negative world coordinates preserve relative rank and safety boundaries");
            var normalized=new ChunkWorkPriority(center,Vector3.forward*17);
            Check(normalized.Rank(front).Score==forward.Rank(front).Score,"Cached forward vectors are normalized once per selection pass");
            var invalid=new ChunkWorkPriority(center,new Vector3(float.NaN,0,1));var zero=new ChunkWorkPriority(center,Vector3.zero);
            Check(invalid.Rank(front).Score==16&&zero.Rank(front).Score==16,"Invalid or absent camera direction falls back to weighted squared distance");
            Check(zero.Rank(new ChunkPos(0,4,0)).Distance==8,"Distance fallback preserves the existing half-weighted vertical metric");
            var left=forward.Rank(new ChunkPos(-3,0,0));var right=forward.Rank(new ChunkPos(3,0,0));
            Check(ChunkWorkPriority.Compare(left,5,right,6,false)<0&&ChunkWorkPriority.Compare(left,5,right,5,false)<0,
                "Equal geometric priorities use enqueue age then stable coordinate ordering");
            Check(!ChunkWorkPriority.IsOldestSlot(0)&&!ChunkWorkPriority.IsOldestSlot(1)&&!ChunkWorkPriority.IsOldestSlot(2)&&
                ChunkWorkPriority.IsOldestSlot(3)&&ChunkWorkPriority.IsOldestSlot(7),"Initial three dispatches protect safety and each fourth dispatch reserves oldest work");
            var loader=forward.Rank(new ChunkPos(10000,0,-10000));var player=forward.Rank(center);
            Check(ChunkWorkPriority.Compare(loader,1,player,100,false)>0&&ChunkWorkPriority.Compare(loader,1,player,100,true)<0,
                "A far loader page yields normal slots to player safety but wins its oldest reservation");
            {
                const int jobs=12;var background=new List<(ChunkWorkPriority.Candidate rank,long age)>();
                for(int i=0;i<jobs;i++)background.Add((forward.Rank(new ChunkPos(1000+i,0,-1000)),i+1));
                int dispatch=0;long sequence=100;
                while(background.Count>0&&dispatch<jobs*4)
                {
                    // Unlimited fresh safety edits must not starve finite older
                    // background work; a fresh enqueue receives a fresh age.
                    bool oldest=ChunkWorkPriority.IsOldestSlot(dispatch);int selected=-1;var best=player;long age=sequence++;
                    for(int i=0;i<background.Count;i++)
                        if(ChunkWorkPriority.Compare(background[i].rank,background[i].age,best,age,oldest)<0)
                        {selected=i;best=background[i].rank;age=background[i].age;}
                    if(selected>=0)background.RemoveAt(selected);dispatch++;
                }
                Check(background.Count==0&&dispatch==jobs*4,"Twelve older loader jobs finish within forty-eight dispatches despite continuous fresh nearby work");
            }
            {
                // Exercise real queue bookkeeping without starting workers or
                // introducing a player-facing fault injection API.
                var owner=new GameObject("Chunk queue lifecycle verification");var world=owner.AddComponent<VoxelWorld>();
                const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
                var residentType=typeof(VoxelWorld).GetNestedType("Resident",BindingFlags.NonPublic);
                var chunks=(IDictionary)typeof(VoxelWorld).GetField("chunks",flags).GetValue(world);
                var pending=(IDictionary)typeof(VoxelWorld).GetField("pendingMeshes",flags).GetValue(world);
                var queue=typeof(VoxelWorld).GetMethod("QueueMesh",flags);var fail=typeof(VoxelWorld).GetMethod("FailMesh",flags);
                var reject=typeof(VoxelWorld).GetMethod("RejectStaleMesh",flags);
                object Resident(int token){var value=Activator.CreateInstance(residentType,true);residentType.GetField("Token").SetValue(value,token);return value;}
                void Set(object resident,string field,object value)=>residentType.GetField(field).SetValue(resident,value);
                T Get<T>(object resident,string field)=>(T)residentType.GetField(field).GetValue(resident);
                void Queue(ChunkPos position,object resident,bool retry=false)=>queue.Invoke(world,new[]{(object)position,resident,retry});
                void Fail(ChunkPos position,int token,int revision)=>fail.Invoke(world,new object[]{position,token,revision,"injected failure"});
                try
                {
                    var first=Resident(1);var second=Resident(2);chunks[center]=first;chunks[front]=second;
                    Queue(center,first);Queue(front,second);long original=Get<long>(first,"PendingSequence");
                    Queue(center,first);Check(Get<long>(first,"PendingSequence")==original,"Repeated queued edits preserve current episode age before a dispatch completes");
                    Queue(center,first,true);Check(Get<long>(first,"PendingSequence")>Get<long>(second,"PendingSequence"),"Rejected worker retry moves behind already waiting background work");
                    Set(first,"Busy",true);Fail(center,1,0);
                    Check(!Get<bool>(first,"Busy")&&pending.Contains(center)&&world.Error.Contains("injected failure"),"Matching worker fault releases Busy, retains its reported error and queues one retry");
                    Set(first,"Busy",true);Fail(center,1,0);
                    Check(!Get<bool>(first,"Busy")&&!pending.Contains(center),"A second failure of unchanged data stops automatic retries without wedging the resident");
                    Set(first,"Revision",1);Queue(center,first);Set(first,"Busy",true);Fail(center,1,1);
                    Check(pending.Contains(center)&&Get<int>(first,"Failures")==1,"Fresh authoritative edits permit one new bounded retry");
                    pending.Remove(center);var replacement=Resident(3);Set(replacement,"Busy",true);chunks[center]=replacement;Queue(center,replacement);
                    long replacementAge=Get<long>(replacement,"PendingSequence");Fail(center,1,1);
                    Check(Get<bool>(replacement,"Busy")&&ReferenceEquals(pending[center],replacement)&&Get<long>(replacement,"PendingSequence")==replacementAge,
                        "Late fault from an unloaded identity cannot change a replacement worker or its queue entry");
                    Set(replacement,"Revision",2);Set(replacement,"Dirty",false);pending.Remove(center);Fail(center,3,1);
                    Check(!Get<bool>(replacement,"Busy")&&!pending.Contains(center)&&!Get<bool>(replacement,"Dirty"),"A late fault cannot dirty a newer synchronously published mesh");
                    int rejectedBefore=world.RejectedJobs;
                    reject.Invoke(world,new[]{(object)center,replacement});
                    Check(!pending.Contains(center)&&!Get<bool>(replacement,"Dirty")&&world.RejectedJobs==rejectedBefore+1,
                        "A stale successful result counts as rejected without requeuing a newer synchronously published mesh");
                    Set(replacement,"Revision",3);Set(replacement,"Dirty",true);Queue(center,replacement);long dirtyAge=Get<long>(replacement,"PendingSequence");
                    reject.Invoke(world,new[]{(object)center,replacement});
                    Check(ReferenceEquals(pending[center],replacement)&&Get<bool>(replacement,"Dirty")&&Get<int>(replacement,"Revision")==3&&
                        Get<long>(replacement,"PendingSequence")>dirtyAge&&world.RejectedJobs==rejectedBefore+2,
                        "A stale successful result preserves newer dirty data and retries it behind previously queued work");
                    pending.Remove(center);
                    chunks.Remove(center);Fail(center,3,2);Check(!pending.Contains(center),"Late fault for an unloaded page cannot recreate pending residency");
                }
                finally{UnityEngine.Object.DestroyImmediate(owner);}
            }
            using var allocationCounter=new AllocationCounter();report.AppendLine(allocationCounter.Description);
            double sum=0;
            void RankLoop(){for(int i=0;i<10000;i++){var a=forward.Rank(new ChunkPos(i%17-8,i%5-2,i%19-9));sum+=a.Score+ChunkWorkPriority.Compare(a,i,loader,1,ChunkWorkPriority.IsOldestSlot(i));}}
            RankLoop();long allocation=allocationCounter.Measure(RankLoop);
            Check(sum>0&&!double.IsInfinity(sum)&&!double.IsNaN(sum),"Repeated ranks remain finite across directions and vertical offsets");
            if(allocationCounter.Supported)Check(allocation==0,"Calibrated counter detects no allocation in ranking and comparison");
            else report.AppendLine("UNVERIFIED ranking allocation: no calibrated allocation counter.");
            Directory.CreateDirectory("Logs/ReleaseReview");
            File.WriteAllText("Logs/ReleaseReview/chunk-work-priority-checks.txt",$"PASS {assertions} assertions\n"+report);
        }
    }
}
