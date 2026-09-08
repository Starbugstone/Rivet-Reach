# Rivet Reach - Simulation and Persistence Contracts

> **Status:** full-game working specifications with resolutions dated 2026-09-08. Sections 12–13 describe the selected first-step implementation boundary; [FIRST_POC.md](FIRST_POC.md) links measured evidence. Industrial, water and durable-save contracts remain later work.
>
> Existing agreed constraints come from [PROJECT_PLAN.md](PROJECT_PLAN.md). Sections 9-11 select concrete behaviour for the earlier contracts and the newly agreed water/item rules. Numerical defaults remain tuning values; unselected alternatives are not competing requirements. Open choices are tracked in [DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md).

## 1. Reference simulation and time

**Agreed direction:** machines and logical networks can operate without rendering; dormant chunks have essentially no continuous simulation cost; player-owned factory tickets require an online owner.

**Working contract:** use a fixed-step authoritative industrial simulation with a stable command order. Rendering can update independently. A provisional starting rate is 20 simulation steps per second; benchmark and tune it before adopting it. This is not a requirement that movement, physics and industrial machines all share one update rate.

Distinguish three clocks:

- simulation time: authoritative ordering and industrial process progress;
- eligible production time: intervals during which a machine is allowed to simulate;
- real elapsed time: useful for diagnostics, but not automatic production credit.

A machine advances once per eligible interval, regardless of overlapping tickets. Active and background modes produce the same industrial result for the same inputs and command sequence. Presentation, distant AI and unrelated physics can differ.

Working dormant policy: freeze production. Reconnecting does not grant production for owner-offline or otherwise ineligible time. Another player's proximity can still activate the area through a valid independent ticket. Sleeping *eligible* work may be batched only if its result remains equivalent to the reference simulation.

Section 9 selects pause, owner-disconnect and cooldown semantics. Universe time and eligible production time remain separate; do not substitute operating-system time implicitly.

## 2. Tick ordering and resource accounting

**Working reference order**, refined in section 9:

1. Apply validated commands and ticket changes at a step boundary.
2. Apply topology mutations and identify simulation-ready components.
3. Evaluate scheduled controls using defined prior-step observations.
4. Allocate power and reserve legal item/fluid transfers using stable ordering.
5. Advance eligible processors against available resources and output capacity.
6. Commit transfers/process results and emit presentation/persistence deltas.

Define whether newly produced material is available this step or the next; the initial proposal is next-step availability. Commands with competing effects must have a stable tie-break, not depend on dictionary iteration or job completion order.

Track inputs, outputs, in-process contents, inventory reservations and transit buffers. Recipes can intentionally transform quantities, but every change must be attributable to a recipe, source, sink or authorized action. A full output must not silently discard material. A failed reservation must not remove its input.

Section 9 selects proportional shortage, stable priorities/round-robin, no fluid mixing, step-boundary pipe transfer and escrow-based cancellation. These are observable rules, not incidental scheduling choices.

Signal loops need bounded propagation. Proposed starting semantics give changes a defined step delay and retain pending events for later steps. Oscillators may remain valid gameplay, but cannot create an unbounded same-step evaluation loop.

## 3. Networks spanning chunk states

**Problem:** a generator, pipe route and consumer can occupy different eligibility regions. Loading one machine must not silently wake an unlimited connected factory or erase the unloaded part of its network.

**Selected boundary policy:** simulate eligible portions only. Connections through dormant portions stop at explicit boundaries; they do not teleport resources across missing topology. Preserve boundary state, and show the resulting blockage in diagnostics. Each network type must define how its eligible subgraphs are derived, including whether stored power/fluid remains local or partitioned.

Retaining traversable topology through dormant terrain was not selected: it would make eligibility and quota accounting harder to explain. A later explicit revision may explore bounded network tickets; the baseline never routes through a dormant middle chunk.

Required cases:

- generator active, consumer dormant, then reversed;
- intermediate pipe chunk dormant between two active endpoints;
- one chunk promoted/demoted while transfers are reserved;
- two owners' tickets overlap and one disconnects;
- network split/merge changes inventory or fluid capacity;
- a player edits a network during a pending topology rebuild.

Proposed topology handling: version affected components and apply rebuild results only to the version they were computed from. Pause affected transfers when necessary; unrelated components continue. Schedule rebuilds within a work budget and expose rebuilding state. A single pipe edit can affect a very large component, so “rebuild only the affected network” alone is not a latency guarantee.

## 4. Safe batching and catch-up

Elapsed-time arithmetic is an optimization, not the definition of production.

A batch is eligible only when it accounts for all relevant constraints: inputs, output space, shared power, fluids, controls, competing consumers, recipe transitions and topology/ticket events. Stop at the next event that could change the result.

Compare optimized results against the reference simulation across different batch sizes. Use exact accounting for discrete items; explicitly choose rounding and tolerances for energy/fluid representations. If equivalence cannot be established, use scheduled reference steps within a supported workload instead.

Never independently fast-forward two processors that compete for the same limited input or battery. Never credit wall-clock offline time merely because a saved timestamp is old.

## 5. Coordinates and generation compatibility

**Agreed direction:** authoritative locations include world identity and integer/chunk coordinates; rendering uses local floating-point space.

**Working coordinate specification:** section 9 selects coordinate widths, supported bounds, negative-coordinate floor division, local-coordinate ranges and overflow behaviour before fixing the save format. Test chunk seams and origin shifts with entities, targeting and pending jobs. “Effectively infinite” means streaming within supported bounds, not unbounded numeric precision.

Persist universe seed, generator version, world-definition version and stable content identities. A seed alone does not preserve terrain when generation algorithms change.

Selected compatibility policy: pin existing worlds to their generator and definitions until an explicit migration is available. Untouched terrain regenerates under that version. If a required version/content definition is unavailable, report incompatibility clearly rather than silently generating replacement terrain or deleting blocks.

Gate-anchor placement must use bounded, deterministic neighbour queries and stable tie-breaking. Minimum-distance rejection, terrain accommodation and structure footprints must work across macro-region/chunk boundaries without depending on exploration order. Test negative regions and competing candidates. The final distribution algorithm remains open.

## 6. Durable state and recovery

Define a coherent saved revision across chunks, block entities, inventories, tickets where applicable, world metadata and player/entity locations. Asynchronous saving must capture a consistent snapshot rather than mixing unrelated revisions.

**Required transfer invariant:** after recovery, a transported entity and its inventory exist at exactly one authoritative location. Section 9 selects durable operation IDs and a journal/checkpoint protocol; exact binary storage implementation remains open.

For a transfer, prepare and reserve the destination, record the recoverable operation, commit ownership/location once, then publish deltas and release obsolete reservations. Retrying an operation must not repeat its resource effects. Recovery must resolve interrupted operations consistently, including when source and destination are stored separately.

Apply equivalent accounting to cargo dispatch, delivery, machine completion and inventory movement. Cargo that cannot unload needs an explicit waiting/return/failure policy; it cannot disappear or generate a second delivery.

Future recovery exercises: interrupted save, interrupted transfer, blocked destination, missing region data, disk-write failure and unsupported schema version. Preserve the last valid save where possible and report recovery outcomes. Choose backups, journal/snapshot design and migration policy before public persistent worlds depend on them.

## 7. Responsiveness and workload budgets

These are **candidate targets**, not measured promises or approved minimum requirements.

| Measure | Initial candidate | Evidence required |
|---|---|---|
| Rendered play | 60 FPS at 1080p; p95 frame time at or below 16.7 ms | Record hardware, quality, view distance and representative traversal/factory workload |
| Visible local interaction feedback | Next rendered frame where practical; p95 below 100 ms | Measure input-to-feedback separately from authoritative completion |
| Industrial simulation | 20 Hz reference; p95 work below 10 ms per step at declared workload | Include signals, transfers, power and topology queues; measure total frame contention too |
| Streaming/topology/save disturbance | No repeated stalls over 50 ms during normal interaction | Record p99/max stalls and queue age, not only average FPS |
| Memory residency | Bounded after repeated travel/unload cycles | Set a concrete RAM budget once a reference machine and view height are selected |
| Transfer readiness | Safe arrival; progress/cancellation behaviour during long waits | Measure cold destination generation separately from warm travel |

Select an actual reference CPU, GPU, RAM, storage device, operating system and build configuration before treating these as pass/fail gates. No hardware purchasing decision is required during brainstorming.

Proposed reproducible workload matrix:

- small functional factory, including a full output and power starvation;
- 100, 1,000 and 10,000 active simple processors as exploratory scaling tiers, not promised supported counts;
- long sparse versus densely connected networks, including repeated split/merge edits;
- dormant factories versus equal-sized active/background factories;
- streaming while production runs, origin shifts and a long travel/unload soak;
- cold cross-world transfer, multiple following entities and repeated ticket refresh attempts;
- save/load and recovery during production and transfer.

Record seed, workload configuration, resident/eligible chunk counts, network nodes/edges, active transfers, frame distributions, simulation time, queue age, memory and save size. Dormant CPU cost can be near zero while persisted metadata and storage still grow; measure those separately.

A benchmark passes only for its stated workload. When overloaded, bounded queues and visible waiting should preserve correctness; silently dropping simulation steps, duplicating transfers or freezing input is not an acceptable optimization.

## 8. World-item entity simulation

**Agreed gameplay direction:** mined blocks, mob drops and manually discarded inventory can exist as physical world-item entities. These should feel Minecraft-like in normal play while using a representation designed for larger worlds and heavier automation.

### One entity represents a stack

A world-item entity contains an authoritative `ItemStack` rather than representing one individual unit.

Conceptually:

```text
WorldItemEntity
|- world position / velocity
|- ItemStack
   |- item id
   |- count
   |- metadata/data where required
|- active/sleeping movement state
```

Dropping a full stack creates one world entity containing that stack. Breaking many blocks may create several world entities initially, but compatible nearby piles should merge where legal.

### Merge compatible piles

Nearby compatible item entities should periodically merge to reduce active entity counts.

A merge is legal only when item identity and relevant metadata are compatible. Merge operations must preserve exact item counts and obey the selected world-pile maximum.

Do not run expensive all-to-all proximity checks every frame. Candidate implementations include local spatial buckets, chunk/entity grids or bounded neighbour queries. Section 11 sets initial merge radius/cadence and lifetime policy; tune their numerical values through benchmarks.

The merge behaviour should be deterministic enough for authoritative multiplayer simulation and must never duplicate or lose items during simultaneous pickup/merge/despawn operations.

### Sleeping settled items

World items should not require continuous full physics after they have clearly settled.

Candidate state flow:

```text
ACTIVE
-> gravity / collision / water movement / merge checks
-> settles
-> SLEEPING
```

A sleeping item can avoid continuous expensive movement/collision work until a relevant event wakes it, for example:

- supporting block removed or moved;
- flowing water reaches/changes around it;
- explosion or other force;
- another physical interaction requiring movement;
- implementation-specific merge/pickup proximity handling.

Section 11 selects lightweight custom voxel-item movement as the first implementation. Benchmark the chosen representation; a rigidbody replacement must preserve its observable rules and improve measured behaviour.

### Water movement and buoyancy

Water uses game-oriented voxel/block fluid rules rather than continuous fluid dynamics.

Flowing water contributes horizontal/current movement to world items.

**Agreed content rule:** item definitions default to non-buoyant. Items sink unless their definition explicitly declares:

```text
buoyant = true
```

The physics/movement system queries this property from the item definition. It must not contain hard-coded special cases such as `if item == wood`.

Non-buoyant items receive downward/sinking behaviour while still responding to current. Buoyant items receive a gentle upward influence while still responding to current.

Section 11 defines bounded initial water response and surface/bottom rules. Exact force/drag coefficients remain feel/performance tuning parameters within those semantics.

### Separation from pipe logistics

Do not convert normal factory transfers into physical world-item entities merely for visual effect.

```text
world item = physical entity simulation
inventory item = stored data
pipe item = logical transfer/accounting
```

All three share stable item identity/count semantics, but only the first participates in gravity, collisions and water movement.

Cosmetic representations inside pipes may be rendered independently and may be dropped/skipped under performance load without changing authoritative item state.

### World-item stress cases

Add explicit future tests for:

- mining/dropping hundreds or thousands of compatible items in one area;
- many small piles merging while players pick them up;
- item piles crossing chunk boundaries in flowing water;
- settled sleeping items after long sessions;
- submerged buoyant and non-buoyant items in still and flowing water;
- block removal under sleeping items;
- save/load of world piles with exact counts;
- multiplayer contention where two players attempt to collect the same pile;
- despawn/merge/pickup occurring near the same simulation boundary.

## 9. Working resolutions for time, networks and durable state

**Resolution dated 2026-09-08:** select the reference architecture in sections 1-6, with the following details. These are implementation contracts to test, not completed systems. The remaining algorithm/performance choices do not reopen the observable semantics.

### Time and eligibility

Use a 20 Hz industrial reference clock. Input/movement/rendering have independent schedules. Single-player pause menus stop universe time; inventory/crafting screens do not. Dedicated-server universe time advances while the server runs, even with no connected players, but owner-online factory eligibility still expires on disconnect. Closing the single-player game stops its universe clock; reopening grants no wall-clock catch-up.

Cooldowns use persisted universe ticks and can expire while their chunks are dormant without ticking each endpoint. Production uses eligible ticks only. Personal factory loaders have exactly one accountable owner; shared access permissions do not automatically transfer ticket ownership. An owner can explicitly transfer a loader to a consenting online group member within that member's quota. A group member logging in does not wake another owner's whole factory set.

### Stable transfer and shortage rules

Apply the reference step order from section 2. Newly produced output and observed sensor changes become available next step. Logical signal propagation has a minimum one-step delay between sequential components; bounded oscillation is supported, unlimited same-step recursion is not.

Item routes use cached topology, declared port filters, highest destination priority first and a persisted round-robin cursor among equal-priority eligible destinations. Reserve source amount, destination capacity and route throughput before committing. The starter pipe has no authoritative in-flight stack: transfer commits at a step boundary and distance affects route topology/capacity, not individual item flight. Cosmetic movement is allowed to lag that transfer. Later transport latency would require a new explicit transit-buffer contract.

Allocate electrical power by configured priority, then fair proportional share among equal-priority consumers using fixed-point energy accounting and carried rounding remainder. A processor advances proportionally to energy actually supplied; below full power it slows rather than destroys inputs. A disabled/full-output machine requests no new processing power. Fuel is converted into stored energy credit once, and unused credit persists. Energy already expended is not refunded by dismantling or recipe cancellation.

Recipe inputs move to explicit internal escrow when work begins. Output capacity is reserved before completing the process. Cancellation/dismantling returns untransformed escrow and preserves finished outputs; it does not also refund the same inputs from the original inventory. A recipe change requires finishing or explicitly cancelling the current job.

Fluid identity cannot mix within one connected eligible pipe component. Connecting conflicting fluids is rejected with visible feedback until drained. Store quantities in spatially owned pipe segments, tanks and machine buffers, even when the network computes transfers in aggregate. A split retains each segment's contents; it never duplicates one shared network total into both halves. Capacity freed by dismantling transfers to a suitable container/recovery parcel instead of vanishing. A recovery parcel is a durable owner-access container with explicit item and fluid manifests, no despawn timer and no chunk-loading effect. Withdrawing fluid requires compatible available container capacity; the parcel does not convert fluid into free item stacks or create terrain sources. Once emptied, delete its persistent record.

Dormant intermediate chunks disconnect routing and supply. No pipe, cable, signal or fluid route traverses them implicitly. Power is stored only in explicit devices; a cable split contains no phantom battery. On a ticket change, settle the previous committed step, cancel unfinished reservations and rebuild the affected eligible subgraphs. Rebuilding components temporarily stop new transfers and expose that state. Versioned jobs cannot publish stale topology.

### Coordinate and compatibility baseline

Use signed 64-bit authoritative block/chunk coordinates with checked arithmetic. Local coordinates are 0-31 for an initial 32-cubed chunk; division at negative positions uses mathematical floor. For example, block -1 maps to chunk -1/local 31. Chunk size is a persisted world-format parameter; changing it for an existing save requires migration.

Initial worlds have finite configured vertical bounds, provisionally Y=-256 through 767 inclusive, and stream horizontally. Retain integer coordinates throughout authoritative addressing, including noise/anchor hashing; avoid turning a large absolute coordinate into a single float. Rebase presentation near the player, initially after 512 m from the local origin. Only presentation/physics-local positions shift; authoritative blocks and identities do not.

The first supported horizontal envelope is +/-1,000,000,000 blocks, far beyond ordinary traversal, with a visible boundary response and no integer wraparound. Validate generator behaviour at this envelope before promising it. Extending supported bounds does not require pretending that numeric infinity exists. Generator and definition versions are pinned per saved world; retain old generation implementations until a supported explicit migration replaces them.

### Persistence protocol

Select a save-level transaction journal plus versioned chunk/entity snapshots. Every inventory/world mutation belongs to an authoritative revision. A committed journal operation is applied at most once using its operation ID; replay finishes committed operations and cancels uncommitted reservations. A snapshot captures a coherent revision, then subsequent committed journal entries supply newer state.

Publish a new checkpoint manifest only after referenced snapshot data has been durably written. Retain the previous known-good checkpoint until the replacement is verified. A failure must not advertise “saved” while data is incomplete. Exact binary packing and operating-system write primitives remain implementation choices; interruption testing must verify their durability assumptions on supported storage.

Start with rotating three automatic checkpoints plus a pre-migration backup. Snapshot requests coalesce; they do not queue unbounded duplicate full saves. World transfers use a prepared record and one commit moving entity ownership from source to destination. Pre-commit failure leaves the source entity; post-commit recovery restores the destination identity exactly once. Save failure reports the error while retaining the last usable checkpoint and unsaved in-memory state where possible.

## 10. Block edits, collision, rendering and navigation

The authoritative block revision is shared by mining, placement, targeting and collision decisions. Jobs consume immutable snapshots plus revision numbers. Applying an edit atomically changes its block/occupancy state, inventory effects and collision query representation before acknowledging authoritative completion. Meshes/light/navigation may update asynchronously.

Select voxel-grid queries for player movement and world-item collision in the first implementation. A recently removed block stops colliding immediately; a placed block cannot be crossed while waiting for a mesh. Use swept queries over traversed cells to avoid tunnelling at chunk seams. General rigidbody entities, when introduced, need equivalent changed-cell collision proxies or a revision barrier before entry; an old mesh collider is not authoritative just because it exists.

Prefer a cheap immediate changed-cell visual patch while the chunk's optimized mesh rebuilds. Retire the patch only when a mesh for the same/newer revision is installed. If a surface cannot be shown promptly, retain a clear placement/mining transition indicator and bound that delay; do not acknowledge an invisible obstruction indefinitely.

At an unready frontier, hold movement against the last safe boundary and prioritize collision/terrain preparation before cosmetic distant work. Do not move the player into missing collision and hope a mesh arrives. Transfers reserve and prepare the destination before ownership changes.

Navigation uses local voxel occupancy and versioned paths. A block edit invalidates intersecting path segments; creatures recheck the next step before entering it. Start with bounded local search for the first passive/hostile behaviours. This permits useful early mobs without constructing a planet-wide navigation mesh.

Lighting begins with sunlight plus bounded voxel light propagation for local emitters. Block edits enqueue affected cells and neighbours; removal events must also withdraw obsolete light. Work budgets prioritize near-player visible updates. Distant light state can be recomputed from persisted sources/terrain when needed. Lighting never determines whether a target is a solid block.

## 11. World water and item scheduling resolutions

### Block water

Represent source/flow level and downward-flow state as compact voxel state. Use a 5 Hz scheduled water queue initially; process only dirty active regions rather than every water block. Compute a cell from an immutable previous water step and commit next states in stable coordinate order. Downward flow takes precedence; supported water spreads horizontally with decreasing level up to 7 cells. Source/falling-column provenance determines valid feeding paths, and flow disappears when no valid feeder remains.

Source removal invalidates its dependent local flow region and recomputes it from surviving sources; a loop of old flow cells must not sustain itself without a source. Queue work at chunk edges until both sides have water-simulation eligibility. Dormant frontiers are temporarily closed, not sinks that delete water or portals that wake an unlimited flood.

Physical water requires player-proximity world simulation. Portal-following tickets permit bounded entity movement using the already prepared water state but do not propagate an unbounded new water region. Background industrial pumps query only an eligible source intake and add measured inventory fluid; they do not cause nearby rivers to tick. On waking, pending water changes resolve within a budget before entities enter newly prepared hazard space.

This is a renewable-source block-fluid model, not mass-conserving ocean simulation. Industrial fluid amounts after intake are conserved. Bucket and source rules in [GAMEPLAY.md](GAMEPLAY.md) are the player-visible source/sink definitions.

### Item movement, merge and lifetime

Select lightweight custom swept voxel movement for dropped stacks, with interpolated/cosmetic rendering. Initial movement cadence is 20 Hz in active regions, with substeps when displacement would skip collision cells. General rigidbodies remain an alternative only if measured results justify replacing the implementation while preserving the same behaviour.

Use spatial buckets and bounded local merge queries every 0.5 seconds initially. A candidate is within 1 m, has compatible item/metadata and ownership/pickup restrictions, has compatible movement state, and is not separated by solid terrain. Merging cannot pull a pile through a wall or change its water side. Cap a world pile at the item's normal inventory stack limit initially; merge partially and leave a remainder when needed. This preserves the shared ItemStack model and prevents an oversized pile from breaking inventory assumptions.

Stable entity-ID ordering chooses the survivor; quantities transfer once through the same authoritative transaction mechanism as pickup. Resolve pickup, merge and expiry within one ordered entity phase. A merged pile inherits the greatest elapsed eligible lifetime of its inputs, not a fresh timer; adding one item cannot keep a pile alive forever. Do not merge piles with different protected recovery/ownership categories.

Ordinary loose items expire after an initial 20 minutes of local physical-simulation eligibility. Sleeping within an active region still ages them through timestamps; dormant time and paused single-player time do not. A source/destination portal-following window can count as physical eligibility, but cannot become a permanent clock/loader loop. Death caches and reserved machine recovery parcels are durable container records, not expiring loose piles.

When moving water/terrain support changes, wake affected piles through their occupied/support cells. Resting items do not need continual collision checks. Still water applies drag plus a bounded downward influence by default; `buoyant=true` reverses the vertical tendency toward the surface. Start with maximum vertical speeds of 1 m/s sinking and 0.8 m/s rising, and a current contribution capped at 1.5 m/s. These are separate water-response caps, not a global cap on gravity or thrown items. Horizontal current can move a non-buoyant pile along the bottom if unobstructed. Clamp surface crossing without perpetual bounce, and sleep when position/velocity remain within tolerance in unchanged surroundings.

Do not delete excess piles to meet a frame budget. Reduce cosmetic instances, sleep settled piles and schedule bounded physical work while preserving pickup/merge correctness. At unsupported sustained load, expose delayed simulation in diagnostics and reject new automated ejections before withdrawing inventory; already committed manual drops/mining must remain accounted for. Scale through measured representation/scheduling improvements, not hidden resource loss.

### Acceptance matrix

- Two miners and a player target one ore block: exactly one extraction succeeds.
- One source feeds a looped channel; source removal drains dependent flow rather than leaving immortal flowing cells.
- Water crosses an active/dormant edge, pauses, then resumes without lost stacks on wake.
- A sinking ore pile and buoyant wood pile move in the same current with distinct vertical behaviour.
- Pickup/partial merge/expiry contend in one step: item totals match one legal ordering.
- A settled pile's support is removed during meshing: movement responds to current voxel data.
- A full furnace is dismantled during save: machine, escrow and outputs are each restored once.
- An active network loses its middle ticket or splits while fluid is stored: no bypass, duplicate amount or stale route remains.

These supplement the existing scaling matrix. They are acceptance requirements for future implementation, not tests run during this documentation update.

## 12. First-step terrain and streaming validation

The locked first milestone in [DEVELOPMENT_STRATEGY.md](DEVELOPMENT_STRATEGY.md#6-locked-first-step-and-provisional-later-sequence) exercises terrain generation/loading, FPS collision, fist mining and real inventory. The industrial, water, transfer and durable-save contracts elsewhere in this document apply as their systems are selected later; they are not prerequisites for completing this step.

Use the existing coordinate/chunk/origin and authoritative-edit contracts. One world definition is sufficient now, with stable WorldId, block/item identities, generator version and seed. Generate natural terrain only; do not place structures or silently enable a structure-generation pass. Start with a small opaque terrain palette and enough height/cave variation to exercise seams, visibility and mining; the exact generation algorithms and visual palette remain implementation/review choices.

Chunk demand is driven by player position and configurable distances. Prioritize safe spawn, nearby collision and movement direction; schedule generation/meshing with bounded work and reject obsolete job revisions. At an unready frontier, retain a safe movement boundary and expose loading feedback. Unload unneeded runtime meshes and collision/presentation resources; retain only the compact authoritative state required to regenerate or restore edits/entities. Changing a render distance must not change terrain contents or delete items.

### Session state and resource conservation

For this first step, the selected working storage boundary is one running session. Keep changed voxel data and unexpired loose-item records independently of resident rendering chunks; unloading is not permission to regenerate mined blocks or reset item counts. Inventory remains authoritative across all streaming operations. Regenerate untouched terrain deterministically and reapply retained changes before a returning chunk becomes playable. Dormant pile lifetime follows the existing eligibility rule.

Do not retain every visited chunk's full unmodified voxel/mesh data merely to simulate persistence. Track compact edit/entity storage separately from resident streaming allocations: retained state can grow with real player changes, while an unedited exploration route must not cause unlimited resident terrain growth. Never evict committed changes or items silently to meet a memory target. Cross-session disk saving, journal durability and migrations remain later work; disclose the session reset on quitting/restarting and make no save-success claim in this build. Preserve the identity/revision boundaries so durable persistence can extend these same models.

### Required evidence for the first review

| Exercise | Evidence to record |
|---|---|
| Same seed, different discovery order | Matching unedited block contents across positive/negative chunk boundaries; safe deterministic spawn |
| Continuous walking/sprinting and rapid turns | Frame-time distribution, generation/mesh queue age, loaded chunks and frontier behaviour |
| Repeated mining at seams and underfoot | Correct authoritative removal/collision, rejected stale mesh results and conserved drop quantities |
| Leave and revisit an edited area | Runtime unloading actually occurred; removed blocks stay removed and unexpired items return once |
| Long unedited travel and return | Resident memory/cache counts stabilize for the declared view settings; compact edited-state growth reported separately |
| Floating-origin shift with the player and loose items | No targeting offset, visual jump, changed world identity or collision loss |
| Inventory interaction during remeshing/loading | No unintended mining through UI and no duplicate/lost stack transfers |

Record seed, generator version, travel route/duration, distances, job settings, build revision and actual reference hardware. Use the existing provisional frame/feedback/memory budgets, reporting measured outcomes and limitations. This step establishes evidence for its tested terrain workload; it does not certify factory scale, additional worlds or unlimited travel.

### First POC implementation evidence

The [first-POC record](FIRST_POC.md#implemented-foundation) and [verification results](verification/FIRST_POC_RESULTS.md) document the implemented subset. Worker meshes use immutable terrain/edit snapshots, per-residency generation tokens and edit revisions. Collision uses current voxel data and closed unready frontiers.

For this small palette, mining currently rebuilds only resident chunks whose halo touches the edit synchronously, keeping visual removal and occupancy aligned immediately. This is a bounded first-slice choice instead of the preferred changed-cell patch followed by an asynchronous optimized rebuild. The initial mesher produced a measured 55 ms hitch; direct stride indexing reduced that path substantially. The final measured cost and remaining frame-time limitation are recorded in the results. Keep measuring at seams and under heavier edits; move to immediate patches/async rebuilds if the synchronous cost exceeds the workload's budget. The nominal streaming publication budget is checked between meshes, so it is not a hard per-frame ceiling for one upload.

The tested route is finite and the item fixture is small. This does not validate unbounded travel, sustained thousands-of-pile workloads, factory simulation, multiplayer or durable recovery. Their existing contracts remain requirements for the stages that introduce them.

## First-POC placement and view-distance revision

The user’s feedback extends Stage 0 with terrain-block placement. Its interaction contract is owned by [GAMEPLAY.md](GAMEPLAY.md#first-step-terrain-placement--user-feedback-extension). The runtime uses one occupancy-change path for mine/place operations: compare expected cell value, store the replacement in the session edit map, update resident neighbour halos and revisions, and immediately publish the bounded local remesh. Placement consumes the selected item only after the occupancy change succeeds. This is a single local authority turn; it does not establish a network transaction or durable journal.

Default horizontal demand is now ten 32 m chunks, with a four-to-fourteen radius setting. Fog start is `max(48, (radius * 32 - 24) * 0.8)` metres; fog end is `radius * 32 - 16` metres. These put haze near the guaranteed interior of the resident square rather than near the player. Runtime view preferences use a revised key so the old default of four does not silently preserve the rejected close fog. Measure actual residency, allocation and frame times at the new default; historical radius-four evidence is not proof of radius-ten performance. See [current revision evidence](verification/VISUAL_REVISION_RESULTS.md).

## 13. Grass random ticks — first-step feedback

The user requested grass-to-dirt fist drops and gradual light-dependent spread. Player-visible rules belong to [GAMEPLAY.md](GAMEPLAY.md#grass-growth--user-feedback-extension). `GrassSimulation` is a pure state transition/scheduler using world-addressed block reads, sky exposure and compare-and-replace mutations; it does not depend on rendered brightness or mesh availability.

The working cadence is 20 Hz. For each eligible 32³ chunk, sample three random cells in each of its eight 16³ sections. Only sampled grass evaluates decay or four candidate neighbours; ordinary stone/air has no per-block ticking object. Eligible chunks are the ready terrain inside a 5×5×3 chunk neighbourhood of the player's chunk, bounded by resident demand. Seed, tick number and sorted chunk coordinates define stable sampling. Empty/dormant eligibility performs no voxel samples. Pause freezes the tick clock; unloaded regions receive no retroactive growth. Cap catch-up at four simulation steps per render frame while retaining the eligible-time accumulator.

Queue at most eight grass/dirt conversions per tick, publish after sampling and recheck the expected source state. Newly converted grass cannot spread again within that same tick. Gameplay mining/placement retains immediate collision and local mesh publication; solid-to-solid grass changes instead mark affected halos/revisions dirty for the existing bounded asynchronous mesh workers. Old worker results cannot undo newer conversions. No item event occurs for grass growth or decay.

The current sky query returns 15 above the highest opaque voxel in a column and 0 below it. Start with deterministic terrain height, include edited blocks above it and search down through mined holes. Cache only queried columns near the active neighbourhood; invalidate when an edit changes solid/air occupancy. Session edits remain authoritative even if a roof's chunk is unloaded. This deliberately does not approximate indirect/torch light from visual ambient fill. A later light service can replace the query while keeping the tick and edit contracts.

Validate illuminated versus covered dirt, overhead roofs, cover removal, cross-chunk spread, deterministic sampling, no item creation, paused/unready/dormant behaviour and preservation through origin shifts/reloading. The tick interval, sampling density and neighbourhood are working defaults for review, not user-selected numerical requirements or a universal performance guarantee.
