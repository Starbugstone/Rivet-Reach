# Inventory and held item agreement — 2026-09-12

The inventory icon and selected item now share their authored object, palette and tool-tier tint. Copper wire displays the copper spool in hand. The same correction covers the ten other industrial crafting components previously routed through a generic survival card. Tank frames now show open rails in hand, and signal wire shows the two arms used by its standalone icon.

The 25 tiered tools, three legacy tools, torch and two buckets use 31 icons baked from their actual held meshes. Raw coal, gold and diamond use terrain-cube icons to match their held cubes. Fallback cards borrow the actual inventory texture. Copper and iron ore/drop models, food and station art retain the independently authored assets included in this review snapshot. [Authoring contract](../CONTENT_PIPELINE.md#inventory-and-held-item-agreement--2026-09-12).

## Native player evidence

Artifact: `Builds/ItemAppearance/RivetReach.exe`, Windows x64 Development player, Unity **6000.4.4f1**. [Build](item-appearance-2026-09-12/build-summary.txt) completed with **0 errors and 1 warning** (an existing deprecated-object-search warning in the hand-crank verification helper). Gameplay assembly SHA-256: `8718851bcb5bb0033acfd0eaf014fbd9cfb3f7f543de538ace0639f80e4ed220`. The executable is a focused review build, separate from the alpha release.

Native run **2026-09-12T09:42:56.4438287Z**: **PASS — 629 assertions, 128 registered items, zero logged errors/exceptions**, at 1280×900. Copper wire uses one renderer and 2,004 triangles; the corrected signal wire has three renderers and 264 triangles; the open tank frame has 24 rail renderers and 7,488 triangles. These counts describe the displayed imported geometry, not rendering performance.

The opt-in `-rr-item-appearance` scenario selects every registered item in the real inventory and captures the actual first-person view. It checks visible geometry, icon availability, industrial asset selection, baked source meshes and tool tints, shared fallback textures, tank-frame panels and signal-wire arms. A framebuffer comparison verifies that the copper spool contributes visible pixels. Copper is also captured with the other player model after opening and closing inventory. Mesh index metadata is inspected without requiring readable imported meshes.

[Raw report](item-appearance-2026-09-12/report.json). The five sheets combine native held-item captures with the actual UI icons exported by `WikiExport` from the same isolated project snapshot. All entries were visually reviewed for silhouette, material and variant agreement; camera angle and scene illumination naturally differ from icon studio lighting.

| Items | Comparison sheet |
| --- | --- |
| First 30 registered entries | [Sheet 1](item-appearance-2026-09-12/catalog-1.png) |
| Entries 31–60 | [Sheet 2](item-appearance-2026-09-12/catalog-2.png) |
| Entries 61–90 | [Sheet 3](item-appearance-2026-09-12/catalog-3.png) |
| Entries 91–120 | [Sheet 4](item-appearance-2026-09-12/catalog-4.png) |
| Remaining entries | [Sheet 5](item-appearance-2026-09-12/catalog-5.png) |

![Copper wire spool and its inventory icon](item-appearance-2026-09-12/copper-wire.png)

[Other player appearance](item-appearance-2026-09-12/copper-other-appearance.png).

The wiki refresh passed generation and publishing checks for **128 item pages, 106 recipes, 137 total pages and 5,481 local links/images**. All **436 exported source fingerprints** match the staged commit contents; unrelated in-progress gameplay changes were excluded from that export.

## Reproduce and limits

Use Unity **6000.4.4f1**. Run `RivetReach.Editor.ItemAppearanceBuild.Run` in a separate project copy to bake icons, export the UI catalog and build `Builds/ItemAppearance/RivetReach.exe`. Alternatively, the menu **Rivet Reach → Bake held item icons** refreshes only the 31 baked assets. Run the built player with `-rr-item-appearance -rr-output <absolute capture directory> -screen-fullscreen 0 -screen-width 1280 -screen-height 900`, then run `Tools/Review-ItemAppearance.ps1` with that directory and the same snapshot's `.docs/wiki/icons` directory.

The audit covers icon/held presentation and copper's two model choices. It does not establish frame-time performance, final art acceptance, every animation pose, dropped/placed appearance or a complete gameplay regression. Connected world geometry and tank sealing remain governed by their existing systems. The 2026-09-12 native snapshot includes the concurrently developed hand crank; its inclusion here is presentation evidence, not new verification of its electrical behavior.
