# Sun shadow stability and lighting cost — 2026-09-10

The user reported continued sideways crawling of a jagged terrain/tree shadow edge after the earlier [ambient-occlusion fix](SHADOW_RESULTS.md). The remaining moving-sun case now holds the directional light rotation between fixed world-time samples. In the close terrain-edge test, all 235 adjacent frame pairs between samples are pixel-identical. Soft shadows, AO, shadow atlas size, cascades, distance and bias are preserved.

## Runtime change

`DayNightCycle` samples the shadowed light direction once per in-world minute: 0.25° of its orbit, every approximately 0.833 real seconds with the current 20-minute day. It writes the light rotation only when that sample or celestial source changes. The world clock, factory/survival ticks, day/night eligibility and lunar phase rules remain continuous and unchanged. Visible sun/moon, ambient colors and fog still blend smoothly. A restoration or long frame jumps directly to the current light sample, without replaying missed updates. Reset invalidates the cached sample.

This replaces 240 light-rotation updates with four in four seconds of simulated time at 60 Hz. It does **not** cache shadow maps: Unity still renders dynamic shadow casters between sun samples. The result removes continuous edge crawl while allowing shadows to step with the sun, as requested. Moving-camera rasterization and cascade transitions are outside the stationary-edge acceptance check.

## Native edge check

Unity **6000.4.4f1**, Windows Direct3D 12 development player, **RTX 2060**. Source base `899bd3f`; the [manifest](sun-shadows/artifact-hashes.json) identifies the tested source and player. Seed 246813, resident view radius 4, a block canopy over a grass platform and a stationary close camera. The continuous control overrides only the light rotation with the previous per-frame formula in the same executable; the ticked case uses production `Apply` behavior.

Each mode warms for 12 frames, then captures 240 frames while advancing time at 1/60 second per capture. Pixel comparison uses the lower **960×270** terrain region of a **960×540** MSAA render target. The sky is excluded. Camera dithering, light color/intensity and ambient/fog shader globals are frozen only in this edge-isolation fixture; they retain their ordinary behavior during play and in the timing phase.

| Edge case | Rotation updates | Held frame pairs | Changed held pairs | Mean RGB-code delta across all adjacent pairs |
| --- | ---: | ---: | ---: | ---: |
| Previous continuous direction | 240 | 0 | N/A | 0.040194 |
| Ticked direction | 4 | 235 | **0** | 0.003488 |

Mean delta uses absolute RGB byte differences on the 0–255 scale. The ticked mean includes the four deliberate sun steps; between steps it is exactly **0**. A temporary 0.5 depth-bias comparison gave the same tested edge result, so production bias remains 0.1. Inspected close captures retain the tree shadow, contact shading and softly filtered edge: [continuous control](sun-shadow-continuous-2026-09-10.png), [ticked result](sun-shadow-ticked-2026-09-10.png). Finite shadow-map resolution is not a claim of a mathematically perfect line at every viewing angle.

The native report passed 21 fixture checks, covering held rotation while the real clock advances, tick changes at dawn/noon/dusk/midnight, large world times and sun/moon alignment. The surrounding runtime harness passed with no recorded errors. The native day/night regression separately passed **39 assertions**, including ordinary advancement, pause/inventory rules, dawn/dusk continuity, all eight lunar phases and session reset. Clock domain checks passed **144 assertions**. The build completed with **zero errors and warnings**.

## Rendering cost

After the image phase, the probe restores normal dithering and continuous color changes, renders directly to the **1280×720** player screen, and hides only the interface child. Each mode warms for 60 frames and measures 240 frames with VSync/frame caps disabled. There are no screenshots, readbacks or pixel arrays in these timing loops. FrameTimingManager and the GPU profiler counter agree within approximately 0.002 ms on these medians. Frame timing instrumentation is enabled only for the review build and restored in project settings afterward.

| Mode | GPU frame median | GPU p95 | CPU frame median | SRP Batcher draws, median | Shadow casters, median |
| --- | ---: | ---: | ---: | ---: | ---: |
| Continuous light | 5.099 ms | 5.882 ms | 5.334 ms | 432 | 212 |
| Ticked light | 5.362 ms | 6.308 ms | 5.557 ms | 432 | 212 |
| Directional shadows off, diagnostic control | 4.002 ms | 5.166 ms | 4.739 ms | 220 | 0 |
| Ticked light repeat | 5.148 ms | 6.017 ms | 5.419 ms | 432 | 212 |

Shadow rendering adds approximately **1.1–1.4 ms of GPU frame time** in this scene by comparison with the shadows-off control. The same 212 shadow casters remain with ticked lighting. No GPU or FPS saving from the direction hold is established; its benefit is stability and fewer transform writes. CPU frame time includes waits and is not an isolated measurement of shadow CPU work. These are short scene measurements, not a large-factory CPU/GPU budget certification.

The existing [lighting bounds](../CONTENT_PIPELINE.md#lighting-cost-and-shadow-stability--2026-09-10) retain one celestial light, eight shadowed torch lights and eight unshadowed workshop lamps. Main shadows already stop at 160 m compared with 304 m fog at the default view radius 10. At the test radius 4, fog ends at 112 m; no runtime range clamp was introduced from this focused comparison. Dense overlapping local lights, moving mobs, factory scale and lower-end GPUs still need their own stress measurements before raising these limits or claiming spare frame time. No quality reduction or shadow-resolution increase is part of this fix.

## Reproduction

Run `powershell -ExecutionPolicy Bypass -File Tools/Verify-Shadows.ps1 -Build -SunTicks` with the pinned Editor open and idle. The current review executable is `Builds/Shadows/RivetReach.exe`. Normal sessions never invoke the probe. Existing older player folders need rebuilding to include this code.

Raw evidence: [sun-edge and timing report](sun-shadows/sun-shadow-report.json), [runtime harness](sun-shadows/runtime-report.json), [day/night regression](sun-shadows/day-night-runtime-report.json), [clock checks](sun-shadows/clock-domain-report.txt), [build](sun-shadows/build-summary.txt), [artifact hashes](sun-shadows/artifact-hashes.json). Capture timestamp: **18:59:57 UTC**. Intermediate offscreen timing and an interrupted harness run are not treated as passing performance evidence.
