# Shift + wrench click — 2026-09-12

[Industry rules](../INDUSTRY.md#wrench-and-configurable-pipe-ends--2026-09-12) and the [Pipes guide](../wiki/Pipes.md) now support crouch + mouse Use (Shift + right-click by default) on a selected wrench. The same Input → Output → No connection cycle applies. Held Use changes once per press and preserves direction feedback instead of trying to place the wrench as a block.

## Measured evidence

- Unity **6000.4.4f1**, Windows x64 Development player: `Builds/Creative/RivetReach.exe`. Final [build](shift-wrench-2026-09-12/build.txt): **0 errors, 0 warnings**. [Artifact/source hashes](shift-wrench-2026-09-12/artifact-hashes.json) identify the tested executable, assembly and changed runtime sources.
- Final native-player [report](shift-wrench-2026-09-12/runtime-report.json): **408 assertions passed**, [exit code 0](shift-wrench-2026-09-12/exit-code.txt). Actual queued keyboard/mouse events crouch the player and cycle both item and fluid ends through all three states, including reconnecting a missing arm, preserving the opposite end, avoiding inventory opening, keeping the wrench, and changing only once while held. Existing ordinary-use, reach, targeting, persistence and connection scenarios also pass.
- Visually reviewed final in-game captures: [item Output](../wiki/images/shift-wrench-item-output.png) and [fluid Output](shift-wrench-2026-09-12/shift-wrench-fluid-output.png). Both show the crouched camera, held wrench, red source arrow, blue destination arrow and preserved connection feedback.

## Limits and reproduction

The broader connections build command stopped at an existing item-pipe chest preference/throughput assertion ([failure](shift-wrench-2026-09-12/connection-build-blocker.txt)). This input-only task does not resolve or certify that behavior. The existing `power-grid-build` request in `Logs/build-request.txt` produced the final player, followed by `Tools/Verify-Connections.ps1 -Executable <Creative player> -OutputDirectory <fresh directory>`. No new legacy-save fixture run or performance claim is made.

Concurrent crafting UI edits were excluded from this task. The wiki export was refreshed in Unity, and publication validation uses an isolated source snapshot matching this commit, with those unrelated files at HEAD. It runs the unchanged `python3 Tools/publish_wiki.py --check`; the shared working tree correctly reports stale fingerprints while those other edits are unfinished. Item data, recipes and artwork are unchanged; only the two changed runtime-source fingerprints differ in the export.
