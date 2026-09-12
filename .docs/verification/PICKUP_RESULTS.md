# Item pickup — 2026-09-12

[Gameplay rules](../GAMEPLAY.md#placement-and-inventory) define normal pickup at 2.04 m and action-created drops at 2.448 m (120% of normal). The bonus belongs to individual world-action piles. Partial collection preserves eligibility; different eligibility prevents merging.

Pickup now uses the shared movement-solid ray instead of the interaction-targeting ray. Passable placed blocks permit collection through them and from within their occupied cells. Open doors permit collection; solid terrain, solid machines and closed doors still block it. Interaction targeting remains unchanged.

## Measured checks

The open Unity **6000.4.4f1** Editor completed `creative-build` at **2026-09-12 13:13:27 UTC** ([completion](pickup-2026-09-12/build-result.txt)). The Windows x64 development player at **`Builds/Creative/RivetReach.exe`** [built successfully](pickup-2026-09-12/build-summary.txt) with **zero errors and zero warnings** in 55.81 seconds.

The `-rr-verify -rr-pickup-review` workload passed [45 runtime assertions](pickup-2026-09-12/runtime-report.json) and [exited successfully](pickup-2026-09-12/exit-code.txt):

- Normal radius accepts 2.03 m and rejects 2.05 m; action radius accepts 2.44 m and rejects 2.46 m.
- Real mining creates an eligible drop collected at 2.2 m while an ordinary pile of the same item remains.
- Partial pickup conserves quantities and the remaining pile's bonus; compatible action piles merge without giving ordinary piles their bonus.
- Pickup delays and solid terrain barriers still prevent collection.
- Fluid pipes, item pipes, power cables, signal conduits and signal wires remain interaction-targetable while allowing both normal and action pickup through them.
- Ordinary items sharing each of those five passable block cells are collected normally.
- A solid crusher and a closed wooden door block normal collection; opening that door immediately permits it.

These replace the earlier 2026-09-10 pickup artifacts, retained in Git history. No collection radius, save format or item transaction changed. The shared predicate also covers other passable blocks; torches, crops, saplings and workshop hatches were not separately exercised in this focused run. Axe-felled drops share the mining drop path but this workload does not replay tree felling. Gameplay feel and whole-game performance remain outside this check.

## Reproduce

With the pinned Editor in Edit mode, write `creative-build` to `Logs/build-request.txt` and wait for `Logs/build-result.txt`. Run the resulting player with `-rr-verify -rr-pickup-review -rr-output <report-directory>`. The runtime cases are in `ReviewPickupRanges` in `PlacementItemVerification.cs`.
