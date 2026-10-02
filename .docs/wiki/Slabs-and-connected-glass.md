# Slabs and connected glass

Use slabs for porches, half-height floors and ceilings, and Glass for clear workshop windows.

| Material | Make it | Result |
| --- | --- | --- |
| [Wooden Slab](Item-wooden-slab.md) | Three Planks in a horizontal workbench row | Six slabs |
| [Stone Slab](Item-stone-slab.md) | Three Stone in a horizontal workbench row | Six slabs |
| [Glass](Item-glass.md) | Smelt one Sand in a Furnace or Electric Furnace | One Glass |

Stone comes from smelting Cobblestone. Glass still works in all its existing component recipes.

## Place and combine slabs

Aim at the top of a block to place a lower slab. Aim at an underside to place an upper slab. On a side, the half you point at determines the half that fills.

Use another matching slab on the exposed half to combine them into a full block. This consumes one more slab; mining the combined block returns two slabs. Wooden and stone slabs do not mix. Pick Block selects the ordinary slab item even when aimed at an upper or combined block.

You can walk up a lower slab without jumping when there is headroom. Selection and collision follow its actual half-height shape. Stone slabs need a wooden pickaxe or better. An upper or combined slab has a full top surface for supported attachments; a lower slab does not. Slabs cannot share their cell with water.

## Build a continuous window

Place Glass blocks beside each other in a flat wall. Their shared borders disappear, leaving a continuous lightly tinted window with a thin outer rim. Corners join around openings, and the same connection rules continue across terrain chunk boundaries. Removing one block restores the rim around the gap. Glass remains solid and can be recovered by mining.

![A connected Glass workshop window with slab porch and roof](images/alpha-guidance/connected-glass-workshop.png)

![The window wrapping a workshop corner](images/alpha-guidance/connected-glass-corner-and-slabs.png)

![A removed Glass block exposes the edge around the new opening](images/alpha-guidance/glass-removed-edge-update.png)

Actual Windows player captures, October 2, 2026. This supplied construction fixture demonstrates appearance and placement; it is separate from the ordinary Survival route. Frame-rate measurements are deferred.

[Building and inventory](Building-and-inventory.md) · [Items](Items.md) · [Home](Home.md)
