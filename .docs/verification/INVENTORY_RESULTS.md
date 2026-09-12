# Expanded player inventory verification — 2026-09-12

The player now has **56 backpack slots (seven rows of eight)** and a **15-slot hotbar**. [Gameplay](../GAMEPLAY.md#9-working-interaction-specification) owns controls and capacity; [save compatibility](../SAVES.md#expanded-player-inventory--2026-09-12) owns schema 6 and older-layout migration.

## Tested artifact

`Builds/Inventory/RivetReach.exe`, Unity **6000.4.4f1**, completed **2026-09-12 13:46:21 UTC**. The [build report](inventory-2026-09-12/build-summary.txt) records success with **zero errors and zero warnings**. [Source and binary hashes](inventory-2026-09-12/source-snapshot.json) identify the shared-workspace build, which also includes pre-existing electrical-grid changes; this report checks the inventory increment.

## Measured checks

- **389 inventory assertions:** exact stack/position migration for schema 1–5 inventory records, empty expansion slots, schema 6 round-trip, section alignment, malformed-size rejection, full capacity and transfers between the final hotbar/backpack slots. [Report](inventory-2026-09-12/inventory-checks.txt).
- **107 native-player assertions**, Windows D3D11 at **1280×720**: all 71 slots rendered once and within the screen; next/previous and wheel wrap; number keys retain their first-ten mapping; actual catalog/slot pointer dragging and Shift-click reach the new row and slot 15; all 71 exact stacks and selected slot 15 survive disk save/load. [Report](inventory-2026-09-12/runtime-report.json), [exit code](inventory-2026-09-12/exit-code.txt).
- **24 full-world legacy assertions:** actual schema 2, 3 and 4 checkpoints load, reject unrelated content changes, migrate to the current schema and then round-trip byte-exactly while paused. [Report](inventory-2026-09-12/legacy-runtime-report.json).
- Existing [domain](inventory-2026-09-12/domain-checks.txt), [crafting](inventory-2026-09-12/crafting-checks.txt) and [survival](inventory-2026-09-12/survival-checks.txt) suites pass with capacity-aware full-inventory and randomized-operation fixtures.

Actual captures were visually inspected: [seven-row inventory after loading](inventory-seven-rows-2026-09-12.png) and [15-slot HUD](inventory-hotbar-2026-09-12.png). The added row fits beside personal crafting and equipment; the HUD hotbar stays centered with its existing slot size.

## Reproduce and limits

With the pinned Editor idle, write `inventory-build` to `Logs/build-request.txt` through the existing local build workflow. After its fresh success result, run `Tools/Verify-Inventory.ps1` in PowerShell. It preserves prior evidence by requiring a new output directory. `Tools/Verify-PlacementFacing.ps1 -Executable <inventory-player> -LegacyDirectory <schema-2-3-4-fixture-directory>` runs the existing full-world migration checks.

Full-world legacy checks cover schemas 2–4; schema 1/5 inventory migration was checked at the serialized inventory-record boundary. These checks do not establish long-session gameplay quality, other display sizes or whole-game performance. The published alpha and earlier feature artifacts were not rebuilt by this task.
