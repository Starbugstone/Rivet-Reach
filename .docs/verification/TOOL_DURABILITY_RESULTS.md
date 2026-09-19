# Tool speed and durability verification — 2026-09-19

[Rules](../TOOLS.md) · [Player guide and current in-game captures](../wiki/Tools-and-durability.md)

Implemented in source commit `712592b`, coordinated with food commit `9c6c217`. Higher-material pickaxes and axes already had increasing effective-target speeds; this update verifies those speeds and adds persistent per-instance wear, individual worn tools, condition bars and breakage. Numeric lifetimes are working tuning values, not a balance conclusion.

## Native build and checks

Pinned Unity **6000.4.4f1**, Windows Direct3D11, RTX 2060. `Builds/Tools/RivetReach.exe` built successfully with zero errors and four existing deprecated-query warnings. [Build summary](tools-durability-2026-09-19/build-summary.txt) and [assembly identities](tools-durability-2026-09-19/assembly-sha256.txt) identify the final focused-check build.

| Check | Result | Evidence |
|---|---:|---|
| Tool domain checks | 111 PASS | [Report](tools-durability-2026-09-19/domain-checks.txt) |
| In-game tool interactions | 243 PASS | [Native report](tools-durability-2026-09-19/runtime-report.json) |
| Worn checkpoint, fresh process | 5 PASS | [Restart](tools-durability-2026-09-19/restart-report.json) |
| Actual historical schema-17 food checkpoint | 9 PASS | [Migration](tools-durability-2026-09-19/schema17-migration-report.json) |
| Actual historical schema-16 checkpoint | 9 PASS | [Migration](tools-durability-2026-09-19/schema16-migration-report.json) |
| General save/recovery regression | 183 PASS | [Report](tools-durability-2026-09-19/save-regression-report.json) |
| General save restart | 5 PASS | [Report](tools-durability-2026-09-19/save-regression-restart-report.json) |
| Existing inventory checks | 1,253 PASS | [Report](tools-durability-2026-09-19/inventory-checks.txt) |
| Existing portable-storage checks | 86 PASS | [Report](tools-durability-2026-09-19/portable-storage-checks.txt) |
| Existing survival checks | 47,725 PASS | [Report](tools-durability-2026-09-19/survival-checks.txt) |
| Renewable compatibility | 2 PASS | [Report](tools-durability-2026-09-19/renewable-compatibility-checks.txt) |

The shared build also passed 89 food-domain checks. The food agent's native HUD/restart evidence retains its original combined-build identity in [food verification](FOOD_BALANCE_RESULTS.md).

Domain checks exhaust all 25 tiered tool definitions to their exact break boundary, cover schema 1–18 stack layouts and reject malformed wear. Native checks exercise real mouse-driven mining, cancelled/invalid actions, selected-tool identity changes, the final-use drop transaction, Creative immunity, hoe use, hostile/passive hits, final-use natural-tree felling, item-pipe transfer, separate equal-wear drops, pickup/redrop, numeric UI and full-state schema-18 roundtrips. Fresh-process and migration checks compare the complete restored payload, including food. [Historical checkpoint identities](tools-durability-2026-09-19/legacy-checkpoint-identities.json) identify the actual older inputs.

## Measured material progression

[Actual timings](tools-durability-2026-09-19/mining-times.csv), in seconds from held mining input until successful block removal. Every completed cut spent exactly one durability use.

| Material | Pickaxe on stone | Axe on log | Lifetime |
|---|---:|---:|---:|
| Wood | 1.501 | 0.756 | 64 |
| Stone | 0.751 | 0.378 | 128 |
| Copper | 0.600 | 0.300 | 256 |
| Iron | 0.500 | 0.256 | 512 |
| Diamond | 0.378 | 0.189 | 1,536 |

These are focused timing observations, including frame scheduling, not statistical performance benchmarks. [Domain timing expectations](tools-durability-2026-09-19/tier-times.csv) retain the authored rates.

## Ordinary Survival route and capture provenance

Before focused acceptance, an ordinary empty-handed Survival route ran for 312 seconds with no supplied resources or Creative mode: gathered four logs, crafted and placed a workbench, crafted a wooden pickaxe, and ended at food 14 / health 20. [Observations](tools-durability-2026-09-19/survival-observations.txt), [Survival capture](tools-durability-2026-09-19/survival-five-minutes.png) and [workbench capture](tools-durability-2026-09-19/survival-workbench.png) retain that run's evidence and [assembly identity](tools-durability-2026-09-19/survival-assembly-sha256.txt).

The [original full report](tools-durability-2026-09-19/original-route-report.json) correctly remains **FAIL**: after the successful ordinary route and tier checks, its focused fixture attempted to place a nonplaceable ore. Subsequent changes corrected verifier-only fixture setup (a placeable protected-grade block, slope approach, the existing pause-before-save requirement and capture presentation). The final focused run passed all 243 checks; the five-minute route was not repeated on that later verifier assembly. Production tool and food behavior did not change between those builds.

The player guide's inventory and hotbar images were captured from the final accepted native run and visually reviewed. They show separate worn tools, changing condition bars, remaining-use text and the actual held pickaxe.

## Limits and publication

Fishing-rod and wrench capacities and shared wear behavior are domain checked; a complete live fishing catch and wrench adjustment were not separately rerun by this tool suite. The suite does not establish long-session balance, repair design or multiplayer behavior. No repairs or armor wear were added.

The illustrated guide, generated tool pages and coordinated food catalog are prepared for publication; deployment and live browser results will be recorded after the final combined export.
