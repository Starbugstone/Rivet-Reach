# Dropped-stack readability — 2026-09-11

The [gameplay rule](../GAMEPLAY.md#fist-mining-and-collection) uses one existing item model for a single item, two overlapping copies for two, and three copies for larger piles. The display refreshes when its quantity category changes and when a culled or loaded pile recreates its view. Physics and saved quantities are unchanged; there is no new art or dependency.

## Build and checks

- Unity **6000.4.4f1**, Windows x64 development player at `Builds/Creative/RivetReach.exe`. [Build summary](drop-stacks-2026-09-11/build-summary.txt): succeeded with **0 errors and 0 warnings**.
- Existing pickup regression: **15 assertions passed**, covering partial collection, merging, item isolation, collection radii, delay and solid barriers. [Measured report](drop-stacks-2026-09-11/pickup-report.json).
- Existing placement/item/movement regression: **96 assertions passed**, including 200 drops merging into four capacity-limited piles, quantity conservation, delayed merging, blocked merging and displacement when placing blocks. [Measured report](drop-stacks-2026-09-11/placement-report.json).
- Inspected actual player captures before and after block placement: the dirt and grass piles have overlapping stepped models, and displaced piles remain visible above the placed block. [In-game stacked drops](drop-stacks-2026-09-11/stacked-drops.png).

This local build includes the workspace's concurrent crafting/UI changes. These checks are focused evidence, not a full-game regression or performance benchmark. Three copies indicate multiple items, not the exact quantity. The captured fixture shows piles of three or more; two-item appearance and the live visual transition after partial collection were reviewed in code, not independently captured. Save-format compatibility is unchanged by inspection; a new save/reload playtest was not run for this presentation change.
