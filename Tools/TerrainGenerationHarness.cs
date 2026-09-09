using System;
using RivetReach.Editor;

// Standalone use of the actual generator source with the pinned Editor's Mono runtime.
class TerrainGenerationHarness
{
    static void Main()
    {
        int assertions=0;
        TerrainGenerationChecks.Run((ok,message)=>{assertions++;if(!ok)throw new Exception(message);});
        Console.WriteLine("PASS: "+assertions+" terrain generation assertions. See Logs/terrain-generation-checks.txt.");
    }
}
