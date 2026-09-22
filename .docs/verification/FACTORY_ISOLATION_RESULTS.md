# Factory isolation and optimization proposals — 2026-09-22

**Status: both extra native runs and the 48-suite Editor gate pass correctness checks; the 60/45 FPS performance gate still fails.** This is a completed investigation, not an implemented speedup. It follows the [full factory profile](FACTORY_PROFILE_RESULTS.md).

## Test design

The same i7-10750H / RTX 2060 laptop runs non-Development Unity 6000.4.4f1, D3D11, 1920×1080, view distance 10, 90 FPS cap, VSync off, original 4× MSAA and four 160 m sun-shadow cascades. The fixture retains 32 production lines, 1,518 industrial assemblies, 230 stations/crates, 512 crops, 64 chickens and 14 initial hostiles. Gameplay, source scan budgets, recipes, resources and default visual settings are unchanged.

Native1 adds eleven detailed CPU scopes beneath the existing parent scopes: native terrain views, terrain mesh submission, fluid mesh submission, first chunk activation, three machine-processing phases and four power phases. Native2 adds ranged-pump preparation. The compact player overlay retains its existing 27 rows; detailed captures contain 38/39 scopes. Parents include children; two charging calls are summed per factory tick. Mesh/activation maxima are observation totals unless explicitly identified as one call.

Native1 also runs five graphics controls twice, reversing their order on the second round. Each has 300 measured frames bracketed by 300-frame default baselines, with 90 warmup frames per stage. The factory/world/crop/mob simulation, camera and principal presentation components are suspended for those windows. Quality settings, sun shadows, renderer states and component enabled flags are restored in `finally`, with native assertions before ordinary live unload/travel resumes. The normal five-minute soak is replaced by these diagnostics; [the preceding full profile](FACTORY_PROFILE_RESULTS.md) owns the soak evidence.

**Control limits:** ambient shader time, particles, cosmetic grass and dropped-item updates remain active. The native witnesses verify factory tick and celestial time, not every pile's state. The terrain queue stays at one pending page throughout the graphics windows, so cosmetic grass can still rebuild periodically. These are frozen factory/camera comparisons, not a claim that every visual or authority is paused. Natural spawning remains disabled. Hiding machines also removes their shadows; this measures their combined rendering contribution. No graphics-control frame is a normal gameplay acceptance sample.

## Graphics findings

Reference GPU time is the mean of the baseline-before and baseline-after medians. These are measured diagnostic reductions, **not shippable optimization percentages**:

| Temporary control | Round 1 GPU change | Round 2 GPU change | Interpretation |
| --- | ---: | ---: | --- |
| Hide machine/pipe renderers | −34.6% | −44.4% | Largest consistent effect; investigate visible machinery rendering first. |
| Render scale 70% | −17.7% | −16.6% | Pixel work contributes; keep native resolution while reducing redundant shader work. |
| MSAA 4× → off | −5.9% | −19.0% | Variable cost; these tests do not justify sacrificing edge quality. |
| Sun shadows off | −4.2% | −5.6% | Smaller effect here than removing visible machinery; do not begin by cutting shadows. |
| Hide creature renderers | +3.2% | +4.9% | No demonstrated benefit from this camera; creature AI/animation costs remain separate. |

Default SRP draw counts stay at about **7,060**. Hiding machine/pipe renderers lowers them to **2,963**; disabling sun shadows lowers them to **4,853**. Creature hiding leaves them at **7,060**, so this camera does not exercise substantial visible creature geometry. These results do not certify a crowd-facing camera. There are **2,247 machine renderers**, **78 creature renderers**, and **zero active shadowed nondirectional lights** in the control setup; this run says nothing about the cost of a torch-heavy room.

GPU thermal slowdown and clock changes persist. Baseline GPU drift ranges approximately 1.5–10.8%, and two-second telemetry provides only one to three observations in many short windows. It cannot normalize each frame to a fixed clock. The repeated large machine-rendering effect is useful for prioritization; small effects and exact savings require longer controlled GPU-pass captures. The experiment does not isolate opaque versus transparent surfaces, vertex work, individual shader functions or driver waits.

## Chunk application findings

Native1 unload/return reproduces **33.962 ms in one terrain mesh submission**, within **34.295 ms** total chunk application. Activation in that observation is only **0.290 ms**. The next recorded frame is **37.997 ms**; a delayed main-thread sample reaches **37.977 ms**, with nearby GPU samples around 7–8 ms. This is a concrete main-thread terrain mesh submission hitch rather than a 34 ms activation scan.

Across that window, terrain view work peaks at **2.948 ms**, fluid mesh submission at **0.559 ms**, and activation at **4.017 ms** per observation. Activation averages **0.330 ms per call** and is a meaningful routine cost, but the long upload tail is the first hitch target. Existing terrain and fluid `Mesh` objects are already reused; adding object pooling alone does not address repeated native buffer uploads. The application time budget is checked after each indivisible `Apply`, so lowering its number cannot prevent a slow call.

Native2 independently repeats the tail: **35.653 ms across two terrain mesh submissions** in one observation, within **36.320 ms** total application, with only **0.609 ms** activation. The next frame is **40.224 ms** and a delayed main-thread sample reaches **40.200 ms**. This is a second reproduction of the costly phase, not evidence that every upload takes that long.

The terrain submission scope includes creation/clear, vertex channel setters, triangle assignment, bounds and renderer mesh assignment. Native driver synchronization, allocation/collection and scheduling within that scope remain unseparated. The marker measures wall latency, not 34 ms of confirmed CPU execution.

## Live CPU findings

Native1 clear-factory means per factory tick:

| Phase | ms/tick |
| --- | ---: |
| State/signals/boiler setup | 0.2371 |
| Machine preparation | 1.2885 |
| Machine advance | 0.1219 |
| Power preparation | 0.1776 |
| Power serving ordinary loads | 0.2920 |
| Power charging, both passes | 0.4789 |
| Power discharge | 0.1868 |

Machine preparation is **78% of the measured processing phases**; generic loop fusion alone is unlikely to recover most of it. Power charging is **42% of the four measured power phases**. Snapshot topology contains **1,276 groups and 1,692 device endpoints**: 1,098 input, 396 output and 198 storage. Native2 counts their static role combinations below, identifying phases that can be skipped without changing allocation rules.

### Focused 45-second live confirmation (Native2)

The second run adds a ranged-pump preparation child marker and runs an unchanged live factory for 45 seconds, without graphics controls. Across **901 factory ticks**, the **32 ranged pumps** produce **28,832 preparation calls**:

| Measurement | Mean ms per factory tick |
| --- | ---: |
| Entire factory | 4.1442 |
| Machine preparation | 1.4824 |
| Ranged-pump preparation, all 32 combined | 1.2184 |
| Remaining preparation | 0.2641 |
| Power preparation | 0.2042 |
| Power ordinary loads | 0.3443 |
| Power charging, both passes | 0.5585 |
| Power discharge | 0.2188 |

Ranged pumps account for **82.2% of preparation and 29.4% of the whole factory tick**. This is now a measured family cost, not just a source suspect. The preceding clear window independently records **1.0954 ms/tick** for ranged pumps. These scopes include cheap full-buffer/cooldown/target checks as well as searching; individual cell-read costs and exact scanned-cell counts were not captured. Source inspection identifies the bounded scan and repeated residency/read lookups as the first operations to optimize.

Power's exact static role histogram:

| Group roles | Groups | Potential transfer |
| --- | ---: | --- |
| Input only | 809 | None |
| Output only | 267 | None |
| Storage only | 102 | None |
| Input + output | 33 | Generation to load |
| Input + storage | 64 | Battery to load |
| Input + output + storage | 1 | Generation/load/storage |

**1,178 of 1,276 groups (92.3%) lack a source/sink role pairing.** Only **one group** has both output and storage roles, so only it can charge storage in this snapshot. This is not a proposed topology change: keep all-face isolated grids intact and cache which existing groups need each allocation phase. Runtime power amounts, shared storage capacity and activity still require live checks. Skipping 92.3% of group visits is not a claim of 92.3% lower total power CPU time; meaningful transfers, preparation/display updates and fairness work remain.

## Proposed optimization order

These are proposals to implement and compare, not changes already delivering the diagnostic percentages:

1. **Terrain upload hitches:** move bounds computation to mesh workers; test packed vertex/index submission instead of separate managed channel assignments. Trial dynamic-buffer hints for frequently remeshed liquid/edited chunks, not every static chunk. If profiling implicates reuse synchronization, compare a bounded alternate-buffer experiment. Preserve exact vertices, lighting attributes, indices, culling bounds, resident publication and teardown; measure memory as well as p99/max latency. Never add blocking GPU fence waits.
2. **Visible machinery rendering:** first test elimination of redundant cave-light/fog queries in the shared material shader. The current shader computes cave-fog lighting even when its fog blend is zero; main-light and ambient helpers also request related surface lighting. Inspect compiled work before assuming the compiler duplicates it. Preserve images across day/night, caves, placed/held lighting, fog boundaries, metal and glass. Then consider fewer static renderer parts/material submissions with unchanged geometry, lighting and shadow shape. Prior route instancing was inconsistent, so require GPU/frame improvement rather than accepting fewer draws alone.
3. **Ranged-pump preparation:** remove repeated residency/dictionary queries first, then test a resident source index or revision-aware cache. Skip reads in known-empty spans while consuming the same conceptual scan positions; preserve the cursor, upper-layer-first order, 256-cell logical budget, retry timing, source revalidation, competing-pump conservation and unloaded boundaries. The measured 1.22 ms/tick is the entire family cost, an upper bound rather than a promised saving.
4. **Power:** cache static endpoint role lists/masks when topology is published, skip charge passes for groups without generation or storage, and skip load/discharge work when compatible roles are absent. Retain demand display/reset behavior even for inert groups, original endpoint order and fairness cursor, shared per-machine budgets across faces, per-cell rounding and both surplus charging passes.
5. **Routine activation/presentation:** replace repeated full chunk crop/spawner discovery with revision-bound worker metadata and chunk-indexed activation if its measured contribution warrants it. Cache unchanged machine presentation and measure animal animation from a crowd-facing camera. These are secondary to the confirmed upload tail and larger factory phases in this fixture.

Every candidate needs the existing conservation, residency, save and interaction checks plus the same native frame distributions and image comparisons. Keep the user's >60 FPS normal / 45 FPS worst target; reducing simulation rates, range, populations or visual quality is not the proposed fix.

## Evidence and reproduction

Native1 passes **2,305 assertions**, exits 0 and records **14,526 live gameplay frames** with **771 frames over 22.22 ms** (worst **87.284 ms**), separately from **9,000 graphics-control frames**. Neither dataset establishes the release gate. The graphic-control counter rows contain zero factory tick and machine-view calls, consistent with their suspended components. Both before/after default captures were visually inspected; no pixel-identical-image claim is made.

Native2 passes **2,264 assertions**, exits 0 and records **16,539 live gameplay frames**, with **1,075 over 22.22 ms** and a worst frame of **87.867 ms**. Its focused live window lasts **45.013 seconds**: median **13.69 ms**, p95 **69.70 ms**, maximum **82.90 ms**, and **268 of 2,486 frames** miss the 45 FPS floor. Typical frames can be quick while frequent long frames still prevent smooth play. Both runs include unload/return, storms, inventory, full outputs, power shortage, travel and save/conservation checks; they do not replace the earlier flowing-world-liquid stress evidence.

Native1 and Native2 use different instrumented assemblies, identified by SHA-256 `6DDF2C0B5A5391EEEB103408E013C64A78839BD47BC3916CADCFB9D9BDBB0EE2` and `5EAB62724F2E51927C2970DA6ADFD7B5E7B35786CFEA6DAF70A205BBD18B1BA9`. Native2's code/shader and active PC pipeline hashes match the final source. Its manifest also records the isolated Editor's automatically serialized, unused Mobile pipeline asset; that asset is not changed in this task. Both builds report zero errors/warnings, and the 48 Editor suites pass for each dated build input.

- Native1: [build identity](factory-isolation-2026-09-22/Native1/build-identity.json), [source manifest](factory-isolation-2026-09-22/source-manifest.json), [48-suite gate](factory-isolation-2026-09-22/domain-summary.txt), [build summary](factory-isolation-2026-09-22/build-summary.txt).
- Native1: [native checks](factory-isolation-2026-09-22/Native1/runtime-report.json), [raw frames](factory-isolation-2026-09-22/Native1/frames.csv.gz), [phase analysis](factory-isolation-2026-09-22/Native1/analysis.json), [spike contexts](factory-isolation-2026-09-22/Native1/analysis.spikes.json.gz), [performance summary](factory-isolation-2026-09-22/Native1/performance.json).
- Native2: [build identity](factory-isolation-2026-09-22/Native2/build-identity.json), [final source manifest](factory-isolation-2026-09-22/Native2/source-manifest.json), [48-suite gate](factory-isolation-2026-09-22/Native2/domain-summary.txt), [build summary](factory-isolation-2026-09-22/Native2/build-summary.txt).
- Native2: [native checks](factory-isolation-2026-09-22/Native2/runtime-report.json), [raw frames](factory-isolation-2026-09-22/Native2/frames.csv.gz), [phase analysis](factory-isolation-2026-09-22/Native2/analysis.json), [spike contexts](factory-isolation-2026-09-22/Native2/analysis.spikes.json.gz), [performance summary](factory-isolation-2026-09-22/Native2/performance.json), [power role counts](factory-isolation-2026-09-22/Native2/graphics-isolation.txt), [GPU telemetry](factory-isolation-2026-09-22/Native2/gpu-telemetry.csv).
- Graphics: [paired comparison](factory-isolation-2026-09-22/Native1/graphics-comparison.json), [control metadata](factory-isolation-2026-09-22/Native1/graphics-isolation.txt), [GPU telemetry](factory-isolation-2026-09-22/Native1/gpu-telemetry.csv). The older binary's broad “presentation updates suspended” wording is qualified by the ambient-system limits above.
- Actual default appearance [before controls](factory-isolation-2026-09-22/Native1/isolation-restored-before.png) and [after restoration](factory-isolation-2026-09-22/Native1/isolation-restored-after.png).

Build through the graphical Editor's `diagnostics-liquids` request. Run `Tools/Verify-ReleaseReview.ps1` with `-Scenarios factory-isolation -FrameLimit 90 -GpuTelemetry` for paired graphics plus live transitions, or `-Scenarios factory-cpu` for the 45-second live preparation sample without graphics controls. Always use fresh evidence/save directories. `Tools/analyze_factory_timings.py` creates phase summaries plus compressed spike contexts; `Tools/analyze_graphics_isolation.py` consumes a directory containing those summaries, `performance.json` and telemetry. Supply the telemetry timezone offset from build identity when it differs from +02:00.

The captures use the isolated checkout's committed art, preserving the user's working atlas. No item, recipe, machine behavior or player-facing visual presentation changes in this diagnostic work; existing player wiki instructions remain applicable.
