# Weather verification — 2026-09-19

Scope: [weather](../WEATHER.md), issue #10. The current working build is `Builds/Weather/RivetReach.exe`, Unity 6000.4.4f1 / Direct3D11. Clear, rain and storm presentation share a pure saved weather authority. This is a focused single-player check, not multiplayer or whole-game certification.

## Sleep weather recheck — current revision

Successful bed sleep now performs exactly one saved-RNG weather check: 50% retains the current target/timer; otherwise it selects a different target and blends over 15 seconds. This supersedes the original weather-preserving sleep decision. Raw celestial clock edits alone still do not alter weather. Other survival/production timers remain unchanged.

[Pure weather checks](weather-sleep-2026-09-19/core-checks.txt): **217 assertions**, including keep/change outcomes across 64 deterministic seeds, no visual discontinuity, retained timers and saved-RNG repeatability. [Native bed review](weather-sleep-2026-09-19/runtime-report.json): **237 assertions**, including actual successful sleep with exactly one weather roll, daytime/repeated morning use without rerolls, byte-identical non-weather production state, bed save/load and failed-load rollback. The [current build](weather-sleep-2026-09-19/build-summary.txt) has zero errors and the same four existing Alpha harness API warnings. [Artifact hashes](weather-sleep-2026-09-19/build-artifacts.json) identify this revised player. Updated [night](../wiki/images/beds/bed-night.png) and [morning](../wiki/images/beds/bed-morning.png) captures show Storm → Rain.

[Wiki deployment 35452653323](https://github.com/Starbugstone/Rivet-Reach/actions/runs/35452653323) published implementation `59cc096` to wiki `61315bb`. [Live review](weather-sleep-2026-09-19/live-wiki-checks.json) verifies both revised guides, all six Weather images, mobile layouts and navigation. All 225 published pages and both refreshed bed captures match the sources. The export/link check passed with 193 items, 197 recipes and 9,653 links/images; [664 source fingerprints](weather-sleep-2026-09-19/staged-export-check.txt) match the committed inputs. Issue #10 records the revised sleep rule.

The original weather implementation and coverage evidence below belong to commit `9791ca5`; the sleep revision has its own evidence and does not re-date those measurements.

## Original weather checks

- [Pure core](weather-2026-09-19/core-checks.txt): **24 assertions** covering deterministic schedules, bulk/chunked advancement, smooth transitions, interrupted transitions, exact RNG continuation, legacy defaults and malformed payloads.
- [Native player](weather-2026-09-19/runtime-report.json): **71 assertions**, including ordinary empty-handed Survival startup, live timing, pause, intermediate rain, visible bounded geometry, opaque roof shelter, cloud attenuation, master mute, delayed thunder, exact schema-16 transition saves and late-payload rollback.
- [Fresh process](weather-2026-09-19/resume-report.json): **4 assertions**, preserving the partly transitioned storm through offline time and residency loading.
- [Schema-15 checkpoint](weather-2026-09-19/legacy15-report.json): **2 assertions**, loading an actual completed crate checkpoint and selecting legacy clear weather.
- [Full save regression](weather-2026-09-19/save-regression.json): **183 assertions**, plus **5** [fresh-process general-save assertions](weather-2026-09-19/save-restart.json), on the final corrected native build.
- [Build](weather-2026-09-19/build-summary.txt): zero errors. Four [warnings](weather-2026-09-19/build-messages.txt) are existing obsolete FindObjects APIs in the separate Alpha verification harness.

## Rain coverage correction

The user's right-edge gap was reproduced with complete roof data: all 256 former square-footprint columns were known. A diagonal view strongly concentrated drops in the centre. The corrected stratified circular footprint retains **1,024 maximum streaks**, maps each streak to its actual cached world column and checks **484 columns per refresh**. [Measured eight-band coverage](weather-2026-09-19/weather-coverage.txt) passes at yaw 35°, 90°, 180° and 270°; both outer bands exceed one quarter of the per-band average. The reproducible native wrapper enforces that acceptance threshold.

[Rain mesh CPU sample](weather-2026-09-19/weather-cost.txt): **0.2033 ms mean, 0.3866 ms maximum** across 120 frames on an Intel i7-10750H / RTX 2060 Windows Direct3D11 fixture. This measures the mesh update, not roof refresh, total weather cost, full-game frame time or worst-case factories.

Current captures: [clear](../wiki/images/weather/weather-clear.png), [rain](../wiki/images/weather/weather-rain.png), [storm](../wiki/images/weather/weather-storm.png), [shelter](../wiki/images/weather/weather-shelter.png), [night storm](../wiki/images/weather/weather-night-storm.png). [Diagonal](../wiki/images/weather/weather-coverage-35.png) and [cardinal](../wiki/images/weather/weather-coverage-90.png) views retain the corrected edge coverage.

## Reproduce

- `powershell -File Tools/Verify-Weather-Core.ps1` uses the pinned Editor's Mono compiler/runtime for pure state, deterministic schedule, interrupted transitions and schema validation.
- `powershell -File Tools/Verify-Weather.ps1 -Build -OutputDirectory <fresh-directory>` uses the already-open Editor request, then the native review player.
- `powershell -File Tools/Verify-Weather.ps1 -OutputDirectory <another-directory> -SaveDirectory <first-directory>/Saves` continues the checkpoint in a new process.
- Add `-Legacy` with a copy of a real pre-weather schema-15 checkpoint to verify legacy clear defaults.
- `Tools/Verify-Saves.ps1` can run the existing full save suite against this executable.

Weather's fixed simulation ticks freeze through pause, death/loading gates and offline time. The native review compares a celestial sleep-style time jump against exact weather bytes and production ticks; the existing bed acceptance suite owns physical bed interaction itself. Original procedural audio is verified for allocation/loading, master mute, pause and delayed thunder scheduling; subjective loudness and long-session audio balance still need playtesting. The bounded rain fixture measures mesh CPU cost only; it does not measure an arbitrary populated world's frame budget.

## Wiki validation

The Unity export retains 193 items and 197 recipes. `Tools/publish_wiki.py --check` passed for **225 pages and 9,650 local links/images**. All **664 export source fingerprints** match the exact staged repository inputs; see [index check](weather-2026-09-19/staged-export-check.txt). The pre-existing local atlas edits were preserved byte-for-byte after building/exporting committed artwork. [Wiki deployment 35452018459](https://github.com/Starbugstone/Rivet-Reach/actions/runs/35452018459) succeeded from implementation commit `9791ca5`, publishing wiki commit `0d7b625`. All 225 published Markdown pages and all ten current weather captures match the maintained sources. [Live browser checks](weather-2026-09-19/live-wiki-checks.json) verified Weather/Home/Items, all four guide images and 193 item icons, desktop/mobile layouts and Home → Weather → Beds navigation. [Desktop](weather-2026-09-19/wiki-weather-desktop.png) and [mobile](weather-2026-09-19/wiki-weather-mobile.png) captures record actual GitHub rendering. Issue #10's five implemented weather tasks and acceptance entry were verified checked; renewables and conditional multiplayer replication remain open.
