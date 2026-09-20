using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace RivetReach.Editor
{
    // Explicit release audit. Run independent suites even when one fails so the
    // report exposes the complete gate state; never author assets as a side effect.
    public static class ReleaseReviewChecks
    {
        public static void Run()
        {
            Directory.CreateDirectory("Logs/ReleaseReview");
            var report=new StringBuilder();int failed=0;
            Action[] suites={WorldLitBatchingChecks.Run,IndustryMaterialVariantChecks.Run,ResourceTickChecks.Run,ChunkWorkPriorityChecks.Run,ControlPresetChecks.Run,PresentationViewCacheChecks.Run,ResidencyInvalidationChecks.Run,ItemInputContractChecks.Run,SaveCompatibilityChecks.Run,DomainChecks.Run,CraftingChecks.Run,PerformanceChecks.Run,
                InventoryChecks.Run,ToolDurabilityChecks.Run,PortableStorageChecks.Run,
                TagReviewChecks.Run,FarmingChecks.Run,FishingChecks.Run,ChickenChecks.Run,
                FoodBalanceChecks.Run,CompostChecks.Run,BedChecks.Run,LightingChecks.Run,
                FluidChecks.Run,LavaChecks.Run,DayNightChecks.Run,AlphaPlaytestChecks.Run,
                AlphaWorldChecks.Run,FloaterChecks.Run,IndustryChecks.Run,ElectricFurnaceChecks.Run,
                GridAllocationChecks.Run,BatteryChecks.Run,MultiblockChecks.Run,ConnectionChecks.Run,
                ConnectedPipeChecks.Run,ItemPipeChecks.Run,DoorChecks.Run,HandCrankChecks.Run,
                BridgeChecks.Run,RangedPumpChecks.Run,CrateChecks.Run,CrateRoutingEdgeChecks.Run,
                RenewableChecks.Run,RenewablePowerChecks.Run,RenewableCompatibilityChecks.Run};
            foreach(var suite in suites)
            {
                var watch=Stopwatch.StartNew();
                try{suite();report.AppendLine($"PASS {suite.Method.DeclaringType.Name}: {watch.Elapsed.TotalMilliseconds:F3} ms");}
                catch(Exception e){failed++;report.AppendLine($"FAIL {suite.Method.DeclaringType.Name}: {e}");}
                File.WriteAllText("Logs/ReleaseReview/domain-summary.txt",report.ToString());
            }
            if(failed>0)throw new InvalidOperationException($"{failed} release review suites failed. See Logs/ReleaseReview/domain-summary.txt.");
        }
    }
}
