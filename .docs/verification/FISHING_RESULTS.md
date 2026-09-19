# Fishing verification — 2026-09-19

The [fishing increment](../FISHING.md) was checked in Unity **6000.4.4f1** and the Windows development player at `Builds/Fishing/RivetReach.exe`. The final build completed with **zero errors and zero warnings**. This is a focused review build, not a published release.

## Measured checks

- **960 Editor assertions:** every authored wait duration, early/last-window/expired reels, exact tick boundaries, cancellation and duplicate-award prevention; each source/air/residency position in the 147-cell eligibility footprint; shallow/flowing/lava rejection; rod recipe quantities/mirroring and fish recipe/tag/food definitions. The build command also completed [4,335 farming assertions](fishing-2026-09-19/farming-checks.txt), [47,720 survival assertions](fishing-2026-09-19/survival-checks.txt) and 469 compost assertions.
- **452 player assertions:** actual world raycasts, valid 7×7×2 pond, cast/line/float, rod tip visible at the physical rod end and line attached after held-item updates, early/missed/successful reels, single award, held mouse versus new Use press, slot/menu/water/range/wall cancellation, full-inventory dropped catch, eating, actual workbench ingredient consumption, cooker production and fish/rod/cooker-output save/load. Crafting and cooking captures assert the relevant station UI stays open.
- **183 durable-save assertions**, then **5 fresh-process restart assertions**, passed with exit code zero using the fishing executable. The suite exercises world/item/station conservation, compatibility rejection, failed-load rollback, recovery and Continue.
- Copies of actual **pre-fishing schema-11** mixed-compost and **schema-10** compost checkpoints each passed **17 historical-load assertions**, including resaving and mixed-generation terrain preservation. Original checkpoint directories were left intact. Save/restart/historical regressions ran before the final line-attachment-only presentation correction; the final focused fishing player repeats its fish/rod/cooker save/load checks. Schema remains 11; no terrain generation change or persistent fishing timer was added.

Evidence: [Editor checks](fishing-2026-09-19/editor-checks.txt), [build summary](fishing-2026-09-19/build-summary.txt), [player report](fishing-2026-09-19/runtime-report.json), [save report](fishing-2026-09-19/save-report.json), [restart report](fishing-2026-09-19/restart-report.json), [schema-11 report](fishing-2026-09-19/legacy-schema11-report.json), [schema-10 report](fishing-2026-09-19/legacy-schema10-report.json).

The player review used an Intel Core i7-10750H, RTX 2060, Direct3D11 and a 1280×800 window. Assertion counts are functional evidence, not a frame-time benchmark. Zero/default performance fields in the shared report were not measured by this workload.

## Artwork and requested snapshots

Original Blender source and review render are in `ArtSource/Fishing`; `Tools/create_fishing_assets.py` reproduces the meshes, atlas and icons. The Unity import measured one renderer per model: rod **3,000 triangles**, raw fish **660**, cooked fish **768**, stew **1,632**, float **276** ([import measurements](fishing-2026-09-19/unity-imports.txt)). These counts do not establish overall rendering performance.

The [illustrated player guide](../wiki/Fishing.md) retains nine actual final-player snapshots: rod recipe, filled crafting grid, crafted rod after ingredient consumption, cast, bite, caught fish, cooked-fish recipe, cooker output and fish-stew recipe. Visual review corrected the held rod orientation and ensured the station screenshots show the active UI. Captures are dated September 19, 2026; the purpose-built minimum-size pond makes the water rule visible.

## Documentation and remaining limits

The exported reference contains **182 items and 188 recipes**. The publisher validated **206 pages and 8,920 local page/section/image links**. Export used the committed terrain atlas while preserving the unrelated local atlas edits byte-for-byte; those existing edits are not included in this feature.

Long-session food balance, accessibility of bite cues, varied natural fishing locations and broad hardware performance remain playtest work. This slice adds no fish mobs, bait, durability, depletion, weather or biome-specific catches. The three-second bite, 8–16-second wait and food values are working defaults, not claims of proven balance.

## Published wiki and issue

[Deployment 35434968498](https://github.com/Starbugstone/Rivet-Reach/actions/runs/35434968498) succeeded, publishing wiki commit `5b381de` from implementation commit `868c9ce`. All 608 exported source fingerprints match the committed inputs. The nine screenshots and four new item icons match their published PNG bytes exactly.

Live Chromium review loaded all nine guide images at 1280×900, and reviewed the rod and three fish item pages at 390×844 with no broken images or horizontal document overflow. The rod's three-stick/two-string 3×3 recipe is visible and its String ingredient link opens the correct item page. Evidence: [live guide](fishing-2026-09-19/wiki-fishing.png), [mobile rod recipe](fishing-2026-09-19/wiki-rod-mobile.png), [publication record](fishing-2026-09-19/publication.json).

[Issue #10](https://github.com/Starbugstone/Rivet-Reach/issues/10) now checks the rod, fishing interaction, fish, cooking, minimum source-water requirement and illustrated steps. Beds, chickens, crates, weather, renewables and unvalidated sustained balance remain unchecked; the issue remains open.
