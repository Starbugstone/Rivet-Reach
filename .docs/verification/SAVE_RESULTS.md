# Save/load/continue verification — 2026-09-11

[Save rules and recovery](../SAVES.md) · [Alpha release evidence](ALPHA_0_0_1_RESULTS.md)

The save feature was exercised by `Tools/Verify-Saves.ps1` in a non-development Windows x64 alpha player using Unity 6000.4.4f1. The script uses a fresh isolated save directory; it never edits the user's normal saves. The second scenario starts a separate player process and continues the first process's disk checkpoint. The locally verified candidate player was built from `141c5be7b4cf3d4b7e9c931cd482bc6f11cdbe9c` with Direct3D 11. The user cancelled the release update after local verification; the published alpha remains unchanged. The [alpha report](ALPHA_0_0_1_RESULTS.md) describes that older published artifact.

## Measured checks

| Scenario | Assertions | Result | Exit |
| --- | ---: | --- | ---: |
| Full persistence, UI, failure/recovery and distant coordinates | 183 | PASS | 0 |
| Continue latest from a fresh process | 5 | PASS | 0 |

[Full save report](saves/save-report.json) · [Fresh-process report](saves/save-resume-report.json)

Coverage includes byte-for-byte equality of every serialized field before simulation resumes; full inventory and retained 2×2 ingredients; chest/workbench/4×4/furnace slots; crop deadlines and burn/work balances; pending fluid/tree work; torch support; exact battery energy and breached-tank contents; tank identity, repair and bank reformation; fitted pipe channels; creatures; dropped-stack age and pickup provenance; model/skin/hotbar and Creative reset; unloaded containers and floating-origin positions; named slots; death save/load; and title Continue.

Failure coverage includes checksum damage, truncation, unsupported schema, invalid world data, late entity-section truncation, locked-file replacement failure, blocked directory writes, preservation of the original session and hotbar/save identity, backup fallback, protection of a good backup when the primary is damaged, and ignored interrupted temporary files. Errors are visible instead of silently discarding progress. Initial fixture and exception-handler failures were corrected before these passing runs.

Fixture save: 5438 bytes; 36.241 ms synchronous capture, checksum, flush and publish. Single bounded workshop on this workstation; not a large-world guarantee.

Hardware: Intel Core i7-10750H, NVIDIA GeForce RTX 2060, 32,553 MB reported RAM, 1280×720. This is one small workshop on one Windows workstation, not a large-world latency, memory or minimum-hardware guarantee.

## Visual evidence

The actual player screenshots were inspected for readable controls and save status:

- [Save Game](saves/save-game.png)
- [Load Game and previous backup](saves/load-game.png)
- [Continue Latest Save](saves/continue-latest-title.png)
- [Expedition restored in a new process](saves/continued-expedition.png)

## Reproduce and limits

Run `Tools/Build-Release.ps1` in a clean checkout using the pinned Editor, then `Tools/Verify-Saves.ps1 -Executable <player> -OutputDirectory <new-directory>`. Both processes must return PASS and exit 0. The launcher uses a retained .NET process handle so exit status remains available after termination. Subsequent D3D12 runs exposed a native shutdown fault despite passing runtime reports; Windows identified `D3D12Core.dll` with exception `0xc0000005`. Three Direct3D 11 continuation runs passed and exited 0. The alpha build now selects Direct3D 11 explicitly; the local candidate regression evidence follows below.

No periodic autosave, offline catch-up, schema migration, cloud save or multiplayer persistence is implemented. Save/load uses synchronous full snapshots. Testing did not interrupt electrical power or certify every filesystem failure, every seed, huge edited worlds or future content migrations. AI paths and world/network presentation rebuild after loading; the save is not a deterministic future-replay system.

## Local candidate regression and publication status

The same source `141c5be` built successfully with zero errors and warnings in 17.58 seconds (228,423,187 bytes). `Tools/Verify-Release.ps1` exercised that Direct3D 11 player:

| Scenario | Assertions | Result / exit |
| --- | ---: | --- |
| [creative](saves/regression/creative.json) | 312 | PASS / 0 |
| [industry](saves/regression/industry.json) | 163 | PASS / 0 |
| [multiblock](saves/regression/multiblock.json) | 287 | PASS / 0 |
| [placement-items](saves/regression/placement-items.json) | 96 | PASS / 0 |
| [browser](saves/regression/browser.json) | 491 | PASS / 0 |
| [survival](saves/regression/survival.json) | 86 | PASS / 0 |
| [workshop-followup](saves/regression/workshop-followup.json) | 471 | PASS / 0 |

Together with the two save scenarios, **2,094 runtime assertions passed**, with empty error lists and exit code 0 in all nine completed processes. These results apply to this isolated source revision, not subsequent changes on main.

The local ZIP passed archive integrity and all 202 payload hashes after extraction. The final extracted-player startup/gameplay run produced no completion report or recorded exit status and remains **incomplete**; it is not counted as passing. The user subsequently requested no release update because another build is planned. No replacement release assets or release notes were published. The existing September 10 release and its evidence remain unchanged.
