# Current verification evidence

[Named bridges and chunk loaders](BRIDGE_RESULTS.md): private pairs, remote resource conservation, persistent chunk tickets and schema-9 saves.

[Shift + wrench click](SHIFT_WRENCH_RESULTS.md): crouched item/fluid end configuration and one change per press.

[Disconnected pipe ends and wrench-only arrows](PIPE_DISCONNECT_RESULTS.md): three-state cycle, missing-arm reconnection, saved states and native-player evidence.


[Starter station graphics](STARTER_STATION_RESULTS.md) — chest, workbench and furnace brought into the Machinist’s Bench material family; actual Blender source and focused Unity review.

- [Save/load/continue verification](SAVE_RESULTS.md) — full-state persistence, failure recovery and fresh-process continuation.

This directory maintains the latest report and useful evidence for each feature. Superseded reports, obsolete recipe screenshots, earlier character models and intermediate failing captures are kept in Git history rather than maintained here. Dates and artifact identities matter: a focused check of one feature does not certify every system or whole-game performance.

| Feature | Current evidence |
| --- | --- |
| Floater and Floater Rock | [Floater verification](FLOATER_RESULTS.md) — hovering collision, combat, single-award loot, saves and original Blender art |
| Electric furnace | [Electric furnace verification](ELECTRIC_FURNACE_RESULTS.md) — shared recipes, exact electricity, pipes, save continuity and actual imported art |
| Lava, deep lakes and burning | [Lava verification](LAVA_RESULTS.md) — slow nonrenewing flow, buckets, item destruction, heat and pinned-generator saves |
| Expanded player inventory and hotbar | [Inventory verification](INVENTORY_RESULTS.md) — seven backpack rows, 15 hotbar slots, input, transfers and save migration |
| Player-facing station and machine placement | [Placement facing](PLACEMENT_FACING_RESULTS.md) — four directions, right-click, rendered starter bounds and schema-5 save compatibility |
| Furnace pipes, cargo compatibility and receiver priority | [Furnace pipe verification](FURNACE_PIPE_RESULTS.md) — ingredient/fuel routing, matching inputs/products, exact transfers and scheduler wake |
| All-face machine connections, wrench and pipe arrows | [Connection verification](CONNECTION_RESULTS.md) — held-wrench input, exact transfers, schema-4 persistence and earlier checkpoints |
| Cable-defined power grids and battery input/output | [Power-grid verification](POWER_GRID_RESULTS.md) — split/rejoin recovery, exact shared storage, uncapped surplus and 800 W generation |
| Electrical connection versus available power | [Power-status verification](POWER_STATUS_RESULTS.md) — registered connections through blackouts, atomic graph publication and actual machine panels |
| Quit Without Saving, origin respawn and pickaxe balance | [Gameplay fixes](GAMEPLAY_FIX_RESULTS.md) — Editor/standalone quit, checkpoint preservation, surface spawning and ore timing |
| Downloadable Windows 0.0.1 alpha | [Alpha build and release verification](ALPHA_0_0_1_RESULTS.md) — exact source commit, runtime checks and download integrity |
| Item sidebar, recipe/uses navigation and Shift/Ctrl+Shift placement | [Recipe browser verification](RECIPE_BROWSER_RESULTS.md) |
| Default missing-ingredient borders in recipe previews | [Missing-ingredient verification](MISSING_INGREDIENT_RESULTS.md) |
| Mouse feedback in menus and crafting | [Pointer responsiveness](POINTER_RESULTS.md) — moving-pointer timings and same-frame held-stack display |
| Retained crafting screens and stale-input protection | [Screen reuse verification](SCREEN_REUSE_RESULTS.md) — opening costs, station/save rebinding and conservation |
| Performance and live Editor script reloads | [Performance pass](PERFORMANCE_RESULTS.md) — measured UI/meshing costs, streaming checks and preserved gameplay |
| Terrain/tree shadow stability and lighting cost | [Sun-shadow ticks](SUN_SHADOW_RESULTS.md), plus the earlier [AO sampling check](SHADOW_RESULTS.md) |
| Multiblock tanks and shared pipe channel fittings | [Multiblock verification](MULTIBLOCK_RESULTS.md) |
| Machine illustrations in interaction panels | [Machine interface verification](MACHINE_INTERFACE_RESULTS.md) — native station captures and retained controls |
| Blue Signal, power and the first workshop | [Industry verification](INDUSTRY_RESULTS.md) — screenshots, gameplay clip, machine interfaces and measured limits |
| Stone and cobblestone processing into sand | [Crusher sand recipes](CRUSHER_SAND_RESULTS.md) — quantities, power, backpressure and discovery checks |
| Inventory icons and held item agreement | [Item appearance audit](ITEM_APPEARANCE_RESULTS.md) — complete registry, shared models/tints and visual comparisons |
| Ore materials, shared mesh and held reuse | [Copper/iron drops](ORE_DROPS_RESULTS.md), [ore variants and sharing](ORE_VARIANTS_RESULTS.md), [Azure source review](AZURE_ORE_RESULTS.md) |
| Pass-through props, camera clearance and Editor icons | [Clearance verification](PROP_CLEARANCE_RESULTS.md) |
| Craftable torches and local lighting | [Torch verification](TORCH_RESULTS.md) |
| Creative sidebar dragging, flight controls and full workshop run | [Creative verification](CREATIVE_RESULTS.md) — current build, Creative industry/tanks and regressions |
| Pumps without electricity | [Pump checks](PUMP_RESULTS.md) — cold startup, blackout recovery and conserved water |
| Batteries, historical pump intake/power, outward controllers, held items and wiki | [Workshop follow-up](WORKSHOP_FOLLOWUP_RESULTS.md) — electrical storage, construction and rendered item checks |
| Starter recipes and block interaction | [Crafting verification](CRAFTING_RESULTS.md) — exact layouts/quantities, placed workbench and 3×3 UI |
| Potato item graphics | [Potato art](POTATO_ART_RESULTS.md) — Blender sources, icons, held items and drops |
| Furnaces, farming, hunger, health and armor | [Survival verification](SURVIVAL_RESULTS.md), including the latest ore and placement regressions |
| Seas, rivers, buckets and fluid physics | [Fluid verification](FLUID_RESULTS.md) — per-fluid renewal, bucket controls, source/flow and session streaming |
| Biomes, caves and streaming | [Terrain verification](TERRAIN_GENERATION_RESULTS.md) |
| Creatures and combat | [Mob verification](MOB_RESULTS.md), plus the current [beetle wall-climbing extension](BEETLE_WALL_CLIMB_RESULTS.md) |
| Sun, moon and time | [Day/night verification](DAY_NIGHT_RESULTS.md) |
| Player model, hands, held equipment and animation | [Avatar rework verification](AVATAR_REWORK_RESULTS.md) |
| Food-to-mouth animation and interrupted bites | [Eating verification](EATING_RESULTS.md) |
| Audio and dated Editor startup | [Audio and earlier startup verification](HIFI_PLAYER_AND_AUDIO_RESULTS.md) |
| Item pickup ranges and passable-block collection | [Pickup verification](PICKUP_RESULTS.md) |
| Mining, placement and pickup effects | [Effects verification](ARCADE_VISUAL_RESULTS.md) |
| Generated trees, felling and placed-wood protection | [Tree verification](TREE_RESULTS.md) |

[Run instructions and controls](../FIRST_POC.md) identify the current review executable. Feature reports preserve their own tested build and workload; older measurements are not relabeled as tests of the latest executable. Artistic acceptance and long-session play balance still need play review. [Named saves](../SAVES.md) now preserve implemented surface-world progress across restarts.

[Hand crank results](HAND_CRANK_RESULTS.md) record manual battery charging, pointer click/hold behavior, the original Blender/Unity asset and additive save compatibility for the 2026-09-12 increment.

- [Wooden doors](DOOR_RESULTS.md): crafting, two-cell placement/recovery, right-click, Blue Signal, collision and durable-state checks.

- [Saplings and apples](ORCHARD_RESULTS.md): natural leaf loot, replanting, blocked growth, apple eating, original art and durable tree state.

- [Battery charge fill](BATTERY_FILL_RESULTS.md): visible stored-energy level, individual bank cells and save/load reconstruction.

- [Fitted armor and ingots](EQUIPMENT_ART_RESULTS.md): matching 3D inventory/held/dropped art, equipped male/female avatars, first-person bracers and portrait synchronization.

[Machine fuel faces](FUEL_FACE_RESULTS.md) covers rear-only fuel, dual-purpose logs, rotated placement and ordinary inputs on fuel-free machines.

- [Ranged liquid pump](RANGED_PUMP_RESULTS.md): source collection, eight-block reach, lava transport, save compatibility and imported machine.

- [Farming and cooking](FARMING_RESULTS.md): shared crop growth, forage/textiles, item tags, basic/electric cookers and persistent per-chunk generation, with original art and actual player captures (2026-09-13).

- [Stability and abstraction review](STABILITY_REVIEW_RESULTS.md): compiled tags/ingredient matching, cooker automation, save compatibility and complete mob wiki coverage (2026-09-13).
