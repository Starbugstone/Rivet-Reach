# Industrial workshop verification — 2026-09-10

The later [Creative workshop run](CREATIVE_RESULTS.md) checks these systems with Creative enabled on the current combined build. The evidence below retains its original artifact identity.

Implementation of the V1 workshop in [issue #2](https://github.com/Starbugstone/Rivet-Reach/issues/2). [INDUSTRY.md](../INDUSTRY.md) owns recipes, ports, rates, timing and remaining roadmap scope. This report separates actual Windows-player evidence from domain checks and artistic review.

## In-game footage

The images below are captures of the actual Unity player and imported Blender models. The reproducible review fixture builds a workshop in a normal generated world; it seeds machine contents for verification. Ordinary sessions still start empty-handed. The camera is scripted for inspection, and the scene is not a completed survival playthrough.

![Running workshop with independent blue signal, electrical cable, item transport and water loop](industry-workshop-running.png)

[Download the six-second gameplay clip](industry-workshop-preview.mp4): actual 1280×720 Unity frames at 30 fps, with crusher ON → OFF → ON and a moving inspection camera. Silent; no generated or interpolated frames. Capture and encoding are excluded from the frame-time sample.

![Boiler, alternator and crusher detail](industry-workshop-close.png)

## Machine interfaces and states

Each machine uses the existing inventory, typography, dark panels and gold controls. Relevant input/output slots, progress, fuel, water, electrical allocation, signal status and port descriptions vary by machine. Bucket actions are whole-container transactions. The bench uses the real shared recipe registry with a 4×4 grid.

![Crusher running with signal ON and allocated power](industry-crusher-running-ui.png)

| State / interface | Actual player capture |
|---|---|
| Signal OFF, zero processing demand | [Disabled crusher](industry-crusher-disabled-ui.png) |
| Signal ON, disconnected electrical cable | [No-power crusher](industry-crusher-no-power-ui.png) |
| Boiler fuel and water | [Boiler](industry-boiler_engine-ui.png) |
| Mechanical shaft to electrical output | [Alternator](industry-alternator-ui.png) |
| Source water intake and buffer | [Pump](industry-pump-ui.png) |
| Finite terrain excavation | [Drill](industry-drill-ui.png) |
| Storage and bucket transactions | [Water tank](industry-water_tank-ui.png) |
| Electrical illumination | [Workshop lamp](industry-workshop_lamp-ui.png) |
| Shared crafting at 4×4 | [Machinist’s Bench](industry-machinist-ui.png) |

## Reproduce

With the pinned Unity 6000.4.4f1 Editor open on this project, run from Windows PowerShell:

```powershell
.\Tools\Verify-Industry.ps1 -Build
```

This invokes the existing Editor request watcher, prepares the industrial imports/recipes, runs industry checks, builds `Builds/Industry/RivetReach.exe`, and launches the focused player review. Results go to ignored `Logs/IndustryVerification/`. `Tools/encode_runtime_review.py` uses Blender’s bundled H.264 encoder to assemble captured `industry-motion-*.png` frames. It does not alter the screenshots.

The Editor watcher also accepts `industry-regression` for the existing domain, starter-recipe, survival and fluid regression suites. Completed regression reports are retained here with their own dated identity; the final player review is a focused industrial check, not a rerun of every previous visual scenario.

## Measured evidence

- **Windows build:** Unity 6000.4.4f1, `Builds/Industry/RivetReach.exe`, completed **2026-09-10 11:07:38 UTC**, 0 build errors and 0 warnings; [build summary](industry-build-summary.txt). Later Editor-only tile-authoring centralization reproduced byte-identical `BlockTiles.asset` content.
- **Player review:** PASS, **83 assertions**, report timestamp **2026-09-10T11:08:45.8733129Z**; [raw report](industry-runtime-report.json). Covers working steam/electricity, actual roller animation and shutdown, button/relay/hatch, item delivery, signal independent of electricity, machine interactions, crafting, incompatible transfers, bucket conservation and residency/floating-origin round trip. No captured Unity errors.
- **Hardware:** Intel(R) Core(TM) i7-10750H CPU @ 2.60GHz, NVIDIA GeForce RTX 2060; 1280×720, view radius 4. Eight-second small-workshop sample at the existing 90 fps cap: median **11.11 ms**, p95 **11.14 ms**, maximum **11.41 ms**. Initial terrain-ready time **4.01 s**. These timings exclude screenshot/video capture and distant streaming excursions.
- **Industry domain/import checks:** **304 assertions**, [raw results](industry-domain-checks.txt). Includes deterministic Azure generation and tier gating, topology splits and dormant segments, relay/button delays, allocation fairness/priority, output backpressure, mixed logistics conservation, finite drill/source-pump behavior and normalized import bounds.
- **Scale experiments:** 10,000 signal nodes rebuilt over **25 bounded steps** with cached steady ticks around 0.001 ms. 1,000 active boiler/alternator/crusher chains + 3,000 cable nodes: p50 1,088 ms; p95 1,480 ms; max 2,093 ms. Domain simulation only, no rendering. These Editor timings are workload-specific.
- **Existing regression suites:** completed **2026-09-10 10:55:08 UTC**, before the final presentation-only refinements. [Domain](industry-regression-domain.txt): **92,340 assertions**; [survival](industry-regression-survival.txt): **47,308 assertions**, including 81 crafting and 11 furnace recipes; [independent starter recipes](industry-regression-recipes.txt): **1,862 checks** preserving all 55 survival layouts/quantities; [fluids](industry-regression-fluids.txt): **52 checks**. The starter report’s historical wording “55 active” refers to that survival subset.


## Blender and Unity import review

The source is [WorkshopKit.blend](../../ArtSource/Industry/WorkshopKit.blend), generated by [create_industry_assets.py](../../Tools/create_industry_assets.py). The [actual Blender kit render](../../ArtSource/Industry/industry-kit-review.png) was inspected against the issue’s existing [component](../concepts/signal-power/electric-power-components.jpg) and [workshop](../concepts/signal-power/automation-workshop-scene.jpg) sheets. No new concept art was generated. Shaped hoppers, paired crusher rollers, boiler bands and flywheel, alternator windings, pump impeller, drill auger, flanged connections and gauges follow their iron/copper/brass/cyan direction. The reference sheets are low resolution; final artistic acceptance remains with the user.

Runtime prefabs bake the FBX import transform into unit-scale mesh assets, preserving named moving pivots. The import check validates usable one-cell bounds as well as triangle ceilings, avoiding a scale-only false pass. Static connectors have no Animator. Machine motions are driven by simulation state; source animation remains in Blender. One shared 256² atlas/surface/emission set serves the kit, with a second shared unlit material for operating-state pilots. No character sources or animation assets were changed.

| Imported assembly | Triangles | Mesh renderers |
|---|---:|---:|
| Machinist's Bench | 1,100 | 1 |
| Signal Wire | 440 | 5 |
| Signal Conduit | 2,328 | 7 |
| Power Cable | 2,064 | 7 |
| Item Pipe | 2,064 | 7 |
| Fluid Pipe | 2,064 | 7 |
| Lever | 604 | 2 |
| Button | 448 | 2 |
| Signal Relay | 676 | 1 |
| Signal Indicator | 704 | 1 |
| Workshop Hatch | 464 | 2 |
| Workshop Lamp | 1,784 | 2 |
| Boiler Engine | 3,248 | 4 |
| Alternator | 3,180 | 3 |
| Crusher | 4,180 | 4 |
| Pump | 2,892 | 3 |
| Drill | 3,224 | 3 |
| Water Tank | 2,396 | 2 |
| Extractor | 1,932 | 3 |
| Inventory Sensor | 732 | 1 |

[Full normalized import report](industry-imported-models.txt); [Blender source counts](../../ArtSource/Industry/geometry-report.json).

Connector totals include all optional branches: runtime disables unused arms. A straight connector uses its centre and two arms; a six-way junction uses all seven renderers. These are measured asset costs, not a draw-call claim for an entire factory. The recorded player profiler field `drawCallsPeak = -1` means unavailable; unrelated zero-valued report fields were not measured by this scenario.

## Remaining limits

- The eight-second frame sample covers this small workshop at a 90 fps cap and view radius 4. It does not establish uncapped GPU cost or sustained performance for thousands of rendered machines.
- The 1,000-chain benchmark exercises simulation without Unity rendering. Large topology rebuilds spread traversal over ticks, but initial eligible-node collection/sorting remains synchronous and production currently pauses across this world during a rebuild.
- Industrial view objects are culled at 64 m; active electrical lamp lights are limited to the nearest eight. Dense factories still need rendered stress and long-session tests.
- Balance defaults, recipe costs and progression need a full survival playthrough. Hybrid wired item/fluid pipes, programmable logic, broader sensors, machine-condition outputs and durable saves remain later extensions described by the issue.
