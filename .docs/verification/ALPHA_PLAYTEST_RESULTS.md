# Alpha 0.0.2 playtest corrections — issue #12

[Issue #12](https://github.com/Starbugstone/Rivet-Reach/issues/12) adds the interaction corrections, Lava Rock, shared hostile validity, generic cages and rare dungeon rooms. Rules belong in [GAMEPLAY](../GAMEPLAY.md#alpha-playtest-interactions--2026-09-19), [FLUIDS](../FLUIDS.md#lava-rock--2026-09-19), [MOBS](../MOBS.md), [SPAWNERS](../SPAWNERS.md) and [SAVES](../SAVES.md). The current review executable is `Builds/AlphaPlaytest/RivetReach.exe`, built through the already open Unity **6000.4.4f1** Editor for Windows/Direct3D11.

## Survival first — September 19, 2026

The [Survival report](alpha-playtest-2026-09-19/survival-report.json), [observations](alpha-playtest-2026-09-19/survival-observations.txt) and [artifact identity](alpha-playtest-2026-09-19/survival-artifact.json) precede focused regression. The ordinary empty-handed route gathered logs, crafted and placed a workbench, crafted a wooden pick and explored for **312.8 seconds**, ending at **14 food / 20 health**. It used normal movement, gathering and crafting with natural spawning enabled; it did not supply items, teleport or enable Creative. Seven assertions passed.

![Workbench reached through ordinary gathering and crafting](alpha-playtest-2026-09-19/survival-workbench.png)

![Survival after the five-minute exploration route](alpha-playtest-2026-09-19/survival-five-minutes.png)

This is a short scripted player route, not a human playtest or proof of final hunger balance. The first draft of the harness began before terrain readiness and then dug itself into a pit; the retained run waits for ready terrain and explores before digging. Subsequent focused harness changes have separate build identities.

## Editor and native coverage

The **59** retained [interaction checks](alpha-playtest-2026-09-19/interaction-checks.txt) cover compatible stack consolidation, metadata-bearing portable storage, owned/Creative picking, selection shapes, ordinary hunger at 75% of prior expenditure and unchanged healing cost. The **40,796** [world checks](alpha-playtest-2026-09-19/world-checks.txt) cover flowing-water/lava-source contact orientations and exclusions, diamond-only Lava Rock recovery, room template contents, old-generator exclusion and 39,304 point/chunk/halo comparisons. Inventory, survival, farming, fishing, chicken and lava checks passed in the same Editor pipeline.

The original Blender cage imports at **3,472 triangles**, one material per renderer and **0.98 × 0.95 × 0.98 metres**. Its configured miniature evaluates the imported Idle pose once and uses frozen render meshes. The preview below is Blender evidence; the native cage captures separately demonstrate game rendering and operation. These measurements do not establish a whole-game performance budget.

![Original cage Blender preview](alpha-playtest-2026-09-19/spawner-blender.png)

## Focused native regression

The final [native report](alpha-playtest-2026-09-19/focused-report.json) passed **87 assertions**, with no reported errors. Its [artifact identity](alpha-playtest-2026-09-19/focused-artifact.json) identifies the final rebuilt player. Coverage includes real held-Space input, Survival/Creative middle-click picking, UI consolidation, shared torch selection, ordinary and selected-hoe right-click harvesting, actual seed consumption, full-backpack overflow, wild/unripe exclusions and four first-person model/skin combinations.

![Flowing water has produced the stable Lava Rock block in a controlled basin](alpha-playtest-2026-09-19/lava-rock-reaction.png)

The resident fluid simulation produces Lava Rock from a flowing-water/lava-source contact; an iron pick fails and a diamond pick recovers exactly one. Ordinary stone hit/break/place adds neither point lights nor cached light sources in an unlit room. A real placed torch still illuminates it, and removing that torch restores darkness.

![An ordinary stone hit in an unlit room](alpha-playtest-2026-09-19/ordinary-block-dark-hit.png)

![The same fixture with a real torch](alpha-playtest-2026-09-19/ordinary-block-real-torch.png)

Natural spawning runs through the ordinary two-second cadence. The retained [diagnostics](alpha-playtest-2026-09-19/natural-spawn-diagnostics.txt) identify a naturally chosen cave origin at **(80, -71, 18)** on stone with authoritative light **0**. The controlled cobblestone room independently passes the same site-validity query. A prior test incorrectly required one particular small room to be chosen before surrounding caves filled the four-Floater cap; it was corrected to test natural spawning and constructed-floor eligibility separately. No spawn-chance increase was used to force that test.

![A naturally spawned Floater in a dark cave, viewed with a held torch](alpha-playtest-2026-09-19/natural-floater-dark-room.png)

Two cages independently reach five origin-owned mobs; a full natural population does not prevent replacing a defeated cage mob. Actual cooldown/attempt bounds, occupied volumes, activation range, placed-light rejection/removal and both natural/spawner chunk-unload lifecycles pass. Schema-13 save/reload preserves identity, origin population, sequence and timer, including living mobs whose cage was destroyed; breaking the cage produces no collectible spawner.

![Active cage with its visible render-only miniature and ordinary spawned Floaters](alpha-playtest-2026-09-19/floater-spawner-active.png)

![Placed light suppresses new cage spawns](alpha-playtest-2026-09-19/floater-spawner-torch-disabled.png)

A native close-up caught an FBX unit/axis-space error that earlier cage-local bounds checks missed. The final presentation uses a neutral metre-space parent, a baked static species display and centred vertices for rotation. Editor world-space/rotated-parent checks and native post-frame bounds/component checks now cover this failure. The miniature has no live animator, skinned renderer, collider or mob entity. [Native renderer measurements](alpha-playtest-2026-09-19/miniature-renderers.txt) place its visible centre inside the cage.

The known seed-246813 room in region (0, -8) loads through ordinary chunk integration, has all **81 cobblestone floor cells**, registers a cage and produces a Floater using shared rules.

![Actual generated cobblestone dungeon and functional cage](alpha-playtest-2026-09-19/generated-floater-room.png)

The fixture visits known coordinates and uses held torch glow for visibility. It does not alter the cage's authoritative spawn light or demonstrate an unassisted dungeon discovery.

## Player reference

The refreshed export contains **190 items and 194 recipes**. Source validation passed **218 pages / 9,358 local links and images**. The dedicated [Spawn mechanics](../wiki/Spawn-mechanics.md) page explains natural hostiles, cages and persistent chickens, including floors, body sizes, light, 3D distances, attempts, independent caps, unloading and empty-room troubleshooting. Updated [Spawners](../wiki/Spawners.md), [Lava](../wiki/Lava.md), [Building and inventory](../wiki/Building-and-inventory.md), item pages and navigation include current native captures. Live publication evidence is recorded after deployment.

## Reported character texture repair

The earlier `Builds/ChickensBeforeVisuals` player reproduced the flat grey character appearance. The asset references and registered skin textures were present: `ExplorerSkin.shader` interpreted the no-fog variant as fully fogged. The repair uses the project's explicit world-distance fog parameters, preserving the real textures and first-person arms behavior. It does not replace assets or conceal missing references globally.

![Before: both model variants rendered as flat silhouettes](alpha-playtest-2026-09-19/skin-before.png)

![After: original face, skin and clothing detail in the rebuilt player](alpha-playtest-2026-09-19/skin-after.png)

The rebuilt native avatar review passed **133 checks** with no reported errors. [Its report](alpha-playtest-2026-09-19/skin-runtime-report.json) and the visual comparison are complementary: the old automated avatar assertions also passed despite the visible defect. Focused first-person captures check both models with both local skins. No performance improvement is claimed from these runs on a shared workstation.

## Saves and generated history

Actual pre-existing **schema 9, 10, 11 and 12** checkpoints each passed **17 migration assertions**: [9](alpha-playtest-2026-09-19/legacy-schema-9.json), [10](alpha-playtest-2026-09-19/legacy-schema-10.json), [11](alpha-playtest-2026-09-19/legacy-schema-11.json), [12](alpha-playtest-2026-09-19/legacy-schema-12.json). Each load retains old terrain/edit history, explores into the current generator, saves again and reloads the mixed history without retrofitting established chunks. The schema-12 input comes from the completed chicken increment.

The broader [save/recovery run](alpha-playtest-2026-09-19/save-report.json) passed **183 assertions**; [fresh-process Continue](alpha-playtest-2026-09-19/save-resume-report.json) passed **5**. Checks cover full-state conservation, corrupt/checksum rejection, failed-load rollback, previous-checkpoint recovery, death saves and title/Continue behavior. [Artifact identity](alpha-playtest-2026-09-19/save-artifact.json) distinguishes this build from the later static-miniature/capture changes; persistence and simulation code were unchanged.

## Remaining evidence limits

Long-session survival balance, room discovery frequency across many seeds, practical farm throughput and large numbers of cages still need human Alpha playtests and measured profiling. The 45% regional candidate setting and 99/1 placement-mode draw are working authoring defaults, not a measured discovery guarantee. The generated-room runtime fixture deliberately travels to a known deterministic room; it is not an ordinary exploration discovery claim. The new generator applies only to previously ungenerated chunks.

Concurrent issue #10 chicken implementation and its verification were completed by the other agent. Ownership was coordinated in ignored local manifests, and its authored assets and persistence section were preserved.
