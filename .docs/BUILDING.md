# Slabs and connected glass

Authorized after the alpha 0.1.0 review on 2026-10-02. This increment adds Wooden and Stone Slabs and makes the existing smelted Glass item placeable. It does not change terrain generation or any existing crafting/processing recipe. [Verification](verification/ALPHA_GUIDANCE_RESULTS.md) owns actual build evidence and remaining limits.

## Slabs

At a 3×3 Workbench, three Planks in one horizontal row make six Wooden Slabs; three Stone in one horizontal row make six Stone Slabs. Stone retains its existing Cobblestone smelting route. These additive working recipes were selected for this request; earlier starter recipes and the approved Cog/Rivet quantities remain unchanged.

Aim at the top of a block to place a lower half, or its underside to place an upper half. A side hit chooses the lower or upper half from its hit height. Using a matching slab on its exposed flat face combines both halves in the same cell. A complementary half in the adjacent destination may also combine. One successful command consumes exactly one slab; occupied/overlapping/rejected placement consumes none. Unlike materials do not combine.

Half-height rendering, selection and player collision share the same geometry. Walking can step up half a block when grounded, with headroom and support checks. Lower slabs do not support a floating bed, door, floor wire or torch at the cell above; upper and double slabs have a complete supporting top face. Half-slab side faces do not support wall torches. Slabs block gameplay light in their owning voxel, so a continuous slab roof shelters the space below. Slabs exclude fluids from the owning voxel; waterlogging is outside this increment. Creature placement rejection remains conservative at the occupied-cell boundary.

Wooden Slabs can be mined by hand and benefit from an axe. Stone Slabs require a wooden pickaxe or better. A single half returns one slab, a combined block returns two (including Drill extraction, which reserves capacity for both), and Pick Block always selects the public slab item. Held/dropped meshes and baked inventory icons show half height.

Public item IDs are 82 (`rivet:wooden_slab`) and 85 (`rivet:stone_slab`). IDs 83/84 and 86/87 are upper/double world-cell variants; they must never become inventory items. `IHalfBlock` is compiled into item capabilities; block collision uses the optional `IBlockCollisionShape` interface. Existing full-cube paths stay shared.

## Glass

One Sand still smelts into one Glass in either furnace. Existing Glass stacks and component recipes retain their identity and quantities. Ordinary Glass is a solid full building block, transmits gameplay light and drops one Glass when mined. Reinforced tank glass retains its separate multiblock role.

Glass geometry is batched per resident chunk with a shared transparent material, without an object per block. Adjacent Glass removes hidden internal faces. Eight-neighbour face connection masks suppress internal texture edges and retain outer rims and concave corners; the one-cell meshing halo supplies connections across chunk boundaries. Placing/removing neighbours invalidates existing resident mesh revisions. Lighting and fog use the project's shared field; the original material adds a faint cool tint, polished perimeter, grazing reflection and continuous world-aligned highlights.

Chunk meshes and materials are derived presentation. Slab variants and placed Glass use ordinary durable terrain edits; they do not regenerate or retrofit existing terrain. [Schema 19](SAVES.md#guidance-and-building-compatibility--2026-10-02) adds the last-death marker and explicitly retains previous catalog fingerprints.

See the [illustrated building guide](wiki/Slabs-and-connected-glass.md).
