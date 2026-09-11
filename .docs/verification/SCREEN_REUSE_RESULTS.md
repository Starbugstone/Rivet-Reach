# Retained crafting screens — 2026-09-11

The HUD, inventory shell, station layouts and recipe-detail widgets are reused. Every inventory publication binds the current session and station before rendering; pending pointer gestures from previous bindings are rejected. [CRAFTING.md](../CRAFTING.md#interface-performance) owns the implementation contract and adjacent-inventory boundary.

## Measured opening cost

On the focused Windows Development player, warm inventory opening plus managed canvas flush measured **4.267 ms median / 4.419 ms p95**, compared with the earlier **24.78–34.96 ms medians**. This run meets the local warm-opening target of p95 below 5 ms; [earlier repeat runs](screen-reuse-2026-09-11/earlier-timing-runs.json) ranged from 4.527 to 5.543 ms p95, so the target is not consistently achieved across workstation runs. These are separate workstation runs, not a universal latency guarantee or an isolated FPS comparison.

| Action | Samples | Median | p95 | Maximum |
| --- | ---: | ---: | ---: | ---: |
| Open inventory and flush canvas | 12 | 4.267 ms | 4.419 ms | 6.193 ms |
| Craft transaction | 48 | 0.0006 ms | 0.0013 ms | 0.343 ms |
| Refresh crafting UI and flush canvas | 48 | 0.146 ms | 0.211 ms | 1.975 ms |
| Open recipe detail and flush canvas | 12 | 1.235 ms | 1.439 ms | 11.117 ms |

[Action samples](screen-reuse-2026-09-11/crafting-timings.csv) retain cold outliers. Staged preparation covers the common shell, sidebar, cursor, recipe widgets, an initial recipe binding and the personal grid during title/terrain loading. Unvisited station layouts remain lazy; their first opening can cost more. Initial preparation, first-use native/JIT work and portrait changes are not covered by the warm-opening target.

[Frame counters](screen-reuse-2026-09-11/crafting-frame-counters.csv) separately sample 120 frames after 30 warm-up frames per phase. Repeated close/open every frame measured GPU time at 4.092 ms median, versus 3.534 ms idle. Main-thread frame duration was 11.459 ms median / 13.405 ms p95 during repeated opening, versus 11.100 ms median idle, including the 90 FPS pacing limit; it is not an uncapped CPU-throughput result. The UI rebuild marker includes both switches per sampled frame. Parent/worker timings overlap and must not be added. `GC.Alloc` uses time units, not bytes or allocation count; zero native counters do not prove zero native work.

Hardware/workload: Unity 6000.4.4f1, i7-10750H, RTX 2060, 32 GB RAM, Direct3D 12, 1280×720, seed 246813, view radius 4, settled terrain, natural mob spawning disabled for the focused fixture. The [previous crafting report](CRAFTING_UX_RESULTS.md) retains the reconstruction baseline and its original build identity.

## Stability and conservation

The [native runtime report](screen-reuse-2026-09-11/runtime-report.json) passes **829 assertions**. It includes the existing drag/fill/browser suite and these additional checks:

- 100 warmed inventory reopenings retain the same backpack widgets and search results, with zero new widgets.
- Changing search results invalidates a press on the reused sidebar icon before it can open or fill the replacement recipe. Unused sidebar cells clear their item identities; clearing the search restores every visible icon and texture.
- Hidden inventory rendering, raycasting and Selectable components are disabled. EventSystem hits exclude the hidden view.
- Real mouse presses, left-drag and right-paint gestures canceled across close/reopen cannot pick up or deposit into the rebound view. Cursor and grid ingredients return exactly once.
- Two placed workbenches with deliberately equal revisions share the same UI widgets. The second shows its own cobblestone and an empty result, replacing the first bench's log/plank recipe before the next frame. Old result presses cannot craft on either bench.
- Thirty full-inventory bench switches preserve ingredients and create no widgets. Thirty recipe navigation cycles create no widgets, and an empty recipe clears old result identities.
- Same-type machine buttons rebind their labels and resolve the currently validated machine. Stale releases affect neither machine; a new click affects the current machine once.
- Loading an isolated checkpoint replaces inventory authority while retaining the widgets. Pre-load input cannot transfer items between sessions. Creative toggles, appearance changes and a new session clear inappropriate controls, old cursor items and recipe previews.

The item transaction layer remains authoritative. [Transfer checks](screen-reuse-2026-09-11/transfer-checks.txt) pass **1,105 assertions**, [crafting checks](screen-reuse-2026-09-11/crafting-checks.txt) pass **1,158,852**, [starter acceptance](screen-reuse-2026-09-11/starter-recipe-checks.txt) passes **1,862**, and [browser index checks](screen-reuse-2026-09-11/index-checks.txt) pass **368**. Existing multisource atomicity/alias checks remain in place; gameplay still supplies only the backpack. Live adjacent-inventory discovery is WIP.

## Broader regressions

The [regression build identity](screen-reuse-2026-09-11/regression-build-identity.json) identifies the preceding player. Its production code differs from the final player only in the subsequent sidebar icon visibility correction; the final browser suite verifies that correction. Reports and process exit codes below belong to that preceding binary.

[Process exits](screen-reuse-2026-09-11/regression-exits.json):

| Scenario | Assertions | Renderer | Result / exit |
| --- | ---: | --- | --- |
| [save](screen-reuse-2026-09-11/save-report.json) | 183 | Direct3D11 | PASS / 0 |
| [save-resume](screen-reuse-2026-09-11/save-resume-report.json) | 5 | Direct3D11 | PASS / 0 |
| [creative](screen-reuse-2026-09-11/creative-report.json) | 312 | Direct3D12 | PASS / 0 |
| [survival](screen-reuse-2026-09-11/survival-report.json) | 93 | Direct3D12 | PASS / 0 |
| [industry](screen-reuse-2026-09-11/industry-report.json) | 89 | Direct3D12 | PASS / 0 |
| [multiblock](screen-reuse-2026-09-11/multiblock-report.json) | 140 | Direct3D12 | PASS / 0 |

The initial Direct3D 12 save-resume attempt completed all five checks but exited with `-1073741819` during shutdown, including one repeat. This matches the previously documented [D3D12 shutdown limitation](SAVE_RESULTS.md#reproduce-and-limits). The clean save/Continue regressions use `-force-d3d11`, matching the release renderer choice; no new renderer fix is claimed. An earlier survival fixture counted hidden cached meters; the corrected fixture selects the active Canvas. These failed attempts are not counted as successful regressions.

## Artifact and visual evidence

Review player: `Builds/RecipeBrowser/RivetReach.exe`. [Build identity](screen-reuse-2026-09-11/build-identity.json) records the actual assembly and isolated source hashes; [build summary](screen-reuse-2026-09-11/build-summary.txt) reports zero errors and warnings. The snapshot includes the separately authored survival HUD changes. The later concurrent dropped-stack presentation commit is outside this build; focused reports identify their own tested scope. The user's open Editor and recovery files were preserved.

Inspected captures:

- [Rebound second workbench, with no phantom output](screen-reuse-2026-09-11/rebound-workbench-no-phantom.png)
- [New session with clean inventory and personal grid](screen-reuse-2026-09-11/new-session-clean-inventory.png)
- [Reused processing recipe detail](screen-reuse-2026-09-11/crusher-recipe.png)
- [Sidebar and crafting at 1024×768](screen-reuse-2026-09-11/sidebar-1024x768.png)

## Reproduce and remaining limits

Run `powershell -File Tools/Verify-RecipeBrowser.ps1 -Build -OutputDirectory <fresh Windows path>`. It builds an isolated project and runs the native pointer suite and timing phases. Save regression uses `Tools/Verify-Saves.ps1 -Executable <review player> -OutputDirectory <fresh Windows path>`; Creative/survival use the existing `-rr-creative-review` / `-rr-survival-review` verification modes.

Retained views trade bounded memory for faster reopening. Station caches are keyed by immutable layout/type, never by placed instance. Zero widget growth was checked for the workloads above; this is not a total allocation or large-catalog memory certification. Save/load/settings menus still rebuild when opened. The Creative list still repopulates when its own search changes. Large saves, active factories, other hardware and subjective playfeel remain separate validation. Automated conservation checks provide evidence for the covered transitions, not a claim that every possible future bug has been excluded.
