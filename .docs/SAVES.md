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

Schema **2** is an explicit binary snapshot with a bounded payload and SHA-256 integrity checksum, written on the main authority thread while paused. It stores a world-definition identity (`rivet:surface`) separately from the owning world GUID. The current payload supports this implemented world; other planets, multiplayer and cross-world transaction journals remain later work.

The file pins `TerrainGenerator.Version` (currently `terrain-6-azure`) and a content fingerprint built from stable ordered item, recipe, fuel and mob definitions. Incompatible versions/content reject visibly. Schema 2 adds the active-healing flag so the 60%/50% food band survives reload. Schema 1 checkpoints remain readable through an explicit compatibility path: their missing healing flag defaults to off and is reevaluated from food on the next survival tick. All other schema 1 fields retain their layout. The sapling/apple extension introduced schema 3; current saves use [schema 6](#expanded-player-inventory--2026-09-12). See [its compatibility rules](#sapling-and-apple-compatibility--2026-09-12) below for schema 1/2 loading. The 2026-09-11 ore-duration floor and origin-bounded respawn policy deliberately retain schema 2 and the content fingerprint: they change future interaction/placement decisions, introduce no serialized fields and reinterpret no stored resources. Existing compatible checkpoints retain their full state and use the revised rules after loading. There is no silent regeneration under a new generator or dropped unknown item; unrelated schema/content migrations remain unsupported. Changes to code-defined simulation/storage rules must deliberately revise the schema or compatibility boundary. SHA-256 detects damage; it is not authentication or cheat prevention.

Capture precedes filesystem mutation. A new temporary file in the same directory is flushed, then moved into a new slot or atomically replaces the existing file while retaining the previous checkpoint. A corrupt primary never overwrites a valid backup during recovery. Incomplete temporary files are ignored. Loading validates the envelope, stages and validates all state, and only then retires the previous session. Invalid body data restores the original references and mode; the load error remains visible.

This alpha uses complete snapshots, capped at **256 MiB payload**, and synchronous save/load. Disk usage grows with edited terrain and retained state. Region-level incremental saves, background serialization, format migrations, cloud sync, exhaustive power-loss testing and large-world save-latency guarantees are not part of this increment.

## Additive hand-crank compatibility — 2026-09-12

[The hand-crank extension](HAND_CRANK.md) retains schema 2 and adds an explicit compatibility fingerprint omitting only the new `rivet:hand_crank` item and `rivet:industry_170` recipe. Pre-crank checkpoints are accepted only if all previously fingerprinted definitions still match. Changes to other definitions remain incompatible. New checkpoints use the complete new fingerprint and require the newer executable. Crank placement/orientation uses ordinary saved machine state; its existing `PulseTicks` field preserves up to ten remaining paid ticks without offline progress. Exact battery energy remains on physical cells.

### Wooden door compatibility

[Doors](DOORS.md#state-and-assets) preserve their paired cells, orientation, manual request, physical latch and last signal using the existing machine record. Loading validates the footprint and state. Older pre-door fingerprints remain acceptable only when all earlier content definitions still match; the existing pre-crank compatibility remains supported.


## Sapling and apple compatibility — 2026-09-12

The [saplings and apples extension](GAMEPLAY.md#saplings-and-apples) writes schema **3**. It appends the natural-leaf harvest sequence and the positions of sapling-grown log/leaf cells to the world section; sapling deadlines reuse the existing plant schedule. Grown provenance is validated against actual edited log/leaf cells, with duplicate entries rejected. Replacing a grown cell clears its provenance. These fields preserve renewable-tree behavior and random drop progression through streaming and reload, without offline growth.

Schema 1/2 retain their exact original section layout and load with no grown cells or prior leaf-harvest sequence. Their compatibility fingerprints omit only newly added sapling/apple definitions, with the existing explicit hand-crank and wooden-door additive paths preserved. All pre-existing item, crafting, processing, fuel and mob definitions still participate; an unrelated definition change rejects rather than silently migrating. That extension used the complete orchard fingerprint and schema 3; the wrench extension below introduced schema 4; current saves use schema 6. Earlier schema statements describe their dated releases. [Orchard verification](verification/ORCHARD_RESULTS.md) records actual legacy-save and round-trip evidence.

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
