# Cable-defined grids and equal resource sharing — 2026-09-12

[Battery rules](../BATTERIES.md#cable-defined-grids--2026-09-12) own separate terminal grids, shared storage and surplus capture. [Industry](../INDUSTRY.md) owns the doubled **800 W** boiler/alternator output; fuel and water costs are unchanged.

## Behavior and checks

- Each machine face terminates its cable run. Only connected cables/power fittings join grids. Multiple connections share one generation, demand or battery-energy budget.
- Batteries charge from generator surplus and supply unmet loads on other attached grids in the same tick, including when empty or full. The former 400 W per-cell charge/discharge cap is removed; capacity, charge and battery modes still apply. Stored energy does not circulate into other batteries.
- The user's example supplies a crusher with 160 W while an 800 W generator adds exactly **32 J per 50 ms tick** to storage. Cutting the generator cable leaves the load running from stored charge; cutting the load cable stores all generation. Replacing a cable restores transfer automatically. Removing an unused branch preserves charging.
- The battery panel reports separate input/output watts. A cable panel reports that grid's actual input, requirements and connected stored energy. A battery attached to several grids represents the same reserve on every panel.

Unity **6000.4.4f1** ran the isolated project’s `PowerGridReview` checks: [615 shared-allocation assertions](power-grid-2026-09-12/grid-allocation-checks.txt), [48 battery assertions](power-grid-2026-09-12/battery-checks.txt), [393 industry assertions](power-grid-2026-09-12/industry-checks.txt), [648 connection assertions](power-grid-2026-09-12/connection-checks.txt), [33 crank assertions](power-grid-2026-09-12/hand-crank-checks.txt), [48 multiblock assertions](power-grid-2026-09-12/multiblock-checks.txt), [92,355 core assertions](power-grid-2026-09-12/domain-checks.txt), and [connected-pipe checks](power-grid-2026-09-12/connected-pipe-checks.txt). The battery suite adds 19 focused assertions for separate grids, split/rejoin, mode gates, full/empty capacity, shared generator/load budgets, 1,600 W surplus from two generators and 480 W discharge from one cell. Industry checks retain proportional shortages and priorities with six crushers against the increased supply, plus water-dependent startup and recovery.

## Playable artifact

The dedicated review player is **`Builds/PowerGrid/RivetReach.exe`**, copied from the isolated Creative player built over source base `cd9b8aa` plus this grid change. The already committed inventory expansion and rear fuel inputs are included; the subsequently committed machine-interface revision (`5cafbfb`) is outside this artifact. [Build record](power-grid-2026-09-12/build.txt): Windows x64 Development, zero errors and zero warnings, **40.266 seconds**; [completion](power-grid-2026-09-12/build-result.txt) at **14:21:00 UTC**. [Executable/assembly hashes](power-grid-2026-09-12/artifact-hashes.json) and [runtime source snapshot](power-grid-2026-09-12/source-snapshot.json) identify the tested candidate.

A fresh run from the dedicated PowerGrid folder passed [388 runtime assertions](power-grid-2026-09-12/runtime-report.json), [exit 0](power-grid-2026-09-12/exit-code.txt), Direct3D11 at 1280×800, with no recorded errors. It covers simultaneous 800 W input/160 W output, distinct grids, input-cable break and reconnection, battery and cable UI, the existing wrench interactions and exact saved battery energy after reload. Visually inspected [battery input/output](power-grid-2026-09-12/battery-separate-grids.png), [generator-side grid](power-grid-2026-09-12/power-input-grid.png) and [crusher-side grid](power-grid-2026-09-12/power-output-grid.png): diagnostics fit their actual panels.

## Equal sharing and common implementation

`FairAllocation<T>` owns identity-based capped equal allocation for electricity, items and fluids. It redistributes saturated/rejected shares and rotates indivisible residuals. Services retain resource-specific compatibility, modes, priorities, throughput and exact commits. `NetworkTopology` remains the shared physical connectivity implementation.

The 615 focused assertions include 300 generated capacity/budget cases with exact conservation and equal uncapped-share checks; three-battery surplus/deficit, empty/full/mode handling and rotating watt remainders; branched item/fluid sharing, limited source quantities, destination caps and unused-branch removal. Existing suites retain separate-grid conservation, no same-phase item/fluid forwarding, tank lifecycle, fluid conflicts and wrench/save behavior.

Native checks measure **320 W charging per battery** from 800 W generation and 160 W load, then **80 W discharge per battery** after generation stops. Two item sources deliver one item to each of two chests; two fluid outputs deliver 100 mL to each of two tanks. The final player also verifies all four pipe-end arrows after replacing the item fixture with fluid pipes at the same positions. Visual references now rebind to the replacement machine identity instead of retaining an inactive old pipe.

The maintained wiki includes the actual final-player [equal charge](../wiki/images/grid-battery-charge-2026-09-12.png), [equal discharge](../wiki/images/grid-battery-discharge-2026-09-12.png), [item branches](../wiki/images/grid-items-equal-2026-09-12.png) and [fluid branches](../wiki/images/grid-fluids-equal-2026-09-12.png) captures. All four were visually reviewed; the pipe captures show configured connections, while quantities are checked in the runtime assertions.

## Compatibility and limits

No serialized fields, content identities or save format changed. Exact cell energy and battery modes retain the existing durable-save representation; transient input/output diagnostics are rebuilt during simulation. Banks retain their shared cells, formation/recovery rules, dormant boundaries and charged-cell mining protection.

Allocation still pauses during a budgeted topology rebuild and resumes when the complete replacement is published; it does not replay missed production. This fix establishes cable-edit recovery, not uninterrupted production during arbitrarily large rebuilds. Equal transfer shares apply within each grid and, for items, among compatible receivers while respecting processing preferences. Allocation order between separate grids sharing a device remains deterministic. A formed battery bank counts as one endpoint; internal cell accounting is unchanged. Equal transfer rates do not equalize different starting charge levels. Existing industry workload timings are recorded in the check output, not a claim of whole-game performance or arbitrary factory scale.

Reproduce through `power-grid-build` in `Logs/build-request.txt` (Edit mode), or run the public `RivetReach.Editor.ProjectBuild.PowerGridReview` batch entry in an isolated checkout with the pinned Editor. These commands build `Builds/Creative`; the dedicated review copy above preserves its tested identity. Run `Tools/Verify-Connections.ps1 -Executable <player> -OutputDirectory <fresh directory>` for the native scenario. The existing packaged alpha release is unchanged.

## Wiki publication

[Deployment 34699389800](https://github.com/Starbugstone/Rivet-Reach/actions/runs/34699389800) succeeded from `14a1cc6` and published wiki commit `9248dda`. The combined wiki validates 144 pages and 5,750 local links/images. Live Chromium review confirmed the equal-sharing sections, all four new 1280-pixel gameplay captures loaded, and the battery guide's link opened the pipe sharing section. [Publication record](power-grid-2026-09-12/wiki-publication.json) retains the measured checks. A query refresh bypassed GitHub's cached pre-deployment electricity page.
