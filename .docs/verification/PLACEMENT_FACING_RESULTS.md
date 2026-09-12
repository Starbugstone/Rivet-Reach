# Player-facing placement — 2026-09-12

[Placement rules](../GAMEPLAY.md#placement-and-inventory) · [Save compatibility](../SAVES.md#station-facing-compatibility--2026-09-12)

New furnaces, chests, workbenches and industrial machines face the player in the nearest horizontal quarter-turn. Starter station visuals rotate around the occupied cell center and preserve facing through view recreation and save/load. Battery-attached cranks retain their rear-socket rule. Existing starter stations in older saves retain their original orientation.

## Artifact and reproduction

Review player: `Builds/PlacementFacing/RivetReach.exe`, Unity 6000.4.4f1. [Exact executable and managed-code hashes](placement-facing-2026-09-12/build-identity.json) identify the tested artifact. This focused player was built from the shared working tree; it does not certify unrelated concurrent changes or replace the published alpha.

Write `placement-facing-build` to `Logs/build-request.txt` to build in the open pinned Editor. Run `Tools/Verify-PlacementFacing.ps1` with a fresh output directory. Its `-LegacyDirectory` option runs migration against copies of earlier `.rrsave` files. Normal startup adds no test items or automation.

## Measured checks

- [Native placement report](placement-facing-2026-09-12/runtime-report.json): **175 assertions passed**, Direct3D 11, no captured Unity errors. Furnace, chest, workbench, Machinist’s Bench, crusher, boiler and battery each pass authority/rendered-front checks from all four sides. Each ordinary placement consumes exactly one item. Starter station rendered bounds remain inside the horizontal cell footprint.
- Actual right-click places an east-facing furnace. Elevated, lower and directly overhead player cases retain horizontal orientation. Recreating a distant view retains facing.
- Schema 5 round-trips the full checkpoint byte-for-byte, including distinct furnace/chest/workbench rotations and chest contents. A checksum-valid invalid rotation rejects and preserves the previous session and contents.
- [Legacy migration](placement-facing-2026-09-12/legacy-report.json): **24 assertions passed** against actual schema 2, 3 and 4 checkpoints. Older starter stations retain rotation zero; migrated full checkpoints round-trip byte-for-byte. Unrelated content-definition changes still reject.
- [Build](placement-facing-2026-09-12/build-summary.txt): succeeded with zero errors and warnings. [Existing survival checks](placement-facing-2026-09-12/survival-checks.txt) passed 47,707 assertions, including furnace transactions and conservation. Their synthetic timing is not a gameplay performance claim.

## Visual evidence and limits

Actual native-player furnace screenshots from placement on each side: [south](placement-facing-2026-09-12/furnace-facing-0.png), [west](placement-facing-2026-09-12/furnace-facing-1.png), [north](placement-facing-2026-09-12/furnace-facing-2.png), [east](placement-facing-2026-09-12/furnace-facing-3.png). Visual inspection confirms the furnace opening faces the placement viewpoint. This is a placement/persistence regression, not a new asset-quality or whole-game performance review. Existing industrial model protrusions are outside the changed starter-station bounds check.
