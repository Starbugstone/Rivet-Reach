# Machine interface illustrations — 2026-09-12

[Gameplay](../GAMEPLAY.md#machine-illustrations--2026-09-12) owns the presentation rule; the [player guide](../wiki/Machine-interfaces.md) explains the illustrated interface.

## Artifact and checks

`Builds/MachineInterface/RivetReach.exe`, Unity **6000.4.4f1**, completed **2026-09-12 14:21:02 UTC**. The [build summary](machine-interface-2026-09-12/build-summary.txt) records success with zero errors and one [warning](machine-interface-2026-09-12/build-messages.txt): concurrent uncompiled Editor changes may not affect build post-processing. This shared-workspace player includes pre-existing industry changes; the [source and binary hashes](machine-interface-2026-09-12/source-snapshot.json) identify its recorded inputs. This report covers the machine illustration increment, not those other features.

- **72 native-player assertions passed at 1280×720**, Windows D3D11: [report](machine-interface-2026-09-12/runtime-report.json), [exit code](machine-interface-2026-09-12/exit-code.txt).
- **72 native-player assertions passed at 1920×1080**: [report](machine-interface-2026-09-12/runtime-1920-report.json), [exit code](machine-interface-2026-09-12/exit-code-1920.txt).
- Opened 11 distinct placed subjects: crusher, boiler, pump, battery, bank controller, tank controller, item pipe, furnace, chest, workbench and Machinist’s Bench. Each uses the correct existing item texture and the same retained image widget. Replacing and reopening a second crusher preserves correct rotation authority and pointer-based input transfers. Personal inventory restores the character portrait.
- Visually reviewed the illustrated processor, furnace, charged battery, bank controller and 4×4 bench. The graphic and name fit without displacing backpack, equipment, hotbar or station controls. The authored graphic remains static while the real battery meter shows 25 / 100 kJ.

## Current visuals

![Crusher illustration and actual input controls](../wiki/images/machine-interface-crusher.png)

![Battery illustration beside current stored energy](../wiki/images/machine-interface-battery.png)

![Furnace illustration beside ingredient, fuel and result slots](../wiki/images/machine-interface-furnace.png)

These are native 1280×720 captures from the artifact above, taken on 2026-09-12. The fixture is staged in a disposable session through real placement/opening and production UI; ordinary sessions receive no fixtures. Source models and inventory icons were reused without modification.

## Reproduction and limits

With the pinned Editor idle, write `machine-interface-build` to `Logs/build-request.txt`; wait for a fresh success result and the actual player artifact. Run `Tools/Capture-MachineInterfaces.ps1` with a fresh output directory; use `-Width 1920 -Height 1080` for the second layout. The capture helper drains its temporary charged cell before replacing it, respecting the normal charged-battery dismantling gate.

The retained card adds no camera, 3D model, simulation component or save field. These checks establish the focused interaction and presentation results, not whole-game performance, all possible resolutions or final artistic acceptance. Initial capture cleanup failed at the charged-cell gate and was corrected before both passing runs. Older feature reports and the downloadable alpha retain their own build dates.
