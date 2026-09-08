# Rivet Reach working instructions

## Current project phase

The project is in early development. The user authorized and the repository now implements the first terrain/FPS/inventory POC in Unity 6000.4.4f1 / URP. [FIRST_POC.md](.docs/FIRST_POC.md) owns its run/build instructions and implementation record; [verification results](.docs/verification/VISUAL_REVISION_RESULTS.md) distinguish measured evidence from remaining review. Keep work scoped to the current request. This is not authorization to implement the full roadmap, upgrade the Editor or install live integrations. Do not describe planned full-game systems as implemented.

The user has locked the first playable milestone in documentation: terrain-only world generation and chunk streaming, FPS movement, a 3D player, fist mining, terrain-block placement, functional inventory and a nonfunctional crafting placeholder inside inventory. The user subsequently extended this slice with generated log/leaf trees, axe-only upward log felling, and selectable starter axe/pickaxe/dagger items; [GAMEPLAY.md](.docs/GAMEPLAY.md#trees-and-axe-felling--user-feedback-extension) owns the rules. The user subsequently authorized modular functional crafting: activate the personal 2×2 grid and share the recipe engine with future 3×3/4×4 interfaces. [CRAFTING.md](.docs/CRAFTING.md) owns implementation/authoring; [GAMEPLAY.md section 16](.docs/GAMEPLAY.md#16-modular-grid-crafting) supersedes the earlier crafting-placeholder restriction. No generated structures. [DEVELOPMENT_STRATEGY.md](.docs/DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence) owns its detailed scope and review gate. The user subsequently authorized implementation of this first POC. Review this slice with the user before selecting subsequent work; the later roadmap groups are candidates, not an automatic queue.

Player appearance must offer male and female model choices, both supporting changeable skins. Keep appearance selection separate from gameplay dimensions/capabilities and verify actual model/rendering cost before claiming performance. Treat the illustrated character as one proposed appearance, not a fixed identity. [GAMEPLAY.md](.docs/GAMEPLAY.md#15-player-skins) owns skin behaviour and the working first-step minimum; [CONTENT_PIPELINE.md](.docs/CONTENT_PIPELINE.md#6-player-skin-authoring-contract) owns model/texture constraints. The first slice includes terrain-block placement (added by the user’s POC feedback), both model variants and two local test skins; further customization remains scoped by later requests.

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

Protect the exploration/automation balance, capability-based progression, world-aware state, logical network simulation and persistent saves. Challenge contradictions with concrete scenarios.

Build the final architecture incrementally rather than making disposable foundational prototypes. Each major implementation stage should produce something directly playable while exercising the real data model and simulation approach intended for the final game. Performance work should enable the intended scope through better representation, sleeping systems, scheduling and measured optimization before reducing core gameplay goals.

Prefer small experiments that test risky assumptions through actual gameplay where possible. Documentation work requires review and link checks, not game tests or implementation scaffolding.

## Unity and Blender access

When a task needs Unity or Blender, use the `unity-blender-local` skill. Its versioned source is [SKILL.md](.docs/skills/unity-blender-local/SKILL.md); read this copy directly if the skill is not in the available-skills catalog. A local copy is installed at `~/.codex/skills/unity-blender-local/SKILL.md` on the current workstation. Keep both copies synchronized when updating this skill here.

Start the required application when needed without asking the user to launch it. Reuse appropriate running instances and preserve unsaved work. Verify the target project/file and distinguish executable access from a working live integration. Startup authorization does not itself authorize game scaffolding, engine upgrades or integration installation. The skill contains known installation paths, version checks and recovery guidance; revalidate machine-specific facts before use.

For actual Blender model authoring and visual-quality revisions, also use [blender-game-art](.docs/skills/blender-game-art/SKILL.md), installed locally under `~/.codex/skills/blender-game-art/`. Compare actual Blender renders and Unity imports with the approved concept before delivery. Keep its installed and versioned copies synchronized.

Current parallel ownership: the user assigned actual player modeling and animation to another agent. See [PLAYER_ASSET_HANDOFF.md](.docs/PLAYER_ASSET_HANDOFF.md) before changing character sources/exports or animation; preserve the terrain/placement and portrait-rendering changes during integration.
