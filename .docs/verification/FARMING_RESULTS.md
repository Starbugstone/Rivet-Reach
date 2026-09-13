# Farming and cooking verification — 2026-09-13

Working scope: [farming specification](../FARMING.md). Final gameplay build completed September 13 at 12:24 CEST; subsequent changes only refine documentation and import review. Review player: **`Builds/Farming/RivetReach.exe`**, Unity **6000.4.4f1**, Windows x64 development build. Actual player captures use Direct3D 12 on an RTX 2060 / i7-10750H at 1280×800. Original Blender assets were authored with Blender 5.2 and reviewed in both source renders and the imported game.

## Measured checks

- **4,312 assertions passed:** [Farming catalog/simulation checks](farming-2026-09-13/checks.txt) exercise immature stock conservation, mature yields, configured tagged fuels, edible eligibility, all six recipes on both cookers, exact electricity/heat, mixed and split tag inputs, rear-only fuel, output-only extraction, output/full/residency/signal gates and recipe-browser alternatives. Seeded surface sampling finds every species and checks point/chunk agreement for all three supported generators.
- [Terrain checks](farming-2026-09-13/terrain-generation-checks.txt) cover the existing biomes, cave connectivity, ore bands, bedrock, dry spawn and generation determinism. Recorded timings are bounded generator samples, not farming frame-rate evidence.
- Existing [industry](farming-2026-09-13/industry-checks.txt), [survival/crafting](farming-2026-09-13/survival-checks.txt), [electric furnace](farming-2026-09-13/electric-furnace-checks.txt) and [shared allocation](farming-2026-09-13/grid-allocation-checks.txt) suites passed during integration. The survival suite preserves the original starter recipe layouts and checks bootstrap reachability with the new crop sources.
- **187 assertions passed:** [Standalone gameplay](farming-2026-09-13/runtime-report.json) exercises genuine generated wild-plant growth on natural soil, hoe/plant/grow/harvest, normal cooker opening/UI, mixed vegetable recipes, actual pipes/chests, save/load during partial work and exact output after resuming.
- **17 assertions passed:** [Legacy migration](farming-2026-09-13/legacy-report.json) uses an isolated copy of a valid pre-update schema-9 bridge checkpoint. It checks all old edits, protection around the old player, new terrain at (20000, 20000), then saves and reloads their independent generator versions. An intentionally invalid bridge checkpoint from the historical fixture set remained rejected by its original ownership/name validation.

- Full save regression passed: [save/rollback/recovery](farming-2026-09-13/save/runtime-report.json) and [fresh-process Continue](farming-2026-09-13/save-resume/runtime-report.json). The fixture compares the saved growth queue count now that natural plants also register growth.

- [Eating regression](farming-2026-09-13/eating-report.json) passed with the new `edible` gate: both model variants, both skins, three field-of-view settings, exact consumption, cancelled bites, full hunger, Creative and planting/station precedence.

## Art and presentation

[Imported model measurements](farming-2026-09-13/imports.txt) check every new FBX and both normalized cooker prefabs against one-block bounds. Crop/item meshes use one renderer each; the basic cooker has 1,616 imported triangles and the electric cooker 1,424, with two renderers each. These are geometry counts, not draw-call or performance guarantees. Crops are merged into the terrain chunk mesh; source vertices are copied on the main thread before worker use.

Original sources and reproducible authoring live in `ArtSource/Farming`, `Tools/create_farming_assets.py` and `Tools/create_cooker_assets.py`. [Blender plant/item review](../../ArtSource/Farming/farm-review.png) and [basic cooker review](../../ArtSource/Farming/cooker-review.png) were visually compared with the actual imports. The [player guide](../wiki/Farming-and-cooking.md) contains current in-game growth stages, natural growth, both operating machines, both interfaces and pipe setup. Inventory icons use the same authored models.

## Wiki review

The export contains 175 registered items and 143 recipes. All 193 wiki pages pass 7,490 local page/section/image links. The GitHub-rendered stew preview loaded all 22 images and followed its carrot ingredient link. The farming guide loaded all six actual game captures at 390×844 with no document overflow. [Automatic deployment 34752058545](https://github.com/Starbugstone/Rivet-Reach/actions/runs/34752058545) passed and published wiki commit `2332764` from implementation commit `ffbe58c`. Live Chromium review loaded all six guide captures and all 22 stew-page images, checked `edible` and tagged substitutions, and followed the carrot ingredient through to its page. [Published guide capture](farming-2026-09-13/wiki-live-guide.png) and [published stew recipe capture](farming-2026-09-13/wiki-live-stew.png) retain the deployment evidence. The live Electric Cooker page displayed 200 W with all 15 images loaded; the item index loaded all 175 icons with no missing or unlinked images.

## Limits

This is a bounded automated gameplay review, not long-session survival balancing. Crop density, food cadence, recipe economics, very large farms/factories and mixed-generator boundary appearance still need representative player review and profiling. No offline growth/production, irrigation, animal system, bed, fishing, compost, weather or spoilage is claimed. The approved legacy fallback cannot recover exploration that old saves never recorded; exact history starts with schema 10.
