# Furnace pipes, compatible cargo and receiver priority — 2026-09-12

[Industry rules](../INDUSTRY.md#furnace-pipes-and-compatible-cargo--2026-09-12) own behavior; [Pipes](../wiki/Pipes.md#example-crusher--furnace--chest) teaches crusher → furnace → chest setup.

The reported missing furnace connection was caused by external item endpoint discovery recognizing only chest storage. Furnace inventories now participate through their own insertion/extraction authority, with all-face geometry, selected-wrench direction settings and survival scheduler wake notifications. No save fields, item IDs or processing recipes were added.

Receivers first request cargo matching existing input or output. Furnace and crusher preferences compare the incoming recipe's product to the stored result; chests prefer existing stored item types. The fallback pass tries other compatible cargo with the same source snapshot and four-items/s budget. Item compatibility and capacity remain required in both passes; fluid compatibility and tank safety remain independent.

## Measured checks

Unity **6000.4.4f1**, Windows x64 Development, Direct3D11: [build](furnace-pipes-2026-09-12/build.txt) succeeded with **0 errors and 0 warnings**, 50.762 seconds. [Source and artifact hashes](furnace-pipes-2026-09-12/artifact-hashes.json) identify the tested working-tree snapshot, including concurrent held-torch edits.

- [Native player](furnace-pipes-2026-09-12/runtime-report.json): **281 assertions**, **exit 0**, no recorded errors. The actual world registers a furnace placed after its pipe, routes crusher products and mixed-chest fuel, wakes sleeping production, renders both endpoint arrows, saves/reloads input/fuel/directions, exports exactly four ingots, wakes a cold output-blocked furnace, and prioritizes iron ingredients when iron output is retained. The captured camera angle was obstructed by the earlier tank fixture, so it is not retained as visual evidence; the runtime assertions check actual arrow instances and inventory state.

- [319 focused Editor assertions](furnace-pipes-2026-09-12/item-pipe-checks.txt): all six faces for all three crushed metals; ingredient/fuel/result separation; exact transfers and processing; dormant endpoints; full buffers; mixed-chest rejection; boiler/crusher/drill compatibility; non-water rejection; receiver preferences across source ordering; and fallback without multiplying throughput.
- Existing [connection](furnace-pipes-2026-09-12/checks.txt), [survival](furnace-pipes-2026-09-12/survival-checks.txt), [battery](furnace-pipes-2026-09-12/battery-checks.txt), [crank](furnace-pipes-2026-09-12/hand-crank-checks.txt), [door](furnace-pipes-2026-09-12/door-checks.txt), [multiblock](furnace-pipes-2026-09-12/multiblock-checks.txt), [connected-pipe](furnace-pipes-2026-09-12/connected-pipe-checks.txt) and [domain](furnace-pipes-2026-09-12/domain-checks.txt) suites passed in the review builder.

## Reproduction and limits

In the pinned Unity **6000.4.4f1** Editor, run **Rivet Reach → Build configurable connections review**, then `Tools/Verify-Connections.ps1 -OutputDirectory <fresh directory>`. Review player: `Builds/Connections/RivetReach.exe`.

An initial runtime fixture tried to advance immediately after load while its chunk was still rebuilding; it failed the post-load product assertion. The final fixture explicitly waits for resident terrain before advancing, then passes. No gameplay code was changed to bypass dormant-world behavior.

The wiki exporter and generated furnace/item-pipe pages were refreshed; the publisher checked **143 pages and 5,700 local links/images**.

Alternate-fluid checks use an unregistered test definition; only water is playable. This is a focused logistics regression, not a large-factory performance measurement or certification of every concurrent change. Earlier screenshots and save-suite reports retain their original artifact identities.
