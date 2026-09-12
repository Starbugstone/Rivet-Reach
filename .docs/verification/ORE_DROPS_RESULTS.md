# Copper and iron drop artwork — 2026-09-12

The user requested the stylised ore-block appearance for mined copper and iron, with coal retaining its ordinary drop. [The content contract](../CONTENT_PIPELINE.md#shared-ore-art-and-rendering--2026-09-11) owns the presentation mapping.

`OreVisuals.VisualId` maps raw copper and iron to their existing ore art. Physical drops, held models and inventory icons share those assets; no mesh or texture was duplicated. Item IDs, mining yields, furnace/crusher recipes, placement eligibility and the save-content fingerprint inputs are unchanged. Existing saved raw metals therefore receive the same presentation change.

## Focused verification

The isolated Windows review player is `Builds/OreDrops/RivetReach.exe`, built with `RivetReach.Editor.ProjectBuild.BuildOreDrops`. Launch with `-force-d3d11 -rr-verify -rr-ore-drops-review -rr-output <absolute-output-directory>` for the focused runtime check. The fixture finds naturally generated deposits, clears an approach, and supplies a temporary light for visual inspection.

The [Windows build](ore-drops-2026-09-12/build-summary.txt) succeeded with **zero errors and zero warnings**. The [runtime report](ore-drops-2026-09-12/runtime-report.json) passed **32 assertions** on Unity 6000.4.4f1 / Direct3D11 at 1280×720. It checks natural mining with tier and stale-command guards, exact drop/pickup counts, shared drop mesh/material references, held palettes, inventory artwork, one-ingot furnace output in 200 ticks, crusher input mapping and the unchanged coal/gold/diamond presentation boundaries.

The actual drops, held models and inventory screenshots were visually inspected against the existing ore graphics.

![Mined copper drop](ore-drops-2026-09-12/ore-drop-16.png)

![Mined iron drop](ore-drops-2026-09-12/ore-drop-15.png)

[Held copper](ore-drops-2026-09-12/ore-drop-held-16.png) · [held iron](ore-drops-2026-09-12/ore-drop-held-15.png) · [copper inventory](ore-drops-2026-09-12/ore-drop-inventory-16.png) · [both inventory icons](ore-drops-2026-09-12/ore-drop-inventory-15.png). This is a focused presentation/processing check, not a new factory-scale performance measurement or a full save/load regression.

## Wiki

The Unity exporter reads the actual inventory icons. Raw iron (`icons/15.png`) shares the iron ore graphic (`icons/9.png`), and raw copper (`icons/16.png`) shares copper ore (`icons/10.png`). The item pages and all linked ingredient/index graphics use those refreshed files. Player notes describe the block appearance and existing processing uses. Source and link validation passed for 127 item pages, 105 recipes, 136 total wiki pages and 5,409 local links/images.
