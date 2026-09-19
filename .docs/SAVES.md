# Save, load and continue

The user authorized durable single-player saves and a rebuilt **0.0.1 Alpha** on 2026-09-11. This supersedes earlier session-only limitations for the implemented surface world. [Verification](verification/SAVE_RESULTS.md) records measured evidence and remaining limits.

## Player controls

- **Escape → Save Game** names a checkpoint. **Update This Save** replaces the current slot; **Save As New Slot** creates an independent checkpoint. New expeditions start empty-handed and have no save until written.
- **Escape → Load Game** lists named saves and previous backups, newest first, with local date/time and seed. Loading replaces unsaved progress. Failed loads preserve the current expedition.
- **Continue Latest Save** on the title screen loads the most recently saved compatible checkpoint, falling back to an earlier checkpoint or previous backup if necessary. The button is disabled when none are available.
- **Save & Title** and **Save & Quit** save before leaving; a write failure keeps the game open with an error. **Quit Without Saving**, closing the window and terminating the process discard changes since the last checkpoint. There is no periodic autosave. In Unity Editor Play mode, Quit stops Play; in a standalone player it closes the application. Both paths preserve the last checkpoint without saving new world progress.
- Death offers Save Game and Load Game alongside Respawn. Saving a dead player preserves the death state and the dropped possessions.

Saves live in `%USERPROFILE%\AppData\LocalLow\Starbugstone\Rivet Reach\Saves`. Each slot uses a generated identifier as its `.rrsave` filename; the display name stays inside the file. Its `.rrsave.bak` file is the previous checkpoint. Copy both files to back them up or transfer them between installations with matching content. Save names never become filesystem paths.

## Persisted state

A checkpoint captures one coherent paused authority turn, including:

- Seed, generator/content compatibility, owning world identity, day/time and lunar phase.
- Integer player position plus local fractions, look direction, crouched height, vertical/fall state, selected hotbar slot, model/skin, health, hunger, exhaustion, regeneration and short respawn immunity.
- Every inventory/equipment slot and retained personal, workbench and Machinist's Bench grid ingredient. Cursor contents return through the existing conservation path before saving; overflow remains a dropped stack.
- All terrain edits, including unloaded chunks, mined ores, placement provenance, crops, water sources/flow cells and floor/wall torch attachments. Untouched terrain regenerates using the pinned generator.
- Chest contents, furnace input/fuel/output, remaining burn and partial work, crop deadlines and the survival clock.
- Machine inventories, partial work, fuel, water, drill depth, orientation, priority, signal source/relay/button state, pipe fittings, valve/port/sensor configuration, tank identity and exact fluid quantity/capacity, and exact energy on each physical battery cell. A breached tank keeps its recovery contents.
- Dropped-stack identity, position, velocity, age, pickup delay/provenance and sleeping state. Existing creatures retain identity/species, health, position/home, intent, combat timers and movement state.
- Pending felling/leaf-decay jobs, sapling growth deadlines and grown-tree provenance, leaf drop sequence, fluid deadlines and sleeping frontiers, grass tick and simulation remainders.

Creative mode and flight remain **session-only** and reset to Survival/off when loading. Items/buildings obtained in Creative persist. Input bindings, graphics/audio/UI settings remain local preferences. Network graphs, multiblock membership, mesh jobs and AI paths are rebuilt from saved authority data; mob wandering uses a newly seeded random sequence rather than preserving `System.Random` internals. Save/load is not a deterministic replay of every future presentation or wandering choice.

Simulation waits for terrain at the restored player before resuming. It grants no offline time, item production, crop growth or lifetime expiration. Existing chunk residency rules still control distant production.

## Storage and compatibility

The current schema **17** is an explicit binary snapshot with a bounded payload and SHA-256 integrity checksum, written on the main authority thread while paused. It stores a world-definition identity (`rivet:surface`) separately from the owning world GUID. The current payload supports this implemented world; other planets, multiplayer and cross-world transaction journals remain later work.

The file pins its saved generator version and a content fingerprint built from stable ordered item, recipe, fuel and mob definitions. Incompatible versions/content reject visibly. Schema 2 adds the active-healing flag so the 60%/50% food band survives reload. Schema 1 checkpoints remain readable through an explicit compatibility path: their missing healing flag defaults to off and is reevaluated from food on the next survival tick. All other schema 1 fields retain their layout. The sapling/apple extension introduced schema 3; current saves use [schema 17](#food-balance-compatibility--2026-09-19). See [its compatibility rules](#sapling-and-apple-compatibility--2026-09-12) below for schema 1/2 loading. The 2026-09-11 ore-duration floor and origin-bounded respawn policy deliberately retain schema 2 and the content fingerprint: they change future interaction/placement decisions, introduce no serialized fields and reinterpret no stored resources. Existing compatible checkpoints retain their full state and use the revised rules after loading. There is no silent regeneration under a new generator or dropped unknown item; unrelated schema/content migrations remain unsupported. Changes to code-defined simulation/storage rules must deliberately revise the schema or compatibility boundary. SHA-256 detects damage; it is not authentication or cheat prevention.

Capture precedes filesystem mutation. A new temporary file in the same directory is flushed, then moved into a new slot or atomically replaces the existing file while retaining the previous checkpoint. A corrupt primary never overwrites a valid backup during recovery. Incomplete temporary files are ignored. Loading validates the envelope, stages and validates all state, and only then retires the previous session. Invalid body data restores the original references and mode; the load error remains visible.

This alpha uses complete snapshots, capped at **256 MiB payload**, and synchronous save/load. Disk usage grows with edited terrain and retained state. Region-level incremental saves, background serialization, format migrations, cloud sync, exhaustive power-loss testing and large-world save-latency guarantees are not part of this increment.

## Additive hand-crank compatibility — 2026-09-12

[The hand-crank extension](HAND_CRANK.md) retains schema 2 and adds an explicit compatibility fingerprint omitting only the new `rivet:hand_crank` item and `rivet:industry_170` recipe. Pre-crank checkpoints are accepted only if all previously fingerprinted definitions still match. Changes to other definitions remain incompatible. New checkpoints use the complete new fingerprint and require the newer executable. Crank placement/orientation uses ordinary saved machine state; its existing `PulseTicks` field preserves up to ten remaining paid ticks without offline progress. Exact battery energy remains on physical cells.

### Wooden door compatibility

[Doors](DOORS.md#state-and-assets) preserve their paired cells, orientation, manual request, physical latch and last signal using the existing machine record. Loading validates the footprint and state. Older pre-door fingerprints remain acceptable only when all earlier content definitions still match; the existing pre-crank compatibility remains supported.


## Sapling and apple compatibility — 2026-09-12

The [saplings and apples extension](GAMEPLAY.md#saplings-and-apples) writes schema **3**. It appends the natural-leaf harvest sequence and the positions of sapling-grown log/leaf cells to the world section; sapling deadlines reuse the existing plant schedule. Grown provenance is validated against actual edited log/leaf cells, with duplicate entries rejected. Replacing a grown cell clears its provenance. These fields preserve renewable-tree behavior and random drop progression through streaming and reload, without offline growth.

Schema 1/2 retain their exact original section layout and load with no grown cells or prior leaf-harvest sequence. Their compatibility fingerprints omit only newly added sapling/apple definitions, with the existing explicit hand-crank and wooden-door additive paths preserved. All pre-existing item, crafting, processing, fuel and mob definitions still participate; an unrelated definition change rejects rather than silently migrating. That extension used the complete orchard fingerprint and schema 3; the wrench extension below introduced schema 4; current saves use schema 14. Earlier schema statements describe their dated releases. [Orchard verification](verification/ORCHARD_RESULTS.md) records actual legacy-save and round-trip evidence.

## Wrench and pipe-end compatibility — 2026-09-12

The [all-face connection and wrench revision](INDUSTRY.md#wrench-and-configurable-pipe-ends--2026-09-12) writes schema **4**, appending one validated direction word to each machine record. Two bits per world-facing end encode uninitialized/default (0), Input (1), Output (2), or No connection (3). The disconnected extension uses the previously reserved value without changing the record layout or then-current schema 5; existing 0/1/2 settings retain their meaning. Out-of-range bits and nonzero settings on non-pipes reject the load. Disconnected ends survive load and residency rebuilds without restoring defaults. Checkpoints using No connection require the updated reader; older executables reject that value. Ordinary inventory state stores the nonstacking wrench. Configured ends, fitted channels, exact energy, fluid, inventory and work all round-trip together.

Schemas **1–3** keep their original byte layouts. Their absent pipe directions are initialized from the old orientation/port defaults when endpoints become resident. Existing inlet/outlet layouts remain the defaults; all-face power becomes available after rebuilding topology. Subsequent rotation does not flip initialized ends. Legacy fingerprints omit only the newly introduced wrench item/recipe while retaining the existing orchard, crank and door compatibility branches. Every earlier item, recipe, fuel and mob definition is still checked; unrelated changes reject. New schema-4 saves require the full current fingerprint and a schema-4 reader. Atomic replacement, previous-checkpoint recovery and failed-load rollback remain the shared save authority.


## Station facing compatibility — 2026-09-12

[Player-facing placement](GAMEPLAY.md#placement-and-inventory) introduces schema **5** with a horizontal quarter-turn integer (0–3) immediately after each station block ID. The furnace, chest and workbench retain this value independently of streamed visuals. Industrial machines continue using their existing rotation record. No content fingerprint changes are introduced.

Schemas **1–4** retain their original byte layouts and initialize the previously absent station rotation to zero, preserving their original visual direction. Existing machine rotations, pipe directions, resource state and earlier additive content checks remain intact. Invalid rotation values reject through the existing failed-load rollback. Schema 5 checkpoints require the updated executable. [Facing verification](verification/PLACEMENT_FACING_RESULTS.md) records migration and round-trip evidence.

## Expanded player inventory — 2026-09-12

[The expanded inventory](GAMEPLAY.md#9-working-interaction-specification) writes schema **6**: 15 hotbar slots followed by 56 backpack slots (seven rows of eight). The existing length-prefixed player inventory record now contains 71 stacks; the selected hotbar index accepts 0–14. Other containers retain their exact size checks and layout. No content fingerprint changes are introduced.

Schemas **1–5** still require their original 60 stacks and selected index 0–11. Loading keeps the first 12 hotbar slots in place, maps the 48 backpack slots to the same row/column behind the larger hotbar, and leaves the three added hotbar slots and eight added backpack slots empty. Unknown sizes and invalid stacks reject through the existing rollback path. Earlier content checks, station rotations, world/resource state and atomic checkpoint handling remain intact. Schema 6 checkpoints require the updated executable. [Inventory verification](verification/INVENTORY_RESULTS.md) records focused evidence.

## Electric furnace compatibility — 2026-09-13

[Electric furnace persistence](ELECTRIC_FURNACE.md#persistence) adds an explicit content-only compatibility path and recipe-derived work validation for the new machine. It adds no serialized fields or schema revision.

## Lava and generator compatibility — 2026-09-13

Lava writes **schema 7**, adding remaining burn ticks and the heat-damage cooldown after the existing health/healing fields. Both are validated and restored without offline advancement. Schemas 1–6 keep their original binary layouts and initialize both values to zero. Lava cells, filled buckets, fluid scheduler deadlines and exact generic tank contents use their existing authority records; tank loading resolves the registered stable fluid identity and rejects unknown identities.

New sessions use `terrain-7-lava`. The save envelope now records the actual world generator rather than a global default. `terrain-6-azure` remains an explicitly supported generator with its exact pre-lava cavity rules. Loaded worlds and subsequent checkpoints preserve that identity, preventing terrain regeneration or lake insertion in older worlds. Unknown generator versions still reject.

The new fingerprint includes `rivet:lava_bucket`. Earlier compatibility paths omit only this addition alongside their existing explicit electric-furnace, wrench, orchard, door and crank additions. Previously registered items, crafting/processing/fuel/mob definitions remain checked; unrelated changes reject. Schema 7 retains the independently additive electric-furnace and lava content branches; each omits only those new definitions and checks all earlier content. Atomic replacement, recovery checkpoints and failed-load rollback remain shared. Creative fire immunity stays session-only and Creative resets on loading.

## Portable storage compatibility — 2026-09-13

Schema **8** adds exact carried storage to every item-stack record: integer millijoules, fluid millilitres, retained fluid capacity and registered stable fluid identity follow the existing item ID/count. Nonempty storage requires count 1, a matching battery/tank/controller item, valid bounds and exactly one resource kind. Unknown liquids, stacked contents, negative quantities, over-capacity contents and payload on unrelated items reject through failed-load rollback. Empty payloads are canonical zeros with no fluid identity. Inventory, crafting grids, station containers, cursor returns and dropped stacks share this encoding.

Each small machine fluid amount additionally stores its stable liquid identity, allowing a standalone Water Tank to contain lava. Water-only pump/boiler buffers reject other liquids. Existing multiblock fluid and placed battery records retain their exact accounting.

Schemas **1–7** keep their previous byte layouts: absent carried contents initialize empty, and old small-machine amounts initialize as water. This change adds no item or recipe definitions and keeps existing content fingerprint checks, pinned terrain generators, atomic checkpoint replacement, previous-checkpoint recovery and failed-load rollback. New checkpoints require the schema-8 reader; Creative remains session-only.

## Floater compatibility — 2026-09-13

[The Floater](MOBS.md#floater--2026-09-13) and its rock are additive content, compatible with pre-Floater schema-7 checkpoints and the current schema-8 item layout. Existing mob position/health/intent records and generic item stacks preserve both without changing payload layout. The current content hash includes the new definitions and appended hover/drop fields; pre-Floater compatibility retains the original mob-field JSON and excludes only the new species/item. Existing definition and recipe checks remain enforced. Defeat creates loot once before checkpoint capture; restoring an already-dead creature does not create another drop. [Verification](verification/FLOATER_RESULTS.md).

## Bridge ownership and chunk-loader compatibility — 2026-09-13

[BRIDGES.md](BRIDGES.md#persistence-and-verification) owns the additive schema-9 owner identity, bridge names and loader enablement fields. Loader tickets and remote graph edges rebuild from saved machine records, including destinations outside player range. Existing envelope checks, atomic writes, rollback, legacy content validation and absence of offline production remain.

## Ranged pump compatibility — 2026-09-13

[RANGED_PUMP.md](RANGED_PUMP.md) adds a machine without new serialized fields. Its registered liquid type, exact buffer, partial work and removed sources use existing records. Additive compatibility omits only its item and recipe for earlier checkpoints while retaining all pre-existing content checks. Search caches rebuild after load without offline production.

## Generated-chunk policy — 2026-09-13

**Hard user rule for every future update:** new terrain content appears only in previously ungenerated chunks. Do not upgrade or retrofit existing terrain. This supersedes earlier statements pinning all future exploration to one saved world-wide generator. Each generated chunk retains its generation version, including chunks whose boundary cells have been sampled for neighboring meshes. Unseen chunks use the latest supported generation. Point reads, mesh interiors and halos resolve the same saved per-chunk version; worker snapshots never read mutable dictionaries.

Schema **10** records the generated-chunk/version ledger and conservative legacy-column reservations. That release introduced `terrain-8-farms`; current unexplored terrain uses `terrain-9-spawners`, while earlier `terrain-7-lava` and `terrain-6-azure` remain available solely to reconstruct their retained chunks. Future releases must preserve recorded versions or supply stored terrain, never silently substitute newer generation. Edits remain exact overrides. No offline simulation is added.

Schemas 1–9 did not record untouched exploration history. The user explicitly approved a conservative fallback: preserve the original generator in full-height columns within one chunk of saved edits and dropped/creature anchors; preserve the current view radius plus one chunk around saved player and original spawn. Machines, stations and planted crops are included through saved edits. Remaining unreserved terrain adopts the new generator. Some previously explored but untouched legacy terrain cannot be identified; exact history protection begins with schema 10. This one-time bookkeeping boundary does not retrofit plants into known established regions.

[Cookers](FARMING.md) append their selected stable food-recipe identity and ingredient signature to machine records. Existing slots, stored fuel heat, partial work and electrical storage retain their authorities. Appended item tags and new farm items/recipes/catalogs have an explicit additive content compatibility projection; pre-existing content still participates in compatibility checking. Atomic replacement, backup recovery, full-state validation and failed-load rollback remain required.

### Material-tag review compatibility — 2026-09-13

The stability review appends `log`, `planks`, `raw_ore` and `ingot` only to their known original item definitions. A narrowly scoped fingerprint projection accepts the earlier complete schema-10 farming catalog by removing those exact additions. It retains `edible`, all previous tags, item statistics, recipes and crop/cooking catalogs in compatibility checks. This does not relax unknown-content checks or change the world body/schema.

## Material-tier recipe compatibility — 2026-09-13

The [bridge](BRIDGES.md#crafting) and [ranged-pump](RANGED_PUMP.md#working-rules) recipe rebalance changes no save fields or schema (10). New saves fingerprint the new gold/diamond costs. An explicit historical recipe projection also accepts checkpoints with the exact earlier four recipes, across the existing supported content variants. It removes only the known appended two-gold ingredient and, for bridges, two-diamond ingredient while hashing; original ingredients, their quantities, station, output and every unrelated definition remain checked. Unknown altered costs are not normalized into compatibility. Loading does not recraft, charge, refund or replace machines/items already saved.

## Compost compatibility — 2026-09-13

[Compost](COMPOST.md#persistence-and-compatibility) reuses schema-10 machine slots/work and crop deadlines. Its additive content fingerprint includes the new catalog; known pre-compost fingerprints remove only the two added items, bin recipe and frozen compostable tags. Existing content checks, atomic replacement, previous-tier recipe compatibility and generated-chunk history remain required.

## Mob spawn-profile compatibility — 2026-09-13

[Shared mob spawning](MOBS.md#shared-hostilepassive-spawning-rules) adds authored habitat and support-block rules, and moves Floater spawning to underground caves at any hour. Schema 10 world/entity payloads remain unchanged. Current content fingerprints include each mob’s full spawn profile. Older fingerprints are accepted only through the known migration: the original support whitelist, Surface for beetle/prowler, and Underground plus non-nocturnal timing for Floater projected back to its former surface/night definition. Other habitat, block-list, timing, mob-stat, item and recipe changes retain compatibility checks. Existing saved mobs keep their identity, health and position; no terrain is regenerated.

## Mob spawn-light compatibility — 2026-09-13

Spawn profiles append an inclusive light range without changing schema 10 or the entity/world payload. Current saves hash both limits. Compatibility accepts pre-light profiles only when the new hostile range is exactly 0–7, removing those two appended fields while retaining the historical habitat/support fields and every unrelated content check. The existing pre-habitat and older-content projections remain active. Unknown light tuning and altered support/combat/item/recipe definitions are not silently migrated. Light affects new natural spawn eligibility; loading retains existing entities and terrain unchanged.

## Mixed compost compatibility — 2026-09-14

[Revised compost](COMPOST.md#recovery-and-compatibility) uses schema 11 for accumulated points, batch sequence and autocomposter charge. Existing schema-10 bin inventories survive migration; unpaid timer work is discarded, inputs are converted only on interaction, and finished output is ejected. Existing bins become manual-only; no free automatic upgrade is granted. The exact previous content fingerprints remain accepted with unrelated item/recipe/mob checks intact. Failed-load rollback and generated terrain history are unchanged.

## Fishing compatibility — 2026-09-19

[Fishing](FISHING.md) keeps schema 11. Rods and fish use stable registered item identities; new cooker recipes use the existing recipe/signature state. Exact pre-fishing fingerprints omit only the added items, rod recipe and separate fish-cooking catalog. Unrelated historical content checks remain enforced. Menus cancel transient casts before saving; loading grants no offline catch and starts with no cast. Failed-load rollback restores the original per-session fishing authority with the rest of the original session.

## Persistent chickens — 2026-09-19

Schema **12** appends a separate passive-animal section after the hostile records. It stores stable chicken identities, position/home, health, growth, egg, breeding readiness/cooldown and deterministic random state, plus the passive scheduler. Views and navigation are reconstructed. Dead animals have already left this collection before capture, so reload cannot award their drops again. Distant/unloaded and offline time never advances this lifecycle.

Formats 1–11 have no passive section. Known previous fingerprints explicitly exclude chicken items and the two new definition texts; unrelated compatibility checks remain enforced. Failed loading restores the old passive collection and its interaction source along with the rest of the old session. [Chicken rules](CHICKENS.md) and [verification](verification/CHICKEN_RESULTS.md) own the functional evidence.

## Alpha spawner compatibility — 2026-09-19

Schema **13** preserves schema 12's chicken records and adds a hostile mob's spawner-origin ID plus cage identities, configured definition, candidate sequence and remaining cooldown. The cage section follows hostile records and precedes the unchanged passive-animal section. Legacy hostile origins default to zero (natural). Broken cages may leave living mobs with an orphan origin; loading never recreates their cage or reuses that identity.

Validation checks unique cage identities/positions, registered definitions, actual cage blocks, finite timers, origin references and per-origin live limits. Natural live mobs retain the 14-mob limit; spawner-origin mobs are validated independently. The prior atomic replacement and failed-load rollback remain authoritative.

Exact schema-12 fingerprints omit only Lava Rock, Mob Spawner, spawner definitions and the known cobblestone/plank hostile-support extension. Earlier additive compatibility projections remain, including all chicken and cooking definitions for saves that contain them. Unrelated definition changes continue to reject. The new `terrain-9-spawners` generator applies only to ungenerated chunks; all recorded older generators remain available without terrain upgrades. [Spawner rules](SPAWNERS.md) and [Alpha verification](verification/ALPHA_PLAYTEST_RESULTS.md) own implementation details and measured evidence.

## Bed and home compatibility — 2026-09-19

Schema 14 appends the bed section after the existing passive animals section. It records the next placed-bed identity, each foot/orientation/identity, optional home foot/identity and configured sleep percentage/minimum. Restore cross-validates every foot/head pair and both supports, rejects duplicate identities, overlapping/orphan cells and internal head inventory items, and retains removed-home bindings for the visible fallback. Bed data reads only into a fresh staged world; failed late reads restore the untouched old world and PlayerBeds together.

Schemas 1–13 begin without beds/home and retain their exact previous layouts. The new fingerprint omits only the Bed item/recipe when accepting the exact previous schema-13 content; all existing chicken, spawner, material and recipe checks remain. No terrain generator is changed, no existing chunk is regenerated, and sleep persists the advanced celestial time without skipped simulation production. [Bed rules](BEDS.md) and [verification](verification/BED_RESULTS.md) own the feature details and measured evidence.


## Bulk crate compatibility — 2026-09-19

Schema **15** preserves schema 14 and appends compact receiver state for the new routing configuration. A Bulk Crate records its assigned item ID, count from 0 through **16,384**, manual-lock flag and receiver priority from 0 through 100. A Crate Controller records its receiver priority but never duplicate aggregate contents; its connected crates are rediscovered from resident face topology after restore. The schema also preserves the configured routing priority of compatible existing receivers. Validation requires the saved station/block identity, a registered ordinary item for a nonempty or locked crate, and the consistent empty/unlocked representation.

Schemas 1–14 retain their exact station layouts and start without crates/controllers. The schema-15 fingerprint admits the exact schema-14 projection by omitting only the two crate items, recipes and crate contract marker; chickens, beds, spawners and all earlier compatibility checks remain active. Failed loading restores the prior whole session, including crate authorities, rather than partially applying a new bank. No generator history or existing terrain is changed. [Crate rules](CRATES.md) own behavior; [verification](verification/CRATE_RESULTS.md) records native roundtrip, rollback, restart and historical-load evidence.

## Weather — 2026-09-19

Schema **16** appends [weather](WEATHER.md) after the bed section: target/previous kind, remaining and transition ticks, deterministic RNG and interpolation start factors. Loading reconstructs the same transition and future schedule; no offline advance occurs. Formats 1–15 begin with deterministic clear weather and consume no weather payload. Invalid kinds, durations, factors or RNG fail the transactional restore and retain the original world and weather. Cosmetic rain positions and thunder/flash timing restart on load. Registry compatibility checks remain unchanged.

### Renewable catalog compatibility

[Renewables](RENEWABLES.md) retain schema **16** because their one-cell machines use the existing generic machine record and save no private generation state. The current fingerprint includes the two registered renewable items, their two 4×4 recipes and `Definitions/Renewables`. The exact pre-renewable schema-16 projection removes only those additions; all earlier item, recipe, processing, fuel, mob and configuration checks remain active. Existing checkpoints therefore load without renewable machines, while an unrelated altered definition still rejects transactionally.

## Food balance compatibility — 2026-09-19

Schema **17** appends the player's integer saturation reserve after the existing food and exhaustion fields. It is bounded from 0 through 20. Schemas 1–16 retain their layouts and initialize the reserve to zero. The current content fingerprint adds the exact `Definitions/FoodBalance.json` text; an altered or unknown balance catalog rejects transactionally. The explicit full-renewable schema-16 fingerprint remains accepted by projecting away only this food-balance configuration, while retaining every renewable and earlier content check. [Food balance](FOOD_BALANCE.md) owns gameplay values and pending verification.
