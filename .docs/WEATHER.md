# Weather

Issue [#10](https://github.com/Starbugstone/Rivet-Reach/issues/10), weather increment, authorized 19 September 2026. [Player guide](wiki/Weather.md); [verification](verification/WEATHER_RESULTS.md).

## Working rules

One `WeatherState` owns clear, rain and storm conditions for the current expedition. New worlds start clear. The following tuning is a working implementation choice, not a validated balance target:

| State | Stable duration | Cloud / rain / wind factors |
| --- | --- | --- |
| Clear | 4–8 minutes | 0.08 / 0 / 0.18 |
| Rain | 3–5 minutes | 0.72 / 0.75 / 0.42 |
| Storm | 2–4 minutes | 1 / 1 / 1 |

Transitions take an additional 15 seconds with smoothstep interpolation. Clear proceeds to rain 75% of the time and storm 25%; rain proceeds to clear or storm equally; storm proceeds to rain 75% or clear 25%. Seeded xorshift selection and remaining fixed ticks are world authority. All factors stay between zero and one. These are an API boundary for later renewable generators, not implemented generation modifiers.

The existing active survival tick feeds weather at 20 Hz. Inventory time continues; title, menus that pause, death/residency gates and offline time freeze it. Beds advance celestial time only, preserving the weather schedule. No terrain generation changes are needed. Existing worlds acquire weather without altering generated chunks.

## Presentation and shelter

`DayNightCycle` applies cloud coverage, sky colour and daylight attenuation while preserving the clock's `IsNight`, moon phases, existing distance-fog ranges and gameplay lighting. Weather does not affect crop light or hostile spawn light. There is no fog weather, crop irrigation, water collection, wetness, temperature, season or lightning damage mechanic.

`WeatherPresentation` uses one dynamic local mesh of at most 1,024 rain streaks (2,048 triangles) in a circular footprint of about 9.5 blocks radius, independent of world size. Stratified radial/angle sampling keeps density consistent when looking diagonally; each streak resolves the actual world column for shelter clipping. It reuses its arrays and does not spawn particle GameObjects or invoke physics. Four batches of 484 cached height queries per second while stationary clip streaks above opaque roofs/ground. Resident fluid cells add a local water/lava surface clip. Missing or invalidated roof data suppresses rain until the lighting-owned cache publishes. Camera movement across a large distance refreshes immediately. Ordinary movement refreshes on the same bounded cadence. This avoids generating terrain for weather. Thin/nonopaque decorative materials are not guaranteed rain shelters.

Audio uses original deterministic rain and rolling-thunder synthesis, created once per session, two bounded listener-relative sources, and the existing canopy wind source. Roof shelter muffles rain/thunder; master volume and pause apply. Storm cloud flashes are restrained cosmetic sky illumination with delayed thunder. Their transient presentation phase is not persisted; they cannot damage or modify the world.

## Persistence and integration

[Schema 16](SAVES.md#weather--2026-09-19) appends the weather kind, previous kind, remaining/transition ticks, RNG and interpolation starting values after beds. Schema 15 and older start clear with a deterministic seed. Failed restoration restores the original weather authority alongside the original world. No content definitions or construction recipes change.

The model is independent of Unity and accepts explicit fixed ticks; clients could later consume authoritative snapshots. Actual multiplayer transport/replication remains unimplemented and must not be claimed as tested.
