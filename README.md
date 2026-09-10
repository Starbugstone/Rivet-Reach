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

**Download:** [Rivet Reach 0.0.1 Alpha for Windows x64](https://github.com/Starbugstone/Rivet-Reach/releases/tag/v0.0.1). Extract the complete Windows ZIP and run `RivetReach.exe`; no Unity installation is needed. **World progress resets when you quit.** See the [alpha notes](.docs/releases/0.0.1.md).

The current Unity **6000.4.4f1 / URP 17.4.0** review build includes FPS movement, block mining/placement, inventory, **89 modular recipes**, personal 2×2 and workbench 3×3 crafting, five tool tiers, craftable light-emitting torches, furnaces, chests, potato farming, hunger, hearts and armor. It also includes seven biomes including seas and rivers, bucket water flow and caves, depth-banded ores, protected bedrock, generated trees, male/female appearances with two skins, a moving day/night sky, and native beetle/prowler enemies.

For testing, the Escape menu now offers [Creative mode](.docs/GAMEPLAY.md#creative-testing-mode): double-tap Space flight, invincibility and a searchable catalog with sidebar dragging into inventory. The local Creative review build is `Builds/Creative/RivetReach.exe`.

Open `Assets/RivetReach/Scenes/Main.unity` in the pinned Editor and press Play, or launch the current local review build **`Builds/Creative/RivetReach.exe`**. [Run instructions and controls](.docs/FIRST_POC.md) explain the first crafting steps: logs → planks → workbench; place it, then **E or right-click** to open 3×3 crafting. Recipes are editable through the [authoring workflow](.docs/CRAFTING.md). The [item sidebar](.docs/CRAFTING.md#item-sidebar-and-recipe-discovery--2026-09-10) provides searchable icons, recipe/uses navigation, required stations and Ctrl-click recipe placement.

[Current verification and screenshots](.docs/verification/README.md) link the maintained report for each feature. [Starter recipe verification](.docs/verification/CRAFTING_RESULTS.md) checks the actual layouts and quantities. Superseded screenshots/reports remain in Git history.

This is an early playable increment. Progress survives chunk unloading during a session and resets on quit. Durable saves, generated structures and the wider game remain planned. The [first industrial workshop](.docs/INDUSTRY.md) adds a 4×4 Machinist’s Bench, Azure resources, Blue Signal, steam/electricity, machines and separate item/fluid pipes. [Multiblock tanks](.docs/MULTIBLOCKS.md) add player-built shared storage, connected glass, valve/level controls and independent signal/power fittings on either transport pipe. [The development strategy](.docs/DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence) requires playable review before selecting further scope. Art, balance and long-session performance remain subject to review.

[Industrial verification](.docs/verification/INDUSTRY_RESULTS.md) includes in-game screenshots, a gameplay clip and measured limits.

The [avatar rework](.docs/AVATAR_REWORK.md) updates both explorers, fitted fingerless gloves, tool grips and animations, and held equipment. Try `Builds/AvatarRework/RivetReach.exe`; [source and Unity evidence](.docs/verification/AVATAR_REWORK_RESULTS.md) records the checks and remaining visual review.

## License and ownership

**Rivet Reach is proprietary and is not an open-source project.** Copyright © 2026 Starbugstone. All rights reserved.

Public visibility of this repository does not grant permission to use, copy, modify, redistribute, commercialize, or create derivative works from its source code, documentation, artwork, assets, or other protected material beyond rights required by GitHub's Terms of Service or applicable law.

See [LICENSE.md](LICENSE.md) for the complete terms.

## Design documents

- [Concept art](.docs/concept-art/README.md) - current player turnarounds and equipment direction; visual references, not implemented screenshots.
- [PROJECT_PLAN.md](.docs/PROJECT_PLAN.md) - technical plan, architecture, POC and long-term target.
- [DEVELOPMENT_STRATEGY.md](.docs/DEVELOPMENT_STRATEGY.md) - how to build the final architecture incrementally while keeping gameplay responsive and performance-aware.
- [GAMEPLAY.md](.docs/GAMEPLAY.md) - responsive play, progression, exploration rewards and full-game completeness.
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

Player construction guides: [GitHub wiki](https://github.com/Starbugstone/Rivet-Reach/wiki) · [repository copy](.docs/wiki/Home.md). Includes tanks, pumps, batteries and separate power/signal connections.
