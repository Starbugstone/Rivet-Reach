using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RivetReach
{
    public sealed partial class IndustrySimulation
    {
        sealed class AutomationComponent
        {
            public readonly NetworkActivity Activity=new NetworkActivity();
            public readonly HashSet<MachineState> Members=new HashSet<MachineState>();
            public readonly HashSet<BlockPos> Nodes=new HashSet<BlockPos>();
            public readonly HashSet<ChunkPos> Pages=new HashSet<ChunkPos>();
        }
        sealed class DependencyNode
        {
            public readonly BlockPos Position;
            public DependencyNode Parent;public int Rank;
            public DependencyNode(BlockPos position){Position=position;Parent=this;}
            public DependencyNode Root()
            {var root=this;while(root.Parent!=root)root=root.Parent;var node=this;while(node.Parent!=node){var next=node.Parent;node.Parent=root;node=next;}return root;}
            public void Join(DependencyNode other)
            {var a=Root();var b=other.Root();if(a==b)return;if(a.Rank<b.Rank)a.Parent=b;else{b.Parent=a;if(a.Rank==b.Rank)a.Rank++;}}
        }
        HashSet<AutomationComponent> components=new HashSet<AutomationComponent>();
        readonly HashSet<AutomationComponent> pendingComponents=new HashSet<AutomationComponent>();
        readonly HashSet<AutomationComponent> rebuildingComponents=new HashSet<AutomationComponent>();
        Dictionary<BlockPos,AutomationComponent> componentAt=new Dictionary<BlockPos,AutomationComponent>();
        Dictionary<ChunkPos,HashSet<AutomationComponent>> componentPages=new Dictionary<ChunkPos,HashSet<AutomationComponent>>();
        bool frameBudgetManaged,structureValidationAvailable;int reconstructionRemaining;long reconstructionDeadline;
        public int ReconstructionStepsThisFrame {get;private set;}
        public int SuspendedComponents=>pendingComponents.Count;
        public void BeginFrame(int operations=2048,double milliseconds=2)
        {
            if(operations<1||double.IsNaN(milliseconds)||milliseconds<=0)throw new ArgumentOutOfRangeException();
            frameBudgetManaged=true;structureValidationAvailable=true;reconstructionRemaining=operations;ReconstructionStepsThisFrame=0;
            reconstructionDeadline=double.IsPositiveInfinity(milliseconds)?long.MaxValue:Stopwatch.GetTimestamp()+(long)(milliseconds*Stopwatch.Frequency/1000);
        }
        public void EndFrame()=>frameBudgetManaged=false;
        void AdvanceStructureValidation()
        {if(!frameBudgetManaged||structureValidationAvailable){Multiblocks.Step();structureValidationAvailable=false;}}
        bool CanSimulate(MachineState machine)=>machine.Eligible&&componentAt.TryGetValue(machine.Position,out var component)&&component.Activity.Active;
        public bool IsSimulating(MachineState machine)=>machine!=null&&At(machine.Position)==machine&&CanSimulate(machine);
        void Suspend(AutomationComponent component)
        {component.Activity.Active=false;pendingComponents.Add(component);}
        void RegisterComponent(MachineState machine)
        {
            // Registration mutates the component/index collections used by yielded
            // publication preparation. Conservatively restart for new anchors.
            if(rebuild!=null)dirty=true;
            if(!componentAt.TryGetValue(machine.Position,out var component))
            {component=new AutomationComponent();components.Add(component);componentAt.Add(machine.Position,component);component.Nodes.Add(machine.Position);WatchComponent(component,machine.Position);}
            component.Members.Add(machine);
        }
        void WatchComponent(AutomationComponent component,BlockPos position)
        {
            void Page(ChunkPos page)
            {if(!component.Pages.Add(page))return;if(!componentPages.TryGetValue(page,out var set))componentPages[page]=set=new HashSet<AutomationComponent>();set.Add(component);}
            Page(position.Chunk);for(int face=0;face<6;face++)Page(IndustryDefinition.Neighbor(position,face).Chunk);
        }
        public void Invalidate(BlockPos position)
        {
            bool found=false;
            void Touch(BlockPos p)
            {
                if(!componentAt.TryGetValue(p,out var component))return;
                Suspend(component);found=true;
                if(rebuild==null||rebuildingComponents.Contains(component))dirty=true;
            }
            Touch(position);for(int face=0;face<6;face++)Touch(IndustryDefinition.Neighbor(position,face));
            if(found){signalsDirty=true;Revision++;}
        }
        void InvalidateAllComponents()
        {foreach(var component in components)Suspend(component);dirty=true;signalsDirty=true;Revision++;}
        void InvalidateComponentPage(ChunkPos page)
        {
            if(!componentPages.TryGetValue(page,out var local))return;
            foreach(var component in local)
            {
                Suspend(component);
                if(rebuild==null||rebuildingComponents.Contains(component))dirty=true;
            }
            signalsDirty=true;Revision++;
        }
        bool DependencyAt(BlockPos position)=>At(position)!=null||(world is IIndustryItemEndpoints endpoints?endpoints.ItemEndpoint(position)!=null:world.Storage(position)!=null);
        void AdvanceReconstruction()
        {
            if(!frameBudgetManaged){reconstructionRemaining=2048;reconstructionDeadline=long.MaxValue;ReconstructionStepsThisFrame=0;}
            if(dirty)
            {
                rebuild?.Dispose();rebuildingComponents.Clear();rebuildingComponents.UnionWith(pendingComponents);
                // A later disjoint request must not mutate an enumerator's seeds
                // or discard the work already performed for this publication.
                var seeds=new List<AutomationComponent>(rebuildingComponents);
                dirty=false;rebuild=Reconstruct(seeds).GetEnumerator();TopologyRebuilds++;
            }
            while(rebuild!=null&&reconstructionRemaining>0&&Stopwatch.GetTimestamp()<reconstructionDeadline)
            {
                reconstructionRemaining--;ReconstructionStepsThisFrame++;
                if(rebuild.MoveNext())continue;
                rebuild.Dispose();rebuild=null;rebuildingComponents.Clear();
            }
        }
        IEnumerable<int> Reconstruct(IReadOnlyList<AutomationComponent> seeds)
        {
            var replaced=new HashSet<AutomationComponent>();var oldActivities=new HashSet<NetworkActivity>();
            var affected=new HashSet<BlockPos>();var affectedMachines=new HashSet<MachineState>();
            var nodes=new Dictionary<BlockPos,DependencyNode>();var search=new Queue<DependencyNode>();
            var collecting=new Queue<AutomationComponent>();
            var named=new Dictionary<(string,byte,string),DependencyNode>();
            var bridgePairs=new Dictionary<(string,byte,string),List<MachineState>>();
            DependencyNode Node(BlockPos position)
            {if(!nodes.TryGetValue(position,out var node)){node=new DependencyNode(position);nodes.Add(position,node);search.Enqueue(node);}return node;}
            void Include(AutomationComponent component)
            {
                if(!replaced.Add(component))return;Suspend(component);rebuildingComponents.Add(component);oldActivities.Add(component.Activity);
                collecting.Enqueue(component);
            }
            foreach(var component in seeds){Include(component);yield return 0;}
            while(collecting.Count>0||search.Count>0)
            {
                if(collecting.Count>0)
                {
                    var old=collecting.Dequeue();
                    foreach(var member in old.Members){affectedMachines.Add(member);affected.Add(member.Position);if(At(member.Position)==member)Node(member.Position);yield return 0;}
                    foreach(var oldPosition in old.Nodes){affected.Add(oldPosition);yield return 0;}
                    continue;
                }
                var node=search.Dequeue();var position=node.Position;
                if(componentAt.TryGetValue(position,out var owner)&&!replaced.Contains(owner))Include(owner);
                var machine=At(position);
                if(machine!=null&&IndustryId.Bridge(machine.Definition.Id)&&machine.OwnerId.Length>0&&machine.LinkName.Length>0)
                {
                    var key=(machine.OwnerId,machine.Definition.Id,machine.LinkName.ToUpperInvariant());
                    if(named.TryGetValue(key,out var partner))node.Join(partner);else named.Add(key,node);
                    if(!bridgePairs.TryGetValue(key,out var pair))bridgePairs[key]=pair=new List<MachineState>();pair.Add(machine);
                }
                for(int face=0;face<6;face++)
                {var neighbor=IndustryDefinition.Neighbor(position,face);if(DependencyAt(neighbor))node.Join(Node(neighbor));}
                yield return 0;
            }
            var nextComponents=new Dictionary<DependencyNode,AutomationComponent>();
            var nextByPosition=new Dictionary<BlockPos,AutomationComponent>();var ready=new List<MachineState>();
            foreach(var node in nodes.Values)
            {
                var root=node.Root();if(!nextComponents.TryGetValue(root,out var component)){component=new AutomationComponent();component.Activity.Active=false;nextComponents.Add(root,component);}
                component.Nodes.Add(node.Position);nextByPosition.Add(node.Position,component);
                component.Pages.Add(node.Position.Chunk);for(int face=0;face<6;face++)component.Pages.Add(IndustryDefinition.Neighbor(node.Position,face).Chunk);
                var machine=At(node.Position);
                if(machine!=null)
                {
                    component.Members.Add(machine);affectedMachines.Add(machine);affected.Add(machine.Position);
                    machine.Eligible=world.Ready(machine.Position)&&(machine.Definition.Id!=IndustryId.WoodenDoor||world.Ready(machine.Position.Offset(0,1,0)));
                    if(machine.Eligible)ready.Add(machine);else{ResetNetworkState(machine);machine.Status=MachineStatus.Dormant;}
                }
                yield return 0;
            }
            foreach(var unit in SortMachines(ready))yield return unit;
            foreach(var unit in InitializePipeEnds(ready))yield return unit;
            var partners=new Dictionary<MachineState,MachineState>();
            foreach(var pair in bridgePairs.Values)
            {if(pair.Count==2&&pair[0].Eligible&&pair[1].Eligible){partners[pair[0]]=pair[1];partners[pair[1]]=pair[0];}yield return 0;}
            var originals=new[]{Signals.Topology,Power.Topology,ItemNetwork,FluidNetwork};
            var snapshots=new NetworkTopology.Snapshot[originals.Length];
            for(int i=0;i<originals.Length;i++)
            {
                var original=originals[i];var replacement=new NetworkTopology(original.Kind)
                {ExternalEndpointFaces=original.ExternalEndpointFaces,ResolvePorts=original.ResolvePorts,RemotePartner=m=>partners.TryGetValue(m,out var partner)?partner:null};
                foreach(var unit in replacement.Rebuild(ready))yield return unit;
                var snapshot=snapshots[i]=new NetworkTopology.Snapshot();
                foreach(var unit in original.Combine(replacement,oldActivities,affected,m=>nextByPosition[m.Position].Activity,snapshot))yield return unit;
            }
            var publishedComponents=new HashSet<AutomationComponent>();
            var publishedPositions=new Dictionary<BlockPos,AutomationComponent>();
            var publishedPages=new Dictionary<ChunkPos,HashSet<AutomationComponent>>();
            foreach(var component in components){if(!replaced.Contains(component))publishedComponents.Add(component);yield return 0;}
            foreach(var component in nextComponents.Values)
            {
                if(component.Members.Count==0)continue;
                publishedComponents.Add(component);yield return 0;
            }
            foreach(var component in publishedComponents)
            {
                foreach(var position in component.Nodes){publishedPositions.Add(position,component);yield return 0;}
                foreach(var page in component.Pages)
                {if(!publishedPages.TryGetValue(page,out var set))publishedPages[page]=set=new HashSet<AutomationComponent>();set.Add(component);yield return 0;}
            }
            var nextEligible=new List<MachineState>();var nextDevices=new List<MachineState>();
            foreach(var machine in eligible){if(!affectedMachines.Contains(machine))nextEligible.Add(machine);yield return 0;}
            foreach(var machine in devices){if(!affectedMachines.Contains(machine))nextDevices.Add(machine);yield return 0;}
            foreach(var machine in ready){nextEligible.Add(machine);if(!IndustryId.Route(machine.Definition.Id))nextDevices.Add(machine);yield return 0;}
            var publishedPartners=new Dictionary<MachineState,MachineState>();
            foreach(var pair in bridgePartners){if(!affectedMachines.Contains(pair.Key))publishedPartners.Add(pair.Key,pair.Value);yield return 0;}
            foreach(var pair in partners){publishedPartners[pair.Key]=pair.Value;yield return 0;}
            // Single main-thread publication. No callback or fixed tick can see
            // one channel reconnected before the other shared-state channels.
            components=publishedComponents;componentAt=publishedPositions;componentPages=publishedPages;
            pendingComponents.ExceptWith(replaced);
            dirty=pendingComponents.Count>0;
            bridgePartners=publishedPartners;
            eligible=nextEligible;devices=nextDevices;
            for(int i=0;i<originals.Length;i++)originals[i].Publish(snapshots[i]);
            foreach(var machine in ready)ResetNetworkState(machine);
            foreach(var component in nextComponents.Values)component.Activity.Active=true;
            signalsDirty=true;Revision++;
        }
        static IEnumerable<int> SortMachines(List<MachineState> machines)
        {
            var scratch=new MachineState[machines.Count];
            for(int width=1;width<machines.Count;width*=2)
            {
                for(int start=0;start<machines.Count;start+=width*2)
                {
                    int middle=Math.Min(start+width,machines.Count),end=Math.Min(start+width*2,machines.Count),left=start,right=middle;
                    for(int output=start;output<end;output++)
                    {scratch[output]=left<middle&&(right>=end||Compare(machines[left].Position,machines[right].Position)<=0)?machines[left++]:machines[right++];yield return 0;}
                }
                for(int i=0;i<machines.Count;i++){machines[i]=scratch[i];yield return 0;}
            }
        }
    }
}
