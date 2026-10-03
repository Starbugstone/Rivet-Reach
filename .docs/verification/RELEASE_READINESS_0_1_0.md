# 0.1.0 release readiness — September 27, 2026

> **October 3 publication decision:** the owner requested a [0.1.0 Alpha playtest release](../releases/0.1.0.md) with the known limitations documented. This supersedes the publication hold below, not its performance, balance or stability findings. [The release report](ALPHA_0_1_0_RESULTS.md) owns the new artifact and checks; all measurements below retain their original dates.

**Decision: hold release. Frame pacing is the first blocker.** The implemented single-player game has substantial functional evidence, but neither a high average FPS nor passing gameplay assertions establishes the required smoothness. This audit covers the current implemented content; it does not add realms, multiplayer or other roadmap features to the release scope.

The user confirmed **steady 60 FPS at 1080p** during this audit. Report every frame above the 16.67 ms budget, plus p95, p99, maximum and individual spike records. The older review's 45 FPS / 22.22 ms floor remains useful for comparing severe breaches; it is not an acceptable steady state or permission to ship recurring stutters. The old p95-only normal-play indicator is insufficient on its own for this clarified priority.

Working acceptance proposal: p95 and p99 at or below 16.67 ms in the headroom benchmark, no frames above the historical 22.22 ms safety ceiling, and stable actual 60 FPS presentation in a separately capped/display-synchronized run. These percentile details are an audit proposal, not numbers individually selected by the user. Loading, paused saves and screenshot/file-write work are separate windows; ordinary gameplay spikes stay in the result even when thermal or operating-system effects are suspected. The benchmark laptop is an available reference, not a declared minimum supported PC.

## What is already good

| Area | Evidence and practical limit |
| --- | --- |
| Playable foundation | Mining/building, crafting, survival, farms/animals, weather and automation have native-player coverage. The [September 20 review](RELEASE_REVIEW_RESULTS.md#correctness-and-interaction-evidence) records the focused results and their individual builds. Scripted checks do not replace a player judging progression or feel. |
| Resource and save integrity | The large factory checks exercise real production, exact transfers, dormant freeze, chunk-loader operation and complete save/reload of 1,518 assemblies and 64 persistent animals. Historical schema/recovery checks exist; rerun them on the eventual packaged candidate. |
| Optimization without changing the game | [September 22 optimizations](FACTORY_OPTIMIZATION_RESULTS.md) reduced measured factory tick work, power allocation, pump reads, shader work and native mesh-submission cost while preserving recipes, simulation rates, view distance and visual settings. The performance floor still failed. |
| Finite image-equivalence coverage | The final September 22 Editor gate passed 48 suites and 34 deterministic shader image comparisons, including held equipment, skins, cave light and fog. Those images matched their reference RGB output. This is useful regression evidence, not proof of stable presentation during every camera movement. |
| Clear diagnostics | Retail builds expose separate inclusive system scopes, worker latency and delayed CPU/GPU samples. Raw frame captures retain failures. Unsupported allocation data is explicitly unavailable. |

## Current audit and artifact identity

Source reviewed: `7c2cb236e65c3aa9e9d5720e841b129b4397772e`, with pre-existing workspace changes preserved. The September 22 manifest plus its documented final diagnostic edits matches all **349 compared source/settings paths**, excluding the isolated Editor's unused Mobile pipeline serialization. This comparison is not a whole-content certification of the modified workspace.

The final optimized standalone player is in the existing `Rivet-Reach-ReleaseReview-20260920` sibling checkout under `Builds/DiagnosticsLiquids`. Its managed assembly SHA-256 is `5ffa636cd6302f89e3424c9421667be077f8197d9ed215865cb47028bdb1350f`. The similarly named player in the main workspace has the older `01d497…` assembly. Selecting a player by folder name alone can therefore test the wrong code. [Source/build comparison](release-readiness-2026-09-27/source-audit.json) records the distinction.

The fresh run uses Unity 6000.4.4f1 non-Development standalone, D3D11, 1920×1080, the existing 90 FPS cap/VSync-off benchmark, radius ten for the factory and unchanged desktop quality (4× MSAA, native render scale and the existing shadows). [Hardware](release-readiness-2026-09-27/hardware.json): i7-10750H, RTX 2060, approximately 32 GB RAM, Windows 11 build 26200. No Unity Editor or Blender instance was running when the audit began. This is one laptop session, not a controlled comparison or a supported-hardware certification.

Both native scenarios exit **0**, with **7 Survival assertions** and **2,263 combined factory/correctness assertions**, and no recorded errors. The ordinary route gathers its resources and establishes a workshop/pickaxe without Creative or supplied items. The separate [stress fixture](release-readiness-2026-09-27/release-review/stress-workload.txt) supplies 32 production lines, 1,518 assemblies, 230 stations/crates, 512 crop cells, 64 chickens and 14 hostiles, with natural spawning disabled. Ordinary simulation, rendering and navigation run during its samples. The full factory checkpoint preserves **1,518 assemblies and 64 persistent animals** byte-for-byte. Paused save is **133.97 ms** and the synchronous load transaction **2,052.29 ms**, excluding subsequent streaming.

**Performance fails:** across 14 measured windows, **30,794 frames**, **9,659 above 16.67 ms** and **4,218 above 22.22 ms**. This includes the separately named 600-frame diagnostic-overlay window; setup, captures and save/load are excluded. The five-minute soak alone has **4,509 of 17,570 frames (25.7%) above the 60 FPS budget**. Clear terrain/day and night pass these finite headroom windows; inventory, storms, loaded factories and traversal still have long frames.

| Current workload | Frames | p95 / p99 / maximum, ms | Above 16.67 ms | Above 22.22 ms |
| --- | ---: | --- | ---: | ---: |
| Terrain day | 600 | 11.15 / 11.70 / 13.73 | 0 | 0 |
| Idle inventory | 600 | 30.32 / 34.59 / 41.32 | 37 | 35 |
| Terrain night | 600 | 11.14 / 11.25 / 12.31 | 0 | 0 |
| Terrain storm | 600 | 28.56 / 29.62 / 31.63 | 42 | 39 |
| 192-device factory | 600 | 14.61 / 20.86 / 31.62 | 19 | 4 |
| Large factory, clear | 1,800 | 32.35 / 66.53 / 79.63 | 818 | 205 |
| Diagnostic overlay | 600 | 65.10 / 66.37 / 76.70 | 251 | 101 |
| Five-minute factory soak | 17,570 | 33.45 / 65.72 / 163.10 | 4,509 | 1,847 |
| Large factory, storm | 1,800 | 33.06 / 66.39 / 87.98 | 1,087 | 538 |
| Factory inventory | 600 | 70.78 / 72.26 / 76.50 | 468 | 189 |
| Active unload/return | 3,224 | 44.08 / 57.70 / 114.96 | 1,284 | 768 |
| Full outputs | 600 | 65.59 / 67.74 / 73.02 | 411 | 183 |
| Power shortage | 600 | 52.77 / 54.58 / 60.75 | 331 | 136 |
| Streaming | 1,000 | 35.96 / 60.35 / 70.82 | 402 | 173 |

The worst **163.10 ms** frame occurs during the soak with no queued terrain/light/fluid work at that observation. Within three preceding/four following observations, delayed render-thread timing reaches **160.39 ms** and present wait **155.61 ms**, while returned GPU timing peaks at **17.75 ms**. This implicates rendering/presentation latency for investigation; it does **not** prove 163 ms of shader execution. Nearby simulation advance totals up to 17.81 ms, including 15.34 ms of nested factory work. Delayed timings, overlapping scopes and catch-up calls must not be added or treated as exact same-frame CPU costs.

There are other CPU-side tails: unload/return records a **61.66 ms total factory scope in one observation**, which may contain multiple ticks; terrain mesh upload peaks at **10.85 ms per observation**. Pending light/terrain queues peak at **1,706 / 2,023**. These are backlog counts, not queue age or individual-call percentiles. The fresh soak's mean factory scope is **4.43 ms/tick**, versus 2.61 ms in the September 22 optimized capture. The source comparison finds no intervening production-code change in its covered paths, so this is not evidence of a new algorithmic regression. Environment, scheduling and thermal variability require controlled attribution.

GPU telemetry records **309 of 454 samples** with thermal slowdown active, **62–88°C**, and clocks from **300 to 1,890 MHz** across the complete scenario. Sampling every two seconds cannot identify each frame's cause. CPU thermals, graphics-driver stacks and actual display presentation are not captured. Retain these frames in acceptance; a less severe maximum than the prior 359 ms run does not establish a speedup.

The separate UI probe passes its focused checks, with inventory-open action p95 **18.68 ms** and recipe-detail action p95 **2.05 ms**. These are action durations, not complete rendered-frame distributions; inventory opening itself warrants attention within a 16.67 ms frame budget. Unity allocated-memory snapshots range approximately **388–495 MiB**; they are not process working set, total RAM/VRAM or proof of a stable long-session plateau. GC/allocation measurements remain unavailable (`-1`).

Evidence: [build identity](release-readiness-2026-09-27/build-identity.json), [Survival report](release-readiness-2026-09-27/alpha-survival/runtime-report.json), [route observations](release-readiness-2026-09-27/alpha-survival/survival-observations.txt), [factory report](release-readiness-2026-09-27/release-review/runtime-report.json), [performance distributions](release-readiness-2026-09-27/release-review/performance.json), [raw frames](release-readiness-2026-09-27/release-review/frames.csv.gz), [phase analysis](release-readiness-2026-09-27/release-review/analysis.json), [summary/CSV hash](release-readiness-2026-09-27/release-review/audit-summary.json), [every historical-floor breach](release-readiness-2026-09-27/release-review/floor-breaches.csv.gz), [spike neighbours](release-readiness-2026-09-27/release-review/analysis.spikes.json.gz), [GPU telemetry](release-readiness-2026-09-27/release-review/gpu-telemetry.csv), [checkpoint](release-readiness-2026-09-27/release-review/stress-save.txt), [UI probe](release-readiness-2026-09-27/release-review/interactions.json). All frames above the tighter 60 FPS budget remain in the raw CSV. The analyzer validates consecutive frame IDs and complete scope-call relationships. Reanalysis of the September 22 raw capture also exactly matches its archived phase report; its numbers remain dated evidence.


## Release blockers, in order

| Priority | Work remaining | Evidence required to close it |
| --- | --- | --- |
| P0 | Resolve the long graphics/presentation stalls in the loaded factory. Correlate CPU/render/GPU and actual displayed-frame timing with sustained clocks and thermal telemetry. Existing scopes identify an associated subsystem, not a proven driver/shader root cause. | Repeat full-distance factory, storm, inventory and five-minute soak windows against the steady-60 target above. Preserve all outliers, both 16.67 ms overruns and 22.22 ms breaches. Use a stable environment for paired optimization experiments, then retest sustained real operating conditions. |
| P0 | Finish streaming, unload/return and flowing-liquid tail work. Packed mesh submission already improved uploads; do not report the previous 34 ms upload as an unfixed current measurement. Remaining simulation, activation and light-backlog costs need attribution. | Fixed-time or fixed-distance travel plus repeated unload/return, block edits at chunk seams and world-water/lava pulses. Measure oldest queue age and recovery, collisions and resource invariants alongside frame times. Include liquids at the default factory view distance; the existing liquid fixture uses radius four. |
| P0 | Make release automation evaluate performance as well as correctness. `ReviewReleasePerformance` writes `meets60AtP95`/`meets45Floor`, but the PowerShell runner only fails on process exit and `runtime-report.result`. | A separate explicit performance gate rejects any failed required workload, missing timing data or wrong build identity. Keep gameplay correctness and performance results separately visible. An exit code of zero must not imply release acceptance. |
| P1 | Verify frame integrity in motion and choose a tested presentation policy. Startup currently forces a 90 FPS cap and VSync off; Settings has no display-sync/frame-cap control. | Validate displayed-frame pacing and input feel on 60 Hz and a higher-refresh/VRR display where available; choose/persist suitable settings after measurement. Inspect fast pans, sun/shadow changes, transparent water/tanks, weather, cave/torch transitions, mining/placement, origin shifts and machine visibility boundaries. No tearing diagnosis follows from screenshots alone. |
| P1 | Complete sustained stability and memory coverage. Existing five-minute runs and Unity allocated-memory snapshots cannot establish multi-hour stability, total process/VRAM use or allocation/GC behavior. | Extended ordinary Survival → automation → travel → save/reload session, repeated residency cycles, process and graphics memory trends, usable GC/allocation traces and no lost/duplicated state or recurring exceptions. Preserve offline/dormant freeze. |
| P1 | Produce one reproducible 0.1.0 candidate and validate its documentation. Version settings and release packaging still name 0.0.1, and several run-guide paths point to historical builds. | Clean pinned-source build with deliberate version/notes, complete runtime files and notices, exact hashes, fresh-extraction launch, legacy-save/recovery checks and a second environment run. Validate/export the wiki against the exact selected artwork. No release/tag is created by this audit. |

[PERFORMANCE_PLAN.md](../PERFORMANCE_PLAN.md) (2026-10-03) proposes prioritized changes for these blockers, with draft patches, risks and required checks. It is a proposal and adds no new evidence.

## Frame integrity: what has and has not been demonstrated

Treat image correctness and presentation timing as two related checks. Shader reference images and resource-safe publication are valuable, but a correct screenshot can coexist with dropped/repeated displayed frames, tearing or an obvious streaming pop. The frame CSV measures Unity frame intervals; delayed `FrameTimingManager` samples do not certify monitor scanout or input-to-photon latency.

The [factory isolation camera](FACTORY_ISOLATION_RESULTS.md#graphics-findings) exercised no active shadowed nondirectional lights and little visible creature geometry. It does not cover a crowded torch-lit cave. The normal 64-block machine-view boundary is visible from distant factory angles in the [release gallery](../wiki/Release-review-gallery.md); review the transition in motion before deciding whether its presentation needs refinement. These are coverage gaps and polish candidates, not newly reproduced rendering defects.

Any optimization must retain collision/render agreement, revision-safe worker publication, both explorers/skins, current shadows/materials, liquid behavior, exact resource accounting and save compatibility. Reducing gameplay populations, simulation rates, range or default image quality is not evidence that the existing target has been met.

## Secondary polish after the blockers

- The fresh factory image below shows pale HUD/help text losing contrast against bright sky and strong lamp glow obscuring some nearby detail. Review HUD backing/contrast and light readability across day/night before changing art; this is a visual-review observation, not a measured rendering fault.
- Review an ordinary resource-paid path from empty hands to first powered production: recipe discovery, missing materials, fuel versus ingredient faces, wrench directions, blackout/backpressure explanations and loader coverage. Focused fixture checks establish transactions, not whether a new player understands the setup.
- Review interface scale, long labels, custom/rebound controls, held-item/armor visibility and inventory gestures at the supported resolutions. Existing pointer/browser evidence is useful but dated by build.
- Check audio levels/transition repetition, combat readability and food/tool progression during sustained play. No new balance defect is established by this audit; avoid speculative rebalancing before performance work.
- Consolidate the main run guide around one candidate. Keep old feature-build paths explicitly historical, remove stale summary counts and link authoritative rules. The repository and issue labels use several earlier alpha numbers; release naming must be consistent in the actual package and player instructions.

![September 27 native factory capture, including bright-sky HUD contrast and operating lights](release-readiness-2026-09-27/factory-clear.png)

Actual 1920×1080 standalone screenshot from the scripted stress fixture, visually inspected during this audit. The shared machinery palette and connections are coherent in this view; it does not demonstrate all machines at every distance, subjective art acceptance or temporal frame integrity.

## Documentation and reproduction

Current wiki validation passes against the committed artwork in the existing isolated review checkout: **232 pages and 9,943 local links/images**, with 195 generated item pages and 199 recipes. In the main workspace the same command correctly rejects the pre-existing modified `BlockTiles.asset` fingerprint. [Documentation checks](release-readiness-2026-09-27/documentation-checks.txt) retain both outcomes. Selecting final artwork and exporting its matching catalog is outstanding release work; this audit does not overwrite that asset.

To repeat the native audit, use the exact optimized player identified above, keep its data/runtime files together, and supply a fresh directory:

```powershell
Tools/Verify-ReleaseReview.ps1 -Executable <verified-player-path> `
  -OutputDirectory Logs/ReleaseAudit/NewRun `
  -Scenarios @('alpha-survival','release-review') `
  -FrameLimit 90 -GpuTelemetry -TimeoutSeconds 1800
```

The 90 FPS cap retains the historical headroom workload; it is not a 90 FPS release requirement or the final capped-60 presentation check. Survival uses the existing radius-four ordinary route; the factory scenario explicitly sets radius ten. Analyze the resulting `release-review/frames.csv` with `Tools/analyze_factory_timings.py`. Screenshots, scenario setup and checkpoint operations remain outside the timing windows. Repeat the relevant focused correctness suite and historical-save sweep on the eventual packaged candidate, rather than relabeling old feature results.

Source review: [frame sampling and result flags](../../Assets/RivetReach/Code/ReleaseReviewVerification.cs), [runner exit criteria](../../Tools/Verify-ReleaseReview.ps1), [startup cap/sync](../../Assets/RivetReach/Code/Expedition.cs), [current Settings controls](../../Assets/RivetReach/Code/UI/GameUI.cs), [0.0.1 packaging](../../Tools/Build-Release.ps1). These establish the release-process/presentation gaps independently of a timing hypothesis.

## Completion checklist

- [ ] All required timing windows meet steady 60 FPS at 1080p on declared reference settings, with an explicit failing/passing performance result and actual 60 FPS presentation check.
- [ ] Graphics/presentation stalls are explained and corrected; frame integrity is reviewed in motion, including display-refresh behavior.
- [ ] Ordinary play plus busy factory, weather, farms/mobs, default-distance liquids, editing and transitions are covered on one candidate.
- [ ] Long-session memory/GC and repeated travel/save/load/recovery evidence is recorded; a second environment checks the packaged build.
- [ ] Version, build identity, archive contents, notices, run instructions and matching illustrated wiki are current.

This is the remaining work for release readiness, not authorization to start unrelated content or a claim that subjective polish has been accepted.
