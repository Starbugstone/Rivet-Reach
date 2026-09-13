# Compost verification — 2026-09-13

Scope: [Compost](../COMPOST.md), the next issue #10 increment. The runtime review uses the pinned Unity 6000.4.4f1 Windows development player with Direct3D 11 on an RTX 2060 / i7-10750H at 1280×800. Focused fixtures exercise real gameplay code; values are working defaults rather than long-session balance acceptance.

## Final build and gameplay

The final player built with **zero errors and warnings**. [Build summary](compost-2026-09-13/build-summary.txt). **144 player assertions passed with exit 0**, including ordinary station targeting/UI, actual right-click consumption and held-button nonrepetition, genuinely generated wild plants, cultivated crops, light rejection, Creative behavior, precise deadline replacement across save/load, pipe transfers, input/output mining recovery and rejection of unrelated modified item definitions. [Player report](compost-2026-09-13/runtime-report.json), [exit](compost-2026-09-13/exit-code.txt). The existing growth scheduler's per-call mutation budget is serviced at an unchanged tick before deadline assertions; the fixture does not bypass the crop mutation path.

Actual [bin recipe](compost-2026-09-13/compost-bin-recipe.png) and [conversion browser](compost-2026-09-13/compost-conversion-recipe.png) captures accompany the guide's operating/UI/crop/connection views. Earlier failed fixture attempts are not counted as passes.

## Measured checks

- **400 focused Editor assertions** cover every configured organic contribution, complete/incomplete batches, all-face item insertion, output-only extraction, zero fuel/electrical demand, output/residency/signal pauses, input changes, save state validation, anti-multiplication economics and the exact seven-plank recipe. [Compost checks](compost-2026-09-13/compost-checks.txt).
- **4,312 farming assertions** and the existing survival/starter-recipe, industry and shared-allocation checks passed during the compost build. The original 56 starter recipes retain their exact layouts/quantities; the new bin is checked separately. [Farming](compost-2026-09-13/farming-checks.txt), [survival](compost-2026-09-13/survival-checks.txt), [starter recipes](compost-2026-09-13/starter-recipe-checks.txt).
- **183 durable-save assertions**, **5 fresh-process restart assertions** and **190 farming player assertions** passed with clean exits. [Save/recovery/rollback](compost-2026-09-13/save.json), [restart](compost-2026-09-13/restart.json), [farming player](compost-2026-09-13/farming-runtime.json).
- **17 assertions each** passed on isolated copies of a schema-9 bridge checkpoint, a pre-compost schema-10 farming checkpoint and a schema-10 recipe-tier checkpoint. They load, retain established terrain, explore new terrain and re-save/reload. [Schema 9](compost-2026-09-13/legacy9.json), [schema 10](compost-2026-09-13/legacy10.json), [material-tier checkpoint](compost-2026-09-13/tier10.json).

The broad runtime regressions above used the [recorded regression build](compost-2026-09-13/regression-build.txt). The subsequent final build changes the verification fixture/capture framing and corrects the recipe browser to show fuel requirements only for recipes with fuels. The fixture adds finished-output mining recovery and rejection of unrelated changed definitions. Processing, crop gameplay, save code and asset definitions are unchanged between those builds.

## Original art and imports

The [source bin render](../../ArtSource/Compost/compost_bin-review.png) and [compost render](../../ArtSource/Compost/compost-review.png) were inspected against the actual in-game model/interface captures. Imported measurements: **1,956 triangles / two renderers** for the bin, **520 triangles / one renderer** for compost; both fit their one-cell bounds. [Import measurements](compost-2026-09-13/imports.txt). These counts are geometry evidence, not frame-rate guarantees. Sources use Rivet Reach's existing original atlas and introduce no third-party assets or dependencies.

The [player guide](../wiki/Compost.md) includes actual bin, processing interface, crop-use and pipe-setup images. Recipe/uses pages export all 19 conversions and matching item icons.

## Reproduction

Use the pinned Unity 6000.4.4f1 Editor in Edit mode and run `Tools/Verify-Compost.ps1 -Build -OutputDirectory <fresh-directory>`. This prepares only compost definitions/art, runs focused compost plus existing farming/survival/industry/allocation checks, exports the wiki and builds `Builds/Compost/RivetReach.exe` with Direct3D 11. Without `-Build`, it exercises the existing player. Use `Tools/Verify-Saves.ps1 -Executable <compost-player> -OutputDirectory <fresh-directory>` for durable save/recovery and a separate-process restart.

## Remaining limits

Tests use bounded deterministic fixtures and the actual Unity player. They do not establish long-session hunger/compost balance, arbitrary modded catalogs, very large farm/factory performance or artistic acceptance. Compost supplies no offline growth, weather, irrigation, animal system, bed or fishing functionality. Existing broad reports retain their dated build identities.
