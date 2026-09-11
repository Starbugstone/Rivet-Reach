# Menu and crafting pointer responsiveness — 2026-09-11

The interface now applies button, slider and item-highlight colours immediately, removing Unity's default 100 ms transition. Held-stack artwork updates after mouse event dispatch in `LateUpdate`, so pickup/deposit feedback reaches the current rendered frame. Separate inventory canvases bound geometry rebuilding to the changing station/sidebar/tooltip regions. [CRAFTING.md](../CRAFTING.md#pointer-feedback-and-moving-ui) owns the behavior and retained-view lifecycle.

## Measured comparison

Two successive Windows Development builds used the same moving-pointer benchmark: Unity 6000.4.4f1, i7-10750H, RTX 2060, Direct3D 12, 1280×720, seed 246813, settled terrain, view radius 4, and mobs disabled during the focused phase. Each phase warms for 60 frames and samples 240 frames with frame pacing disabled; normal play retains its existing 90 FPS cap. Queued mouse events traverse actual buttons, sidebar icons or inventory slots. The crafting phase also crafts one log every frame.

| Workload | Baseline frame median / p95 | Optimized frame median / p95 | Baseline → optimized geometry-job median |
| --- | --- | --- | --- |
| Menu hover | 3.715 / 5.157 ms | 3.401 / 4.397 ms | 0.0595 → 0.0582 ms |
| Sidebar hover | 5.597 / 6.939 ms | 5.281 / 5.979 ms | 0.5648 → 0.0660 ms |
| Held-stack motion | 5.520 / 6.809 ms | 5.216 / 5.664 ms | 0.0031 → 0.0028 ms |
| Crafting and slot hover | 5.428 / 6.943 ms | 5.191 / 5.699 ms | 0.5703 → 0.0463 ms |

The measured `Canvas.GeometryJob` median fell **88.3% for sidebar hover and 91.9% for repeated crafting**. This counter covers UI geometry work, not total frame cost. The moving held stack was already on a separate canvas; its change addresses dispatch-to-render timing. Menu geometry cost is essentially unchanged; menu highlights no longer spend 100 ms fading into their state.

[Baseline pointer samples](pointer-2026-09-11/baseline-pointer-timings.csv) and [optimized pointer samples](pointer-2026-09-11/optimized-pointer-timings.csv) retain p95/max values and GPU/main-thread counters. Counter timings overlap and must not be added. This is one comparison on this workstation, not a physical mouse-to-display latency measurement or a guarantee for every resolution/world. The optimized crafting phase still had a 13.679 ms maximum frame interval.

There is a small opening-cost tradeoff from toggling the extra retained canvases: warm open plus managed canvas flush measured 4.683 / 4.948 ms median/p95, versus 4.380 / 4.526 ms in the baseline. Craft UI plus managed flush measured 0.139 / 0.155 ms, versus 0.141 / 0.195 ms. [Baseline action timings](pointer-2026-09-11/baseline-crafting-timings.csv), [optimized action timings](pointer-2026-09-11/optimized-crafting-timings.csv), and [optimized paced-frame counters](pointer-2026-09-11/optimized-crafting-frame-counters.csv) retain those separate workloads. The previous [retained-screen report](SCREEN_REUSE_RESULTS.md) keeps its original dated build identity.

## Verification and artifacts

Review player: **`Builds/RecipeBrowser/RivetReach.exe`**. Keep its adjacent data/runtime folders. The [build summary](pointer-2026-09-11/build-summary.txt) reports zero errors and warnings. [Baseline identity](pointer-2026-09-11/baseline-build-identity.json) and [optimized identity](pointer-2026-09-11/optimized-build-identity.json) record the source baseline, changed-source hashes and actual runtime assembly hashes. The baseline includes the new benchmark but no production optimization.

The [native runtime report](pointer-2026-09-11/runtime-report.json) passes **833 assertions**, with no errors and [process exit 0](pointer-2026-09-11/process-exits.json). The baseline passed 829 assertions; the optimized run adds four checks. The native pointer/browser/crafting suite checks recipe navigation, 2×2/3×3/4×4 placement, left dragging, right painting, full inventories, machine rebinding, session replacement and stale-input conservation. Additional coverage checks every retained child canvas/raycaster is disabled on hide and restored on reopen, and verifies a pickup after `Update` is drawn with its correct count and pointer position that same frame. A deposit after `Update` must also hide the held image in the current rendered frame.

[Crafting domain checks](pointer-2026-09-11/crafting-checks.txt), [starter recipe checks](pointer-2026-09-11/starter-recipe-checks.txt), [recipe transfer checks](pointer-2026-09-11/transfer-checks.txt) and [browser index checks](pointer-2026-09-11/index-checks.txt) also pass during the build.

Inspected images: [inventory/sidebar](pointer-2026-09-11/sidebar-inventory.png) and [recipe overlay](pointer-2026-09-11/crusher-recipe.png). The station, portrait, tooltip and recipe overlay retain their ordering and readable layout.

Reproduce with `powershell -File Tools/Verify-RecipeBrowser.ps1 -Build -OutputDirectory <fresh Windows path>`. The script builds an isolated snapshot, preserving the open project and recovery files. The benchmark writes `pointer-timings.csv` alongside existing crafting timings and the runtime report. No release package was replaced. Large factories, higher resolutions and subjective pointer feel still require play review.
