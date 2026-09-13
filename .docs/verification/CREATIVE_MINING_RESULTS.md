# Creative instant mining — 2026-09-13

Artifact: **`Builds/Creative/RivetReach.exe`**, Unity **6000.4.4f1**, Windows x64 Development, Direct3D 11. [Build summary](creative-mining-2026-09-13/build-summary.txt): **0 errors, 0 warnings**. [Artifact/source hashes](creative-mining-2026-09-13/identity.json).

- **111 runtime assertions passed**, no captured errors: [raw report](creative-mining-2026-09-13/runtime-report.json). One mining update removes a diamond block using fists, a held dirt item, wooden pickaxe, wooden axe or wrench. Only tools spawn its ordinary item drop.
- Fists suppress chest contents, pipe power/signal fittings, charged batteries, lava tanks and mature crop drops. Suppression resets after successful and stale removals. Subsequent tool mining recovers a lava tank with exactly its stored contents.
- Creative preserves protected bedrock and omits Survival tool-tier hints. Returning to Survival restores fist restrictions on stone, timed log mining, ordinary fist log drops, tier restrictions and timed suitable-tool mining.
- [47,717 Survival domain assertions](creative-mining-2026-09-13/survival-checks.txt) and [86 portable-storage assertions](creative-mining-2026-09-13/portable-storage-checks.txt) passed before building.

Actual player captures, visually reviewed: [fists without drops](../wiki/images/creative-fist-mining-2026-09-13.png), [tool with collectible drop](../wiki/images/creative-tool-mining-2026-09-13.png). Break particles still appear in both cases. The selected hotbar slot and item-pile diagnostic distinguish the actions.

Reproduce with `Tools/Verify-Creative.ps1 -Build -Mining`. The focused fixture positions the camera on an elevated platform and invokes the actual player targeting/mining update once with verification mining input. It checks authoritative cells and spawned items; this is not a manual playthrough or a performance benchmark. Other Creative controls retain their separately dated [2026-09-10 evidence](CREATIVE_RESULTS.md).

The older full Creative build route stopped at an unrelated pre-existing texture-layer assertion ([failure](creative-mining-2026-09-13/legacy-build-check-failure.txt)). The focused route runs the relevant Survival/storage checks and builds without changing that older assertion. An initial runtime fixture tried to place a nonplaceable ore; it was corrected to use a placeable diamond block with the same mining tier requirement before the passing run.

[Gameplay rules](../GAMEPLAY.md#creative-testing-mode) and [player guide](../wiki/Home.md) describe the Creative-only behavior. No save format or item definitions changed.
