# Performance pass — 2026-09-10

The user reported crafting lag in the Unity Editor and requested a full runtime pass without gameplay changes. This pass optimizes presentation and streaming work, and protects live Editor sessions from script reload corruption. Recipes, item transactions, combat, movement, view distance, geometry, lighting quality and simulation rates/budgets are preserved. Concurrent cursor-crafting and recipe-browser changes belong to their own feature work.

## Measured costs

Measured on Unity 6000.4.4f1, i7-10750H, RTX 2060, 32 GB RAM, seed 246813, view radius 10. Editor captures render at 2560×1440. The workloads begin after initial residency settles; timing an action excludes the following frame's canvas/render work. Ten screen/portrait samples, 32 craft attempts and 12 place/remove pairs are recorded. Craft attempts deliberately fill the cursor and then verify that rejected crafts preserve ingredients.

[Original Editor measurements](performance/baseline-editor.json) predate the concurrent browser migration. [Current Editor measurements](performance/editor-performance.json) include that browser, so whole-screen/frame comparisons include its extra UI and are **not an isolated measure of the optimizations**. Other development activity can affect timing. The original `inventoryObjects` field counts the session hierarchy and is not a valid UI-object measurement. Zero allocation counters in these Editor action samples are unvalidated and must not be interpreted as allocation-free behavior.

| Editor action | Original median | Current median |
| --- | ---: | ---: |
| Play → inventory rebuild sequence | 107.656 ms | 60.699 ms |
| Unchanged portrait refresh | 8.005 ms | 0.001 ms |
| Craft click, including full-cursor attempts | 0.139 ms | 0.037 ms |
| Place and remove the same block | 16.633 ms | 5.909 ms |

The current inventory includes the separately authored browser. An earlier integrated run with the same optimization set measured 55.682–82.118 ms for the rebuild sequence, illustrating timing variation under concurrent development activity. Portrait reuse and exact mesh equivalence have focused checks; whole-game FPS improvement is not established by these short samples.

The [isolated mesher comparison](performance/mesh-before-after.txt), using the pinned Mono runtime, compares the original and optimized implementations in one process. All seven fixtures have exactly matching hashes for vertex positions, normals, winding, UVs, terrain tiles, fluid colors and scheduled fluid cells. Representative results:

| Mesh workload | Original | Optimized |
| --- | ---: | ---: |
| Surface chunk | 4.929 ms | 2.270 ms |
| Cave chunk | 3.595 ms | 2.253 ms |
| Fully solid page | 6.278 ms | 1.777 ms |
| Water surface page | 2.770 ms | 2.443 ms |
| Deliberately noisy worst-case fixture | 42.677 ms | 40.190 ms |

These are local meshing costs, not frames or universal hardware guarantees. [Golden geometry regression](performance/mesh-regression.txt) checks the retained fixture hashes in Unity and also checks native fluid Mesh reuse.

## Full runtime review

| Area | Finding and action |
| --- | --- |
| Crafting and inventory | Recipe matching already uses exact indexed lookups and revision caching. Reuse unchanged portrait models/graphs; update only changed slot visuals; construct screens under an inactive root and enable once complete. The browser's moving cursor has a child Canvas. |
| Terrain streaming | Avoid rebuilding unchanged demand, sorting all resident chunks for two workers, and temporary removal arrays. New surface bounds, observer chunk and view-distance changes still update demand. |
| Block edits and trees | Keep immediate collision and synchronous local mesh publication. Replace repeated solid/industry predicates in the mesher with an immutable lookup table. Limit immediate edit lookup to neighboring chunks. |
| Background remeshing | Clone current resident cells/halos instead of regenerating terrain for fluid/grass edits. Keep immutable worker inputs, residency tokens and revision rejection. |
| Unity terrain/fluid presentation | Empty pages keep voxel data without empty renderers or native meshes. Reuse visible native meshes; remove the temporary index array formerly allocated for every fluid quad. |
| Multiblock presentation | Replace per-shell string/array geometry keys with exact packed bit fields and cache the status renderer. Shared storage and validation/transfer rules are unchanged. |
| Industry networks | Existing logical networks cache topology and bound rebuild work. No simulation-rule change. Large factories undergoing unrelated chunk residency changes remain a follow-up profiling target because residency can invalidate topology/structure work. |
| Mobs and drops | Existing bounded navigation, sleeping/distance behavior and spatial drop merging were reviewed. No AI, spawn, pickup, physics or population changes. |
| Furnaces, crops, grass and fluids | Existing scheduled/sleeping work and fixed-step rules are retained. Their presentation benefits from the remeshing changes. |
| Avatars, lighting, audio and sky | Retain authored models, animations, shadows, textures and light limits. No quality reduction or engine/package change. Portrait reuse removes unnecessary model reconstruction. |

## Editor errors

The inspected Editor log contained thousands of NullReferenceExceptions in `GameUI.Update`, `IndustryPresentation.Update` and `MultiblockPresentation.Update`, beginning directly after a live assembly reload. Their runtime session authorities are non-serialized and cannot survive that reload. A clean Play session did not reproduce the exceptions.

`PlaySessionReloadGuard` now holds a balanced reload lock only during live Play. Pending scripts apply after Stop; normal entry/exit domain reloads remain enabled. It preserves the running session instead of resetting the world or hiding null references. [Unity setup](../UNITY_SETUP.md#script-changes-during-a-live-play-session) owns the workflow. Compilation and reload can both defer while this lock is held, so the regression requests compilation, checks live state for 60 frames, then verifies that Stop releases the lock.

## Verification and remaining limits

The [final Editor run](performance/editor-result.txt) passes all 13 assertions without runtime errors, and the [reload regression](performance/reload-check.txt) passes during the same run. The [crafting capture](performance-editor-2026-09-10.png) was visually inspected. The Editor gameplay probe verifies cursor capacity, exact crafting output, recipe display, immediate edits, settled residency, resident remeshing without regeneration, a newer edit superseding an in-flight worker, and valid animation graphs. [Domain checks](performance/domain-checks.txt) pass 92,348 assertions, and [crafting checks](performance/crafting-checks.txt) pass 1,158,808 assertions. [Industry](performance/industry-checks.txt), [multiblock](performance/multiblock-checks.txt), [fluid](performance/fluid-checks.txt) and golden mesh checks also pass.

The [Windows Development build](performance/build-summary.txt) completed with zero errors and warnings. [Artifact hashes](performance/build-identity.json) identify `Builds/Performance/RivetReach.exe` and its assemblies from the integrated workspace, including the concurrent recipe browser. Focused standalone runtime results are recorded below. The base traversal fixture was corrected to reset motion at its artificial teleports: retaining fall velocity/distance had killed the fixture player before its remine assertion. It now asserts survival and aims from the actual camera; production damage and camera rules are untouched. The combined movement fixture also waits for residency after teleporting from an underground scene into sky pages; unloaded cells correctly remain solid until ready.

| Final Windows scenario | Result |
| --- | --- |
| [Full gameplay](performance/gameplay-runtime.json): traversal, origin shifts, edits, avatars, inventory, caves, grass, drops and movement | PASS — 265 assertions |
| [Crafting](performance/crafting-runtime.json): pointer input, batching and full inventory conservation | PASS — 38 assertions |
| [Fluids](performance/fluid-runtime.json): buckets, flow, renewal, swimming and residency | PASS — 36 assertions |
| [Multiblocks](performance/multiblock-runtime.json): connected shells, exact shared storage, breach/repair, ports and residency | PASS — 140 assertions |

All four runs finished with empty runtime error lists. These functional runs are at 1280×720; their frame counters are diagnostic samples, not controlled before/after FPS benchmarks. The connected tank capture was also visually inspected.

Opening an entire inventory still creates a substantial UI hierarchy; retaining complete screens/pooling more widgets remains a measured follow-up target. The noisy mesh fixture also shows that exposed-face count dominates highly fragmented pages. GPU profiling, sustained traversal, very large factories and long-session allocation behavior are not certified by this pass. No claim is made that all lag has been eliminated.

## Reproduce

Open the saved Main scene in the pinned Editor, wait for script import, then use **Rivet Reach → Verify performance**. This explicitly starts and stops a fixture session and writes `Logs/Performance/Manual`. To request only the short reload regression, write `Logs/Performance/Reload` to `Logs/performance-request.txt`; `checks` runs the domain/mesh suites and `build` builds the separate `Builds/Performance/RivetReach.exe` artifact. Requests run only while the Editor is idle and out of Play; import changed scripts before requesting a run.

`Tools/Verify-Performance.ps1 -Build -Scenario Gameplay` builds and runs the base runtime checks. Other scenarios are `Crafting`, `Fluids`, `Multiblocks` and the focused `Movement` regression. Fixtures are verification-only; ordinary sessions still start empty-handed.
