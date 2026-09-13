# Ranged liquid pump

The user requested a Floater Rock upgrade for collecting finite liquids on 2026-09-13 and explicitly confirmed **eight blocks in each direction**. This means a world-aligned **17×17×17 cube**, including the pump cell, with inclusive offsets −8 through +8 on X, Y and Z. It is an axis-based reach, not a spherical distance test. Turning the pump changes its ports but not this area.

## Working rules

One Pump plus one Floater Rock and **two gold ingots** crafts **one Ranged Liquid Pump**, shapeless at the **4×4 Machinist's Bench**. The two-gold cost is the working default for the user’s material-tiering request; [ECONOMY.md](ECONOMY.md#material-tiered-construction--2026-09-13) owns the progression rationale. Operating speed remains the existing working default. Existing basic recipes are unchanged. Item `rivet:ranged_liquid_pump` uses runtime ID 180 and the normal placement, mining, held/drop, item browser and Creative catalog paths.

The pump collects any registered liquid, including water and lava, but only **source blocks**. Forty eligible 20 Hz ticks (two seconds) remove one actual source and deposit exactly **10 L** into its 10 L buffer. Flowing and falling liquid cannot be collected. Lava is finite and does not regenerate; the normal water renewal rule remains active. The ordinary Pump retains its water-only intake directly below.

No electricity or fuel is required. Optional Blue Signal at the front pauses extraction when OFF. Full buffers, incompatible retained liquid, unloaded source cells and dormant pumps pause work. A removed/replaced target resets its work; no failed removal can create liquid. A filled buffer accepts only its own liquid; completely emptying it allows another liquid. Competing pumps revalidate their targets before committing and cannot collect the same source twice.

The collector reaches sources through intervening blocks; no hose path or line of sight is required. Only resident cells are considered, and the pump itself does not load chunks. Searches inspect at most **256 cells per fixed tick**, top layer first, then Z and X. A full 4,913-cell search takes up to 20 ticks; an exhausted search waits 40 ticks before retrying. These search delays are additional to the two-second collection time. Selected targets are cached and rechecked; search cursors are transient. Disabled/full pumps do not scan. This is a bounded workload design, not a factory-scale performance claim.

Fluid pipes connect on all six faces, using the existing selected-Wrench Input / Output / No connection rules. Ends facing the pump default to Output; the receiving tank end should be Input. Shared network allocation, typed storage, tank gates and liquid conservation remain authoritative. The machine panel reports searching/no compatible source, running, signal/residency pauses, full output and the actual liquid name/quantity. Buckets can add or take 10 L through the normal compatible-liquid controls. Drain the buffer before mining if you want to keep its contents; this pump is not a portable tank.

## Persistence and assets

The pump adds no serialized fields or schema revision. Existing machine fields retain its orientation, signal state, liquid type/amount and partial collection work; source removals use ordinary persisted world edits. Loading rebuilds the search cache without crediting offline time. An additive fingerprint omits only the new item and recipe for older checkpoints; pre-existing definitions remain checked.

[The Blender script](../Tools/create_ranged_pump.py) authors a copper/iron collector cradle with a suspended faceted Floater Rock, field hoops, outlet and status lamp. Editable source is `ArtSource/RangedPump/RangedPump.blend`; the explicit FBX and icon 180 use the existing Workshop atlas. The core moves only with active collection, and the lamp follows machine status. Existing avatars and material assets are preserved.

[Verification](verification/RANGED_PUMP_RESULTS.md) records measured evidence and remaining review. [The player guide](wiki/Ranged-Liquid-Pump.md) teaches the recipe and setup.

[Material-tier recipe verification](verification/RECIPE_TIER_RESULTS.md) records the later gold/diamond cost update, compatibility checks and current recipe captures. Earlier feature evidence above retains its original build identity.
