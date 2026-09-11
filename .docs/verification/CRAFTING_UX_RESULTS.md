# Crafting interactions and bottleneck measurements — 2026-09-11

> This is the dated baseline before screen reuse. [Retained-screen results](SCREEN_REUSE_RESULTS.md) record the subsequent implementation and timings; the baseline measurements below retain their original build identity.

The Windows review player is `Builds/RecipeBrowser/RivetReach.exe`, built with Unity 6000.4.4f1. Right-drag now deposits one ingredient per visited crafting/inventory cell; Shift-click a browser icon to prepare one recipe, or Ctrl+Shift-click to prepare the maximum complete batch. Ctrl-click remains a one-recipe alias and right-click opens uses. [CRAFTING.md](../CRAFTING.md#right-drag-placement-and-future-ingredient-sources--2026-09-11) owns exact behavior and the adjacent-inventory WIP boundary.

## Measured bottleneck

**Reconstructing screens is the largest measured crafting UI cost.** The first run measured a 24.78 ms median inventory open; the final run measured 34.96 ms. A craft transaction measured 0.0008–0.0009 ms at the median; the full craft UI call plus managed canvas flush measured 0.135–0.138 ms. A screen open can exceed a 16.7 ms frame budget even when crafting transactions are fast. This identifies a screen-opening hitch; it does not establish the cause of every lag spike in an arbitrary save.

The [final action timings](crafting-ux-2026-09-11/crafting-timings.csv) separate command work from UI updates. The [first-run timings](crafting-ux-2026-09-11/initial-timings.json) retain their original artifact timestamp and implementation context. These are two local runs with other workstation activity, not an isolated before/after performance claim.

| Action in final Development player | Samples | Median | p95 |
| --- | ---: | ---: | ---: |
| Open inventory and flush canvas | 12 | 34.956 ms | 39.412 ms |
| Craft transaction only | 48 | 0.0009 ms | 0.0015 ms |
| Craft UI and flush canvas | 48 | 0.138 ms | 0.196 ms |
| Open recipe detail and flush canvas | 12 | 3.526 ms | 4.525 ms |

`Canvas.ForceUpdateCanvases` includes managed layout/geometry work in these action samples, but not the subsequent native batching or GPU work. Cold outliers are retained (including the first transaction at 0.52 ms and recipe-detail maximum at 31.81 ms); medians are not latency guarantees.

The [frame counters](crafting-ux-2026-09-11/crafting-frame-counters.csv) independently record 120 frames after 30 warm-up frames for idle inventory and a synthetic craft-every-frame workload. During repeated crafting, median slot input was 0.0256 ms, slot refresh 0.0176 ms, `UIEvents.WillRenderCanvases` 0.084 ms and the canvas geometry worker marker 0.404 ms. GPU frame time was 3.63 ms idle and 3.66 ms during crafting. Main-thread frame duration remained about 11.1 ms in both phases, including frame pacing; this is not an uncapped CPU-throughput measurement. Worker/parent marker durations overlap and must not be added together. `GC.Alloc` is recorded in time units, not bytes or allocation count.

The tested machine is an i7-10750H / RTX 2060 / 32 GB RAM, Direct3D12, 1280×720, seed 246813, view radius 4 after terrain settles. Mobs are disabled for this focused fixture. These results do not profile an active factory, streaming traversal, a large loaded save, Editor overhead or other hardware.

## Changes and remaining work

- Correct the right-drag event routing: deposit on press/entry, retain a visited-cell set, reject incompatible/full cells, and end the gesture on release, rebuild or focus loss. The old drag path treated right-drag as left-drag and deposited on release.
- Add batch ingredient planning and atomic publication across an ordered source set. Every registered recipe is checked for single-to-maximum top-up, matching, conservation and repeated-fill stability. The gameplay caller supplies only the backpack; adjacent discovery, access and automation wake-up are WIP.
- Construct recipe-detail children while their panel is inactive, then enable it once. Cache the cursor stack display so unchanged counts do not allocate a new string every frame. No material speedup is claimed from the separate timing runs.
- Add `RivetReach.UI.Rebuild`, `SlotInput`, `RefreshSlots`, `RecipeView` and `RecipeFill` profiler markers. Whole-screen reuse/pooling remains the principal optimization target because opening inventory still reconstructs its hierarchy. This pass diagnoses that cost; it does not remove the screen-opening hitch.

## Acceptance and artifacts

The [build identity](crafting-ux-2026-09-11/build-identity.json) records executable/assembly hashes and hashes of the actual isolated source snapshot. The [build summary](crafting-ux-2026-09-11/build-summary.txt) reports success with **zero errors and warnings**. The user's main Editor and unrelated work were preserved.

- [Transfer checks](crafting-ux-2026-09-11/transfer-checks.txt): **1,105 assertions**, including all 91 recipes, repeated ingredient aggregation, per-cell limits, leftover materials, blocked returns and multi-source atomicity/alias rejection.
- [Crafting checks](crafting-ux-2026-09-11/crafting-checks.txt): **1,158,852 assertions**. Independent starter acceptance also passed **1,862 checks** for the 55 baseline layouts/quantities.
- [Browser index checks](crafting-ux-2026-09-11/index-checks.txt): **368 assertions**, with 127 items, 91 grid recipes and 11 furnace recipes.
- [Native runtime report](crafting-ux-2026-09-11/runtime-report.json): **515 assertions, zero errors**. Covers new drag/fill gestures, actual 2×2/3×3/4×4 interfaces, furnace/chest/crusher rejection, browsing, search focus, original left-drag crafting and full-inventory conservation.

The following final player captures were visually inspected:

![Maximum fill prepares four workbenches from nineteen planks, leaving three in inventory](crafting-ux-2026-09-11/maximum-recipe-fill.png)

![One right-drag places a plank in each personal crafting cell while keeping fifteen on the cursor](crafting-ux-2026-09-11/right-drag-pattern.png)

[Browser verification](RECIPE_BROWSER_RESULTS.md) includes the current bench and small-window captures. User playfeel acceptance remains separate from automated pointer acceptance.

Run `powershell -File Tools/Verify-RecipeBrowser.ps1 -Build -OutputDirectory <Windows path>` to rebuild the isolated snapshot and reproduce the checks, timing files and screenshots. This is an explicit fixture; ordinary play receives no test items. No recipe balance, dependency, Editor upgrade, save format or live adjacent-inventory integration changed.
