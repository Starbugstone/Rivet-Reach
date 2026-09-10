# Machinery in-game showcase

Actual Windows Unity player captures of the [multiblock tank](../MULTIBLOCKS.md) and [industrial workshop](../INDUSTRY.md), staged on 2026-09-10. Original Blender models use their normal game materials and connected-block presentation. The sun cycle and electrically supplied workshop lamps provide the lighting. HUD and player hands are hidden; no image generation, compositing or external colour grading is used.

The staged session contains a 6×5×5 tank, visible shared water storage, a level sensor, a wired valve, fluid and item pipes with independent channel fittings, a boiler/alternator pair, an operating crusher, an overhead fluid header and a vertical item route. Fuel, ore and water are supplied by the capture fixture. Ordinary sessions still start empty handed. The simulation pauses for consistent stills after the fixture checks formation, power and sensor state.

## Gallery

### Golden workshop

![Workshop and tank in afternoon sunlight](machinery-01-golden-workshop-2026-09-10.png)

### Tank hero

![Low angle across the connected tank shell](machinery-02-tank-hero-2026-09-10.png)

### Connected controls

![Tank interfaces and pipe channel fittings](machinery-03-connected-controls-2026-09-10.png)

### Blue hour

![Workshop lamps and tank at dusk](machinery-04-blue-hour-2026-09-10.png)

### Steam and steel

![Boiler and alternator detail with tank behind](machinery-05-steam-and-steel-2026-09-10.png)

### Vertical routing and junctions

![Overhead fluid header with vertical bends and real branches](machinery-06-vertical-junctions-2026-09-10.png)

## Pipe revision

Following the user’s image review, the connected pipes use uniform straights and rounded elbows; branch fittings appear only at actual T/cross/multi-axis junctions. Enclosed signal/power conductors follow the same shape rules. The independent fitted leads also connect continuously across cell boundaries.

![Actual Blender source review of straight, elbow, T, cross, vertical and six-way shapes](connected-pipes-blender-2026-09-10.png)

## Measured evidence

Captured in Unity **6000.4.4f1** at **3840×2160** on 2026-09-10, using the committed `0142dcf` multiblock baseline plus this pipe/capture revision. Concurrent avatar changes were excluded from the capture build and preserved in the main workspace. [Build identity](machinery-build-identity-2026-09-10.json) records the executable and game assembly hashes. The Windows build completed with **zero errors and zero warnings**.

- [4,736 pipe assertions](machinery-connected-pipe-checks-2026-09-10.txt): all 64 masks in six Blender mesh families; UV/material availability, cell bounds, valid normals, matching boundary positions, tangent boundary normals and both rotated transport families with independent signal/power graphs.
- [197 runtime assertions](machinery-runtime-2026-09-10.json): actual placed workshop formation and power, overhead fluid delivery, vertical item connections, cross → T → cross edits reflected in live renderers, one renderer for a plain pipe and three for an upgraded pipe; no Unity errors.
- [320 industrial checks](machinery-industry-checks-2026-09-10.txt) and [48 multiblock checks](machinery-multiblock-checks-2026-09-10.txt) also pass on the revised build inputs.

The [Blender geometry report](../../ArtSource/Pipes/geometry-report.json) records each variant separately. A connected straight item/fluid section has 32 source triangles; its two independent straight leads add 32 each when installed. These counts describe selected geometry, not measured frame rate or draw-call performance. The source and final Unity renders were visually inspected. Wider factory scale and artistic acceptance remain subject to playtesting/user review.

## Reproduce

Use the pinned Unity 6000.4.4f1 Editor menu **Rivet Reach → Build Machinery Showcase**. This produces `Builds/MachineryShowcase/RivetReach.exe` without the development watermark. Then run:

```powershell
.\Tools\Capture-Machinery.ps1
```

The tool prints the six PNG paths under a fresh `Logs/MachineryShowcase-*` directory. It also accepts `-PlayerPath` and `-OutputDirectory`. Captures use Unity's 2× screenshot rendering at a requested 1920×1080 player window. The capture entry point requires all three flags: `-rr-verify -rr-multiblock-review -rr-machinery-showcase`.

For a closed project or isolated checkout, the existing build entry point is:

```powershell
& 'D:\Unity\Hub\6000.4.4f1\Editor\Unity.exe' -batchmode -quit -projectPath '<project>' -executeMethod RivetReach.Editor.MachineryShowcaseBuild.Build -logFile '<project>\Logs\showcase-editor.log'
```

After editing pipe sources, run `Tools/create_connected_pipes.py` in Blender and use the Editor batch entry point `RivetReach.Editor.MachineryShowcaseBuild.PreparePipesAndBuild` to regenerate the normalized families and run the checks before building.

These are staged presentation images, not a survival progression or performance benchmark. [Multiblock verification](MULTIBLOCK_RESULTS.md) owns the functional regression evidence.
