# Item pickup ranges — 2026-09-10

[Gameplay rules](../GAMEPLAY.md#placement-and-inventory) define normal pickup at 2.04 m and action-created drops at 2.448 m (120% of normal). The bonus belongs to individual world-action piles, including mining and axe-felling outputs. It does not affect existing ordinary piles of the same item type. Different eligibility prevents merging; partial collection preserves the remaining pile’s eligibility. Inventory items do not retain the bonus when thrown again.

Verification uses Unity 6000.4.4f1 and the `-rr-verify -rr-pickup-review` runtime workload in `Builds/Creative/RivetReach.exe`.

The [Windows build](pickup-2026-09-10-build.txt) succeeded with **0 errors and 0 warnings**. The [runtime report](pickup-2026-09-10-runtime-report.json) passed **15 assertions**, including startup/residency and these pickup checks:

- Normal radius accepts 2.03 m and rejects 2.05 m; action radius accepts 2.44 m and rejects 2.46 m.
- Real mining creates an eligible drop collected at 2.2 m while an ordinary pile of the same item remains.
- Partial pickup conserves inventory and world quantities and retains the remainder’s bonus.
- Action piles merge with compatible action piles; ordinary piles remain separate.
- Delays and solid barriers still prevent collection.

Axe-felled logs use the same `SpawnMinedDrop` path; this focused run exercises block mining rather than replaying tree felling. The bonus’s reset on a manual throw follows the existing `Expedition.Drop` path, which creates an ordinary pile; it is not separately exercised here. Gameplay feel and whole-game performance remain unmeasured by this workload.
