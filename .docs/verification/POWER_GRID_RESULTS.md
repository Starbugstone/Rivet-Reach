# Cable-defined power grids — 2026-09-12

[Battery rules](../BATTERIES.md#cable-defined-grids--2026-09-12) own separate terminal grids, shared storage and surplus capture. [Industry](../INDUSTRY.md) owns the doubled **800 W** boiler/alternator output; fuel and water costs are unchanged.

## Behavior and checks

- Each machine face terminates its cable run. Only connected cables/power fittings join grids. Multiple connections share one generation, demand or battery-energy budget.
- Batteries charge from generator surplus and supply unmet loads on other attached grids in the same tick, including when empty or full. The former 400 W per-cell charge/discharge cap is removed; capacity, charge and battery modes still apply. Stored energy does not circulate into other batteries.
- The user's example supplies a crusher with 160 W while an 800 W generator adds exactly **32 J per 50 ms tick** to storage. Cutting the generator cable leaves the load running from stored charge; cutting the load cable stores all generation. Replacing a cable restores transfer automatically. Removing an unused branch preserves charging.
- The battery panel reports separate input/output watts. A cable panel reports that grid's actual input, requirements and connected stored energy. A battery attached to several grids represents the same reserve on every panel.

Unity **6000.4.4f1** ran the isolated `ProjectBuild.PowerGridReview` simulation checks: [48 battery assertions](power-grid-2026-09-12/battery-checks.txt), [393 industry assertions](power-grid-2026-09-12/industry-checks.txt), [166 connection assertions](power-grid-2026-09-12/connection-checks.txt), [33 crank assertions](power-grid-2026-09-12/hand-crank-checks.txt), [48 multiblock assertions](power-grid-2026-09-12/multiblock-checks.txt), [92,355 core assertions](power-grid-2026-09-12/domain-checks.txt), and [connected-pipe checks](power-grid-2026-09-12/connected-pipe-checks.txt). The battery suite adds 19 focused assertions for separate grids, split/rejoin, mode gates, full/empty capacity, shared generator/load budgets, 1,600 W surplus from two generators and 480 W discharge from one cell. Industry checks retain proportional shortages and priorities with six crushers against the increased supply, plus water-dependent startup and recovery.

## Playable artifact

The dedicated review player is **`Builds/PowerGrid/RivetReach.exe`**. It is a byte-matching copy of the completed Connections player that includes the concurrently committed pipe-disconnection feature and these power changes. [Build record](power-grid-2026-09-12/build.txt): Windows x64 Development, zero errors and zero warnings, 48.739 seconds. [Executable/assembly hashes](power-grid-2026-09-12/artifact-hashes.json) and the retained [source snapshot](power-grid-2026-09-12/source-snapshot.json) identify the combined artifact; the isolated simulation run above is separate evidence.

A fresh run from the dedicated PowerGrid folder passed [316 runtime assertions](power-grid-2026-09-12/runtime-report.json), [exit 0](power-grid-2026-09-12/exit-code.txt), Direct3D11 at 1280×800, with no recorded errors. It covers simultaneous 800 W input/160 W output, distinct grids, input-cable break and reconnection, battery and cable UI, the existing wrench interactions and exact saved battery energy after reload. Visually inspected [battery input/output](power-grid-2026-09-12/battery-separate-grids.png), [generator-side grid](power-grid-2026-09-12/power-input-grid.png) and [crusher-side grid](power-grid-2026-09-12/power-output-grid.png): all diagnostics fit their actual panels.

## Compatibility and limits

No serialized fields, content identities or save format changed. Exact cell energy and battery modes retain the existing durable-save representation; transient input/output diagnostics are rebuilt during simulation. Banks retain their shared cells, formation/recovery rules, dormant boundaries and charged-cell mining protection.

Allocation still pauses during a budgeted topology rebuild and resumes when the complete replacement is published; it does not replay missed production. This fix establishes cable-edit recovery, not uninterrupted production during arbitrarily large rebuilds. Allocation order between separate grids sharing a device is deterministic; equal priority across independent grids and equal state of charge are not promised. Existing industry workload timings are recorded in the check output, not a claim of whole-game performance or arbitrary factory scale.

Reproduce through `power-grid-build` in `Logs/build-request.txt` (Edit mode), or run the public `RivetReach.Editor.ProjectBuild.PowerGridReview` batch entry in an isolated checkout with the pinned Editor. These commands build `Builds/Creative`; the dedicated review copy above preserves its tested identity. Run `Tools/Verify-Connections.ps1 -Executable <player> -OutputDirectory <fresh directory>` for the native scenario. The existing packaged alpha release is unchanged.
