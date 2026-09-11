# Save/load/continue verification — 2026-09-11

[Save rules and recovery](../SAVES.md) · [Alpha release evidence](ALPHA_0_0_1_RESULTS.md)

The save feature was exercised by `Tools/Verify-Saves.ps1` in a non-development Windows x64 alpha player using Unity 6000.4.4f1. The script uses a fresh isolated save directory; it never edits the user's normal saves. The second scenario starts a separate player process and continues the first process's disk checkpoint. The final release manifest identifies the source commit; the [alpha report](ALPHA_0_0_1_RESULTS.md) owns packaging and broader regression evidence.

## Measured checks

| Scenario | Assertions | Result | Exit |
| --- | ---: | --- | ---: |
| Full persistence, UI, failure/recovery and distant coordinates | 183 | PASS | 0 |
| Continue latest from a fresh process | 5 | PASS | 0 |

[Full save report](saves/save-report.json) · [Fresh-process report](saves/save-resume-report.json)

Coverage includes byte-for-byte equality of every serialized field before simulation resumes; full inventory and retained 2×2 ingredients; chest/workbench/4×4/furnace slots; crop deadlines and burn/work balances; pending fluid/tree work; torch support; exact battery energy and breached-tank contents; tank identity, repair and bank reformation; fitted pipe channels; creatures; dropped-stack age and pickup provenance; model/skin/hotbar and Creative reset; unloaded containers and floating-origin positions; named slots; death save/load; and title Continue.

Failure coverage includes checksum damage, truncation, unsupported schema, invalid world data, late entity-section truncation, locked-file replacement failure, blocked directory writes, preservation of the original session and hotbar/save identity, backup fallback, protection of a good backup when the primary is damaged, and ignored interrupted temporary files. Errors are visible instead of silently discarding progress. Initial fixture and exception-handler failures were corrected before these passing runs.

Fixture save: 5438 bytes; 32,52 ms synchronous capture, checksum, flush and publish. Single bounded workshop on this workstation; not a large-world guarantee.

Hardware: Intel Core i7-10750H, NVIDIA GeForce RTX 2060, 32,553 MB reported RAM, 1280×720. This is one small workshop on one Windows workstation, not a large-world latency, memory or minimum-hardware guarantee.

## Visual evidence

The actual player screenshots were inspected for readable controls and save status:

- [Save Game](saves/save-game.png)
- [Load Game and previous backup](saves/load-game.png)
- [Continue Latest Save](saves/continue-latest-title.png)
- [Expedition restored in a new process](saves/continued-expedition.png)

## Reproduce and limits

Run `Tools/Build-Release.ps1` in a clean checkout using the pinned Editor, then `Tools/Verify-Saves.ps1 -Executable <player> -OutputDirectory <new-directory>`. Both processes must return PASS and exit 0. The launcher uses a retained .NET process handle so exit status remains available after termination.

No periodic autosave, offline catch-up, schema migration, cloud save or multiplayer persistence is implemented. Save/load uses synchronous full snapshots. Testing did not interrupt electrical power or certify every filesystem failure, every seed, huge edited worlds or future content migrations. AI paths and world/network presentation rebuild after loading; the save is not a deterministic future-replay system.
