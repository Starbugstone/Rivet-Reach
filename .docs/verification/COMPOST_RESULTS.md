# Mixed compost and electric automation verification — 2026-09-14

Scope: the user's [revised compost rules](../COMPOST.md). The current Windows development player is `Builds/Compost/RivetReach.exe`, Unity 6000.4.4f1 / Direct3D 11 on an RTX 2060 and i7-10750H, captured at 1280×800. The former stack/timer implementation and its screenshots are superseded; [its dated evidence remains in Git history](https://github.com/Starbugstone/Rivet-Reach/blob/8629bab/.docs/verification/COMPOST_RESULTS.md).

## Current checks

- **469 focused Editor assertions** pass: every configured input, immediate single/stack consumption, mixed values, fractional-threshold carry-over, all manual-bin pipe rejections, all-face automatic input, output-only extraction, full-output/disabled/dormant gates, random 1–4 yields without rerolls, maximum-yield crop-loop bounds, invalid catalog/state rejection and material-tiered construction. The suite checks schema-10 queued input/output migration and a subsequent schema-11 save before interaction. [Checks](compost-mixing-2026-09-14/checks.txt).
- **152 focused player assertions** pass with exit 0: actual station targeting/interfaces, mixed manual deposits, random ejection, save/load of mixed progress and crop deadlines, wild/cultivated crop use, held-click nonrepetition, Creative rules, powered pipe transfers, manual automatic-output collection and mining recovery. The battery fixture accounts for exactly **192 J consumed by 24 organic items plus 512 J retained charge**, with no other demand. [Runtime report](compost-mixing-2026-09-14/runtime-report.json), [exit](compost-mixing-2026-09-14/exit-code.txt).
- **183 save/recovery/rollback assertions**, **five fresh-process continuation assertions** and **17 historical-checkpoint assertions** pass with exit 0. The historical checkpoint is an isolated copy from the September 13 composter build, loaded and re-saved without modifying the original. [Save](compost-mixing-2026-09-14/save.json), [restart](compost-mixing-2026-09-14/restart.json), [legacy composter](compost-mixing-2026-09-14/legacy-compost.json).
- Existing farming, survival/starter-recipe, industry and shared-allocation Editor checks pass during the full build. [Farming](compost-mixing-2026-09-14/farming-checks.txt), [survival](compost-mixing-2026-09-14/survival-checks.txt), [industry](compost-mixing-2026-09-14/industry-checks.txt), [allocation](compost-mixing-2026-09-14/grid-allocation-checks.txt).

The final [build summary](compost-mixing-2026-09-14/build-summary.txt) records zero errors and warnings. Broad save/historical regressions used the [earlier recorded build](compost-mixing-2026-09-14/regression-build.txt); subsequent changes correct fill-model positioning, refine interface text and add focused migration/output-capture checks. Processing, electrical accounting, content definitions and save implementation are unchanged between those builds. The later wiki-export correction only refreshes Editor assets before export and does not change the player implementation. No timing or frame-rate guarantee is inferred from these functional checks.

## Art and gameplay review

Original Blender [autocomposter source render](../../ArtSource/Compost/auto_composter-review.png) was compared with the imported model and actual player captures. The upgrade retains the wooden bin silhouette and adds iron bands, gold fittings and an outlet. Imported measurements are **2,840 triangles / two renderers** for the autocomposter; the basic bin and Compost retain 1,956 / 520 triangles. [Import measurements](compost-mixing-2026-09-14/imports.txt). No third-party assets or dependencies were introduced.

The [player guide](../wiki/Compost.md) shows the revised deposit UI, filled basic bin, ejected Compost, powered automatic interface and connected chest/pipe/battery setup. Actual [basic recipe](compost-mixing-2026-09-14/compost-bin-recipe.png), [automatic recipe](compost-mixing-2026-09-14/compost-auto-recipe.png) and [contribution browser](compost-mixing-2026-09-14/compost-conversion-recipe.png) captures accompany those views.

## Reproduction and limits

Use `Tools/Verify-Compost.ps1 -Build -OutputDirectory <fresh-directory>` with the pinned Editor open in Edit mode. This prepares compost assets/definitions, runs focused and related Editor checks, exports the wiki and builds the player. Omit `-Build` to run the existing player. `Tools/Verify-Saves.ps1 -Executable <compost-player> -OutputDirectory <fresh-directory>` runs save recovery and a separate-process continuation.

Long-session encounter/food/electricity balance, very large composter farms and artistic acceptance remain unmeasured. The random-yield sample verifies the range and persistence, not statistical uniformity over every location. Dismantling intentionally discards already-consumed partial organics and internal charge; ordinary queued legacy input and finished output are recovered. The scope adds no bed, fishing, livestock, weather or renewable generator.

## Published reference and issue tracking

[Wiki deployment 34878054396](https://github.com/Starbugstone/Rivet-Reach/actions/runs/34878054396) passed from repository commit `6962ccc`, publishing wiki commit `f912b2c`. Validation covers **201 pages / 8,614 links and images**, 178 items and 183 crafting/processing/contribution entries. The first deployment caught an omitted generated catalog; the corrected Unity export uses committed source assets. The workstation's pre-existing terrain-texture edit was backed up and restored byte-for-byte, and is not part of this change.

Live Chromium review loaded all six guide captures and all images on the basic bin, autocomposter and Compost pages (31 / 32 / 115). The autocomposter recipe also passed a 390-pixel mobile review without horizontal page overflow; its gold ingredient link opened the correct item page. [Deployment and navigation results](compost-mixing-2026-09-14/wiki-deployment.json), [live guide](compost-mixing-2026-09-14/wiki-live-Compost.png), [mobile recipe](compost-mixing-2026-09-14/wiki-live-auto-mobile.png).

[Issue #10](https://github.com/Starbugstone/Rivet-Reach/issues/10) now records mixed deposits, random output, manual-only bins, electric automation and schema-11 migration as complete. Its other systems and sustained balance checks remain pending; the issue stays open.
