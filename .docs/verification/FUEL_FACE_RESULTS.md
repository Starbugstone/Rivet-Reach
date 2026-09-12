# Machine fuel faces — 2026-09-12

[Industry rules](../INDUSTRY.md#machine-item-inputs-by-face--2026-09-12) own the rear-only fuel contract; [Pipes](../wiki/Pipes.md#example-crusher--furnace--chest) teaches setup.

The transport boundary now passes the receiving machine’s local face into compatibility, receiver preference and insertion. Furnace rear inputs select fuel and other inputs select ingredients before capacity checks. Boiler item fuel enters only at the rear; crushers retain ingredients on all faces. Manual inventory interactions and output extraction retain their existing authority.

## Verification

Unity **6000.4.4f1**, Windows x64 Development / Direct3D11, in an isolated checkout including the completed pipe-disconnection changes:

- [Build](fuel-faces-2026-09-12/build.txt): succeeded, **0 errors, 0 warnings**, 30.53 seconds.
- [Item pipes](fuel-faces-2026-09-12/item-pipe-checks.txt): **641 assertions**, covering all four rotations and six faces, logs as fuel versus ingredients, full-slot rejection without fallback, incompatible cargo conservation, ordinary crusher rear inputs, output-only devices and the shared source rate.
- [Native player](fuel-faces-2026-09-12/runtime-report.json): **296 assertions**, [exit 0](fuel-faces-2026-09-12/exit-code.txt). A rotated furnace receives rear fuel, saves/loads its orientation and inventory, exports exactly four ingots and resumes after blocked output. After loading, separate log supplies fill its rear fuel and side ingredient slots; a rear-fed log then burns to produce charcoal.
- [Connection](fuel-faces-2026-09-12/checks.txt), [survival](fuel-faces-2026-09-12/survival-checks.txt), [battery](fuel-faces-2026-09-12/battery-checks.txt), [crank](fuel-faces-2026-09-12/hand-crank-checks.txt), [door](fuel-faces-2026-09-12/door-checks.txt), [multiblock](fuel-faces-2026-09-12/multiblock-checks.txt) and [domain](fuel-faces-2026-09-12/domain-checks.txt) regressions passed.
- Wiki generation and publishing validation passed for **143 pages and 5,698 links/images**.

[Source and executable hashes](fuel-faces-2026-09-12/source-and-artifacts.json) identify this tested snapshot. The review player is `Builds/FuelFaces/RivetReach.exe` in the main workspace. Concurrent uncommitted power-grid changes are outside this player’s snapshot.

An initial native fixture retained the old world reference after loading and failed when replacing its supply machine. The fixture now reacquires the loaded world; the final run above passes. No gameplay authority was bypassed.

## Compatibility and limits

No new save fields, item IDs or recipes. Stored contents, pipe directions and placed orientation retain their existing formats. Existing side-fed fuel lines now reject fuel until moved to the back; rejected items remain at their source. Unchanged output-only devices do not gain input inventories.

The earlier [furnace-pipe report](FURNACE_PIPE_RESULTS.md) retains its original build identity and describes the previous automatic item sorting behavior. This report supersedes its input-routing rules only. These checks are not a large-factory performance measurement or user play acceptance.

Reproduce with the pinned Editor’s `-executeMethod RivetReach.Editor.ItemPipeChecks.BuildFuelFaceReview`, then `Tools/Verify-Connections.ps1` against the resulting `Builds/Connections/RivetReach.exe`.
