# Native mobs — first playable implementation

> **2026-09-11 persistence extension:** [SAVES.md](SAVES.md) owns the implemented Save Game, Load Game and Continue Latest Save behavior. Its bounded surface-world persistence supersedes earlier session-only/durable-save exclusions below; older verification retains its original artifact identity.

The user authorized Blender-authored enemies, AI and spawning on 2026-09-09 and chose **Rustback beetle** and **Dusk prowler**. These are original native creatures; they do not imply ancient technology, generated structures, raids or the rest of the full-game roadmap. Art and tuning remain available for user review.

## Playable rules

| Species | Behaviour | Health | Hit damage | Spawn rule |
| --- | --- | ---: | ---: | --- |
| Rustback beetle | Wanders and climbs walls; warns for 1.4 seconds when approached within 5 m; attacks if the player stays or strikes it | 12 | 2 HP | Supported surface, day or night; up to 8 |
| Dusk prowler | Detects a visible player within 16 m, pursues and bites | 18 | 3 HP | Supported surface at night; up to 6 |
| Floater | Hovers 0.6 m above terrain, pursues a visible player within 14 m and punches | 16 | 3 HP | Supported surface at night; up to 4 |

Night comes from the day/night agent's authoritative `WorldClock.IsNight` (18:00 through 05:59). There is no second mob clock. Dawn stops new prowler spawns; existing prowlers remain normal living entities. Beetles return home beyond a 20 m leash; prowlers beyond 30 m. Breaking sight for four seconds also ends pursuit. Creatures never mine terrain or raid player machinery.

Hold the existing mining/attack button while aiming at a creature within 3.2 m. The same first-person swing and selected item apply. Damage comes from the shared item definition’s `attackDamage` field: fists deal 1 HP; blades use wood 4 / stone 5 / copper 5 / iron 6 / diamond 7 HP, with separately authored axe and other tool values. Blade cooldown is 0.30 s; axe 0.55 s; other items and fists 0.40 s. The survival/content owner authors tier damage in one registry; mobs do not keep a second weapon table. The aimed creature gets a name/health bar; hits tint and stagger the model, interrupting its current action, and its death action finishes before the body disappears. Floaters drop exactly one Floater Rock on defeat; other mob loot, ranged attacks and breeding remain future content.

Bites have a visible preparation action/tint (0.8 s beetle, 0.65 s prowler), then recheck range, height, line of sight and player state. Back away or put solid terrain between you and the attack. The recovery interval is 1.2/1.1 s, with a shared 0.65 s minimum between accepted mob hits. Damage uses `Expedition.TakeDamage` and its existing armor, health, death and respawn rules. Mob code does not create another player-health system. Inventory retains the shared game time rule; pause stops mob simulation.

Player movement resolves lateral contact with living creatures; block placement rejects their bodies. The beetle's collision footprint is 0.90 m square and the prowler's is 0.85 m square, allowing their bodies to navigate one-voxel lanes and steps; extended legs, ears and tails are visual appendages. Attacking a creature takes priority over mining the block behind it. Dead creatures cannot receive a second defeat or block construction.

## Beetle wall climbing

The user subsequently requested wall-climbing beetles. The Rustback definition enables `climbsWalls`, with a working climbing speed of **1.8 m/s**. The same navigation graph now includes vertical edges on loaded solid wall faces, and connects those edges to the supporting top surface. Climbing works during pursuit, wandering and return movement; existing detection, line-of-sight, warning and leash rules still apply.

Two grip probes check the actual wall voxels on each move. Vertical edges recheck support and body clearance along the route. Removing support releases the beetle into normal gravity; missing wall bands, solid overhangs and unloaded cells cannot supply a climbing route. A short checked lip transition lets it pull onto the top or descend over an edge. Climbing does not destroy terrain or add ceiling traversal.

The imported Blender rig turns around its body centre so its feet face the wall and its head follows movement along the surface. Its existing leg action follows climbing speed. Authoritative position, damage, targeting and the compact collision body remain in the same `MobState`/voxel movement system. Prowlers retain their ground-only navigation. The existing 20 Hz simulation and per-step search budgets apply to both routes.

[Wall-climbing verification](verification/BEETLE_WALL_CLIMB_RESULTS.md) records the focused evidence and review limits.

## Spawn and simulation boundaries

Working defaults: at most 14 tracked ambient mobs, a 24–48 m spawn ring around the player, and a protected 16 m radius around the original world spawn. Spawn attempts run every two simulation seconds, with at most eight candidates per attempt. A candidate needs sky access above its body, actual loaded terrain, a solid supporting surface across the collision footprint and clear space for the full body. Spawning rejects the current visible camera area when unobstructed. Surface samples use the current terrain generator only as a search hint, then validate actual edited voxels (grass, dirt, stone, sand, sandstone, snow or red clay); leaves, cave voids and unloaded borders are not valid support.

One session `MobSystem` owns stable instance IDs, health, intent and `WorldPoint` positions. The renderer and animations contain no combat authority. Simulation runs at 20 Hz with a bounded catch-up of four steps/frame. At most two path searches are performed per step, with 96 node expansions and 256 stored nodes each. The local cardinal voxel search permits ground steps/drops of one block and beetle wall edges, checks overhead clearance, and rechecks movement against current terrain edits. Full maze navigation, water traversal and unrestricted flying remain outside this behaviour set. The Floater extension below adds low hovering through surface routes.

Views and thinking sleep beyond 68 m or when the entity's chunk is unavailable. Ambient mobs beyond 112 m are removed and may be replenished by later spawning. This is a bounded ambient population, not persistent named creatures: mob identity/health survive floating-origin shifts and temporary sleep within the active session, and are included in durable saves. Ambient distance removal does not count as a defeat and produces no loot. Initial combat grace is ten gameplay seconds; the shared `Respawned` event resets eight seconds of mob grace in addition to survival-owned respawn immunity. A large relocation also triggers this grace as a fallback.

## Source and authoring

- `Tools/create_mob_assets.py` reproducibly authors both original meshes, rigs, shared 64×64 palette, four actions and front/back review renders in Blender 5.2.
- Editable sources: `ArtSource/Mobs/RustbackBeetle.blend` and `DuskProwler.blend` (metric units, unit scale 1).
- Runtime exports: `Assets/RivetReach/Resources/Mobs/` contains FBX files, palette, shared URP material and separately editable definition assets.
- `MobAssetImport` enforces explicit FBX conversion, Generic animation import, preserved bone hierarchy, one material and readable meshes. A manually evaluated Playables graph blends the four authored actions, with stride/attack timing tied to movement and wind-up and no animation work for sleeping views. Runtime rotates the authoring forward axis consistently; no per-instance correction is required.
- Meshes share one normalized `CreatureUV` layer and one material each. The sources use nine bones per creature, with rigid segment weights. This keeps the small stylized animals readable; joint deformation/artistic acceptance remains a review concern.
- Source counts: beetle 904 triangles, prowler 1,476. These counts are geometry evidence, not a claim of whole-game performance.

All meshes, palette pixels and animations were authored for Rivet Reach. No third-party art, copied creature designs or additional dependencies were introduced. Existing proprietary ownership and notices apply.

## Build and verification

Use the pinned Unity 6000.4.4f1 Editor. `Rivet Reach > Mobs > Build Windows review` prepares the shared project assets, runs mob import validation and produces `Builds/Mobs/RivetReach.exe`; a batch process in an otherwise unopened checkout can execute `RivetReach.Editor.MobBuild.Build`. Preserve active Editor sessions and establish current ownership before sharing a build checkout.

Run `Tools/Verify-Mobs.ps1` after building. It invokes the explicit `-rr-mob-verify` mode and writes `Logs/MobVerification/mob-runtime-report.json` plus screenshots. That mode uses the actual gameplay authority, creates a bounded test platform, and checks loaded support, day/night spawn policy, population limits, collision/navigation, melee occlusion, telegraph/avoidance, health integration, player input, defeat and origin/distance lifecycle. Normal games never create test fixtures. Generic `-rr-verify` runs disable natural mob spawning to keep pre-existing terrain/inventory tests reproducible.

The [initial mob verification results](verification/MOB_RESULTS.md) record 56 passing standalone checks, imported geometry and actual day/night/combat screenshots. The [wall-climbing revision](verification/BEETLE_WALL_CLIMB_RESULTS.md) extends this to 79 passing checks and records the current wall behavior, screenshot and review limits. Completed integration messages remain in Git history.

## Floater — 2026-09-13

The user requested a hostile floating rocky sphere with a face and arms, dropping a **Floater Rock** when killed. The original Blender model uses a faceted round body, a carved scowl, mineral eyes and jointed arms with clenched stone fingers. Its four actions provide idle hover, pursuit sway, a two-arm punch wind-up and a falling defeat pose.

Working defaults chosen for this increment: 16 health, 3 HP punches, 2.4 m/s pursuit, 14 m detection, 26 m home leash, 2.1 m attack range, 0.8 s wind-up and 1.3 s recovery. Night-only ambient spawning uses `WorldClock.IsNight`, the existing spawn ring/sanctuary/visibility checks, up to four Floaters and the unchanged total cap of 14. Dawn stops new spawns; existing Floaters remain hostile. These values need balance playtesting; they are not individually user-selected numbers.

The authoritative collision body's lower point stays 0.6 m above loaded supporting terrain, with a 0.9 m square footprint and 1.05 m height. Arms extend outside that compact body as the other creatures' appendages do. Hovering changes real position, targeting and construction occupancy, not just the renderer. The shared bounded voxel graph routes around walls and over one-block surface steps; ascent precedes horizontal crossing. Swept voxel movement blocks walls, ceilings and unloaded terrain. Ground probes retarget height after terrain edits, falling under gravity when no nearby ground is available. Floaters do not fly freely over deep gaps, climb vertical walls or mine terrain. Gentle animation is presentation only; pause and residency sleeping use the existing lifecycle.

`MobDefinition.deathDropId` resolves through the item registry at initialization. A live-to-dead damage transition creates exactly one item through `DroppedItems`, with ordinary pickup, stack limits and world hazards. Repeated hits and ambient removal cannot create drops. **Floater Rock** (`rivet:floater_rock`, runtime ID 175) stacks to 64. Its magical properties enable Floater hovering and the [bridge/chunk-loader crafting extension](BRIDGES.md); it remains a non-food, non-fuel resource rather than a terrain block. Its original Blender fragment model is shared by held/dropped presentation and its rendered icon. Inventory, chest, pipe transport and dropped-item persistence use existing item systems. [Ranged liquid pump crafting](RANGED_PUMP.md) uses one Floater Rock to upgrade a Pump.

The existing save format already stores mob stable IDs, position, health and intent, plus dropped stacks. The Floater adds no body-format change; the independent portable-storage work owns schema 8. New fingerprints include the Floater, rock and appended hover/drop fields; older fingerprints omit only this additive content and project the exact original mob fields. All existing mob, item and recipe content checks remain active. A save made during defeat stores both the dead mob and its already-created rock; loading never awards loot again.

Sources: `Tools/create_floater_assets.py`, editable `ArtSource/Mobs/Floater.blend` and `FloaterRock.blend`, runtime `Resources/Mobs/Floater.fbx` and `Resources/MobLoot/`. Source mesh counts are **1,266 triangles / 7 bones / 1 material** for the Floater and **116 triangles / 1 material** for the rock; These are geometry counts, not performance claims. [Floater verification](verification/FLOATER_RESULTS.md) records measured checks and limits. [Player guide](wiki/Floater.md).

## Passive-animal boundary — issue #10 first-pass decision

The user confirmed separate hostile and passive mob systems. Future chickens may share appropriate voxel movement, collision, targeting and damage primitives with hostile creatures, but need their own persistent lifecycle for breeding, chick growth, eggs and following. The ambient hostile distance-removal policy must not delete livestock. This farming/cooking increment records the boundary; it does not implement chickens or another passive animal.

## Player wiki

The [Mobs overview](wiki/Mobs.md) links [Rustback beetle](wiki/Rustback-Beetle.md), [Dusk prowler](wiki/Dusk-Prowler.md) and [Floater](wiki/Floater.md), with actual game captures and the current combat/spawning/persistence rules. Passive livestock remains future scope and must use the separate lifecycle boundary above.
