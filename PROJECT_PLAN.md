# Rivet Reach - Project Plan and Long-Term Vision

> **Status:** evolving design / proof-of-concept phase
>
> This document describes the current direction of Rivet Reach. It is intentionally **evolutive**: we will continue brainstorming, prototyping and benchmarking, and we expect details to change as the POC exposes better design or technical solutions.
>
> The POC must prove the core architecture before large amounts of content are added. Performance and scalability are first-class design constraints from the beginning, not cleanup work for later.

---

## 1. Core vision

Rivet Reach is a **first-person voxel sandbox** built around two equally important sides of play:

1. **Discovery and exploration**
   - Infinite procedural worlds.
   - Biomes, caves, villages, ruins, temples and other generated structures.
   - Day/night cycles and mobs.
   - New materials and resources discovered through exploration.
   - Rockets to physically reachable planets.
   - Ancient ruined gateway structures leading to strange portal realms.

2. **Engineering and automation**
   - Minecraft-style block placement, mining and crafting.
   - Machines and processing chains.
   - Item pipes and fluid pipes instead of conveyor belts and thousands of loose moving item entities.
   - Electrical power networks.
   - Redstone-inspired signal/control networks.
   - Automated factories and interplanetary logistics.
   - Chunk loading for factories that must remain simulated away from the player.

The goal is not to make a direct Minecraft clone. The familiar voxel/crafting interaction is the foundation, but Rivet Reach should develop its own visual identity, machinery, structures, creatures, materials, technology and exploration systems.

---

## 2. Design pillars

### 2.1 Open progression

There should be **no conventional player levels, XP gates or mandatory research-point progression** controlling the technology tree.

Progress is driven by what the player can physically obtain and manufacture:

```text
new resource
    -> new material
    -> new component
    -> new machine
    -> new manufacturing process
    -> access to further resources
```

A knowledgeable player should be able to **beeline toward a technology** if they want to. Doing so may be difficult, inefficient and resource-intensive, but the game should not respond with an arbitrary "requires level 20" message.

Technology is therefore mostly **soft-gated by materials, tools, machinery and infrastructure**.

Examples:

- A material may require a stronger tool to mine.
- A component may require precision manufacturing rather than hand crafting.
- A machine may require electricity that the player has not yet established.
- Rockets may require advanced alloys, fuel production and electronics.
- Advanced teleportation may require resources only available after space exploration.

We explicitly do **not** currently want temperature-management or toxic-atmosphere survival systems to become progression gates.

### 2.2 Discovery

Exploration should continue to matter even when the player owns a large automated industrial base.

Some progression comes from finding:

- new ores and materials;
- structures and ruins;
- villages and unusual generated locations;
- distant planets;
- ancient portal structures;
- rare resources found only in portal realms.

The recipe/UI system can reveal new possibilities as materials are discovered, but discovery is primarily a **knowledge/UI aid**, not a hard crafting permission system.

### 2.3 Automation

Automation should eventually allow large industrial systems comparable in spirit to Factorio and classic Minecraft technology mods, but adapted to a first-person voxel game.

The player should be able to build systems such as:

```text
Ore Storage
    |
    v
 Item Pipe
    |
    v
 Crusher
    |
    v
 Item Pipe
    |
    v
 Furnace
    |
    v
 Finished Storage
```

with power, fluid and signal networks controlling the process.

### 2.4 Performance and scalability

Optimization is a design requirement from the first prototype.

We should not build features first and attempt to optimize them once the game is already slow.

A useful rule:

> If a system's CPU cost scales directly with the total number of blocks in the world, its architecture is probably wrong.

Large static worlds and inactive factories should be cheap. CPU time should primarily be spent on things that are **near players, active, or changing**.

The target is a game that runs well on reasonable gaming hardware and does not require a high-end PC simply because the player has built a serious factory.

---

## 3. Selected technology stack

### Engine

**Unity 6.3 LTS**

### Language

**C#**

### Rendering

**Universal Render Pipeline (URP)**

The world should keep its block-based visual identity while improving presentation through features such as:

- higher-quality textures;
- material variation;
- normal/PBR information where useful;
- ambient occlusion;
- better sunlight and shadows;
- atmospheric fog;
- improved water;
- particles;
- animated vegetation;
- more detailed machine and mob models;
- dynamic lights where performance permits.

The POC should remain visually simple until the underlying world and simulation architecture is proven.

### Performance tooling

Use Unity's data-oriented tools selectively where they make sense:

- **Jobs System**;
- **Burst Compiler**;
- `NativeArray` / native collections;
- background worker jobs for generation and meshing;
- ECS/Entities only where benchmarking shows a real advantage.

The entire game should **not** be forced into ECS purely because it exists.

Likely candidates for Jobs/Burst include:

- terrain generation;
- chunk meshing;
- lighting calculations;
- noise generation;
- bulk voxel operations;
- some pathfinding tasks;
- network topology calculations if profiling justifies it.

### Commercial licensing assumption

At the time this plan was written, Unity Personal permits commercial use below Unity's applicable financial threshold, the old Runtime Fee has been cancelled, and Unity Pro becomes required above the Personal threshold. These terms must be rechecked before commercial release or major funding because licensing can change.

---

## 4. Simulation architecture

The simulation should be separated from the presentation layer as much as practical.

Conceptually:

```text
                    Rivet Reach
                        |
        +---------------+----------------+
        |                                |
     Client/View                  Simulation Core
   Unity presentation               Pure C# where practical
        |                                |
   rendering/audio             world/entities/networks
                                         |
                             +-----------+-----------+
                             |                       |
                         Signal networks         Power networks
                             |                       |
                         Machines                Logistics
```

This is important for:

- deterministic testing;
- save/load reliability;
- background simulation;
- dedicated servers later;
- multiplayer authority;
- running simulations without rendering.

Single-player should use the same fundamental command/simulation path that multiplayer can later use.

Avoid gameplay code where a view/controller directly mutates authoritative world state.

Prefer:

```text
Player action
    -> command
    -> simulation validation
    -> world change
    -> event
    -> presentation update
```

rather than:

```text
PlayerController directly edits everything
```

---

## 5. Save and universe model

Each save is a **completely separate universe**.

There are no interactions between saves:

- no shared inventory;
- no shared machines;
- no shared discoveries;
- no cross-save portals;
- no cross-save logistics;
- no global resource storage.

Conceptually:

```text
SAVE
|
+-- Player data
+-- Discovery / recipe-knowledge state
+-- Universe state
|
+-- Worlds
    |
    +-- Physical worlds
    |   +-- Starting planet
    |   +-- Moon
    |   +-- Other planets
    |
    +-- Portal realms
        +-- Realm A
        +-- Realm B
        +-- Realm C
```

Terminology:

- **Save**: one isolated game/universe.
- **Universe**: all worlds belonging to that save.
- **World**: one actual voxel simulation space.
- **Planet**: a normal physical celestial body/world in the universe.
- **Portal realm**: a separate alien/otherworldly reality reached through ancient gateways.

The POC may initially generate only one world, but the APIs and save format must **not assume that only one world exists**.

---

## 6. World definition system

World generation should be configuration/modular driven from the beginning.

We should avoid hard-coding a single `GenerateOverworld()` implementation and attempting to retrofit moons, planets and portal realms later.

A conceptual `WorldDefinition` may contain information such as:

```text
WorldDefinition
|- world id / type
|- seed
|- terrain profile
|- biome profile
|- cave profile
|- ore distribution
|- structure profile
|- gravity
|- sky / lighting profile
|- water profile
|- mob/spawn rules
|- rocket rules
|- teleporter rules
|- gateway rules
|- chunk-loader rules
|- automation rules
|- generation modifiers
```

Examples:

### Starting planet

```text
terrain: continental
biomes: broad natural set
water: oceans, lakes, rivers
structures: villages, ruins, temples, ancient gateways
normal automation: yes
rockets: yes
teleporters: eventually yes
chunk loading: yes
```

### Moon / nearby physical planet

```text
terrain: cratered / planet-specific
biomes: planet-specific
water: definition-dependent
structures: rare generated structures
normal automation: yes
rockets: yes
teleporters: eventually yes
chunk loading: yes
```

### Portal realm

```text
terrain: alien / realm-specific
biomes: alien
structures: ancient / unique
rockets: no
normal inter-world teleporters: no
remote resource automation: no
chunk loaders: disabled as current design
```

The exact values and content are future design work, but the **ability to express these differences must exist from the first generation architecture**.

---

## 7. Procedural world generation

World generation should be deterministic from a seed.

A possible normal-world pipeline:

```text
seed
 -> large-scale land/continent distribution
 -> elevation / ridges / mountains
 -> humidity / biome distribution
 -> terrain shaping
 -> rivers / water features
 -> caves
 -> ores
 -> vegetation/features
 -> structures
```

The final generation technique will be benchmarked during the POC rather than selected purely theoretically.

### Structural generation

Generated structures are a core requirement, eventually including things such as:

- villages;
- roads;
- houses;
- farms;
- workshops;
- ruins;
- temples;
- mines;
- abandoned settlements;
- ancient gateway sites;
- world-specific structures.

Structures should ultimately be modular and data-driven where possible.

For example, villages can be assembled from:

```text
village seed
 -> layout / road graph
 -> plots
 -> building pools
 -> variants
 -> decoration
```

rather than every village being a single fixed blueprint.

---

## 8. Voxel/chunk architecture

### Blocks are data, not GameObjects

The infinite world must not use one Unity `GameObject`/`Node` equivalent per block.

Normal blocks should be compact data stored inside chunks.

Conceptually:

```csharp
struct Block
{
    ushort Type;
    ushort State;
}
```

The exact representation will evolve and can later use palette compression and bit packing.

### Chunk storage

Tentative starting point for testing:

```text
32 x 32 x 32 blocks per chunk
```

This is not final. Chunk dimensions must be benchmarked against:

- mesh generation cost;
- memory;
- update frequency;
- save size;
- networking later;
- visibility and culling.

### Block entities

Only blocks that require additional persistent behaviour/state should become richer simulation objects, for example:

- chests;
- furnaces;
- generators;
- batteries;
- machines;
- configurable sensors;
- pumps;
- controllers.

Millions of stone/dirt blocks require no object instances.

---

## 9. Chunk rendering

Each chunk should build combined meshes rather than rendering individual cubes.

Pipeline:

```text
chunk voxel data
 -> visible-face calculation
 -> greedy/optimized meshing
 -> mesh buffers
 -> Unity renderer
```

Use **greedy meshing or equivalent face merging** to reduce geometry where appropriate.

Rendering should support separate paths/material groups for at least:

- opaque geometry;
- cutout geometry;
- transparent geometry;
- fluids as required.

Chunk meshing should move off the main thread where practical using Jobs/Burst.

Only affected chunks and relevant neighbours should remesh after voxel changes.

### Floating origin

The world can be effectively infinite in X/Z, but Unity's floating-point precision cannot be trusted at extreme distances.

Store absolute locations using chunk/world coordinates and use a **floating-origin strategy** for the rendered local world around the player.

---

## 10. FPS player interaction

Rivet Reach is designed as a **first-person game**.

POC interaction includes:

- mouse look;
- walk;
- sprint;
- jump;
- block targeting/raycast;
- mine/break block;
- place block;
- interact with machines/containers;
- hotbar selection.

Third-person is not a POC priority.

The physical world should remain important. Machines can eventually display information through models, indicator lights, gauges, moving parts and local controls rather than forcing every interaction into large abstract menus.

---

## 11. Inventory and hotbar

Keep the familiar Minecraft-like interaction model, but make it much more generous because an industrial game will contain many resources and components.

Tentative POC values to test:

- **12-slot hotbar**;
- **6 x 8 main inventory = 48 slots**;
- around **60 immediately accessible slots total**.

These numbers are not final.

Stack sizes should also be more permissive than Minecraft. Tentative examples:

```text
building blocks: 250-500
basic materials: 250
components: ~100
machines: smaller stacks depending on type
unique tools/equipment: 1
```

The goal is not to make repeated inventory-full messages a major gameplay mechanic.

Storage and logistics should matter because factories process large volumes of material, not because the player can barely carry basic building supplies.

---

## 12. Crafting system

Keep the recognisable **grid crafting** approach.

Possible early structure:

- player inventory crafting grid (small/basic recipes);
- workbench with a larger traditional grid;
- industrial machines for advanced recipes.

The exact 2x2/3x3 division can be tested, but hand crafting should remain simple and intuitive.

As technology advances, recipes should move naturally into purpose-built machines rather than growing into enormous hand-crafting grids.

Examples:

- furnace;
- crusher;
- machine shop / assembler;
- chemical processing equipment;
- future age-specific manufacturing machines.

### Data-driven recipes

Recipes should be registered/configured as data wherever practical rather than encoded through giant chains of bespoke conditionals.

This supports:

- easier balancing;
- recipe browsing;
- future modding possibilities;
- machine upgrades;
- content expansion.

---

## 13. Recipe browser / discovery UI

A built-in **NEI/JEI-style recipe browser** is a core feature, not an optional future mod.

Selecting an item/material should allow the player to answer both:

```text
How do I make this?
What can I make with this?
```

Example:

```text
COPPER INGOT

Uses:
- Copper wire
- Copper pipe
- Copper plate
- Motor component
- Generator component
- Electrical cable
```

Recipes should be navigable as a graph: clicking an ingredient opens its recipes and uses.

The same interface must include machine recipes and processing alternatives.

For example:

```text
IRON ORE

Smelting:
Iron Ore -> Iron Ingot

Crusher:
Iron Ore -> Crushed Iron

Advanced processing:
Iron Ore -> higher-yield intermediate
```

### Discovery vs hard unlocks

The recipe browser may classify content as:

- known;
- newly discoverable;
- unknown/hidden.

Acquiring a new resource can reveal relevant recipes so the UI does not show thousands of meaningless late-game items immediately.

However, this should not become an arbitrary permission wall. If an experienced player already knows a recipe and can physically obtain the required materials/machine, the system should favour openness rather than forcing a research meter.

---

## 14. Technology ages

Progression should naturally pass through technological eras, but **ages are descriptive stages of capability, not XP levels**.

A provisional direction is:

```text
Primitive / hand crafting
        |
Early metalworking
        |
Mechanical systems
        |
Steam / steampunk industrial age
        |
Electric age
        |
Industrial / advanced manufacturing
        |
Aerospace
        |
Advanced interplanetary technology
```

The exact ages and resource chains will be designed later.

### Steampunk age

One era should have a strong steampunk visual identity, including elements such as:

- brass and copper;
- riveted metal;
- boilers;
- pressure gauges;
- steam exhaust;
- flywheels;
- pistons;
- shafts/gears where useful;
- analogue controls;
- steam generators;
- large mechanical machines.

Technology should visibly evolve rather than merely replacing a machine with a higher-number version.

For example, moving from a steam/mechanical installation to electrical motors and distributed wiring should change how factories are built.

---

## 15. Machines

Machines should share reusable capabilities/interfaces rather than each being a giant bespoke system.

Conceptually, a machine may expose combinations of:

```text
inventory input
inventory output
power input/output
signal input/output
fluid input/output
recipe processor
status/configuration
```

This makes new machines primarily combinations of common systems and configuration.

Example crusher:

```text
power demand: 250 W
input: ore
output: crushed ore
processing time: 4 s
```

Exact numbers are placeholders for future balancing.

---

## 16. Item logistics

Rivet Reach should use **item pipes**, not Factorio-style conveyor belts as the primary logistics system.

The reason is both gameplay preference and performance: the game should not need to render and simulate thousands of individual items moving physically across belts.

### Network-level simulation

A connected pipe system should become a logical network.

```text
Chest ===== Item Pipe Network ===== Crusher
```

Do not tick every pipe block individually every frame.

Instead:

```text
pipe blocks = topology
connected topology = ItemNetwork
ItemNetwork = transfer decisions/state
```

When a pipe is placed or removed, rebuild only the affected topology/network.

The network can transfer abstract quantities between inventories.

Optional visual items travelling through transparent pipes can be cosmetic and must not become authoritative physics entities.

### Future capabilities

Potential item-pipe features include:

- extraction modules;
- filters;
- priority outputs;
- round-robin distribution;
- throughput tiers;
- colour/channel routing;
- machine side configuration;
- storage interfaces.

---

## 17. Fluid logistics

Fluids use their own network system.

A fluid network should track useful game quantities such as:

- fluid type;
- amount/volume;
- capacity;
- input rate;
- output demand;
- throughput.

It should **not** attempt computational fluid dynamics.

Potential fluids later include:

- water;
- steam;
- oil;
- fuels;
- coolant;
- industrial/chemical liquids.

Different pipe tiers can provide different throughput/capacity without requiring realistic fluid simulation.

---

## 18. Signal/control network

The game must retain the creativity of Minecraft's redstone-like automation, but implement its own **signal/control network**.

Signal tells a device **what to do**.

Power determines whether the device **can do it**.

These are separate systems.

Possible signal devices:

- switch;
- button;
- pressure plate;
- sensor;
- timer;
- repeater/delay;
- comparator;
- AND/OR/XOR/NOT logic;
- latch;
- counter;
- relay;
- configurable controller;
- lamps;
- doors;
- machines.

Example:

```text
Storage sensor
    |
    v
IF iron > threshold
    |
    v
Stop crusher
```

or:

```text
Tank sensor
    |
    v
IF water < 20%
    |
    v
Enable pump
```

### Event-driven simulation

Signals must **not** be recalculated for every wire every frame.

Prefer:

```text
switch changes
 -> signal-change event
 -> affected network queued
 -> network recalculated
 -> connected devices notified
```

A huge dormant circuit should cost nearly nothing.

Future signal channels/colours/numbers may allow several independent circuits to share conduit routes.

---

## 19. Electrical power

Electricity is separate from signals.

Example:

```text
Switch ---- signal ----> Electric Furnace
                           ^
                           |
Generator ---- cable ---- Battery
```

The furnace can be commanded ON while still reporting **NO POWER**.

### Simplified engineering model

Do not initially implement full Kirchhoff electrical-network simulation.

The game should model electricity deeply enough to create engineering decisions without becoming electrical-engineering software.

A power network can track concepts such as:

- generation capacity;
- demand;
- stored energy;
- machine priority;
- cable/network limits;
- later voltage tiers.

Potential progression:

```text
basic generator / low voltage
 -> steam generation / larger storage
 -> medium voltage industrial grid
 -> advanced high-capacity grid
```

Future voltage/transformer mechanics may allow incorrect connections to fail or damage machinery, but the exact level of harshness is a later gameplay decision.

Possible power sources across progression may include:

- primitive/mechanical sources;
- water/wind where appropriate;
- combustion generators;
- steam;
- solar;
- geothermal;
- nuclear;
- advanced late-game systems.

---

## 20. Network architecture rule

Item, fluid, power and signal systems should use the same broad optimization principle:

```text
blocks represent topology
networks represent simulation
```

A line of 500 inactive cable blocks should not mean 500 `Update()` calls.

Only topology changes, resource transfers, signal changes or active machines should perform meaningful work.

---

## 21. Chunk states and simulation levels

Chunk loading should have several concepts rather than only "loaded" vs "not loaded".

Possible states:

```text
DORMANT / UNLOADED
    |
BACKGROUND SIMULATION
    |
ACTIVE PLAYER SIMULATION
```

### Active chunks

Near players:

- rendering;
- full relevant block simulation;
- nearby mobs/AI;
- physics;
- particles;
- machines;
- power/logistics;
- local gameplay.

### Background-simulated chunks

Chunk-loader factory chunks can run:

- machines;
- item networks;
- fluid networks;
- power networks;
- signals;
- production timers.

They generally should not run expensive unrelated systems such as:

- rendering;
- particles;
- distant mob AI;
- unnecessary physics.

### Dormant chunks

Store state and consume essentially no continuous CPU until something loads/wakes them.

---

## 22. Chunk loaders

Chunk loaders are necessary because industrial bases must continue working when the player travels elsewhere, including to other planets.

### Player-owned load tickets

Current preferred rule:

> A chunk loader contributes its load ticket **only while its owner is online**.

This avoids the server waking every offline player's factories when any unrelated player logs in.

Example:

```text
Stone offline
Alice online elsewhere

Stone's chunk-loader tickets: inactive
Alice's chunk-loader tickets: active
```

If a player physically visits Stone's factory, the chunks can still become active through normal player proximity. The important rule is that Stone's remote loader does not wake merely because somebody else is online somewhere on the server.

### Per-player quota

Chunk loading must have a **per-player limit**.

Exact numbers will only be chosen after benchmarking.

Possible concept:

```text
Chunk loading budget: 8 / 12 / 16 chunks
```

The limit may evolve with server configuration or game progression, but it must never be unlimited by default.

### Overlapping tickets

If multiple players load the same chunk, simulate it once.

```text
Chunk 50,50
load tickets:
- Stone
- Alice
```

Disconnecting Stone removes Stone's ticket. Alice's ticket keeps it active.

When no valid ticket and no nearby player remains, the chunk can sleep/unload.

---

## 23. Background/offline-style factory calculation

Where possible, long-running machine processes should support coarse or elapsed-time calculation rather than literally executing thousands of tiny ticks.

Example:

```text
machine cycle: 10 seconds
elapsed background time: 1800 seconds
available input: 100
available fuel/power: enough

maximum cycles by time: 180
maximum cycles by input: 100
result: process 100 cycles
```

This will not work for every complex machine/network, but it should be exploited where mathematically safe.

The purpose is to support substantial multi-world automation without keeping huge areas in expensive full simulation.

---

## 24. Physical planets

Physical planets are part of the normal universe and are intended to become **industrial territory**.

Players initially reach new worlds through rockets.

Expected long-term loop:

```text
build industrial base
 -> construct rocket infrastructure
 -> reach new planet
 -> explore and establish outpost
 -> find planet-specific resources
 -> build automated extraction/processing
 -> automate return logistics
 -> eventually replace expensive transport with advanced teleportation
```

### Rockets

Rockets should be meaningful machines/projects rather than simply a menu button.

Potential requirements later:

- launch structure/pad;
- fuel;
- engines;
- structural materials;
- electronics;
- cargo capacity;
- player transport.

We do not currently intend full Kerbal-style orbital mechanics, but launches should feel physical and significant.

### Automated interplanetary logistics

Later systems may include:

- cargo rockets;
- automated launches;
- resource import/export;
- remote planetary factories;
- advanced item teleportation.

The desired late-game result is a network of automated worlds supplying one another.

---

## 25. Teleporters

Teleporters are **player-built advanced technology** for the normal physical universe.

They can eventually reduce the logistics cost of rockets and connect industrial bases directly.

Potential late-game concept:

```text
Planet A storage
    |
 item pipe
    |
 teleport transmitter
    ||
    || interplanetary link
    ||
 teleport receiver
    |
 item pipe
    |
 Planet B factory
```

Teleportation should require substantial infrastructure and/or energy so it does not trivialize rockets immediately after rockets are invented.

Normal teleporters **do not work with portal realms**.

---

## 26. Ancient gateways / portal realms

Portal realms serve a fundamentally different design purpose from physical planets.

They are focused on **discovery, mystery and player exploration**, not automated resource exploitation.

Ancient gateways should be discovered as **ruined generated structures**, giving the exploration loop a Stargate-like feeling while using our own visual design, terminology and lore.

Possible discovery experience:

```text
exploration
 -> unusual ruin
 -> ancient inactive gateway
 -> unknown materials/symbols
 -> later learn how to repair/activate it
 -> travel to alien realm
```

Different gateway sites may lead to different realms or require different activation knowledge/components.

### Portal-realm rules

Current intended rules:

- reached through ancient gateways;
- rockets cannot reach them;
- normal player-built teleporters cannot link to them;
- automated inter-world resource transfer is not allowed;
- chunk loaders are disabled there as the current design;
- resources must be obtained through actual player travel/exploration.

Local machines may eventually be usable while players are present, but the realm must not turn into a remote AFK mining colony.

The player should continue having reasons to personally return to portal realms even in the late game.

This preserves the distinction:

```text
PHYSICAL PLANETS
explore -> colonize -> automate -> optimize logistics

PORTAL REALMS
find -> enter -> explore personally -> bring discoveries home
```

---

## 27. Interplay between exploration and industry

The two progression branches should feed each other without becoming identical.

Example long-term structure:

```text
                    DISCOVERY
                       |
          +------------+------------+
          |                         |
    Physical space             Portal realms
          |                         |
       Rockets                  Ancient gates
          |                         |
       Planets                  Exploration
          |                         |
   Automated industry          Unique resources
          |                         |
   Cargo / teleporters         Manual player travel
          |                         |
          +------------+------------+
                       |
                New technologies
                       |
                 Better industry
```

Some advanced technology may require combining industrial capability with rare discoveries from portal realms.

---

## 28. Day/night and mobs

The final game requires:

- day/night cycle;
- passive/neutral/hostile creatures as appropriate;
- mob spawning rules;
- world-specific creatures later;
- combat/survival pressure;
- generated settlements and inhabitants where designed.

The POC does **not** need a complete mob ecosystem.

### Performance approach

Mob AI must respect simulation distance.

Conceptually:

```text
near player: full AI
medium range: simplified simulation
far away: sleeping / no expensive AI
```

We should not attempt to run complex AI for every creature in generated worlds.

Navigation should be compatible with destructible voxel terrain; relying on one huge static Unity NavMesh for an infinite world is not appropriate.

---

## 29. Multiplayer direction

Multiplayer is **not part of the first POC**, but architecture decisions from day one must avoid making multiplayer a rewrite.

The intended model is **server authoritative**.

Conceptually:

```text
Client
 -> BreakBlockCommand
 -> Server / authoritative simulation
 -> validation
 -> block changes
 -> event/delta
 -> interested clients
```

Single-player can run the same simulation locally.

### Future networking concerns

The networking layer will eventually need:

- dedicated server builds;
- chunk interest management;
- chunk streaming;
- block deltas;
- entity snapshots;
- machine/network state replication;
- player commands;
- anti-cheat validation;
- ownership/permissions;
- chunk-loader ownership.

The client should never be trusted to authoritatively decide important world state.

---

## 30. Render distance vs simulation distance

Rendering and simulation should be independently configurable.

Example concept only:

```text
render distance: 20 chunks
full simulation distance: 8 chunks
```

Far terrain can remain visible without running mobs, machines, physics and every block tick at the same distance.

Chunk-loaded factories are handled through the separate background-simulation system rather than simply increasing global simulation distance.

---

## 31. Persistence

World persistence must support extremely large generated universes efficiently.

Likely direction:

- deterministic generation from seed;
- save modified chunks rather than storing every untouched generated voxel;
- compressed binary chunk/region storage;
- separate block-entity state;
- world/version metadata;
- migration/versioning strategy before save format becomes public/stable.

Exact storage format will be selected through POC testing.

Saving should not freeze the game for noticeable periods.

---

## 32. Debug/profiling tools are part of the POC

Voxel games become difficult to optimize when internal state is invisible.

Build debug tools early for displaying things such as:

- chunk boundaries;
- loaded chunks;
- background-simulated chunks;
- chunk-loader tickets/owners;
- mesh generation timings;
- world generation timings;
- active block entities;
- power networks;
- item networks;
- fluid networks;
- signal networks;
- network rebuild timings;
- entity/mob counts;
- memory usage where practical.

Profiling is not a final-stage activity.

---

## 33. Proof-of-concept scope

The POC should be **small in content and deep in architecture**.

### Phase 1 - Core voxel world

- [ ] Unity 6.3 LTS project using URP.
- [ ] FPS controller.
- [ ] Chunk coordinate/world coordinate system.
- [ ] Modular `WorldDefinition` architecture.
- [ ] Deterministic seeded terrain generation.
- [ ] Infinite X/Z chunk streaming.
- [ ] Basic height terrain.
- [ ] Basic caves.
- [ ] Small block palette (~5-8 block types initially).
- [ ] Chunk mesh generation.
- [ ] Greedy/optimized meshing.
- [ ] Chunk load/unload.
- [ ] Floating-origin support or architecture ready for it.
- [ ] Block break/place.
- [ ] Save/load modified chunks.

### Phase 2 - Inventory and crafting

- [ ] Larger hotbar.
- [ ] Larger main inventory.
- [ ] Stack handling.
- [ ] Basic item registry.
- [ ] Data-driven crafting recipe registry.
- [ ] Hand/workbench grid crafting.
- [ ] Basic NEI/JEI-style recipe/uses browser.
- [ ] Discovery-aware recipe visibility without hard XP/research gates.

### Phase 3 - Machines and power

- [ ] Reusable machine/block-entity foundation.
- [ ] Furnace.
- [ ] Crusher or equivalent powered processing machine.
- [ ] Basic generator.
- [ ] Power cable blocks.
- [ ] Logical `PowerNetwork`.
- [ ] Power production/demand.
- [ ] Basic energy storage if needed for the POC.

### Phase 4 - Signals

- [ ] Switch.
- [ ] Signal wire/conduit.
- [ ] Lamp and/or machine signal input.
- [ ] Event-driven `SignalNetwork`.
- [ ] No per-wire per-frame polling.

### Phase 5 - Item logistics

- [ ] Container/chest.
- [ ] Item pipes.
- [ ] Extractor/interface.
- [ ] Logical `ItemNetwork`.
- [ ] Machine input/output automation.
- [ ] At least basic filtering or destination rules.

### Phase 6 - Fluids

- [ ] Fluid type/quantity model.
- [ ] Pump/source for testing.
- [ ] Fluid pipe network.
- [ ] Tank.
- [ ] Machine consuming or producing fluid.
- [ ] Aggregate/network simulation rather than per-pipe fluid physics.

### Phase 7 - Chunk-loader/background simulation

- [ ] Chunk ticket system.
- [ ] Player ownership.
- [ ] Per-player quota support.
- [ ] Owner-online activation rule.
- [ ] Overlapping-ticket deduplication.
- [ ] Background factory simulation without rendering/mob AI.
- [ ] Coarse elapsed-time machine processing where valid.

### Phase 8 - Multi-world readiness

- [ ] Save owns a universe, not one hard-coded world.
- [ ] World IDs included in coordinates/persistence APIs where necessary.
- [ ] Multiple `WorldDefinition` profiles can be instantiated.
- [ ] Travel API capable of moving a player between worlds.
- [ ] Rules for normal planets vs portal realms represented in configuration.

The POC does not initially need polished rockets, multiple finished planets or finished ancient gateway content. It needs to prove that adding them later does not require replacing the foundation.

---

## 34. POC acceptance test

The main technical acceptance scenario should be something close to:

```text
                    Generator
                       |
                    Power
                       |
                       v
Ore Chest -> Pipe -> Crusher -> Pipe -> Furnace -> Pipe -> Storage
                         ^
                         |
                    Signal Control
```

The player must be able to:

1. generate an effectively infinite seeded world;
2. build this small factory in first person;
3. save and reload it correctly;
4. use the recipe browser to understand its components;
5. supply and process resources through item pipes;
6. supply any required fluid through fluid pipes;
7. power the machines through a real logical power network;
8. control at least part of it through the signal system;
9. chunk-load the factory;
10. travel far enough away that the factory is no longer rendered/fully simulated;
11. have production continue correctly in background simulation;
12. return and observe the expected inventory/state;
13. maintain stable performance without unnecessary per-block/per-pipe updates.

If this works reliably and benchmarks well, the POC has proven the systems that the larger game depends on.

---

## 35. Explicitly outside the initial POC

Do not allow these to distract from the foundation initially:

- huge biome library;
- polished procedural villages;
- full mob ecosystem;
- final combat balance;
- final art direction;
- final age/technology tree;
- full steam-era content;
- rockets;
- finished planets;
- final gateway/portal-realm content;
- late-game teleporters;
- multiplayer UI/matchmaking;
- massive recipe/content library.

They are important final-game goals, but they depend on the core architecture being stable and fast first.

---

## 36. Long-term target

The intended end-state is a sandbox where a player can begin by punching/mining/placing familiar voxel blocks and eventually build a multi-world industrial network without the game losing its exploration side.

A possible long-term player journey:

```text
Explore starting world
        |
Gather / craft / build
        |
Discover metals
        |
Mechanical industry
        |
Steampunk / steam factories
        |
Electric industry
        |
Large automated pipe-based factories
        |
Discover ancient gateway ruins --------+
        |                               |
Advanced manufacturing             Portal realms
        |                               |
Aerospace                          Manual exploration
        |                               |
Rockets                            Strange resources
        |                               |
Other planets                         |
        |                               |
Automated colonies                     |
        |                               |
Cargo logistics                        |
        +---------------+---------------+
                        |
               Advanced technology
                        |
          Interplanetary teleporters
                        |
          Large optimized world network
```

Physical planets increasingly reward **automation and logistics**.

Portal realms deliberately remain **places the player must personally explore**.

That tension between discovering the unknown and then engineering increasingly powerful systems from what was discovered is the core long-term identity of Rivet Reach.

---

## 37. Current non-negotiable architectural rules

These are the decisions most likely to create major headaches if ignored early:

1. **No GameObject per normal voxel.**
2. **World state is chunked data.**
3. **Multiple worlds are supported by the model from the beginning.**
4. **Each save is completely isolated.**
5. **Normal planets and portal realms are distinct world categories.**
6. **World generation is driven by modular definitions, not one hard-coded overworld.**
7. **Power, signal, item and fluid networks are distinct simulation systems.**
8. **Pipes/cables are topology; connected networks perform the simulation.**
9. **No per-wire/per-pipe/per-block `Update()` architecture.**
10. **Chunk loaders use player-owned tickets and have quotas.**
11. **Chunk-loader tickets are active only while the owner is online.**
12. **Portal realms cannot be automated into remote resource farms.**
13. **Performance is benchmarked continuously.**
14. **Multiplayer is later, but authoritative command/simulation architecture starts now.**
15. **Progress comes from resources and capability, not character levels.**
16. **Recipe discovery assists the player but should not arbitrarily block knowledgeable players.**
17. **The POC proves architecture before content volume.**

---

## 38. Open design areas

The following are intentionally not final yet and should evolve through brainstorming and testing:

- exact chunk dimensions;
- world height/storage layout;
- terrain/noise algorithm;
- lighting algorithm;
- exact inventory dimensions and stack sizes;
- final crafting-grid dimensions;
- exact age names and progression chains;
- early mechanical power depth;
- exact electrical voltage/transformer rules;
- power-shortage behaviour;
- final signal channels and logic blocks;
- item-pipe routing algorithms;
- fluid throughput model;
- chunk-loader quota values;
- chunk-loader progression/upgrades;
- background-simulation granularity;
- exact planet count/types;
- rocket mechanics;
- gateway activation/discovery mechanics;
- portal-realm rules beyond the core no-automation requirement;
- mobs and combat;
- village/NPC systems;
- final visual style and asset pipeline;
- networking library/implementation once multiplayer work begins.

This file should be updated whenever POC measurements or design decisions materially change the plan.
