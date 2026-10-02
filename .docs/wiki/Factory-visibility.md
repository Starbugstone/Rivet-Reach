# Seeing your factory from a distance

Placed machines, pipe and cable runs, batteries, connected tank shells, bulk crates and beds now stay visible out to the terrain fog distance while their chunks are loaded. At the default view distance this is 304 blocks, replacing the former 64-block machine cutoff.

## The old boundary and the new view

The same camera position shows the difference. With the previous cutoff restored for comparison, the far production rows and tank vanish:

![Previous 64-block cutoff leaves the back of the platform empty](images/factory-visibility-2026-10-02/previous-cutoff.png)

With the new distance rendering, those machines remain visible:

![Extended visibility shows the complete factory and distant tank](images/factory-visibility-2026-10-02/extended-distance.png)

*Matched camera positions in the same test build; production and daylight continue between captures.*

## Nearby detail and distant views

![Working factory viewed nearby](images/factory-visibility-2026-10-02/visibility-close.png)

Nearby machines show their normal detailed models, moving parts and controls. Between 48 and 64 blocks, they transition to simpler versions of the same models. Farther away you can still see the layout and connected runs; move closer to read labels and inspect moving parts.

![Factory still visible beyond the previous cutoff](images/factory-visibility-2026-10-02/visibility-midrange.png)

![Long view of the factory against terrain fog](images/factory-visibility-2026-10-02/visibility-longrange.png)

Distance includes height, so this also applies when looking down from a tower or flying in Creative mode.

## Tanks and loaded factories

![Elevated view of the connected tank and its water](images/factory-visibility-2026-10-02/visibility-elevated-tank.png)

![Half-full demonstration tank seen through its glass wall](images/factory-visibility-2026-10-02/visibility-tank-detail.png)

A formed tank’s liquid remains visible with its walls even when the controller is farther away. Stored amounts and machine operation follow the usual rules.

Visibility does not keep chunks loaded. Use [chunk loaders](Bridges-and-chunk-loaders.md) when a factory needs persistent residency, and follow [Tanks](Tanks.md), [Pipes](Pipes.md) and [Electricity and batteries](Electricity-and-batteries.md) for setup.

[Watch the 24-second in-game camera tour](https://media.githubusercontent.com/media/Starbugstone/Rivet-Reach/main/.docs/verification/factory-visibility-2026-10-02/factory-visibility.mp4).

*Captured from the native Windows player on October 2, 2026. This is a staged working stress-test factory; the separate visual run fills the demonstration tank halfway so its level is easy to see. The video contains actual Unity frames recorded at a fixed 30 Hz; it is a visual demonstration, not a real-time performance measurement.*
