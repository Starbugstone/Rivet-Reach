# 0.1.0 Alpha release verification — October 3, 2026

The owner explicitly requested a release build, GitHub tag and downloadable alpha after reviewing the earlier release hold. This authorizes publication for playtesting with the known limits; it does **not** establish steady 60 FPS, balance acceptance or extended-session stability. [Release notes](../releases/0.1.0.md) and [the player download guide](../wiki/Alpha-0.1.0.md) explain those limits and saving.

## Source and build

Compiled source: `0ca023355fa46d3f58c1c98ff5ffdc1a90e32444`. The release checkout starts from current remote `main` (`14fdac7`) and adds version/build/packaging changes and a cold-Editor verification correction. Gameplay code, recipes, assets, generation and save format are unchanged. Pre-existing edits in the main workspace were excluded and preserved. Later documentation/evidence commits contain the package record; the payload manifest retains the actual compiled source identity.

Unity **6000.4.4f1**, Windows x64, Mono, **non-Development**, Direct3D 11, version **0.1.0**, schema **19**. There are no `RR_VALIDATE_WORLD` extra defines or isolated shader reference assets in the player. [Build summary](alpha-0.1.0-2026-10-03/build-summary.txt): Succeeded; errors 0; warnings 0; seconds 1781,2887435; bytes 335387289.

Managed assembly SHA-256: `c9ca7c9a1056f7207ea633e01e6f821416704b376dc7a525b0a2e26ec96c4a44`. [Build identity and source trees](alpha-0.1.0-2026-10-03/build-identity.json) tie every listed runtime check to this candidate.

**52/52 Editor suites passed**, including current/legacy content checks, conservation, recipes, terrain, creatures, lighting and the performance-plan differential checks. [Suite results](alpha-0.1.0-2026-10-03/domain-summary.txt) and [shader metadata checks](alpha-0.1.0-2026-10-03/worldlit-batching-checks.txt) retain the measured output.

A copied import cache initially failed to resolve asset catalogs, so the final player uses a fresh asset database. A concurrent-build memory-pressure attempt crashed during FBX animation import; limiting this Editor to one job worker allowed import to complete. This is a build-time setting, not a runtime optimization measurement. The cold batch shader check also needed an actual SRP preview render before reading native compatibility metadata; its pass/error/compatibility assertions remain intact. The successful build follows these corrections. No failed-attempt player was packaged.

## Native player checks

The ordinary five-minute Survival route ran before focused native checks, without Creative or supplied ingredients. [Route observations](alpha-0.1.0-2026-10-03/native/alpha-survival/survival-observations.txt) record gathering, crafting, exploration and mining. All runs used isolated save/evidence directories. Save continuation starts a fresh process. Historical fixtures exercise complete saves from schemas **1–17**; current/newer compatibility checks are also part of the Editor gate and save checks. These are scripted correctness checks on one Windows workstation, not a completed human progression/balance review or a comparative FPS experiment.

| Scenario | Assertions | Result / exit |
| --- | ---: | --- |
| [alpha-survival](alpha-0.1.0-2026-10-03/native/alpha-survival/runtime-report.json) | 7 | PASS / 0 |
| [save](alpha-0.1.0-2026-10-03/native/save/runtime-report.json) | 183 | PASS / 0 |
| [save-resume](alpha-0.1.0-2026-10-03/native/save-resume/runtime-report.json) | 5 | PASS / 0 |
| [bed](alpha-0.1.0-2026-10-03/native/bed/runtime-report.json) | 237 | PASS / 0 |
| [browser](alpha-0.1.0-2026-10-03/native/browser/runtime-report.json) | 937 | PASS / 0 |
| [chicken](alpha-0.1.0-2026-10-03/native/chicken/runtime-report.json) | 321 | PASS / 0 |
| [connections](alpha-0.1.0-2026-10-03/native/connections/runtime-report.json) | 408 | PASS / 0 |
| [crates](alpha-0.1.0-2026-10-03/native/crates/runtime-report.json) | 137 | PASS / 0 |
| [fluid](alpha-0.1.0-2026-10-03/native/fluid/runtime-report.json) | 36 | PASS / 0 |
| [industry](alpha-0.1.0-2026-10-03/native/industry/runtime-report.json) | 89 | PASS / 0 |
| [inventory-gestures](alpha-0.1.0-2026-10-03/native/inventory-gestures/runtime-report.json) | 31 | PASS / 0 |
| [multiblock](alpha-0.1.0-2026-10-03/native/multiblock/runtime-report.json) | 140 | PASS / 0 |
| [performance-plan](alpha-0.1.0-2026-10-03/native/performance-plan/runtime-report.json) | 19 | PASS / 0 |
| [release-legacy](alpha-0.1.0-2026-10-03/native/release-legacy/runtime-report.json) | 87 | PASS / 0 |
| [renewables](alpha-0.1.0-2026-10-03/native/renewables/runtime-report.json) | 147 | PASS / 0 |
| [tools](alpha-0.1.0-2026-10-03/native/tools/runtime-report.json) | 252 | PASS / 0 |
| [weather](alpha-0.1.0-2026-10-03/native/weather/runtime-report.json) | 73 | PASS / 0 |
| **Total before packaging** | **3109** | **17 reports passed** |

## Current captures

Actual 0.1.0 standalone player, October 3, 2026. These images retain this release's identity rather than relabeling earlier feature captures.

![Resource-paid first Workbench](../wiki/images/alpha-0.1.0/survival-workbench.png)

![Saved frame-pacing choices](../wiki/images/alpha-0.1.0/settings.png)

## Package verification

Archive extraction, downloaded-byte comparison and publication evidence are recorded here after the final packaging checks.

## Reproduction and remaining limits

Use a clean checkout with LFS content and the pinned Editor. `Tools/Build-Release.ps1` runs the full Editor gate and builds `Builds/Release/0.1.0/RivetReach-0.1.0-alpha-windows-x64/RivetReach.exe`. Its default `-EditorJobWorkers 1` bounds Editor concurrency; [Unity documents that switch](https://docs.unity3d.com/6000.4/Documentation/Manual/EditorCommandLineArguments.html). Keep the entire player folder intact.

Run `Tools/Verify-ReleaseReview.ps1` with the scenarios above, first running `alpha-survival` separately. Use `Tools/Verify-Saves.ps1` for `save`/`save-resume`; pass `.docs/verification/release-review-2026-09-19/legacy` as the historical fixture directory. All runners need fresh output directories. Package only the checked executable using `python3 Tools/package_alpha.py <player-folder> <fresh-package-directory> --source-commit 0ca023355fa46d3f58c1c98ff5ffdc1a90e32444`. Packaging rejects changed build inputs and records version/schema from the source.

The [September timing audit](RELEASE_READINESS_0_1_0.md), [October gameplay review](ALPHA_0_1_0_GAMEPLAY_REVIEW.md) and [performance-plan report](PERFORMANCE_PLAN_RESULTS.md) retain their dated findings. No new steady-60 acceptance, second-machine result, minimum-hardware specification, multi-hour memory/GC result or final art/balance acceptance is claimed. Public 0.0.1 remains unchanged and session-only; this is a new release with durable saves.
