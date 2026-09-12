# Pumps without electricity — 2026-09-12

The user requested pumps that do not consume electricity because water is needed for power generation. [Industry rules](../INDUSTRY.md) now specify extraction without electrical demand or a power endpoint, retaining the two-second cycle, 10 L source volume, optional Blue Signal, buffer capacity and residency checks.

## Measured checks

Unity **6000.4.4f1** ran the existing `creative-build` request in the open project. [Industry checks](pump-2026-09-12/industry-checks.txt) passed **393 assertions**, including **18 new pump assertions**: exact 40-tick extraction, unchanged adjacent battery charge, full/partly occupied buffer protection, OFF/ON signal pause/resume, invalid and unloaded intake, dormant machines, cold startup of an empty fueled boiler through a fluid pipe, conserved startup water, and restoration of generation after water starvation.

[Battery checks](pump-2026-09-12/battery-checks.txt) passed **29 assertions**, including completion from retained partial pump work without electricity. [Domain checks](pump-2026-09-12/domain-checks.txt) passed **92,355 assertions**; [multiblock checks](pump-2026-09-12/multiblock-checks.txt), connected-pipe checks and [1,144 recipe-transfer assertions](pump-2026-09-12/creative-transfer-checks.txt) also passed during this build.

The Windows x64 development player at **`Builds/Creative/RivetReach.exe`** [built successfully](pump-2026-09-12/build-summary.txt) with **zero errors and zero warnings** in 59.85 seconds. [Completion](pump-2026-09-12/build-result.txt) was recorded at **12:47:58 UTC**. This does not replace the packaged alpha release.

The existing connections scenario passed [281 runtime assertions](pump-2026-09-12/runtime-report.json) in this player, including the revised pump diagnostics, separate electrical behavior, wrench/pipe interactions and schema-4 save round-trip. [The process exited successfully](pump-2026-09-12/exit-code.txt). This scenario checks the pump interface; the cold-start and extraction measurements above come from the Editor simulation.

The [actual player interface](../wiki/images/industry-pump-ui.png), captured on this build, was visually inspected: help says “No electricity required”; signal, water and intake diagnostics fit without electrical allocation or connection labels.

No save schema, serialized pump fields, item definitions, recipe assets or save compatibility checks changed. Existing buffers and partial work use the existing serialization; fractional work from previously underpowered pumps resumes with one work unit per eligible tick.

The Unity wiki export and generated pump reference now show zero electrical demand. The water, pipe and electricity guides explain startup and blackout recovery without powering the pump. `Tools/publish_wiki.py --check` passed the generated-reference and local-link checks. Older workshop reports retain their dated evidence with an explicit supersession note.

## Reproduce and limits

Write `creative-build` to `Logs/build-request.txt` with the pinned Editor in Edit mode. The focused cases are `IndustryChecks.PumpWithoutPower`. These checks establish the bounded simulation behavior; they do not prove long-session stability or performance of arbitrary factory layouts. The updated broad workshop-followup scenario has not been rerun for its full held-item catalogue in this task.
