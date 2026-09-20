using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace RivetReach.Editor
{
    public static class PresentationViewCacheChecks
    {
        sealed class World:IIndustryWorld
        {
            public bool Ready(BlockPos p)=>true;public byte Get(BlockPos p)=>0;public bool Remove(BlockPos p,byte expected)=>false;
            public ItemContainer Storage(BlockPos p)=>null;public byte Drop(byte id)=>id;public bool PlayerInside(BlockPos p)=>false;
        }
        sealed class State
        {
            public int Position;
            public override bool Equals(object other)=>other is State state&&state.Position==Position;
            public override int GetHashCode()=>Position;
        }
        public static void Run()
        {
            var lines=new List<string>();
            void Check(bool condition,string message){if(!condition)throw new Exception("Presentation view cache: "+message);lines.Add("PASS "+message);}
            // Editor-only access keeps this implementation helper internal to the
            // player assembly. Gameplay discovery never uses reflection.
            var type=typeof(IndustryPresentation).Assembly.GetType("RivetReach.PresentationViewCache`2",true).MakeGenericType(typeof(State),typeof(object));
            var cache=Activator.CreateInstance(type,new object[]{2});
            MethodInfo Method(string name)=>type.GetMethod(name);
            object Call(string name,params object[] args)=>Method(name).Invoke(cache,args);
            int Count(string name)=>(int)type.GetProperty(name).GetValue(cache);
            long Total(string name)=>(long)type.GetProperty(name).GetValue(cache);
            bool Activate(State state,out object view){var args=new object[]{state,null};bool found=(bool)Method("TryActivate").Invoke(cache,args);view=args[1];return found;}
            var owner=new GameObject("Presentation cache verification");var roots=new List<GameObject>();
            GameObject Root(string name){var root=new GameObject(name);root.transform.SetParent(owner.transform,false);roots.Add(root);return root;}
            try
            {
                var first=new State{Position=1};var replacement=new State{Position=1};var root=Root("Original view");var view=new object();
                Call("Register",first,view,root);Call("Hide",first);
                Check(!root.activeSelf&&Count("CachedCount")==1,"Leaving view range hides the existing root immediately");
                Check(!Activate(replacement,out _),"Equal-position replacement state cannot acquire an older state's view");
                Check(Activate(first,out var reused)&&ReferenceEquals(reused,view)&&root.activeSelf&&Total("Created")==1&&Total("Reused")==1,
                    "Returning exact state reuses its same active view without another creation");
                Call("Hide",first);Call("Hide",first);
                Check(Count("CachedCount")==1,"Repeated hiding cannot duplicate a retained view");
                var secondRoot=Root("Replacement view");Call("Register",replacement,new object(),secondRoot);Call("Hide",replacement);
                var third=new State{Position=3};var thirdRoot=Root("Third view");Call("Register",third,new object(),thirdRoot);Call("Hide",third);
                Check(Count("CachedCount")==2&&Count("PendingDestroyCount")==1&&Count("PeakPendingDestroyCount")==1&&Total("Destroyed")==0,
                    "Overflow caps reusable roots and queues native destruction instead of performing it in the refresh burst");
                Check(!root.activeSelf&&!secondRoot.activeSelf&&!thirdRoot.activeSelf&&!Activate(first,out _),
                    "Evicted root stays hidden and cannot be reacquired from the reusable cache");
                Check(Activate(replacement,out _)&&secondRoot.activeSelf&&Count("CachedCount")==1,
                    "A replacement's own view remains independently reusable");
                Call("Clear");
                Check(Count("CachedCount")==0&&Count("PendingDestroyCount")==0&&!Activate(replacement,out _),"Session disposal clears every cache and retirement reference");
                UnityEngine.Object.DestroyImmediate(owner);
                Check(roots.TrueForAll(value=>value==null),"Disposing the owning hierarchy destroys active, cached and queued roots");
            }
            finally{if(owner!=null)UnityEngine.Object.DestroyImmediate(owner);}
            // Exercise the actual presentation pruning path while the topology
            // still publishes the old machine identity, including same-cell replacement.
            var presentationRoot=new GameObject("Pending topology view verification");
            try
            {
                var presentation=presentationRoot.AddComponent<IndustryPresentation>();presentation.enabled=false;
                var sim=new IndustrySimulation(new World(),_=>64);var position=new BlockPos(10,20,10);
                var old=sim.Add(position,IndustryId.PowerCable);for(int i=0;i<20&&sim.Rebuilding;i++)sim.Step();
                const BindingFlags fields=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
                var viewType=typeof(IndustryPresentation).GetNestedType("View",BindingFlags.NonPublic);
                var oldView=Activator.CreateInstance(viewType,true);var oldRoot=new GameObject("Removed machine view");oldRoot.transform.SetParent(presentationRoot.transform,false);
                viewType.GetField("Root",fields).SetValue(oldView,oldRoot);viewType.GetField("State",fields).SetValue(oldView,old);
                var views=(System.Collections.IDictionary)typeof(IndustryPresentation).GetField("views",fields).GetValue(presentation);views.Add(position,oldView);
                var actualCache=typeof(IndustryPresentation).GetField("cache",fields).GetValue(presentation);
                actualCache.GetType().GetMethod("Register").Invoke(actualCache,new[]{(object)old,oldView,oldRoot});
                sim.Remove(position);var replacement=sim.Add(position,IndustryId.PowerCable);
                sim.BeginFrame(1,double.PositiveInfinity);sim.Step();sim.EndFrame();
                Check(sim.Rebuilding&&System.Linq.Enumerable.Contains(sim.EligibleMachines,old),"Fixture retains the removed identity in an unfinished published topology snapshot");
                typeof(IndustryPresentation).GetMethod("PruneReplacedViews",fields).Invoke(presentation,new object[]{sim});
                Check(presentation.ViewAt(position)==null&&!oldRoot.activeSelf&&presentation.CachedViewCount==1,
                    "Same-cell replacement hides and detaches its predecessor before topology publication");
                var activateArgs=new object[]{replacement,null};
                Check(!(bool)actualCache.GetType().GetMethod("TryActivate").Invoke(actualCache,activateArgs),"Replacement cannot reactivate the removed machine's cached view");
                typeof(IndustryPresentation).GetMethod("PruneReplacedViews",fields).Invoke(presentation,new object[]{sim});
                Check(presentation.CachedViewCount==1,"Repeated pending-topology pruning does not duplicate retired identity ownership");
            }
            finally{UnityEngine.Object.DestroyImmediate(presentationRoot);}
            Directory.CreateDirectory("Logs/ReleaseReview");File.WriteAllLines("Logs/ReleaseReview/presentation-cache-checks.txt",new[]{"PASS "+lines.Count+" assertions"});
            File.AppendAllLines("Logs/ReleaseReview/presentation-cache-checks.txt",lines);
        }
    }
}
