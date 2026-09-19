# Fishing — issue #10

The user authorized the next issue #10 increment on September 19, 2026, then required a minimum water area/depth and screenshots of fishing and crafting. This slice implements the rod, fishing interaction, fish and cooker integration. Chickens, beds, crates, weather and renewables remain separate increments.

## Working rules

Craft one Fishing Rod at a workbench from three sticks and two String, using the familiar beginning layout (also mirrored):

```text
· · S
· S T
S · T
```

`S` is Stick; `T` is String from the existing flax chain. The reusable rod occupies one inventory slot and has no durability in this increment. No bait or additional resource is required.

Hold the rod and use the bound **Use** action (right-click by default) while aiming at water within eight blocks. A valid site needs **a 7×7 square of water sources, two source blocks deep**, centred on the targeted cell, with an empty layer immediately above the entire square. All 147 inspected cells must be resident. The 98 water cells must be sources: flowing water, waterfalls and lava do not qualify. A single bucket puddle cannot qualify. The user requested two source blocks deep and suggested a seven- or nine-block square; **7×7** is the selected working default to keep more rivers usable. Player-built ponds can qualify; natural-only provenance is not required. Underground ponds may qualify if they have the same open surface.

Casting starts a random **8–16 second** wait, then a **three-second bite window**. The float dips, a sound plays and a message prompts the player to Use again. A successful reel grants exactly one Raw Fish. Reeling early or missing the window gives nothing. Holding Use never automatically reels a bite. The rod remains in inventory. Catch quantity is fixed; biome/species tables, junk, physical fish mobs and depletion are not part of this increment.

Switching away from the casting slot, losing the rod, opening a menu, dying, entering body inspection, moving beyond a twelve-block tether, blocking the line or invalidating/unloading the water cancels the cast without a catch. Catch-time validation repeats before awarding. Full inventories receive an ordinary nearby dropped fish, preserving the caught item instead of silently deleting it. Normal pickup, drop hazards and saves apply.

All timings are working balance values, not measured long-session food balance. Creative uses the same wait and bite rules.

## Food and cooking

| Item | Food points | Acquisition |
|---|---:|---|
| Raw Fish | 2 | One successful reel |
| Cooked Fish | 6 | One `#raw_fish` in either Cooker |
| Fish Stew | 12 | One `#fish` plus two `#vegetable` in either Cooker |

Raw and cooked fish carry `fish`; only raw fish carries `raw_fish`. Both foods and the stew use the shared `edible` contract. Potatoes and carrots can fill the vegetable requirement, including mixed stacks. Cooking takes 200 ticks (ten seconds) at full heat/power and uses the existing fuel/electric cooker accounting, recipe browser, signals and pipe rules. Fish cannot be recooked repeatedly for a gain. Food does not spoil. No furnace recipe or compost contribution is added by this slice.

`Definitions/FishingCooking.json` adds the two recipes through the existing compiled cooking catalog. The original cooking JSON remains byte-for-byte intact for exact legacy fingerprints.

## Lifecycle and compatibility

`FishingCast` owns the bounded per-player wait/bite state. `PlayerFishing` owns targeting, resident water/tether validation, cancellation and the single inventory/drop award. `FishingPresentation` owns only the shared float and six-point line. There is no per-water-cell component or background fishing simulation. Water validation is bounded to 147 cells per active check and stops at the first failure. Absolute integer cells retain correctness across floating-origin shifts; presentation is rebuilt from the current origin.

The active cast is transient. Menus cancel it before a save; loading starts with no cast and grants no offline catch. Fish and rods persist through the ordinary stable item identities and item transactions. **Save schema remains 11**: no persistent section is added. The new content fingerprint adds fish items, rod construction and the additional cooking catalog, while accepting the exact pre-fishing definitions by projecting only these additions away. Existing compost state, crop deadlines, cooker work, unrelated content checks and failed-load rollback remain intact. Terrain generation and generated-chunk history are unchanged.

Original Blender sources, review render and reproducible exporter live in `ArtSource/Fishing` and `Tools/create_fishing_assets.py`. Rod, float, raw/cooked fish and stew have matching imported models and icons. The [player guide](wiki/Fishing.md) contains actual gameplay/crafting captures. [Verification](verification/FISHING_RESULTS.md) records the measured build and remaining review.
