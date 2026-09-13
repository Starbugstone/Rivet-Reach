# Farming, forage and food cookers

Issue [#10](https://github.com/Starbugstone/Rivet-Reach/issues/10), first playable increment, authorized 2026-09-13. This working specification implements crops/forage, flax fibre/string/cloth, food-only cooker recipes and shared item tags. Beds, compost, fishing, chickens, crates, weather and renewables remain later increments. The user confirmed distinct hostile/passive mob systems for future livestock; this increment does not add animals.

## Plants

Potatoes, wheat, flax, carrots and berries share four visible stages. Mushrooms are gatherable mature clusters, with no cultivation in this increment. Cultivable wild plants generate at varying stages and grow through the same scheduled lifecycle as farm plants. Wild plants support grass, dirt or farmland; planting stock placed by the player requires farmland prepared with a hoe. Growth requires resident terrain and sky light of at least 9. No moisture, irrigation, rain hydration or offline growth is required or credited.

`Resources/Definitions/Crops.json` authors growth intervals and harvest quantities. Working defaults are one stage per 1,200 fixed ticks (60 seconds), three minutes from planting to maturity, two or three harvested resources and two planting seeds. Potatoes return two to four potatoes, which also serve as planting stock. Breaking an immature plant returns at most one non-produce planting item and no crop resource. Wheat, flax, carrots and berries return one seed; immature potatoes return nothing because the planting item is itself the edible produce. Mature potatoes still provide renewable planting stock. Mushrooms return one or two mushrooms. Block mining and supporting-ground removal share the same harvest transaction. Ordinary drops, pickup, stacking and saves remain authoritative.

The generator chooses a candidate in each six-by-six surface cell with a 38% chance before grass, clearance and spawn rejection. Species and initial stage are seeded; point reads and worker-generated chunks use the same rules. These density/yield/time defaults require playtesting, not an assertion that every seed provides every plant nearby.

On first chunk readiness, immature natural plants enter the world survival scheduler. Saved deadlines are not restarted on residency. Growth processing keeps the existing sixteen-mutation budget and pauses when terrain is absent or light is insufficient. Mature plants need no growth ticks. Plant geometry is original Blender-authored mesh data combined into terrain chunks; it does not create one GameObject per plant.

## Shared item tags and fuel

`ItemDefinition.tags` holds reusable string tags. This increment authors `edible`, `burnable`, `boiler_fuel`, `seed`, `vegetable`, `grain`, `fruit`, `mushroom`, `fibre`, `cordage`, `fabric` `prepared_food`, `log`, `planks`, `raw_ore` and `ingot`. All consumable foods carry `edible`; eating requires that tag and a positive configured `foodPoints` value. `ItemRegistry.FoodPoints` is the shared gameplay query. Tags identify capabilities/categories; they never grant production quantities or energy by themselves. Existing crafting recipes remain exact-item recipes. Food selectors use either a stable item identity or a `#tag` in `Resources/Definitions/Cooking.json`.

The shared processing fuel catalog supplies explicit burn durations. Each registered fuel must also have `burnable`. Furnaces and basic cookers accept those tagged/configured fuels, including coal, charcoal and wood. Boilers keep their coal/charcoal subset through `boiler_fuel`, preserving their existing fuel/output balance. Rear-only fuel piping remains mandatory; other faces supply usable ingredients. Electric appliances have no item-fuel slot and accept ingredients on every face. Adding a tag alone cannot make an unconfigured item yield energy.

## Crafting and food

Three flax fibres make one String; four Strings make one Cloth in personal crafting. These are configurable recipe assets. String/cloth are usable crafting materials reserved for the subsequently planned rod and bed; this increment does not advertise those as available.

One furnace and two copper ingots make a basic Cooker at a workbench. One Cooker, one machine casing and four copper wire make an Electric Cooker at the Machinist's Bench. Existing beginning recipes retain their layouts/quantities.

Both cookers share six recipes, each taking 200 ticks (10 seconds) at full heat/power:

| Ingredients | Result | Food points |
|---|---|---:|
| 3 Grain | 1 Bread | 7 |
| 1 Potato | 1 Baked Potato | 5 |
| 1 Carrot | 1 Roasted Carrot | 5 |
| 1 Mushroom | 1 Cooked Mushrooms | 4 |
| 2 `#vegetable` + 1 `#mushroom` | 1 Vegetable Stew | 12 |
| 1 `#grain` + 1 `#fruit` | 1 Fruit Porridge | 9 |

Potatoes and carrots are vegetables; apples and berries are fruit. Mixed tag members and split stacks count correctly. Wild carrots restore two food points, berries/mushrooms one. There is no food spoilage, freshness or refrigerator requirement. Hunger still uses the existing food/exhaustion state, without adding saturation. Prepared meals restore more in one eating action. Baseline idle food drain is one point per 102.4 seconds, with additional movement/mining/healing expenditure; that calculation is not a measured play-session food cadence.

## Cooker operation

Right-click or Interact opens the cooker. Select a recipe, insert ingredients in any of three input slots, supply basic fuel or electricity, and take the output. Recipe changes retain input items but reset partial work. The shared bounded ingredient planner validates the full transaction and output space before consuming ingredients.

The Electric Cooker requests 200 W while able to process. Lower allocation advances work proportionally, consuming exactly allocated electricity. Both variants pause for invalid/missing ingredients, full output, signal OFF or dormant terrain. Basic cookers retain unused paid heat while paused/idle; normal furnaces retain their existing continuously burning ignition rule. Optional Blue Signal controls either cooker. Electrical priority and all-face grid connections use the ordinary machine system.

Pipe inputs add only units that satisfy a still-unmet recipe requirement and leave enough slots/capacity to finish the batch. Overlapping tags and mixed members use the same compiled matcher as final consumption. Manual loading can hold larger stacks; surplus does not prevent pipes from supplying missing ingredients. Basic fuel is accepted only through the rear, ingredients through the other five faces. Electric ingredients enter through all faces. Only finished output can be extracted by pipes. Independent wrench-configured input/output/disconnected ends remain unchanged.

The browser and wiki show each tagged requirement's alternatives rather than treating every example as compulsory. Food recipes run in cookers; the ordinary furnace/electric furnace keep their existing processing catalog, including baked potato compatibility.

## Saves and generation policy

Schema 10 stores cooker selection/signature alongside existing partial work, inventory and fuel fields. It records a generation version per generated chunk, including chunks sampled for its one-cell halo. Already recorded terrain keeps its original generator; new ungenerated terrain uses the newest supported version. See [the durable generation rule](SAVES.md#generated-chunk-policy--2026-09-13).

Older content fingerprints omit only this increment's new items/recipes/catalogs and appended tags while still checking all pre-existing fields and content. Unknown identities, invalid work/storage and damaged snapshots reject through existing failed-load rollback.

[Current verification](verification/STABILITY_REVIEW_RESULTS.md) and [original asset evidence](verification/FARMING_RESULTS.md) separate measured checks from remaining play review. [Farming and cooking](wiki/Farming-and-cooking.md) is the player guide.

## Stability review and tag authoring — 2026-09-13

`ItemRegistry.Select` compiles an exact stable ID or `#tag` into an immutable membership lookup. Tags use lowercase letters, digits and underscores, start with a letter and are at most 64 characters; duplicate/invalid tags and empty recipe selectors are rejected. Registry edits invalidate compiled selectors. `IngredientMatcher` consumes `RecipeIngredient` requirements and is independent of cooker fuel, power, presentation and world state. It supports up to nine input slots and nine total requirement units; the cooker selects three slots. It validates that a recipe can fit before publishing the catalog, and uses caller-owned work buffers during simulation. Pipe staging verifies a useful additional unit and a possible completion, including overlapping categories, full stacks and manually supplied surplus.

Cooking authoring stays in `CookingCatalog`; station input/state belongs to `CookerState`; execution belongs to `CookerSimulation`. All burners use the compiled processing fuel lookup. Cooker signatures are rebuilt only when input identities or the selected recipe change. Crop catalog validation completes before changing any live growth defaults.

The item sidebar and Creative item search accept intersecting name and tag terms: `#edible`, `#vegetable #edible`, `#ingot iron`. Material tags distinguish logs, planks, raw metal ores and metal ingots; iron/copper/gold remain distinct recipe identities. Use tag selectors when every member is an intended substitute. A broad `#ingot` must not accidentally permit copper in an iron-tier tool recipe. Existing grid layouts/quantities remain exact. Tool capabilities/tiers, armor slots/protection, crop planting definitions and explicit processing outputs already have typed authorities; duplicating those into behavioural tags would create conflicting rules. Future compost/feed tags should arrive with their actual consumers and balancing data, not imply systems that are absent.

The review adds only the frozen material tags to existing item definitions. Schema-10 compatibility can project those exact additions away for pre-review saves while retaining all earlier tags, fields and catalogs. Machine state encoding and generation history are unchanged. [Review verification](verification/STABILITY_REVIEW_RESULTS.md) records tests and remaining limits.
