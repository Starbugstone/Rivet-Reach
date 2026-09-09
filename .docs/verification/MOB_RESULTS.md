# Native mob verification — 2026-09-09

Status: **56 standalone mob checks passed, zero logged errors**. Original Blender assets, Unity import validation, the Windows build and shared domain checks passed. Actual source renders and Unity day/night/combat captures were inspected; artistic acceptance and broader play balance remain for user review.

## Blender source evidence

Both original creature sources and explicit FBX exports were generated with Blender 5.2.0 LTS. The generator completed successfully. Actual front/back renders were inspected: the rustback has domed divided copper wing cases, six jointed legs, antennae and curved mandibles; the prowler has a shaped quadruped torso, four tapered legs and paws, swept ears, cheek/dorsal mane, slit eyes and balancing tail. The shared stylized natural-world reference was inspected before authoring. The user selected the two species direction; final visual acceptance remains theirs.

| Source | Triangles | Vertices | Bones | Materials | Actions |
| --- | ---: | ---: | ---: | ---: | --- |
| Rustback beetle | 904 | 526 | 9 | 1 | Idle, Walk, Attack, Death |
| Dusk prowler | 1,476 | 852 | 9 | 1 | Idle, Walk, Attack, Death |

![Rustback front](RustbackBeetle-front.png)
![Rustback back](RustbackBeetle-back.png)
![Dusk prowler front](DuskProwler-front.png)
![Dusk prowler back](DuskProwler-back.png)

## Unity import and build evidence

Pinned Unity **6000.4.4f1**, URP **17.4.0**, Windows x64 Development player. The final corrected import/build completed with **0 errors, 0 warnings in 29.287 seconds** using an isolated copy of the shared working project. This warm-build duration is not a frame-time measurement.

The imported FBX files preserve **904 / 1,476 triangles, nine bones, one material/submesh and all four authored actions**. The import check validates bone weights and metre-scale bounds. Beetle imported bounds are approximately 1.59 × 0.88 × 1.77 m; prowler 1.17 × 1.84 × 2.69 m (animation bounds include movement/death, so they are larger than the static source silhouette). See [raw import report](mob-import-report.txt). The runtime blends Generic clips through a manually evaluated Playables graph and defines facing using named Head/Body bones under a separate orientation parent.

The same build runner executed these existing suites:

- **92,338 domain assertions**, including terrain, signed coordinates/halos, inventory, grass, trees, ores and bedrock.
- **1,158,103 crafting assertions**.
- **45,402 survival assertions**, including 52 recipes, six furnace recipes, tiers, food, health and equipment.

These suite counts are domain evidence; they do not establish mob gameplay quality or whole-game performance. [Snapshot C# hashes](mob-source-manifest.json) identify the tested code. The terrain/survival owners acknowledged their source/asset match and requested these shared checks before reusing the executable for their focused runtime verification.

The first isolated startup failed before compilation while UPM copied a bundled Shader Graph sample (`copyfile UNKNOWN`); copying the existing pinned package cache recovered startup. Import checks then caught early clip mapping and a 100× animation-scale issue. The final import uses post-import clip mapping, Generic rigs with explicit avatar creation, and metre-bound assertions. [Unity documents the first-import clip lifecycle](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ModelImporter-clipAnimations.html). These failures were fixed before the corrected build above.

No user-owned editor was stopped or unsaved scene replaced. The completed integration serialized builds/captures while preserving concurrent work; historical coordination messages are retained in Git history.

## Runtime iteration

The first real-player run passed day/night integration, supported and hidden spawning, population caps, imported geometry/actions, creature occupancy, pause and editable wall navigation/attack occlusion. Its one-block climb assertion failed: footprints wider than one voxel could neither stand immediately before a riser nor fully land on its edge in the cell-centred path graph. Body footprints were corrected to 0.90 m for the beetle and 0.85 m for the prowler, excluding their extended appendages. The first screenshot was queued in the same frame as camera/yaw/clock changes, before those changes were presented. Its old HUD time and apparent backwards facing are therefore not conclusive evidence of an asset defect. Facing is now calibrated after the first Generic graph evaluation, and a runtime assertion checks that the visible Head/Body direction agrees with movement. Captures now allow presentation to settle before recording. The corrected run below passed both the path query and actual full-body step traversal. It also passed facing alignment for both animated models. The final captures show the intended camera pose and clock time.


## Standalone gameplay evidence

`Tools/Verify-Mobs.ps1` completed successfully at **2026-09-09 06:59:08 UTC**, using the actual Windows player, seed **73519**, and **1280 × 720** output. The opt-in fixture creates a loaded stone pad to isolate navigation/combat from random slopes; its spawning checks also exercise the surrounding naturally generated terrain. Normal sessions do not create the pad.

The [complete runtime report](mob-runtime-report.json) records **56 passing checks / zero errors**, covering:

- Shared 18:00 night / 06:00 dawn boundaries, supported natural spawns, daylight prowler exclusion, nighttime prowler inclusion and population caps.
- Imported topology, four actions, correct animated facing and living-body placement rejection.
- Wall detours and melee occlusion, live terrain removal, one-block pathing **and physical climbing**, full-body collision, low ceilings and mined shafts.
- Pursuit, visible wind-up timing, dodging a committed bite and accepted damage through shared player health.
- Real held-blade input and authored weapon damage, no mining through a mob, and unfinished food-use cancellation when aiming at a mob.
- Beetle warning/back-away behavior, one-time defeat, death cleanup, player-body contact, floating-origin identity/health preservation, sleeping views and distant population release.

The player ran on an **Intel Core i7-10750H / NVIDIA GeForce RTX 2060**. It recorded **nine scheduled AI path searches** and a **6.193 ms maximum measured mob simulation tick** during this short mixed verification workload. That timer surrounds `MobSystem.Tick`, excluding rendering/animation presentation and manual fixture setup; it is not a steady frame-time, allocation or stress-test claim. The explicit navigation budget check passed. Extended crowded encounters, long mazes, low-end hardware and long-session balance have not been established by this run.

![Imported creatures in daylight](mobs-import-day.png)
![Imported creatures under the shared night sky](mobs-import-night.png)
![Actual dagger hit and creature health target](mob-combat-target.png)

The day capture shows the two distinct silhouettes, palette regions and faces in the actual world. The night capture follows the day/night owner's lighting and leaves the prowler substantially darker; night visibility and encounter tuning should be judged in play. The combat capture shows the imported beetle, existing held dagger and its reduced health bar. These are actual player screenshots, not concept images.

The reviewed executable is **`Builds/Mobs/RivetReach.exe`**, with the complete adjacent data/runtime/license files. [Build summary](mob-build-summary.txt), [build/asset identity](mob-build-context.json), [source hashes](mob-source-manifest.json) and the runtime report identify this candidate. `Assembly-CSharp.dll` SHA256 is `587320e294c369a9ee5cb14f494cf62c942a0f4c3823861a0b0606013d3c312b`.

No loot, named-creature persistence, multiplayer AI, raids, water traversal or full maze navigation is claimed. Sources, rig actions, material and species definitions remain editable for subsequent art/tuning feedback.
