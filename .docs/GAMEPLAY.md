# Rivet Reach - Player Experience and Complete-Game Scope

> **Status:** brainstorming specification with working resolutions dated 2026-09-08. Existing pillars and world-item rules remain agreed direction. Sections 9-12 select the interaction, survival and water baseline under the user's request to resolve design gaps. Subsequent authorized increments are implemented; [current verification](verification/README.md) distinguishes measured evidence from remaining play review.

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

The current arcade presentation request adds dynamic cosmetic feedback to the playable slice. [The presentation contract](CONTENT_PIPELINE.md#arcade-presentation-and-dynamic-feedback) owns its style and effect boundaries; [runtime evidence](verification/ARCADE_VISUAL_RESULTS.md) separates checks from artistic acceptance.

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

Use 12 hotbar slots plus 48 main slots as the initial configuration. Direct keys select the first ten slots; mouse wheel and rebindable next/previous actions reach all twelve. The selected survival content uses 64-item ordinary stacks and one-item tool/armor stacks. Larger industrial stack tuning remains a future decision. These are content definitions, not hard-coded assumptions in inventory storage.

Support quick transfer, splitting, combining compatible stacks, drag placement and a full-inventory message. Pickup accepts as much of a pile as fits and leaves the remainder with its identity/timer. Normal drop releases one item; modified drop releases the stack. A 0.75-second owner pickup delay prevents instantly recollecting an intentional throw; another player may collect it under ordinary pickup rules. Automatic pickup radius is 1.7 blocks (1.7 m), as explicitly requested in subsequent user feedback, and pickup requires no solid barrier between player and pile. Eligible players compete in a stable tick/entity order; a pickup transfers only actual accepted quantity. The delay/ownership metadata participates in legal merge compatibility.

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

The implemented [world-fluid rules](FLUIDS.md#playable-water-rules) now own source/flow behaviour, bucket use, two-source renewal, player immersion and item currents. The user explicitly confirmed `RenewsSources` as a per-liquid boolean: water enables it; future liquids may disable it. This supersedes the earlier proposal that adjacent sources could not renew water.

The current bucket moves one source cell. The future industrial model maps that placement to 10 L, conserves tank/pipe contents after intake and treats eligible source-water pumping as renewable. Tanks, pumps, pipe ejection and physical collectors remain later scope; they must reuse fluid identity and source policy rather than introducing a separate water type.

Future industrial interaction contract: transferring a bucket to a tank requires the full 10 L of available capacity or rejects without loss; filling from a tank removes exactly 10 L. Pumps require an eligible source and stop when it is removed or blocked. Flowing water is not an intake source. A pipe leak/ejector needs a full 10 L placement action to create a source; ordinary buffer transfers and visual particles do not wet terrain. Physical water channels remain a valid transport option with physical collectors and pile merge/pickup rules. Pipes provide filters, sealed routing and controlled throughput rather than arbitrarily disabling channels.

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

**User scope decision, 2026-09-08:** the first playable step contains natural terrain, chunk streaming, FPS movement, fist mining, terrain-block placement, functional inventory/crafting and a 3D player. No structures. [DEVELOPMENT_STRATEGY.md](DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence) owns the milestone boundary; the details below are working implementation defaults within it, not claims of tested feel or final visual approval.

### Player and controls

Start in first person at a safely prepared terrain spawn. Support mouse look, walk, sprint, jump and crouch using the section 9 movement defaults and rebindable actions. Spawn readiness and collision must precede player control. Provide usable pause/resume, mouse capture/release and inventory open/close behaviour. Opening inventory releases the pointer and suppresses movement/mining commands so clicking slots cannot also mine terrain; existing universe-time rules remain unchanged.

Use a real 3D player model with a body and visible first-person arms/fists. Basic idle, locomotion and mining animation must share one player state; animation never decides whether an authoritative block is removed. First-person presentation must avoid the camera seeing inside the head/body or fists obscuring the target. Provide a development inspection view for reviewing the complete model/animation; a player-facing third-person mode is not required. Model proportions, silhouette, material treatment and animation style are selected through the visual review, with asset conventions in CONTENT_PIPELINE.md.

### Fist mining and collection

Use the existing grid targeting/reach and held mining progress rules. Every ordinary block in the first terrain palette must be mineable with fists; use definition-driven durations and yields. This explicitly supersedes the wooden/stone-pick prerequisites in section 9 for this slice. The subsequent [ore/bedrock extension](#17-ore-mining-and-bedrock) adds pickaxe-only ores and an unbreakable base without changing fist eligibility for this original palette. Broader tool progression remains later content.

Give immediate fist/selection/progress feedback and simple readable impact feedback. Only a successful authoritative removal creates the defined physical item stack once. Use the existing dry-terrain drop, pickup, stack merging, sleeping and partial-transfer semantics. A full inventory leaves the uncollected amount in the world; repeated mining, pickup and inventory actions must not duplicate or silently erase material. Water interactions are deferred by the milestone boundary.

**User interaction revision, 2026-09-08:** fist-mined grass drops one **dirt** item; dirt drops itself. The later survival extension makes stone pickaxe-only and changes its drop to cobblestone. Mining removes the addressed voxel normally. A selected block rests on a flat upward-facing right palm, changes with the hotbar and disappears when that stack becomes empty. Fingers extend beneath the block like a tray. Handle-based item presentation uses a separate closed finger/thumb grip, with a two-hand support pose where appropriate; the original grip-review models also supply the selectable starter dagger and pickaxe. Empty-hand and block-held mining use a quick diagonal right-hand sweep, with a complete recovery after release; successful placement triggers a single swing. The 2026-09-10 [avatar rework](AVATAR_REWORK.md#first-person-presentation) now uses tool-specific authored anticipation/contact/recovery and playback speeds instead of the earlier blanket 2.5× speed. Animation cadence remains independent of definition-driven break durations. The original Rivet Reach rig/textures implement this familiar voxel-sandbox behaviour. Holding a terrain block grants no tool speed or extra reach. First person renders the dominant arm; the full player and its shadow retain both arms.

### Grass growth — user feedback extension

Grass spreads gradually to nearby dirt on random fixed ticks when source and destination have light and the destination has an open top. Covering grass causes it to decay to dirt on a later random tick. The first implementation uses direct sky exposure in the current opaque-terrain/daylight world: a roof anywhere above the column blocks growth, and removing it restores eligibility. Indirect skylight, artificial lights and light-filtering materials require the later voxel-light system. This is an explicit limitation, not a claim of exact Minecraft light propagation.

The working neighbourhood includes horizontal and diagonal neighbours one block away, up one or down three levels, allowing grass to move across slopes. Timing is randomized rather than an immediate flood. Nearby resident terrain ticks during Play and inventory; paused or distant/unloaded terrain does not accrue catch-up. Conversion changes terrain state without producing or consuming items, and survives chunk unload/reload within the session. [SIMULATION.md](SIMULATION.md#13-grass-random-ticks--first-step-feedback) owns scheduling and mutation details. Artificial grass collection/tools remain later work; ordinary fist mining therefore supplies dirt for building.

### Trees and axe felling — user feedback extension

**Explicit user direction, 2026-09-08:** natural terrain also generates voxel trees made of logs and leaves. Logs remain hand-breakable. Breaking a generated tree log with an axe or an axe-type tool breaks the connected generated logs at or above that cut; bare hands, held blocks and other tool types remove only the targeted block. The user explicitly corrected the initial behaviour after losing base wood: player-placed logs must always be mined individually, including with an axe.

Working implementation: one small tree shape with a four-to-six-block vertical trunk and a layered block canopy. Generation is seeded and independent of chunk discovery order, with clear space around the initial spawn. Logs and leaves are solid, targetable inventory blocks and can be placed with the existing placement action. Every manually mined block yields one matching block item, including leaves in this initial palette. Axe felling yields one physical log item per successful log removal, with ordinary pickup/overflow behaviour.

The felling search follows face-connected logs while staying at or above the original cut height, including connected generated branches. It never crosses leaves or air, and it leaves the stump below the cut intact. Player-placed logs never participate or connect the search to other wood, including logs placed at former generated-log coordinates. Every queued log rechecks its generated provenance before removal. Diagonal-only contact does not connect the search. Axe eligibility is a definition capability flag, so future combined tools can opt in without matching an item name. Switching the selected item resets unfinished mining progress. The cut uses normal reach/held-action targeting; its resulting felling operation continues across unloaded upper chunks through session edits.

Natural leaves decay after losing support through a connected leaf path to a nearby log. Player-placed leaves remain until mined. Decay produces no items in this slice. Initial tuning is a four-cell support distance and a two-second minimum delay, with bounded queued processing; exact timing and tree density remain review choices. Leaves use an opaque leafy block texture and transmit daylight for the existing grass rules. Saplings, regrowth, fruit, transparent leaf cutouts and full voxel light attenuation remain future content decisions.

The earlier tree review supplied a starter dagger, pickaxe and axe. The selected [survival progression](#survival-progression-farming-health-and-armor) now starts normal sessions empty-handed and provides craftable tool tiers. Legacy starter identities remain only for explicit verification fixtures. Use the mouse wheel, `[ / ]`, or inventory to select tools; each has a one-item stack limit. The axe has an original textured model and one-hand grip, positioned lower and right with the cutting edge facing the strike direction. Pickaxes use the existing two-hand grip. Only axe capability fells generated trees; other tools mine one log at a time. Tool grade changes speed according to [ECONOMY.md](ECONOMY.md#current-survival-recipes-and-tiers). Tool wear remains unimplemented; tools cannot be placed as terrain voxels.

Review generated trees at chunk seams, cut a trunk in its middle with hands and an axe, check mixed tool capabilities, item conservation, nearby trees with touching leaves, leaf decay, player-placed foliage, and unload/reload during felling. See [tree verification](verification/TREE_RESULTS.md) for measured evidence and remaining review.

### Inventory and crafting

Deliver real hotbar/main inventory storage using stable item definitions and section 9's initial slot/stack configuration. Display collected item identity and count; support selection, moving stacks, splitting, combining, quick transfer, manual dropping and clear full-inventory feedback. The selected stack remains separate from fist mining. The user’s subsequent POC feedback explicitly adds placement of collected grass, dirt and stone in this step; the contract below supersedes the original placement exclusion.

The personal 2×2 grid and placed workbench 3×3 interface use the [modular crafting rules](#16-modular-grid-crafting). Ingredients and outputs are real item stacks; the original nonfunctional placeholder has been removed.

### Playable review cases

- Start a terrain session, walk/sprint/jump/crouch and cross chunk seams using normal controls.
- Mine nearby terrain with fists, including at chunk edges and underfoot; confirm targeting, fist motion, visible progress, removal and collision agree.
- Collect the drops, move/split/merge/drop stacks, fill inventory and pick up only the amount that fits.
- Place collected terrain against reachable block faces, verify rejected overlaps consume nothing, leave/reload the area, then remine a placed block and recover exactly one item.
- Open/close inventory while mining; no world edit leaks through UI input and no progress commits against a stale target.
- Inspect the player body and first-person fists, then review terrain, lighting, item icons and inventory together for a coherent visual concept.
- Verify crafting through the subsequently authorized [section 16 interaction cases](#16-modular-grid-crafting).
- Leave a mined area until its runtime chunk representation unloads, then return and confirm the session's terrain edits and unexpired items remain correct.

The first POC now implements this loop. [Current revision results](verification/TERRAIN_GENERATION_RESULTS.md) record the automated and visual checks actually performed; user assessment of feel and final visual acceptance remains pending. Decide the next implementation with the user after reviewing this slice.

### First-step world seed — user feedback extension

Normal application/Editor Play startup prepares a fresh random seed. The optional title-screen seed field is blank by default; starting with it blank uses that prepared world. An explicitly entered signed 32-bit integer recreates the corresponding terrain. Returning through settings or appearance preserves the typed seed. F12 diagnostics expose the active seed for replay. Automated terrain regression explicitly enters its fixed seed and does not set the normal startup default.

### First-step terrain placement — user feedback extension

Use the opposite mouse button from mining (right mouse by default). Aim at an existing block within 5 m; the voxel ray supplies the entered face and therefore the adjacent destination cell. Following the user's later Minecraft-style interaction request, show one thin dark outline around the aimed existing block, with consistent screen-space thickness and normal terrain occlusion. This supersedes the green/red destination wireframe. Destination validation still occurs on every placement attempt, with the reason shown when rejected. These three terrain blocks have no directional state, so rotation is unnecessary; machinery orientation and footprints remain later work.

Validate the current selected stack, destination residency, empty occupancy and overlap with the player on each attempt. Use the same occupied-cell boundary and 1 mm skin as movement, so touching feet do not reject a block that fits below them. Loose items are allowed in the destination; after the voxel commits, move overlapping piles above it or into a clear side, waking their movement without changing their item identities, quantities, pickup delays or lifetimes. Different item types may share the destination and escape space. Reject placement while an inventory/menu is open, while inspecting the body, or when there is no reachable target face. Leave quantities unchanged on rejection. Player-overlap rejection is silent in the normal HUD and recorded in the F12 debug panel; other actionable placement failures retain their HUD feedback. [The play guide](FIRST_POC.md#foundation-and-diagnostics) describes panel contents and controls. A successful local authority turn commits one terrain voxel and immediately consumes one item from the selected stack. Successful held placements repeat at most once per 0.22 seconds using the current target; a failed attempt does not spend this cooldown. Holding placement while jumping can therefore fill the space as soon as the feet clear it. Release resets the repeat timer. Mining and placing simultaneously gives placement priority.

Placed terrain uses the same session edit records, immediate voxel collision and revision-aware neighbour remeshing as mining. It survives chunk unload/reload and origin shifts in-session. Mining it follows the ordinary hardness/drop path. There is no creative/free block source, generated structure placement, blueprint building, undo, terrain gravity or durable save in this extension. The 0.22-second repeat delay and overlap margins are working defaults, not user-validated feel.

## 15. Player skins

**Explicit user direction, 2026-09-08:** players must be able to change their skin, with the approachable customization of a voxel sandbox. The character concept shown during brainstorming is one possible skin; it does not fix every player's face, skin colour or outfit.

**Additional explicit user direction, 2026-09-08:** offer both a male and a female player model, with player choice between them. Both support changeable skins. Model choice and skin choice are separate appearance settings in the first playable slice; this is not a request for arbitrary body-shape sliders.

**Working design:** a skin changes the texture appearance of the selected reusable player body/rig. Face details, colours and painted clothing can change; body shape, collision, reach, movement and mining capability stay the same. The selected appearance must also update first-person arms/fists and any player preview. A painted glove or sleeve is cosmetic and does not equip a tool or grant protection. Texture skins cannot change the underlying hair/clothing silhouette; asset constraints are in [CONTENT_PIPELINE.md](CONTENT_PIPELINE.md#6-player-skin-authoring-contract).

For the first playable slice, the working minimum is a small local skin selector with a preview and two visibly different skins demonstrated on each of the male and female models. Use a compatible shared rig/animation contract and skin layout, verifying both models rather than assuming the fit. Model choice must not change collision dimensions, camera eye height, reach, movement speed or mining capability. First-person hands and the preview follow both selections. Apply selection without restarting the terrain session or losing inventory. Remember model and skin selections as local player preferences independently of the first slice's session-only world state; a missing skin falls back visibly to the default. This narrowly extends the earlier exclusion of all character customization. A body editor, skin-painting tool, marketplace and account service remain outside the first step.

Allowing players to supply their own PNG skin using a published Rivet Reach template is the proposed custom-skin workflow. Decide its exact layout/resolution and whether file import ships in this first slice during the player visual review; basic skin switching and a reusable layout must work regardless. Future multiplayer must show each player's selected skin consistently, but upload/distribution limits, caching and server policy belong to the later multiplayer implementation. The first POC implements the two-skin selector and both model variants. Custom-file import and multiplayer distribution remain unimplemented. See [FIRST_POC.md](FIRST_POC.md#survival-and-exploration) for the initial shared texture layout and its limitations.

## 16. Modular grid crafting

**Explicit user extension, 2026-09-08:** implement modular, easily editable recipes as a core system supporting 2×2, 3×3 and 4×4 grids. Activate the existing personal 2×2 inventory area. The personal 2×2 and placed workbench 3×3 interfaces are active. The future 4×4 interface already has core support.

**Working interaction rules:** move, split, combine, swap and drag ingredients using the same controls as inventory. Shaped recipes may move anywhere they fit in a larger grid; internal empty cells remain empty, mirroring is per-recipe, and rotation is not implicit. Shapeless recipes accept any cell arrangement with the exact occupied-slot ingredients. Extra inputs reject the match. Minimum grid size is an independent recipe requirement. No knowledge or XP flag gates the registered recipes.

The result slot previews the actual output bundle without owning or creating an item. Left- or right-click crafts one complete recipe into a compatible cursor stack; an incompatible/full cursor consumes nothing. Shift-click crafts as many complete results directly into inventory as available ingredients and inventory space allow, even while holding an item on the cursor. The held stack stays unchanged. This applies to personal crafting and crafting benches. Insufficient destination space, changed ingredients or no matching recipe consumes nothing. Dragging a result to an inventory slot crafts once. The preview immediately updates when inputs change or are consumed.

Shift-clicking an ingredient returns what fits to inventory. **Return ingredients** and closing inventory do the same for the whole grid. Any remainder stays in that session's grid and is visible when inventory reopens; it is never deleted or dropped by ingredient return. Existing cursor-on-close behavior still returns the cursor stack and physically drops overflow. Appearance/menu transitions that close inventory also return its ingredients. UI rebuilds do not own/reset grid contents. World and inventory state, including retained crafting ingredients, still reset on application exit.

The personal **Recipes** guide reads the same compiled catalog and shows available layouts, counts and mirroring/shapeless rules. It is a small current-station guide, not the full discovery/uses graph. The current starter set and its working balance are owned by [ECONOMY.md](ECONOMY.md#current-survival-recipes-and-tiers). Authoring, matching, conflict rules and future extension boundaries are owned by [CRAFTING.md](CRAFTING.md).

Review normal click/right-click/drag input, craft-one and batch output, full/partially full destinations, empty/incompatible cursor states, extra ingredients, stale previews, and closing/reopening with a full inventory. All grid sizes have core tests for translation, mirroring, quantity consumption and size gates. [Crafting verification](verification/CRAFTING_RESULTS.md) records current evidence; player assessment of usability and recipe balance remains pending.


## 17. Ore mining and bedrock

**Explicit user extension, 2026-09-08:** generate multiple ores at different levels, with an unbreakable bedrock base. [ECONOMY.md](ECONOMY.md#current-ore-generation-and-bedrock) owns the ore selection, height bands, yields and current processing boundary.

Explore and dig to discover coal, copper, iron, gold and diamond veins. Craft and select a pickaxe, then hold Mine within the existing 5 m reach. Coal requires wood grade, copper/iron require stone grade, and gold/diamond require iron grade. Bare hands, unsuitable capabilities and insufficient tiers cannot remove ore or build mining progress; the target label names the required pickaxe grade. The world mining command checks both capability and tier. Changing tools or targets resets accumulated work. [The progression table](ECONOMY.md#current-survival-recipes-and-tiers) owns current speed multipliers and recipes; tool wear remains unimplemented.

A successful extraction removes one voxel and produces exactly one matching physical resource stack through the existing drop/pickup path. Resources have distinct names, icons and held swatches. They support inventory movement, splitting and dropping, but cannot be placed as ore or terrain. A repeated stale mining command produces nothing. Mined veins stay depleted when unloaded and revisited within the session; quitting still resets world progress.

Bedrock at Y = −256 is a continuous solid floor. Targeting it shows **Unbreakable**. Every tool, direct removal and placement reject changes to this layer without progress, item consumption or drops. Player collision prevents descent through it. Stone now requires a wooden pickaxe or better and yields cobblestone; soil, logs and leaves retain their hand-mining and placement behavior. F12 coordinates help review the ore levels; no scanner or map markers are added.


## Day, night and lunar phases

**Explicit user extension, 2026-09-09:** implement a day/night system with a moving sun and moon, and a different lunar phase each night. This is an authorized addition to the current playable slice.

**Working defaults:** a complete day lasts 20 minutes of running simulation, beginning at 08:00 on Day 1. Sunrise is at 06:00 and sunset at 18:00. The sun travels east to west; the moon follows the opposite arc at night. Sky colors, clouds, directional shadows, ambient light and distant fog blend through dawn/day/dusk/night. Stars appear after dusk. Moonlight is strongest at full moon and absent at new moon; a low ambient floor keeps terrain readable. These durations, colors and visibility levels remain tuning choices awaiting player review.

Eight phases repeat over eight nights: new moon, waxing crescent, first quarter, waxing gibbous, full moon, waning gibbous, last quarter and waning crescent. The first night is full. The phase advances at dawn, preserving one phase across the whole night, including midnight. The moon is an illuminated procedural disk with distinct waxing/waning silhouettes and a faint dark side. Sun/moon positions use a stylized opposed orbit so the moon crosses the night sky at every phase; this is not an astronomical orbital simulation.

The HUD shows the civil day, time and phase. Civil days change at midnight. Play and inventory advance time; title/pause/settings/appearance/controls freeze it with the existing universe-time rule. A new world starts the clock over; quitting has no offline catch-up or durable clock save in this session-only slice. [Simulation contract](SIMULATION.md#session-world-clock-and-celestial-presentation) owns the API and update boundary. [Day/night verification](verification/DAY_NIGHT_RESULTS.md) records actual evidence and remaining integration review.


## Native creature combat and spawning

The user explicitly authorized mobs, Blender-authored enemies, AI and spawning on 2026-09-09, choosing Rustback beetle and Dusk prowler. This supersedes the earlier mob/combat exclusion for this bounded extension. [MOBS.md](MOBS.md) owns the species, working combat defaults, natural spawning, voxel movement, lifecycle, asset contract and verification workflow. Mob attacks use the shared survival health/armor/death authority and prowler spawning reads the authoritative day/night clock. This is not authorization for factory raids, terrain-destroying creatures, broader ecology or durable mob saves.

## Survival progression, farming, health and armor

**Block interaction:** aim at a visible workbench, furnace or chest within five-block targeting reach and press **E** (rebindable Interact) or mouse Use (right-click by default). The workbench displays nine crafting input cells. Crouch + mouse Use places against a station; keyboard Interact explicitly opens it. The HUD names the current key and mouse button. Obstructed, out-of-reach or unloaded station commands are rejected.

**Selected by the user on 2026-09-09**, extending the initial crafting request with basic recipes, five tool tiers, furnaces, potatoes, farming, hunger, health and armor. This section supersedes earlier statements that these features are deferred. The broader game vision and later systems remain intact. [ECONOMY.md](ECONOMY.md#current-survival-recipes-and-tiers) owns recipe quantities and tier tables; [CRAFTING.md](CRAFTING.md#survival-progression-extension--2026-09-09) owns architecture and authoring.

- Start empty-handed. Gather logs, craft planks, sticks and a workbench in the personal 2×2 grid. Place and use the workbench for 3×3 tools, stations, armor and storage blocks. Stone requires a wooden pickaxe or better; later ore extraction follows the displayed grade requirement. Below-grade attempts preserve the resource. Supplied starter tools and their temporary personal recipes are retired from ordinary play.
- Use a workbench, furnace or chest within reach to open it. Crouch-use permits placing against a station instead. Furnace ingredients, fuel and results are separate filtered slots; result slots never accept inserted items. Shift-click moves station stacks; dragging logs to the fuel slot selects burning instead of charcoal production. Closing returns held items and workbench ingredients where they fit; station contents and remaining ingredients survive interface closure and chunk unloading in this session.
- Find ripe wild potato plants, harvest by hand and keep potatoes for planting. Use any hoe on exposed grass/dirt to create farmland, then use a potato on it to plant. Crops have four visible stages and do not block movement. A ripe harvest yields 2–4 potatoes; immature harvest returns its seed potato. Removing the supporting soil uproots the crop and returns its harvest. No crop GameObject, per-frame crop update, irrigation or fertilizer system is introduced. Growth uses loaded terrain and geometric skylight; its current 60-second stages and lack of hydration are working defaults, not full Minecraft farming equivalence.
- Hunger has 20 food points. Walking uses 0.01 exhaustion per metre, sprinting 0.1, jumping 0.2 and successful mining/tilling 0.05; four exhaustion spend one food point. Food at 6 or below prevents sprinting. Hold Use for 1.2 seconds to eat a held raw potato (+1) or baked potato (+5). Full hunger does not consume food. Planting on farmland takes precedence over eating.
- Health has 20 points, shown as ten hearts. At food 18 or higher, missing health regenerates one point every four seconds and costs six exhaustion. At zero food, starvation removes one health point every four seconds down to one; further damage can kill. Falling more than three blocks deals one point per additional whole block. Inventory screens keep simulation running; pause/death screens stop it.
- Copper, iron and diamond armor have head, chest, legs and feet slots. Drag the matching piece into a slot, or shift-click it in the personal/workbench inventory. Each armor point reduces ordinary impact/creature damage by 4%, capped at 80%. Fall and starvation damage bypass armor. Equipment affects survival independently of the male/female model and skin choice. Armor currently appears in equipment slots and held-item presentation; fitted armor meshes and equipment wear remain future visual/content work.
- Lethal damage drops carried inventory, personal-grid ingredients and equipped armor exactly once, shows a death screen and offers Respawn. World construction, station contents, crops and edits remain. Respawn restores health/food, resets movement and grants two seconds of damage immunity. `Expedition.TakeDamage` is the common health authority for creature attacks and other damage sources; `Respawned` lets creature behavior clear stale pursuit independently. Drops retain the existing pickup/despawn rules.

These controls and values form the current playable survival slice. Tool durability, enchantments, irrigation, breeding, armor cosmetics and durable saves are not represented as implemented. [Survival verification](verification/SURVIVAL_RESULTS.md) records checks and outstanding review.

## Terrain and biome rework — 2026-09-09

The user-requested terrain rework adds five distinct landscape profiles, naturally exposed entrances, connected passages and caverns. Sand, sandstone, snow and red clay follow the existing mining/drop/placement paths. [TERRAIN_GENERATION.md](TERRAIN_GENERATION.md) owns the surface/cave/material defaults, safe start and limits; [terrain verification](verification/TERRAIN_GENERATION_RESULTS.md) records actual checks. Bedrock remains unbreakable and existing ore Y bands remain in force. Terrain generation supplies the mature wild-potato locations agreed with survival; survival owns harvesting and growth.

## Creative testing mode

The user authorized a session-only Creative mode for testing. Start an expedition, press **Escape**, and click **Creative Mode: Off/On**. New sessions default to Survival. The toggle preserves carried items, equipment, health and hunger; items obtained in Creative remain when returning to Survival.

- Creative automatically enables hovering flight: movement bindings steer horizontally, Jump rises, Crouch descends, and Sprint accelerates. The same voxel collision, creature collision and ready-chunk boundaries apply. Working speeds are 7 blocks/second, or 12 while sprinting, with normalized diagonal movement. Inventory blocks flight input and holds position; Escape pauses the world.
- Incoming damage and automatic starvation cannot reduce health. Movement, mining and tilling do not spend hunger; existing health/food values freeze while world clocks, stations, crops, fluids and creatures keep simulating.
- **Tab → All Items** shows the full registered item catalog with icons, names, search and scrolling. Clicking supplies one legal full stack through the normal inventory authority. A full inventory rejects the entire grant with visible feedback. **Crafting** switches back to the personal grid; placed stations retain their regular interfaces.
- Block placement and potato planting retain the selected stack. Buckets retain their normal fill/empty behavior. Mining, tool requirements, reach and protected bedrock retain their current rules.
- Turning Creative off resets accumulated movement/fall state, restores gravity, damage, hunger expenditure and item consumption, and removes catalog access. Disabling it in midair begins an ordinary fall from that position. Quitting or starting a replacement world resets Creative to off.

These defaults are working implementation choices under the testing request. Flight feel remains subject to user review. [Creative verification](verification/CREATIVE_RESULTS.md) records measured evidence and limits.


### Torches

**User-authorized extension, 2026-09-10:** craftable torches that illuminate the world. One coal above one stick, or one charcoal above one stick, produces **four torches** in the personal 2×2 grid or on a workbench. The two independent recipe assets appear in Recipes; Creative's registry catalog also includes Torch. [ECONOMY.md](ECONOMY.md#current-survival-recipes-and-tiers) owns the ingredient/output table.

Use a selected torch against a solid floor or wall within normal five-block reach. Torches occupy a dry air cell, attach to that face, and do not block movement or skylight. Ceiling and underwater placement reject without spending an item. Survival consumes one torch per successful placement; Creative retains its stack. Torches are targetable with the normal voxel outline and can be mined by hand to recover one item. Removing their supporting block or flowing water entering their cell also returns exactly one torch and removes the light. They stay lit without fuel expenditure or burnout. Held/dropped torches use an original item silhouette; illumination comes from placed torches.

Attachments and voxel edits survive chunk unloading and floating-origin shifts during the session; quitting still resets them. The working presentation uses warm point lights with a ten-block range and real terrain shadows. Eight nearby lights are active at once within 24 blocks of the observer; the nearest torches receive those pooled lights. Torch geometry is displayed within 48 blocks. This is a bounded first rendering implementation, not a measured final lighting budget. Artificial light currently changes rendering only; grass/crops still use geometric skylight and mobs retain their clock-based spawning rules. Torch brightness, appearance and dense-room lighting need player review. [Torch verification](verification/TORCH_RESULTS.md) records the actual build, checks and captures.

## Authorized industrial extension — 2026-09-10

The user selected implementation of [GitHub issue #2](https://github.com/Starbugstone/Rivet-Reach/issues/2), including original Blender machines, animated operating states, matching interfaces and stability/performance checks. [INDUSTRY.md](INDUSTRY.md) owns the current Azure/Copper unlock, 4×4 Machinist’s Bench, separate signal/power/item/fluid graphs, steam/electrical bootstrap, crusher/pump/drill and fixed sensor/relay behavior. This supersedes earlier statements excluding this bounded industrial content; hybrid transport/control variants, advanced logic and durable saves remain later extensions. [Industry verification](verification/INDUSTRY_RESULTS.md) owns evidence and remaining limits.


## Authorized multiblock and pipe extension — 2026-09-10

The user selected [issue #3](https://github.com/Starbugstone/Rivet-Reach/issues/3): reusable multiblock lifecycle, player-built liquid tanks, connected Blender shell surfaces and shared pipe connections. [MULTIBLOCKS.md](MULTIBLOCKS.md) owns construction, exact shared storage, breach/resize recovery, signal valves/level sensors and independent signal/power fittings on both item and fluid pipes. This supersedes earlier exclusions of those specific hybrid channels and tank controls. [Verification](verification/MULTIBLOCK_RESULTS.md) records the measured build and remaining review. Whole-world durable saves remain later scope.
