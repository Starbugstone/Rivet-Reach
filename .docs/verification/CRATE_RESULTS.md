# Crate and item-routing verification — 2026-09-19

[Crates and controllers](../CRATES.md) and [numeric item routing](../wiki/Item-pipe-routing.md) were checked in Unity **6000.4.4f1** and `Builds/Crates/RivetReach.exe`. The Windows development build succeeds with **zero errors and four existing obsolete-API warnings** in `AlphaPlaytestVerification.cs`. This is a focused development build, not a published game release.

## Measured behavior

- **49 crate Editor checks** cover exact capacity, ordinary stack limits, type locks, portable-content rejection, 64/65-crate boundaries, residency, multiple controllers, mining-independent stock, machine/controller/crate/chest fallback, numeric overrides, round-robin, sensor counts, congestion, recipes and explicit previous-content compatibility.
- **6 routing edge checks** cover controller/direct aliases across separate pipe networks, self-delivery rejection, equal priorities across receiver types, multiple-face deduplication, and priority changes without topology/cache reconstruction.
- Existing **615 grid-allocation** and **413 industry** assertions pass.
- **137 native assertions**, including fixture setup, exercise real recipe browser/crafting, placement, inventory Shift transfers, lock/unlock buttons, typed priority controls, rendered front labels, connected controller access, safe mining, actual pipe delivery and save/load. A chest at 90 overrides a machine at 50; a furnace and chest at 50 receive three items each over six transfers; a cobblestone crate at 100 rejects iron. Item and electrical priorities remain independent.
- **7 fresh-process assertions** retain exact physical crate counts, empty/occupied locks, controller access and custom priorities. **183 full-save/recovery checks**, **5 general fresh-process Continue checks**, and **17 actual schema-14 historical-load checks** pass using isolated copies. Late-load failure restores the original world and crate authority.

Reports: [crate checks](crates-2026-09-19/editor-checks.txt), [routing edges](crates-2026-09-19/routing-edge-checks.txt), [native review](crates-2026-09-19/runtime-report.json), [crate restart](crates-2026-09-19/restart-report.json), [full saves](crates-2026-09-19/save-report.json), [save restart](crates-2026-09-19/save-restart-report.json), [schema 14](crates-2026-09-19/legacy-schema14-report.json), [grid allocation](crates-2026-09-19/grid-allocation-checks.txt), [industry](crates-2026-09-19/industry-checks.txt), [build](crates-2026-09-19/build-summary.txt) and [warnings](crates-2026-09-19/build-messages.txt).

## Routing cost

Topology changes rebuild cached endpoint identities/faces. Transfer steps reuse those lists, snapshot eligible source slots, group receivers by numeric priority and retain a round-robin cursor per graph/priority/item. Editing a priority rebuilds only that graph's compact priority groups. Rejected item/receiver pairs are remembered for that transfer step and retried on the next one; source-alias exclusions never mark a receiver incompatible. Stock, recipe, face and residency checks remain authoritative.

The following **Editor simulation microbenchmarks** ran on the local Intel Core i7-10750H. Each used 200 warmed active transfer phases, measuring only `TransferConfiguredItems`, including endpoint-priority scans and transfers. Timing excludes topology construction and the rest of the simulation/rendering. [Raw benchmark](crates-2026-09-19/routing-benchmark.txt):

| Fixture | Median routing | p95 routing | Managed allocation | Receiver probes |
|---|---:|---:|---:|---:|
| 16 pipes, one source / one receiver | 0.0016 ms | 0.0028 ms | 0 bytes | 200 |
| 1,024 pipes, one source / one receiver | 0.0026 ms | 0.0028 ms | 0 bytes | 200 |
| 128 pipes, 64 sources / 64 full receivers | 0.1736 ms | 0.2200 ms | 0 bytes | 12,800 |

Both pipe-length fixtures rebuilt their route cache **zero times** during measurement and probed one receiver per delivery. Congestion probes each full receiver once per phase, preserves every item, and recovers on the next phase after capacity is freed. These are bounded fixtures, not a whole-game FPS result or proof for arbitrary factory sizes. Topology edits, mixed recipes, many distinct item types, rendering thousands of crates and sustained factory play remain broader profiling work.

## Art and captures

`Tools/create_crate_assets.py` reproduces original Blender models in `ArtSource/Crates`. Unity imports fit one cell: [Bulk Crate](crates-2026-09-19/bulk_crate-imports.txt) **1,900 triangles**, [controller](crates-2026-09-19/crate_controller-imports.txt) **2,380 triangles**, each one renderer/material before the runtime front-label canvas. Blender front/back renders and native placement were visually compared.

The [warehouse guide](../wiki/Crates-and-warehouses.md) and [routing guide](../wiki/Item-pipe-routing.md) show actual crafting, placement, contents, interfaces, controls and configured pipe connections. Captures use a constructed Survival platform with scripted resources; they do not establish start-to-warehouse balance. [Capture hashes](crates-2026-09-19/capture-hashes.json) identify the images. Capacity 16,384 and maximum 64 crates are working defaults, not validated factory-balance claims.

A temporary clean-atlas build/export interval used the committed terrain atlas; the pre-existing working atlas was restored byte-for-byte and excluded from this task’s commit ([hashes](crates-2026-09-19/atlas-preservation.json)). No terrain generation, player art or unrelated work is included. Zero/default timing fields in shared native reports were not measured by those workloads.

## Wiki publication

The refreshed reference contains **193 items and 197 crafting/processing recipes**, and passes **224 pages / 9,642 local links and images** plus [661 staged source fingerprints](crates-2026-09-19/staged-export-check.txt). It includes the new crate/controller pages, current potato growth icons and the dedicated routing guide. Implementation commit `3a943e9` was pushed to `main`. [Wiki deployment 35445699744](https://github.com/Starbugstone/Rivet-Reach/actions/runs/35445699744) succeeded and published wiki revision `bfe99ad99a5143428d0ca9b59377c6085c33336a` ([deployment record](crates-2026-09-19/wiki-deployment.json)).

All **224 remote Markdown pages** match the maintained publisher output; **21 current images/icons** match byte-for-byte ([asset hashes](crates-2026-09-19/published-assets.json)). Live Chromium loaded all **193 index icons** and every image on seven reviewed pages: Items, the warehouse guide, routing guide, both new item pages, Machine interfaces and Farming/cooking. All seven fit a **390×844** viewport without document overflow. The controller recipe's Gold Ingot ingredient link was followed successfully after waiting for GitHub navigation to complete ([browser readback](crates-2026-09-19/live-wiki-checks.json), [routing desktop](crates-2026-09-19/wiki-routing-desktop.png), [controller mobile](crates-2026-09-19/wiki-controller-mobile.png)).

[Issue #10](https://github.com/Starbugstone/Rivet-Reach/issues/10) was updated and read back: **12 crate/controller phase and acceptance entries** are newly checked. Its status now documents numeric priorities and measured routing limits. Weather, renewables and sustained hunger/balance acceptance remain open ([checklist readback](crates-2026-09-19/issue10-check.json)).
