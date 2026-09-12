# Default missing-ingredient borders — 2026-09-12

[Crafting rules](../CRAFTING.md) own the behavior. Opening a recipe immediately outlines under-supplied ingredient types in red in the layout and materials list. The list reports the quantity missing for one operation. Availability includes the backpack and existing grid, aggregates repeated ingredients, and excludes the cursor. Recipe navigation and inventory/grid revisions refresh the feedback. Fuel alternatives remain separate choices.

## Measured checks

- Unity **6000.4.4f1**, Windows Development player, i7-10750H / RTX 2060 / Direct3D12. [Build summary](missing-ingredients-2026-09-12/build-summary.txt): zero errors and warnings. [Build identity](missing-ingredients-2026-09-12/build-identity.json) records the actual snapshot and player hashes.
- [Runtime review](missing-ingredients-2026-09-12/runtime-report.json): **908 assertions, no errors**, including default borders before any fill click, exact coal/charcoal variants, available ingredients left unmarked, navigation to a different ingredient, repeated planks, backpack-plus-grid availability, unchanged containers on failed fill, live border removal and a successful retry. Existing personal/workbench/Machinist browser, screen reuse and crafting checks also passed.
- [Transfer checks](missing-ingredients-2026-09-12/transfer-checks.txt): **1,144 assertions**. [Index checks](missing-ingredients-2026-09-12/index-checks.txt): **380 assertions**, 132 items and 110 indexed recipes.
- The existing keyboard-uses fixture now explicitly moves the pointer away and back after closing recipe details, because recipe binding clears cached hover state. An earlier run stopped at that fixture before the new checks; the retained report is the subsequent complete run.

The two actual player captures below were visually inspected at 1280×720. The torch capture occurs immediately on opening the recipe, before a fill action. The workbench capture shows one missing plank despite planks already being available in both the backpack and grid.

![Missing coal outlined, with the available stick unmarked](../wiki/images/missing-torch-ingredient-2026-09-12.png)

![Repeated plank ingredients outlined with an aggregate shortage of one](../wiki/images/missing-workbench-ingredient-2026-09-12.png)

The retained player is `Builds/RecipeBrowser/RivetReach.exe`. Reproduce with `Tools/Verify-RecipeBrowser.ps1 -Build`. These are focused automated checks and screenshot review, not a new performance claim or user playfeel acceptance. Recipes, items and saves are unchanged; the wiki catalog was re-exported to refresh source fingerprints.

## Wiki publication

The player [crafting guide](../wiki/Crafting-Recipes.md) and screenshot passed **144 pages / 5,760 local links and images**. [Deployment 34701605547](https://github.com/Starbugstone/Rivet-Reach/actions/runs/34701605547) successfully published source commit `4046888`. Live Chromium review of the [published guide](https://github.com/Starbugstone/Rivet-Reach/wiki/Crafting-Recipes) confirmed the default-visibility instructions and loaded screenshot, with no missing images; its rendered screenshot was visually inspected.
