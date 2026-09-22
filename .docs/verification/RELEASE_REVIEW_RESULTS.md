# Pre-0.1.0 release review — September 20, 2026

**Latest factory measurement:** [September 22 phase profile](FACTORY_PROFILE_RESULTS.md) reruns the full fixture with 27 retail scopes, including the soak and chunk transitions. Its dated results supplement the earlier evidence below.

**The 60/45 FPS release gate is not met.** The non-development player passes the combined factory’s correctness checks, but the large workload still has frame-time tails below the requested floor. The review fixes correctness and scheduling defects; this report does not certify 0.1.0.

The user requires **above 60 FPS normally** and **45 FPS at worst**: 16.67 ms and 22.22 ms per frame. We use p95 below 16.67 ms as the normal-play indicator and count every measured gameplay frame above 22.22 ms as a floor breach. Means alone do not establish either requirement.

## What changed

- [Capability interfaces](../CAPABILITIES.md) replace runtime scans of all 25 shipped item tags with cached typed contracts. Items can expose multiple interfaces. Recipe selectors share the same membership authority; serialized labels, recipes and save fingerprints retain compatibility. A shared receiver contract owns ingredient acceptance, preference and exact-payload transfers.
- [Factory residency](../GAMEPLAY.md#factory-residency-and-player-responsibility) now disconnects an affected component immediately and reconstructs its four channel graphs under a shared frame budget. Unrelated factories retain their published graph identity and continue. Unloaded contents and partial work freeze exactly; players must provide [Chunk Loaders](../BRIDGES.md#chunk-loaders). No implicit factory tickets, reduced-rate simulation or return/offline catch-up was added.
- [Chunk work](../SIMULATION.md#camera-aware-streaming-priority--2026-09-20) prioritizes the immediate neighborhood, then the camera direction using dot products. Every fourth dispatch serves the oldest pending request. This includes pitch, needs no raycast and changes no residency or terrain rules. Worker failures have bounded retries; stale completions cannot overwrite replacement residents or redundantly rebuild a newer synchronous edit.
- Natural crop growth updates authoritative state immediately and queues/coalesces its mesh work. Meshing reuses private scratch buffers, retaining at most three idle builders, 32 MiB each and 64 MiB total; published geometry stays independently owned. The 512-mature-crop check exercises 603,656 vertices and 737,904 indices within that budget. Player placement/mining keeps its immediate path.
- Weather reuses its mesh/arrays and cached shelter columns. Machine/crate views reuse exact state identities, bound cold creation and spread retirement over frames. Seven shared session-owned material variants preserve the existing appearance and remove per-renderer overrides. Diagnostic counters refresh four times per second; normal UI remains responsive to actions. Hotbar digit bindings are cached instead of allocating an array each frame. Repeated terrain-worker diagnostics display once per world/message, preserving the retained error without indefinitely replacing ordinary notifications.
- Windows standalone defaults to D3D11, matching the measured native path and the previously recorded D3D12 shutdown fault workaround. The pinned Unity/URP versions and quality settings are unchanged.
- Inventory transfers share one transaction helper. Right-drag paints compatible input slots, spare armor Shift-click falls back to ordinary storage, and an optional **E inventory / F Interact** preset preserves unrelated/custom controls. [The illustrated guide](../wiki/Building-and-inventory.md) documents the gestures.

Confirmed fixes include starvation from later disjoint residency/configuration invalidations restarting an in-flight reconstruction, obsolete machine visuals surviving replacement during rebuild, pipe ingredient preference, power-demand display surviving unrelated rebuilds, a furnace guide opening a construction use before its operating recipes, a closed tank inlet hiding another open inlet’s capacity, choosing a compatible filled bucket after an incompatible one, repeated synchronous crop meshes and redundant stale-worker retries. Tests also now require the complete pinned historical fixture set and unwind nested verification failures so temporary controls and recorders are restored. Obsolete test fixtures were corrected without weakening save or resource checks.

## Build, PC and workload

The measured PC is an **Intel i7-10750H / RTX 2060 / 32 GB RAM**, using **Unity 6000.4.4f1, URP 17.4.0, D3D11, 1920×1080**, view radius **10**, seed **246813**, VSync off and the ordinary **90 FPS cap**. Quality settings were preserved, including 4× MSAA and four directional-shadow cascades. The ordinary review samples disable diagnostics; a separate diagnostic sample is identified below.

AcceptedFinal assembly SHA-256: `EBBD919AF2CB6D163A4904C83A484C29EABFD332D6AB6E7D91C7BA54C74C2756`. [Build identity](release-review-2026-09-20/AcceptedFinal/build-identity.json) records the executable, time and settings. [Editor evidence](release-review-2026-09-20/AcceptedFinal/Editor/domain-summary.txt) contains **46 passing suites**; the [build](release-review-2026-09-20/AcceptedFinal/Editor/build-summary.txt) has zero errors and warnings. The executable is `Builds/ReleaseReviewRetail/RivetReach.exe`; keep its adjacent files together.

Reproduce the final native sweep from the repository root with a fresh output directory:

```powershell
Tools/Verify-ReleaseReview.ps1 -Executable Builds/ReleaseReviewRetail/RivetReach.exe `
  -OutputDirectory Logs/ReleaseReview/NewRun -Scenarios @('browser','connections','inventory-gestures','weather','release-review') `
  -FrameLimit 90 -GpuTelemetry -TimeoutSeconds 1800
```

The scripted fixture contains **32 working lines, 1,518 industrial assemblies (including routes), 230 stations/crates, 512 crop cells, 64 persistent chickens and 14 hostile mobs**. It exercises every current machine family, boiler/alternator, solar/wind, batteries/banks, a hand crank, tanks, item/liquid/power bridges and an explicit loader-maintained remote circuit. Materials and creature populations are supplied for testing; ordinary simulation and rendering operate during samples. Setup/loading and screenshot capture are outside gameplay timing windows.

The user’s existing uncommitted terrain atlas was preserved. Earlier Development diagnostics used that working atlas. The non-development review builds and final wiki export use an isolated checkout with committed atlas SHA-256 `8ae32ec365732b5a355857c3d4599414445548eb2d44e63f9caca2e9f1763969`. This avoids incorporating unrelated artwork and means these iterations are **not** a controlled before/after performance percentage.

## Native frame times

| Workload | Frames | Median / p95 / p99 / maximum (ms) | Frames above 22.22 ms |
| --- | ---: | --- | ---: |
| terrain-day | 600 | 11.11 / 11.16 / 24.49 / 29.75 | 7 |
| inventory-idle | 600 | 11.11 / 11.15 / 21.35 / 35.24 | 6 |
| terrain-night | 600 | 11.11 / 11.14 / 11.33 / 11.43 | 0 |
| terrain-storm | 600 | 11.11 / 11.15 / 11.28 / 11.61 | 0 |
| factory-192 | 600 | 11.11 / 48.03 / 49.28 / 50.98 | 45 |
| stress-factory-farm-mobs-clear | 1800 | 13.41 / 22.00 / 32.84 / 82.34 | 89 |
| stress-diagnostics | 600 | 13.92 / 22.80 / 68.60 / 81.28 | 35 |
| stress-five-minute-soak | 19476 | 13.63 / 20.68 / 78.41 / 605.93 | 775 |
| stress-factory-farm-mobs-storm | 1800 | 13.79 / 27.04 / 80.84 / 82.79 | 120 |
| stress-inventory | 600 | 14.70 / 41.66 / 87.25 / 90.90 | 74 |
| active-factory-unload-return | 4325 | 11.12 / 18.19 / 60.28 / 83.28 | 143 |
| stress-full-output | 600 | 13.88 / 18.43 / 35.58 / 82.63 | 15 |
| stress-power-shortage | 600 | 11.98 / 18.60 / 67.52 / 68.77 | 17 |
| streaming | 1000 | 11.54 / 57.47 / 65.73 / 74.08 | 80 |

[Final performance JSON](release-review-2026-09-20/AcceptedFinal/release-review/performance.json), [all 33,801 frames, gzip](release-review-2026-09-20/AcceptedFinal/release-review/frames.csv.gz) and [all 1,406 floor breaches](release-review-2026-09-20/AcceptedFinal/release-review/floor-breaches.csv) preserve the measured distributions. Ordinary terrain samples have p95 near 90 FPS; day and inventory still contain isolated breaches. The large factory, smaller factory and streaming miss acceptance. The previous [RetailFinal measurements](release-review-2026-09-20/RetailFinal/release-review/performance.json) retain their own [build identity](release-review-2026-09-20/RetailFinal/build-identity.json); thermally varying runs do not establish a controlled optimization percentage.

[NVIDIA telemetry](release-review-2026-09-20/AcceptedFinal/release-review/gpu-telemetry.csv) records thermal throttling and clocks as low as 300 MHz. Timestamps use the offset in build identity. This is a measured constraint, not proof that every spike is thermal. Returned CPU/GPU timings arrive late; CSV `timing_timestamp` identifies that returned sample rather than the current gameplay frame. The clear factory reports median GPU 12.43 ms, CPU main 5.14 ms and render thread 3.66 ms; those durations overlap and must not be added.

The largest measured stall was **605.93 ms** during the soak. Nearby returned timing samples report GPU 606.15 ms and present wait 599.09 ms, with zero machine/crate view creations. [Timing notes](release-review-2026-09-20/AcceptedFinal/release-review/timing-notes.txt) retain the exact neighboring rows. This points toward a rendering/presentation stall but does not identify its root cause or excuse it from acceptance. Retail scope/allocation counters unavailable in this build are **−1**, not zero allocation. Editor allocation checks positively calibrate their fallback recorder before claiming a warmed query allocates nothing.

An isolated explicit route-instancing experiment was **rejected**. The 32 lines contain 904 route objects; with the annex, the experiment submitted 914 visible instances in 133 batches. Three alternating 600-frame pairs reduced SRP draw submissions from roughly 7,000 to 5,200, but p95 off/on was **23.88/27.08, 20.06/23.59 and 80.46/25.31 ms**. The first two pairs worsened and the third was confounded by thermal slowdown. Its [2,321 correctness checks](release-review-2026-09-20/RouteExperiment/release-review/runtime-report.json) passed, including frozen resource/transform and actual origin-shift checks; [pixel comparison](release-review-2026-09-20/RouteExperiment/release-review/pixel-comparison.txt) did not establish exact visual equivalence. Lower draw counts alone did not justify the extra renderer/lifecycle code, so it is absent from the accepted source. [Full experimental timings](release-review-2026-09-20/RouteExperiment/release-review/performance.json) remain dated evidence, not final-build measurements.

The smaller shader correction replaces a duplicated Forward material-buffer layout with the installed URP declaration shared by its shadow/depth passes. The pinned Editor’s [actual compatibility metadata](release-review-2026-09-20/AcceptedFinal/Editor/worldlit-batching-checks.txt) reports code 0 (compatible) for the authored and normal/cutout variants after binding all four passes. Vertex/fragment lighting calculations and rendering quality settings remain unchanged. Metadata alone does not establish a frame-rate gain; final native timing and captures are separate evidence.

## Correctness and interaction evidence

[The combined native report](release-review-2026-09-20/AcceptedFinal/release-review/runtime-report.json) passes **2,263 checks**: real production and conservation, bridge transfers, dormant freeze, loader-backed operation, unload/return, output-full/power-shortage cases, bounded presentation work and actual shared material usage. Machine views recorded 1,684 reuses and crate views 440. The temporary machine retirement queue peaked at 875 and drained; the 512/256 inactive-cache caps do not cap that separate retirement backlog or total session memory.

[The full stress checkpoint](release-review-2026-09-20/AcceptedFinal/release-review/stress-save.txt) contains **1,518 assemblies and 64 animals**, and reloads byte-for-byte before simulation resumes. Paused save took **67.30 ms**; the synchronous load transaction took **1,001.63 ms**, excluding subsequent streaming. These are reported separately from gameplay frame tails. An earlier Development iteration accidentally saved an empty replacement session; that result was superseded by the explicit population guard and correctly ordered checkpoint.

Final native checks pass browser **927**, connections **408**, inventory gestures **31** and weather **73**. The browser check covers actual pointer gestures, station recipes/fuels, Back history, screen reuse and alternate resolutions. Old torch/young-crop/renamed-power-button fixture assumptions were corrected without weakening transactions; the corrected compost test passed **154** checks in its dated RouteExperiment build.

Earlier RetailFinal checks retain all 17 historical schemas **87**, Alpha interactions **87**, beds **237**, fishing **453**, lighting **722** and food/Survival **391**. The 15-minute food cadence finished with 12 food, zero reserve and 1,078.56 metres walked without another meal. Earlier dated Functional1 reports retain successful bridges 422, chickens 321, crafting 38, crates 137, fluids 36, industry 89, inventory 107, multiblocks 140, portable storage 180, renewables 147, saves 183 and tools 252 checks; Functional2 farming passed 190. The earlier five-minute empty-handed Survival route passed seven checks. Each artifact identifies its own assembly; earlier focused checks are not relabeled as AcceptedFinal.

The corrected [UI probe version 3](release-review-2026-09-20/AcceptedFinal/release-review/interactions/performance.json) passes its 13 focused checks, reports Windows Release correctly and uses −1 for unsupported allocation measurements. It calls the current recipe browser directly. Action p95 was inventory open **7.62 ms**, craft **0.044 ms** and recipe detail **0.771 ms**. These action measurements are separate from full-frame FPS. The older RetailFinal subprobe's incorrect Development label and unsupported zero-allocation values do not establish allocation-free UI.

## Visuals, documentation and remaining acceptance

[Four direct game captures](../wiki/Release-review-gallery.md) provide the requested blog material: full factory overview, moonlit production, storm operation and farming/mobs. The overview and dusk view are the recommended lead images. The four final-build captures were visually reviewed for materials, routes, shadows, weather and crop/mob presentation; this is visual inspection, not pixel-perfect equivalence. Actual Controls capture verifies the optional preset’s layout. No generated/composited artwork or reduced quality setting was used. The normal 64-block machine-view limit remains visible from distant farm/storm angles; these views do not imply every assembly is rendered at every distance.

The refreshed catalog contains 195 items and 199 recipes. Canonical clean-source wiki validation passes **231 pages and 9,938 links/images**. All 696 exported source fingerprints also match the staged Git blobs, including the committed LFS asset identities. [Publication run 35482166070](https://github.com/Starbugstone/Rivet-Reach/actions/runs/35482166070) succeeded, publishing wiki commit `7d6afe9` from source commit `1b3a1a9`. Live Chromium review verified the gallery’s four full-resolution images, all six inventory-guide images, the updated loader responsibility text and 195 loaded/linked item icons. Clicking Iron pickaxe and its Iron ingot ingredient reached the correct pages; the browser reported no console errors. Eleven affected published pages/images match the reviewed publisher output byte-for-byte. [Deployment evidence](release-review-2026-09-20/AcceptedFinal/wiki-verification.json) records the result. The main workspace’s preserved terrain edit intentionally differs from the export fingerprint; validation uses the clean review checkout and committed artwork.

**Remaining release acceptance:** resolve the large-factory and streaming frame tails, including the unexplained rendering/presentation stall, without reducing gameplay/fidelity; obtain accepted sustained frame-time evidence. The five-minute soak and scripted pointer tests reduce regression risk; they do not establish multi-hour stability or every player’s subjective familiarity. No version tag or 0.1.0 release has been published.
