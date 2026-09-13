# Cave lighting and crop growth verification — 2026-09-13

Scope: [cave lighting](../LIGHTING.md) and [underground crop growth](../FARMING.md). Tested with Unity 6000.4.4f1, the Windows development player at `Builds/Lighting/RivetReach.exe`, Direct3D 11, RTX 2060 and i7-10750H. The fixture uses actual world edits and survival/industry systems in a constructed enclosed farm. The checkout also contained concurrent Floater spawning work; the focused farm fixture freezes mobs.

## Gameplay and rendering

The [player report](lighting-2026-09-13/runtime-report.json) passes **686 assertions** with [exit 0](lighting-2026-09-13/exit-code.txt). It checks all five plantable species reaching maturity under torches, due crops waiting in darkness, growth independent of the torch presentation pool, source removal, solid-wall occlusion and reopening, powered-lamp growth and power-loss darkening, compost in torchlight, vertical skylights, lateral entrances, roof replacement and rebuilding light after saving/loading. It also checks that neither unchanged terrain nor day/night presentation changes launch repeated light solves.

The original captures were visually inspected for sealed caves, growing/mature torch farms, open shafts and side entrances. Their images have been superseded by the current captures below; the numerical reports retain their original build identities. GPU interpolation smooths the light field while preserving voxel occlusion and greedy terrain meshes.

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

Long-distance exploration beyond the measured view radius, very large simultaneous terrain edits and artistic acceptance remain review limits. The player guide is [Lighting and underground farms](../wiki/Lighting-and-underground-farms.md). The same player passed **183 save/recovery/rollback assertions** and **five fresh-process continuation assertions**, with clean exits: [save regression](lighting-2026-09-13/save-regression.json), [save exit](lighting-2026-09-13/save-regression-exit.txt), [restart](lighting-2026-09-13/save-restart.json), [restart exit](lighting-2026-09-13/save-restart-exit.txt).

## Reproduction

With the existing pinned Editor in Edit mode, run `Tools/Verify-Lighting.ps1 -Build -OutputDirectory <fresh-directory>`. The script builds the Lighting player and runs the focused gameplay/capture/performance fixture. `Tools/Verify-Saves.ps1 -Executable <Lighting-player> -OutputDirectory <fresh-directory>` runs save/recovery/rollback followed by a separate-process continuation.

## Published player guide

[Wiki deployment 34762946143](https://github.com/Starbugstone/Rivet-Reach/actions/runs/34762946143) succeeded from source commit `cd2d72524ceae81066e0890a6fa7ec83b4ae4c6a`, publishing wiki commit `6dc8a4d`. Validation passed for 200 pages and 8,112 local links/images (177 item pages, 163 recipes).

The [live lighting guide](https://github.com/Starbugstone/Rivet-Reach/wiki/Lighting-and-underground-farms) was checked in Chromium on 2026-09-13 at desktop and 390×844 mobile sizes. All five gameplay captures loaded at their original 1,280-pixel width; neither viewport had document overflow. [Desktop capture](lighting-2026-09-13/wiki-live-guide.png) and [mobile capture](lighting-2026-09-13/wiki-live-mobile.png) were visually inspected. Its Torch and Workshop Lamp item links both returned HTTP 200 and contained the updated crop-light instructions. [Deployment record](lighting-2026-09-13/wiki-deployment.json) preserves the source and checks.

## Brighter torches follow-up — 2026-09-13

User feedback requested more torch light. The shared held/placed light prefab now uses intensity **9 instead of 3** and range **14 instead of 10 blocks**, with its existing warm colour, shadows and bounded light pool. The original prefab authoring defaults match. This is presentation tuning; crop thresholds, source propagation and saves are unchanged.

The rebuilt `Builds/Lighting/RivetReach.exe` completed with [zero build errors/warnings](torch-brightness-2026-09-13/build-summary.txt); [build hashes](torch-brightness-2026-09-13/build-identity.json) include the resources asset. The existing cave fixture passed [686 assertions](torch-brightness-2026-09-13/runtime-report.json) with [exit 0](torch-brightness-2026-09-13/exit-code.txt). Its [raw timings](torch-brightness-2026-09-13/lighting-performance.txt) belong to this brighter build; earlier save and Editor checks above retain their earlier build identity.

The farm captures from this build were visually compared with the previous run: wall/floor coverage is clearer and terrain shadows remain visible. The held light uses the same prefab; a separate held-light playtest was not repeated. Increasing light range can affect rendering cost; this run does not isolate that cost or establish a dense-room performance guarantee. Player preference remains the final brightness check.

[Wiki deployment 34764027245](https://github.com/Starbugstone/Rivet-Reach/actions/runs/34764027245) passed from `dd7df1b`, publishing wiki commit `c9bce7e`. Validation passed for 200 pages and 8,112 local links/images. Live Chromium inspection confirmed the fourteen-block guidance, but byte comparison found GitHub serving cached older farm images despite updated query hashes. The two farm image paths were renamed to invalidate that cache; final live verification follows.

The [final screenshot deployment 34764176614](https://github.com/Starbugstone/Rivet-Reach/actions/runs/34764176614) passed from `610bfc9`. Live Chromium loaded both renamed farm captures, and fetched SHA-256 hashes matched the local images (`be44af9da5a3…` and `542e3d7e6fc2…`). The refreshed [live screenshot](torch-brightness-2026-09-13/wiki-live.png) was visually inspected and shows the brighter walls and floor.


## Dim ambient, distant activation and Workshop Lamps — 2026-09-13

The current follow-up adds a faint cave visibility floor independent of the world clock, dim moonless outdoor visibility, and a seven-block warm ambient fill from the selected torch. Torch point intensity is now 27 (another 3× increase); range remains 14 blocks. Placed lights fade smoothly from zero at 64 blocks to full strength at 48. Workshop Lamps now use intensity 30 and range 20 (previously 2.5 and 7), retaining received-power scaling, 20 W demand and eight active lights. Lamps retain their existing unshadowed presentation. Visual fill never changes crop or hostile-spawn light.

The final player was built in the coordinated warm `Rivet-Reach-FloaterCaves` checkout and copied to `Builds/Lighting`. The [build summary](ambient-lighting-2026-09-13/build-summary.txt) reports zero errors/warnings. [Build identity](ambient-lighting-2026-09-13/build-identity.json) and [matching source hashes](ambient-lighting-2026-09-13/source-hashes.json) identify this artifact. The ordinary main Editor had returned `Unknown` without build errors; using the isolated checkout preserved its open work. Concurrent mob evidence remains attached to its own earlier frozen player in [mob verification](MOB_LIGHT_RESULTS.md).

The [native report](ambient-lighting-2026-09-13/runtime-report.json) passes **715 assertions**, with [exit 0](ambient-lighting-2026-09-13/exit-code.txt). It includes every original cave/crop check plus constant day/night cave visibility, actual held-fill pixel brightening, selection cleanup, observer-distance fading, powered lamp radius/intensity and shutdown, moonless visibility, shared spawn-query day/night values and cross-border freshness. The [moonless outdoor capture](ambient-lighting-2026-09-13/surface-new-moon-dim.png) remains dimly visible with zero directional-light intensity.

[Final raw timings](ambient-lighting-2026-09-13/lighting-performance.txt) measured 0.0005 ms mean idle main-thread lighting work at both 332 and 3,051 resident chunks, with no new solves. At normal view distance 10, lighting GPU storage was 16,908,288 bytes; the maximum lighting main-thread update across streaming was 4.546 ms. Worker solves accumulated 15.000 seconds across loading/convergence. Total-frame medians with sky sampling were 3.893 and 3.752 ms, versus 3.365 ms bypassed; these include workstation/frame variation and do not isolate GPU cost or prove a fixed shader overhead.

Current in-game captures: [dim sealed cave](../wiki/images/lighting/sealed-cave-ambient.png), [the same cave at night](ambient-lighting-2026-09-13/sealed-cave-night.png), [held torch](../wiki/images/lighting/held-torch-ambient.png), [held ambient alone](ambient-lighting-2026-09-13/held-torch-ambient-only.png), [mature torch farm](../wiki/images/lighting/torch-farm-mature-ambient.png), [powered lamp](../wiki/images/lighting/workshop-lamp-wide.png) and [unpowered lamp](../wiki/images/lighting/workshop-lamp-unpowered.png). These were visually reviewed. The held-ambient-only capture temporarily disables the point light to isolate the warm fill. The distance comparisons keep the camera fixed and move the lighting observer: [56 blocks](ambient-lighting-2026-09-13/torch-distance-56.png), [47 blocks](ambient-lighting-2026-09-13/torch-distance-47.png).

The full lamp update does not change simulation emission, save format or terrain generation. Earlier broad save and Editor suites above retain their original artifact dates. The bounded point-light pools can still replace sources in very dense scenes; this task does not establish a worst-case rendering budget. Larger lamp reach increases possible pixel work. Held fill approximates bounce light and does not perform a separate wall-occlusion solve. Artistic preference and dense-scene performance remain review limits.
