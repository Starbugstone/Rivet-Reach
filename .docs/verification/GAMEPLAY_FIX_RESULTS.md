# Quit, respawn and mining verification — 2026-09-11

[Gameplay rules](../GAMEPLAY.md#movement-and-targeting) · [Save rules](../SAVES.md) · [Run instructions](../FIRST_POC.md)

The local Windows review build is **`Builds/GameplayFixes/RivetReach.exe`**, produced with Unity **6000.4.4f1**, Direct3D 11, from base `f7e6ef1` plus the changes identified by the [source hashes](gameplay-fixes/source-sha256.txt). The [executable/assembly hashes](gameplay-fixes/build-sha256.txt) match the delivered copy. This is a local review build; the published 0.0.1 release was not replaced.

## Measured results

- [Build](gameplay-fixes/build-summary.txt): succeeded, zero build errors/warnings, 38.02 seconds, 229,846,243 bytes.
- [Editor checks](gameplay-fixes/editor-checks.txt): **22 passed**, including five seeds, flooded surface over a dry cave, tall construction, excavation to bedrock, a destination near the radius boundary, fully blocked terrain and insufficient headroom. The real pause-menu button stopped Play mode, preserved the checkpoint bytes and created no additional checkpoint or backup. Editor process exited 0.
- [Standalone checks](gameplay-fixes/runtime-report.json): **37 passed**, empty runtime error list. Dying at X=800 after unloading the origin and shifting the render origin respawned within 100 blocks of world 0:0, selected another surface column when the original one was flooded, paused simulation until destination residency, retained immunity and remained on the surface after streaming settled. Eligible tiers were checked against stone for every ore, including Azure.
- [Standalone quit result](gameplay-fixes/quit-result.txt): the actual pause-menu button closed the process with exit 0; SHA-256 of the saved checkpoint remained unchanged after unsaved inventory edits, with no extra save or backup.
- Persistence regression against the delivered build: [save/load/failure recovery](gameplay-fixes/save-report.json) **183 passed** and [fresh-process continuation](gameplay-fixes/save-resume-report.json) **5 passed**, both with empty error lists and exit 0. Full serialized-state conservation, backup recovery and failed-load rollback remain covered.
- [Survival domain checks](gameplay-fixes/survival-checks.txt): **47,678 assertions passed**, including all pickaxe tier gates, each ore taking at least 25% longer than stone, and each successive eligible tier mining that ore faster. [Broader domain checks](gameplay-fixes/domain-checks.txt) also passed.

The [actual pause-menu capture after respawn](gameplay-fixes/respawn-and-quit.png) was visually inspected. An earlier iteration could choose a cave below flooded ground; it was corrected and both the domain fixture and final runtime check now explicitly reject that behavior. Only final gameplay captures/reports are maintained here.

## Reproduce and limits

Use a fresh isolated checkout with no Editor already attached. Launch the pinned Editor with `-batchmode -projectPath <checkout> -executeMethod RivetReach.Editor.GameplayFixChecks.RunBatch -logFile <log>` (without `-quit`; the check exits after testing the quit button). It writes `Logs/GameplayFixChecks`, runs domain checks, and builds the local alpha player before testing Editor Play. Use a fresh check directory for another run so its checkpoint fixture cannot overlap previous files.

Run `Tools/Verify-GameplayFixes.ps1 -Executable <player> -OutputDirectory <new-directory>` for standalone checks. It uses isolated saves, requires process exit 0 and checks the checkpoint hash after quitting. Run `Tools/Verify-Saves.ps1` with another fresh output directory for persistence regression. The user's normal saves and already-open Editor session were preserved.

The Editor log also recorded an internal `UnityEditor.Search.SearchDatabase` index exception during startup/domain reload; the focused Play/quit checks completed and the Editor exited 0. This report does not claim an entirely error-free Editor log. The final standalone runtime report contains no logged errors.

Mining durations are verified through the same registry routine used by held mining; they are not stopwatch measurements of every rendered mining animation or acceptance of mining feel. The 25% margin remains tuning for play review. The finite spawn search respects existing terrain and never destroys construction or searches outside the circle. If every surface column is unsafe, respawn stays on the death screen with a message. Beds and saved spawn bindings remain unimplemented. No universal seed, long-session or large-world performance guarantee follows from these checks.
