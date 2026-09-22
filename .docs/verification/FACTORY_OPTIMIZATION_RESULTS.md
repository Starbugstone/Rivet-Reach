# Factory optimization results — 2026-09-22

Status: implemented and verified for correctness; **the 45 FPS worst-frame target still fails**. This follows the [measured isolation tests](FACTORY_ISOLATION_RESULTS.md); earlier captures are baseline evidence, not results from this build.

## Implemented changes

- Terrain and fluid mesh workers publish a single packed vertex payload and its bounds. The main thread submits complete vertex/index buffers, preserving Float32 attributes, UInt32 indices, mesh reuse and revision/residency publication. It no longer assigns each channel separately or recalculates bounds. Held/dropped terrain geometry and crop icon consumers use the same representation.
- Power topology builds role-specific endpoint lists and eligible allocation-phase lists once per published snapshot. Per-tick allocation skips impossible transfer groups while keeping all-group demand/display resets, live activity, shared multi-face budgets, both charging passes and the existing fairness order. Partial and cancelled rebuilds retain atomic publication.
- Ranged pumps discover an optional resident-cell reader once when the simulation is created. The native world supplies its existing combined residency/read operation, avoiding repeated dictionary lookups per scanned cell. There is no stale cross-tick source cache: scan order, 256-cell budget, cooldown, target revalidation, source competition and dormant boundaries remain authoritative.
- The project PBR entry point shares one surface-light sample between directional and ambient lighting. World models skip the separate fog-field sample only when fog weight is zero. Fog uses its original sampling offset wherever it contributes. Geometry, resolution, anti-aliasing, shadows, material response and simulation rates remain unchanged.

## Validation

The Editor gate includes existing geometry golden hashes, native packed-versus-legacy mesh readback, empty/small/large reused meshes, water/lava/crops, exact pump search traces, and randomized full-state power comparisons against the previous allocation algorithm. Shader reference assets are generated from repository commit `3034df3` only in an isolated verification project; they are not maintained production assets. Pixel comparisons and paired native GPU timings are recorded separately from gameplay performance.

Native runs use the same i7-10750H / RTX 2060 / 32 GB laptop, Unity 6000.4.4f1 non-Development player, D3D11, 1920×1080, view distance 10, 90 FPS cap, VSync off, 4× MSAA and four 160 m shadow cascades. Preserve the >60 FPS normal / 45 FPS worst target and report every floor breach. CPU scopes are inclusive wall latency, not additive costs or guaranteed CPU execution time. GPU/CPU samples can be delayed; compare distributions and thermal telemetry.

The first native run passes **2,275 correctness assertions** and exits 0. It records **17,542 live gameplay frames**, including **636 over 22.22 ms**, with a worst live frame of **74.485 ms**. The **7,200 shader-control frames** are reported separately and are not gameplay acceptance samples. The first run still fails the 45 FPS floor.

## Native CPU comparison

The 45.007-second live sample contains 900 factory ticks and 28,800 ranged-pump calls (32 per tick). Compared with the preceding Native2 isolation run:

| Inclusive phase | Before, ms/tick | After, ms/tick | Observed reduction |
| --- | ---: | ---: | ---: |
| Entire factory | 4.1442 | 2.4586 | 40.7% |
| Power allocation | 1.3898 | 0.4119 | 70.4% |
| Ranged-pump preparation | 1.2184 | 0.5587 | 54.1% |
| Both storage charging passes | 0.5585 | 0.0328 | 94.1% |

These nested rows must not be added. They measure this fixture and hardware; the surrounding frame-count stages take less time at higher FPS, so preceding production duration and thermal history are not perfectly matched. The independent clear-factory window shows the same direction (38.2% less total factory time, 69.4% less power, 52.9% less ranged-pump preparation). No source-index cache or changed scan timing is needed for this improvement.

The independent five-minute factory soak passes **2,263 correctness assertions** across its complete release-review scenario. Across all stages it records **33,837 frames**, **2,238 floor breaches** and a **359.012 ms** worst frame. In the timed soak itself, 6,000 factory ticks average **2.6063 ms/tick**, versus **4.1399 ms/tick** in the earlier full-profile baseline: **37.0% less factory time**. Power averages **0.3970 versus 1.3419 ms/tick**, a 70.4% reduction. Item/fluid transport costs remain broadly unchanged.

| Five-minute soak | Earlier full profile | Optimized |
| --- | ---: | ---: |
| Frames | 16,534 | 19,451 |
| Mean frame, ms | 18.145 | 15.424 |
| Median frame, ms | 13.827 | 11.607 |
| p95 frame, ms | 64.017 | 60.997 |
| p99 frame, ms | 81.183 | 66.053 |
| Worst frame, ms | 100.235 | 359.012 |
| Frames above 22.22 ms | 1,520 (9.19%) | 1,411 (7.25%) |

The lower mean is useful, but neither the p95 nor the worst frame passes the release target. These are sequential laptop runs, with uncontrolled thermal and scheduling history, not a locked-frequency benchmark.

## Paired shader result

Two old/current/old 1,200-frame brackets give median GPU times **11.82 / 10.07 / 11.77 ms** and **11.97 / 10.49 / 12.50 ms**. The current shader reduces median GPU time by **14.7% and 14.3%** relative to the mean of each pair of historical baselines. Baseline drift is **0.4% and 4.3%**. These compare the real material replacement, with identical visible geometry and quality settings; they are distinct from the earlier hide-machinery experiment. The staged reference includes all 22 authored WorldLit material variants to preserve local shader keywords through player stripping. Swapping shaders explicitly restores the original keyword arrays.

The 34 deterministic Editor comparisons across WorldLit, held blocks/tools and explorer skin are pixel-identical in the captured RGB output. Cases include outdoor/day, dark cave/night, mixed sky/block light, held fill, partial/full/no fog, disabled field previews, normal mapping/emission and transparent WorldLit blending. These finite fixtures do not prove every possible image, camera or backend. Native screenshots provide separate scene evidence.

SRP draw counts remain exactly **7,046** in every bracket. Thermal slowdown remains active. Longer brackets improve telemetry coverage but do not lock GPU clocks; long-frame tails still occur, including during optimized shader windows. A lower median GPU time is not a passing 45 FPS floor.

## Chunk submission result

In the first optimized unload/return run, peak terrain submission is **5.207 ms across two calls** in one observation, within **5.853 ms** total application. The corresponding previous peaks were 33.962 ms in one call and 35.653 ms across two. Mean terrain submission falls from **0.066–0.073 to 0.026 ms/call**; fluid submission falls from approximately **0.010 to 0.0048 ms/call**. These observation maxima are not individual-call percentiles.

Activation stays near **0.333 ms/call**, and completed terrain-worker latency averages approximately **8.76 ms/job**, close to the previous 8.72–8.74 ms. The optimized timed 60-second unload/return window applies **5,314 chunks**, compared with 4,991/4,999 previously, and ends with no terrain backlog. Lighting still has **1,758 pending pages**. The larger completed-worker observation at the mesh peak is parallel latency, not main-thread execution.

The separate streaming stage measures 1,000 frames while moving at 8 m/s. Faster frames shorten its duration and distance: 12.514 seconds versus 14.688–15.243 seconds. Its remaining 89 terrain pages therefore cannot establish either a throughput regression or an eliminated backlog; a fixed-distance/time comparison is needed for that claim.

The independent full release-review unload window peaks at **5.577 ms** terrain submission, with **0.0260 ms/call** mean. Its fluid submission averages **0.0050 ms/call**. This supports the first run's improvement without establishing a universal maximum.

## Flowing-liquid regression

The real water/lava scenario passes **53 assertions**, exits 0 and reports no errors. Its existing fixture uses **view distance 4**, so it is not directly comparable to the view-10 factory. Two 48×24×18 basins contain 16 elevated sources each. Source counts remain exact; paused unload/return restores sources and derived flows byte-for-byte; removing all sources drains every dependent flow. The final fluid and mesh queues reach zero.

| Stage | Frames | p95, ms | Worst, ms | Above 22.22 ms |
| --- | ---: | ---: | ---: | ---: |
| empty-basins | 451 | 11.114 | 11.396 | 0 |
| water-cascades | 2,241 | 11.156 | 33.703 | 4 |
| water-and-lava-cascades | 2,248 | 11.182 | 12.621 | 0 |
| pulsed-liquid-fronts | 3,229 | 11.174 | 37.139 | 2 |
| paused-unload | 451 | 11.141 | 11.818 | 0 |
| paused-return | 450 | 11.163 | 12.968 | 0 |
| source-removal-drain | 3,148 | 11.155 | 11.738 | 0 |

Across **12,218 frames**, six exceed the floor; the worst is **37.139 ms**. Pulsed fronts intentionally leave pending work at the sampling boundary; the later drainage audit proves convergence. Occupied flow cells are not liquid-volume conservation units. Setup, captures, audits and subsequent paused settle tails are excluded from timed windows. [Flowing water/lava](factory-optimization-2026-09-22/Native1/fluid-stress/fluid-stress-water-lava.png) and [drained basins](factory-optimization-2026-09-22/Native1/fluid-stress/fluid-stress-drained.png) were visually inspected.

## Remaining tail latency

The first optimized run's worst gameplay frame is 74.49 ms during inventory use. Nearby delayed GPU timing reaches 70.96 ms; factory work in that observation totals only 3.04 ms across two ticks. Several other worst windows show similar GPU/presentation bursts. NVIDIA telemetry records 42 of 312 samples at 300 MHz and active thermal slowdown later; this supports a hardware limitation without proving each frame's cause.

Three `SimulationAdvance` observation totals still exceed 22.22 ms (23.68, 24.63 and 22.63 ms), each containing two factory ticks. These are real main-thread wall-latency breaches. No individually isolated synchronous child phase exceeds the floor in this run. Delayed main-thread samples can also reach 40–50 ms without a matching dominant scope: scheduling, GC, uninstrumented native work and CPU thermal behavior remain unmeasured. Graphics/presentation tail profiling under sustained load is the next target; the data does not justify reducing gameplay scope or declaring every spike a network problem.

The full soak's **359.012 ms** frame occurs at sample time 564.775 s (Unity frame 36,486), with terrain/light queues empty. Nearby delayed GPU timing reaches **360.554 ms** and present wait **352.077 ms**. The observation contains 15.367 ms of factory work and 17.518 ms of simulation advance, including catch-up. This is a graphics/presentation-associated outlier, not evidence that meshing or a network rebuild consumed 359 ms. The cause still requires graphics/driver and scheduling profiling; retain the outlier in acceptance statistics.

## Evidence and build identity

- [48-suite Editor summary](factory-optimization-2026-09-22/domain-summary.txt), [mesh regression](factory-optimization-2026-09-22/mesh-regression.txt), [power comparisons](factory-optimization-2026-09-22/grid-allocation-checks.txt), [pump traces](factory-optimization-2026-09-22/ranged-pump-checks.txt).
- [34 shader image comparisons](factory-optimization-2026-09-22/shader-comparison/results.txt), [historical shader manifest](factory-optimization-2026-09-22/shader-reference.json).
- [Measured source manifest](factory-optimization-2026-09-22/source-manifest.json), [measured build identity](factory-optimization-2026-09-22/Native1/build-identity.json), [measured build summary](factory-optimization-2026-09-22/build-summary.txt): zero errors and warnings. Measured Assembly-CSharp SHA-256: `eb2881e6f80cee912fad4a70cc66c8ed3f474458ac7a90f29779bd318b6c6937`.
- Shader scenario: [correctness](factory-optimization-2026-09-22/Native1/factory-shader/runtime-report.json), [frame analysis](factory-optimization-2026-09-22/Native1/factory-shader/analysis.json), [paired GPU analysis](factory-optimization-2026-09-22/Native1/factory-shader/graphics-comparison.json), [raw frames](factory-optimization-2026-09-22/Native1/factory-shader/frames.csv.gz).
- Full factory: [correctness](factory-optimization-2026-09-22/Native1/release-review/runtime-report.json), [frame analysis](factory-optimization-2026-09-22/Native1/release-review/analysis.json), [raw frames](factory-optimization-2026-09-22/Native1/release-review/frames.csv.gz).
- Liquids: [correctness](factory-optimization-2026-09-22/Native1/fluid-stress/runtime-report.json), [stage measurements](factory-optimization-2026-09-22/Native1/fluid-stress/fluid-stress.json), [raw frames](factory-optimization-2026-09-22/Native1/fluid-stress/fluid-stress-frames.csv.gz).

After native measurements, only diagnostic scenario-description wording and the Editor-only image-check request polling throttle changed. The archived measured source/build identity remains separate from [final verification identity](factory-optimization-2026-09-22/final-verification/identity.json). The final [48-suite gate](factory-optimization-2026-09-22/final-verification/domain-summary.txt), [34 image comparisons](factory-optimization-2026-09-22/final-verification/shader-comparison.txt) and [player build](factory-optimization-2026-09-22/final-verification/build-summary.txt) pass; the build has zero errors/warnings. The measured shader metadata retains the older generic quality-control description; actual `gpu-shader` CSV rows and the 1,200-frame brackets above define the experiment.

Wiki validation passes **232 pages and 9,943 local links/images** in the isolated checkout using committed artwork. The shared working tree contains an unrelated modified block atlas; its fingerprint check fails as expected, and that artwork is neither overwritten nor included in this change. The isolated Editor has already serialized its unused mobile pipeline asset to the pinned URP format; desktop benchmark settings and the committed mobile asset are unchanged.

## Reproduction

Use `Tools/prepare_shader_comparison.py <isolated-project> --reference 3034df3`, then the pinned graphical Editor. `Logs/shader-comparison-request.txt` runs deterministic preview comparisons. The `diagnostics-liquids` build request runs the complete Editor gate and creates a non-Development player. `Tools/Verify-ReleaseReview.ps1` supports `factory-cpu`, `factory-shader`, ordinary `release-review` and `fluid-stress`; use fresh evidence/save directories and `-FrameLimit 90 -GpuTelemetry`. The shader scenario swaps only the actual WorldLit materials between the historical and current shaders in old/current/old brackets; all settings and materials are restored before live transitions.
