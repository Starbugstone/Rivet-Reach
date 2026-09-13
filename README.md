# Rivet Reach

**Rivet Reach** is a first-person voxel sandbox focused on **exploration, crafting, automation and technological progression**.

The game keeps the freedom of a block-based survival/crafting world while pushing much further into machinery, logistics and exploration. Players progress naturally by discovering resources and learning what can be built from them rather than through character levels or arbitrary technology unlocks.

The long-term vision combines:

- Infinite procedural voxel worlds with caves, biomes and generated structures.
- Minecraft-style block building, mining, inventory and grid crafting.
- Factorio-inspired automation using machines, item pipes, fluid pipes, power and signal networks.
- Technology evolving through distinct ages, including a strong steampunk/steam-industrial period.
- Rockets and automated colonies on other planets.
- Ancient ruined Gatebuilder networks leading to strange portal realms that must be explored personally.
- Multiplayer support after the single-player foundations are proven.

## Current status

**Download:** [Rivet Reach 0.0.1 Alpha for Windows x64](https://github.com/Starbugstone/Rivet-Reach/releases/tag/v0.0.1). Extract the complete Windows ZIP and run `RivetReach.exe`; no Unity installation is needed. **Save Game, Load Game and Continue Latest Save are included.** See the [alpha notes](.docs/releases/0.0.1.md).

The current Unity **6000.4.4f1 / URP 17.4.0** review build includes FPS movement, block mining/placement, inventory, **93 modular recipes**, personal 2×2 and workbench 3×3 crafting, five tool tiers, craftable light-emitting torches, furnaces, chests, potato farming, hunger, hearts and armor. It also includes seven biomes including seas and rivers, bucket water flow and caves, depth-banded ores, protected bedrock, generated trees, male/female appearances with two skins, a moving day/night sky, and native beetle/prowler enemies and [hovering Floaters](.docs/wiki/Floater.md).

For testing, the Escape menu now offers [Creative mode](.docs/GAMEPLAY.md#creative-testing-mode): double-tap Space flight, invincibility and a searchable catalog with sidebar dragging into inventory. The local Creative review build is `Builds/Creative/RivetReach.exe`.

Open `Assets/RivetReach/Scenes/Main.unity` in the pinned Editor and press Play, or launch the current local review build **`Builds/Creative/RivetReach.exe`**. [Run instructions and controls](.docs/FIRST_POC.md) explain the first crafting steps: logs → planks → workbench; place it, then **E or right-click** to open 3×3 crafting. Recipes are editable through the [authoring workflow](.docs/CRAFTING.md). The [item sidebar](.docs/CRAFTING.md#item-sidebar-and-recipe-discovery--2026-09-10) provides searchable icons, recipe/uses navigation, required stations, Shift-click recipe placement and maximum fill through Ctrl+Shift-click icons or Shift-click Fill grid. Hold right-click across crafting slots to place one item in each.

[Current verification and screenshots](.docs/verification/README.md) link the maintained report for each feature. [Starter recipe verification](.docs/verification/CRAFTING_RESULTS.md) checks the actual layouts and quantities. Superseded screenshots/reports remain in Git history.

This is an early playable increment. [Named saves](.docs/SAVES.md) preserve progress across restarts. Generated structures and the wider game remain planned. The [first industrial workshop](.docs/INDUSTRY.md) adds a 4×4 Machinist’s Bench, Azure resources, Blue Signal, steam/electricity, machines and separate item/fluid pipes. [Multiblock tanks](.docs/MULTIBLOCKS.md) add player-built shared storage, connected glass, valve/level controls and independent signal/power fittings on either transport pipe. [The development strategy](.docs/DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence) requires playable review before selecting further scope. Art, balance and long-session performance remain subject to review.

[Industrial verification](.docs/verification/INDUSTRY_RESULTS.md) includes in-game screenshots, a gameplay clip and measured limits.

For early electricity, craft a [Hand Crank](.docs/HAND_CRANK.md) at a workbench, attach it to a battery side and right-click or hold right-click to generate and store a small reserve. The focused review player is `Builds/HandCrank/RivetReach.exe`.

The [avatar rework](.docs/AVATAR_REWORK.md) updates both explorers, fitted fingerless gloves, tool grips and animations, and held equipment. Try `Builds/AvatarRework/RivetReach.exe`; [source and Unity evidence](.docs/verification/AVATAR_REWORK_RESULTS.md) records the checks and remaining visual review.

The chest, workbench and furnace now have [detailed station models](.docs/verification/STARTER_STATION_RESULTS.md) in the Machinist’s Bench material family, with matching held items and icons. The focused review build is `Builds/StarterStations/RivetReach.exe`.

Natural leaves now occasionally drop **saplings and apples**. Replant saplings on grass or dirt to grow trees; hold Use to eat apples. Try `Builds/Orchard/RivetReach.exe`; [rules](.docs/GAMEPLAY.md#saplings-and-apples) and [verification](.docs/verification/ORCHARD_RESULTS.md) cover growth, drops and save compatibility.

## License and ownership

**Rivet Reach is proprietary and is not an open-source project.** Copyright © 2026 Starbugstone. All rights reserved.

Public visibility of this repository does not grant permission to use, copy, modify, redistribute, commercialize, or create derivative works from its source code, documentation, artwork, assets, or other protected material beyond rights required by GitHub's Terms of Service or applicable law.

See [LICENSE.md](LICENSE.md) for the complete terms.

## Design documents

- [Concept art](.docs/concept-art/README.md) - current player turnarounds and equipment direction; visual references, not implemented screenshots.
- [PROJECT_PLAN.md](.docs/PROJECT_PLAN.md) - technical plan, architecture, POC and long-term target.
- [DEVELOPMENT_STRATEGY.md](.docs/DEVELOPMENT_STRATEGY.md) - how to build the final architecture incrementally while keeping gameplay responsive and performance-aware.
- [GAMEPLAY.md](.docs/GAMEPLAY.md) - responsive play, progression, exploration rewards and full-game completeness.
- [SAVES.md](.docs/SAVES.md) - saving, loading, continuing and backup recovery.
- [SIMULATION.md](.docs/SIMULATION.md) - working simulation/persistence contracts and candidate benchmark budgets.
- [LORE.md](.docs/LORE.md) - hidden lore, world history, Gatebuilders and current mob/world rules.
- [TRANSPORT.md](.docs/TRANSPORT.md) - Gate networks, rockets, coordinate-preserving travel, portal state and future transport systems.
- [FLUIDS.md](.docs/FLUIDS.md) - seas, rivers, bucket water and reusable fluid rules.
- [CRAFTING.md](.docs/CRAFTING.md) - editable recipes, shared grid engine and transaction/verification contract.
- [ECONOMY.md](.docs/ECONOMY.md) - finite extraction, first recipes and durable exploration rewards.
- [CONTENT_PIPELINE.md](.docs/CONTENT_PIPELINE.md) - asset scale, Blender/Unity import and source conventions.
- [DELIVERY.md](.docs/DELIVERY.md) - release scope, verification and production dependencies.
- [DESIGN_QUESTIONS.md](.docs/DESIGN_QUESTIONS.md) - resolved working decisions, remaining questions and validation needs.

Project specifications live in `.docs/`. Contributor and agent workflow is recorded in [AGENTS.md](AGENTS.md).

**Decision status:** agreed direction describes existing project intent; working decisions resolve design gaps under the user's delegated request; proposals remain subject to brainstorming and future tests; open questions are not settled requirements. Numerical targets are provisional until measured.

Player guides: [GitHub wiki](https://github.com/Starbugstone/Rivet-Reach/wiki) · [repository copy](.docs/wiki/Home.md). Browse the [visual item catalog](.docs/wiki/Items.md) for individual item explanations, linked icons and crafting/processing recipes, alongside the tank, pump, battery and connection guides. [Wiki authoring and publishing](.docs/WIKI_AUTHORING.md) explains how to maintain and deploy the local copy.

Craft [wooden doors](.docs/DOORS.md) from six planks at a workbench. Right-click either half to open/close, or connect Blue Signal at the base. The focused review player is `Builds/Doors/RivetReach.exe`; see [door verification](.docs/verification/DOOR_RESULTS.md).

[Upgraded armor and ingots](.docs/EQUIPMENT_ART.md) now have matching 3D held/dropped models and inventory icons. Equipped armor appears on both explorers, the inventory portrait and first-person bracers. Try `Builds/EquipmentArt/RivetReach.exe`; [verification](.docs/verification/EQUIPMENT_ART_RESULTS.md) records source and native-player checks.

[All-face connections and wrench verification](.docs/verification/CONNECTION_RESULTS.md) covers the craftable wrench, machine-facing pipe controls and saved directions. Players can follow the [Pipes guide](.docs/wiki/Pipes.md).

The player now carries **56 backpack slots (seven rows of eight)** and a **15-slot hotbar**. Try `Builds/Inventory/RivetReach.exe`; [inventory verification](.docs/verification/INVENTORY_RESULTS.md) records the layout, controls and older-save migration checks.

[Electrical grids](.docs/BATTERIES.md#cable-defined-grids--2026-09-12) now follow connected cables, with separate battery input/output networks, storage of all surplus and **800 W** boiler/alternator generation. Batteries share surplus/deficits equally, and item/fluid pipes use the same capped allocation rules. The focused review player is `Builds/PowerGrid/RivetReach.exe`; [power-grid verification](.docs/verification/POWER_GRID_RESULTS.md) records the checks.

[Lava](.docs/FLUIDS.md#lava--2026-09-13) adds slow nonrenewing flow, near-bedrock lakes in new worlds, lava buckets and burning hazards. The focused review player is `Builds/Lava/RivetReach.exe`; [lava verification](.docs/verification/LAVA_RESULTS.md) records the checks and [the player guide](.docs/wiki/Lava.md) explains safe collection.
