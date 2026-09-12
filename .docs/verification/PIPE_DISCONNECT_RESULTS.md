# Disconnected pipe ends and wrench-only arrows — 2026-09-12

[Industry rules](../INDUSTRY.md#wrench-and-configurable-pipe-ends--2026-09-12) and the [Pipes guide](../wiki/Pipes.md) own the three-state cycle: **Input → Output → No connection → Input**. No connection blocks that terminal, retracts its pipe arm and hides its arrow. Wrench-click the same side to reconnect. Arrows appear only while the wrench is selected in hand. Other ends, pipe runs, power fittings and signal fittings remain independent.

## Tested artifact

Unity **6000.4.4f1**, Windows x64 Development, Direct3D11, `Builds/Connections/RivetReach.exe`. [Build result](pipe-disconnect-2026-09-12/build.txt): succeeded, **0 errors, 0 warnings**, 48.739 seconds. [Artifact hashes](pipe-disconnect-2026-09-12/artifact-hashes.json) identify this player and compiled game assembly. [Source snapshot](pipe-disconnect-2026-09-12/source-snapshot.json) records the working sources after verification; the build also includes concurrent battery/electrical work. Earlier [connection evidence](CONNECTION_RESULTS.md) retains its original build identity.

## Measured checks

- [Focused Editor checks](pipe-disconnect-2026-09-12/checks.txt): **648 assertions**, including both transport types on six faces and four rotations, disabled extraction/insertion, exact conservation, reconnection, residency rebuilds, rendered boundary gaps, replacement pipe runs and independent fitted channels.
- [Native player](pipe-disconnect-2026-09-12/runtime-report.json): **316 assertions**, [exit 0](pipe-disconnect-2026-09-12/exit-code.txt), no recorded errors. Real mouse presses disable and reconnect an aimed end without consuming the wrench or opening its interface. Holding Use does not repeat. Empty hands and another selected item hide all arrows; reselecting the wrench restores arrows only on connected ends. The same target remains accessible with the arm absent.
- The native scenario saves disabled item and fluid ends, loads every serialized field byte-for-byte, resumes simulation with those ends still disabled, and rejects an out-of-range direction word without changing the live world. Exact inventory, water and battery energy survive.
- [Earlier checkpoints](pipe-disconnect-2026-09-12/legacy-report.json): **21 assertions**, [exit 0](pipe-disconnect-2026-09-12/legacy-exit-code.txt). Actual schema-2/3 fixtures load and round-trip through the current schema; unrelated changed item definitions still reject.
- Existing [item transport](pipe-disconnect-2026-09-12/item-pipe-checks.txt), [connected meshes](pipe-disconnect-2026-09-12/connected-pipe-checks.txt), [multiblocks](pipe-disconnect-2026-09-12/multiblock-checks.txt), [batteries](pipe-disconnect-2026-09-12/battery-checks.txt), [crank](pipe-disconnect-2026-09-12/hand-crank-checks.txt), [doors](pipe-disconnect-2026-09-12/door-checks.txt), [survival](pipe-disconnect-2026-09-12/survival-checks.txt) and [domain](pipe-disconnect-2026-09-12/domain-checks.txt) checks passed in the review builder.

## Visual review and limits

Inspected actual player captures of [disconnected item](pipe-disconnect-2026-09-12/item-end-disconnected.png) and [fluid](pipe-disconnect-2026-09-12/fluid-end-disconnected.png) terminals, [reconnected blue Input](pipe-disconnect-2026-09-12/item-end-blue-input.png), [restored settings](pipe-disconnect-2026-09-12/restored-connection-directions.png) and [pipe help](pipe-disconnect-2026-09-12/pipe-connection-help.png). Both disabled terminals leave a visible gap while the other end remains attached. Existing Blender meshes are reused; the zero/one-connection straight variants retract through presentation transforms. The pipe block and its wrench target remain present.

No new save record layout or schema is introduced: the previously reserved two-bit value 3 means disconnected. Existing defaults, Input and Output retain their meaning. Older executables reject saves that contain disconnected ends; use this updated player for those saves. These focused checks do not establish large-factory performance or user acceptance of the appearance.

Reproduce with `build` in `Logs/connections-build-request.txt`, then `Tools/Verify-Connections.ps1` with a fresh output directory. Its `-LegacyDirectory` option accepts the retained fixtures linked by the earlier connection report.
