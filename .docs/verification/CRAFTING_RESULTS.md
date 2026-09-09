# Modular crafting verification — 2026-09-08

The personal 2×2 crafting interface and the shared 2×2/3×3/4×4 recipe engine are implemented and verified below. [CRAFTING.md](../CRAFTING.md) owns authoring and architecture; [GAMEPLAY.md section 16](../GAMEPLAY.md#16-modular-grid-crafting) owns interaction behavior; [ECONOMY.md](../ECONOMY.md#current-survival-recipes-and-tiers) now owns the expanded survival recipes; the measurements below describe the earlier three-recipe starter slice.

## Tested configuration and artifact

- Unity **6000.4.4f1**, URP **17.4.0**, Windows x64 development player, 1280×720.
- Intel Core i7-10750H, NVIDIA GeForce RTX 2060, 32,553 MB reported system memory.
- Final build: **Succeeded, zero errors, zero warnings**, 23.208 seconds with the warmed build cache, 186,604,564 bytes in Unity's report. [Raw build summary](crafting/build-summary.txt).
- The reviewed standalone is copied to `Builds/Crafting/RivetReach.exe`; keep its adjacent data/runtime files together. The usual build script still generates `Builds/PlayerRevision4/RivetReach.exe`.
- Verification used an isolated copy under ignored `Logs/CraftingProject/`, based on committed `1c4fea2` plus this feature. The original Editor was left open; concurrent character/audio work in the shared checkout was excluded after its stale audio compilation error interrupted the first shared builds. The screenshots therefore show the committed player art baseline, not an assessment of the parallel art revision.

## Correctness checks

**1,157,353 crafting assertions passed**, including 15,000 randomized inventory/grid/cursor/craft/return operations with weighted resource conservation and per-slot bounds. Cases cover all three grid sizes, every legal translation of a sample shape, mirrors, internal holes, extra items, station gates, full 3×3/4×4 recipes, shapeless permutations and repeated ingredients with unequal per-slot quantities. [Raw crafting checks and benchmark](crafting/crafting-checks.txt).

Other cases cover immutable definition snapshots, malformed/ambiguous content, duplicate IDs, unknown items, empty/free recipes, over-limit counts, unavailable or partially available output capacity, unstackable tools, stale previews, exact insertion, requested batch limits and ingredient return with a full inventory. The shipped catalog is checked from its own registered assets, so a valid recipe addition does not require changing a hard-coded build-test recipe count.

The existing **54,283 domain assertions passed** with the shared inventory container: coordinates, deterministic terrain and halos, meshing, inventory transfer/overflow/split, grass and trees. [Raw domain report](crafting/domain-checks.txt).

The final Windows player passed **44 focused runtime assertions**, timestamp 20:45:13 UTC, with no logged errors. Virtual Input System mouse events went through normal UI raycasting and handlers to exercise right-place, craft-one, full cursor rejection, Shift-result batching, result dragging, input dragging and quick return. The same run checked guide rendering, full-destination rejection, close/reopen retention, UI rebuilding and inventory-mode access. [Raw focused report](crafting/runtime-report.json).

The broader Windows **terrain/inventory/visual regression passed 263 assertions** with no logged errors. It covered normal mining and placement, partial/full pickup, inventory close, streaming/origin shifts, player controls and existing interaction behavior. This run preceded only the final tooltip-footer presentation correction; the final build and focused UI suite were then rerun. [Raw regression report](crafting/full-regression-report.json).

## Measured matching cost

Final isolated Editor Mono run, 20:44:03 UTC. Each row compiles the specified number of distinct synthetic 4×4 recipes, warms 10,000 lookups and measures 100,000 repeated hits on the final recipe, then 100,000 misses on a changed grid. Figures are loop means on this workstation; they are not worst-case latency or whole-game frame timings.

| Recipes | Compile once | Matching hit | No-match lookup | Managed allocation in each measured loop |
|---:|---:|---:|---:|---:|
| 10 | 0.682 ms | 0.847 µs | 0.727 µs | 0 bytes |
| 1,000 | 14.590 ms | 0.840 µs | 0.798 µs | 0 bytes |
| 10,000 | 27.226 ms | 0.709 µs | 0.707 µs | 0 bytes |

One million unchanged preview reads took **5.172 ms** and allocated **zero managed bytes**. These reads reuse the revision cache and do not match again. Compilation allocates its immutable content/index structures; it is outside the matching loop. Repeated runs varied with machine load. The measured loops establish allocation and cost for these workloads, not a hardware-independent performance guarantee.

The focused runtime report's unused frame/memory sampling fields are zero because that scenario does not sample them. They must not be read as zero whole-game memory or zero frame cost. The allocation measurements above come from `GC.GetAllocatedBytesForCurrentThread` around the isolated loops.

## Visual review

Actual final player screenshots were inspected. The ingredient/result areas, guide layouts, quantity labels and full-inventory message fit at 1280×720. Review found a tooltip overlapping the hotbar; the final revision places contextual text in the footer and hides the general hint while a tooltip is visible. Hover information also refreshes after crafting.

![Axe recipe ready in the personal grid](crafting/crafting-axe-ready.png)

![Guide generated from the same compiled recipe catalog](crafting/crafting-recipe-guide.png)

![Full inventory rejection with ingredients retained and unobstructed hotbar](crafting/crafting-inventory-full.png)

[Completed craft screenshot](crafting/crafting-axe-complete.png) shows the outputs in inventory and emptied ingredient grid.

## Remaining review and scope

The 3×3 and 4×4 cores are tested; their station interfaces are not implemented by this extension. The current recipes remake the existing starter tools and are working balance defaults. Larger progression, ingredient alternatives/tags, byproducts/catalysts, durability/metadata, machine processing, durable saves and multiplayer authority transport remain future work. The current registry and transactions have explicit extension boundaries in [CRAFTING.md](../CRAFTING.md).

Player assessment of the recipe guide, interaction feel and starter balance remains pending. These automated checks and screenshot reviews do not claim final game quality or exhaustive correctness for later systems.
