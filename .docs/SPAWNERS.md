# Mob spawners and rare rooms

Working implementation for [issue #12](https://github.com/Starbugstone/Rivet-Reach/issues/12). These are implementation defaults selected to make the requested Alpha behavior concrete, not measured final balance. [Verification](verification/ALPHA_PLAYTEST_RESULTS.md) records what has actually run.

## Shared definition and authority

A cage references a stable spawner definition; the definition references a registered mob species. The initial definition is `rivet:floater_spawner`, producing `rivet:floater`. Its working defaults are a **36-block activation radius**, **10-second attempt interval**, **five active origin-owned mobs**, **four-block horizontal candidate radius**, and **12 bounded candidate attempts per cycle**. At most one mob is produced by a successful cycle. The first attempt is delayed four seconds, clamped to the configured interval when an author chooses a shorter cycle. The cooldown pauses while the cage is outside player activation range or its chunk is not resident.

Every cage has a monotonically assigned world-session identity, persisted with its definition, location, candidate sequence and remaining cooldown. Every produced mob records that origin identity. Living mobs count against their own cage even after walking away; proximity to other cages never changes ownership. Death, ordinary despawn or chunk unloading releases the slot. Breaking a cage leaves surviving mobs under their normal lifecycle and never transfers them to a replacement cage at the same position.

Natural spawning counts only mobs without a spawner origin. Its existing global and species caps do not prevent a cage producing its own five mobs. This separation does not bypass body clearance, occupied volume, residency, support, habitat, species timing or light rules. Both creation paths use the same site validity contract. The cage also requires eligible light immediately above itself, so lighting its centre cannot merely redirect attempts to a darker candidate corner.

Sufficient placed light prevents the initial Floater cage from spawning; held torch glow remains visual-only. Pausing the game pauses its simulation. Cages do not load chunks or create persistent tickets.

## Appearance and interaction

The original Blender cage is an open metal frame with copper joints. Its rotating miniature displays the configured species. The miniature is render geometry only: it has no hostile state, AI, collider, loot or population entry. Nearby resident cages receive presentation objects independently of simulation state.

Breaking the cage destroys it and drops **no collectible spawner**. Alpha provides no Survival recipe or collection mechanism. A registry entry supports Creative testing and ordinary target identity; future collection may preserve the configured definition without changing the origin model. No such collection mechanic is included here.

## Rare dungeon room template

The reusable template has a 9 × 6 × 9 outer shell, cobblestone walls/floor/ceiling, a 7 × 4 × 7 hollow interior and one cage. Connected placement adds a bounded passage into a supported, three-block-high existing cave opening. Buried placement retains a sealed rock margin around the same room.

The initial generator considers 128 × 128 horizontal regions, selects 45% as candidates, and makes up to 96 deterministic placement probes per selected region. Those are **overall frequency controls**; terrain feasibility can reject a candidate region. They are separate from the **99% cave-connected / 1% buried placement-mode draw**. A region must supply viable positions for both modes before that draw selects the result, preventing different placement rejection rates from silently biasing the requested split.

Rooms are below the surface with a deep-rock margin, above the near-bedrock lava band and outside the immediate spawn area. The complete shell and passage fit within one 32³ chunk. Region queries use a bounded shared cache and deterministic seed/coordinate keys; point reads and chunk/halo generation use the same template.

The new generator identity is `terrain-9-spawners`. Previously generated chunks retain their saved generator identity, including `terrain-8-farms`; no established terrain is regenerated or retrofitted. Newly explored chunks use the new identity. The single-chunk template prevents a room from carving into an adjacent preserved old chunk. Changing shipped room defaults later requires a new generator identity. See [generation rules](TERRAIN_GENERATION.md) and [save history](SAVES.md#generated-chunk-policy--2026-09-13).

## Save boundary

Schema 13 extends schema 12 without changing passive-animal ownership or authority. The cage section sits after hostile records and before the unchanged chicken section. It adds hostile origin references and cage state, retaining exact prior content fingerprints through explicit projections. Legacy mobs have no spawner origin; old chunks remain free of newly introduced rooms. Failed loads retain the normal whole-session rollback behavior. Definition changes outside recognized additive migrations must still reject incompatible checkpoints.
