# Wooden doors — 2026-09-12

[Door rules](../DOORS.md) own crafting, placement, control precedence and persistence. This report records the focused `Builds/Doors/RivetReach.exe` review, built in an isolated project with Unity **6000.4.4f1 / URP 17.4.0**. It does not update the downloadable alpha.

## Original assets

[The Blender authoring script](../../Tools/create_door_assets.py) produces the editable [WoodenDoor.blend](../../ArtSource/Doors/WoodenDoor.blend), explicit FBX and inventory icon. It reuses the project's workshop atlas and original geometry helpers. The source and Unity import both contain **2,196 triangles, two renderers and one shared material**. The frame and hinged leaf fit the 1×2×1 footprint in both open and closed positions. Import checks include UVs, normals, material identity, hinge identity and bounds. Small binding/handle overhangs found during development were corrected before the final asset.

![Actual closed Blender model](../../ArtSource/Doors/door-closed.png)

![Actual open Blender model](../../ArtSource/Doors/door-open.png)

## Verification

The focused domain checks cover exact six-plank/three-door crafting in translated 3×3 and 4×4 grids; manual control; ON/OFF and disconnect edges; manual overrides under steady signal; occupied upper/lower closing protection; chunk dormancy; internal upper-cell exclusion from items; and additive content compatibility with unrelated definition changes still rejected.

The starter acceptance suite now independently checks the new layout alongside the existing 55 starter recipes. Existing domain, survival, industry, battery and multiblock checks are also run by [DoorBuild](../../Assets/RivetReach/Editor/DoorBuild.cs).

The built-player review exercises the actual placement transaction and right-click input, both collision cells, Blue Signal, imported hinge state, mining/support recovery and durable save/load. Run it with [Verify-Doors.ps1](../../Tools/Verify-Doors.ps1), using `-Build` to reproduce the isolated build.

The final Windows build completed in **34.84 seconds with zero errors and zero warnings**. [Build identity and source hashes](doors-2026-09-12/artifact.json) pin the artifact to the recorded committed base plus the door changes, independently of concurrent workspace work. [Build summary](doors-2026-09-12/door-build.txt) and [import checks](doors-2026-09-12/door-import.txt) record that build.

[Door domain checks](doors-2026-09-12/door-checks.txt) pass **29 assertions**. The [starter fixtures](doors-2026-09-12/starter-recipe-checks.txt), [domain](doors-2026-09-12/domain-checks.txt), [survival](doors-2026-09-12/survival-checks.txt), [industry](doors-2026-09-12/industry-checks.txt), [battery](doors-2026-09-12/battery-checks.txt) and [multiblock](doors-2026-09-12/multiblock-checks.txt) reports preserve the broader checks.

The [built-player report](doors-2026-09-12/runtime-report.json) passes **32 checks with no recorded Unity errors**, including actual right-click/held-use behavior, single-item consumption and recovery, two-cell collision, safe closing, blue cable control without electricity, imported pivot movement, absence of invisible creature footholds, and open-door save/load after topology/residency reconstruction. Native creature foothold and wall-grip queries now use current world collision, so an open door cannot supply invisible support.

![Closed door and Blue Signal connection in Unity](doors-2026-09-12/door-signal-closed.png)

![The same door opened through Blue Signal](doors-2026-09-12/door-signal-open.png)

The actual Blender closed/open renders and the Unity imports/screenshots were visually reviewed. The code-level and native checks establish only the workloads listed in their reports.

## Limits

The full two-cell voxel volume blocks movement while closed; collision does not trace the thin mesh. Open doors retain their terrain cells for targeting, placement and water routing. The test fixture uses a small wiring network, not a large-factory performance workload. Original asset geometry is measured; artistic acceptance, sustained playfeel and broad hardware performance remain user review.
