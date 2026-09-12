# Hand crank verification — 2026-09-12

[Specification and controls](../HAND_CRANK.md). The feature adds a manually operated generator using the existing power allocator and exact battery storage.

## Source review

Blender 5.2.0 LTS executed [create_hand_crank.py](../../Tools/create_hand_crank.py), exported the original mesh/icon and saved [HandCrank.blend](../../ArtSource/HandCrank/HandCrank.blend). The actual [source render](../../ArtSource/HandCrank/hand-crank-review.png) was visually inspected against the existing electrical-component direction: dark iron mount, exposed copper winding, brass wheel/arm and wooden grip.

Measured source geometry: **3,044 triangles, two mesh parts, one shared Workshop atlas material**. The crank pivot and editable source animation are included. This is a source measurement, not a frame-time benchmark or final artistic acceptance.

## Unity checks

Unity **6000.4.4f1** compiled the new scripts and passed [33 focused checks](hand-crank-checks-2026-09-12.txt): exact 50 J turns, repeat limits, idle/dormant/stale-instance behavior, socket orientation, live-load priority, capacity clamping, cell/bank modes, workbench ingredients and both legacy/current content fingerprints. Altering an unrelated legacy item definition still rejects the checkpoint.

The existing battery, industry, multiblock and domain suites also passed. The domain report recorded 92,351 assertions; the survival suite recorded 47,679 assertions with **92 crafting recipes and 11 furnace recipes**. These are focused simulation/catalog checks, not a new whole-game performance claim.

[Imported geometry](hand-crank-import-2026-09-12.txt) matches the source: **3,044 triangles, two renderers, one shared material**, complete UV/normal streams and the named moving pivot. Measured bounds are `(0.15, 0.12, 0.04)` to `(0.85, 0.88, 1.00)` metres in its one-cell footprint.

## Windows player

The [Windows build](hand-crank-build-2026-09-12.txt) succeeded with **zero errors and zero warnings**. The maintained local player is `Builds/HandCrank/RivetReach.exe`. It was built in an isolated working snapshot using the pinned Editor; the concurrent appearance work was included in that review snapshot. This report does not attribute those other changes to the crank increment.

The [actual player run](hand-crank-runtime-2026-09-12.json) passed **28 assertions** at 1280×800 on Direct3D 11 (i7-10750H / RTX 2060): four-side socket alignment, actual right-click placement without opening the battery, a single right-click storing exactly 50 J, held Use producing repeated complete turns, visible pivot animation, release/look-away behavior, paused paid work, battery inspection and disk save/load preserving exact charge and the remaining paid turn. No runtime errors were logged.

![Original crank attached directly to a battery](hand-crank-battery-2026-09-12.png)

![Battery retains manually generated energy after cranking stops](hand-crank-stored-power-2026-09-12.png)

Both actual Unity screenshots were visually inspected against the Blender source: the mount, winding, wheel and handle remain recognizable and the assembly meets the battery face. The battery interface shows the retained 0.35 kJ from the fixture's manual turns.

## Remaining review

Balance and prolonged manual-use comfort remain play-review items. The runtime report's zero timing fields are unused fields, not performance measurements. No large-factory or frame-time claim is made. Older feature evidence retains its original build identity.
