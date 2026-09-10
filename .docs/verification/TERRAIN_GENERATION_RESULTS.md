# Terrain generation verification — 2026-09-09

The user requested caves, varied terrain and biomes. [TERRAIN_GENERATION.md](../TERRAIN_GENERATION.md) owns the working profile, material identities and limitations. This record separates source/generator checks from Unity rendering and gameplay evidence.

## Completed generator checks

`Tools/Check-Terrain.ps1` passed **37,952 assertions**, compiling the actual repository source with the pinned Unity 6000.4.4f1 Editor’s Mono compiler/runtime. Four seeds were sampled: **246813, 777, −917 and −2147483648**. This runner does not launch Unity or verify rendering.

| Seed | Sampled height range | Grassland | Forest | Dunes | Badlands | Alpine |
| --- | --- | --- | --- | --- | --- | --- |
| 246813 | 28–208 | 2,268 | 2,295 | 1,234 | 821 | 2,791 |
| 777 | 29–191 | 1,365 | 2,045 | 2,412 | 1,930 | 1,657 |
| −917 | 29–187 | 2,383 | 1,324 | 2,563 | 1,137 | 2,002 |
| −2147483648 | 30–189 | 2,456 | 1,841 | 1,804 | 2,235 | 1,073 |

Each biome count comes from a 97 × 97 column sample, X/Z −768 through 768 at sixteen-block intervals. The combined samples contain **1,227 surface entrance cells**. Counts describe sampled cells, not distinct entrances or biome regions. Adjacent one-block height differences were checked at the sampled coordinates to catch abrupt biome walls. All sampled seeds have a supported, clear temperate spawn and nearby wood.

Generation checks also cover:

- Point queries against generated pages and every shared halo face on all three axes, at surfaces, underground, the bedrock floor, negative coordinates and near-billion-block coordinates.
- Identical output after reverse-order generation, concurrent workers and cache eviction.
- Worker surface extrema covering terrain and canopy heights.
- Trees rooted in supported grass; sparse mature wild potatoes matching both point and worker results, with spawn exclusion and clearance.
- Every ore type remains discoverable after carving. Sampled ore remains within its authored Y band and replaces only stone; cave air, biome soil and bedrock remain excluded. Bedrock is protected at/below −256.
- A supported natural deep-cave standing volume and a 64³ underground flood-fill sample. Its largest face-connected air component contains **114,044 cells**, spanning **63 vertical blocks**. This demonstrates local connectivity across vertical chunks; it does not prove all caves connect to the surface.

The [captured generator report](terrain-generation-checks.txt) records **13.902 ms median / 29.854 ms maximum** page-generation time for this focused sample; they include terrain, caves, ores and vegetation and exclude meshing. The machine was also running other feature imports/builds, so these observations are not an isolated performance comparison or a gameplay frame-time claim. Current runtime and Editor source also passed a separate Mono compilation preflight with Unity’s native source generators omitted; that is syntax/type checking, not a Unity import/build.

## Completed Unity checks

The coordinated Windows Development build passed with **zero errors and zero warnings** in Unity **6000.4.4f1**. The build owner reported 32.921 seconds for the warm build. The [integrated Unity domain suite](terrain-domain-checks.txt) passed **92,338 assertions**, including terrain, ore, tree, grass and inventory coverage. The [Unity generator sample](terrain-unity-generation-checks.txt) measured **6.301 ms median / 10.606 ms maximum** page generation, excluding meshing. These supersede the earlier pre-streaming-fix integrated report.

The [final terrain runtime report](terrain-runtime-report.json) passed **60 assertions with zero errors** at **2026-09-09 06:49:30 UTC**, using `-rr-verify -rr-terrain-review`. It verified all four imported biome materials, natural spawn support, nearby full surface/canopy residency in each biome, a supported deep cave, loaded geometry 80 blocks above and below it, three metres of collision-checked cave movement, and actual targeting/mining with exactly one drop for each new block. A placed clay block survived unloading, regeneration and an origin shift. Bedrock rejected removal and mining after carving.

The playable full folder is **`Builds/Terrain`**, copied from the checked `Rivet-Reach-MobVerify/Builds/Mobs` artifact. Its `RivetReach_Data/Managed/Assembly-CSharp.dll` SHA256 is **`c1b6e67d1adb7ed02bc438600c7ca2ea7607d330d7b33d6a7055abd6b4494d2d`**. [Build context and matched terrain inputs](terrain-build-context.json) identify the actual binary/source boundary. This integrated artifact includes independently owned survival, day/night and unpublished mob code/assets; natural mobs are disabled by verification mode. The joint terrain/survival commit excludes mob implementation, so the runtime evidence is not an exact-commit build claim. Replaying `Tools/Verify-Terrain.ps1` uses the preserved full folder; `-Build` prepares a new artifact from the then-current checkout.

The run used an Intel i7-10750H, RTX 2060 and 1280 × 720 window. It first drained the default radius-10 demand, then reviewed terrain at radius 6 with fog from 134.4 to 176 blocks. **39.798 seconds** is the verification startup-to-full-demand-drain interval, including startup/seed gestures; it is not time to the first playable local region. Peak residency was **3,301 chunks**, including that radius-10 startup. The final bedrock view retained **915,548 terrain mesh triangles**, not a frame-visible or peak triangle measurement. Frame-time, allocation and draw-call sampling were not enabled by this focused scenario; zero/default fields in its shared report are not measurements. The deeper view volume increases residency and preparation work and still needs an isolated frame-time/memory profile.

## Visual review

All eleven final ground/survey/cave captures were inspected. [Natural review coordinates](terrain-sites.txt) use seed **246813**: grassland (0,50,0), forest (96,70,32), dunes (−272,60,−96), badlands (32,74,−96), alpine (−240,160,16), cave (−23,−123,−26). Test-only teleports and elevated cameras inspect the generated world; ordinary gameplay receives neither flight nor terrain fixtures.

| View | Observed result |
| --- | --- |
| [Grassland survey](terrain-survey-grassland.png), [ground](terrain-ground-grassland.png) | Rolling slopes, supported broadleaf trees and visible ground cover |
| [Forest survey](terrain-survey-forest.png), [ground](terrain-ground-forest.png) | Stronger relief and a denser canopy, with nearby terrain/treetops present |
| [Dunes survey](terrain-survey-desert.png), [ground](terrain-ground-desert.png) | Sandy ridges and sandstone faces alongside a blended alpine foothill |
| [Badlands survey](terrain-survey-badlands.png), [ground](terrain-ground-badlands.png) | Red-clay terraces and mesas adjoining temperate terrain |
| [Alpine survey](terrain-survey-alpine.png), [ground](terrain-ground-alpine.png) | Steep snowy ridges and foothills under distance fog; ground capture is a close material view |
| [Corrected cavern](terrain-cave.png) | Continuous deeper walls/floor and exposed ore across multiple vertical chunks |

The current cave capture and readiness checks cover the corrected bounded vertical sight demand. They do not prove that every possible vista is free of streaming defects. Cave lighting still uses the existing ambient/fog system; this extension adds no voxel light propagation.

## Review limits

Biome scale, entrance frequency, cave navigation, exposed ore availability and traversal feel remain user play-review questions. The new generator changes same-seed terrain; durable saves/migration remain unimplemented. There are no generated structures or water in this terrain change. Other sessions' day/night, survival and mob evidence remains in their own reports.

## Subsequent fluid generator

The authorized sea/river extension advances generation to `terrain-5-seas-rivers`. [Fluid verification](FLUID_RESULTS.md) records the new terrain/domain checks and sea/river player views. The focused land/cave screenshots and timings above retain their original build identity.
