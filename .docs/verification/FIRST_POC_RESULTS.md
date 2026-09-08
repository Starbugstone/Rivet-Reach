# First POC verification — 2026-09-08

The terrain/FPS/inventory slice is implemented as a **playable review candidate**. The Windows build and checks below passed. User acceptance of movement feel, the initial player art and the next milestone remains pending. [FIRST_POC.md](../FIRST_POC.md) contains controls, architecture details, asset provenance and reproduction commands.

## Build and test evidence

- Unity **6000.4.4f1**, URP **17.4.0**, Windows x64, Mono development build; no Editor upgrade. The final project build completed with **0 errors and 0 warnings**.
- [Domain checks](domain-checks.txt): **22,371 assertions passed**, including signed/large coordinate round trips, reproducible generator output, both sides of negative-coordinate chunk halos, greedy-mesh winding/occlusion, inventory overflow/split/transfer and 10,000 randomized stack operations with quantity/limit invariants.
- [Standalone runtime report](runtime-report.json): **40 checks passed**, exit code 0, no captured runtime errors/exceptions. These include prepared spawn/headroom, positive and negative unready-frontier collision, mapped walk/jump/crouch/inventory input, held fist mining, exact item yield, seam edits, physical merge overflow, partial/full pickup, unload/reload state, floating origin, appearance switching and underground cave collision.
- Separate native Windows input smoke check: clicked **Start Expedition**, held the mouse to mine one grass block, collected it, opened inventory with Tab and moved that item from hotbar to main storage by clicking the slots. This ordinary session used no test-mode inventory fixtures.
- Visually inspected actual build screenshots for terrain, first-person hands, caves, inventory, both player variants and skins, settings and controls. Fixed the observed slider overflow, overlapping hotbar labels, excess first-person torso visibility and the initially weak waistcoat/hair presentation.
- [Clean source-copy import and build](clean-build.txt): started the pinned Editor against a separate project directory containing Assets, Packages and ProjectSettings, with no Library/Temp/build cache copied. Import, domain checks and a new Windows build passed with exit code 0; this checks independence from the working Editor/cache, not remote-clone hydration.
- Blender source files were reopened and their skin image references changed to verified project-relative paths; the runtime FBX geometry was unchanged.
- Runtime assets all have Unity metadata. Binary sources/exports/screenshots use Git LFS; generated caches, logs, builds and performance-package metadata stay outside version control.

The verified game's managed assembly fingerprint is:

```text
RivetReach_Data/Managed/Assembly-CSharp.dll
SHA256 c32ce08a2e822df1d076be5246df7a297755632db66cf54524fe9ecf471c7cbd
```

## Measured workload and limits

Hardware: **Intel Core i7-10750H**, **NVIDIA RTX 2060**, **32,553 MB reported system memory**. Windows player at **1280×720**, FOV **78°**, view radius **4 chunks**, two terrain workers, vsync off and a **90 FPS cap**. The main project's Unity Editor remained open; a separate clean import was started only after the timing run ended.

Seed **246813**, generator **terrain-1**, world **surface**. The route begins around `(0,83,0)`, edits both sides of the x=31/32 seam, relocates to x=640 to force unloading/origin movement, then samples a controlled 180 m path through x=819 in one-metre steps with at least 25 ms between steps. It returns to the edited area and later enters a generated cave around `(-32,24,-32)`. Relocation and controlled flight are explicit test fixtures, not player abilities. This is a short streaming sample, not an uninterrupted 819 m walk or a long-duration memory soak.

| Measurement | Final observed result |
|---|---:|
| Initial full nearby demand drain, including the test's settling delay | 1.97 s |
| Streaming sample median frame | 11.11 ms |
| Streaming sample p95 frame | 11.15 ms |
| Streaming sample maximum frame | 11.59 ms |
| Fist-mining local remesh case | 9.03 ms |
| Maximum frame during that mining sample | 16.81 ms |
| Peak resident chunks across the scenario | 203 |
| Terrain triangles at the end of sampled flight | 133,524 |
| Peak Unity allocated memory during sampled flight | 127,564,028 bytes (~122 MiB) |

The frame sample stays near the configured cap; it does **not** measure uncapped GPU throughput. Unity allocated memory is not total Windows working set or driver memory. The draw-call recorder was unavailable in this runner and is recorded as **−1**; separate CPU/GPU timings and a profiler draw-call capture remain outstanding. No smooth-performance claim is made for other hardware, denser edits, large item populations, multiplayer or factories.

The first mining implementation measured 55.28 ms for its local mesh rebuild. Replacing per-cell coordinate-array copying with direct stride indexing reduced the final measured case to 9.03 ms. Mining still uses a synchronous bounded local rebuild; the observed 16.81 ms mining frame slightly exceeds the provisional 16.67 ms frame target. More complex edit patterns may need the preferred immediate changed-cell patch plus asynchronous rebuild. The streaming upload budget is checked between meshes and is not a hard ceiling for a single upload.

## Imported character cost

| Geometry | Triangles | Rig / material |
|---|---:|---|
| Full male body | 1,920 | 17 bones, 1 material/submesh |
| Full female body including longer hair | 2,180 | 17 bones, 1 material/submesh |
| Selected first-person arms/hands | 616 | Derived from the selected body/skin |
| Male body visible in first person | 552 | Upper torso, head and duplicate arms excluded |

These imported geometry counts meet the provisional 5,000-body / 1,500-arms review budgets. They do not establish a multiplayer crowd or GPU budget. The portrait renders only while the inventory/appearance screen is open. Both body choices use the same movement/collision/mining dimensions and both test skins; the selected appearance persists locally.

The current animation uses rigid part weights and a small procedural pose system. Shoulder/elbow articulation, the neutral garment silhouette and the reusable skin layout still need user art review. The skin atlas reuses regions across multiple faces, so it is not yet a detailed front/back painting template or a custom-file import feature.

## Actual screenshots

These are rendered POC captures, separate from the earlier concept paintings. Inventory/appearance captures in test mode contain declared test quantities.

- [Title / normal start](10-title.png)
- [Surface and first-person fists](01-world.png)
- [After fist mining](02-mining.png)
- [Inventory and inactive crafting area](03-inventory.png)
- [Male / Field skin](04-male.png), [male / Ochre skin](04b-male-alternate.png)
- [Female / Field skin](05-female.png), [female / Ochre skin](06-alternate-skin.png)
- [Settings](07-settings.png), [rebindable controls](08-controls.png)
- [Generated underground cave](09-cave.png)

## Remaining review

World/inventory state resets on quit; durable saves are intentionally outside this slice. The build has no structures, placement, functioning recipes, tools, water, vegetation, machines or mobs. Terrain shading uses simple directional/ambient light and haze; caves are readable but do not yet have occluded/propagated voxel lighting. Player poses and skin painting support are initial implementations, not final production art.

The next decision is the user's review of this playable slice. Refine the terrain, controls or visuals from that feedback before selecting another milestone. The later roadmap remains a set of candidates.
