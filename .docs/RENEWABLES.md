# Renewable electricity

Solar Panels and Wind Turbines are the renewable generators authorized for the next Issue #10 increment. This document owns their working generation rules. [WEATHER.md](WEATHER.md) owns weather authority; [BATTERIES.md](BATTERIES.md) owns storage and allocation; [SAVES.md](SAVES.md) owns compatibility.

## Working generators

Both assemblies occupy one cell, connect to the existing electrical cable system, and have a **400 W** peak output. Their values come from `Definitions/Renewables` and are read through the data-driven renewable configuration; the values are working balance defaults, not long-session measurements.

| Generator | Clear | Rain | Storm | Time rule |
| --- | ---: | ---: | ---: | --- |
| Solar Panel | 400 W at profile peak | 200 W at profile peak | 60 W at profile peak | A sine curve runs from zero at 06:00 to its noon peak, then returns to zero at 18:00; output is zero at night. |
| Wind Turbine | 40–140 W | 240–360 W | 400 W | Runs day and night; low baseline plus gradual random gusts. |

A deterministic seeded hash chooses successive gust targets, blended with smoothstep over 240 active survival ticks (12 seconds). Sampling the existing saved seed and survival tick reproduces the exact gust without a new save field, offline credit or sleep-time jumps. Weather-specific minimum/maximum output factors are authored in the JSON configuration; storm minimum and maximum both equal 100%.

The turbine rotor advances only while `DeliveredWatts > 0`, tracking generation actually consumed by machines or battery charging. Full storage with no machine demand leaves it stationary; available output remains visible separately. When turning, rotor speed follows wind strength.

The shared weather state supplies its smoothly interpolated profile, so a transition changes output continuously rather than stepping from one weather value to another. Renewables create no weather, do not advance its timer, and have no private randomness or clock.

## Placement and exposure

Each generator needs open sky: the cell immediately above it must be air, and the lighting-owned cached highest opaque column must be below that air cell. An opaque roof blocks generation. The test is cache-only: it does not scan columns, perform physics queries, or generate chunks. Missing or stale cached coverage prevents output until the shared cache becomes valid.

## Crafting and power integration

Solar Panel and Wind Turbine are 4×4 Machinist’s Bench assemblies. Their gold and diamond components are defined by `RenewableBuild.cs`, keeping renewable power on the advanced material tier. The compiled recipe registry remains the crafting authority.

Renewables are ordinary generators. Existing connected-cable allocation combines generator supply on a physical grid, serves machines before charging, splits eligible surplus equally among batteries and redistributes shares blocked by full or mode-restricted storage. Generation is the only source that charges storage. Batteries discharge only to machine demand; their discharge never becomes battery-charging supply. A battery may still receive generation on one cable grid and supply a machine on another during the same fixed step, using the established shared budget and without creating energy or making a hidden cable connection.

The 400 W peaks are deliberately below the existing 800 W boiler/alternator output. This is a working progression and balance comparison, not evidence about sustained factory balance or performance.

## Persistence and limits

Renewables add registered items and recipes but no saved per-machine state. Schema **16** remains the current format. Its content-fingerprint compatibility path accepts the exact pre-renewable catalog by excluding only the two renewable items, their two recipes and the renewable configuration text; unrelated definition changes still reject. Existing saves contain no renewable terrain or machine records and load unchanged.

Generation pauses with ordinary industrial residency/topology gates. Offline production, solar tracking, weather manipulation, maintenance, wear and power storage beyond the existing batteries are outside this increment. The [verification report](verification/RENEWABLE_RESULTS.md) separates measured checks from balance and artistic review.
