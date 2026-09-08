# Rivet Reach - Player Experience and Complete-Game Scope

> **Status:** brainstorming specification. Existing pillars are agreed direction; experience targets and reward models below are proposals. No feature is implemented or playtested.

Related: [PROJECT_PLAN.md](PROJECT_PLAN.md), [LORE.md](LORE.md), [TRANSPORT.md](TRANSPORT.md), [DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md).

## 1. What responsive means

The intended game responds clearly to player actions while supporting large persistent systems. High average FPS alone does not establish responsiveness.

Proposed interaction contract:

- targeting, placement previews, hotbar changes and inventory actions acknowledge input promptly;
- an invalid action explains its relevant cause: obstruction, range, missing resource or incompatible port;
- machines expose why they stopped: no input, full output, insufficient power, control disabled or unavailable network region;
- long generation, travel and save operations show meaningful state and avoid apparently lost input;
- visual anticipation may be immediate, while authoritative state remains validated;
- sound, symbols and animation supplement colour; essential information remains readable with reduced effects.

Technical candidate budgets live in [SIMULATION.md](SIMULATION.md). Multiplayer latency handling will need separate validation when networking is designed.

## 2. First-session experience

**Proposed scenario:** a new player can discover a useful material, learn a recipe, make a tool, improve a process and understand a small automated chain without external documentation. Experienced players can move directly toward known capabilities.

The recipe browser should expose prerequisites and machine requirements without revealing every exploration secret. Discoverability must not become an invisible crafting lock. Open: a spoiler setting, unknown-material silhouettes, and how much of an undiscovered recipe path is shown.

Do not set a mandatory timed tutorial yet. Observe where players become confused, how long they spend walking or collecting, and whether a completed machine creates an understandable benefit. Exact onboarding prompts and first-session pacing remain open.

## 3. Progression that changes play

A complete progression chain should connect resource discovery to manufacturing capability and then to a meaningful new activity. Every major age needs a reason to exist beyond larger numerical output.

Proposed review questions for each age:

- What new construction or logistical decision becomes possible?
- What earlier inconvenience is reduced?
- What new resource, place or process becomes reachable?
- Can a knowledgeable player obtain prerequisites without an arbitrary unlock?
- Does the player understand the next useful capability through the world and recipe browser?

Steam and electricity should change layout and control decisions. Aerospace should create a colony/logistics loop. Teleporters should improve an already useful interplanetary system. Exact recipes, durations and costs remain open.

## 4. Exploration rewards without repeated errands

**Agreed direction:** realms require personal exploration and cannot become unattended inter-world mining colonies.

**Proposed reward preference:** favour durable upgrades, reusable catalysts, unusual building options, discoveries and substantial expedition yields. A discovered technique can reveal a manufacturing path without creating a mandatory research-point gate.

Recurring realm materials remain an option, but review the full consumption rate. A factory that continually exhausts manually gathered material can turn exploration into maintenance. Compare expedition yield, industrial consumption, trip duration and variety of encounters before committing to such a chain.

Open alternatives: finite caches, renewable encounters, distant new deposits, reusable rare components or mostly optional realm technology. Decide whether any realm visit is required for core aerospace/endgame progression, and ensure early access does not create an impossible repair/material dependency loop.

## 5. Distance, discovery and return journeys

Coordinate preservation protects geography, but distance still consumes player time. Widely separated Gates need useful exploration between them, readable environmental clues and a viable way to retrace discoveries.

Measure first-Gate discovery time, travel between meaningful discoveries, expedition return burden and the usefulness of maps/markers. Sparse structures should feel intriguing without making the main exploration system practically invisible to unlucky players.

Open: local vehicles before teleportation, personal waypoints, death recovery and expedition supplies. These are design questions, not added committed features. A realm with easier terrain may offer a faster journey despite equal X/Z distance; decide whether that is an acceptable reward, consistent with the existing transport direction.

## 6. Complete-game coverage

The POC proves selected architecture. A complete release also needs coherent player-facing systems and recovery paths. This table records coverage to design, not a promise that every possible feature will ship.

| Area | Complete experience to specify | Still open |
|---|---|---|
| Building and inventory | Reliable targeting, placement, mining, storage and understandable item handling | Tool wear, block recovery, bulk building and inventory conveniences |
| Survival and combat | Readable threats, player damage, death/respawn and recovery | Difficulty, hunger if any, death penalties and factory damage |
| Industry | Useful progression, maintainable layouts and understandable failures | Mechanical depth, power shortages, routing and resource renewal |
| Exploration and worlds | Distinct discoveries, navigation and rewarding expeditions | Realm rewards, biome/planet set and discovery pacing |
| Transport | First expedition, repeat travel, safe return and cargo failure handling | Fuel, pad rules, Gate repair and teleporter costs |
| Settlements and ecology | World inhabitants with clear behaviour and a reason to encounter them | Trading, farming, NPC interaction depth and persistence |
| Interface and accessibility | Rebinding, readable text/UI, clear feedback and usable options | Controller scope, UI scaling, reduced motion, audio cues and localization |
| Persistence and lifecycle | Create/load/save, pause/quit, settings and intelligible recovery | Backup policy, compatibility window and supported platforms |
| Multiplayer, after single-player foundations | Joining, ownership, permissions, disconnect/reconnect and shared-world behaviour | Hosting model, player counts, claims and cooperative factory ownership |
| Long-term motivation | Meaningful goals after reaching advanced industry | Megaprojects, optional mastery goals and whether any explicit completion milestone exists |

No mandatory final boss, quest story or finite ending is implied. “Complete” means the chosen scope works as a coherent game, including onboarding, failures, saves and late play.

## 7. Future gameplay validation

Use separate evidence for fun and technical correctness:

1. New player: find a resource and build something useful without external help.
2. Builder: modify a working factory and diagnose a deliberate blockage.
3. Explorer: discover and restore a Gate, understand endpoint damage and return safely.
4. Industrial player: establish a planetary supply route and recover from a blocked delivery.
5. Returning player: load an existing save and understand what continued, paused or changed.
6. Long-session player: retain meaningful choices beyond repeated bulk collection.

Record confusion, waiting, repetitive travel, failure recovery and motivation alongside frame time. These scenarios are future playtests; documentation review cannot declare them successful.

## 8. Minecraft-like world item behaviour

**Agreed direction:** basic moment-to-moment play should retain the familiar, readable feel of Minecraft where that interaction already works well. Rivet Reach differentiates itself through its own automation depth, technology progression, worlds, lore and visual identity rather than by making basic sandbox controls unnecessarily unfamiliar.

### Dropping items

Items can be thrown out of the player inventory into the world.

The intended feel is familiar sandbox behaviour:

- a normal drop action throws a small amount/one item from the selected stack;
- a modifier can drop the full stack;
- mined blocks, mob drops and manually discarded inventory become physical world-item entities;
- world items fall under gravity, collide with terrain and settle on the ground;
- walking close enough collects compatible items into the player's inventory;
- exact keys and controller bindings remain configurable rather than hard-coded to Minecraft's defaults.

A dropped stack is represented as **one world entity containing an item stack**, not one physics entity per individual item.

Conceptually:

```text
ItemStack
|- item id
|- count
|- item-specific data where required
```

The same logical item-stack model should be used by inventories, world-item entities and automated item logistics. A pipe transfer does not create a second kind of iron ore; it moves the same authoritative item identity/count through a different representation.

### Item merging

Nearby compatible world-item entities should merge into larger piles where legal.

Example:

```text
Stone x30 + nearby Stone x40
-> Stone x70
```

This preserves the familiar visual behaviour while reducing entity, renderer, collision and pickup overhead.

Compatibility must respect item identity and any item metadata that makes two stacks meaningfully different. Exact merge radius, cadence and maximum world-pile size are implementation/balance details to measure.

### Water interaction and buoyancy

Water should use a Minecraft-like block-fluid interaction model rather than expensive continuous fluid dynamics.

Flowing water applies current to physical entities, including dropped items.

Rivet Reach deliberately differs from modern Minecraft in one clear rule:

> **Dropped items sink in water by default.**

An item definition can opt into floating behaviour with a data property/tag:

```text
buoyant = true
```

If `buoyant` is false or absent, the item sinks while still being pushed horizontally by flowing water. If `buoyant = true`, the item receives a gentle upward buoyancy influence and can rise/float while still responding to the current.

Typical intended examples:

```text
wood / planks       -> buoyant = true
some plants/leaves  -> buoyant = true

stone               -> default false
ore                  -> default false
metal ingots         -> default false
machines             -> default false
```

The tag is a **content property**, not a special-case list embedded in world-item physics code.

The exact upward/downward force, drag and settling behaviour should be tuned for readability and playability rather than physical realism. Water should be useful for environmental interaction and simple player-made channels without replacing the dedicated item-pipe automation system.

### World items versus factory items

Maintain a clear distinction between presentation/simulation modes:

```text
World item
-> physical entity
-> visible
-> gravity / collisions / water
-> can settle, merge and be picked up

Inventory item
-> data in an inventory/container

Pipe item
-> logical network transfer
-> no authoritative loose physics entity travelling through the pipe
```

Optional moving icons/items visible inside pipes can be cosmetic only.

This keeps the world tactile and Minecraft-like while allowing large factories to scale without thousands of authoritative moving item entities.
