# Starter station graphics — 2026-09-12

The user requested that chest, workbench and furnace match the graphical quality of the Machinist’s Bench, then requested updated wiki graphics. [Content pipeline](../CONTENT_PIPELINE.md#starter-station-models--2026-09-12) owns the source/import/presentation contract.

Original assets share the existing industrial palette and use coherent iron fittings, shaped wood and dressed stone. The Workbench remains a 3×3 station; the Machinist’s Bench remains the distinct 4×4 upgrade. Closed lids and fixed station facing retain existing behavior.

## Editable source

![Actual Blender source meshes](../../ArtSource/Stations/starter-stations-review.png)

Blender **5.2.0 LTS** generated the three explicit exports with the reproducible [authoring script](../../Tools/create_starter_station_assets.py). This image renders the real meshes, not concept art. [Source geometry measurements](../../ArtSource/Stations/geometry-report.json):

| Station | Source triangles | Mesh parts |
|---|---:|---:|
| Workbench | 3,012 | 1 |
| Chest | 3,684 | 1 |
| Furnace | 3,672 | 2 |

All three fit inside a single metre voxel. The authoring script checks positive signed volume for every component; an inward-wound arch surface was corrected before the final export. One existing atlas supplies the bodies; the furnace ember part gets its own shared Unity material for burning/cold presentation.

## Unity verification

The Windows development build completed in **256.5 seconds with zero errors and zero warnings** under Unity **6000.4.4f1 / URP 17.4.0**. [Build identity](starter-stations-2026-09-12/artifact.json) and [build summary](starter-stations-2026-09-12/build-summary.txt) identify the isolated station review; subsequent unrelated changes retain their own evidence. An earlier build hit a transient lock on a Unity debug-symbol file; the final build succeeded.

[Imported geometry checks](starter-stations-2026-09-12/import-checks.txt) passed **26 assertions**. Unity imports 3,012 workbench triangles, 3,684 chest triangles and 3,648 furnace triangles (24 fewer than the source after import). Workbench and chest each use one renderer; the furnace uses two for body and embers. Bounds, material references, normals/UV streams, absence of the old cube mesh and visibility of neighboring terrain passed.

The [survival checks](starter-stations-2026-09-12/survival-checks.txt) passed **47,678 assertions**, and the [starter recipe checks](starter-stations-2026-09-12/starter-recipe-checks.txt) retained the existing layouts and quantities. Those domain checks do not measure rendering cost.

The [built-player report](starter-stations-2026-09-12/runtime-report.json) passed **47 checks** without Unity errors, covering keyboard Interact and mouse Use for all three stations, the 3×3 workbench and 27-slot chest, actual held/dropped mesh references, burning/cold embers, iron smelting, view release/reconstruction, save/load and mining recovery of exactly 17 stored ingots. Run the focused review with [Verify-StarterStations.ps1](../../Tools/Verify-StarterStations.ps1).

![Actual workshop comparison, including the existing Machinist’s Bench](starter-stations-2026-09-12/station-workshop.png)

![Workbench in Unity](starter-stations-2026-09-12/station-workbench.png)

![Chest in Unity](starter-stations-2026-09-12/station-chest.png)

![Burning furnace in Unity](starter-stations-2026-09-12/station-furnace.png)

The [rear view](starter-stations-2026-09-12/station-back.png), [cold firebox](starter-stations-2026-09-12/station-furnace-cold.png) and [held chest](starter-stations-2026-09-12/station-held-chest.png) were also visually inspected against the source meshes. The three station interfaces and remaining held views are retained in the same evidence folder.

## Player wiki

The item pages receive matching inventory icons and actual in-game close-ups; the Home page receives the workshop comparison. Wiki export and publication checks are recorded after deployment.

## Remaining limits

Artistic acceptance remains the user’s review. Mesh counts are measured geometry, not proof of large-factory frame performance. These stations preserve full-cell collision/targeting despite their more detailed silhouettes. This task does not update the downloadable alpha release.
