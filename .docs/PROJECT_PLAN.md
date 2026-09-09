# Rivet Reach - Project Plan and Long-Term Vision

> **Status:** first terrain/FPS/inventory POC implemented for review; later milestones remain planned
>
> Rivet Reach is intentionally **evolutive**. The design will continue to change while we brainstorm, prototype, benchmark and test the POC. The current documents describe the agreed direction, not an immutable final specification.
>
> The first objective is a responsive playable slice using the real architecture. Performance, scalability, deterministic world generation and multi-world readiness are first-class constraints from the beginning rather than cleanup work for later.

Related design documents:

- [LORE.md](LORE.md) - hidden world history, Gatebuilders, environmental storytelling and current mob/ecology direction.
- [TRANSPORT.md](TRANSPORT.md) - Gate networks, coordinate-preserving world travel, rockets, portal state, chunk wake-up, satellites and future teleporters.

- [DEVELOPMENT_STRATEGY.md](DEVELOPMENT_STRATEGY.md) - authoritative playable implementation stage order.
- [ECONOMY.md](ECONOMY.md) - finite extraction, recipe bootstrap and technological demand.
- [CONTENT_PIPELINE.md](CONTENT_PIPELINE.md) - Blender/Unity asset conventions and validation.
- [DELIVERY.md](DELIVERY.md) - staged release scope, verification and external planning inputs.
- [SIMULATION.md](SIMULATION.md) - simulation timing, network boundaries, persistence and benchmark contracts.
- [GAMEPLAY.md](GAMEPLAY.md) - player experience, responsiveness and full-game completeness.
- [DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md) - prioritized unresolved decisions and validation needs.

### How to read this plan

The existing vision and explicitly agreed rules remain the design baseline. The 2026-09-08 resolution pass adds **working decisions** selected under the user's request to solve conflicts; they are current specifications, not validated results. Specialist documents distinguish these from unresolved proposals and numerical tuning. Unchecked milestones are future work, not implemented capabilities. The user subsequently authorized initialization and then implementation of the locked first POC. [FIRST_POC.md](FIRST_POC.md) records the resulting playable slice and links its verification; later milestones still require a scope decision.

Detailed simulation behaviour belongs to `SIMULATION.md`, transport behaviour to `TRANSPORT.md`, player-experience criteria to `GAMEPLAY.md`, and hidden history to `LORE.md`. Economy, content workflow and delivery details belong to their named specialist files. Stage order belongs only to `DEVELOPMENT_STRATEGY.md`. Summaries here do not override those documents. Conflicts should be resolved explicitly and recorded, not silently interpreted as approval.

---

## 1. Core vision

Rivet Reach is a **first-person voxel sandbox** built around two equally important gameplay pillars:

### Discovery and exploration

- effectively infinite procedural worlds;
- block-based terrain, caves, rivers, biomes and generated structures;
- villages, ruins, mines, temples and ancient sites;
- day/night cycle and mobs;
- resource discovery as a major driver of progress;
- additional physical planets reached by rockets;
- ancient ruined Gatebuilder gateways leading to strange portal realms;
- environmental storytelling rather than a heavily scripted narrative.

### Engineering and automation

- Minecraft-style mining, placement, hotbar, inventory and grid crafting;
- much more generous inventory and stack limits;
- machines and processing chains;
- item pipes rather than conveyor belts as the primary item-logistics system;
- fluid pipes;
- electrical power;
- Redstone-inspired signal/control networks;
- automated factories;
- chunk loading/background simulation;
- automated interplanetary cargo;
- late-game teleportation between normal physical worlds.

The game should begin with the familiarity of a voxel crafting sandbox and gradually allow players to create large Factorio-like industrial systems without losing the importance of exploration.

Rivet Reach should be inspired by Minecraft, classic technology mods and Factorio without directly copying Minecraft-specific mobs, structures, textures, names, recipes, dimensions or visual assets. The user subsequently selected familiar basic recipe layouts for the bounded survival increment; code and visual assets remain original, and the broader game vision remains distinct.

---

## 2. Design pillars

### 2.1 Open progression

There should be **no conventional character levels, XP gates or mandatory research-point tree controlling technology**.

Progress should emerge from resources and capability:

```text
find resource
    -> process material
    -> manufacture component
    -> build new machine
    -> access better process/resource
    -> expand capability
```

A knowledgeable player should be able to beeline toward a particular technology if desired. Doing so may be inefficient, risky and resource-intensive, but the game should not block the player with `Requires Level 20` or a similar arbitrary gate.

Technology is primarily soft-gated by:

- resource location;
- tool/material capability;
- manufacturing processes;
- infrastructure;
- machine requirements;
- industrial scale;
- exploration discoveries.

We explicitly do **not** want temperature management or toxic-gas/atmosphere survival systems to become major progression gates.

### 2.2 Discovery remains important

Automation should not eventually remove all reasons to explore.

Physical planets increasingly reward industrialisation and automation.

Portal realms deliberately remain places the player must physically visit.

Some rare materials, structures and discoveries should therefore remain tied to personal exploration rather than unattended remote extraction.

### 2.3 Automation should scale

A player should eventually be able to build large systems such as:

```text
Ore Storage
    |
 Item Pipe
    |
 Crusher
    |
 Item Pipe
    |
 Furnace
    |
 Item Pipe
    |
 Finished Storage
```

with power, fluids and signals controlling the overall factory.

The game must be designed for players who will eventually build absurdly large factories.

### 2.4 Performance before content volume

A core rule:

> If CPU cost scales directly with the total number of blocks in the world, the architecture is probably wrong.

Ten million static stone blocks should be almost free to simulate.

A huge dormant pipe network should be almost free.

CPU should primarily be spent on things that are:

- near players;
- currently active;
- changing;
- being transferred;
- being rendered;
- being explicitly background-simulated.

The target is good performance on reasonable gaming hardware rather than designing for high-end PCs only.

---

## 3. Selected technology stack

### Engine

**Unity 6000.4.4f1 (Unity 6.4)**

Initialization uses the existing installed Editor, superseding the earlier 6.3 LTS target. The exact version is pinned in `ProjectSettings/ProjectVersion.txt`; URP is pinned to 17.4.0. See [UNITY_SETUP.md](UNITY_SETUP.md). No Editor update was required to initialize this project.

### Language

**C#**

### Rendering

**Universal Render Pipeline (URP)**

The game keeps the block aesthetic but improves presentation through features such as:

- higher-quality textures;
- material variation;
- PBR/normal information where useful;
- ambient occlusion;
- improved sunlight and shadows;
- atmospheric fog;
- better water;
- particles;
- animated vegetation;
- more detailed machine models;
- more detailed mob models;
- dynamic lighting where performance allows it.

The user subsequently authorized the current slice’s [arcade visual and dynamic-effects treatment](CONTENT_PIPELINE.md#arcade-presentation-and-dynamic-feedback). Its presentation evolves alongside the existing world/simulation architecture and measured runtime checks.

### Performance tooling

Use Unity's data-oriented tools selectively:

- Jobs System;
- Burst Compiler;
- `NativeArray` and native collections;
- background jobs for generation/meshing;
- ECS/Entities only where profiling proves it useful.

Likely Jobs/Burst candidates:

- terrain generation;
- chunk meshing;
- lighting calculations;
- noise generation;
- bulk voxel operations;
- portions of pathfinding;
- network topology work if profiling justifies it.

Do **not** force the entire game into ECS merely because it exists.

### Commercial licensing assumption

Unity Personal currently permits commercial use below Unity's applicable financial threshold, the old Runtime Fee has been cancelled, and Unity Pro is required above the Personal eligibility threshold. Licensing must be rechecked before release/funding because commercial terms can change.

---

## 4. Simulation architecture

Separate simulation from presentation as much as practical.

```text
                    Rivet Reach
                        |
        +---------------+----------------+
        |                                |
     Client/View                  Simulation Core
 Unity rendering/UI            Pure C# where practical
        |                                |
 rendering/audio              world/entities/networks
                                         |
                           +-------------+-------------+
                           |             |             |
                        signals        power        logistics
                                         |
                                      machines
```

Benefits:

- deterministic testing;
- reliable saves;
- background simulation;
- easier dedicated-server support;
- multiplayer authority;
- simulations that can run without rendering.

Prefer command/event flow:

```text
player action
    -> command
    -> authoritative simulation validation
    -> world change
    -> event/delta
    -> presentation update
```

Single-player should use the same broad simulation path so multiplayer does not require rewriting gameplay logic.

---

## 5. Save and universe model

Each save is a **completely separate universe**.

There are no interactions between saves:

- no shared inventory;
- no shared discoveries;
- no shared machines;
- no shared resources;
- no cross-save portals;
- no cross-save teleporters;
- no cross-save logistics.

Conceptually:

```text
SAVE
|
+-- player state
+-- discovery / recipe knowledge
+-- universe state
|
+-- worlds
    |
    +-- physical worlds
    |   +-- starting planet
    |   +-- moons
    |   +-- other planets
    |
    +-- portal realms
        +-- realm A
        +-- realm B
        +-- realm C
```

Terminology:

- **Save** - one isolated game/universe.
- **Universe** - all worlds in that save.
- **World** - one voxel simulation space.
- **Physical world/planet** - normal celestial world in the system.
- **Portal realm** - strange world accessed through ancient Gatebuilder gateways.

The POC may initially instantiate only one world, but the APIs and persistence model must never assume only one world can exist.

---

## 6. Multi-world coordinate model

World identity must be part of location/state APIs from the beginning.

Conceptually:

```text
WorldId
ChunkX / ChunkY / ChunkZ
LocalBlockX / LocalBlockY / LocalBlockZ
```

Absolute world position should not rely on Unity floating-point coordinates at extreme distances.

Use integer/chunk coordinates for authoritative locations plus a **floating origin** for rendered local space.

A major transport rule is now established:

> Early/intermediate travel between worlds preserves horizontal X/Z position.

Detailed rules are defined in [TRANSPORT.md](TRANSPORT.md).

---

## 7. WorldDefinition architecture

World generation must be modular/configuration driven from the start.

Avoid building one hard-coded `GenerateOverworld()` and trying to bolt moons/alien worlds onto it later.

A conceptual definition can expose:

```text
WorldDefinition
|- world id/type
|- seed
|- terrain profile
|- biome profile
|- cave profile
|- ore profile
|- structure profile
|- gravity
|- sky/lighting profile
|- water profile
|- mob spawn rules
|- rocket rules
|- teleporter rules
|- Gate-network rules
|- chunk-loader rules
|- automation rules
|- generation modifiers
```

Examples:

### Starting planet

```text
continental terrain
normal biome system
rivers/oceans/lakes
villages and normal ruins
Gatebuilder ruins and Gate networks
normal automation: yes
rockets: yes
chunk loading: yes
late teleporters: yes
```

### Other physical planet

```text
planet-specific terrain
planet-specific resources
planet-specific structures
Gatebuilder presence according to history
normal automation: yes
rockets: yes
chunk loading: yes
late teleporters: yes
```

### Portal realm

```text
alien terrain/biomes
unique structures/resources/mobs
Gatebuilder Gate lattice
rockets: no
normal player teleporters: no
remote inter-world resource automation: no
factory chunk loading: disabled; attended local processing only
```

The content remains future work. The ability to express these differences is required from the beginning.

---

## 8. Procedural generation

All generation must be deterministic from the save seed.

A possible normal-world pipeline:

```text
seed
 -> macro world facts / important anchors
 -> large-scale continents/landmass
 -> elevation / ridges / mountains
 -> climate/biome distribution
 -> terrain shaping
 -> rivers/water
 -> caves
 -> ores
 -> vegetation/features
 -> historical/structure placement
```

The current user-authorized ore step adds coal, copper, iron, gold and diamond in distinct depth bands and an unbreakable bedrock floor. [ECONOMY.md](ECONOMY.md#current-ore-generation-and-bedrock) owns their working distribution; [SIMULATION.md](SIMULATION.md#15-ore-generation-and-the-world-base) owns deterministic generation and depletion. Processing and tool tiers remain future work.

The final algorithms must be benchmarked during the POC rather than chosen purely theoretically.

### History-aware structures

World structures belong to different historical layers rather than one random pool:

- current villages/farms/workshops/mines;
- abandoned/recent ruins;
- ancient Gatebuilder ruins;
- Gatebuilder gateway complexes;
- world-specific special structures.

See [LORE.md](LORE.md).

### Important deterministic anchors

Critical structures such as Gatebuilder Gate sites must not be created by a simple random roll when a chunk happens to generate.

Their locations are determined mathematically from the universe seed so corresponding structures can exist in linked dimensions.

See [TRANSPORT.md](TRANSPORT.md).

---

## 9. Voxel/chunk architecture

### Blocks are compact data

Do **not** create one Unity GameObject per ordinary block.

Conceptual starting representation:

```csharp
struct Block
{
    ushort Type;
    ushort State;
}
```

The format can later use palette compression/bit packing.

### Chunk size

Tentative POC starting point:

```text
32 x 32 x 32 blocks
```

Not final. Benchmark against:

- generation time;
- mesh cost;
- memory;
- remeshing frequency;
- save size;
- future networking;
- visibility/culling.

### Block entities

Only special blocks require richer persistent objects/state, for example:

- chests;
- furnaces;
- generators;
- batteries;
- machines;
- sensors;
- pumps;
- controllers;
- Gate control structures when locally loaded.

Millions of stone/dirt blocks do not.

---

## 10. Chunk rendering

Each chunk generates combined geometry rather than rendering individual cubes.

```text
voxel data
 -> visible-face calculation
 -> greedy/optimized meshing
 -> mesh buffers
 -> Unity renderer
```

Support distinct rendering paths/material groups for at least:

- opaque geometry;
- cutout geometry;
- transparent geometry;
- fluids where necessary.

Chunk meshing should move off the main thread where practical with Jobs/Burst.

Only affected chunks/neighbours should remesh after block changes.

---

## 11. FPS interaction

Rivet Reach is designed first and foremost as an **FPS**.

POC controls:

- mouse look;
- walk;
- sprint;
- jump;
- block targeting;
- mine/break;
- place;
- interact;
- hotbar selection.

Third person is not a POC priority.

Machines and structures should communicate through physical models, ports, animation, indicator lights and gauges where practical rather than reducing everything to abstract menus.

---

## 12. Inventory and hotbar

Retain the familiar Minecraft-like interaction model but make it significantly more permissive for an industrial game.

Working initial values (tunable; detailed interactions in [GAMEPLAY.md](GAMEPLAY.md)):

- **12 hotbar slots**;
- **6 x 8 main inventory = 48 slots**;
- approximately **60 immediately accessible slots** total.

These values are not final.

Earlier industrial stack proposal (unimplemented; the selected survival content uses 64 for ordinary items and one for equipment):

```text
building blocks: 500
basic resources: 250
components: 100
machines: 10
unique tools/equipment: 1
```

Inventory inconvenience should not be a dominant gameplay loop.

Storage/logistics matter because factories create volume, not because the character can carry almost nothing.

---

## 13. Crafting

Keep recognisable grid crafting.

User-authorized grid sizes are 2×2, 3×3 and 4×4. The personal 2×2 grid is now playable; the shared core supports all three sizes. Larger station interfaces and advanced machine manufacturing remain future work. [CRAFTING.md](CRAFTING.md) owns the modular recipe engine and authoring contract; [GAMEPLAY.md section 16](GAMEPLAY.md#16-modular-grid-crafting) owns current interaction behavior.

Recipes should be data driven.

Benefits:

- easier balancing;
- easier recipe browser integration;
- machine recipes use the same registry model;
- easier content expansion;
- possible modding later.

---

## 14. Recipe browser and discovery

A built-in **NEI/JEI-style recipe/uses browser** is a core feature.

Selecting any material/item should answer:

```text
How do I make this?
What can I make with this?
```

Recipes must be navigable as a graph, including machine processes and alternative processing paths.

Example:

```text
IRON ORE

Smelting
-> Iron Ingot

Crusher + smelting
-> higher intermediate yield

Later advanced processing
-> still better yield
```

### Discovery without hard locking

New materials can reveal relevant entries so the recipe interface does not dump thousands of late-game recipes on a new player.

This is a **knowledge/UI discovery system**, not a mandatory research tree.

If an experienced player knows how to obtain/manufacture the required ingredients and equipment, the game should favour allowing the action rather than blocking it because a research meter is incomplete.

---

## 15. Technology ages

Ages describe technological capability but are **not player levels**.

Provisional progression:

```text
Primitive / hand crafting
        |
Early metalworking
        |
Mechanical systems
        |
Steam / steampunk industrial age
        |
Electrical age
        |
Advanced industrial manufacturing
        |
Aerospace
        |
Advanced interplanetary technology
```

The first resource chain and per-age capability roles are specified in [ECONOMY.md](ECONOMY.md). Later recipe balance and final era names remain adjustable.

### Steampunk era

One age should have a strong steampunk visual/engineering identity:

- brass/copper;
- riveted steel;
- boilers;
- pressure gauges;
- steam exhaust;
- flywheels;
- pistons;
- shafts/gears where useful;
- analogue controls;
- steam engines/generators;
- large mechanical machinery.

Moving from steam/mechanical installations to electrical motors and distributed wiring should materially change factory construction rather than simply replacing `Machine Mk1` with `Machine Mk2`.

---

## 16. Universal machine interaction language

All player-built machines should follow a consistent connection/UX grammar.

Functions include:

- item input/output;
- fluid input/output;
- power input/output;
- signal input/output;
- inventories;
- internal buffers;
- process state;
- configurable sides/ports where useful.

### Port communication

Do not rely on colour alone.

Use **shape + symbol + direction + colour**.

Current conceptual grammar:

| Function | Shape idea | Visual cue |
|---|---|---|
| Power | hexagonal | lightning/energy symbol |
| Items | square | box/item symbol |
| Fluid | circular | droplet/pipe symbol |
| Signal | diamond/small control port | pulse/signal symbol |
| Component/module | recessed keyed slot | matching silhouette |
| Input/output | directional marking | inward/outward arrow |
| Stored amount | gauge/bar/dial | universal fill indication |

Exact colours/art are not final.

A cable/pipe should visually connect only to compatible ports.

### Machine state

Machines should visibly communicate broad states such as:

- off;
- ready;
- running;
- fault/blocked.

Steampunk machinery can use physical gauges and motion; later machinery can use more advanced indicators while retaining the same mental model.

Gatebuilder pedestals use related interaction principles but a deliberately alien visual language. See [LORE.md](LORE.md).

---

## 17. Machine architecture

Machines should compose common reusable capabilities rather than each being a large bespoke system.

Conceptually:

```text
MachineBlock
|- inventories
|- item ports
|- fluid ports
|- power ports
|- signal ports
|- recipe processor
|- internal buffers
|- state/configuration
```

Example crusher, illustrative only:

```text
power demand: 250 W
input: ore
output: crushed ore
processing duration: 4 s
```

Exact values are future balance work.

---

## 18. Item logistics

Use **item pipes**, not conveyor belts as the primary logistics system.

Reasons:

- gameplay preference;
- cleaner first-person factory layouts;
- avoids large numbers of physical moving item entities;
- easier to optimise at network level.

### Simulation rule

```text
pipe blocks = topology
connected topology = ItemNetwork
ItemNetwork = transfer state/decisions
```

Do not tick every pipe every frame.

When topology changes, rebuild only the affected network.

Actual item transfer can be abstract quantities between inventories.

Any visible item movement inside transparent pipes should be cosmetic rather than authoritative physics.

Potential later features:

- extraction modules;
- filters;
- priorities;
- round robin;
- throughput tiers;
- channel routing;
- configurable machine faces.

---

## 19. Fluid logistics

Fluid networks are separate from item networks.

Track game-relevant quantities such as:

- type;
- amount;
- capacity;
- input rate;
- output demand;
- throughput.

Do not implement computational fluid dynamics.

Possible fluids later:

- water;
- steam;
- oil;
- fuels;
- coolant;
- industrial liquids.

Different pipe tiers can change throughput/capacity.

---

## 20. Signal/control network

Retain the creative automation role of Redstone using Rivet Reach's own signal/control system.

Signal tells something **what to do**.

Power determines whether it **can do it**.

Possible components:

- switch;
- button;
- pressure plate;
- sensors;
- timer/delay;
- repeater;
- comparator;
- AND/OR/XOR/NOT;
- latch;
- counter;
- relay;
- controller;
- lamp;
- door;
- machine inputs.

Example:

```text
Storage sensor
    |
IF iron > threshold
    |
Stop crusher
```

### Event-driven rule

Do not recalculate every wire every frame.

```text
input changes
 -> signal event
 -> affected network queued
 -> relevant state recalculated
 -> connected devices notified
```

Future signal channels may allow multiple independent circuits through compact conduits.

---

## 21. Electrical power

Electricity is its own simulation network.

The first implementation should remain game-readable rather than becoming an electrical-engineering simulator.

Track concepts such as:

- generation;
- demand;
- stored energy;
- machine priority;
- cable/network capacity;
- later voltage tiers;
- later transformers where useful.

Possible sources across progression:

- mechanical conversion;
- wind/water where appropriate;
- combustion;
- steam;
- solar;
- geothermal;
- nuclear;
- advanced systems.

The working baseline slows processors in proportion to allocated power and stops safely on missing supply; it does not damage machines. Allocation and escrow are specified in [SIMULATION.md](SIMULATION.md). Later voltage tiers require a separate explicit revision.

**Gatebuilder gateways are not part of this conventional electricity system.** Their operation is intentionally different. See [LORE.md](LORE.md) and [TRANSPORT.md](TRANSPORT.md).

---

## 22. Network optimization rule

Item, fluid, power and signal systems follow the same fundamental model:

```text
blocks = topology
network object/state = simulation
```

A line of 500 inactive cables/pipes must not mean 500 `Update()` calls.

Meaningful work happens only for:

- topology changes;
- active transfers;
- changing signals;
- active machines;
- relevant network calculations.

---

## 23. Chunk states

Chunks need more than loaded/unloaded.

Conceptual levels:

```text
DORMANT / UNLOADED
       |
BACKGROUND SIMULATION
       |
ACTIVE PLAYER SIMULATION
```

### Active near-player chunks

Can include:

- rendering;
- mob AI;
- physics;
- local block simulation;
- machines;
- networks;
- particles;
- player interactions.

### Background factory chunks

Can run only what is needed:

- machines;
- item/fluid networks;
- power;
- signals;
- production timers;
- coarse process state.

No unnecessary rendering, distant mob AI or unrelated physics.

### Dormant chunks

Persist state and consume essentially no continuous CPU until something wakes them.

---

## 24. Chunk ticket system

Different reasons can wake/retain chunks.

At minimum:

```text
Player proximity ticket
-> full active simulation

Player-owned factory chunk-loader ticket
-> background factory simulation

Portal activity ticket
-> temporary simulation required around Gate traversal
```

The strongest relevant ticket determines simulation level within world-specific capability restrictions. A portal safety ticket must not implicitly authorize unattended realm production; see [TRANSPORT.md](TRANSPORT.md) and [SIMULATION.md](SIMULATION.md).

### Player-owned chunk loaders

Current preferred multiplayer rule:

> Factory chunk-loader tickets are active **only while their owner is online**.

This prevents one unrelated online player from waking every offline player's factories.

Chunk loading has a **per-player quota**. Exact values will be benchmarked later.

Overlapping tickets are deduplicated; a chunk simulates once no matter how many players have tickets for it.

### Portal tickets

Portal activity tickets are system-owned and temporary. They do not count against player factory quotas.

See [TRANSPORT.md](TRANSPORT.md).

---

## 25. Background factory processing

Where mathematically safe, use coarse elapsed-time calculation rather than thousands of tiny ticks.

Example:

```text
cycle: 10 sec
elapsed background time: 1800 sec
available input: 100
possible by time: 180
possible by input: 100
result: 100 completed cycles
```

Not every network can use this optimization. The arithmetic above illustrates time and input limits only; output capacity, energy, fluids, controls and coupled consumers must also be respected. A correct reference simulation comes first. See [SIMULATION.md](SIMULATION.md) for the selected eligibility-time and equivalence rules. Dormant time is not automatically productive time.

---

## 26. Physical planets

Physical planets are normal parts of the universe and are intended to become **industrial territory**.

Long-term loop:

```text
starting-world industry
 -> aerospace manufacturing
 -> build rocket infrastructure
 -> reach new planet
 -> explore/resources
 -> establish outpost
 -> automate extraction/processing
 -> automate cargo return
 -> eventually replace expensive routes with advanced teleportation
```

The exact planets and resources remain future design work.

The Gatebuilders colonised the planetary system before the player, so additional worlds can contain their ruins and gateway infrastructure. See [LORE.md](LORE.md).

---

## 27. Rockets and cargo

Rockets should be physical projects/machines rather than a menu button.

Potential later requirements:

- launch pad;
- structural materials;
- engines;
- fuel;
- electronics;
- cargo;
- player cabin/transport.

We do not intend full Kerbal-style orbital simulation.

### Coordinate-preserving travel

Launch location X/Z maps approximately to the same X/Z on the target world.

The first landing can select safe terrain within a small constrained radius because no destination pad exists yet.

After arrival, the player can build a receiving pad.

Established pad-to-pad routes support repeat travel and automated cargo.

Cargo rockets eventually enable Factorio-like off-world supply chains.

Detailed behaviour is in [TRANSPORT.md](TRANSPORT.md).

---

## 28. Ancient Gatebuilder gateways

Ancient gateways are a fundamentally different exploration system.

They are:

- naturally generated ruined structures;
- never player-crafted;
- part of sparse world-spanning Gate networks;
- deterministic from the save seed;
- linked by corresponding X/Z anchors in portal realms;
- based on non-conventional Gatebuilder technology rather than ordinary electricity;
- persistent once activated;
- free to traverse once operational;
- able to transfer players/mobs physically;
- not an automated item/fluid logistics conduit.

The player can enter through one Gate, explore the corresponding realm, find another Gate and return through it to the matching distant Gate on the physical world.

Distance is preserved, so the portal realm does not become a compressed fast-travel network.

Gate state, damaged receiving endpoints, control pedestals and chunk wake-up are defined in [TRANSPORT.md](TRANSPORT.md).

Hidden lore is defined in [LORE.md](LORE.md).

---

## 29. Portal realm rules

Portal realms exist to preserve the **discovery** half of the game.

Current rules:

- rockets cannot reach them;
- normal player-built teleporters cannot link to them;
- automated cross-world item/fluid transfer is not allowed;
- factory chunk loaders and automatic extraction/harvesting are disabled there;
- resources are carried back by players or accompanied living entities; loose world-item piles cannot cross Gates;
- Gate traversal itself is repeatable and should not become a grind;
- local storage/hand crafting are allowed; attended processors require player proximity, while automatic realm extraction/harvesting is disabled. See [ECONOMY.md](ECONOMY.md) and [TRANSPORT.md](TRANSPORT.md).

The endgame must not reduce every portal realm to `place miner, chunk-load it, never visit again`.

---

## 30. Player-built teleporters

Teleporters are later player technology for the **normal physical universe**.

Unlike rockets/Gates, they may eventually allow arbitrary linked endpoints and therefore break geography.

They should require substantial advanced infrastructure so rockets remain relevant through aerospace progression.

Potential uses:

- player transport;
- item logistics;
- direct planetary factory links.

They cannot connect to portal realms.

---

## 31. Satellites

A future orbital satellite system can assist with **cartography**.

Current concept only:

- satellites reveal/expand map coverage around the player or according to an eventual coverage model;
- balance/radius/orbital behaviour remains open;
- satellites should not automatically locate Gatebuilder structures;
- satellites should not become a default ore scanner.

The technology helps the player understand geography without deleting discovery.

---

## 32. Day/night and mobs

The final game requires:

- day/night cycle;
- passive, neutral and hostile mobs;
- world-specific spawning;
- combat/survival pressure;
- settlement inhabitants;
- realm-specific creatures later.

The current lore/mob direction is intentionally simple and documented in [LORE.md](LORE.md).

POC mob AI should prioritise a few reusable behaviour types rather than content volume.

### Performance

Mob simulation must respect distance:

```text
near player -> full AI
medium range -> simplified simulation
far away -> sleeping/no expensive AI
```

Do not attempt to maintain full AI for creatures across infinite worlds.

Navigation must cope with destructible voxel terrain; one giant static NavMesh is not appropriate.

Mobs may traverse active Gatebuilder gateways and should retain entity state when transferred.

---

## 33. Multiplayer direction

Multiplayer comes **after the single-player foundations**, but architecture must support it from day one.

Intended model: **server authoritative**.

```text
Client
 -> command
 -> authoritative server simulation
 -> validation
 -> state change
 -> event/delta
 -> interested clients
```

Future requirements:

- dedicated server build;
- world/chunk interest management;
- chunk streaming;
- block deltas;
- entity snapshots;
- machine/network replication;
- player commands;
- ownership/permissions;
- anti-cheat validation;
- chunk-loader ownership/quotas;
- authoritative Gate/rocket transport state.

The client should never authoritatively decide important world state.

### Geography and multiplayer

Sparse Gate networks plus coordinate-preserving travel deliberately allow distant communities to remain distant.

Spawn can naturally develop into a hub without forcing a player who settles thousands of chunks away to travel back to a unique portal.

---

## 34. Render distance vs simulation distance

Keep them independent.

Example only:

```text
render distance: 20 chunks
full simulation distance: 8 chunks
```

Far terrain may remain visible without running mobs, physics, machines and every simulation system at the same range.

Chunk-loaded factories use background simulation rather than increasing global simulation distance.

---

## 35. Persistence

Persistence must support extremely large universes efficiently.

Likely principles:

- deterministic generation from seed;
- store modified chunks rather than untouched procedural terrain;
- compressed binary chunk/region storage;
- block-entity state stored separately where appropriate;
- persistent universe-level state for systems such as Gate activation;
- save/version metadata;
- migration/versioning strategy before public save format stabilises;
- asynchronous/non-blocking saving where possible.

Undiscovered seed-defined Gate anchors should not require a huge explicit list in the save if they can be reproduced deterministically.

---

## 36. Debug/profiling tooling

Debug tooling is part of the POC.

Useful overlays/instrumentation include:

- chunk boundaries;
- chunk coordinates/world IDs;
- loaded chunks;
- background chunks;
- chunk ticket source/owner;
- mesh generation timings;
- world generation timings;
- active block entities;
- active mob counts;
- power networks;
- signal networks;
- item networks;
- fluid networks;
- topology rebuild timings;
- Gate anchors/states in developer mode;
- memory usage where practical;
- frame timing/profiling.

Optimization is continuous, not a final sprint.

---

## 37. Proof-of-concept scope

The POC should be **small in content and deep in architecture**.

The user's 2026-09-08 staging instruction locks **only the first playable step** below. [DEVELOPMENT_STRATEGY.md](DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence) owns its scope, exclusions, internal checkpoints and acceptance gate. The later groups preserve the full-game ambition as candidates; their order and size must be decided after reviewing the first playable result. Implementation was subsequently authorized; the first review led to a scoped placement and visual revision, recorded in [FIRST_POC.md](FIRST_POC.md). Checkboxes below distinguish implemented foundations from pending user acceptance.

### Stage 0 - First playable terrain, FPS and visual concept (locked scope)

- [x] Create the authorized Unity/URP project, pinned to 6000.4.4f1 with URP 17.4.0.
- [x] World-aware coordinates and deterministic natural terrain with no generated structures; real chunk generation, meshing, loading and unloading.
- [x] Playable FPS movement, selectable male/female 3D player models and visible first-person fists with basic animation; basic skin selection on both models with a shared body/hand appearance, as specified in [GAMEPLAY.md](GAMEPLAY.md#15-player-skins).
- [x] Fist mining, precise targeting/feedback and immediate collision correctness during asynchronous remeshing.
- [x] Real item stacks, physical mining drops/pickup, functional inventory/hotbar and personal crafting, activated after the original placeholder under the explicit [crafting extension](CRAFTING.md).
- [x] Preserve session edits and item state when leaving and returning to chunks; exercise origin shifts and bounded streaming residency.
- [x] Terrain-block placement from collected stacks, with current-face targeting, player overlap rejection, dropped-item displacement and session edit persistence.
- [ ] Establish and review a coherent terrain/player/UI/lighting concept in a standalone Windows build.
- [x] Record interaction, visual and streaming evidence against [GAMEPLAY.md](GAMEPLAY.md#14-locked-first-step-interaction-contract) and [SIMULATION.md](SIMULATION.md#12-first-step-terrain-and-streaming-validation).
- [ ] Review the result and known issues with the user, then decide the next implementation. Do not automatically start the candidates below.

### Candidate Stage 1 - Extend the sandbox (not yet selected)

- [ ] Extend the first step's item/inventory/drop systems with advanced building conveniences and tool progression.
- [x] Workbench, furnace and basic survival recipes under the explicit [survival extension](GAMEPLAY.md#survival-progression-farming-health-and-armor); industrial components remain later content.
- [ ] Save/reload terrain, inventory, piles and simple block entities.
- [ ] World water, sinking/buoyant piles, compatible merging and sleeping movement.
- [x] Initial day/night, health/death recovery and native creature behaviours under the explicit day/night, survival and [mob extensions](MOBS.md); broader ecology remains later scope.
- [ ] Play 20-30 minutes of gathering/building/crafting without developer commands.

### Candidate Stage 2 - Industrial hook (not yet selected)

- [ ] Reusable machine inventories, escrow, ports, process state and visible stop reasons.
- [ ] Boiler-engine/alternator, crusher, pump and finite-terrain drill with manual startup routes.
- [ ] Real item, power and minimal fluid networks; switch and logical signal control.
- [ ] Recipe/uses browser backed by the same recipe registry; no discovery hard locks.
- [ ] Build, diagnose and dismantle the complete extraction-to-storage chain, not just supplied chests.
- [ ] Keep the same authority/identity model used by the sandbox loop.

### Candidate Stage 3 - Lifecycle, scale and background simulation (not yet selected)

- [ ] Generic ticket capabilities, owner-online quotas, overlap deduplication and dormant boundaries.
- [ ] Active/background equivalence, resource accounting and stable scheduling.
- [ ] Topology split/merge, water/item load and excavation while saving.
- [ ] Coherent journal/checkpoint recovery and failure reporting.
- [ ] Benchmark playable workloads plus scaling tiers; batch only after reference equivalence.

### Candidate Stage 4 - Multi-world skeleton (not yet selected)

- [ ] Two lightweight world definitions in the same saved universe.
- [ ] Deterministic matching anchors with world-specific terrain and stable identities.
- [ ] Safe prepared arrivals, persistent Gate states and same-entity transfers.
- [ ] Temporary bounded tickets, cancellation, unload/reload and interrupted-transfer recovery.
- [ ] Exercise the selected Gate accompaniment/sanctuary rules with simple test presentation.

### Candidate Stage 5 - Expand the real game (not yet selected)

- [ ] Expand technology ages, manufacturing and construction conveniences.
- [ ] Richer terrain, settlements/ecology, Gate ruins and distinct exploration realms.
- [ ] Rockets, physical planets, prepared cargo routes and advanced teleporters.
- [ ] Multiplayer implementation, permissions and dedicated-server operation.
- [ ] Complete content/asset and release validation from [CONTENT_PIPELINE.md](CONTENT_PIPELINE.md) and [DELIVERY.md](DELIVERY.md).

These candidate groups are mirrored in [DEVELOPMENT_STRATEGY.md](DEVELOPMENT_STRATEGY.md). Their retained numbering is a planning reference, not approval to implement them in that order. The first step can be reviewed without functional crafting, industry or the wider POC scenarios below.

---

## 38. Broader POC acceptance scenarios (later candidates)

These are cumulative technical/gameplay ambitions for later selected milestones. They are not completion requirements for the locked first step, whose acceptance gate is in DEVELOPMENT_STRATEGY.md section 6.

Primary factory scenario:

```text
                    Generator
                       |
                    Power
                       |
                       v
Finite ore -> Drill -> Buffer -> Pipe -> Crusher -> Pipe -> Furnace -> Storage
                         ^
                         |
                    Signal Control
```

The player must be able to:

1. generate an effectively infinite deterministic world;
2. build the factory in first person;
3. save/reload correctly;
4. use the recipe browser to understand its components;
5. process items through pipe logistics;
6. use a fluid network where required;
7. power machines through a logical network;
8. control machinery through signals;
9. chunk-load the factory using player-owned tickets;
10. leave rendering/full simulation range;
11. have appropriate production continue in background simulation;
12. return and see correct inventories/state;
13. maintain stable performance without per-block/per-pipe polling;
14. construct the chain from gathered materials through the manual bootstrap path;
15. excavate finite terrain into output buffers without duplicate world drops;
16. redirect world water, observe buoyant/sinking stacks and recover from death;
17. diagnose a blocked output, insufficient power and a dormant intermediate network chunk.

Secondary architecture scenario:

1. instantiate two lightweight test worlds from different definitions;
2. generate deterministic matching test anchors from the same save seed;
3. transfer a player/entity at preserved X/Z;
4. generate/wake destination chunks before arrival;
5. keep the destination/source temporarily active via system ticket;
6. unload correctly after inactivity;
7. reload the save with cross-world state intact.

These scenarios provide evidence for the tested foundations, not proof of unlimited scale or a finished game. Use the workload matrix and provisional budgets in [SIMULATION.md](SIMULATION.md), record actual hardware/build settings, and include chunk boundaries, failure recovery, topology edits and long traversal. Full-game readiness also requires the player-experience coverage in [GAMEPLAY.md](GAMEPLAY.md).

---

## 39. Deferred scope

The locked first step's exclusions are owned by [DEVELOPMENT_STRATEGY.md](DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence). In particular, all generated structures, 4×4 station interfaces, water simulation and industry remain deferred from that step. The subsequently selected [survival progression](GAMEPLAY.md#survival-progression-farming-health-and-armor) includes the workbench, furnace, five tool tiers, farming, hunger, health and armor. The following larger content areas also remain outside the broader early POC ambition:

- huge biome library;
- polished villages;
- complete mob ecosystem;
- final combat balance;
- final art direction;
- final technology/age progression;
- full steampunk content;
- complete rocket system;
- finished additional planets;
- polished Gatebuilder gateway activation;
- complete portal realms;
- late-game teleporters;
- final satellite system;
- multiplayer matchmaking/UI;
- enormous recipe/content library.

---

## 40. Long-term target

The intended long-term journey resembles:

```text
Explore starting world
        |
Mine / craft / build
        |
Discover resources
        |
Early metalworking
        |
Mechanical industry
        |
Steam / steampunk factories
        |
Electrical industry
        |
Large pipe-based automation
        |
        +-------------------- discover ancient Gate ruins
        |                                  |
Advanced manufacturing                Portal realms
        |                                  |
Aerospace                         personal exploration
        |                                  |
Rockets                          unusual resources
        |                                  |
Other physical planets                    |
        |                                  |
Automated colonies                         |
        |                                  |
Cargo rockets                              |
        +------------------+---------------+
                           |
                  advanced technology
                           |
             physical-world teleporters
                           |
             large multi-world industry
```

Physical planets increasingly reward **automation and logistics**.

Portal realms deliberately continue rewarding **personal discovery and travel**.

The tension between those two behaviours is the core long-term identity of Rivet Reach.

---

## 41. Current non-negotiable architectural/design rules

Unless deliberately revisited, these decisions should guide implementation:

1. No GameObject per normal voxel.
2. World state is chunked data.
3. Multiple worlds are supported by the model from the beginning.
4. Each save is completely isolated.
5. World generation is modular through world definitions.
6. Important cross-world Gate anchors are deterministic from the save seed.
7. Early/intermediate inter-world travel preserves horizontal X/Z.
8. Power, signals, items and fluids are distinct simulation systems.
9. Pipes/cables represent topology; logical networks perform simulation.
10. No per-wire/per-pipe/per-block `Update()` architecture.
11. Item pipes are the primary automated item transport, not conveyor belts.
12. Chunk loaders use player-owned tickets and per-player quotas.
13. Factory chunk-loader tickets are active only while their owner is online.
14. Portal activity uses temporary system tickets rather than permanent hidden chunk loading.
15. Portal realms cannot be unattended remote resource farms.
16. Gatebuilder gateways cannot be constructed by players.
17. Gatebuilder Gate operation is separate from conventional player electricity.
18. Gate traversal has no per-use resource/energy cost once operational.
19. Gate networks contain many extremely widely spaced sites, not one unique global portal.
20. Shared Gate anchors preserve coordinates across linked dimensions.
21. Gate state persists outside loaded chunks.
22. A damaged receiving Gate cannot permanently strand the player.
23. Mobs can physically traverse active Gates.
24. Rockets preserve X/Z between physical worlds.
25. Player teleporters are later technology and cannot link to portal realms.
26. Satellites are currently cartography tools, not automatic Gate/ore scanners.
27. Progress comes from resources/capability rather than character levels.
28. Recipe discovery assists the player but should not arbitrarily block knowledgeable players.
29. Optimization is measured continuously.
30. Multiplayer is later, but authoritative simulation architecture starts now.
31. The POC proves architecture before content volume.

---

## 42. Open design areas

Working resolutions are indexed in [DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md). The following are remaining tuning/content/implementation choices; they do not reopen rules already selected in the specialist documents.

### Core engine

- exact chunk dimensions;
- final vertical bounds after testing the initial configured envelope;
- terrain/noise algorithms;
- optimized implementation of the selected queued voxel-light behaviour;
- binary packing and filesystem implementation of the journal/checkpoint protocol;
- floating-origin details.

### Crafting/industry

- exact inventory dimensions and stack sizes;
- recipe layouts and balance within the authorized 2×2/3×3/4×4 crafting model;
- later age names/resource chains beyond the specified bootstrap;
- depth of mechanical power;
- electrical voltage/transformer rules;
- tuning of the selected proportional shortage/safe-stop behaviour;
- signal channels;
- routing cache/optimization implementation under selected priority/round-robin semantics;
- fluid throughput model;
- multi-function conduits.

### Background simulation

- player chunk-loader quota values;
- chunk-loader progression/upgrades;
- exact background-simulation granularity;
- which eligible processes can batch equivalently; dormant production never earns catch-up.

### Worlds/transport

- exact physical planet count/types;
- rocket manufacture/fuel mechanics;
- cargo rocket balance;
- Gate macro-region size/minimum separation;
- number of Gate networks/portal realms;
- cost/art tuning for the selected two-slot Gate repair components;
- exact damaged-side cooldown;
- final Gatebuilder technology terminology;
- any future deliberate revision to planet-specific realm instances; cross-planet Gate shortcuts are excluded from the baseline;
- player teleporter limitations/energy requirements;
- satellite map-coverage model.

### World/lore/content

- final Gatebuilder identity/name;
- reason for their disappearance;
- exact portal-realm nature;
- final mobs and creature designs;
- village/NPC behaviour;
- structure library;
- portal realm ecology/resources;
- final visual identity and measured asset budgets under the selected content workflow.

### Multiplayer

- networking implementation/library;
- dedicated server configuration;
- UI and implementation of cooperative access, claims and personal loader ownership;
- server-side Gate/transport configuration;
- player limits and performance targets.

Use [DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md) to prioritize these open areas and record resolutions. These documents should be updated whenever measurements or new decisions materially change the project direction.

## Terrain and biome rework — 2026-09-09

The current terrain extension is specified in [TERRAIN_GENERATION.md](TERRAIN_GENERATION.md): five surface biomes, varied relief, natural entrances and deep caves, preserving the existing ore/base and session-edit contracts. Its [verification record](verification/TERRAIN_GENERATION_RESULTS.md) owns actual evidence; the wider exploration vision remains a future scope.


## Authorized native mob increment

The user selected Rustback beetle and Dusk prowler with original Blender assets, AI, melee combat and natural spawning on 2026-09-09. [MOBS.md](MOBS.md) owns this bounded extension and [its verification report](verification/MOB_RESULTS.md) owns measured evidence. It connects the current voxel world to shared day/night, item and survival authority, with bounded active entities and local terrain-aware navigation. Broader ecology, factory raids, mob loot, named persistent creatures and multiplayer AI remain future work.
