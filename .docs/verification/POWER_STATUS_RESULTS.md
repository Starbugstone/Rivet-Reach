# Electrical connection and power status — 2026-09-12

> The pump power requirement and electrical UI captured here were superseded on 2026-09-12. See [pumps without electricity](PUMP_RESULTS.md) for current behavior and fresh evidence; this report retains its dated checks of other workshop features.

The machine interface now reports **Power network: connected / not connected** separately from supplied/requested watts and operating status. A registered connection with an empty battery shows **No electrical power**, not a world-wide “Connecting networks” message. [Industry rules](../INDUSTRY.md#simulation-and-rendering) and the [player guide](../wiki/Pipes.md#connected-network-versus-available-electricity) own the behavior.

## Implementation and boundaries

Each channel builds replacement topology privately and publishes only a completed result. Yielding or cancelling a traversal cannot expose half-built connections or erase the last registered network. Connection lookup checks both the registered machine instance and its connected-face mask; a replacement at the same location cannot inherit the removed instance's registration. Electrical generation and allocation never determine that connection flag.

Eligible machines retain their last completed operating/allocation state during a rebuild. Dormant machines clear transient power immediately. Resource production still pauses while rebuilding; no energy or processing is replayed. This change does not remove the existing world-wide rebuild pause or claim large-factory performance. Serialization, recipes, wrench interaction, signal semantics and energy allocation rules are unchanged.

## Measured verification

- Unity **6000.4.4f1**, Windows x64 Development, Direct3D11; [build](power-status-2026-09-12/build.txt) succeeded with **0 errors and 0 warnings**, 32.871 seconds. Updated player: `Builds/Connections/RivetReach.exe`. [Source and artifact hashes](power-status-2026-09-12/artifact-hashes.json) identify the tested candidate over `a4679ba`.
- [166 connection assertions](power-status-2026-09-12/checks.txt) passed. Added checks cover sustained blackout without re-registration, partial power, depletion, partial/cancelled graph publication, a budgeted rebuild caused by 800 unrelated signal nodes, cable removal and replacement identity.
- Existing [battery](power-status-2026-09-12/battery-checks.txt), [crank](power-status-2026-09-12/hand-crank-checks.txt), [door](power-status-2026-09-12/door-checks.txt), [multiblock](power-status-2026-09-12/multiblock-checks.txt), [connected-pipe](power-status-2026-09-12/connected-pipe-checks.txt), [domain](power-status-2026-09-12/domain-checks.txt) and [starter-recipe](power-status-2026-09-12/starter-recipe-checks.txt) suites passed.
- [Native player report](power-status-2026-09-12/runtime-report.json): **187 assertions**, **exit 0**, no recorded errors, 1280×800. The actual crusher panel shows connected/0 W, remains unchanged during a global dirty state, switches to Running/160 W when supplied, returns to No electrical power on depletion, and reports not connected after physical cable removal. The existing held-wrench and schema-4 save round-trip checks also run in this scenario.
- Inspected [connected blackout](power-status-2026-09-12/power-connected-empty.png) and [pump diagnostics](power-status-2026-09-12/pump-network-status.png); the added connection line fits alongside power, water and intake information. [Disconnected cable](power-status-2026-09-12/power-cable-disconnected.png) retains the same zero-power operating status with a separate disconnected label.

Reproduce through **Rivet Reach → Build configurable connections review**, then `Tools/Verify-Connections.ps1 -OutputDirectory <fresh directory>`. Earlier [connection/art evidence](CONNECTION_RESULTS.md) and save-resume reports retain their original build identities; they are not relabeled as new measurements. The wiki export and affected electricity/pipe guides were refreshed alongside the runtime change.

[Wiki deployment 34692483243](https://github.com/Starbugstone/Rivet-Reach/actions/runs/34692483243) passed from `d365e3c`, publishing wiki `8705fa1`. The deployed files match the reviewed publisher output; both the electricity and Pipes guides explain connection status separately from supplied power.
