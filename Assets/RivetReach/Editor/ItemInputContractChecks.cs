using System;
using System.IO;
namespace RivetReach.Editor
{
    public static class ItemInputContractChecks
    {
        public static void Run()
        {
            int checks=0;void Check(bool ok,string message){if(!ok)throw new Exception("Item input contract: "+message);checks++;}
            var registry=ItemRegistry.Load();int Limit(byte id)=>registry.Get(id).stackLimit;
            var storage=new ItemContainer(3,Limit);IItemPipeInventory endpoint=storage;
            storage.Add(BlockId.RawIron,1);
            Check(endpoint.QueryInput(BlockId.RawIron)==ItemInputRequest.Prefer&&endpoint.QueryInput(BlockId.RawCopper)==ItemInputRequest.Accept,"Storage declares cargo affinity independently from receiver fairness");
            Check(endpoint.QueryInput(0)==ItemInputRequest.Reject,"Empty identity never requests transfer");
            var furnace=new FurnaceState(ProcessingCatalogAsset.Load().Compile(registry),Limit);IItemPipeInventory processor=furnace;
            Check(processor.QueryInput(BlockId.Coal,4)!=ItemInputRequest.Reject&&processor.QueryInput(BlockId.Coal,0)==ItemInputRequest.Reject,"Fuel declaration follows authored rear-only face");
            Check(processor.QueryInput(BlockId.RawIron,4)==ItemInputRequest.Reject&&processor.QueryInput(BlockId.RawIron,0)==ItemInputRequest.Accept,"Ingredient declaration follows ordinary faces");
            processor.TryInsert(BlockId.RawIron,0);
            Check(processor.QueryInput(BlockId.RawIron,0)==ItemInputRequest.Prefer,"Processor declares current ingredient affinity");
            long revision=storage.Revision;int rejected=0;
            using var counter=new AllocationCounter();
            long allocated=counter.Measure(()=>{for(int i=0;i<10000;i++){if(processor.QueryInput(BlockId.Coal,0)==ItemInputRequest.Reject)rejected++;endpoint.QueryInput(BlockId.RawIron,0);}});
            Check(rejected==10000&&storage.Revision==revision,"Repeated interface queries never mutate inventory");
            if(counter.Supported)Check(allocated==0,"Repeated interface queries emit no managed allocations");
            var output=new ItemContainer(1,Limit);output.Add(BlockId.IronIngot,5);ItemStack held=default;
            Check(output.TakeToCursor(0,ref held,true)&&held.Count==3&&output.Slots[0].Count==2,"Shared result gesture rounds a right-click half upward");
            held=new ItemStack(BlockId.CopperIngot,1);Check(!output.TakeToCursor(0,ref held,false)&&output.Slots[0].Count==2,"Result slots reject insertion and mismatched cursor cargo");
            held=new ItemStack(BlockId.IronIngot,64);Check(!output.TakeToCursor(0,ref held,false)&&output.Slots[0].Count==2,"Full cursor preserves output");
            output.Take(0,64);var battery=new ItemStack(IndustryId.Battery,1){Energy=12345};output.Add(battery);held=default;
            Check(output.TakeToCursor(0,ref held,false)&&held.Equals(battery)&&output.Slots[0].Empty,"Shared extraction retains exact instance payload");
            File.WriteAllText("Logs/ReleaseReview/input-contract.txt","PASS "+checks+" assertions; 10000 receiver query allocation result "+allocated+". "+counter.Description+"\n");
        }
    }
}
