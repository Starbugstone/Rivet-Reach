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

Rivet Reach is in **early development and specification design**. The repository root is now a Unity **6000.4.4f1** project using **URP 17.4.0**. It now includes the first playable terrain/FPS/inventory POC, with generated log/leaf trees, axe-only upward felling of generated logs, male/female player models and two interchangeable skins, plus modular crafting in the personal 2×2 inventory grid, five ores distributed by depth and an unbreakable bedrock floor. This is an early review build; the wider game remains planned.

Open this repository folder in Unity Hub with the pinned Editor version. Open `Assets/RivetReach/Scenes/Main.unity` and press Play. The Windows executable is generated at `Builds/PlayerRevision4/RivetReach.exe`. See [ore levels and bedrock](.docs/ECONOMY.md#current-ore-generation-and-bedrock), [ore verification](.docs/verification/ORE_RESULTS.md), [POC controls and build instructions](.docs/FIRST_POC.md), [modular crafting verification](.docs/verification/CRAFTING_RESULTS.md), the [high-fidelity player and audio revision](.docs/verification/HIFI_PLAYER_AND_AUDIO_RESULTS.md), [faster swings and starter tools](.docs/verification/TOOL_HOTBAR_RESULTS.md), [tree and axe checks](.docs/verification/TREE_RESULTS.md), [building, movement and dropped-item checks](.docs/verification/BUILDING_AND_MOVEMENT_RESULTS.md), [grass and hand interactions](.docs/verification/GRASS_AND_HAND_INTERACTION_RESULTS.md), and the [player asset results](.docs/verification/PLAYER_POLISH_RESULTS.md). See [Unity setup](.docs/UNITY_SETUP.md) for the exact setup and verification notes.

The **locked first playable step** is terrain generation and chunk streaming, FPS movement, a 3D player, fist mining, terrain-block placement, functional inventory and originally a crafting placeholder inside that inventory. The user subsequently authorized [functional modular crafting](.docs/CRAFTING.md), now connected to the 2×2 personal grid with a shared core for future 3×3/4×4 interfaces. It contains **no generated structures**. Establish the visual concept and validate terrain/loading through play before choosing the next implementation. Larger crafting station interfaces, the broader recipe/progression chain, industry and background factories remain later ambitions. See the authoritative [first-step scope and review gate](.docs/DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence). The first slice is implemented for review. Progress survives chunk unloading in-session but resets when the game quits.

The design will remain **evolutive** while ideas are brainstormed and the POC is tested. Systems described today may be refined as implementation and performance testing expose better solutions.

## License and ownership

**Rivet Reach is proprietary and is not an open-source project.** Copyright © 2026 Starbugstone. All rights reserved.

Public visibility of this repository does not grant permission to use, copy, modify, redistribute, commercialize, or create derivative works from its source code, documentation, artwork, assets, or other protected material beyond rights required by GitHub's Terms of Service or applicable law.

See [LICENSE.md](LICENSE.md) for the complete terms.

## Design documents

- [Concept art](.docs/concept-art/README.md) - player turnarounds, future equipment and three first-person terrain/UI drafts; visual references, not implemented screenshots.
- [PROJECT_PLAN.md](.docs/PROJECT_PLAN.md) - technical plan, architecture, POC and long-term target.
- [DEVELOPMENT_STRATEGY.md](.docs/DEVELOPMENT_STRATEGY.md) - how to build the final architecture incrementally while keeping gameplay responsive and performance-aware.
- [GAMEPLAY.md](.docs/GAMEPLAY.md) - responsive play, progression, exploration rewards and full-game completeness.
- [SIMULATION.md](.docs/SIMULATION.md) - working simulation/persistence contracts and candidate benchmark budgets.
- [LORE.md](.docs/LORE.md) - hidden lore, world history, Gatebuilders and current mob/world rules.
- [TRANSPORT.md](.docs/TRANSPORT.md) - Gate networks, rockets, coordinate-preserving travel, portal state and future transport systems.
- [CRAFTING.md](.docs/CRAFTING.md) - editable recipes, shared grid engine and transaction/verification contract.
- [ECONOMY.md](.docs/ECONOMY.md) - finite extraction, first recipes and durable exploration rewards.
- [CONTENT_PIPELINE.md](.docs/CONTENT_PIPELINE.md) - asset scale, Blender/Unity import and source conventions.
- [DELIVERY.md](.docs/DELIVERY.md) - release scope, verification and production dependencies.
- [DESIGN_QUESTIONS.md](.docs/DESIGN_QUESTIONS.md) - resolved working decisions, remaining questions and validation needs.

Project specifications live in `.docs/`. Contributor and agent workflow is recorded in [AGENTS.md](AGENTS.md).

**Decision status:** agreed direction describes existing project intent; working decisions resolve design gaps under the user's delegated request; proposals remain subject to brainstorming and future tests; open questions are not settled requirements. Numerical targets are provisional until measured.
