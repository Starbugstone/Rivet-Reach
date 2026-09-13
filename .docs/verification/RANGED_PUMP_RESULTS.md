# Ranged liquid pump verification — 2026-09-13

[Feature rules](../RANGED_PUMP.md) own the Floater Rock upgrade and the user's confirmed **eight-block reach on each axis**. [The player guide](../wiki/Ranged-Liquid-Pump.md) contains four actual gameplay captures of source collection, controls, pipe setup and the receiving tank.

## Verified artifact

The isolated Unity **6000.4.4f1** candidate builds successfully with **zero errors and zero warnings**, in **19.71 seconds** with a warm cache. The packaged review player is **`Builds/RangedPump/RivetReach.exe`**. [Build summary](ranged-pump-2026-09-13/build-summary.txt) and [SHA-256 identities](ranged-pump-2026-09-13/artifact.json) identify the actual tested executable, gameplay assembly and FBX. This candidate retains **schema 8** and excludes the concurrent bridge/chunk-loader extension; older feature evidence is not represented as rerun on this build.

## Domain and native evidence

- **64 focused assertions pass**: exact Pump + Floater Rock recipe, four inclusive corners for both liquids, 39/40-tick boundary, zero power endpoint/demand, full-buffer protection, all six excluded radius-nine faces, flow/falling rejection, 256-read scan bound, signal pause/resume, unloaded target, failed removal, fluid switching/mixing, competing pumps, all-face pipe export, typed machine save round trip, additive compatibility and changed-definition rejection. [Focused report](ranged-pump-2026-09-13/ranged-pump-checks.txt).
- Existing industry, fluid, portable-storage and connection regression entry points pass in the same build workflow. [Industry report](ranged-pump-2026-09-13/industry-checks.txt) and [fluid report](ranged-pump-2026-09-13/fluid-checks.txt) retain detailed checks. [Floater checks](ranged-pump-2026-09-13/floater-checks.txt) also accept a real pre-Floater schema-7 checkpoint and reject unrelated earlier item/mob changes.
- **140 native assertions pass**, including fixture setup, with **zero reported runtime errors**. Actual world edits drain four lava sources through a fluid pipe into exactly **40 L** in the receiving tank. Full buffers stop further removal. Save/load preserves 17 ticks of partial collection, untouched sources and later a typed 10 L lava buffer. Machine panels show the actual liquid/quantity, and mining the drained pump recovers its item. [Native report](ranged-pump-2026-09-13/runtime-report.json) and [exit code](ranged-pump-2026-09-13/exit-code.txt).
- The original Blender asset and Unity import have **2,008 triangles in three mesh parts**: cradle, moving core and status lamp. Imported vertices remain inside one cell. [Blender source render](../../ArtSource/RangedPump/ranged-pump-review.png), [actual Unity model](ranged-pump-2026-09-13/model.png) and [native mesh count](ranged-pump-2026-09-13/model.txt) were visually reviewed. The icon is rendered from that model.

The native source pool is a staged, elevated fixture; this is not a natural-cave playtest or factory-scale benchmark. Automated source checks use selected corners and face boundaries, not every arrangement of 4,913 cells. The recipe cost, sustained throughput balance and artistic acceptance remain subject to player review. No new frame-time or multiplayer performance claim is made.

## Reproduction and wiki

Run `Tools/Verify-RangedPump.ps1 -Build` with the target project open and refreshed in the pinned Editor. It invokes the focused/affected checks, exports wiki data, builds the review player and runs `-rr-verify -rr-ranged-pump-review`. The authoring script is `Tools/create_ranged_pump.py`; its editable `.blend` and geometry report are under `ArtSource/RangedPump`.

The isolated candidate's Unity export contains **136 items**. GitHub-rendered Markdown review loaded all seven recipe-page images, checked its exact two ingredients and the 4×4 shapeless grid, and found no horizontal document overflow at 390 px. [Recipe review](ranged-pump-2026-09-13/wiki-recipe.png) records the layout. `python3 Tools/publish_wiki.py --check` passes **152 pages and 6,195 local links/images**, with 136 item pages and 123 crafting/processing recipes. All four 1280 px guide screenshots load in Chromium at 390 px without horizontal document overflow. Deployment is recorded below after publication.
