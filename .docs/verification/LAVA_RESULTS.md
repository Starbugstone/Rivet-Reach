# Lava verification — 2026-09-13

The user requested slower nonrenewing lava, near-bedrock lakes, bucket transfers, dropped-item destruction and rapid player burning damage. [Fluid rules](../FLUIDS.md#lava--2026-09-13), [generation](../TERRAIN_GENERATION.md#deep-lava-lakes--2026-09-13), [save compatibility](../SAVES.md#lava-and-generator-compatibility--2026-09-13) and [player guide](../wiki/Lava.md) own behavior.

## Artifact and replay

Unity **6000.4.4f1 / URP**, Windows x64 **Direct3D 11** Development player at `Builds/Lava/RivetReach.exe`. The source, current bucket icon and shared original bucket model are in the repository. Run `Tools/Verify-Lava.ps1 -Build` with the pinned editor open; it waits for other editor build requests and uses its own lava request/result files. Ordinary sessions receive no lava fixture or starter items.

The first native run (`Logs/Lava-20260913-A`, 04:18 UTC) passed **252 assertions**, including repeated deck-fixture setup checks. The final Direct3D 11 review (`Logs/Lava-20260913-D3D11`) passed **28 assertions**, with one consolidated deck check and a clearer natural-lake camera. [Native report](lava-2026-09-13/runtime-report.json) and [build summary](lava-2026-09-13/lava-build-summary.txt) retain the artifact identity: build succeeded with **zero errors and zero warnings**. Performance and visual polish are not certified by the assertion count.

## Measured checks

- The [25 focused editor checks](lava-2026-09-13/lava-checks.txt) passed lava’s twenty-tick propagation, three-cell reach, settled sleep, no source renewal, receding flow, source-only full-slot bucket transfers and preservation of different source fluids at contact.
- Heat checks cover immediate four-point damage, ten-tick repeats, lingering fire, water extinguishing, respawn and frame-batching invariance.
- Three seeds (246813, 17, −99) yielded deep lakes. Full 34³ chunk/halo comparisons agreed with point reads for both generator versions. Only previously carved deep cave air became lava: the sampled pages contained 2,061, 4,386 and 409 lava cells respectively. All other terrain, ores, the bedrock buffer and dry spawn were preserved in those samples.
- Native review exercised generated lava, the aimed expedition bucket command in both directions, three-cell flow, complete sleeping-stack destruction before pickup, dry items above the lowered surface, shallow foot-edge contact, real player heat/Creative/water behavior, and saving/loading lava cells, a filled bucket, hearts and burn duration.
- The existing **water runtime review passed 36 assertions** against the first lava player (`Logs/Lava-Regression-A/fluid`), covering generated sea/river water, swimming, item buoyancy/current, seven-cell flow, input, renewal, source removal and residency/origin shifts.
- Actual **schema-2 and schema-3 checkpoints passed 21 migration assertions** against that player (`Logs/Lava-Regression-A/connections-legacy`), including rejection of unrelated changed item definitions and exact serialized-state round trips. Independent envelope reads confirmed their migrated schema-7 checkpoints still name **`terrain-6-azure`**.

The final Direct3D 11 player also passed the full [183-assertion save/load/recovery suite](lava-2026-09-13/save/runtime-report.json) and [five-assertion fresh-process continuation](lava-2026-09-13/save-resume/runtime-report.json), both with exit code 0. These cover resource conservation, atomic/backup failure cases, failed-load rollback, UI saving and process restart.

## Remaining limits

Working flow, damage and four-second burn defaults still need user play/balance review. Cave accessibility and lake distribution are not guaranteed across every seed. The focused tests do not establish long-session fluid performance, exhaustive migration coverage or all possible terrain/fluid contacts. Lava emission makes its own surface visible; it does not add propagated cave lighting. Water/lava reaction products and broader fire spread are not implemented.

## Current visuals

![Generated deep lake](../wiki/images/lava-natural-lake.png)

![Source and three-cell flowing lava](../wiki/images/lava-flow.png)

![Player burning and losing hearts](../wiki/images/lava-burning.png)

![Held lava bucket](../wiki/images/lava-bucket-held.png)

![Current inventory bucket artwork](../wiki/images/lava-bucket-inventory.png)

These are actual Windows-player captures from the final September 13 review. The natural lake is generated; the surface deck is an explicit verification fixture.

## Renderer limitation

Two Direct3D 12 continuation attempts completed their five assertions but exited with `-1073741819` during native shutdown. They are not counted as clean passes. This reproduces the previously documented [D3D12 shutdown limitation](SAVE_RESULTS.md#reproduce-and-limits). The final lava player selects Direct3D 11, matching the alpha and connection review builds; the build restores the user’s editor graphics settings afterward. [Artifact hashes](lava-2026-09-13/artifact-hashes.json) identify the delivered executable and runtime assembly.

## Wiki publication

[Wiki deployment run 34738200603](https://github.com/Starbugstone/Rivet-Reach/actions/runs/34738200603) successfully published feature commit `a2af33b`. The local publisher validated **148 pages and 6,103 links/images**, with **134 item pages and 122 crafting/processing recipes** in the combined catalog.

Live Chromium review opened [Lava](https://github.com/Starbugstone/Rivet-Reach/wiki/Lava), loaded all four game screenshots, followed its Lava bucket link to the correctly illustrated item page, and followed the empty Bucket link to its updated recipe/use page. The guide had no document overflow at 390×844 or 1280×900; its images and the item icon loaded successfully. Retained [mobile guide](lava-2026-09-13/wiki-lava-mobile.png), [desktop guide](lava-2026-09-13/wiki-lava-desktop.png) and [bucket page](lava-2026-09-13/wiki-lava-bucket.png) show the live rendering.
