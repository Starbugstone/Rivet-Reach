# Item sidebar, recipe discovery and recipe placement

Verified on **2026-09-11** in Unity **6000.4.4f1 / URP 17.4.0**. Review player: **`Builds/RecipeBrowser/RivetReach.exe`**. [Crafting rules and authoring](../CRAFTING.md#item-sidebar-and-recipe-discovery--2026-09-10) own the implementation contract.

The sidebar contains **127 registered items** and indexes **91 grid recipes, 11 furnace recipes and 3 crusher processes**. Click an icon for production recipes; right-click for uses; Shift-click (or Ctrl-click) to place one recipe, or Ctrl+Shift-click to fill the maximum complete batch in the open compatible crafting grid. Recipe outputs and the Fill grid button select the exact displayed variant. R/U inspect hovered inventory slots while preserving stack gestures.

![Item sidebar alongside ordinary personal crafting](crafting-ux-2026-09-11/sidebar-inventory.png)

## Measured checks

The final Windows development build succeeded with **zero errors and zero warnings**, in **18.497 seconds** after caches were populated. The [build/source manifest](crafting-ux-2026-09-11/build-identity.json) identifies the executable, managed assembly and relevant source snapshot. The isolated project was `Builds/RecipeBrowserVerificationProject`; it preserved the main Editor and concurrent work. The integrated snapshot includes the current ore artwork and earlier systems. [Crafting performance evidence](CRAFTING_UX_RESULTS.md) distinguishes screen opening from transactions and frame-level rendering.

| Check | Result |
| --- | --- |
| Production/uses index, canonical layouts, furnace inputs/timing, fuels, stations and crusher mapping | **368 assertions passed** |
| Recipe placement in every supported grid size, conservation, ready-grid idempotence, wrong size, missing items, blocked returns, freed inventory capacity, grid reuse and repeated shapeless inputs | **1,105 assertions passed** |
| Existing crafting matching, validation, transactions and conservation suite | **1,158,852 assertions passed** |
| Independent starter layout/quantity acceptance | **1,862 checks passed**, covering the 55 survival layouts |
| Native pointer/keyboard, station layouts, browser and crafting regression | **515 assertions passed; no runtime errors**, completed **2026-09-11 18:30:29 UTC** |

The [full native runtime report](crafting-ux-2026-09-11/runtime-report.json) records actual raycast targets and assertions. It covers all item pages, case-insensitive and stable-ID search, empty results, left/right-click lookups and Shift/Ctrl+Shift fill, R/U on inventory stacks, search focus, alternative recipes, machine/ingredient navigation, Back, non-granting browsing and held-stack preservation. It opens actual personal, workbench, Machinist, furnace, chest and crusher interfaces, checks their slot bounds, and checks the sidebar inside a **1024×768** window as well as the main **1280×720** render.

Shift-click, Ctrl-click and Fill grid prepare real 2×2, 3×3 and 4×4 grids without claiming the output. Ctrl+Shift-click tops up complete batches. Native right-drag checks cover once-per-cell placement, revisits, exhausted cursors, incompatible/full cells, split-to-drag and a fresh subsequent press. Non-crafting station rejection is checked with both new fill shortcuts. [Crafting interaction and performance results](CRAFTING_UX_RESULTS.md) own the expanded behavior and timing analysis. Pointer checks cover repeated placement, undersized-grid rejection, exact displayed variants, sidebar alternative selection, and rejection of a furnace recipe even when that output also has a valid block-unpacking grid recipe. The ordinary crafting regression still passes drag-to-craft, batch output with an occupied cursor, full-destination rejection, ingredient return and close/reopen conservation.

## Visual evidence

The final standalone captures were inspected for readable labels, icon presence, full grid layouts, opaque recipe backing and separation from the sidebar:

- [Raw iron uses: furnace ingredients and alternative fuels](crafting-ux-2026-09-11/raw-iron-uses.png).
- [Following a machine to its construction recipe and required 4×4 bench](crafting-ux-2026-09-11/machine-construction.png).
- [Filled 3×3 workbench: wooden pickaxe ready to craft](crafting-ux-2026-09-11/ctrl-fill-workbench.png).
- [Ctrl-filled 4×4 Machinist's Bench: crusher ready to craft](crafting-ux-2026-09-11/ctrl-fill-machinist.png).
- [Sidebar at 1024×768](crafting-ux-2026-09-11/sidebar-1024x768.png).

## Reproduce and remaining review

Run `powershell -File Tools/Verify-RecipeBrowser.ps1 -Build` from the project on the documented Windows workstation. It mirrors Assets/Packages/ProjectSettings into the dedicated ignored verification project, uses the pinned Editor, copies the built player into `Builds/RecipeBrowser`, and runs the focused input scenario. Without `-Build`, it reviews that existing executable. Full transient logs remain under `Logs/RecipeBrowser` and the verification project's `Logs/RecipeBrowser`.

The explicit `-rr-verify -rr-browser-review` scenario supplies its own fixtures. Ordinary sessions still begin empty-handed. No Minecraft/JEI code or artwork, third-party dependency, Editor upgrade or persistent-save system was added.

These are focused functional/layout checks, not a whole-game frame-time benchmark or user usability acceptance. The native report's unused timing fields are not performance claims. Fuel icons are alternatives for one craft from an unlit furnace; residual burn and batching affect actual consumption. Broader gathering/extraction/energy processes, discovery filtering, localization and controller navigation remain outside this browser increment. Item/recipe authoring changes require a new Play session; arbitrary future content volumes and fuel-list sizes have not been visually measured.
