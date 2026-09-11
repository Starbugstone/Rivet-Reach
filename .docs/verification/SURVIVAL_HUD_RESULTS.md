# Food, armor icons and healing — 2026-09-11

The user selected food/armor icon meters and revised healing to start at **60% food**, continue through **55%**, and stop at **50% or below**. [Gameplay](../GAMEPLAY.md#survival-progression-farming-health-and-armor) owns behavior; [save compatibility](../SAVES.md#storage-and-compatibility) owns the persisted healing flag.

## Build and measured checks

Local review executable: **`Builds/SurvivalHUD/RivetReach.exe`**, Unity **6000.4.4f1**, Windows development build. The [build summary](survival-hud-2026-09-11/build-summary.txt) records zero errors and zero warnings. [Build context and source hashes](survival-hud-2026-09-11/build-context.json) identify the tested assembly and shared workspace, which also contained concurrent inventory-screen reuse changes. This report certifies the focused survival workload, not that separate feature.

- [Survival domain checks](survival-hud-2026-09-11/survival-checks.txt): **47,634 assertions passed**, including exact passive hunger, healing start/continue/stop thresholds, eating to restart, no early healing, no healing surcharge at full health, batched/individual tick equivalence and positive monotonic armor mitigation for small and ordinary hits with protection beyond the cap.
- [Standalone survival run](survival-hud-2026-09-11/survival-runtime.json): **93 checks passed**, Direct3D 12, 1280×720. Covers real eating, equipment drag, damage, icon refresh after unequipping, fixed empty/full meters on respawn, station/farming regressions and unloaded station conservation. Focused binary checks preserve active healing at food 11, its partial timer, exhaustion and the next section boundary; schema 1 health loads without reading the new schema 2 flag.

- Schema 2 full-state save/load/recovery: [183 checks passed](survival-hud-2026-09-11/save-runtime.json); [fresh-process Continue](survival-hud-2026-09-11/save-resume-runtime.json): **5 checks passed**. Both Direct3D 11 runs exited cleanly ([save exit](survival-hud-2026-09-11/save-exit-code.txt), [resume exit](survival-hud-2026-09-11/save-resume-exit-code.txt)). The initial Direct3D 12 run passed the same 183/5 assertions, but its resume process exited with `-1073741819` after writing its passing report, consistent with the previously recorded renderer shutdown issue. The clean rerun used the same player assembly and runner with `-force-d3d11` added to both player launches.

## Inspected Unity captures

[HUD food and full armor](survival-hud-2026-09-11.png) · [Half-filled armor](survival-half-icons-2026-09-11.png) · [Equipment shields](survival-equipment-icons-2026-09-11.png) · [Full food and empty armor after respawn](survival-respawn-icons-2026-09-11.png).

The ten food silhouettes and ten shields are original code-authored UI meshes. Empty positions stay visible, odd values use half icons, and health retains its existing hearts. HUD and equipment protection counters no longer show numbers. The old numeric HUD/armor captures are superseded and retained in Git history.

## Reproduce and limits

With this project open in the pinned Editor, write `survival-hud-build` to `Logs/build-request.txt`; the existing local request handler runs `SurvivalChecks.Run` and builds the review player. Run `Tools/Verify-POC.ps1 -Survival -Executable <absolute path to Builds/SurvivalHUD/RivetReach.exe> -OutputDirectory <fresh absolute folder>`. Run save regressions with `Tools/Verify-Saves.ps1 -Executable <same executable> -OutputDirectory <fresh absolute folder>`. For the clean Direct3D 11 reproduction, add `-force-d3d11` to the player argument list for both `save` and `save-resume` scenarios (the local run used `Logs/Verify-SurvivalHUDSaves-D3D11.ps1`, a copy of that runner with only this flag added).

Timing remains working tuning: one idle food point per 102.4 seconds, one health point per four healing seconds, and six extra exhaustion per healed point. Long-session balance and user play acceptance remain unmeasured. These focused runs do not measure frame-time performance; zero frame timing fields in the runtime report mean unmeasured. The published alpha and other local build folders were not replaced.
