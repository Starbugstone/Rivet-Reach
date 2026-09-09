# Rivet Reach - Development Strategy

> **Status:** agreed development philosophy for the POC and early implementation
>
> Rivet Reach must be built toward the complete game from the beginning, while remaining playable, responsive and measurable at every stage. We should neither build a disposable prototype that ignores the final architecture nor spend months implementing speculative late-game infrastructure before the core game is enjoyable.

Related documents:

- [PROJECT_PLAN.md](PROJECT_PLAN.md) - complete project vision, architecture and feature scope.
- [GAMEPLAY.md](GAMEPLAY.md) - player experience and responsiveness.
- [SIMULATION.md](SIMULATION.md) - simulation correctness, persistence and performance contracts.
- [TRANSPORT.md](TRANSPORT.md) - world travel, Gates, rockets and transport architecture.
- [DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md) - working decisions and remaining validation.
- [ECONOMY.md](ECONOMY.md) - first real resource/recipe chain and finite extraction.
- [DELIVERY.md](DELIVERY.md) - release scope and verification.
- [CONTENT_PIPELINE.md](CONTENT_PIPELINE.md) - asset conventions and import checks.

---

## 1. Core principle

The development rule is:

> **Build the final architecture incrementally, one playable brick at a time.**

Every major system should satisfy three goals at once:

1. **Useful now** - it contributes to an actual playable loop.
2. **Compatible with the final game** - it does not deliberately create an architectural dead end that must later be replaced.
3. **Designed to scale** - its data model and simulation approach account for the final game's expected world size, automation and multiplayer direction even if the first version contains very little content.

The POC is therefore not a disposable prototype. It is the first thin slice of the real game.

---

## 2. Gameplay and architecture are not competing priorities

Responsiveness must not be achieved by cutting out the systems that define Rivet Reach.

Likewise, architectural purity must not be used as an excuse to postpone playable interaction indefinitely.

Bad approach:

```text
Make a fast finite test map
-> fake inventory
-> hard-coded machine
-> later rewrite everything for infinite worlds/multiplayer
```

Also bad:

```text
Build complete persistence/networking/background simulation
-> no satisfying movement/mining/crafting for months
```

Preferred approach:

```text
real chunk system
+ simple terrain
+ responsive mining

real item registry
+ a few items
+ usable inventory

real machine architecture
+ one or two machines
+ immediate feedback

real logical networks
+ tiny automation chain
+ measurable scale behaviour
```

The content starts small. The architecture points toward the final game.

---

## 3. Performance is an enabling constraint

Optimization exists to make the intended game possible, not to redefine the intended game into something cheaper.

We should not respond to an expensive required feature by removing the goal without first designing a scalable representation.

Examples:

### Infinite worlds

Do not replace the requirement with a small finite map.

Instead use:

- deterministic generation;
- chunk streaming;
- compact voxel data;
- asynchronous generation/meshing;
- floating-origin rendering;
- persistence of modified state only where practical.

### Large factories

Do not solve performance by allowing only a handful of machines.

Instead use:

- logical networks rather than per-pipe/per-wire updates;
- sleeping inactive systems;
- event-driven signals;
- background simulation levels;
- bounded work queues;
- batching only where equivalent to the reference simulation.

### Multiple worlds

Do not remove planets or realms because several active Unity scenes would be expensive.

Instead ensure:

- worlds are independent simulation spaces;
- only required worlds/chunks are resident;
- dormant worlds cost almost no continuous CPU;
- cross-world state is lightweight and persistent.

### Multiplayer

Do not implement multiplayer immediately, but avoid local-only authority assumptions that force gameplay code to be rewritten later.

Single-player should already pass meaningful state changes through authoritative simulation commands/events where practical.

---

## 4. Responsiveness is part of the architecture

A responsive game does not require every expensive operation to complete synchronously.

Example block break flow:

```text
input
-> immediate local feedback
-> authoritative world mutation
-> mark affected chunks dirty
-> schedule mesh/light/save work
-> presentation updates as work completes
```

The player should receive clear acknowledgement immediately while expensive secondary work is queued safely.

The same principle applies to:

- inventory operations;
- machine controls;
- pipe/network edits;
- crafting;
- terrain streaming;
- world travel preparation;
- saving.

Expensive background work must not make ordinary interaction feel lost or sticky.

---

## 5. Build real systems with minimal content

### World

Build the actual world-aware chunk architecture from the first implementation.

Initial content can be only:

- simple terrain;
- basic caves;
- a few block types;
- a small number of resources.

Do not create a separate `POCWorld` architecture that cannot support multiple world definitions later.

### Items and inventory

Build stable item identities, stacks and inventories from the beginning.

Start with a very small item list.

The inventory UX should already support fast operations such as stack movement, splitting and quick transfer as they become available.

### Crafting

The user has now explicitly selected modular functional crafting. Use the real data-driven recipe registry from its first implementation, shared by 2×2/3×3/4×4 grids. The personal 2×2 interface replaces the original locked step’s placeholder; see [CRAFTING.md](CRAFTING.md) and [the gameplay extension](GAMEPLAY.md#16-modular-grid-crafting).

Begin with only enough recipes to create the first playable loop.

The recipe/uses browser should consume the same registry rather than maintaining a separate hard-coded list.

### Machines

Build reusable machine/block-entity capabilities rather than a special-case crusher script.

The first machine may use only:

- one input;
- one output;
- one processing recipe;
- one power requirement.

But it should already fit the common machine model and visual state language.

### Logistics and control

Build the real logical-network abstraction with minimal capabilities.

Initial examples:

- item pipe: simple source to destination;
- power: generator to one consumer;
- signal: switch to lamp/machine;
- fluid: source/tank to one consumer.

Routing, priorities, filters, voltage tiers and complex logic are added later without replacing the core network model.

### Persistence

Saving should be introduced early enough that foundational data models do not become transient-only assumptions.

It does not need every future migration/recovery feature before mining feels good, but world, inventory and machine identities should be designed so durable storage is possible without redesigning them.

---

## 6. Locked first step and provisional later sequence

This document owns milestone scope and sequencing, mirrored by PROJECT_PLAN.md. The user's 2026-09-08 staging instruction supersedes the earlier automatic Stage 0-5 progression: **only the first step below is locked. Review its playable result with the user before choosing the next implementation.** The later stages remain candidate groupings of the full-game vision, not an approved queue. The user subsequently authorized implementation of this locked slice; [FIRST_POC.md](FIRST_POC.md) records the delivered review candidate.

### Stage 0 - First playable terrain, FPS and visual concept (locked scope)

Goal: **play in a generated terrain world, move as a 3D first-person player, mine with fists, collect/manage the results and place collected terrain blocks while establishing the visual concept and reliable chunk streaming.**

User-required scope:

- Natural voxel terrain generation only: no generated structures, villages, ruins, Gates or buildings.
- Real chunk generation, meshing, loading and unloading around the player, rather than a finite demonstration map.
- Playable first-person movement and camera controls.
- Selectable male and female 3D player models, both supporting changeable skins, visible first-person fists and enough presentation to review movement and mining (subsequent explicit user appearance decisions).
- Fist mining with targeting, progress and clear feedback.
- Terrain-block placement from the selected hotbar stack, with an aimed-block outline, face-adjacent placement, collision validation and conserved inventory (explicit extension from the user’s first-POC feedback).
- Functional inventory/hotbar, using real item identities and quantities collected from mined terrain.
- Functional personal 2×2 crafting, superseding the original nonfunctional placeholder under the explicit crafting extension below. Its shared core supports future 3×3/4×4 station interfaces.
- A coherent initial visual concept for terrain, player, lighting and inventory; review the concept in play.

Working scope boundaries, chosen to keep this step small:

- Use one terrain-only world definition and a small fist-mineable block palette. The original hills/cave palette was subsequently extended by the user with depth-banded ores and then [varied terrain, caves and biomes](TERRAIN_GENERATION.md). The terrain extension preserves the same streaming/edit architecture.
- Preserve the existing physical stack-drop/pickup rules for the mining-to-inventory loop, including compatible merging and sleeping on dry terrain. Water simulation is not required here.
- Establish a reusable player model and basic idle/movement/fist animation. The user subsequently requested changeable player skins: include a reusable skin texture layout and basic local skin selection as the working first-step extension, with matching body/fists. Detailed behaviour is in [GAMEPLAY.md](GAMEPLAY.md#15-player-skins). A third-person gameplay mode, body-shape customization, an in-game skin painter, combat and final animation polish are not required.
- Beyond the explicitly requested starter tools/tree felling extension, broader tool progression, workbench/furnace, survival/death, mobs, water simulation, day/night progression, machinery, logistics and additional worlds remain outside this step.
- Retain terrain edits, inventory and unexpired drops across chunk unloading/reloading within the running session. Durable cross-session saving and recovery remain later candidates; the first build must clearly disclose its session-only state. This is a scoped milestone boundary, not removal of the final persistent-save requirement.
- Use a stable lighting setup for visual review. Final art breadth and a complete voxel-light feature set are not prerequisites for this step.

Architectural requirements still apply: world-aware integer/chunk coordinates, real block/item definitions, authoritative mining, placement and inventory changes, voxel-grid collision, revision-safe asynchronous meshes, no GameObject per voxel, bounded heavy work and floating-origin handling. Build only the portions needed by this step. Interaction detail belongs to [GAMEPLAY.md](GAMEPLAY.md#14-locked-first-step-interaction-contract); streaming/state evidence belongs to [SIMULATION.md](SIMULATION.md#12-first-step-terrain-and-streaming-validation); visual delivery belongs to [CONTENT_PIPELINE.md](CONTENT_PIPELINE.md#2-first-visual-kit).

Implementation checkpoints within this one milestone, each reviewed in the same growing project:

1. **Traverse:** seeded terrain, safe spawn, chunk streaming, FPS controls and an initial 3D player.
2. **Mine, collect and place:** fist targeting/mining, authoritative edits, physical pickups and usable inventory with the subsequently activated personal crafting grid, and placement of collected terrain blocks.
3. **Review and stabilize:** cohesive terrain/player/UI visuals, model and animation review, chunk-boundary/long-traversal checks and a measured standalone build.

These checkpoints do not authorize separate later features. Stage 0 is complete only when the combined playable result meets all of the following:

- The player can start, move, mine, place collected blocks and manage inventory without developer commands; the subsequently requested personal crafting grid executes its authored starter recipes.
- Both male and female player choices are present and visually coherent with terrain and UI; switching between two test skins on either model updates the body and first-person fists without changing movement, collision or mining. Review actual geometry/rendering cost using CONTENT_PIPELINE.md section 7.
- Terrain contains no generated structures; the same seed and generator definition reproduce unedited terrain independently of discovery order.
- Crossing chunk boundaries, digging at seams, outrunning generation and shifting the rendering origin produce no fall-through, stale collision or reappearing mined blocks.
- Leaving and returning preserves session edits and item accounting; streaming releases unnecessary runtime meshes/colliders and does not hide an unlimited resident-world cache.
- A standalone Windows build has a recorded hardware/settings/workload profile and documented remaining visual, control and streaming issues. Numerical performance targets remain provisional until measured.

**Feedback revision, 2026-09-08:** the user rejected the initial player/terrain presentation, requested farther fog and identified missing placement. Placement now belongs to Stage 0; the art remains under review. This does not authorize later systems. Current evidence is recorded in [the visual revision report](verification/VISUAL_REVISION_RESULTS.md).

**Further first-step feedback, 2026-09-08:** the user requested grass-to-dirt drops, light-dependent grass spreading on ticks, a Minecraft-style fist swing and held-item display, and a simpler targeting outline. These narrowly extend this same terrain/interaction slice; detailed rules are in GAMEPLAY.md section 14 and SIMULATION.md section 13.

**Tree feedback extension, 2026-09-08:** the user subsequently requested generated log/leaf trees and axe-only breaking of logs above the cut. Include this in the current terrain slice, with a starter axe available without functional crafting. The user subsequently requested 2.5× faster swing playback and hotbar availability for the existing dagger/pickaxe grip props. [GAMEPLAY.md](GAMEPLAY.md#trees-and-axe-felling--user-feedback-extension) owns the behaviour and working defaults. This does not select the broader tool/recipe progression.

**Crafting extension, 2026-09-08:** the user explicitly requested modular, editable recipes and a fast, reliable shared engine for 2×2, 3×3 and 4×4 grids. Activate the personal inventory grid, implement the shared definition/matching/transaction core and measure it. [CRAFTING.md](CRAFTING.md) owns technical boundaries; [GAMEPLAY.md section 16](GAMEPLAY.md#16-modular-grid-crafting) owns behavior. This authorization does not implement larger station interfaces, furnace/industry, durable saves or the full bootstrap progression.

**Ore extension, 2026-09-08:** the user explicitly selected ore generation as the next step, then required multiple ores at different levels and an unbreakable bedrock base. Implement this bounded terrain/resource extension with pickaxe extraction and session depletion. [ECONOMY.md](ECONOMY.md#current-ore-generation-and-bedrock) owns the working bands and yields. This does not select processing, tool tiers, durable saves or the broader candidate stages.

**Day/night extension, 2026-09-09:** the user requested moving sun/moon and nightly lunar phases. This bounded environment extension is now selected; [GAMEPLAY.md](GAMEPLAY.md#day-night-and-lunar-phases) owns its working behavior. It does not automatically select the remaining candidate sandbox systems.

**Survival extension, 2026-09-09:** the user selected familiar basic recipes, wood/stone/copper/iron/diamond tool tiers, a workbench and furnace, then potatoes, farming, hunger, health and armor. Implement this requested playable progression using the existing authoritative item/grid/world model. [GAMEPLAY.md](GAMEPLAY.md#survival-progression-farming-health-and-armor) owns behavior and [CRAFTING.md](CRAFTING.md#survival-progression-extension--2026-09-09) owns authoring/transactions. Earlier exclusions of these selected features are superseded; this does not automatically select industry, water, durable saves or the rest of the candidate stages.

**Review gate:** present the playable build, visual concept, measurements and known limitations to the user. Decide the next implementation scope from that evidence. The crafting, ore/bedrock and survival extensions are authorized; select further systems explicitly after review rather than automatically executing the old Stage 1 list.

### Candidate Stage 1 - Extend the sandbox (not yet selected)

Goal: **the player can play rather than merely test a voxel engine.**

Add:

- extend the real item/inventory/drop systems delivered in the first step;
- advanced building conveniences and tool progression;
- block-water interaction;
- data-driven grid crafting;
- workbench/furnace;
- save/reload of terrain, inventory and simple block entities;
- simple day/night cycle (subsequently selected separately; see the extension above);
- one passive and one hostile mob when useful for feel testing.

Acceptance question:

> Can a player spend 20-30 minutes gathering, building and crafting without developer commands?

### Candidate Stage 2 - Industrial hook (not yet selected)

Goal: **prove that Rivet Reach's automation is satisfying, not only technically possible.**

Add:

- common machine architecture;
- starter boiler-engine and alternator with manual water/fuel bootstrap;
- powered crusher, pump and finite-terrain drill;
- minimal tank/fluid pipes using the final quantity/type model;
- containers;
- item pipes using a logical network;
- power cables/network;
- switch + signal connection;
- machine status feedback;
- recipe/uses browser;
- cosmetic flow feedback separated from authoritative simulation.

Target playable chain:

```text
manual gathering / smelting / components
-> boiler-engine + alternator
-> crusher + storage + pipes + signal control
-> pump sustains water
-> drill automates finite extraction
-> more useful construction / next deposit
```

Acceptance question:

> Is building and diagnosing the first automated process immediately understandable and satisfying?

### Candidate Stage 3 - Lifecycle, scale and background simulation (not yet selected)

Goal: **prove the playable systems can survive the scale implied by the final game.**

Add/validate:

- active/background/dormant chunk states;
- generic ticket model;
- owner-online factory loaders and per-player quotas;
- cross-chunk logical networks;
- deterministic simulation ordering;
- resource accounting/reservations;
- background equivalence tests;
- safe batching where proven equivalent;
- asynchronous coherent saves;
- topology-edit stress tests;
- realistic workload benchmarks.

This is where [SIMULATION.md](SIMULATION.md) becomes heavily exercised.

Do not invent a separate fast background machine implementation with different game rules. Optimize the same authoritative system.

### Candidate Stage 4 - Multi-world skeleton (not yet selected)

Goal: **prove the existing game can exist in more than one world without redesign.**

Add:

- second lightweight `WorldDefinition`;
- world-aware persistence and player/entity state;
- deterministic cross-world anchors;
- safe destination preparation;
- transfer of the same player/entity identity;
- temporary system chunk tickets;
- unload/reload of inactive worlds;
- transfer recovery tests.

This remains an architecture test, not finished Gate or rocket content.

### Candidate Stage 5 - Expand the real game (not yet selected)

Only once the core interaction and industrial loop are both enjoyable and structurally sound should content expand substantially:

- richer terrain/biomes;
- villages/structures;
- broader mobs/ecology;
- technology ages;
- steampunk machinery;
- richer signals/logistics;
- Gatebuilder structures and activation;
- portal realms;
- rockets and planets;
- cargo automation;
- later teleporters;
- multiplayer implementation.

These are expansions of existing foundations, not reasons to replace them.

---

## 7. Rules for every new feature

Before adding a system, answer:

### 1. What does the player gain?

The feature should create a useful action, decision, convenience, challenge or discovery.

### 2. Which final-game requirement does it serve?

Avoid spending significant effort on infrastructure that has no clear connection to the game's intended experience.

### 3. What is the smallest real version?

Implement the minimum version that uses the intended architecture rather than a mock that will certainly be thrown away.

### 4. How does it scale?

Ask what happens at:

- 1 instance;
- 100 instances;
- 10,000 instances;
- multiple chunks/worlds;
- multiplayer authority later.

The feature does not need to support absurd loads immediately, but its representation should not make the required future scale obviously impossible.

### 5. What can sleep?

Identify which state must remain persistent and which runtime work can stop when:

- nobody is nearby;
- the owning player is offline;
- a world is inactive;
- nothing is changing.

### 6. How does the player know what happened?

Define feedback for:

- success;
- invalid action;
- missing resource;
- blocked output;
- unavailable power;
- loading/waiting;
- failure.

Performance optimizations must not create silent or confusing behaviour.

---

## 8. Performance validation philosophy

Do not optimize by intuition alone and do not declare architecture scalable because it looks efficient on paper.

For important systems:

1. implement the simplest correct version;
2. create a representative workload;
3. profile it;
4. identify actual bottlenecks;
5. optimize without changing intended gameplay semantics;
6. compare results against the correct reference behaviour;
7. repeat as scale grows.

Performance tests should include **gameplay workloads**, not only synthetic maximum counts.

Examples:

- moving through terrain while chunks stream;
- mining while nearby chunks remesh;
- inventory/crafting while saving occurs;
- editing a factory while production runs;
- splitting a large pipe network;
- moving far enough to trigger background simulation;
- returning to a running factory;
- later, cross-world travel while other factories operate.

Frame time, responsiveness, simulation correctness, queue age, memory and save behaviour all matter.

---

## 9. Refactoring policy

Building toward the final architecture does **not** mean refusing to refactor.

Refactoring is expected as measurements and playtests expose better designs.

The distinction is:

- acceptable: improve or replace an implementation because evidence shows a better way to satisfy the same architecture/game requirement;
- avoid: deliberately build a known-dead-end foundational model because it is faster for one demo.

No first implementation is sacred.

The final goal is the constraint; individual classes and algorithms are not.

---

## 10. Relationship to the POC

The broader POC ambition must prove both **gameplay value** and **technical viability**. The following describes cumulative evidence across future milestones; it is not the acceptance bar for the locked first step in section 6. That step proves terrain/FPS/inventory/visual foundations and can finish before functional crafting or automation exists.

A technically correct voxel engine with no satisfying game loop is not a successful POC.

A fun automation demo built on an architecture that cannot support infinite worlds, background factories or later multiplayer is also not a successful POC.

Success requires both tracks:

```text
PLAYABLE
responsive movement
mining/building
inventory/crafting
first automation loop
clear feedback

        +

SCALABLE FOUNDATION
chunked deterministic worlds
world-aware state
logical networks
persistence
simulation levels
performance measurements
multiplayer-compatible authority direction
```

Neither side replaces the other.

---

## 11. Current development priority

The first milestone's scope is locked in section 6. Its initial playable implementation is recorded in [FIRST_POC.md](FIRST_POC.md), with measured results and remaining review work. The user subsequently selected the bounded ore/bedrock extension. Broader milestone selection and remaining visual/feel acceptance remain pending.

The current priority is:

> **Review the terrain/FPS/inventory/player slice and its crafting and ore/bedrock extensions, then select the next playable increment with the user.**

When two approaches both meet the final requirements, prefer the one that can be tested through actual gameplay sooner.

When a gameplay requirement and a performance problem conflict, first look for a better representation, simulation level or scheduling strategy before reducing the intended game scope.


## Authorized mob extension — 2026-09-09

The user selected mob implementation, Blender-authored enemies, AI and spawning, then chose Rustback beetle and Dusk prowler. [MOBS.md](MOBS.md) owns this bounded playable increment. It integrates the concurrently selected day/night and survival work without selecting factory raids, generated structures, the broader creature catalogue or cross-session mob persistence. Earlier exclusions of mobs/combat describe the original first-step boundary and are superseded only for this explicit extension.

### Terrain and biome extension — 2026-09-09

The user selected a terrain-generation rework with caves, varied relief and biomes. [TERRAIN_GENERATION.md](TERRAIN_GENERATION.md) owns this bounded working profile and its review questions. It preserves finite ore bands, bedrock and session edits, and supplies the coordinated wild-food generation hook to survival. Structures, water and additional worlds are not selected by this terrain request. [Verification](verification/TERRAIN_GENERATION_RESULTS.md) records measured evidence separately from user play review.
