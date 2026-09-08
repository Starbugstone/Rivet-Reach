# Rivet Reach - Transport, Gates and World Travel

> **Status:** evolving design / architecture notes
>
> This document defines the current rules for movement between worlds, Gatebuilder gateway generation/state, rockets, chunk wake-up around portals, future teleporters and satellite cartography.
>
> Working resolutions in sections 37-40 (2026-09-08) select previously open safety, accompaniment and route policies; numerical defaults remain unvalidated tuning.
>
> These rules are intentionally documented early because world travel touches procedural generation, save data, multiplayer geography and performance. Retrofitting them after the world format is established would create major headaches.

---

## 1. Core spatial rule

The current preferred world-travel rule is:

> **Inter-world travel preserves horizontal X/Z coordinates unless a deliberately advanced technology explicitly breaks that rule.**

This applies to both major early/intermediate travel systems:

```text
Physical planet -> physical planet by rocket
X/Z preserved

Physical world -> linked portal realm by Gatebuilder gateway
X/Z preserved
```

The rule is important for both gameplay and multiplayer.

A player who deliberately settles extremely far away remains geographically isolated even after Gates and rockets are available.

Example:

```text
Player A settlement: X = 0
Player B hermit base: X = 80,000

Starting world
0 ======================================= 80,000

Portal realm
0 ======================================= 80,000

Moon / other planet
0 ======================================= 80,000
```

Gates and rockets move the player **between worlds**, not sideways across enormous distances.

This prevents world travel from accidentally becoming early fast travel.

---

## 2. Why distance preservation matters

A single unique gateway would create a multiplayer cluster and punish distant settlements.

Likewise, compressed-coordinate travel similar to Minecraft's Nether would undermine the choice to live remotely.

Rivet Reach should support both:

- community hubs forming organically around convenient structures;
- hermit players intentionally living thousands of chunks away.

No world-travel system should silently erase that choice.

Late-game player-built teleporters may eventually break geography deliberately, but that should feel like a major technological achievement.

---

## 3. Seeded Gate lattice

Gatebuilder gateways are not generated as independent random structures.

The **save/universe seed** determines a sparse set of Gate anchors at stable world coordinates.

Conceptually:

```text
Universe seed
    |
    v
Gate network algorithm
    |
    +-- Anchor #001  X/Z
    +-- Anchor #002  X/Z
    +-- Anchor #003  X/Z
    +-- ...
```

The same anchor exists in every world that participates in that Gate network.

This guarantees that matching gateways line up geographically across dimensions.

---

## 4. Multiple Gates, not one unique Gate

There must be **many** Gatebuilder sites across an effectively infinite world.

They should be:

- rare;
- very widely separated;
- deterministic from the seed;
- distributed indefinitely as the procedural world expands;
- prevented from clustering too closely;
- not centred on world spawn;
- available in distant regions so remote players are not forced to return to one central portal.

The system should feel like discovering remnants of ancient planetary infrastructure rather than finding one magical dungeon entrance.

---

## 5. Gate distribution strategy

Do not use a simple `chance per chunk` roll for major Gate sites.

Pure random probability can create both ugly clusters and enormous accidental voids.

The preferred broad approach is a **macro-region / blue-noise style distribution**:

1. Divide the infinite X/Z world into very large deterministic macro-regions.
2. Derive one or more Gate candidates from the universe seed and macro-region coordinates.
3. Apply strong positional jitter so Gates do not form an obvious grid.
4. Enforce a large minimum distance between neighbouring Gate anchors.
5. Apply terrain/structure validity rules.
6. Keep the distribution deterministic and independent of chunk exploration order.

Exact macro-region dimensions and minimum distances must be tuned through playtesting.

The goal is:

```text
      X

                    X

 X

             X

                         X
```

rather than either a visible grid or random clustering.

---

## 6. Gate networks / destinations

The starting world can contain several different Gate networks/destination realms.

The exact number is still open, but the current design discussions have used the idea of multiple networks such as:

```text
Network A <-> Realm A
Network B <-> Realm B
Network C <-> Realm C
```

Each network contains **many physical Gate sites**, not only one Gate.

For example:

```text
Starting world

A17                  C51

       B29

                           A18

               C52

                                  B30
```

Destination/network identity and physical damage condition are separate properties.

Two A-network Gates can lead to the same realm while one is almost intact and the other is heavily damaged.

---

## 7. Shared coordinates across linked worlds

For a Gate network, the same seed-derived anchors exist in both the physical world and its linked portal realm.

Example:

```text
Starting world

Gate #1847
X = 37,900
Z = 15,600
       |
       v

Realm A

Gate #1847
X = 37,900
Z = 15,600
```

The surrounding ruin and terrain may differ between worlds, but the Gate identity and horizontal anchor are shared.

This means Gate realms have actual geography.

---

## 8. Travelling through one Gate and returning through another

This shared lattice creates an important exploration behaviour.

A player can:

```text
Starting world
Gate #001
    |
    v
Realm A
Gate #001
    |
    | explore normally through Realm A
    v
Realm A Gate #002
    |
    | repair/activate if necessary
    v
Starting world Gate #002
```

The player may emerge on their home planet at a location they had never reached from the normal world.

This is intentional and should create unscripted discovery moments.

Because X/Z distance is preserved, this is **not** a compressed fast-travel shortcut. If Gate #001 and Gate #002 are 30 km apart on the starting world, they are also roughly 30 km apart in the portal realm.

Different realm terrain may make one route easier or harder, but the distance itself remains meaningful.

---

## 9. Gate anchors exist before chunks

Critical generation rule:

> The existence and coordinates of important Gate anchors must not depend on which chunks happen to generate first.

Bad design:

```text
generate chunk
-> random portal roll
```

Preferred design:

```text
universe seed
-> deterministic Gate-anchor function
-> chunks query whether an anchor intersects their region
-> appropriate structure generated when chunk is first created
```

An untouched distant region does not need to consume save space. The Gate position can be reproduced mathematically from the seed.

Only changed state needs persistence.

---

## 10. Universe / Gate metadata

A conceptual representation may look like:

```text
GateAnchor
|- id
|- network id
|- X/Z
|- shared anchor seed
|- linked worlds

GateSideState
|- world id
|- generated/not generated where useful
|- repair/component state
|- local condition
|- cooldown/recovery state if needed

GatePairState
|- activated once
|- link established
|- persistent network state
```

The exact data format will be designed during implementation.

The important rule is that authoritative Gate state exists **outside a loaded block/chunk object** so unloading the physical structure does not erase or reset the Gate network.

---

## 11. Terrain and Gate anchors

Strict shared X/Z does not mean the Gate should accidentally spawn inside unusable geometry.

Because Gate anchors are known deterministically before local chunk generation, terrain generation should be able to accommodate the important structure.

Preferred approach:

- anchor stays at the intended X/Z;
- local terrain is shaped/adapted around the site where needed;
- the surrounding ruin can be buried, overgrown, partially collapsed or otherwise varied intentionally;
- accidental placement in impossible geometry should be prevented.

Small local offsets may be considered if technically necessary, but they should remain tightly constrained. The coordinate relationship is a core gameplay rule, not a suggestion.

---

## 12. Gate structures and damage

The same Gate anchor does not require identical ruins on both sides.

Example:

```text
Gate #1847 - Starting world side
|- ring mostly intact
|- control pedestal damaged
|- one missing component

Gate #1847 - Realm side
|- surrounding complex collapsed
|- Gate core intact
|- stabilisation system badly damaged
```

Structure appearance can be generated from per-side deterministic seeds while retaining the shared Gate identity.

This gives procedural variation without breaking linking.

---

## 13. Gate activation

The repair interaction is specified in section 37; final artwork/component names remain tunable. Core rules:

- players cannot construct new Gatebuilder gateways;
- gateways are discovered as ancient ruined structures;
- activation is based on repairing/restoring required Gatebuilder systems/components;
- activation must not be gated by arbitrary player levels;
- normal electrical power is **not currently intended to be the fundamental Gate power system**;
- a sufficiently intact/lucky Gate may be activated earlier than expected;
- once a pair has been successfully activated, that discovery/link remains persistently known.

The Gatebuilder mechanism should feel technological but operate on a principle distinct from the player's normal electrical grid.

See [LORE.md](LORE.md) for the hidden-world rationale.

---

## 14. No per-traversal cost

Normal travel through an established Gate has **no energy, fuel or consumable cost**.

This is a firm gameplay direction.

The exploration loop must not become:

```text
walk through
-> pay resource
-> inspect something
-> pay resource to come back
```

Once the player has earned access to a Gate connection, the game should get out of the way and allow repeated exploration.

Difficulty belongs in discovery/restoration, not in a toll booth attached to every journey.

---

## 15. One operational side can sustain the pair

Gate endpoints may be damaged independently.

Current rule:

> One restored endpoint sustains normal service. Once established, the pair also retains a protected minimum return function if all optional repaired systems become unavailable; see section 37.

This does not require both structures to be fully repaired before first use.

A healthy source can therefore connect to a damaged receiving endpoint.

The exact definition of `operational` will evolve with the activation design, but it should be persistent state rather than requiring permanent chunk simulation.

---

## 16. Damaged receiving-side cooldown

The player should never be permanently stranded by discovering that the receiving Gate is too damaged to return.

Instead, a damaged receiving side can impose a **short stabilisation/recovery cooldown** before the Gate can be used again from that side.

Conceptually:

```text
Healthy endpoint
-> immediate or near-immediate reuse

Damaged endpoint
-> outbound traversal waits while receiving remains possible
-> pedestal displays visible recovery progress
-> short cooldown
-> Gate becomes usable again
```

Repairing the receiving endpoint reduces or removes this cooldown.

This gives meaningful value to repairing both ends without creating resource grind or soft locks.

Exact timing should be tuned later. It should be long enough for damage to matter but short enough not to punish exploration.

---

## 17. Gate control pedestal / UX

Each Gate should have a Gatebuilder control pedestal or equivalent diegetic interface.

This is the simplest current solution for communicating state without lore dumps or giant machine menus.

The pedestal should use consistent visual grammar:

- hollow/empty shape = missing component;
- filled/illuminated shape = installed/working;
- dark = inactive;
- illuminated = linked/active;
- flicker/broken segment = damage;
- filling ring/dial = cooldown or stabilisation progress;
- paired endpoint symbols = local/remote state.

For example:

```text
LOCAL       REMOTE
  active --- damaged

Recovery:
  (partially filled circular indicator)
```

The exact art language must not rely on colour alone; shape, animation and iconography should also carry meaning.

Detailed inspection can provide more explicit information if needed, but basic Gate state should be readable directly from the world.

---

## 18. Active Gate state is not a permanent chunk loader

An activated Gate should **not** keep both endpoint chunks permanently loaded.

Otherwise a multiplayer server containing thousands of discovered Gates would gradually accumulate hidden simulation load.

When nobody is nearby:

```text
Gate pair state: active/linked
Source chunks: unloaded
Destination chunks: unloaded
Rendering: none
AI: none
Physics: none
Continuous Gate simulation: essentially none
```

The link exists as lightweight persistent state.

---

## 19. Portal traversal and temporary chunk wake-up

When an entity enters an active Gate:

```text
entity intersects source Gate
    |
    v
validate persistent Gate state
    |
    v
determine linked world and same X/Z anchor
    |
    v
reserve source/destination with temporary preparation tickets
    |
    v
wake/load destination Gate area
    |
    v
ensure destination terrain/entity space is ready
    |
    v
transfer entity
    |
    v
apply temporary portal activity tickets
```

The entity must never be moved into an ungenerated/unready destination chunk.

---

## 20. Portal activity tickets

Recent Gate traversal should keep the necessary areas active for a short period.

This is a **temporary server/system ticket**, not a player chunk-loader ticket.

Conceptually:

```text
entity crosses
-> source Gate area held active temporarily
-> destination Gate area held active temporarily
-> timer starts

another entity crosses
-> timer refreshed

no further activity
-> timer expires
-> chunks become eligible for normal unload
```

Exact duration is a POC/balancing decision, likely on the order of seconds rather than permanent loading.

Portal activity tickets do **not** count against a player's chunk-loader quota.

---

## 21. Mobs and Gate traversal

Mobs can physically pass through active Gates under the player-accompaniment rule in section 38.

If a hostile creature follows a player into the gateway, it may arrive in the destination world.

Likewise, players can potentially move livestock or other creatures manually.

The same entity identity/state should be transferred where practical, preserving data such as:

- health;
- inventory/equipment;
- taming/ownership;
- custom name;
- relevant AI/state;
- other persistent attributes.

Do not treat traversal as `destroy old mob -> spawn generic replacement` if the architecture can avoid it.

Portal activity tickets exist partly so following mobs have time to complete traversal rather than hitting an instantly unloaded source/destination.

---

## 22. Portal realms and automation

Gate travel is for physical player/entity movement and exploration.

Gatebuilder gateways must **not** become an automated logistics conduit.

Current portal-realm rules:

- no automated item transfer through Gates;
- no automated fluid transfer through Gates;
- no player-built inter-world teleporter link into the realm;
- no cargo rockets into the realm;
- no unattended remote extraction loop that turns a realm into another factory colony;
- player/entity traversal remains allowed;
- local storage/hand crafting and attended processing are allowed; automatic realm extraction/harvesting is disabled under sections 38 and ECONOMY.md.

The player should always retain a reason to personally return to portal realms.

---

## 23. Player chunk loaders remain separate

Factory chunk loaders are unrelated to Gate activity.

Current preferred factory rule remains:

> A player's chunk-loader tickets are active only while that owner is online.

There is also a per-player quota.

This prevents one connected player from waking every offline player's remote factories.

Chunk ticket categories can therefore include at least:

```text
Player proximity ticket
-> full local simulation

Player-owned factory chunk-loader ticket
-> background machine/logistics simulation while owner online

Portal activity ticket
-> temporary local/full simulation needed for safe traversal
```

The strongest applicable reason determines the simulation level, subject to world-specific capability restrictions. Portal safety activity must not implicitly authorize unattended realm factories; see section 38 for the selected accompaniment and entity rules.

---

## 24. Rockets and coordinate-preserving planetary travel

Rockets are the player's normal way to reach additional physical worlds before advanced teleportation.

Current preferred rule:

> A launch from X/Z on one physical world targets approximately the same X/Z on the destination physical world.

Example:

```text
Starting planet
Launch pad
X = 25,100
Z = 10,050
      |
      | rocket
      v
Moon
Landing region/pad
X ~= 25,100
Z ~= 10,050
```

This makes rockets move the player through the planetary system without becoming sideways fast travel.

---

## 25. Launch pads as spatial infrastructure

Once established, launch/landing pads should form explicit routes.

Players are encouraged to build vertically aligned infrastructure across worlds:

```text
Moon
Mining colony
X/Z = region A
    |
    | cargo route
    v
Starting planet
Factory
X/Z = region A
```

Another multiplayer group thousands of chunks away can establish its own independent planetary infrastructure at its own X/Z region.

This preserves settlement geography across the whole physical system.

---

## 26. First landing on a new world

A player obviously cannot have built a receiving landing pad on a planet they have never visited.

Current preferred concept:

### First expedition

- launch coordinates determine target X/Z;
- destination terrain is generated safely;
- the rocket selects suitable landing terrain within a small constrained radius around the target;
- the player establishes a proper landing pad after arrival.

### Later travel

- explicit pad-to-pad routes can use exact known endpoints;
- automated cargo requires established infrastructure at both ends.

The allowed first-landing adjustment must remain small enough that coordinate preservation remains meaningful.

---

## 27. Cargo rockets

Cargo rockets are part of the Factorio-style expansion loop.

Long-term concept:

```text
Remote planet mine
-> local processing
-> cargo pad
-> rocket
-> corresponding X/Z region on home planet
-> receiving pad
-> pipes
-> factory/storage
```

Cargo pads can expose the same general industrial interfaces as other machines:

- item inventory/input/output;
- fuel input;
- power/control systems as appropriate;
- signal input;
- destination/route configuration.

Potential automation example:

```text
IF cargo >= threshold
AND fuel sufficient
AND receiving pad available
THEN launch
```

Exact rocket/fuel mechanics are future work.

---

## 28. Rockets must not become accidental fast travel

Without coordinate preservation, players could use another planet as a hub to jump laterally between distant settlements before teleportation exists.

The intended model prevents this:

```text
Base A at X = 0
-> rocket
Moon at X = 0

Base B at X = 100,000

To reach B through the Moon, the player still has to cross roughly 100,000 horizontal units there.
```

That is intentional.

---

## 29. Player-built teleporters

Teleporters are a separate, much later technology.

Unlike rockets and Gatebuilder gateways, player-built teleporters may eventually allow explicitly linked arbitrary locations and therefore **break normal geography**.

That is why they should be powerful and late-game.

Current intended distinction:

```text
ROCKET
|- player-built
|- physical worlds only
|- coordinate-preserving
|- cargo automation possible

GATEBUILDER GATE
|- ancient / cannot be built by player
|- portal realms
|- coordinate-preserving Gate lattice
|- no automated inter-world resource transfer
|- free repeated traversal once operational

PLAYER TELEPORTER
|- late-game player technology
|- normal physical universe
|- arbitrary linked endpoints potentially allowed
|- automated logistics possible
|- cannot connect to portal realms
```

The exact teleporter energy/infrastructure balance will be designed later.

---

## 30. Additional physical planets and Gatebuilder networks

The Gatebuilders colonised the system, so additional physical planets should also contain Gatebuilder infrastructure.

The same procedural system can vary Gate/ruin density by planet.

Resolved working rule:

> A realm instance belongs to one physical planet/network. Themes may repeat, but other planets use distinct instances.

Allowing this could create routes such as:

```text
Home planet -> Realm A -> Moon
```

which might bypass rockets and undermine aerospace progression.

The selected model **excludes cross-planet Gate shortcuts**. A future change would require an explicit progression review.

The architecture should not hard-code an answer that makes later experimentation impossible.

---

## 31. Satellite cartography

Orbital satellites are a future concept, but their role should be **mapping**, not automatic discovery.

Current direction:

- satellites can reveal/expand world map coverage around the player or within some coverage model;
- exact radius, timing and number of satellites require balancing later;
- satellites should **not automatically mark Gatebuilder Gates**;
- satellites should not become an ore/ancient-structure cheat scanner by default.

The player still explores the world physically.

Possible balancing models include coverage radius, orbital passes or progressive map revelation, but none is final yet.

---

## 32. Map and discovery philosophy

Travel technology should help the player move or understand geography without deleting discovery.

Current principles:

- Gates are not automatically placed on the map before discovery;
- satellite cartography reveals terrain, not every point of interest;
- Gate sites may have environmental clues that reward observation;
- discovered Gate locations can of course become known/map-able thereafter;
- multiplayer communities can share coordinates socially without the generator itself revealing everything.

---

## 33. Multiplayer implications

All world-travel state must be server authoritative once multiplayer exists.

The server owns:

- Gate pair/network state;
- repair/activation state;
- Gate cooldowns;
- entity traversal;
- destination chunk wake-up;
- rocket routes;
- cargo transfer;
- chunk tickets;
- world coordinates.

Clients render and request actions but do not authoritatively decide that a Gate is active or that an entity appeared in another world.

Sparse Gate generation and coordinate preservation also support decentralized multiplayer settlement rather than forcing everyone around spawn.

---

## 34. Performance rules

Transport must follow the project's broader optimization philosophy.

Non-negotiable intentions:

1. An activated but unused Gate does **not** continuously simulate both worlds.
2. Undiscovered/unmodified Gate anchors should be reproducible from the seed and require almost no save storage.
3. Destination chunks are loaded only when traversal requires them or another normal ticket already does.
4. Portal activity uses short-lived tickets, not permanent hidden chunk loaders.
5. Gate pair state is lightweight persistent metadata, not an always-running machine.
6. Mobs are only actively simulated where normal simulation/tickets require them.
7. Rocket/cargo routes should use coarse state simulation when full physical simulation is unnecessary.
8. Transport networks must remain viable on multiplayer servers containing many distant settlements.

---

## 35. Current transport rules

Unless deliberately revised, the current agreed rules are:

1. Each save is a completely isolated universe.
2. The save seed deterministically defines Gate anchors.
3. There are many Gatebuilder sites, extremely widely spaced, rather than one unique Gate.
4. Gate distribution extends indefinitely with the procedural world.
5. Gate networks use shared X/Z anchors across linked dimensions.
6. Travelling through one Gate and later finding another in the realm can return the player to the corresponding different Gate on the physical world.
7. Portal travel does not compress horizontal distance.
8. Gatebuilder gateways cannot be constructed by players.
9. Gatebuilder technology is separate from conventional player electricity.
10. Normal Gate traversal costs no fuel/energy/resource.
11. Once a pair is established, persistent state records that link.
12. One sufficiently operational side sustains normal service; established pairs retain protected fallback return after optional modules are removed.
13. A damaged receiving side may create a short visible cooldown but must not strand the player.
14. Repairing the second side reduces/removes that inconvenience.
15. The Gate pedestal is the primary diegetic status/repair interface.
16. Active Gates do not permanently load chunks.
17. Traversal temporarily wakes destination/source areas using system-owned portal tickets.
18. Mobs can pass physically through Gates under player accompaniment; loose item piles cannot cross.
19. Portal realms cannot be automated as unattended inter-world resource farms.
20. Factory chunk loaders remain owner-online and quota-limited.
21. Rockets preserve X/Z between physical worlds.
22. First landings may make only a small safe local adjustment.
23. Established launch/landing pads support repeated and automated cargo routes.
24. Late player teleporters may eventually break geography, but only in normal physical space.
25. Satellites are currently intended for cartography, not Gate/ore scanning.

---

## 36. Open transport/design questions

Remaining tuning and implementation review (policies resolved in sections 37-40 are no longer open alternatives):

- exact Gate macro-region size and minimum separation;
- number of Gate networks/portal realms on the starting world;
- how network identities are communicated visually;
- final names/art and balance for the selected two-slot surface-repair components;
- final Gatebuilder energy/resonance lore;
- exact damaged-side cooldown timing;
- whether multiple repair stages affect cooldown differently;
- presentation of dropped items remaining at the Gate threshold;
- portal collision/transition presentation;
- first-landing rocket safety algorithm;
- launchpad/landing-pad sizes and construction rules;
- rocket fuel and manufacturing chains;
- cargo rocket scheduling/capacity;
- planet count and orbital progression;
- any future deliberate revision to the selected planet-specific realm-instance model;
- final player-teleporter limitations/costs;
- satellite map coverage model and balance;
- server configuration for Gate density and transport-related chunk tickets.

Working policies are selected below; remaining tuning and evidence are tracked in [DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md). These specifications resolve the requested gaps without claiming implementation or playtest validation.

---

## 37. Working Gate safety and return contract

**Resolution dated 2026-09-08:** select protected Gate cores with a permanent minimum return function after first establishment. This replaces the unresolved alternatives previously listed here. Repairs still matter for convenient operation; they cannot revoke an established pair's basic travel.

### Persistent state and restoration

A seeded Gate core and its small arrival sanctuary cannot be mined, exploded, moved or overwritten by ordinary construction/quarries. Damage in generation affects replaceable systems and the surrounding ruin, not the existence of its matching core. The sanctuary is generated from the anchor before ordinary structure/terrain decoration and reserves a safe floor, clearance and Gate approach on both sides.

Before establishment, at least one endpoint must be restored with locally obtainable repair components. After establishment, the persistent pair retains a fallback link even if all optional repaired modules are removed. One restored endpoint supplies normal pair service; the fallback is the explicit exception to the earlier “one operational side” requirement. It prevents later sabotage or optional-component removal from stranding a visitor.

Working cooldown values: restored local endpoint 1 second between departures; damaged local endpoint 10 seconds; pair fallback with no restored endpoint 15 seconds. These are tuning defaults. Each endpoint keeps an explicit next-ready universe tick; no always-running chunk object is needed. Arrival is allowed while the local departure recovery timer runs, so following creatures do not become trapped solely by a player's arrival. Each entity also has a 2-second re-entry guard and must leave the portal volume before crossing again.

The first restoration interaction has two visible service slots: a coupling assembly and a stabilizer. A generated endpoint may already contain either/both in working condition. Inspecting a slot identifies its missing/damaged function and the accepted component; clearing overgrowth is ordinary mining around protected core cells. Compatible salvage from surface ruins can fill a slot. Physical substitutes are workbench recipes: coupling assembly = 4 iron plates + 4 copper wire + 1 gear; stabilizer = 4 copper plates + 2 iron plates + 1 gear. Installing both on one side and holding the pedestal's establish action completes the persistent link. Already intact sites need no manufactured parts. These service modules do not let players construct new cores/Gates.

The receiving side's sanctuary/core exists regardless of optional slot damage. Establishment does not require a realm material, conventional electrical supply or repairing both ends. Components are consumed into persistent slot inventories and recovered once when deliberately removed; removal leaves the established fallback intact.

The pedestal shows established/unestablished, local condition, pair fallback and departure recovery separately. Repair removes inconvenience without imposing a per-use resource cost. A claim cannot lock another player's only established return route or revoke core traversal. Ordinary surrounding builds remain subject to player permissions.

### Arrival sanctuary and obstruction

Core definitions reserve at least a 3x3 floor with 3 blocks of clear height at the exit, plus a clear approach back to the portal. Water/lava placement and solid construction inside that volume are rejected with a visible protected-area preview. Generated hazards cannot occupy it. Do not clear an existing player's blocks silently after arrival is requested.

Entities inside the sanctuary cannot attack, take combat damage or body-block another arrival; the protection ends when they leave. Non-player entities may traverse it but cannot choose it as a permanent AI resting target. This is a small explicit safe-return exception, not a safe zone across the ruin. Players can still build hazards outside it; cache/death rules handle ordinary expedition risk.

Legacy or corrupt saves with an invalid sanctuary require explicit recovery to a nearby safe reservation or a reported blocked transfer, never an arrival inside a wall. If the player is already in the realm and the local return approach is invalid, an inspectable persistent Gate-return interaction within the sanctuary transfers to its paired sanctuary after the fallback delay. It does not select arbitrary coordinates or carry unattended items.

### Traversal transaction

1. Validate the entity, its player-accompaniment eligibility, pair identity and cooldown.
2. Acquire bounded preparation tickets at both ends and reserve a safe destination pose.
3. Keep the source entity authoritative while loading. During a requested player transfer, protect the waiting player in the source sanctuary and show progress; cancellation returns normal control there.
4. Revalidate destination revision and reserve the entity's complete identity/inventory state.
5. Commit a uniquely identified ownership/location transfer once through the save journal.
6. Apply temporary activity tickets, departure cooldown and entity re-entry guard; release preparation reservations.
7. Pre-commit failure leaves the entity at the source. Post-commit recovery restores it at the destination. Retries never create another copy.

Initial preparation timeout is 30 seconds before returning a clear retryable failure; profiling may change it. One player can own one pending Gate preparation. Coalesce requests for the same endpoint; a server-wide initial pool of 8 simultaneous cold preparations queues additional requests fairly. This is a concurrency scheduler, not a cap on discovered Gates or permanently loaded worlds. Expired/cancelled reservations release their tickets.

Cooldowns use universe time as defined in [SIMULATION.md](SIMULATION.md): they can elapse while chunks sleep, pause with a paused single-player universe, and never require a Gate tick on every frame.

## 38. Working entity travel and realm-automation boundary

**Selected policy:** living entities cross only under player accompaniment; loose world-item piles never cross Gates. A player carries inventory normally. This deliberately narrows the earlier broad mob-crossing statement to preserve pursuit and livestock travel while closing unattended cargo/loader loops.

An eligible crossing has either a player within 16 m of the source Gate, or a still-valid 10-second following window created by a player's successful departure in that direction. An entity must physically enter the source Gate; proximity does not teleport it. The player's departure creates one following lease shared by both endpoint regions. Non-player crossings cannot extend it, and they cannot create new cold destination preparation after it expires.

Named/tamed inventory-bearing companions may cross under the same accompaniment rule with their original state intact. The player is physically present for that transport. Creatures circulated unattended by water, machinery or AI eventually stop at the inactive traversal surface without losing inventory. World-item piles, including buoyant piles pushed by water, collide/settle at the Gate threshold and remain in their source world. No normal dropped item is destroyed merely for touching the Gate.

An endpoint with no local player and no following window retains its persistent linked state but does not provide unattended entity travel. The pedestal distinguishes linked from currently receiving accompanied traffic. Queued creatures that miss the window stay at the source; the player can return to escort them. Within the active window, prepared endpoint queues process entities in stable order with a per-step budget rather than spawning unbounded loading jobs.

Ticket capabilities are independent: entity collision/pursuit readiness does not confer factory or world-water propagation permission. Realm extractors/drills/pumps/automatic harvesting are disabled even under player proximity. Local hand crafting, storage and player-attended processing are allowed. A realm mob only produces normal drops through an eligible nearby player interaction/combat; unattended crushers, environmental kill loops and non-player attacks cannot produce harvest loot there. This explicit realm spawn/drop policy prevents a home-side player from farming realm creatures through a Gate indefinitely. Companions are not renewable realm-resource generators after export.

Local processing uses only player-proximity production eligibility and pauses when the last nearby player leaves. Portal-following leases cannot maintain it. Physical worlds retain ordinary machinery and water channels. These distinctions are visible in machine placement/status and world rules, not hidden exceptions discovered after resources are spent.

Acceptance: hostile pursuit through one Gate; escorted livestock and a cargo companion; dropped ore/wood against the threshold; an unattended circulating creature; a player leaving a realm processor running; environmental realm mob farming; many simultaneous arrivals; save/reload during a following window. Preserve entity/item identity, bounded work and the player's return ability in every case.

## 39. Working rocket, route and realm-network resolutions

### Planet-specific realm instances

A Gate network belongs to exactly one physical planet and one realm instance. Identity includes universe, physical-world ID and network ID. Different planets may share realm themes, but they never share the same instance/exit routing. Thus Home -> Realm -> Moon cannot bypass rockets. Any deliberate future cross-planet Gate feature requires an explicit revision of this rule.

### First landing and stable coordinates

Select a deterministic landing anchor from the departure X/Z before launch, searching within an initial 16-block horizontal radius on the destination. A suitable site must have a safe footprint/clearance and an owned arrival reservation. If no site exists, reject launch preparation without consuming the vehicle, fuel or cargo. The player can move the launch site; the system does not silently widen the search.

Persist the expedition route's canonical X/Z and both actual pads/landing sites. Subsequent trips and returns use those recorded sites, not a new offset from the most recent landing. Creating a route at an offset arrival pad reuses its canonical anchor; a pad cannot belong to several incompatible anchors, and conflicting reserved route footprints are rejected. To create a new canonical route elsewhere, the player must physically move beyond the existing reserved footprint and build new infrastructure. Repeated clicking, pad dismantling/rebuilding in that footprint or alternating destinations cannot accumulate lateral displacement.

Only the first site selection permits the small safety offset. Established cargo routes require registered endpoints; relocating a receiving pad invalidates the route until a new explicit route is established under these coordinate rules. The route/landing records survive temporary pad destruction and do not disappear on chunk unload.

### Player and cargo flight lifecycle

Use a persistent flight record: prepared -> launched -> travelling -> awaiting destination -> landed/unloaded. A record owns the vehicle and cargo exactly once during flight. Visual ascent/descent does not duplicate a stored vehicle entity.

For a player expedition, destination reservation must be ready before launch commits. Carry sufficient reserved return capability for the first expedition; landing does not assume the player can manufacture fuel on an unknown world. Actual propulsion/return-fuel recipes remain aerospace content design. A failed post-launch destination uses a safe holding state and the reserved return endpoint; never eject the player into unloaded terrain.

Cargo dispatch validates fuel, source contents and a receiving reservation before committing. On full, removed or inaccessible receiving pad, keep cargo in the flight's durable hold, report the cause and retry only while the route owner is online. Initial retries occur every 10 eligible seconds; stop automatic retries after 6 failures until conditions change or the owner requests another attempt. Holding uses metadata, not an orbiting physics scene, and consumes no additional fuel. Delivery requires space for the complete manifest initially; partial delivery is deferred to avoid ambiguous retries.

Owner disconnect pauses automatic flight progress/retries at the next committed step and releases destination production tickets; reconnect resumes from remaining eligible duration without offline catch-up. Player proximity can operate ordinary factories but does not impersonate a route owner's dispatch authorization. An already committed delivery remains delivered. A player flight is handled by player transfer/reconnect recovery rather than the automatic-cargo pause policy.

A route owner can request return if a source pad is available, using the vehicle's reserved return capability. If neither endpoint is usable, the manifest remains recoverable in the route interface as a held flight pending rebuilt infrastructure; it never silently becomes lost loot. Detailed vehicle loss from deliberate combat is outside the baseline transport model.

### Sparse but discoverable Gates

Use one total major anchor per 2,048x2,048 macro-region initially. Assign each region one network identity in a deterministic balanced regional pattern; each realm receives only its network's subset of anchors. Hash each anchor into the central 1,024x1,024 window of its region. This guarantees at least 1,024 blocks of separation between major anchors, including different networks, without exploration-order-dependent rejection. There is not one independent anchor per network in every region.

Terrain accommodates the shared X/Z anchor, with world-specific Y and ruin state. It does not reject the anchor after generation and leave an accidental void. Clues form a bounded regional hierarchy: ordinary small ruins may reveal a direction/region, then nearer architectural remnants lead toward the Gate. No undiscovered exact map marker is awarded automatically. The macro-region and jitter values are tuning data; test boundary spacing and discoverability before final adoption. Multiple network themes can be interleaved without clustering independent major anchors.

## 40. Transport implementation acceptance

Gate testing must include removal of every optional repair module after a player departs, attempt to build/flood the sanctuary, cold-load cancellation, same-entity retry after crash, companion inventory transfer and unlimited non-player ticket-refresh attempts. The fallback must work without hidden permanent chunk loading or per-traversal fuel.

Rocket testing must include invalid terrain before launch, blocked full delivery, deleted/rebuilt pad, owner disconnect in flight, recovery after committed delivery and repeated first-landing offset attempts. Check canonical X/Z and exact vehicle/cargo ownership at each state transition.

These rules resolve the previously open policies; manufacturing costs, final visual presentation, cooldown tuning and benchmarked concurrency remain adjustable. [DESIGN_QUESTIONS.md](DESIGN_QUESTIONS.md) records the working decisions and remaining validation.
