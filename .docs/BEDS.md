# Beds, home spawn and sleep

The user authorized this issue #10 increment after persistent chickens. The implementation is playable; [verification](verification/BED_RESULTS.md) records measured checks, captures and remaining limits.

## Crafting and placement

One **Bed** uses three Cloth across a row above three Planks at a 3×3 Workbench. This adapts the required beginning-recipe layout to Rivet Reach's existing flax-derived cloth. No animal resource or advanced ore is needed.

A bed reserves two horizontal cells, one block wide, above two solid floors. Both cells must be empty, dry, loaded and clear of players/creatures. The pillow extends away from the player along the selected cardinal facing. Both cells remain occupied for collision and fluid routing; beds are not waterlogged. Cached selection boxes follow the frame, mattress and legs, allowing aim through the open space beneath the bed. Mining either half returns exactly one Bed and clears both cells. Removing either supporting floor also recovers one Bed. This includes beds across chunk boundaries.

The original timber-frame/teal-quilt/linen-pillow model uses the existing workshop palette. The current single style is a working art choice. No coloured-bed variants or sleep animation is implied.

## Home and safe arrival

Aim at either half and press Use/right-click or Interact. Daytime use sets home and explains that sleeping is available at night. A valid bed must have a clear, supported, dry standing space beside either half. Blocked beds explain the problem and do not replace a valid previous home.

Home is tied to a unique placed-bed identity, not just a coordinate. Breaking and replacing a bed at the same location does not silently restore its old home binding. Using another valid bed replaces the player's home.

On death, validate the saved bed, both supporting floors and a deterministic bounded ring of standing positions beside it. Reject obstructed headroom, liquids in or immediately around the arrival, and creature occupancy. If none is safe or the bed was removed, use the existing world-spawn fallback and notify the player. If that fallback is also unavailable, retain the existing death-state recovery message. Distant beds use authoritative saved terrain; loading the arrival chunks gates resumed play. Bed use does not permanently load chunks.

## Sleep and time

At night (18:00–06:00), valid bed use also advances the celestial clock to the next 06:00. Before midnight this reaches the following calendar day; after midnight it reaches the current day's morning. Holding Use does not trigger repeated sleeps. Sleep cancels an active fishing cast.

Successful sleep also performs one [weather recheck](WEATHER.md), which may retain the weather or begin a smooth change. Only celestial time advances; Crops, trees, animal growth/eggs/breeding, hunger, fluids, fuel, machinery and stored electricity receive no skipped simulation ticks. There is no instant production or offline catch-up. Existing mobs remain and normal dawn spawning rules apply. The current increment does not add a nearby-monster sleep restriction, automatic healing or hunger restoration.

The sleep policy defaults to 50% of active participants, rounded up, with a minimum of one. Percentage and minimum count are configuration fields saved with the world; the current game supplies its one local participant. Policy unit checks do not establish working multiplayer; online rosters, voting UI and multiplayer integration remain later work.

## Persistence and boundaries

Item 254 (`rivet:bed`) is the craftable foot; internal head cell 255 is never a separate item. Schema 14 follows issue #12's schema 13, adding placed-bed identities/orientations, the player's home binding and the sleep policy. Previous saves start with no beds/home; their existing content fingerprints and checks must remain intact. Failed restore must roll back bed authority together with the old world/player.

No terrain-generation changes are needed. [Crates](CRATES.md) and [weather](WEATHER.md) are implemented subsequent increments; renewable power remains separate. See the [illustrated player guide](wiki/Beds-and-home-spawn.md) and [measured evidence](verification/BED_RESULTS.md).
