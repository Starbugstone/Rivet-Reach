# All-face connections and wrench — 2026-09-12

The [power-status follow-up](POWER_STATUS_RESULTS.md) identifies the later build that separates registered electrical links from supplied watts. The artifact identities and wrench/art evidence below remain those of the earlier focused run.

[Industry rules](../INDUSTRY.md#wrench-and-configurable-pipe-ends--2026-09-12) and the [player pipe guide](../wiki/Pipes.md) own behavior. These checks exercise selected-wrench authority, all six faces, resource conservation and additive save compatibility.

## Tested build

Unity **6000.4.4f1**, Windows x64 Development, **Direct3D11**, `Builds/Connections/RivetReach.exe`. [Build result](connections-2026-09-12/build.txt): succeeded, **0 errors, 8 warnings**, 31.247 seconds. The warnings are obsolete object-search API usage in the concurrent equipment verification code. The review builder temporarily selects the release's existing Direct3D11 renderer, then restores Editor settings.

[Artifact hashes](connections-2026-09-12/artifact-hashes.json) identify the executable, compiled game assembly and wrench assets. [Source snapshot](connections-2026-09-12/source-snapshot.json) identifies runtime code and definitions at build time, based on `296f5d6` plus this feature and concurrent equipment presentation changes. The concurrent equipment verification script was subsequently extended; the hash manifest retains the version compiled for this run. This is a focused workload on that snapshot, not a claim that earlier feature screenshots were recaptured or every concurrent feature was tested.

## Measured checks

| Check | Result |
| --- | --- |
| [Connection domain fixtures](connections-2026-09-12/checks.txt) | **156 assertions**: six faces × four rotations, 160 W crusher allocation/disconnection, independent item/fluid ends, no hidden machine bridge, exact transfers, chest extraction, boiler fuel, dormant boundaries, wrench crafting and direction validation |
| Existing Editor checks | [Battery](connections-2026-09-12/battery-checks.txt), [crank](connections-2026-09-12/hand-crank-checks.txt), [door](connections-2026-09-12/door-checks.txt), [multiblock](connections-2026-09-12/multiblock-checks.txt), [4,736 connected-pipe checks](connections-2026-09-12/connected-pipe-checks.txt), [92,355 domain assertions](connections-2026-09-12/domain-checks.txt) and [1,888 starter-recipe assertions](connections-2026-09-12/starter-recipe-checks.txt) passed |
| [Standalone connections](connections-2026-09-12/runtime/runtime-report.json) | **176 assertions**, exit **0**, no recorded errors, 1280×800 |
| [Full save regression](connections-2026-09-12/save/runtime-report.json) | **183 assertions**, exit **0**, no recorded errors |
| [Fresh-process Continue](connections-2026-09-12/save-resume/runtime-report.json) | **5 assertions**, exit **0**, no recorded errors |
| [Actual earlier checkpoints](connections-2026-09-12/legacy/runtime-report.json) | **21 assertions**, exit **0**: real schema-2 and schema-3 worlds load, migrate to schema 4 and round-trip all serialized fields; unrelated changed content is still rejected |

The standalone scenario drives the actual input system: empty-handed right-click with a wrench elsewhere, another selected item and Interact cannot change an end; selected-wrench right-click changes once, holding does not repeat, and the tool is not consumed. Pipe centres, pipe-to-pipe joins, unattached ends, out-of-reach ends and power cables reject direction changes. It crafts the real three-iron recipe, inspects the shared held/dropped model, powers a crusher from a cranked battery, checks horizontal and vertical arrows, and saves/reloads per-end directions. Invalid saved directions reject with the existing world intact.

Source candidates are captured before item delivery; an initially empty chest cannot forward a new item in the same phase. The pre-existing fluid reservation checks also pass. Existing tank formation, valve/signal and drain-only recovery gates remain authoritative.

An initial save-resume run passed its assertions but exited with `-1073741819` on the project's default Direct3D12 selection. It is **not** counted as a clean pass. The maintained final evidence above was rerun on the final Direct3D11 build and exited normally; no Direct3D12 stability claim is made.

## Art and visual review

Original source: [Wrench.blend](../../ArtSource/Wrench/Wrench.blend), [reproduction script](../../Tools/create_wrench.py), [source render](../../ArtSource/Wrench/wrench-review.png), [source geometry](../../ArtSource/Wrench/geometry-report.json). The Blender evaluated source reports **1,052 triangles**. [Actual Unity import](connections-2026-09-12/wrench-import.txt) reports **892 triangles, one renderer**, shared Workshop material, approximately **0.21 × 0.54 × 0.04 m**, with normals and UVs. Counts describe different source/import representations.

Inspected the Blender render and actual player screenshots: [held wrench](connections-2026-09-12/wrench-held.png), [blue item input](connections-2026-09-12/item-end-blue-input.png), [red item output](connections-2026-09-12/item-end-red-output.png), [paired fluid arrows](connections-2026-09-12/fluid-end-arrows.png), [vertical ends](connections-2026-09-12/vertical-fluid-arrows.png) and [pipe help](connections-2026-09-12/pipe-connection-help.png). Blue enters the machine; red leaves it. Indicators remain visible with empty hands and idle flow. No player model or animation asset was replaced by this feature.

## Reproduction and limits

In the pinned Editor, choose **Rivet Reach → Build configurable connections review**, or write `build` to `Logs/connections-build-request.txt`. `checks` runs its Editor assertions without a build. The result is written to `Logs/connections-build-result.txt`.

Run `Tools/Verify-Connections.ps1` for the playable scenario. Pass `-LegacyDirectory` pointing at [the retained fixture directory](connections-2026-09-12/legacy-fixtures/fixtures.json) for earlier-checkpoint migration. Fixture provenance and hashes are retained; the test writes migrated copies to its fresh output directory. Run `Tools/Verify-Saves.ps1 -Executable <absolute review executable>` for save/resume. Existing evidence and user saves are preserved.

The wrench recipe is a working balance choice: three iron ingots at a workbench; stack limit one. Item endpoints use existing supported inventories (including chests and boiler fuel); ordinary furnaces/workbenches gain no new logistics channel. Output cannot extract a crusher's raw input slot or create inputs on an output-only machine. Indicators refresh nearby endpoints within 32 m, sharing meshes/materials. Large-factory profiling, multiplayer authority and user artistic/gameplay acceptance remain unmeasured.

The wiki export contains **132 items and 108 total crafting/processing recipes**. All generated item/reference pages were refreshed; the new wrench and Pipes guide are linked from navigation and affected power/fluid/tank/signal guides. GitHub Markdown API output was inspected in Chromium: all 132 item-index images loaded, the wrench page loaded all seven images, its iron ingredient link opened the iron-ingot page, and the Pipes guide had no document overflow at 390×844. Retained previews: [recipe](connections-2026-09-12/wiki-wrench-recipe.png), [narrow guide](connections-2026-09-12/wiki-pipes-mobile.png). The preview uses approximate wiki styling; live publication is checked separately.

## Published wiki

[Automatic deployment 34691438729](https://github.com/Starbugstone/Rivet-Reach/actions/runs/34691438729) succeeded from repository commit `c03d324`, publishing wiki commit `3f73fca`. All **290 deployed files**, including the publishing manifest, match the reviewed publisher output byte-for-byte. The live [Pipes guide](https://github.com/Starbugstone/Rivet-Reach/wiki/Pipes) displays the strict selected-wrench rule and links to the [Wrench recipe](https://github.com/Starbugstone/Rivet-Reach/wiki/Item-wrench).
