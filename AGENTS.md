# Rivet Reach working instructions

## Current project phase

The project is in early development with an implemented Unity **6000.4.4f1 / URP** playable increment. [FIRST_POC.md](.docs/FIRST_POC.md) owns current run/build instructions and controls; [current verification](.docs/verification/README.md) distinguishes measured evidence from remaining review. Keep work scoped to the user's current request. Later roadmap groups are candidates, not an automatic queue; no Editor upgrade or live integration installation is implied.

The authorized and implemented scope includes terrain/FPS movement, inventory, terrain placement, generated trees and axe felling, modular personal 2×2/workbench 3×3 crafting with a tested future 4×4 core, five depth-banded ores and protected bedrock, five tool tiers, furnace/chest, potatoes and baking, farming, hunger, health/hearts and copper/iron/diamond armor. Ordinary sessions start empty-handed. [GAMEPLAY.md](.docs/GAMEPLAY.md), [CRAFTING.md](.docs/CRAFTING.md) and [ECONOMY.md](.docs/ECONOMY.md) own the current rules, authoring and progression. The user requires the implemented beginning recipes to match Minecraft's ingredient layouts and quantities; original code/art remain required. Stations open through the bound Interact action or mouse Use.

The authorized day/night cycle includes moving sun/moon and nightly lunar phases. [GAMEPLAY.md](.docs/GAMEPLAY.md#day-night-and-lunar-phases) owns behavior; [SIMULATION.md](.docs/SIMULATION.md#session-world-clock-and-celestial-presentation) owns the clock/API boundary. Irrigation, equipment wear, fitted armor meshes, industry, generated structures and durable saves remain later scope.

Player appearance must offer male and female model choices, both supporting changeable skins. Keep appearance selection separate from gameplay dimensions/capabilities and verify actual model/rendering cost before claiming performance. Treat the illustrated character as one proposed appearance, not a fixed identity. [GAMEPLAY.md](.docs/GAMEPLAY.md#15-player-skins) owns skin behaviour and the working first-step minimum; [CONTENT_PIPELINE.md](.docs/CONTENT_PIPELINE.md#6-player-skin-authoring-contract) owns model/texture constraints. The first slice includes terrain-block placement (added by the user’s POC feedback), both model variants and two local test skins; further customization remains scoped by later requests.

The user subsequently authorized varied terrain, caves and biomes. [TERRAIN_GENERATION.md](.docs/TERRAIN_GENERATION.md) owns the working surface/cave profile, materials and generation compatibility. Preserve the ore bands, protected bedrock base and coordinated survival crop hook when changing generation. [Terrain verification](.docs/verification/TERRAIN_GENERATION_RESULTS.md) distinguishes generator checks from actual Unity evidence.

The user subsequently authorized native mobs, Blender-authored enemies, AI and spawning, selecting Rustback beetle and Dusk prowler. [MOBS.md](.docs/MOBS.md) owns the working species, combat, spawning, voxel navigation and lifecycle rules; [mob verification](.docs/verification/MOB_RESULTS.md) separates measured results from remaining review. Integrate with the shared `WorldClock.IsNight`, item `attackDamage`, `Expedition.TakeDamage` and `Respawned` interfaces instead of duplicating clock or player survival state. Broader ecology, raids, mob loot and durable mob saves remain future scope.

The user subsequently authorized [world fluids](.docs/FLUIDS.md): a sea biome, rivers, buckets and Minecraft-style source/flow physics. Two-source renewal is explicitly a per-fluid boolean (`RenewsSources`), enabled for water and independently configurable for future liquids such as lava. Preserve ore bands, bedrock, dry spawn and existing farming hooks. Actual lava, mixing reactions, irrigation, industrial fluids and durable saves remain later scope.

## Licensing and ownership

- Rivet Reach is **proprietary, all rights reserved, and not open source**. Read `LICENSE.md` before introducing code, assets, dependencies or contribution workflows.
- Do not add, replace or suggest an open-source license for the Rivet Reach repository unless the user explicitly instructs it after being made aware that doing so grants reuse rights.
- Do not copy source code, documentation, artwork, models, textures, sounds or other protected material from third-party projects merely because it is publicly visible.
- Any dependency or third-party asset introduced later must have a license compatible with commercial distribution of a proprietary game, and its license/attribution requirements must be recorded.
- Strong-copyleft or otherwise source-disclosure-triggering dependencies must not be introduced without an explicit licensing review and user approval.
- External contributions must not be incorporated unless their ownership/licensing is clear and compatible with `LICENSE.md`; a separate contributor agreement may be required before accepting outside code or assets.
- Preserve copyright and proprietary notices. The copyright holder is currently identified as `Starbugstone`; update the notice deliberately if ownership is later assigned to a legal person/company.

## Documentation workflow

- Keep project design documents and specifications in `.docs/` (plural). The user explicitly confirmed this spelling. Preserve it when integrating other agents' or remote changes; only a new explicit user instruction changes this convention.
- Keep `README.md`, `LICENSE.md` and this `AGENTS.md` at the repository root. Use the standard uppercase `AGENTS.md` filename for agent instructions.
- Read `README.md`, `.docs/PROJECT_PLAN.md`, `.docs/DEVELOPMENT_STRATEGY.md` and the relevant specialist documents before changing the design.
- Preserve the full-game vision while building toward it through small playable increments.
- Do not treat gameplay responsiveness, scalability and final architecture as competing goals; the project explicitly requires all three to evolve together.
- Distinguish agreed direction, working decisions selected under an explicit user request to resolve designs, proposals to validate, and unresolved questions. Record delegated solutions as working specifications with rationale and remaining evidence; do not claim that each numerical default was explicitly chosen by the user or validated by tests.
- Make useful documentation changes autonomously within the user's request. Record nonblocking design questions in `.docs/DESIGN_QUESTIONS.md`; ask directly only when an answer is needed to proceed.
- Keep each detailed rule in its authoritative document; summaries elsewhere should link to it. Update affected summaries when a rule changes.
- Keep links relative and verify them after moving or adding documents.
- Always commit and push completed project modifications to the current branch's remote after appropriate checks, unless the user explicitly requests otherwise. This is standing authorization; do not ask for confirmation again. If the push fails, report the blocker and preserve the local work; do not force-push to bypass remote changes.
- Do not claim performance, gameplay quality or implementation correctness has been proven without measurements or playtests.
- Maintain the latest useful documentation and evidence for each feature. Remove superseded screenshots/reports and update their incoming links; use Git history for prior versions. Preserve dated artifact identities and remaining limits instead of implying older focused checks ran on a new build.

## Document ownership

- `UNITY_SETUP.md`: pinned Editor/packages, project location, startup and initialization evidence.

- `PROJECT_PLAN.md`: vision, architecture overview, scope and milestones.
- `DEVELOPMENT_STRATEGY.md`: implementation philosophy, playable incremental development and the relationship between responsiveness, performance and final architecture.
- `SIMULATION.md`: proposed timing, network boundaries, persistence and technical validation contracts.
- `GAMEPLAY.md`: player experience, responsiveness, progression and full-game completeness.
- `TRANSPORT.md`: Gate/rocket/teleporter behaviour, traversal and transport exceptions.
- `LORE.md`: hidden history, environmental storytelling and ecology.
- `CRAFTING.md`: modular recipe authoring, compiled registry, transaction boundaries and crafting verification workflow.
- `ECONOMY.md`: resource conservation, finite extraction, bootstrap recipes and progression demand.
- `CONTENT_PIPELINE.md`: asset creation/import conventions and reproducibility.
- `DELIVERY.md`: complete-game scope, verification, production ownership and external dependencies.
- `DESIGN_QUESTIONS.md`: working decision register, unresolved choices and required evidence.

## Working principles

Preserve other agents’ work, establish current ownership before overlapping edits, and stage only owned changes. Closed historical build/index reservations do not apply to new tasks. Current feature APIs and verification belong in their specialist documents; superseded coordination logs remain in Git history.

Protect the exploration/automation balance, capability-based progression, world-aware state, logical network simulation and persistent saves. Challenge contradictions with concrete scenarios.

Build the final architecture incrementally rather than making disposable foundational prototypes. Each major implementation stage should produce something directly playable while exercising the real data model and simulation approach intended for the final game. Performance work should enable the intended scope through better representation, sleeping systems, scheduling and measured optimization before reducing core gameplay goals.

Prefer small experiments that test risky assumptions through actual gameplay where possible. Documentation work requires review and link checks, not game tests or implementation scaffolding.

## Unity and Blender access

When a task needs Unity or Blender, use the `unity-blender-local` skill. Its versioned source is [SKILL.md](.docs/skills/unity-blender-local/SKILL.md); read this copy directly if the skill is not in the available-skills catalog. A local copy is installed at `~/.codex/skills/unity-blender-local/SKILL.md` on the current workstation. Keep both copies synchronized when updating this skill here.

Start the required application when needed without asking the user to launch it. Reuse appropriate running instances and preserve unsaved work. Verify the target project/file and distinguish executable access from a working live integration. Startup authorization does not itself authorize game scaffolding, engine upgrades or integration installation. The skill contains known installation paths, version checks and recovery guidance; revalidate machine-specific facts before use.

For actual Blender model authoring and visual-quality revisions, also use [blender-game-art](.docs/skills/blender-game-art/SKILL.md), installed locally under `~/.codex/skills/blender-game-art/`. Compare actual Blender renders and Unity imports with the approved concept before delivery. Keep its installed and versioned copies synchronized.

Current parallel ownership: the user assigned actual player modeling and animation to another agent. See [PLAYER_ASSET_HANDOFF.md](.docs/PLAYER_ASSET_HANDOFF.md) before changing character sources/exports or animation; preserve the terrain/placement and portrait-rendering changes during integration.
