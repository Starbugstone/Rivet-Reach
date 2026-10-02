# Factory visibility verification — 2026-10-02

Implementation and native verification of the [authorized distance policy](../FACTORY_VISIBILITY.md), following the [previous 64-block review](MACHINE_VISIBILITY_REVIEW.md). This report concerns presentation and its measured cost; it does not certify whole-game release performance.

## Change and preservation

Resident industrial models, pipes, cable runs, connected tank shells, crates and beds share the terrain fog limit (304 blocks at default view distance ten). Detailed and derived distant models transition over 48–64 blocks, retaining detailed roots through 72. Distant geometry is instanced and omits per-machine labels, animated decoration, lights and shadow casting. Connected tank liquid uses the complete tank bounds instead of requiring a visible controller.

The change does not grant chunk residency, advance dormant factories or change machine state. Starter recipes and the approved Cog/Rivet definitions remain unchanged. The refreshed catalog differs only in source fingerprints; item and recipe data are identical. Original artwork and icons are retained.

## Build and domain checks

Pinned Unity 6000.4.4f1, URP 17.4.0, native Windows release player. The isolated build uses committed artwork and the task-owned source, preserving unrelated local artwork changes. [Benchmark source hashes](factory-visibility-2026-10-02/benchmark-source-identity.json) identify tested files.

The [50-suite domain gate](factory-visibility-2026-10-02/domain-summary.txt) passed before the final material/pose and verification refinements. The final player build reran the [35,531 visibility assertions](factory-visibility-2026-10-02/factory-visibility-checks.txt) and [10 shared-material assertions](factory-visibility-2026-10-02/material-variant-checks.txt). Native results below identify the subsequent coverage.

Six representative models contain **18,128 source triangles and 3,082 derived triangles** (83.0% fewer). This is a geometry count, not a frame-rate claim. Checks cover finite derived vertices, bounds/elevation, transition overlap, pipe transforms and exact original material properties, including retaining unlit status indicators.

## Native conditions

Intel Core i7-10750H, NVIDIA GeForce RTX 2060 laptop, Windows Direct3D 11, 1920×1080, default view radius ten, 90 FPS cap and VSync off. Sequential camera/control samples can differ with thermal state and ongoing production. GPU samples are delayed and deduplicated; unavailable counters must remain unavailable rather than be treated as zero. [Thermal telemetry](factory-visibility-2026-10-02/factory-visibility/thermal-summary.json) recorded 64–89°C, graphics clocks from 300 to 1,875 MHz and active software thermal slowdown in 419 of 562 samples.

The five-minute ordinary Survival route passed with seven assertions: empty-handed gathering, paid starter crafting, workbench placement, wooden-pickaxe mining, exploration and survival without supplied resources or Creative mode.

## Factory stress measurements

The native [correctness report](factory-visibility-2026-10-02/factory-visibility/runtime-report.json) passed **2,312 assertions**. The fixture contains 32 production lines, 1,518 industrial assemblies, 230 stations/crates, 512 crop cells, 64 persistent chickens and 14 explicitly placed hostiles. Checks include:

- Four matched camera positions using distant rendering, the previous cutoff control, and restored distant rendering; an additional full-detail-to-fog reference at the mid-distance camera.
- Tank-liquid presentation from all four viewpoints, including elevation; a drained mesh queue and nonzero distant instance/draw submissions at each viewpoint.
- Byte-identical paused industrial save payload across visibility changes; a newly placed distant cable appears and leaves no phantom after removal.
- Thirty seconds of movement across the detail transition in both directions, followed by the five-minute live-factory soak, storm, inventory, full-output and power-shortage samples.
- Exact ore-equivalent resources and bridge item/liquid conservation; dormant production freezes, loader-backed production remains active, and returning views reuse their bounded caches.
- [Complete checkpoint restoration](factory-visibility-2026-10-02/factory-visibility/stress-save.txt): 870,619 bytes, all 1,518 assemblies and 64 persistent animals restored byte-for-byte. Paused save took 74.97 ms; synchronous load took 1,028.68 ms, excluding subsequent streaming.

**Steady 60 FPS and the 45 FPS worst-frame floor still fail.** Across [all 28 workloads](factory-visibility-2026-10-02/frame-distributions.md), 36,429 recorded frames include 12,818 over 16.67 ms and 6,011 over 22.22 ms. The [compressed raw frame capture](factory-visibility-2026-10-02/factory-visibility/frames.csv.gz) retains every sampled frame, and [scope analysis](factory-visibility-2026-10-02/timing-analysis.json.gz) retains spike neighbours and queue peaks.

| Workload | Median | P95 | P99 | Maximum | Over 22.22 ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| Initial working factory | 16.07 ms | 58.09 ms | 77.02 ms | 85.25 ms | 262 / 1,800 |
| Moving visibility transition | 12.06 ms | 34.85 ms | 41.57 ms | 60.93 ms | 207 / 1,983 |
| Five-minute factory soak | 18.38 ms | 54.64 ms | 95.33 ms | 817.07 ms | 3,838 / 12,946 |
| Factory unload and return | 12.55 ms | 44.58 ms | 76.19 ms | 87.39 ms | 347 / 3,700 |

At the mid-distance camera, median GPU samples were 5.80 ms with distant rendering, 5.34 ms with the old cutoff, 9.56 ms for the full-detail-to-fog reference, and 5.98 ms after restoring distant rendering. These are individual sequential observations, not a general speedup claim: strong thermal throttling and frame-time tails occur even in old-cutoff samples. The old cutoff also renders less of the requested scene. No separate old-binary performance run is claimed.

The session cached **208,915 source triangles versus 27,387 derived triangles**, across the encountered derived shapes, rather than a per-frame scene total; unchanged glass meshes are outside this counter. Cold cooperative mesh preparation peaked at **41.677 ms**, above the frame budget: the first preparation is indivisible, so the one-millisecond scheduling budget is not a hard execution cap. This remains an optimization limit. Runtime graphics draw-call counters are unavailable in this retail player (`-1`); nonzero submissions were asserted, and the separate capture records the distant renderer’s own submission counts. Those are not total engine draw calls.

## Evidence identity and limits

The final capture refinement changes only the verification camera path, half-filled demonstration tank and capture counters; the measured gameplay implementation is unchanged apart from shader whitespace. [Capture source hashes](factory-visibility-2026-10-02/source-identity.json) distinguish it from the benchmark. The full-detail diagnostic deliberately creates additional detailed views; its allocations and sequential thermal/production drift limit attribution of later hitches. This test demonstrates the requested visibility and state preservation, not the cause of every performance spike or visual quality on every GPU.

Distant models intentionally lose small detail, animation and cast shadows. The screen-door transition must also be judged in the native video; static screenshots alone cannot prove continuity. Cold arrivals can wait for the bounded mesh-preparation queue. Whole-game performance, extreme world sizes and long sessions remain separate review work.

## Images and video

[The player guide](../wiki/Factory-visibility.md) contains actual native screenshots and the camera tour. Capture is performed separately from the benchmark. A fixed 30 Hz image sequence demonstrates appearance and transitions; it is not evidence of real-time 30 FPS performance.

The separate [capture run](factory-visibility-2026-10-02/capture-runtime-report.json) passed 2,211 assertions and produced five native 1080p screenshots plus 720 consecutive Unity frames. A supplied half-full tank makes the visual level readable. [Capture counters](factory-visibility-2026-10-02/visibility-capture-counts.csv) record zero pending meshes at each static shot; they count distant renderer submissions only, separately from engine draw calls.

The seven guide images (including the matched old-cutoff comparison) and selected video frames were visually inspected: distant machinery stays present and the tank’s water is visible. Elevated captures also expose terrain/sea streaming boundaries; those remain outside this machine-presentation fix. The fixed-rate video must not be used to infer frame pacing.

Wiki validation passed in the isolated tested checkout: **234 pages, 9,980 local links/images, 195 item pages and 199 recipes**. The main workspace’s separate `BlockTiles.asset` artwork edit causes its existing fingerprint mismatch; it was preserved and excluded from this task. The isolated validation uses the committed artwork that will be published.
