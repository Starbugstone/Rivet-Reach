# Alpha gameplay review probes

These are review tools, not changes to the shipped game or its recipes. The [October 2 review](../../.docs/verification/ALPHA_0_1_0_GAMEPLAY_REVIEW.md) owns conclusions and limitations.

## Recipe accounting

From the repository root:

```sh
python3 Tools/AlphaReview/catalog_review.py \
  --output .docs/verification/alpha-gameplay-review-2026-10-02/catalog-review.json
```

The input is Unity's exported catalog. The script checks constructive reachability with explicit natural-resource roots, mining tiers, stations, fuel and a possible power source. It expands the listed starter/factory layouts using actual crafting batches and retained leftovers. Raw-ore smelting supplies the bootstrap ingots; the script does not assume a free crusher before construction. It does not simulate acquisition time, routes, finite deposits, combat, placements, machine connections, tool replacement or food consumption. Its no-cycle result is a recipe feasibility result.

## Actual generator and native population survey

Use an isolated, initialized checkout of the reviewed commit with committed artwork and the pinned Unity **6000.4.4f1** Editor. Preserve any running user Editor. Copy `ReadinessSurvey.cs` into `Assets/RivetReach/Code/ReadinessSurvey.cs` in that checkout only. Allow import/compilation, then write any text into `Logs/readiness-survey-request.txt`. The Editor poll runs the survey and invokes the existing Windows build method with `BuildOptions.None`, producing `Builds/ReadinessSurvey/RivetReach.exe`. Read `Logs/readiness-survey-result.txt` and `Logs/build-summary.txt`; do not infer success from an existing executable.

`Logs/readiness-worlds.json` samples the actual current `TerrainGenerator`, `OreGenerator`, crop harvest rules, item definitions and room generator:

- Twelve explicit seeds, including negative/extreme seeds; a complete 129×129 surface food/tree square around spawn.
- Surviving **same-band vein centres** within horizontal ±128 and each ore's complete height band. Nearest means straight-line distance to that centre from the spawn surface, not a walking route or the nearest ore block.
- Air contact in at most the nearest 16 surviving veins per ore. Air exposure may be inside an inaccessible cave; it does not establish surface visibility or a traversable connection.
- Biomes on a 16-block lattice within ±768. Rooms in 16 regions per seed, a 512×512 horizontal area; this finite sample cannot validate a 1% buried-room distribution.

Close only the Editor started for this checkout, then run the complete built player directory:

```powershell
RivetReach.exe -rr-readiness-survey -rr-output <fresh-absolute-directory> `
  -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -force-d3d11 `
  -logFile <fresh-absolute-directory>\player.log
```

Do **not** add `-rr-verify`: that launches the separate ordinary verification runner. The observer explicitly sets a 1.64 m eye position before disabling player movement. Six fresh worlds cover two seeds × surface day / surface night / deep cave day, each with 120 real seconds of **normal-cadence natural spawning and live creature AI** after residency settles. Radius four includes the 48 m spawn shell; this is not a default-distance performance test. Initial generated terrain is unmodified. Population measurements use an invincible, stationary Creative observer, no supplied creatures, no killings and no breeding. They measure local accumulation and species selection, not combat difficulty, perceived encounter frequency during exploration, or a normal Survival route. Surface observations can include underground Floaters. The CSV separates cage-origin creatures if any are present.

The report's `attackHits` is not a difficulty measure: Creative rejects damage. `SpawnRejections` uses the `SpawnRejection` enum order; successful creation validates twice, and a search may examine many sites, so these counters are **not spawn-cycle counts**. Each CSV samples living species and persistent chickens roughly once per second. The final JSON must have six completed samples, no errors and a nonempty timestamp. Afterwards the probe captures five generated biome viewpoints. All screenshots are direct native captures.

An initial unpublished run was discarded because its disabled controller had not positioned the eye camera. Only the corrected probe's final run belongs to the maintained evidence. `build-identity.json` identifies its exact source and assembly. No production code is copied back from the isolated checkout.
