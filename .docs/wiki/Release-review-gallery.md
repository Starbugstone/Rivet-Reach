# Factory stress-test gallery

Actual Rivet Reach screenshots captured on September 20, 2026, in the Windows release-review player at **1920 × 1080**. This is a deliberately constructed testing world, with supplied resources and controlled creature populations. The machines, pipes, storage, farms, weather and creatures use the game’s real systems.

## Production after sunset

![Glowing machines and connected production lines beneath the moon](images/release-review/blog-02-dusk-production.png)

Thirty-two production lines combine boilers and alternators with solar and wind power. Crushers and electric furnaces process iron into bulk storage; batteries absorb surplus electricity. The warm furnace glow and moonlight come from the game’s lighting.

## The whole installation

![A raised industrial platform beside planted fields and a persistent chicken flock](images/release-review/blog-01-factory-overview.png)

The fixture contains **1,518 industrial assemblies**, including pipes and cables, plus storage/stations, a **512-cell crop plot**, **64 persistent chickens** and **14 deliberately placed hostile mobs**. Separate checks exercise tanks, battery banks, bridges and a remote circuit maintained by an explicit Chunk Loader. Not every assembly is a processing machine, and not every object is visible in each camera view.

## Working through a storm

![An operating industrial yard under storm clouds and rain](images/release-review/blog-03-storm-industry.png)

The same factory is tested in clear weather and storms, with inventory use and journeys far enough to unload its unprotected chunks. Weather preserves its ordinary presentation and power-generation rules.

## Farming beside industry

![Crop fields and animals next to the factory’s power and processing equipment](images/release-review/blog-04-farm-and-power.png)

Crops and persistent animals share the test with automation, so growth and movement run while machines are busy. Unloaded factories keep their contents and partial work but produce nothing: [cover the required chunks](Bridges-and-chunk-loaders.md#keep-the-remote-workshop-running) if you want them operating while away.

These images show the test world’s appearance; they are not an FPS guarantee or a release certification. The repository’s [release review](https://github.com/Starbugstone/Rivet-Reach/blob/main/.docs/verification/RELEASE_REVIEW_RESULTS.md) records hardware, frame distributions, correctness checks and remaining limits. The PNG files are direct game captures, with no generated scenery or composited machinery.

[Building and inventory](Building-and-inventory.md) · [Renewable power](Renewable-power.md) · [Crates and warehouses](Crates-and-warehouses.md) · [Home](Home.md)
