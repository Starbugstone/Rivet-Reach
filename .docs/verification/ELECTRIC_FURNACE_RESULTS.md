# Electric furnace verification — 2026-09-13

[Rules](../ELECTRIC_FURNACE.md) own the working recipe and electrical behavior. [The player guide](../wiki/Electric-Furnace.md) teaches setup.

## Measured checks

Unity 6000.4.4f1 passed 109 focused Editor assertions covering all 11 registered furnace recipes, exact duration/output/energy, all six power faces at four rotations, ingredient/fuel separation, reduced power, signal control, full output, residency, input changes, receiver preferences, and crafting transfer conservation. A synthetic recipe verifies variable quantities and duration. Pre-electric content is accepted while unrelated definition changes reject.

Blender 5.2.0 LTS authored and rendered the original model: 1,596 source triangles, two mesh parts and one atlas material. The actual source render was visually reviewed against the existing workshop kit. Unity imported the same **1,596 triangles**, two renderers, with bounds **0.92 × 0.97 × 0.94 m**. The imported oven and inventory artwork were visually inspected; the controls fit the actual machine panel.

The [Windows build](electric-furnace-2026-09-13/build-summary.txt) succeeded with **0 errors and 0 warnings**, 67.582 seconds. [Artifact hashes](electric-furnace-2026-09-13/artifact-hashes.json) and [runtime source hashes](electric-furnace-2026-09-13/source-snapshot.json) identify the tested isolated candidate, excluding concurrent lava work. The catalog in this player contains 133 items and 122 crafting/processing entries.

The [native player report](electric-furnace-2026-09-13/runtime-report.json) passed **101 assertions**, [exit 0](electric-furnace-2026-09-13/exit-code.txt), with no recorded errors. The actual world places/renders the furnace, accepts ingredients, rejects fuel, reports 200 / 200 W, saves at tick 151 and reloads exact inputs/work/energy, completes after exactly 49 more powered ticks, stops after cable removal, and routes crushed ore through pipes into four exact ingots while leaving coal in its source chest. Mining removes its machine authority and its drop mapping recovers the furnace item.

Reviewed the actual [powered model](../wiki/images/electric-furnace-powered.png), [running interface](../wiki/images/electric-furnace-interface.png), [no-power interface](../wiki/images/electric-furnace-no-power.png) and [configured pipe setup](../wiki/images/electric-furnace-pipes.png). Inventory icon 174 is the render of the actual Blender model.

## Reproduction

With the project refreshed in the pinned Unity Editor, run `Tools/Verify-ElectricFurnace.ps1 -Build -OutputDirectory <fresh directory>`. This prepares assets, runs the focused and affected regression suites, exports wiki data, builds `Builds/ElectricFurnace/RivetReach.exe`, and starts the native review. The review flag is `-rr-electric-furnace-review` together with `-rr-verify`.

The working 200 W consumption and crafting cost are tuning defaults, not a balance playtest or factory-scale performance claim. The delivered electric-furnace candidate retains schema 6 and adds no serialized fields. Concurrent lava work in the shared workspace is outside this artifact.

## Wiki validation

The isolated candidate’s actual Unity export, player notes and generated pages pass **146 pages / 6,076 local links and images** through `Tools/publish_wiki.py --check`. This isolated validation preserves unfinished unrelated wiki edits in the shared workspace. Chromium reviewed the GitHub-rendered item page with all 20 images loaded, plus the setup guide at 390 px without horizontal overflow. [Deployment 34738133253](https://github.com/Starbugstone/Rivet-Reach/actions/runs/34738133253) succeeded from `fb33d7a` and published wiki commit `68c5170`. Live Chromium review confirmed all four 1280 px gameplay images loaded, followed the guide to the item page with all 20 images intact, visually reviewed the 4×4 shapeless recipe, and opened Copper Wire through its ingredient link. [Publication record](electric-furnace-2026-09-13/wiki-publication.json) retains the checks.
