# Floater and shared spawning verification — 2026-09-13

[Mob rules](../MOBS.md#shared-hostilepassive-spawning-rules) · [Floater guide](../wiki/Floater.md) · [Mobs overview](../wiki/Mobs.md)

The current review player is **`Builds/FloaterCaves/RivetReach.exe`**, built with Unity **6000.4.4f1 / URP**, with **0 errors and 0 warnings**. [Build summary](mob-spawning-2026-09-13/build-summary.txt) and [artifact/source hashes](mob-spawning-2026-09-13/artifacts.json) identify the tested build. Verification used an isolated checkout based on `8629bab` plus this task’s changes, excluding concurrent lighting edits in the shared checkout. After lighting commit `8eee7c9` landed, the combined source compiled successfully for a fresh Unity wiki export. The runtime reports and review executable retain the isolated pre-lighting artifact identity above.

## Measured results

- [Full native mob suite](mob-spawning-2026-09-13/mob-runtime-report.json): **140 assertions passed**, no errors, at **14:21:01 UTC**. Covers beetle/prowler spawning, combat, climbing, collision, lifecycle and player-input regressions, plus all new habitat and support-block checks.
- [Focused Floater suite](mob-spawning-2026-09-13/focused-runtime-report.json): **63 assertions passed**, no errors, on the same executable. Covers natural underground spawning, day/night eligibility, surface/shelter/shaft/liquid/ceiling rejection, all three reusable habitat profiles, grass-only support in both environments, population caps, hover navigation, combat, exact single-rock loot and save/load.
- [Asset and save checks](mob-spawning-2026-09-13/asset-and-save-checks.txt): **13 checks passed**. Actual retained pre-Floater schema-7 and pre-habitat schema-10 checkpoints remain readable. Unrelated habitat, support-list, timing, mob-health and item-stat changes are still rejected. Schema-10 entity/world payloads are unchanged.

The reusable profile has no hostile AI/state dependency. Hostile mobs consume it now; no passive animal population exists yet. Future passive callers must combine it with their separate persistent lifecycle. Numeric search/population limits remain working defaults; these bounded tests do not establish long-session performance or encounter balance. The runtime report’s timing counters belong to the last restored mob system and are not aggregate benchmarks.

## Reviewed visuals

![A Floater hovering inside a generated underground cave](mob-spawning-2026-09-13/floater-cave.png)

This is an actual **2026-09-13** native-player capture inside a generated cave, with a temporary point light for review. The shaft test removes overhead cover and refills non-placeable ore cells with stone before capture. The natural-spawn assertions run separately from this deliberately framed encounter. The image demonstrates the underground setting and supported hover; it is not a new art-authoring pass.

The original models and icons are unchanged. Retained [Unity import evidence](floater-2026-09-13/import-report.txt) records the Floater’s 1,266 triangles, seven bones, one material and four actions, and the rock’s 116 triangles. Earlier same-day Blender [front](floater-2026-09-13/blender-front.png) and [back](floater-2026-09-13/blender-back.png) renders remain the source-art review. The [rock-drop](floater-2026-09-13/floater-rock-drop.png), [held-rock](floater-2026-09-13/floater-rock-held.png) and [inventory](floater-2026-09-13/floater-rock-inventory.png) captures retain their earlier build identity; combat balance and art acceptance remain playtest concerns.

## Reproduce

In an idle pinned Editor, request `floater-caves-build` through `Logs/build-request.txt`. An isolated, unopened project can run `RivetReach.Editor.ProjectBuild.BuildFloaterCaves` in batch mode. Run `Tools/Verify-Mobs.ps1` or `Tools/Verify-Floater.ps1` with the new executable and a fresh output directory. The legacy checkpoint directories named in the check log are local retained fixtures, not bundled player saves.

## Wiki publication

[Deployment run 34762827286](https://github.com/Starbugstone/Rivet-Reach/actions/runs/34762827286) succeeded, publishing wiki commit `6d3e2a7` from game commit `8496554`. Its checkout validated **199 pages and 8,098 local links/images**, with 177 item pages and 163 recipes.

Live Chromium review confirmed the cave-only guide text and all three in-game images, followed the Floater Rock link to its updated acquisition text and loaded inventory icon, and verified the Mobs page’s shared hostile/passive habitat and support-block explanation. All three checked pages fit a **390×844** viewport without horizontal document overflow. Reviewed captures: [Floater guide](mob-spawning-2026-09-13/wiki-floater-mobile.png), [Floater Rock](mob-spawning-2026-09-13/wiki-rock-mobile.png).
