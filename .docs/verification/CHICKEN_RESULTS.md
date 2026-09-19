# Chicken verification — 2026-09-19

[Persistent chickens](../CHICKENS.md) were checked in Unity **6000.4.4f1** and the Windows development player `Builds/Chickens/RivetReach.exe`. The clean checkpoint build completed with zero errors and zero warnings. This is a focused review build, not a published game release.

## Measured checks

- **563 Editor assertions:** exact growth/egg/readiness/cooldown boundaries, blocked growth, repeated feed and dead-state rejection, deterministic saved egg sequences, partitioned timing, seed/grain tags, shared grass/light profile, food values, recipe ingredient filters and both cookers' recipe-browser entries. Existing farming, survival and fishing Editor suites also passed.
- **321 native player assertions**, including fixture setup: daylight/grass/visibility gates; shared block/entity selection; obstruction and placement; actual mouse feeding and held-use suppression; rendered egg/meat/feather item views; one chick per pair; cooldown; seed attraction through voxel navigation; roof-blocked growth and later clearance; distant timer freeze; exactly-once egg and adult loot; no chick loot; three real cooker transactions; exact population/identity/health/timer/random-state save/load; late passive-section failure restoring the original collection and interaction source.
- **183 save assertions** and **5 fresh-process Continue assertions** passed with exit code zero. These exercise full-world/resource conservation, rejected/corrupt files, staged rollback, backup recovery and persistence after restarting the executable. See [save report](chickens-2026-09-19/save-report.json) and [restart report](chickens-2026-09-19/restart-report.json).
- An isolated copy of the actual pre-chicken schema-11 fishing checkpoint passed **17 historical-load assertions**, including resaving and retained/new generator-region checks ([report](chickens-2026-09-19/legacy-schema11-report.json)). The original checkpoint was preserved.

- The shared target-dispatch change also passed **453 fishing player assertions**, including bound input, catch conservation and save/load ([report](chickens-2026-09-19/fishing-runtime-report.json)).

Evidence: [Editor checks](chickens-2026-09-19/editor-checks.txt), [native report](chickens-2026-09-19/runtime-report.json), [build summary](chickens-2026-09-19/build-summary.txt), [farming checks](chickens-2026-09-19/farming-checks.txt), [survival checks](chickens-2026-09-19/survival-checks.txt), [fishing checks](chickens-2026-09-19/fishing-checks.txt).

The save/restart/historical and fishing regressions used the same gameplay code before the final verification-only dropped-item capture correction. The final native review repeats chicken save/load and rollback checks.

The native fixture uses an Intel Core i7-10750H, RTX 2060, Direct3D11 and a 1280×800 window. It advances deadlines explicitly to exercise lifecycle boundaries; it is not a ten-minute unattended playtest. The [small-fixture tick record](chickens-2026-09-19/passive-metrics.txt) includes first-use view/navigation costs and is not a population-performance benchmark. Zero/default frame fields in the shared report were not measured by this workload.

## Original artwork and captures

Original Blender source and front/back renders are in `ArtSource/Chickens`; `Tools/create_chicken_assets.py` reproduces the rigs, meshes, palette and icons. Actual Unity imports measured one renderer and eight bones per creature: adult **3,154 triangles**, chick **2,586 triangles**. Both import Idle, Walk, Peck and Death. Item models range from **212 to 1,640 triangles** ([import measurements](chickens-2026-09-19/unity-imports.txt)). These counts do not establish overall rendering performance.

The [illustrated guide](../wiki/Chickens.md) shows the adult/chick appearance, feeding, successful breeding, a laid egg, three recipe-browser pages and the real Cooker output. Eight actual final-player captures are retained. The egg capture enables ordinary dropped-item rendering; an obscured loot shot is omitted. All six new inventory icons are exported from their authored art.

## Coordination and remaining limits

Issue #12 was developed in the same checkout. The agreed shared prerequisite is its unified `SelectionTargets` interface and `FirstPersonPlayer` dispatch. The chicken checkpoint excludes the other agent's unfinished liquid/spawner/world-generation changes. A coordinated temporary clean interval compiled/exported the exact checkpoint inputs while preserving byte-backed-up foreign work; all 12 original files were restored and verified by SHA-256 before release. Existing unrelated presentation/settings edits were preserved and excluded from this commit.

Long-session food/breeding balance, wild encounter density across varied terrain, flock crowding/path quality and large-population rendering/simulation remain playtest work. No hostile distance despawning owns passive records. No offline production, distant chunk-loader animal production, egg hatching, feathers recipe, beds or new fencing is included. The ten-minute growth, five-minute breeding cooldown and five-to-ten-minute egg interval are working defaults.

The exported reference contains **188 items and 194 recipes**. The final publisher check against the staged source snapshot validated **213 pages and 9,291 local links/images**. Eight current gameplay captures are retained in the guide. All 627 export source fingerprints are checked against the planned checkpoint inputs.
