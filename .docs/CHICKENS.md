# Chickens

Working implementation of [issue #10, phase 5](https://github.com/Starbugstone/Rivet-Reach/issues/10). This is the first persistent passive species. [Farming](FARMING.md) owns crops and shared ingredient tags; [Mobs](MOBS.md) owns shared navigation, spawn-site and combat boundaries. [Verification](verification/CHICKEN_RESULTS.md) records measured results and limits.

## Lifecycle and player loop

Wild adults appear during daytime on resident surface grass with light levels **9–15**, using the shared habitat/support/light profile. Search attempts are bounded to eight columns every ten seconds, 20–44 blocks from the player; visible unobstructed sites are excluded. A successful site may produce a nearby second adult. The working local natural population limit is 12 within 48 blocks. Existing chickens survive darkness and never use ambient hostile distance despawning.

Hold any item carrying `seed` or `grain` to attract nearby chickens within ten blocks. Right-click/Use or Interact on an adult consumes one seed or grain and makes it ready for a partner for 30 active seconds. Repeated use on an already ready adult, chick or resting adult consumes nothing. Two ready adults within 2.5 blocks, with clear sight and a supported free chick site, produce one chick. Only successful birth consumes both readiness states and starts each parent's five-minute cooldown. Holding Use cannot automatically feed repeatedly. Feeding still consumes the item in Creative testing mode.

Chicks grow in ten active minutes. Growth waits if the adult body would intersect terrain, another creature or the player. Adult chickens lay one ordinary collectible Egg after each saved, randomized five-to-ten-minute interval. Chicks do not lay eggs. Eggs are ingredients, with no throw/hatching operation in this increment.

These times, distances and caps are **working balance defaults**, not numbers explicitly selected by the user or validated as final gameplay balance. Adults have 6 health; chicks have 3. Chickens flee when hurt. Defeated adults drop exactly one Raw Chicken and one or two Feathers; chicks yield no loot. Items use ordinary pickup, inventories, drops and pipes. Feathers are a future crafting material with no added recipe in this increment.

Use ordinary blocks and doors to enclose a pen. Navigation shares the voxel walker, supports one-block steps, avoids planned fluid/pit routes and respects body collision. Animal bodies prevent player block placement and tree growth through occupied cells. No new fence, special animal container or egg-collecting machine is implied.

## Cooking

Both the fuel Cooker and Electric Cooker expose the same tagged recipes. Each operation takes 200 powered/working ticks (ten seconds at full rate), using existing fuel/energy and item conservation.

| Result | Ingredients | Food points |
| --- | --- | ---: |
| Raw Chicken | Adult defeat | 2 |
| Cooked Chicken | 1 `raw_meat` | 6 |
| Cooked Egg | 1 `raw_egg` | 4 |
| Chicken Stew | 1 `meat` + 2 `vegetable` | 12 |

Raw or cooked chicken satisfies `meat`; mixed vegetable kinds may satisfy the two-vegetable requirement. A raw Egg has no direct food points. Existing fish, crops, cooking catalogs and compost values remain unchanged.

## Persistent passive architecture

`PassiveSystem` owns persistent records separately from `MobSystem`. It shares `MobSpawnRules`, `MobNavigation`, registered weapon damage, voxel collision and the common nearest-target interface. Adult/chick views and four authored animations are derived presentation and cannot produce items or offspring.

Records are indexed by horizontal chunk column. At most 128 nearby resident animals simulate within 68 blocks, at 20 Hz; each tick permits at most two bounded path searches. The persistent safety cap is 4,096 records. Distant or unloaded animals retain their records and sleep; growth, egg, breeding and cooldown timers freeze. There is no offline production or distant chunk-loader animal production. These are scheduling limits, not a measured performance guarantee.

[Schema 12](SAVES.md#persistent-chickens--2026-09-19) adds stable identities, position/home, health, growth/egg/cooldown/readiness timers and random state. Reload reconstructs views and paths without respawning or advancing the population. Older saves contain no passive section and start with an empty persistent population; ordinary later spawning can populate suitable terrain without altering generation. No terrain is regenerated or retrofitted.

Original Blender source, exports and authoring script live in `ArtSource/Chickens`, `Assets/RivetReach/Resources/Chickens` and `Tools/create_chicken_assets.py`. Adult/chick each use one material and an eight-bone rig, with Idle, Walk, Peck and Death actions. Actual imports, screenshots and remaining artistic review belong in the verification record.
