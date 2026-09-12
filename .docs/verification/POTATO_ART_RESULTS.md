# Potato item art — 2026-09-12

The user's request upgrades raw and baked potatoes to the game's warm, faceted item style. The new source meshes supply inventory/hotbar icons, palm-held food and physical dropped stacks. Item IDs, quantities, recipes, planting, nutrition and saves retain their existing rules.

## Reproduce

- Author/export/render: Blender 5.2.0 LTS, `--background --python Tools/create_potato_assets.py`.
- Import/build/runtime check: `powershell -File Tools/Verify-PotatoArt.ps1 -Build`, using Unity 6000.4.4f1 in `Builds/PotatoArtVerificationProject` to preserve the open working Editor.
- Play the resulting `Builds/PotatoArt/RivetReach.exe`; enable Creative in Escape and search for potato in inventory to try both items.

Editable original source: [Potatoes.blend](../../ArtSource/Food/Potatoes.blend). [Source render](../../ArtSource/Food/potato-review.png) shows the actual exported geometry, not generated concept art. Each source contains 812 triangles, one mesh and one material slot. Runtime exports share a 128² skin palette and use separate transparent 256² icons. No third-party artwork or runtime dependency was introduced.

## Measured Unity evidence

The [Windows build](potato-art-2026-09-12-build.txt) succeeded with **zero errors and zero warnings**. The [runtime report](potato-art-2026-09-12-runtime.json), recorded at **2026-09-12 09:12 UTC**, passed **17 assertions** on Unity 6000.4.4f1 / Direct3D11 at 1280×720. The review uses committed base `da9761d` plus this potato patch; concurrent unfinished station/tool-icon work is excluded from the review copy.

[Import checks](potato-art-2026-09-12-import.txt) measured **812 triangles** per food, **2,165 raw / 2,168 baked vertices**, and one mesh/material slot each. They verify scale, axes, normals, UVs, material remapping and avoidance of the held-tool shader's metallic palette regions. Held food preserves the FBX axis conversion before adding the palm's yaw.

The runtime checks cover visible authored held geometry, shared palettes, three-mesh dropped stacks, actual inventory texture bindings and eating one baked potato to restore five food points. The final source renders and native screenshots were visually reviewed for the two distinct surfaces, palm contact, baked-opening orientation and icon readability.

![Raw potato in hand](potato-art-2026-09-12-held-Potato.png)

![Baked potato in hand](potato-art-2026-09-12-held-BakedPotato.png)

[Inventory and matching item icons](potato-art-2026-09-12-potato-inventory.png) · [Dropped stacks](potato-art-2026-09-12-potato-drops.png).

## Limits

This focused art check does not establish overall frame cost, large food-pile performance or final artistic acceptance. Existing item simulation, stack-copy caps, visibility distance and palm/hand animation remain shared with other items. Crop growth-stage graphics are outside this item-art request.
