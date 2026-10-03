using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace RivetReach.Editor
{
    // Golden outputs captured from e293b1e before the performance patches. Capture
    // is an explicit command, never a side effect of a check or player build.
    public static class PerformancePlanChecks
    {
        const string Reference = ".docs/verification/performance-plan-2026-10-03/reference.txt";

        public static void CaptureReference()
        {
            if (File.Exists(Reference)) throw new InvalidOperationException("Performance reference already exists.");
            Directory.CreateDirectory(Path.GetDirectoryName(Reference));
            File.WriteAllText(Reference, Snapshot());
        }

        public static void Run()
        {
            if (File.ReadAllText(Reference) != Snapshot())
                throw new InvalidOperationException("Block traits, save fingerprints or terrain differ from the pre-optimization reference.");
            PerformanceEquivalenceChecks.Run();
        }

        static string Snapshot()
        {
            var result = new StringBuilder();
            for (int i = 0; i < 256; i++)
            {
                byte id = (byte)i;
                result.AppendLine($"block {id}: {BlockId.Solid(id)} {BlockId.Opaque(id)} {BlockId.Crop(id)} {BlockId.Placeable(id)}");
            }
            var store = new SaveStore("unused", ItemRegistry.Load());
            foreach (string name in new[] { "content", "currentSchemaContent", "legacyContent", "currentContent", "modernContent" })
            {
                object value = typeof(SaveStore).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(store);
                IEnumerable<string> values = value is string single ? new[] { single } : ((IEnumerable<string>)value).OrderBy(s => s, StringComparer.Ordinal);
                foreach (string fingerprint in values) result.AppendLine(name + ": " + fingerprint);
            }
            using var hash = SHA256.Create();
            foreach (string version in new[] { TerrainGenerator.LegacyVersion, TerrainGenerator.LavaVersion, TerrainGenerator.FarmVersion, TerrainGenerator.Version })
            foreach (int seed in new[] { 17, 7441, -93 })
            {
                var generator = new TerrainGenerator(seed, version);
                var cells = new List<byte>();
                for (int z = -40; z <= 40; z += 2)
                for (int x = -40; x <= 40; x += 2)
                {
                    int height = generator.Height(x, z);
                    for (int y = height - 4; y <= height + 12; y++) cells.Add(generator.At(new BlockPos(x, y, z)));
                    cells.Add(generator.At(new BlockPos(x, TerrainGenerator.MinY + 8, z)));
                }
                result.AppendLine($"terrain {version} {seed}: {Convert.ToBase64String(hash.ComputeHash(cells.ToArray()))}");
            }
            return result.ToString().Replace("\r\n", "\n");
        }
    }
}
