# Cave lighting and crop growth verification — 2026-09-13

Scope: [cave lighting](../LIGHTING.md) and [underground crop growth](../FARMING.md). Tested with Unity 6000.4.4f1, the Windows development player at `Builds/Lighting/RivetReach.exe`, Direct3D 11, RTX 2060 and i7-10750H. The fixture uses actual world edits and survival/industry systems in a constructed enclosed farm. The checkout also contained concurrent Floater spawning work; the focused farm fixture freezes mobs.

## Gameplay and rendering

The [player report](lighting-2026-09-13/runtime-report.json) passes **686 assertions** with [exit 0](lighting-2026-09-13/exit-code.txt). It checks all five plantable species reaching maturity under torches, due crops waiting in darkness, growth independent of the torch presentation pool, source removal, solid-wall occlusion and reopening, powered-lamp growth and power-loss darkening, compost in torchlight, vertical skylights, lateral entrances, roof replacement and rebuilding light after saving/loading. It also checks that neither unchanged terrain nor day/night presentation changes launch repeated light solves.

Actual captures were visually inspected: [sealed cave](../wiki/images/lighting/sealed-cave-no-light.png), [growing torch farm](../wiki/images/lighting/torch-farm-growing.png), [mature farm](../wiki/images/lighting/torch-farm-mature.png), [open shaft](../wiki/images/lighting/cave-open-skylight.png) and [side entrance](../wiki/images/lighting/cave-side-entrance.png). GPU interpolation smooths the light field while preserving voxel occlusion and greedy terrain meshes. The original unsmoothed trial captures are superseded.

The [build summary](lighting-2026-09-13/build-summary.txt) reports zero errors and warnings. Ten [focused solver assertions](lighting-2026-09-13/lighting-checks.txt) cover darkness, vertical access, lateral attenuation/range, source removal, walls, cross-chunk input and independent light channels. The existing [4,314 farming assertions](lighting-2026-09-13/farming-checks.txt), [survival checks](lighting-2026-09-13/survival-checks.txt) and [industry checks](lighting-2026-09-13/industry-checks.txt) also passed in this build. [Build identity](lighting-2026-09-13/build-identity.json) records the executable and managed assembly hashes.

## Measured cost

[Raw measurements](lighting-2026-09-13/lighting-performance.txt):

| Workload | Measurement |
| --- | --- |
| Unchanged 332-chunk fixture, 180 frames | Mean 0.0004 ms main-thread lighting work; p95 0.0006 ms; no new solves |
| Lighting GPU storage, 332 resident chunks | 4,210,688 bytes (about 4.0 MiB) |
| Streaming/edits across the focused run | Maximum observed main-thread lighting update 5.759 ms |
| Normal view distance 10, 3,051 resident chunks | Mean idle lighting work 0.0007 ms; no new solves |
| Lighting GPU storage at normal view distance | 16,908,288 bytes (about 16.1 MiB) |
| Normal-distance streaming | Maximum observed main-thread lighting update 4.603 ms; 5,633 accepted worker solves, 16.288 seconds accumulated worker time across loading/convergence |

An uncapped stationary scene comparison measured median total frame times of 4.086 and 3.641 ms with skylight sampling, versus 3.863 ms with sampling bypassed; p95 values were 6.001, 5.777 and 5.247 ms respectively. Run-to-run variation overlaps the difference. These are total frame timings, not isolated GPU timestamps or proof of a fixed shader overhead. The Editor and other workstation activity remained present.

Uniform light pages share CPU arrays and occupy only a value in the GPU lookup table. Nonuniform pages use one packed byte per voxel. Work runs after edits, source changes or residency; day/night changes reuse the cache. Updates are queued, so large loading/edit batches can precede their final lighting. The observed maxima are not hard frame-budget guarantees.

## Compatibility and remaining limits

No save schema, item/recipe definition or terrain generator version is changed. Light is derived after loading and does not regenerate old chunks. The existing surface geometric-skylight growth rule remains; this task does not introduce night-only growth restrictions, moisture or offline growth. Growth uses placed sources, while held torches remain a viewing light. Rendering retains the existing finite point-light pools and lava emission; this is not physically based multiple-bounce lighting.

Long-distance exploration beyond the measured view radius, very large simultaneous terrain edits and artistic acceptance remain review limits. The player guide is [Lighting and underground farms](../wiki/Lighting-and-underground-farms.md). The same player passed **183 save/recovery/rollback assertions** and **five fresh-process continuation assertions**, with clean exits: [save regression](lighting-2026-09-13/save-regression.json), [save exit](lighting-2026-09-13/save-regression-exit.txt), [restart](lighting-2026-09-13/save-restart.json), [restart exit](lighting-2026-09-13/save-restart-exit.txt). Wiki deployment is recorded below when complete.

## Reproduction

With the existing pinned Editor in Edit mode, run `Tools/Verify-Lighting.ps1 -Build -OutputDirectory <fresh-directory>`. The script builds the Lighting player and runs the focused gameplay/capture/performance fixture. `Tools/Verify-Saves.ps1 -Executable <Lighting-player> -OutputDirectory <fresh-directory>` runs save/recovery/rollback followed by a separate-process continuation.
