# Rivet Reach - Player Experience and Complete-Game Scope

> **Status:** brainstorming specification with working resolutions dated 2026-09-08. Existing pillars and world-item rules remain agreed direction. Sections 9-12 select the interaction, survival and water baseline under the user's request to resolve design gaps. No feature is implemented or playtested.

Related: [PROJECT_PLAN.md](PROJECT_PLAN.md), [LORE.md](LORE.md), [TRANSPORT.md](TRANSPORT.md), [DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md).

## 1. What responsive means

The intended game responds clearly to player actions while supporting large persistent systems. High average FPS alone does not establish responsiveness.

Working interaction contract (detailed in section 9):

- targeting, placement previews, hotbar changes and inventory actions acknowledge input promptly;
- an invalid action explains its relevant cause: obstruction, range, missing resource or incompatible port;
- machines expose why they stopped: no input, full output, insufficient power, control disabled or unavailable network region;
- long generation, travel and save operations show meaningful state and avoid apparently lost input;
- visual anticipation may be immediate, while authoritative state remains validated;
- sound, symbols and animation supplement colour; essential information remains readable with reduced effects.

Technical candidate budgets live in [SIMULATION.md](SIMULATION.md). Multiplayer latency handling will need separate validation when networking is designed.

## 2. First-session experience

**Proposed scenario:** a new player can discover a useful material, learn a recipe, make a tool, improve a process and understand a small automated chain without external documentation. Experienced players can move directly toward known capabilities.

The recipe browser should expose prerequisites and machine requirements without revealing every exploration secret. Discoverability must not become an invisible crafting lock. Default browsing shows discovered items and the immediate prerequisite chain of a pinned goal, using silhouettes for undiscovered optional realm artifacts. A player-selectable full-recipe view exposes every ordinary manufacturing recipe. Both views use the same unrestricted craft validation; neither reveals undiscovered world coordinates.

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

**Working reward decision:** use durable upgrades, reusable catalysts, unusual building options, discoveries and substantial expedition yields, as specified in [ECONOMY.md](ECONOMY.md). A discovered technique can reveal a manufacturing path without creating a mandatory research-point gate.

Core physical-world progression, including aerospace and ordinary teleporters, has no mandatory realm visit or realm-consumable supply dependency. Optional specialized machines may require additional durable recovered artifacts per new machine, but never consume them continuously to sustain throughput. Dismantling recovers those components. First-Gate repairs use surface-accessible salvage/substitutes so access cannot depend on already crossing the closed Gate.

## 5. Distance, discovery and return journeys

Coordinate preservation protects geography, but distance still consumes player time. Widely separated Gates need useful exploration between them, readable environmental clues and a viable way to retrace discoveries.

Measure first-Gate discovery time, travel between meaningful discoveries, expedition return burden and the usefulness of maps/markers. Sparse structures should feel intriguing without making the main exploration system practically invisible to unlucky players.

Personal waypoints and durable death caches are selected in section 10. Easier terrain may reward faster travel at equal X/Z distance. Local vehicles remain later content, not a dependency of safe return. Regional clues lead toward Gates under the distribution model in TRANSPORT.md.

## 6. Complete-game coverage

The POC proves selected architecture. A complete release also needs coherent player-facing systems and recovery paths. This table records coverage; selected baseline decisions are detailed in sections 9-12 and DELIVERY.md. Entries describe the remaining refinements, not permission to ignore the selected rules.

| Area | Complete experience to specify | Still open |
|---|---|---|
| Building and inventory | Reliable targeting, placement, mining, storage and understandable item handling | No starter tool wear; refine bulk building beyond settings copy and atomic recovery |
| Survival and combat | Readable threats, player damage, death/respawn and recovery | Tune health/combat; baseline has no hunger, durable death caches and no automatic factory raids |
| Industry | Useful progression, maintainable layouts and understandable failures | Extend mechanical depth and tune the selected shortage/routing/finite-ore model |
| Exploration and worlds | Distinct discoveries, navigation and rewarding expeditions | Realm rewards, biome/planet set and discovery pacing |
| Transport | First expedition, repeat travel, safe return and cargo failure handling | Fuel, pad rules, Gate repair and teleporter costs |
| Settlements and ecology | World inhabitants with clear behaviour and a reason to encounter them | Trading, farming, NPC interaction depth and persistence |
| Interface and accessibility | Rebinding, readable text/UI, clear feedback and usable options | Controller scope, UI scaling, reduced motion, audio cues and localization |
| Persistence and lifecycle | Create/load/save, pause/quit, settings and intelligible recovery | Test checkpoint/migration policy and later platform expansion |
| Multiplayer, after single-player foundations | Joining, ownership, permissions, disconnect/reconnect and shared-world behaviour | Implement claims/access and cooperative hosting under personal loader ownership |
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

Nearby world-item entities of the **same item type** merge into larger piles where legal. Different types remain separate piles and may occupy the same block; there is no one-item-type-per-cell restriction.

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

## 9. Working interaction specification

**Resolution dated 2026-09-08:** the following selects concrete behaviour for the previously broad responsiveness goal. Values are initial tuning parameters; rules and acceptance cases govern the first playable stages. Existing drop/sinking/buoyancy rules in section 8 remain unchanged.

### Movement and targeting

One voxel edge represents one metre. Start with a 0.6 m wide, 1.8 m tall player collision volume, 4.5 m/s walk, 6.5 m/s sprint and a jump reaching 1.6 blocks, explicitly chosen by the user for future half-block clearance. Half blocks themselves remain later work. Sprint starts with the dedicated sprint key or a double tap of the mapped Forward action. The double-tap gesture has an initial 0.3-second press-to-press window and stays active while forward is held; releasing forward, crouching, inspecting or opening a menu/inventory clears it. Sprint has no stamina meter in the initial survival rules. Crouch reduces height/speed and prevents stepping off an edge unless the player deliberately jumps. Water slows movement; swimming uses held vertical input. These values need feel testing rather than physical realism.

Target the authoritative voxel grid within 5 m, not a possibly stale render mesh. Display the selected face, block identity where discovered, placement ghost and mining progress. Mining uses a held action with duration from block hardness and tool capability; changing target resets uncommitted progress. There is no mandatory mouse-click-per-block repetition. Wood is hand-breakable; a wooden pick mines stone, and a stone pick mines starter ores. An inadequate tool reports the required capability and does not silently destroy ore without its expected drop.

The first tools do not wear out. Durability is deferred unless playtests establish a useful maintenance decision; it must not become a surprise prerequisite for early-loop completion. Mining permission and item yield are server-authoritative even though sound/selection feedback can begin locally.

### Placement and inventory

Placement uses the targeted face and a rotatable ghost. Rotate cycles through allowed orientations defined by the block/machine; ordinary machines initially rotate in four horizontal directions. Placement fails visibly when out of reach, obstructed, unsupported where support is required, protected, or overlapping a player or solid entity. Loose dropped items do not obstruct placement: they pop above the new block or move into a clear side. Inventory is consumed only when the world placement commits. Held repeat placement is rate-limited and uses the current target each time.

Use 12 hotbar slots plus 48 main slots as the initial configuration. Direct keys select the first ten slots; mouse wheel and rebindable next/previous actions reach all twelve. Initial stack limits: terrain blocks 500, raw resources 250, components 100, machines 10, unique equipment 1. These are content definitions, not hard-coded assumptions in inventory storage.

Support quick transfer, splitting, combining compatible stacks, drag placement and a full-inventory message. Pickup accepts as much of a pile as fits and leaves the remainder with its identity/timer. Normal drop releases one item; modified drop releases the stack. A 0.75-second owner pickup delay prevents instantly recollecting an intentional throw; another player may collect it under ordinary pickup rules. Initial automatic pickup radius is 1.5 m and requires no solid barrier between player and pile. Eligible players compete in a stable tick/entity order; a pickup transfers only actual accepted quantity. The delay/ownership metadata participates in legal merge compatibility.

### Machine footprint and dismantling

A machine definition declares anchor, occupied cells, allowed rotations and ports relative to that anchor. Initial machines may occupy one cell, but placement validation reserves the entire footprint atomically. A later multi-block machine uses one logical identity, not independent inventories in each occupied cell.

A wrench interaction toggles compatible side configurations and inspects ports. A deliberate dismantle action pauses the machine, cancels uncommitted reservations and returns the machine item, stored stacks and unprocessed recipe escrow. Previously spent fuel/energy is not refunded. Completed outputs remain outputs. If the player lacks space, recoverable items become ordinary world stacks at a safe nearby cell; a protected recovery parcel is used when no safe drop cell exists. Removing any occupied cell addresses the same dismantle command.

Copy/paste of machine settings is a construction convenience for the industrial stage. It copies compatible configuration only, never inventory or manufactured equipment. Full structure blueprints are later scope; their absence must not prevent placing a useful first factory.

## 10. Survival, death and expedition recovery

**Working default:** health-based survival with readable melee/ranged enemies, no hunger/starvation, no temperature/toxic-atmosphere gate and no automatic enemy raids on factories. Food restores health with a short use cooldown. Hostiles threaten players; baseline attacks do not dismantle machines or destroy terrain. Player tools/quarries can alter terrain under their explicit permissions.

On death, transfer carried inventory/equipment into one durable recovery cache and respawn at the player's bound shelter, or the world's safe starting spawn if that shelter is unavailable. A realm death respawns the player at their physical-world shelter; the cache remains in the realm with a known route/waypoint. Learned recipe visibility and discovered map markers remain. No XP penalty exists.

The cache cannot burn, despawn, merge with loose piles or cross a Gate automatically. It is stored in persistent world metadata without keeping chunks active. If the death point is inside a hazard or inaccessible cell, place the cache at the last recorded safe grounded position in that world; use the arrival sanctuary when there is no such position. Repeated deaths create distinct caches rather than replacing the previous one. Cache contents remain recoverable after another player edits surrounding terrain; inspection includes position and recovery guidance.

Only the owner can withdraw cache items by default; explicit cooperative permission can allow help. Reaching the cache is the consequence of death. A character cannot intentionally die to teleport carried cargo home. The cache is a deliberate exception to ordinary timed world drops, preserving the recently agreed physical-item behaviour for mining, mobs and manual dropping.

Personal waypoints and a death marker are available early. Maps record visited terrain; they do not reveal undiscovered Gates/ores. A handheld compass/coordinate readout and marker labels make return journeys practical. Earlier ordinary surface vehicles can be introduced in the expansion stage, but coordinate-preserving world travel does not depend on them. Easier realm terrain can reward a faster walk at the same distance; no coordinate scaling is introduced.

## 11. Initial world-water behaviour visible to players

Water occupies voxel cells as source or flowing water. Sources produce descending flow and a limited horizontal spread; water routes around simple block obstacles, pushes entities and can be redirected by construction. The initial horizontal reach is 7 cells from a source/falling column on one level. Flow does not form new sources merely because two sources are adjacent. Generated lakes/rivers contain authored seeded source cells; a placed bucket source is explicitly marked as a source too.

A bucket takes one source into a 10 L container and removes that placed/generated source cell; nearby sources may refill it with flowing water, which is not a newly created source. Emptying the bucket places one source in a legal empty cell. Interacting with a tank instead transfers 10 L from the bucket into available tank capacity atomically; if less than 10 L is free the operation is rejected without loss. Filling a bucket from a tank removes exactly 10 L and never creates extra fluid. Pumps read an eligible source as a renewable water intake at their defined rate; flowing water is not a pump source. Removing or blocking the intake stops it. This deliberate source abstraction makes water renewable without simulating an entire lake's volume or distant flow.

Source water is inexhaustible for pumping, but tank/pipe quantities are conserved after intake. A pipe leak/ejector cannot create a source for less than a full 10 L placement action. Ordinary industrial flow stays in buffers; visual pipe particles do not wet terrain.

Items continue to sink by default and float only with `buoyant=true`. Currents act on both. Channel transport needs physical piles and a physical intake/collector, with merge and pickup rules; pipes transfer inventory data directly. Channels remain a valid early construction choice. They lack filters, sealed routing and the controlled throughput of pipes; no arbitrary rule makes an otherwise valid water channel stop working because pipes are available.

## 12. Interaction acceptance cases

The locked first step uses the interaction and review contract in section 14. Test moving/jumping/crouching across chunk seams, mining beneath the player, rapid mining edits and looking beyond generated terrain. Placement/collision tests for newly placed blocks apply when building is selected for a later milestone.

Partial pickup, owner drop delay, full-inventory handling and configurable actions apply to the first step's real inventory. Functional recipes, water/item buoyancy, death recovery and durable save/reload remain later candidates, selected after the first-step review. Their full-game rules remain specified here and in [ECONOMY.md](ECONOMY.md).

When the industrial candidate is selected, test a furnace/boiler/crusher chain, one deliberate starvation/full-output fault, settings copy, rotated ports and dismantling with a full inventory. Show stop reasons on the machine, not only in a debug console.

Values are adjusted through recorded playtests. The current rules are complete enough to build the first increments without deciding every late-game vehicle, creature or recipe.

## 13. Settlements and cooperative-world baseline

Settlements provide recognizable inhabitants, shelter landmarks and a small trade catalogue; they are not required quest gates or walking lore encyclopaedias. Use local schedules and inventory offers that restock only through an explicit timed/source rule while eligible. No full offscreen civilization economy is required. Trades must not offer cheaper reverse recipes that generate unlimited metal. Farming supports food/healing and renewable plant materials; livestock use a small shared behaviour set rather than custom planet-wide simulations.

Cooperative worlds distinguish build/use/storage permissions from production-ticket ownership. Players can grant a group access to a base and its machines, but a loader keeps one explicit owner and quota account until transferred. Claims protect placed blocks, inventories and mining targets; they cannot appropriate a Gate sanctuary or block its return interaction. Unclaimed natural terrain remains editable under server rules. PvP is off by default for the initial cooperative profile. Changing those server rules is an explicit configuration choice, not an implicit consequence of multiplayer.

Late-game goals come from constructing settlements, expanding useful industrial capacity, completing optional artifact collections and building multi-world projects. The release progression must provide durable uses for advanced output; it does not require a narrative victory screen. Final trade catalogues, creatures, optional megaproject recipes and combat numbers remain content work.

## 14. Locked first-step interaction contract

**User scope decision, 2026-09-08:** the first playable step contains natural terrain, chunk streaming, FPS movement, fist mining, terrain-block placement, functional inventory with a crafting placeholder and a 3D player. No structures. [DEVELOPMENT_STRATEGY.md](DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence) owns the milestone boundary; the details below are working implementation defaults within it, not claims of tested feel or final visual approval.

### Player and controls

Start in first person at a safely prepared terrain spawn. Support mouse look, walk, sprint, jump and crouch using the section 9 movement defaults and rebindable actions. Spawn readiness and collision must precede player control. Provide usable pause/resume, mouse capture/release and inventory open/close behaviour. Opening inventory releases the pointer and suppresses movement/mining commands so clicking slots cannot also mine terrain; existing universe-time rules remain unchanged.

Use a real 3D player model with a body and visible first-person arms/fists. Basic idle, locomotion and mining animation must share one player state; animation never decides whether an authoritative block is removed. First-person presentation must avoid the camera seeing inside the head/body or fists obscuring the target. Provide a development inspection view for reviewing the complete model/animation; a player-facing third-person mode is not required. Model proportions, silhouette, material treatment and animation style are selected through the visual review, with asset conventions in CONTENT_PIPELINE.md.

### Fist mining and collection

Use the existing grid targeting/reach and held mining progress rules. Every ordinary block in the first terrain palette must be mineable with fists; use definition-driven durations and yields. This explicitly supersedes the wooden/stone-pick prerequisites in section 9 for this slice. Tools, ore progression and future special unbreakable blocks are later content, not hidden requirements for this first loop.

Give immediate fist/selection/progress feedback and simple readable impact feedback. Only a successful authoritative removal creates the defined physical item stack once. Use the existing dry-terrain drop, pickup, stack merging, sleeping and partial-transfer semantics. A full inventory leaves the uncollected amount in the world; repeated mining, pickup and inventory actions must not duplicate or silently erase material. Water interactions are deferred by the milestone boundary.

**User interaction revision, 2026-09-08:** fist-mined grass drops one **dirt** item; dirt and stone drop themselves. Mining removes the addressed voxel normally. A selected block rests on a flat upward-facing right palm, changes with the hotbar and disappears when that stack becomes empty. Fingers extend beneath the block like a tray. Handle-based item presentation uses a separate closed finger/thumb grip, with a two-hand support pose where appropriate; the current sword/pickaxe assets are cosmetic review props, not gameplay tools. Empty-hand and block-held mining use a quick diagonal right-hand sweep, with a complete recovery after release; successful placement triggers a single swing. The original Rivet Reach rig/textures implement this familiar voxel-sandbox behaviour. Holding a terrain block grants no tool speed or extra reach. First person renders the dominant arm; the full player and its shadow retain both arms.

### Grass growth — user feedback extension

Grass spreads gradually to nearby dirt on random fixed ticks when source and destination have light and the destination has an open top. Covering grass causes it to decay to dirt on a later random tick. The first implementation uses direct sky exposure in the current opaque-terrain/daylight world: a roof anywhere above the column blocks growth, and removing it restores eligibility. Indirect skylight, artificial lights and light-filtering materials require the later voxel-light system. This is an explicit limitation, not a claim of exact Minecraft light propagation.

The working neighbourhood includes horizontal and diagonal neighbours one block away, up one or down three levels, allowing grass to move across slopes. Timing is randomized rather than an immediate flood. Nearby resident terrain ticks during Play and inventory; paused or distant/unloaded terrain does not accrue catch-up. Conversion changes terrain state without producing or consuming items, and survives chunk unload/reload within the session. [SIMULATION.md](SIMULATION.md#13-grass-random-ticks--first-step-feedback) owns scheduling and mutation details. Artificial grass collection/tools remain later work; ordinary fist mining therefore supplies dirt for building.

### Inventory and crafting placeholder

Deliver real hotbar/main inventory storage using stable item definitions and section 9's initial slot/stack configuration. Display collected item identity and count; support selection, moving stacks, splitting, combining, quick transfer, manual dropping and clear full-inventory feedback. The selected stack remains separate from fist mining. The user’s subsequent POC feedback explicitly adds placement of collected grass, dirt and stone in this step; the contract below supersedes the original placement exclusion.

Reserve a visible area inside the inventory labelled **Crafting — coming later**. It is a UI/layout placeholder only: no functional recipes, ingredient escrow, crafted output, workbench or recipe browser. Placeholder slots accept no real items and never consume or trap stacks. Keep the inventory's actual storage separate so later crafting can connect to the real item model.

### Playable review cases

- Start a terrain session, walk/sprint/jump/crouch and cross chunk seams using normal controls.
- Mine nearby terrain with fists, including at chunk edges and underfoot; confirm targeting, fist motion, visible progress, removal and collision agree.
- Collect the drops, move/split/merge/drop stacks, fill inventory and pick up only the amount that fits.
- Place collected terrain against reachable block faces, verify rejected overlaps consume nothing, leave/reload the area, then remine a placed block and recover exactly one item.
- Open/close inventory while mining; no world edit leaks through UI input and no progress commits against a stale target.
- Inspect the player body and first-person fists, then review terrain, lighting, item icons and inventory together for a coherent visual concept.
- Verify the crafting area is visibly unavailable and cannot change item totals.
- Leave a mined area until its runtime chunk representation unloads, then return and confirm the session's terrain edits and unexpired items remain correct.

The first POC now implements this loop. [Current revision results](verification/VISUAL_REVISION_RESULTS.md) record the automated and visual checks actually performed; user assessment of feel and final visual acceptance remains pending. Decide the next implementation with the user after reviewing this slice.

### First-step world seed — user feedback extension

Normal application/Editor Play startup prepares a fresh random seed. The optional title-screen seed field is blank by default; starting with it blank uses that prepared world. An explicitly entered signed 32-bit integer recreates the corresponding terrain. Returning through settings or appearance preserves the typed seed. F12 diagnostics expose the active seed for replay. Automated terrain regression explicitly enters its fixed seed and does not set the normal startup default.

### First-step terrain placement — user feedback extension

Use the opposite mouse button from mining (right mouse by default). Aim at an existing block within 5 m; the voxel ray supplies the entered face and therefore the adjacent destination cell. Following the user's later Minecraft-style interaction request, show one thin dark outline around the aimed existing block, with consistent screen-space thickness and normal terrain occlusion. This supersedes the green/red destination wireframe. Destination validation still occurs on every placement attempt, with the reason shown when rejected. These three terrain blocks have no directional state, so rotation is unnecessary; machinery orientation and footprints remain later work.

Validate the current selected stack, destination residency, empty occupancy and overlap with the player on each attempt. Use the same occupied-cell boundary and 1 mm skin as movement, so touching feet do not reject a block that fits below them. Loose items are allowed in the destination; after the voxel commits, move overlapping piles above it or into a clear side, waking their movement without changing their item identities, quantities, pickup delays or lifetimes. Different item types may share the destination and escape space. Reject placement while an inventory/menu is open, while inspecting the body, or when there is no reachable target face. Leave quantities unchanged on rejection. Player-overlap rejection is silent in the normal HUD and recorded in the F12 debug panel; other actionable placement failures retain their HUD feedback. [The play guide](FIRST_POC.md#f12-debug-panel) describes panel contents and controls. A successful local authority turn commits one terrain voxel and immediately consumes one item from the selected stack. Successful held placements repeat at most once per 0.22 seconds using the current target; a failed attempt does not spend this cooldown. Holding placement while jumping can therefore fill the space as soon as the feet clear it. Release resets the repeat timer. Mining and placing simultaneously gives placement priority.

Placed terrain uses the same session edit records, immediate voxel collision and revision-aware neighbour remeshing as mining. It survives chunk unload/reload and origin shifts in-session. Mining it follows the ordinary hardness/drop path. There is no creative/free block source, generated structure placement, blueprint building, undo, terrain gravity or durable save in this extension. The 0.22-second repeat delay and overlap margins are working defaults, not user-validated feel.

## 15. Player skins

**Explicit user direction, 2026-09-08:** players must be able to change their skin, with the approachable customization of a voxel sandbox. The character concept shown during brainstorming is one possible skin; it does not fix every player's face, skin colour or outfit.

**Additional explicit user direction, 2026-09-08:** offer both a male and a female player model, with player choice between them. Both support changeable skins. Model choice and skin choice are separate appearance settings in the first playable slice; this is not a request for arbitrary body-shape sliders.

**Working design:** a skin changes the texture appearance of the selected reusable player body/rig. Face details, colours and painted clothing can change; body shape, collision, reach, movement and mining capability stay the same. The selected appearance must also update first-person arms/fists and any player preview. A painted glove or sleeve is cosmetic and does not equip a tool or grant protection. Texture skins cannot change the underlying hair/clothing silhouette; asset constraints are in [CONTENT_PIPELINE.md](CONTENT_PIPELINE.md#6-player-skin-authoring-contract).

For the first playable slice, the working minimum is a small local skin selector with a preview and two visibly different skins demonstrated on each of the male and female models. Use a compatible shared rig/animation contract and skin layout, verifying both models rather than assuming the fit. Model choice must not change collision dimensions, camera eye height, reach, movement speed or mining capability. First-person hands and the preview follow both selections. Apply selection without restarting the terrain session or losing inventory. Remember model and skin selections as local player preferences independently of the first slice's session-only world state; a missing skin falls back visibly to the default. This narrowly extends the earlier exclusion of all character customization. A body editor, skin-painting tool, marketplace and account service remain outside the first step.

Allowing players to supply their own PNG skin using a published Rivet Reach template is the proposed custom-skin workflow. Decide its exact layout/resolution and whether file import ships in this first slice during the player visual review; basic skin switching and a reusable layout must work regardless. Future multiplayer must show each player's selected skin consistently, but upload/distribution limits, caching and server policy belong to the later multiplayer implementation. The first POC implements the two-skin selector and both model variants. Custom-file import and multiplayer distribution remain unimplemented. See [FIRST_POC.md](FIRST_POC.md#actual-first-visual-kit) for the initial shared texture layout and its limitations.
