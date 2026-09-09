# Native mobs — first playable implementation

The user authorized Blender-authored enemies, AI and spawning on 2026-09-09 and chose **Rustback beetle** and **Dusk prowler**. These are original native creatures; they do not imply ancient technology, generated structures, raids or the rest of the full-game roadmap. Art and tuning remain available for user review.

## Playable rules

| Species | Behaviour | Health | Hit damage | Spawn rule |
| --- | --- | ---: | ---: | --- |
| Rustback beetle | Wanders; warns for 1.4 seconds when approached within 5 m; attacks if the player stays or strikes it | 12 | 2 HP | Supported surface, day or night; up to 8 |
| Dusk prowler | Detects a visible player within 16 m, pursues and bites | 18 | 3 HP | Supported surface at night; up to 6 |

Night comes from the day/night agent's authoritative `WorldClock.IsNight` (18:00 through 05:59). There is no second mob clock. Dawn stops new prowler spawns; existing prowlers remain normal living entities. Beetles return home beyond a 20 m leash; prowlers beyond 30 m. Breaking sight for four seconds also ends pursuit. Creatures never mine terrain or raid player machinery.

Hold the existing mining/attack button while aiming at a creature within 3.2 m. The same first-person swing and selected item apply. Damage comes from the shared item definition’s `attackDamage` field: fists deal 1 HP; blades use wood 4 / stone 5 / copper 5 / iron 6 / diamond 7 HP, with separately authored axe and other tool values. Blade cooldown is 0.30 s; axe 0.55 s; other items and fists 0.40 s. The survival/content owner authors tier damage in one registry; mobs do not keep a second weapon table. The aimed creature gets a name/health bar; hits tint and stagger the model, interrupting its current action, and its death action finishes before the body disappears. Mob loot, ranged attacks and breeding remain future content.

Bites have a visible preparation action/tint (0.8 s beetle, 0.65 s prowler), then recheck range, height, line of sight and player state. Back away or put solid terrain between you and the attack. The recovery interval is 1.2/1.1 s, with a shared 0.65 s minimum between accepted mob hits. Damage uses `Expedition.TakeDamage` and its existing armor, health, death and respawn rules. Mob code does not create another player-health system. Inventory retains the shared game time rule; pause stops mob simulation.

Player movement resolves lateral contact with living creatures; block placement rejects their bodies. The beetle's collision footprint is 0.90 m square and the prowler's is 0.85 m square, allowing their bodies to navigate one-voxel lanes and steps; extended legs, ears and tails are visual appendages. Attacking a creature takes priority over mining the block behind it. Dead creatures cannot receive a second defeat or block construction.

## Spawn and simulation boundaries

Working defaults: at most 14 tracked ambient mobs, a 24–48 m spawn ring around the player, and a protected 16 m radius around the original world spawn. Spawn attempts run every two simulation seconds, with at most eight candidates per attempt. A candidate needs sky access above its body, actual loaded terrain, a solid supporting surface across the collision footprint and clear space for the full body. Spawning rejects the current visible camera area when unobstructed. Surface samples use the current terrain generator only as a search hint, then validate actual edited voxels (grass, dirt, stone, sand, sandstone, snow or red clay); leaves, cave voids and unloaded borders are not valid support.

One session `MobSystem` owns stable instance IDs, health, intent and `WorldPoint` positions. The renderer and animations contain no combat authority. Simulation runs at 20 Hz with a bounded catch-up of four steps/frame. At most two path searches are performed per step, with 96 node expansions and 256 stored nodes each. The local cardinal voxel search permits one-block steps and one-block drops, checks overhead clearance, and rechecks movement against current terrain edits. Full maze navigation, water traversal and flying are outside this initial behaviour set.

Views and thinking sleep beyond 68 m or when the entity's chunk is unavailable. Ambient mobs beyond 112 m are removed and may be replenished by later spawning. This is a bounded ambient population, not persistent named creatures: mob identity/health survive floating-origin shifts and temporary sleep within the active session, but there is no cross-session mob save. Creatures carry no inventories or loot that could be silently lost during removal. Initial combat grace is ten gameplay seconds; the shared `Respawned` event resets eight seconds of mob grace in addition to survival-owned respawn immunity. A large relocation also triggers this grace as a fallback.

## Source and authoring

- `Tools/create_mob_assets.py` reproducibly authors both original meshes, rigs, shared 64×64 palette, four actions and front/back review renders in Blender 5.2.
- Editable sources: `ArtSource/Mobs/RustbackBeetle.blend` and `DuskProwler.blend` (metric units, unit scale 1).
- Runtime exports: `Assets/RivetReach/Resources/Mobs/` contains FBX files, palette, shared URP material and separately editable definition assets.
- `MobAssetImport` enforces explicit FBX conversion, Generic animation import, preserved bone hierarchy, one material and readable meshes. A manually evaluated Playables graph blends the four authored actions, with stride/attack timing tied to movement and wind-up and no animation work for sleeping views. Runtime rotates the authoring forward axis consistently; no per-instance correction is required.
- Meshes share one normalized `CreatureUV` layer and one material each. The sources use nine bones per creature, with rigid segment weights. This keeps the small stylized animals readable; joint deformation/artistic acceptance remains a review concern.
- Source counts: beetle 904 triangles, prowler 1,476. These counts are geometry evidence, not a claim of whole-game performance.

All meshes, palette pixels and animations were authored for Rivet Reach. No third-party art, copied creature designs or additional dependencies were introduced. Existing proprietary ownership and notices apply.

## Build and verification

Use the pinned Unity 6000.4.4f1 Editor. `Rivet Reach > Mobs > Build Windows review` prepares the shared project assets, runs mob import validation and produces `Builds/Mobs/RivetReach.exe`; a batch process in an otherwise unopened checkout can execute `RivetReach.Editor.MobBuild.Build`. Preserve any active Editor session and follow [the shared coordination mailbox](MOB_DAY_NIGHT_HANDOFF.md).

Run `Tools/Verify-Mobs.ps1` after building. It invokes the explicit `-rr-mob-verify` mode and writes `Logs/MobVerification/mob-runtime-report.json` plus screenshots. That mode uses the actual gameplay authority, creates a bounded test platform, and checks loaded support, day/night spawn policy, population limits, collision/navigation, melee occlusion, telegraph/avoidance, health integration, player input, defeat and origin/distance lifecycle. Normal games never create test fixtures. Generic `-rr-verify` runs disable natural mob spawning to keep pre-existing terrain/inventory tests reproducible.

The [mob verification results](verification/MOB_RESULTS.md) record 56 passing standalone checks, imported geometry, actual day/night/combat screenshots and the remaining review limits. Read the [day/night handoff](MOB_DAY_NIGHT_HANDOFF.md) for coordination provenance.
