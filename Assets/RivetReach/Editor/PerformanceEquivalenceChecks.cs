using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Random = System.Random;

namespace RivetReach.Editor
{
    public static class PerformanceEquivalenceChecks
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static int assertions;
        static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException("Performance equivalence: " + message);
        }
        static object Field(object owner, string name) => owner.GetType().GetField(name, Private).GetValue(owner);
        static byte[] Save(object owner)
        {
            using var stream = new MemoryStream();
            using (var writer = new SaveWriter(stream)) owner.GetType().GetMethod("WriteSave", Private).Invoke(owner, new object[] { writer });
            return stream.ToArray();
        }
        public static void Run()
        {
            assertions = 0;
            Coordinates(); TickCap(); FrameSettings(); PumpShortcut(); SchedulerCache(); FluidSchedule(); WorldReads(); SaveListing();
            Directory.CreateDirectory("Logs/PerformancePlan");
            File.WriteAllText("Logs/PerformancePlan/equivalence.txt", $"PASS {assertions} assertions: coordinates, capped ticks/save backlog, frame settings, pump shortcut, scheduler cache, fluid decisions/save bytes, resident reads/source counts, save listing.\n");
        }

        static void Coordinates()
        {
            void Compare(long x, int y)
            {
                var chunk = new BlockPos(x, y, x).Chunk;
                Check(chunk.X == BlockPos.FloorDiv(x, 32) && chunk.Z == chunk.X && chunk.Y == BlockPos.FloorDiv(y, 32), "negative/extreme floor division");
            }
            for (int i = -70000; i <= 70000; i++) Compare(i, i);
            foreach (long x in new[] { long.MinValue, long.MinValue + 31, long.MaxValue, -1000000033L, 1000000033L })
            { Compare(x, int.MinValue); Compare(x, int.MaxValue); }
        }

        static void TickCap()
        {
            var root = new GameObject("Tick budget check"); root.SetActive(false);
            try
            {
                var game = root.AddComponent<Expedition>(); var world = root.AddComponent<VoxelWorld>();
                typeof(Expedition).GetField("<World>k__BackingField", Private).SetValue(game, world);
                var survival = new WorldSurvival(game); var random = new Random(51);
                double reference = 0; long total = 0;
                for (int frame = 0; frame < 2000; frame++)
                {
                    float seconds = frame % 19 == 0 ? .333f : (float)(random.NextDouble() / 45);
                    reference += seconds * WorldSurvival.TicksPerSecond;
                    int ticks = survival.Advance(seconds); total += ticks;
                    Check(ticks >= 0 && ticks <= 3 && Math.Abs(total + survival.TickFraction - reference) < 1e-8, "tick cap retains all accrued time");
                }
                while (survival.TickFraction >= 1) total += survival.Advance(0);
                Check(total == (long)Math.Floor(reference), "catch-up drains to the uncapped total");
                survival.Advance(.333f); Check(survival.TickFraction > 1, "fixture holds an actual backlog");
                byte[] bytes = Save(survival); var restored = new WorldSurvival(game);
                using (var reader = new SaveReader(new MemoryStream(bytes), ItemRegistry.Load()))
                    typeof(WorldSurvival).GetMethod("ReadSave", Private).Invoke(restored, new object[] { reader });
                Check(bytes.SequenceEqual(Save(restored)), "saved tick backlog round-trips without format changes");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void FrameSettings()
        {
            const string key = "display.framePacing";
            bool existed = PlayerPrefs.HasKey(key); int saved = PlayerPrefs.GetInt(key), cap = Application.targetFrameRate, sync = QualitySettings.vSyncCount;
            try
            {
                PlayerPrefs.DeleteKey(key); Check(FramePacing.Current == FramePacing.Mode.Legacy90, "default remains 90 FPS");
                foreach (FramePacing.Mode mode in Enum.GetValues(typeof(FramePacing.Mode)))
                {
                    FramePacing.Apply(mode, true);
                    int expected = mode == FramePacing.Mode.Cap60 ? 60 : mode == FramePacing.Mode.Cap120 ? 120 : mode == FramePacing.Mode.DisplaySync ? -1 : 90;
                    Check(FramePacing.Current == mode && Application.targetFrameRate == expected && QualitySettings.vSyncCount == (mode == FramePacing.Mode.DisplaySync ? 1 : 0), "persisted pacing mode applies");
                }
            }
            finally
            {
                if (existed) PlayerPrefs.SetInt(key, saved); else PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save(); Application.targetFrameRate = cap; QualitySettings.vSyncCount = sync;
            }
        }

        class IndustryWorld : IIndustryWorld, IIndustryResidentCells
        {
            public readonly Dictionary<BlockPos, byte> Cells = new Dictionary<BlockPos, byte>();
            public readonly HashSet<ChunkPos> Sleeping = new HashSet<ChunkPos>();
            public int Reads;
            public bool Ready(BlockPos p) => !Sleeping.Contains(p.Chunk);
            public byte Get(BlockPos p) { Reads++; return Cells.TryGetValue(p, out byte value) ? value : (byte)0; }
            public bool TryRead(BlockPos p, out byte value) { value = 0; if (!Ready(p)) return false; value = Get(p); return true; }
            public bool Remove(BlockPos p, byte expected) => Ready(p) && Get(p) == expected && Cells.Remove(p);
            public ItemContainer Storage(BlockPos p) => null;
            public byte Drop(byte block) => block;
            public bool PlayerInside(BlockPos p) => false;
        }
        sealed class IndexedWorld : IndustryWorld, IFluidSourceIndex
        {
            public bool TryGetFluidSourceCount(ChunkPos chunk, out int count)
            {
                count = 0; if (Sleeping.Contains(chunk)) return false;
                foreach (var pair in Cells)
                    if (pair.Key.Chunk.Equals(chunk) && Fluids.Registry.Get(pair.Value) is FluidDefinition fluid && fluid.IsSource(pair.Value)) count++;
                return true;
            }
        }
        static void PumpShortcut()
        {
            var plain = new IndustryWorld(); var indexed = new IndexedWorld();
            var a = new IndustrySimulation(plain, _ => 64); var b = new IndustrySimulation(indexed, _ => 64);
            var p = new BlockPos(0, 40, 0); plain.Cells[p] = indexed.Cells[p] = IndustryId.RangedPump;
            var first = a.Add(p, IndustryId.RangedPump); var second = b.Add(p, IndustryId.RangedPump);
            var random = new Random(719);
            for (int tick = 0; tick < 1200; tick++)
            {
                if (tick == 180) Check(indexed.Reads < plain.Reads, "empty resident reach avoids cell reads");
                if (tick >= 180 && tick % 23 == 0)
                {
                    var cell = p.Offset(random.Next(-8, 9), random.Next(-8, 9), random.Next(-8, 9));
                    plain.Cells[cell] = indexed.Cells[cell] = tick % 2 == 0 ? Fluids.Water.Source : Fluids.Lava.Source;
                }
                if (tick % 79 == 0)
                {
                    var chunk = p.Offset(-8, -8, -8).Chunk;
                    if (!plain.Sleeping.Add(chunk)) plain.Sleeping.Remove(chunk);
                    if (!indexed.Sleeping.Add(chunk)) indexed.Sleeping.Remove(chunk);
                }
                if (tick % 131 == 0) { first.Fluid.Withdraw(first.Fluid.Amount); second.Fluid.Withdraw(second.Fluid.Amount); }
                a.Step(); b.Step();
                foreach(string field in new[] { "PumpScanIndex", "PumpTarget", "PumpScanUnloaded", "PumpRetryTick" })
                    Check(Equals(Field(first, field), Field(second, field)), "indexed pump preserves " + field);
                Check(first.Work == second.Work && first.WorkInput == second.WorkInput && first.Status == second.Status
                    && first.Fluid.Amount == second.Fluid.Amount && first.Fluid.Fluid == second.Fluid.Fluid, "indexed pump preserves every scan/work/collection state");
            }
        }

        static void SchedulerCache()
        {
            var world = new IndustryWorld(); var sim = new IndustrySimulation(world, _ => 64); var random = new Random(18);
            var method = typeof(IndustrySimulation).GetMethod("CanSimulate", Private);
            for (int turn = 0; turn < 500; turn++)
            {
                var p = new BlockPos(random.Next(-5, 6), 20, random.Next(-5, 6));
                if (sim.At(p) == null) { world.Cells[p] = IndustryId.PowerCable; sim.Add(p, IndustryId.PowerCable); }
                else { sim.Remove(p); world.Cells.Remove(p); }
                if (turn % 17 == 0)
                {
                    var page = new ChunkPos(0, 0, 0);
                    if (!world.Sleeping.Add(page)) world.Sleeping.Remove(page);
                    sim.Invalidate();
                }
                for (int i = 0; i < 3; i++) sim.Step();
                var index = (IDictionary)Field(sim, "componentAt");
                foreach (var machine in sim.Machines.Values)
                {
                    object component = index[machine.Position];
                    var activity = component == null ? null : (NetworkActivity)component.GetType().GetField("Activity").GetValue(component);
                    bool expected = machine.Eligible && activity != null && activity.Active;
                    Check((bool)method.Invoke(sim, new object[] { machine }) == expected, "cached scheduler agrees during edits, suspension and publication");
                }
            }
        }

        sealed class FlowWorld : IFluidWorld
        {
            public readonly Dictionary<BlockPos, byte> Cells = new Dictionary<BlockPos, byte>();
            public readonly List<string> Decisions = new List<string>();
            public bool Dormant;
            public Action<IFluidWorld, BlockPos> Changed;
            public bool TryRead(BlockPos p, out byte value)
            {
                value = 0; Decisions.Add("read " + p);
                if (Dormant && p.X >= 16) return false;
                value = p.Y <= 0 || Math.Abs(p.X) > 24 || Math.Abs(p.Z) > 24 ? BlockId.Stone : Cells.TryGetValue(p, out byte cell) ? cell : (byte)0;
                return true;
            }
            public bool ChangeFluid(BlockPos p, byte expected, byte replacement)
            {
                if (!TryRead(p, out byte current) || current != expected) return false;
                Decisions.Add($"change {p} {expected} {replacement}"); Cells[p] = replacement; Changed(this, p); return true;
            }
        }
        static void FluidSchedule()
        {
            var old = new ReferenceFluidSimulation(Fluids.Registry); var current = new FluidSimulation(Fluids.Registry);
            var a = new FlowWorld { Changed = old.Changed }; var b = new FlowWorld { Changed = current.Changed };
            var random = new Random(62);
            for (int i = 0; i < 1800; i++)
            {
                var p = new BlockPos(i % 40 - 20, i / 1600 + 1, i / 40 % 40 - 20);
                old.Wake(p, i % 11); current.Wake(p, i % 11);
            }
            for (int tick = 0; tick < 400; tick++)
            {
                if (tick % 11 == 0)
                {
                    var p = new BlockPos(random.Next(-20, 21), random.Next(1, 7), random.Next(-20, 21));
                    byte id = tick % 3 == 0 ? Fluids.Lava.Source : tick % 3 == 1 ? Fluids.Water.Source : (byte)0;
                    a.Cells[p] = b.Cells[p] = id; old.Changed(a, p); current.Changed(b, p);
                }
                if (tick % 71 == 0)
                {
                    a.Dormant = b.Dormant = !a.Dormant;
                    if (!a.Dormant) { old.Ready(new ChunkPos(0, 0, 0)); current.Ready(new ChunkPos(0, 0, 0)); }
                }
                a.Decisions.Clear(); b.Decisions.Clear(); old.Step(a); current.Step(b);
                Check(a.Decisions.SequenceEqual(b.Decisions) && old.LastWork == current.LastWork, "fluid reads, transactions, wake order and budget match frozen algorithm");
                Check(Save(old).SequenceEqual(Save(current)), "fluid schedule and dormant frontier save bytes match frozen algorithm");
                if (tick == 199)
                {
                    byte[] saved = Save(current); current = new FluidSimulation(Fluids.Registry);
                    using var reader = new SaveReader(new MemoryStream(saved), ItemRegistry.Load());
                    typeof(FluidSimulation).GetMethod("ReadSave", Private).Invoke(current, new object[] { reader });
                    b.Changed = current.Changed;
                }
            }
        }

        static void WorldReads()
        {
            var root = new GameObject("Resident read equivalence"); root.SetActive(false);
            try
            {
                var world = root.AddComponent<VoxelWorld>(); world.Initialize(719);
                var chunks = (IDictionary)Field(world, "chunks");
                var edits = (IDictionary)Field(world, "edits");
                var residentType = typeof(VoxelWorld).GetNestedType("Resident", BindingFlags.NonPublic);
                for (int z = -1; z <= 0; z++) for (int x = -1; x <= 0; x++)
                {
                    var resident = Activator.CreateInstance(residentType, true);
                    residentType.GetField("Cells").SetValue(resident, new byte[34 * 34 * 34]);
                    chunks.Add(new ChunkPos(x, 12, z), resident);
                }
                var change = typeof(VoxelWorld).GetMethod("Change", Private); var random = new Random(921);
                byte OldGet(BlockPos p)
                {
                    var page = (Dictionary<int, byte>)edits[p.Chunk];
                    if (page != null && page.TryGetValue(p.Index, out byte edited)) return edited;
                    object resident = chunks[p.Chunk];
                    if (resident != null)
                    {
                        var cells = (byte[])residentType.GetField("Cells").GetValue(resident);
                        if (cells != null) return cells[ChunkMesher.Index(p.Index % 32, p.Index / 32 % 32, p.Index / 1024)];
                    }
                    return world.Generator.At(p);
                }
                var reference=new ReferenceWorldReads(world,OldGet);
                byte[] ids = { 0, BlockId.Stone, Fluids.Water.Source, Fluids.Lava.Source, Fluids.Water.Falling, BuildingBlocks.WoodenSlab, BuildingBlocks.StoneUpper, IndustryId.Glass };
                void CompareQueries(BlockPos cell)
                {
                    var feet=world.Local(cell)+new Vector3(.5f,.25f,.5f);
                    Check(world.Overlaps(feet,.6f,1.8f)==reference.Overlaps(feet,.6f,1.8f), "shape-aware collision matches old reads");
                    var start=feet+new Vector3(random.Next(-6,7),random.Next(-6,7),random.Next(-6,7));
                    var direction=feet-start;
                    foreach(bool sources in new[]{false,true})
                    {
                        bool expected=reference.Trace(start,direction,12,out var oldHit,sources,false);
                        bool actual=world.Select(start,direction,12,out var hit,sources);
                        Check(expected==actual&&(!actual||hit.Position.Equals(oldHit.Position)&&hit.Block==oldHit.Block&&hit.Face==oldHit.Face&&hit.Distance==oldHit.Distance), "selection traversal matches old reads");
                    }
                    bool solidExpected=reference.Trace(start,direction,12,out var expectedHit,false,true);
                    bool solidActual=world.RaycastSolid(start,direction,12,out var position,out byte id);
                    Check(solidExpected==solidActual&&(!solidActual||position.Equals(expectedHit.Position)&&id==expectedHit.Block), "solid traversal matches old reads");
                }
                for (int i = 0; i < 700; i++)
                {
                    var p = new BlockPos(random.Next(-32, 32), random.Next(384, 416), random.Next(-32, 32));
                    byte before = OldGet(p), after = ids[random.Next(ids.Length)];
                    Check((bool)change.Invoke(world, new object[] { p, before, after, false, true }), "fixture edit succeeds");
                    Check(world.Get(p) == after && world.TryRead(p, out byte value) && value == after && world.Solid(p) == BlockId.Solid(after), "resident reads preserve edited authority");
                    CompareQueries(p);
                    foreach (DictionaryEntry pair in chunks)
                    {
                        var cells = (byte[])residentType.GetField("Cells").GetValue(pair.Value); int count = 0;
                        for (int z = 0; z < 32; z++) for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                        {
                            byte cell = cells[ChunkMesher.Index(x, y, z)];
                            if (Fluids.Registry.Get(cell) is FluidDefinition fluid && fluid.IsSource(cell)) count++;
                        }
                        Check(world.TryGetFluidSourceCount((ChunkPos)pair.Key, out int indexed) && indexed == count, "resident source count excludes halos and follows every edit");
                    }
                }
                var pageToUnload=new ChunkPos(-1,12,-1);object residentToReload=chunks[pageToUnload];chunks.Remove(pageToUnload);
                for(int i=0;i<100;i++)CompareQueries(new BlockPos(-random.Next(1,33),400,-random.Next(1,33)));
                chunks[pageToUnload]=residentToReload;
                for(int i=0;i<100;i++)CompareQueries(new BlockPos(-random.Next(1,33),400,-random.Next(1,33)));
                var dormant = new BlockPos(64, 400, 64);
                Check(!world.TryRead(dormant, out _) && world.Solid(dormant) && !world.TryGetFluidSourceCount(dormant.Chunk, out _), "nonresident boundaries remain closed");
            }
            finally { root.GetComponent<VoxelWorld>()?.Stop(); UnityEngine.Object.DestroyImmediate(root); }
        }

        static void SaveListing()
        {
            string path = Path.Combine(Path.GetTempPath(), "RivetReach-listing-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new SaveStore(path, ItemRegistry.Load());
                var entry = new SaveEntry { Id = Guid.NewGuid().ToString("N"), WorldId = Guid.NewGuid().ToString("N"), Name = "First", UtcTicks = DateTime.UtcNow.Ticks };
                byte[] bytes = store.Encode(entry, w => w.Write(71)); store.Write(entry, bytes);
                var first = store.List(); Check(first.Count == 1, "first save lists"); first[0].Name = "caller mutation";
                Check(store.List()[0].Name == "First", "cached metadata is isolated from callers");
                entry.Name = "Replacement"; entry.UtcTicks++; store.Write(entry, store.Encode(entry, w => w.Write(72)));
                Check(store.List().Count == 2 && store.List()[0].Name == "Replacement", "write invalidates current and backup metadata");
                string current = Path.Combine(path, entry.Id + ".rrsave");
                File.WriteAllBytes(current, new byte[] { 1, 2, 3 });
                Check(store.List().Count == 1 && store.ScanWarning != null, "corruption is revalidated and reported");
                File.WriteAllBytes(current, bytes); Check(store.List().Count == 2 && store.ScanWarning == null, "failed entries are not cached");
                entry.Name = "Other";
                byte[] sameLength = store.Encode(entry, w => w.Write(71));
                Check(sameLength.Length == bytes.Length, "same-length replacement fixture");
                DateTime previousTime = File.GetLastWriteTimeUtc(current);
                File.WriteAllBytes(current, sameLength); File.SetLastWriteTimeUtc(current, previousTime.AddSeconds(2));
                Check(store.List().Single(e => !e.Backup).Name == "Other", "timestamp invalidates same-length metadata");
                string renamed = Path.Combine(path, Guid.NewGuid().ToString("N") + ".rrsave");
                File.Move(current, renamed);
                Check(store.List().Count == 1 && store.ScanWarning != null, "renamed identity is revalidated");
                File.Move(renamed, current);
                Check(store.List().Count == 2 && store.ScanWarning == null, "restored filename is listed again");
                File.Delete(current); Check(store.List().Count == 1, "deletion invalidates metadata");
                Directory.Delete(path, true); Check(store.List().Count == 0, "directory deletion clears listing");
            }
            finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
        }
    }
}
