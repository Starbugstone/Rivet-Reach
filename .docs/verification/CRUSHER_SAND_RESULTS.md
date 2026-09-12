# Crusher sand recipes — 2026-09-12

The user requested stone and cobblestone processing into sand. [Industry rules](../INDUSTRY.md) now specify **1 stone or 1 cobblestone → 1 sand**, at the existing **160 W / 100 ticks (5 seconds)**. The 1:1 yield is a working default selected for this request. Raw copper, iron and gold still each produce two crushed pieces.

`MachineState.CrusherOutput` supplies output identity and quantity to manual/automatic input acceptance, simulation capacity checks and completion, and `RecipeBrowserIndex`. No item definitions, save schema or compatibility fingerprints changed.

## Measured checks

Unity **6000.4.4f1** ran `IndustryChecks.Run` through the existing Editor's `industry-logic` request. [The retained report](crusher-sand-2026-09-12/industry-checks.txt) contains **375 passing assertions**, including **47 added recipe assertions**. These exercise both sand inputs and all three existing ores: manual input, no-power conservation, exact completion after 100 powered ticks, output quantities, 800 J per operation, full/incompatible output backpressure, and recipe/uses indexing. Switching stone to cobblestone after half a cycle resets paid progress; inventory transfer accepts the replacement.

The existing industry suite also exercises signal shutdown, underpower, item-pipe conservation, dormant topology, finite extraction and imported machine footprints. These are Editor simulation checks, not a fresh visual playthrough or long-session performance claim.

The Unity wiki export contains **132 items and 110 recipes**, including five crusher recipes. Generated sand, stone, cobblestone and crusher pages share those quantities. `Tools/publish_wiki.py --check` passed **143 pages and 5,696 local links/images**.

The final Windows x64 development player at **`Builds/Creative/RivetReach.exe`** [built successfully](crusher-sand-2026-09-12/build-summary.txt), with zero errors and zero warnings in 29.35 seconds; [completion](crusher-sand-2026-09-12/build-result.txt) was recorded at 12:05:52 UTC. It includes the crusher's general INPUT label and compact ore/sand help. Its build command also passed [92,355 domain assertions](crusher-sand-2026-09-12/domain-checks.txt), [battery](crusher-sand-2026-09-12/battery-checks.txt), [multiblock](crusher-sand-2026-09-12/multiblock-checks.txt), [connected-pipe](crusher-sand-2026-09-12/connected-pipe-checks.txt), and [1,144 recipe-transfer assertions](crusher-sand-2026-09-12/creative-transfer-checks.txt). No new player screenshot or manual sand-processing playtest is claimed.

## Reproduce

With this project open in the pinned Editor in Edit mode, write `industry-logic` to `Logs/build-request.txt`. The watcher refreshes scripts and runs the checks; inspect `Logs/build-result.txt` and `Logs/industry-checks.txt`. The sand-focused cases live in `IndustryChecks.CrusherRecipes`.
