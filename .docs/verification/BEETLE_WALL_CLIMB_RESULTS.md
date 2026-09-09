# Beetle wall climbing — 2026-09-09

Status: **79 standalone checks passed**, with no runtime errors, on 2026-09-09 at 17:03:01 UTC. The Windows development build passed with zero errors and zero warnings in 18.545 seconds using Unity 6000.4.4f1.

The user requested wall climbing for beetles after the initial native mob delivery. [MOBS.md](../MOBS.md#beetle-wall-climbing) owns the resulting behavior and working speed. This revision reuses the original Blender mesh, rig and actions; no third-party content, dependency or player asset is introduced.

The verifier adds a four-block-high wall to its opt-in loaded test pad. It exercises actual pursuit, top-edge transition, return descent, pause, mined support and the imported model's wall pose. It also checks cardinal wall grip, missing bands, overhang collision, unloaded terrain and the prowler's ground-only capability.

The 23 added wall checks run alongside the original 56 mob checks, including real player attacks, eating cancellation, warning/avoidance, day/night spawning, shared health, population limits and floating-origin lifecycle. An initial descent failure exposed collider feet resting slightly below the integer top face; navigation now accounts for that collision tolerance. The warning fixture now spawns its encounter after grace expires, so random wandering during grace cannot invalidate its proximity setup.

## Evidence

- [Runtime report](beetle-wall-runtime-report.json): 79 passing assertions, 19 path searches, beetle/prowler imports retaining 904/1,476 triangles.
- [Build summary](beetle-wall-build.txt): zero errors and warnings. The build also completed the existing domain checks. A previous batch exhausted host memory; the successful batch used `-nographics` and two job workers in the isolated `Rivet-Reach-MobVerify` project, preserving the open user Editor.
- [Source and artifact identity](beetle-wall-source-checks.json): the seven owned source/definition files match the tested build. The complete 311-file player was copied to `Builds/Mobs`; executable, gameplay assembly and resource hashes match the verified output.
- [Actual Unity wall pose](beetle-wall-climb.png): the beetle reached this pose through normal provoked pursuit. The verifier briefly froze mob simulation while moving the review camera; the mesh and pose were not manually placed or painted. Visual inspection confirmed its head faces upward and its legs face the wall.

![Rustback beetle climbing a voxel wall in the standalone player](beetle-wall-climb.png)

## Evidence limits

This is a controlled four-block encounter, not an exhaustive playtest of every cave or wall shape. Ceiling traversal is outside scope. The observed maximum mob tick was 4.556 ms on an i7-10750H / RTX 2060 during this run; that single maximum does not establish a general performance budget or scaling result.

The isolated build includes the concurrently developed controls present at its source snapshot. Later station-interaction edits are separately owned and are not covered by this mob report. All seven mob files remained identical to the tested source; other agents' changes were preserved.
