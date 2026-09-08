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

Rivet Reach is currently in **early brainstorming and specification design**. This repository contains documentation only; the POC is planned, not implemented.

The first objective is not content volume, but proving that an infinite voxel world, crafting, machinery, logistics and persistent background factories can all run efficiently on ordinary gaming hardware.

The design will remain **evolutive** while ideas are brainstormed and the POC is tested. Systems described today may be refined as implementation and performance testing expose better solutions.

## License and ownership

**Rivet Reach is proprietary and is not an open-source project.** Copyright © 2026 Starbugstone. All rights reserved.

Public visibility of this repository does not grant permission to use, copy, modify, redistribute, commercialize, or create derivative works from its source code, documentation, artwork, assets, or other protected material beyond rights required by GitHub's Terms of Service or applicable law.

See [LICENSE.md](LICENSE.md) for the complete terms.

## Design documents

- [PROJECT_PLAN.md](.docs/PROJECT_PLAN.md) - technical plan, architecture, POC and long-term target.
- [DEVELOPMENT_STRATEGY.md](.docs/DEVELOPMENT_STRATEGY.md) - how to build the final architecture incrementally while keeping gameplay responsive and performance-aware.
- [GAMEPLAY.md](.docs/GAMEPLAY.md) - responsive play, progression, exploration rewards and full-game completeness.
- [SIMULATION.md](.docs/SIMULATION.md) - working simulation/persistence contracts and candidate benchmark budgets.
- [LORE.md](.docs/LORE.md) - hidden lore, world history, Gatebuilders and current mob/world rules.
- [TRANSPORT.md](.docs/TRANSPORT.md) - Gate networks, rockets, coordinate-preserving travel, portal state and future transport systems.
- [ECONOMY.md](.docs/ECONOMY.md) - finite extraction, first recipes and durable exploration rewards.
- [CONTENT_PIPELINE.md](.docs/CONTENT_PIPELINE.md) - asset scale, Blender/Unity import and source conventions.
- [DELIVERY.md](.docs/DELIVERY.md) - release scope, verification and production dependencies.
- [DESIGN_QUESTIONS.md](.docs/DESIGN_QUESTIONS.md) - resolved working decisions, remaining questions and validation needs.

Project specifications live in `.docs/`. Contributor and agent workflow is recorded in [AGENTS.md](AGENTS.md).

**Decision status:** agreed direction describes existing project intent; working decisions resolve design gaps under the user's delegated request; proposals remain subject to brainstorming and future tests; open questions are not settled requirements. Numerical targets are provisional until measured.
