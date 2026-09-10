# World fluids, seas and rivers

The user authorized world liquids, a sea biome, rivers and bucket collection/placement, with Minecraft-style behaviour. They explicitly confirmed that **two-source renewal is a per-fluid boolean**, enabled for water and independently disabled for other liquids. This supersedes the earlier no-renewal water proposal. These are original implementations; no third-party code or assets were imported.

## Playable water rules

- Water has explicit source cells, seven progressively weaker horizontal flow levels and falling cells. It flows downward first. On supported ground it spreads up to seven cells from a source or the foot of a falling column. A bounded four-cell route search prefers nearby drops, sharing equally short routes; otherwise it spreads on the flat. Blocking a route or removing a source wakes neighbouring flow; disconnected flow recedes.
- A cell with at least two horizontally adjacent sources of the **same fluid** becomes a source only when that fluid's `RenewsSources` is true and the cell rests on a blocking cell or a source of the same fluid. Diagonal placement into a supported 2×2 pool therefore renews water. Flowing neighbours alone do not renew it. Sources never convert another fluid type.
- Craft one bucket from three iron ingots on a workbench: `I.I / .I.`. Use an empty bucket on a source within five blocks to collect it. Use a filled bucket on a block face to place a source. Flowing water cannot be collected. Empty and filled buckets each occupy one inventory slot; collection and placement replace that same slot, including in a full inventory. Occupied, out-of-reach, occluded and unready targets reject the action without consuming the bucket.
- Terrain construction displaces water in the destination cell and redirects subsequent flow. Water is passable, does not obstruct ordinary mining rays, and cannot be mined into a block item. Empty-bucket aiming includes sources through flowing water.
- Immersion slows player travel and applies the fluid current. Hold Jump to rise and Crouch to descend; otherwise the player sinks gently. Immersion resets accumulated fall damage. Existing male/female collision dimensions and appearance remain unchanged.
- Dropped items experience drag and current. `ItemDefinition.buoyant` defaults to false, so ordinary items sink. A definition can opt into rising. This does not change item identity, quantities, pickup delay or lifetime.

The familiar bucket and renewal reference is Mojang's [bucket overview](https://www.minecraft.net/en-us/article/taking-inventory-bucket) and [water overview](https://www.minecraft.net/en-us/article/block-week-water). The request selects similar block-fluid behaviour, not exact compatibility with every Minecraft edition, tick-order quirk or block interaction.

## Fluid abstraction and extension

[`FluidDefinition`](../Assets/RivetReach/Code/Fluids/FluidDefinition.cs) is immutable session data: stable identity, compact cell encoding, filled-bucket item, horizontal reach, tick delay, **`bool RenewsSources`**, drag, current speed and display colour. Water's working definition sets `renewsSources: true`. A future lava definition can pass `false`, a shorter reach and a longer delay. Changing renewal does not require another solver or a water-specific branch.

`FluidRegistry` validates and resolves definitions once. `FluidSimulation` consumes a registry and the plain C# `IFluidWorld` interface; `BucketTransfer` uses the same registry. Domain checks instantiate a second, slower, nonrenewing test liquid alongside water. This fixture is not shipped lava content. Runtime `Fluids.Registry` registers only water; adding playable lava later also requires its item/assets and any separately selected damage or mixing rules.

The present byte voxel storage reserves nine consecutive encodings per fluid: source at offset 0, horizontal levels 1–7, falling at offset 8. Water uses 100–108; bucket item IDs are 94–95. Stable `rivet:water` identity is separate from the runtime encoding. Future definitions must reserve nonoverlapping cell ranges and item IDs. This extends the current compact voxel format rather than representing each fluid cell with a GameObject. Expanding beyond the byte address space requires an explicit storage migration; this is not an unlimited fluid registry.

## Scheduling, rendering and session lifetime

Each `VoxelWorld` owns its fluid scheduler. The 20 Hz simulation clock schedules water updates five ticks apart (0.25 seconds), with at most 512 cell evaluations per tick and at most four ticks per frame. A deterministic due-time FIFO and deduplication bound each work batch; late work remains queued. Local commands and neighbour changes wake work; settled interiors have no periodic volume scan. Bounded route search reuses its scratch storage.

This implementation uses sequential authority updates in scheduled order, replacing the earlier proposed whole-step snapshot/coordinate-sort rule. That matches the existing synchronous voxel mutation boundary and keeps work incremental. It is deterministic for the same initial world, readiness and command/tick sequence; arbitrary chunk arrival orders are not claimed to be lockstep-equivalent.

Unready frontiers are closed. Requests wait by chunk and resume when that chunk becomes ready. Workers identify exposed or unsettled cells for initial activation; settled source interiors and shared source boundaries never enter the queue on mere residency. Modified fluid state uses the existing world-aware, signed-coordinate session edits and revision-safe immutable worker snapshots. Fluid changes update collision immediately and dirty adjacent meshes; they do not synchronously rebuild a chunk per flowing cell. Surface bounds include water above the seabed. Floating-origin shifts move the owning chunk views.

`FluidMesher` emits only exposed surfaces into a separate transparent mesh, with level-dependent heights and definition colours. Its URP shader adds subtle animated ripples, directional lighting and world fog. Water contributes no ordinary terrain collider and uses no imported texture. Fluid surfaces are currently stepped between levels, with standard transparent sorting rather than screen-space refraction.

Sources and flow edits survive unloading during the session; **quitting still resets the world**. Durable saves, fluid reactions, lava gameplay, waterlogging, irrigation, drowning, boats, pumps, tanks and pipes are outside this increment. [Placed torches](GAMEPLAY.md#torches) are displaced by incoming water and drop one recoverable item. Crops and stations currently block flow; fluid destruction of those blocks is not implemented.

## Generation and verification

[TERRAIN_GENERATION.md](TERRAIN_GENERATION.md) owns the sea/river profile and compatibility version. [Fluid verification](verification/FLUID_RESULTS.md) owns measured checks, screenshots and remaining limits. Run `Tools/Verify-Fluids.ps1 -Build` with the pinned Editor available to validate and build `Builds/Fluids/RivetReach.exe`, then run the explicit player review. Ordinary sessions receive no test fixtures or starter buckets.
