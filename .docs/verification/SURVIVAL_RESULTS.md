# Survival progression verification

The latest [food/armor icon and healing review](SURVIVAL_HUD_RESULTS.md) covers the 2026-09-11 changes. The sections below retain their earlier build identities and feature evidence.

The 2026-09-09 crafting/survival increment implements **52 modular grid recipes, six furnace recipes, five tool tiers, copper/iron/diamond armor, world stations, potato farming, hunger, health and respawn**. The personal 2×2 and workbench 3×3 interfaces share the existing compiled recipe engine; its 4×4 core remains tested without a 4×4 station interface. [CRAFTING.md](../CRAFTING.md) owns authoring and transaction boundaries; [ECONOMY.md](../ECONOMY.md#current-survival-recipes-and-tiers) owns content and working tuning.

## Actual Unity build and core checks

Unity **6000.4.4f1**, Windows development player, **zero build errors and zero build warnings**, 17.881 seconds. The current build uses `RivetReach.Editor.ProjectBuild.PrepareAndBuildSurvival` and the committed mob wall-climbing code, plus the reviewed crafting interaction changes. The local executable is `Builds/Survival/RivetReach.exe`; [build context](survival/build-context.json), [summary](survival/build-summary.txt) and [source manifest](crafting/source-manifest.json) identify it. Assembly SHA256: `9e72f8b49ab11bc3823135825fede84e5254b79d3942539a81b95777296919e7`.

| Check | Actual result |
| --- | --- |
| [Integrated domain suite](survival/domain-checks.txt) | 92,338 assertions passed |
| [Crafting suite](survival/crafting-checks.txt) | 1,158,103 assertions passed |
| [Survival suite](survival/survival-checks.txt) | 47,146 assertions passed (including 1,744 independent recipe acceptance checks) |
| Terrain arrays | Both imported at 64×64×44 layers |
| Active furnace benchmark | 1,000 states × 200 one-tick advances: 34.285 ms total, zero measured managed bytes |
| 10,000-recipe lookup benchmark | 0.632 µs hit / 0.644 µs miss, zero measured managed bytes in warmed loops |
| Cached crafting preview | 1,000,000 reads: 4.540 ms, zero measured managed bytes |

Measurements used an Intel Core i7-10750H. They isolate synchronous domain work; they do not measure complete game frames, rendering, world remeshing or multiplayer throughput. The checks cover bootstrap reachability from gathered resources, tier gates, furnace fuel/output boundaries, incompatible/full destinations, conservation, random time-partition equivalence, recipe validation, food use, regeneration/starvation and equipment filtering/protection.

## Windows player input and lifecycle review

The [final survival runtime report](survival/runtime-report.json) passed **78 assertions with zero Unity/runtime errors**, at 1280×720 on an NVIDIA GeForce RTX 2060 and 32 GB system RAM. Verification owns virtual keyboard/mouse devices. Explicit fixtures construct the test stations and supply ingredients; ordinary sessions start empty-handed. Normal frame progression and input exercise interfaces/eating, while explicit simulation ticks accelerate crop and furnace boundaries.

The run verifies personal crafting clicks, splitting, result dragging, batch crafting, full inventory rejection, close/reopen and stale-handler protection; workbench and furnace opening; real ingredient/fuel/output transfers; smelting and baking; hoe use, planting, growth, harvest and uprooting; food/sprint rules and interrupted eating; four armor slots, damage reduction, death drops and respawn immunity. Chest transfers use the actual visible slots. Travelling 800 blocks unloads stations and shifts rendering origin: input/output, paid fuel/progress, chest contents and workbench ingredients survive; unloaded furnaces stop ticking, and returning retains the same station authorities. Dismantling drains contents exactly once.

Visual review checked the starter layouts, furnace UI and readable hearts/food/armor HUD. The retained earlier capture set includes: [personal crafting](survival/personal-workbench-ready.png), [3×3 pickaxe layout](survival/workbench-pickaxe-ready.png), [recipe guide](survival/workbench-recipes.png), [furnace](survival/furnace-smelting.png), [chest](survival/chest-storage.png), [potatoes](survival/potato-farm.png), [death screen](survival/death-and-respawn.png).

This run waited 27.643 seconds for all initial radius-10 terrain demand to drain and peaked at 3,301 resident chunks after the coordinated cave-loading correction. That is a complete-demand test barrier, not a measurement of the earliest playable frame. It did not collect frame percentiles; zero-valued frame fields in this focused report mean unmeasured. [Terrain results](TERRAIN_GENERATION_RESULTS.md) own the separate cave/biome visual review. Concurrent creature behavior and art are reviewed by their owner and are not certified by these survival assertions.

The latest [ore regression](survival/ore-runtime-report.json) passed **87 checks**: actual generated ores at five depths, tier feedback, extraction and pickup, bedrock collision/protection, and depletion after unloading/origin shifts. Its corridor fixture keeps an unobstructed pickup route and continuous support in caves; the ore itself is naturally generated. The latest [placement/movement regression](survival/placement-runtime-report.json) passed **95 checks**: 64-item overflow and mixed-identity merging, delayed pickup, obstacle separation, displaced piles, pillar jumping at three frame-rate targets and sprint/rebinding behavior. [Ore inventory](survival/ore-inventory.png), [protected floor](survival/bedrock-floor.png) and [displaced piles](survival/placement-items-pop-up.png) preserve their focused captures. These two separate regressions retain their original timestamps and assembly identities in the build context; they were not rerun for this starter-crafting review.

## Reproduce and remaining limits

Run `Tools/Verify-POC.ps1 -Survival -Executable Builds/Survival/RivetReach.exe -OutputDirectory <absolute folder>`. To build this feature directly, the pinned Editor exposes `RivetReach.Editor.ProjectBuild.PrepareAndBuildSurvival`; the normal `Tools/Build-Windows.ps1` produces `Builds/PlayerRevision4`, which the verification script uses when `-Executable` is omitted. Coordinate use of an already-open project rather than starting a competing Editor.

Recipe balance and long-session play quality still need user play review. The original run predates [durable saves](../SAVES.md); current builds preserve station/crop/world and survival state. Tool/armor wear, fitted armor meshes, irrigation, enchantments and animal farming remain unimplemented. [Current starter crafting verification](CRAFTING_RESULTS.md) records exact recipe layouts and the placed-workbench interaction flow.
