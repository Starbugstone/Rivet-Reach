# Bed verification — 2026-09-19

[Beds, home spawn and sleep](../BEDS.md) were checked in Unity **6000.4.4f1** and the Windows development player `Builds/Beds/RivetReach.exe`. The build succeeds with zero errors and four pre-existing obsolete-API warnings in `AlphaPlaytestVerification.cs`. This is a focused review build, not a published game release.

## Measured checks

- **58 Editor assertions:** default/configured sleep thresholds and rounding, calendar boundaries, internal head exclusion, cached selection bounds, exact six-input recipes across translated Workbench/Machinist grids, cardinal footprints, support exclusions, previous-content compatibility and rejection of unrelated content changes.
- **233 native player assertions**, including fixture setup: real recipe browser and crafting transaction, obstructed second-half placement without item loss, cross-chunk placement, all four orientations, under-bed ray clearance, bound mouse Use, daytime home assignment, both-half sleep, no held-use repeat, actual death/respawn, blocked arrival, destroyed/replaced home fallback, exactly-one mining/support-loss recovery, saved identity/configuration, late-load rollback and distant home residency gating. Serialized terrain/growth, fluids, animals, machinery and hunger remain byte-for-byte unchanged across sleep.
- **6 fresh-process bed continuation assertions** retain the paired cells, unique home identity, nondefault sleep policy, safe arrival and ordinary Survival mode.
- **183 full-save/recovery assertions** and **5 fresh-process Continue assertions** pass. Actual pre-bed schema-13 and schema-12 checkpoints each pass **17 historical-load assertions**, including preserved terrain edits and resaving. Checks used isolated copies; original saves were preserved.
- The shared targeting path passes **453 fishing assertions** and **321 chicken assertions** ([chicken report](beds-2026-09-19/chicken-runtime-report.json)).

Evidence: [bed Editor checks](beds-2026-09-19/editor-checks.txt), [native report](beds-2026-09-19/runtime-report.json), [bed restart](beds-2026-09-19/bed-restart-report.json), [full saves](beds-2026-09-19/save-report.json), [general restart](beds-2026-09-19/restart-report.json), [schema 13](beds-2026-09-19/legacy-schema13-report.json), [schema 12](beds-2026-09-19/legacy-schema12-report.json), [fishing](beds-2026-09-19/fishing-runtime-report.json), [survival Editor checks](beds-2026-09-19/survival-checks.txt), [build summary](beds-2026-09-19/build-summary.txt) and [build messages](beds-2026-09-19/build-messages.txt).

The general save, historical-load, fishing and chicken suites used the same gameplay code before the final verification-only camera alignment correction. The final native bed run repeats bed save/load and rollback checks.

## Original art and visual review

`Tools/create_bed_assets.py` reproduces the original timber frame, teal quilt, linen pillow and brass details. Blender source and front/back renders are retained in `ArtSource/Beds`. Actual Unity import measures **1,780 triangles, one renderer and one material**, within a 1×1×2 footprint ([import measurements](beds-2026-09-19/unity-imports.txt)); Blender's pre-import source has 1,812 triangles. These geometry counts do not establish overall rendering performance.

Eight actual player captures in the [illustrated guide](../wiki/Beds-and-home-spawn.md) show the recipe, filled crafting grid, crafted item, placed model, home confirmation, night, morning and home arrival. Screenshots use a constructed review platform in an ordinary Survival session with scripted fixture resources. They demonstrate feature behavior, not a full start-to-bed survival playthrough.

## Coordination and limits

Issue #12 finished in the same checkout before bed integration. Its schema-13 checkpoint `4ec2909` and final evidence `4034c97` were preserved. Bed saves append schema 14 after the existing spawner/passive sections. A temporary clean-atlas build/export interval used the committed terrain atlas. The unrelated working atlas was backed up, restored and SHA-256 checked byte-for-byte; no foreign artwork/settings changes are included.

The sleep policy is configurable in the authoritative API and saved world state; it defaults to 50% rounded up, minimum one, clamped to the active participant count. The current session supplies one local participant. Policy checks do **not** prove multiplayer: online membership, voting UI and network integration remain later work. There is no settings-screen control for the threshold in this increment.

Long-session food/sleep balance and broad placement/performance playtests remain. Sleep adds no production catch-up, healing, monster proximity restriction or sleep animation. Existing creatures remain. Zero/default timing fields in the shared report were not measured by this workload.

## Wiki publication

The exported reference contains **191 items and 195 recipes**. The publisher validates **220 pages and 9,433 local links/images**. All [644 source fingerprints](beds-2026-09-19/staged-export-check.txt) match the exact Git index. Implementation commit `1eaa39b` was pushed to `main`. [Wiki deployment 35442897068](https://github.com/Starbugstone/Rivet-Reach/actions/runs/35442897068) succeeded, publishing wiki revision `33a9d8f2e318d891020e25b1bf169695707fe9c4` on September 19, 2026.

Real Chromium review covered the live bed guide, Bed, Cloth, Planks, Workbench and Spawn mechanics pages. The Bed recipe’s Cloth ingredient link was followed successfully. All page images loaded and none of those six pages overflowed at 390×844 ([browser readback](beds-2026-09-19/live-wiki-checks.json)). The guide was reviewed on desktop and the Bed recipe on mobile ([desktop capture](beds-2026-09-19/wiki-guide-desktop.png), [mobile capture](beds-2026-09-19/wiki-bed-mobile.png)). All eight guide PNGs and the new inventory icon match the local SHA-256 hashes ([published assets](beds-2026-09-19/published-assets.json)).

[Issue #10](https://github.com/Starbugstone/Rivet-Reach/issues/10) was updated and read back: nine bed-related recipe/phase/acceptance entries are newly checked, stale current-status wording is reconciled, and the multiplayer-policy limitation is explicit. Crates/controllers, weather, renewables and sustained hunger tuning remain unchecked; the issue stays open ([readback](beds-2026-09-19/issue10-check.json)).
