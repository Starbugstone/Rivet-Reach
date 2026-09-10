# Multiblock tanks and pipe fittings — 2026-09-10

[Clean in-game machinery gallery](MACHINERY_SHOWCASE.md) provides staged presentation shots with the HUD hidden.
[MULTIBLOCKS.md](../MULTIBLOCKS.md) owns the working implementation selected from issue #3. This feature adds a reusable validator/machine-data lifecycle, individually editable tank shells, exact shared storage, connected Blender surfaces, valve and level controls, and independent signal/power fittings on both transport pipes.

## Measured result

- **Build:** Unity 6000.4.4f1, completed 2026-09-10T13:15:30.072162+00:00; 0 errors, 0 warnings. [Build summary](multiblock-build-summary-2026-09-10.txt), [artifact identity and hashes](multiblock-build-identity-2026-09-10.json).
- **Windows player:** PASS, **140 assertions**, 2026-09-10T13:16:19.5777368Z; no captured Unity errors. [Raw report](multiblock-runtime-2026-09-10.json). Covers cross-chunk formation, shared ports, wired valve shutdown/enable, sensor threshold, whole-bucket swaps with a full inventory, actual item-pipe fitting transactions, mining, breach diagnostics, controller protection, repair and residency/floating-origin round trip.
- **Focused domain:** **48 assertions**. [Results](multiblock-core-checks-2026-09-10.txt). Includes non-cubic and maximum sizes, hard scan budgets, duplicate controllers, invalid interior/world fluid, glass roofs, orientation, touching tanks/claims, storage retained while dormant, expansion/shrink, exact sub-bucket recovery, shared capacity and source reservations across separate graphs, conflicting fluid identities, both pipe families with isolated signal/power channels, and a different validator using non-fluid machine data.
- **Industry/import regression:** **320 assertions**. [Results](multiblock-industry-checks-2026-09-10.txt), [normalized model counts and bounds](multiblock-industry-models-2026-09-10.txt). All eight tank parts have imported one-cell geometry. Existing power/signal, machines, item/fluid transfers and scale checks were rerun.
- **Other regression suites:** [domain](multiblock-domain-checks-2026-09-10.txt) **92,348** assertions; [survival](multiblock-survival-checks-2026-09-10.txt) **47,316** assertions, 89 crafting/11 furnace recipes; [starter recipes](multiblock-starter-recipe-checks-2026-09-10.txt) **1,862** checks preserving the 55 survival layouts and quantities; [world fluids](multiblock-fluid-checks-2026-09-10.txt) **52** checks.
- **Actual formed tank geometry:** **104,594 triangles**, **122 mesh renderers**, including 23 glass renderers and one liquid volume across 96 shell views. [Count scope](multiblock-render-cost-2026-09-10.txt). The counter uses only roots owned by tank presentation, excluding surrounding terrain and pipes.
- **Small fixture timing:** Intel(R) Core(TM) i7-10750H CPU @ 2.60GHz; NVIDIA GeForce RTX 2060; 1280×720, view radius 4, existing 90 fps cap, six-second sample. Median **11.11 ms**, p95 **11.13 ms**, max **11.26 ms**. Initial terrain-ready time **4.16 s**. Screenshot I/O and the distant streaming excursion are outside the frame sample. Actual draw-call profiling was unavailable (`drawCallsPeak = -1`).

## Actual player captures

![Formed player-built tank with connected glass and separate pipe routes](multiblock-formed-connected-tank-2026-09-10.png)

![Tank controller quantity, capacity and recovery controls](multiblock-controller-ui-2026-09-10.png)

| Review | Capture |
|---|---|
| Controller, hatch, sensor and wired valve detail | [Tank controls and fittings](multiblock-controls-and-pipe-additions-2026-09-10.png) |
| Independent fittings on an actual Item Pipe | [Pipe interface](multiblock-pipe-channel-ui-2026-09-10.png) |
| Missing member coordinate with retained contents | [Breach diagnostics](multiblock-breach-diagnostics-2026-09-10.png) |
| Repaired tank after residency round trip | [Connected side window](multiblock-repaired-side-window-2026-09-10.png) |

## Verification boundary

The final review is built in an isolated checkout of committed player baseline `a963423` plus this feature's source/assets. Concurrent character/held-tool work in the shared workspace was deliberately excluded from that artifact. The earlier shared-workspace run completed its tank assertions but failed on incomplete character-animation imports during that separate asset revision; those captures are not delivery evidence. This report does not certify the concurrent player revision or relabel prior feature scenarios as runs of this executable.

Ordinary sessions start empty-handed. The scripted review constructs and seeds a 6×4×5 tank across an X chunk boundary in a generated world, with a 6,000 L capacity, separate feed/output routes, an upgraded fluid pipe, controlled valve, sensor and visible liquid. It uses the normal world placement/mining, machine interaction and inventory transaction authorities. Source meshes and imported meshes are real Blender/Unity artifacts; concept illustrations are only references.

## Reproduce

With the pinned Editor open on this project, run `Tools/Verify-Multiblocks.ps1 -Build`. The Editor watcher accepts `multiblock-build` for the full import/check/build pass, `multiblock-checks` for imports/regressions without a player, `multiblock-logic` for the focused domain checks, and `multiblock-player` for rebuilding after presentation-only revisions. The batch entry point for a separate, closed project is `RivetReach.Editor.ProjectBuild.PrepareAndBuildMultiblocks`.

The build output is `Builds/Multiblocks/RivetReach.exe`; the focused player flag is `-rr-multiblock-review`, together with `-rr-verify` and `-rr-output`. The verification wrapper defaults to ignored `Logs/MultiblockVerification/`. A completed isolated executable is copied into the main workspace's same build location for review.

## Art and remaining limits

[TankKit.blend](../../ArtSource/Multiblocks/TankKit.blend), the [original authoring script](../../Tools/create_multiblock_assets.py), [actual Blender kit render](../../ArtSource/Multiblocks/tank-kit-review.png) and [source geometry counts](../../ArtSource/Multiblocks/geometry-report.json) retain reproducible asset evidence. The models use the existing original iron/copper/brass atlas, separate clear glass and fluid materials, gauges, rivets, hatches and keyed signal fittings. Formed frames activate authored casing skins; matching panels remove shared borders. The renderer combines selected surfaces per cell and shares meshes by configuration. No tank formation removes/replaces the underlying blocks.

Artistic acceptance and a full survival progression playthrough remain with the user. The model follows the game's simplified industrial materials; the rendered water is a simple quantity-driven volume, without the concept illustration's detailed ripples/refraction. Captures use a scripted review camera and fixture, rather than demonstrating a completed survival playthrough.

Session chunk unload/reload preserves world/structure identity, contents and configuration. Whole-world durable saves, recovery parcels, arbitrary shapes, physical breach leaks and additional playable liquids remain later scope. Controller removal requires draining first; the explicit recovery outlet handles sub-bucket remainders.

One hard-budget structure scan is processed per tick, using a chunk-indexed candidate footprint. Residency changes still invalidate all controllers and topology rebuilds may pause the world's industry. The timing sample covers this one tank and small pipe fixture at a frame cap; it does not establish dense-factory rendering, uncapped GPU cost, multiplayer or long-session scaling. Source triangle counts include every optional surface, while the formed mesh report counts only selected live surfaces. Mesh-renderer count is not an actual draw-call measurement.
